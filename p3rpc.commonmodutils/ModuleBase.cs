using Reloaded.Mod.Interfaces;

namespace p3rpc.commonmodutils
{
    public abstract class ModuleBase<TContext> where TContext : Context
    {
        // Reloaded-II APIs
        protected TContext _context;
        protected readonly Dictionary<string, ModuleBase<TContext>> _modules;

        public ModuleBase(TContext context, Dictionary<string, ModuleBase<TContext>> modules)
        {
            _context = context;
            _modules = modules;
        }

        public abstract void Register();

        public TModule GetModule<TModule>() where TModule : ModuleBase<TContext>
        {
            _modules.TryGetValue(typeof(TModule).Name, out var module);
            if (module == null) throw new Exception($"No module exists with the name \"{typeof(TModule).Name}\"");
            return (TModule)module;
        }

        public virtual void OnConfigUpdated(IConfigurable newConfig) => _context._config = newConfig;
    }

    public abstract class ModuleAsmInlineColorEdit<TContext> : ModuleBase<TContext> where TContext : Context
    {
        protected List<AddressToMemoryWrite> _asmMemWrites;

        public unsafe ModuleAsmInlineColorEdit(TContext context, Dictionary<string, ModuleBase<TContext>> modules) : base(context, modules)
        {
            _asmMemWrites = new();
        }
        public override void OnConfigUpdated(IConfigurable newConfig)
        {
            base.OnConfigUpdated(newConfig);
            foreach (var mem in _asmMemWrites) mem.WriteAtAddress(mem.Address);
        }
    }
}
