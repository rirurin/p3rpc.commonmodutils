using p3rpc.nativetypes.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Mod.Interfaces;
using Reloaded.Hooks.Definitions;
using SharedScans.Interfaces;

#pragma warning disable CS1591

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
        public ISharedScans _sharedScans { get; init; }

        public Context(long baseAddress, IConfigurable config, ILogger logger, IStartupScanner startupScanner,
            IReloadedHooks hooks, string modLocation, Utils utils, Memory memory, ISharedScans sharedScans)
        {
            _baseAddress = baseAddress;
            _config = config;
            _logger = logger;
            _startupScanner = startupScanner;
            _hooks = hooks;
            _modLocation = modLocation;
            _utils = utils;
            _memory = memory;
            _sharedScans = sharedScans;
        }
    }

    // For Unreal Engine games (Persona 3 Reload, future UE Atlus games)
    public class UnrealContext : Context
    {
        public unsafe FNamePool* g_namePool { get; private set; }
        public unsafe FUObjectArray* g_objectArray { get; private set; }
        protected nuint TransformAddressForFUObjectArray(int offset) => Utils.GetGlobalAddress((nint)(_baseAddress + offset + 3)) - 0x10;
        public UnrealContext(long baseAddress, IConfigurable config, ILogger logger, IStartupScanner startupScanner,
            IReloadedHooks hooks, string modLocation, Utils utils, Memory memory, ISharedScans sharedScans)
            : base(baseAddress, config, logger, startupScanner, hooks, modLocation, utils, memory, sharedScans)
        {
            unsafe
            {
                _sharedScans.CreateListener("FUObjectArray", addr => _utils.AfterSigScan(addr, TransformAddressForFUObjectArray, addr => g_objectArray = (FUObjectArray*)addr));
                _sharedScans.CreateListener("FGlobalNamePool", addr => _utils.AfterSigScan(addr, _utils.GetIndirectAddressLong, addr => g_namePool = (FNamePool*)addr));
            }
        }

        // TODO: Find Object by name
    }
}
