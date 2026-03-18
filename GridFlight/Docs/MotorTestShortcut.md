# MotorTestShortcut

**Archivo:** `GridFlight/MotorTestShortcut.cs`
**Perfil:** Solo Piloto
**Fase:** `Loaded()` + `Loop()` a 2 Hz

## Que hace

Acceso rapido a la pagina de Motor Test desde el toolbar principal.

- **Visibilidad:** Solo visible cuando SITL esta activo (`SITL.SITLSEND.Client.Connected`)
- **Accion:** Navega a Setup > activa la pagina "Motor Test" del BackstageView
- **Icono:** `engine.png` desde `GridFlight/assets/`

## Assets requeridos

- `GridFlight/assets/engine.png`
