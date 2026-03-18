# ElevationGraphShortcut

**Archivo:** `GridFlight/ElevationGraphShortcut.cs`
**Perfil:** Solo Piloto
**Fase:** `Loaded()` + `Loop()` a 2 Hz

## Que hace

Acceso rapido al perfil de elevacion del terreno desde el toolbar.

- **Boton en toolbar:** Aparece solo cuando hay waypoints cargados en el plan (`Commands.Rows.Count > 1`)
- **Icono:** Renderizado dinamico con SkiaSharp desde SVG path data (`ElevationGraphIcon.svg`)
- **Accion:** Abre la ventana de perfil de elevacion del terreno

## Assets requeridos

- `GridFlight/assets/ElevationGraphIcon.svg`
