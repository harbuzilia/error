using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace GHelper.Lenovo
{
    /// <summary>
    /// Keyboard backlight types
    /// </summary>
    public enum KeyboardBacklightType
    {
        None = 0,
        White = 1,      // Single color (white), 3 levels: Off, Low, High
        RGB4Zone = 2,   // 4-zone RGB
        Spectrum = 3    // Per-key RGB
    }

    /// <summary>
    /// Manages RGB keyboard backlight for Lenovo Legion laptops
    /// Supports white, 4-zone RGB, and spectrum keyboards
    /// </summary>
    public class LenovoRGB : IDisposable
    {
        private SafeFileHandle? _deviceHandle;
        private KeyboardBacklightType _backlightType = KeyboardBacklightType.None;

        // RGB Keyboard State Structure (simplified)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct LENOVO_RGB_KEYBOARD_STATE
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] Header;           // [0xCC, 0x16]
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
            public byte[] Unused;
            
            public byte Padding;
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Zone1Rgb;         // R, G, B
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Zone2Rgb;
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Zone3Rgb;
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Zone4Rgb;
            
            public byte Effect;             // 1=Static, 3=Breath, 4=Wave, 6=Smooth
            public byte Speed;              // 1=Slowest, 2=Slow, 3=Medium, 4=Fast
            public byte Brightness;         // 1=Low, 2=High
            public byte Padding2;
        }

        public LenovoRGB()
        {
            try
            {
                DetectBacklightType();
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Lenovo keyboard backlight initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Detect keyboard backlight type
        /// </summary>
        private void DetectBacklightType()
        {
            try
            {
                // First check for white backlight via EnergyDrv
                if (CheckWhiteBacklight())
                {
                    _backlightType = KeyboardBacklightType.White;
                    Logger.WriteLine("Detected white keyboard backlight");
                    return;
                }

                // TODO: Check for RGB/Spectrum keyboards via HID enumeration
                // For now, assume no backlight if white check fails
                _backlightType = KeyboardBacklightType.None;
                Logger.WriteLine("No keyboard backlight detected");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to detect backlight type: {ex.Message}");
                _backlightType = KeyboardBacklightType.None;
            }
        }

        /// <summary>
        /// Check if white keyboard backlight is supported
        /// </summary>
        private bool CheckWhiteBacklight()
        {
            try
            {
                // Use EnergyDrv to check for white backlight support
                // IOCTL code 0x831020D0 with input 0x1
                // If (output >> 1) == 0x2, white backlight is supported

                if (Program.lenovoBattery == null)
                    return false;

                // For now, return false - will implement when we have proper EnergyDrv access
                // This would require the same driver handle as battery management
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get keyboard backlight type
        /// </summary>
        public KeyboardBacklightType GetBacklightType()
        {
            return _backlightType;
        }

        /// <summary>
        /// Check if any keyboard backlight is supported
        /// </summary>
        public bool IsSupported()
        {
            return _backlightType != KeyboardBacklightType.None;
        }

        /// <summary>
        /// Set RGB keyboard state (4-zone)
        /// </summary>
        public void SetRGBState(byte effect, byte speed, byte brightness, 
            byte[] zone1, byte[] zone2, byte[] zone3, byte[] zone4)
        {
            if (!IsSupported())
            {
                Logger.WriteLine("RGB keyboard not supported");
                return;
            }

            try
            {
                var state = new LENOVO_RGB_KEYBOARD_STATE
                {
                    Header = new byte[] { 0xCC, 0x16 },
                    Unused = new byte[13],
                    Padding = 0x00,
                    Zone1Rgb = zone1,
                    Zone2Rgb = zone2,
                    Zone3Rgb = zone3,
                    Zone4Rgb = zone4,
                    Effect = effect,
                    Speed = speed,
                    Brightness = brightness,
                    Padding2 = 0x00
                };

                SendToDevice(state);
                Logger.WriteLine($"Set RGB: Effect={effect}, Speed={speed}, Brightness={brightness}");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set RGB state: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get white keyboard backlight state
        /// </summary>
        public WhiteKeyboardBacklightState GetWhiteBacklightState()
        {
            if (_backlightType != KeyboardBacklightType.White)
                return WhiteKeyboardBacklightState.Off;

            try
            {
                // TODO: Implement via EnergyDrv IOCTL 0x831020D0 with input 0x22
                // For now, return Off
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
        public void SetWhiteBacklightState(WhiteKeyboardBacklightState state)
        {
            if (_backlightType != KeyboardBacklightType.White)
            {
                Logger.WriteLine("White backlight not supported");
                return;
            }

            try
            {
                // TODO: Implement via EnergyDrv IOCTL 0x831020D0
                // Input values: 0x00023 (Off), 0x10023 (Low), 0x20023 (High)
                uint value = state switch
                {
                    WhiteKeyboardBacklightState.Off => 0x00023,
                    WhiteKeyboardBacklightState.Low => 0x10023,
                    WhiteKeyboardBacklightState.High => 0x20023,
                    _ => 0x00023
                };

                Logger.WriteLine($"Set white backlight to: {state} (value: 0x{value:X})");
                // Actual implementation would send this via EnergyDrv
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set white backlight: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Turn off keyboard backlight
        /// </summary>
        public void TurnOff()
        {
            if (!IsSupported())
                return;

            try
            {
                if (_backlightType == KeyboardBacklightType.White)
                {
                    SetWhiteBacklightState(WhiteKeyboardBacklightState.Off);
                }
                else
                {
                    // Set all zones to black (off) for RGB keyboards
                    byte[] black = new byte[] { 0, 0, 0 };
                    SetRGBState(1, 1, 1, black, black, black, black);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to turn off backlight: {ex.Message}");
            }
        }

        /// <summary>
        /// Send RGB state to device via HID
        /// </summary>
        private unsafe void SendToDevice(LENOVO_RGB_KEYBOARD_STATE state)
        {
            if (_deviceHandle == null || _deviceHandle.IsInvalid)
                throw new InvalidOperationException("RGB device not available");

            IntPtr ptr = IntPtr.Zero;
            try
            {
                int size = Marshal.SizeOf<LENOVO_RGB_KEYBOARD_STATE>();
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(state, ptr, false);

                // TODO: Implement HidD_SetFeature call
                // This requires proper HID device handle
                Logger.WriteLine("RGB state prepared - HID send not yet implemented");
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        public void Dispose()
        {
            _deviceHandle?.Dispose();
        }
    }
}

