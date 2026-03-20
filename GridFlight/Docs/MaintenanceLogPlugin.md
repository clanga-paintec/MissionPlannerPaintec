# MaintenanceLogPlugin — Guia Completa

**Archivo:** `GridFlight/MaintenanceLogPlugin.cs`
**Perfil:** Solo Mecanico
**Fase:** `Loaded()` (ejecucion unica)

---

## Por que existe este plugin

Cuando un mecanico repara o calibra un dron, esa informacion se pierde si no se documenta. ¿Quien cambio la helice? ¿Cuando se calibro el compas por ultima vez? ¿Que se reparo la semana pasada?

Este plugin proporciona un registro local de mantenimientos, sin necesidad de base de datos, internet, ni infraestructura externa. Un archivo JSON dentro de `GridFlight/configs/`.

## Que valor aporta

1. **Trazabilidad:** Cada intervencion queda registrada con fecha, tecnico y descripcion
2. **Responsabilidad:** Saber quien hizo que y cuando
3. **Mantenimiento preventivo:** Revisar el historial para detectar patrones
4. **Simplicidad:** No requiere internet, bases de datos, ni cuentas de usuario

## Como funciona (paso a paso)

### 1. Init() — Solo para mecanicos

```csharp
public override bool Init() => GridFlightProfile.IsMechanic;
```

Los pilotos no registran mantenimientos. Solo los mecanicos ven este plugin.

### 2. Loaded() — Boton de llave en toolbar

```csharp
public override bool Loaded()
{
    AddToolbarButton();  // Icono de llave en amber
    return false;        // Sin Loop()
}
```

### 3. El modelo de datos

Cada entrada de mantenimiento tiene 3 campos:

```csharp
private class MaintenanceEntry
{
    public string Date        { get; set; }  // "2026-03-19"
    public string Technician  { get; set; }  // "Carlos"
    public string Description { get; set; }  // "Cambio helices motor 3"
}
```

### 4. Persistencia — JSON local

```csharp
// Guardar
var json = JsonConvert.SerializeObject(entries, Formatting.Indented);
File.WriteAllText(LogFilePath, json);

// Cargar
var json = File.ReadAllText(LogFilePath);
var entries = JsonConvert.DeserializeObject<List<MaintenanceEntry>>(json);
```

El archivo se guarda en `GridFlight/configs/maintenanceLog.json`. Se crea automaticamente en el primer uso. Usamos `Newtonsoft.Json` que ya viene incluido en MissionPlanner.

### 5. La interfaz

El dialogo modal contiene:
- **ListView** con 3 columnas: Fecha, Tecnico, Descripcion
- **Campos de entrada** para nueva entrada (fecha auto-rellenada con hoy)
- **Boton Añadir** para crear nueva entrada
- **Boton Eliminar** con confirmacion para borrar una entrada

Todo estilizado con `ThemeManager.ApplyThemeTo(form)` para respetar el tema amber.

## Por que JSON y no SQLite

- **Simplicidad:** JSON se lee/escribe con una linea de codigo
- **Portabilidad:** Un solo archivo que se puede copiar o respaldar
- **Sin dependencias:** No necesita drivers de base de datos
- **Suficiente:** Para un registro de mantenimiento local, JSON es mas que suficiente
- **Escalabilidad futura:** Si algún dia se necesita mas, se migra a SQLite sin cambiar la interfaz

## Archivos del plugin

| Archivo | Proposito |
|---------|-----------|
| `GridFlight/MaintenanceLogPlugin.cs` | Plugin principal |
| `GridFlight/configs/maintenanceLog.json` | Datos (creado en runtime) |

## Atomicidad

Para eliminar: borrar `MaintenanceLogPlugin.cs`. El JSON es un archivo inerte que no afecta nada si se queda.

## Patron de diseño reutilizable

Este plugin sigue exactamente el mismo patron que `FavoriteConfigsPlugin`:
1. Boton en toolbar (SkiaSharp icon)
2. Click abre dialogo modal (Form con controles WinForms)
3. Datos persistidos en archivo local (JSON en vez de .param)
4. `ThemeManager.ApplyThemeTo()` para tematizado

Si necesitas crear otro plugin con un CRUD local, copia este patron.
