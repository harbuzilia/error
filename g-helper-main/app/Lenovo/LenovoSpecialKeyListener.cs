using System.Management;
using System.Diagnostics;
using GHelper.Input;

namespace GHelper.Lenovo
{
    /// <summary>
    /// Lenovo special key codes from LENOVO_UTILITY_EVENT WMI
    /// </summary>
    public enum LenovoSpecialKey
    {
        FnF9 = 1,           // Smart key (customizable)
        FnLockOn = 2,       // Fn+Esc - Fn Lock enabled (LED ON)
        FnLockOff = 3,      // Fn+Esc - Fn Lock disabled (LED OFF)
        FnR = 16,           // Refresh rate toggle
        FnN = 42,           // Fn+N - bring to foreground
        FnF12 = 72          // Calculator
    }

    /// <summary>
    /// Listens for Lenovo special key events via WMI
    /// </summary>
    public class LenovoSpecialKeyListener : IDisposable
    {
        private ManagementEventWatcher? _watcher;
        private bool _isRunning = false;

        public event EventHandler<LenovoSpecialKey>? SpecialKeyPressed;

        public LenovoSpecialKeyListener()
        {
            try
            {
                Start();
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"LenovoSpecialKeyListener init failed: {ex.Message}");
            }
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                var query = new WqlEventQuery("SELECT * FROM LENOVO_UTILITY_EVENT");
                _watcher = new ManagementEventWatcher("root\\WMI", query.QueryString);
                _watcher.EventArrived += OnEventArrived;
                _watcher.Start();
                _isRunning = true;
                Logger.WriteLine("LenovoSpecialKeyListener started");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"LenovoSpecialKeyListener start failed: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                _watcher?.Stop();
                _isRunning = false;
                Logger.WriteLine("LenovoSpecialKeyListener stopped");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"LenovoSpecialKeyListener stop failed: {ex.Message}");
            }
        }

        private void OnEventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                if (e?.NewEvent == null)
                {
                    Logger.WriteLine("LenovoSpecialKeyListener: NewEvent is null");
                    return;
                }

                // Log all properties to find the correct one
                Logger.WriteLine("Lenovo special key event properties:");
                foreach (PropertyData prop in e.NewEvent.Properties)
                {
                    Logger.WriteLine($"  {prop.Name} = {prop.Value}");
                }

                // Get key code from PressTypeDataVal property
                object? keyValue = null;
                try
                {
                    if (e.NewEvent.Properties["PressTypeDataVal"]?.Value != null)
                        keyValue = e.NewEvent.Properties["PressTypeDataVal"].Value;
                }
                catch { }

                if (keyValue == null)
                {
                    Logger.WriteLine("No key code found in event");
                    return;
                }

                var keyCode = Convert.ToInt32(keyValue);
                Logger.WriteLine($"Lenovo special key code: {keyCode}");

                if (Enum.IsDefined(typeof(LenovoSpecialKey), keyCode))
                {
                    var specialKey = (LenovoSpecialKey)keyCode;
                    SpecialKeyPressed?.Invoke(this, specialKey);
                    HandleSpecialKey(specialKey);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"LenovoSpecialKeyListener event handler error: {ex.Message}");
                Logger.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void HandleSpecialKey(LenovoSpecialKey key)
        {
            switch (key)
            {
                case LenovoSpecialKey.FnF9:
                    // Smart key - bring G-Helper to foreground
                    try
                    {
                        Program.settingsForm.Invoke((MethodInvoker)delegate
                        {
                            Program.settingsForm.Show();
                            Program.settingsForm.WindowState = FormWindowState.Normal;
                            Program.settingsForm.Activate();
                            Program.settingsForm.BringToFront();
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine($"Failed to activate G-Helper: {ex.Message}");
                    }
                    break;

                case LenovoSpecialKey.FnLockOn:
                    // Fn+Esc - Fn Lock enabled (code 2 = LED ON)
                    try
                    {
                        AppConfig.Set("fn_lock", 1);
                        Program.settingsForm.BeginInvoke(Program.settingsForm.VisualiseFnLock);
                        Logger.WriteLine("Fn Lock enabled via hardware key (code 2)");
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine($"Failed to handle Fn Lock On: {ex.Message}");
                    }
                    break;

                case LenovoSpecialKey.FnLockOff:
                    // Fn+Esc - Fn Lock disabled (code 3 = LED OFF)
                    try
                    {
                        AppConfig.Set("fn_lock", 0);
                        Program.settingsForm.BeginInvoke(Program.settingsForm.VisualiseFnLock);
                        Logger.WriteLine("Fn Lock disabled via hardware key (code 3)");
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine($"Failed to handle Fn Lock Off: {ex.Message}");
                    }
                    break;

                case LenovoSpecialKey.FnN:
                    // Fn+N - bring G-Helper to foreground
                    try
                    {
                        Program.settingsForm.Invoke((MethodInvoker)delegate
                        {
                            Program.settingsForm.Show();
                            Program.settingsForm.WindowState = FormWindowState.Normal;
                            Program.settingsForm.Activate();
                            Program.settingsForm.BringToFront();
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine($"Failed to activate G-Helper: {ex.Message}");
                    }
                    break;

                case LenovoSpecialKey.FnF12:
                    // Calculator
                    try
                    {
                        Process.Start("calc.exe");
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine($"Failed to open calculator: {ex.Message}");
                    }
                    break;

                case LenovoSpecialKey.FnR:
                    // Refresh rate toggle
                    GHelper.Display.ScreenControl.ToggleScreenRate();
                    break;
            }
        }

        public void Dispose()
        {
            Stop();
            _watcher?.Dispose();
        }
    }
}

