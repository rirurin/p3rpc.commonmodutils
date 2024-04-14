#pragma warning disable CS1591
using Reloaded.Hooks.Definitions;

namespace p3rpc.commonmodutils
{
    // Extend a particular class (increase size, hook to ctor)
    public class UnrealClassExtender
    {
        public uint Size { get; init; }
        public InternalConstructor? CtorHook { get; set; }
        public IHook<InternalConstructor>? CtorHookReal { get; set; }
        public UnrealClassExtender
            (uint size, InternalConstructor? ctorHook)
        {
            Size = size;
            CtorHook = ctorHook;
            CtorHookReal = null;
        }

        public unsafe delegate void InternalConstructor(nint alloc);
    }
}
