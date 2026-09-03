# Fase 1: estabilización de la base actual

## Objetivo

Dejar el repositorio en una línea base verificable y reproducible antes de iniciar la modernización de dependencias, del build y del runtime a .NET 8 LTS.

Se trata de estabilizar la infraestructura del proyecto, no de introducir nuevas funcionalidades. El alcance queda restringido a:

- repositorio y submodulos
- feeds y paquetes NuGet
- propiedades de build
- validación de restauración y compilación
- preparación para la siguiente migración de runtime

## Principios

- Linux es la prioridad del primer ciclo productivo.
- No se trabaja en nuevas funcionalidades ni en UX.
- La UI y la compatibilidad multi-OS se dejan para fases posteriores.
- Cada cambio debe tener evidencia de validación en repo local.
- La corrección de la base tiene prioridad sobre la mejora de rendimiento o limpieza estética.

## Estado actual verificado

Se ha comprobado que el repositorio está en una base heredada con los siguientes bloqueos reales:

1. Submodulos con URLs antiguas (`git://github.com`) y de repositorios no compatibles con el entorno moderno.
2. Feed de paquetes obsoleto (`dotnet.myget.org`, `www.myget.org`) que ya no es fiable o ni siquiera responde.
3. Dependencias de Roslyn heredadas, en concreto `Microsoft.CodeAnalysis.CSharp.Scripting` con versión `3.11.0-4.25056.4` y versiones de build nightly como `3.4.0-beta4-19568-04`, que no están disponibles en el ecosistema actual.
4. Tras limpiar los feeds muertos y pintear una versión pública de Roslyn, la restauración de la solución sigue bloqueada por paquetes legacy del stack de Visual Studio y Roslyn: `Microsoft.VisualStudio.CodingConventions`, `Microsoft.CodeAnalysis.Features`, `Microsoft.CodeAnalysis.Scripting.Common`, `Microsoft.CodeAnalysis.CSharp.Features`, `Microsoft.CodeAnalysis.VisualBasic.Features`.
5. La solución no está todavía en una línea base de restauración reproducible para una migración directa a .NET 8.

## Progreso actual

Se han corregido los puntos más evidentes:

- se normalizaron los submodulos a HTTPS,
- se retiraron los feeds muertos de MyGet,
- se reemplazó la versión Roslyn por una variante pública disponible en NuGet.org.

Sin embargo, el repositorio sigue requiriendo una capa adicional de compatibilidad con paquetes antiguos del editor/IDE de Visual Studio, por lo que la estabilización completa queda bloqueada en esta etapa y debe continuarse en la siguiente fase de modernización de dependencias.

## Entregables de la Fase 1

### 1. Repositorio y bootstrap reproducible

Objetivo:
- asegurar que el checkout del repositorio se pueda inicializar de forma fiable.

Tareas:
- revisar all submodulos listados en `.gitmodules` y normalizar URLs HTTP/HTTPS según corresponda
- ejecutar `git submodule sync --recursive`
- ejecutar `git submodule update --init --recursive`
- confirmar que la estructura de `main/external/` queda completa
- registrar cualquier submodulo que tenga dependencia de repositorios muertos o antiguos

Criterio de cierre:
- el repo queda inicializable sin errores de red ni de submodulo en esta máquina

### 2. Feed NuGet y restauración reproducible

Objetivo:
- dejar el repositorio en un estado donde `dotnet restore` no dependa de feeds muertos.

Tareas:
- revisar [NuGet.config](./NuGet.config)
- eliminar fuentes obsoletas de MyGet
- dejar solo fuentes activas y compatibles con la solución
- validar que todas las APIs de paquete se resuelven con feeds actuales
- documentar cualquier dependencia que deba moverse a un feed alternativo o a paquete local

Criterio de cierre:
- la restauración no falla por feeds muertos ni por URLs inválidas

### 3. Estabilización de Roslyn y versiones del build

Objetivo:
- resolver la dependencia heredada que bloquea la restauración actual de la solución.

Tareas:
- revisar `main/Directory.Build.props`
- revisar `main/msbuild/RoslynVersion.props`
- identificar cuál es la versión de Roslyn esperada por el proyecto
- determinar si la dependencia está apuntando a una build nightly o a un paquete no disponible en NuGet público
- decidir entre:
  - ajustar el pin de versión
  - mover a un feed compatible
  - introducir una dependencia reconstruida / reubicada
- documentar la decisión con justificación técnica

Criterio de cierre:
- la solución ya no falla por residuos de paquetes de Roslyn nightly no disponibles

### 4. Línea base de build y tests

Objetivo:
- dejar un estado operativo de referencia para continuar la siguiente fase.

Tareas:
- ejecutar `dotnet restore` sobre `main/Main.sln`
- si aún hay fallos, agruparlos por causa técnica y distinguir entre:
  - bloqueo de bootstrap
  - bloqueo de paquete
  - bloqueo de runtime
  - bloqueo de proyecto o de sintaxis heredada
- registrar lo que sí está validado y lo que queda pendiente
- dejar un punto de referencia para rollback y para el trabajo de la fase 2

Criterio de cierre:
- se dispone de una línea base con evidencias de restauración o fallos exactamente categorizados

## Riesgos principales

1. Dependencias de paquetes nocturnas o no publicadas que no tienen un reemplazo directo
2. Adicción a feeds no soportados por la red actual
3. Acoplamiento fuerte a Mono/Gtk# que puede ocultarse dentro de varios proyectos
4. Múltiples props globales heredadas que pueden estar forzando versiones incompatibles

## Decisiones de alcance

- No se toca la estrategia de UI.
- No se intenta una migración funcional de Mono a .NET 8 en esta fase.
- No se incorporan nuevas características.
- El objetivo es eliminar bloqueos de infraestructura y dejar una línea base para avanzar con seguridad.

## Criterio de cierre de la fase

La Fase 1 se considera concluida cuando se cumple lo siguiente:

- el repositorio inicializa correctamente con submodulos
- no quedan feeds muertos en la restauración
- la solución no falla por dependencias de paquetes obsoletos
- existe un estado de referencia documentado para la siguiente etapa de modernización
- el repositorio está preparado para entrar en la Fase 2 (modernización de dependencias)

## Tareas inmediatas recomendadas

1. Revisar `.gitmodules` y normalizar submodulos a HTTPS.
2. Revalidar `NuGet.config` y dejar un único origen activo compatible.
3. Ejecutar restore tras limpieza de feeds.
4. Aislar bloqueos de Roslyn y de props globales.
5. Registrar resultados y dejar evidencia para los agentes siguientes.

## Observación de ejecución

Esta fase debe mantenerse relativamente conservadora: el objetivo no es convertir aún el proyecto a .NET 8, sino preparar la plataforma de arranque para que el cambio sea posible y verificable.
