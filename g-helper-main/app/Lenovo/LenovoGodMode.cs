using System.Management;

namespace GHelper.Lenovo
{
    /// <summary>
    /// Lenovo GodMode - CPU/GPU power limits and advanced settings
    /// Uses LENOVO_OTHER_METHOD with GetFeatureValue/SetFeatureValue
    /// </summary>
    public class LenovoGodMode
    {
        private const string WMI_NAMESPACE = "root\\WMI";
        private const string OTHER_METHOD_CLASS = "LENOVO_OTHER_METHOD";
        private const string CAPABILITY_DATA_CLASS = "LENOVO_CAPABILITY_DATA_01";

        /// <summary>
        /// Power limit value with min/max/step
        /// </summary>
        public class PowerLimit
        {
            public int Value { get; set; }
            public int Min { get; set; }
            public int Max { get; set; }
            public int Step { get; set; }
            public int? DefaultValue { get; set; }

            public PowerLimit(int value, int min, int max, int step, int? defaultValue = null)
            {
                Value = value;
                Min = min;
                Max = max;
                Step = step;
                DefaultValue = defaultValue;
            }
        }

        /// <summary>
        /// Capability IDs for power limits (from Legion Toolkit)
        /// </summary>
        private enum CapabilityID : uint
        {
            CPUShortTermPowerLimit = 0x0101FF00,
            CPULongTermPowerLimit = 0x0102FF00,
            CPUPeakPowerLimit = 0x0103FF00,
            CPUTemperatureLimit = 0x0104FF00,
            APUsPPTPowerLimit = 0x0105FF00,
            CPUCrossLoadingPowerLimit = 0x0106FF00,
            CPUPL1Tau = 0x0107FF00,
            GPUPowerBoost = 0x0201FF00,
            GPUConfigurableTGP = 0x0202FF00,
            GPUTemperatureLimit = 0x0203FF00
        }

        #region Helper Methods

        /// <summary>
        /// Get feature value using LENOVO_OTHER_METHOD
        /// </summary>
        private int? GetFeatureValue(CapabilityID id)
        {
            try
            {
                uint idRaw = (uint)id & 0xFFFF00FF;
                
                using var searcher = new ManagementObjectSearcher(WMI_NAMESPACE, $"SELECT * FROM {OTHER_METHOD_CLASS}");
                using var collection = searcher.Get();

                foreach (ManagementObject obj in collection)
                {
                    using (obj)
                    {
                        using var inParams = obj.GetMethodParameters("GetFeatureValue");
                        inParams["IDs"] = idRaw;
                        using var outParams = obj.InvokeMethod("GetFeatureValue", inParams, null);

                        if (outParams != null && outParams["Value"] != null)
                        {
                            return Convert.ToInt32(outParams["Value"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to get feature value for {id}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Set feature value using LENOVO_OTHER_METHOD
        /// </summary>
        private bool SetFeatureValue(CapabilityID id, int value)
        {
            try
            {
                uint idRaw = (uint)id & 0xFFFF00FF;
                
                using var searcher = new ManagementObjectSearcher(WMI_NAMESPACE, $"SELECT * FROM {OTHER_METHOD_CLASS}");
                using var collection = searcher.Get();

                foreach (ManagementObject obj in collection)
                {
                    using (obj)
                    {
                        using var inParams = obj.GetMethodParameters("SetFeatureValue");
                        inParams["IDs"] = idRaw;
                        inParams["value"] = value;
                        using var outParams = obj.InvokeMethod("SetFeatureValue", inParams, null);

                        Logger.WriteLine($"Set feature {id} to {value}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set feature value for {id}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Get capability data (min/max/step/default) - uses fallback defaults
        /// </summary>
        private (int min, int max, int step, int? defaultValue) GetCapabilityData(CapabilityID id)
        {
            // Fallback defaults based on capability type
            return id switch
            {
                CapabilityID.CPULongTermPowerLimit => (0, 200, 1, null),
                CapabilityID.CPUShortTermPowerLimit => (0, 200, 1, null),
                CapabilityID.CPUPeakPowerLimit => (0, 200, 1, null),
                CapabilityID.CPUCrossLoadingPowerLimit => (0, 200, 1, null),
                CapabilityID.CPUPL1Tau => (0, 128, 1, null),
                CapabilityID.APUsPPTPowerLimit => (0, 200, 1, null),
                CapabilityID.CPUTemperatureLimit => (0, 105, 1, null),
                CapabilityID.GPUPowerBoost => (0, 50, 1, null),
                CapabilityID.GPUConfigurableTGP => (0, 200, 1, null),
                CapabilityID.GPUTemperatureLimit => (0, 100, 1, null),
                _ => (0, 200, 1, null)
            };
        }

        #endregion

        #region CPU Power Limits

        /// <summary>
        /// Get CPU Long Term Power Limit (SPL)
        /// </summary>
        public PowerLimit? GetCPULongTermPowerLimit()
        {
            var value = GetFeatureValue(CapabilityID.CPULongTermPowerLimit);
            if (value == null)
                return null;

            var capData = GetCapabilityData(CapabilityID.CPULongTermPowerLimit);
            return new PowerLimit(value.Value, capData.min, capData.max, capData.step, capData.defaultValue);
        }

        /// <summary>
        /// Set CPU Long Term Power Limit (SPL)
        /// </summary>
        public bool SetCPULongTermPowerLimit(int value)
        {
            return SetFeatureValue(CapabilityID.CPULongTermPowerLimit, value);
        }

        /// <summary>
        /// Get CPU Short Term Power Limit (sPPT)
        /// </summary>
        public PowerLimit? GetCPUShortTermPowerLimit()
        {
            var value = GetFeatureValue(CapabilityID.CPUShortTermPowerLimit);
            if (value == null)
                return null;

            var capData = GetCapabilityData(CapabilityID.CPUShortTermPowerLimit);
            return new PowerLimit(value.Value, capData.min, capData.max, capData.step, capData.defaultValue);
        }

        /// <summary>
        /// Set CPU Short Term Power Limit (sPPT)
        /// </summary>
        public bool SetCPUShortTermPowerLimit(int value)
        {
            return SetFeatureValue(CapabilityID.CPUShortTermPowerLimit, value);
        }

        /// <summary>
        /// Get CPU Peak Power Limit (fPPT)
        /// </summary>
        public PowerLimit? GetCPUPeakPowerLimit()
        {
            var value = GetFeatureValue(CapabilityID.CPUPeakPowerLimit);
            if (value == null)
                return null;

            var capData = GetCapabilityData(CapabilityID.CPUPeakPowerLimit);
            return new PowerLimit(value.Value, capData.min, capData.max, capData.step, capData.defaultValue);
        }

        /// <summary>
        /// Set CPU Peak Power Limit (fPPT)
        /// </summary>
        public bool SetCPUPeakPowerLimit(int value)
        {
            return SetFeatureValue(CapabilityID.CPUPeakPowerLimit, value);
        }

        /// <summary>
        /// Get CPU Cross Loading Power Limit
        /// </summary>
        public PowerLimit? GetCPUCrossLoadingPowerLimit()
        {
            var value = GetFeatureValue(CapabilityID.CPUCrossLoadingPowerLimit);
            if (value == null)
                return null;

            var capData = GetCapabilityData(CapabilityID.CPUCrossLoadingPowerLimit);
            return new PowerLimit(value.Value, capData.min, capData.max, capData.step, capData.defaultValue);
        }

        /// <summary>
        /// Set CPU Cross Loading Power Limit
        /// </summary>
        public bool SetCPUCrossLoadingPowerLimit(int value)
        {
            return SetFeatureValue(CapabilityID.CPUCrossLoadingPowerLimit, value);
        }

        /// <summary>
        /// Get CPU PL1 Tau (Time window in seconds)
        /// </summary>
        public PowerLimit? GetCPUPL1Tau()
        {
            var value = GetFeatureValue(CapabilityID.CPUPL1Tau);
            if (value == null)
                return null;

            var capData = GetCapabilityData(CapabilityID.CPUPL1Tau);
            return new PowerLimit(value.Value, capData.min, capData.max, capData.step, capData.defaultValue);
        }

        /// <summary>
        /// Set CPU PL1 Tau (Time window in seconds)
        /// </summary>
        public bool SetCPUPL1Tau(int value)
        {
            return SetFeatureValue(CapabilityID.CPUPL1Tau, value);
        }

        /// <summary>
        /// Get APU sPPT Power Limit
        /// </summary>
        public PowerLimit? GetAPUSPPTPowerLimit()
        {
            var value = GetFeatureValue(CapabilityID.APUsPPTPowerLimit);
            if (value == null)
                return null;

            var capData = GetCapabilityData(CapabilityID.APUsPPTPowerLimit);
            return new PowerLimit(value.Value, capData.min, capData.max, capData.step, capData.defaultValue);
        }

        /// <summary>
        /// Set APU sPPT Power Limit
        /// </summary>
        public bool SetAPUSPPTPowerLimit(int value)
        {
            return SetFeatureValue(CapabilityID.APUsPPTPowerLimit, value);
        }

        /// <summary>
        /// Get CPU Temperature Limit
        /// </summary>
        public PowerLimit? GetCPUTemperatureLimit()
        {
            var value = GetFeatureValue(CapabilityID.CPUTemperatureLimit);
            if (value == null)
                return null;

            var capData = GetCapabilityData(CapabilityID.CPUTemperatureLimit);
            return new PowerLimit(value.Value, capData.min, capData.max, capData.step, capData.defaultValue);
        }

        /// <summary>
        /// Set CPU Temperature Limit
        /// </summary>
        public bool SetCPUTemperatureLimit(int value)
        {
            return SetFeatureValue(CapabilityID.CPUTemperatureLimit, value);
        }

        #endregion

        #region GPU Power Limits

        /// <summary>
        /// Get GPU Power Boost (PPAB)
        /// </summary>
        public PowerLimit? GetGPUPowerBoost()
        {
            var value = GetFeatureValue(CapabilityID.GPUPowerBoost);
            if (value == null)
                return null;

            var capData = GetCapabilityData(CapabilityID.GPUPowerBoost);
            return new PowerLimit(value.Value, capData.min, capData.max, capData.step, capData.defaultValue);
        }

        /// <summary>
        /// Set GPU Power Boost (PPAB)
        /// </summary>
        public bool SetGPUPowerBoost(int value)
        {
            return SetFeatureValue(CapabilityID.GPUPowerBoost, value);
        }

        /// <summary>
        /// Get GPU Configurable TGP (cTGP)
        /// </summary>
        public PowerLimit? GetGPUConfigurableTGP()
        {
            var value = GetFeatureValue(CapabilityID.GPUConfigurableTGP);
            if (value == null)
                return null;

            var capData = GetCapabilityData(CapabilityID.GPUConfigurableTGP);
            return new PowerLimit(value.Value, capData.min, capData.max, capData.step, capData.defaultValue);
        }

        /// <summary>
        /// Set GPU Configurable TGP (cTGP)
        /// </summary>
        public bool SetGPUConfigurableTGP(int value)
        {
            return SetFeatureValue(CapabilityID.GPUConfigurableTGP, value);
        }

        /// <summary>
        /// Get GPU Temperature Limit
        /// </summary>
        public PowerLimit? GetGPUTemperatureLimit()
        {
            var value = GetFeatureValue(CapabilityID.GPUTemperatureLimit);
            if (value == null)
                return null;

            var capData = GetCapabilityData(CapabilityID.GPUTemperatureLimit);
            return new PowerLimit(value.Value, capData.min, capData.max, capData.step, capData.defaultValue);
        }

        /// <summary>
        /// Set GPU Temperature Limit
        /// </summary>
        public bool SetGPUTemperatureLimit(int value)
        {
            return SetFeatureValue(CapabilityID.GPUTemperatureLimit, value);
        }

        #endregion

        #region Support Check

        /// <summary>
        /// Check if GodMode is supported on this device
        /// </summary>
        public bool IsSupported()
        {
            try
            {
                // Check if LENOVO_OTHER_METHOD exists
                using var searcher = new ManagementObjectSearcher(WMI_NAMESPACE, $"SELECT * FROM {OTHER_METHOD_CLASS}");
                using var collection = searcher.Get();

                if (collection.Count == 0)
                {
                    Logger.WriteLine("LENOVO_OTHER_METHOD not found");
                    return false;
                }

                // Try to get at least one CPU power limit to verify support
                var cpuLimit = GetCPULongTermPowerLimit();
                if (cpuLimit == null)
                {
                    Logger.WriteLine("CPU power limits not supported");
                    return false;
                }

                Logger.WriteLine("GodMode is supported");
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"GodMode support check failed: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
