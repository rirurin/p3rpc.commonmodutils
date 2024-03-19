using p3rpc.nativetypes.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Mod.Interfaces;
using Reloaded.Hooks.Definitions;
using SharedScans.Interfaces;
using System.Data;
using System.Collections.Concurrent;

#pragma warning disable CS1591

namespace p3rpc.commonmodutils
{
    public abstract class Context
    {
        public static int elementsPerChunk = 65536;
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
            _findObjectThread = new Thread(new ThreadStart(ProcessObjectQueue));
            _findObjectThread.IsBackground = true;
            _findObjectThread.Start();
        }
        // "We have UE4SS at home"
        // UE4SS at home:
        public unsafe string GetObjectName(UObject* obj) => g_namePool->GetString(obj->NamePrivate);
        public unsafe string GetObjectType(UObject* obj) => g_namePool->GetString(((UObject*)obj->ClassPrivate)->NamePrivate);
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
            /*
            for (int i = 0; i < g_objectArray->NumElements; i++)
            {
                var currObj = &g_objectArray->Objects[i >> 0x10][i & 0xffff];
                if (currObj->Object == null || (currObj->Flags & EInternalObjectFlags.Unreachable) != 0) continue;
                if (DoesNameMatch(currObj->Object, targetObj))
                {
                    if (objType == null || DoesClassMatch(currObj->Object, objType))
                        return currObj->Object;
                }
                //_logger.WriteLineAsync($"FindObject: ({g_namePool->GetString(((UObject*)currObj->Object->ClassPrivate)->NamePrivate)}) {g_namePool->GetString(currObj->Object->NamePrivate)}");
            }
            return null;
            */
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
    }
}
