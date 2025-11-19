using System;
using System.Runtime.InteropServices;

namespace GHelper.Lenovo
{
    /// <summary>
    /// Flip To Start feature using UEFI variables
    /// </summary>
    public class LenovoFlipToStart
    {
        private const string GUID = "{D743491E-F484-4952-A87D-8D5DD189B70C}";
        private const string VARIABLE_NAME = "FBSWIF";
        private const uint VARIABLE_ATTRIBUTES = 0x00000007; // NON_VOLATILE | BOOTSERVICE_ACCESS | RUNTIME_ACCESS

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct FlipToBootStruct
        {
            [MarshalAs(UnmanagedType.U1)]
            public byte FlipToBootEn;
            [MarshalAs(UnmanagedType.U1)]
            public byte Reserved1;
            [MarshalAs(UnmanagedType.U1)]
            public byte Reserved2;
            [MarshalAs(UnmanagedType.U1)]
            public byte Reserved3;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetFirmwareEnvironmentVariableEx(
            string lpName,
            string lpGuid,
            IntPtr pBuffer,
            uint nSize,
            IntPtr pdwAttribubutes);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetFirmwareEnvironmentVariableEx(
            string lpName,
            string lpGuid,
            IntPtr pBuffer,
            uint nSize,
            uint dwAttributes);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            uint DesiredAccess,
            out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupPrivilegeValue(
            string lpSystemName,
            string lpName,
            out long lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AdjustTokenPrivileges(
            IntPtr TokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState,
            uint BufferLength,
            IntPtr PreviousState,
            IntPtr ReturnLength);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const string SE_SYSTEM_ENVIRONMENT_NAME = "SeSystemEnvironmentPrivilege";

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public long Luid;
            public uint Attributes;
        }

        private static bool AddPrivileges()
        {
            try
            {
                IntPtr processHandle = GetCurrentProcess();
                if (!OpenProcessToken(processHandle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr tokenHandle))
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.WriteLine($"FlipToStart AddPrivileges: OpenProcessToken failed with error {error}");
                    return false;
                }

                try
                {
                    if (!LookupPrivilegeValue(null, SE_SYSTEM_ENVIRONMENT_NAME, out long luid))
                    {
                        int error = Marshal.GetLastWin32Error();
                        Logger.WriteLine($"FlipToStart AddPrivileges: LookupPrivilegeValue failed with error {error}");
                        return false;
                    }

                    TOKEN_PRIVILEGES tokenPrivileges = new TOKEN_PRIVILEGES
                    {
                        PrivilegeCount = 1,
                        Luid = luid,
                        Attributes = SE_PRIVILEGE_ENABLED
                    };

                    bool result = AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero);
                    if (!result)
                    {
                        int error = Marshal.GetLastWin32Error();
                        Logger.WriteLine($"FlipToStart AddPrivileges: AdjustTokenPrivileges failed with error {error}");
                    }
                    else
                    {
                        // Check if privilege was actually enabled (sometimes AdjustTokenPrivileges returns true but doesn't enable)
                        int checkError = Marshal.GetLastWin32Error();
                        if (checkError != 0 && checkError != 1300) // 1300 = ERROR_SUCCESS, but sometimes it's set even on success
                        {
                            Logger.WriteLine($"FlipToStart AddPrivileges: Warning - GetLastError after AdjustTokenPrivileges = {checkError}");
                        }
                        else
                        {
                            Logger.WriteLine($"FlipToStart AddPrivileges: Privilege enabled successfully");
                        }
                    }
                    return result;
                }
                finally
                {
                    CloseHandle(tokenHandle);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"FlipToStart AddPrivileges: Exception - {ex.Message}");
                return false;
            }
        }

        public bool IsSupported()
        {
            try
            {
                _ = GetState();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public unsafe FlipToStartState GetState()
        {
            if (!AddPrivileges())
            {
                Logger.WriteLine("FlipToStart: Cannot set UEFI privileges");
                throw new InvalidOperationException("Cannot set UEFI privileges");
            }

            var structSize = Marshal.SizeOf<FlipToBootStruct>();
            var ptr = Marshal.AllocHGlobal(structSize);

            try
            {
                var ptrSize = (uint)structSize;
                uint size = GetFirmwareEnvironmentVariableEx(VARIABLE_NAME, GUID, ptr, ptrSize, IntPtr.Zero);
                // GetFirmwareEnvironmentVariableEx returns size on success, 0 on failure (like Toolkit)
                if (size != 0 && size == ptrSize)
                {
                    var result = Marshal.PtrToStructure<FlipToBootStruct>(ptr);
                    Logger.WriteLine($"FlipToStart: Read successfully, FlipToBootEn={result.FlipToBootEn}");
                    return result.FlipToBootEn == 0 ? FlipToStartState.Off : FlipToStartState.On;
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.WriteLine($"FlipToStart: Cannot read UEFI variable (error: {error}, size={size}, expected={ptrSize})");
                    throw new InvalidOperationException($"Cannot read variable {VARIABLE_NAME} from UEFI: {error}");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public unsafe bool SetState(FlipToStartState state)
        {
            if (!AddPrivileges())
            {
                Logger.WriteLine("FlipToStart: Cannot set UEFI privileges");
                return false;
            }

            var structure = new FlipToBootStruct
            {
                FlipToBootEn = state == FlipToStartState.On ? (byte)1 : (byte)0,
                Reserved1 = 0,
                Reserved2 = 0,
                Reserved3 = 0
            };

            var structSize = Marshal.SizeOf<FlipToBootStruct>();
            var ptr = Marshal.AllocHGlobal(structSize);

            try
            {
                Marshal.StructureToPtr(structure, ptr, false);
                var ptrSize = (uint)structSize;
                if (!SetFirmwareEnvironmentVariableEx(VARIABLE_NAME, GUID, ptr, ptrSize, VARIABLE_ATTRIBUTES))
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.WriteLine($"FlipToStart: Cannot write UEFI variable (error: {error})");
                    throw new InvalidOperationException($"Cannot write variable {VARIABLE_NAME} to UEFI: {error}");
                }

                Logger.WriteLine($"FlipToStart: Set state to {state}");
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}

