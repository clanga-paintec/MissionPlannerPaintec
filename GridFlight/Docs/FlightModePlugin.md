# FlightModePlugin

**Archivo:** `GridFlight/FlightModePlugin.cs`
**Perfil:** Todos (Piloto y Mecanico)
**Fase:** `Loaded()` (ejecucion unica)

## Que hace

Controla la seleccion de modos de vuelo en el tab **Actions** de FlightData. Realiza tres tareas:

### 1. Reordenacion del grid

Reorganiza los controles del `tableLayoutPanel1` del layout upstream al layout GridFlight. Los controles de columna 4 (modifyandSet) se mueven a fila 4, y los botones de fila 4 se mueven a columna 4. Usa `Remove` + `Add` atomico dentro de `SuspendLayout` para evitar superposiciones.

### 2. Filtro de modos de vuelo

Filtra el ComboBox `CMB_modes` para ocultar modos peligrosos o irrelevantes. El filtro se aplica en el evento `Click` (despues del handler original que carga los modos).

**Modos ocultos por defecto:**

```
Acro, FBWA, FBWB, AVOID_ADSB, QAcro, Thermal,
Loiter To QLand, AUTOLAND, INITIALISING
```

> Para modificar los modos visibles, editar el array `HiddenModes` al inicio de la clase.
> Los nombres deben coincidir exactamente con los valores de `ArduPilot.Common.getModesList()`.

### 3. Lista completa (solo Mecanico)

Para el perfil Mecanico, crea controles adicionales en la fila 4 del grid:
- `CMB_modes_full_list` (posicion 0,4): ComboBox con todos los modos sin filtrar
- `BUT_setmode_full_list` (posicion 1,4): Boton "Set Mode" con confirmacion de failsafe

Estos controles **no se muestran** para el perfil Piloto.

## Historial

Este plugin fue creado migrando cambios que existian directamente en:
- `GCSViews/FlightData.cs` (handlers de click y filtrado)
- `GCSViews/FlightData.Designer.cs` (declaracion de controles)

La migracion a plugin permite mantener ambos archivos 100% upstream.
