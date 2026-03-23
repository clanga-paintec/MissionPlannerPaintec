# MotorTestShortcut

**Archivo:** `GridFlight/MotorTestShortcut.cs`
**Perfil:** Ambos perfiles (comportamiento diferenciado)
**Fase:** `Loaded()` + `Loop()` a 2 Hz

## Que hace

Acceso rapido a la pagina de Motor Test desde el toolbar principal.

- **Visibilidad Piloto:** Solo visible cuando SITL esta activo (`SITL.SITLSEND.Client.Connected`)
- **Visibilidad Mecanico:** Visible cuando hay vehiculo conectado (`MainV2.comPort.BaseStream.IsOpen`)
- **Accion:** Navega a Setup > activa la pagina "Motor Test" del BackstageView
- **Icono:** `engine.png` desde `GridFlight/assets/`

## Assets requeridos

- `GridFlight/assets/engine.png`
