using Reloaded.Mod.Interfaces;
using System.Reflection;

#pragma warning disable CS1591

namespace p3rpc.commonmodutils
{
    public class ModuleRuntime<TContext> : ModuleCommunicate<TContext> where TContext : Context
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
}
