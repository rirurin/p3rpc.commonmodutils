using p3rpc.classconstructor.Interfaces;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using SharedScans.Interfaces;

#pragma warning disable CS1591

namespace p3rpc.commonmodutils
{
    /// <summary>
    /// For Unreal Engine games (Persona 3 Reload, SMTVV, Persona 4 Revival, Persona 6 etc.)
    /// For older mods which use Unreal Class Constructor. New mods should inherit from UnrealToolkitContext instead.
    /// </summary>
    [Obsolete("Switch to UnrealToolkitContext if possible")]
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
