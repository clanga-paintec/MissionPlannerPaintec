# BrandingPlugin

**Archivo:** `GridFlight/BrandingPlugin.cs`
**Perfil:** Todos (Piloto y Mecanico)
**Fase:** `Loaded()` (ejecucion unica)

## Que hace

Aplica la identidad visual de GridFlight al arrancar la aplicacion:

1. **Logo del toolbar:** Escala `logo2.png` proporcionalmente y lo aplica a `MenuArduPilot`
2. **Enlace del logo:** Redirige el click del logo de ardupilot.org a `gridflight.tech` (usando reflexion sobre `EventHandlerList`)
3. **Icono de ventana:** Carga y aplica `Gridflight-Icon.ico` como icono de la ventana principal

## Assets requeridos

- `GridFlight/assets/logo2.png`
- `GridFlight/assets/Gridflight-Icon.ico`
