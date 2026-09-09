# Plan de ejecución para la migración de MonoDevelop a .NET 8 LTS

## 0. Hoja operativa ejecutiva para agentes

La versión detallada y ejecutiva para las fases 3 a 5 queda en `migration-phase-3-5-execution-plan.md`.

Este documento centraliza la estrategia principal y deja la ejecución granular para agentes que necesitan seguir con tareas específicas por bloque.

## 1. Objetivo de esta hoja de trabajo

Este documento convierte el informe técnico en un plan operativo para otros agentes. Su propósito es permitir que el trabajo avance en fases, con entregables claros, criterios de aceptación y prioridad por área.

No se consideran nuevas funcionalidades en esta fase. La prioridad es la modernización de la base técnica y la preparación de una plataforma estable bajo .NET 8 LTS.

## 2. Principios de ejecución

- Linux es la prioridad del primer ciclo productivo.
- La compatibilidad para otros sistemas operativos se prepara por abstracción y separación de capas, no como requisito inicial.
- La UI se decide más adelante; no debe bloquear la migración de base.
- Cada fase debe cerrar con validación, documentación y evidencia.
- Se prioriza la reducción del acoplamiento a Mono/Gtk# sobre la reescritura completa.

## 3. Fases y entregables

### Fase 0 - Inventario y línea base

Objetivo:
- comprender el repositorio real antes de cambiarlo.

Áreas a revisar:
- `main/README.md`
- `main/MonoDevelop.props`
- `main/Directory.Build.props`
- `main/src/core/`
- `main/src/addins/`
- `main/tests/`
- `main/external/fsharpbinding/`

Tareas:
- [ ] Inventariar dependencias directas e indirectas de Mono y Gtk#.
- [ ] Identificar scripts legacy: `configure`, `make`, `xbuild` y su alcance.
- [ ] Mapear proyectos que usan MSBuild clásico o propiedades heredadas.
- [ ] Clasificar dependencias por riesgo y prioridad.
- [ ] Establecer una línea base de build y test actual.

Criterio de cierre:
- Existe un mapa de dependencias con riesgo, propietarios de bloque y prioridad.

### Fase 1 - Estabilización de la base actual

Objetivo:
- dejar el repositorio en un estado verificable antes de cambios de infraestructura.

Tareas:
- [ ] Asegurar un build reproducible para la rama actual.
- [ ] Validar tests existentes y registrar resultados.
- [ ] Separar riesgos de infraestructura de cambios funcionales.
- [ ] Crear un estado de referencia para rollback.
- [ ] Documentar bloqueadores reales del repositorio.

Criterio de cierre:
- El repo está en estado de referencia verificable, con evidencia de builds y tests.

### Fase 2 - Modernización de dependencias

Objetivo:
- actualizar paquetes y librerías a versiones compatibles con .NET 8 sin cambiar el modelo funcional.

Áreas involucradas:
- `main/Directory.Build.props`
- `main/msbuild/`
- `main/src/addins/MonoDevelop.DotNetCore/`
- `main/src/core/`
- `main/tests/`

Tareas:
- [ ] Revisar `NuGetVersion*` en `main/Directory.Build.props`.
- [ ] Priorizar paquetes de compilación y testing.
- [ ] Actualizar `NuGet`, `NUnit`, `Newtonsoft.Json`, `Microsoft.TemplateEngine` y dependencias relacionadas.
- [ ] Validar compatibilidad con MSBuild moderno.
- [ ] Registrar incompatibilidades por paquete y si requieren cambios de arquitectura.

Criterio de cierre:
- El árbol de dependencias está actualizado y funcional con el modelo de build moderno.

#### Bloque detectado: paquete Roslyn de editor/IDE legado

Durante la revisión del siguiente bloque se confirmó que la dependencia más crítica del árbol actual ya no es un paquete "desactualizado" simple, sino una familia de ensamblados de Roslyn asociados a editor/IDE que quedaron en el modelo privado de Visual Studio:

- `Microsoft.CodeAnalysis.CSharp.Features`
- `Microsoft.CodeAnalysis.Features`
- `Microsoft.CodeAnalysis.CSharp.EditorFeatures`
- `Microsoft.CodeAnalysis.EditorFeatures`

La evidencia concreta es que la versión del repositorio (`main/msbuild/RoslynVersion.props`) estaba anclada a una familia vieja (`3.11.0`), y la validación con `.NET 8` mostró que el paquete `Microsoft.CodeAnalysis.CSharp.Features` ya no existe en los feeds configurados (`vssdk` y `nuget.org`). El paquete equivalente que sí existe en `vssdk` es `Microsoft.CodeAnalysis.CSharp.Workspaces`, pero no la familia `Features` de la antigua capa de edición/IDE.

Esto indica que el problema no es solo un número de versión, sino una dependencia estructural al stack interno de Roslyn/VS. La modernización correcta pasa por:

1. retirar la dependencia directa a la familia `Features` de editor/IDE;
2. sustituir la integración con APIs públicas de Roslyn disponibles en `Microsoft.CodeAnalysis.Workspaces`;
3. encapsular la lógica de edición y convenios con adaptadores locales o shims, en lugar de depender de ensamblados del feed privativo de Visual Studio.

Ficheros relevantes observados:
- `main/msbuild/RoslynVersion.props`
- `main/src/core/MonoDevelop.Core/MonoDevelop.Core.csproj`
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Editor/CodingConventionsShim.cs`
- `main/external/vs-editor-api/Directory.Build.props`

Resultado de la validación:
- el feed `vssdk` resuelve paquetes como `Microsoft.CodeAnalysis.EditorFeatures` y `Microsoft.CodeAnalysis.Workspaces.Common`, pero no `Microsoft.CodeAnalysis.CSharp.Features` ni `Microsoft.CodeAnalysis.Features`;
- por tanto, el siguiente paso real no es un pin más moderno aislado, sino un desacoplamiento de la capa Roslyn/Editor de Visual Studio.

### Fase 3 - Separación de la lógica del runtime Mono

Objetivo:
- reducir el acoplamiento entre la lógica de negocio y Mono/Gtk#.

Áreas relevantes:
- `main/src/core/`
- `main/src/addins/`
- `main/src/addins/MonoDevelop.DotNetCore/`
- `main/src/addins/VersionControl/`

Tareas:
- [ ] Identificar componentes que tienen dependencia directa de Mono.
- [ ] Mover lógica de compilación y análisis a capas portables.
- [ ] Mantenar adaptadores específicos de plataforma.
- [ ] Aislar servicios de proyecto, resolución y edición.
- [ ] Preparar capas reutilizables para .NET 8.

Criterio de cierre:
- La lógica principal ya no depende directamente del runtime Mono para su funcionamiento básico.

### Fase 4 - Migración del sistema de build

Objetivo:
- pasar de flujo legado a un pipeline basado en .NET SDK.

Áreas relevantes:
- `main/MonoDevelop.props`
- `main/Directory.Build.props`
- `main/Directory.Build.targets`
- `main/msbuild/`
- `main/src/core/MonoDevelop.Projects.Formats.MSBuild/`

Tareas:
- [ ] Revisar suposiciones de MSBuild clásico y xbuild.
- [ ] Ajustar propiedades globales para SDK-style projects.
- [ ] Reemplazar rutas customizadas y targets de legacy.
- [ ] Validar restauración y compilación con `dotnet build`/`dotnet msbuild`.
- [ ] Reducir dependencias de `configure` y `make` en la fase de build.

Criterio de cierre:
- La compilación del repositorio se ejecuta principalmente con .NET SDK moderno.

### Fase 5 - Migración del runtime a .NET 8 LTS

Objetivo:
- llevar el runtime base a .NET 8 LTS.

Áreas relevantes:
- `main/src/addins/MonoDevelop.DotNetCore/`
- `main/src/core/`
- `main/src/addins/`
- `main/tools/`

Tareas:
- [ ] Revisar uso de APIs dependientes de Mono.
- [ ] Adaptar carga de ensamblados y resolución de dependencias.
- [ ] Revisar `System.Reflection`, `Path`, `Environment` y `AssemblyLoadContext`.
- [ ] Validar compatibilidad con proyectos .NET modernos.
- [ ] Asegurar soporte de add-ins y extensibilidad.

Criterio de cierre:
- La aplicación puede ejecutarse y trabajar bajo .NET 8 en Linux.

### Fase 6 - Linux-first validation

Objetivo:
- hacer que la base migrada sea operativa en Linux.

Tareas:
- [ ] Ejecutar build en Linux con la nueva base.
- [ ] Validar carga de plugins/add-ins en Linux.
- [ ] Probar flujo de edición, compilación y debug básicos.
- [ ] Revisar librerías y paquetes nativos del sistema.
- [ ] Registrar diferencias de comportamiento respecto al stack anterior.

Criterio de cierre:
- El sistema es estable y funcional para el escenario Linux principal.

### Fase 7 - Preparación para multi-OS

Objetivo:
- dejar la plataforma lista para ampliar soporte fuera de Linux.

Tareas:
- [ ] Identificar componentes de plataforma y de dependencias del sistema.
- [ ] Separar lógica de negocio de I/O de sistema y UI.
- [ ] Definir adaptadores por sistema operativo.
- [ ] Preparar el repositorio para soportar Windows/macOS en una fase posterior.

Criterio de cierre:
- La base técnica ya no está acoplada a un único sistema operativo.

### Fase 8 - Decisión de interfaz de usuario

Objetivo:
- revisar la tecnología visual con criterio técnico antes del corte final.

Tareas:
- [ ] Analizar opciones de UI multiplataforma.
- [ ] Evaluar consistencia visual, rendimiento y mantenibilidad.
- [ ] Definir la recomendación final para el entorno de escritorio.
- [ ] Separar la decisión de UI de la migración del runtime.

Criterio de cierre:
- Existe una recomendación técnica documentada para la UI antes del cutover final.

### Fase 9 - Cutover y estabilización

Objetivo:
- cerrar la migración con validación, estabilización y rollback preparado.

Tareas:
- [ ] Ejecutar validación final de build y pruebas.
- [ ] Realizar smoke tests de escenarios clave.
- [ ] Cerrar v1 migrada en rama o entorno de evaluación.
- [ ] Confirmar rollback y estrategia de recuperación.
- [ ] Preparar la siguiente etapa de nuevas funcionalidades para discusión posterior.

Criterio de cierre:
- El repositorio queda en una base estable con migration cutover documentado.

## 4. Priorización por carpetas

### `main/` (raíz y configuración global)
- Revisar propiedades globales.
- Consolidar build y runtime.
- Definir compatibilidad de SDK y toolchain.

### `main/src/core/`
- Revisión de servicios centrales.
- Separación de lógica de negocio.
- Aislamiento de Mono/Gtk#.

### `main/src/addins/`
- Análisis de extensión y plugins.
- Validación de compatibilidad con .NET 8.
- Soporte de nuevos runtime para add-ins.

### `main/src/addins/MonoDevelop.DotNetCore/`
- Este es un punto estratégico porque ya existe soporte parcial para .NET Core.
- Es una base natural para migración incremental hacia .NET 8.

### `main/src/core/MonoDevelop.Projects.Formats.MSBuild/`
- Critical path para build y carga de proyectos.
- Debe revisarse con prioridad estratificada.

### `main/tests/`
- Validación de regresión y smoke tests.
- Revisión de compatibilidad y soporte de proyecto.

### `main/external/fsharpbinding/`
- Revisar compatibilidad y dependencias externas a la plataforma base.

## 5. Criterios de aceptación globales

- Se mantiene la capacidad de construir y ejecutar el proyecto moderno.
- El producto deja de depender críticamente de Mono / Gtk# para la lógica principal.
- La base puede ejecutarse en Linux como objetivo principal.
- Existen pruebas y evidencia técnica para cada fase.
- El plan queda documentado para que otros agentes puedan continuar sin perder contexto.

## 6. Restricciones de alcance

Los siguientes temas quedan fuera de la fase actual:

- nuevas funcionalidades del IDE,
- rediseño completo de UX,
- estrategia completa de multi-OS como requisito inicial,
- decisiones definitivas de la UI sin análisis posterior,
- grandes reescrituras sin validación incremental.

## 7. Recomendación para continuidad

Se recomienda seguir el orden exacto de fases:

1. Baseline y diagnóstico
2. Estabilización
3. Dependencias
4. Separación de runtime
5. Build moderno
6. .NET 8
7. Linux validation
8. Multi-OS preparation
9. UI validation
10. Cutover final

El trabajo posterior a esta fase debe reabrirse como plan funcional y de producto, no como parte de la migración base.
