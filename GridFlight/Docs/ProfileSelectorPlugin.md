# ProfileSelectorPlugin

**Archivo:** `GridFlight/ProfileSelectorPlugin.cs`
**Perfil:** Todos (Piloto y Mecanico)
**Fase:** `Loaded()` (ejecucion unica)

## Que hace

Gestion de la seleccion y cambio de perfil (Piloto / Mecanico) desde la interfaz.

### Primer arranque

Si `GridFlightProfile.IsFirstLaunch` es `true`, muestra un dialogo modal con dos opciones:
- **PILOTO** — Experiencia GridFlight completa
- **MECANICO** — MissionPlanner original con branding GridFlight

### Toolbar

Añade un `ToolStripDropDownButton` al toolbar mostrando el perfil activo ("PILOTO" o "MECANICO") con un dropdown para cambiar al otro perfil.

### Cambio de perfil

1. Persiste la seleccion en `config.xml` (`GridFlight_Profile`)
2. Solicita reinicio via `Application.Restart()` (necesario porque `Init()` es el unico punto de control en el ciclo de vida de plugins)
