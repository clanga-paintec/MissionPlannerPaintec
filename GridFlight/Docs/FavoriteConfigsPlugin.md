# FavoriteConfigsPlugin

**Archivo:** `GridFlight/FavoriteConfigsPlugin.cs`
**Perfil:** Solo Piloto
**Fase:** `Loaded()` (ejecucion unica)

## Que hace

Gestor de configuraciones favoritas de parametros del dron. Permite guardar, cargar, eliminar e importar archivos `.param`.

- **Boton en toolbar:** Estrella ambar de 5 puntas renderizada con SkiaSharp
- **Almacenamiento:** Archivos `.param` en `GridFlight/configs/`

### Operaciones

| Operacion | Descripcion |
|-----------|-------------|
| **Guardar** | Lee `MainV2.comPort.MAV.param` > pide nombre > `ParamFile.SaveParamFile()` |
| **Cargar** | `ParamFile.loadParamFile()` > `ParamCompare()` para comparar y aplicar selectivamente via MAVLink |
| **Eliminar** | Borra archivo `.param` con confirmacion |
| **Importar** | Copia `.param` externo al directorio de configuraciones |
