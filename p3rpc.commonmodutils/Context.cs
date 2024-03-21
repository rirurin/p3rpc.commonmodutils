using p3rpc.nativetypes.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Mod.Interfaces;
using Reloaded.Hooks.Definitions;
using SharedScans.Interfaces;
using System.Data;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

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

    // For Unreal Engine games (Persona 3 Reload, future UE Atlus games)
    public class UnrealContext : Context
    {
        // "We have UE4SS at home"
        // UE4SS at home:
        public abstract class FindObjectBase
        {
            protected UnrealContext Context { get; init; }
            public FindObjectBase(UnrealContext context) { Context = context; }
            public abstract void Execute();
        }
        public class FindObjectByName : FindObjectBase
        {
            public string ObjectName { get; set; }
            public string? TypeName { get; set; }
            public Action<nint> FoundObjectCallback { get; set; } // Action<UObject*>
            public FindObjectByName(UnrealContext context, string objectName, string? typeName, Action<nint> foundCb)
                : base(context)
            {
                ObjectName = objectName;
                TypeName = typeName;
                FoundObjectCallback = foundCb;
            }
            public unsafe override void Execute()
            {
                var foundObj = Context.FindObject(ObjectName, TypeName);
                if (foundObj != null) FoundObjectCallback((nint)foundObj);
            }
        }
        public class FindObjectFirstOfType : FindObjectBase
        {
            public string TypeName { get; set; }
            public Action<nint> FoundObjectCallback { get; set; }
            public FindObjectFirstOfType(UnrealContext context, string typeName, Action<nint> foundCb)
                : base(context)
            {
                TypeName = typeName;
                FoundObjectCallback = foundCb;
            }
            public unsafe override void Execute()
            {
                var foundObj = Context.FindFirstOf(TypeName);
                if (foundObj != null) FoundObjectCallback((nint)foundObj);
            }
        }
        public class FindObjectAllOfType : FindObjectBase
        {
            public string TypeName { get; set; }
            public Action<ICollection<nint>> FoundObjectCallback { get; set; }
            public FindObjectAllOfType(UnrealContext context, string typeName, Action<ICollection<nint>> foundCb)
                : base(context)
            {
                TypeName = typeName;
                FoundObjectCallback = foundCb;
            }
            public unsafe override void Execute()
            {
                var foundObj = Context.FindAllOf(TypeName);
                if (foundObj != null) FoundObjectCallback(foundObj);
            }
        }
        public unsafe FNamePool* g_namePool { get; private set; }
        public unsafe FUObjectArray* g_objectArray { get; private set; }
        private Thread _findObjectThread { get; init; }

        private BlockingCollection<FindObjectBase> _findObjects = new();

        private IHook<ICommonMethods.StaticConstructObject_Internal> _staticConstructObject;
        private Dictionary<string, List<Action<nint>>> _objectListeners = new();
        private IHook<ICommonMethods.GetPrivateStaticClassBody> _staticClassBody;
        private Dictionary<string, nint> _classNameToType = new();
        protected nuint TransformAddressForFUObjectArray(int offset) => Utils.GetGlobalAddress((nint)(_baseAddress + offset + 3)) - 0x10;
        public UnrealContext(long baseAddress, IConfigurable config, ILogger logger, IStartupScanner startupScanner,
            IReloadedHooks hooks, string modLocation, Utils utils, Memory memory, ISharedScans sharedScans)
            : base(baseAddress, config, logger, startupScanner, hooks, modLocation, utils, memory, sharedScans)
        {
            unsafe
            {
                _sharedScans.CreateListener("FUObjectArray", addr => _utils.AfterSigScan(addr, TransformAddressForFUObjectArray, addr => g_objectArray = (FUObjectArray*)addr));
                _sharedScans.CreateListener("FGlobalNamePool", addr => _utils.AfterSigScan(addr, _utils.GetIndirectAddressLong, addr => g_namePool = (FNamePool*)addr));
                _sharedScans.CreateListener("StaticConstructObject_Internal", addr => _utils.AfterSigScan(addr, _utils.GetDirectAddress, addr => _staticConstructObject = _utils.MakeHooker<ICommonMethods.StaticConstructObject_Internal>(StaticConstructObject_InternalImpl, addr)));
                _sharedScans.CreateListener("GetPrivateStaticClassBody", addr => _utils.AfterSigScan(addr, _utils.GetDirectAddress, addr => _staticClassBody = _utils.MakeHooker<ICommonMethods.GetPrivateStaticClassBody>(GetPrivateStaticClassBodyImpl, addr)));
            }
            _findObjectThread = new Thread(new ThreadStart(ProcessObjectQueue));
            _findObjectThread.IsBackground = true;
            _findObjectThread.Start();
        }
        public unsafe string GetFName(FName name) => g_namePool->GetString(name);
        public unsafe string GetObjectName(UObject* obj) => g_namePool->GetString(obj->NamePrivate);

        private unsafe string GetPathName(UObject* obj, UObject* end)
        {
            var path = GetObjectName(obj);
            if (obj->OuterPrivate != null)
            {
                var separator = obj == end ? ":" : ".";
                path = $"{GetPathName(obj->OuterPrivate, end)}{separator}{path}";
            }
            return path;
        }
        public unsafe string GetFullName(UObject* obj) // path name used throughout UE4SS
        {
            return obj->OuterPrivate != null ? GetPathName(obj, obj) : GetObjectName(obj);
        }
        public unsafe string GetObjectType(UObject* obj) => g_namePool->GetString(((UObject*)obj->ClassPrivate)->NamePrivate);
        public unsafe UClass* GetType(string type) => (UClass*)FindObject(type, "Class");
        public unsafe void GetTypeAsync(string type, Action<nint> foundCb) => FindObjectAsync(type, "Class", foundCb);
        public unsafe bool IsObjectSubclassOf(UObject* obj, UClass* type)
        {
            var currType = obj->ClassPrivate;
            while (currType != null)
            {
                if (g_namePool->GetString(((UObject*)currType)->NamePrivate).Equals("Object")) break; // UObject is base type
                if (((UObject*)currType)->NamePrivate.Equals(((UObject*)type)->NamePrivate))
                    return true;
                currType = (UClass*)currType->_super.super_struct;
            }
            return false;
        }
        private unsafe bool DoesNameMatch(UObject* tgtObj, string name) => g_namePool->GetString(tgtObj->NamePrivate).Equals(name);
        private unsafe bool DoesClassMatch(UObject* tgtObj, string name) => g_namePool->GetString(((UObject*)tgtObj->ClassPrivate)->NamePrivate).Equals(name);
        private unsafe void ForEachObject(Action<nint> objItem)
        {
            for (int i = 0; i < g_objectArray->NumElements; i++)
            {
                var currObj = &g_objectArray->Objects[i >> 0x10][i & 0xffff];
                if (currObj->Object == null || (currObj->Flags & EInternalObjectFlags.Unreachable) != 0) continue;
                objItem((nint)currObj);
            }
        }
        public unsafe UObject* FindObject(string targetObj, string? objType = null)
        {
            UObject* ret = null;
            ForEachObject(currAddr =>
            {
                var currObj = (FUObjectItem*)currAddr;
                if (DoesNameMatch(currObj->Object, targetObj))
                {
                    if (objType == null || DoesClassMatch(currObj->Object, objType))
                    {
                        ret = currObj->Object;
                        return;
                    }
                }
            });
            return ret;
        }
        public unsafe ICollection<nint> FindAllObjectsNamed(string targetObj, string? objType = null)
        {
            var objects = new List<nint>();
            ForEachObject(currAddr =>
            {
                var currObj = (FUObjectItem*)currAddr;
                if (DoesNameMatch(currObj->Object, targetObj))
                {
                    if (objType == null || DoesClassMatch(currObj->Object, objType))
                        objects.Add((nint)currObj->Object);
                }
            });
            return objects;
        }
        public unsafe UObject* FindFirstOf(string objType)
        {
            UObject* ret = null;
            ForEachObject(currAddr =>
            {
                var currObj = (FUObjectItem*)currAddr;
                if (DoesClassMatch(currObj->Object, objType))
                {
                    ret = currObj->Object;
                    return;
                }
            });
            return ret;
        }
        public unsafe ICollection<nint> FindAllOf(string objType)
        {
            var objects = new List<nint>();
            ForEachObject(currAddr =>
            {
                var currObj = (FUObjectItem*)currAddr;
                if (DoesClassMatch(currObj->Object, objType))
                    objects.Add((nint)currObj->Object);
            });
            return objects;
        }
        // Convenience functions
        public unsafe UObject* GetEngineTransient() => FindObject("/Engine/Transient", "Package");
        // void cb -> Action<UObject*>
        public unsafe void NotifyOnNewObject(UClass* type, Action<nint> cb) => NotifyOnNewObject(GetObjectName((UObject*)type), cb);
        public unsafe void NotifyOnNewObject(string typeName, Action<nint> cb)
        {
            if (_objectListeners.TryGetValue(typeName, out var listener)) listener.Add(cb);
            else _objectListeners.Add(typeName, new() { cb });
        }
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
        public unsafe void FindObjectAsync(string targetObj, string? objType, Action<nint> foundCb) => _findObjects.Add(new FindObjectByName(this, targetObj, objType, foundCb));
        public unsafe void FindObjectAsync(string targetObj, Action<nint> foundCb) => FindObjectAsync(targetObj, null, foundCb);
        public unsafe void FindFirstOfAsync(string objType, Action<nint> foundCb) => _findObjects.Add(new FindObjectFirstOfType(this, objType, foundCb));
        public unsafe void FindAllOfAsync(string objType, Action<ICollection<nint>> foundCb) => _findObjects.Add(new FindObjectAllOfType(this, objType, foundCb));
        private void ProcessObjectQueue()
        {
            try
            {
                while (true)
                {
                    if (_findObjects.TryTake(out var currFindObj))
                        currFindObj.Execute();
                }
            } catch (OperationCanceledException) { } // Called during process termination
        }

        private unsafe UObject* StaticConstructObject_InternalImpl(FStaticConstructObjectParameters* pParams)
        {
            var newObj = _staticConstructObject.OriginalFunction(pParams);
            if (_objectListeners.TryGetValue(GetObjectType(newObj), out var listeners))
                foreach (var listener in listeners) listener((nint)newObj);
            return newObj;
        }

        private unsafe void GetPrivateStaticClassBodyImpl(
            nint packageName,
            nint name,
            UClass** returnClass,
            nint registerNativeFunc,
            uint size,
            uint align,
            uint flags,
            ulong castFlags,
            nint config,
            nint inClassCtor,
            nint vtableHelperCtorCaller,
            nint addRefObjects,
            nint superFn,
            nint withinFn,
            byte isDynamic,
            nint dynamicFn)
        {
            _staticClassBody.OriginalFunction(packageName, name, returnClass, registerNativeFunc, size, align, flags, castFlags, 
                config, inClassCtor, vtableHelperCtorCaller, addRefObjects, superFn, withinFn, isDynamic, dynamicFn);
            var className = Marshal.PtrToStringUni(name);
            if (className != null)
                _classNameToType.Add(className, *(nint*)returnClass);
        }
    }
}
