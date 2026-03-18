# IconOverridePlugin

**Archivo:** `GridFlight/IconOverridePlugin.cs`
**Perfil:** Todos (Piloto y Mecanico)
**Fase:** `Loop()` a 0.2 Hz (disparo unico con flag `_applied`)

## Que hace

Reemplaza los iconos del toolbar principal con versiones ambar personalizadas de GridFlight.

### Mecanismo

Asigna `MainV2.displayicons` a `GridFlightMenuIcons`, que carga PNGs desde `GridFlight/assets/` con fallback a los recursos embebidos originales de MissionPlanner.

### Iconos reemplazados

| Icono | Archivo (dark) | Archivo (light) |
|-------|----------------|-----------------|
| Flight Data | `dark_flightdata_icon.png` | `light_flightdata_icon.png` |
| Flight Planner | `dark_flightplan_icon.png` | `light_flightplan_icon.png` |
| Initial Setup | `dark_initialsetup_icon.png` | `light_initialsetup_icon.png` |
| Config/Tune | `dark_tuning_icon.png` | `light_tuning_icon.png` |
| Simulation | `dark_simulation_icon.png` | `light_simulation_icon.png` |
| Terminal | `dark_terminal_icon.png` | `light_terminal_icon.png` |
| Help | `dark_help_icon.png` | `light_help_icon.png` |
| Connect | `dark_connect_icon.png` | — |
| Disconnect | `dark_disconnect_icon.png` | — |
