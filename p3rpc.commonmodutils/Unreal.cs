using p3rpc.nativetypes.Interfaces;
using System.Runtime.InteropServices;

#pragma warning disable CS1591

namespace p3rpc.commonmodutils
{
    /// <summary>
    /// Convenience functions for allocating Unreal FStrings
    /// </summary>
    public class Unreal
    {
        public static unsafe FString* MakeFString(IMemoryMethods memoryMethods, string? text)
        {
            FString* newStr = memoryMethods.FMemory_Malloc<FString>();
            if (text != null)
            {
                text += "\0";
                newStr->text.allocator_instance = (nint*)memoryMethods.FMemory_Malloc(text.Length * 2, 8);
                nint marshallerToUtf16 = Marshal.StringToHGlobalUni($"{text}\0");
                NativeMemory.Copy((void*)marshallerToUtf16, newStr->text.allocator_instance, (nuint)(text.Length * 2));
                Marshal.FreeHGlobal(marshallerToUtf16);
                newStr->text.arr_num = text.Length;
                newStr->text.arr_max = text.Length;
            }
            else NativeMemory.Fill(newStr, (nuint)sizeof(FString), 0);
            return newStr;
        }

        public static unsafe void MakeFString(IMemoryMethods memoryMethods, FString* alloc, string? text)
        {
            if (text != null)
            {
                text += "\0";
                alloc->text.allocator_instance = (nint*)memoryMethods.FMemory_Malloc(text.Length * 2, 8);
                nint marshallerToUtf16 = Marshal.StringToHGlobalUni($"{text}\0");
                NativeMemory.Copy((void*)marshallerToUtf16, alloc->text.allocator_instance, (nuint)(text.Length * 2));
                Marshal.FreeHGlobal(marshallerToUtf16);
                alloc->text.arr_num = text.Length;
                alloc->text.arr_max = text.Length;
            }
        }

        public static unsafe TArray<TArrayType>* MakeArrayRef<TArrayType>(IMemoryMethods memoryMethods, int entries) where TArrayType : unmanaged
        {
            var arr = memoryMethods.FMemory_Malloc<TArray<TArrayType>>();
            arr->allocator_instance = memoryMethods.FMemory_MallocMultiple<TArrayType>((uint)entries);
            arr->arr_num = entries;
            arr->arr_max = entries;
            return arr;
        }
    }
}
