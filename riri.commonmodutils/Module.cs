#pragma warning disable CS1591
using Reloaded.Mod.Interfaces;
using System.Reflection;

namespace riri.commonmodutils;
public abstract class ModuleCommunication<TContext> where TContext : Context
{
    // Reloaded-II APIs
    protected TContext _context;
    protected Dictionary<string, ModuleBase<TContext>> _modules;

    public ModuleCommunication(TContext context, Dictionary<string, ModuleBase<TContext>> modules)
    {
        _context = context;
        _modules = modules;
    }

    public TModule GetModule<TModule>() where TModule : ModuleBase<TContext>
    {
        _modules.TryGetValue(typeof(TModule).Name, out var module);
        if (module == null) throw new Exception($"No module exists with the name \"{typeof(TModule).Name}\"");
        return (TModule)module;
    }

    public TModule? TryGetModule<TModule>() where TModule : ModuleBase<TContext>
    {
        if (_modules.TryGetValue(typeof(TModule).Name, out var module)) return (TModule)module;
        else return null;
    }
}

public abstract class ModuleBase<TContext> : ModuleCommunication<TContext> where TContext : Context
{
    public ModuleBase(TContext context, Dictionary<string, ModuleBase<TContext>> modules)
        : base(context, modules) { }

    public abstract void Register();
    public virtual void OnConfigUpdated(IConfigurable newConfig) => _context.OnConfigUpdated(newConfig);
}

public abstract class ModuleInlineWrite<TContext> : ModuleBase<TContext> where TContext : Context
{
    protected List<InlineWrite> _inlineWrite;
    public unsafe ModuleInlineWrite(TContext context, Dictionary<string, ModuleBase<TContext>> modules)
        : base(context, modules) { _inlineWrite = new(); }
    public override void OnConfigUpdated(IConfigurable newConfig)
    {
        base.OnConfigUpdated(newConfig);
        foreach (var mem in _inlineWrite) mem.WriteAtAddress(mem.Address);
    }
}

public class ModuleRuntime<TContext> : ModuleCommunication<TContext> where TContext : Context
{
    public ModuleRuntime(TContext context)
        : base(context, new()) { }
    public void AddModule<TModule>() where TModule : ModuleBase<TContext>
    {
        Type typeInfo = typeof(TModule);
        ConstructorInfo? construct = typeInfo.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis,
            [typeof(TContext), typeof(Dictionary<string, ModuleBase<TContext>>)], null
        );
        if (construct != null)
        {
            TModule newModule = (TModule)construct.Invoke(new object[] { _context, _modules });
            _modules.Add(typeInfo.Name, newModule);
            _context._utils.Log($"Added module {typeInfo.Name}");
        }
        else
        {
            throw new Exception($"Could not find appropriate constructor for type {typeInfo.Name}");
        }
    }
    public void RegisterModules() { foreach (var mod in _modules.Values) mod.Register(); }
    public void UpdateConfiguration(IConfigurable newConfig) { foreach (var mod in _modules.Values) mod.OnConfigUpdated(newConfig); }
}