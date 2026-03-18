# WriteVerifyPlugin

**Archivo:** `GridFlight/WriteVerifyPlugin.cs`
**Perfil:** Solo Piloto
**Fase:** `Loaded()` (ejecucion unica)

## Que hace

Boton "Write and Verify" en FlightPlanner que combina las operaciones de escritura y verificacion de mision en un solo paso.

- **Ubicacion:** `FlightPlanner.panel5` en posicion (3, 90), tamano 115x23
- **Accion:** Ejecuta `BUT_write_Click()` seguido de `BUT_read_Click()` para escribir la mision y leerla de vuelta para verificar que se cargo correctamente
