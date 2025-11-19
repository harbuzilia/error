# Краткая сводка: Интеграция Lenovo Legion в G-Helper

## Что это за проект?

**G-Helper** - легковесное приложение для управления ASUS ноутбуками (альтернатива тяжелому Armoury Crate).
Изначально работало только с ASUS через ACPI интерфейс.

**Цель проекта** - портировать функциональность Lenovo Legion Toolkit в G-Helper, чтобы владельцы Lenovo Legion могли использовать G-Helper вместо Lenovo Vantage.

**Подход:**
- Сохранить всю существующую ASUS функциональность
- Добавить Lenovo-специфичные классы в папку `Lenovo/`
- Адаптировать UI для работы с обоими производителями
- Использовать WMI для Lenovo вместо ACPI

## Архитектура

### Технологии:
- C# / .NET 8.0 / Windows Forms
- WMI (Windows Management Instrumentation) для Lenovo
- ACPI для ASUS

### Структура файлов:
```
g-helper-main/app/
├── Lenovo/                      # Все Lenovo-специфичные классы
│   ├── LenovoWMI.cs            # Базовый WMI интерфейс
│   ├── LenovoPowerMode.cs      # Режимы производительности
│   ├── LenovoGPUMode.cs        # GPU режимы (Eco/Standard/Ultimate)
│   ├── LenovoBattery.cs        # Управление батареей
│   ├── LenovoFanControl.cs     # Вентиляторы и кривые
│   ├── LenovoRGB.cs            # Подсветка клавиатуры
│   └── LenovoGodMode.cs        # Custom Mode (Power Limits)
├── GodModeSettings.cs          # UI для Custom Mode
├── Settings.cs                 # Главное окно (адаптировано для Lenovo)
├── Fans.cs                     # Окно настроек вентиляторов/power
└── Program.cs                  # Инициализация
```

### WMI интерфейсы Lenovo:

**1. LENOVO_GAMEZONE_DATA** (основной интерфейс):
- `IsSupportGodMode` - проверка поддержки Custom режима
- `IsSupportSmartFan` - проверка поддержки управления вентиляторами
- `SetSmartFanMode(mode)` - установка режима производительности (1=Quiet, 2=Balance, 3=Performance, 255=GodMode)
- `GetSmartFanMode()` - чтение текущего режима
- `SetGPUMode(mode)` - установка GPU режима (1=Eco, 2=Standard, 3=Ultimate)
- `GetGPUMode()` - чтение GPU режима
- `GetFanTableData(mode)` - чтение кривых вентиляторов для режима
- `SetFanTableData(mode, data)` - запись кривых вентиляторов

**2. LENOVO_OTHER_METHOD** (современный интерфейс для power limits):
- `GetFeatureValue(CapabilityID)` - чтение значения параметра
- `SetFeatureValue(CapabilityID, value)` - запись значения параметра
- Используется для всех 10 параметров Custom Mode (CPU/GPU power limits)

**3. EnergyDrv** (драйвер для батареи и клавиатуры):
- Управление режимами зарядки (Conservation/Normal/Rapid)
- Управление подсветкой клавиатуры (Off/Low/High)
- Требует установленного драйвера Lenovo

## Что реализовано ✅

### Базовая функциональность:
1. **Определение Lenovo устройств** - автоматическое определение производителя
2. **Режимы производительности** - Silent/Balanced/Turbo/Custom (GodMode)
3. **GPU режимы** - Eco (iGPU only) / Standard (Hybrid) / Ultimate (dGPU only)
4. **Управление батареей** - Conservation Mode / Normal / Rapid Charge
5. **Подсветка клавиатуры** - Off/Low/High (белая подсветка, синхронизация с Fn+Space)

### Мониторинг:
6. **Чтение сенсоров** - CPU/GPU температура и скорость вентиляторов (RPM)

### Custom Mode (GodMode) - 10 параметров:
7. **CPU Power Limits (7 параметров):**
   - **SPL** (Sustained Power Limit) - длительная мощность CPU
   - **sPPT** (slow Package Power Tracking) - мощность CPU на 2 минуты
   - **fPPT** (fast Package Power Tracking) - пиковая мощность CPU
   - **CPU Cross Loading** - кросс-загрузка между ядрами
   - **PL1 Tau** - время удержания PL1
   - **APU sPPT** - мощность APU (интегрированная графика)
   - **CPU Temperature Limit** - максимальная температура CPU

8. **GPU Power Limits (3 параметра):**
   - **GPU Dynamic Boost** - динамическое перераспределение мощности между CPU/GPU
   - **GPU cTGP** (Configurable TGP) - настраиваемая мощность GPU
   - **GPU Temperature Limit** - максимальная температура GPU

9. **UI для Custom Mode:**
   - Окно `GodModeSettings.cs` размером 600x650
   - 2 секции: CPU Power Limits (7 слайдеров) и GPU Power Limits (3 слайдера)
   - Каждый слайдер показывает текущее значение в Watts или °C
   - Кнопка "Apply" - применить настройки к железу
   - Кнопка "Reset to Defaults" - сбросить на заводские значения
   - Автосохранение в AppConfig при Apply
   - Автозагрузка при открытии окна
   - Кнопка "Custom" в главном окне с фиолетовой подсветкой
   - Шестеренка на кнопке "Custom" для открытия настроек

### Fan Curves:
10. **Кривые вентиляторов:**
    - **Формат Lenovo:** 10 точек температуры (0-100°C), значения в RPM (0-6000)
    - **Формат ASUS:** 8 точек температуры (30-100°C), значения в процентах (0-100%)
    - **Чтение:** `GetFanTableData(mode)` возвращает массив из 10 точек для CPU и GPU вентиляторов
    - **Запись:** `SetFanTableData(mode, data)` применяет кривые к железу
    - **Конвертация:**
      - `ConvertLenovoToAsusCurve()` - Lenovo (10 точек RPM) → ASUS (8 точек %)
      - `ConvertAsusCurveToLenovo()` - ASUS (8 точек %) → Lenovo (10 точек RPM)
    - **Интеграция с Fans.cs:**
      - При открытии окна Fans для Lenovo устройств кривые читаются из WMI
      - При включении "Apply Custom Fan Curve" кривые применяются к железу
    - **Ограничение:** Работает только в Custom режиме (режим 3)
      - Silent/Balance/Turbo (режимы 0/1/2) контролируются BIOS
      - Согласно FAQ Legion Toolkit, в этих режимах кривые изменить нельзя

### Advanced настройки:
11. **Undervolting** - снижение напряжения через RyzenControl (AMD Ryzen)
12. **CPU Temperature Limit** - ограничение температуры CPU
13. **CPU Boost** - Aggressive/Enabled/Disabled
14. **Windows Power Mode** - Balanced/Best Performance/Best Power Efficiency

### GPU Overclock (NVIDIA):
15. **GPU разгон** - Core/Memory offset для NVIDIA GPU
    - Применяется на всех режимах 0/1/2 при смене режима
    - Использует NVAPI для установки offset
    - Clock Limit через `nvidia-smi` (требует admin права)
    - Проверка Eco mode для Lenovo через `AppConfig.Get("gpu_mode")`

### Extra функции (Lenovo):
16. **OverDrive** - кнопка в Settings рядом с частотой экрана
    - Текст меняется: "OD: ON" / "OD: OFF"
    - Через WMI: `IsSupportOD()`, `GetODStatus()`, `SetODStatus()`

17. **Windows Key Lock** - чекбокс в Extra
    - Блокировка клавиши Windows
    - Через WMI: `IsSupportDisableWinKey()`, `GetWinKeyStatus()`, `SetWinKeyStatus()`

18. **Battery Night Charge** - чекбокс в Extra
    - Ночная зарядка аккумулятора
    - Через EnergyDrv: `GetBatteryNightChargeState()`, `SetBatteryNightChargeState()`
    - IOCTL: `0x83102150`

19. **Always On USB** - комбобокс в Extra (3 опции)
    - Off / On When Sleeping / On Always
    - Через EnergyDrv: `GetAlwaysOnUSBState()`, `SetAlwaysOnUSBState()`
    - IOCTL: `0x831020E8`

## Что НЕ работает / Требует тестирования ⏳

### Требует тестирования:

1. **STAPM/SLOW/FAST Limits** (вкладка CPU в Fans окне):
   - STAPM Limit - Skin Temperature Aware Power Management
   - SLOW Limit - Sustained Power Limit
   - FAST Limit - Peak Power Limit
   - Слайдеры есть, применение через RyzenControl
   - Не тестировалось на Lenovo - может работать или не работать

2. **Auto Apply on Mode Change**:
   - Автоматическое применение Custom Mode настроек при переключении на режим 3
   - Код есть, но требует тестирования
   - Может быть проблема с синхронизацией

### Не реализовано (Extra функции из Legion Toolkit):

1. **Instant Boot** - мгновенное включение
   - Требует WMI методы: `IsSupportInstantBootAc()`, `GetInstantBootAcState()`, `SetInstantBootAcState()`
   - Требует Capability проверку

2. **Flip To Start** - включение при открытии крышки
   - Требует WMI методы: `IsSupportFlipToStart()`, `GetFlipToStartState()`, `SetFlipToStartState()`
   - Требует Capability проверку

3. **HDR** - управление HDR
   - Требует Windows Display API
   - Более сложная реализация

4. **Smart Fn Lock** - умная блокировка Fn
   - Требует WMI или EnergyDrv методы
   - Детали реализации неизвестны

5. **Disable Lenovo Hotkeys** - отключение горячих клавиш Lenovo
   - Требует WMI или EnergyDrv методы
   - Детали реализации неизвестны

### Не реализовано (другое):

1. **Fan Curves UI в Custom Mode Settings**:
   - Попытки добавить графики CPU/GPU Fan в окно GodModeSettings провалились
   - Проблемы с Windows Forms layout (Dock, AutoScroll, позиционирование)
   - Графики либо не отображались, либо перекрывали другие элементы
   - Требуется переработка подхода (возможно, отдельное окно или TabControl)
   - **Текущее решение:** Fan Curves редактируются в стандартном окне Fans (работает)

2. **RGB подсветка (расширенная)**:
   - Реализована только белая подсветка (Off/Low/High)
   - Нет поддержки цветной RGB подсветки
   - Нет поддержки эффектов (breathing, wave, и т.д.)

3. **Spectrum** - анализ производительности (из Legion Toolkit):
   - Графики CPU/GPU загрузки в реальном времени
   - Не портировано

4. **Dashboard** - детальная информация о системе (из Legion Toolkit):
   - Подробная информация о железе
   - Версии BIOS/драйверов
   - Не портировано

5. **Updates** - проверка обновлений BIOS/драйверов (из Legion Toolkit):
   - Автоматическая проверка обновлений
   - Не портировано

## Ключевые технические решения

### 1. Маппинг режимов производительности:

G-Helper использует внутренние номера режимов 0/1/2/3, которые маппятся на Lenovo WMI значения:

```
G-Helper Mode → Lenovo WMI Value → Описание:
0 (Balanced)  → 2 (Balance)      → Сбалансированный режим
1 (Turbo)     → 3 (Performance)  → Максимальная производительность
2 (Silent)    → 1 (Quiet)        → Тихий режим
3 (Custom)    → 255 (GodMode)    → Пользовательские настройки
```

**Важно:** При инициализации Lenovo устройства автоматически создается режим 3 (Custom) если его нет в конфиге.
Это делается в `Settings.cs` при первом запуске.

### 2. Power Limits через WMI:

Все 10 параметров Custom Mode читаются/пишутся через `LENOVO_OTHER_METHOD`:

```csharp
// Чтение значения
int value = GetFeatureValue(CapabilityID & 0xFFFF00FF);

// Запись значения
SetFeatureValue(CapabilityID & 0xFFFF00FF, value);

// CapabilityID для каждого параметра (из LenovoGodMode.cs):
CPULongTermPowerLimit    = 0x0102FF00  // SPL
CPUShortTermPowerLimit   = 0x0202FF00  // sPPT
CPUPL4PowerLimit         = 0x0302FF00  // fPPT
CrossLoadingPowerLimit   = 0x0402FF00  // CPU Cross Loading
CPUPL1Tau                = 0x0502FF00  // PL1 Tau
APUsPPTPowerLimit        = 0x0602FF00  // APU sPPT
CPUTemperatureLimit      = 0x0702FF00  // CPU Temp Limit
GPUPowerBoost            = 0x0802FF00  // GPU Dynamic Boost
GPUConfigurableTGP       = 0x0902FF00  // GPU cTGP
GPUTemperatureLimit      = 0x0A02FF00  // GPU Temp Limit
```

**Важно:** Маска `& 0xFFFF00FF` обязательна для корректной работы с WMI.
Значения хранятся в Watts (для power limits) или °C (для temperature limits).

### 3. Fan Curves конвертация:

**Проблема:** Lenovo использует 10-точечные кривые в RPM, ASUS использует 8-точечные кривые в процентах.
G-Helper изначально работал только с ASUS форматом.

**Решение:** Конвертация форматов в `LenovoFanControl.cs`:

```csharp
// Lenovo формат (из WMI):
struct FanTable {
    byte Temperature;  // 0-100°C
    ushort FanSpeed;   // 0-6000 RPM
}

// ASUS формат (в G-Helper):
byte[] curve = new byte[8];  // 8 точек, каждая 0-100%

// Конвертация Lenovo → ASUS:
byte[] ConvertLenovoToAsusCurve(FanTableData data)
- Берет 10 точек Lenovo (RPM)
- Интерполирует в 8 точек ASUS (проценты)
- Конвертирует RPM в проценты (RPM / 60)

// Конвертация ASUS → Lenovo:
FanTableData ConvertAsusCurveToLenovo(byte[] asusCurve, int sensorId)
- Берет 8 точек ASUS (проценты)
- Интерполирует в 10 точек Lenovo (RPM)
- Конвертирует проценты в RPM (percent * 60)
```

**Интеграция с Fans.cs:**
- `LoadProfile()` - при открытии окна для Lenovo читает кривые из WMI и конвертирует в ASUS формат
- `SaveProfile()` - при сохранении конвертирует ASUS формат обратно в Lenovo и пишет в WMI

### 4. Автосоздание Custom режима:

При первом запуске G-Helper на Lenovo устройстве автоматически создается режим 3 (Custom):

```csharp
// В Settings.cs, метод InitLenovo():
if (AppConfig.ContainsModel("custom_fan"))
{
    // Режим уже существует
}
else
{
    // Создаем новый режим 3 (Custom)
    AppConfig.Set("custom_fan", 1);
    AppConfig.Set("performance_mode", 3);  // Переключаемся на Custom
}
```

**Зачем это нужно:**
- ASUS устройства имеют режимы 0/1/2 (Balanced/Turbo/Silent)
- Lenovo добавляет режим 3 (Custom/GodMode) для расширенных настроек
- Автосоздание гарантирует что Custom режим доступен сразу после установки

## Известные проблемы и ограничения

### 1. WMI требует прав администратора
Некоторые WMI операции (особенно запись power limits) требуют прав администратора.
G-Helper должен запускаться с правами администратора для полной функциональности.

### 2. EnergyDrv драйвер
Для работы батареи и подсветки клавиатуры требуется установленный драйвер `EnergyDrv` от Lenovo.
Обычно устанавливается вместе с Lenovo Vantage.

### 3. Lenovo Vantage конфликт
Lenovo Vantage может конфликтовать с G-Helper (оба пытаются управлять одними и теми же WMI интерфейсами).
Рекомендуется закрывать Lenovo Vantage перед использованием G-Helper.
В коде есть `ProcessHelper.KillByName("LenovoVantage")` для автоматического закрытия.

### 4. Fan Curves только в Custom режиме
Согласно FAQ Legion Toolkit, кривые вентиляторов в режимах Silent/Balance/Turbo контролируются BIOS и не могут быть изменены.
Пользовательские кривые работают только в Custom (GodMode) режиме.

### 5. Layout проблемы с Fan Curves UI
Попытки добавить графики Fan Curves в окно GodModeSettings (Custom Mode Settings) не увенчались успехом:
- Проблемы с Windows Forms layout (Dock, AutoScroll)
- Графики не отображались или перекрывали другие элементы
- Требуется переработка подхода (возможно, отдельное окно или другой layout)

### 6. Тестирование на одной модели
Весь код тестировался только на одной модели Lenovo Legion (модель пользователя).
Требуется тестирование на других моделях для проверки совместимости.

## Статус проекта (2025-11-17)

**Версия:** 1.3
**Статус:** Основная функциональность + GPU OC + 4 Extra функции реализованы и работают
**Протестировано на:** Lenovo Legion (модель пользователя)
**Требует:** Тестирование на других моделях Legion

**Последние изменения (2025-11-17):**
- ✅ GPU Overclock (Core/Memory offset) - работает на всех режимах
- ✅ OverDrive кнопка в Settings - текстовая индикация "OD: ON/OFF"
- ✅ Windows Key Lock в Extra - чекбокс, работает через WMI
- ✅ Battery Night Charge в Extra - чекбокс, работает через EnergyDrv
- ✅ Always On USB в Extra - комбобокс (3 опции), работает через EnergyDrv
- ⏳ Остальные Extra функции (Instant Boot, Flip To Start, HDR, Smart Fn Lock, Disable Hotkeys) - не реализованы

## Для новой нейросети: С чего начать?

### 1. Изучи логи
**Путь:** `C:\Users\<USERNAME>\AppData\Local\GHelper\log.txt`

Логи содержат всю информацию о работе приложения:
- WMI вызовы и их результаты
- Ошибки и исключения
- Текущие значения параметров
- Переключения режимов

**Важно:** Пользователь часто просит "закинуть лог в наше старое место" - это означает скопировать содержимое `log.txt` в `g-helper-main/log.txt` в workspace.

### 2. Основные файлы для изучения

**Lenovo-специфичные классы:**
- `Lenovo/LenovoWMI.cs` - базовый WMI интерфейс, все WMI вызовы идут через него
- `Lenovo/LenovoGodMode.cs` - Custom Mode, 10 параметров power limits
- `Lenovo/LenovoFanControl.cs` - чтение/запись fan curves, конвертация форматов
- `Lenovo/LenovoPowerMode.cs` - переключение режимов производительности
- `Lenovo/LenovoGPUMode.cs` - переключение GPU режимов
- `Lenovo/LenovoBattery.cs` - управление батареей
- `Lenovo/LenovoRGB.cs` - подсветка клавиатуры

**UI файлы:**
- `GodModeSettings.cs` - окно настроек Custom Mode (600x650, 10 слайдеров)
- `Settings.cs` - главное окно, адаптировано для Lenovo (кнопки режимов, GPU, батарея)
- `Fans.cs` - окно настроек вентиляторов и power limits

**Общие файлы:**
- `Program.cs` - инициализация, определение производителя
- `AppConfig.cs` - сохранение/загрузка настроек

### 3. WMI отладка

Для проверки доступных WMI методов используй:
- **WMI Explorer** - GUI инструмент для просмотра WMI классов
- **PowerShell:**
  ```powershell
  Get-WmiObject -Namespace root\WMI -Class LENOVO_GAMEZONE_DATA
  Get-WmiObject -Namespace root\WMI -Class LENOVO_OTHER_METHOD
  ```

### 4. Тестирование

**Всегда проверяй логи после изменений:**
1. Запусти приложение
2. Выполни действие (переключи режим, измени power limit, и т.д.)
3. Закрой приложение
4. Проверь `log.txt` на наличие ошибок и корректность WMI вызовов

**Пользователь часто просит:**
- "Закинь лог в наше старое место" - скопируй `log.txt` в `g-helper-main/log.txt`
- "Собери" - запусти сборку проекта
- "Проверь" - проверь работу после сборки

### 5. Сборка проекта

**Debug сборка (быстрая):**
```powershell
dotnet build g-helper-main/app/GHelper.csproj -c Release
```
Результат: `g-helper-main/app/bin/x64/Release/net8.0-windows/GHelper.exe`

**Release сборка (полная, single-file):**
```powershell
dotnet publish g-helper-main/app/GHelper.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
Результат: `g-helper-main/app/bin/x64/Release/net8.0-windows/win-x64/publish/Lhelper.exe`

**Важно:** Пользователь переименовал выходной файл в `Lhelper.exe` (вместо `GHelper.exe`).

### 6. Стиль работы с пользователем

**Пользователь предпочитает:**
- Краткие ответы ("Готово", "Собрано", "Проверь")
- Не повторять очевидное
- Не расписывать детали если не просят
- Экономить токены

**Пользователь НЕ любит:**
- Длинные объяснения без запроса
- Повторение того что он уже знает
- Лишние слова и "воду"

**Если что-то не получается:**
- Признай проблему честно
- Не пытайся делать одно и то же снова
- Спроси у пользователя как лучше поступить

## Полезные документы

- **`LENOVO_INTEGRATION_STATUS.md`** - детальный статус всех функций с техническими деталями
- **`LENOVO_INTEGRATION_TESTING.md`** - результаты тестирования на реальном железе
- **`LENOVO_INTEGRATION_SUMMARY.md`** (этот файл) - краткая сводка для быстрого понимания проекта
- **`ErrorsHelp.txt`** - помощь по ошибкам и их решению

## Важные моменты для понимания

### Почему Custom Mode называется GodMode?
В Legion Toolkit режим с пользовательскими настройками называется "GodMode" (WMI значение 255).
В G-Helper это режим 3 (Custom), но внутри кода часто используется название "GodMode".

### Почему Fan Curves работают только в Custom?
Согласно FAQ Legion Toolkit, в режимах Silent/Balance/Turbo кривые вентиляторов жестко контролируются BIOS.
Изменить их программно нельзя - это ограничение железа, а не софта.

### Почему не получилось добавить Fan Curves UI в GodModeSettings?
Windows Forms layout оказался сложнее чем ожидалось:
- Использование Dock = DockStyle.Top для вертикального стека панелей
- Добавление AutoScroll для прокрутки
- Попытки добавить графики (System.Windows.Forms.DataVisualization.Charting)
- Графики либо не отображались (размер 0x0), либо перекрывали другие элементы
- Попытки исправить через абсолютное позиционирование тоже не сработали
- Пользователь устал от попыток и попросил откатить все изменения

**Текущее решение:** Fan Curves редактируются в стандартном окне Fans (кнопка "Fans and Power" в главном окне).
Это работает нормально, просто не так удобно как могло бы быть в Custom Mode Settings.

### Почему выходной файл называется Lhelper.exe?
Пользователь переименовал выходной файл из `GHelper.exe` в `Lhelper.exe`.
Причина не уточнялась, но это важно знать при сборке и тестировании.

### Как работает определение производителя?
При запуске `Program.cs` проверяет `SystemInfo.Manufacturer`:
- Если содержит "LENOVO" - инициализируется Lenovo WMI
- Если содержит "ASUS" - инициализируется ASUS ACPI
- Вся дальнейшая логика адаптируется под производителя

### Зачем нужна конвертация Fan Curves?
G-Helper изначально работал только с ASUS форматом (8 точек, проценты).
Весь UI (графики, слайдеры) рассчитан на этот формат.
Вместо переписывания всего UI, проще конвертировать Lenovo формат (10 точек, RPM) в ASUS формат при чтении,
и обратно при записи. Это позволяет использовать существующий UI без изменений.

