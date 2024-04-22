using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Mod.Interfaces;
using Reloaded.Hooks.Definitions;
using SharedScans.Interfaces;
using Unreal.ClassConstructor.Interfaces;
using Unreal.NativeTypes.Interfaces;

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
        private IClassExtender _classExtender;
        private IClassFactory _classFactory;
        private IObjectListeners _objectListeners;
        private IObjectSearch _objectSearch;
        private IObjectUtilities _objectUtils;

        // Talk to Unreal.ClassConstructor to access shared resources.
        public UnrealContext(long baseAddress, IConfigurable config, ILogger logger, IStartupScanner startupScanner, IReloadedHooks hooks, 
            string modLocation, Utils utils, Memory memory, ISharedScans sharedScans, IClassExtender classExtender, 
            IClassFactory classFactory, IObjectListeners objectListeners, IObjectSearch objectSearch, IObjectUtilities objectUtils)
            : base(baseAddress, config, logger, startupScanner, hooks, modLocation, utils, memory, sharedScans)
        {
            _classExtender = classExtender;
            _classFactory = classFactory;
            _objectListeners = objectListeners;
            _objectSearch = objectSearch;
            _objectUtils = objectUtils;
        }
        
        public unsafe string GetFName(FName name) => _objectUtils.GetFName(name);
        public unsafe string GetObjectName(UObject* obj) => _objectUtils.GetObjectName(obj);
        public unsafe string GetFullName(UObject* obj) => _objectUtils.GetFullName(obj);
        public unsafe string GetObjectType(UObject* obj) => _objectUtils.GetObjectType(obj);
        public unsafe UClass* GetType(string type) => _objectSearch.GetType(type);
        public unsafe void GetTypeAsync(string type, Action<nint> foundCb) => _objectSearch.GetTypeAsync(type, foundCb);
        public unsafe bool IsObjectSubclassOf(UObject* obj, UClass* type) => _objectUtils.IsObjectSubclassOf(obj, type);
        public unsafe bool DoesNameMatch(UObject* tgtObj, string name) => _objectUtils.DoesNameMatch(tgtObj, name);
        public unsafe bool DoesClassMatch(UObject* tgtObj, string name) => _objectUtils.DoesClassMatch(tgtObj, name);
        // Convenience functions
        public unsafe UObject* GetEngineTransient() => _objectSearch.GetEngineTransient();
        // void cb -> Action<UObject*>
        public unsafe void NotifyOnNewObject(UClass* type, Action<nint> cb) => _objectListeners.NotifyOnNewObject(type, cb);
        public unsafe void NotifyOnNewObject(string typeName, Action<nint> cb) => _objectListeners.NotifyOnNewObject(typeName, cb);
        /*
        TODO For Unreal.ClassConstructor
        public unsafe UObject* SpawnObject(UClass* type, UObject* owner)
        {
            var objParams = (FStaticConstructObjectParameters*)NativeMemory.AllocZeroed((nuint)sizeof(FStaticConstructObjectParameters));
            objParams->Class = type;
            objParams->Outer = owner;
            //objParams->Name = name;
            var newObj = StaticConstructObject_InternalImpl(objParams);
            NativeMemory.Free(objParams);
            return newObj;
        }
        public unsafe UObject* SpawnObject(string type, UObject* owner)
        {
            if (_classNameToType.TryGetValue(type, out var typePtr))
            {
                var objParams = (FStaticConstructObjectParameters*)NativeMemory.AllocZeroed((nuint)sizeof(FStaticConstructObjectParameters));
                objParams->Class = (UClass*)typePtr;
                objParams->Outer = owner;
                //objParams->Name = name;
                var newObj = StaticConstructObject_InternalImpl(objParams);
                NativeMemory.Free(objParams);
                return newObj;
            }
            return null;
        }
        public unsafe void FreeObject(UObject* obj)
        {
            // TODO: Invoke GMalloc::Free
        }
        public unsafe void FreeObject(string objName, string typeName)
        {
            // TODO: Invoke GMalloc::Free
        }
        */
        public unsafe void FindObjectAsync(string targetObj, string? objType, Action<nint> foundCb) => _objectSearch.FindObjectAsync(targetObj, objType, foundCb);
        public unsafe void FindObjectAsync(string targetObj, Action<nint> foundCb) => _objectSearch.FindObjectAsync(targetObj, foundCb);
        public unsafe void FindFirstOfAsync(string objType, Action<nint> foundCb) => _objectSearch.FindFirstOfAsync(objType, foundCb);
        public unsafe void FindAllOfAsync(string objType, Action<ICollection<nint>> foundCb) => _objectSearch.FindAllOfAsync(objType, foundCb);

    }
}
