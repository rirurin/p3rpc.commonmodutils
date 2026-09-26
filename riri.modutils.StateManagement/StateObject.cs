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
}