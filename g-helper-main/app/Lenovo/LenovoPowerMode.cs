namespace GHelper.Lenovo
{
    /// <summary>
    /// Manages power modes for Lenovo Legion laptops
    /// Equivalent to PowerModeFeature in Legion Toolkit
    /// </summary>
    public class LenovoPowerMode : LenovoWMI
    {
        public LenovoPowerMode() : base()
        {
        }

        /// <summary>
        /// Check if Smart Fan (power modes) is supported
        /// </summary>
        public bool IsSupported()
        {
            try
            {
                int result = GameZoneCall<int>("IsSupportSmartFan", "Data");
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get current power mode
        /// </summary>
        public PowerModeState GetPowerMode()
        {
            try
            {
                int mode = GameZoneCall<int>("GetSmartFanMode", "Data");
                Logger.WriteLine($"Lenovo Power Mode: {mode}");
                return (PowerModeState)mode;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to get power mode: {ex.Message}");
                return PowerModeState.Balance;
            }
        }

        /// <summary>
        /// Set power mode
        /// </summary>
        public void SetPowerMode(PowerModeState mode)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Data", (int)mode }
                };
                
                GameZoneCall("SetSmartFanMode", parameters);
                Logger.WriteLine($"Set Lenovo Power Mode to: {mode}");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set power mode: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get all available power modes
        /// </summary>
        public PowerModeState[] GetAvailableModes()
        {
            // Check if GodMode is supported
            bool supportsGodMode = SupportsGodMode();
            
            if (supportsGodMode)
            {
                return new[]
                {
                    PowerModeState.Quiet,
                    PowerModeState.Balance,
                    PowerModeState.Performance,
                    PowerModeState.Extreme,
                    PowerModeState.GodMode
                };
            }
            else
            {
                return new[]
                {
                    PowerModeState.Quiet,
                    PowerModeState.Balance,
                    PowerModeState.Performance,
                    PowerModeState.Extreme
                };
            }
        }

        /// <summary>
        /// Check if GodMode (Custom mode) is supported
        /// </summary>
        public bool SupportsGodMode()
        {
            try
            {
                // Check if GodMode is supported via LenovoGodMode
                if (Program.lenovoGodMode != null)
                {
                    return Program.lenovoGodMode.IsSupported();
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get Intelligent Sub Mode (for some models)
        /// </summary>
        public int GetIntelligentSubMode()
        {
            try
            {
                return GameZoneCall<int>("GetIntelligentSubMode", "Data");
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Set Intelligent Sub Mode
        /// </summary>
        public void SetIntelligentSubMode(int mode)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Data", mode }
                };
                GameZoneCall("SetIntelligentSubMode", parameters);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set intelligent sub mode: {ex.Message}");
            }
        }

        /// <summary>
        /// Convert PowerModeState to string for display
        /// </summary>
        public static string GetModeName(PowerModeState mode)
        {
            return mode switch
            {
                PowerModeState.Quiet => "Quiet",
                PowerModeState.Balance => "Balance",
                PowerModeState.Performance => "Performance",
                PowerModeState.Extreme => "Extreme",
                PowerModeState.GodMode => "Custom",
                _ => "Unknown"
            };
        }
    }
}

