using System.Runtime.InteropServices;
using System.Management;

namespace GHelper.Lenovo
{
    /// <summary>
    /// Manages fan control for Lenovo Legion laptops (GodMode/Custom mode)
    /// Equivalent to GodModeController in Legion Toolkit
    /// </summary>
    public class LenovoFanControl : LenovoWMI
    {
        private const int CPU_SENSOR_ID = 3;
        private const int GPU_SENSOR_ID = 4;
        private const int CPU_FAN_ID = 0;
        private const int GPU_FAN_ID = 1;

        // Fan table structure (10 points)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FanTable
        {
            public byte FSTM;  // Mode (always 1)
            public byte FSID;  // ID (always 0)
            public uint FSTL;  // Length (always 0)
            public ushort FSS0;
            public ushort FSS1;
            public ushort FSS2;
            public ushort FSS3;
            public ushort FSS4;
            public ushort FSS5;
            public ushort FSS6;
            public ushort FSS7;
            public ushort FSS8;
            public ushort FSS9;

            public ushort[] GetTable()
            {
                return new ushort[] { FSS0, FSS1, FSS2, FSS3, FSS4, FSS5, FSS6, FSS7, FSS8, FSS9 };
            }

            public byte[] GetBytes()
            {
                int size = Marshal.SizeOf(this);
                byte[] arr = new byte[size];
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(this, ptr, false);
                    Marshal.Copy(ptr, arr, 0, size);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
                return arr;
            }
        }

        public class FanTableData
        {
            public byte FanId { get; set; }
            public byte SensorId { get; set; }
            public ushort[] FanSpeeds { get; set; } = new ushort[10];
            public ushort[] Temps { get; set; } = new ushort[10];
        }

        public LenovoFanControl() : base()
        {
        }

        /// <summary>
        /// Check if fan control is supported
        /// </summary>
        public bool IsSupported()
        {
            try
            {
                // Check if we can read fan table data
                return true; // Most Legion laptops support this
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get current CPU temperature
        /// </summary>
        public int GetCPUTemperature()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "SensorID", CPU_SENSOR_ID }
                };
                
                int temp = FanMethodCall<int>("Fan_GetCurrentSensorTemperature", "CurrentSensorTemperature", parameters);
                return temp > 0 ? temp : -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Get current GPU temperature
        /// </summary>
        public int GetGPUTemperature()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "SensorID", GPU_SENSOR_ID }
                };
                
                int temp = FanMethodCall<int>("Fan_GetCurrentSensorTemperature", "CurrentSensorTemperature", parameters);
                return temp > 0 ? temp : -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Get current CPU fan speed
        /// </summary>
        public int GetCPUFanSpeed()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "FanID", CPU_FAN_ID }
                };
                
                return FanMethodCall<int>("Fan_GetCurrentFanSpeed", "CurrentFanSpeed", parameters);
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Get current GPU fan speed
        /// </summary>
        public int GetGPUFanSpeed()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "FanID", GPU_FAN_ID }
                };
                
                return FanMethodCall<int>("Fan_GetCurrentFanSpeed", "CurrentFanSpeed", parameters);
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Set fan table (custom fan curve)
        /// </summary>
        public void SetFanTable(byte[] fanTable)
        {
            try
            {
                if (fanTable == null || fanTable.Length == 0)
                {
                    Logger.WriteLine("Invalid fan table");
                    return;
                }

                var parameters = new Dictionary<string, object>
                {
                    { "FanTable", fanTable }
                };
                
                FanMethodCall("Fan_Set_Table", parameters);
                Logger.WriteLine($"Set fan table: {fanTable.Length} bytes");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set fan table: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get fan full speed status
        /// </summary>
        public bool GetFanFullSpeed()
        {
            try
            {
                return FanMethodCall<bool>("Fan_Get_FullSpeed", "Status");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Set fan full speed
        /// </summary>
        public void SetFanFullSpeed(bool enabled)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Status", enabled ? 1 : 0 }
                };

                FanMethodCall("Fan_Set_FullSpeed", parameters);
                Logger.WriteLine($"Set fan full speed: {enabled}");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to set fan full speed: {ex.Message}");
            }
        }

        /// <summary>
        /// Read fan table data from WMI for specific mode
        /// </summary>
        public List<FanTableData> GetFanTableData(int mode)
        {
            var result = new List<FanTableData>();

            try
            {
                using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM LENOVO_FAN_TABLE_DATA"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        try
                        {
                            int objMode = obj["Mode"] != null ? Convert.ToInt32(obj["Mode"]) : -1;

                            // Mode mapping: 0=Silent(1), 1=Balanced(2), 2=Performance(3), 3=Custom(4)
                            if (objMode != mode + 1)
                                continue;

                            var data = new FanTableData
                            {
                                FanId = Convert.ToByte(obj["Fan_Id"]),
                                SensorId = Convert.ToByte(obj["Sensor_ID"])
                            };

                            if (obj["FanTable_Data"] is ushort[] fanSpeeds)
                                data.FanSpeeds = fanSpeeds;

                            if (obj["SensorTable_Data"] is ushort[] temps)
                                data.Temps = temps;

                            result.Add(data);
                            Logger.WriteLine($"Fan table data: Mode={objMode}, FanId={data.FanId}, SensorId={data.SensorId}");
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLine($"Error parsing fan table entry: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to read fan table data: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Convert Lenovo fan table data to ASUS-compatible curve format (16 bytes: 8 temps + 8 fan%)
        /// </summary>
        public byte[] ConvertLenovoToAsusCurve(FanTableData data)
        {
            byte[] curve = new byte[16];

            // Take first 8 points from 10-point Lenovo curve
            for (int i = 0; i < 8; i++)
            {
                // Temperature (0-127°C)
                curve[i] = (byte)Math.Min((int)127, (int)data.Temps[i]);

                // Fan speed: convert RPM to percentage (0-100)
                // Lenovo uses RPM values, need to normalize to 0-100%
                // Typical max RPM is around 5000-6000
                int maxRpm = data.FanSpeeds.Max();
                if (maxRpm > 0)
                {
                    int fanPercent = (int)((data.FanSpeeds[i] * 100.0) / maxRpm);
                    curve[i + 8] = (byte)Math.Min(100, Math.Max(0, fanPercent));
                }
                else
                {
                    curve[i + 8] = 0;
                }
            }

            return curve;
        }

        /// <summary>
        /// Convert ASUS curve format to Lenovo FanTable structure
        /// </summary>
        public FanTable ConvertAsusCurveToLenovo(byte[] curve, FanTableData referenceData)
        {
            var table = new FanTable
            {
                FSTM = 1,
                FSID = 0,
                FSTL = 0
            };

            // Get max RPM from reference data
            int maxRpm = referenceData.FanSpeeds.Max();
            if (maxRpm == 0) maxRpm = 5000; // Default fallback

            // Convert 8 points to 10 points by interpolation
            ushort[] values = new ushort[10];

            for (int i = 0; i < 8; i++)
            {
                // Convert percentage back to RPM
                int fanPercent = curve[i + 8];
                values[i] = (ushort)((fanPercent * maxRpm) / 100);
            }

            // Interpolate last 2 points
            values[8] = (ushort)((values[7] + maxRpm) / 2);
            values[9] = (ushort)maxRpm;

            table.FSS0 = values[0];
            table.FSS1 = values[1];
            table.FSS2 = values[2];
            table.FSS3 = values[3];
            table.FSS4 = values[4];
            table.FSS5 = values[5];
            table.FSS6 = values[6];
            table.FSS7 = values[7];
            table.FSS8 = values[8];
            table.FSS9 = values[9];

            return table;
        }
    }
}

