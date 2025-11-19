# Lenovo Legion Integration for G-Helper

This folder contains the integration layer for Lenovo Legion laptops into G-Helper.

## Overview

The Lenovo integration brings Legion Toolkit functionality into G-Helper's Windows Forms interface, allowing Lenovo Legion laptop users to control their hardware using the same lightweight application that ASUS users enjoy.

## Architecture

### Core Classes

1. **LenovoWMI.cs** - Base WMI communication class
   - Provides methods to interact with Lenovo's WMI interfaces
   - Handles `LENOVO_GAMEZONE_DATA`, `LENOVO_FAN_METHOD`, and `LENOVO_OTHER_METHOD`
   - Includes error handling and logging

2. **LenovoEnums.cs** - Enumerations
   - `PowerModeState` - Quiet, Balance, Performance, Extreme, GodMode
   - `BatteryState` - Conservation, Normal, RapidCharge
   - `HybridModeState` - GPU switching modes
   - `IGPUModeState` - iGPU mode states
   - `CapabilityID` - WMI capability identifiers

3. **LenovoPowerMode.cs** - Power mode management
   - Get/Set power modes (Quiet, Balance, Performance, Extreme, Custom)
   - Check supported modes
   - Intelligent sub-mode support

4. **LenovoGPUMode.cs** - GPU/Hybrid mode management
   - Switch between Hybrid, iGPU Only, Auto, and Discrete modes
   - GSync control
   - Requires reboot for mode changes

5. **LenovoBattery.cs** - Battery management
   - Conservation mode (60-80% charging limit)
   - Rapid charge mode
   - Normal charging mode
   - Uses EnergyDrv driver

6. **LenovoFanControl.cs** - Fan control (GodMode/Custom)
   - Read CPU/GPU temperatures
   - Read CPU/GPU fan speeds
   - Set custom fan curves
   - Fan full speed control

## Integration Points

### Program.cs
- Added Lenovo controller initialization
- Manufacturer detection (ASUS vs Lenovo)
- Conditional initialization based on hardware

### AppConfig.cs
- Added `IsLenovo()` method
- Added `IsLegion()` method
- Added `GetManufacturer()` method
- Added `IsASUS()` method for compatibility

## WMI Methods Used

### LENOVO_GAMEZONE_DATA
- `IsSupportSmartFan` - Check if power modes are supported
- `GetSmartFanMode` - Get current power mode
- `SetSmartFanMode` - Set power mode
- `IsSupportIGPUMode` - Check if GPU switching is supported
- `GetIGPUModeStatus` - Get current GPU mode
- `SetIGPUModeStatus` - Set GPU mode
- `IsSupportGSync` - Check GSync support
- `GetGSyncStatus` / `SetGSyncStatus` - GSync control

### LENOVO_FAN_METHOD
- `Fan_GetCurrentSensorTemperature` - Read sensor temperature
- `Fan_GetCurrentFanSpeed` - Read fan speed
- `Fan_Set_Table` - Set custom fan curve
- `Fan_Get_FullSpeed` / `Fan_Set_FullSpeed` - Full speed control

### EnergyDrv Driver
- `IOCTL_ENERGY_BATTERY_CHARGE_MODE` (0x831020F8) - Battery mode control

## Status

### ✅ Completed
- [x] Base WMI infrastructure
- [x] Power mode control (integrated into ModeControl)
- [x] GPU/Hybrid mode control (integrated into GPUModeControl)
- [x] Battery management (integrated into BatteryControl)
- [x] Fan control sensors (integrated into ModeControl)
- [x] Manufacturer detection
- [x] Program.cs integration
- [x] RGB keyboard base class (LenovoRGB.cs)
- [x] UI integration - existing G-Helper buttons now work with Lenovo!

### 🚧 Requires Testing
- [ ] Power mode switching on real Lenovo Legion
- [ ] GPU mode switching (requires reboot)
- [ ] Battery Conservation mode
- [ ] Fan curves in Custom/GodMode
- [ ] RGB keyboard (requires HID device enumeration)

### 📋 Future Enhancements
- [ ] Spectrum RGB keyboard support
- [ ] Complete RGB HID implementation
- [ ] Additional sensor monitoring in UI
- [ ] Power limit controls (TDP) in UI
- [ ] Custom fan curve UI for Lenovo

## Testing

To test on a Lenovo Legion laptop:

1. Build the project
2. Run as Administrator (required for WMI access)
3. Check logs for "Lenovo device detected"
4. Verify controllers are initialized

## Notes

- Requires Administrator privileges for WMI access
- GPU mode changes require system reboot
- Conservation mode limits charging to 60-80% (firmware controlled)
- Custom fan curves only work in GodMode/Custom power mode

## References

Based on [Lenovo Legion Toolkit](https://github.com/BartoszCichecki/LenovoLegionToolkit) by BartoszCichecki

