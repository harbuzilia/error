using System.Runtime.InteropServices;
using System.Diagnostics;
using GHelper.Input;

namespace GHelper.Lenovo
{
    /// <summary>
    /// Driver key codes from EnergyDrv (FnF4, FnF8, FnF10, FnSpace)
    /// </summary>
    [Flags]
    public enum LenovoDriverKey : uint
    {
        FnF10 = 32,      // Touchpad toggle
        FnF4 = 256,      // Microphone toggle
        FnSpace = 4096,  // White keyboard backlight
        FnF8 = 8192      // Airplane mode
    }

    public class LenovoDriverKeyListener : IDisposable
    {
        private const uint IOCTL_KEY_WAIT_HANDLE = 0x831020D8;
        private const uint IOCTL_KEY_VALUE = 0x831020CC;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private IntPtr _energyHandle = IntPtr.Zero;
        private Thread? _listenerThread;
        private bool _isRunning = false;
        private ManualResetEvent? _resetEvent;

        public void Start()
        {
            if (_isRunning)
                return;

            try
            {
                // Open EnergyDrv driver
                _energyHandle = CreateFile(@"\\.\EnergyDrv",
                    0xC0000000u, // GENERIC_READ | GENERIC_WRITE
                    0x00000003u, // FILE_SHARE_READ | FILE_SHARE_WRITE
                    IntPtr.Zero,
                    3, // OPEN_EXISTING
                    0x80, // FILE_ATTRIBUTE_NORMAL
                    IntPtr.Zero);

                if (_energyHandle == IntPtr.Zero || _energyHandle.ToInt64() == -1)
                {
                    Logger.WriteLine("Failed to open EnergyDrv for key listener");
                    return;
                }

                _resetEvent = new ManualResetEvent(false);

                // Bind wait handle
                IntPtr handlePtr = Marshal.AllocHGlobal(16);
                try
                {
                    Marshal.WriteInt32(handlePtr, (int)_resetEvent.SafeWaitHandle.DangerousGetHandle());
                    
                    if (!DeviceIoControl(_energyHandle, IOCTL_KEY_WAIT_HANDLE, handlePtr, 16, IntPtr.Zero, 0, out _, IntPtr.Zero))
                    {
                        Logger.WriteLine("Failed to bind key listener wait handle");
                        return;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(handlePtr);
                }

                // Clear register
                GetKeyValue(out _);

                // Start listener thread
                _isRunning = true;
                _listenerThread = new Thread(ListenerLoop);
                _listenerThread.IsBackground = true;
                _listenerThread.Start();

                Logger.WriteLine("LenovoDriverKeyListener started");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to start LenovoDriverKeyListener: {ex.Message}");
                _isRunning = false;
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _resetEvent?.Set();
            _listenerThread?.Join(1000);
            
            if (_energyHandle != IntPtr.Zero && _energyHandle.ToInt64() != -1)
            {
                CloseHandle(_energyHandle);
                _energyHandle = IntPtr.Zero;
            }

            _resetEvent?.Dispose();
            _resetEvent = null;
        }

        private void ListenerLoop()
        {
            while (_isRunning)
            {
                try
                {
                    _resetEvent?.WaitOne();

                    if (!_isRunning)
                        break;

                    if (GetKeyValue(out uint value))
                    {
                        var key = (LenovoDriverKey)value;
                        Logger.WriteLine($"Driver key event: {key} (0x{value:X})");
                        HandleDriverKey(key);
                    }

                    _resetEvent?.Reset();
                }
                catch (Exception ex)
                {
                    Logger.WriteLine($"LenovoDriverKeyListener error: {ex.Message}");
                }
            }
        }

        private bool GetKeyValue(out uint value)
        {
            value = 0;
            IntPtr inBuffer = Marshal.AllocHGlobal(4);
            IntPtr outBuffer = Marshal.AllocHGlobal(4);

            try
            {
                Marshal.WriteInt32(inBuffer, 0);

                bool result = DeviceIoControl(_energyHandle, IOCTL_KEY_VALUE, inBuffer, 4, outBuffer, 4, out _, IntPtr.Zero);

                if (result)
                    value = (uint)Marshal.ReadInt32(outBuffer);

                return result;
            }
            finally
            {
                Marshal.FreeHGlobal(inBuffer);
                Marshal.FreeHGlobal(outBuffer);
            }
        }

        private void HandleDriverKey(LenovoDriverKey key)
        {
            if (key.HasFlag(LenovoDriverKey.FnF4))
            {
                InputDispatcher.ToggleMic();
            }

            if (key.HasFlag(LenovoDriverKey.FnF8))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = "/c \"start ms-settings:network-airplanemode\"",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                }
                catch (Exception ex)
                {
                    Logger.WriteLine($"Failed to open airplane mode: {ex.Message}");
                }
            }

            if (key.HasFlag(LenovoDriverKey.FnF10))
            {
                InputDispatcher.ToggleTouchpadEvent(true);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

