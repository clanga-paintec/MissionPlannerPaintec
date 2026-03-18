# GridFlightProfile

**Archivo:** `GridFlight/GridFlightProfile.cs`
**Tipo:** Clase estatica de utilidad (no es un plugin)

## Que hace

Gestion centralizada del perfil activo (Piloto o Mecanico). Todos los plugins consultan esta clase para decidir si deben activarse.

### Propiedades

| Propiedad | Descripcion |
|-----------|-------------|
| `Current` | Lee el perfil de `Settings.Instance["GridFlight_Profile"]`, default `"Pilot"` |
| `IsPilot` | `!IsMechanic` — cualquier valor que no sea "Mechanic" se trata como Piloto |
| `IsMechanic` | `Current == "Mechanic"` (comparacion case-insensitive) |
| `IsFirstLaunch` | `true` si la clave no existe en config.xml |
| `ConfigsDirectory` | Ruta a `GridFlight/configs/` (se crea automaticamente) |

### Persistencia

- **Clave en config.xml:** `GridFlight_Profile`
- **Valores validos:** `"Pilot"` / `"Mechanic"`
- Los cambios de perfil requieren reinicio (el ciclo de vida de plugins solo ejecuta `Init()` una vez)
