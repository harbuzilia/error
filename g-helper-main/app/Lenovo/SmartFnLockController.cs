using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GHelper.Lenovo
{
    /// <summary>
    /// Smart Fn Lock Controller - automatically disables Fn Lock when modifier keys (Ctrl/Shift/Alt) are pressed
    /// Based on LenovoLegionToolkit SmartFnLockController
    /// </summary>
    public class SmartFnLockController : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;

        // Virtual key codes
        private const int VK_LCONTROL = 0xA2;
        private const int VK_RCONTROL = 0xA3;
        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;
        private const int VK_LMENU = 0xA4;  // Left Alt
        private const int VK_RMENU = 0xA5;  // Right Alt

        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;
        private bool _ctrlDepressed;
        private bool _shiftDepressed;
        private bool _altDepressed;
        private bool _restoreFnLock;
        private bool _enabled;
        private ModifierKey _modifierFlags = ModifierKey.None;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public SmartFnLockController()
        {
            _proc = HookCallback;
        }

        /// <summary>
        /// Enable Smart Fn Lock controller with modifier flags
        /// </summary>
        public void Enable(ModifierKey modifierFlags = ModifierKey.None)
        {
            _modifierFlags = modifierFlags;
            
            if (_modifierFlags == ModifierKey.None)
            {
                Disable();
                return;
            }

            if (_enabled)
            {
                // Already enabled, just update flags (no need to re-hook)
                Logger.WriteLine($"Smart Fn Lock Controller flags updated to: {modifierFlags}");
                return;
            }

            try
            {
                _hookId = SetHook(_proc);
                _enabled = true;
                Logger.WriteLine($"Smart Fn Lock Controller enabled with flags: {modifierFlags}");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to enable Smart Fn Lock Controller: {ex.Message}");
            }
        }

        /// <summary>
        /// Disable Smart Fn Lock controller
        /// </summary>
        public void Disable()
        {
            if (!_enabled)
                return;

            try
            {
                if (_hookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookId);
                    _hookId = IntPtr.Zero;
                }
                _enabled = false;
                Logger.WriteLine("Smart Fn Lock Controller disabled");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to disable Smart Fn Lock Controller: {ex.Message}");
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule?.ModuleName), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _modifierFlags != ModifierKey.None)
            {
                var kbStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                var wParamValue = wParam.ToInt32();

                // Check if selected modifier key is pressed
                if (IsModifierKeyPressed(wParamValue, kbStruct))
                {
                    if (!_restoreFnLock)
                    {
                        // Get current Fn Lock state
                        bool fnLockOn = LenovoFnLock.GetFnLockState();
                        if (fnLockOn)
                        {
                            Logger.WriteLine("Smart Fn Lock: Temporarily disabling Fn Lock (modifier pressed)");
                            LenovoFnLock.SetFnLockState(false);
                            _restoreFnLock = true;
                        }
                    }
                }
                else if (_restoreFnLock)
                {
                    // Restore Fn Lock when modifier is released
                    Logger.WriteLine("Smart Fn Lock: Restoring Fn Lock (modifier released)");
                    LenovoFnLock.SetFnLockState(true);
                    _restoreFnLock = false;
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private bool IsModifierKeyPressed(int wParam, KBDLLHOOKSTRUCT kbStruct)
        {
            bool isKeyDown = wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN;
            int vkCode = kbStruct.vkCode;

            // Update modifier key states
            if (vkCode == VK_LCONTROL || vkCode == VK_RCONTROL)
                _ctrlDepressed = isKeyDown;

            if (vkCode == VK_LSHIFT || vkCode == VK_RSHIFT)
                _shiftDepressed = isKeyDown;

            if (vkCode == VK_LMENU || vkCode == VK_RMENU)
                _altDepressed = isKeyDown;

            // Check if any selected modifier is currently depressed
            bool result = false;
            if (_modifierFlags.HasFlag(ModifierKey.Ctrl))
                result |= _ctrlDepressed;
            if (_modifierFlags.HasFlag(ModifierKey.Shift))
                result |= _shiftDepressed;
            if (_modifierFlags.HasFlag(ModifierKey.Alt))
                result |= _altDepressed;

            return result;
        }

        public void Dispose()
        {
            Disable();
        }
    }
}

