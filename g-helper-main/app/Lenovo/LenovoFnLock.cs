using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace GHelper.Lenovo
{
    public class LenovoFnLock
    {
        private const string ENERGY_DRV_PATH = "\\\\.\\EnergyDrv";
        private const uint IOCTL_ENERGY_SETTINGS = 0x831020E8;
        private const uint FN_LOCK_BUFFER_VALUE = 0x2;

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

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        public static bool IsSupported()
        {
            try
            {
                using var handle = CreateFile(
                    ENERGY_DRV_PATH,
                    GENERIC_READ | GENERIC_WRITE,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);

                return !handle.IsInvalid;
            }
            catch
            {
                return false;
            }
        }

        public static unsafe bool GetFnLockState()
        {
            try
            {
                using var handle = CreateFile(
                    ENERGY_DRV_PATH,
                    GENERIC_READ | GENERIC_WRITE,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);

                if (handle.IsInvalid)
                {
                    int openError = Marshal.GetLastWin32Error();
                    Logger.WriteLine($"Failed to open EnergyDrv for Fn Lock read, error: {openError}");
                    return false;
                }

                uint inBuffer = FN_LOCK_BUFFER_VALUE;
                uint outBuffer = 0;
                uint bytesReturned = 0;

                Logger.WriteLine($"Calling DeviceIoControl for Fn Lock read: inBuffer=0x{inBuffer:X}, IOCTL=0x{IOCTL_ENERGY_SETTINGS:X}");

                bool success = DeviceIoControl(
                    handle,
                    IOCTL_ENERGY_SETTINGS,
                    &inBuffer,
                    sizeof(uint),
                    &outBuffer,
                    sizeof(uint),
                    &bytesReturned,
                    IntPtr.Zero);

                if (success)
                {
                    // Bit 10 indicates Fn Lock state
                    bool fnLockOn = (outBuffer & (1 << 10)) != 0;
                    Logger.WriteLine($"Fn Lock state read: outBuffer=0x{outBuffer:X}, bytesReturned={bytesReturned}, Fn Lock: {fnLockOn}");
                    return fnLockOn;
                }

                int error = Marshal.GetLastWin32Error();
                Logger.WriteLine($"Failed to read Fn Lock state, error: {error}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Error reading Fn Lock state: {ex.Message}");
                return false;
            }
        }

        public static unsafe bool SetFnLockState(bool enabled)
        {
            try
            {
                using var handle = CreateFile(
                    ENERGY_DRV_PATH,
                    GENERIC_READ | GENERIC_WRITE,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);

                if (handle.IsInvalid)
                {
                    int openError = Marshal.GetLastWin32Error();
                    Logger.WriteLine($"Failed to open EnergyDrv for Fn Lock write, error: {openError}");
                    return false;
                }

                uint inBuffer = enabled ? 0xEu : 0xFu;
                uint outBuffer = 0;
                uint bytesReturned = 0;

                Logger.WriteLine($"Calling DeviceIoControl for Fn Lock write: inBuffer=0x{inBuffer:X}, IOCTL=0x{IOCTL_ENERGY_SETTINGS:X}");

                bool success = DeviceIoControl(
                    handle,
                    IOCTL_ENERGY_SETTINGS,
                    &inBuffer,
                    sizeof(uint),
                    &outBuffer,
                    sizeof(uint),
                    &bytesReturned,
                    IntPtr.Zero);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.WriteLine($"Set Fn Lock to {enabled}: Failed, error: {error}");
                    return false;
                }

                Logger.WriteLine($"Set Fn Lock to {enabled}: OK, outBuffer=0x{outBuffer:X}, bytesReturned={bytesReturned}");

                // Verify state was set correctly by reading it back
                System.Threading.Thread.Sleep(50); // Small delay for hardware to update

                bool currentState = GetFnLockState();
                if (currentState != enabled)
                {
                    Logger.WriteLine($"Warning: Fn Lock state verification failed. Expected: {enabled}, Got: {currentState}");
                    return false;
                }

                Logger.WriteLine($"Fn Lock state verified: {enabled}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Error setting Fn Lock state: {ex.Message}");
                return false;
            }
        }
    }
}

