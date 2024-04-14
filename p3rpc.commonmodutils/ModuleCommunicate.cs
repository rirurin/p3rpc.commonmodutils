#pragma warning disable CS1591
namespace p3rpc.commonmodutils
{
    public abstract class ModuleCommunicate<TContext> where TContext : Context
    {
        // Reloaded-II APIs
        protected TContext _context;
        protected Dictionary<string, ModuleBase<TContext>> _modules;

        public ModuleCommunicate(TContext context, Dictionary<string, ModuleBase<TContext>> modules)
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
}
