using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Mod.Interfaces;
using Reloaded.Hooks.Definitions;
using SharedScans.Interfaces;
using p3rpc.nativetypes.Interfaces;
using p3rpc.classconstructor.Interfaces;

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

        public virtual void OnConfigUpdated(IConfigurable newConfig) => _config = newConfig;
    }

    // For Unreal Engine games (Persona 3 Reload, SMTVV, Persona 6 etc.)
    public class UnrealContext : Context
    {
        public IClassMethods _classMethods { get; private set; }
        public IObjectMethods _objectMethods { get; private set; }

        // Talk to Unreal.ClassConstructor to access shared resources.
        public UnrealContext(long baseAddress, IConfigurable config, ILogger logger, IStartupScanner startupScanner, IReloadedHooks hooks, 
            string modLocation, Utils utils, Memory memory, ISharedScans sharedScans, IClassMethods classMethods, IObjectMethods objectMethods)
            : base(baseAddress, config, logger, startupScanner, hooks, modLocation, utils, memory, sharedScans)
        {
            _classMethods = classMethods;
            _objectMethods = objectMethods;
        }
        public unsafe string GetFName(FName name) => _objectMethods.GetFName(name);
        public unsafe string GetObjectName(UObject* obj) => _objectMethods.GetObjectName(obj);
        public unsafe string GetFullName(UObject* obj) => _objectMethods.GetFullName(obj);
        public unsafe string GetObjectType(UObject* obj) => _objectMethods.GetObjectType(obj);
        public unsafe UClass* GetType(string type) => _objectMethods.GetType(type);
        public unsafe void GetTypeAsync(string type, Action<nint> foundCb) => _objectMethods.GetTypeAsync(type, foundCb);
        public unsafe bool IsObjectSubclassOf(UObject* obj, UClass* type) => _objectMethods.IsObjectSubclassOf(obj, type);
        public unsafe bool DoesNameMatch(UObject* tgtObj, string name) => _objectMethods.DoesNameMatch(tgtObj, name);
        public unsafe bool DoesClassMatch(UObject* tgtObj, string name) => _objectMethods.DoesClassMatch(tgtObj, name);
        // Convenience functions
        public unsafe UObject* GetEngineTransient() => _objectMethods.GetEngineTransient();
        // void cb -> Action<UObject*>
        public unsafe void NotifyOnNewObject(UClass* type, Action<nint> cb) => _objectMethods.NotifyOnNewObject(type, cb);
        public unsafe void NotifyOnNewObject(string typeName, Action<nint> cb) => _objectMethods.NotifyOnNewObject(typeName, cb);
        public unsafe void FindObjectAsync(string targetObj, string? objType, Action<nint> foundCb) => _objectMethods.FindObjectAsync(targetObj, objType, foundCb);
        public unsafe void FindObjectAsync(string targetObj, Action<nint> foundCb) => _objectMethods.FindObjectAsync(targetObj, foundCb);
        public unsafe void FindFirstOfAsync(string objType, Action<nint> foundCb) => _objectMethods.FindFirstOfAsync(objType, foundCb);
        public unsafe void FindAllOfAsync(string objType, Action<ICollection<nint>> foundCb) => _objectMethods.FindAllOfAsync(objType, foundCb);
        public unsafe void ExtendClass(string targetClass, uint newSize, IClassMethods.InternalConstructor? ctorHook = null) => _classMethods.AddUnrealClassExtender(targetClass, newSize, ctorHook);
    }
}
