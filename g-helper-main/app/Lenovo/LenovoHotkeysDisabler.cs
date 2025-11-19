using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;
using GHelper.Helpers;
using System.Linq;

namespace GHelper.Lenovo
{
    /// <summary>
    /// Disables/enables Lenovo Hotkeys service without uninstalling it
    /// Based on LenovoLegionToolkit FnKeysDisabler
    /// </summary>
    public class LenovoHotkeysDisabler
    {
        private const string SERVICE_NAME = "LenovoFnAndFunctionKeys";
        private readonly string[] PROCESS_NAMES = ["LenovoUtilityUI", "LenovoUtilityService", "LenovoSmartKey"];

        /// <summary>
        /// Get current status of Lenovo Hotkeys service
        /// </summary>
        public SoftwareStatus GetStatus()
        {
            try
            {
                bool isEnabled = IsServiceRunning() || AreProcessesRunning();
                bool isInstalled = IsServiceInstalled();

                if (isEnabled)
                    return SoftwareStatus.Enabled;
                else if (!isInstalled)
                    return SoftwareStatus.NotFound;
                else
                    return SoftwareStatus.Disabled;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to get Lenovo Hotkeys status: {ex.Message}");
                return SoftwareStatus.NotFound;
            }
        }

        /// <summary>
        /// Enable Lenovo Hotkeys service
        /// </summary>
        public void Enable()
        {
            try
            {
                Logger.WriteLine("Enabling Lenovo Hotkeys service...");

                SetServiceEnabled(true);
                SetUwpStartup(true);

                Logger.WriteLine("Lenovo Hotkeys service enabled");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to enable Lenovo Hotkeys service: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Disable Lenovo Hotkeys service
        /// </summary>
        public void Disable()
        {
            try
            {
                Logger.WriteLine("Disabling Lenovo Hotkeys service...");

                KillProcesses();
                SetServiceEnabled(false);
                SetUwpStartup(false);

                Logger.WriteLine("Lenovo Hotkeys service disabled");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to disable Lenovo Hotkeys service: {ex.Message}");
                throw;
            }
        }

        private bool IsServiceInstalled()
        {
            try
            {
                return ServiceController.GetServices().Any(s => s.ServiceName == SERVICE_NAME);
            }
            catch
            {
                return false;
            }
        }

        private bool IsServiceRunning()
        {
            try
            {
                using var service = new ServiceController(SERVICE_NAME);
                return service.Status != ServiceControllerStatus.Stopped;
            }
            catch
            {
                return false;
            }
        }

        private bool AreProcessesRunning()
        {
            try
            {
                // Check standard processes
                foreach (var processName in PROCESS_NAMES)
                {
                    if (Process.GetProcesses().Any(p => p.ProcessName.StartsWith(processName, StringComparison.InvariantCultureIgnoreCase)))
                        return true;
                }

                // Check "utility" process with "Lenovo Hotkeys" description
                foreach (var process in Process.GetProcessesByName("utility"))
                {
                    try
                    {
                        var description = process.MainModule?.FileVersionInfo.FileDescription;
                        if (description != null && description.Equals("Lenovo Hotkeys", StringComparison.InvariantCultureIgnoreCase))
                            return true;
                    }
                    catch { /* Ignored */ }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void SetServiceEnabled(bool enabled)
        {
            try
            {
                if (!IsServiceInstalled())
                {
                    Logger.WriteLine($"Service {SERVICE_NAME} not found");
                    return;
                }

                if (enabled)
                {
                    // Enable and start service
                    ProcessHelper.StartEnableService(SERVICE_NAME, automatic: true);
                }
                else
                {
                    // Stop and disable service
                    ProcessHelper.StopDisableService(SERVICE_NAME, disable: "Disabled");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set service {SERVICE_NAME} to {(enabled ? "enabled" : "disabled")}: {ex.Message}");
                throw;
            }
        }

        private void KillProcesses()
        {
            try
            {
                // Kill standard processes
                foreach (var processName in PROCESS_NAMES)
                {
                    foreach (var process in Process.GetProcesses().Where(p => p.ProcessName.StartsWith(processName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        try
                        {
                            process.Kill();
                            process.WaitForExit(5000);
                            Logger.WriteLine($"Killed process: {process.ProcessName}");
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLine($"Failed to kill process {process.ProcessName}: {ex.Message}");
                        }
                    }
                }

                // Kill "utility" process with "Lenovo Hotkeys" description
                foreach (var process in Process.GetProcessesByName("utility"))
                {
                    try
                    {
                        var description = process.MainModule?.FileVersionInfo.FileDescription;
                        if (description != null && description.Equals("Lenovo Hotkeys", StringComparison.InvariantCultureIgnoreCase))
                        {
                            process.Kill();
                            process.WaitForExit(5000);
                            Logger.WriteLine($"Killed process: utility (Lenovo Hotkeys)");
                        }
                    }
                    catch { /* Ignored */ }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to kill processes: {ex.Message}");
            }
        }

        private void SetUwpStartup(string appPattern, string subKeyName, bool enabled)
        {
            try
            {
                const string hive = "HKEY_CURRENT_USER";
                const string subKey = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData";
                const string valueName = "State";

                // Find the app key
                using var baseKey = Registry.CurrentUser.OpenSubKey(subKey);
                if (baseKey == null)
                    return;

                string? startupKey = null;
                foreach (var keyName in baseKey.GetSubKeyNames())
                {
                    if (keyName.Contains(appPattern, StringComparison.InvariantCultureIgnoreCase))
                    {
                        startupKey = System.IO.Path.Combine(subKey, keyName, subKeyName);
                        break;
                    }
                }

                if (startupKey == null)
                    return;

                // Set the value (0x2 = enabled, 0x1 = disabled)
                var fullPath = $"{hive}\\{startupKey}";
                Microsoft.Win32.Registry.SetValue(fullPath, valueName, enabled ? 0x2 : 0x1, RegistryValueKind.DWord);
                Logger.WriteLine($"Set UWP startup for {appPattern} to {(enabled ? "enabled" : "disabled")}");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set UWP startup: {ex.Message}");
            }
        }

        private void SetUwpStartup(bool enabled)
        {
            SetUwpStartup("LenovoUtility", "LenovoUtilityID", enabled);
        }
    }

    public enum SoftwareStatus
    {
        Enabled,
        Disabled,
        NotFound
    }
}

