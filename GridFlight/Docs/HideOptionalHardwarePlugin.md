# HideOptionalHardwarePlugin

**Archivo:** `GridFlight/HideOptionalHardwarePlugin.cs`
**Perfil:** Solo Piloto
**Fase:** `Init()` para flags + `Loop()` a 1 Hz para CubeID

## Que hace

Oculta items irrelevantes del menu **Optional Hardware** (Setup) para simplificar la interfaz del piloto.

### Flags desactivados (18)

displayRTKInject, displaySikRadio, displayGPSOrder, displayBattMonitor, displayCAN, displayJoystick, displayCompassMotorCalib, displayRangeFinder, displayAirSpeed, displayPx4Flow, displayOpticalFlow, displayOsd, displayCameraGimbal, displayAntennaTracker, displayBluetooth, displayParachute, displayEsp, displayFFTSetup

### Caso especial: CubeID Update

CubeID Update no tiene flag en `DisplayView`, por lo que se oculta via `BackstageViewPage.Show = false` en cada visita al tab Setup (porque InitialSetup se recrea cada vez que se visita).

### Preserva

- **Motor Test:** Unico item de Optional Hardware que permanece visible (necesario para operacion)
