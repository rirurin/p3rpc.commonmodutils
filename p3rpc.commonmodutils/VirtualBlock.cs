#pragma warning disable CS1591
using System.Diagnostics.CodeAnalysis;

namespace p3rpc.commonmodutils
{
    // Represents a pointer to a location in memory + a length
    public class VirtualBlockEntry
    {
        public Func<nint> PtrCb { get; init; }
        public int Length { get; init; }
        // For a fixed memory location
        public VirtualBlockEntry(nint ptr, int length)
        {
            PtrCb = () => ptr;
            Length = length;
        }
        // For a memory location that may move
        public VirtualBlockEntry(Func<nint> ptrCb, int length)
        {
            PtrCb = ptrCb;
            Length = length;
        }
    }
    // Combines a group of disparate byte arrays into a single contiguous block.
    public class VirtualBlock
    {
        private int Length { get; init; } = 0;
        private List<VirtualBlockEntry> Entries { get; init; } = new();
        public VirtualBlock Append(nint ptr, int length)
        {
            Entries.Add(new VirtualBlockEntry(ptr, length));
            return this;
        }
        public VirtualBlock Append(Func<nint> ptrCb, int length)
        {
            Entries.Add(new VirtualBlockEntry(ptrCb, length));
            return this;
        }
        // O(N) - move away from linear search at some point...
        public unsafe bool Get<T>(int vPos, [MaybeNullWhen(false)] out T target) where T : unmanaged
        {
            target = default;
            if (vPos > Length) return false;
            int cursor = 0;
            int i = 0;
            for (; i < Entries.Count; i++)
            {
                if (cursor + Entries[i].Length > vPos) break;
                cursor += Entries[i].Length;
            }
            target = *(T*)(Entries[i].PtrCb() + vPos);
            return true;
        }
    }
}
