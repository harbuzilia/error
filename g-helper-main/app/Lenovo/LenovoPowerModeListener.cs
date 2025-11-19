using System.Management;

namespace GHelper.Lenovo
{
    /// <summary>
    /// Listens for Lenovo power mode changes (Fn+Q key press)
    /// Monitors WMI event: LENOVO_GAMEZONE_SMART_FAN_MODE_EVENT
    /// </summary>
    public class LenovoPowerModeListener : IDisposable
    {
        private ManagementEventWatcher? _watcher;
        private bool _isListening = false;

        public event EventHandler<PowerModeState>? PowerModeChanged;

        public LenovoPowerModeListener()
        {
        }

        /// <summary>
        /// Start listening for power mode change events
        /// </summary>
        public void Start()
        {
            if (_isListening)
            {
                Logger.WriteLine("Power mode listener already started");
                return;
            }

            try
            {
                // Create WMI event query for power mode changes
                var query = new WqlEventQuery("SELECT * FROM LENOVO_GAMEZONE_SMART_FAN_MODE_EVENT");
                
                _watcher = new ManagementEventWatcher("root\\WMI", query.QueryString);
                _watcher.EventArrived += OnPowerModeEvent;
                _watcher.Start();

                _isListening = true;
                Logger.WriteLine("Lenovo power mode listener started");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to start power mode listener: {ex.Message}");
                _isListening = false;
            }
        }

        /// <summary>
        /// Stop listening for power mode change events
        /// </summary>
        public void Stop()
        {
            if (!_isListening)
                return;

            try
            {
                if (_watcher != null)
                {
                    _watcher.Stop();
                    _watcher.EventArrived -= OnPowerModeEvent;
                    _watcher.Dispose();
                    _watcher = null;
                }

                _isListening = false;
                Logger.WriteLine("Lenovo power mode listener stopped");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Error stopping power mode listener: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle power mode change event
        /// </summary>
        private void OnPowerModeEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                // Extract mode value from event
                var mode = Convert.ToInt32(e.NewEvent["mode"]);
                var powerMode = (PowerModeState)mode;

                Logger.WriteLine($"Power mode changed via Fn+Q: {powerMode}");

                // Raise event on UI thread
                PowerModeChanged?.Invoke(this, powerMode);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Error handling power mode event: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

