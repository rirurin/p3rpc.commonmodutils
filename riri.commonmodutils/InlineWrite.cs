#pragma warning disable CS1591
using Reloaded.Memory;
using Reloaded.Memory.Interfaces;
namespace riri.commonmodutils;
public class InlineWrite
{
    public nuint Address;
    public Action<nuint> WriteAtAddress;

    public InlineWrite(Memory memory, nuint address, Action<nuint> writeAtAddress)
    {
        Address = address;
        WriteAtAddress = writeAtAddress;
        memory.ChangeProtection(address, 0x10, Reloaded.Memory.Enums.MemoryProtection.ReadWriteExecute);
        WriteAtAddress(Address); // run delegate for first time on init
    }
}

