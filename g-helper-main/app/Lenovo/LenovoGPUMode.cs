namespace GHelper.Lenovo
{
    /// <summary>
    /// Manages GPU/Hybrid modes for Lenovo Legion laptops
    /// Equivalent to HybridModeFeature and IGPUModeFeature in Legion Toolkit
    /// </summary>
    public class LenovoGPUMode : LenovoWMI
    {
        public LenovoGPUMode() : base()
        {
        }

        /// <summary>
        /// Check if iGPU Mode is supported
        /// </summary>
        public bool IsSupported()
        {
            try
            {
                int result = GameZoneCall<int>("IsSupportIGPUMode", "Data");
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get current GPU mode (combines iGPU mode + GSync)
        /// </summary>
        public (IGPUModeState igpuMode, GSyncState gsync) GetGPUMode()
        {
            try
            {
                int mode = GameZoneCall<int>("GetIGPUModeStatus", "Data");
                var igpuMode = (IGPUModeState)mode;

                var gsync = GSyncState.Off;
                if (IsGSyncSupported())
                {
                    gsync = GetGSyncStatus();
                }

                Logger.WriteLine($"Lenovo GPU Mode: iGPU={igpuMode}, GSync={gsync}");
                return (igpuMode, gsync);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to get GPU mode: {ex.Message}");
                return (IGPUModeState.Default, GSyncState.Off);
            }
        }

        /// <summary>
        /// Get current iGPU mode status
        /// </summary>
        public IGPUModeState GetIGPUMode()
        {
            try
            {
                int mode = GameZoneCall<int>("GetIGPUModeStatus", "Data");
                Logger.WriteLine($"Lenovo iGPU Mode: {mode}");
                return (IGPUModeState)mode;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to get iGPU mode: {ex.Message}");
                return IGPUModeState.Default;
            }
        }

        /// <summary>
        /// Set iGPU mode (requires reboot)
        /// </summary>
        public void SetIGPUMode(IGPUModeState mode)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "mode", (int)mode }
                };
                
                GameZoneCall("SetIGPUModeStatus", parameters);
                Logger.WriteLine($"Set Lenovo iGPU Mode to: {mode} (reboot required)");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set iGPU mode: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Notify dGPU status
        /// </summary>
        public void NotifyDGPUStatus(int status)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Status", status }
                };
                GameZoneCall("NotifyDGPUStatus", parameters);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to notify dGPU status: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if GSync is supported
        /// </summary>
        public bool IsGSyncSupported()
        {
            try
            {
                int result = GameZoneCall<int>("IsSupportGSync", "Data");
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get GSync status
        /// </summary>
        public GSyncState GetGSyncStatus()
        {
            try
            {
                int status = GameZoneCall<int>("GetGSyncStatus", "Data");
                return (GSyncState)status;
            }
            catch
            {
                return GSyncState.Off;
            }
        }

        /// <summary>
        /// Set GSync status
        /// </summary>
        public void SetGSyncStatus(GSyncState state)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Data", (int)state }
                };
                GameZoneCall("SetGSyncStatus", parameters);
                Logger.WriteLine($"Set GSync to: {state}");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set GSync: {ex.Message}");
            }
        }

        /// <summary>
        /// Get available GPU modes as strings for UI
        /// </summary>
        public string[] GetAvailableModeNames()
        {
            return new[]
            {
                "Hybrid (iGPU + dGPU)",
                "iGPU Only (dGPU Off)",
                "Auto Switch",
                "Discrete Only (dGPU Direct)"
            };
        }

        /// <summary>
        /// Convert IGPUModeState to user-friendly string
        /// </summary>
        public static string GetModeName(IGPUModeState mode)
        {
            return mode switch
            {
                IGPUModeState.Default => "Hybrid",
                IGPUModeState.IGPUOnly => "iGPU Only",
                IGPUModeState.Auto => "Auto",
                _ => "Unknown"
            };
        }
    }
}

