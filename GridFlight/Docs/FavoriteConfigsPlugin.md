# FavoriteConfigsPlugin

**Archivo:** `GridFlight/FavoriteConfigsPlugin.cs`
**Perfil:** Ambos perfiles
**Fase:** `Loaded()` (ejecucion unica)

## Que hace

Gestor de configuraciones favoritas de parametros del dron. Permite guardar, cargar, eliminar e importar archivos `.param`.

- **Boton en toolbar:** Estrella ambar de 5 puntas renderizada con SkiaSharp
- **Almacenamiento:** Archivos `.param` en `GridFlight/configs/`

### Operaciones

| Operacion | Descripcion |
|-----------|-------------|
| **Guardar** | Lee `Settings.Instance` > pide nombre > `QuickConfig.SaveQuickConfig` |
| **Editar** | Lee `Settings.Instance` lo guarda en un QuickConfig temporal y lo guarda otra vez |
| **Cargar** | `QuickConfig.LoadSQuickConfig` > `Settings.Instance` para comparar y aplicar selectivamente |
| **Eliminar** | Borra lista con confirmacion |
