# Informe de migración y modernización del repositorio MonoDevelop

## 1. Alcance y contexto de la fase actual

Este documento define la primera parte del plan de modernización del repositorio. El objetivo de esta fase no es implementar nuevas funcionalidades, sino preparar la base técnica para migrar la aplicación y sus dependencias desde el ecosistema Mono/Gtk hacia una plataforma moderna basada en .NET 8 LTS.

Las decisiones clave para esta etapa son:

- La migración es incremental y por fases.
- La primera prioridad es migrar dependencias y runtime, no añadir funcionalidad.
- Se usará .NET 8 como objetivo principal por su soporte LTS prolongado.
- La aplicación se enfocará inicialmente en Linux, con compatibilidad para otros sistemas operativos como una línea posterior, no como requisito inicial.
- La interfaz de usuario se revisará en el momento apropiado; la intención es emplear una tecnología con apariencia consistente en entornos de escritorio diferentes.
- Se evitará la reescritura completa en un solo paso; el proceso debe ser controlado, verificable y reversible.

## 2. Diagnóstico del repositorio

### 2.1 Evidencia del estado actual

El repositorio presenta señales claras de que está orientado a MonoDevelop clásico, no a una solución moderna basada en .NET SDK:

- El README indica que el proyecto es un IDE para Mono usando Gtk# y que la compilación tradicional se realiza con `./configure ; make`.
- El archivo `main/MonoDevelop.props` define propiedades y targets del stack Monodevelop/MSBuild clásico.
- El archivo `main/Directory.Build.props` fija versiones antiguas de dependencias, incluyendo NuGet, NUnit, VS editor, Newtonsoft y Microsoft Template Engine.
- El repo contiene soporte para .NET Core en `main/src/addins/MonoDevelop.DotNetCore`, lo que indica que ya hubo un intento de compatibilidad parcial con el modelo moderno, pero no una migración completa del stack base.

### 2.2 Estado arquitectónico

El proyecto está estructurado como una IDE con múltiples capas:

- Core y servicios generales.
- Add-ins específicos por funcionalidad.
- Soporte de paquetes, depuración, ASP.NET, GTK, versiones de control, etc.
- Integración con MSBuild y templates.

La arquitectura actual combina varias preocupaciones que deben separarse con cuidado durante la migración:

- runtime Mono/Gtk#
- tooling de compilación basado en MSBuild clásico
- soporte de proyectos .NET Core y SDKs
- UI y lógica de IDE
- add-ins y extensibilidad

Esto sugiere que la migración no debe tratarse como un cambio de un único paquete o de una sola dependencia, sino como una modernización de la plataforma completa del IDE.

## 3. Decisiones de diseño acordadas para esta fase

1. Se prioriza la migración de base técnica sobre nuevas funcionalidades.
2. Se usa .NET 8 LTS como objetivo del runtime moderno y del tooling principal.
3. La primera entrega funcional será centrada en Linux.
4. Compatibilidad multi-OS se evaluará en una fase posterior, no como requisito inicial.
5. La UI se revisará con una estrategia independiente de esta fase de migración.
6. La transición será incremental por componentes para reducir riesgos.
7. Los cambios se validarán con build y pruebas en cada fase.
8. Se mantiene un plan de rollback y un estado de compatibilidad intermedia.

## 4. Objetivo de la migración

El objetivo es llevar este repositorio desde un modelo basado en Mono y Gtk# a una plataforma moderna con .NET 8, manteniendo la capacidad de desarrollar software y de compilar para diferentes objetivos, sin forzar a la vez una reescritura total de la interfaz o una expansión del producto.

La meta de la primera parte es:

- migrar dependencias de compilación y runtime,
- eliminar la dependencia crítica de Mono donde sea posible,
- estabilizar la base para soportar un IDE moderno y escalable,
- preparar la plataforma para futuras entregas funcionales.

## 5. Plan de migración por fases

### Fase 0: inventario y línea base

Objetivo:
- identificar exactamente qué depende de Mono, Gtk#, xbuild y scripts legacy.
- documentar todas las dependencias relevantes del proyecto.
- establecer una línea base estable del repositorio antes de tocar la arquitectura.

Actividades:
- auditar todas las referencias de `main`, add-ins y pruebas,
- identificar proyectos que aún usan `xbuild`/MSBuild clásico,
- revisar scripts de build y de configuración,
- categorizar dependencias por riesgo:
  - bajo riesgo,
  - medio riesgo,
  - alto riesgo.

Resultado esperado:
- un mapa de dependencias y runtime con riesgos y prioridad de migración.

### Fase 1: estabilización del código actual

Objetivo:
- dejar el repositorio en una base limpia y verificable antes de migrar.

Actividades:
- asegurar build reproducible,
- estabilizar tests y CI,
- fijar el estado de referencia,
- documentar bloqueadores conocidos.

Resultado esperado:
- una línea base verde y verificable antes de cambios de infraestructura.

### Fase 2: actualización de dependencias a versiones modernas

Objetivo:
- modernizar las librerías del stack sin todavía cambiar el runtime principal.

Dependencias a revisar por prioridad:
- `NuGet`
- `NUnit`
- `Newtonsoft.Json`
- `Microsoft.TemplateEngine`
- `VSCodeDebugProtocol`
- `VSComposition`
- `VSEditor`

Criterio:
- cada actualización debe validarse aislada y documentarse como bloque independiente.

Resultado esperado:
- un árbol de dependencias más moderno y compatible con .NET 8, sin romper el comportamiento del IDE.

### Fase 3: desacople de Mono de la lógica de negocio

Objetivo:
- separar la lógica funcional del IDE de la dependencia directa de Mono.

Actividades:
- mover responsabilidades de proyecto, resolución, análisis y servicios a librerías no acopladas a Mono,
- mantener adaptadores específicos para plataforma,
- aislar la lógica que puede ejecutarse bajo .NET moderno.

Resultado esperado:
- una capa de dominio y servicios portables a .NET 8.

### Fase 4: migración del sistema de build

Objetivo:
- reemplazar el flujo `configure` + `make` + `xbuild` por un pipeline basado en `dotnet` moderno.

Actividades:
- convertir proyectos a estilo SDK si aplica,
- unificar propiedades y targets con `Directory.Build.props` y `Directory.Build.targets`,
- reemplazar suposiciones de MSBuild clásico,
- eliminar dependencia de rutas y utilidades Mono-specific donde sea posible.

Resultado esperado:
- compilación y restauración gestionadas por .NET SDK moderno.

### Fase 5: migración del runtime a .NET 8

Objetivo:
- mover el runtime base de Mono a .NET 8 LTS.

Actividades:
- revisar APIs de runtime que dependan de Mono,
- adaptar carga de ensamblados y resolución de dependencias,
- revisar comportamiento del IDE con `System.Reflection`, `AssemblyLoadContext`, `Path`, `Environment` y herramientas de MSBuild,
- asegurar compatibilidad con proyectos .NET y con la carga de add-ins.

Resultado esperado:
- aplicación ejecutándose en .NET 8 con la capa funcional restaurada.

### Fase 6: estrategia Linux-first

Objetivo:
- garantizar que la primera base migrada sea usable en Linux, que es el objetivo principal del plan actual.

Actividades:
- validar build y ejecución en Linux,
- revisar paquetes nativos y bibliotecas del sistema,
- asegurar que los componentes de edición y compilación funcionen en Linux,
- definir qué partes del producto serán nativas a la plataforma y cuáles se compartirán.

Resultado esperado:
- una versión estable y operativa en Linux como base para el siguiente paso.

### Fase 7: compatibilidad futura para otros sistemas operativos

Objetivo:
- dejar la plataforma lista para evolucionar hacia Windows y macOS en una segunda etapa.

Actividades:
- separar la lógica de plataforma de la lógica de producto,
- definir una capa de adaptación para cada sistema operativo,
- usar abstracciones para servicios UI y del sistema,
- dejar la compatibilidad como trabajo posterior a la estabilización de Linux.

Resultado esperado:
- el repositorio ya no estará acoplado a un único sistema operativo.

### Fase 8: estrategia de UI y consistencia visual

Objetivo:
- preparar la decisión de interfaz para la etapa posterior, sin bloquear la migración.

Se recomienda un enfoque de UI multiplataforma con consistencia visual, con una tecnología que permita:

- apariencia consistente en diferentes entornos de escritorio,
- integración nativa suficiente para la experiencia de un IDE,
- estabilidad y mantenibilidad a medio plazo.

No es una decisión para esta fase inicial, pero sí es un punto de arquitectura que se debe tener en cuenta antes del cutover final.

La propuesta técnica concreta queda documentada en `ui-technology-proposal.md` y será la referencia base para la decisión de framework de UI en la siguiente etapa de migración.

### Fase 9: validación, cutover y estabilización

Objetivo:
- introducir la nueva base en producción funcional.

Actividades:
- validación de build en CI,
- smoke tests funcionales,
- pruebas de carga de add-ins,
- ejecución de escenarios clave del IDE,
- cutover controlado con rollback.

Resultado esperado:
- la migración es estable y el repositorio queda listo para la siguiente etapa de evolución funcional.

## 6. Riesgos y mitigaciones

### Riesgo 1: dependencias ocultas de Mono
Mitigación:
- auditar y clasificación por bloque,
- migración por capas,
- pruebas de integración por ítem de compatibilidad.

### Riesgo 2: scripts de build heredados
Mitigación:
- reemplazar secuencias de `configure/make` por flujo `.NET SDK`,
- mantener compatibilidad intermedia y validar por CI.

### Riesgo 3: acoplamiento fuerte UI + lógica
Mitigación:
- separar la capa de negocio de la UI,
- reducir dependencias directas a Gtk# en la lógica central.

### Riesgo 4: quebrar soporte de proyectos y add-ins
Mitigación:
- pruebas por add-in y por tipo de proyecto,
- compatibilidad con proyectos .NET existentes,
- migración controlada por módulos.

### Riesgo 5: riesgo de reescritura completa
Mitigación:
- plan incremental por fases,
- no se agregan funcionalidades nuevas en esta fase,
- cada fase debe terminar con validación objetiva.

### Riesgo 6: decisión prematura de UI
Mitigación:
- no bloquear la migración principal en la decisión UX,
- preparar una evaluación técnica posterior con criterios de consistencia, mantenibilidad y portabilidad.

## 7. Qué no se hará en esta fase

Para mantener claridad de alcance, esta primera etapa no contempla:

- nuevas funcionalidades del IDE,
- ampliar el conjunto de herramientas del producto,
- cambiar la estrategia de UX sin análisis posterior,
- soportar de golpe todos los sistemas operativos a la vez,
- reescritura completa de la interfaz.

La fase actual se centra en base técnica, estabilidad y capacidad de migración.

## 8. Recomendación operativa para otros agentes

Los siguientes agentes que continúen el trabajo deben operar bajo estas reglas:

1. Mantener el enfoque en modernización del runtime y dependencias.
2. Priorizar diagnóstico y validación antes de cambios de arquitectura.
3. Documentar cada fase con evidencia del repositorio y pruebas.
4. Evitar cambiar el alcance funcional antes de cerrar la migración base.
5. Mantener compatibilidad en Linux como prioridad de la etapa actual.
6. Registrar decisiones de diseño para que futuras fases no vuelvan a reabrir el mismo debate.

## 9. Conclusión

El repositorio actual evidencia una base de IDE moderna en intención, pero construida sobre un stack heredado y muy ligado a Mono/Gtk#. La ruta más segura es una migración incremental, principalmente basada en .NET 8 LTS, con Linux como prioridad inicial, separación clara entre plataforma y lógica, y un proceso de validación gradual por fases.

Este documento debe servir como punto de partida para la migración, y no como una hoja de ruta funcional completa; la próxima fase funcional se discutirá una vez cerrada la base técnica.

## 10. Estado de la decisión

- Estado actual: plan aprobado para la primera fase de modernización.
- Objetivo principal: migración de dependencias y Mono a .NET 8 LTS.
- Competencia operativa: Linux primero, multi-OS posterior.
- UI: pendiente de evaluación técnica futura.
- Nuevas funcionalidades: diferidas hasta la estabilización de la migración.
