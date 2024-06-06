#pragma warning disable CS1591
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using SharedScans.Interfaces;
namespace riri.commonmodutils;
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

    public virtual void OnConfigUpdated(IConfigurable newConfig) => _config = newConfig;
}
