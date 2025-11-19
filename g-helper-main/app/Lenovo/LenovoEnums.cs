namespace GHelper.Lenovo
{
    // Power Mode States for Lenovo Legion
    public enum PowerModeState
    {
        Quiet = 1,
        Balance = 2,
        Performance = 3,
        Extreme = 224,  // 223 + 1 offset
        GodMode = 255   // 254 + 1 offset
    }

    // Battery States
    public enum BatteryState
    {
        Conservation = 0,
        Normal = 1,
        RapidCharge = 2
    }

    // GPU/Hybrid Mode States
    public enum HybridModeState
    {
        On = 0,           // Hybrid mode (iGPU + dGPU)
        OnIGPUOnly = 1,   // iGPU only (dGPU disabled)
        OnAuto = 2,       // Auto switch
        Off = 3           // Discrete only (dGPU direct)
    }

    // iGPU Mode States
    public enum IGPUModeState
    {
        Default = 0,
        IGPUOnly = 1,
        Auto = 2
    }

    // GSync State
    public enum GSyncState
    {
        Off = 0,
        On = 1
    }

    // OverDrive State
    public enum OverDriveState
    {
        Off = 0,
        On = 1
    }

    // White Keyboard Backlight State
    public enum WhiteKeyboardBacklightState
    {
        Off = 0,
        Low = 1,
        High = 2
    }

    // Touchpad Lock State
    public enum TouchpadLockState
    {
        Off = 0,
        On = 1
    }

    // Win Key State
    public enum WinKeyState
    {
        Enabled = 0,
        Disabled = 1
    }

    // Battery Night Charge State
    public enum BatteryNightChargeState
    {
        Off = 0,
        On = 1
    }

    // Always On USB State
    public enum AlwaysOnUSBState
    {
        Off = 0,
        OnWhenSleeping = 1,
        OnAlways = 2
    }

    // Instant Boot State
    public enum InstantBootState
    {
        Off = 0,
        AcAdapter = 1,
        UsbPowerDelivery = 2,
        AcAdapterAndUsbPowerDelivery = 3
    }

    // Flip To Start State
    public enum FlipToStartState
    {
        Off = 0,
        On = 1
    }

    // HDR State
    public enum HDRState
    {
        Off = 0,
        On = 1
    }

    // Smart Fn Lock State (deprecated - use ModifierKey instead)
    public enum SmartFnLockState
    {
        Off = 0,
        On = 1
    }

    // Modifier Key Flags for Smart Fn Lock
    [Flags]
    public enum ModifierKey
    {
        None = 0,
        Shift = 1,
        Ctrl = 2,
        Alt = 4
    }

    // Fan Table Type
    public enum FanTableType
    {
        Unknown = 0,
        CPU = 1,
        CPUSensor = 2,
        GPU = 3,
        GPU2 = 4,
        PCH = 5
    }

    // Capability IDs for WMI calls
    public enum CapabilityID : uint
    {
        IGPUMode = 0x00010000,
        FlipToStart = 0x00030000,
        InstantBootAc = 0x00050000,
        InstantBootUsbPowerDelivery = 0x00060000,
        SupportedPowerModes = 0x00070000,
        OverDrive = 0x001A0000,
        CPUShortTermPowerLimit = 0x0101FF00,
        CPULongTermPowerLimit = 0x0102FF00,
        CPUPeakPowerLimit = 0x0103FF00,
        CPUTemperatureLimit = 0x0104FF00,
        GPUPowerBoost = 0x0201FF00,
        GPUConfigurableTGP = 0x0202FF00,
        GPUTemperatureLimit = 0x0203FF00,
        FanFullSpeed = 0x04020000,
        CpuCurrentFanSpeed = 0x04030001,
        GpuCurrentFanSpeed = 0x04030002,
        PchCurrentFanSpeed = 0x04030004,
        PchCurrentTemperature = 0x05010000,
        CpuCurrentTemperature = 0x05040000,
        GpuCurrentTemperature = 0x05050000
    }
}

