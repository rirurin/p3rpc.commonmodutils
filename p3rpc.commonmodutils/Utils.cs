using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace p3rpc.commonmodutils
{
    public class Utils
    {
        private IStartupScanner _startupScanner;
        private IReloadedHooks _hooks;
        private ILogger _logger;
        private long _baseAddress;
        private string _name;
        private Color _color;

        public Utils(IStartupScanner startupScanner, ILogger logger, IReloadedHooks hooks, long baseAddress, string name, Color? color)
        {
            _startupScanner = startupScanner;
            _hooks = hooks;
            _baseAddress = baseAddress;
            _logger = logger;
            _name = name;
            _color = color != null ? color.Value : Color.White;
        }

        /// <summary>
        /// Gets the address of a global from something that references it
        /// </summary>
        /// <param name="ptrAddress">The address to the pointer to the global (like in a mov instruction or something)</param>
        /// <returns>The address of the global</returns>
        public static unsafe nuint GetGlobalAddress(nint ptrAddress) => (nuint)((*(int*)ptrAddress) + ptrAddress + 4);

        public void SigScan(string pattern, string name, Func<int, nuint> transformCb, Action<long> hookerCb)
        {
            _startupScanner.AddMainModuleScan(pattern, result =>
            {
                if (!result.Found)
                {
                    Log($"Couldn't find location for {name}, stuff will break :(");
                    return;
                }
                var addr = transformCb(result.Offset);
                Log($"Found {name} at 0x{addr:X}");
                hookerCb((long)addr);
            });
        }
        public void Log(string text) => _logger.WriteLineAsync($"[{_name}] {text}", _color);
        public nuint GetDirectAddress(int offset) => (nuint)(_baseAddress + offset);
        public nuint GetIndirectAddressShort(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 1);
        public nuint GetIndirectAddressShort2(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 2);
        public nuint GetIndirectAddressLong(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 3);
        public nuint GetIndirectAddressLong4(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 4);
        public IHook<T> MakeHooker<T>(T delegateMethod, long address) => _hooks.CreateHook(delegateMethod, address).Activate();
        public T MakeWrapper<T>(long address) => _hooks.CreateWrapper<T>(address, out _);

        // Pushes the value of an xmm register to the stack, saving it so it can be restored with PopXmm
        public static string PushXmm(int xmmNum)
        {
            return // Save an xmm register 
                $"sub rsp, 16\n" + // allocate space on stack
                $"movdqu dqword [rsp], xmm{xmmNum}\n";
        }

        // Pushes all xmm registers (0-15) to the stack, saving them to be restored with PopXmm
        public static string PushXmm()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                sb.Append(PushXmm(i));
            }
            return sb.ToString();
        }

        // Pops the value of an xmm register to the stack, restoring it after being saved with PushXmm
        public static string PopXmm(int xmmNum)
        {
            return                 //Pop back the value from stack to xmm
                $"movdqu xmm{xmmNum}, dqword [rsp]\n" +
                $"add rsp, 16\n"; // re-align the stack
        }

        // Pops all xmm registers (0-7) from the stack, restoring them after being saved with PushXmm
        public static string PopXmm()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 7; i >= 0; i--)
            {
                sb.Append(PopXmm(i));
            }
            return sb.ToString();
        }
    }
}
