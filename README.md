# GridFlight Mission Planner

![GridFlight](https://img.shields.io/badge/GridFlight-Mission%20Planner-FFC107?style=for-the-badge&logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZD0iTTIxIDl2MTBoLTZ2LTRoLTZ2NEgzVjlsOS03eiIgZmlsbD0iI0ZGQzEwNyIvPjwvc3ZnPg==)
![License](https://img.shields.io/badge/license-GPLv3-blue?style=for-the-badge)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey?style=for-the-badge)

Version personalizada de [ArduPilot Mission Planner](https://github.com/ArduPilot/MissionPlanner) desarrollada por **GridFlight** para operadores de drones comerciales.

---

## Que es GridFlight

GridFlight transforma Mission Planner en una herramienta mas segura e intuitiva para pilotos y mecanicos, sin modificar el codigo fuente original. Todo funciona mediante un **sistema de 12 plugins** que se activan segun el perfil del usuario.

### Perfiles

| Perfil | Descripcion |
|--------|-------------|
| **Piloto** | Interfaz simplificada con tema ambar, menus reducidos, atajos operativos y modos de vuelo filtrados |
| **Mecanico** | MissionPlanner original con branding GridFlight y acceso completo a todas las funciones |

---

## Funcionalidades

### Identidad Visual
- Tema moderno con paleta ambar oscura (`ModernThemePlugin`)
- Iconos de toolbar personalizados (`IconOverridePlugin`)
- Logo, icono de ventana y splash screen GridFlight (`BrandingPlugin`)

### Seguridad de Vuelo
- Filtro de modos de vuelo peligrosos — configurable (`FlightModePlugin`)
- Lista completa de modos solo para mecanicos

### Simplificacion de Interfaz
- Ocultacion de menus de hardware irrelevante (`HideOptionalHardwarePlugin`)
- Ocultacion de configuracion avanzada (`HideSetupMenuItemsPlugin`)

### Atajos Operativos
- Write & Verify en un click (`WriteVerifyPlugin`)
- Motor Test rapido desde toolbar — solo en SITL (`MotorTestShortcut`)
- Perfil de elevacion rapido (`ElevationGraphShortcut`)

### Gestion de Configuraciones
- Guardar, cargar, importar y eliminar archivos `.param` (`FavoriteConfigsPlugin`)
- Selector de perfil Piloto/Mecanico con persistencia (`ProfileSelectorPlugin`)

---

## Arquitectura

```
GridFlight/
├── assets/                    Iconos, logos, fuentes, frames
├── configs/                   Configuraciones .param guardadas (runtime)
├── Docs/                      Documentacion tecnica
│   ├── ARCHITECTURE.md        Arquitectura del sistema
│   └── *.md                   Doc individual por plugin
├── GridFlightProfile.cs       Utilidad de gestion de perfil
├── BrandingPlugin.cs          Identidad visual
├── IconOverridePlugin.cs      Iconos toolbar
├── ModernThemePlugin.cs       Tema ambar
├── FlightModePlugin.cs        Control de modos de vuelo
├── HideOptionalHardwarePlugin.cs
├── HideSetupMenuItemsPlugin.cs
├── WriteVerifyPlugin.cs       Write & Verify
├── MotorTestShortcut.cs       Atajo Motor Test
├── ElevationGraphShortcut.cs  Atajo perfil elevacion
├── FavoriteConfigsPlugin.cs   Gestor de configs
└── ProfileSelectorPlugin.cs   Selector de perfil
```

> Documentacion tecnica completa en [`GridFlight/Docs/ARCHITECTURE.md`](GridFlight/Docs/ARCHITECTURE.md)

---

## Compilacion

### Requisitos

- Windows 10/11 (64-bit)
- Visual Studio 2022 ([descargar](https://visualstudio.microsoft.com/downloads/))
- .NET Framework 4.8+

### Pasos

```bash
# Clonar el repositorio
git clone https://github.com/clanga-paintec/MissionPlannerPaintec.git

# Inicializar submodulos
git submodule update --init

# Abrir MissionPlanner.sln en Visual Studio y compilar
```

> Los plugins se compilan automaticamente con el proyecto principal. No requieren configuracion adicional.

---

## Roadmap

### v1.2 — En desarrollo

- [ ] **Perfil Mecanico completo** — Implementacion completa de funcionalidades exclusivas para mecanicos (diagnostico, parametros avanzados, herramientas de mantenimiento)

### Futuro

- [ ] Sistema de actualizaciones OTA para plugins
- [ ] Dashboard de estado de flota
- [ ] Integracion con herramientas de registro de vuelo

---

## Creditos

- Basado en [ArduPilot Mission Planner](https://github.com/ArduPilot/MissionPlanner) por Michael Oborne
- Desarrollado por el equipo de **GridFlight**

## Licencia

Este proyecto esta basado en el codigo abierto de ArduPilot Mission Planner. Consulte [COPYING.txt](COPYING.txt) para informacion sobre la licencia GPLv3.
