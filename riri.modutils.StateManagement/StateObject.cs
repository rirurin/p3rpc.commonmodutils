using Reloaded.Mod.Interfaces;
using riri.modutils.ContextBase;

namespace riri.modutils.StateManagement;

/// <summary>
/// Base object for holding context and program modules
/// </summary>
/// <typeparam name="TContext">Type of context object</typeparam>
/// <param name="context">Context object</param>
/// <param name="modules">List of existing modules</param>
public abstract class StateObject<TContext>(
    TContext? context, Dictionary<string, ModuleBase<TContext>> modules
    ) where TContext: BaseContext
{
    /// <summary>
    /// Context objects
    /// </summary>
    public TContext Context { get; protected set; } = context!;
    /// <summary>
    /// Module map
    /// </summary>
    protected Dictionary<string, ModuleBase<TContext>> Modules { get; set; } = modules;

    /// <summary>
    /// Try to get a module given a data type. It will return the module instance if it was loaded.
    /// </summary>
    /// <typeparam name="TModule">Data type for the module</typeparam>
    /// <param name="module">The module instance if it was loaded</param>
    /// <returns>True if the moduled was loaded, otherwise false</returns>
    public bool GetModule<TModule>(out TModule? module) where TModule : ModuleBase<TContext>
    {
        module = Modules.TryGetValue(typeof(TModule).Name, out var moduleRaw) && moduleRaw.ShouldLoad() ? (TModule)moduleRaw : null;
        return module != null;
    }
    
    /// <summary>
    /// Called by the Reloaded mod loader when a mod is loading
    /// </summary>
    /// <param name="mod">Controls for the loading mod</param>
    /// <param name="modConfig">Configuration for the loading mod</param>
    public virtual void OnModLoading(IMod mod, IModConfig modConfig) {}
    
    /// <summary>
    /// Called by the Reloaded mod loader when a mod has finished loading
    /// </summary>
    /// <param name="mod">Controls for the loaded mod</param>
    /// <param name="modConfig">Configuration for the loaded mod</param>
    public virtual void OnModLoaded(IMod mod, IModConfig modConfig) {}
    
    /// <summary>
    /// Called by the Reloaded mod loader when a mod is unloading 
    /// </summary>
    /// <param name="mod">Controls for the unloaded mod</param>
    /// <param name="modConfig">Configuration for the unloaded mod</param>
    public virtual void OnModUnloading(IMod mod, IModConfig modConfig) {}
    
    /// <summary>
    /// Called by the Reloaded mod loader when the mod loader has finished initialization
    /// </summary>
    public virtual void OnModLoaderInitialized() {}
}