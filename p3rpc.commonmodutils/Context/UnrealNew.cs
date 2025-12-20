using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using SharedScans.Interfaces;
using UE.Toolkit.Core.Types;
using UE.Toolkit.Core.Types.Unreal.UE5_4_4;
using UE.Toolkit.Interfaces;

#pragma warning disable CS1591

namespace p3rpc.commonmodutils
{
    /// <summary>
    /// For Unreal Engine games (Persona 3 Reload, SMTVV, Persona 4 Revival, Persona 6 etc.)
    /// Uses Ryo's UE.Toolkit: https://github.com/RyoTune/UE.Toolkit. Supersedes UnrealContext.
    /// </summary>
    public class UnrealToolkitContext : Context
    {
        public IUnrealStrings _toolkitStrings { get; private set; }
        public IUnrealObjects _toolkitObjects { get; private set; }
        public IUnrealMemory _toolkitMemory { get; private set; }
        public IUnrealClasses _toolkitClasses { get; private set; }
        public UnrealToolkitContext(long baseAddress, IConfigurable config, ILogger logger, IStartupScanner startupScanner, 
            IReloadedHooks hooks, string modLocation, Utils utils, Memory memory, ISharedScans sharedScans, 
            IUnrealStrings toolkitStrings, IUnrealObjects toolkitObjects, IUnrealMemory toolkitMemory,
            IUnrealClasses toolkitClasses)
            : base(baseAddress, config, logger, startupScanner, hooks, modLocation, utils, memory, sharedScans)
        {
            _toolkitStrings = toolkitStrings;
            _toolkitObjects = toolkitObjects;
            _toolkitMemory = toolkitMemory;
            _toolkitClasses = toolkitClasses;
        }

        public string GetFName(FName name) => name.ToString();
        public unsafe string GetObjectName(UObjectBase* obj) => obj->NamePrivate.ToString();

        private unsafe string GetPathName(UObjectBase* obj, UObjectBase* end)
        {
            var path = GetObjectName(obj);
            if (obj->OuterPrivate != null)
            {
                var separator = obj == end ? ":" : ".";
                path = $"{GetPathName(obj->OuterPrivate, end)}{separator}{path}";
            }
            return path;
        }

        /// <summary>
        /// Path name used throughout UE4SS
        /// </summary>
        /// <param name="obj">UObject to get path name from</param>
        /// <returns></returns>
        public unsafe string GetFullName(UObjectBase* obj)
            => obj->OuterPrivate != null ? GetPathName(obj, obj) : GetObjectName(obj);

        public unsafe UClass* GetType(string type) =>
            _toolkitClasses.GetClassInfoFromName(type, out var Class) ? (UClass*)Class!.Ptr : null;
        public unsafe void GetTypeAsync(string type, Action<nint> foundCb) => throw new NotImplementedException();
        public unsafe bool IsObjectSubclassOf(UObjectBase* obj, UClass* type) => throw new NotImplementedException();
        public unsafe bool DoesNameMatch(UObjectBase* tgtObj, string name) => throw new NotImplementedException();
        public unsafe bool DoesClassMatch(UObjectBase* tgtObj, string name) => throw new NotImplementedException();
        public unsafe UObjectBase* GetEngineTransient() => throw new NotImplementedException();

        public unsafe void NotifyOnNewObject<TObject>(Action<Ptr<TObject>> cb) where TObject: unmanaged
            => _toolkitObjects.OnObjectLoadedByClass<TObject>(obj => cb(new Ptr<TObject>(obj.Self)));
    }
}
