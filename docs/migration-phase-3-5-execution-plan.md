# Plan operativo para agentes: fases 3 a 5 de migración

## 1. Propósito

Este documento convierte la estrategia técnica en una hoja de trabajo ejecutable para que otros agentes puedan continuar la migración sin depender del contexto previo del análisis inicial.

Este plan cubre exclusivamente:

- Fase 3: separación del runtime Mono de la lógica del IDE
- Fase 4: migración del sistema de build
- Fase 5: migración del runtime base a .NET 8 LTS

No incluye nuevas funcionalidades ni decisiones de UI definitivas. La intención es dejar la base del IDE operativa y estable bajo .NET 8 en Linux antes de avanzar a la siguiente etapa.

## 2. Principios operativos

- La prioridad es la estabilidad del runtime y del build, no la entrega de funcionalidades nuevas.
- La refactorización debe ser incremental y verificable por bloque.
- Cada bloque debe cerrarse con compilación y evidencia de prueba.
- Todo acoplamiento a Mono/Gtk# debe quedar aislado detrás de abstracciones o adaptadores.
- La migración no debe reescribir el IDE completo ni cambiar la arquitectura funcional sin una validación intermedia.

## 3. Criterio de entrada

Antes de iniciar una fase, el agente debe comprobar que:

- se cuenta con el SDK .NET 8 real en `/home/daniel/.dotnet` o equivalente del entorno;
- el repositorio ya está en una línea base funcional previa a la migración;
- los feeds muertos, pins legacy y bloqueadores de bootstrap ya fueron tratados;
- la subcapa Mono.Addins y la dependencia Gtk# están aisladas como compatibilidad provisional y no como solución final.

## 4. Fase 3: separación del runtime Mono de la lógica del IDE

### Objetivo

Reducir el acoplamiento de la lógica funcional del IDE con Mono, Gtk# y APIs específicas del runtime heredado.

### Resultado esperado

La capa de dominio, servicios del IDE y flujo principal de compilación y edición no dependen de Mono para operar de forma básica.

### Trabajo por área

#### 4.1 Core del IDE

Objetivo: extraer la lógica reusable del runtime.

Tareas:
- [ ] Enumerar tipos y servicios que dependan directamente de `Mono` o `Gtk`.
- [ ] Identificar clases que encapsulen operaciones de sistema, archivos, entorno o ejecución de herramientas.
- [ ] Mover la lógica del dominio a una capa independiente del runtime.
- [ ] Dejar adaptadores en la capa de plataforma para I/O, shell y entorno.

Archivos clave:
- `main/src/core/`
- `main/src/core/MonoDevelop.Core/`
- `main/src/core/MonoDevelop.Ide/`

Aceptación:
- no hay referencias directas de la lógica principal al runtime Mono en los servicios fundamentales;
- el código funcional se ejecuta sin depender de la presencia de Mono como requisito de carga principal;
- la interfaz de plataforma queda encapsulada en una capa específica.

#### 4.2 Modelos de proyecto y resolución

Objetivo: separar la lógica de proyectos del runtime del sistema.

Tareas:
- [ ] Revisar proyectos, parsers, resolución y carga de soluciones.
- [ ] Aislar la lógica de MSBuild, proyecto y carga de add-ins de Mono-specific APIs.
- [ ] Crear adaptadores para lectura de archivos, rutas, metadata y resolución.
- [ ] Verificar compatibilidad con proyectos .NET modernos.

Archivos clave:
- `main/src/core/MonoDevelop.Projects.*`
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Templates/`
- `main/src/addins/MonoDevelop.DotNetCore/`

Aceptación:
- los modelos de proyecto pueden evaluarse sin depender de Mono;
- el comportamiento de resolución de rutas y proyectos sigue intacto bajo .NET 8;
- no se introducen dependencias nuevas a Gtk# en el core.

#### 4.3 Servicios de edición y análisis

Objetivo: aislar editor, convenios de código y análisis de APIs internas.

Tareas:
- [ ] Revisar la capa de convenios de código y comportamiento de edición.
- [ ] Mover la lógica de editor a un adaptador o shim local que no dependa de paquetes privativos de VS/Roslyn.
- [ ] Mantener una frontera clara entre servicio de edición y runtime del IDE.
- [ ] Mantener compatibilidad funcional con Roslyn público y paquetes no privativos.

Archivos clave:
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Editor/`
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Editor/CodingConventionsShim.cs`
- `main/src/addins/MonoDevelop.Refactoring/`

Aceptación:
- la lógica del editor no depende de código privado del stack Visual Studio;
- los shims se usan como adaptadores y no como capa de negocio real;
- la migración futura puede sustituir la implementación del shim sin tocar la lógica de edición.

#### 4.4 Pruebas y validación de desacople

Tareas:
- [ ] Ejecutar smoke tests del core.
- [ ] Validar carga de proyectos y compilación básica.
- [ ] Verificar que la eliminación de Mono no rompe la lógica central de IDE.

Criterio de cierre de la fase:
- la base funcional del IDE está separada del runtime Mono;
- queda clara la frontera entre plataforma, servicios y dominio;
- el siguiente bloque se puede migrar sin tocar la lógica de negocio.

## 5. Fase 4: migración del sistema de build

### Objetivo

Reemplazar la base heredada de `configure` + `xbuild` + `make` por un pipeline gestionado con `.NET SDK`/MSBuild moderno.

### Resultado esperado

El proyecto puede restaurar y compilar principalmente usando `dotnet build` y `dotnet msbuild` sobre una base moderna.

### Trabajo por bloque

#### 5.1 Propiedades globales y targets

Tareas:
- [ ] Revisar `main/MonoDevelop.props`.
- [ ] Revisar `main/Directory.Build.props`.
- [ ] Revisar `main/Directory.Build.targets`.
- [ ] Consolidar las propiedades de SDK y de resolución de referencia.
- [ ] Eliminar o aislar suposiciones heredadas de MSBuild clásico.

Aceptación:
- no quedan propiedades globales de MSBuild clásico que se encuentren en conflicto con `dotnet build`;
- las rutas de resolución de referencia se basan en propiedades explícitas y verificables;
- la estructura del build es entendible para agentes y reproducible en Linux.

#### 5.2 Proyectos legacy y SDK-style

Tareas:
- [ ] Identificar proyectos old-style que mezclan condicionales heredados con SDK-style.
- [ ] Resolver incompatibilidades en `Import` / `TargetFramework` / `ReferencePath`.
- [ ] Revisar proyectos con `TargetFrameworks.props` y `MonoDevelop.props`.
- [ ] Convertir proyectos de bloque clave a un formato uniforme y compatible con SDK.

Archivos clave:
- `main/external/mono-addins/`
- `main/src/core/MonoDevelop.Core/MonoDevelop.Core.csproj`
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.csproj`
- `main/msbuild/`

Aceptación:
- los proyectos principales ya no se basan en un modelo de build mixto incompatible;
- la construcción del árbol se vuelve reproducible con el SDK .NET 8;
- no se depende de rutas de sistema conflictivas ni de `xbuild` para la básica del IDE.

#### 5.3 Scripts del pipeline

Tareas:
- [ ] Revisar `scripts/configure.*`.
- [ ] Identificar las partes que aún dependen de Mono o de Csc clásico.
- [ ] Substituir la lógica vieja por una ruta compatible con SDK .NET 8.
- [ ] Mantener un modo fall-back solo para compatibilidad de entorno viejo.

Archivos clave:
- `scripts/configure.cs`
- `scripts/configure.sh`
- `scripts/configure.net8.csproj`

Aceptación:
- la configuración del build puede ejecutarse con SDK moderno;
- la generación de build variables es compatible con .NET 8;
- la ruta legacy queda como fallback y no como requerimiento principal.

#### 5.4 Validación del build moderno

Tareas:
- [ ] Ejecutar `dotnet msbuild` sobre las soluciones clave.
- [ ] Validar restauración, compilación y referencias de proyectos en Linux.
- [ ] Registrar errores de resolución y migrarlos a una prioridad de corrección.
- [ ] Repetir validación tras cada ajuste crítico.

Criterio de cierre de la fase:
- el repositorio puede compilar con `dotnet build`/`dotnet msbuild` en Linux;
- el flujo de build ya no depende de `configure`/`make` para la base técnica principal;
- se ha aislado el trabajo final de UI y el trabajo de runtime.

## 6. Fase 5: migración del runtime base a .NET 8 LTS

### Objetivo

Llevar el runtime principal del IDE desde Mono a .NET 8 LTS y mantener el comportamiento funcional base del IDE.

### Resultado esperado

La aplicación puede arrancar y ejecutar bajo .NET 8, con la lógica principal del IDE funcionando en Linux.

### Trabajo por bloque

#### 6.1 APIs dependientes del runtime

Tareas:
- [ ] Revisar uso de `System.Reflection`, `Environment`, `Path`, `AppDomain`, `AssemblyLoadContext`, `TypeLoadException` y APIs de runtime específicas.
- [ ] Identificar usos de APIs que dependan de Mono, AppDomain clásico o remoting.
- [ ] Sustituir por equivalentes compatibles con .NET 8.

Aceptación:
- no quedan dependencias de Mono en la capa principal del runtime;
- la carga de ensamblados se comporta acorde a .NET 8.

#### 6.2 Carga y resolución de add-ins

Tareas:
- [ ] Revisar la infraestructura de add-ins y carga dinámica.
- [ ] Verificar compatibilidad con runtime moderno.
- [ ] Ajustar resolución de dependencias y rutas de carga.
- [ ] Validar que los add-ins no requieren Mono clásico para inicializarse.

Aceptación:
- la plataforma de add-ins puede arrancar con .NET 8;
- la carga de extensiones es segura y verificable.

#### 6.3 Compatibilidad con proyectos y herramientas de compilación

Tareas:
- [ ] Revisar la interacción con MSBuild, compilación de solución y proyectos.
- [ ] Asegurar la compatibilidad de compilación con SDK moderno.
- [ ] Validar flujo de proyectos .NET actual y legados.
- [ ] Identificar qué parte del IDE requiere compatibilidad dual transitoria.

Aceptación:
- los proyectos y herramientas que forman la base del IDE siguen funcionando bajo .NET 8;
- el runtime moderno no rompe la capacidad de compilar ni de resolver add-ins.

#### 6.4 Validación funcional del runtime

Tareas:
- [ ] Ejecutar la aplicación en Linux bajo .NET 8.
- [ ] Probar escenario base de edición, carga de solución y build.
- [ ] Ejecutar smoke tests de arranque.
- [ ] Registrar diferencias funcionales respecto al stack heredado.

Criterio de cierre de la fase:
- la aplicación principal arranca bajo .NET 8 en Linux;
- la base funcional del IDE está operativa en el nuevo runtime;
- la capa de UI y la plataforma más específica quedan separadas del runtime principal.

## 7. Definición de terminado por fase

### Fase 3 - listo cuando:
- la lógica del IDE ya no depende críticamente de Mono;
- la plataforma queda separada por capas;
- los adaptadores de plataforma están identificados y aislados.

### Fase 4 - listo cuando:
- el build se ejecuta con .NET SDK moderno;
- no depende de `configure`/`make` ni de xbuild para la base principal;
- los proyectos clave compilan en Linux con el SDK real.

### Fase 5 - listo cuando:
- el IDE arranca sobre .NET 8;
- la capa funcional básica funciona en Linux;
- la arquitectura de UI y plataforma queda preparada para la etapa siguiente.

## 8. Riesgos principales

- Mezcla de proyectos old-style y SDK-style.
- Dependencias de AppDomain y runtime Mono ocultas.
- Referencias de paquetes internos de Roslyn/VS no públicos.
- Dependencias de sistema que solo existen en Mono/Gtk#.
- Penalización en pruebas de integración si la refactorización se hace sin validación por bloque.

## 9. Reglas de continuidad para otros agentes

Cuando otro agente continúe desde aquí, debe:

1. confirmar la fase activa;
2. validar el último bloque compilado;
3. corregir solo el siguiente cuello de botella detectado;
4. mantener evidencia de build y resultados;
5. no introducir funcionalidad nueva;
6. documentar decisiones de arquitectura junto al cambio.

## 10. Orden recomendado de ejecución

1. Fase 3: separar runtime Mono de la lógica principal.
2. Fase 4: unificar y modernizar el build.
3. Fase 5: migrar runtime base a .NET 8.
4. Luego: validación Linux-first, preparación de multi-OS y evaluación de UI.

## 11. Archivo de referencia

Este documento debe leerse junto con:
- `migration-plan.md`
- `migration-execution-plan.md`
- `phase-1-stabilization-plan.md`
- `ui-technology-proposal.md`

El objetivo es que cualquier agente pueda retomar la migración sin perder el contexto ni la intención general del plan.
