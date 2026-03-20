# PreFlightChecklistPlugin — Guia Completa

**Archivo:** `GridFlight/PreFlightChecklistPlugin.cs`
**Perfil:** Ambos perfiles (Piloto y Mecanico)
**Fase:** `Init()` + `Loaded()`

---

## Por que existe este plugin

Antes de cada vuelo, un piloto de drones DEBE verificar que el vehiculo esta en condiciones seguras. Cosas como: ¿la bateria tiene suficiente carga? ¿el GPS tiene buena señal? ¿las helices estan bien puestas?

MissionPlanner ya tiene un sistema de checklist (`CheckListControl`), pero viene con items genericos orientados a aviones de ala fija ("Tail and wings secured?"). Nuestro plugin aprovecha esa infraestructura y la alimenta con items especificos para drones.

## Que valor aporta

1. **Seguridad:** Previene despegues con condiciones inseguras
2. **Automatizacion:** 8 checks evaluan telemetria en tiempo real (verde = OK, rojo = fallo)
3. **Disciplina operacional:** 6 checks manuales fuerzan al piloto a verificar fisicamente
4. **Accesibilidad:** Boton de acceso rapido en el toolbar

## Como funciona (paso a paso)

### 1. Init() — Habilitar el tab

```csharp
public override bool Init()
{
    MainV2.DisplayConfiguration.displayPreFlightTab = true;
    return true;
}
```

`DisplayConfiguration` controla que tabs son visibles en FlightData. Al poner `displayPreFlightTab = true`, garantizamos que el tab "Pre-Flight" aparezca. Este patron es identico al que usa `HideSetupMenuItemsPlugin`.

### 2. Loaded() — Desplegar checklist + boton

```csharp
public override bool Loaded()
{
    DeployDefaultChecklist();  // Copia XML si no existe
    AddToolbarButton();        // Icono clipboard en toolbar
    return false;              // No necesita Loop()
}
```

### 3. DeployDefaultChecklist() — El truco clave

MissionPlanner busca el checklist en dos ubicaciones:
1. `UserDataDirectory/checklist.xml` (prioridad alta — configuracion del usuario)
2. `RunningDirectory/checklistDefault.xml` (fallback — el default de MissionPlanner)

Nosotros copiamos nuestro XML desde `GridFlight/configs/checklistGridFlight.xml` al `checklist.xml` del usuario, SOLO si no existe uno previo. Asi:
- No tocamos ningun archivo del codigo fuente
- Si el usuario ya personalizo su checklist, no lo sobrescribimos
- Si se borra el plugin, el XML del usuario permanece (inofensivo)

### 4. Toolbar button — Navegacion rapida

El boton navega a FlightData y selecciona el tab PreFlight:
```csharp
MainV2.View.ShowScreen("FlightData");
Host.MainForm.BeginInvoke((MethodInvoker)TrySelectPreFlightTab);
```

Se usa `BeginInvoke` porque FlightData necesita un ciclo de UI para reconstruir sus tabs.

## Checks automaticos

Cada check evalua una propiedad de `CurrentState` contra un umbral:

| Check | Propiedad | Que mide | Umbral |
|-------|-----------|----------|--------|
| Bateria - Voltaje | `battery_voltage` | Voltaje de la bateria | > 22V |
| Bateria - Restante | `battery_remaining` | Porcentaje restante | > 20% |
| GPS - Fix 3D | `gpsstatus` | Tipo de fix GPS (0=nada, 3=3D) | >= 3 |
| GPS - Satelites | `satcount` | Satelites visibles | >= 8 |
| GPS - HDOP | `gpshdop` | Precision horizontal (menor = mejor) | < 2.0 |
| PreArm Status | `prearmstatus` | Todos los checks del autopiloto OK | = 1 |
| Sin Failsafe | `failsafe` | Ningun failsafe activo | = 0 |
| Calidad enlace | `linkqualitygcs` | Señal de radio al GCS | > 50% |

## Checks manuales

El operador debe verificar fisicamente y marcar el checkbox:
- Helices aseguradas y sin dano
- Bateria bien conectada
- Payload asegurado
- Area despejada de personas
- Meteorologia aceptable
- Zona de despegue despejada

## Archivos del plugin

| Archivo | Proposito |
|---------|-----------|
| `GridFlight/PreFlightChecklistPlugin.cs` | Plugin principal |
| `GridFlight/configs/checklistGridFlight.xml` | Items del checklist (XML) |
| `Directory.Build.targets` | Copia el XML al output en build |

## Atomicidad

Para eliminar completamente: borrar `PreFlightChecklistPlugin.cs` + `checklistGridFlight.xml` + entrada en `Directory.Build.targets`. Opcionalmente borrar `checklist.xml` del directorio de datos del usuario para volver al default de MissionPlanner.

## Infraestructura de MissionPlanner reutilizada

- `Controls/PreFlight/CheckListControl.cs` — El UserControl con timer
- `Controls/PreFlight/CheckListItem.cs` — Items con condiciones sobre CurrentState
- `tabPagePreFlight` — Tab en FlightData
- `DisplayView.displayPreFlightTab` — Flag de visibilidad
