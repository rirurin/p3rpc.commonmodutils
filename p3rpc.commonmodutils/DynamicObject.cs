#pragma warning disable CS1591
using p3rpc.nativetypes.Interfaces;

namespace p3rpc.commonmodutils
{
    // Defines a fake Unreal class conatining a customizable vtable, size and constructor.
    // Can be 
    public unsafe class DynamicObject
    {
        public nint* vtableNative;
        public UClass* returnClass;
        
    }
}
