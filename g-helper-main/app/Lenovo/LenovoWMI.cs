using System.Management;

namespace GHelper.Lenovo
{
    /// <summary>
    /// Base WMI class for Lenovo Legion laptops
    /// Provides methods to interact with Lenovo WMI interfaces
    /// </summary>
    public class LenovoWMI
    {
        private const string WMI_SCOPE = "root\\WMI";
        
        // WMI Class Names
        private const string LENOVO_GAMEZONE_DATA = "LENOVO_GAMEZONE_DATA";
        private const string LENOVO_FAN_METHOD = "LENOVO_FAN_METHOD";
        private const string LENOVO_OTHER_METHOD = "LENOVO_OTHER_METHOD";
        private const string LENOVO_FAN_TABLE_DATA = "LENOVO_FAN_TABLE_DATA";

        private bool _isConnected = false;

        public LenovoWMI()
        {
            try
            {
                // Test connection by checking if LENOVO_GAMEZONE_DATA exists
                _isConnected = WMIExists(LENOVO_GAMEZONE_DATA);
                if (_isConnected)
                {
                    Logger.WriteLine("Lenovo WMI interface detected");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Lenovo WMI initialization failed: {ex.Message}");
                _isConnected = false;
            }
        }

        public bool IsConnected()
        {
            return _isConnected;
        }

        /// <summary>
        /// Check if a WMI class exists
        /// </summary>
        private bool WMIExists(string className)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(WMI_SCOPE, $"SELECT * FROM {className}");
                using var collection = searcher.Get();
                return collection.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Call a WMI method without return value
        /// </summary>
        protected void WMICall(string className, string methodName, Dictionary<string, object>? parameters = null)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(WMI_SCOPE, $"SELECT * FROM {className}");
                using var collection = searcher.Get();
                
                foreach (ManagementObject obj in collection)
                {
                    using (obj)
                    {
                        var methodParams = obj.GetMethodParameters(methodName);
                        
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                methodParams[param.Key] = param.Value;
                            }
                        }

                        obj.InvokeMethod(methodName, methodParams, null);
                        Logger.WriteLine($"WMI Call: {className}.{methodName}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"WMI Call failed: {className}.{methodName} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Call a WMI method with return value
        /// </summary>
        protected T WMICall<T>(string className, string methodName, string returnProperty, Dictionary<string, object>? parameters = null)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(WMI_SCOPE, $"SELECT * FROM {className}");
                using var collection = searcher.Get();
                
                foreach (ManagementObject obj in collection)
                {
                    using (obj)
                    {
                        var methodParams = obj.GetMethodParameters(methodName);
                        
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                methodParams[param.Key] = param.Value;
                            }
                        }

                        var result = obj.InvokeMethod(methodName, methodParams, null);
                        
                        if (result != null && result[returnProperty] != null)
                        {
                            var value = (T)Convert.ChangeType(result[returnProperty], typeof(T));
                            Logger.WriteLine($"WMI Call: {className}.{methodName} = {value}");
                            return value;
                        }
                    }
                }
                
                throw new InvalidOperationException($"No result from WMI call: {className}.{methodName}");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"WMI Call failed: {className}.{methodName} - {ex.Message}");
                throw;
            }
        }

        // Convenience methods for common WMI classes
        protected void GameZoneCall(string methodName, Dictionary<string, object>? parameters = null)
        {
            WMICall(LENOVO_GAMEZONE_DATA, methodName, parameters);
        }

        protected T GameZoneCall<T>(string methodName, string returnProperty, Dictionary<string, object>? parameters = null)
        {
            return WMICall<T>(LENOVO_GAMEZONE_DATA, methodName, returnProperty, parameters);
        }

        protected void FanMethodCall(string methodName, Dictionary<string, object>? parameters = null)
        {
            WMICall(LENOVO_FAN_METHOD, methodName, parameters);
        }

        protected T FanMethodCall<T>(string methodName, string returnProperty, Dictionary<string, object>? parameters = null)
        {
            return WMICall<T>(LENOVO_FAN_METHOD, methodName, returnProperty, parameters);
        }

        protected void OtherMethodCall(string methodName, Dictionary<string, object>? parameters = null)
        {
            WMICall(LENOVO_OTHER_METHOD, methodName, parameters);
        }

        protected T OtherMethodCall<T>(string methodName, string returnProperty, Dictionary<string, object>? parameters = null)
        {
            return WMICall<T>(LENOVO_OTHER_METHOD, methodName, returnProperty, parameters);
        }

        /// <summary>
        /// Check if GPU Overclock is supported
        /// </summary>
        public bool IsSupportGpuOC()
        {
            try
            {
                int result = GameZoneCall<int>("IsSupportGpuOC", "Data");
                Logger.WriteLine($"GPU OC Support: {result}");
                return result > 0;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"GPU OC Support check failed: {ex.Message}");
                return false;
            }
        }

        // ===== Windows Key Lock =====
        public bool IsSupportWinKey()
        {
            try
            {
                return GameZoneCall<int>("IsSupportDisableWinKey", "Data") > 0;
            }
            catch { return false; }
        }

        public int GetWinKeyStatus()
        {
            try
            {
                return GameZoneCall<int>("GetWinKeyStatus", "Data");
            }
            catch { return 0; }
        }

        public void SetWinKeyStatus(int status)
        {
            try
            {
                GameZoneCall("SetWinKeyStatus", new Dictionary<string, object> { { "Data", status } });
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"SetWinKeyStatus failed: {ex.Message}");
            }
        }

        // ===== OverDrive =====
        public bool IsSupportOverDrive()
        {
            try
            {
                return GameZoneCall<int>("IsSupportOD", "Data") > 0;
            }
            catch { return false; }
        }

        public int GetOverDriveStatus()
        {
            try
            {
                return GameZoneCall<int>("GetODStatus", "Data");
            }
            catch { return 0; }
        }

        public void SetOverDriveStatus(int status)
        {
            try
            {
                GameZoneCall("SetODStatus", new Dictionary<string, object> { { "Data", status } });
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"SetOverDriveStatus failed: {ex.Message}");
            }
        }

        // ===== Instant Boot (через Get_Device_Current_Support_Feature) =====
        private const int INSTANT_BOOT_AC_INDEX = 5;
        private const int INSTANT_BOOT_USB_PD_INDEX = 6;

        /// <summary>
        /// Get Device Current Support Feature flags
        /// </summary>
        private int GetDeviceCurrentSupportFeature()
        {
            try
            {
                return OtherMethodCall<int>("Get_Device_Current_Support_Feature", "Flag", null);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"GetDeviceCurrentSupportFeature failed: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Set Device Current Support Feature flag
        /// </summary>
        private void SetDeviceCurrentSupportFeature(int functionId, int value)
        {
            try
            {
                OtherMethodCall("Set_Device_Current_Support_Feature", new Dictionary<string, object>
                {
                    { "FunctionID", functionId },
                    { "value", value }
                });
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"SetDeviceCurrentSupportFeature failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Check if bit is set in flags
        /// </summary>
        private static bool IsBitSet(int flags, int bitIndex)
        {
            return (flags & (1 << bitIndex)) != 0;
        }

        public bool IsSupportInstantBoot()
        {
            try
            {
                // Try to read current state - if it works, feature is supported
                int flags = GetDeviceCurrentSupportFeature();
                // If we got here, method exists and works
                bool acSupported = IsBitSet(flags, INSTANT_BOOT_AC_INDEX);
                bool usbSupported = IsBitSet(flags, INSTANT_BOOT_USB_PD_INDEX);
                Logger.WriteLine($"Instant Boot check: flags=0x{flags:X}, AC={acSupported}, USB={usbSupported}");
                return acSupported || usbSupported; // At least one should be supported
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Instant Boot not supported: {ex.Message}");
                return false;
            }
        }

        public InstantBootState GetInstantBootState()
        {
            try
            {
                int flags = GetDeviceCurrentSupportFeature();
                bool acAdapter = IsBitSet(flags, INSTANT_BOOT_AC_INDEX);
                bool usbPowerDelivery = IsBitSet(flags, INSTANT_BOOT_USB_PD_INDEX);

                return (acAdapter, usbPowerDelivery) switch
                {
                    (true, true) => InstantBootState.AcAdapterAndUsbPowerDelivery,
                    (true, false) => InstantBootState.AcAdapter,
                    (false, true) => InstantBootState.UsbPowerDelivery,
                    _ => InstantBootState.Off
                };
            }
            catch
            {
                return InstantBootState.Off;
            }
        }

        public void SetInstantBootState(InstantBootState state)
        {
            try
            {
                var (ac, usb) = state switch
                {
                    InstantBootState.AcAdapterAndUsbPowerDelivery => (1, 1),
                    InstantBootState.AcAdapter => (1, 0),
                    InstantBootState.UsbPowerDelivery => (0, 1),
                    _ => (0, 0)
                };

                SetDeviceCurrentSupportFeature(INSTANT_BOOT_AC_INDEX, ac);
                SetDeviceCurrentSupportFeature(INSTANT_BOOT_USB_PD_INDEX, usb);
                Logger.WriteLine($"SetInstantBootState: {state} (AC={ac}, USB={usb})");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"SetInstantBootState failed: {ex.Message}");
            }
        }

        // ===== Flip To Start (через UEFI) =====
        // Note: Flip To Start uses UEFI variables, not WMI
        // Use LenovoFlipToStart class instead

        // ===== Touchpad Lock (через LENOVO_GAMEZONE_DATA) =====
        public bool IsSupportTouchpadLock()
        {
            try
            {
                return GameZoneCall<int>("IsSupportDisableTP", "Data") > 0;
            }
            catch { return false; }
        }

        public TouchpadLockState GetTouchpadLockState()
        {
            try
            {
                int value = GameZoneCall<int>("GetTPStatus", "Data");
                return value == 1 ? TouchpadLockState.On : TouchpadLockState.Off;
            }
            catch { return TouchpadLockState.Off; }
        }

        public void SetTouchpadLockState(TouchpadLockState state)
        {
            try
            {
                int value = state == TouchpadLockState.On ? 1 : 0;
                GameZoneCall("SetTPStatus", new Dictionary<string, object> { { "Data", value } });
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"SetTouchpadLockState failed: {ex.Message}");
            }
        }
    }
}

