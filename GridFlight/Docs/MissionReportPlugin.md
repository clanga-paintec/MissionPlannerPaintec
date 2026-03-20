# MissionReportPlugin — Guia Completa

**Archivo:** `GridFlight/MissionReportPlugin.cs`
**Perfil:** Ambos perfiles
**Fase:** `Loaded()` + `Loop()` a 1 Hz

---

## Por que existe este plugin

Despues de cada vuelo, es util tener un resumen: ¿cuanto tiempo vole? ¿que distancia recorri? ¿cuanta bateria consumi? ¿que altitud maxima alcance?

Este plugin rastrea datos durante el vuelo y genera un reporte HTML profesional con la identidad visual de GridFlight (tema oscuro con acentos amber).

## Que valor aporta

1. **Documentacion de vuelo:** Registro automatico de cada mision
2. **Analisis post-vuelo:** Metricas clave para evaluar el vuelo
3. **Profesionalismo:** Reportes HTML exportables con branding GridFlight
4. **Deteccion de problemas:** Caida de bateria excesiva, velocidades anormales

## Como funciona (paso a paso)

### 1. Init() — Ambos perfiles

```csharp
public override bool Init() => true;
```

Tanto pilotos como mecanicos generan reportes.

### 2. Loaded() — Boton + activar Loop

```csharp
public override bool Loaded()
{
    AddToolbarButton();
    loopratehz = 1f;  // 1 Hz = una vez por segundo
    return true;       // true = registrar en el loop activo
}
```

Importante: `return true` en `Loaded()` + `loopratehz` activa el `Loop()`. Esto es diferente a plugins que retornan `false` (no necesitan loop).

### 3. Loop() — El corazon del rastreo

El loop corre cada segundo en un hilo de fondo. Detecta:

**Transicion a armado (inicio de vuelo):**
```csharp
if (armed && !_wasArmed)
{
    _maxAlt = 0;
    _maxSpeed = 0;
    _startVoltage = cs.battery_voltage;
    // ... resetear contadores
}
```

**Durante el vuelo (actualizar maximos):**
```csharp
if (armed)
{
    if (cs.alt > _maxAlt) _maxAlt = cs.alt;
    if (cs.groundspeed > _maxSpeed) _maxSpeed = cs.groundspeed;
    // ... registrar modos de vuelo usados
}
```

**Transicion a desarmado (fin de vuelo):**
```csharp
if (!armed && _wasArmed)
{
    _endVoltage = cs.battery_voltage;
}
```

Este patron de "maquina de estados" (armado/desarmado) es comun en plugins de telemetria.

### 4. Generacion del reporte HTML

Al pulsar el boton, se genera un HTML con:
- **Datos de vuelo:** Duracion, distancia, altitud max, velocidad max
- **Datos de bateria:** Voltaje inicio/fin, porcentaje inicio/fin, mAh consumidos
- **Modos usados:** Secuencia de modos de vuelo (ej: "Guided → Auto → RTL")
- **Estilo GridFlight:** Fondo oscuro #181818, textos amber #FFC107

El HTML se guarda en `GridFlight/configs/reports/` y se abre automaticamente en el navegador.

### 5. Datos de CurrentState usados

| Propiedad | Que mide |
|-----------|----------|
| `cs.armed` | Si el vehiculo esta armado |
| `cs.alt` | Altitud relativa al home |
| `cs.groundspeed` | Velocidad sobre el suelo (m/s) |
| `cs.battery_voltage` | Voltaje de bateria |
| `cs.battery_remaining` | Porcentaje de bateria |
| `cs.battery_usedmah` | mAh consumidos desde el arranque |
| `cs.timeInAir` | Segundos en el aire |
| `cs.distTraveled` | Distancia total recorrida (metros) |
| `cs.mode` | Modo de vuelo actual (string) |

Todos estos vienen del autopiloto via MAVLink y se actualizan en tiempo real.

## El reporte generado

El HTML tiene este aspecto (tema oscuro GridFlight):

```
┌──────────────────────────────────────┐
│  REPORTE DE MISION                   │
│  19/03/2026 14:32 | GridFlight       │
│                                      │
│  ── VUELO ──                         │
│  Duracion:        12m 34s            │
│  Distancia total: 2.30 km            │
│  Altitud maxima:  85.2 m             │
│  Velocidad max:   43.6 km/h          │
│                                      │
│  ── BATERIA ──                       │
│  Voltaje:    25.2V → 22.1V          │
│  Nivel:      98% → 23%              │
│  Consumo:    1850 mAh               │
│                                      │
│  ── MODOS ──                         │
│  Guided → Auto → RTL                │
│                                      │
│  Generado por GridFlight v1.0        │
└──────────────────────────────────────┘
```

## Archivos del plugin

| Archivo | Proposito |
|---------|-----------|
| `GridFlight/MissionReportPlugin.cs` | Plugin principal |
| `GridFlight/configs/reports/*.html` | Reportes generados (runtime) |

## Atomicidad

Para eliminar: borrar `MissionReportPlugin.cs`. Los HTML generados son archivos inertes.

## Conceptos clave para juniors

### Loop() en hilo de fondo
`Loop()` corre en un hilo separado al de la UI. Por eso, si necesitas actualizar controles visuales, debes usar `BeginInvoke()`. En este plugin no modificamos UI desde Loop(), solo leemos datos, asi que no es necesario.

### Maquina de estados con _wasArmed
Este patron es clasico: guardas el estado anterior (`_wasArmed`) y lo comparas con el actual (`armed`). Cuando cambian, detectas una transicion. Es mucho mas eficiente que guardar timestamps o usar eventos.

### CultureInfo.InvariantCulture
Usamos `ci` para formatear numeros con punto decimal (3.14) en vez de coma (3,14). Esto asegura que el HTML se vea bien sin importar la configuracion regional del sistema operativo.
