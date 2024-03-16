using Reloaded.Memory;
using Reloaded.Memory.Interfaces;

namespace p3rpc.commonmodutils
{
    public class AddressToMemoryWrite
    {
        public nuint Address;
        public Action<nuint> WriteAtAddress;

        public AddressToMemoryWrite(Memory memory, nuint address, Action<nuint> writeAtAddress)
        {
            Address = address;
            WriteAtAddress = writeAtAddress;
            memory.ChangeProtection(address, 0x10, Reloaded.Memory.Enums.MemoryProtection.ReadWriteExecute);
            WriteAtAddress(Address); // run delegate for first time on init
        }
    }
}
