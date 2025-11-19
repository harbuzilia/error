using System;
using System.Runtime.InteropServices;

namespace GHelper.Lenovo
{
    public class LenovoHDR
    {
        // Windows Display API structures and functions
        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_SOURCE_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_TARGET_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint outputTechnology;
            public uint rotation;
            public uint scaling;
            public DISPLAYCONFIG_RATIONAL refreshRate;
            public uint scanLineOrdering;
            public bool targetAvailable;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_RATIONAL
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_INFO
        {
            public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
            public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint value;
            public uint colorEncoding;
            public uint bitsPerColorChannel;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint value;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
        {
            public uint type;
            public uint size;
            public LUID adapterId;
            public uint id;
        }

        private const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
        private const uint DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9;
        private const uint DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE = 10;

        [DllImport("user32.dll")]
        private static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

        [DllImport("user32.dll")]
        private static extern int QueryDisplayConfig(uint flags, ref uint numPathArrayElements, [Out] DISPLAYCONFIG_PATH_INFO[] pathInfoArray, ref uint numModeInfoArrayElements, [Out] IntPtr modeInfoArray, IntPtr currentTopologyId);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO requestPacket);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE setPacket);

        public bool IsSupported()
        {
            try
            {
                var info = GetAdvancedColorInfo();
                bool supported = (info.value & 0x1) != 0; // AdvancedColorSupported bit
                Logger.WriteLine($"HDR IsSupported: {supported} (value=0x{info.value:X})");
                return supported;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"HDR IsSupported failed: {ex.Message}");
                return false;
            }
        }

        public HDRState GetState()
        {
            try
            {
                var info = GetAdvancedColorInfo();
                bool enabled = (info.value & 0x2) != 0; // AdvancedColorEnabled bit
                return enabled ? HDRState.On : HDRState.Off;
            }
            catch
            {
                return HDRState.Off;
            }
        }

        public bool SetState(HDRState state)
        {
            try
            {
                var path = GetInternalDisplayPath();
                if (path == null)
                    return false;

                var setInfo = new DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE
                {
                    header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE,
                        size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE>(),
                        adapterId = path.Value.targetInfo.adapterId,
                        id = path.Value.targetInfo.id
                    },
                    value = state == HDRState.On ? 1u : 0u
                };

                int result = DisplayConfigSetDeviceInfo(ref setInfo);
                return result == 0;
            }
            catch
            {
                return false;
            }
        }

        private DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO GetAdvancedColorInfo()
        {
            var path = GetInternalDisplayPath();
            if (path == null)
                throw new InvalidOperationException("Internal display not found");

            var info = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
            {
                header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                {
                    type = DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO,
                    size = (uint)Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>(),
                    adapterId = path.Value.targetInfo.adapterId,
                    id = path.Value.targetInfo.id
                }
            };

            int result = DisplayConfigGetDeviceInfo(ref info);
            if (result != 0)
                throw new InvalidOperationException($"DisplayConfigGetDeviceInfo failed: {result}");

            return info;
        }

        private DISPLAYCONFIG_PATH_INFO? GetInternalDisplayPath()
        {
            uint pathCount, modeCount;
            int error = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount);
            if (error != 0)
            {
                Logger.WriteLine($"HDR GetInternalDisplayPath: GetDisplayConfigBufferSizes failed with error {error}");
                return null;
            }

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            // QueryDisplayConfig can work with IntPtr.Zero for modeInfoArray if we don't need mode info
            // But we need to pass the correct size. Let's try with IntPtr.Zero first.
            error = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, IntPtr.Zero, IntPtr.Zero);
            if (error != 0)
            {
                // If that fails, try with allocated buffer
                int modeInfoSize = Marshal.SizeOf(typeof(IntPtr)) * 100; // Allocate enough space
                IntPtr modesPtr = Marshal.AllocHGlobal(modeInfoSize);
                try
                {
                    error = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modesPtr, IntPtr.Zero);
                    if (error != 0)
                    {
                        Logger.WriteLine($"HDR GetInternalDisplayPath: QueryDisplayConfig failed with error {error}");
                return null;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(modesPtr);
                }
            }

            // Try to find internal display (DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL = 0x80000001)
            const uint DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL = 0x80000001;
            for (int i = 0; i < pathCount; i++)
            {
                if (paths[i].targetInfo.outputTechnology == DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL)
                {
                    Logger.WriteLine($"HDR GetInternalDisplayPath: Found internal display at index {i}");
                    return paths[i];
                }
            }

            // If no internal display found, return first active path
            if (pathCount > 0)
            {
                Logger.WriteLine($"HDR GetInternalDisplayPath: No internal display found, using first path (outputTechnology=0x{paths[0].targetInfo.outputTechnology:X})");
                return paths[0];
            }

            Logger.WriteLine("HDR GetInternalDisplayPath: No active paths found");
            return null;
        }
    }
}

