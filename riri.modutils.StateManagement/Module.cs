using System.Reflection;
using riri.modutils.ContextBase;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;

namespace riri.modutils.StateManagement;

/// <summary>
/// Base object for defining a module for a Reloaded mod. This is a component consisting of Load and Unload methods,
/// along with a shared context between it, other components and the runtime.
/// </summary>
/// <typeparam name="TContext"></typeparam>
public abstract class ModuleBase<TContext> : StateObject<TContext> where TContext : BaseContext
{
    internal bool IsLoaded { get; private set; }
    
    /// <inheritdoc/>
    protected ModuleBase() : base(null, []) {}

    /// <summary>
    /// Determines if the given module should be loaded by the runtime (should the runtime call <c>Load()</c>?).
    /// For example, you can configure a module to conditionally load at startup depending on a config option
    /// in your Reloaded mod.
    /// </summary>
    /// <returns>True if the module should be loaded, false otherwise</returns>
    public virtual bool ShouldLoad() => true;

    /// <summary>
    /// The method called when loading your module. Use this to initialize objects within your module such as
    /// function hooks. For anything that requires referencing the state of other modules, use <c>PostLoad()</c> instead.
    /// In p3rpc.commonmodutils/riri.commonmodutils, this is equivalent to the class's constructor.
    /// </summary>
    public virtual void Load()
    {
        IsLoaded = true;
    }

    internal void LoadInternal(TContext context, Dictionary<string, ModuleBase<TContext>> modules)
    {
        Context = context;
        Modules = modules;
        if (!ShouldLoad()) return;
        Load();
    }

    /// <summary>
    /// The method called when unloading your module. Use this to deallocate resources and clean up function hooks
    /// if your intend for parts of your mod to only be active some of the time.
    /// </summary>
    public virtual void Unload()
    {
        IsLoaded = false;
    }
    
    /// <summary>
    /// Called after every module registered by the runtime has had <c>Load()</c> called on this. If you rely on
    /// or need to reference objects from other modules, do it here.
    /// In p3rpc.commonmodutils/riri.commonmodutils, this is equivalent to <c>Register()</c>
    /// </summary>
    public virtual void PostLoad() {}
}

/// <summary>
/// Object for holding the shared context and modules.
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class ModuleRuntime<TContext> : StateObject<TContext> where TContext : BaseContext
{
    /// <summary>
    /// Initialize the mod runtime using the specified context. The list of modules that will get loaded is determined
    /// based on the assembly that the context is in. In other words, modules that are defined in the same .NET project
    /// as your context are automatically loaded.
    /// </summary>
    /// <param name="context">The context object</param>
    /// <exception cref="ModuleConstructorNotFoundException">Called if the default constructor is missing</exception>
    public ModuleRuntime(TContext context) : this(context, Assembly.GetAssembly(typeof(TContext))!) {}
    
    /// <summary>
    /// Initialize the mod runtime using the specified context and a reference to the assembly containing a list of
    /// modules to load. This is useful in cases where the context is defined in a different project from your modules.
    /// </summary>
    /// <param name="context">The context object</param>
    /// <param name="assembly">The assembly containing your modules</param>
    /// <exception cref="ModuleConstructorNotFoundException">Called if the default constructor is missing</exception>
    public ModuleRuntime(TContext context, Assembly assembly) : base(context, [])
    {
        if (Context.ModLoader != null)
        {
            Context.ModLoader.ModLoading += OnModLoadingInner;
            Context.ModLoader.ModLoaded += OnModLoadedInner;
            Context.ModLoader.ModUnloading += OnModUnloadingInner;
            Context.ModLoader.OnModLoaderInitialized += OnModLoaderInitializedInner;
        }
        var definedModules = assembly.GetTypes().Where(
            x => x.IsSubclassOf(typeof(ModuleBase<TContext>)));
        foreach (var moduleType in definedModules)
        {
            var moduleConstructor = moduleType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public,
                null,
                CallingConventions.HasThis,
                [],
                null
            );
            if (moduleConstructor == null)
            {
                throw new ModuleConstructorNotFoundException(moduleType);
            }
            var newModule = (ModuleBase<TContext>)moduleConstructor.Invoke([]);
            Modules.Add(moduleType.Name, newModule);
        }

        foreach (var module in Modules.Values) module.LoadInternal(Context, Modules);
        foreach (var module in Modules.Values) module.PostLoad();
    }

    /// <summary>
    /// Called when the Reloaded mod invokes the "ConfigurationUpdated" method, which happens in events such as
    /// clicking Save on the config dialog or closing the config window.
    /// </summary>
    /// <param name="newConfig">The value of the new config object</param>
    public void OnConfigUpdated(IConfigurable newConfig)
    {
        Context.OnConfigUpdated(newConfig);
        foreach (var module in Modules.Values.Where(module => module.IsLoaded != module.ShouldLoad()))
        {
            if (module.IsLoaded)
            {
                module.Unload();
            }
            else
            {
                module.Load();
                module.PostLoad();
            }
        }
    }

    private void OnModLoadingInner(IModV1 modv1, IModConfigV1 modConfigv1)
    {
        var mod = (IMod)modv1;
        var config = (IModConfig)modConfigv1;
        OnModLoading(mod, config);
        foreach (var (_, module) in Modules)
            module.OnModLoading(mod, config);
    }

    private void OnModLoadedInner(IModV1 modv1, IModConfigV1 modConfigv1)
    {
        var mod = (IMod)modv1;
        var config = (IModConfig)modConfigv1;
        OnModLoaded(mod, config);
        foreach (var (_, module) in Modules)
            module.OnModLoaded(mod, config);
    }

    private void OnModUnloadingInner(IModV1 modv1, IModConfigV1 modConfigv1)
    {
        var mod = (IMod)modv1;
        var config = (IModConfig)modConfigv1;
        OnModUnloading(mod, config);
        foreach (var (_, module) in Modules)
            module.OnModUnloading(mod, config);
    }

    private void OnModLoaderInitializedInner()
    {
        OnModLoaderInitialized();
        foreach (var (_, module) in Modules)
            module.OnModLoaderInitialized();
    }
}