# QuickConfig
**Archivo** `GridFlight/QuickConfig.cs`
**Tipo** `Logica de objetos`

# Que hace

Permite guardar configuraciones visuales de la pestaña Flight Data como puede ser columnas, filas
elementos en la subpestaña "Quick", etc...

# Atributos

**paramsShown**: parametros mostrados en la subpestaña "Quick"
**name**: nombre de la configuración
**displayView**: subpestañas visibles en Flight Data

# Funciones

Getters y Setters de cada atributo

**SaveQuickConfig(QuickConfig qc)**: guarda la configuración dentro de Settings.Instance para mantenerlos
entre sesiones. Si hay un nombre repetido no lo guarda y devuelve el valor booleano false

**LoadQuickConfig(string name)**: busca la configuración con el nombre dado y devuelve un objeto QuickConfig

**AllQuickConfigs()**: devuelve todas las configuraciones guardadas

**EraseQuickConfig(string name)**: borra una configuración dado el nombre de esta