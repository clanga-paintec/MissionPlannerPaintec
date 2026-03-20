# GridFlight - Arquitectura del Sistema de Plugins

## 1. Arquitectura General

GridFlight extiende MissionPlanner mediante un sistema de **15 plugins** que se compilan dentro del ensamblado principal (`MissionPlanner.exe`). Los plugins se descubren automaticamente por reflexion en `PluginLoader.InitPlugin("self")` y siguen el ciclo de vida estandar: `Init()` > `Loaded()` > `Loop()` > `Exit()`.

Todas las personalizaciones siguen el **principio Open/Closed**: se extiende la funcionalidad sin modificar el codigo fuente de MissionPlanner, salvo 1 archivo con cambios minimos marcados con bloques `// GRIDFLIGHT CHANGES` (anteriormente eran 3, pero los cambios de FlightData.cs y FlightData.Designer.cs fueron migrados al plugin `FlightModePlugin`).

### 1.1 Sistema de Perfiles

GridFlight opera con dos perfiles mutuamente excluyentes:

| Perfil | Descripcion | Plugins activos |
|--------|-------------|-----------------|
| **Piloto** | Experiencia GridFlight completa: tema ambar, menus simplificados, atajos, configuraciones favoritas, checklist pre-vuelo, reporte de mision | Todos (14) |
| **Mecanico** | MissionPlanner completo con branding GridFlight: tema ambar, test de motores, gestion de parametros, registro de mantenimiento, acceso total a hardware y configuracion | IconOverride, Branding, ProfileSelector, ModernTheme, FavoriteConfigs, MotorTestShortcut, FlightMode, PreFlightChecklist, MaintenanceLog, MissionReport (10) |

El perfil se persiste en `config.xml` bajo la clave `GridFlight_Profile` (valores: `"Pilot"` / `"Mechanic"`). Los cambios de perfil requieren reinicio de la aplicacion porque `Init()` es el unico punto de control en el ciclo de vida de plugins.

**Clase de gestion:** `GridFlightProfile` (estatica, `GridFlight/GridFlightProfile.cs`)
- `Current`: Lee el perfil de Settings, default `"Pilot"`
- `IsPilot`: `!IsMechanic` (cualquier valor que no sea explicitamente "Mechanic" se trata como Pilot)
- `IsMechanic`: `Current == "Mechanic"`
- `IsFirstLaunch`: `true` si la clave no existe en config.xml
- `ConfigsDirectory`: `GridFlight/configs/` (creado automaticamente)

---

## 2. Inventario de Plugins

### 2.1 Plugins de Identidad Visual (ambos perfiles)

#### `BrandingPlugin.cs`
- **Proposito:** Aplica la identidad visual GridFlight al arrancar
- **Fase:** `Loaded()` (ejecucion unica, sin loop)
- **Acciones:**
  - Escala `logo2.png` proporcionalmente y lo aplica a `MenuArduPilot`
  - Redirige el click del logo de ardupilot.org a `gridflight.tech` (via reflexion sobre `EventHandlerList`)
  - Carga y aplica `Gridflight-Icon.ico` como icono de ventana

#### `IconOverridePlugin.cs`
- **Proposito:** Reemplaza los iconos del toolbar con version ambar
- **Fase:** `Loop()` a 0.2 Hz (disparo unico con flag `_applied`)
- **Mecanismo:** Asigna `MainV2.displayicons` a `GridFlightMenuIcons`, que carga PNGs desde `GridFlight/assets/` con fallback a recursos embebidos de MissionPlanner
- **Iconos reemplazados:** FlightData, FlightPlanner, InitialSetup, ConfigTune, Simulation, Terminal, Help, Connect, Disconnect

### 2.2 Plugins de Tema y Simplificacion

#### `ModernThemePlugin.cs` (ambos perfiles)
- **Proposito:** Tema moderno con paleta ambar oscura
- **Fase:** `Loaded()` (ejecucion unica)
- **Paleta principal:**
  - Fondo: `#181818`, Superficie: `#212121` / `#2D2D2D`
  - Acento primario: `#FFC107` (ambar), Oscuro: `#D39E00`
  - Texto: `#F5F5F5` (primario), `#AAAAAA` (secundario)
- **Tecnica:** Sobrescribe campos estaticos de `ThemeManager` + recorrido recursivo de controles aplicando `FlatStyle`, renderers custom y fuente Segoe UI 9pt
- **Fuente iconos:** Material Symbols Rounded (TTF cargado via `PrivateFontCollection`)
- **Respeta:** `PreventThemingAttribute`, `Tag="custom"`

#### `HideOptionalHardwarePlugin.cs` (solo Piloto)
- **Proposito:** Oculta items irrelevantes de Optional Hardware (Setup)
- **Fase:** `Init()` para flags + `Loop()` a 1 Hz para CubeID
- **Flags desactivados (18):** displayRTKInject, displaySikRadio, displayGPSOrder, displayBattMonitor, displayCAN, displayJoystick, displayCompassMotorCalib, displayRangeFinder, displayAirSpeed, displayPx4Flow, displayOpticalFlow, displayOsd, displayCameraGimbal, displayAntennaTracker, displayBluetooth, displayParachute, displayEsp, displayFFTSetup
- **Caso especial:** CubeID Update no tiene flag en `DisplayView`; se oculta via `BackstageViewPage.Show = false` en cada visita al tab Setup (InitialSetup se recrea cada vez)
- **Preserva:** Motor Test (unico item relevante para operacion)

#### `HideSetupMenuItemsPlugin.cs` (solo Piloto)
- **Proposito:** Oculta items avanzados de Mandatory Hardware
- **Fase:** `Init()` (ejecucion unica, sin loop)
- **Flags desactivados (3):** displayFailSafe, displayHWIDs, displayADSB

### 2.3 Plugins de Atajos Operativos

#### `WriteVerifyPlugin.cs` (solo Piloto)
- **Proposito:** Boton "Write and Verify" en FlightPlanner
- **Fase:** `Loaded()` (ejecucion unica)
- **Ubicacion:** `FlightPlanner.panel5` en posicion (3, 90), tamano 115x23
- **Accion:** Ejecuta `BUT_write_Click()` seguido de `BUT_read_Click()` para escribir y verificar la mision

#### `MotorTestShortcut.cs` (ambos perfiles, comportamiento diferenciado)
- **Proposito:** Acceso rapido a Motor Test desde el toolbar
- **Fase:** `Loaded()` + `Loop()` a 2 Hz
- **Visibilidad Piloto:** Solo cuando SITL esta activo (`SITL.SITLSEND.Client.Connected`)
- **Visibilidad Mecanico:** Cuando hay vehiculo conectado (`MainV2.comPort.BaseStream.IsOpen`)
- **Accion:** Navega a Setup > activa la pagina "Motor Test" del BackstageView
- **Icono:** `engine.png` desde `GridFlight/assets/`

#### `ElevationGraphShortcut.cs` (solo Piloto)
- **Namespace:** `ElevationGraphShortcut` (diferente al resto)
- **Proposito:** Acceso rapido al perfil de elevacion
- **Fase:** `Loaded()` + `Loop()` a 2 Hz
- **Visibilidad:** Solo cuando hay waypoints en el plan (`Commands.Rows.Count > 1`)
- **Icono:** Renderizado dinamico con SkiaSharp desde SVG path data

### 2.4 Plugins del Sistema de Perfiles

#### `ProfileSelectorPlugin.cs`
- **Proposito:** Seleccion y cambio de perfil Piloto/Mecanico
- **Carga en:** Ambos perfiles (Init() siempre retorna true)
- **Primer arranque:** Dialogo modal con dos opciones (PILOTO / MECANICO)
- **Toolbar:** `ToolStripDropDownButton` mostrando "PILOTO" o "MECANICO" con dropdown para cambiar
- **Cambio de perfil:** Persiste en config.xml + solicita reinicio via `Application.Restart()`
- **Contrasena Mecanico:** El perfil Mecanico requiere contrasena (default configurable). Se valida al seleccionar el perfil y se puede cambiar desde el dropdown. Persiste en `Settings.Instance["GridFlight_MechanicPassword"]`.

#### `FavoriteConfigsPlugin.cs` (ambos perfiles)
- **Proposito:** Gestor de configuraciones favoritas de parametros de dron
- **Fase:** `Loaded()` (ejecucion unica)
- **Icono:** Estrella ambar de 5 puntas renderizada con SkiaSharp
- **Almacenamiento:** Archivos `.param` en `GridFlight/configs/`
- **Operaciones:**
  - **Guardar:** Lee `MainV2.comPort.MAV.param` > pide nombre > `ParamFile.SaveParamFile()`
  - **Cargar:** `ParamFile.loadParamFile()` > `ParamCompare(null, current, file)` para comparar y aplicar selectivamente via MAVLink
  - **Eliminar:** Borra archivo `.param` con confirmacion
  - **Importar:** Copia `.param` externo al directorio de configuraciones

#### `GridFlightProfile.cs`
- **Tipo:** Clase estatica de utilidad (no es un plugin)
- **Proposito:** Lectura/escritura centralizada del perfil activo
- **Clave Settings:** `GridFlight_Profile`
- **Defensa:** Try-catch en `Current` con default a "Pilot"; `IsPilot = !IsMechanic` para que cualquier valor inesperado se trate como Piloto

### 2.5 Plugins de Seguridad (ambos perfiles)

#### `PreFlightChecklistPlugin.cs` (ambos perfiles)
- **Proposito:** Habilita el tab PreFlight en FlightData y proporciona checklist default para drones
- **Fase:** `Init()` (flags de visibilidad) + `Loaded()` (boton toolbar)
- **Icono:** Clipboard ambar renderizado con SkiaSharp
- **Checklist:** 8 checks automaticos (bateria, GPS, prearm, failsafe, enlace) + 6 manuales
- **Infraestructura:** Reutiliza CheckListControl + CheckListItem de MissionPlanner

#### `MaintenanceLogPlugin.cs` (solo Mecanico)
- **Proposito:** Registro local de mantenimientos del dron
- **Fase:** `Loaded()` (ejecucion unica)
- **Icono:** Llave ambar renderizada con SkiaSharp
- **Almacenamiento:** `GridFlight/configs/maintenanceLog.json`
- **Operaciones:** Añadir, eliminar, visualizar historial

#### `MissionReportPlugin.cs` (ambos perfiles)
- **Proposito:** Genera reportes HTML post-vuelo con estadisticas de la mision
- **Fase:** `Loaded()` + `Loop()` a 1 Hz (rastreo de datos en vuelo)
- **Icono:** Documento ambar renderizado con SkiaSharp
- **Datos rastreados:** Altitud max, velocidad max, bateria, modos usados, distancia, duracion
- **Output:** HTML con tema GridFlight en `GridFlight/configs/reports/`

### 2.7 Plugins de Control de Vuelo (todos los perfiles)

#### `FlightModePlugin.cs`
- **Proposito:** Control de seleccion de modos de vuelo en tab Actions
- **Fase:** `Loaded()` (ejecucion unica)
- **Acciones:**
  - Reordena el grid del tab Actions (posiciones upstream → layout GridFlight) usando Remove + Add atomico
  - Filtra CMB_modes para excluir modos peligrosos (lista `HiddenModes` configurable)
  - Para mecanicos: crea controles adicionales (CMB_modes_full_list + BUT_setmode_full_list) con lista completa sin filtrar
- **Modos ocultos por defecto:** Acro, FBWA, FBWB, AVOID_ADSB, QAcro, Thermal, Loiter To QLand, AUTOLAND, INITIALISING
- **Nota:** Migrado desde cambios inline en FlightData.cs y FlightData.Designer.cs

---

## 3. Modificaciones a Archivos Originales de MissionPlanner

Actualmente solo **1 archivo** del codigo fuente original esta modificado. Todos los cambios estan marcados con bloques de comentarios `// GRIDFLIGHT CHANGES` / `// END GRIDFLIGHT CHANGES`.

### 3.1 `Program.cs` (lineas 220-224)
```csharp
// Carga logos GridFlight si existen en GridFlight/assets/
if (File.Exists(...Path.Combine("GridFlight", "assets", "missionplannergrid.png")))
    Logo = new Bitmap(...);
if (File.Exists(...Path.Combine("GridFlight", "assets", "logo2.png")))
    Logo2 = new Bitmap(...);
```
**Impacto:** Sustituye los logos de ArduPilot por los de GridFlight al arrancar.

### 3.2 ~~`GCSViews/FlightData.cs`~~ → MIGRADO a `FlightModePlugin.cs`
> Los cambios que existian en FlightData.cs (filtrado de modos, lista completa, handlers de click)
> fueron extraidos al plugin `FlightModePlugin.cs` para mantener el codigo fuente limpio.
> FlightData.cs ahora es 100% upstream.

### 3.3 ~~`GCSViews/FlightData.Designer.cs`~~ → MIGRADO a `FlightModePlugin.cs`
> Los controles `CMB_modes_full_list` y `BUT_setmode_full_list` se crean ahora en runtime
> desde el plugin, solo para el perfil Mecanico. FlightData.Designer.cs ahora es 100% upstream.

---

## 4. Configuracion de Build

### 4.1 `Directory.Build.targets`
- **Icono de aplicacion:** `Gridflight-Icon.ico` para `MissionPlanner.csproj`
- **Copia de assets al output:** logo2.png, iconos (dark_*/light_*), engine.png, fuente Material Symbols, Gridflight-Icon
- **Target custom:** Copia `Gridflight-Icon.png` como `icon.png` al bin para deteccion por splash screen

### 4.2 Inclusion automatica
Los archivos `.cs` en `GridFlight/` se incluyen automaticamente por el globbing del SDK-style project. No requieren entrada explicita en `.csproj`.

---

## 5. Assets (`GridFlight/assets/`)

| Categoria | Archivos | Descripcion |
|-----------|----------|-------------|
| Branding | `Gridflight-Icon.ico`, `Gridflight-Icon.png`, `logo2.png`, `missionplannergrid.png` | Iconos y logos de la aplicacion |
| Toolbar (dark) | `dark_flightdata_icon.png`, `dark_flightplan_icon.png`, etc. (8) | Iconos ambar para tema oscuro |
| Toolbar (light) | `light_flightdata_icon.png`, `light_flightplan_icon.png`, etc. (7) | Iconos ambar para tema claro |
| Funcionales | `engine.png`, `ElevationGraphIcon.svg` | Iconos de plugins operativos |
| Fuente | `MaterialSymbolsRounded-VariableFont.ttf` | Fuente de iconos Material Design |
| Frames | 80+ archivos PNG/JPG | Iconos de vehiculos recoloreados a paleta ambar |
| Herramientas | `green_to_amber/run.bat`, `recolor_green_to_amber.py`, `environment.yml` | Script de recoloreo automatico |

---

## 6. Flujo de Inicio de la Aplicacion

```
1. Program.Main()
   └─ Carga logos GridFlight (Program.cs:220-224)

2. MainV2 constructor
   └─ Settings.Instance.Load()  ← config.xml con GridFlight_Profile

3. MainV2.LoadAll()
   ├─ PluginLoader.DisabledPluginNames ← desde Settings
   └─ PluginLoader.LoadAll()
       └─ InitPlugin("self")  ← descubre los 14 plugins GridFlight
           ├─ GridFlightProfile.IsPilot? ← lee Settings
           ├─ [Pilot] Todos los plugins pasan Init()
           └─ [Mechanic] Branding, Icons, ProfileSelector, ModernTheme, FavoriteConfigs, MotorTest, FlightMode, PreFlightChecklist, MaintenanceLog, MissionReport pasan Init()

4. PluginInit()  ← llama Loaded() en cada plugin aceptado
   ├─ ProfileSelectorPlugin.Loaded()
   │   ├─ IsFirstLaunch? → Dialogo de seleccion (modal)
   │   └─ Añade dropdown de perfil al toolbar
   ├─ ModernThemePlugin.Loaded() → Aplica tema ambar [ambos perfiles]
   ├─ BrandingPlugin.Loaded() → Logo + icono + URL
   ├─ WriteVerifyPlugin.Loaded() → Boton Write & Verify [solo Pilot]
   ├─ MotorTestShortcut.Loaded() → Boton Motor Test [ambos perfiles]
   ├─ ElevationGraphShortcut.Loaded() → Boton elevacion [solo Pilot]
   ├─ FavoriteConfigsPlugin.Loaded() → Boton estrella [ambos perfiles]
   ├─ FlightModePlugin.Loaded() → Reordena grid + filtro modos [todos]
   ├─ PreFlightChecklistPlugin.Loaded() → Despliega checklist + boton clipboard [ambos perfiles]
   ├─ MaintenanceLogPlugin.Loaded() → Boton llave para registro mantenimiento [solo Mechanic]
   └─ MissionReportPlugin.Loaded() → Boton documento para reportes post-vuelo [ambos perfiles]

5. Loop de plugins (hilo de fondo, MainV2.cs:2497-2537)
   ├─ IconOverridePlugin.Loop() → Aplica iconos (0.2 Hz, disparo unico)
   ├─ HideOptionalHardwarePlugin.Loop() → Oculta CubeID (1 Hz)
   ├─ MotorTestShortcut.Loop() → Visibilidad SITL (2 Hz)
   ├─ ElevationGraphShortcut.Loop() → Visibilidad waypoints (2 Hz)
   └─ MissionReportPlugin.Loop() → Rastreo de stats de vuelo (1 Hz)
```

---

## 7. APIs de MissionPlanner Reutilizadas

| API | Ubicacion | Uso en GridFlight |
|-----|-----------|-------------------|
| `Plugin` (clase base) | `Plugin/Plugin.cs` | Base de todos los plugins |
| `PluginHost` | `Plugin/Plugin.cs:74-255` | Acceso a MainForm, Settings, comPort |
| `Settings.Instance` | `ExtLibs/Utilities/Settings.cs` | Persistencia de perfil y configuracion |
| `ThemeManager` | `ExtLibs/Utilities/ThemeManager.cs` | Override de colores del tema |
| `DisplayView` | `ExtLibs/Utilities/DisplayView.cs` | Flags de visibilidad de menus |
| `ParamFile` | `ExtLibs/Utilities/ParamFile.cs` | Lectura/escritura de archivos .param |
| `ParamCompare` | `Controls/paramcompare.cs` | Comparacion y aplicacion selectiva de params |
| `InputBox` | `ExtLibs/Controls/InputBox.cs` | Dialogos de entrada de texto |
| `BackstageView` | `ExtLibs/Controls/BackstageView/` | Manipulacion de paginas de Setup |
| `MainV2.View` | `MainV2.cs` | Navegacion entre pantallas |
| `MAVLinkParamList` | `ExtLibs/Mavlink/MAVLinkParamList.cs` | Parametros del vehiculo |

---

## 8. Estructura de Directorios

```
GridFlight/
├── assets/
│   ├── dark_*_icon.png          (8 iconos toolbar oscuros)
│   ├── light_*_icon.png         (7 iconos toolbar claros)
│   ├── Gridflight-Icon.ico      (icono aplicacion)
│   ├── Gridflight-Icon.png      (icono ventana)
│   ├── logo2.png                (logo toolbar)
│   ├── missionplannergrid.png   (logo splash)
│   ├── engine.png               (icono motor test)
│   ├── ElevationGraphIcon.svg   (icono elevacion)
│   ├── MaterialSymbolsRounded-VariableFont.ttf
│   ├── [80+ frame PNGs/JPGs]   (vehiculos recoloreados)
│   └── green_to_amber/
│       ├── run.bat
│       ├── recolor_green_to_amber.py
│       └── environment.yml
├── configs/                     (creado en runtime)
│   ├── *.param                  (configuraciones guardadas)
│   ├── checklistGridFlight.xml  (checklist default para drones)
│   ├── maintenanceLog.json      (historial de mantenimientos - creado en runtime)
│   └── reports/                 (reportes HTML post-vuelo - creado en runtime)
├── BrandingPlugin.cs
├── ElevationGraphShortcut.cs
├── FavoriteConfigsPlugin.cs
├── GridFlightProfile.cs
├── HideOptionalHardwarePlugin.cs
├── HideSetupMenuItemsPlugin.cs
├── IconOverridePlugin.cs
├── ModernThemePlugin.cs
├── MotorTestShortcut.cs
├── ProfileSelectorPlugin.cs
├── FlightModePlugin.cs
├── WriteVerifyPlugin.cs
├── PreFlightChecklistPlugin.cs
├── MaintenanceLogPlugin.cs
├── MissionReportPlugin.cs
└── Docs/
    ├── ARCHITECTURE.md          (este documento)
    ├── BrandingPlugin.md
    ├── ElevationGraphShortcut.md
    ├── FavoriteConfigsPlugin.md
    ├── FlightModePlugin.md
    ├── GridFlightProfile.md
    ├── HideOptionalHardwarePlugin.md
    ├── HideSetupMenuItemsPlugin.md
    ├── IconOverridePlugin.md
    ├── ModernThemePlugin.md
    ├── MotorTestShortcut.md
    ├── ProfileSelectorPlugin.md
    ├── WriteVerifyPlugin.md
    ├── PreFlightChecklistPlugin.md
    ├── MaintenanceLogPlugin.md
    └── MissionReportPlugin.md
```
