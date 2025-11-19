using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace GHelper.Lenovo
{
    /// <summary>
    /// Manages battery settings for Lenovo Legion laptops
    /// Uses EnergyDrv driver for battery control
    /// </summary>
    public class LenovoBattery
    {
        private const uint IOCTL_ENERGY_BATTERY_CHARGE_MODE = 0x831020F8;
        private const uint IOCTL_ENERGY_BATTERY_NIGHT_CHARGE = 0x83102150;
        private const uint IOCTL_ENERGY_KEYBOARD = 0x83102144;  // Correct IOCTL from Toolkit
        private const uint IOCTL_ENERGY_SETTINGS = 0x831020E8;

        private SafeFileHandle? _energyHandle = null;
        private BatteryState _lastState = BatteryState.Normal;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern unsafe bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            void* lpInBuffer,
            uint nInBufferSize,
            void* lpOutBuffer,
            uint nOutBufferSize,
            uint* lpBytesReturned,
            IntPtr lpOverlapped);

        public LenovoBattery()
        {
            try
            {
                InitializeDriver();

                // Initialize _lastState by reading current hardware state
                if (IsSupported())
                {
                    _lastState = GetBatteryState();
                    Logger.WriteLine($"LenovoBattery initialized, current state: {_lastState}");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Lenovo Battery initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize EnergyDrv driver handle
        /// </summary>
        private void InitializeDriver()
        {
            try
            {
                _energyHandle = CreateFile(@"\\.\EnergyDrv",
                    0xC0000000u, // GENERIC_READ | GENERIC_WRITE
                    0x00000003u, // FILE_SHARE_READ | FILE_SHARE_WRITE
                    IntPtr.Zero,
                    3, // OPEN_EXISTING
                    0x80, // FILE_ATTRIBUTE_NORMAL
                    IntPtr.Zero);

                if (_energyHandle != null && !_energyHandle.IsInvalid)
                {
                    Logger.WriteLine("EnergyDrv driver initialized");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to initialize EnergyDrv: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if battery management is supported
        /// </summary>
        public bool IsSupported()
        {
            return _energyHandle != null && !_energyHandle.IsInvalid;
        }

        /// <summary>
        /// Get current battery state
        /// </summary>
        public unsafe BatteryState GetBatteryState()
        {
            if (!IsSupported() || _energyHandle == null)
                return BatteryState.Normal;

            try
            {
                uint inBuffer = 0xFF;
                uint outBuffer = 0;
                uint bytesReturned = 0;

                bool success = DeviceIoControl(
                    _energyHandle,
                    IOCTL_ENERGY_BATTERY_CHARGE_MODE,
                    &inBuffer,
                    sizeof(uint),
                    &outBuffer,
                    sizeof(uint),
                    &bytesReturned,
                    IntPtr.Zero);

                if (success)
                {
                    // Reverse endianness
                    uint state = ReverseEndianness(outBuffer);

                    // Check bits to determine state
                    BatteryState currentState;

                    if (GetBit(state, 17)) // Is charging?
                    {
                        currentState = GetBit(state, 26) ? BatteryState.RapidCharge : BatteryState.Normal;
                    }
                    else if (GetBit(state, 29))
                    {
                        currentState = BatteryState.Conservation;
                    }
                    else
                    {
                        currentState = BatteryState.Normal;
                    }

                    // Update last state
                    _lastState = currentState;
                    return currentState;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to get battery state: {ex.Message}");
            }

            return BatteryState.Normal;
        }

        /// <summary>
        /// Set battery state (Lenovo Legion Toolkit logic)
        /// </summary>
        public void SetBatteryState(BatteryState state)
        {
            if (!IsSupported())
                return;

            try
            {
                // Get command sequence based on current and target state
                uint[] commands = GetBatteryCommands(state);

                Logger.WriteLine($"Setting battery from {_lastState} to {state}, commands: {string.Join(", ", commands.Select(c => $"0x{c:X}"))}");

                // Execute each command in sequence
                foreach (uint command in commands)
                {
                    if (!SendBatteryCommand(command))
                    {
                        Logger.WriteLine($"Failed to send battery command 0x{command:X}");
                        return;
                    }
                }

                // Update last state
                _lastState = state;

                // Update registry for persistence
                SetBatteryStateInRegistry(state);

                Logger.WriteLine($"Successfully set battery state to: {state}");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set battery state: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get battery command sequence based on state transition (Toolkit logic)
        /// </summary>
        private uint[] GetBatteryCommands(BatteryState targetState)
        {
            return targetState switch
            {
                // Conservation: 0x3, but if coming from RapidCharge need 0x8 first
                BatteryState.Conservation => _lastState == BatteryState.RapidCharge
                    ? new uint[] { 0x8, 0x3 }
                    : new uint[] { 0x3 },

                // Normal: 0x8, but if coming from Conservation need 0x5 first
                BatteryState.Normal => _lastState == BatteryState.Conservation
                    ? new uint[] { 0x5 }
                    : new uint[] { 0x8 },

                // RapidCharge: 0x7, but if coming from Conservation need 0x5 first
                BatteryState.RapidCharge => _lastState == BatteryState.Conservation
                    ? new uint[] { 0x5, 0x7 }
                    : new uint[] { 0x7 },

                _ => new uint[] { 0x8 }
            };
        }

        /// <summary>
        /// Send single battery command via IOCTL
        /// </summary>
        private unsafe bool SendBatteryCommand(uint command)
        {
            if (_energyHandle == null)
                return false;

            uint outBuffer = 0;
            uint bytesReturned = 0;

            bool success = DeviceIoControl(
                _energyHandle,
                IOCTL_ENERGY_BATTERY_CHARGE_MODE,
                &command,
                sizeof(uint),
                &outBuffer,
                sizeof(uint),
                &bytesReturned,
                IntPtr.Zero);

            return success;
        }

        /// <summary>
        /// Set battery state in registry for persistence
        /// </summary>
        private void SetBatteryStateInRegistry(BatteryState state)
        {
            try
            {
                string value = state switch
                {
                    BatteryState.Conservation => "Storage",
                    BatteryState.RapidCharge => "Quick",
                    BatteryState.Normal => "Normal",
                    _ => "Normal"
                };

                Microsoft.Win32.Registry.SetValue(
                    "HKEY_CURRENT_USER\\Software\\Lenovo\\VantageService\\AddinData\\IdeaNotebookAddin",
                    "BatteryChargeMode",
                    value);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set battery state in registry: {ex.Message}");
            }
        }

        /// <summary>
        /// Reverse endianness of uint
        /// </summary>
        private uint ReverseEndianness(uint value)
        {
            return ((value & 0x000000FF) << 24) |
                   ((value & 0x0000FF00) << 8) |
                   ((value & 0x00FF0000) >> 8) |
                   ((value & 0xFF000000) >> 24);
        }

        /// <summary>
        /// Get nth bit from uint
        /// </summary>
        private bool GetBit(uint value, int n)
        {
            return (value & (1 << n)) != 0;
        }

        /// <summary>
        /// Get battery state name for display
        /// </summary>
        public static string GetStateName(BatteryState state)
        {
            return state switch
            {
                BatteryState.Conservation => "Conservation (60-80%)",
                BatteryState.RapidCharge => "Rapid Charge",
                BatteryState.Normal => "Normal",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Check if white keyboard backlight is supported
        /// </summary>
        public unsafe bool IsWhiteBacklightSupported()
        {
            if (!IsSupported() || _energyHandle == null)
            {
                Logger.WriteLine("IsWhiteBacklightSupported: EnergyDrv not available");
                return false;
            }

            try
            {
                uint inBuffer = 0x1;
                uint outBuffer = 0;
                uint bytesReturned = 0;

                bool success = DeviceIoControl(
                    _energyHandle,
                    IOCTL_ENERGY_KEYBOARD,
                    &inBuffer,
                    sizeof(uint),
                    &outBuffer,
                    sizeof(uint),
                    &bytesReturned,
                    IntPtr.Zero);

                if (success)
                {
                    Logger.WriteLine($"White backlight check: outBuffer=0x{outBuffer:X}, bytesReturned={bytesReturned}");
                    outBuffer >>= 1;
                    bool isSupported = outBuffer == 0x2;
                    Logger.WriteLine($"White backlight supported: {isSupported}");
                    return isSupported;
                }

                int error = Marshal.GetLastWin32Error();
                Logger.WriteLine($"White backlight check: DeviceIoControl failed, error={error}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to check white backlight support: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get white keyboard backlight state
        /// </summary>
        public unsafe WhiteKeyboardBacklightState GetWhiteBacklightState()
        {
            if (!IsSupported() || _energyHandle == null)
                return WhiteKeyboardBacklightState.Off;

            try
            {
                uint inBuffer = 0x22;
                uint outBuffer = 0;
                uint bytesReturned = 0;

                bool success = DeviceIoControl(
                    _energyHandle,
                    IOCTL_ENERGY_KEYBOARD,
                    &inBuffer,
                    sizeof(uint),
                    &outBuffer,
                    sizeof(uint),
                    &bytesReturned,
                    IntPtr.Zero);

                if (success)
                {
                    Logger.WriteLine($"Get white backlight state: outBuffer=0x{outBuffer:X}");
                    return outBuffer switch
                    {
                        0x1 => WhiteKeyboardBacklightState.Off,
                        0x3 => WhiteKeyboardBacklightState.Low,
                        0x5 => WhiteKeyboardBacklightState.High,
                        _ => WhiteKeyboardBacklightState.Off
                    };
                }

                int error = Marshal.GetLastWin32Error();
                Logger.WriteLine($"Get white backlight state failed, error={error}");
                return WhiteKeyboardBacklightState.Off;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to get white backlight state: {ex.Message}");
                return WhiteKeyboardBacklightState.Off;
            }
        }

        /// <summary>
        /// Set white keyboard backlight state
        /// </summary>
        public unsafe bool SetWhiteBacklightState(WhiteKeyboardBacklightState state)
        {
            if (!IsSupported() || _energyHandle == null)
                return false;

            try
            {
                uint command = state switch
                {
                    WhiteKeyboardBacklightState.Off => 0x00023,
                    WhiteKeyboardBacklightState.Low => 0x10023,
                    WhiteKeyboardBacklightState.High => 0x20023,
                    _ => 0x00023
                };

                Logger.WriteLine($"Setting white backlight to: {state} (command: 0x{command:X})");

                uint outBuffer = 0;
                uint bytesReturned = 0;

                bool success = DeviceIoControl(
                    _energyHandle,
                    IOCTL_ENERGY_KEYBOARD,
                    &command,
                    sizeof(uint),
                    &outBuffer,
                    sizeof(uint),
                    &bytesReturned,
                    IntPtr.Zero);

                if (success)
                {
                    Logger.WriteLine($"Successfully set white backlight to: {state}, outBuffer=0x{outBuffer:X}");
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.WriteLine($"Failed to set white backlight, error={error}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set white backlight state: {ex.Message}");
                return false;
            }
        }

        // ===== Battery Night Charge =====
        public unsafe BatteryNightChargeState GetBatteryNightChargeState()
        {
            if (!IsSupported() || _energyHandle == null)
                return BatteryNightChargeState.Off;

            try
            {
                uint inBuffer = 0x11;
                uint outBuffer = 0;
                uint bytesReturned = 0;

                bool success = DeviceIoControl(
                    _energyHandle,
                    IOCTL_ENERGY_BATTERY_NIGHT_CHARGE,
                    &inBuffer,
                    sizeof(uint),
                    &outBuffer,
                    sizeof(uint),
                    &bytesReturned,
                    IntPtr.Zero);

                if (success && GetBit(outBuffer, 0))
                {
                    return GetBit(outBuffer, 4) ? BatteryNightChargeState.On : BatteryNightChargeState.Off;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to get battery night charge state: {ex.Message}");
            }

            return BatteryNightChargeState.Off;
        }

        public unsafe bool SetBatteryNightChargeState(BatteryNightChargeState state)
        {
            if (!IsSupported() || _energyHandle == null)
                return false;

            try
            {
                uint command = state == BatteryNightChargeState.On ? 0x80000012u : 0x12u;
                uint outBuffer = 0;
                uint bytesReturned = 0;

                bool success = DeviceIoControl(
                    _energyHandle,
                    IOCTL_ENERGY_BATTERY_NIGHT_CHARGE,
                    &command,
                    sizeof(uint),
                    &outBuffer,
                    sizeof(uint),
                    &bytesReturned,
                    IntPtr.Zero);

                Logger.WriteLine($"Set battery night charge to {state}: {success}");
                return success;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set battery night charge: {ex.Message}");
                return false;
            }
        }

        // ===== Always On USB =====
        public unsafe AlwaysOnUSBState GetAlwaysOnUSBState()
        {
            if (!IsSupported() || _energyHandle == null)
                return AlwaysOnUSBState.Off;

            try
            {
                uint inBuffer = 0x2;
                uint outBuffer = 0;
                uint bytesReturned = 0;

                bool success = DeviceIoControl(
                    _energyHandle,
                    IOCTL_ENERGY_SETTINGS,
                    &inBuffer,
                    sizeof(uint),
                    &outBuffer,
                    sizeof(uint),
                    &bytesReturned,
                    IntPtr.Zero);

                if (success)
                {
                    uint state = ReverseEndianness(outBuffer);
                    if (GetBit(state, 31))
                    {
                        return GetBit(state, 23) ? AlwaysOnUSBState.OnAlways : AlwaysOnUSBState.OnWhenSleeping;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to get always on USB state: {ex.Message}");
            }

            return AlwaysOnUSBState.Off;
        }

        public unsafe bool SetAlwaysOnUSBState(AlwaysOnUSBState state)
        {
            if (!IsSupported() || _energyHandle == null)
                return false;

            try
            {
                uint[] commands = state switch
                {
                    AlwaysOnUSBState.Off => new uint[] { 0xB, 0x12 },
                    AlwaysOnUSBState.OnWhenSleeping => new uint[] { 0xA, 0x12 },
                    AlwaysOnUSBState.OnAlways => new uint[] { 0xA, 0x13 },
                    _ => new uint[] { 0xB, 0x12 }
                };

                foreach (uint command in commands)
                {
                    uint outBuffer = 0;
                    uint bytesReturned = 0;

                    bool success = DeviceIoControl(
                        _energyHandle,
                        IOCTL_ENERGY_SETTINGS,
                        &command,
                        sizeof(uint),
                        &outBuffer,
                        sizeof(uint),
                        &bytesReturned,
                        IntPtr.Zero);

                    if (!success)
                    {
                        Logger.WriteLine($"Failed to set always on USB command 0x{command:X}");
                        return false;
                    }
                }

                Logger.WriteLine($"Set always on USB to {state}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set always on USB: {ex.Message}");
                return false;
            }
        }

        // ===== Smart Fn Lock =====
        public unsafe SmartFnLockState GetSmartFnLockState()
        {
            if (!IsSupported() || _energyHandle == null)
                return SmartFnLockState.Off;

            try
            {
                uint inBuffer = 0x2;
                uint outBuffer = 0;
                uint bytesReturned = 0;

                bool success = DeviceIoControl(
                    _energyHandle,
                    IOCTL_ENERGY_SETTINGS,
                    &inBuffer,
                    sizeof(uint),
                    &outBuffer,
                    sizeof(uint),
                    &bytesReturned,
                    IntPtr.Zero);

                if (success)
                {
                    uint state = ReverseEndianness(outBuffer);
                    return GetBit(state, 21) ? SmartFnLockState.On : SmartFnLockState.Off;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to get smart fn lock state: {ex.Message}");
            }

            return SmartFnLockState.Off;
        }

        public unsafe bool SetSmartFnLockState(SmartFnLockState state)
        {
            if (!IsSupported() || _energyHandle == null)
                return false;

            try
            {
                uint command = state == SmartFnLockState.On ? 0xEu : 0xFu;
                uint outBuffer = 0;
                uint bytesReturned = 0;

                bool success = DeviceIoControl(
                    _energyHandle,
                    IOCTL_ENERGY_SETTINGS,
                    &command,
                    sizeof(uint),
                    &outBuffer,
                    sizeof(uint),
                    &bytesReturned,
                    IntPtr.Zero);

                Logger.WriteLine($"Set smart fn lock to {state}: {success}");
                return success;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set smart fn lock: {ex.Message}");
                return false;
            }
        }

        // ===== Touchpad Lock (Disable Hotkeys) =====
        public unsafe TouchpadLockState GetTouchpadLockState()
        {
            if (!IsSupported() || _energyHandle == null)
                return TouchpadLockState.Off;

            try
            {
                uint inBuffer = 0x13;
                uint outBuffer = 0;
                uint bytesReturned = 0;

                bool success = DeviceIoControl(
                    _energyHandle,
                    IOCTL_ENERGY_SETTINGS,
                    &inBuffer,
                    sizeof(uint),
                    &outBuffer,
                    sizeof(uint),
                    &bytesReturned,
                    IntPtr.Zero);

                if (success)
                {
                    uint state = ReverseEndianness(outBuffer);
                    return GetBit(state, 31) ? TouchpadLockState.On : TouchpadLockState.Off;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to get touchpad lock state: {ex.Message}");
            }

            return TouchpadLockState.Off;
        }

        public unsafe bool SetTouchpadLockState(TouchpadLockState state)
        {
            if (!IsSupported() || _energyHandle == null)
                return false;

            try
            {
                uint command = state == TouchpadLockState.On ? 0xAu : 0xBu;
                command = (command << 8) | 0x14u;

                uint outBuffer = 0;
                uint bytesReturned = 0;

                bool success = DeviceIoControl(
                    _energyHandle,
                    IOCTL_ENERGY_SETTINGS,
                    &command,
                    sizeof(uint),
                    &outBuffer,
                    sizeof(uint),
                    &bytesReturned,
                    IntPtr.Zero);

                Logger.WriteLine($"Set touchpad lock to {state}: {success}");
                return success;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set touchpad lock: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _energyHandle?.Dispose();
            _energyHandle = null;
        }
    }
}


