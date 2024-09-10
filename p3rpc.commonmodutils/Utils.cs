using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Drawing;
using System.Text;

#pragma warning disable CS1591

namespace p3rpc.commonmodutils
{
    public enum LogLevel
    {
        Verbose,
        Debug,
        Information,
        Warning,
        Error
    }
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
    public class Utils
    {
        private IStartupScanner _startupScanner;
        private IReloadedHooks _hooks;
        private ILogger _logger;
        private long _baseAddress;
        private string _name;
        private Color _color;
        private LogLevel _logLevel;

        public Utils(IStartupScanner startupScanner, ILogger logger, IReloadedHooks hooks, long baseAddress, string name, Color? color, LogLevel logLevel = LogLevel.Information)
        {
            _startupScanner = startupScanner;
            _hooks = hooks;
            _baseAddress = baseAddress;
            _logger = logger;
            _name = name;
            _color = color != null ? color.Value : Color.White;
            _logLevel = logLevel;
        }

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
                        } else
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
                    } else
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
        // Log defaults to a verbosity level of LogLevel.Information
        public void Log(string text) { if (_logLevel <= LogLevel.Information) _logger.WriteLineAsync($"[{_name}] {text}", _color); }
        public void Log(string text, Color customColor) { if (_logLevel <= LogLevel.Information) _logger.WriteLineAsync($"[{_name}] {text}", customColor); }
        public void Log(string text, LogLevel verbosity) { if (verbosity >= _logLevel) _logger.WriteLineAsync($"[{_name}] {text}", _color); }
        public void Log(string text, Color customColor, LogLevel verbosity) { if (verbosity >= _logLevel) _logger.WriteLineAsync($"[{_name}] {text}", customColor); }
        public nuint GetDirectAddress(int offset) => (nuint)(_baseAddress + offset);
        public nuint GetIndirectAddressShort(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 1);
        public nuint GetIndirectAddressShort2(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 2);
        public nuint GetIndirectAddressLong(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 3);
        public nuint GetIndirectAddressLong4(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 4);
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
}
