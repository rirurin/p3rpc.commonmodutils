#pragma warning disable CS1591
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace riri.commonmodutils;

/// <summary>
/// Defines a target logging level for a particular message. If the config's log level is as or less verbose than a particular log, 
/// it'll be written to the Reloaded console
/// </summary>
public enum LogLevel
{
    Verbose,
    Debug,
    Information,
    Warning,
    Error
}
/// <summary>
/// Basic implementation to map multiple memory signatures to a single transform + response function. Designed for
/// cases where we can assume that the implementation of the code being hooked won't change significantly between
/// updates.
/// </summary>
public class MultiSignature
{
    public readonly object __sigLock;
    public int registeredSignatures { get; set; }
    public nuint? returnedAddress { get; set; }
    public MultiSignature()
    {
        __sigLock = new object();
        returnedAddress = null;
        registeredSignatures = 0;
    }
}
/// <summary>
/// Create an entry for an AdvancedMultiSignature, with a distinct pattern, validator (choose to sigscan or not depending on user code), address transform and response
/// </summary>
public class AdvancedMultiSignatureEntry
{
    public string Pattern;
    public Func<bool> Validator;
    public Func<int, nuint> Transform;
    public Action<long> Response;
    public AdvancedMultiSignatureEntry(string _Pattern, Func<bool> _Validator, Func<int, nuint> _Transform, Action<long> _Response)
    {
        Pattern = _Pattern;
        Validator = _Validator;
        Transform = _Transform;
        Response = _Response;
    }
}
/// <summary>
/// Advanced implementation for handling multiple memory signatures where each signature maps to a set of shared transform
/// and response functions. Signatures can be masked based on the XXH hash of the program, if the option for that is enabled in utilites. 
/// Initialization requires calling the CreateAdvancedMultiSignature function in the Utils class to capture dependencies. 
/// Useful in cases where the implementation is expected to change between updates, particularly with assembly hooks.
/// </summary>
public class AdvancedMultiSignature
{
    public readonly object __sigLock;
    public List<AdvancedMultiSignatureEntry> Entries;
    public int SignaturesScanned;
    public string Name;
    public nuint? ReturnedAddress;
    private IStartupScanner _startupScanner;
    private Action<string> DebugLog;
    private Action<string> ErrorLog;
    public AdvancedMultiSignature(List<AdvancedMultiSignatureEntry> _Entries, string _Name, IStartupScanner startupScanner, Action<string> _DebugLog, Action<string> _ErrorLog)
    {
        Entries = _Entries;
        SignaturesScanned = 0;
        __sigLock = new object();
        DebugLog = _DebugLog;
        ErrorLog = _ErrorLog;
        Name = _Name;
        _startupScanner = startupScanner;
        foreach (var entry in Entries)
        {
            if (!entry.Validator())
            {
                // Don't bother setting up a sigscan if we know this doesn't apply to this executable
                continue;
            }
            _startupScanner.AddMainModuleScan(entry.Pattern, result =>
            {
                lock (__sigLock) { SignaturesScanned++; }
                if (!result.Found)
                {
                    if (ReturnedAddress != null)
                    {
                        DebugLog($"Location {Name} was already found in a candidate pattern");
                    }
                    else if (SignaturesScanned == Entries.Count)
                    {
                        ErrorLog($"Couldn't find location for {Name}, stuff will break :(");
                    }
                    else
                    {
                        DebugLog($"Couldn't find location for {Name} using pattern {entry.Pattern}, trying with another pattern...");
                    }
                    return;
                }
                var callHookCb = false;
                lock (__sigLock)
                {
                    if (ReturnedAddress == null)
                    {
                        ReturnedAddress = entry.Transform(result.Offset);
                        callHookCb = true;
                    }
                }
                if (callHookCb)
                {
                    DebugLog($"Found {Name} at 0x{ReturnedAddress:X}");
                    entry.Response((long)ReturnedAddress);
                }
                else
                {
                    DebugLog($"Location {Name} was already found in a candidate pattern");
                    return;
                }
            });
        }
    }
}
/// <summary>
/// Provides common utility instances for mod components.
/// </summary>
public class Utils
{
    private IModLoader? _modLoader;
    private IStartupScanner _startupScanner;
    private IReloadedHooks _hooks;
    private ILogger _logger;
    private long _baseAddress;
    private string _name;
    private Color _color;
    private LogLevel _logLevel;
    private ulong? _moduleHashValue;

    [Obsolete("Constructor does not create an IModLoader instance. Please use Utils.Create() instead.")]
    public Utils(IStartupScanner startupScanner, ILogger logger, IReloadedHooks hooks, long baseAddress, string name, Color? color, LogLevel logLevel = LogLevel.Information)
    {
        _startupScanner = startupScanner;
        _hooks = hooks;
        _baseAddress = baseAddress;
        _logger = logger;
        _name = name;
        _color = color ?? Color.White;
        _logLevel = logLevel;
    }

    [Obsolete("Constructor does not create an IModLoader instance. Please use Utils.Create() instead.")]
    public Utils(IStartupScanner startupScanner, ILogger logger, IReloadedHooks hooks, long baseAddress, string name, Color? color, LogLevel logLevel, ulong? ProcessHash)
    {
        _startupScanner = startupScanner;
        _hooks = hooks;
        _baseAddress = baseAddress;
        _logger = logger;
        _name = name;
        _color = color ?? Color.White;
        _logLevel = logLevel;
        // I'll have to trust that the hash being sent the correct one for the executable - I don't want to have to read the exe each time a Utils instance is made lol
        _moduleHashValue = ProcessHash;
    }

    private Utils(IModLoader modLoader, IStartupScanner startupScanner, ILogger logger, IReloadedHooks hooks, long baseAddress, string name, Color? color, LogLevel logLevel, ulong? ProcessHash)
    {
        _modLoader = modLoader;
        _startupScanner = startupScanner;
        _hooks = hooks;
        _baseAddress = baseAddress;
        _logger = logger;
        _name = name;
        _color = color ?? Color.White;
        _logLevel = logLevel;
        // I'll have to trust that the hash being sent the correct one for the executable - I don't want to have to read the exe each time a Utils instance is made lol
        _moduleHashValue = ProcessHash;
    }

    /// <summary>
    /// Initializes an instance of commonmodutils' Utils class. Create one instance of this in your mod's startup.
    /// </summary>
    /// <param name="modLoader"></param>
    /// <param name="startupScanner"></param>
    /// <param name="logger"></param>
    /// <param name="hooks"></param>
    /// <param name="baseAddress"></param>
    /// <param name="name"></param>
    /// <param name="color"></param>
    /// <param name="logLevel"></param>
    /// <returns></returns>
    public static Utils Create(IModLoader modLoader, IStartupScanner startupScanner, ILogger logger, IReloadedHooks hooks, long baseAddress, string name, Color? color, LogLevel logLevel = LogLevel.Information)
        => Create(modLoader, startupScanner, logger, hooks, baseAddress, name, color, logLevel, null);

    /// <summary>
    /// Initializes an instance of commonmodutils' Utils class, including the processor hash. Create one instance of this in your mod's startup.
    /// </summary>
    /// <param name="modLoader"></param>
    /// <param name="startupScanner"></param>
    /// <param name="logger"></param>
    /// <param name="hooks"></param>
    /// <param name="baseAddress"></param>
    /// <param name="name"></param>
    /// <param name="color"></param>
    /// <param name="logLevel"></param>
    /// <param name="ProcessHash"></param>
    /// <returns></returns>
    public static Utils Create(IModLoader modLoader, IStartupScanner startupScanner, ILogger logger, IReloadedHooks hooks, long baseAddress, string name, Color? color, LogLevel logLevel, ulong? ProcessHash)
        => new Utils(modLoader, startupScanner, logger, hooks, baseAddress, name, color, logLevel, ProcessHash);

    /// <summary>
    /// Gets the address of a global from something that references it
    /// </summary>
    /// <param name="ptrAddress">The address to the pointer to the global (like in a mov instruction or something)</param>
    /// <returns>The address of the global</returns>
    public static unsafe nuint GetGlobalAddress(nint ptrAddress) => (nuint)((*(int*)ptrAddress) + ptrAddress + 4);

    public void SigScan(string pattern, string name, Func<int, nuint> transformCb, Action<long> hookerCb)
    {
        _startupScanner.AddMainModuleScan(pattern, result =>
        {
            if (!result.Found)
            {
                Log($"Couldn't find location for {name}, stuff will break :(", Color.Red, LogLevel.Error);
                return;
            }
            var addr = transformCb(result.Offset);
            Log($"Found {name} at 0x{addr:X}", LogLevel.Debug);
            hookerCb((long)addr);
        });
    }

    // Signature scan using multiple candidate signatures. This is useful in situations where the signature varies
    // between versions of the executable and the executable doesn't update the version value in it's metadata
    // (e.g Persona 3 Reload always reports version number 1.0.0.0 (Win64) or 4.27.2.0 (WinGDK) no matter the patch version)
    public void MultiSigScan(string[] patterns, string name, Func<int, nuint> transformCb, Action<long> hookerCb, MultiSignature sync)
    {
        sync.registeredSignatures = patterns.Length;
        foreach (var pattern in patterns)
        {
            _startupScanner.AddMainModuleScan(pattern, result =>
            {
                lock (sync.__sigLock)
                {
                    sync.registeredSignatures--;
                }
                if (!result.Found)
                {
                    if (sync.returnedAddress != null)
                    {
                        Log($"Location {name} was already found in a candidate pattern", LogLevel.Debug);
                    }
                    else if (sync.registeredSignatures == 0)
                    {
                        Log($"Couldn't find location for {name}, stuff will break :(", Color.Red, LogLevel.Error);
                    }
                    else
                    {
                        Log($"Couldn't find location for {name} using pattern {pattern}, trying with another pattern...", Color.Khaki, LogLevel.Debug);
                    }
                    return;
                }
                var callHookCb = false;
                lock (sync.__sigLock)
                {
                    if (sync.returnedAddress == null)
                    {
                        sync.returnedAddress = transformCb(result.Offset);
                        callHookCb = true;
                    }
                }
                if (callHookCb)
                {
                    Log($"Found {name} at 0x{sync.returnedAddress:X}", LogLevel.Debug);
                    hookerCb((long)sync.returnedAddress);
                }
                else
                {
                    Log($"Location {name} was already found in a candidate pattern", LogLevel.Debug);
                    return;
                }
            });
        }
    }

    // Used to run callbacks for signatures scanned using SharedScans, usually ones that are shared with multiple mods
    public void AfterSigScan(nint addr, Func<int, nuint> transformCb, Action<long> hookerCb)
    {
        var addrTransformed = transformCb((int)(addr - _baseAddress));
        hookerCb((long)addrTransformed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CreateAdvancedMultiSignature(List<AdvancedMultiSignatureEntry> _Entries, string _Name)
        => new AdvancedMultiSignature(_Entries, _Name, _startupScanner, text => Log(text, LogLevel.Debug), text => Log(text, Color.Red, LogLevel.Error));

    public bool ValidateSignaturesByHash(List<ulong> CandidateHashes)
    {
        foreach (ulong CandidateHash in CandidateHashes)
            if (ValidateSignatureByHash(CandidateHash))
                return true;
        return false;
    }
    public bool ValidateSignatureByHash(ulong CandidateHash) => _moduleHashValue.HasValue && _moduleHashValue.Value == CandidateHash;

    private unsafe nuint DerefInstructionPointerShort(nuint ptr, bool boundsCheck = true)
    {
        var ptr_new = ptr + (nuint)(*(sbyte*)(ptr + 1) + 2);
        return TryDerefInstructionPointer(ptr_new, boundsCheck);
    }

    private unsafe nuint DerefInstructionPointerNear(nuint ptr, bool boundsCheck = true)
    {
        var ptr_new = ptr + (nuint)(*(int*)(ptr + 1) + 5);
        return TryDerefInstructionPointer(ptr_new, boundsCheck);
    }

    private nuint TryDerefInstructionPointer(nuint ptr, bool boundsCheck = true)
    {
        if (boundsCheck && ptr < (nuint)_baseAddress) { return 0; }
        unsafe
        {
            return (*(byte*)ptr) switch
            {
                0xeb => DerefInstructionPointerShort(ptr, boundsCheck),
                0xe9 => DerefInstructionPointerNear(ptr, boundsCheck),
                _ => ptr
            };
        }
    }

    // Log defaults to a verbosity level of LogLevel.Information
    public void Log(string text) { if (_logLevel <= LogLevel.Information) _logger.WriteLineAsync($"[{_name}] {text}", _color); }
    public void Log(string text, Color customColor) { if (_logLevel <= LogLevel.Information) _logger.WriteLineAsync($"[{_name}] {text}", customColor); }
    public void Log(string text, LogLevel verbosity) { if (verbosity >= _logLevel) _logger.WriteLineAsync($"[{_name}] {text}", _color); }
    public void Log(string text, Color customColor, LogLevel verbosity) { if (verbosity >= _logLevel) _logger.WriteLineAsync($"[{_name}] {text}", customColor); }
    
    public nuint GetDirectAddress(int offset) => (nuint)(_baseAddress + offset);
    public nuint GetAddressMayThunk(int offset) => TryDerefInstructionPointer(GetDirectAddress(offset));
    public nuint GetIndirectAddressShort(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 1);
    public nuint GetIndirectAddressShort2(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 2);
    public nuint GetIndirectAddressLong(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 3);
    public nuint GetIndirectAddressLong4(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 4);

    public nuint GetAddressMayThunkAbsolute(nuint ptr) => TryDerefInstructionPointer(ptr, false);
    
    public IHook<T> MakeHooker<T>(T delegateMethod, long address) => _hooks.CreateHook(delegateMethod, address).Activate();
    public T MakeWrapper<T>(long address) => _hooks.CreateWrapper<T>(address, out _);

    // RCX, RDX, R8, R9
    public string PreserveMicrosoftRegisters() => $"push rcx\npush rdx\npush r8\npush r9";
    public string RetrieveMicrosoftRegisters() => $"pop r9\npop r8\npop rdx\npop rcx";

    // Pushes the value of an xmm register to the stack, saving it so it can be restored with PopXmm
    public static string PushXmm(int xmmNum)
    {
        return // Save an xmm register 
            $"sub rsp, 16\n" + // allocate space on stack
            $"movdqu dqword [rsp], xmm{xmmNum}\n";
    }

    // Pushes all xmm registers (0-15) to the stack, saving them to be restored with PopXmm
    public static string PushXmm()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 16; i++)
        {
            sb.Append(PushXmm(i));
        }
        return sb.ToString();
    }

    // Pops the value of an xmm register to the stack, restoring it after being saved with PushXmm
    public static string PopXmm(int xmmNum)
    {
        return                 //Pop back the value from stack to xmm
            $"movdqu xmm{xmmNum}, dqword [rsp]\n" +
            $"add rsp, 16\n"; // re-align the stack
    }

    // Pops all xmm registers (0-7) from the stack, restoring them after being saved with PushXmm
    public static string PopXmm()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 7; i >= 0; i--)
        {
            sb.Append(PopXmm(i));
        }
        return sb.ToString();
    }
}