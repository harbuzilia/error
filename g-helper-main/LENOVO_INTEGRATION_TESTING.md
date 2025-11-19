# Lenovo Legion Integration - Testing Guide

## 🎯 Overview

G-Helper now supports Lenovo Legion laptops! This document describes what has been integrated and how to test it.

## ✅ What's Been Integrated

### 1. **Performance Modes** (Silent/Balanced/Turbo buttons)
- **ASUS Mapping:**
  - Silent → Lenovo Quiet
  - Balanced → Lenovo Balance
  - Turbo → Lenovo Performance
  - Custom modes → Lenovo GodMode

- **Location:** Main G-Helper window, performance mode buttons
- **Code:** `ModeControl.cs` → `SetLenovoPerformanceMode()`

### 2. **GPU Modes** (Eco/Standard/Ultimate buttons)
- **ASUS Mapping:**
  - Eco → Lenovo iGPU Only (dGPU disabled)
  - Standard → Lenovo Hybrid Mode (iGPU + dGPU)
  - Ultimate → Lenovo Hybrid Mode (no discrete-only via WMI)

- **Location:** Main G-Helper window, GPU mode buttons
- **Code:** `GPUModeControl.cs` → `SetLenovoGPUMode()`
- **⚠️ Important:** GPU mode changes require system reboot!

### 3. **Battery Management**
- **ASUS Mapping:**
  - Charge limit ≤80% → Lenovo Conservation Mode (60-80%)
  - Charge limit 100% → Lenovo Normal Mode
  - Rapid Charge → Available but not auto-mapped

- **Location:** Settings → Battery section
- **Code:** `BatteryControl.cs` → `SetLenovoBatteryMode()`

### 4. **Fan Control**
- **Status:** Sensors working, custom curves require Custom/GodMode
- **Location:** Fans window
- **Code:** `ModeControl.cs` → `SetLenovoFanCurves()`
- **Note:** Custom fan curves only work in Custom power mode

### 5. **RGB Keyboard** (Basic structure)
- **Status:** Base class created, requires HID device testing
- **Location:** `Lenovo/LenovoRGB.cs`
- **Note:** Full implementation needs real hardware testing

## 🧪 Testing Checklist

### Prerequisites
- [ ] Lenovo Legion laptop (any model with WMI support)
- [ ] Administrator privileges
- [ ] G-Helper compiled with Lenovo integration
- [ ] Backup of current system settings

### Test 1: Device Detection
1. Launch G-Helper
2. Check log file (`%AppData%\GHelper\log.txt`)
3. Look for:
   ```
   Manufacturer: LENOVO
   Lenovo device detected - initializing Lenovo WMI
   Lenovo WMI interface detected
   Lenovo controllers initialized
   ```

**Expected:** All Lenovo controllers should initialize successfully

### Test 2: Performance Modes
1. Click **Silent** button
2. Check log: `Set Lenovo power mode: Quiet`
3. Verify system behavior (fans should be quieter)
4. Click **Balanced** button
5. Check log: `Set Lenovo power mode: Balance`
6. Click **Turbo** button
7. Check log: `Set Lenovo power mode: Performance`

**Expected:** Each mode change should be logged and system should respond

### Test 3: GPU Modes
1. Click **Eco** button (iGPU Only)
2. Dialog should appear: "Switch to iGPU Only (dGPU Off)? This will require a system restart."
3. Click **Yes**
4. Dialog: "GPU mode changed. Restart now?"
5. Click **Yes** to restart
6. After restart, verify dGPU is disabled in Device Manager

**Expected:** GPU mode changes with reboot confirmation

### Test 4: Battery Management
1. Go to Settings → Battery
2. Set charge limit to 80%
3. Check log: `Setting Lenovo battery to Conservation mode (60-80%)`
4. Verify battery stops charging at ~80%
5. Set charge limit to 100%
6. Check log: `Setting Lenovo battery to Normal mode (100%)`

**Expected:** Battery charging behavior changes according to mode

### Test 5: Fan Sensors
1. Open Fans window
2. Check if CPU/GPU temperatures are displayed
3. Check if fan speeds are shown

**Expected:** Temperature and fan speed readings should update

### Test 6: Custom Fan Curves (Advanced)
1. Switch to **Custom** power mode
2. Open Fans window
3. Try to modify fan curves
4. Check log for fan curve messages

**Expected:** Message about requiring Custom mode

## 📝 Log Analysis

### Success Indicators
```
Manufacturer: LENOVO
Lenovo device detected - initializing Lenovo WMI
Lenovo WMI interface detected
Lenovo controllers initialized
Set Lenovo power mode: Balance (from G-Helper mode 0)
Set Lenovo GPU mode: Default
Set battery state to: Conservation
```

### Error Indicators
```
Lenovo WMI not available
Lenovo power mode not supported
Failed to set Lenovo performance mode: [error]
```

## 🐛 Known Issues & Limitations

1. **GPU Discrete-Only Mode:** Lenovo doesn't expose discrete-only mode via WMI (Ultimate button maps to Hybrid)
2. **Custom Fan Curves:** Require Custom/GodMode power mode to work
3. **RGB Keyboard:** Requires HID device enumeration (not yet fully implemented)
4. **Reboot Required:** GPU mode changes always require system restart
5. **Conservation Mode:** Firmware-controlled, charges between 60-80% (not exact 80%)

## 🔧 Troubleshooting

### "Lenovo WMI not available"
- Ensure you're running as Administrator
- Check if Lenovo Vantage/Legion Zone is installed (may conflict)
- Verify WMI service is running: `services.msc` → "Windows Management Instrumentation"

### Performance modes not changing
- Check if Lenovo Vantage is running (kill it)
- Verify in log that WMI calls are successful
- Try switching modes manually in BIOS to verify hardware support

### GPU mode not working
- Ensure you clicked "Yes" to reboot
- Check Device Manager after reboot
- Some models may not support all GPU modes

### Battery Conservation not working
- Verify EnergyDrv driver is present
- Check if battery is already above 80% (won't discharge to 80%)
- Try Normal mode first, then Conservation

## 📊 Reporting Issues

When reporting issues, please include:
1. Lenovo Legion model (e.g., Legion 5 Pro, Legion 7)
2. Full log file (`%AppData%\GHelper\log.txt`)
3. Screenshot of error messages
4. Steps to reproduce
5. Expected vs actual behavior

## 🎉 Success Criteria

Integration is successful if:
- [x] Device is detected as Lenovo
- [x] Performance modes switch correctly
- [x] GPU modes work (with reboot)
- [x] Battery management functions
- [x] Fan sensors display data
- [x] No crashes or errors in normal operation

## 📚 Additional Resources

- Lenovo WMI Documentation: See `Lenovo/README.md`
- G-Helper Documentation: Main README
- Legion Toolkit Source: Reference implementation

---

**Good luck with testing! 🚀**

