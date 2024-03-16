using p3rpc.nativetypes.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Mod.Interfaces;
using Reloaded.Hooks.Definitions;

namespace p3rpc.commonmodutils
{
    public abstract class Context
    {
        public long _baseAddress { get; init; }
        public IConfigurable _config { get; set; }
        public ILogger _logger { get; init; }
        public IStartupScanner _startupScanner { get; init; }
        public IReloadedHooks _hooks { get; init; }
        public Utils _utils { get; init; }
        public Memory _memory { get; init; }
        public string _modLocation { get; init; }

        public Context(long baseAddress, IConfigurable config, ILogger logger, IStartupScanner startupScanner,
            IReloadedHooks hooks, string modLocation, Utils utils, Memory memory)
        {
            _baseAddress = baseAddress;
            _config = config;
            _logger = logger;
            _startupScanner = startupScanner;
            _hooks = hooks;
            _modLocation = modLocation;
            _utils = utils;
            _memory = memory;
        }
    }

    // For Unreal Engine games (Persona 3 Reload, future UE Atlus games)
    public class UnrealContext : Context
    {
        public unsafe FNamePool* g_namePool { get; private set; }
        public unsafe FUObjectArray* g_objectArray { get; private set; }

        protected string FUObjectArray_SIG = "48 8B 05 ?? ?? ?? ?? 48 8B 0C ?? 48 8D 04 ?? 48 85 C0 74 ?? 44 39 40 ?? 75 ?? F7 40 ?? 00 00 00 30 75 ?? 48 8B 00";
        protected string FGlobalNamePool_SIG = "4C 8D 05 ?? ?? ?? ?? EB ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 4C 8B C0 C6 05 ?? ?? ?? ?? 01 48 8B 44 24 ?? 48 8B D3 48 C1 E8 20 8D 0C ?? 49 03 4C ?? ?? E8 ?? ?? ?? ?? 48 8B C3";

        protected nuint TransformAddressForFUObjectArray(int offset) => Utils.GetGlobalAddress((nint)(_baseAddress + offset + 3)) - 0x10;
        public UnrealContext(long baseAddress, IConfigurable config, ILogger logger, IStartupScanner startupScanner,
            IReloadedHooks hooks, string modLocation, Utils utils, Memory memory)
            : base(baseAddress, config, logger, startupScanner, hooks, modLocation, utils, memory)
        {
            unsafe
            {
                _utils.SigScan(FUObjectArray_SIG, "FUObjectArray", TransformAddressForFUObjectArray, addr => g_objectArray = (FUObjectArray*)addr);
                _utils.SigScan(FGlobalNamePool_SIG, "FGlobalNamePool", _utils.GetIndirectAddressLong, addr => g_namePool = (FNamePool*)addr);
            }
        }

        // TODO: Find Object (like in UE4SS)

        /*
        public unsafe UObject* FindObject(string name)
        {
            
        }
        */
    }
}
