using Reloaded.Mod.Interfaces;

#pragma warning disable CS1591

namespace p3rpc.commonmodutils
{
    public abstract class ModuleBase<TContext> : ModuleCommunicate<TContext> where TContext : Context
    {
        public ModuleBase(TContext context, Dictionary<string, ModuleBase<TContext>> modules)
            : base(context, modules) { }

        public abstract void Register();
        public virtual void OnConfigUpdated(IConfigurable newConfig) => _context.OnConfigUpdated(newConfig);
    }

    public abstract class ModuleAsmInlineColorEdit<TContext> : ModuleBase<TContext> where TContext : Context
    {
        protected List<AddressToMemoryWrite> _asmMemWrites;

        public unsafe ModuleAsmInlineColorEdit(TContext context, Dictionary<string, ModuleBase<TContext>> modules) 
            : base(context, modules) { _asmMemWrites = new(); }
        public override void OnConfigUpdated(IConfigurable newConfig)
        {
            base.OnConfigUpdated(newConfig);
            foreach (var mem in _asmMemWrites) mem.WriteAtAddress(mem.Address);
        }
    }
}
