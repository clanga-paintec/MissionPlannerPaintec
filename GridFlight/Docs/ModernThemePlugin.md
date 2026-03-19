# ModernThemePlugin

**Archivo:** `GridFlight/ModernThemePlugin.cs`
**Perfil:** Ambos perfiles
**Fase:** `Loaded()` (ejecucion unica)

## Que hace

Aplica un tema moderno con paleta ambar oscura a toda la interfaz de MissionPlanner.

### Paleta de colores

| Rol | Color | Hex |
|-----|-------|-----|
| Fondo | Negro oscuro | `#181818` |
| Superficie | Gris oscuro | `#212121` / `#2D2D2D` |
| Acento primario | Ambar | `#FFC107` |
| Acento oscuro | Ambar oscuro | `#D39E00` |
| Texto primario | Blanco suave | `#F5F5F5` |
| Texto secundario | Gris | `#AAAAAA` |

### Tecnica

- Sobrescribe campos estaticos de `ThemeManager`
- Recorrido recursivo de controles aplicando `FlatStyle`, renderers custom
- Fuente base: Segoe UI 9pt
- Fuente de iconos: Material Symbols Rounded (TTF cargado via `PrivateFontCollection`)

### Respeta

- Controles con `PreventThemingAttribute`
- Controles con `Tag = "custom"`
