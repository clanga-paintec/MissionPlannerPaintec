# FlightModePlugin

**Archivo:** `GridFlight/FlightModePlugin.cs`
**Perfil:** Todos (Piloto y Mecanico)
**Fase:** `Loaded()` (ejecucion unica)

## Que hace

Controla la seleccion de modos de vuelo en el tab **Actions** de FlightData. Realiza tres tareas:


Reorganiza los controles del `tableLayoutPanel1` del layout upstream al layout GridFlight. Los controles de columna 4 (modifyandSet) se mueven a fila 4, y los botones de fila 4 se mueven a columna 4. Usa `Remove` + `Add` atomico dentro de `SuspendLayout` para evitar superposiciones.

### 2. Filtro de modos de vuelo

Filtra el ComboBox `CMB_modes` para ocultar modos deseados dependiendo del perfil que haya seleccionado. El filtro se aplica en el evento `Click` (despues del handler original que carga los modos).

**Modos ocultos por defecto:**

```
Acro, FBWA, FBWB, AVOID_ADSB, QAcro, Thermal,
Loiter To QLand, AUTOLAND, INITIALISING
```

> Para modificar los modos visibles, editar el array `HiddenModes` al inicio de la clase.
> Los nombres deben coincidir exactamente con los valores de `ArduPilot.Common.getModesList()`.

## Historial

Este plugin fue creado migrando cambios que existian directamente en:
- `GCSViews/FlightData.cs` (handlers de click y filtrado)
- `GCSViews/FlightData.Designer.cs` (declaracion de controles)

La migracion a plugin permite mantener ambos archivos 100% upstream.
