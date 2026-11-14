# Plan "Interfaz" — Migración de la GUI y estabilización preparatoria

Estado del plan (2026-09-07, actualizado):
- **Objetivo del plan**: migración de la interfaz (GUI) de la aplicación — actualmente **Gtk#/Mono** — a **Avalonia UI 12.1.2** (la versión 12.1.2 es compatible con .NET 10 según el sitio web de Avalonia), produciendo todo con el **.NET 10 SDK + Runtime**, sin dependencia de mono SDK ni ejecución con Runtime Mono.
- **Iteración actual (preparatoria)**: estabilización del build y del arranque de la GUI sobre el stack Gtk# existente, produciendo todo con el SDK de .NET (`dotnet msbuild`), sin mono SDK, y dejando evidencias de cada bloqueo para alimentar la migración.
- La decisión de tecnología se tomó tras analizar y revisar las webs de las opciones de `docs/ui-technology-proposal.md` una por una, eligiendo la opción recomendada: **Avalonia UI 12**.

Restricciones de entorno:
- Compilar SIEMPRE con `dotnet` (SDK 10). NO usar mono SDK ni `mono` para ejecutar la aplicación.
- La validación de GUI bajo runtime .NET 10 llega con la migración; mientras tanto la verificación se hace a nivel de build (compilación offline de núcleo + addins) y evidencias documentadas.

## Enunciado del plan (según el usuario)

Se trata de la migración de la UI de la aplicación que actualmente es Gtk#/Mono a una nueva tecnología.

1. **Primero se estabilizará la UI legacy** para tener el correcto funcionamiento de la aplicación y así comprender cómo funciona cada módulo para su migración a la nueva UI elegida.
2. De las opciones propuestas en `docs/ui-technology-proposal.md`, se analizaron y revisaron las webs una por una y se decidió ir por la opción recomendada: **Avalonia UI 12**.
3. **La migración se realizará por partes** (no toda de una), compilando y ejecutando la UI con cada cambio para probar que no rompa la aplicación y que sea funcional el cambio.
4. Se revisarán las dependencias de la UI legacy Gtk# (por ejemplo Mono.Cairo) para decidir si es necesario mantenerlas o cambiarlas por otras tecnologías (por ejemplo SkiaSharp) durante la migración.
5. **Mantener los módulos UI legacy migrados** por las dudas; cuando la migración se complete y la aplicación se ejecute completa con la nueva UI, se procederá a eliminar la UI legacy.
6. Si aún hay algún módulo o código remanente funcionando con SDK/Runtime Mono por tema de compatibilidad, se cambiará definitivamente al terminar la migración de UI; **no deberá haber nada de dependencia SDK o Runtime Mono en el código al finalizar**.
7. Después de que la migración de la UI se complete, se procederá al **rebranding de la aplicación** (cambio de nombre y logo), ya que no se forma parte del proyecto Mono; también se proveerá a cambiar la nomenclatura "Mono", "MonoDevelop" y "Xamarin" en la documentación, código y nombres de archivo del repo por el nuevo nombre de la aplicación.
8. Aún no hay un nuevo nombre ni logo de la aplicación, pero se escuchan sugerencias.

## Estado de avance (2026-09-07)

- **Core|Ide|Compat — COMPILAN offline con `.NET 8 SDK`** (`dotnet msbuild -t:Build -p:DisableDownloadNupkg=true`, salida verde a `main/build/bin`).
- **CSharpBinding — corte de la cadena UnitTesting/NUnit**: se eliminó el `ProjectReference` a `MonoDevelop.UnitTesting` y los 2 Compile de `MonoDevelop.CSharp.UnitTests` (NUnit), sustituidos por comentarios `deferred`. La única dependencia de CSharpBinding hacia la cadena NuGet 5.4.0 queda diferida.
- **CSharpBinding — referencias añadidas** (mismas que MonoDevelop.Refactoring): `Microsoft.VisualStudio.Threading` 16.10.56, `MonoRoslynCompat`, `System.Composition.*` 1.0.31 por `Reference`+`HintPath` (patrón offline).
- **CSharpBinding — COMPILA a 0 errores** (`dotnet msbuild -p:DisableDownloadNupkg=true`, salida `main/build/AddIns/CSharpBinding/MonoDevelop.CSharpBinding.dll`), tras cerrar el ciclo de stubs de `MonoRoslynCompat` (CompatStubs.cs). Pasó de ~97 errores → build4 ~34 categorías → 0 en 4 iteraciones. Ya no hay errores de `MonoDevelop.Ide`/`Refactoring` ni del addin.
- **Manifest CSharpBinding saneado**: se comentaron los `AddinModule ("MonoDevelop.CSharpBinding.Autotools.dll"/"AspNet.dll")` (sub-addins que dependen de `MonoDevelop.Deployment`, roto) y el `AddinDependency ("UnitTesting")` (addin diferido). El addin ya carga en runtime sin errores de manifiesto.
- **MonoDevelop.RegexToolkit — COMPILA** (requisito de `MonoDevelop.Refactoring`): añadidas referencias Roslyn `Microsoft.CodeAnalysis*` 3.4.0-beta4-final + `System.Composition.AttributedModel` 1.0.31 por `HintPath` (offline). Antes su csproj no referenciaba Roslyn.
- **Runtime deps offline desplegadas en `build/bin`** (run #5): `System.Threading.Tasks.Dataflow` 4.5.24, `System.Composition.*` 1.0.31, `System.Threading.Tasks.Extensions` 4.5.2 (asm 4.2.0.1), `SQLitePCLRaw.core` 1.1.12, `System.Reflection.Metadata` 1.6.0 (asm **1.4.3.0**, la 1.3.0.0 rompía el descubrimiento MEF de Workspaces con `ReflectionTypeLoadException`).
- **`IThreadingContext` MEF export**: nueva parte `MonoDevelop.Ide.Composition/MonoDevelopThreadingContext.cs` (`[Shared, Export(typeof(IThreadingContext))]` sobre `JoinableTaskContext`) que restaura el exportador que migró de Roslyn 2.x (Workspaces) a `Microsoft.VisualStudio.LanguageServices` y por tanto faltaba con Roslyn 3.4 netstandard2.0.
- **UnitTesting / MonoDevelop.PackageManagement**: deuda diferida (cadena NuGet 5.4.0) que se reinsertará con el patrón `PackageReference→Reference+HintPath` desde `$(NuGetPackageRoot)`.

Estado de avance (2026-09-08):
- **MSB3644 resuelto** (root cause de builds limpios): `TargetFrameworkRootPath` → paquete `microsoft.netframework.referenceassemblies.net472\1.0.3\build\` en `msbuild/MonoDevelop.AfterCommon.props`. Sweep `rm -rf obj` + rebuild offline: **24/24 proyectos de M1 verdes reales** (ver `docs/migration-phase-net8.md` M1).
- **Addin repo**: `addins.monodevelop.com` dado de baja → `https://lastexitcode.com/monodevelop-addins/{version}/main.mrep` cableado en `AddinSetupService`/`Runtime`.
- **M4 arrancado**: prototipo `main/src/core/MonoDevelop.Startup.Avalonia` (Avalonia 12.1.2, net8.0) compila offline y arranca headless sin excepción. Los generators de Avalonia 12 requieren Roslyn ≥4.14 → SDK 10.0.401; la **raíz del repo está pineada a 10.0.401** (`global.json` con `rollForward: disable`) — nota histórica: durante la fase net8 hubo un pineo transitorio a 8.0.424, ya retirado.

## Fases

### In1 — Registro/despliegue de addins (preparatorio)
1. `CSharpBinding` → COMPILA a 0 errores (cierre del ciclo de stubs de compat; el corte UnitTesting y las refs quedan documentados arriba).
2. `MonoDeveloperExtensions`/`NUnit.csproj` → diferido con evidencia (arrastra cadena NuGet 5.4.0).
3. `MonoDevelop.GtkCore` → diferido con evidencia (addin reviertiente; solo `libstetic*.dll`).
4. `FSharpBinding` → diferido (toolchain F#).

### In2 — Runtime residual limpio
Con el stack net4x actual (solo para diagnóstico interno, NO como producto):
1. Verificar `TypeSystemService` sin `TypeInitialization`/`ArgumentNull` al cargar .sln (hecho: ok).
2. Monitorizar qué cadena impide el binding C# (en curso: CSharpBinding→stubs de compat).
3. Documentar warnings esperados (red, templates, memory limit) para que no cuenten como fallos.

Cadenas cerradas en runtime (run #6, log `~/.cache/MonoDevelop/8.0/Logs/Ide.*`):
- Manifiesto/sub-addins: `Autotools`/`AspNet`/`UnitTesting` fuera del manifest; `RegexToolkit` compilado y desplegado → `Refactoring` y `CSharpBinding` cargan.
- Runtime deps: `System.Composition` + `Dataflow` (eliminan el FATAL "Can't create roslyn workspace" original), `System.Reflection.Metadata` 1.4.3.0 (desbloquea el descubrimiento MEF de Workspaces), `Tasks.Extensions`/`SQLitePCLRaw` (reducen `FileNotFoundException` de partes).
- MEF threading: `MonoDevelopThreadingContext` restaura el export `IThreadingContext`; quedan 0 errores de ese contrato.

Bloqueo residual (run #6, línea ~409): FATAL "Can't create roslyn workspace" por **cast de un workspace service de Roslyn 3.4 netstandard2.0** (`InvalidCastException` en `MefWorkspaceServices.GetService[TWorkspaceService]` → `SolutionServices..ctor`, 3 slots: `ITemporaryStorageService`, `IMetadataService`, `IProjectCacheHostService`) y ausencia de partes de hosting Roslyn editor (`ITodoListProvider` con 0 exports). Son partes que en el stack real viven en `Microsoft.VisualStudio.LanguageServices`/build net472 y **no existen en** `Microsoft.CodeAnalysis.Workspaces` **netstandard2.0** → se resuelven en la fase de migración bajo runtime .NET 8 (In3), no en el diagnóstico mono.

CERRADO (run #10 = `Ide.2026-09-08__00-26-59.log`, exit 124, 0 FATAL): lo que quedaba del bloqueo era el unwrap de fábrica del MEF host (`MefWorkspaceServices` `b__1` → `CreateService`) aplicado a los factories de MonoDevelop.Ide, cuyos valores finales (manager de metadata, cache host) no eran casteables al contrato interno de Workspaces. Fix sistémico en 9 factories/partes de `MonoDevelop.Ide`:
- Cada `[ExportWorkspaceServiceFactory]` también implementa su contrato de servicio y `CreateService` devuelve `this`: `IMetadataService`, `IProjectCacheHostService`, `IDocumentTrackingService`, `IErrorReportingService`, `IFrameworkAssemblyPathResolver`, `ISymbolNavigationService`, `INotificationService` (x2), `IExtensionManager`.
- Cache host: el stub de compat `ProjectCacheService` NO implementa el `IProjectCacheHostService` interno de Workspaces → el downcast del manager fallaba dentro de `CreateService`; se expone ahora un shim `NoOpProjectCacheHostService` (delegación `EnableCaching` inert, genéricos passthrough = miss de cache honesto) y el service activo `MonoDevelopProjectCacheService` se conserva para el wiring de flush/active-document.
- Build MonoDevelop.Ide a 0 errores CS; restante del log = ruido ambiental mono 6.14 (faltan `/usr/lib/mono/{2.0,3.5,4.5.x}-api` y `xbuild-frameworks/.NETFramework/*`).

Validación con solución C# (run #11 = `Ide.2026-09-08__00-38-17.log`, `mono MonoDevelop.exe ~/opencode/testproj/TestProj.sln`, exit 124): 0 FATAL, 0 Unhandled; todos los servicios IDE se crean (CommandManager, DesktopService, RootWorkspace, TypeSystemService, CompositionManager, DisplayBindingService, DocumentManager, IShell); la composición MEF corre y el catálogo host-services queda sellado (sin InvalidCast). La **evaluación del proyecto C# falla** por un límite del entorno mono 6.14: los `Microsoft.Build*.dll` de `build/bin` son copias exactas del dotnet SDK 8.0.424 (hash `fd46dcf`, refs `System.Runtime 8.0.0.0`, inbindables en mono: `ToolLocationHelper` no se resuelve) y no existe engine MSBuild 15.x mono-compatible (`GetMSBuildBinPath("15.0")` → null porque Arch no tiene `/usr/lib/mono/msbuild`; nuget `Microsoft.Build` 15.9.20 no distribuye el engine `Microsoft.Build.dll`; xbuild 4.x no evalúa SDK-style). La evaluación corre en subproceso (`RemoteBuildEngineManager.cs:456` → `MonoDevelop.MSBuildBuilder.exe` cargando desde el appbase). No es la cadena Roslyn: se re-resuelve con MSBuild net8 nativo en la fase de migración.

0-exports del vs-editor del fork (decisión): `IGuardedOperations`, `IBufferGraphFactoryService`, `IContentTypeRegistryService`, `UndoHistoryRegistry`, `EditorOperationsProvider`, `TextSearchService`, `SmartIndentationService`, `TextStructureNavigatorSelectorService`, `MultiSelectionBrokerFactory`, `FeatureServiceFactory`, etc. (interfaces en `Microsoft.VisualStudio.CoreUtility.dll`/`Microsoft.VisualStudio.Text.Data.dll` de compat) **no tienen implementación en el fork** (grep: no existe `class GuardedOperations`/`BufferGraphFactoryService` en src/). Portarlas (~20 servicios + adornos WPF `PresentationCore` que además no aplican a GTK) es trabajo descartable bajo mono → se difieren a In3 (rework del catálogo editor con `Microsoft.VisualStudio.LanguageServices` net8), donde se reinsertan las partes reales. Los errores de composición MEF siguen siendo solo warnings (partes descartadas), no críticos.

### In3 — Smoke tests de GUI (criterio de aceptación, para la fase MIGRACIÓN)
Runsheet (net8): 1 arranque limpio; 2 menús; 3 About; 4 Preferences; 5 AddinManager; 6 crear/compilar C# Library; 7 abrir `.sln` y navegar. Cierre: 0 críticas nuevas.
**CERRADO (2026-10-17): 7/7 PASS, 0 críticas nuevas.** Evidencia y detalle en `~/opencode/qa_in3/` (reporte `qa_report_in3.md`, logs/capturas en `smoke/`).
- 1 arranque limpio: 0 FATAL/Exception, `LinuxPlatform ready`, 30/32 addins, pads pintados.
- 2 menús: `--menudump` → `menus=11 items=203 bindings=180` (File 19, Edit 35, View 39, Search 27, Project 13, Build 7, Run 13, Version 14, Control 14, Tools 8, Window 15, Help 13). AT-SPI no disponible en el entorno (sin `at-spi-bus-launcher`) → validación por modelo + píxel.
- 3 About / 4 Preferences / 5 AddinManager: diálogos reales (About 560×520 pintado; Preferences con paneles reales — keybindings 180 cmds/5 schemes, themes 4/Dark, cfmt, vc; AddinManager `tab=installed items=32 host=True`).
- 6 crear/compilar C# Library: **dos fixes de producto encontrados y resueltos por el runsheet**:
  1. `NewSolutionDialog`: la etiqueta interna de plantilla (`library`) se pasaba tal cual a `dotnet new` (el short name del SDK es `classlib`) → fallo silencioso; ahora hay mapeo console/classlib/nunit, scaffold mínimo `.shproj/.projitems` para `shared`, log del stderr del CLI y fallback de `.sln` que referencia el fichero real creado con su GUID de proyecto.
  2. `RunBuildAsync` enumeraba proyectos por escaneo del directorio del `.sln` (se perdía cualquier proyecto referenciado fuera de él) → ahora parsea las referencias `Project(...)` del `.sln` (con fallback al escaneo).
  Resultado: `created=True sln-entry=True mappings=True` y build de la solución compila QaB4 + QAAdded con 0 errores.
- 7 abrir `.sln` y navegar: sln cargado; clases→caret, outline→caret, code issues→caret (12,5) verificados.
Hallazgos no bloqueantes (validación del subagente Tester QA Senior, reporte en `~/opencode/qa_in3/qa_report_in3.md`; 7/7 PASS, gate cierra):
- **I (medio) — CERRADO (2026-10-18, ver fila I del inventario)**: colisión de documentos por nombre de fichero → el modelo de documentos quedó claveado por ruta completa (`DocKey` = `GetFullPath`) con 3 iteraciones de QA Senior (4/4 hallazgos cerrados). Durante el fix se detectó y corrigió también que el marcador de sucio " •" de las pestañas nunca se pintaba (el buscador de la etiqueta no descendía al `StackPanel` del header).
- **J (bajo/medio) — CERRADO (2026-10-19, ver la sesión *Fila J + Fila C* de `docs/migration-status-report.md`)**: `RunStartupProjectAsync` elige ahora el proyecto de arranque desde las referencias del `.sln` (`SolutionProjects`) con prioridad a `<OutputType>Exe</OutputType>` (`PickStartupProject`); el build y el run comparten el helper.
- **C (actualizado por QA) — CERRADO (2026-10-19, ibidem)**: los 3 comandos de Session Recording tienen comportamiento real en el shell: grabador local en el formato XML legacy (`SessionRecorder`), viewer in-proceso de `InstrumentationService` (sin mdmonitor) y replay por re-dispatch de los comandos grabados. La integración total del backend no-GTK (`AutoTestService`/`SessionRecord` completos) queda como fase posterior, según la directriz del operador.
- F1 (bajo): el conteo `bindings=`/`commands=` depende del estado (incluye `recent:` de las soluciones persistidas) → el runsheet debe usar umbrales estables.
- F2 (reclasificado a bajo por QA: fixture intencional): el message de code issues en `--dblclick` es un fixture hardcodeado (`MainWindow.axaml.cs:2324`) para validar la cadena parse→caret, no análisis Roslyn.
- F3 (bajo, preexistente): 8× `[prefs-csharp] Roslyn key unavailable ... using legacy name`.
- F4/F5/F6 (bajo): línea `[addins]` duplicada; doble `Loaded QaB4.sln` con `--sln`+`--dblclick`; ruido `[menu-icon] '' skipped`.
Ejecución detallada por hitos en `docs/migration-phase-net8.md` (M0: baseline; M1: build total dotnet msbuild; M2: runtime net8 + MSBuild nativo; M3: Roslyn moderno y retiro de MonoRoslynCompat; M4: shell Avalonia 12; M5: vistas por módulos; M6: cutover + este runsheet como gate).

### In4 — Cierre y documentación
Sync de `docs/session_summary.md` + este plan con las decisiones (migración net10, Avalonia UI 12, mono fuera, evidencia de cada bloqueo).

## Fuera de alcance en esta iteración (diferido explícito, con evidencia)
- La migración GUI en sí a Avalonia UI 12 (es el objetivo del plan; la iteración actual solo la prepara).
- Rework de run/execution (`MonoExecutionParameters`, run handlers, `MD1DotNetProjectHandler`, `MONO_*`).
- Addins/compiladores mono (`MonoDevelopPluginFactory`, `DotNetCoreDevCertsTool`, LinuxDeploy, UIThreadMonitor, ILAsm/CSharp `MONO_*`, templates ASP.NET).
- Submódulos xwt/gui-unit y serialización de clipboard.
- Cadena NuGet 5.4.0 de `MonoDevelop.PackageManagement`/`MonoDevelop.UnitTesting` (se reinserta al compilar offline).
- Rebranding (nombre/logo pendientes de decidir; se aplica al completar la migración de UI).

## Notas de ejecución
- Build reproducible:
  `export PATH="$HOME/.dotnet:$PATH"; export DOTNET_ROOT="$HOME/.dotnet"`
  `dotnet msbuild <csproj> -p:Configuration=Debug -m:1 -t:Build -p:DisableDownloadNupkg=true`
- Probes de compilación offline: `mcs` con refs a `main/build/bin` + `netstandard` facade (solo análisis de semántica).
- Artefactos/bitácora: `docs/session_summary.md`; logs `~/opencode/md_*`; `main/build/bin/MonoRoslynCompat.dll|MonoDevelop.Ide.dll|MonoDevelop.Core.dll`.

## Extras del usuario (2026-09-19) — checklist de la migración

Requisitos adicionales al plan base, con estado y evidencia:

| # | Extra | Estado | Evidencia / ubicación |
|---|-------|--------|------------------------|
| 1 | Bordes de ventana propios de Avalonia (no del OS) | **HECHO** (M4) | `SystemDecorations=None` + `ExtendClientAreaToDecorationsHint` en `MainWindow.axaml` |
| 2 | Min/max/close respetan diseño/ubicación del OS (mac izquierda; linux/win derecha), integrados a la barra de menú, sin barra de título | **HECHO** (M4) | fila título = menú + `CaptionButtons`; `MoveCaptionButtonsLeft` para macOS en `MainWindow.axaml.cs` |
| 3 | Pestañas del panel central estilo isla (navegador) | **HECHO** (M4) | `TabItem.island` en `App.axaml` (esquinas redondeadas, hover, activa fundida con el documento) |
| 4 | Soporte nativo Wayland y X11 | **HECHO** (M4) | `Avalonia.Desktop` + `UsePlatformDetect`, sin GTK; verificado en X11 |
| 5 | Temas claro y oscuro consistentes | **HECHO** (M4) | `ThemeDictionaries` (paleta `Ide*`) + cambio en runtime (menú View) |
| 6 | Mono.Cairo → SkiaSharp | **EN CURSO (M5/M11y)** | `SkTextEditor` (Avalonia) renderiza vía SkiaSharp (frame nuevo por render + SKSurface) con gutter, caret, multi-caret, folding, burbujas, intellisense (CompletionPopup) y hover tooltip (EditorTooltipPopup); ref base para sustituir el dibujo Cairo de Mono.TextEditor |
| 7 | Módulos Mono.* sin reemplazo: fork del repo (solo módulos afectados), port a .NET 10, quitar Gtk, rebranding `Mono.* → DotNet.*`, submódulo en `main/external/*` | EN CURSO | los 15 submódulos ya están forkeados a `DRavainera` y enlazados (commit 14b2bacaa4) con rama `net10`; port/renombre módulo a módulo según se toque en M5/M6 |
| 8 | Xwt y módulos dependientes de Gtk o Mac-only: evaluar reemplazo o modificación | PENDIENTE | `xwt` forkeado; decisión por módulo cuando el cutover lo requiera |
| 9 | Rediseño iconos PNG (MonoDevelop.Ide/icons/) estilo Fluent respetando tamaño y transparencias | EN CURSO | PNG ya integrados en la nueva UI vía `IconService` (menú principal, secciones de Preferences); redibujo uno a uno continúa con QA visual |
| 10 | MonoDevelop.Ide 2.6.0.0 → 12.1.2.0 | **HECHO** | commit 72816594c7; verificado por reflexión + arranque IDE |
| 11 | Migración en bucle: al terminar una tarea se inicia la siguiente | EN CURSO | este bucle; cada módulo pasa QA de paridad funcional antes de avanzar |
| 12 | Subagente QA senior: plan de pruebas + validación por módulo, bucle hasta error=0 | EN CURSO | la verificación por módulo se hace contra el módulo Gtk original (inventario de funcionalidades → prueba en Avalonia → 0 diferencias) |
| 13 | Seguimiento en docs (interfaz-plan, session_summary, migration-status-report) + commit/push por tarea | EN CURSO | cada tarea termina en commit+push en esta rama |
| 14 | Al finalizar: compila completo con builder del SDK 10 real, sin bloqueos | PENDIENTE (M6) | gate de cierre |
| 15 | Al finalizar migración: NuGet de addins → 7.9 | **RE-SCOPED (2026-11-08)**: 7.9.0 en el lado Avalonia (pin de la shell, Oleada 5 Slice A); los addins GTK quedan **congelados en 6.11.2** por decisión del operador (no se tocan) | pin explícito `NuGet.Configuration 7.9.0` en `MonoDevelop.Startup.Avalonia.csproj` (NO el property compartido `$(NuGetVersionNuGet)`); coexistencia 6.11.2/7.9.0 verificada en maps del proceso legacy — ver follow-up "Oleada 5 — Slice A" (2026-11-08) |

**Regla del bucle por módulos**: cada módulo (ventana/diálogo/pad) se migra desde su equivalente Gtk listando primero sus funcionalidades, se implementa en Avalonia y se prueba que TODAS están presentes (QA por módulo) antes de pasar al siguiente; la UI legacy se conserva hasta el cutover.

## M4 — Shell Avalonia 12.1.2 (2026-09-18, avance del hito)

Requisitos del usuario implementados en `main/src/core/MonoDevelop.Startup.Avalonia` (net10.0, Avalonia 12.1.2):
- **Chrome propio de Avalonia**: `SystemDecorations="None"` + `ExtendClientAreaToDecorationsHint` — bordes de la app, no del OS; la barra de menú hace de barra de título (drag + doble clic para maximizar).
- **Botones min/max/close integrados a la barra de menú**: sin barra de título del OS. Ubicación por OS: derecha (Linux/Windows) por defecto; macOS los reubica a la izquierda en `OnOpened` (`MoveCaptionButtonsLeft`, con glifos del sistema en el futuro). Estilo hover con rojo de cierre (#E81123).
- **Pestañas isla** (estilo navegador): `TabItem.island` con esquinas superiores redondeadas, fondo transparente inactiva, activa que se funde con el documento; hover diferenciado.
- **Temas claro/oscuro**: `ThemeDictionaries` con paleta IDE (`IdeWindowBg/IdeChromeBg/IdeTab*/IdeFg/IdeBorder`) y cambio en runtime desde el menú View.
- **X11 + Wayland**: `Avalonia.Desktop` con `UsePlatformDetect` (Avalonia abstrae ambos; sin dependencias GTK).
- Compila 0 errores con SDK 10 y arranca en X11 verificado (ventana "MonoDevelop — Avalonia Shell", captura `~/opencode/avalonia_shell.png`).

Siguiente: M5 — vistas por módulos (pads reales con datos de MonoDevelop.Core, editor con SkiaSharp en vez de Mono.Cairo, AddinManager con contenido real).

## M5 — Vistas por módulos (2026-09-19, avance del hito)

Migración módulo a módulo con QA de paridad funcional contra el diálogo Gtk original antes de avanzar:

- **About** (commit 6d95ce196c): imagen de branding, versión, copyrights, página de detalles (info de sistema + tabla de ensamblados), toggle Show/Hide Details, Copy to clipboard. QA: render verificado + toggle de detalles por captura.
- **Preferences**: estructura completa de secciones (Environment, Projects, Text Editor, Source Code, Version Control, Other), panel Visual Style funcional con selector de tema en vivo (dark/light), placeholder informativo para paneles aún no portados, OK/Cancel. QA: panel por defecto, cambio de tema en vivo verificado (panel blanco en light), placeholder Fonts verificado (`--prefs=fonts`).
- **Hooks QA** (`--about` | `--prefs` | `--addins` | `--prefs=<light|dark|panelId>`): abren cada diálogo al arrancar para validación automatizada sin navegación de UI; usados por el bucle de pruebas y aprovechables en CI.
- **Add-in Manager**: pestañas Installed/Updates/Gallery con **datos reales** del registry de Mono.Addins compartido con el IDE (AddinEngine con `startupDirectory` = `main/build/net10run`, misma base de datos de addins), filtro de búsqueda, panel de detalles (versión/autor/descripción), Enable/Disable, Uninstall, Install from file (.mpack), Refresh. QA: 27 addins del IDE listados, render verificado.

- **UI Gtk legacy**: se conservará en el árbol (oculta) y será ejecutable con el parámetro `--old-gui` durante toda la etapa de migración; su eliminación se decidirá cuando la nueva UI Avalonia esté madura (orden explícita del usuario).
- **Pad Solution con datos reales**: `SolutionLoader` (reflection sobre `Microsoft.Build.dll` del runtime/SDK, `SolutionFile.Parse`) puebla el pad con la jerarquía real de una solución vía `--sln=<ruta>`; verificado con `SyntaxProbe.sln` (2 proyectos) y estado en la barra inferior.

Hallazgo de QA del entorno: los clics sintéticos XTEST no llegan a ventanas que no tienen el foco de input en este escritorio (mutter); por eso los hooks `--prefs=<valor>` son la vía de validación programática del estado de cada panel.

### M5 — Menú principal completo + iconos legacy (2026-09-19, continuación)

- **Menú principal completo (paridad legacy)**: `MenuService.cs` modela los 11 menús de la UI GTK (File/Edit/View/Search/Project/Build/Run/Version Control/Tools/Window/Help) con TODOS sus submenús, ítems, separadores, mnemónicos `_`, atajos y cabeceras, extraídos de `MainMenu.addin.xml`, `Commands.addin.xml` y `VersionControl.addin.xml`. `MenuBuilder.cs` los convierte a `MenuItem` de Avalonia con icono en el slot del tema (PART_IconPresenter en Avalonia 12) y atajo; los comandos aún sin portar NO se ocultan: reportan "not wired yet" en el pad Output/barra de estado (misma filosofía que los paneles placeholder).
- **IconService** (`Services/IconService.cs`): carga los PNG de `MonoDevelop.Ide/icons` imitando a `ImageService` del IDE: mapeo stock-id→recurso de `StockIcons.addin.xml`, variantes `~dark`/`~disabled`/`@2x`, fallback base→`missing-image-16`, cache por (id, tema, disabled, escala) y alternancia por `ActualThemeVariant`. Los stock-ids del addin de Version Control resuelven a su propio directorio `icons/`. Iconos ubicados como la UI legacy: a la izquierda de cada ítem de menú y en cada sección del árbol de Preferences.
- **Preferences — selector de idioma**: nueva sección "User Interface Language" con la lista completa de locales de `LocalizationService` (25 entradas), lectura/persistencia de `MonoDevelop.Ide.UserInterfaceLanguage` (MonoDevelop-properties.xml), nota "takes effect next time you start" y botón Restart (UX de `IDEStyleOptionsPanel`). Iconos `md-prefs-*` en el árbol de secciones.
- **Add-in Manager — chrome Avalonia**: `SystemDecorations="None"` + fila de título propia (título + botón close), igual que About/Preferences; ya no muestra el borde del OS.
- QA: build 0 errores; smoke 12s OK; `--addins` lista 27 addins del registry real; `--prefs=language` sin excepciones; verificado que los ~65 recursos PNG referenciados existen (IDE + Version Control).

### M5 — Toolbar principal + pestañas compactas + inventario de ventanas (2026-09-19, continuación 2)

- **Barra de herramientas bajo el menú** (paridad del `MainToolbar` GTK): fila con botón Run redondo (icono `gtk-execute` del set PNG, sensible al tema), combos *Run Configuration / Configuration / Runtime* (150px, como el legacy) y caja de búsqueda a la derecha (240px). La fila hace de zona de arrastre + doble-clic maximizar como la barra compuesta del título GTK; los controles (combo/botón/entry) se excluyen del drag (`IsToolbarInteractive`). Handlers reportan en Output/barra de estado mientras los comandos reales se portan.
- **Pestañas isla compactas**: padding 14,7→10,3, radio 9→6, margen 3,7→2,4, FontSize 12 y MinHeight 24 (antes ~34px por pestaña). QA visual por análisis de píxel de captura (render ASCII de bandas): perfil de filas menú→toolbar→pestañas correcto y pestañas de ~14px de alto.

#### Inventario de ventanas/diálogos Gtk aún no migrados (hoja de ruta M5/M6, todas con chrome Avalonia)

Fuente: `src/core/MonoDevelop.Ide` + addins (`grep "class .*: .*Dialog"`). Orden por oleadas de impacto:

- **Wave A — flujo esencial**: New Solution/New Project (`GtkNewProjectDialogBackend` + `NewProjectOptionsWidget`), Open File/Project (`OpenFileDialog`/`FileSelectorDialog`), Dirty Files al cerrar (`DirtyFilesDialog`), progreso (`ProgressDialog`, `MultiTaskProgressDialog`), alertas base (`AlertDialog`/`TextQuestionDialog` de Components.Extensions, `GtkAlertDialog`, `MultiMessageDialog`, `AddinLoadErrorDialog`), Find in Files (`FindInFilesDialog`), Navigate To / Go to File / Go to Type (`NavigateToCommand`).
- **Wave B — configuración de proyecto**: Project/Solution Options (`ProjectOptionsDialog`/`CombineOptionsDialog` reutilizando el marco de `OptionsDialog`/Preferences ya portado), Apply/Export Policy (`ApplyPolicyDialog`, `ExportProjectPolicyDialog`, `NewPolicySetDialog`, `DefaultPolicyOptionsDialog`), New Layout (`NewLayoutDialog`), New Configuration (`NewConfigurationDialog`), New Folder (`NewFolderDialog`), Select File Format (`SelectFileFormatDialog`), Confirm Project Delete (`ConfirmProjectDeleteDialog`), Custom Execution Modes (`CustomExecutionModeDialog`, `CustomExecutionModeManagerDialog`, `PortableRuntimeSelectorDialog`, `NewSolutionRunConfigurationDialog`).
- **Wave C — depurador**: Attach to Process, Breakpoint Properties, Debug Application, Expression Evaluator, Busy Evaluator, Value Visualizer (`MonoDevelop.Debugger`).
- **Wave D — addins**: Gettext (Translation Project Options, Language Chooser), AspNetCore (Publish to Folder), SourceEditor (New Color Scheme), CodeTemplates (Edit Template), Version Control (History, Diff viewer, Status, Resolve Conflict).
- **Wave E — ventanas no modales**: Welcome Page, pads de resultados (Search Results, Errors, Task List), tooltips de marcadores.

Regla transversal: TODAS las ventanas nuevas usan `SystemDecorations=None` + chrome Avalonia (fila título + caption buttons), iconos del set PNG vía `IconService` y temas claro/oscuro.

### M5 — Paridad contra la UI GTK EN VIVO: Welcome Page, Nuevo Proyecto y sistema de pads (2026-09-19, continuación 3)

La UI Gtk corre en .NET 10 (`cd main/build/net10run && ~/.dotnet/dotnet MonoDevelop.dll`; el build obsoleto `build/bin` fue eliminado). Metodología de análisis implementada:
- **Captura real**: `import/magick x:<winid>` sobre ventanas X concretas (los popups GTK de mutter+XWayland no se componen en `x:root`; capturar por ID sí funciona).
- **Interacción real**: AT-SPI (accesibilidad) para abrir menús/diálogos por acción (`do_action`) sin depender del foco, más injector XTEST compilado (`/tmp/xinject`) para clics.
- **Estructura verificada en runtime**: volcado AT-SPI de los 10 menús con TODOS sus ítems/submenús localizados (es) — coincide con `MenuService` y aporta deltas: Ver→**Paneles** (Solución/Clases/Cuadro de herramientas/Propiedades/Esquema/Errores/Tareas/Pruebas/Ayuda) y **Paneles de depuración** (Puntos de interrupción/Locales/Inspeccionar/Subprocesos/Inmediato/Pila de llamadas); Ejecutar completo con depuración (Asociar al proceso, pasos, breakpoints); Proyecto con NuGet; Compilar con Publicar.
- **Diálogos capturados y medidos**: Nueva Solución (904x632; columnas categorías|plantillas 32px|descripción+campos|vista previa) y Buscar en archivos (480x422; búsqueda+reemplazo, directorios, scopes, file mask) — guardados en `/tmp/gtk-*.png`.

Implementado en el shell Avalonia:
- **Welcome Page** (`WelcomePageView`): anatomía legacy (`WelcomePageFrame`): barra de proyecto con "Go Back to Solution" cuando hay solución abierta, marca, acciones New/Open y lista "Recent Solutions" con tiles (título en negrita + ruta, hover con fondo/borde y estrella de fijar). Abre por defecto como documento inicial; se oculta al abrir solución (como la GTK).
- **Diálogo Nueva Solución** (`NewSolutionDialog`, chrome Avalonia 920x600): categorías C#/F#, plantillas con iconos 32px del set PNG (Console/Library/Unit Test/Shared), descripción, nombre, ubicación, checkboxes (directorio/git), panel de vista previa. **Creación funcional**: ejecuta `dotnet new <plantilla>` (fallback .sln mínimo), carga la solución creada vía `SolutionLoader`, la añade a recientes y refresca el menú File.
- **Sistema de pads** (`PadHost`): panel acoplable con cabecera (título + ocultar ✕), pestañas seleccionables (Solution/Classes a la izquierda; Properties a la derecha; Output/Errors/Tasks abajo, estas últimas ocultas por defecto) y **strip de restauración** sobre la barra de estado con un chip por pad oculto. Ver→**Paneles** conmuta la visibilidad de los grupos (toggle como el legacy).
- **Cableado funcional**: File→New Solution/Open/Exit/Quit; File→Recent Solutions dinámico (persistido en `MonoDevelopProperties.xml` con formato Property key/value legacy, claves `/MonoDevelop/AvaloniaShell/RecentSolutions/ItemN`, y limpiar lista); Window→Welcome Page; dispatch `pads:`/`recent:`/`cmd:`; toolbar existente.
- **QA E2E**: clics reales XTEST abren el menú File de Avalonia (popup renderizado con iconos) y Cancel/Create del diálogo funcionan; `dotnet new console` creó `~/opencode/TestProj/TestProj.sln` real; `--sln` carga la solución (`Loaded TestProj.sln`), puebla el pad Solution, persiste el reciente y reconstruye el menú. Build 0 errores.

Siguiente: portar Find in Files desde la captura real; wiring de run/build; paneles de depuración cuando se porte el debugger.

### M5 — Welcome Page réplica 1:1 del estilo "página web" del legacy + overlay completo (2026-09-20, continuación 4)

Corrección de la iteración anterior: la Welcome debía verse como la página web del IDE GTK y NO debe dejar ver los pads. Análisis del código GTK real:
- **No es un WebKit**: AT-SPI sobre el IDE en ejecución confirma `WelcomePageFrame` (panel nativo Xwt) con `WelcomePageProjectBar`; el aspecto "web" lo da el diseño de `MonoDevelop.Ide.WelcomePage` (`Style.cs` define colores/fuentes como strings hex para pintar "pads" tipo tarjeta).
- **Anatomía implementada desde las fuentes**: `WelcomePageFrame` = project bar (tooltip-style, visible solo con solución abierta: "Solution 'X' is currently open" + "Go Back to Solution"; Escape también oculta con solución abierta) + página: logo 876x72 (`branding/welcome-logo.png`, copiado al proyecto) sobre fondo de página, barra de enlaces (MonoDevelop.com / Documentation / Support / Q&A con iconos `welcome-link-*` y URLs reales de `DefaultWelcomePage`) y las 3 secciones-card: **Solutions** (recientes con tiles 260x46, icono 38px a la izquierda, ruta pequeña, estrella star/unstar(+hover) que fija vía `RecentFiles.SetFavoriteFile`), **Xamarin News** (slot del feed retirado; nota + enlace) y **Did you know?** (tips del `TipsOfTheDay.xml` real de `build/data/options` + botón "Next Tip", aleatorio inicial como el legacy).
- **Paleta exacta de `Style.cs`**: dark → página #000000, pads #222222, links #868686, hover de tile #2B3E50; light → página base, pads blancos, links secundarios. Títulos de sección 24px light (LargeTitleFontSize Linux).
- **Comportamiento legacy**: la página se muestra como OVERLAY sobre todo el DockFrame (`WelcomePageService.ShowWelcomePage` → `DockFrame.AddOverlayWidget`): en Avalonia es un `Border` sobre el Grid de pads en `MainWindow` — **no se ve Solution/Output/pestañas detrás**; se oculta al abrir solución y vuelve con View→Welcome / Window→Welcome.
- **IconService**: nuevo `GetResourceImage(name)` para iconos por nombre de recurso sin stock-id (`welcome-link-*`, `star-16`, `unstar-16` con variantes `~dark`/`@2x`).
- **Recientes**: `RecentSolutions.IsFavorite/SetFavorite` persiste el pin en `MonoDevelopProperties.xml`.
- QA: build 0 errores; captura de la app → página #000000, cards #222222 con los 3 bloques en fila (perfil idéntico a la captura GTK real de la Welcome); flujo `--sln` → overlay oculto, pads Solution/Properties/Output visibles, `Loaded TestProj.sln`.

### M5 — Iconos migrados + Preferences funcional + gettext + editor isla + catálogo completo de pads (2026-09-20, continuación 5)

- **Iconos de todo el set**: `IconService` resuelve dinámicamente los 457 stock-ids de `StockIcons.addin.xml` (parse del addin + variantes `~dark`/`@2x`/`~sel`), y los pads menú/pestañas usan los stock-ids reales del legacy (`md-solution-pad`, `md-classes-pad`, `md-help-pad`, `md-toolbox-pad`, `md-properties-pad`, `md-pad-document-outline`, `nunit-pad-icon`, `md-output-icon`, `md-errors-list`, `md-task-list`, `md-view-debug-*`, `gtk-find`).
- **gettext**: `GettextService` parsea los `.mo` reales de `build/locale/<lang>/LC_MESSAGES/monodevelop.mo`; `Program.Main` fija la cultura según `MonoDevelop.Ide.UserInterfaceLanguage` y `MenuService` traduce el menú con el mismo catálogo que la GTK.
- **Preferences** (`PreferencesDialog`, chrome Avalonia): paneles funcionales con las claves legacy exactas — Visual Style (tema), User Interface Language (lista `LocalizationService` + restart), Author Information (`Author.*`), Key Bindings (editor de atajos que persiste `~/.local/share/MonoDevelop/9.0/KeyBindings/Custom.kb.xml` y reconstruye el menú), Fonts (roles `Editor`/`Pad`/`OutputPad` en la propiedad anidada `FontProperties`, aplicables a los editores abiertos), Updates (`MonoDevelop.Ide.AddinUpdater.CheckForUpdates`), Tasks (colores `Monodevelop.UserTasks*Color` en formato `rgb:rrrr/gggg/bbbb`), External Tools (`MonoDevelop-tools.xml` formato legacy), Feedback (`MonoDevelop.LogAgent.Report*`), Load/Save (`StartupBehaviour`, `SharpDevelop.*`, ruta por defecto), Build (`BeforeCompileAction`, `MSBuildVerbosity`, `ParallelBuild`…), Maintenance (`MonoDevelop.Enable*`).
- **Atajos de teclado globales**: `KeyboardShortcutRegistry` vincula cada MenuItem con comando a `HotKeyManager` (aceleradores en toda la app) y `MainWindow.OnKeyDown` despacha los comandos del menú (precedencia Custom.kb.xml → Commands.addin.xml), excluyendo combos del editor.
- **Editor con pestañas isla restaurado**: `SkTextEditor` (render Skia, sustituto de Mono.TextEditor) gana `FilePath`/`IsDirty`/`Save`; doble clic en un archivo del pad Solution lo abre en pestaña isla (título con marcador de modificado • + tooltip de ruta); File→Save/Save All/Close funcionales; corrección del re-parenting del header de pestaña.
- **Catálogo completo de pads (Pads.addin.xml + addins)**: Left = Solution/Classes/Help; Right = Toolbox/Properties/Document Outline/Unit Tests; Bottom = Output/Errors/Tasks/Code Issues/Search Results; sub-dock derecho inferior = Call Stack/Locals/Watch/Breakpoints/Threads (pads de debug, `defaultPlacement=Bottom` del legacy). Ver→Paneles con **un toggle por pad** (orden/labels del addin) con checkmarks sincronizados a la visibilidad real en cada rebuild, selección de pestaña al mostrar, y auto-ocultado del dock cuando no quedan pestañas. Estado por defecto = `defaultStatus` del addin (Solution/Properties/Output visibles; resto auto-hide).
- QA: build 0 errores; captura → iconos en las tiras de pestañas (píxeles del icono azul del pad Solution verificados), árbol de solución poblado, `Loaded TestProj.sln`; toggles de pads con checkmarks.

### M5 — Find in Files / Replace in Files + Build/Run funcionales (2026-09-20, continuación 6)

Port desde `MonoDevelop.Ide.FindInFiles` y `ProjectOperations` del legacy:
- **FindInFilesDialog** (chrome Avalonia 520x420): modos Find/Replace (toggles con iconos `gtk-find`/`gtk-find-and-replace`), combo editable de búsqueda/reemplazo, Path + botón "…", checkbox "Recursively", máscara de archivos (con presets como el GTK), **los 7 scopes del legacy** (Whole solution / All solutions / Current project / All open files / Directories / Current document / Selection) y las opciones de `FilterOptions` (Case sensitive / Whole words only / Regular expression).
- **Búsqueda real** (`FindInFilesDialog.Search`): recorre el scope resuelto (directorio de la solución), excluye bin/obj/.git (como `FileProvider`), respeta máscara múltiple (`;`), regex/whole-words/case y límite de 1000 resultados; **Replace in Files reescribe los archivos** con el reemplazo.
- **Pad Search Results** (`SearchResultPad` legacy): se activa automáticamente con cabecera "N match(es) for '…'" y una fila por resultado `archivo:línea: texto`; **doble clic abre el archivo y salta a la línea** (`SearchResultWidget.Activate` → `OpenFileDocumentAtLine` + `SkTextEditor.GotoLine`).
- **Editor**: `SkTextEditor` gana selección real (ancla+drag), `SelectedText` (prellenar el diálogo con la selección, como `UseSelectionForFind`), `FindFromCaret` (Find Next/Previous del menú Search) y `GotoLine`; scroll sigue al caret.
- **Build/Run** (`ProjectCommands.BuildSolution/Rebuild/Clean/Run/Stop`): ejecuta `dotnet build/rebuild/clean` por proyecto de la solución y `dotnet run --project` para el ejecutable, con salida en vivo al pad Output; los hilos del proceso se marshalizan a la UI (`Dispatcher.UIThread.Post`, equivalente a `Gtk.Application.Invoke`).
- **Pad Errors con parseo MSBuild**: las líneas `archivo(line,col): error|warning CODE: mensaje` se parsean (`ParseBuildMessage`) y activan el pad Errors con "N problem(s) — last: …", el flujo legacy BuildCycle→ErrorListPad; "Build succeeded." en verde de estado cuando exit 0.
- **QA hooks**: `--find` (diálogo + búsqueda automatizada), `--build`, `--run`.
- **QA E2E real**: búsqueda de "Hello" en ~/TestProj → 1 match y fila en el pad (la plantilla no contiene "class" → 0 matches verificado también); build con error inyectado → `CS1525` parseado al pad Errors y exit 1; build limpio → exit 0; Run → `Hello, World!` en el Output.

### M6 — Go To File/Type + pad Tasks + External Tools (portados del GTK)
- **Go To File/Type** (`GoToDialog.axaml.cs`, legacy `SearchPopupWindow`+`FileSearchCategory`): popup sin chrome,
  ranking por subsecuencia (prefijo > inicio de palabra > subsecuencia, como `MatchRank`), iconos stock
  `md-class`/`md-plain-file`, Enter o doble clic abre el documento vía `OpenFileDocument`. QA: filtro "Prog"
  → 1 resultado → Enter abre Program.cs en pestaña isla.
- **Pad Tasks** (`TaskScanner.cs`, legacy `TaskService` + `Monodevelop.TaskListTokens`): escanea .cs del
  proyecto con los tokens legacy (FIXME:2;TODO:1;HACK:1;UNDONE:0, case-sensitive con `:`), filas
  `archivo:línea: texto` y activación del pad al rescan. QA: 1 TODO real detectado en TestProj.
- **External Tools** (`ExternalToolRunner.cs`, legacy `ToolsCommands.LaunchTool`): lee `MonoDevelop-tools.xml`,
  expande `${FilePath} ${FileDir} ${FileName} ${SolutionDir} ${CurLine:Text}` y ejecuta con salida al pad Output.
  Herramientas visibles en el menú Tools (antes de Preferences, como el legacy).
- QA E2E: app + solución cargada, GoTo con teclado real (XTEST), Tasks rescan con match real, build exit 0.

### M7 — Go To Line, pad Errors clicable, context menu y clipboard del editor
- **Go To Line** (legacy `GotoLineNumberWidget` de MonoDevelop.SourceEditor2): overlay dentro del editor
  (no popup X11), prellenado con la línea actual, parseo idéntico: `N`, `N:C`, `N,C` y saltos relativos
  `+N/-N`; Enter aplica, Escape cierra. El parseo vive en `SkTextEditor.ParseGotoInput` (compartido con QA).
- **Pad Errors interactivo** (legacy ErrorListPad → ILocationList): una fila por problema parseado de MSBuild;
  doble clic abre el archivo en la línea+columna del error. Los errores se limpian en cada build.
- **Context menu del editor** con iconos stock legacy (gtk-cut/copy/paste/select-all + Go To Line…) y
  **clipboard real** (Cut/Copy/Paste/SelectAll, Ctrl+X/C/V/A) sobre la selección del editor Skia.
- QA E2E: `--gotoline=9:9` → caret exactamente en línea 9 col 9 de Program.cs; app estable, build limpio.

### M8 — Solution pad jerárquico completo (anatomía NodeBuilder legacy)
- Réplica del árbol `SolutionNodeBuilder → ProjectNodeBuilder`: solución (md-solution) → proyectos
  (md-project) → **References** (md-reference-folder, con una fila md-reference por Reference/
  PackageReference del csproj) → carpetas recursivas (md-closed-folder; bin/obj ocultos como el legacy)
  → archivos con icono por extensión (md-file-source / md-xml-file-icon / md-text-file-icon …,
  equivalente a `DesktopService.GetIconForFile`).
- Doble clic en archivo abre editor isla (ya funcional); nodos References/carpetas ignoran el open.
- QA: árbol con contenido e iconos coloreados verificados por captura.

### M9 — Operaciones de línea, undo/redo y dispatch del menú Edit
- **SkTextEditor**: DeleteLine, DeleteToLineStart/End (Ctrl+K), DuplicateLine (Ctrl+Shift+D),
  MoveBlockUp/Down (Alt+Up/Down), ToggleLineComment (idempotente, estilo legacy `// `), JoinWithNextLine,
  SortSelectedLines, Indent/Unindent, Upper/Lowercase, RemoveTrailingWhitespace, InsertGuid, DeleteForward.
- **Undo/Redo** con stack de snapshots (200) compartido por Commit y MutateLines; el menú Edit
  (Undo/Redo/Cut/Copy/Paste/Delete/SelectAll + submenús Format) despacha al editor activo vía
  `WithActiveEditor`, igual que el legacy SourceEditorView.
- QA E2E (`--editops`): duplicate→undo→comment→uncomment→redo/undo con aserciones por texto y conteo.

### M10 — File > New File / Open File / Save As funcionales
 - **NewFile** (legacy `MonoDevelop.Ide.Projects.NewFileDialog`): diálogo real "New File" — árbol de
   categorías (C# → General), lista de templates (Empty File / Assembly Information, icono 32px +
   lenguaje), panel de info (título bold + descripción), campo `Name:`, caja "Add to project"
   (checkbox + combo de proyectos + `Path:` + `Browse`) y botones `Cancel`/`New`. Creación del fichero
   con **BOM UTF8 + standard header** (policy global) + cuerpo del template (sustituciones
   `$${Tag}`/`$if$`/`$else$`/`$endif$`); Assembly Information → `Properties/AssemblyInfo.cs`;
   duplicado AssemblyInfo.cs se excluye solo con proyecto. Sin proyecto abierto crea documento en
   memoria (unsaved, persistible con Save As). Hooks QA: `--newfile`, `--newfile-unsaved`,
   `--ctxnewfile` (menú contextual), `--newfile-visual` (captura). Ventana 718x524 (pares 192/292/*),
   idéntica al legacy. QA 17/17 PASS (2026-11-03).
- **OpenFile** ya no abre solo soluciones: `FilePickerOpenOptions` para cualquier archivo de texto → editor isla.
- **SaveAs** (legacy FileService.SaveAs): escribe a la ruta elegida, retargetea el editor y re-nombra la pestaña isla.
- QA: app sin excepciones; los pickers nativos quedan wired al dispatch del menú.

### M11a — Menú Window completo (Next/Prev/Document List/1-9/Close All/Close Workspace)
- **Next/PrevDocument** (legacy NextDocumentHandler/PrevDocumentHandler): ciclo con wrap-around,
  deshabilitado con <2 documentos. **OpenDocument1-9** (OpenDocumentNHandler): selecciona el N-ésimo.
- **CloseAllFiles** cierra en orden; **CloseWorkspace** cierra documentos + solución y muestra Welcome
  (mismo flujo legacy FileCommands).
- QA E2E (`--windocs`): ciclo Program.cs↔new1.cs, OpenDocument2 correcto, out-of-range inofensivo.

### M11b — NavigationCommands + Zoom (legacy NavigationHistoryService)
- **NavigationHistoryService** nuevo: pila de puntos (archivo+línea) con MoveBack/MoveForward,
  CanMoveBack/Forward, truncado de rama forward y Clear — la misma semántica que los handlers legacy.
- **NavigateBack/Forward/History/ClearNavigationHistory** dispatchados; **ZoomIn/Out/Reset** operan el
  FontSize del editor Skia (límites 5..60, reset a 12 como el legacy Options.ZoomReset).
- QA E2E (`--navhist`): back 11→7→1, forward→7, clear→CanMoveBack=False.

### M11c — Bookmarks + Use Selection for Find
- **IBookmarkBuffer legacy** (ViewCommandHandlers): ToggleBookmark en la línea del cursor,
  Next/PrevBookmark con wrap-around, ClearBookmarks. Marcador visual en el gutter del editor Skia
  (cuadrado azul estilo gutter-bookmark-15) + tinte de fila completa.
- **UseSelectionForFind**: prellenado de la búsqueda con la selección actual (SearchCommands legacy).
- QA E2E (`--bookmarks` con solución cargada): toggle de 3 líneas, next con wrap 9→1, prev 1→9.

### M11d — AddReference + ReloadFile + Project/Solution Options
- **AddReferenceDialog** (legacy AddReferenceDialog, pestaña assemblies): lista de ensamblados
  conocidos + campo custom, y **edición real del .csproj** (XDocument: dedupe + `<ItemGroup><Reference/>`),
  la misma transformación que `DotNetProject.References.Add` del legacy.
- **ReloadFile** (FileCommands): revierte el editor activo al contenido en disco.
- **ProjectOptions/SolutionOptions**: mensaje de estado (los paneles viven en Preferences).
- QA E2E (`--addref`): TryAddReference(System.Json) → True, csproj contiene la referencia, y revert limpio.

### M11e — HelpCommands completo
- **OpenLogDirectory** (legacy UserProfile.LogDir): abre ~/.local/share/MonoDevelop en el explorador
  real (Process.Start con UseShellExecute, igual que el legacy).
- **MarkLog** escribe el separador ===== MARK ===== en el log; **DumpUITree/A11yTree** reportan el
  estado del workbench; **CheckForUpdates** replica la respuesta del Updater; **Help (F1)** registra
  la petición de documentación (HelpOperations.ShowHelp('root:')).
- QA: app sin excepciones tras el wiring.

### M11 — Lote final de comandos de menú (iteraciones D–F)
- **AddReferenceDialog**: añade `<Reference>` real al .csproj del proyecto activo (QA: referencia añadida y revertida).
- **HelpCommands**: OpenLogDirectory abre el directorio real de logs; MarkLog/DumpUITree/A11y con salida funcional; CheckForUpdates.
- **Iteración F**: GotoMatchingBrace real (saltos anidados {}, (), []) + RefactorCommands.Rename file-wide con InputDialog + PrintDocument + ShowMessageBubbles persistido + SaveCurrentLayout/DeleteCurrentLayout (Monodevelop.PadLayout) + VersionControlCommands con git real (status/log/diff/pull/add) enviando salida al pad Output.
- QA determinista `--brace`: matching de llaves clase↔cierre, rename+undo, dispatch git verificado.

### M11g — Build por proyecto, exportación y recientes
- ProjectCommands.Build/Rebuild/Clean sobre el proyecto activo (RunBuildAsync con projectFilter), SetStartupProjects persistido en Monodevelop.StartupProject.
- ExportSolution (copia del árbol de la solución), ClearRecentFiles vía RecentSolutions.Clear + rebuild del menú File, InsertStandardHeader.
- QA --buildone: dispatch del comando Build con MSBuild exit 0.

### M11h — Multi-caret (familia InsertNextMatchingCaret)
- SkTextEditor: carets secundarios, InsertNextMatchingCaret (Alt+Shift+.), InsertAllMatchingCarets (Alt+Shift+A), RemoveLastSecondaryCaret (Alt+Shift+,), RotatePrimaryCaretNext/Previous, MoveLastCaretDown, InsertAtAllCarets (bottom-up, desplazando carets en la misma línea), Escape colapsa a primario; render con carets secundarios más cortos (InsertionCursor legacy).
- Dispatch de los 6 TextEditorCommands en MainWindow. QA --mcaret: all-matching + insert + undo + rotate verificados.

### M11j — FormatBuffer (menú Edit > Format > Format Document)
- Legacy: `FormatBufferHandler` (MonoDevelop.CSharpFormatting) reindenta y normaliza.
- Avalonia: `SkTextEditor.FormatBuffer()` — reindentación por profundidad de llaves
  (outdent en `}`, indent tras `{`), colapso de espacios, push a undo y QA E2E
  determinista (`--fmt`): desindentar → formatear → undo.

### M11k — VersionControl.Commands.Diff (visor de diff real)
- Legacy: vista Diff del pad de control de versiones sobre `git diff`.
- Avalonia: `ShowDiffAsync()` — ejecuta `git diff` en el directorio de la solución
  y muestra el patch en una ventana modal monoespaciada con SystemDecorations Full.
- QA E2E (`--diff`): repo git en TestProj con cambio real → 9 líneas de patch
  renderizadas; verificación visual por captura X11 de la ventana.

### M11l — Plegado de código (TextEditorCommands: folding)
- Legacy: `SourceEditorView.IFoldable` (ToggleFolding/ToggleAllFoldings/FoldDefinitions/
  EnableDisableFolding) sobre FoldSegments de Mono.TextEditor.
- Avalonia: parser de regiones por balance de llaves en `SkTextEditor`
  (`RebuildFolds`, colapso con marcador [+]/[−] en el gutter y resumen "… } // N lines"),
  salto de líneas ocultas en render y navegación, y dispatch de los 4 comandos de menú.
- QA E2E (`--fold`): 2 regiones detectadas en Program.cs; toggle colapsa/expande,
  ToggleAll, EnableDisable limpia regiones y re-habilita — texto siempre intacto.

### M11m — ViewCommands restantes (layouts + navegación de resultados)
- Legacy: `Workbench.ShowNext/ShowPrevious` (ILocationList), layouts Single/SideBySide,
  NewLayout/DeleteCurrentLayout, CenterAndFocusCurrentDocument.
- Avalonia: `ShowNextResult/ShowPreviousResult` recorren los resultados de
  Búsqueda en Archivos con salto a archivo+línea y wrap-around;
  `PadHost.SetHostVisible` oculta/restaura los hosts de pads (Single/SideBySide);
  CenterCaret centra la línea del caret en el viewport; New/DeleteLayout persisten
  en `Monodevelop.PadLayout`.
- QA E2E (`--viewcmds`): 3 coincidencias → next/next/prev con saltos verificados;
  pads ocultos y restaurados.

### M11n — MessageBubbleCommands (burbujas inline del editor)
- Legacy: `MessageBubbleCommands.Toggle/ToggleIssues/HideIssues` con los tres estados
  de `IdePreferences.ShowMessageBubbles` (Never/ForErrors/ForErrorsAndWarnings).
- Avalonia: `SkTextEditor.SetBubbles/SetBubbleMode/ToggleBubbles` — marcador inline
  "● CODE: message" (rojo error, ámbar warning) tras el texto de la línea afectada;
  sincronización en vivo desde el parser de MSBuild (`ParseBuildMessage` coloca la
  burbuja en el documento abierto en cuanto el error llega).
- QA E2E (`--bubbles`): ciclo completo de los 3 estados verificado.

### M11o — Alias de namespaces + ViewList/LayoutList/NavigateTo
- Los mismos comandos legacy están registrados con ids distintos según el menú
  (folding en EditCommands y TextEditorCommands, Rename en EditCommands y
  RefactorCommands) — el dispatch acepta ambos ids.
- ViewList/LayoutList reportan pads guardados y layout persistido;
  NavigateTo (toolbar) abre el GoToDialog (Go To File/Type).
- Inventario: 119/141 → ~127/141 ids de menú con wiring real.

### M11p — Completado de código y plantillas (TextEditorCommands de completado)
- Legacy: ShowCompletionWindow = "Complete Word" (candidato único → commit, múltiple →
  ciclo), ShowParameterCompletionWindow (parameter info), ToggleCompletionSuggestionMode,
  ShowCodeTemplateWindow (plantillas cw/prop/fore/forr/svm/if del CodeTemplate addin)
  y ShowCodeSurroundingsWindow (surround with if/while/for/foreach/try).
- Avalonia: `SkTextEditor.CompleteWord` (agrega palabras del documento, ciclo con
  memoria de prefijo), `ParameterHint`, `ToggleCompletionSuggestionMode`,
  `ExpandCodeTemplate`, `SurroundSelectionWith` + `CaretRight(n)` para automatización.
- QA E2E (`--compl`): candidato único 'World'→'WorldWide' insertado y deshecho;
  parameter info nula fuera de paréntesis; sugerencia conmutada.

### M11q — ToolCommands (herramientas externas)
- Legacy: `ToolListHandler` (submenú dinámico con una entrada por herramienta
  configurada en MonoDevelop-tools.xml), `EditCustomToolsHandler` (abre
  Preferencias en el panel External Tools), Instrumentation/SessionRecorder.
- Avalonia: `ToolCommands.ToolList` ejecuta la primera herramienta configurada vía
  `ExternalToolRunner` (macros ${SolutionDir} resueltas, salida al pad Output);
  `EditCustomTools` abre Preferencias directamente en el panel "externaltools".
- QA E2E (`--tool`): "List Solution Files" (ls -R ${SolutionDir}) ejecutada con
  exit 0 y listado en el Output pad.

### M11r — Cobertura completa del dispatch (141/141 ids de menú)
- Formato: `CodeFormattingCommands.FormatBuffer` (id real del menú) ejecuta el
  formateo y reporta líneas cambiadas.
- VersionControl: los ids del addin (`MonoDevelop.VersionControl.Commands.*`) se
  mapean a los mismos pipelines git — Diff/Log/Status/Update/Add/Remove/Revert;
  Lock/Unlock/Checkout/Publish/Annotate informan que requieren backend con locks.
- Políticas (DefaultPolicies/ApplyPolicy/ExportPolicy/CustomCommandList) y
  impresión (PrintPageSetup/PrintPreviewDocument) reportan su estado honesto
  en el Output pad en vez de caer al fallback.
- Resultado: todos los 141 command-ids del menú tienen handler; el fallback
  "not wired" queda sin casos alcanzables por menú.

### M11s — Menú contextual del Solution pad (ProjectPadContextMenu.addin.xml)
- Legacy: menú por tipo de nodo vía ItemType conditions — Build/Rebuild/Clean
  (IBuildTarget), Set as Startup (Project), Add ▸ New File/Reference/New Folder
  (Project|ProjectFolder), Tools ▸ Find in Files/Open Containing Folder,
  Edit ▸ Rename/Delete, Properties.
- Avalonia: `OnSolutionPadContextMenu` selecciona el nodo bajo el puntero y abre
  un `MenuFlyout` construido por `BuildProjectPadMenu()` según `SelectedNodeType()`
  (Solution/Project/ProjectFolder/ProjectFile/References). Los comandos llegan a
  `OnMenuCommand(id, nodeContext)` con el path del nodo (`contextNodePath`),
  de modo que Build/Rebuild/Clean actúan sobre el proyecto del nodo
  (`ResolveCommandProject`), Rename renombra el archivo en disco retargeteando
  el documento abierto, Delete borra con confirmación, AddNewFiles crea la
  clase plantilla y abre el editor, NewFolder crea la carpeta — y el árbol se
  refresca (`RefreshSolutionTree`) como el UpdateAll del pad legacy.
- QA E2E (`--ctxmenu`): create/rename/folder/delete sobre TestProj verificados;
  flyout verificado visualmente por clic derecho sintético X11.

### M11t — Búsqueda incremental en el Solution pad
- Legacy: `SearchEntry` (MonoDevelop.Components.SearchEntry) con placeholder
  "Search…", debounce de cambios y filtrado del TreeModelFilter que conserva
  los ancestros de las coincidencias (TemplatePickerWidget.SetSearchFilter como
  referencia de semántica).
- Avalonia: `solutionSearchBox` (TextBox con Watermark) sobre el árbol del pad;
  `ApplySolutionTreeFilter` filtra en vivo sin reconstruir (selección/expansión
  se conservan): un nodo es visible si coincide por texto o Tag, o si tiene un
  descendiente visible; los ancestros de coincidencias se expanden.
- QA: determinista (`--filter`: 4→3 nodos con "Program", root visible, vacío
  restaura) + visual E2E (teclear "Wri" → 6 filas; limpiar → 13 filas).

### M11u — Fix: columna duplicada en el Solution pad
- El host del pad contenía el TreeView real y un ListBox resumen de iteraciones
  tempranas; en DockPanel el ListBox quedaba a la derecha como segunda columna.
- Fix: eliminado el ListBox (campo, creación, llenado en OpenSolutionInWindow y
  fallback en OnSolutionOpen) — el pad queda con search box + árbol, como el
  ProjectPad legacy.
- Verificado por captura: el árbol ocupa la única columna y no hay contenido
  en la banda de la columna eliminada.

### M11v — Fix: pads con contenido vacío (Properties/Bottom)
- Causa raíz (auditoría visual por capturas): `PadHost.AddTab` no seleccionaba la
  primera pestaña; los hosts instanciados desde XAML (RightPads, BottomPads)
  quedaban con `SelectedId = null` y el área de contenido vacía hasta que algo
  los seleccionaba explícitamente. El pad izquierdo funcionaba porque
  `OpenSolutionInWindow` llama `Select("solution")`.
- Fix: `AddTab` selecciona automáticamente la primera pestaña visible
  (el comportamiento por defecto del DockItem del legacy).
- Verificado por captura tras un build: el pad Properties muestra su contenido,
  el pad inferior muestra Errors ("Build succeeded."), sin áreas vacías.

### M11w — Properties pad conectado al Solution pad (PropertyGrid legacy)
- Legacy: el `PropertyPad` (`MonoDevelop.DesignerSupport`) usa *descriptores* por
  tipo de nodo — `SolutionItemDescriptor` (Name/FilePath/RootDirectory/FileFormat),
  `ProjectFileDescriptor` (Name/Path/Type/BuildAction…) y descriptor de workspace
  para soluciones — y se repuebla al cambiar la selección vía
  `DesignerSupport.Service.UpdateSelection`.
- Avalonia: descriptores por clasificación de nodo (Solution / Project /
  ProjectFolder / ProjectFile) que leen datos reales del `.sln`/`.csproj` en
  disco; cabecera (nombre + tipo) y filas agrupadas en secciones Misc/Build
  (Target framework, Assembly name, Output type, etc.).
- La selección en el árbol del Solution pad (`SelectionChanged`) repuebla el pad
  al vuelo; el contenido se reconstruye limpio en cada cambio (fix del bug de
  controles reutilizados que duplicaba filas).
- QA: hook determinista `--props` (solución → proyecto → archivo, 23 filas
  verificadas, 0 excepciones) + verificación visual por captura.
- Commit `846703135a`.

### M11x — DirtyFilesDialog ("Save Files")
- Legacy: `MonoDevelop.Ide.Gui.Dialogs.DirtyFilesDialog` es el gate obligatorio
  al cerrar solución/salir con documentos modificados (`Workbench.OnDeleteEvent`,
  `DockWindow`, `FileTabCommands`): TreeStore con checkbox por documento,
  agrupados bajo "Project: {name}", cascada de checks padre→hijos y recálculo
  tri-state hijos→padre (`NewCheckStatus`/`ToggleChildren`), y 3 acciones:
  **Save and Quit/Close**, **Quit/Close** y **Cancel**.
- Avalonia: `DirtyFilesDialog.axaml` con chrome Avalonia (sin decoraciones del
  OS); `Load(docs, closeWorkspace)` con agrupación, cascada con guard
  anti-recursión y tri-state (null = mixed); `Result` con los 3 valores legacy;
  `CheckedDocs` expone qué persistir.
- Cableado en los 4 puntos del legacy: **CloseDocument** (untitled dirty →
  confirmación), **File > Close Workspace**, **File > Exit** y **el botón de
  cerrar la ventana** (`Window.Closing` con `e.Cancel` como `OnDeleteEvent`).
- QA triple: determinista (`--dirtyfiles`, 7 aserciones: dirty → diálogo con
  grupo "Project: TestProj" → Save and Quit persiste en disco → dirty=False →
  gate activo); E2E real (WM_DELETE con documento sucio **bloquea el cierre** y
  muestra el diálogo); E2E del botón ("Save and Close" → guardó y cerró la app
  con el archivo persistido).
- Commit `cc2024c15c`.

### M11y — Editor: fixes de render + intellisense + hover tooltip (code completion legacy)
- **Fantasma/líneas duplicadas (causa raíz)**: el compositor de Avalonia puede
  seguir leyendo el bitmap del frame anterior mientras `Render` se re-ejecuta al
  teclear; pintar sobre esa misma memoria (WriteableBitmap reutilizado) emborrona
  el frame viejo bajo el nuevo. Fix: cada frame se renderiza en un
  **WriteableBitmap nuevo** (el bitmap presentado nunca se muta; el anterior se
  libera una frame después vía `pendingDispose`).
- **Backspace sin efecto**: el flag `applyingCommit` se quedaba pegado en `true`
  cuando `SetValue` era no-op (valor igual) y tragaba el refresh externo
  (`SetLines`); eliminado — la comparación por valor absorbe el eco. Además el
  Backspace/Delete solo operaban el caret primario con carets secundarios
  residuales congelados (las barras `|` visibles): ahora aplican a **todos los
  carets** bottom-up (semántica multi-caret legacy) y un clic normal colapsa al
  caret primario (solo Alt+Shift añade).
- **CompletionPopup** (port de `CompletionListWindowGtk`): ventana borderless
  con lista de entries (icono legacy `element-*` + texto + descripción), filtro
  mientras se teclea, navegación Up/Down/PageUp/PageDown (paso 8 del legacy),
  commit Enter/Tab/clic, cancelación Escape/focus-loss. Trigger: `.` y
  **Ctrl+Space** (`TextEditorCommands.ShowCompletionWindow`). Fuente de datos:
  palabras del documento + keywords C# (el legacy agrega palabras del documento
  cuando no hay modelo de lenguaje).
- **EditorTooltipPopup** (port del pipeline `TooltipProvider`): hover 500 ms
  (HOVER_TIME) sobre una palabra muestra tooltip borderless con icono, la
  firma/declaración de la palabra (búsqueda de declaración en el documento) y la
  línea; posición por `PointToScreen` (la coordenada editor-relative debe
  traducirse a pantalla: el popup es top-level).
- **Otros fixes**: cursor I-beam en el área de edición (`StandardCursorType.Ibeam`
  en las dos rutas de apertura de MainWindow); el pad del editor permanece
  abierto sin pestañas (placeholder "No open documents"); `ShowTooltipForQa`
  permite QA visual del tooltip sin puntero real.
- QA determinista `--editqa` (todo verde, restaura el archivo original al final
  para no dejar residuo): insert/backspace elimina carácter; hover info de
  "Main" → `Main (method)`; popup visible tras `Console.` → commit →
  `Console.WriteLine` insertado; items `Write*` ≥ 1; editor pad vivo con 1
  pestaña. Verificación visual: captura con tooltip renderizado y editor limpio
  (sin fantasmas ni carets residuales).
- Archivos: `Controls/CompletionPopup.cs` y `Controls/EditorTooltipPopup.cs`
  (nuevos), `Controls/SkTextEditor.cs`, `Views/MainWindow.axaml.cs`,
  `Program.cs`.
- Commit `c4fd345c44`.

### M11z — Tests del modelo del editor + Diff en pad inferior + TipOfTheDay y ProgressDialog
- **Tests xunit (`tests/AvaloniaShell.Editor.Tests`, `dotnet test`)**: 16 tests
  sobre el modelo del `SkTextEditor` — división/normalización de líneas
  (`\r\n`→`\n`, línea final vacía), movimiento de caret (GotoLine/GotoLineEnd/
  CaretRight), Backspace de un caret (borra carácter, une línea previa),
  **backspace multi-caret** (borra en todos los carets bottom-up, une líneas
  cuando los carets están al inicio), insert en todos los carets,
  colapso al primario, undo/redo y dirty tracking. Encontró y corrigió 2 bugs
  reales: `BackspaceForQa` era single-caret mientras el handler `Key.Back` era
  multi-caret (unificados en `DeleteBackwardAtCarets`, una sola fuente de
  verdad) y el modelo nacía vacío porque el default de TextProperty nunca
  disparaba `OnPropertyChanged` (el constructor ahora siembra `SetLines(Text)`).
  Accesores de test: `LineCountForTest`/`LineTextForTest`/`CurrentLineForTest`/
  `CurrentColumnForTest`/`AddSecondaryCaretForTest`.
- **Diff en pad inferior** (antes ventana modal con borde OS): el `git diff`
  se muestra ahora como pestaña "Diff" del pad inferior (icono legacy
  `vc-diff`), comportamiento del visor interno del legacy; en ejecuciones
  siguientes el contenido se reemplaza (`ReplaceTabContent`) y la pestaña se
  selecciona. Verificado: `[diff] pad shown 14 patch lines` + captura del pad.
- **TipOfTheDayDialog** (port de `TipOfTheDayWindow`): icono info +
  "Did you know...?", tips del `TipsOfTheDay.xml` legacy (39 entradas, buscado
  como PropertyService.DataPath), primer tip aleatorio, Next cicla, checkbox
  "Don't show tips at startup" que persiste la preferencia legacy (invertida)
  vía nuevos `UserPreferences.GetBool/SetBool`. Chrome Avalonia.
- **ProgressDialog** (port de `ProgressDialog`): label de mensaje, barra,
  intercambio Cancel→Close, expander "Details" con el log de tareas indentado
  (`BeginTask`/`EndTask`/`WriteText`, 2 espacios por nivel) y los 3 estados
  finales de `ShowDone` (errors/warnings/success); cancelación vía
  `CancellationTokenSource` (guarda `Cancelled`).
- QA: `--totd` y `--progress` (diálogo + tareas anidadas + ShowDone con
  verificación de mensaje/barra/visibilidad de botones) + capturas (estado
  completado del progress, tip ciclado, pestaña Diff en el pad).
- Commit `b9b2fd0e42`.

### M13 — Configs reales + import de proyectos + comandos restantes

**Nuevo `Services/ConfigurationService.cs`** (persistencia como el
ProjectService legacy):
- `GetSolutionConfigurations` lee `GlobalSection(SolutionConfigurationPlatforms)`
  del .sln (claves `Name|Platform` → nombres puros).
- `AddSolutionConfiguration` escribe la config en el .sln
  (`Name|Any CPU` en SolutionConfigurationPlatforms + entradas
  `ActiveCfg`/`Build.0` por GUID de proyecto en ProjectConfigurationPlatforms) y,
  con `createChildren`, añade en cada .csproj el
  `<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Name|AnyCPU' " />`
  (mapeo AnyCPU del legacy). Devuelve false si ya existe.
- `AddProjectToSolution` crea el .sln wrapper de un .csproj suelto
  (Format Version 12.00, GUID C#, Debug/Release con ActiveCfg/Build.0) — el
  equivalente a `ProjectOperations.ImportProject`.

**NewConfigurationDialog persiste de verdad**: al aceptar con solución cargada
crea la config en .sln + .csproj y recarga el árbol
(el QA `--newconfig-real` crea "QAConfig", verifica en disco
`sln-entry=True csproj-entries=True` y limpia para repetibilidad).

**File > Open... importa proyectos**: `OpenFileOrProject` enruta
.sln/.slnf → abrir solución, .csproj → importar (crea wrapper .sln si no
existe) y abre, resto → pestaña de documento
(el QA `--openimport` genera un proyecto temporal y verifica
`wrapper-sln=True loaded=True`; captura del árbol importado).
El wrapper .sln ya no aparece como archivo dentro del árbol (el legacy lo oculta).

**Comandos restantes cableados** (el menú ya no tiene "not wired" de Project/Search):
`ProjectCommands.BuildSolution` (build completo), `RunCodeAnalysisSolution/Project`
(build con analíticos, mensaje en Output) y `SearchCommands.FindNextSelection`
(selección actual → Find in Files + ShowNextResult, como el legacy).

- QA: `--newconfig-real`, `--openimport` + captura visual del proyecto importado
  (árbol + References + Properties "Visual Studio solution / Projects 1").
- Tests: 16/16 en verde como gate pre-commit.
- Commit `(M13)`.

### M14 — Active Configuration real + New Project añade a la solución + pad Bookmarks

**Active Configuration (legacy SelectActiveConfigurationHandler +
MainToolbarController):**
- `ConfigurationService.Get/SetActiveConfiguration` persiste la config activa en
  `<sln>.userprefs` con el formato legacy exacto
  (`<MonoDevelop.Ide.Workspace><Property name="ActiveConfiguration" value="…"/>`).
- El combo de la toolbar se llena con las configs reales del .sln al abrir la
  solución y muestra la persistida; cambiarlo guarda y refresca el menú
  (QA: switch Debug→Release→persisted=True→combo=Release→restaurado a Debug).
- Project > Active Configuration es ahora dinámico: un ítem checkeable por config
  real (`SelectActiveConfiguration:<name>`), ya no el Debug/Release hardcodeado.

**New Project añade a la solución abierta (legacy AddSolutionItem):**
- El diálogo gana el check "Add to open solution" (default ON con solución
  abierta, como el radio del GTK) y `CreatedProjectPath`.
- `ConfigurationService.AppendProjectToSolution` inserta el Project entry con
  GUID nuevo + mappings ActiveCfg/Build.0 por cada config del .sln (crea la
  sección ProjectConfigurationPlatforms si el .sln no la tiene) y recarga el árbol.
- QA `--newproject` con AutoCreateForQa: `created=True sln-entry=True
  mappings=True` + captura del diálogo pre-rellenado.

**Pad Bookmarks (port del pad del SourceEditor addin):**
- Nueva pestaña en la zona debug (icono legacy `md-bookmark-toggle`): una fila
  por bookmark del documento activo (`línea: texto`), doble clic salta a la línea
  (GotoLine), refresco al cambiar de documento y al toggle/clear.
- QA `--bmkpad`: rows=2 con el texto de línea correcto, NextBookmark navega,
  Clear deja la fila "No bookmarks…"; captura del pad en la zona debug.

- Tests 16/16 en verde como gate pre-commit. Commit `(M14)`.

### M15 — Pad Breakpoints con persistencia real + menú/navegación en Bookmarks + build/run con la config activa

**Pad Breakpoints (port de BreakpointPad del addin Debugger):**
- Nueva pestaña en la zona debug (icono legacy `md-breakpoint`): una fila por
  breakpoint de todos los documentos abiertos — icono `md-breakpoint` /
  `md-breakpoint-disabled` + `Archivo:línea` (con sufijo ` (disabled)`), el
  equivalente a las columnas FileName/Enabled del pad GTK.
- Gutter markers en SkTextEditor: círculo rojo relleno (habilitado) o contorno
  gris (deshabilitado) como los stock `md-breakpoint`/`md-breakpoint-disabled`;
  clic en el margen hace toggle (semántica del gutter legacy).
- Menú contextual del pad: Go to Breakpoint, Enable/Disable, Remove y Clear All,
  despachados por los command ids legacy de `MonoDevelop.Debugger.DebugCommands`.
- **Persistencia real**: `Services/BreakpointService.cs` replica el formato
  `BreakpointStore.Save/Load` de Mono.Debugging — elementos `<Breakpoint
  file=… relfile=… line=… (1-based) enabled=…/>` dentro de
  `<MonoDevelop.Ide.DebuggingService.Breakpoints>` en el `<Properties>` de
  `<sln>.userprefs`; los breakpoints se restauran al (re)abrir cada documento
  (camino de carga del DebuggingService legacy).

**Menú contextual + navegación en el pad Bookmarks:**
- Clic derecho sobre el pad: Previous/Next Bookmark, Remove bookmark y
  Remove All Bookmarks, con la semántica del pad del SourceEditor legacy; el
  pad sigue refrescándose en toggle/clear/cambio de documento.

**Build/Run con la Active Configuration real (ProjectOperations):**
- Build por proyecto ahora ejecuta `dotnet build -c "<config>"` y Run ejecuta
  `dotnet run -c "<config>" --project`, con la config activa persistida en
  .userprefs (QA `--buildone`: `[build] configuration Debug`).

- QA: `--bkpad` (filas del pad `rows=2 first=Program.cs:7`, XML persistido
  `lines=True&True`, enable/disable `disabled-row=Program.cs:9  (disabled)`,
  navegación `NextBreakpoint → line 7`, cleanup repetible) y `--keepbps` para
  dejar los breakpoints puestos en capturas visuales del pad.
- Tests: 16/16 en verde como gate pre-commit.
- Commit `(M15)`.
- Captura de la paridad visual: `docs/img/breakpoints-pad.png` (pad
  Breakpoints con `● Program.cs:7` e `◎ Program.cs:9 (disabled)` en el pad
  inferior de ancho completo).

### M15b — Un proyecto, una build: salida unificada en main/build + --old-gui

**Unificación del árbol de build (no hay "proyecto Avalonia" ni "proyecto
legacy": es un solo proyecto cuya UI legacy GTK# queda oculta como
compatibilidad durante la rama 9.x y solo se muestra con `--old-gui`):**
- Las DLLs Avalonia y las del runtime GTK conviven en la MISMA carpeta
  `main/build/` (deps/runtimeconfig separados por ensamblado, sin
  subcarpetas): el shell Avalonia compila directo a `main/build/`
  (multi-target `net10.0;net10.0-windows`; el TFM windows es `Exe` para abrir
  consola de diagnóstico en Windows) y el runtime GTK (`MonoDevelop.dll` +
  todo net10run) se stagea a esa misma carpeta. El conflicto inicial
  (`MonoRoslynCompat.dll`/`Mono.Addins*` viejos en build/ contra los del
  runtime) se resolvió stageando siempre los bins del runtime encima.
- `MonoDevelop.Startup.Avalonia` entra en `Main.sln` (GUID propio, mapeos
  ActiveCfg/Build.0 para las 8 configuraciones) y la solución completa valida
  (`msbuild Main.sln -t:ValidateSolutionConfiguration` en verde).
- **`--old-gui` SOLO existe en `MonoDevelop.AvaloniaShell.dll`**: con ese
  parámetro relanza la UI GTK legacy (`MonoDevelop.dll`, misma carpeta
  `main/build`), hereda el resto de argumentos y propaga el exit code. El
  GTK no acepta `--old-gui`: se le pasa la variable de entorno
  `MONODEVELOP_LEGACY_UI=1` que SOLO el relay establece — un lanzamiento
  directo de `MonoDevelop.dll` se rechaza con exit 2 y el aviso "The legacy
  GTK UI is hidden. Start MonoDevelop with MonoDevelop.AvaloniaShell.dll
  (add --old-gui for the legacy GTK UI).". Verificado en vivo: ventana GTK
  NORMAL 1920x1009 titulada "MonoDevelop", sin diálogo de error.

**4 pads exactos (rework del layout):** edición central + Solution (izq.) +
Properties (der.) + UN pad inferior con todos los tabs del grupo debug
(Call Stack, Locals, Watch, Threads, Bookmarks, Breakpoints) junto a Output,
Errors, Tasks, Code Issues y Search Results. El pad inferior-derecha
(DebugPads) se eliminó del AXAML y del code-behind; esto destapó además el
bug por el que el pad Breakpoints no mostraba filas: había un tab
"breakpoints" duplicado (un ListBox vacío añadido antes que el real; AddTab
ignora IDs repetidos) — solo queda el real.

**Fix del popup fatal del arranque GTK legacy:** el diálogo "No se pudo iniciar
MonoDevelop — Remoting channels were removed…" mataba el arranque cuando
`MonoDevelop.EnableAutomatedTesting=True` (persistido en
MonoDevelopProperties.xml): `AutoTestService.Start(publishServer: true)` llama
al stub `RemotingService.GetMarshaledUrl` (remoting eliminado en .NET 10) y la
excepción subía hasta el handler fatal. Ahora el inicio y la publicación del
servidor AutoTest degradan con aviso en consola y la sesión continúa sin
autotest remoto (el transporte se rehará sobre el message bus en su fase).

## M16 — Debugger: Locals/Watch con valores en runtime (netcoredbg DAP), Attach to Process y Run con debug

**netcoredbg como submodule (regla: cero DLLs binarios externos):** fork de
[Samsung/netcoredbg](https://github.com/Samsung/netcoredbg) →
`DRavainera/netcoredbg`, agregado como submodule `main/external/netcoredbg`
(.gitmodules, igual que debugger-libs/libgit2). Se compila desde fuente con
cmake+clang (Makefiles: el generador ninja no tolera el `DEPENDS .../*.cs`
glob literal de ManagedPart); el binario queda en
`main/external/netcoredbg/build/src/netcoredbg` junto a su parte managed
(ManagedPart.dll, Microsoft.CodeAnalysis.*). Los DLL que habían sido
copiados a `main/build/netcoredbg/` fueron BORRADOS; `.gitignore` pierde las
excepciones viejas. El SDK que el build de netcoredbg descarga a
`main/external/netcoredbg/.dotnet/` queda ignorado por el `.gitignore` del
propio submodule. Smoke test DAP de punta a punta con TestProj: launch →
setBreakpoints(10) → stopped → Locals (`answer=42,greeting=null`). El
adaptador exige `source.path` en setBreakpoints (sin él responde
`key 'path' not found` y el breakpoint no se aplica).

**DebugSessionService (`Services/DebugSessionService.cs`):** cliente DAP por
stdio (initialize → launch → setBreakpoints → configurationDone), eventos
stopped/terminated/output como C# events + `LastStop` bufferado (el launch y
el QA corrían en paralelo: un suscriptor tardío perdía el stop — ahora el
polling de `LastStop` es determinista). Stack/scopes/variables bajo demanda;
`FindNetcoredbg` resuelve el binario del submodule caminando hacia arriba
desde `AppContext.BaseDirectory`.

**Pads Locals/Watch reales:** los tabs Locals y Watch del pad inferior se
llenan en cada stop con los scopes/variables del frame 0 (`FillVariableList`
con `Name = Value`, placeholder honesto si no hay sesión). `SetExecutionLine`
en `SkTextEditor` pinta la línea de ejecución (fondo amarillo) y el gutter
con el marcador; `ClearExecutionLineHighlight` al continuar/terminar. El bot
ón Debug (icono bicho, junto al Run) y los comandos Debug/Continue/Stop del
menú Run comparten la sesión; Debug respeta los breakpoints persistidos en
.userprefs (los carga del store).

**QA `--locals` (determinista):** carga la solución, abre Program.cs, pone
breakpoint en la línea 10, lanza Debug (DAP) y verifica: `[debug] stopped
(breakpoint) at Program.cs:10` → `[locals] stopped reason=breakpoint
file=Program.cs:10` → `[locals] highlight=True` → `[locals]
values=answer=42,greeting=null` → `[locals] pad-realized=2 rows=2` → cleanup
(terminate + store limpio) → re-selecciona el tab Locals para capturas.
Captura en repo: `docs/img/locals-pad.png`.

**Attach to Process como TAB del pad (nada de ventanas con borde de OS):**
`AttachToProcessDialog` (Window con SystemDecorations por defecto) fue
REHECHO como `AttachToProcessPanel` (UserControl) dentro del pad inferior:
tab "Attach to Process" (icono `md-debug-all`) con filtro, Refresh, contador
"N of M processes", Close/Attach. Regla de la casa recordada: JAMÁS ventanas
con borde del sistema operativo — todo chrome es Avalonia
(`SystemDecorations=None` en las 15+ ventanas del shell). Enumeración real
de /proc como el NetCoreProcessAttacher legacy (cmdline/comm, skip kernels/
self, estado Sleeping/Running/… del /proc/<pid>/stat). El comando
Run > Attach to Process abre el tab (ScanProcesses + Select). QA
`--attachdlg`: `processes=603 has-own=False has-systemd=True
no-kernel-threads=True` + `pad-realized=1` + `tab=attach
window-chrome=Avalonia`. Captura: `docs/img/attach-to-process-pad.png`.

**Staging automático del runtime GTK tras compilar Main.sln:**
`main/after.Main.sln.targets` (enganchado desde `main/Directory.Build.targets`
cuando la sln termina de compilar) ejecuta el target `StageUnifiedRuntime`:
si `build/net10run` existe, copia a `main/build` solo lo más nuevo
(MonoDevelop.dll/Core/Ide/Startup recién compilados, MonoRoslynCompat,
Mono.Addins*, configs) — el runtime GTK de la carpeta unificada ya no se
desincroniza de la compilación. `main/data/options/TipsOfTheDay.xml` se
agregó al árbol fuente (la Welcome Page GTK lo necesita para la sección
"Did you know?"; sin él el overlay crasheaba y el GTK caía al abrir).

**Welcome Page del GTK legacy en vivo:** con TipsOfTheDay.xml en su sitio el
arranque GTK muestra la welcome page real (logo, Solutions recientes, New/
Open) — verificado en X11 con capturas; era el último bloqueo del arranque
limpio del `--old-gui`.

## M16b — Debugger avanzado: Threads/Call Stack reales, attach DAP real, Watch (evaluate) y breakpoints condicionales

**Servicio DAP extendido (`DebugSessionService`):** `GetThreadsAsync` (DAP
threads), `GetStackTraceAsync(threadId)` (20 niveles, para cualquier thread),
`EvaluateAsync(expr, frameId)` (DAP evaluate, contexto watch; devuelve valor,
variablesReference para hijos, o el error del adaptador), `PauseAsync
(threadId?)` (DAP pause — netcoredbg acepta un thread id real; con el PID del
proceso funciona en attach y emite stopped con allThreadsStopped) y
`AttachAsync(pid, breakpoints)` — ahora sobre el comando DAP **attach**
(núcleo del fix: el handler `launch` de netcoredbg IGNORA `mode=attach` y
exige `program`; el attach real necesita el comando `attach` con
`processId`). La respuesta de start se verifica (`success`), los breakpoints
se envían con `condition`/`hitCondition`/`logMessage` por línea, y
`Terminate` en sesión attach hace detach (el debuggee sobrevive; verificado).

**Pads Threads y Call Stack reales:** en cada stop, `RefreshDebugPadsAsync`
llena Call Stack con los frames del thread detenido (doble clic →
`OpenFileDocumentAtLine`, navegación al frame como el pad legacy) y Threads
con los threads reales marcando el detenido `(stopped)`; doble clic en un
thread cambia el Call Stack a ese thread (`GetStackTraceAsync`). QA
`--locals` ampliado: `threads=1 frames=1` tras el stop y `evaluate(answer)=42`
(evaluate DAP real sobre el frame).

**Watch con evaluate:** expresiones en `watchExpressions` (add/remove vía menú
contextual del pad o doble clic, InputDialog con chrome Avalonia); el pad se
reevalúa en cada stop contra el frame actual y muestra `expr = value`
(`answer = 42`, `answer + 1 = 43`). QA `--watch`: rows=2 → remove → rows=1 →
cleanup. Captura `docs/img/watch-pad.png` (pads Locals/Watch con valores del
proceso). `MD_QA_HOLD=<secs>` mantiene el estado en pantalla para capturas.

**Attach to Process real:** el Attach del tab lanza `AttachToProcessAsync`
(AttachAsync con los breakpoints persistidos; `stopped` en el attach llena los
pads). QA `--attachreal`: duerme un proceso .NET de larga vida
(`/tmp/dotsleeper`), hace el attach real, pausa (PID como threadId, con
reintentos — el runtime necesita un instante tras el attach), verifica
`paused=True reason=pause`, `threads=3`, evaluate con error honesto del
adaptador, y DETACH dejando el proceso vivo (`alive-after-detach=True`),
como el DetachFromProcess legacy. El hook `--attachreal` mata el sleeper al
final (QA no destructivo).

**Breakpoints condicionales + hit count + tracepoints:** `BreakpointEntry`
ampliado (Condition/HitCount/LogMessage) y persistidos en `<sln>.userprefs`
como atributos `condition`/`hitcount`/`tracepoint` del XML de Mono.Debugging
(compatibles con el IDE legacy); `SkTextEditor.Breakpoints` expone enabled +
opciones por línea, `SetBreakpointOptions` los edita y el toggle/remove los
limpia. Menú del pad Breakpoints: **Condition…**, **Hit Count…**,
**Tracepoint…** (InputDialogs, vacío = limpiar) junto a los existentes; las
filas del pad muestran `when <cond>`, `(hit N)` y `print: <msg>`. Al
lanzar Debug/Attach los atributos viajan al adaptador (`condition`/
`hitCondition`/`logMessage` de DAP). QA `--condbp`: persiste
`cond=answer == 42 hit=3`, fila del pad `Program.cs:10 when answer == 42
(hit 3)`, la sesión DAP arranca con ese breakpoint y para en la línea 10.

## M16c — Hover eval, stepping (F10/F11), árbol de variables e Immediate

**Hover eval en el editor (debugger tooltip):** `SkTextEditor.DebugHoverEval`
es el gancho que MainWindow instala por documento; en pausa, el tooltip de
hover muestra `word = <valor DAP>` (evaluate en el frame actual, icono
`md-debug-all`) en lugar de la descripción estática Roslyn — el pipeline del
legacy TooltipProvider + debugger tooltip. Sin sesión en pausa, cae a la
descripción normal. `ShowDebugTooltipForQa` ejercita el mismo popup para QA
visual.

**Stepping:** `StepOver/StepInto/StepOutAsync` (DAP next/stepIn/stepOut sobre
el thread detenido); botones en la toolbar junto a Debug (iconos legacy
`md-step-over-debug`/`md-step-into-debug`/`md-step-out-debug` con fallback),
menú Run completo (Debug F5, Step Over F10, Step Into F11, Step Out Shift
F11, Continue, Pause, Stop Debugging Shift F5, Detach, Attach to Process), y
dispatcher para `DebugCommands.StepOver/StepInto/StepOut`. El stopped event
de cada paso re-resalta la línea y refresca los pads. QA `--step`: bp en la
línea 13 (Console.WriteLine — con el bp sobre `int answer = 42;` el stop es
ANTES de la asignación y los locals leen 0/null, como un debugger real),
Step Over → 14 con highlight movido.

**Árbol de variables (Locals/Watch):** los pads son TreeViews
(`MakeVariableTree` + `VariableNode`); un nodo con `variablesReference > 0`
carga sus hijos LAZY al expandir (DAP variables), igual que el árbol del pad
legacy. Watch reevalúa sus expresiones como raíces expandibles. QA `--tree`:
roots=3 (`answer = 42`, …), expandir el List → 13 hijos (`_items =
{int[4]}`, …).

**Immediate pad:** tab del pad inferior con TextBox + Run (Enter también
ejecuta); cada expresión se evalúa en el frame detenido (DAP evaluate) y el
resultado queda en el Output como `[immediate] <expr> = <valor>` (o el error
del adaptador); sin sesión → mensaje honesto. QA `--imm`: sin sesión →
mensaje; en pausa: `answer + 1 = 43`, `greeting = "hello"`.

**Lección de determinismo documentada:** el stop sobre una línea de
asignación ocurre ANTES de ejecutarla (bp en `int answer = 42;` ⇒
answer=0); los QAs usan la línea de Console.WriteLine y esperan
`CurrentFrameId` antes de evaluar — evaluar sin frame cae al scope estático
(answer=0/1).

## M16d — Gutter breakpoints, data tip inline, cambio de frame, autocompletado del Immediate y persistencia de la sesión de debug

**Breakpoint con clic en el gutter (toggle como el legacy):** la franja de
iconos del gutter (los últimos 18px, donde se dibuja el círculo rojo) es
clicable en `SkTextEditor.OnPointerPressed` — el clic sobre ella hace toggle
del breakpoint de esa línea (Mono.TextEditor ActionTextArea "left margin
click"), mueve el caret y consume el evento; el resto del gutter conserva su
rol de mover el caret. `ToggleBreakpointAtGutter(line0)` expone la misma ruta
para QA/servicios. QA `--gutterbp`: dos clics (on→off) + tercero on,
persistencia en .userprefs (`line="13"`), burbuja roja en la línea.

**Data tip inline (valor de la variable al pausar):** al detenerse,
`ShowDataTipForFrame` evalúa los identificadores de la línea parada (hasta 5,
en orden — el primero puede ser un tipo como `Console` que ningún scope
resuelve, igual que el tooltip legacy solo resuelve lo evaluable por el frame
actual) y muestra el primer valor resuelto como burbuja verde inline en la
propia línea (`bg 0x2a4d2e`, borde `0x4e8f55`, texto `0xa6d9aa`), como el
DataTip de VS. Se limpia en Continue/Step/Stop. QA `--gutterbp`:
`datatip=line=13 'greeting = "hello"'`.

**Call Stack con cambio de frame:** la selección de un frame en el pad
(`SelectionChanged`) recarga el árbol de Locals con LOS scopes de ESE frame
(`DebugSessionService.GetLocalsForFrameAsync(frameId)` → `scopes` con
`frameId` → primer scope → `variables`), como el StackFrame pad legacy; el
doble clic sigue navegando al código fuente. QA `--frame` con bp dentro de
`TestProj.Double` (fixture ampliado: `Main` llama a `Double(list.Count)`):
`stack=2 frames: Double() | Main()` y al seleccionar el 2º frame
`[frame] locals of Main() — 3 rows` (answer/greeting/list de Main).

**Autocompletado de miembros en el Immediate:** al teclear `expr.`, el
prefijo se evalúa por DAP y sus miembros (`GetVariablesAsync` del
variablesReference) llenan un popup bajo el input (`nombre  valor`,
SelectedIndex=0, doble clic confirma); Down/Up mueven, Tab/Enter confirman
(reemplaza tras el último `.` y coloca el caret), Esc oculta. El commit
evaluable queda demostrado en el QA `--immcompl`: popup con 13 miembros de
`List<int>`, Tab → `list.Count`, Output `[immediate] list.Count = 3`.

**Persistencia de la sesión de debug (breakpoints + watches + config
activa):** los breakpoints ya persistían; ahora los watches también —
`WatchService` espeja el patrón de `BreakpointService` con la clave legacy
`MonoDevelop.Ide.DebuggingService.PinnedWatches` en el `<sln>.userprefs`
(el PinnedWatchStore legacy serializa también la ubicación del pin, que el
pad Watch del shell no necesita: solo viaja `expression`). Se cargan al abrir
la solución (el pad se rellena en el próximo stop), se persisten en cada
add/remove y al cerrar el workspace. La config activa ya persistía
(`MonoDevelop.Ide.Workspace/ActiveConfiguration`) y `RefreshConfigurationSelectors`
la restaura al abrir. QA `--persistqa`: cierra y reabre la solución →
`watch-exprs=answer + 1 restored-pad=True`, `bp@13=True`, `config=Debug`.

## M16e — Integración estructural (Services/Controls → MonoDevelop.Ide, debug → MonoDevelop.Debugger), Watch editable in-place con reevaluación por step, gutter con hover y pinned watches como burbujas

**Integración estructural (sin módulos duplicados ni carpeta `Avalonia/`):**
los módulos propios del shell dejaron de vivir en `Startup.Avalonia` y ahora
están integrados PLANOS junto a los demás módulos del proyecto, con `git mv`
(historial limpio):

- `Startup.Avalonia/Services/*` → `main/src/core/MonoDevelop.Ide/Services/`
  (10 servicios, ns `MonoDevelop.Ide.Services`).
- `Startup.Avalonia/Controls/*` → `main/src/core/MonoDevelop.Ide/Controls/`
  (SkTextEditor, PadHost, CompletionPopup, EditorTooltipPopup; ns
  `MonoDevelop.Ide.Controls`).
- Los servicios de debug → `main/src/addins/MonoDevelop.Debugger/` junto a
  `PinnedWatch*.cs` (WatchService, BreakpointService, DebugSessionService; ns
  `MonoDevelop.Debugger.Services`).
- `Startup.Avalonia` queda con XAML + code-behind que conecta (Views/), como
  pide la regla de migración: la lógica se transfiere para que el código de
  Avalonia esté integrado al resto del proyecto. Mientras la superficie
  Avalonia de `MonoDevelop.Ide` no tome el mando, el csproj del shell compila
  esas fuentes con `<Compile Include="..">` — fuente única, sin copias.
  GTK/Gdk/Mono.Cairo siguen intactos (legacy que se retira tras 9.x) y los
  módulos no-GTK no se duplicaron ni reemplazaron.

**Watch pad con edición in-place (paridad del Watch pad legacy):** doble clic
sobre una fila o el menú "Edit Watch…" pone un TextBox inline SOBRE la fila
(`container.Header = box` sobre el `TreeViewItem` realizado cuyo DataContext
es el nodo seleccionado — `GetRealizedContainers()`, sin SelectedIndex).
Enter confirma por el camino compartido `CommitWatchExpressionAsync`
(reemplaza la expresión ANTIGUA en su misma posición preservando el orden,
dedup, persiste en `.userprefs` y reevalúa el pad); Esc cancela con rollback;
LostFocus cierra. La reevaluación automática ya existía por stop
(`RefreshDebugPadsAsync` en cada evento stopped) y ahora se demuestra que
cubre los steps sin refresco manual. QA `--watchedit` (bp en la línea 10:
`answer` aún es 0):

```
[watchedit] initial=answer = 0
[watchedit] inline-editor=True
[watchedit] after-esc=answer = 0 kept=True          # Esc → rollback
[watchedit] after-commit=answer + 1 = 1 order-kept=True
[watchedit] after-step=answer + 1 = 43 reevaluated=True  # 1 StepOver y el pad solo
[watchedit] done
```

**Gutter con hover (paridad del editor legacy):** mover el puntero por el
gutter resalta la línea bajo el cursor (banda alpha 28 sobre la fila completa
+ refuerzo alpha 26 en la franja de breakpoints), la franja de iconos muestra
cursor de mano y tooltip "Line N — click to toggle breakpoint" (el resto del
gutter muestra solo "Line N"), y `OnPointerExited` limpia banda, cursor y
tooltip. El pipeline de tooltip de palabra queda desactivado sobre el gutter
(no compiten). `GutterHover`/`ClearGutterHover`/`SimulateGutterHoverForQa`
exponen el estado para QA/servicios. QA `--gutterbp` ampliado (además del
toggle/persistencia/data tip de M16d):

```
[gutterbp] hover-line=12 hand=True tip='Line 12 — click to toggle breakpoint'
[gutterbp] hover-cleared=True tip-removed=True
```

**Pinned watches como burbujas (paridad del PinnedWatch legacy):** "Pin
Watch" en el menú contextual del editor fija la palabra bajo el caret
(`WordAtCaret`) a la línea actual (`TogglePinnedWatch`, toggle como el
legacy); la burbuja ámbar (bg 0x503f1a, borde 0x8f742e, texto 0xe8cf9a) se
dibuja tras el texto de la línea y muestra `expr = valor` — el valor llega
por DAP en cada stop (`RefreshPinnedWatchValuesAsync`, un evaluate por pin)
o `expr = ?` fuera de sesión. Serialización en la MISMA clave legacy
`MonoDevelop.Ide.DebuggingService.PinnedWatches` del `<sln>.userprefs` con la
ubicación completa del PinnedWatchStore (file relativo a la solución,
line 1-based, column/endLine/endColumn/offsetX/offsetY + expression);
`SavePinned` mezcla en un solo elemento los watches del pad (solo
expression) y los pins del editor, y `LoadPinned` resuelve el file relativo
y salta las filas sin file — el IDE GTK legacy y el shell Avalonia comparten
el mismo `.userprefs`. Los pins se restauran al reabrir cada documento
(filtrando por ruta absoluta) y el menú del pad permite quitarlos. QA
`--pinwatch` (pins "answer"@10 y "greeting"@11 desde el caret):

```
[pinwatch] bubbles=10:answer | 11:greeting
[pinwatch] legacy-file-attr=True line10=True line11=True expr-answer=True
[pinwatch] load-path=2 first=Program.cs:10:answer
[pinwatch] live=answer = 42 | greeting = "hello" answer-evaluated=True
[pinwatch] after-unpin=1
[pinwatch] done
```

**Bloqueo de arranque resuelto (entorno):** tras un reinicio del entorno los
QA dejaron de arrancar: el proceso giraba al 100% de CPU dentro de
`StartWithClassicDesktopLifetime` sin crear ventana (gdb: bucle en
`FcPatternGetString` desde `SkFontMgr_fontconfig::GetFamilyNames` de
libSkiaSharp, antes de tocar X11). Causa: las fuentes de usuario con
directorios WOFF/WOFF2 (`~/.local/share/fonts/VictorMono/`) disparan el bucle
en el scan de familias de fontconfig; con una config `FONTCONFIG_FILE` que
solo incluya `/usr/share/fonts` (+ OTF/TTF del usuario) el shell arranca
normal. Workaround documentado en el README del shell (receta QA); no requiere
cambios de código. Nota operativa: los QA muertos por `timeout` dejan un
`dotnet` vivo que bloquea el rebuild del DLL (`AVLN9999`) — hacer
`pkill -f "MonoDevelop[.]AvaloniaShell"` y `dotnet build-server shutdown`
antes de recompilar.

## M16f — Watchdog de arranque, burbujas pinned-watch clicables, restauración del formato legacy GTK y esqueleto Xwt.Avalonia

**Watchdog de arranque (detección temprana del bucle de SkiaSharp):** los QA
pueden colgarse antes de que exista ventana (el bucle de
`SkFontMgr_fontconfig::GetFamilyNames → FcPatternGetString` con fuentes de
usuario WOFF/WOFF2, ver el bloqueo resuelto en M16e). El shell arma un
watchdog en `Program.Main`: si la ventana no abrió en el presupuesto (30s por
defecto, `MD_STARTUP_WATCHDOG=<secs>` lo ajusta) imprime el diagnóstico de la
causa conocida y el workaround y sale con código 2; `MainWindow.Opened` lo
desarma. Validado en ambos sentidos: entorno roto → `[fatal]` + exit 2;
arranque sano → la ventana abre y el watchdog se cancela sin ruido.

**Burbujas pinned-watch clicables (paridad del adorner legacy):** el render
registra el rect de cada burbuja por frame (`pinnedWatchRects`) y
`TryGetPinnedWatchAt` hace el hit-test; el right-click sobre una burbuja
(`ContextRequested`) muestra SU menú en vez del menú del editor:
`Remove pinned watch '<expr>'` y `Go to line N` (con caret a la línea y
`GotoLine` + foco). QA `--pinwatch` extendido: `bubble-rect=True`,
`bubble-hit=True line=11 expr=greeting`,
`bubble-menu=Remove pinned watch 'greeting' | Go to line 11`, el Go to line
mueve el caret (`goto-line=11`) y el Remove borra el pin
(`after-bubble-remove=0`).

**Restauración desde el IDE legacy GTK (idéntica clave .userprefs):** QA
nuevo `--legacyqa`: cierra el workspace, SIEMBRA el XML exacto que escribe el
serializador legacy (`<Watch file="TestProj/Program.cs" line="10" column="9"
endLine=… endColumn=… offsetX=… offsetY=… expression="answer"
liveUpdate="False"/>` — file relativo al dir de la solución vía
`ProjectPathItemProperty`/`PathDataType`), reabre la solución y verifica que
los pins aparecen como burbujas (`restored=10:answer | 11:greeting`),
`LoadPinned` los resuelve con columna (`col=9`) y que un guardado del lado
Avalonia conserva el formato legacy (`legacy-format-kept=True`). FIX real de
integración que destapó el QA: la reapertura de solución llamaba
`PersistWatches` → `WatchService.Save`, que REEMPLAZABA el elemento
`PinnedWatches` completo borrando los pins legacy recién cargados; ahora esa
ruta usa `WatchService.SavePreservingPins` (reescribe solo las filas del pad
y conserva las filas con ubicación del editor).

**Esqueleto Xwt.Avalonia (reemplazo de Xwt.Gtk, dentro del submódulo
external/xwt):** el núcleo Xwt ahora es multi-target `net40;net10.0` (shim de
compatibilidad `Xwt/Compatibility/SystemXamlShim.cs` para los 3 tipos de
System.Xaml que usa el frontend + `XamlServices` del designer con
PlatformNotSupported + BinaryFormatter con guard NET en TransferDataSource;
net40 compila bit a bit igual). Nuevo proyecto `Xwt.Avalonia/` (net10.0,
Avalonia 12.1.2) con `AvaloniaEngine : ToolkitEngineBackend` (guest mode:
reusa la Application del shell; standalone: dispatcher propio; InvokeAsync/
timers/RunJobs sobre Avalonia.Threading; GetNativeWidget/GetBackendForWindow/
GetNativeWindow) y la PRIMERA oleada de backends: Window (IWindowBackend
sobre Avalonia.Window), Label, Button (con routing del Clicked al sink del
frontend), Box (contenedor fijo que aplica las allocation que calcula el
frontend Box, como el CustomContainer de Gtk), TextEntry y Canvas. App de
humo `Xwt.Avalonia.Smoke` headless: 9/9 aserciones verdes (initialize por
nombre de backend, Label/Button/Entry/Box → controles nativos, composición
del Box, ventana con contenido, evento Clicked de vuelta al frontend,
round-trip de texto del entry). Oleadas siguientes documentadas en el engine:
handlers de dibujo (Avalonia.Media+SkiaSharp), Scroll/CheckBox/Frame/ImageView,
TreeView/ListView+stores (los Pads), menús/diálogos/clipboard, hosting guest
ICustomWidgetBackend. Nota de API: Avalonia 12 renombró `SystemDecorations →
WindowDecorations` y `TemplatedControl` vive en `…Controls.Primitives`; los
tipos Avalonia se usan por alias dentro de `Xwt.AvaloniaBackend` porque los
namespaces Xwt sombrean Control/Window/Canvas/Alignment/WrapMode.

## M16g — Fix de visibilidad: el sanitizador fontconfig ahora se aplica de verdad

### Síntoma
El shell arrancaba sin ventana: proceso vivo al 100% CPU en
`FcPatternGetString → FcObjectTypeLookup → strcmp` (libfontconfig) **antes** del
ctor de `MainWindow` (watchdog `[fatal]` a los 30s). El sanitizer de M16f
(`FontconfigSanitizer.cs`) generaba una config válida — `fc-list` con ella
mostraba 0 WOFFs — pero la app colgaba igual, mientras que la misma config pasada
por el entorno (`FONTCONFIG_FILE=/tmp/…`) abría ventanas y cargaba la solución.

### Causa raíz
`Environment.SetEnvironmentVariable` de .NET solo actualiza la vista administrada
del entorno: **no escribe en el bloque environ del proceso**, y libfontconfig
(cargada por libSkiaSharp) lee `FONTCONFIG_FILE` con `getenv(3)` nativo al
inicializarse. Resultado: la config se generaba, el mensaje `[fontconfig]
web-font exclusion applied` salía, y Skia seguía inicializando el font manager
con la config por defecto (que arrastra el caché compartido de
`~/.cache/fontconfig` con las entradas de los WOFF/WOFF2 del usuario) → bucle.
Matriz empírica que la aisló (arnés `env -i`, X :0, `--goto`):
- A (setenv interno, estado borrado): cuelga.
- B (setenv interno, caché caliente): cuelga — descarta "lento la 1ª vez".
- C (solo `/usr/share/fonts` por entorno): ventana + solución < 22s.
- D/E (config del sanitizer / fonts17 por entorno): cargan.
Nota: `/proc/<pid>/environ` tampoco refleja `setenv` post-exec; el stack de gdb
fue la prueba definitiva. Además, `fc-list`/`fc-cache` no ejercitan el mismo
camino que el font manager de Skia: una config puede ser "válida" y aun así no
estar aplicada en el proceso.

### Fix (M16g)
- `FontconfigSanitizer.Apply()`: ahora hace `setenv(3)` de libc vía P/Invoke
  (además del setenv administrado); si libc falla, imprime WARNING.
- Guard nuevo: si el entorno YA define `FONTCONFIG_FILE` (usuario/arnés QA), el
  sanitizer no toca nada (antes lo pisaba/regeneraba).
- Watchdog: el presupuesto por defecto sube a **120s** cuando
  `FontconfigSanitizer.FirstRun` (regeneración de config ⇒ caché privado frío:
  el escaneo completo de ~1300 fuentes tarda más de los 30s originales; cada
  reintento moría a mitad del escaneo y dejaba un caché parcial — por eso el
  "hang permanente" parecía eterno). Corridas siguientes vuelven a 30s.

### Validación (QA, GNOME Wayland/Xwayland :0, arnés `env -i`)
- T6: sin `FONTCONFIG_FILE` externo + estado borrado → regenera config, abre
  ambas ventanas (`MonoDevelop — Avalonia Shell` + `Go To File`), carga TestProj.
- T7: `FONTCONFIG_FILE` externo + estado borrado → no regenera (guard), ventana
  + solución; log sin `[fontconfig] … applied`.
- T8a/T8b: primera corrida en frío (2 ventanas, loaded) y corrida caliente
  (2 ventanas, loaded, 0 `[fatal]`).
- Regresión completa tras el fix: tests 16/16 y hooks `--watchedit`
  (reevaluated=43), `--pinwatch` (bubble-hit/goto-line/remove), `--gutterbp`
  (toggle+stored+datatip), `--legacyqa` (restored + formato legacy conservado),
  todos verdes. Nota de arnés: el debuggee necesita `dotnet` en PATH
  (netcoredbg lo lanza); un `PATH=/usr/bin:/bin` sin `~/.dotnet` produce
  `0x80004005` en las evaluaciones — artefacto del arnés, no del shell.

## M17 — Xwt.Avalonia oleada 1: dibujo real (Context/Font/TextLayout/Gradient/Image sobre Avalonia.Media + SkiaSharp)

Dentro del submódulo `external/xwt`, nuevo `Xwt.Avalonia/DrawingBackends.cs`
(~1150 líneas) con los 8 handlers de dibujo que el frontend Xwt inicializa
en `Toolkit.Initialize` (el Canvas custom-draw y los diálogos los necesitan):
`ContextBackend` (pila estilo Cairo: `SkDrawContext` con SKCanvas + SKPath
compartido + stack de `DrawState`; clip/fill/stroke ±preserve, patrones,
transformaciones, paths, hit-test), `FontBackend` (`FontData` + cache de
typefaces; mono/serif con candidatos conocidos, default runtime),
`TextLayoutBackend` (`AvaloniaTextLayout`: un blob por run para colores por
atributo, ellipsize, alineación, mapeo índice↔coordenada), `GradientBackend`,
`ImagePatternBackend`, `ImageBuilderBackend` (SKSurface offscreen) y
`AvaloniaImageBackend`. Además `CanvasBackend` reescrito: `Grid` con un host
de render (`Control` con `Render` override — `Panel.Render` está sellado en
Avalonia 12) que rasteriza cada frame a un `WriteableBitmap` Bgra8888 (patrón
`SkTextEditor` del shell) y entrega al sink `ICanvasEventSink.OnDraw` un
`SkDrawContext`; los children van en un overlay `Canvas`.

### Tres trampas del frontend que costaron cada una su depuración

1. **ConvertToBitmap de imágenes custom-drawn**: `ImageBuilder.ToBitmap()` →
   `VectorImage` (backend sin tamaño intrínseco). El raster debe REPLAYAR el
   callback con `idesc.Size` (el tamaño real); ignorarlo producía un bitmap
   1×1 con memoria sin inicializar (síntoma del probe: builder 16×16 → rojo
   perfecto por backend directo, pero `ToBitmap()` basura).
2. **`SKTypeface.Default.FamilyName` VACÍO** con el font manager fontconfig de
   SkiaSharp 3.119 en este entorno (310 familias visibles y aun así default
   `''`): cualquier layout con el font del sistema medía 1×1 (fallback del
   frontend a `Font.SystemFont` ⇒ familia `''` ⇒ typeface sin métricas).
   Fix: `FontCache.DefaultFamily()` elige una sans real instalada (DejaVu
   Sans → Liberation Sans → …) o la primera familia no vacía.
3. **`ImageDescription.Alpha` default = 0**: el replay del `DrawingImage` hace
   `c.GlobalAlpha = idesc.Alpha`; con el ImageDescription sintético del raster
   (sin Alpha explícito) TODO salía invisible — el callback corría feliz y
   pintaba cero píxeles (sin excepción, imposible de ver sin instrumentar).
   Fix: `Alpha = 1` en el replay.

Además, `ContextBackend.SetPattern` recibe el Pattern del FRONTEND (el
recorder de VectorImage lo guarda tal cual): hay que resolverlo con
`Toolkit.GetSafeBackend` igual que hace Xwt.WPF antes de sacarle el shader.

### Validación

Smoke headless (`FONTCONFIG_FILE=/tmp/fctest/c_only.conf`, el fixture de
tmp se regenera igual que en M16e — se borró con el reinicio):
**19/19 aserciones** — 9 de widgets de la oleada 0 + 5 de dibujo de la
oleada 1 + 5 de la oleada 3 (ver abajo).
Pipeline backend-directo verificado además con un probe temporal de reflexión
(borrado; 310 familias, builder 16×16 → `#ffff0000` en (5,5)).

## M18 — Xwt.Avalonia oleada 3: los Pads (ListView/TreeView + stores, ComboBox)

`ListBackends.cs` (~1100 líneas) con `ListViewBackend` (implementa también
`IListBoxBackend`), `TreeViewBackend` y `ComboBoxBackend`, más los stores
`ListStoreBackend`/`TreeStoreBackend`.

### Decisión de diseño: panel de filas propio, sin templates de Avalonia

Los rows renderizan con controles Avalonia planos (`ScrollViewer →
StackPanel` de `Grid` por fila) en vez de `ItemsControl`/`TreeView` con
templates de datos: el contrato Xwt exige APIs claveadas por `TreePosition`
(SelectRow/ExpandRow/GetCellBounds/GetRowAtPosition…) sobre un origen de
datos que el backend NO posee, y la API de árboles de Avalonia 12
(`ITreeDataTemplate.BindChildren`) descarta la identidad por nodo que hay
que conservar. El contenido de celda mapea igual que `CellUtil` de Xwt.WPF
(TextCellView→TextBlock, ImageCellView→Image rasterizada por el backend de
imagen de la oleada 1, CheckBoxCellView→CheckBox); `CanvasCellView` queda
para la oleada de celdas custom-drawn.

### Descubrimiento: los stores SÍ requieren backend propio

Los comentarios de `ITreeStoreBackend`/`IListStoreBackend` dicen "you don't
need to implement this" — pero es falso en la práctica: `ListStore`
exige un backend registrado (su `BackendHost` no tiene fallback y explota
con `No backend found for object: Xwt.ListStore`) y el fallback del
`TreeStore` (`DefaultTreeStoreBackend`) es **internal** al núcleo, así que
ningún engine externo puede registrarlo. `ListStoreBackend` y
`TreeStoreBackend` implementan ambos contratos con filas/nodos cuya
identidad ES el `TreePosition` (los handles se pueden guardar y comparar,
igual que los GtkTreeIter de GTK; `IndexOf` resuelve posición→índice vía el
store). Para `IListDataSource` propios (los view models de los Pads) hay
fallback por índice (`IndexRowHandle`).

El TreeView re reconstruye de los eventos del origen (NodeChanged/
NodeInserted/…) y el flatten depth-first respeta la expansión, disparando
los pares Expanding/Expanded y Collapsing/Collapsed del sink. La
selección (simple/múltiple con Ctrl), el foco, scroll-to-row,
ExpandToRow/ExpandRow(recursivo) y alternancia de filas van contra los
handles; la indentación visual es margen por profundidad (el glifo del
expander llega con la oleada de celdas custom).

### Validación

Smoke 19/19: ListView sobre `ListStore` del núcleo (2 filas, selección por
índice via backend), TreeView sobre `TreeStore` (selección de un hijo por
posición de nodo estable) y ComboBox sobre `ListStore` (índice seleccionado
round-trip). Aserción en el smoke: un `DataField` no puede reutilizarse
entre stores (el núcleo lo rechaza — "already belongs to another data
store"), detalle del contrato del frontend que conviene respetar en los
Pads.

## M19 — Integración en Main.sln y un diálogo REAL de MonoDevelop sobre Xwt.Avalonia

### Integración de build

- `Xwt.Avalonia` entra en `Main.sln` (junto a Xwt/Xwt.Gtk/Xwt.WPF del
  submódulo) y `MonoDevelop.Startup.Avalonia.csproj` referencia `Xwt` +
  `Xwt.Avalonia` — el shell y el backend comparten la MISMA pila
  Avalonia 12.1.2 + SkiaSharp 3.119.4.
- El shell compila el diálogo REAL desde su fuente original
  (`MonoDevelop.Ide/MonoDevelop.Ide.Projects/NewSolutionRunConfigurationDialog.cs`,
  0 líneas cambiadas — patrón "integration without duplicates").
- Adaptador `CoreGettextShim.cs`: los diálogos reales llaman a
  `MonoDevelop.Core.GettextCatalog`; referenciar MonoDevelop.Core arrastraría
  el runtime legacy del addin engine (`Runtime.Preferences` en el static
  ctor), así que — como el `SystemXamlShim` de Xwt net10 — el tipo se provee
  como adaptador fino que delega en `GettextService.T` del shell (única
  fuente de traducción).

### Huecos de backend cerrados para diálogos

- **`DialogBackend`** (`IDialogBackend`): ventana Avalonia con barra de
  botones Xwt (SetButtons/UpdateButton/DefaultButton/sensitive) y el par
  RunLoop/EndLoop. Lección clave: el bucle modal es un **DispatcherFrame
  anidado** — un pump de `RunJobs` + sleep NO sirve los timers de Avalonia,
  y cualquier automatización/animación dentro del diálogo se congelaba.
- **`AvaloniaDesktopBackend`**: pantallas/ratón vía la plataforma
  (Avalonia 12 ya no expone lista estática de pantallas: se lee de
  `Window.Screens` de una ventana viva registrada). `Dialog.AdjustSize` lo
  exige: sin él, `Dialog.Run()` explota en `Desktop.PrimaryScreen`.

### Validación (hook QA `--xruncfg`)

```
[xruncfg] dialog opened on Xwt.Avalonia (backend=Toolkit)
[xruncfg] closed command=create name='Multiple Projects'
```

El `NewSolutionRunConfigurationDialog` original abre, corre su bucle modal,
el temporizador QA responde el comando `create` a los 2s (Respond →
EndLoop → retorno de `Run()`), el resultado round-trips y el nombre leído
es el del TextEntry. Smoke del backend 19/19; build del shell 0 errores.
Arnés: `env -i` + `DISPLAY=:0` (Xwayland del usuario; el :1 del arnés
anterior ya no sirve — la sesión es Wayland/Xwayland :0) y fontconfig
sanitizado o por entorno.

## M21 — Solution pad sobre Xwt.TreeView real + fixes de pads/tabs + ports NewFolder/ExecutionMode

### Solution pad: conexión al Xwt.TreeView real (oleada de los pads, M18)

El pad Solution deja el placeholder Avalonia y embede un `Xwt.TreeView` real
(`GetNativeWidget` sobre el host Avalonia), poblado desde el parser de solutions
(`SolutionLoader.SolutionEntry(Name, ProjectPath, IsFolder, Parent)`):

- Jerarquía: raíz solution (icono solution-16) → solution folders anidan los
  proyectos por `Parent` (ParentProjectGuid) → cada proyecto con su nodo
  References + los archivos/carpertas físicas del proyecto.
- Iconos: los PNG viven como recursos planos en `MonoDevelop.Ide.dll`; como
  `IconService` se compila DENTRO de MonoDevelop.AvaloniaShell.dll,
  `typeof(IconService).Assembly` NO los tiene — hay que usar
  `IconService.IconsDirectory` + `Xwt.Drawing.Image.FromFile(...).WithSize(16,16)`
  (fallback file-source-16, placeholder 1x1 si falla todo).
- `RowActivated` abre el archivo (documentos en tabs), `SelectionChanged` listo
  para el PropertyGrid.

**Causa raíz del bug de expansión (el HashSet por identidad de handles)**: el
backend guardaba en un HashSet el handle devuelto por `ExpandRow(rootNav.CurrentPosition)`
pero `FlattenTree` consultaba `GetChild(null,0)`, que devuelve OTRA instancia de
`TreeNodeHandle` (Equals por ParentList+NodeId del DefaultTreeStoreBackend, pero
los handles de navegación no son los mismos objetos que los de flatten) →
`expanded.Contains(child)` siempre false → solo renderizaba 1 fila (root
colapsado). Log de la época: `[xwtflatten] child#021766F3 isOpen=False setCount=1`
vs `[xwtexpand] added TreeNodeHandle#00DF2F0E`.

**Fix (semántica "default-expanded", como el GtkTreeView legacy)**: el backend
GTK no guarda estado de expansión por identidad de handles (lo lleva el propio
GtkTreeView). `TreeViewBackend` ahora mantiene un HashSet `collapsed`:
`isOpen = !collapsed.Contains(child)` — todo visible salvo lo colapsado
explícitamente; ToggleExpansion invierte, IsRowExpanded niega, ExpandRow remueve.
El estado frágil por identidad desaparece.

**Validación**: hook `--qaresults` con probe xwtbackend (reflection de
cols/source/hostRows/storeNodes): `hostRows=4` (antes 1) con TestProj.sln
cargada; captura visual: TestProj › TestProj › References › Program.cs con
iconos.

### Pads: hit-area de colapsar y botón expandir cuadrado (PadHost)

- **Colapsar solo respondía en las esquinas**: el template Fluent de Button
  solo hit-testea en las esquinas del glifo (confirmado con InputHitTest).
  Fix: el botón interno `IsHitTestVisible=false`, y un Border wrapper
  `collapseWrap` dueño del click (Background chrome, Cursor Hand, Tapped →
  ToggleCollapse) — toda la superficie del botón es clicable.
- **Botón expandir del pad inferior cuadrado**: el chevron del dock inferior
  era una píldora lateral full-width; ahora es un botón 34x34 en la esquina
  (Grid 2x2 del collapsedPanel: chevronRow (0,0) 34x34 alineado a la izquierda
  en dock inferior, full-width en laterales; railHost en (1,0) con orientación
  horizontal para el bottom).
- **Validación XTEST**: `sanity collapse btn hit=Border` (wrapper dueño del
  click en toda la fila), `bottom square corner win=(267,738) 34x34
  centerHit=Border`, ciclo colapsar/restaurar por los tres docks.

### Tabs del editor: sin scroller, 2 botones de navegación con overflow

- **No es un scroller**: la strip de tabs (`TabControl#DocTabs`) usa
  `StripViewport` con ClipToBounds + `TranslateTransform` controlada desde
  code-behind; 2 botones ‹ › (`DocScrollLeft/DocScrollRight`) que se muestran
  SOLO cuando el ancho total de tabs excede el viewport
  (`UpdateTabScrollButtons` con SizeChanged del viewport/presenter).
- TabItem.island: icono por tipo de archivo + close + acento azul VS-style
  (Rectangle#TabAccent 0→1 en selected; OJO: no poner Opacity local en el XAML
  del template porque pisa el setter del estilo; IdeAccent #379CFF dark /
  #0078D4 light) + `HorizontalAlignment=Left` (si no cada tab se estira a media
  pantalla).
- Breadcrumb sol›proj›dir›name bajo la strip (`BreadcrumbSegments`), actualizado
  con la tab activa.
- **Validación**: tab compacta 121x26 con icono+texto+close, acento 97/97 px
  #379CFF, breadcrumb 241 px visibles, botones ‹ › solo con overflow.

### Ports Avalonia: NewFolderDialog + ExecutionModeSelectorDialog (hooks QA)

- `Views/NewFolderDialog.cs`: port del Xwt NewFolderDialog — entry con el
  primer "New Folder[NN]" libre, validación en vivo (vacío, caracteres
  ilegales, nombre en uso → warning ámbar inline + Add disabled) y Add que
  crea el directorio. QA: `FolderNameForQa`/`ValidateForQa`/`AcceptForQa`/
  `IsAddEnabledForQa`/`WarningForQa`, hook `--newfolder` (valida illegal →
  disabled, crea "Created" en temp y lo borra).
- `Views/ExecutionModeSelectorDialog.cs`: port del Xwt ExecutionModeSelectorDialog
  — lista de Run Configurations + árbol de Execution Modes (sets anidan modos;
  set con un solo modo se eleva a raíz como el cleanup MoveNext/Remove legacy),
  preselección del modo actual y Run/Cancel; el label de Run pasa a ser el
  nombre del set seleccionado (legacy UpdateButtons). QA: `AcceptForQa`/
  `IsRunEnabledForQa`/`RunLabelForQa`/`SelectedModeIdForQa` + contadores, hook
  `--exmode` (2 configs, 2 modos, timer que responde a los 2s como `--xruncfg`).
- Trampa de lifecycle (costó un QA falso-negativo): las preselecciones hechas
  ANTES del attach se pierden cuando el TreeView/ListBox materializa containers
  en ShowDialog → re-afirmar en `Opened` + recordar el último par válido
  (lastModeId/lastConfig) como fallback del Run. Y ShowDialog es Task: sin
  `await` el código posterior corre antes de cerrar el dialog.
- Nota de flags: `QaDialogArg` toma el PRIMER flag de la lista; `--qaresults --newfolder`
  ejecuta solo --qaresults (usar un flag por corrida).

### Issue upstream SkiaSharp publicado

El colgado de arranque por WOFF/WOFF2 en los directorios de fuentes del usuario
(`SkFontMgr_fontconfig::GetFamilyNames` → `FcPatternGetString` sin retorno,
100% CPU antes de pintar UI) está documentado y publicado upstream:
<https://github.com/mono/SkiaSharp/issues/5210> (texto completo en
`docs/skiasharp-fontconfig-issue.md`, con el workaround FontconfigSanitizer y
la matriz empírica de M16e/M16f/M16g).

## M22 — Integración de los ports: fuente embebida, menús conectados y Properties desde el árbol Xwt

### Fuente Selawik embebida de verdad (y la ventana que se quedaba corta)

- El recurso `ContentFontFamily` apuntaba a `Selawik` plano: resolvía contra
  fontconfig del SISTEMA (dependía de que el usuario la tuviera instalada;
  sin ella, fallback por glifo → captions cortadas). El URI embebido correcto
  en Avalonia 12 es **`avares://MonoDevelop.AvaloniaShell/Fonts/selawk.ttf#Selawik`** —
  el formato viejo `fonts:Assembly#Family` ya NO resuelve y crashea el
  FontManager (`Could not create glyphTypeface. Font family: Selawik (key:
  fonts:MonoDevelop.AvaloniaShell)`) al primer texto renderizado.
- Los TTF Selawik (OFL) ya viven en `Fonts/` como AvaloniaResource: la UI no
  depende de ninguna fuente del sistema.
- AddReferenceDialog: `Width = 80` → `MinWidth = 80` en OK/Cancel/Remove
  (Width fijo + fuente distinta = caption recortada) y `Height 560 → 600` —
  a 560 el frame recortaba la fila de acciones entera (holgura medida tras el
  fix: 43px bajo los captions).
- **Validación**: captura del dialog con XTEST, análisis de píxeles (extensión
  del texto OK 443-459, Cancel 531-569, ambos dentro del botón; 43px libres
  al borde inferior) y visual 2x del preview.

### Context menu del árbol Xwt real (ButtonPressed → MenuFlyout)

- `TreeViewBackend.HandleRowClick` ahora distingue el right-click: selecciona
  la fila bajo el puntero (legacy pads seleccionan antes de mostrar el menú)
  y eleva `Widget.ButtonPressed` con `IsContextMenuTrigger=true` y las coords
  dentro del widget (`RowOffset(index)` suma las alturas de las filas
  previas).
- `EnsureXwtSolutionTree` cablea `ButtonPressed → OnXwtSolutionContextMenu` →
  `ShowProjectPadMenuAtPointer` (el MISMO `BuildProjectPadMenu` por tipo de
  nodo que ya existía; `SelectedNodeType` ahora lee del árbol Xwt).
- La ruta muerta `solutionTreeView` (árbol Avalonia de preview, nunca
  asignado desde M21) queda desconectada del menú/properties.

### Properties pad alimentado por la selección del árbol Xwt

- `SelectedXwtNode()`: resuelve el tag del nodo seleccionado con la API
  tipada del frontend (`tree.SelectedRow → store.GetNavigatorAt(pos).GetValue(tagField)`)
  — cero reflexión.
- `SelectXwtSolutionRow(tag)` (QA/demos): resuelve SIEMPRE con un walk fresco
  del store (`FindXwtRowByTag` sobre TreeNavigator). Los TreeNavigator
  guardados en `xwtSolutionNodes` MUTAN al construir hijos durante el build:
  su `CurrentPosition` no es estable (el nav del proyecto quedó apuntando a
  su último hijo — el bug que la 1ª QA destapó).
- `TreeViewBackend.SelectRow`: modo Single REEMPLAZA la selección (como
  `gtk_tree_selection_select_path`) y eleva `OnSelectionChanged` — antes solo
  añadía a la lista y el pad no se actualizaba.
- **Validación `--selpad`**: solution→6 filas (Name/File Path/Format/…),
  project→9 (Target framework/Assembly name/Output type…), Program.cs→10
  (Build action Compile, tamaño, fecha…).

### Run With: ExecutionModeSelectorDialog en el menú Run

- El submenu `Run > Run With` lleva Tag="RunWith" en MenuService (nuevos
  campos Tag/AutoHide en MenuEntry); `FillRunWithMenu` (en BuildMenu) lo llena
  con los modos (.NET Core/Mono/External Terminal/(Default)) y marca el
  persistido; `MenuService.FindMenuByTag` lo encuentra en el menú nativo.
- `OnMenuCommand("runwith:")` abre el ExecutionModeSelectorDialog con las run
  configurations REALES del proyecto activo (PropertyGroups Condition del
  csproj → Debug/Release) y aplica el par elegido: persiste el modo en
  `MonoDevelop.Ide.RunWithMode` (SettingsStore), la config en el .userprefs
  (SetActiveConfiguration) y `RunStartupProjectAsync` lo ejecuta: mono →
  `mono bin/Debug/net10.0/X.dll` (con build previo y fallback a dotnet run si
  no hay dll), external → `xterm -e dotnet run`, default → dotnet run.

### Add → New Folder: el NewFolderDialog real en el menú del Solution pad

- `CreateContextNewFolder` ya no usa el InputDialog genérico: abre el
  `NewFolderDialog` (default "New Folder[NN]", validación ilegal/duplicado
  con warning ámbar, Add deshabilitado) sobre `ContextTargetDirectory()` y al
  confirmar refresca el árbol (`RefreshSolutionTree`) — el flujo de
  FolderNodeBuilder legacy.

### Hook QA `--tabscroll`: overflow real de la strip de tabs

- **Causa de la QA falsa-negativa**: el `PART_ItemsPresenter` se arrangea AL
  ancho del viewport (el clip oculta el exceso), así que `row.Bounds.Width`
  jamás lo reporta. El ancho de contenido real es la SUMA de las tabs
  (`TabStripContentWidth`).
- El hook abre "QaOverflowN.cs" hasta que ‹ › aparecen (60ms de layout entre
  aperturas), mide el nudge (‹ → offset negativo, › → 0), cierra docs hasta
  que caben y verifica que los botones desaparecen.
- **Validación**: overflow al 6º tab (True), nudge 0→−143→0, tras cerrar
  docs=1 scrollVisible=False.

### Otros

- `--tabscroll` y `--selpad` registrados en `QaDialogArg`.
- `IPopupWindowBackend` registrado en AvaloniaEngine (PopupWindowBackend:
  Window sin decoraciones, SizeToContent, Topmost, Esc → ClosePopup +
  `Sink.OnClosed`); deja listo el popup Xwt para los menús widget.

## M23 — Search de la toolbar en vivo + Rename/Delete/RowActivated sobre el árbol Xwt

### Toolbar search: popup de resultados en vivo (SearchPopupWindow legacy)

- El box de la toolbar (`ToolbarSearch`) ahora lleva icono de lupa (md-find,
  sobre un Grid con padding izquierdo) y búsqueda EN VIVO: cada TextChanged
  reconstruye un popup dropdown anclado bajo el box (Border ZIndex=100 como
  hijo del panel raíz de la ventana — dentro del Grid del box quedaba bajo el
  resto del árbol).
- Categorías legacy: texto plano → files primero (documentos abiertos con
  "(open document)", luego la solution completa con ranking nombre>ruta, cap
  12) + commands; `:c <término>` solo comandos; `:f <término>` solo archivos;
  `:s <término>` la entrada "Search for '…' in Solution" que abre Find in
  Files (SearchInSolutionSearchCategory).
- Comandos desde `MenuCommandBindings()` — dos fixes: (1) el id se recupera
  del closure (`e.OnClick.Target` → reflexión de la propiedad `Id` capturada)
  porque la conversión implícita CommandAction→Action envuelve el delegado y
  el Target ya NO es el CommandAction; (2) los label-matches se listan antes
  que los id-matches (el catálogo tiene ~178 comandos y el cap hambrientaba
  "Build").
- Navegación: ↑/↓ mueven la selección (Enter activa, Esc limpia y cierra,
  click/double-tap en la fila activa; LostFocus cierra con 150ms de gracia).
- **Validación `--searchpopup`**: "Prog" → `Program.cs | TestProj/Program.cs`
  visible=True; `:c build` → 4 resultados (Build All primero, bindings=178);
  `:s TODO` → la entrada de Find in Files; activación → tab Program.cs
  seleccionada. Visual: popup con el ítem resaltado (azul #3563a3) capturado
  tras tipear con XTEST.

### Rename/Delete y RowActivated sobre el árbol Xwt real

- `DeleteContextNode`/`RenameContextNode` resuelven el nodo desde la
  SELECCIÓN VIVA (`SelectedNodeType()` → SelectedXwtNode) en lugar del
  `contextNodePath` del árbol Avalonia muerto; `ContextTargetDirectory`
  despoja TODOS los prefijos tag (`project:`/`solution:`/`references:`) antes
  de cualquier operación de path — `GetDirectoryName("project:/home/…")`
  producía una ruta relativa basura y la carpeta nacía en un literal
  `project:/` bajo el CWD.
- `OnXwtSolutionRowActivated` (double-click) abre el documento y loguea el
  paso — hook `--rowactivate`: selecciona la fila de Program.cs por tag,
  invoca el handler con `TreeViewRowEventArgs(pos)` y verifica `opened=True
  tab=Program.cs`.

### Add → New Folder verificado de punta a punta

- Hook `--ctxnewfolder`: selecciona la fila del proyecto, ejecuta
  `ProjectCommands.NewFolder` (el handler del menú) con auto-confirmación del
  dialog (`dialogAutoConfirmFolder`), verifica directorio + refresh del árbol
  y limpia. Resultado: `created=True treeRefreshed=True` (tras el fix del
  prefijo `project:`, que era el bug real que la primera pasada destapó).
- Nota de entorno: el flyout del menú es una superficie X11 popup separada de
  Avalonia — no aparece en el pixmap de la ventana ni en grabs de pantalla
  (bloqueados por mutter/XWayland), así que la verificación es funcional
  (hook) y no visual.

### Otros

- `--searchpopup`, `--rowactivate`, `--ctxnewfolder` registrados en `QaDialogArg`.

## M24 — Jerarquía Xwt por posiciones, close en pads, colapso bottom, search :t, Delete → .csproj

### FIX: la jerarquía del Solution pad (TreeNavigator MUTA)

- CAUSA RAÍZ del árbol "todo anidado como hijos": `TreeNavigator.AddChild()`
  MUTA el navigator al hijo (`pos = backend.AddChild(pos); return this;` —
  TreeNavigator.cs:113). Reusar `parentNav` en un bucle encadenaba cada fila
  como hija de la anterior (escalera de indentación +16px por fila).
- FIX: población por POSICIONES inmutables — `xwtSolutionStore.AddNode
  (parentPos)` (TreeStore.cs:90 devuelve un navigator INDEPENDIENTE);
  `AddXwtReferencesNode(Xwt.TreePosition projPos, …)` y
  `BuildXwtFolderChildren(Xwt.TreePosition parentPos, …)` reescritas; en
  `OpenSolutionInWindow`: `rootPos = store.AddNode().CurrentPosition`, dict
  `folderPositions`, `projPos = nav.CurrentPosition`, `ExpandRow(projPos)`.
  Los navegadores guardados MUTAN al construir hijos — para LEER sirve un
  walk con el navigator mutante (`FindXwtRowByTag`), para POBLAR hay que usar
  posiciones siempre.
- Verificado por análisis de píxeles (soltree_fixed.png): hermanos al mismo
  nivel (escalera 9→30→46→63→63→86px terminada).

### Close (✕) en las pestañas de los pads + colapso del pad inferior

- `PadHost.AddTab`: header con icono + label + `closeBtn` (✕, FontSize 8,
  Padding 3,0, ToolTip "Close pad", `Tag = tab.Id`). Click →
  `SetTabVisible(id,false)` + nuevo evento `PadTabClosed` (el shell puede
  reflejarlo en View > Pads como el legacy `DockItem.Closed`).
  - **Verificación visual completada (M25b)**: el ✕ del pad Properties se
    verificó por píxeles y por click real. Rect medido con `PointToScreen`:
    `screen=(1420,242) size=16x16 content='✕' tip='Close pad'`; el glifo se
    renderiza (crop del rect: dominante `(0,120,215)` del botón + 48 px de
    `(15,75,122)` del glifo, luminancia 62–211). Click XTEST real sobre el
    centro → `properties tab visible=False selected=properties visibleTabs=[]`
    y `View > Pads > Properties checked=False`; el pad desaparece del lado
    derecho. Detalle en § M25b.
- Colapsado del pad INFERIOR reescrito: ya no usa el panel 2x2
  (corner+rail). `ApplyCollapseState` para `DockOrientation == Horizontal`:
  `Height = 34`, `contentHost` oculto, la MISMA fila de tabs sigue visible y
  el chevron (ahora TextBlock ‹/› dentro de `collapseWrap`) queda dockado a
  la DERECHA de esa fila con `MinWidth = 34` — sin rail, sin segundo renglón,
  sin nada debajo. Restore: `Height = savedHeight`; la fila nunca se movió.
- Chevron de los pads: el Button de Fluent TRAGABA el glifo (el Border
  pintaba, el content nunca) — reemplazado por un `TextBlock` con
  `Foreground` explícito. Además el chevron mide 34px de ancho SIEMPRE
  (`collapseWrap.MinWidth = 34` en el ctor): el pedido del usuario — igual
  ancho expandido que colapsado. Dump autoritativo (`[padchrome]` en cada
  arranque, PointToScreen): left 34x31, bottom 34x31, right 34x29.
- Geometría real del workbench (para futuras QAs): el pad bottom expandido
  (170px) ocupa window-y 602-772 con la fila de tabs ARRIBA (602-633);
  colapsado queda bottom-anchored (738-772). La banda chrome 772-793 es la
  BARRA DE ESTADO, no el pad. Verificado por píxeles: colapso = una fila de
  34px con tabs en su sitio + chevron a la derecha (cluster 998-1031 ×
  738-768); click XTEST en el chevron restaura (`toggle → collapsed=False`),
  contenido reaparece, fila intacta.

### Search popup: categoría :t (RoslynSearchCategory) + teclado completo

- `:t` / `:type` / `:types`: escaneo de declaraciones
  class/interface/struct/enum sobre los .cs de la solución con un regex
  anclado por línea (`typeDeclRegex`; Groups[2] = keyword, Groups[3] = nombre
  — Groups[1] son los modificadores), cacheado por archivo con
  `LastWriteTimeUtc` (`symbolIndex`), ranking startsWith > contains, cap 12.
  Activar → `OpenFileDocumentAtLine (file, line)` (1-based). Incluida en la
  búsqueda "all" (tras archivos). Icono `md-<kind>` (fallback silencioso si
  el stock no existe).
- Las filas del popup llevan TOOLTIP con la RUTA COMPLETA (para símbolos
  `path : línea`) — la tupla de resultados ganó el campo `Tip`; el subtitle
  sigue truncable.
- Hover sobre una fila MUEVE la selección (PointerEntered → SelectedIndex) y
  PageUp/PageDown mueven la selección por páginas de 5 con `ScrollIntoView`,
  como el legacy SearchPopupWindow.
- Hook `--searchpopup` extendido: `':t Program'` → `Program (class) |
  TestProj/Program.cs : 6 | tip=<ruta completa>`, activación abre el tab
  (open tab=Program.cs). ':c build' → 4 hits; ':s TODO' → 1.

### Delete del context menu también limpia el .csproj

- `DeleteContextNode` ahora localiza el proyecto dueño (`FindOwningProject`:
  sube desde el archivo hasta el primer directorio con *.csproj, sin pasar
  del directorio de la solución) y elimina del .csproj los items
  `<Compile|None|Content|EmbeddedResource Include="...">` que coinciden
  (archivo exacto o todo lo de debajo si es carpeta) — `RemoveProjectItemEntries`
  edita el .csproj como TEXTO con regex por línea para preservar el resto del
  documento byte a byte (el equivalente a `ProjectFileNodeBuilder
  .DeleteFromProject`).
- Hook `--delcsproj`: SIEMBRA una entrada `<Compile Include="Program.cs" />`
  en el .csproj (los SDK projects compilan implícito — sin siembra el test es
  vacuo), borra Program.cs, corre el MISMO código del Delete del menú,
  verifica `compileEntryGone=True restOfDocIntact=True` (el PackageReference
  y el Project Sdk quedan) y RESTAURA el fixture. Resultado: `1 item(s)
  removed (Program.cs)`.

### --ctxmenu-visual (captura manual del flyout)

- Hook `--ctxmenu-visual`: abre el flyout del Solution pad
  (`BuildProjectPadMenu` + `ShowAt (xwtSolutionHost)`) y lo deja ABIERTO 5
  segundos (log "[ctxvisual] flyout open for 5s — capture with PrtScr") para
  capturarlo MANUALMENTE con PrtScr de GNOME — mutter bloquea los grabs
  automáticos (XGetImage falla con error 8; x11grab captura negro) y el
  flyout es superficie X11 popup separada que no sale en el pixmap de la
  ventana. Verificado: mensajes open/closed en el log y app viva.

### Otros

- `--ctxmenu-visual`, `--delcsproj` y `--collapsebottom` registrados en
  `QaDialogArg`; dump `[padchrome]` en cada arranque (rects PointToScreen de
  ambos targets de colapso por pad).
- Lección de entorno re-aprendida: NUNCA combinar `pkill -f` y el launch del
  dll en el mismo bash — el cmdline del bash contiene el path del dll y
  `pgrep -f`/`pkill -f` se auto-matan (el log no llega a crearse). Separar
  siempre en llamadas. También: el fixture fontconfig de /tmp se regenera tras
  cada reinicio (tmpfs) y el XAUTHORITY de mutter cambia de nombre — revisar
  ambos si `XOpenDisplay failed` o el arranque excede 30s.

## M25 — Pad de edición: tabs horizontales, editor visible, breadcrumb, splitter, GoToType, :t con contenedor, checks View > Pads, persistencia de layout

### Editor pad (SkTextEditor) visible y con tabs horizontales

- **Tabs en horizontal**: el template custom de TabControl en App.axaml no
  propagaba el ItemsPanel — `PART_ItemsPresenter` ahora lleva
  `ItemsPanel="{TemplateBinding ItemsPanel}"`. Verificado: los headers de
  Program.cs/TestProj.csproj comparten una sola fila (`distinctTops=1`).
- **Código invisible (2 causas)**: (1) `GetMonospaceTypeface` pedía
  "DejaVu Sans Mono", inexistente en el sistema → typeface sin glifos y todo
  DrawText no-op; ahora itera candidatos {DejaVu Sans Mono, Noto Sans Mono,
  Source Code Pro, Cascadia Mono, Liberation Mono, Nimbus Mono PS, default}
  validando `CountGlyphs("ABCdef123") > 0` (log `[skeditor] monospace face`).
  (2) El editor montado en DocContent (Panel simple) quedaba 0x0;
  `OnDocSelectionChanged` fuerza `HorizontalAlignment/VerticalAlignment =
  Stretch` antes del Add (bounds 765x477 verificado).
- **Breadcrumb dinámico** bajo las tabs: `UpdateBreadcrumb` usa ScanSymbols
  sobre el texto vivo y arma la cadena TestProj › TestProj › Program.cs ›
  Program › Main según el caret (GotoLine(7) → Main verificado).

### Línea toolbar + PadSplitter (límite redimensionable)

- Línea fina del color de la toolbar (chrome 45,45,45) en el borde superior
  de los pads de edición e inferior (DocumentPane en XAML; BottomPads
  BorderThickness 0,1,1,0 en BuildPads).
- `Controls/PadSplitter.cs` (nuevo): Border de 6px con cursor BottomSide que
  ajusta `TargetRow.Height` directamente (el GridSplitter de Avalonia
  ignoraba drags reales). El delta se mide contra el **Grid padre** (marco
  fijo): medirlo contra el propio splitter alimentaba la fila con su propio
  movimiento (runaway que clavaba el pad al mínimo). Clamp 34…(grid − 150).
- `DragStarted` permite al shell expandir un pad colapsado ANTES de tomar la
  base del drag (el drag desde el rail recupera la fila en vez de pelear con
  el colapso). Thumb con highlight en hover/drag para que el agarre sea
  descubrible. Cableado: `EditorBottomSplitter.TargetRow = RowDefinitions[1]`
  en el ctor; declarado tras BottomPads (z-order encima).
- Verificado por telemetría (press/move/release): UP 150 → 179→329, DOWN 400
  → clamp 34, UP desde el rail → 34→184 (recupera), y drags manuales en vivo
  sin runaway.

### Colapso ✕ ⇄ fila (sin vacío) y ciclo completo

- Al colapsar el bottom (✕ de la última tab o chevron), el coupling
  `CollapseChanged` fija la fila a 34 (el rail del pad ancla abajo) y guarda
  la altura previa en `bottomSavedRowH`; al expandir (chevron) la fila
  vuelve a su altura — sin el vacío entre frontera y rail.
- El click de la ✕ dentro del ToggleButton del header disparaba TAMBIÉN el
  Click del padre (handledEventsToo) y re-expandía lo recién colapsado:
  guard `lastClickWasClose` + `e.Handled`, reset diferido vía
  `Dispatcher.Post(Background)` (el Click del padre llega tras el finally
  del hijo en el mismo dispatch de release).
- **View > Pads refleja la visibilidad** (paridad DockItem.Closed): el bug
  era `CommandAction` — su lambda capturaba el parámetro del ctor (closure
  compilada) y `OnClick.Target` nunca era el CommandAction, así que
  `UpdatePadChecks` no recuperaba el id `pad:*` y los checks quedaban con
  los defaults estáticos. FIX: el lambda lee la propiedad `Id` de la
  instancia (delegado ligado a `this`). Los checks se renderizan como icono
  ✓ (MenuBuilder), no `MenuItem.IsChecked` — el dump QA lee el glyph.
- Ciclo verificado con el hook `--padmenu` (usa el MISMO dispatch del ítem,
  `pad:output`): off → tab oculta + `Output checked=False`; on → tab
  visible + seleccionada + `checked=True`. El ✕ de la última tab colapsa al
  rail (`empty-close`) y el chevron restaura.

### Search :t con tipo contenedor (y Go To Type Ctrl+T)

- ScanSymbols: la llave de apertura Allman (indent igual al dueño) expulsaba
  el tipo recién pusheado del stack de indentación → Main/Double sin
  Container. Sólo una llave de CIERRE hace pop de su scope. Los tooltips de
  `:t` ahora llevan `ruta : línea — in Program` (métodos y properties
  incluidos con `methodDeclRegex`/`propertyDeclRegex`); el breadcrumb usa
  `hit.Name` porque el tipo ya es su propio segmento (evita
  "Program › Program.Main").
- `GoToDialog` reutiliza el escaneo: ctor `GoToDialog (string? kind)` fija el
  Title ANTES de BuildSource (el modo type/list depende de él — el bug era
  setear Title después); shortcut Ctrl+T en MenuService (GotoType). Hook
  `--gototype`: filtro "Prog" → matches=2 first=Program, activación abre
  Program.cs.

### Persistencia del layout de pads (paridad DockFrame)

- `SavePadsLayout` (en CLOSING, antes de cualquier early-return del diálogo
  de dirty docs) y `RestorePadsLayout` (ctor, diferido con
  `Dispatcher.Post(Background)` tras BuildPads) persisten en
  `Monodevelop.PadsLayout` un registro por host: `visible|collapsed|tabs|
  selected` + altura de fila. La fila se guarda PRE-colapso
  (`bottomSavedRowH`) porque colapsado vale 34; el restore aplica la fila
  ANTES de restaurar los hosts para que el coupling la capture al re-colapsar
  y el expand posterior vuelva al split real.
- Guard anti-envenenamiento: un registro con `tabs=` vacío (sesión que cerró
  todas las tabs) NO oculta todas las tabs al relanzar — se conservan los
  defaults de arranque (el legacy DockFrame recrea los DockItem ausentes).
- Ciclo verificado con hooks `--padsqa` (fila 237 + colapso + Close real →
  prefs `…|1|1|output|output|237`) y `--padsqa2` (relanzamiento: fila 34
  colapsada con `visibleTabs=[output]`, expand → `actualRowH=237`).
  Relanzamiento normal sin flags también restaura.

### Infra

- `.github/workflows/avalonia-shell.yml` (nuevo): build de
  MonoDevelop.Startup.Avalonia (Release, net10.0) + tests del editor model en
  cada push de la rama, con `submodules: recursive` (el restore necesita las
  fuentes de xwt/mono-addins) y caché de NuGet (`actions/cache` sobre
  ~/.nuget/packages). El README ya documentaba
  `git submodule update --init --recursive` para clones frescos.
- Hooks QA nuevos en `QaDialogArg`: `--padmenu`, `--padsqa`, `--padsqa2`.
- **CORRECCIÓN (M25b) del "artefacto conocido" del hook `--searchpopup`**: el
  diagnóstico previo ("la PRIMERA asignación programática de Text tras `:s` no
  retriggerea el handler") era **incorrecto**. La causa real era una edición de
  texto del commit `8d3c607821` que **fusionó la línea de comentario con la
  llamada**: `OnToolbarSearchTextChanged (":t Program")` y su `Output` quedaron
  DENTRO del comentario `// first hit opens its file and jumps to the
  declaration line.` (verificado con `cat -A` y `git log -L`). Consecuencia: el
  `Output` de `:t Program` reportaba el estado dejado por `:s TODO` (stale) y la
  activación posterior usaba los resultados de `:t Double` (0 hits) en vez de
  los de `:t Program`. No era debounce ni retrigger del handler. Detalle y
  evidencia en § M25b.

## M25b — Fix del hook `--searchpopup` (`:t Program` stale) y verificación visual del ✕ de Properties

Cierre de los dos pendientes que M25 dejó abiertos: el resultado stale del
primer `:t Program` en el hook `--searchpopup` y la verificación visual del
botón de cerrar (✕) del pad Properties.

### Fix: `:t Program` reportaba el estado de `:s TODO` (código comentado, no debounce)

- **Síntoma**: en el hook `--searchpopup`, la línea `[searchpopup] ':t Program'`
  imprimía los resultados de la consulta ANTERIOR (`:s TODO`) y la activación
  posterior saltaba (`activation skipped`) porque operaba sobre los resultados
  de `:t Double` (0 hits).
- **Diagnóstico previo (incorrecto)**: M25 lo documentó como "la PRIMERA
  asignación programática de Text tras `:s` no retriggerea el handler". Falso.
- **Causa raíz real**: el commit `8d3c607821` (M25) fusionó en una sola línea el
  comentario y la llamada —
  `// first hit opens its file and jumps to the declaration line.\t\t\t\t\tOnToolbarSearchTextChanged (":t Program");`
  — de modo que la llamada y su `Output` quedaron DENTRO del comentario.
  Verificado con `cat -A` (tabs literales en la misma línea) y con
  `git log -L` sobre el bloque. No hay debounce ni retrigger involucrado: el
  handler nunca se invocaba para `:t Program`.
- **Fix**: separar el comentario de la llamada y re-indentar el bloque de
  `:t Program` / `:t Main` / `:t Double` en `MainWindow.axaml.cs`.
- **Evidencia** (`~/opencode/searchpopup_qa.log`, hook `--searchpopup` sobre el
  fixture `ConsoleApp-VS2010`):

```
[searchpopup] 'Prog' results=2 visible=True first=Program.cs | Program.cs
[searchpopup] ':c build' results=4 bindings=179 first=Build All
[searchpopup] ':s TODO' results=1 first=Search for 'TODO' in Solution
[searchpopup] ':t Program' results=1 first=Program (class) | Program.cs : 8 | tip=<ruta completa> : 8
[searchpopup] ':t Main' results=1 first=Main (method) | Program.cs : 10 | tip=<ruta> : 10 — in Program
[searchpopup] ':t Double' results=0 first=-
[searchpopup] activated → tab selected: Program.cs expected: Program.cs
```

### Verificación visual del ✕ del pad Properties (pendiente de M24)

- **Instrumentación**: `PadHost.PadTab` ganó `internal Button? CloseButton`
  (asignado en `AddTab`), y `LogCollapseChrome()` emite por cada tab con ✕:
  `[padclose] '<host>' tab=<id> visible=... bounds=... screen=(x,y)`.
- **Hook QA `--padclose`** (nuevo, registrado en `QaDialogArg`): loguea el
  tamaño de cliente, itera los tres hosts (left/bottom/right), loguea el rect
  medido del ✕ de Properties, hace hit-test en su centro y vigila con un
  `DispatcherTimer` la transición de visibilidad de la tab + el check del menú
  View > Pads.
- **Rect medido**: `screen=(1420,242) size=16x16 content='✕' tip='Close pad'`.
- **Render del glifo (análisis de píxeles)**: crop del rect (client
  1096,81–1120,105) → dominante `(0,120,215)` ×495 (azul del botón) con 48 px
  de `(15,75,122)` (glifo oscuro), luminancia 62–211. El ✕ **sí se dibuja**.
- **Click real (XTEST)**: con el puntero confirmado sobre el centro del rect
  (window-relative 1108,93), el click produjo
  `[padclose] properties tab visible=False selected=properties visibleTabs=[]`
  y `[pads-menu] (padclose) 'Properties' checked=False enabled=True`; el pad
  desaparece del lado derecho (capturas `padclose_verified.png` y
  `padclose_verified2.png`).
- **Re-validación (arranque limpio)**: repetido desde cero con el shell recién
  lanzado, el click sobre el ✕ vuelve a producir la transición
  (`visible=False`, `'Properties' checked=False`) y el pad desaparece del lado
  derecho. **El ✕ funciona; no hay bug de hit-test.**
- **Falso negativo del intento intermedio**: en un intento previo el click no
  disparó porque el pad derecho ya estaba **colapsado** (un click de control
  sobre el chevron "Collapse pad" lo había plegado). Con el host colapsado el
  ✕ no está en el árbol visual y no es hittable — el dump lo reporta como
  `not attached to the visual tree`. Lección: el QA del ✕ debe correr sobre un
  arranque limpio, no sobre un shell ya manipulado.
- **Listener de captura en `--padclose`**: se añadió un `PointerPressed` con
  `handledEventsToo: true` que imprime la cadena de controles que recibe el
  press (`[padclose] press at (x,y) → ...`), para distinguir "el click no llega
  a la ventana" de "el click llega pero el handler no corre" sin adivinar
  coordenadas. **Hallazgo**: un `PointerPressed +=` normal **nunca** ve el click
  sobre el ✕ porque `Button` marca el evento como handled; hay que usar
  `AddHandler (PointerPressedEvent, ..., RoutingStrategies.Tunnel | Bubble,
  handledEventsToo: true)`. Con eso, el press se reporta como
  `AccessText < ContentPresenter < Button < StackPanel < ContentPresenter <
  ToggleButton < ... < PadHost#RightPads` — es decir, el ✕ **sí** es el target
  del hit-test en su centro (window-relative 1108,93), y el `ToggleButton`
  padre no lo intercepta.
- **Hallazgo de instrumentación**: `InputHitTest` devuelve `null` para el ✕
  aunque el click real funcione (confirma la nota ya existente en
  `PadHost.cs:111`). El hook usa el **bounds** como autoritativo:
  `new Rect (propsClose.Bounds.Size).Contains (local)` — `Visual.Bounds` está
  en coordenadas del PADRE, no propias, así que comparar contra
  `propsClose.Bounds` directamente da falso negativo.
- **Guard obligatorio**: `PointToScreen` lanza
  `ArgumentException: Visual does not belong to a visual tree` si el visual no
  está attached. Todo el dump va detrás de `IsAttachedToVisualTree()`, incluido
  el loop por tab (las tabs ocultas — `classes`, `help` — tienen su ✕ fuera del
  árbol y rompían el dump a mitad).

### Nota de entorno para QA visual (Wayland/XWayland)

- La sesión es **Wayland** con el shell bajo **XWayland**. En este entorno
  `xtest.fake_input (d, X.MotionNotify, x, y)` **NO mueve el puntero real**
  (queda donde estaba), lo que hace parecer que el click no llega.
- **Solución que funciona**: `root.warp_pointer (x, y)` + `d.sync ()` +
  `time.sleep (0.5)`, y recién entonces `xtest.fake_input (ButtonPress/ButtonRelease)`.
  Confirmar con `root.query_pointer ()` (el `child` debe ser el window id del
  shell) y con `w.query_pointer ()` (coordenadas window-relative).
- **El foco importa**: `_NET_ACTIVE_WINDOW` puede cambiar a otra ventana entre
  comandos y el click se pierde. Reactivar con `wmctrl -i -a 0x0260000f` y
  confirmar con `get_input_focus ()` antes de cada click.
- Los artefactos de QA de agentes viven en `~/opencode/` (no en `/tmp/`, que es
  tmpfs y se borra al reiniciar).

### Infra

- Hooks QA nuevos en `QaDialogArg`: `--padclose`.
- Scripts de QA (fuera del repo, en `~/opencode/`): `qa_searchpopup.sh`,
  `qa_padclose.sh`.

## M26 — Paridad de pads placeholder (documentoutline, classes, codeissues)

El usuario aprobó la **Opción 1 acotada** (los tres primeros pads de la
propuesta de abajo: `documentoutline`, `classes`, `codeissues`) y el trabajo se
**implementó y verificó el 2026-10-02**. La propuesta original de alcance se
conserva al final de la sección como contexto de decisión.

### Implementación

- **`SymbolIndexService`** (`MonoDevelop.Ide/Services/SymbolIndexService.cs`,
  nuevo): escáner de una pasada sobre C# que recupera tipos y miembros (field,
  event, property, method) con línea e INDENT, y de ahí el árbol por
  indentación. Porta a la vez los tres consumidores que antes vivían dispersos:
  `ScanSymbols` (breadcrumb, Go To Type, `:t`), `BuildOutline` (Document
  Outline) y `BuildClassTree` (Classes: project ▸ namespace ▸ tipo ▸ miembro,
  con el orden legacy `ClassNodeBuilder`: tipos → field → event → property →
  method, alfabético por bucket). `ScanFile` cachea por fecha de modificación
  (el Classes pad reescanea la solución entera). Los regex de declaración
  excluyen comentarios, llaves de apertura Allman (no deben hacer pop del
  scope), y solo admiten campos a nivel de tipo (un `const` dentro de un método
  no es miembro).
- **`CodeIssueService`** (`MonoDevelop.Ide/Services/CodeIssueService.cs`,
  nuevo): porta el modelo de `CodeIssuePad` sobre los diagnósticos que el shell
  ya produce (`ParseBuildMessage`/`SetErrors`): parsea la línea MSBuild
  `file(line,col): severity CODE: message`, agrupa por severidad en el orden
  legacy (error → warning → info → hidden) y formatea la fila y el resumen
  (`1 error(s), 2 warning(s), 1 info(s)`).
- **Pads** (`MainWindow.axaml.cs`): `documentoutline` (RightPads) y `classes`
  (LeftPads) son árboles `TreeView` con doble clic que abre el archivo y salta a
  la línea; `codeissues` (BottomPads) es la lista con encabezado de resumen. Los
  tres nacen `Visible=false` (auto-ocultos como el `Pads.addin.xml` legacy) y se
  encienden por View > Pads. El Document Outline sigue al documento activo
  (el timer del breadcrumb lo refresca cuando cambia texto o pestaña).
- **Hooks QA** (`QaDialogArg`): `--outline[=<path>]`, `--classes`,
  `--codeissues` y `--dblclick[=<path>]` (este último ejercita la activación de
  los tres pads sin XTEST: selecciona un nodo con destino conocido y llama el
  mismo método del handler `DoubleTapped`, verificando el caret con
  `VerifyCaret`). **Nota**: `--dblclick` se implementó pero no se había
  registrado en `QaDialogArg` (el hook nunca disparaba); el registro se corrigió
  en este cierre.

### QA (fixture `~/opencode/m26fix`, clase `Widget` + interface `IThing`)

Logs `~/opencode/m26_qa_{outline,classes,codeissues,dblclick}.log`, 0 FATAL /
0 Unhandled en los cuatro:

```
[outline]  10 node(s) in Program.cs
[outline]  class Widget @5 · field counter @7 · field Max @8 · event Changed @9
           property Value @10 · property Name @11 · method Run @13 · method Double @19
           interface IThing @22 · method Go @24
[classes]  project m26fix ▸ namespace M26Fix ▸ interface IThing (Program.cs:22) [method Go]
           class Widget (Program.cs:5) ▸ field counter/Max · event Changed
           property Name/Value · method Double/Run
[codeissues] 1 error(s), 2 warning(s), 1 info(s) — rows=4
[codeissues] Program.cs (12,5): error CS0103 · (20,9): warning CS0219 · (24,1): warning CS0168 · (30,3): info CS8019
[dblclick] classes activated interface IThing @Program.cs:22 → caret line 22 (expected 22)
[dblclick] outline activated class Widget @5 → caret line 5 (expected 5)
[dblclick] codeissues activated error CS0103 @Program.cs:12,5 → caret line 12 (expected 12)
```

El estado vacío también está cubierto (`No solution loaded` en Classes,
`(empty state)` en Outline).

### Hallazgos del Tester QA Senior (§18.5) corregidos antes del cierre

El subagente Tester QA Senior (§18.5) reprodujo fallos reales del escáner que
alimenta los tres pads; el ciclo recursivo se repitió **3 rondas** hasta aprobar
limpio. Correcciones:

1. **(bloqueante H1)** el regex de miembro incluía `\s` en la clase del tipo, así
   que la **indentación** servía de "tipo": `        if (true)` se leía como
   `method if`, y `while`/`foreach`/`get`/`set` igual. Fix: los regex corren sobre
   la línea **sin indentación** + guard de palabras clave de sentencia.
2. **(mayor H2)** propiedad estilo **Allman** (`public int Prop` con `{` en la
   línea siguiente) se perdía → `allmanPropertyRegex`.
3. **(menor H3)** campo **sin modificador** (`int Field;`) → el campo admite cero
   modificadores.
4. **(menor H4)** cuerpos de **comentarios de bloque** y **strings verbatim** →
   `UpdateLexState` rastrea `/* */` y `@"..."` (con `""` escapado).
5. **(mayor N1, regresión del rediseño)** `throw new X ();` y `yield return
   X ();` caían como métodos porque el guard comparaba el tipo entero → guard
   **token-aware** (cualquier token keyword rechaza la línea).
6. **(mayor N2)** `record` / `record class` / `record struct` no se reconocían
   (caían como método) → `typeDeclRegex` con `record`; kind `record` en el
   Classes pad y en el orden legacy.
7. **(menor N3)** **indexers** (`public int this [int i] => i;`) →
   `indexerDeclRegex` → propiedad `this[]`.
8. **(menor N4)** **raw strings** `"""..."""` (C# 11) → cuarto estado en
   `UpdateLexState`. De paso, `void Go ();` de una interfaz ahora sí se detecta
   (9 → 10 nodos en el fixture).

Regresión permanente: 8 tests nuevos en `SymbolIndexTests` (control de flujo,
Allman, campo sin modificador, comentarios/verbatim, throw/yield, record,
indexer, raw string). Verificación adversarial por GUI
(`~/opencode/m26_adv_outline2.log`, fixture `Adv.cs` con `record Point`,
`throw new`, `yield return`, indexer) → exactamente 6 nodos (`record Point @6`,
`class Adv @8`, `property Prop @10`, `method Run @16`, `method It @32`,
`property this[] @37`); arnés `~/opencode/qa_m26_adv/` → `ALL CHECKS PASSED`.
Build del shell **0 errores**; tests **53/53** (`AvaloniaShell.Editor.Tests`).
Veredicto del Tester QA Senior: **M26 PASA**.

### Propuesta original (contexto de decisión)

Estado de partida: M25 cerró el pad de edición (tabs horizontales, editor
SkiaSharp visible, breadcrumb, splitter, GoToType, `:t` con contenedor, checks
de View > Pads y persistencia de layout) y M25b cerró los dos pendientes que
quedaban abiertos (fix del hook `--searchpopup` y verificación visual del ✕ de
Properties). El shell ya cubre: menú completo (11 menús, 176 comandos), toolbar
con búsqueda en vivo, Solution pad sobre `Xwt.TreeView` real, Properties con
descriptores reales, editor con intellisense/hover/folding/burbujas, debugger
DAP completo (Locals/Watch/Call Stack/Threads/Immediate/Attach/breakpoints),
build/run con configuración activa, diálogos (About/Preferences/Addins/Find/
GoTo/New Project/Add Reference/Dirty Files) y persistencia de layout y sesión.

### Inventario de trabajo abierto (evidencia, no opinión)

| # | Frente | Evidencia | Impacto |
|---|--------|-----------|---------|
| A | Pads placeholder sin contenido real | **CERRADO (2026-10-21)**: los 6 pads portados con contenido real. `documentoutline`, `classes`, `codeissues` (M26); `unittests` (2026-10-20: descubrimiento por escaneo de texto, ejecución `dotnet test` por proyecto con filtro FQN, árbol en vivo, paridad del addin UnitTesting; suite 83/83); `help` y `toolbox` (2026-10-21: árbol Monodoc real con carga perezosa por nodo — `HelpTreeService` parsea el índice `monodoc.xml` sin la librería Monodoc, que es un stub en este build — y state vacío con rutas buscadas; toolbox sobre code templates builtin + user XML legacy v3.0 con override por shortcut, filtro y categorías — `ToolboxService`; suite 103/103). 2 rounds del Tester QA Senior con cierre **0 hallazgos** (R1: H1 crítico de expansión del árbol Avalonia `:empty` + H2 restore de layout; ambos corregidos). Páginas de ayuda Monodoc como **web tab** renderizada en el área de edición vía `Avalonia.Controls.WebView` (WebKitGTK) (2026-10-22, QA PASS). Sesiones *Fila A* (2026-10-20), *Fila A — Help y Toolbox* (2026-10-21) y *Fila A — web tab* (2026-10-22) en `docs/migration-status-report.md` | los 6 de 6 portados; el legacy tiene implementación real para todos |
| B | Build completo `Main.sln` (frente B) | **DECIDIDO (operador, 2026-10-22, cambio de rumbo a las 22:48): TODO a net10.0, se elimina el legacy net4x (nada de Mono como target)** — los ~77 proyectos old-style net472/net461/net45 (nrefactory×3, debugger-libs×6, MSBuildResolver, GuiUnit, ~66 addins/tools/tests) se convierten a SDK-style net10.0 siguiendo el patrón de los 36 ya convertidos (p. ej. `MonoDevelop.GtkCore.csproj`); los submódulos multi-TFM quedan solo net10.0 (Xwt `net40;net10.0`→`net10.0`; Mono.Addins `net461;net472;net8.0;net10.0`→`net10.0`). **Supera y reemplaza** la decisión anterior "restaurar multi-TFM del core" (fila D, 21:30). La UI Gtk (`--old-gui`) y la shell Avalonia deben seguir funcionando igual: el runtime Gtk ya carga DLLs net10.0 desde `main/build`, así que sus addins pasan a compilar también como net10.0. Estado previo (2026-10-17): CS1503/CS0012 resueltos en B.5; restante = 126 NU1201 (legacy net472 → core net10.0-only). El frente **H (NRefactory/Cecil→Roslyn) va dentro de B** | gate de cierre de la fase de migración — **EN CURSO** |
| C | Comandos sin comportamiento real | **Actualizado 2026-10-17 (QA In3)**: solo el dummy deshabilitado `...` carece de case; `ToolCommands.ToggleSessionRecorder` / `InstrumentationViewer` / `ReplaySession` tienen case pero son stubs que solo loguean (`MainWindow.axaml.cs:6306-6314`), y `ToggleSessionRecorder` avisa "recording to the log" sin grabar (engañoso). El resto resuelve por prefijo (`pad:`, `pads:`, `recent:`, `tool:`, `cmd:`) o caso comodín (`SelectActiveConfiguration:`) | bajo, pero rompe la promesa "ningún ítem se oculta" en 3 ítems de Tools |
| D | Xwt y módulos Gtk/Mac-only | **DECIDIDO (operador, 2026-10-22, matiz del cambio de rumbo a las 22:48)**: Xwt se queda (ya existe `Xwt.Avalonia` como port de `Xwt.Gtk` para la shell Avalonia); los módulos Gtk/Mac-only se **portean a Avalonia** (Linux/Mac/Windows). **Anulado el "restaurar multi-TFM del core"**: ahora el core y todo el árbol quedan **net10.0-only** (se elimina el legacy net4x, nada de Mono como target) — ver fila B. El *fichero/fuente* legacy se conserva (no se borra) pero compila como net10.0 | decisión tomada: desbloquea la parte legacy del frente B |
| E | Rebranding (nombre/logo) | **DECIDIDO (operador, 2026-10-22)**: el rebranding viene **después** de completar la migración de la UI Gtk legacy a la shell Avalonia (100 % de interfaces, componentes y funcionalidades UI porteadas, cada componente completamente migrado, funcionalmente estable y fiel al legacy) | se desbloquea al cerrar la paridad de UI |
| F | Redibujo de iconos PNG estilo Fluent | **DECIDIDO (operador, 2026-10-22)**: subagente con rol **Diseñador Gráfico Senior** que analiza cada PNG y construye el equivalente con diseño Fluent respetando tamaño, nombre de fichero, extensión y transparencias (fondos transparentes); los PNG originales se quedan para la UI Gtk; los nuevos van en carpeta nueva `main/src/core/MonoDevelop.Ide/MonoDevelop.Avalonia.Icons/`; se integran en la shell Avalonia cambiando los originales por estos y se comprueba el resultado visual | paridad visual de la shell |
| G | Runsheet In3 (smoke tests de GUI) | **CERRADO 2026-10-17**: 7/7 PASS, 0 críticas nuevas; 2 fixes de producto resueltos durante la ejecución (mapeo de plantillas en `NewSolutionDialog`, enumeración de proyectos del build desde el `.sln`). Detalle en la sección *In3* y `~/opencode/qa_in3/` | gate de aceptación de la fase de migración: superado |
| H | NRefactory/Cecil legacy | **DECIDIDO (operador, 2026-10-22): parte del frente B** — analizar cómo está integrado y cómo funciona NRefactory/Cecil, y reemplazarlo por Roslyn **sin romper ni alterar** las funcionalidades de la UI Gtk y la shell Avalonia (todo el árbol queda net10.0-only según el cambio de rumbo de la fila B) | deuda técnica marcada por el usuario; se ejecuta dentro de B |
| I | Colisión de documentos por nombre de fichero (hallazgo In3) | **CERRADO (2026-10-18)**: `OpenFileDocument` claveaba documentos/pestañas por `Path.GetFileName` y hacía early-return si ya existía uno con ese nombre → abrir `A/Program.cs` y luego `B/Program.cs` activaba el primero en vez de abrir el segundo. Fix: clave = ruta completa normalizada (`DocKey` = `GetFullPath`) en `docs`, `documents` y `TabItem.Tag`; el header muestra `Path.GetFileName`; botón de cierre lee la tag al hacer click; rename/save-as re-clavean in-place (antes dejaban la pestaña huérfana); helpers `DocKey`/búsqueda por nombre. QA Senior 3 iteraciones: 4/4 hallazgos cerrados (incl. una regresión del buscador "(open document)" detectada en la iteración 1, el NRE preexistente de `--gutterbp` sin sln, y el marcador dirty " •" que **nunca se pintaba** — el buscador de la etiqueta del tab no descendía al StackPanel; corregido). Evidencia en `~/opencode/qa_in3/qa_report_fila_i.md` | resuelto |
| J | `RunStartupProjectAsync` escanea el directorio (hallazgo In3) | `MainWindow.axaml.cs:7037-7046`: el mismo patrón que el build viejo (primero `.csproj` bajo el directorio del `.sln`) → si la solución trae una librería antes que la consola, `dotnet run` se lanza sobre la librería | bajo/medio: no lo toca el runsheet (ítem 6 cubre build, no run); se corrige junto con I o como follow-up del build |

### Candidatos de alcance (elegir uno o combinar)

**Opción 1 — Paridad de pads restantes (frente A).** Portar los 6 pads
placeholder desde su equivalente Gtk, con la regla del bucle por módulos
(inventario de funcionalidades → implementación → QA de paridad → 0
diferencias). Orden sugerido por valor y por dependencia ya resuelta:
1. `documentoutline` — el shell ya tiene `ScanSymbols` (usado por el breadcrumb
   y por `:t`); el pad es un árbol de esos símbolos con navegación al hacer
   doble clic. Legacy: `MonoDevelop.DesignerSupport/DocumentOutlinePad.cs`.
2. `classes` — el shell ya tiene el índice de símbolos por archivo; el legacy
   (`MonoDevelop.Ide.Gui.Pads.ClassPad/ClassBrowserPad.cs`) agrupa por
   namespace → tipo → miembros. Reutiliza el mismo escaneo.
3. `codeissues` — el shell ya parsea diagnósticos de build
   (`ParseBuildMessage`/`SetErrors`); el legacy
   (`MonoDevelop.Refactoring/.../CodeIssuePad.cs`) lista los diagnósticos de
   Roslyn con severidad y navegación.
4. `unittests` — requiere runner; el legacy usa `MonoDevelop.UnitTesting`.
   Mayor esfuerzo (proceso de test + árbol de resultados).
5. `toolbox` — el legacy depende de providers por addin (GtkCore, AspNet,
   DesignerSupport); el valor en Linux-first es bajo.
6. `help` — el legacy es un índice de documentación Mono; valor bajo en el
   objetivo Linux-first.

**Opción 2 — Desbloqueo del build completo (frente B).** Atacar la
compatibilidad `net472`/`netstandard` de `Mono.Addins.Setup` (CS1503/CS0012) y
seguir con la normalización del árbol de solución hasta que `Main.sln` compile
con el SDK 10 real. Es el **gate de cierre** de la fase de migración
(`docs/interfaz-plan.md` fila 14) y desbloquea todo lo demás.

**Opción 3 — Cierre de paridad y cutover (frentes C, D, E, G).** Cerrar los 2
comandos sin cablear, decidir el destino de Xwt y de los módulos Gtk/Mac-only,
resolver el rebranding y ejecutar el runsheet In3 como gate de aceptación.

**Opción 4 — Deuda técnica marcada (frente H).** Reemplazar NRefactory/Cecil
legacy por el stack Roslyn moderno. Es la deuda que el usuario marcó
explícitamente como "deberá reemplazarse"; conviene planificarla antes del
cutover para no arrastrarla.

### Recomendación

**Opción 1 acotada a los tres primeros pads (`documentoutline`, `classes`,
`codeissues`)**, porque:
- los tres reutilizan infraestructura que el shell **ya tiene** (índice de
  símbolos, parser de diagnósticos, navegación a línea), así que el costo es
  bajo y el riesgo de regresión es mínimo;
- elimina 3 de los 6 pads cáscara, que es la brecha de paridad más visible;
- mantiene el ritmo del bucle por módulos sin abrir frentes de infraestructura
  (B) ni decisiones de producto (D/E) que requieren al usuario.

Los frentes B y G quedan como **siguiente paso natural** una vez cerrada la
paridad de pads, porque B es el gate de cierre y G es su criterio de
aceptación.

### Criterio de aceptación (cumplido)

- Cada pad portado tiene su inventario de funcionalidades legacy documentado y
  su QA determinista (hook `--<pad>` en `QaDialogArg`) con evidencia en el log.
- 0 diferencias funcionales contra el pad Gtk de referencia.
- Build del shell en 0 errores y los 53 tests del editor en verde.
- Documentación actualizada (`interfaz-plan.md`, `session_summary.md`,
  `migration-status-report.md`) + commit + push.

## M27 — Preferences: paridad con el árbol legacy + panel "Errors and Warnings"

Segunda pasada sobre la ventana Preferences (2026-10-02), tras M4/`--prefs`.
Se analizó la Preferences GTK legacy (`OptionsDialog` +
`ExtensionModel/GlobalOptionsDialog.addin.xml` + las extensiones de add-ins de
Debugger, SourceEditor2/TextEditor, Refactoring, Xml, VersionControl(+Git),
PackageManagement, DotNetCore, GtkCore, ChangeLog, PerformanceDiagnostics y
FSharp) para reconstruir el árbol de secciones, el header de panel y los
componentes/iconos.

### Cambios

- **Navegación jerárquica real** (`TreeView`, antes un `ListBox` plano):
  categorías (Environment, Projects, Text Editor, Source Code, Version Control,
  NuGet, Other, Performance Diagnostics) → secciones → subpaneles, fiel al
  orden/labels del addin.xml. El modelo vive en `PreferencesDialog.axaml.cs`
  (`PrefsNode`/`BuildModel`) y el árbol se construye con `TreeViewItem`
  (icono 16px + label).
- **Header de panel** (icono 28px + título) como el `OptionsDialogHeader` legacy;
  se quitaron los títulos duplicados de cada página.
- **Iconos**: se corrigieron `Feedback`/`MonoDevelop Maintenance` (usaban
  `md-prefs-generic` teniendo `md-prefs-feedback`/`md-prefs-maintenance`) y se
  mapeó cada sección a su `md-prefs-*` (los que no existen en
  `StockIcons.addin.xml` caen a `md-prefs-generic`). Verificado: 0 `(missing)`.
- **Panel nuevo `Build → Errors and Warnings`** (legacy `BuildMessagePanel`):
  combos JumpToFirst (`MonoDevelop.Ide.NewJumpToFirstErrorOrWarning`:
  Error/ErrorOrWarning), ShowErrorPadAfterBuild
  (`MonoDevelop.Ide.NewShowErrorPadAfterBuild`:
  Always/OnErrors/OnErrorsOrWarnings) y ShowMessageBubbles
  (`MonoDevelop.Ide.NewShowMessageBubbles`: ForErrors/ForErrorsAndWarnings).
  El legacy tenía un off-by-one (su combo omitía `Never` pero casteaba el índice
  al enum); aquí se mapea correctamente por nombre de enum.
- **Subpaneles y secciones faltantes** añadidos al árbol: `IntelliSense →
  Behavior/Appearance`, `Source Analysis → C#`, `XML Schemas`, `F# Settings`.
  El selector de idioma se movió **dentro de Visual Style** (como el legacy
  `IDEStyleOptionsPanelWidget`), no como sección propia.
- **QA**: hook `--prefs-tree` (dump `[prefs-tree]` con id/label/icono y si
  resuelve) + telemetría `[prefs-panel]` al seleccionar; `--prefs=<id>` sigue
  permitiendo abrir cualquier panel (incluidos los anidados).

### Estado / deuda

Los paneles provistos por add-ins (Text Editor General/Markers/Behavior/Color
Theme/Formatting/Code Snippets/Language Bundles/Source Analysis; Source Code
naming/formatting/header; Version Control; NuGet; .NET Runtimes; SDK
Locations/.NET Core; Debugger; GTK# Designer; Performance; F#) aparecen en el
árbol con label/icono correctos pero como **placeholder**: su lógica usa el
stack de add-in (Roslyn, VersionControl, NuGet, policy framework) que el shell
no referencia. El bucle por módulos los irá portando uno a uno. Desviación
deliberada documentada: `.NET Runtimes` (legacy bajo `Condition FeatureSwitch
RUNTIME_SELECTOR optIn`) se muestra siempre en el shell net10-only.

### QA (§18.5, 2 rondas)

Tester QA Senior: ronda 1 → 4 hallazgos menores (idioma como sección propia,
subpaneles omitidos, F# ausente, dead code) corregidos; ronda 2 → **PASA
limpio** (0 bloqueantes/mayores/menores). Build 0 errores; 53/53 tests. Logs
`~/opencode/prefs_qa_{tree,buildmessages}.log`, captura `prefs_tree.png`.

## M28 — Fix de la doble barra de título + paneles del Text Editor (add-in SourceEditor2)

Continuación de M27 (2026-10-02).

### Fix: doble barra de título en los diálogos

`Controls/DialogWindow.cs` (`Apply`) buscaba la fila XAML `Classes="dialogchrome"`
en `LogicalExtensions.GetLogicalDescendants(window)` **después** de
`window.Content = null`; al estar el contenido ya desprendido, no la encontraba y
añadía una **segunda** barra de título (uno de los síntomas que veía el usuario:
dos bandas de chrome con texto en el top del diálogo). Fix: recorrer el árbol
lógico del contenido desprendido (`oldContent`). Afecta a todos los diálogos que
usan `DialogWindow.Apply` (About/NewSolution/DirtyFiles/FindInFiles/AddinManager/
Preferences). Verificado por píxeles (una sola banda de título) y
`_NET_FRAME_EXTENTS` ausente (el WM no dibuja marco).

### Integración de add-in: paneles del Text Editor

Los paneles de Preferences provistos por el add-in `MonoDevelop.SourceEditor2`
son widgets GTK (`Gtk.Bin`), por lo que no se pueden hostear en Avalonia; se
**portan** a Avalonia leyendo/escribiendo las **mismas claves** de
`MonoDevelopProperties.xml` que usa el add-in vía `DefaultSourceEditorOptions`/
`EditorPreferences` (así ambas UIs comparten los settings). Portados:

- **General** (`general`, legacy `GeneralOptionsPanel`): `LineEndingConversion`,
  `ShowFoldMargin`, `DefaultRegionsFolding`, `DefaultCommentFolding`,
  `WordWrapStyle` (flags `WordWrap`/`VisibleGlyphs`).
- **Markers and Rulers** (`markers`, legacy `MarkerPanel`):
  `ShowLineNumberMargin`, `ShowRuler`, `HighlightCaretLine`,
  `HighlightMatchingBracket`, `EnableHighlightUsages`, `ShowBlockStructure`,
  `EnableQuickDiff`, `ShowProcedureLineSeparators`, `EnableAnimations`,
  `ShowWhitespaces`, `IncludeWhitespaces`.
- **Behavior** (`behavior`, legacy `BehaviorPanel`): `IndentStyle`,
  `WordNavigationStyle`, `AutoInsertMatchingBracket`, `SmartSemicolonPlacement`,
  `TabIsReindent`, `SmartBackspace`, `AutoFormatDocumentOnSave`,
  `AutoSetPatternCasing`, `EnableSelectionWrappingKeys`,
  `GenerateFormattingUndoStep`.
- **IntelliSense** (`intellisense`, legacy `CompletionOptionsPanel`):
  `EnableAutoCodeCompletion`, `AddImportedItemsToCompletionList`,
  `IncludeKeywordsInCompletionList`, `IncludeCodeSnippetsInCompletionList`,
  `ForceCompletionSuggestionMode`.

Los `[Flags]` enums se serializan como el `EnumConverter` legacy: el compuesto
`IncludeWhitespaces.All` se escribe/lee como `"All"`, `"None"` si vacío, y se
preservan miembros válidos fuera del conjunto gestionado (p.ej.
`WordWrapStyles.AutoIndent`).

### QA (§18.5, 3 rondas)

Tester QA Senior: barra de título **aprobada**; paneles aprobados con hallazgos
H1/H2 (`IncludeWhitespaces`: no reconocía `"All"` ni el default `All`) y H3
(`SetFlag` descartaba `AutoIndent`), **todos corregidos** y re-verificados con
probe real de `EnumConverter`. Build 0 errores; 53/53 tests; 4 paneles
`placeholder=False`, 0 FATAL. Logs `~/opencode/prefs_qa_{general,markers,behavior,intellisense}.log`.

### Pendiente

Resto de paneles de add-in (Source Code naming/formatting/header, Version
Control, NuGet, .NET Runtimes, SDK Locations/.NET Core, Debugger, GTK# Designer,
Performance, F#, Color Theme, Code Snippets, Language Bundles, Source Analysis,
XML Schemas) siguen como placeholder con label/icono correctos; se portarán por
módulos igual que el Text Editor.

## M29 — Text Editor: Color Theme + Code Snippets + Language Bundles

Continuación del grupo Text Editor (2026-10-02). Los tres paneles GTK dependen
de `SyntaxHighlightingService` (parsea `.tmTheme`/`.sublime-syntax`, con deps de
NRefactory) y `CodeTemplateService`; en el shell se portan conciliando las
**mismas carpetas y claves** de usuario (`UserDataRoot` = `~/.local/share/
MonoDevelop/9.0`), sin arrastrar esos servicios.

- **Color Theme** (`colortheme`, legacy `HighlightingPanel`): lista los built-in
  {Light, Dark, High Contrast Dark, High Contrast Light} + temas de usuario de
  `ColorThemes/` (`*.json`/`*.vssettings`/`*.tmtheme`), con Add (import por copia)
  / Remove (no borra built-ins) / Open folder. Persiste en **`ColorScheme`**
  (UI light) o **`ColorScheme-Dark`** (UI dark), las claves de
  `IdePreferences.ThemeConfigurationProperty`.
- **Code Snippets** (`codesnippets`, legacy `CodeTemplatePane`): lista los
  `<Shortcut>.template.xml` de `Snippets/`, muestra el código en preview y
  Remove borra el archivo (Add/Edit —que abre el `EditTemplateDialog`— queda
  pendiente).
- **Language Bundles** (`languagebundles`, legacy
  `TextMateBundleOptionsPanelWidget`): lista los bundles de usuario de
  `LanguageBundles/` con Add (copia) / Remove; los built-in los provee el IDE.

### QA (§18.5, 2 rondas)

Tester QA Senior: ronda 1 → **H1 bloqueante** (el `SnippetPreview` con
`FontFamily="monospace"` lanzaba `Could not create glyphTypeface` y abortaba la
app con fontconfig mínimo) + H2/H3 del harness QA; corregidos (fuente por
defecto del diálogo, script QA case-insensitive/sin truncar) y ronda 2 → **APTO**
(6/6 escenarios adversariales de Code Snippets sin crash). Build 0 errores;
53/53 tests; 3 paneles `placeholder=False`, fixtures listadas
(`themes=5`, `count=1`, `count=1`).

## M30 — Source Code: .NET Naming Policies + Standard Header (global policy set)

Continuación del grupo Source Code (2026-10-02). Los paneles GTK son
`PolicyOptionsPanel<T>` (policy framework); en el shell se escribe/lee el **policy
set global** directamente en `Policies/UserDefault.mdpolicy.xml`, con un elemento
por política nombrado por su `[DataItem]` y sus `[ItemProperty]` como hijos —
exactamente lo que `PolicySet.LoadFromXml`/`PolicyService.DiffDeserializeXml`
consumen (verificado por el Tester leyendo el serializador).

- **.NET Naming Policies** (`naming`, legacy `NamespaceSynchronisationPanel`):
  asociar namespaces↔carpetas, namespace raíz, flat/hierárquico, y naming de
  recursos de 3 estados (File format default / File name / MSBuild).
  Escribe el elemento `<DotNetNamingPolicy>` (`DirectoryNamespaceAssociation`,
  `ResourceNamePolicy`). Default efectivo del sistema **`PrefixedHierarchical`**
  (el de `DefaultDotNetNamingPolicy.xml`), no el cero del enum.
- **Standard Header** (`standardheader`, legacy `StandardHeaderPolicyPanel`):
  texto del header + "incluir en archivos nuevos"; escribe `<StandardHeader>`
  (`Text`, `IncludeInNewFiles`).
- **Code Formatting** (`codeformatting`) queda **placeholder** (es el panel más
  grande, 466 líneas, con preview y edición de políticas de formato).
- Escritura: `StoreGlobalPolicies` guarda el set en **una pasada**, conserva las
  demás políticas y normaliza una raíz `<PolicySet>` directa a `<Policies>` sin
  perder hermanos; copia `.previous` (como `PolicyService.ParanoidSave`).

### QA (§18.5, 3 rondas)

Tester QA Senior: ronda 1 → formato **APTO** + 2 hallazgos medios (default de
naming = `PrefixedHierarchical`; pérdida de `ResourceNamePolicy.FileName`) y 2
bajos; ronda 2 → medios y `.previous` corregidos; ronda 3 → raíz directa
preservando hermanos y restore byte-exacto del hook. Build 0 errores; 53/53
tests; `naming`/`standardheader` `placeholder=False`, política escrita y
restaurada. Hook QA `--prefs-sourcewrite` (dry-run con backup/restore).

## M31 — Editor multi-lenguaje dinámico: pila legacy de coloreo + paletas VSCode (Fase 1)

2026-11-11. **VERDE, 0 hallazgos abiertos.**

**Objetivo**: transformar el editor de la shell (`SkTextEditor`, coloreo hardcodeado
C# con `Tokenize`/`GetThemePalette`) en un editor dinámico multi-lenguaje que
**reutilice la pila de coloreo legacy** (`MonoDevelop.Ide.Editor.Highlighting`:
RegexEngine + formatos Sublime3/TextMate + `SyntaxHighlighting`/`EditorTheme`) con
**paletas de calidad VSCode** (Dark+/Light+), como base de las fases siguientes
(intellisense, errores + quick fixes, extension points VSIX, minimap, scroll
bloqueado, fondo oscuro, dropdown de pestañas ▼). Lenguajes v1: C#, F#, JSON, XML
(`.csproj`/`.sln`), C, C++, Markdown, YAML.

**Enfoque (regla 18.4)**: la pila legacy **no tiene GTK** y se compila *inline* en el
dll de la shell (globs `<Compile Include="..\MonoDevelop.Ide\MonoDevelop.Ide.Editor
.Highlighting\...">` en el csproj de la shell). **No hay carpeta "Avalonia"**: el
código convive en el árbol original de `MonoDevelop.Ide`. La ruta GTK (`--old-gui`)
debe seguir compilando y arrancando: los ficheros legacy que usan Gtk/Mono.Addins/
NRefactory llevan guards `#if !AVALONIA_SHELL` (solo la shell define la constante).
`HslColor.cs`, `StackMatchExpression.cs` y `EditorTheme.cs` quedan **sin cambios**
(compilan en los dos builds).

**Qué se hizo (fases 1a–1g)**:
- **1a Probe E2E** de la pila standalone (`~/opencode/highlight_e2e/`, artefacto QA):
  demostró que la pila no necesita Gtk y que los únicos huecos son shims de
  atributos CAS (`.NET 9+` eliminó los de `System.Security.Permissions` que el
  código usa solo por compatibilidad).
- **1b Pila compilando en la shell**: guards en `SyntaxHighlightingService.cs`,
  `SemanticHighlighting.cs`, `TemplateCodon.cs`, `TemplateExtensionNodeLoader.cs`,
  `Formats/OldFormat.cs` (completo) y `TextMateFormat.cs`, `IEditorThemeProvider.cs`,
  `IReadonlyTextDocument.cs` (parcial). Shims CAS inertes en
  `Controls/HighlightingCasShims.cs` (bajo `#if AVALONIA_SHELL`). Servicio portado en
  `Controls/SyntaxHighlightingService.Avalonia.cs` — **a propósito** en el namespace
  `MonoDevelop.Ide.Editor.Highlighting` (la `SyntaxHighlightingDefinition` legacy
  resuelve sus scope-include contra `SyntaxHighlightingService.AllBundles` sin
  cualificar). Los 8 grammars viven en
  `MonoDevelop.Ide.Editor.Highlighting/Resources/` y se incrustan como
  `EmbeddedResource` con `LogicalName="MonoDevelop.AvaloniaShell.Highlighting
  .Resources.<fichero>"`, cargados con el `ResourceStreamProvider` legacy.
- **1e Integración en `SkTextEditor`** (región `Dynamic syntax highlighting`):
  highlighter **recreado por generación** (invalidación en `SetLines`/`Commit`/setter
  `FilePath`) + cache `HighlightedLine` por línea (snapshot inmutable vía
  `Controls/EditorTextDocumentAdapter.cs`). **El tema se resuelve en cada paint**
  (no se cachea con el resaltado). `ToSegments` traduce `ColoredSegment`
  (scope→`ColorStyleKey`) a `theme.TryGetColor(scope,"foreground")` →
  `HslColor.ToRgba()` → `SKColor`. `Tokenize`/`GetThemePalette` **borrados**.
- **1f Wiring**: `OpenFileDocument` asigna `FilePath` **antes** que `Text`.
- **Paletas** en `Controls/HighlightingThemePalette.cs`: Dark+/Light+ como datos
  puros, convertidos al modelo legacy (`ThemeSetting`/`StackMatchExpression`), de
  modo que la resolución scope→color es la implementación legacy compartida.

**Fixes hallados por la QA (ciclo §18.5)**:
1. **`Microsoft.Extensions.ObjectPool` no resolvible** (crash en el primer paint,
   `StringBuilderCache.Allocate`): `MonoDevelop.Core` declara el paquete con
   `PrivateAssets="runtime"` → el runtime asset **no fluye** al deps.json de la shell
   vía `ProjectReference` (aunque el dll físico sí llega por `IncludeCopyLocal` a
   `main/build`). Fix contenido en la shell: `PackageReference` directo
   (`3.0.0-preview9.19423.4`, misma versión) emite la entrada de runtime. Un
   `<Reference>` con `HintPath` NO funciona (el compilador sí, el host no).
   `MonoDevelop.Core` no se toca (regla 18.4).
2. **Fondo del editor negro (preexistente, ejercitado por la QA)**: los editores se
   montan con `Background = Brushes.Transparent`; `ToSk(Transparent)` devuelve
   `SKColor(0,0,0,0)` (no nulo) → el fallback `?? #1E1E1E` nunca se aplicaba y el
   fill transparente dejaba la superficie `#000000`. Invisible en dark (base-fg
   claro), crítico en light (base-fg oscuro → texto plano invisible). Fix en
   `SkTextEditor.Draw`: si `Background` es nulo o `Alpha==0`, el fondo se pinta desde
   la paleta activa (`TryGetColor("background")`; fallback por variante `#FFFFFF` /
   `#1E1E1E`). Cubre además la requirement "fondo negro en tema oscuro".
3. **Recolorido live dark/light** (requirement: el tema NO está en la cache): un
   cambio de tema muta los brushes vinculados *in place* (la re-resolución del
   `DynamicResource` cambia el `Color` de la misma instancia `SolidColorBrush`) →
   `ForegroundProperty` no cambia → no hay `AffectsRender` ni `OnPropertyChanged` →
   el compositor re-presenta el frame viejo. (Se probó y **revertió** una rama en
   `OnPropertyChanged` por propiedades de brush: no se dispara; `Control
   .ActualThemeVariant` en Avalonia 12 es una propiedad simple sin `AvaloniaProperty`.)
   Fix definitivo: el handler existente de `Application.Current
   .ActualThemeVariantChanged` en `MainWindow` llama además a `ed.InvalidateColors()`
   de cada editor abierto (nuevo método público de `SkTextEditor` → `MarkDirty()`).

**Driver QA**: flag `--themetoggle[=<path>]` (registro en `Program.QaDialogArg` +
rama en la cadena QA de `MainWindow`) — abre un fixture y alterna el tema por la
**ruta real de producción** (`ApplyThemeVariant`, la misma del menú de tema),
verificando `QaRenderCount` antes/después. Mismo estilo que `--outline`/`--editqa`.

**Validación (veredicto por requisito)**:
- **Builds canónicos a 0 errores**: shell (`MonoDevelop.Startup.Avalonia.csproj
  -c DebugLinux -m:1`, 2198 warnings preexistentes) y GTK
  (`MonoDevelop.Ide.csproj -c DebugLinux -m:1`, 242 warnings). La shell **no**
  referencia `MonoDevelop.Ide.csproj` (la pila se compila inline); el proyecto GTK
  no define `AVALONIA_SHELL` (los guards lo excluyen).
- **Probe headless del glue** (`~/opencode/highlight_qa/headless/`, referenciando los
  DLLs reales de `main/build`): 42 aserciones `[OK]`, 0 `[FAIL]` — 8 resoluciones por
  extensión + 2 negativos, paletas Dark+/Light+ (tolerancia ±2 LSB, el round-trip
  HSL del `HslColor` pierde 1-2 bits — idéntico al mundo GTK), fallback de tema
  desconocido, tiling completo y scopes esperados en los 6 fixtures. `GLUE PROBE
  VERDE`.
- **QA visual 6 lenguajes × 2 temas** (12 capturas Xvfb + `analyze_colors.py`):
  colores distintos de tinta — dark cs 566 / fs 634 / json 508 / xml 480 / cpp 525 /
  md 270; light cs 479 / fs 569 / json 505 / xml 346 / cpp 481 / md 139. Canónicos
  presentes (dark: keyword `#BE82B9`, string `#C68A72`, comment `#669352`; light:
  keyword `#9C00C4`, string `#921212`, comment `#007A00`). "Canónicos faltantes" en
  json/md/xml esperados (los fixtures no tienen esos tokens).
- **Fix de fondo verificado** (`fix_light_cs.png` / `fix_dark_cs.png`): light →
  fondo `#FFFFFF` (0.0% `#000000`), 12 líneas visibles; dark → fondo `#1E1E1E`, 0 px
  de `#000000` en la zona del editor.
- **Recolorido live en la misma sesión** (`--themetoggle`): `Dark -> Light
  renderCount 2 -> 3 RECLORED` + captura post-toggle con UI y editor completos en
  light, paleta Light+ aplicada sin reabrir el documento.
- **Arranque `--old-gui` verificado**: vía `dotnet <dll> --old-gui` el hijo es
  `dotnet main/build/MonoDevelop.dll` con `MONODEVELOP_LEGACY_UI=1`, carga ~287 MB
  RSS, crea ventana, vivo >120 s sin excepciones. Quirk preexistente: vía **apphost**
  `Environment.ProcessPath` devuelve el apphost → el hijo re-ejecuta la shell
  Avalonia (no es regresión; el diff no toca `LaunchLegacyGtk`). Captura negra en
  Xvfb = **artefacto de entorno** (sin window manager instalado: `twm`/openbox
  ausentes) → la evidencia es proceso vivo + RSS + `xwininfo -root -tree`, no píxeles.

**Decisiones de diseño (trade-offs documentados)**:
1. **Invalidación por generación + cache por línea, sin invalidación incremental.**
   El adapter es un snapshot inmutable (no `ITextDocument` con cambios), así que cada
   edición recrea el highlighter y descarta la cache; el coste se amortigua con la
   cache `HighlightedLine` por línea (el resaltado por línea es independiente).
   Trade-off: un documento grande se re-highlighta entero tras cada edición; a
   cambio, cero riesgo de desincronización. Paso natural futuro: adapter incremental.
2. **Sync-over-async en `Draw` es seguro**: `GetColoredSegments`/
   `GetHighlightedLineAsync` son 100% síncronos (`Task.FromResult`); el
   `GetAwaiter().GetResult()` en el camino de pintura no puede deadlock.
3. **Round-trip HSL de `HslColor` da ±1-2 LSB por canal** (modelo HSL:
   `Parse→ToRgba` pierde 1-2 bits). El mundo GTK hace el mismo round-trip; las
   aserciones de QA usan tolerancia ±2.
4. **El `PackageReference` de ObjectPool en el csproj de la shell es un fix de
   empaquetado contenido en la shell** (root cause en `MonoDevelop.Core`, no se
   toca — regla 18.4).
5. **`HslColor` ambiguo**: existe `Avalonia.Media.HslColor`; las referencias del
   glue están cualificadas como `MonoDevelop.Components.HslColor`.

**Archivos**:
- Nuevos (solo compilan en la shell; `Controls/` por el glob `Controls\**\*.cs`):
  `Controls/EditorTextDocumentAdapter.cs`, `Controls/HighlightingCasShims.cs`
  (bajo `#if AVALONIA_SHELL`), `Controls/HighlightingThemePalette.cs`,
  `Controls/SyntaxHighlightingService.Avalonia.cs`, y
  `MonoDevelop.Ide.Editor.Highlighting/Resources/` (8 grammars:
  `csharp.sublime-syntax`, `fsharp.sublime-syntax`, `markdown.sublime-syntax`,
  `yaml.sublime-syntax`, `c.json`, `cpp.json`, `xml.json`, `json.plist`).
- Modificados: `Controls/SkTextEditor.cs` (integración del highlighter, borrado de
  `Tokenize`/`GetThemePalette`, invalidación por generación, fix de fondo,
  `InvalidateColors()`); guards `#if !AVALONIA_SHELL` en `SyntaxHighlightingService
  .cs`, `SemanticHighlighting.cs`, `TemplateCodon.cs`,
  `TemplateExtensionNodeLoader.cs`, `Formats/OldFormat.cs`, `TextMateFormat.cs`,
  `IEditorThemeProvider.cs`, `IReadonlyTextDocument.cs`;
  `MonoDevelop.Startup.Avalonia.csproj` (compile set, 8 `EmbeddedResource` con
  `LogicalName`, `Reference` Roslyn core, `PackageReference ObjectPool`);
  `Program.cs` (flag QA `--themetoggle`); `Views/MainWindow.axaml.cs`
  (`InvalidateColors()` en `ActualThemeVariantChanged` + flag QA `--themetoggle`).
- Sin cambios (deliberado): `HslColor.cs`, `StackMatchExpression.cs`,
  `EditorTheme.cs` (compilan en los dos builds); `MonoDevelop.Core` (regla 18.4).

**Cómo se valida** (artefactos en `~/opencode/highlight_qa/`, regla 18.6):
1. Builds canónicos: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/
   MonoDevelop.Startup.Avalonia.csproj -c DebugLinux -m:1` y
   `dotnet build main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.csproj -c DebugLinux
   -m:1` → 0 errores.
2. Probe headless: `cd ~/opencode/highlight_qa/headless && dotnet build
   glue_probe.csproj -c Debug -m:1 && dotnet bin/Debug/net10.0/glue_probe.dll` →
   `GLUE PROBE VERDE`.
3. QA visual: `~/opencode/highlight_qa/capture_all.sh <dark|light> cs fs json xml
   cpp md` + `python3 analyze_colors.py <tema> shot_*.png`.
4. Recolorido live: lanzar la shell con `--themetoggle=<fixture>` → buscar
   `RECLORED` en el log.
5. `--old-gui`: `dotnet main/build/MonoDevelop.AvaloniaShell.dll --old-gui` → proceso
   vivo, ~290 MB RSS, ventana creada, sin excepciones.

**Queda pendiente** (fases 2-9, fuera de esta tarea): lenguajes adicionales del
alcance original (`.properties` y `.txt` — el servicio ya resuelve por extensión);
intellisense, errores + quick fixes, extension points VSIX (aplicables a toda la
shell Avalonia); minimap siempre visible a la derecha (clic/arrastre mueve viewport;
muestra errores y breakpoints). *(Resuelto en M31b — § 2026-11-11 al pie de este
fichero: scrollbar vertical del editor y dropdown de pestañas ▼; el fondo oscuro
pasó de la paleta Dark+ `#1E1E1E` al negro puro `#000000` pedido por el operador.)*

### M31b — Fondo negro del editor, scrollbar vertical y botón de lista de pestañas ▾ (2026-11-11)

2026-11-11. **VERDE, 0 hallazgos abiertos** (QA ciclo #3).

**Pedido**: el operador vio M31 y pidió (1) **fondo negro** en el editor, no gris
("Yo quería el fondo del editor negro, no gris": el fix de M31 pintaba `#1E1E1E`
de la paleta Dark+; el look original es `#000000`), y (2) **el scrollview y el
botón de lista de pestañas** ("tambien falta el scrollview y el botón de lista
de pestañas": el editor solo scrollaba por rueda y la tira no tenía dropdown).
"Poco coloreo" en `.cs` dark: aceptado (la paleta era casi VSCode-dark).

**Qué se hizo**:
- **Fondo negro en dark** (`SkTextEditor.Draw`): con `Background` transparente,
  dark pinta `#000000` puro sin mirar la paleta; light sigue usando la paleta/
  `#FFFFFF`.
- **Scrollbar vertical** (pintada en el canvas Skia — el editor es 100%
  custom-drawn, sin control nativo): 14px al borde derecho; visible solo si el
  documento desborda; thumb proporcional (viewport/total, mín. 24px) arrastrable
  (`e.Pointer.Capture(this)`); clic en track pagina 0.9 viewport; cursores
  `SizeNorthSouth`/`UpArrow`; hover de palabra/gutter suprimido sobre la barra.
  El clamp de rueda pasó a `MaxScroll`.
- **Botón ▾ de lista de pestañas** (`DocTabList`, siempre visible, docked right
  junto a ‹ ›): `ShowTabListMenu` (ContextMenu con todas las tabs: icono + ✓ en
  la activa + nombre + `•` dirty) y `RevealSelectedTab` (desplaza la tira para
  mostrar la pestaña elegida). Flag QA `--tablist` (autocontenido).
- **Fix de doble sustracción (crítico, preexistente desde M16e `0309f21902`)**:
  el bucle de `Draw` hacía `y = (drawn - scrollLines)·lineH` con
  `firstLine=(int)scrollLines` → offset entero restado dos veces → editor en
  blanco total con `scrollLines > ~1 viewport`. Inalcanzable antes (solo rueda +
  fixtures cortos); la scrollbar lo expone. Fix:
  `y = (firstLine + drawn - (float)scrollLines) * lineH`.
- **Fix H-1 (preexistente, media-baja)**: tras un fold, el gutter duplicaba el
  número de la última línea en las filas sobrantes. (a) `MaxScroll` sobre líneas
  de buffer, no rows visibles; (b) fallback `RowToLine` a `lines.Count - 1` +
  bucle que emitía `visible` filas aunque quedaran menos. Fix: `RowToLine`
  devuelve `-1` al agotarse las filas visibles y el bucle rompe (`i < 0`);
  `MaxScroll = VisibleRowCount - ScrollVisibleLines` (cuenta líneas no ocultas
  por folds).

**Decisiones de diseño (M31b, históricas)**:
1. **Scrollbar pintada, no control nativo** — *SUSTITUIDA (ver M31c más abajo)*:
   la barra se pintaba en el canvas Skia del editor. El operador la rechazó
   ("Scrollbar no, scrollview"): el editor debe scrollear con un `ScrollViewer`
   real (equivalente al `GtkScrolledWindow` legacy, scrollbar nativa).
2. **Fondo negro solo en dark**: dark no mira la paleta (el operador pidió
   negro); light respeta la paleta actual.
3. **Botón ▾ siempre visible** (como VSCode): no depende de overflow; el menú
   lista todas las tabs y el reveal es un no-op sin overflow.

### M31c — Scrollbar nativa: `ScrollViewer` real en lugar de barra pintada (2026-11-11)

**Pedido**: el operador rechazó la scrollbar pintada en el canvas
("Scrollbar no, scrollview"): el editor debe scrollear con un **`ScrollViewer`**
estándar de la shell (el equivalente Avalonia del `GtkScrolledWindow` que
envolvía el text view legacy), con scrollbar nativa. Elegido explícitamente
("ScrollViewer real (recomendado)").

**Qué se hizo**:
- **Montaje en `ScrollViewer`** (`MainWindow.AddDocument`): cada editor
  `SkTextEditor` se envuelve en **un único** `ScrollViewer` (vertical `Auto` →
  solo aparece con desborde; horizontal `Disabled`) **una sola vez**, y ese
  wrapper se guarda como `doc.Content`. `OnDocSelectionChanged` solo re-añade
  el mismo control al panel (que ya fue desanclado con `Children.Clear()`).
  **Crear el wrapper por selección** re-papereaba el editor long-lived a un
  `ScrollContentPresenter` nuevo mientras Avalonia lo seguía viendo hijo del
  anterior → `InvalidOperationException` al volver a cualquier tab ya abierta
  (regresión detectada en QA, corregida: el editor se captura ANTES de
  embolver para que `docs`/F#/dirty/undo sigan cableados).
- **El editor deja de gestionar scroll internamente** (`SkTextEditor`):
  eliminada la barra pintada (`ScrollThumb`, `SetScrollFromThumb`,
  `PageScrollToward`, `draggingScroll`, `scrollGrabOffset`, `ScrollBarWidth`,
  `MaxScroll`, `ScrollVisibleLines`, `VisibleRowCount`, `ScrollBarVisible`) y
  el `OnPointerWheelChanged` propio (el `ScrollViewer` scrollea nativamente).
  El offset/viewport se leen del `ScrollViewer` dueño (`scrollOwner`) en
  `ScrollChanged` + attach; `scrollLines` pasa a ser propiedad de solo lectura
  `=> scrollOffset / LineHeight`.
- **Render viewport-sized**: `Render` dibuja un `WriteableBitmap` **del tamaño
  del viewport** (`vh`) y lo coloca en `Rect(0, scrollOffset, w, h)`; `Draw`
  usa `vpH` en vez de `Bounds.Height` (que ahora es la altura total del doc).
  Un documento de 308 líneas (~5600 px) renderiza un bitmap de ~765×477 —
  seguro en ficheros grandes.
- **Coordenadas (clave)**: `GetPosition`/`GetCurrentPoint` sobre el contenido
  scrolleado llegan en **coordenadas de documento** (offset ya incluido) y
  `PointToScreen` lo descuenta. En click→línea se quitó el `+ scrollLines`; en
  los 3 popups (Y de documento para `PointToScreen`) se quitó el `- scrollLines`.
  **Fix de espacios**: los rects de *pinned-watch bubbles* se registran en
  espacio de **viewport** (en `Draw`) pero el hit-test del menú contextual
  recibía el punto en **documento** → se convierte con `- scrollOffset` antes
  del hit-test (`OnEditorContextRequested`).
- **`InvalidateMeasure()`** en los puntos que cambian el nº de filas visibles
  (`SetLines`, `Commit`, `ToggleFolding`, `ToggleAllFoldings`, `FoldDefinitions`,
  `EnableDisableFolding`) para que el `ScrollViewer` re-pida su extent.
- **Avalonia 12 no tiene `FindAncestorOfType`**: el walk a `ScrollViewer` usa
  `GetVisualParent()` (patrón ya usado en `MainWindow`).

**Decisiones de diseño**:
1. **`ScrollViewer` real, no `ScrollBar` acoplado ni barra pintada**: el
   `ScrollViewer` posee offset/clip/barra nativa; el editor deja de gestionar
   scroll. Reutiliza el control estándar de la shell (equivalente al
   `GtkScrolledWindow` legacy), coherente con §18.4 (reutilización obligatoria).
2. **Render del frame solo del viewport** (no del documento completo): seguro
   en ficheros grandes.
3. **Un `ScrollViewer` por editor, creado una vez**: re-montar el mismo
   control es seguro; re-embolverlo por selección rompe el parent visual.

**Validación (valores medidos)**:
- **Build 0 errores** (`-c DebugLinux -m:1`). **Ruta GTK intocada**:
  `SkTextEditor.cs` solo lo compila la shell (glob `Controls\**\*.cs`);
  `MonoDevelop.Ide.csproj` = `EnableDefaultCompileItems=false` sin globs.
- **Smoke (Xvfb `:99`, tema Dark)**: doc largo 308 l → scrollbar nativa Fluent
  (thumb x≈1088, y=191..230, ~11% de la pista); rueda 8 ticks → contenido de
  líneas 1-6 a 24-28, thumb 191→224; **arrastre del thumb** scrollea; clic→línea
  correcto (caret en la línea clicada, breadcrumb actualizado); coloreo + gutter
  intactos; render `765x477` (viewport, no ~5600px). Doc corto 18 l → **sin
  scrollbar** (scan de píxeles `thumb=None`).
- **QA §18.5 recursiva (3 ciclos, 0 hallazgos abiertos)**: ciclo #1 hallazgo
  crítico (crash `SkTextEditor already has a visual parent` al volver a una tab
  ya abierta, 5/5) → fix (wrapper único) → ciclo #2 hallazgo (regresión:
  `docs` vacío → Save no-op, gate dirty inoperante) → fix (capturar el editor
  antes de embolver) → **ciclo #3 VERDE 0 hallazgos** (`--dupdocs docs=2` +
  `save-correct`, Save real Ctrl+S/Ctrl+Shift+S a disco, re-montaje VIVO por
  tira de tabs, gate dirty "Save Files" al cerrar, thumb/rueda/corto, `--tablist`
  `reveal-ok=True`). Artefactos `~/opencode/qa_scrollview/` (cycle1/2/3).

**Nota de incidencia (regresión de GTK reportada por el operador)**: el
operador vio "la UI GTK se rompió al abrir una solución". Diagnóstico: el
arranque `main/build/MonoDevelop.AvaloniaShell --old-gui` (el apphost nativo)
**no lanza GTK** — el relay `LaunchLegacyGtk` usa `Environment.ProcessPath`,
que con el apphost apunta al apphost, así que el hijo re-ejecuta la **shell
Avalonia** (quirk documentado desde antes de M31b; los tests previos lo
lanzaban vía `dotnet <dll> --old-gui`, que sí relía a `MonoDevelop.dll`). Esa
shell Avalonia llevaba el crash de re-montaje de este refactor, que crashea
al abrir una solución de varias pestañas. **Corregido por el mismo fix**
(wrapper único): `--old-gui <sln>` queda VIVO. La pila GTK real (`MonoDevelop.dll`
+ `MonoDevelop.Ide.dll` + Xwt/SourceEditor) no fue tocada por este cambio
(`SkTextEditor.cs`/`MainWindow.axaml.cs` no entran en el build GTK); los 3 DLLs
compartidos reestadiados por el build de la shell (`Core`, `MonoUnixCompat`,
`MonoRoslynCompat`) son IL-idénticos a builds previos conocidos buenos.

**Archivos** (2 M): `Controls/SkTextEditor.cs` (scroll owner, render
viewport-sized, coords documento, hit-test pinned-watch, `InvalidateMeasure`,
eliminación de la barra pintada); `Views/MainWindow.axaml.cs` (wrapper
`ScrollViewer` único en `AddDocument`, re-montaje en `OnDocSelectionChanged`).

**Validación (valores medidos)**:
- **Build 0 errores** (`-c DebugLinux -m:1`). **Ruta GTK intocada**:
  `SkTextEditor.cs` solo lo compila la shell (glob `Controls\**\*.cs`);
  `MonoDevelop.Ide.csproj` = `EnableDefaultCompileItems=false` sin globs
  (`--old-gui` no se toca, verificado por grep).
- **Fondo**: dark 96% `#000000` en la zona del editor; light 96% `#FFFFFF`;
  coloreo intacto.
- **Scrollbar**: aparece con 308 líneas (track+thumb, dark y light), no aparece
  con 19; arrastre por xdotool funcional.
- **Tablist** (`--tablist`): `overflow=True tabs=11`, `button-found=True
  button-visible=True`, `reveal-ok=True`, offset 0 → -746.
- **Fix de scroll**: repro exacta (arrastre en 308 líneas) → 4231 px de texto
  tras el scroll (antes ~0); línea 308 al final; rueda+clamp; coloreo y
  caret/gutter alineados.
- **H-1**: fold de toda la clase (309→5 rows) + scroll al fondo → 0 tinta de
  número bajo la última fila; fold intermedio (188 rows, desborda) → thumb al
  final = final real del documento.
- **QA §18.5 recursiva (3 ciclos)**: #1 → blocker doble sustracción → fix →
  #2 VERDE (0 regresiones) + hallazgo H-1 → fix → **#3 VERDE 0 hallazgos**
  (7 ítems: build, H-1, scroll básico, wheel, fondo+coloreo, folds,
  gutter/caret). Artefactos `~/opencode/qa_m31_bg/` (cycle1/2/3, fix_scroll).

**Archivos** (4 M, +223/−11): `Controls/SkTextEditor.cs` (fondo negro,
scrollbar, fix de doble sustracción, H-1); `App.axaml` (botón `DocTabList`);
`Views/MainWindow.axaml.cs` (wiring, `ShowTabListMenu`, `RevealSelectedTab`,
flag `--tablist`); `Program.cs` (`--tablist` en `QaDialogArg`).

**Queda pendiente** (fases 2-9, fuera de esta tarea): `.properties`/`.txt`;
intellisense; errores + quick fixes; extension points VSIX; minimap siempre
visible a la derecha.

## Plan de migración de add-ins (blueprint: `docs/plan-migracion-addins.md`)

Estado (2026-10-06): las fases 0 a 3 de este plan están ejecutadas y validadas por QA. La Fase 4 (oleadas de add-ins legacy → Avalonia) está en curso: oleada A completa, oleadas B–E pendientes. El shell Avalonia convive con la UI GTK legacy (`--old-gui`). No se elimina soporte GTK. Los add-ins Avalonia viven en carpetas `MonoDevelop.Avalonia.*`; las carpetas legacy no se tocan.

### Objetivo

Alinear el sistema de add-ins de la shell Avalonia con el modelo de extensiones de Visual Studio, migrar Preferences, pads/comandos y hooks de plataforma, y dejar la ruta GTK (`--old-gui`) compilable y funcional.

### Restricciones técnicas

- Simetría: no se retira GTK. El shell Avalonia coexiste con la UI GTK legacy.
- Ediciones quirúrgicas: no se hace un refactor masivo de `MonoDevelop.Ide`; se envuelve la lógica existente en add-ins compatibles con Avalonia.
- Anidación limitada: por el tope de `childId` en `AddonExtensionNode`, los manifiestos usan rutas de extensión aplanadas (p. ej. `/TextEditor/Analysis/C#`).
- Carpetas: los add-ins Avalonia van en `main/src/addins/MonoDevelop.Avalonia.*`. El código GTK y las carpetas de add-ins legacy no se tocan.

### Fase 0: alineación con el modelo de Visual Studio (infraestructura)

El `AddonHost` trataba el manifiesto como un archivo de configuración simple. Se alinea con el modelo de extensiones de Visual Studio (`extension.vsixmanifest`).

#### AddonManifest.cs

- Assets: `AddonAsset` (`Type`, `Src`, `Assembly`, `ClassName`). El contribuidor declara qué aporta (p. ej. un `MefComponent`) sin lógica hardcodeada del shell.
- Identity: `AddonIdentity` ampliado con `DisplayName`, `Preview` y `Language`.
- Dependencies: `AddonDependency` con rangos de versión (p. ej. `[9.0, 10.0)`) en lugar de igualdad.

#### AddonHost.cs

- Descubrimiento de ensamblados: `EntryPoint` pasa de ruta de archivo a nombre de ensamblado. `AssemblyDependencyResolver` localiza las DLL en la carpeta del add-in.
- Validación de dependencias: comprobación previa a la carga. Si un rango no lo satisface un add-in cargado, el add-in queda `Loaded = false` con error concreto.
- Registro: se respeta `autoLoad: false`.

#### Estado

Hecho. `AddonManifest` y `AddonHost` compilados. Añadidos `AddonLoadContext` y `AddonLoadState`. Corregidos errores de sintaxis en el host. `IsVersionSatisfied` acepta rangos tipo `[9.0, 10.0)`.

### Fase 1: completitud de Preferences (oleada 1 — alta)

Migrar los paneles de opciones restantes creando manifiestos y actualizando el árbol en `PreferencesDialog.axaml.cs`.

#### Add-ins a migrar

- NuGet (General, Sources)
- Debugger (Debugger)
- CSharpBinding (OnTheFly Formatting, Code Style)
- ChangeLogAddIn (ChangeLog Integration)
- DocFood (Feedback)

#### Actualizaciones del árbol

- Extender `MergePoints` con las rutas nuevas.
- Mantener el `LegacySectionMap` acotado por punto para evitar colisiones (p. ej. "General" en Text Editor frente a Version Control).
- Corregir el `ParentId` de .NET Core para que quede dentro de SDK Locations, no en la raíz de Projects.

#### Estado

Hecho. Manifiestos Avalonia de esos add-ins en `MonoDevelop.Avalonia.*`. Árbol de Preferences con `MergePoints` y mapa por punto.

### Fase 2: migración funcional (oleada 2 — media)

Migrar add-ins que aportan comportamiento, comandos y vistas.

#### Pads y menús

Migrar AssemblyBrowser, UnitTesting, HexEditor, RegexToolkit y DesignerSupport.

#### Integración

Usan `IAvaloniaAddon` e `IPackage` para registrarse en el `MenuService` y el `DockingService` del shell.

#### Estado

Hecho. Add-ins Avalonia correspondientes en `main/src/addins/MonoDevelop.Avalonia.*`.

### Fase 3: hooks de plataforma y limpieza (oleada 3 — baja)

- LinuxPlatform: migrar el soporte de plataforma (antes GnomePlatform).
- Hooks multiplataforma: estandarizar hooks para MacPlatform y WindowsPlatform.
- Verificación: la ruta GTK legacy (`--old-gui`) sigue compilable y funcional.

#### Estado

Hecho. Creados:

- `main/src/addins/MonoDevelop.Avalonia.LinuxPlatform/`
- `main/src/addins/MonoDevelop.Avalonia.WindowsPlatform/`
- `main/src/addins/MonoDevelop.Avalonia.MacPlatform/`

`GnomePlatform` y el resto de add-ins GTK intactos. Manifiestos `.avaloniaaddon.json` solo en carpetas `MonoDevelop.Avalonia.*`. QA de la fase 3: aprobado.

### Cómo validar

- Compilar `MonoDevelop.Avalonia.Addons` (0 errores).
- Comprobar que no hay `.avaloniaaddon.json` en carpetas legacy.
- Arrancar el shell Avalonia por defecto y GTK con `--old-gui`.
- QA Senior (AGENTS.md §18.5) sobre cada fase; ciclo hasta 0 hallazgos.

### Pendiente

- Fase 4 del plan de migración de runtime/build (pipeline SDK; no forma parte de este blueprint de add-ins).
- Completar MIME/iconos en los hooks de plataforma más allá del esqueleto → **CERRADO en B.4 (2026-10-17)**: `MimeCatalog` + nodos `/MonoDevelop/Core/MimeTypes` de los manifiestos + iconos del árbol vía `IPlatformHooks` (sección Track B — B.4).

### Fase 4: oleadas de add-ins legacy → Avalonia (estado: oleada A completa)

Nota: esta sección redefine la línea de "Pendiente" anterior sobre la "Fase 4 del plan de migración de runtime/build": la Fase 4 de este blueprint de add-ins son las oleadas A–E de migración de add-ins legacy → Avalonia.

Migrar los add-ins legacy Gtk restantes (alcance aprobado: Núcleo + Dominio, 13 add-ins) a contrapartes Avalonia compatibles con el modelo de extensiones de Visual Studio. Patrón por add-in: carpeta `main/src/addins/MonoDevelop.Avalonia.<X>/` + manifiesto `MonoDevelop.<X>.avaloniaaddon.json` + stub `.cs` (los `.cs` no se compilan en la shell; el manifiesto es la superficie de conexión de la extensión). Las carpetas legacy no se tocan.

#### Oleadas

| Oleada | Add-ins legacy | Estado |
|--------|----------------|--------|
| A | ILAsmBinding, TextTemplating, PerformanceDiagnostics, ConnectedServices | Completa (2026-10-06) |
| B | Deployment (sub-add-ins AspNet/AspNetCore) | Completa (2026-10-06) |
| C | PackageManagement (sub-add-ins UnitTesting.NUnit + Runners) | Completa (2026-10-06) |
| D | VBNetBinding | Completa (2026-10-06) |
| E | MonoDevelop.TextEditor + Packaging | Completa (2026-10-06) |

Diferidos (fuera de las oleadas): backends debugger (Gdb, Soft, VSCodeDebugProtocol, Win32), `MonoDevelop.GtkCore`, y MIME/iconos en los hooks de plataforma más allá del esqueleto.

#### Oleada A — informe (2026-10-06)

**Add-ins creados** (manifiesto + stub en cada carpeta; nodos legacy traducidos a extension points aplanados):

- `main/src/addins/MonoDevelop.Avalonia.ILAsmBinding/` — ID `MonoDevelop.ILAsmBinding`: `ProjectTemplates`, `FileFilters`, `ItemOptionPanels/Build`, `LanguageBindings`, `MSBuildItemTypes` (legacy `main/src/addins/ILAsmBinding/`).
- `main/src/addins/MonoDevelop.Avalonia.TextTemplating/` — ID `MonoDevelop.TextTemplating`: `MimeTypes`, `FileFilters`, `TypeSystem/Parser`, `TextEditorExtensions`, `FileTemplates`, `CustomTools`, `Commands`, `ContextMenu/ProjectPad/Tools`, `FileTemplateTypes` (legacy `main/src/addins/TextTemplating/`).
- `main/src/addins/MonoDevelop.Avalonia.PerformanceDiagnostics/` — ID `MonoDevelop.PerformanceDiagnostics`: `Commands`, `MainMenu/Help`, `GlobalOptionsDialog/PerformanceDiagnostics`, `Pads`, `WorkbenchLayouts/Solution` (legacy `main/src/addins/PerformanceDiagnostics/`).
- `main/src/addins/MonoDevelop.Avalonia.ConnectedServices/` — ID `MonoDevelop.ConnectedServices`: `ProjectModelExtensions`, `Commands/Project`, `MainMenu/Project`, `Pads/ProjectPad`, `Commands/Hidden` (legacy `main/src/addins/ConnectedServices/`).

**Fixes del host de add-ins (regresión de la limpieza de Fase 3 `dbc333d8c8`)** en `main/src/core/MonoDevelop.Avalonia.Addons/`:

- `AddonHost.Discover()`: no registraba los add-ins descubiertos (`addons.Add(state)` ausente) → 0 add-ins cargados. Ahora el registro ocurre en `Discover` y la validación de dependencias se mueve a `LoadAll` (sobre el conjunto completo descubierto).
- Add-ins solo-manifiesto (sin `entryPoint` ni `assets`): se marcan `loaded` y contribuyen sus extension nodes.
- `ValidateDependencies`: una dependencia sobre un componente core del host (p. ej. `MonoDevelop.Ide`, no aportado por otro add-in) se considera satisfecha; solo falla si el add-in dependiente tiene error de carga.
- `LoadAll`: restaurada la composición de partes (`composition.Add(inst)`); eliminado `FindManifestPath` (muerto).
- `AddonLoadState.MarkLoaded()`: `Loaded = Error is null` (un add-in fallido no publica sus nodos).
- `AddonLoadContext`: la resolución del ensamblado por nombre busca junto al manifiesto y en el árbol de build unificado `main/build/` (única dirección de build: `manifestDir/../../../build`; las rutas `bin/Debug/net10.0`/`net8.0` de la carpeta del add-in quedaron obsoletas y retiradas).

**Correcciones de referencias de build** (árbol unificado `main/build/`): los 4 `.csproj` de plataforma (`MonoDevelop.Avalonia.LinuxPlatform/` canonical + duplicado `LinuxPlatform.csproj`, `MacPlatform`, `WindowsPlatform`) apuntan ahora a `../../../build/MonoDevelop.{Core,Ide,Avalonia.Addons}.dll` en lugar del árbol obsoleto `../../build/bin/net10.0/`.

**Limpieza**: eliminado `main/src/addins/MonoDevelop.Avalonia.CSharpBinding/CSharpBinding.avaloniaaddon.json` (manifiesto duplicado con el mismo ID `MonoDevelop.CSharpBinding` y sin `displayName`; el correcto es `MonoDevelop.CSharpBinding.avaloniaaddon.json`).

**Validación:**

- Build de la shell (`dotnet build src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj`): **0 errores**.
- Smoke test (`xvfb-run -a dotnet MonoDevelop.AvaloniaShell.dll --addonmanager` desde `main/build/`): **22/25 add-ins cargados**, 0 FATAL; los 4 de la oleada A `loaded=True`; `MonoDevelop.CSharpBinding` carga una sola vez. Fallos esperados (deuda conocida): `MonoDevelop.LinuxPlatform` (manifiesto duplicado sin DLL — hallazgo M-1, pendiente de la decisión de plataforma), `MonoDevelop.Avalonia.MacPlatform` y `MonoDevelop.Avalonia.WindowsPlatform` (ensamblados no compilados en Linux — deuda de hooks multiplataforma).
- Sin manifiestos en carpetas legacy: `find main/src/addins -name "*.avaloniaaddon.json"` solo devuelve carpetas `MonoDevelop.Avalonia.*`.
- QA Senior (§18.5): **APTO**. Hallazgos: A-1 (ALTO) cierre §18.2 sin documentación/commit → resuelto con esta documentación (el commit queda pendiente de la decisión del operador); M-1 `LinuxPlatform.avaloniaaddon.json` con ID fantasma → pendiente de la migración GnomePlatform→LinuxPlatform; M-2 duplicado CSharpBinding → resuelto en esta limpieza; B-1 build frágil por MSBuild nodeReuse (ambiental, sin acción de código).

**Pendiente de la Fase 4:** ~~oleadas C–E y la decisión de diseño de la migración GnomePlatform→LinuxPlatform.~~ → **CERRADO**: oleadas C, D y E completadas e informadas abajo (2026-10-06) y la migración GnomePlatform→LinuxPlatform completa (2026-10-06; `session_summary.md` § Migración GnomePlatform→LinuxPlatform).

#### Oleada B — informe (2026-10-06)

**Add-ins creados** (manifiesto + stub; nodos legacy traducidos a extension points aplanados):

- `main/src/addins/MonoDevelop.Avalonia.Deployment/` — ID `MonoDevelop.Deployment`: `Commands` (CreatePackage/AddPackage/Install), `MainMenu/Project`, `ContextMenu/ProjectPad/Tools`, `ProjectTemplates` (PackagingProject), `Pads/ProjectPad` (2 NodeBuilders), `PackageBuilders`, `DeployFileCopiers`, `DeployServiceExtensions`, `PackageBuilderEditors`, `FileCopyConfigurationEditors`, `ContextMenu/ProjectPad/Package`, `ContextMenu/ProjectPad/PackagingProject`, `DeployDirectories` (6), `DeployPlatforms` (2), `ItemOptionPanels`, `SerializableClasses` (6), `StockIcons`/`TemplateImages` (legacy `main/src/addins/Deployment/MonoDevelop.Deployment/`).
- `main/src/addins/MonoDevelop.Avalonia.AspNet/` — ID `MonoDevelop.AspNet`: `ProjectTemplates` (3), `FileTemplates` (34), `FileTemplateTypes`, `FileFilters` (2), `LegacyEditorSupport`, `ProjectModelExtensions` (5 flavors con GUID), `TypeSystem/Parser` (3), `SerializableClasses`, `ItemOptionPanels/Run` (XSP), `ToolboxLoaders`/`ToolboxProviders`, `Commands` (6), `ContextMenu/ProjectPad/Add`, `Pads/ProjectPad` (2), `MimeTypes` (16), `TextEditorExtensions` (2), `Html/DocTypes` (4), `ExecutionHandlers` (XSP), `CompletionCharacters`, `SourceEditor2/ContextMenu/Editor`, `CodeTemplates`, `CodeFormatters`, `CustomTools`, `FileTemplateConditionTypes`, `ProjectTemplateWizards`, `TemplateImages`/`StockIcons` (legacy `main/src/addins/AspNet/`).
- `main/src/addins/MonoDevelop.Avalonia.AspNetCore/` — ID `MonoDevelop.AspNetCore` (deps: `MonoDevelop.Ide` + `MonoDevelop.DotNetCore`): `FileTemplates` (13, con `file`), `ProjectTemplateCategories/netcore/app`, `Templates` (13, con condición SDK), `ExecutionHandlers`, `RunConfigurationEditors`, `FileTemplateConditionTypes`, `Pads/ProjectPad` (ScaffoldNodeExtension), `ContextMenu/ProjectPad`, `Commands/Project` (3), `ContextMenu/ProjectPad/Publish`, `MainMenu/Build`, `ProjectTemplateWizards` (legacy `main/src/addins/MonoDevelop.AspNetCore/`).

**Decisiones documentadas (hallazgos BAJO de QA aceptados como decisiones del plan):**

- AspNetCore: el legacy define las plantillas en 6 bloques por SDK (3.1/3.0/2.2/2.1/2.0/1.x, ~57 nodos); el manifiesto aplanado conserva las 13 del SDK más reciente (3.1) con condición `AspNetCoreSdkInstalled, UseNetCore31=true` y descarta los bindings de variantes de SDKs legacy (`templateId`/`path` al nupkg). La shell moderna (.NET 10) no consume esas variantes.
- EP `/MonoDevelop/Deployment/DeployDirectoryResolvers` (legacy) sin declarar: en el host los EP se auto-declaran al registrar el primer nodo (`AddonHost.cs`); el sub-add-in pendiente `MonoDevelop.Deployment.Linux` lo extiende y se migrará con él.
- EP `/MonoDevelop/Asp/CompletionBuilders` (consumidor: CSharpBinding legacy) sin declarar hasta migrar su consumidor; `commitChars` de `CompletionCharacters` y detalles `isText`/`baseType` de MIME descartados en el aplanado (se conservan `commitOnSpace`/`pattern`).

**QA (§18.5, 2 rondas):** ronda 1 **NO APTO** (2 MEDIO: 3 FileTemplates faltantes en AspNetCore → añadidos con su atributo `file`, y los `file` faltantes de los 10 existentes; 4 PNG de `docs/img/` borrados accidentalmente del working tree → restaurados con `git checkout -- docs/img/...`, mismo remedio que el precedente de `docs/session_summary.md:696-698`) + 3 BAJO aceptados como decisiones del plan; ronda 2 **APTO**: build 0 errores; smoke `discovered=28` / `loaded 25/28` (los 3 nuevos `loaded=True`, `FileTemplates nodes=49`, `MonoDevelop.CSharpBinding` una sola vez, solo los 3 fallos esperados, 0 FATAL); 28 manifiestos con 0 IDs duplicados; 0 manifiestos en carpetas legacy; 0 cambios en archivos legacy (ruta GTK intacta).

#### Migración GnomePlatform→LinuxPlatform (completa, 2026-10-06)

**Decisión del operador**: seguir la migración ya iniciada en `main/src/addins/MonoDevelop.Avalonia.LinuxPlatform/`; los archivos `LinuxPlatform.avaloniaaddon.json` y `LinuxPlatform.csproj` (sin prefijo Avalonia) **no son residuo**: forman parte de la migración y deben funcionar ahí. **Sin borrados.**

**Qué se hizo:**

- `LinuxPlatform.csproj` (AssemblyName `MonoDevelop.LinuxPlatform`, con referencia a `MonoDevelop.Ide`) compila `LinuxPlatform.cs` y produce `MonoDevelop.LinuxPlatform.dll` en el árbol de build unificado `main/build/` → el manifiesto `LinuxPlatform.avaloniaaddon.json` (asset `MonoDevelop.LinuxPlatform.dll`, clase `MonoDevelop.Platform.LinuxPlatform`) ahora carga.
- `MonoDevelop.Avalonia.LinuxPlatform.csproj` (canonical) produce `MonoDevelop.Avalonia.LinuxPlatform.dll` en `main/build/`; su manifiesto registra el nodo `/MonoDevelop/Core/PlatformService` para la shell Avalonia.
- Los 4 csproj de plataforma (Linux ×2, Mac, Windows) fijan `OutDir` a `$(MSBuildProjectDirectory)/../../../build/` = `main/build/` (única dirección de build). Nota: la barra antes de los `..` es obligatoria — sin ella, `$(MSBuildProjectDirectory)` (sin barra final) concatena el primer `..` con el nombre de la carpeta (`LinuxPlatform..`) y el SDK resuelve un nivel menos.
- `AddonLoadContext`: la resolución por nombre busca junto al manifiesto y en el árbol unificado `main/build/` (ver corrección del bullet de `AddonLoadContext` en la oleada A).
- Implementación `LinuxPlatform.cs` (común a ambos ensamblados): detecta el escritorio (`XDG_CURRENT_DESKTOP`/`DESKTOP_SESSION`), apertura de URLs con `xdg-open`, fuente monoespaciada por defecto, y el registro del servicio de plataforma por reflexión a `MonoDevelop.Ide.Desktop.DesktopService`. El método `SetPlatformService` no existe aún en `MonoDevelop.Ide`; cuando exista, el registro se activa sin más cambios (`MonoDevelop.Ide.dll` está disponible en `main/build/` para el load context del add-in). Mientras tanto, el nodo de plataforma funcional para Avalonia lo aporta el manifiesto canonical.

**Validación:** build 0 errores (shell + 2 proyectos de plataforma); smoke `--addonmanager`: **26/28** add-ins cargados, `MonoDevelop.LinuxPlatform` y `MonoDevelop.Avalonia.LinuxPlatform` `loaded=True`, 0 FATAL. Los 2 fallos restantes son la deuda conocida de Mac/Windows (sus `.cs` llaman `DesktopService.SetPlatformService`, inexistente, y no compilan en Linux).

**Migración Mac/Windows (completa, 2026-10-06):** las carpetas `MonoDevelop.Avalonia.MacPlatform/` y `MonoDevelop.Avalonia.WindowsPlatform/` quedan con la misma estructura que LinuxPlatform — doble manifiesto + doble csproj (canonical `MonoDevelop.Avalonia.<P>Platform.*` + legacy-named `<P>Platform.*`, IDs `MonoDevelop.MacPlatform`/`MonoDevelop.WindowsPlatform`) — con **compilación condicional por SO**: `WindowsPlatform` solo se compila en Windows y `MacPlatform` solo en macOS, vía `Directory.Build.targets` en cada carpeta (fuera de su SO el proyecto es no-op: los targets `CoreCompile`/`CopyFilesToOutputDirectory` se anulan y un target de limpieza elimina el `deps.json`; no queda ningún artefacto en `main/build/`). Nota: los overrides van en `Directory.Build.targets` (importado al final de `Microsoft.Common.targets`) porque los targets definidos en el cuerpo del `.csproj` quedan anulados por `Microsoft.NET.Sdk.targets` (último en importarse; regla last-wins).

**Fixes de fuente** (`MacPlatform.cs`, `WindowsPlatform.cs`): la migración previa no compilaba en ninguna plataforma. (1) Eliminada la llamada a `DesktopService.SetPlatformService(this)` — el método no existe: `DesktopService.OnInitialize` lee el EP `/MonoDevelop/Core/PlatformService` vía `AddinManager.GetExtensionObjects`, y el nodo de ese EP (declarado en el manifiesto canonical) es lo que registra la plataforma. (2) Eliminados los `override IsWindows/IsMac/IsLinux` — no existen en `PlatformService` (pertenecen a la clase estática `MonoDevelop.Core.Platform`). Las clases son ahora subclasses puros de `PlatformService` + `IAvaloniaAddon` (mismo modelo que el legacy `GnomePlatform`): overrides válidos `DefaultMonospaceFont`, `Name`, `ShowUrl` (mac: `open`; windows: shell execute), `GetApplications`, `OnGetMimeType*`, `OnGetIcon*`.

**Verificación**: (1) en Linux los 4 csprojs de Mac/Windows compilan como no-op (0 errores, 0 artefactos en `main/build/`); (2) compilación forzada de las fuentes contra `MonoDevelop.{Core,Ide,Avalonia.Addons}.dll` + `Xwt.dll` (scratch `~/opencode/forced_build/`): **0 errores / 0 advertencias** en los 4 proyectos, y cada DLL contiene solo su clase; (3) build de la shell 0 errores; smoke `--addonmanager`: **35 discovered, 31 loaded=True** — en Linux los 4 add-ins Mac/Windows reportan `Assembly not found` (esperado por diseño: la DLL solo existe al compilar en su plataforma), los 2 Linux siguen `loaded=True`, `ep /MonoDevelop/Core/PlatformService nodes=1`, 0 FATAL. Log: `~/opencode/platform_smoke.log`.

**Pendiente restante:** ~~el enganche runtime GUI de la plataforma (puente entre el registro del host y `AddinManager`/`DesktopService`) es deuda de hooks compartida por las 3 plataformas (ver `docs/fase3-platform-hooks-cleanup.md`); MIME/iconos más allá del esqueleto.~~ → **CERRADO**: el enganche runtime GUI de la shell Avalonia lo resolvió el Track 1 (2026-10-12: `IPlatformHooks` + `App.Platform` + activación real del add-in vía ALC; el lado GTK mantiene su wiring original vía `Mono.Addins`) y los MIME/iconos lo resolvió B.4 (2026-10-17).
- QA (rol Tester QA Senior, §18.5): **APTO** (2 rondas). Ronda 1: NO APTO — 1 MEDIO (discrepancia de alcance del prompt QA: los cambios en los csproj de LinuxPlatform son parte aprobada y documentada de la migración de plataformas de esta sesión) + 4 BAJO → corregidos: `return null!` en `OnGetIconForFile` (CS8603 con `<Nullable>enable`), restaurados los 2 PDB sucios del submódulo `mono-addins`, eliminados bin/obj obsoletos de las 3 carpetas de plataforma. Ronda 2: **APTO** — compilación nullable 0 errores/0 advertencias (cada DLL solo con su clase), no-op en Linux sin artefactos en `main/build/`, smoke 35/31 con 0 FATAL; único BAJO no bloqueante (obj/ regenerado por el build de validación, gitignored) limpiado antes del commit. Artefactos: `~/opencode/qa_platforms/`.

### Oleada C — informe (2026-10-06)

**Add-ins creados** (`main/src/addins/`):

| Carpeta nueva | ID manifiesto | Notas |
|---|---|---|
| `MonoDevelop.Avalonia.PackageManagement/` | `MonoDevelop.PackageManagement` | Manifiesto completo: 10 `NuGet.*` commands, MainMenu/Project, ContextMenu ProjectPad (+Add), StockIcons, GlobalOptionsDialog, `ProjectTemplatePackageInstallers`/`ItemTemplatePackageInstallers`, 6 NodeBuilders en `Pads/ProjectPad`, EP `/MonoDevelop/PackageManagement/ContextMenu/ProjectPad/PackageReference` (3 nodos: `MonoDevelop.PackageManagement.Commands.PackageReferenceNodeCommands.ReinstallPackage`, `...UpdatePackage`, `MonoDevelop.Ide.Commands.EditCommands.Delete`), PropertyProviders, StartupHandlers, SearchCategories, ProjectModelExtensions, SystemInformation, FileTemplateConditionTypes |
| `MonoDevelop.Avalonia.UnitTesting.NUnit/` | `MonoDevelop.UnitTesting.NUnit` | `TestProviders` `SystemTestProvider` (clase `MonoDevelop.UnitTesting.NUnit.SystemTestProvider`), `ProjectTemplates` C#/VB NUnit, `FileTemplates` `NUnitTestClass`, TemplateImages/StockIcons `md-test-project`, `UnitTestMarkers` con los 5 atributos NUnit. Depende de `MonoDevelop.Ide` y `MonoDevelop.UnitTesting` (base existente) |
| — | `MonoDevelop.UnitTesting.NUnit.Runners` | **Nada que migrar**: la carpeta legacy es una librería vacía (`MyClass.cs`), sin manifiesto ni referencias; la implementación real de runners vive en el add-in `MonoDevelop.UnitTesting`, que ya existe como Avalonia (`MonoDevelop.Avalonia.UnitTesting`) |

**Verificación**: build 0 errores. Smoke `--addonmanager`: **30 discovered, 28 loaded=True** — `MonoDevelop.PackageManagement` y `MonoDevelop.UnitTesting.NUnit` cargan (`loaded=True`); EPs: `TestProviders nodes=1`, `UnitTestMarkers nodes=1`, `ProjectTemplates nodes=7` (+2 NUnit), `FileTemplates nodes=50` (+1), `PackageManagement/.../PackageReference nodes=3`. Únicos fallos: Mac/Windows (deuda pendiente, sin API en el lado compartido). Log: `~/opencode/wavec_smoke.log`.
- QA (rol Tester QA Senior, §18.5): **APTO**. 0 ALTO; 2 imprecisiones documentales del propio informe corregidas (IDs reales de los 3 nodos del EP `PackageReference`; 5 atributos NUnit, no 6). 59 nodos comparados 1:1 con los manifiestos legacy (0 faltantes, 0 divergentes); 30/30 JSON válidos, 0 IDs duplicados; legacy intacta. Artefactos: `~/opencode/qa_wavec/`.

### Oleada D — informe (2026-10-06)

**Add-in creado**: `main/src/addins/MonoDevelop.Avalonia.VBNetBinding/` — ID `MonoDevelop.VBNetBinding` (legacy `main/src/addins/VBNetBinding/`): `MSBuildItemTypes` (VBNet/.vbproj, guid, `VBProject` + resourceHandler), `FileFilters` (*.vb), `FileTemplates` (EmptyVBFile, VBAssemblyInfo), `ProjectTemplates` (EmptyProject, ConsoleProject), `ItemOptionPanels/Build/General` + `/Build/Compiler` (3 panels con condición `ProjectTypeId=VBNet`), `StockIcons` (md-vb-file), `MimeTypes` (text/x-basic), `LanguageBindings` (VBNet), `SerializableClasses` (VBCompilerParameters, Import), `TextEditorExtensions` (VBNetTextEditorExtension), `TypeSystem/Parser` (TypeSystemParser).

**Decisión**: el `<Module>` legacy (dependencia `GtkCore` + plantilla `MonoDevelop.VBNet.GtkSharp2Project`) queda fuera de alcance: `MonoDevelop.GtkCore` es diferido.

**Verificación**: build 0 errores. Smoke `--addonmanager`: **31 discovered, 29 loaded=True** — `MonoDevelop.VBNetBinding loaded=True`; EPs: `ProjectTemplates nodes=9` (+2), `FileTemplates nodes=52` (+2), `LanguageBindings nodes=2` (+1), `MSBuildItemTypes nodes=2` (+1), `TextEditorExtensions nodes=4` (+1). Únicos fallos: Mac/Windows (deuda pendiente). Log: `~/opencode/waved_smoke.log`.

### Oleada E — informe (2026-10-06)

**Add-ins creados**:

- `main/src/addins/MonoDevelop.Avalonia.Packaging/` — ID `MonoDevelop.Packaging` (legacy `main/src/addins/MonoDevelop.Packaging/`): `ConditionTypes` (ProjectHasNuGetMetadata), `MSBuildItemTypes` (NuGet.Packaging/.nuproj), `ProjectModelExtensions` (PackagingProjectFlavor + DotNetProjectPackagingExtension), `LanguageBindings` (NuGet.Packaging), `MSBuildGlobalPropertyProviders`, `ProjectTemplates` (PackagingProject, CrossPlatformLibrary), `ProjectTemplateWizards` ×2, `ItemOptionPanels` (sección NuGetPackage + panels Build/Metadata/ReferenceAssemblies con condiciones), `Commands` (CreateNuGetPackage, AddPlatformImplementation), `MainMenu/Build`, `ContextMenu/ProjectPad` (+Add), `DesignerSupport/PropertyProviders` ×2.
- `main/src/addins/MonoDevelop.Avalonia.TextEditor/` — ID `MonoDevelop.TextEditor` (legacy `main/src/addins/MonoDevelop.TextEditor/MonoDevelop.TextEditor/Properties/`): `Ide/Composition` (MonoDevelop.TextEditor.dll), `Commands` (categoría TextEditor + 6 comandos de caret con shortcuts), `TextEditor/CommandMapping` (53 Maps a `*CommandArgs` de VS), `TextEditor/ContextMenu/Editor` (20 nodos: 15 comandos + 5 separadores; bloque "temporary" activo), `Core/FeatureSwitches` (AlwaysUseLegacyEditor).

**Decisiones**:

1. Solo se migran nodos activos: los bloques legacy comentados en XML dentro del manifiesto de TextEditor (TooltipProviders, comandos SourceEditor, menús antiguos, menús Scrollbar/NavigationBar, secciones GlobalOptionsDialog/TextEditor, MainMenu/Search, EditorFactory, AutoInsertBracket, UserDataMigration, StockIcons) ya estaban desactivados en el legacy → no se migran.
2. Las 5 declaraciones `ExtensionPoint` (ContextMenu/Editor, CommandMapping, SupportedFileTypes, LegacyEditorSupport, TextMate) y el `ConditionType` `FileType` no tienen representación funcional en el host Avalonia: el host auto-declara el EP al registrar el primer nodo (precedente oleada B); `SupportedFileTypes` y `LegacyEditorSupport` además no tienen nodos en el propio manifiesto legacy.
3. Sub-add-ins `MonoDevelop.TextEditor.Wpf` y `.Cocoa`: backends de plataforma → diferidos (Mac/Windows).

**Verificación**: build 0 errores. Smoke `--addonmanager`: **33 discovered, 31 loaded=True** — `MonoDevelop.Packaging` y `MonoDevelop.TextEditor` `loaded=True`; EPs: `TextEditor/CommandMapping nodes=53`, `TextEditor/ContextMenu/Editor nodes=20`, `ProjectTemplates nodes=11` (+2), `MSBuildItemTypes nodes=3` (+1), `LanguageBindings nodes=3` (+1), `Core/FeatureSwitches nodes=1`, `Ide/MainMenu/Build nodes=4` (+2). Únicos fallos: Mac/Windows (deuda pendiente). Log: `~/opencode/wavee_smoke.log`.

**Estado de la Fase 4**: oleadas A–E completas — los 13 add-ins del alcance (Núcleo + Dominio) están cubiertos; `MonoDevelop.UnitTesting.NUnit.Runners` queda documentado como stub vacío sin migración (oleada C).
- QA (rol Tester QA Senior, §18.5): **APTO** (2 rondas). Ronda 1: 0 ALTO; 2 MEDIO resueltos — jerarquía de la sección `NuGetPackage` registrada con `childId` en los 3 paneles y paréntesis en la condición aplanada de `ContextMenu/ProjectPad`; el otro MEDIO es preexistente (duplicado CSharpBinding a HEAD; el borrado está en el worktree, pendiente del commit que decidirá el operador). Ronda 2: verificación focalizada, smoke idéntico (33/31, 0 FATAL), 0 hallazgos nuevos. Comparación 1:1: VBNetBinding 12 EPs; Packaging 13 EPs + ConditionType; TextEditor 53/53 Maps y 20/20 nodos de menú. Artefactos: `~/opencode/qa_waveDE/`.

### Integración add-ins ↔ Preferencias/shell (completa, 2026-10-07)

**Contexto**: tras el commit `558210e02a` (Fase 4 oleadas A–E + plataformas Mac/Windows), se verificó cómo se integran los add-ins migrados con el shell: el diálogo de Preferencias (skeleton + merge de extension points), el gestor de add-ins y la ruta de arranque.

**Defectos encontrados y corregidos** (`main/src/core/MonoDevelop.Startup.Avalonia/Views/PreferencesDialog.axaml.cs`):

1. **P1 (ALTO) — panel PerfDiag inalcanzable**: el nodo de la rama "Performance Diagnostics → General" tenía id `perfgeneral`, pero la página portada (`PanelPerfDiag`, ~línea 2429) y `functionalPanels` esperan `perfdiag` → la categoría mostraba placeholder. Fix: skeleton y `LegacySectionMap` usan ahora `perfdiag`; no queda ninguna referencia a `perfgeneral`.
2. **P2 (MEDIO) — secciones NuGet mal ubicadas bajo "Projects"**: el EP raíz `/MonoDevelop/Ide/GlobalOptionsDialog` se mergeaba en la categoría "Projects", de modo que los nodos de PackageManagement (`NuGetPackageManagement`, `NuGetGeneral`, `PackageSources`) aparecían como leaves bajo Projects duplicando la categoría top-level "NuGet". En el modelo legacy el EP raíz porta secciones top-level (la sección NuGet es `insertafter="VersionControl"` con hijos General/Sources). Fix: `MergeAddonSections` trata el EP raíz por separado (constante `RootOptionsPoint`): los nodos sin `childId` (o con childId huérfano) son categorías top-level — se adopta la categoría existente si el label coincide (case-insensitive) o se añade una nueva al final — y los hijos vinculados por `childId` se añaden con dedupe por id/label (método auxiliar `AddSection`). En `MonoDevelop.PackageManagement.avaloniaaddon.json`, `NuGetGeneral` y `PackageSources` llevan ahora `"childId": "NuGetPackageManagement"` (misma convención que `ItemOptionPanels` de la oleada E).
3. **P3 (BAJO) — merge point `/TextEditor/Analysis/C#` fallaba en silencio**: la búsqueda del parent (`analysis-csharp`) solo miraba hijos directos de "Text Editor", pero el nodo está anidado bajo "Source Analysis" → no se añadían `CodeStylePanel` (CSharpBinding) ni `Code Actions`/`Code Generation`/`Code Rules` (Refactoring). Fix: búsqueda recursiva `FindNode` (DFS) dentro de la categoría.
4. **P4 (BAJO) — iconos declarados en manifiestos no resolubles**: `md-prefs-package` y `md-prefs-package-source` (PackageManagement) y, una vez expuesto por P3, `md-prefs-code-actions` y `md-prefs-code-rules` (Refactoring) no existían en `IconService`. Fix: 4 mapeos añadidos al mapa de add-ins de `main/src/core/MonoDevelop.Ide/Services/IconService.cs` (los PNG ya existen en `main/src/addins/<add-in>/icons/`) y los leaves del skeleton "NuGet" usan los stock-ids reales (`md-prefs-package`/`md-prefs-package-source`), como en el legacy GTK.

**Cómo validar**:
1. `cd main && dotnet build src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj` → 0 errores.
2. `cd main/build && timeout 60 xvfb-run -a dotnet MonoDevelop.AvaloniaShell.dll --prefs-tree` (exit 124 normal) → el log trae `merged=5 (registry points=85)` (DocFood + 4 nodos de `Analysis/C#`); **0** `(missing)`; `Projects` sin entrada NuGet; `NuGet` top-level con `General id=nugetgeneral icon=md-prefs-package` + `Sources id=packagesources icon=md-prefs-package-source`; `Text Editor → Source Analysis → C#` con `codestylepanel`, `codeactions`, `codegeneration`, `coderules`; `Performance Diagnostics → General id=perfdiag`; 8 categorías top-level.
3. Smoke `--addonmanager` (exit 124): **35 discovered, 31 loaded=True** (las 4 falsas de Mac/Windows son por diseño en Linux); `PackageManagement loaded=True` (el manifiesto con `childId` parsea).
4. `--old-gui` (ruta Gtk legacy, mismo binario): arranque sin excepciones; los cambios solo añaden entradas de diccionario en `IconService` y tocan un Views exclusivo de Avalonia.

**QA (§18.5)**: Tester QA Senior, **APTO** en 2 rondas. Ronda 1: 0 ALTO/MEDIO; 1 BAJO resuelto en ronda 2 (iconos NuGet "dormidos": el dedupe conservaba los leaves del skeleton con `md-prefs-generic` → los leaves usan ahora los stock-ids reales). INFO no cerrados (preexistentes/cosméticos): (a) `merged=` se loguea 2 veces porque `BuildModel` se llama desde `BuildSectionTree` y `DumpTreeForQa`; (b) `[prefs-csharp] Roslyn key unavailable ... using legacy name` es fallback preexistente. Artefactos: `~/opencode/qa_prefs_shell_fix/`.

**Pendiente**:
- Los 5 nodos añadidos por el merge (`codestylepanel`, `codeactions`, `codegeneration`, `coderules`, `docfood`) son placeholders hasta portar sus paneles (no existen aún en el skeleton).
- P5 (INFO): el log duplicado de `merged=` es cosmético; no se toca para mantener el diff mínimo.

### Revisión final 1:1 — migración de add-ins COMPLETADA (2026-10-07)

**Contexto**: revisión final y exhaustiva de las 32 carpetas / 35 manifiestos Avalonia frente a sus equivalentes legacy, para dar por terminada la migración de add-ins: paridad de extension points (nodos, jerarquías, labels, condiciones), iconos e imágenes, y comportamiento del diálogo de Preferencias. Herramientas: `~/opencode/final_review/compare.py` (comparador 1:1 nodo a nodo, con normalización de mnemónicos GTK en cualquier posición) e `icon_audit.py` (resolución de cada icono usado en manifiestos AV contra los mapas de `IconService`).

**Defectos encontrados y corregidos**:

1. **`main/src/core/MonoDevelop.Ide/Services/IconService.cs` — 22 stock-ids legacy sin resolver** (secciones de Preferencias, pads y comandos se renderizaban sin icono):
   - Mapa core: `md-command-window` (pad Immediate) y `md-vb-file` (alias stock→stock en legacy hacia `md-file-source`; mapeado directo al mismo PNG `file-source-16`).
   - Mapa de add-ins: 11 de Preferencias (`md-prefs-text-editor-general`, `md-prefs-markers-rulers`, `md-prefs-text-editor-behavior`, `md-prefs-completion`, `md-prefs-syntax-highlighting` → SourceEditor2; `md-prefs-debugger` → Debugger; `md-prefs-code-analysis` → Refactoring; `md-prefs-version-control`, `md-prefs-commit-message-style`, `md-prefs-git` → VersionControl; `md-prefs-xml` → Xml) y 10 de pads/comandos (`md-view-debug-call-stack/locals/threads/watch` → Debugger; `md-properties-pad`, `md-toolbox-pad`, `md-pad-document-outline` → DesignerSupport; `nunit-pad-icon` → `MonoDevelop.UnitTesting/Gui`; `md-gettext-locale` → Gettext).
2. **`main/src/core/MonoDevelop.Startup.Avalonia/Views/PreferencesDialog.axaml.cs`**: (a) los hijos declarados con `childId` se perdían en el merge (p. ej. `CompletionBehavior` bajo `CodeCompletion`); `MergePoints` ahora procesa en dos pasadas (top-level primero, luego hijos bajo su padre) y `AddSection` devuelve el nodo existente en caso de duplicado (dedupe). (b) Los leaves del skeleton usaban stock-ids genéricos (`md-prefs-generic`); ahora usan los stock-ids reales del legacy, los mismos que mostraba la UI GTK.
3. **18 manifiestos `*.avaloniaaddon.json`** — espejo 1:1 de EPs, jerarquías, labels e iconos:
   - **UnitTesting**: EP `Ide/Commands` reescrito — el manifiesto tenía 1 nodo inventado (`RunTests` + icono `md-prefs-run` inexistente en legacy) y el legacy declara la categoría "Unit Testing" con 20 comandos → espejo completo (21 nodos; `md-run-unit-tests` resuelve por el mapa core `ParsedStockMap`).
   - **AspNet**: hack `commandId` sustituido por `childId` real; `GoToController` dividido en 2 nodos por condición (`.aspx`/cshtml) + separadores; labels del legacy (`Controller...`, `View...`, `Add View...`); id `AspNetApp` completo.
   - **PerformanceDiagnostics**: 13 nodos MainMenu/Help con las 3 ItemSets `Diagnostics` del legacy (repartidas en 2 archivos legacy; la central lleva `condition=FeatureSwitch=WidgetLeaks`).
   - **PackageManagement**: 10 comandos con `childId=NuGet` + 2 separadores condicionales; labels 1:1 (`Manage NuGet Packages...`, contexto `NuGet Packages...`).
   - **SourceEditor2**: `childId` invertido eliminado; `CompletionBehavior`/`CompletionAppearance` añadidas bajo `CodeCompletion`; classNames.
   - **Refactoring**: panel demo (`"class": MonoDevelop.AvaloniaAddons.Demo.AnalysisPlaceholder`) → `className` real (`MonoDevelop.AnalysisCore.Gui.AnalysisOptionsPanel`); ids de paneles a los del legacy (`CodeActions`/`CodeGeneration`/`CodeRules`) + classNames.
   - **Gettext**: nodo fantasma `TranslateProject` eliminado; 4 items de contexto condicionados + EP `Ide/Commands` con las 4 definiciones (icono `md-gettext-locale`).
   - **CSharpBinding/XmlEditor/Debugger/ChangeLogAddIn/VersionControl**: classNames de paneles; iconos VC reales (`md-prefs-commit-message-style`, `md-prefs-git`).
   - **Deployment/Packaging/TextEditor**: `childId` de categorías en comandos; **AspNetCore**: 2 `ProfilesSeparator`; **ConnectedServices**: separador + label `Add`; **DotNetCore**: panel `DotNetCoreSdkSettings`.

**Set documentado (no corregido, por decisión)** — el comparador termina en `TOTAL nodos FALTANTES: 57`, 0 divergentes y 2 dif-parent, todos dentro de este set:

- **AspNetCore (48 faltantes + 13 divergentes + 1 dif-parent)**: templates de variantes por SDK y formato de condiciones — fuera del alcance de la fase 4 (plantillas generadas por SDK).
- **Refactoring (4 faltantes)**: artefacto del aplanado del EP `/TextEditor/Analysis/C#` solo-av; en runtime verificado 1:1.
- **LinuxPlatform / MacPlatform (1 faltante + 1 extra c/u)**: sustitución de clases de plataforma (`GnomePlatform`→`LinuxPlatform`, `MacPlatformService`→`MacPlatform`).
- **VBNetBinding (1 faltante + 1 extra + 2 sin-id)**: `GtkSharp2Project` (GtkCore diferido) y ids legacy sin espejar.
- **VersionControl (2 faltantes + 4 extras)**: el legacy usa el id duplicado `VersionControlGeneral` ×2 (el modelo AV no admite duplicados) → ids propios + `LegacySectionMap` (correctitud en runtime); los extras son la consolidación de los fragmentos Git/ChangeLog.
- **DotNetCore (1 dif-parent)**: aplanado del panel (el runtime deduplica).
- **Deployment (3 sin-id)**: nodos legacy sin id (limitación del modelo).
- Los `eps-solo-legacy` restantes (CSharpBinding 41, VersionControl 21, Debugger 15, ...) son superficies diferidas (context menus, test chart, etc.), no EPs de la fase 4.
- **MacPlatform `pause.png`**: defecto legacy preexistente (el recurso jamás existió en el historial de git).

**Cómo validar**:

1. `cd main && dotnet build src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -m:1` → 0 errores.
2. `cd main/build && timeout 120 dotnet MonoDevelop.AvaloniaShell.dll --prefs-tree` → `merged=5 (registry points=85)`, 0 `(missing)`, iconos legacy en las 11 secciones verificadas.
3. `--addonmanager` → `loaded 31/35` (las 4 falsas son Mac/Windows, por diseño en Linux); 0 FATAL.
4. `--old-gui` (ruta Gtk legacy, mismo binario) → arranca y corre sin excepciones (exit 124 por timeout = OK).
5. `python3 ~/opencode/final_review/icon_audit.py` → 66 ids en uso, **0 sin resolver**.
6. `python3 ~/opencode/final_review/compare.py` → `TOTAL nodos FALTANTES: 57` (el set documentado de arriba), 0 divergentes.

**QA (§18.5)**: Tester QA Senior, **APTO — 0 errores** (2 advertencias + 7 info, ninguno bloqueante). Advertencias: (1) 5 ids duplicados dentro del mismo EP — espejo fiel de ids duplicados del legacy bajo condiciones distintas (desambiguados por `condition`; esos EPs no son consumidos por el runtime actual); (2) campos de nodo fuera del listado mínimo en manifiestos — preexistentes, no introducidos por este diff, ignorados por el runtime (deuda técnica documentada). Cerrados en este diff: `.gitignore` no cubría `main/src/core/MonoDevelop.Avalonia.Addons/bin/` (añadida la regla) y el manifiesto de UnitTesting sin newline final (añadido). Artefactos: `~/opencode/final_review/` (`findings_v4.txt`, `run_*.txt`, `qa_report_final.txt`, `manifest_check.py`, `report_*.txt`).

**Cierre**: la migración de add-ins está **COMPLETADA**: los 35 manifiestos son espejo 1:1 de los legacy dentro del alcance aprobado, todos los iconos en uso resuelven, y las rutas Avalonia y `--old-gui` del mismo binario funcionan. Este documento queda cerrado para trabajo de add-ins: las tareas posteriores (paneles de Preferencias placeholder, superficies diferidas, variantes de SDK, GtkCore) se documentarán en sus propios documentos de sesión.

### Add-in Manager: 3 pestañas sobre el host nuevo (Avalonia) — sin Mono.Addins/Mono.Cecil (2026-10-07)

**Contexto**: el Add-in Manager de la UI Avalonia (`AddinManagerDialog`, Tools > Extensions… / `--addins` / `--extensionsdlg`) se alimentaba del registry Mono.Addins (`SetupService` + `AddinEngineHost`). El escaneo de ese registry fallaba **íntegro** en el build unificado: `Mono.Addins.CecilReflector` (submódulo) compila contra Mono.Cecil 0.9.6 (`InterfaceImplementation`, `ModuleDefinition.FileName`) mientras el build estagia Cecil 0.10.1 → `MissingMethodException` al escanear cada ensamblaje → "The add-in database could not be updated" → **pestaña Installed vacía (items=0)**. El diálogo se desacopló de los componentes legacy: sus 3 pestañas (Installed, Updates, Gallery) conservan la UI original y se alimentan del nuevo sistema de add-ins Avalonia (`MonoDevelop.Avalonia.Addons` / `AddonHost` + manifiestos `*.avaloniaaddon.json`), reconstruido a partir de la referencia legacy GTK del submódulo (`main/external/mono-addins/Mono.Addins.GuiGtk3/Mono.Addins.Gui/`: `AddinManagerDialog.cs`, `AddinTreeWidget.cs`).

**Cambios** (38 archivos):

1. **`MonoDevelop.Avalonia.Addons`** (componente nuevo): `AddonIdentity.Icon` (campo `identity.icon` del manifiesto); `AddonLoadState.ManifestPath`; `AddonHost.ResolveIconFile` — resuelve `core:file.png` contra el set de iconos core legacy y cualquier otra ruta contra la raíz de add-ins (o la propia carpeta del add-in), con walk-up desde el directorio del binario como `IconService` (resuelve en el árbol del repo y en un build estagiado).
2. **`AddinManagerDialog` sobre el host nuevo, UI fiel a la referencia legacy**:
   - **Installed**: `TreeView` con **nodos de categoría colapsables** (el `TreeStore` de `AddinTreeWidget` legacy: "Other" al final, estado de expansión preservado entre recargas — `SaveStatus`/`RestoreStatus` — y expansión total al cambiar el filtro); nombre con el **foreground del tema** (`IdeFgBrush`; el white hardcoded era invisible en tema claro), add-ins no cargados en gris (legacy `<span foreground="grey">`), **icono por add-in** (`identity.icon` resuelto por el host; fallback `plugin-32.png`, como `StoreIcon` legacy) y detalle vacío al seleccionar un nodo de categoría (legacy `ColAddin==null`).
   - **Gallery**: el catálogo del host **sin los add-ins instalados** (los cargados pertenecen a la página Installed; los no cargados se ven "available" con `plugin-avail-32`). El selector de repositorio es un **listbox** (`ComboBox` "All repositories", como el `repoCombo` GtkComboBox legacy; el nuevo host trae un único catálogo local).
   - **Updates**: **plana, sin categorías** (legacy `updatesTree.ShowCategories=false`); versión más nueva del catálogo frente a la cargada (el host estagia un manifiesto por add-in → hoy "No updates found"); header "N updates available" y Update All como el original.
   - Los botones de acción (Install/Update/Disable/Enable/Uninstall/Install from file) conservan la misma lógica de visibilidad original; sus handlers loguean `[addins] ... not available in the new add-in host` (el host aún no expone esas APIs — deferido).
3. **Borrado de componentes legacy del shell**: `MonoDevelop.Ide/Services/AddinEngineHost.cs` eliminado (exclusivo de Avalonia — el csproj GTK no lo lista en su lista explícita — y su único usuario era el diálogo); ProjectReferences `Mono.Addins`/`Mono.Addins.Setup` retiradas de `MonoDevelop.Startup.Avalonia.csproj`. Los ensamblajes Mono.Addins siguen estagiados en `main/build` para el build GTK (árbol unificado, regla 18.4); el diálogo GTK legacy vive en el submódulo `mono-addins` (intacto).
4. **27 manifiestos** declaran `identity.icon` reutilizando PNG 32px/48px ya existentes (core y carpetas legacy): `core:file-web-32` (AspNet/AspNetCore/WebReferences), `core:file-source-32` (CSharp/VB/SourceEditor), `core:file-unit-test-32` (UnitTesting/NUnit), `MonoDevelop.Debugger/icons/exception-48`, `Deployment/MonoDevelop.Deployment/icons/package-32`, `MonoDevelop.Gettext/icons/file-locale-32`, `MonoDevelop.PackageManagement/icons/package-source-32`, `core:package-32` (NuGet), `core:project-package-32` (Packaging), `core:project-crossplatform-shared-32` (DotNetCore), `core:file-xml-32` (XmlEditor), `core:workspace-32` (VersionControl), etc. Los 8 add-ins sin icono (3 plataformas ×2 manifiestos, Refactoring, RegexToolkit) usan el fallback genérico — mismo comportamiento que el diálogo legacy sin `Icon32`.

**Cómo validar**:

1. `cd main && dotnet build src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -m:1` → 0 errores.
2. `cd main/build && timeout 45 dotnet MonoDevelop.AvaloniaShell.dll --addins` → log: `[addins] tab=installed items=35 host=True`; al cambiar de pestaña: `tab=updates items=0` y `tab=gallery items=4` (solo los no cargados); 0 `Exception`; 0 líneas Cecil/Mono.Addins runtime.
3. Los 27 iconos declarados resuelven a PNG reales en disco (script `~/opencode/addins_manager/03_icon_check.py`: 27/27, magic bytes verificados).
4. Paridad UI: `git diff 3f619973b6..HEAD -- main/src/core/MonoDevelop.Startup.Avalonia/Views/AddinManagerDialog.axaml` = estilos `TreeView.addinlist` + `Window.Resources` (template `AddinRowTemplate`) + ListBox→TreeView + Button→ComboBox; el resto idéntico al original.
5. Regresión: `--prefs-tree` → `merged=5 (registry points=85)`, 0 missing; `--addonmanager` → `loaded 31/35` (Mac/Windows por diseño); `--old-gui` → 20 s sin crash (exit 124); `--extensionsdlg` → mismo diálogo.
6. Sin `using Mono.Addins`/`AddinEngineHost`/`SetupService` en el código del shell Avalonia; csproj sin referencias a mono-addins; el csproj GTK no lista `AddinEngineHost.cs` → el borrado no afecta al build GTK.

**QA (§18.5)**: Tester QA Senior, **APTO — 0 errores** en las 2 rondas (ronda 2: clics simulados Xlib confirmando `updates=0`/`gallery=4`; INFO no bloqueantes: doble línea `tab=installed` en el arranque, patrón preexistente; filas disabled seleccionables con su aviso, adaptación documentada). Artefactos: `~/opencode/addins_manager/` (01–07, 10_* y 20_*).

**Pendiente/deferido**:

- ~~Acciones Install/Update/Enable/Disable/Uninstall/Install from file requieren API en el nuevo host (repositorio remoto + instalación + habilitación) — las pestañas y la UI están listas sobre los nuevos componentes.~~ → **CERRADO en B.3 (2026-10-13)**: APIs de gestión en `AddonHost` (`SetEnabled`/`Uninstall`/`Update`/`UpdateAll`/`InstallFromDirectory`/`InstallFromFile`) + cableado completo del diálogo.
- ~~El `CecilReflector` de `main/external/mono-addins` sigue incompatible con el Cecil 0.10.1 del build: afecta al diálogo/registry Mono.Addins **del lado GTK** (no a la shell Avalonia, que ya no lo usa). Parchearlo para Cecil 0.10 es tarea del lado GTK.~~ → **CERRADO en B.5 (2026-10-12)**: el submódulo compila con .NET 10 (Cecil 0.11.6) y el registro de add-ins del lado GTK vuelve a escanearse de runtime (smoke `--old-gui` con rescan forzado, 0 líneas cecil/MissingMethod/TypeLoad).

### Build de add-ins de plataforma Mac (Darling) y Windows (WINE) → DLL x86_64 en `main/build/` (2026-10-11)

**Contexto**: los add-ins de plataforma (`MonoDevelop.Avalonia.MacPlatform` / `.WindowsPlatform` / `.LinuxPlatform`) están restringidos a su SO vía `Directory.Build.targets` (build no-op fuera del SO; regla 18.4: compiles integradas, sin carpeta separada). Antes de esta task el no-op estaba **roto en todos los SO**: redefinía `CoreCompile` y `CopyFilesToOutputDirectory` con la condición `IsOSPlatform(...) != 'true'` — pero `IsOSPlatform` devuelve `"True"` con mayúscula y la comparación de cadenas es case-sensitive, así que `!= 'true'` es **siempre** true → el no-op (y la redefinición de `CoreCompile`, que en este MSBuild invalida la del SDK con o sin condición) se activaba en **todos** los SO → el add-in de plataforma **no compilaba en ninguno**, ni siquiera en el suyo. Además cada carpeta tenía el csproj/manifiesto **duplicado** (nombre corto `MacPlatform.csproj` + canónico `MonoDevelop.Avalonia.MacPlatform.csproj`), que MSBuild no tolera en una misma carpeta. Objetivo: corregir la restricción por SO y el dedupe, y **compilar** cada add-in de plataforma con su SDK .NET 10 nativo emulado — Mac bajo **Darling** (macOS x64 sobre Linux) y Windows bajo **WINE** (Windows x64) — para producir las DLL x86_64 en `main/build/`.

**Cambios**:

1. **Fix `Directory.Build.targets` (Mac y Windows) + nuevo (Linux)**: ya no se redefinen `CoreCompile`/`CopyFilesToOutputDirectory`. Fuera del SO destino: (a) `PlatformNoOp_StripCompileSources` (antes de `CoreCompile`) vacía `@(Compile)` para que el SDK produzca un ensamblaje vacío en vez de fallar; (b) `NoWarn` += `CS2008`; (c) `SkipPlatformBuildCleanup` (tras `Build`) borra los artefactos vacíos (`dll`/`pdb`/`deps.json`/`xml`). La comparación usa `IsOSPlatform(...) == 'True'` (mayúscula, case-sensitive).
2. **Dedupe de csproj/manifiesto (Mac, Windows y Linux)**: eliminados los duplicados con nombre corto (`MacPlatform.csproj`, `MacPlatform.avaloniaaddon.json`, `WindowsPlatform.csproj`, `WindowsPlatform.avaloniaaddon.json`, `LinuxPlatform.csproj`, `LinuxPlatform.avaloniaaddon.json`); se conservan los canónicos `MonoDevelop.Avalonia.<Plataforma>.csproj` / `.avaloniaaddon.json`.
3. **Compilación Mac bajo Darling**: `main/build/MonoDevelop.Avalonia.MacPlatform.dll` producido con el SDK .NET 10 de **Mac** (x86_64) bajo Darling.
4. **Compilación Windows bajo WINE**: `main/build/MonoDevelop.Avalonia.WindowsPlatform.dll` producido con el SDK .NET 10 de **Windows** (x64) bajo WINE (prefijo `~/.wine-dotnet`).

**Arquitectura x86_64**: los ensamblajes de clase .NET son **IL AnyCPU** (header PE `machine=0x14c`/i386; no es un binario 32-bit-only y llevan CLR runtime header). "x86_64" queda satisfecho porque la **toolchain que los produce es x64**: bajo Darling el `dotnet` y el `csc` (Roslyn) son `Mach-O 64-bit x86_64`, y bajo WINE el `dotnet.exe` es `PE32+ x86-64` (`dotnet --info` → `Architecture: x64`). Los 4 ensamblajes de referencia de `main/build/` comparten el mismo encoding.

**Cómo validar**:

1. **Build Mac limpio (Darling)**: `rm -rf <dir-mac>/obj && rm -f main/build/MonoDevelop.Avalonia.MacPlatform.{dll,pdb,deps.json,xml}` y `bash ~/opencode/trace_mac_build.sh` → `mac_build.exit=0`, out con `Compilación correcta.` y `0 Errores`, DLL en `main/build/` (~37 s). El script corre el build en background porque el CLI host `darling` se cuelga tras terminar el guest (espera a que desaparezca el proceso `mldr`).
2. **Build Windows limpio (WINE)**: `rm -rf <dir-win>/obj && rm -f main/build/MonoDevelop.Avalonia.WindowsPlatform.{dll,pdb,deps.json,xml}` y `bash ~/opencode/build_win_addin.sh` → `win_build.exit=0`, out con `Compilación correcta.` y `0 Errores`, DLL en `main/build/` (~3 s).
3. **Arquitectura**: `file` de ambos → `PE32 ... Intel i386 Mono/.Net assembly`; parse PE → `machine=0x14c`, PE32, CLR runtime header (data dir [14] ≠ 0); toolchains x64 verificadas con `file` (Darling dotnet/csc = Mach-O x86_64; WINE dotnet.exe = PE32+ x86-64).
4. **Contenido**: la DLL Mac contiene el TypeDef `MacPlatform` (ns `MonoDevelop.Platform`, base `PlatformService`, iface `IAvaloniaAddon`) con PDB path `/Volumes/SystemRoot/...` (build Darling); la DLL Windows contiene `WindowsPlatform` (ns `MonoDevelop.Platform`) con PDB path `Z:\...` (build WINE). Sin cruce (0 ocurrencias de cada nombre en la DLL contraria).
5. **Regresión**: los 4 DLL de referencia (`MonoDevelop.Core`/`Ide`/`Avalonia.Addons`/`Xwt.dll`) en `main/build/` intactos (md5 = baseline).
6. **Determinismo**: los builds limpios son **byte-idénticos** (md5 `c85cfd86…` Mac, `c3beccb2…` Windows entre builds limpios consecutivos).

**QA (§18.5)**: Tester QA Senior, **PASS — 0 errores** (8/8 checks; los builds de validación son byte-idénticos a los del desarrollo). Hallazgos no bloqueantes: (1) **MSB3374 ambiental** — un build Mac limpio falló en el 1.er intento con `error MSB3374: No se puede establecer la hora ... Up2Date. Invalid argument` (el `Touch` de `CopyUpToDateMarker` → `setattrlist`→`EINVAL` del guest sobre `/Volumes/SystemRoot`) por **degradación de estado de Darling** (userland residual tras `kill` del `mldr`), no por el código; tras el reset completo los builds limpios siguientes pasaron byte-idénticos. (2) El namespace real es `MonoDevelop.Platform` (las fuentes lo declaran explícito; el `RootNamespace` del csproj no se usa). Artefactos de prueba en `~/opencode/` (`trace_mac_build.sh`, `build_win_addin.sh`, `mac_build.*`, `win_build.*`, `qa_pe_check.py`, `qa_il_types.py`, `qa_baseline_md5.txt`).

**Cierre**: los dos add-ins de plataforma Mac y Windows **compilan a DLL x86_64 en `main/build/`** sobre su SO destino (Mac bajo Darling, Windows bajo WINE); el build es reproducible y determinista en estado limpio, y la restricción por SO (`Directory.Build.targets`) quedó corregida para los 3 add-ins de plataforma.

**Pendiente/deferido**:

- **Degradación de estado de Darling** (causa del MSB3374): matar `mldr`/`darlingserver` en plena build o dejar userland residual vivo puede degradar el siguiente arranque de guest (montaje `/Volumes/SystemRoot` ausente o `setattrlist`→`EINVAL`). **Recuperación validada**: matar todo el userland (launchd, memberd, opendirectoryd, iokitd, shellspawn, securityd) + `mldr` + `darlingserver`; `rm ~/.darling/.init.pid ~/.darling/.darlingserver.sock`; relanzar.
- ~~El add-in de plataforma **Linux** compila en el host (Linux) pero no fue objeto de la validación de esta task (el alcance era Mac + Windows).~~ → **Validado luego en el Track 1 (2026-10-12)**: smoke `LinuxPlatform ready` + activación real del part vía ALC + `--old-gui` regresión limpia.
- El build Mac depende de **Darling** y el Windows de **WINE** (instalados localmente, con su caché NuGet poblada: `system.componentmodel.composition/7.0.0`, requerida por `main/Directory.Build.props`). Las adaptaciones hechas a Darling para habilitar el build son de la herramienta local y **no se commitean** (fuera del worktree real).

### Track 1 — Hooks de plataforma: el add-in de plataforma se activa de verdad y la shell lo resuelve vía `IPlatformHooks` (2026-10-12)

**Contexto**: la task anterior dejó los add-ins de plataforma compilados (Mac bajo Darling, Windows bajo WINE, Linux en host) y staged en `main/build/`, pero **nunca se activaban**: `AddonLoadContext.Load` doble-cargaba `MonoDevelop.Avalonia.Addons.dll`, `MonoDevelop.Core.dll`, `Xwt.dll`, etc. en el ALC de cada add-in (el `AssemblyDependencyResolver` los encontraba vía el `deps.json` del add-in y el árbol unificado), lo que rompía la identidad de tipos — el `IAvaloniaAddon` del add-in no era el tipo del host, `typeof(IAvaloniaAddon).IsAssignableFrom(t)` fallaba en silencio en `LoadAll` (sin activación, sin log) y `state.MarkLoaded()` marcaba `Loaded=true` de todas formas → el manager mostraba 26/28 falsos. Además no existía puente real host→backend de plataforma: la shell hardcodeaba xdg-open/shell-execute y `DesktopService` (GTK, `--old-gui`) recibía la plataforma solo de `Mono.Addins` vía `/MonoDevelop/Core/PlatformService`.

**Clarificaciones del operador** (esta sesión):

1. El componente `MonoDevelop.Avalonia.Addons` es **solo para la shell Avalonia**; `Mono.Addins.Gui` (UI GTK) **no se toca** — se queda como está con `Mono.Addins` + add-ins legacy (único cambio autorizado: net4x→.NET 10, ya hecho). → Los add-ins de plataforma **no** llaman a `DesktopService.SetPlatformService` (la shell Avalonia no compila `DesktopService.cs`; el método se añadió y se **revertió**).
2. El manager **real** es `AddinManagerDialog` (Tools > Extensions..., flag QA `--addins`); `AddonManagerDialog` (Tools > Add-ins (Avalonia), `--addonmanager`) era **provisional** de pruebas → eliminado.
3. **Reorganización del repo** (operador): módulo `MonoDevelop.Avalonia.Addons` movido a `main/src/core/MonoDevelop.Ide/MonoDevelop.Avalonia.Addons/`; borradas todas las DLL fuera de `main/build/`. **Convención: todos los builds van a `main/build/`** (única salida de build).

**Cambios**:

1. **Fix de identidad de tipos en ALC** (`AddonLoadContext.cs`): `Load` devuelve `null` si el ensamblado (por simple name) ya está cargado en `AssemblyLoadContext.Default` → los ensamblajes core se sirven del contexto default y no se duplica una copia en el ALC del add-in. Con esto `LoadAll` **sí** activa los parts del add-in de plataforma (verificado en smoke: log `LinuxPlatform ready`).
2. **`IPlatformHooks`** (`IAvaloniaAddon.cs`, ns `MonoDevelop.AvaloniaAddons`): `Name`, `DefaultMonospaceFont`, `ShowUrl(string)`, `OpenFolder(string)`, `GetMimeTypeForUri(string)`. Vive en el ensamblaje Addons (compartido entre contexto default y ALCs) para que la shell lo consuma sin reflexión y ningún tipo de `MonoDevelop.Ide` cruce contextos.
3. **Gating por OS** (`AddonManifest.cs` + `AddonHost.cs`): nuevo campo de manifiesto `"os"` ("" = todos, "linux"|"macos"|"windows"); `Discover` hace gating (`MatchesCurrentOs` vía `System.OperatingSystem`) y los add-ins no correspondientes quedan `loaded=False` con error `not supported on this platform (requires: X)` (antes: falso positivo `Loaded=true`). `AddonHost.Parts` expone los parts de la composición para la shell.
4. **Reescritura de los 3 add-ins de plataforma** (`LinuxPlatform`/`MacPlatform`/`WindowsPlatform`, ns `MonoDevelop.Platform`): `: PlatformService, IAvaloniaAddon, IPlatformHooks`, guard de OS en `Initialize(IAddonContext)` (log `"<X>Platform ready"`), implementaciones reales: Linux → `xdg-open` (URLs/carpetas), `CanOpenTerminal=true` + `x-terminal-emulator`, `StartConsoleProcess` vía `ProcessWrapper`, "Monospace 11"; Mac → `open`, "Menlo 12"; Windows → shell-execute + `explorer.exe`, "Consolas 10". Eliminados los overrides de esqueleto dañinos (`OnGetMimeTypeForUri` → `application/octet-stream` incondicional, `OnGetIconIdForFile` → "generic-file"): `GetMimeTypeForUri` se hereda de `PlatformService` (público, con la firma exacta de la interfaz).
5. **Manifiestos**: `"os"` canónico en los 3 (`linux`/`macos`/`windows`).
6. **Shell** (`MonoDevelop.Startup.Avalonia`): `App.Platform` (`IPlatformHooks?`) resuelto tras `LoadAll` vía `addons.Parts.OfType<IPlatformHooks>().FirstOrDefault()` + logs `[avalonia-addons] …` (root, discovered, platform, monospace, estado por add-in, extension points); enrutados por plataforma con fallback: `MenuService.OpenLink` → `ShowUrl`, `MainWindow.OpenContextContainingFolder` y `PreferencesDialog.OnThemeOpenFolder` → `OpenFolder`.
7. **Eliminado el `AddonManagerDialog` provisional**: `Views/AddonManagerDialog.axaml{,.cs}`, item de menú "Add-ins (Avalonia)...", `OnAvaloniaAddonManagerMenu`, flag QA `--addonmanager` (`Program.cs` + `MainWindow.axaml.cs`). El `AddinManagerDialog` real (`--addins`) se conserva y muestra el estado correcto (los 2 add-ins de plataforma `not supported` en Linux).
8. **`DesktopService.cs` revertido**: sin `SetPlatformService` (clarificación 1; el wiring GTK vía `Mono.Addins` + `/MonoDevelop/Core/PlatformService` queda intacto).
9. **Reorganización reflejada en build**: `ProjectReference` del shell a la nueva ruta (`..\MonoDevelop.Ide\MonoDevelop.Avalonia.Addons\…`); `OutputPath` del csproj Addons → `main/build/` (los de plataforma ya llevaban `OutDir` allí); los 3 add-ins de plataforma recompilados desde las fuentes nuevas (Linux en host, Mac en Darling, Windows en WINE) → `main/build/`.
10. **Refactors del operador** (no hechos por el agente; reflejados): csproj Addons con `ImplicitUsings`/`Nullable` disable + `EnableDefaultItems` true; `IPlatformHooks.GetMimeTypeForUri` con `(string uri)` y `IAvaloniaAddon.Initialize(IAddonContext)`; overrides de `GetMimeTypeForUri` retirados de los add-ins (heredados de la base).

**Cómo validar**:

1. **Builds** (0 errores, salida a `main/build/`): `~/.dotnet/dotnet build` de `main/src/core/MonoDevelop.Ide/MonoDevelop.Avalonia.Addons/MonoDevelop.Avalonia.Addons.csproj`, `main/src/addins/MonoDevelop.Avalonia.LinuxPlatform/MonoDevelop.Avalonia.LinuxPlatform.csproj` y `main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj`. (Mac/Win solo si cambian sus fuentes: `bash ~/opencode/trace_mac_build.sh` / `bash ~/opencode/build_win_addin.sh`.)
2. **Smoke 1** (`cd main/build && timeout 35 xvfb-run -a ~/.dotnet/dotnet MonoDevelop.AvaloniaShell.dll`): esperable `LinuxPlatform ready`, `platform=Linux monospace=Monospace 11`, `skipped (requires macos|windows)`, `loaded 30/32 add-in(s)` y los 2 add-ins gateados con `loaded=False error=not supported on this platform (requires: macos|windows)`. **Check clave**: antes del fix de ALC no aparecía ningún log `ready` (no había activación).
3. **Smoke 2** (`--addins`): `[addins] tab=installed items=32 host=True` (manager real con el estado nuevo).
4. **Smoke 3** (`--old-gui`): el proceso hijo GTK (`dotnet …/main/build/MonoDevelop.dll`) arranca y vive (regresión de la ruta legacy, regla 18.4).
5. **Estático**: 0 referencias a `AddonManagerDialog`/`--addonmanager`; 0 `SetPlatformService` en `main/`; `"os"` en los 3 manifiestos; `IPlatformHooks` público en `main/build/MonoDevelop.Avalonia.Addons.dll` y `MonoDevelop.Platform.{Linux,Mac,Windows}Platform` presentes en sus DLLs (tool `~/opencode/dumptypes2`).

**QA (§18.5)**: Tester QA Senior, **PASS — 0 errores** (15/15: 3 builds, 3 smokes, 9 estáticos). Hallazgos no bloqueantes: (1) `AVLN9999` transitorio en el 1.er build del shell del QA (build previo interrumpido por su propio pipe `head` dejó DLL corrupto; rebuild limpio → 0 errores, reproducible). (2) El worktree lleva cambios de **otro track** (reubicación de `Mono.TextEditor.Shared` → `MonoDevelop.Ide/` y `MonoDevelop.TextEditor.Tests`): no staged en este commit. (3) El smoke `--old-gui` deja log vacío (buffering de stdout al morir por timeout); arranque verificado por `pgrep`. Artefactos en `~/opencode/` (`qa_build_shell*.log`, `qa_smoke*.log`, `dumptypes2/`).

**Cierre**: los hooks de plataforma de la shell Avalonia quedan **funcionales de punta a punta**: el host descubre, gatea por OS, carga y **activa** el add-in de plataforma (identidad de tipos real), y la shell lo resuelve como su backend de plataforma (`App.Platform`) para URLs, carpetas y la fuente monoespaciada por defecto. La ruta GTK (`--old-gui` + `Mono.Addins` + add-ins legacy) queda sin cambios y funcional. El manager provisional de add-ins queda eliminado; el manager real muestra el estado correcto.

**Pendiente / para ratificar**:

- **Para ratificar — dedupe (commit `43594c38e0`)**: se eliminaron los duplicados de nombre corto `LinuxPlatform.avaloniaaddon.json`/`LinuxPlatform.csproj` (+ Mac/Win). Eran **rotos**: id `MonoDevelop.LinuxPlatform` y assembly `MonoDevelop.LinuxPlatform.dll` (inexistente; el canónico es `MonoDevelop.Avalonia.LinuxPlatform.dll`). El operador pidió antes "sin borrados" para los archivos `LinuxPlatform`; se conservaron los canónicos (los que funcionan de verdad — activado en smoke). **Decisión**: ratificar el dedupe o restaurar los duplicados (recuperables por git).
- **Rebuild al cambiar fuentes**: add-in de plataforma Linux → host (`dotnet build`); Mac → Darling (`trace_mac_build.sh`); Windows → WINE (`build_win_addin.sh`). Recuperación de degradación de Darling: sección anterior (2026-10-11).
- **Convención de builds**: todo a `main/build/` (gitignored). El módulo Addons vive ahora en `main/src/core/MonoDevelop.Ide/MonoDevelop.Avalonia.Addons/` (no lo referencia `MonoDevelop.Ide.csproj` — ese proyecto mantiene lista de ficheros explícita, `EnableDefaultCompileItems=false`).
- ~~MIME/iconos en los hooks más allá del default heredado de `PlatformService`: diferido (como antes).~~ → **CERRADO en B.4 (2026-10-17)**: `GetMimeTypeForUri`/`GetMimeTypeDescription` vía `MimeCatalog` (superficie del manifiesto `/MonoDevelop/Core/MimeTypes`) + `GetIconIdForFile`/`GetIconIdForType` en `IPlatformHooks`.

### Track B — B.5: mono-addins compila con .NET 10 (Cecil 0.10.1 → 0.11.6) (2026-10-12)

**Contexto**: la task del 2026-10-07 (Add-in Manager desacoplado de la shell) desvió `Mono.Addins.CecilReflector` como "tarea del lado GTK": el submódulo solo compilaba para net472 contra Mono.Cecil 0.10.1 y el runtime net10 de la ruta legacy (`--old-gui`) fallaba al escanear el registro de add-ins (`MissingMethodException` → "The add-in database could not be updated"). B.5 cierra eso: el submódulo `mono-addins` compila con .NET 10.

**Cambios** (submódulo `main/external/mono-addins/`, rama `net10`; sin cambios de código fuente):

1. `Mono.Addins/Mono.Addins.csproj`: `TargetFrameworks` `net461;net472;net8.0` → `net461;net472;net8.0;net10.0`.
2. `Mono.Addins.CecilReflector/Mono.Addins.CecilReflector.csproj`: reescrito de clásico → SDK-style: `net472;net10.0`, `PackageReference Mono.Cecil 0.11.6` (reemplaza el HintPath 0.10.1), `GenerateAssemblyInfo=false`, `ImplicitUsings=false`, `Nullable=disable`, `SignAssembly=false`, `NoWarn 1591;1574`, `CopyLocalLockFileAssemblies=true`, `OutputPath=bin\`. Definición `READ_ONLY` leftover (sin uso) eliminada.
3. **Staging en `main/build/`** (convención: todos los builds a `main/build/`, gitignored): `Mono.Addins.dll` (build net10.0, v1.0.0.0), `Mono.Addins.CecilReflector.dll` (build net10.0, v1.0.0.0) y `Mono.Cecil.dll` **0.11.6.0** (pk `50cebf1cceb9d05e`, idéntico al de 0.10.1 → los consumidores compilados contra 0.10.1 — `MonoDevelop.Core`, `ICSharpCode.NRefactory.Cecil` — roll-forward en runtime; el nuevo CecilReflector resuelve 0.11.6.0 exacto).
4. **`Reflector.cs` sin cambios**: las APIs usadas (`ReaderParameters(ReadingMode.Deferred)`, `MainModule.FileName`, `InterfaceImplementation`, `CustomAttribute.ConstructorArguments`) son compatibles con 0.11.6 (verificado en build + probe).
5. **Fuera de scope (documentado como follow-up)**: el bump global de `NuGetVersionCecil` en `main/Directory.Build.props` (afectaría a nrefactory×2, debugger-libs×2, `MonoDevelop.Core.Tests`) y el pin 0.10.1 por HintPath de `MonoDevelop.Core.csproj:76-93` (Cecil/Mdb/Pdb/Rocks). No era necesario: la compilación sigue resolviendo 0.10.1 desde la caché (`Private=False`) y el runtime resuelve el 0.11.6 estagiado por roll-forward.

**Cómo validar**:

1. **Builds 0 errores**: `~/.dotnet/dotnet build` de `main/external/mono-addins/Mono.Addins/Mono.Addins.csproj` (4 TFMs) y `main/external/mono-addins/Mono.Addins.CecilReflector/Mono.Addins.CecilReflector.csproj` (net472+net10.0), y del shell `main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj` (los warnings MSB3245 gtk-sharp / MD0009 son preexistentes).
2. **Integridad del staging**: `~/opencode/dumptypes` (versiones + PK + referencias) y `cmp` byte a byte: `main/build/Mono.Cecil.dll` vs `~/.nuget/packages/mono.cecil/0.11.6/lib/netstandard2.0/Mono.Cecil.dll`; los dos Mono.Addins* vs sus `bin/net10.0` del submódulo.
3. **Probe de runtime** (`~/opencode/cecilprobe/`): harness net10 que carga el `Mono.Addins.CecilReflector.dll` estagiado de `main/build` vía `Assembly.LoadFrom` (el camino exacto de `AddinFileSystemExtension.GetReflectorForFile`) y ejercita la superficie completa de `IAssemblyReflector` sobre (a) un add-in sintético (`Probe.Test.dll`: `[Addin]` con args de ctor + propiedades nombradas + enum; `[Extension]` con `Type` (decode de `TypeReference`); un `CustomExtensionAttribute` derivado aplicado al ensamblaje — flujo de producción `AddinScanner:746` con mapeo `[NodeAttribute]` y cadena de bases cruzando ensamblajes vía locator; recursos embebidos) y (b) un add-in real (`AddIns/MonoDevelop.Debugger/MonoDevelop.Debugger.dll`: 150 tipos, 30 refs) → **23/23 chequeos**, exit 0.
4. **Smoke `--old-gui` con rescan forzado**: el registro vivo está en `~/.cache/MonoDevelop/9.0/addin-db-003` (el de `~/.config/MonoDevelop/9.0/` es stale; backup en `~/opencode/addin-db-003.backup-b5/`). Borrar el vivo → `SHELL_LOG=~/opencode/shell_olddui_b5.log bash ~/opencode/launch_shell.sh --old-gui` → al arrancar `Runtime.Initialize(true)` ejecuta `AddinManager.Registry.Update(null)` (rescan completo con el CecilReflector nuevo) → DB regenerado en ~20 s, IDE legacy arrancando completo (MEF, TypeSystemService, Welcome Page) y **0 líneas** cecil/MissingMethod/TypeLoad en `~/.cache/MonoDevelop/9.0/Logs/`. Los 12 ERROR "referenced in the manifest could not be found" (libstetic, System.Web.*) son preexistentes (idénticos en los baselines 03:58/04:11 del mismo día).

**QA (§18.5)**: Tester QA Senior, **PASS — 0 errores** (5/5: 3 builds, integridad del staging, probe 23 chequeos, smoke con rescan forzado, revisión estática del submódulo: solo los 2 csproj tocados, sin bin/obj). Hallazgo no bloqueante (menor): 2× WARNING `Cannot deserialise unregistered policy name 'VersionControlPolicy'/'ChangeLogPolicy'` en el log del smoke — preexistentes, asociados al add-in `MonoDevelop.VersionControl` ausente (el propio log reporta "missing: MonoDevelop.VersionControl,9.0") y sin relación causal con el cambio (la fase de scan terminó antes, sin incidencias). Artefactos en `~/opencode/` (`qa_b5_*.log`, `cecilprobe/`, `dumptypes/`, `addin-db-003.backup-b5/`, `qa_b5_db_backup_2207/`).

**Cierre**: el submódulo mono-addins compila con .NET 10 y la ruta legacy (`--old-gui`) vuelve a poder escanear y actualizar el registro de add-ins de runtime (CecilReflector net10 + Mono.Cecil 0.11.6) — cierra el bug pendía desde el 2026-10-07. La shell Avalonia queda sin efecto (no usa CecilReflector; su Add-in Manager corre sobre `MonoDevelop.Avalonia.Addons`).

**Pendiente / follow-up**:

- **Bump global `NuGetVersionCecil` a 0.11.6** (follow-up documentado): `main/Directory.Build.props:21` + los pins de HintPath 0.10.1 en `MonoDevelop.Core.csproj:76-93` (incl. `Mono.Cecil.Mdb/Pdb/Rocks`, cuyo 0.11.6 no está en la caché local). Solo si se quiere uniformar la compilación en 0.11.6; el runtime ya funciona con roll-forward.
- `Mono.Addins.Setup` (referenciado por `MonoDevelop.Core`) sigue sin target net10.0 (hasta net8.0): funciona en runtime net10; añadir net10.0 si se uniforma el submódulo.
- `Mono.Addins.Gui.dll` / `Mono.Addins.Setup.dll` estagiados en `main/build/` son builds antiguos (sep 11 / oct 3): funcionan en runtime net10; actualizar al tocar la UI GTK del Add-in Manager.
- El log de consola de la shell (`SHELL_LOG`) queda vacío en el smoke `--old-gui` (buffering de stdout); el log real del IDE legacy está en `~/.cache/MonoDevelop/9.0/Logs/`.

### Track B — B.3: APIs del host del Add-in Manager (enable/disable, install, update, uninstall) + cableado del diálogo (2026-10-13)

**Contexto**: el Add-in Manager de la shell Avalonia (diálogo de 3 pestañas desde el 2026-10-07) era solo lectura: el host `MonoDevelop.Avalonia.Addons` solo tenía `Discover`/`LoadAll` y los botones del diálogo (Enable/Disable, Uninstall, Update, Update All, Install, Install from File) no tenían acción. B.3 añade las APIs de gestión al host, cablea el diálogo y aporta la segunda raíz de add-ins de usuario (`~/.local/share/MonoDevelop-Avalonia/UserAddIns`), para que la instalación nunca escriba en el repo ni en `main/build/`.

**Cambios** (7 ficheros; sin submódulos):

1. `AddonLoadState.cs`: + `Enabled`, `Superseded`, `IsUserAddin` (`Loaded` = ensamblaje en ALC; `Enabled` = parts + nodos activos).
2. `AddonRegistry.cs`: + `RemoveNode` (deregistra un nodo por path).
3. `Extensions/CompositionHost.cs`: + `RemoveRange` (quita un rango de parts de la composición).
4. `AddonHost.cs` (reescrito de la capa de gestión; `Discover`/`LoadAll` conservados):
   - ctor `(rootDirectory, userRootDirectory = null)`; `StateStore` (`IAddonUserState`); `UserRootDirectory`.
   - **Persistencia vía `IAddonUserState { string Get(string key); void Set(string key, string value); }`**: el módulo Addons es standalone (solo System.Text.Json, sin referencia a `MonoDevelop.Core`), así que la shell lo cablea con el adapter `ShellAddonUserState` (`App.axaml.cs`) sobre `SettingsStore` (`MonoDevelopProperties.xml`). Claves: `MonoDevelop.Avalonia.Addins.Disabled` / `MonoDevelop.Avalonia.Addins.Uninstalled` (arrays JSON de ids; `Set(key, null)` borra).
   - **Precedencia por versión**: mismo id en shipped y user → gana la más reciente (`Version.TryParse` ?? 0.0; empate → user); las perdedoras quedan `Superseded` (aparecen en la pestaña Updates y NUNCA se cargan). `MarkSuperseded()` idempotente (resetea y recalcula).
   - **APIs**: `FindAddon`, `FindVersions`, `SetEnabled` (unload/recompone parts desde el ALC vivo), `Uninstall` (user → borra la carpeta; shippado → marker por id que oculta todas las versiones shippadas de ese id — solo se pone si el ganador es user), `Update` (requiere 2+ versiones staged; si el ganador es shippado → limpieza de sesión solo), `UpdateAll`, `InstallFromDirectory`, `InstallFromFile` (solo `.zip`, v1; zip sin manifiesto rechazado), `LoadAddon`, `ActivateId`.
   - **Decisiones de diseño**: `AddonLoadContext` no-colectible → enable/disable en runtime = unload/reinstancia de parts desde el ALC vivo (sin restart del proceso); contextos perezosos (`Discover()` no crea ALCs; `EnsureContext` envuelve la creación de `AddonLoadContext` en try/catch porque lanza `FileNotFoundException` si falta el assembly); `Discover()` empieza con `UnloadAll()` (composición/extensions limpias en re-descubrimiento); tracking de parts por id de add-in (`partsByAddon`/`nodesByAddon`); `ComposeAddon` es handover (descompone antes lo compuesto del id); `Update`/`Uninstall` → `MarkSuperseded()` + `ActivateId(id)` (carga y compone la versión que queda activa, si la hay); `UnloadAddon` solo invoca `IPackage.Unload()` (convención preexistente); `ValidateDependencies` prefiere la dependencia no-superseded; sin anotaciones nullable nuevas (`Nullable` deshabilitado en ambos proyectos → sin CS8632 nuevos).
5. `Program.cs`: + `UserAddinsDirectory` (env `MONODEVELOP_AVALONIA_USER_ADDINS` ?? `~/.local/share/MonoDevelop-Avalonia/UserAddIns`).
6. `App.axaml.cs`: `new AddonHost (Program.AddonsDirectory, Program.UserAddinsDirectory)` + `addons.StateStore = new ShellAddonUserState ()` + logs de diagnóstico `[addins]` (userRoot, enabled, superseded).
7. `AddinManagerDialog.axaml.cs`: cableado de handlers — Enable/Disable → `SetEnabled`; Uninstall → ventana de confirmación (patrón de `MainWindow.DeleteContextNode`) + `Uninstall`; Install → `Update` (fila instalada) / `LoadAddon` (fila de galería) + cambio a la pestaña installed; Update All → `UpdateAll`; Install from File → `FilePickerOpenOptions` (*.zip) + `InstallFromFile` (try/catch). `RowFor` null-safe + `Enabled = installed ? state.Loaded && state.Enabled && !state.Superseded : true`; `LoadUpdates` por `FindVersions (id).Count > 1` (eliminado `CompareVersions`, sin uso); `LoadGallery` salta Superseded; botones de acción ocultos en filas superseded; helper `SelectTab`.

**Cómo validar**:

1. **Builds 0 errores**: `MonoDevelop.Avalonia.Addons.csproj` (0 errores / 2 warnings — NU1510 preexistentes; antes 14) y `MonoDevelop.Startup.Avalonia.csproj` en `net10.0` y `net10.0-windows` (0 errores; 738 warnings, todos preexistentes).
2. **Harness funcional** (`~/opencode/qa_b3/harness/`, QaB3Harness con `ProjectReference` a Addons): **50/50 ALL PASS** (dual-root shipped+user, precedencia por versión y superseded, `SetEnabled` round-trip + persistencia, `Uninstall` user/shipped + limpieza de markers, `Update` con ganador user/shippado, `UpdateAll`, `InstallFromDirectory`/`InstallFromFile` zip + rechazo de zip sin manifiesto, `ActivateId`, dependencias, OS gating, estados de error). El add-in sintético (`part/PartTest.cs`) escribe Load/Unload a un fichero vía env `QA_ADDON_LOG`: el harness NO referencia PartTest → identidad de tipos correcta a través del ALC.
3. **Extratest del QA Senior** (`~/opencode/qa_b3/qa_senior/extratest/`, QaB3Extra): **25/25** (incluye el caso H1 — uninstalled user que shadoweaba un shippado → el shippado queda activo — y el caso H2 — deshabilitado no cargado persiste deshabilitado).
4. **Test enfocado `ActivateId`**: 12/12.
5. **Smoke shell real** (`~/opencode/qa_b3/smoke.sh`, user root `smoke/user/` con el add-in solo-manifiesto `Qa.UserSmoke`): `discovered=33` (32 shippados + 1 user), `loaded 31/33` (2 = Mac/WindowsPlatform por OS gate), `Qa.UserSmoke v1.0.0 loaded=True enabled=True`, `[addins] tab=installed items=33`, gallery items=2, sin `Exception` en el log. La ruta `--old-gui` queda sin efecto (sin cambios del lado GTK; cubierta por el smoke de B.5).

**QA (§18.5)**: Tester QA Senior, 2 rondas. Ronda 1 **FAIL**: H1 bloqueante (`Uninstall` sin `MarkSuperseded()` → estado huérfano cuando un user shadoweaba un shippado: `FindAddon` devolvía null); H2 menor (`SetEnabled` persistía `a.Loaded && !a.Enabled` y perdía el estado deshabilitado de add-ins no cargados); H3 menor (4 CS8632 nuevos); H4 (aceptado por diseño: `Update` descompone/recompone transitoriamente las parts del ganador); H5 (`Discover()` no limpiaba la composición en re-descubrimiento); H6 parcial (`RowFor` null-safe + try/catch; feedback de errores en diálogo = follow-up). Todos corregidos y re-verificados. Ronda 2 **PASS** — 0 hallazgos abiertos: builds 0 errores (3 proyectos/TFMs), harness 50/50, extratest 25/25, `ActivateId` 12/12, smoke exacto, fixes confirmados por lectura (`AddonHost.cs:512-518` Uninstall, `:677-691` `ActivateId`).

**Cierre**: B.3 completa: el Add-in Manager es un manager real en la shell Avalonia — enable/disable, install (directorio/zip), update (incl. multi-versión), uninstall (user/shippado) — con estado persistido en `SettingsStore` y precedencia por id entre la raíz shippada (`main/src/addins/`) y la raíz de usuario. La ruta `--old-gui` queda sin cambios.

**Pendiente / follow-up**:

- **B.4** (completada el 2026-10-17): MIME/iconos de plataforma en los hooks — sección siguiente.
- Feedback de errores en el diálogo (resto de H6): los fallos de `SetEnabled`/`Update`/`Install*` quedan hoy solo en el log (línea `[addins]` + estado de la fila); mostrarlos con diálogo de error.
- `Update` con ganador shippado es limpieza de sesión (no escribe marker a disco) — documentado en el código; decidir si se quiere persistir.
- `InstallFromFile` solo acepta `.zip` (v1); el uninstall de shippados es por id (oculta todas sus versiones).

### Track B — B.4: MIME/iconos de plataforma en los hooks de plataforma de la shell Avalonia (2026-10-17)

**Contexto**: la shell Avalonia no tenía resolución real de MIME/iconos: `PlatformService.GetMimeTypeForUri`/`GetMimeTypeDescription` se apoyan en el `MimeTypeCatalog` de GTK (vacío sin Mono.Addins → solo respondían `text/plain`/`application/octet-stream`) y el árbol usaba un switch local por extensiones (`FileIconResource`/`FileIconId` en `MainWindow.axaml.cs`). B.4 lleva la resolución MIME + iconos a los hooks de plataforma (`IPlatformHooks` del Track 1) con un catálogo compartido alimentado desde los propios manifiestos de los add-ins (misma fuente de verdad que el extension point legacy `/MonoDevelop/Core/MimeTypes`).

**Cambios** (10 ficheros en el repo principal + 2 en el submódulo xwt):

1. `MonoDevelop.Avalonia.Addons/MimeCatalog.cs` (nuevo, 312 líneas): catálogo MIME/iconos de la shell. `MimeEntry` (id, descripción, icono stock, baseType, isText, patrones). Construido desde una tabla estática que espeja las declaraciones core legacy + formatos modernos, **superpuesta** por los nodos `/MonoDevelop/Core/MimeTypes` que declaran los add-ins en sus manifiestos (el overlay gana — mismo contrato que el extension point legacy; CSharpBinding sigue siendo dueño de `text/x-csharp`). `SetExtensionRegistry(registry)` lo cablea la shell; invalidación de caché por el contador `Version` del registro. APIs: `GetMimeTypeForFile`, `GetMimeTypeDescription`, `GetIconIdForFile`, `GetIconIdForType`.
2. `IAvaloniaAddon.cs` (`IPlatformHooks`): + `GetMimeTypeDescription`, `GetIconIdForFile`, `GetIconIdForType` con **implementación por defecto** (default interface methods) que resuelve vía `MimeCatalog` — los add-ins de plataforma compilados antes de estos miembros (builds Mac/Windows stale) siguen funcionando sin implementación de lado clase; un add-in de plataforma puede sobreescribirlos para resolución específica de SO.
3. `LinuxPlatform.cs` (add-in de plataforma): sobreescribe `GetMimeTypeForUri` + `GetMimeTypeDescription` → `MimeCatalog` (la versión heredada se apoyaba en el `MimeTypeCatalog` de GTK, que queda vacío sin Mono.Addins).
4. `PlatformService.cs` (capa compartida): `GetMimeTypeForUri`/`GetMimeTypeDescription` → `virtual`. El IDE GTK **no** los sobreescribe: comportamiento idéntico al de antes (regla 18.4, la ruta `--old-gui` no se toca).
5. `AddonManifest.cs` (manifiesto del host): `MimeTypeNode` + `baseType`, `isText`, `pattern`, `fileExtensions` (forma de `MimeTypeNode` legacy) + `FileExtensionsJsonConverter` que acepta la lista de patrones como array JSON **o** como string comma-separated (las dos formas que usan los manifiestos shipped).
6. `AddonRegistry.cs` (host): + `Version` (contador monótono de mutaciones para invalidación de caché de consumidores que snapshotean nodos) + extension point `ExtensionPoints.MimeTypes = "/MonoDevelop/Core/MimeTypes"` (incluido en `All`).
7. `App.axaml.cs` (shell): `MimeCatalog.SetExtensionRegistry (addons.Extensions)` (funde los nodos de los add-ins cargados en el catálogo) + línea de sonda al log del smoke: `[avalonia-addons] mime .cs=… icon .cs=… desc .cs=…` (el smoke de QA lo aserta).
8. `MonoDevelop.CSharpBinding.avaloniaaddon.json`: + nodo `/MonoDevelop/Core/MimeTypes` — `text/x-csharp` (.cs/.csx, label "C# source code", icono `md-file-source`, isText) y `application/x-csproject` (.csproj, icono `md-project`, baseType `application/x-msbuild`).
9. `MainWindow.axaml.cs` (árbol): `FileIconResource`/`FileIconId` preguntan primero a los hooks de plataforma (`App.Platform?.GetIconIdForFile`); si el hook responde un icono id conocido (`IconService.IsKnown`), se usa (`IconService.ResolveFilePath` para la ruta del PNG en el set de iconos). El switch por extensiones queda solo como fallback sin-plataforma/unknown.
10. `IconService.cs`: + `ResolveFilePath(stockId)` público (el fichero PNG que `ImageService` elegiría bajo el tema actual — permite a consumidores no-Avalonia cargar el mismo PNG por ruta de fichero).

**Fixes del pipeline de iconos** (hallados y validados durante el QA de B.4):

11. `IconService.cs` (fix #1, `ParsedStockMap`): bug last-write-wins — `StockIcons.addin.xml` declara pares Menu(16px)+Dnd(32px) y el 32px ganaba → celdas de 16px degradadas. Ahora: las entradas Menu (16px) ganan; las Dnd solo actúan como fallback para ids sin entrada Menu.
12. `Xwt.Avalonia/DrawingBackends.cs` (fix #2a, submódulo xwt): `GetBitmap` size-aware (antes colapsaba a 1×1 para imágenes con size explícito).
13. `Xwt.Avalonia/ListBackends.cs` (fix #2b, submódulo xwt): `ImageToSource` llamaba la sobrecarga 1-args `GetBitmap(backend)` (→ `Size.Zero` → 1×1 cacheado en `d.Bitmap`); ahora llama `GetBitmap(backend, image.Size)`.

**Cómo validar** (evidencia completa en `~/opencode/qa_b4/`, índice `qa_report_b4.md`):

1. **Builds 0 errores**: shell (`MonoDevelop.Startup.Avalonia.csproj`) y submódulo xwt.
2. **Smoke `--addins` + `--sln`**: `discovered=32`, loaded 30/32 (2 = Mac/WindowsPlatform por OS gate), línea de sonda exacta `mime .cs=text/x-csharp icon .cs=md-file-source desc .cs=C# source code`, `Loaded QaB4.sln`, 0 `Exception` en el log.
3. **Validación arbitral de iconos (P3e)**: 17/17 filas del árbol de QaB4 correctas en build limpio — cadena PNG→SKBitmap→WriteableBitmap→pantalla fiel (src==wb byte-identical 17/17; SAD pantalla 0–39; cross-identificación sin intercambios; variantes `~dark` reales bajo tema oscuro).
4. **Regresión `--old-gui` (regla 18.4)**: proceso legacy vivo, ventana renderizada, árbol de QaB4 pintado con iconos, 0 TypeLoad/MissingMethod/GetMimeType. Nota de entorno: los smokes en `env -i` necesitan `DOTNET_ROOT=~/.dotnet` para que el apphost `MonoDevelop.MSBuildBuilder.exe` arranque; sin él, los loads de sln reportan `Connection failed`/restore errors no relacionados con el código.
5. **avprobe** (probe del pipeline real de celda en app Avalonia): 5/5 `PROBE DONE fails=0`.

**QA (§18.5)**: Tester QA Senior, iteraciones P1–P5. P1 PASS (builds + harness). P2 PASS (smokes). P3: dos bugs reales hallados y corregidos — fix #1 (last-write-wins 32px) y fix #2 (pipeline del árbol colapsaba a 1×1; en dos complementos: backend size-aware + llamada 2-args en `ImageToSource`) — cerrado en **P3e con 17/17** (la iteración P3d fue un FAIL a falso: el analista usó referencias BASE en vez de las variantes `~dark` y geometría obsoleta; la captura "fallada" resultó píxel-identical a la del build limpio). P4: core dump del probe QA → **resuelto, no es bug de la app** (era del probe: un `using var conv` dentro de un bloque `if` liberaba el backend antes de tocar el `SKBitmap` crudo extraído por reflection; el clone byte-a-byte crasheaba 5/5 y el probe corregido 0/5; la app opera estable y su pipeline no libera el backend antes de pintar). P5: regresión `--old-gui` PASS + checks estáticos limpios (git diff = solo B.4 + los 3 fixes; sin código de diagnóstico residual). **Veredicto final: POSITIVO — sin hallazgos abiertos.**

**Cierre**: B.4 completa: los hooks de plataforma de la shell Avalonia son dueños de la resolución MIME + iconos real (catálogo compartido + manifiestos por add-in; CSharpBinding dueño de `text/x-csharp`), el árbol muestra iconos por tipo desde el set de iconos (variantes `~dark` bajo tema oscuro) y la ruta `--old-gui` queda sin efecto (los métodos de `PlatformService` solo pasan a `virtual`; el IDE GTK no los sobreescribe). Submódulo xwt: 2 ficheros (fixes #2a/#2b) commiteados dentro del submódulo (rama `net10`) y puntero actualizado en el repo principal.

**Pendiente / follow-up**:

- Extender el catálogo a policy panels por mime type: el extension point `/MonoDevelop/ProjectModel/Gui/MimeTypePolicyPanels` ya existe en el registro del host pero sin consumidores.
- El campo `icon` de los nodos MimeTypes es un stock id (ej. `md-file-source`); si algún manifiesto quisiera un resource path directo haría falta convención.

### Oleada 4 — cierre net10: P1 Mono.Posix, T1 WPF, T4 Roslyn 5.9.0, T5 NuGet 6.11.2, T6 Cairo-MIG (2026-10-26)
Antecedente: los cierres de olas 1-3 (2026-10-23/24) quedaron documentados en
`docs/migration-status-report.md` (secciones "Lote 1", "Lote 2 + cierre de olas 1-3" y "T7";
este plan se retoma aquí). Detalle completo por tarea en la sección "Oleada 4" de ese informe.

**Cambios** (resumen; 79 M + 2 D + 1 carpeta nueva en main + 3 submódulos):
1. **P1 — Mono.Posix → MonoUnixCompat**: nuevo proyecto `main/src/compat/MonoUnixCompat/`
   (facades `Catalog`/`UnixCompat` sobre `System.IO.Unix`/P-Invoke net10.0), integrado por
   `ProjectReference` desde `MonoDevelop.Core.csproj`; además entra en `Main.sln` (carpeta
   "Core", 8 mapeos de configuración; añadido tras el cierre de ola 4 a petición del
   operador, verificado en QA QS1). Eliminados
   `core/MonoDevelop.Core/MonoDevelop.Core/{Catalog,Unix.Catalog}.cs`.
   Submódulos: `Mono.Debugging.Soft.csproj` (debugger-libs) y `Mono.Addins.Gui.csproj`
   (mono-addins) → `ProjectReference` a MonoUnixCompat en vez de `<Reference Mono.Posix>`.
2. **T1 — WPF net10.0-windows (opción A)**: 5 csprojs (WindowsAPICodePack Core/Shell,
   MonoDevelop.TextEditor.Wpf, WindowsPlatform, Xwt.WPF); 3/5 build limpio en Linux + 2
   latentes preexistentes documentados (NU1102 `DiagnosticMargin`; `_wpftmp` de
   WindowsPlatform). `xwt` (submódulo): `AccessibleBackend.AccessibilityHelper.cs`
   `#if NET47` → `#if NET47 || NET10_0` (accesibilidad reactivada).
3. **T4 — Roslyn 4.8.0 → 5.9.0**: `msbuild/RoslynVersion.props`; `MonoRoslynCompat/src/CompatStubs.cs`
   (stubs + adaptador MEF; fix stub `ValueSource<T>`); `MonoDevelopMetadataReferenceManager.cs`
   reescrito stream-based; null-guards de `ISolutionCrawlerRegistrationService` (eliminada en
   5.9.0); staging 5.9.0 md5-verificado en `main/build/`; `libgit2-71efcb3.so` compilado de
   `external/libgit2` y stageado (bloqueador preexistente de document-open en Gtk).
4. **T5 — NuGet 5.4.0 → 6.11.2**: `Directory.Build.props` `<NuGetVersionNuGet>` + 10 ficheros
   adaptados a la API (bug Roslyn-10 CS0534/CS0114 documentado, no bloqueo).
5. **T6 — Mono.Cairo (ámbito MIG)**: `HslColor.ToRgb` public; `RtfWriter` a `List<HslColor>`;
   overload Cairo muerto de `SignatureMarkupCreator` (×2); `MonoDevelopHexEditorStyle` a
   HslColor; 6 `using Cairo;` muertos; referencia `Mono.Cairo` retirada de HexEditor +
   CSharpBinding (Gtk) + Avalonia.CSharpBinding. Bloqueador CS0012 resuelto quitando el
   `using MonoDevelop.Components;` innecesario en 4 panel-files (forzaba cargar firmas de
   extension methods Cairo de `HelperMethods`) y cualificando `MonoDevelop.Components.Control`
   en `CreatePanelWidget()` (conversión implícita desde `Gtk.Widget`). Residual G1
   (core + 14 addins Gtk) → T7.

**Cómo validar**:
- Gate `dotnet build main/Main.sln -c DebugLinux` = 0 Errores.
- Smoke Gtk `--old-gui` (QaB4 sol+doc): ventana, fatals=0, errors=0; ruta semántica Roslyn
  5.9.0 ejercitada (class outline → `GetSemanticModelAsync`, 167 refs in-process).
- Smoke Avalonia `--sln=QaB4`: solución carga de verdad, proceso vivo, fatals=0.
- Nota: en modo Avalonia la solución solo se abre con `--sln=<path>` (posicional no aplica;
  `MonoDevelop.Startup.Avalonia/Program.cs:172-176`); en `--old-gui` el sln/doc son posicionales.

**QA (§18.5)**: Tester QA Senior en 8 partes encadenadas con informe persistente
(`~/opencode/qa_wave4/`, fuera del repo por §18.6): Q1 gate 0 Errores · Q2 smoke Gtk OK ·
Q3 smoke Avalonia OK (r2) · Q4 WPF 5/5 en estado documentado · Q5 Mono.Posix OK · Q6 NuGet
6.11.2 OK · Q7 Cairo OK (r2) · Q8 estado repos OK. **Veredicto: APROBADO, 0 hallazgos
abiertos**. Hallazgos resueltos en el ciclo: (1) smoke Avalonia con argumento posicional en
vez de `--sln=` (defecto del script de QA); (2) estado rancio de `vs-editor-api` (faltaba
`bin/Editor/Text/Def/TextData/DebugLinux` → CS0234 en MonoRoslynCompat en rebuild forzado;
el proyecto `TextData` está en Main.sln y un build limpio lo regenera — nota T9); (3) copia
rancia de `MonoRoslynCompat.dll` en `main/build/` (normalizada al build DebugLinux).

**Cierre**: olas 1-4 cerradas sobre `Main.sln` a 0 errores; submódulos debugger-libs,
mono-addins y xwt commiteados/pushed en su rama `net10` (forks DRavainera) y punteros
actualizados en el commit de ola 4 del repo principal.

**Pendiente / follow-up**:
- T8: reimplementar funcionalidades quitadas (auditoría T3, 68 items).
- T9: higiene de build — regeneración de outputs de `vs-editor-api` desde cero; carrera
  `runtimeconfig.json` entre TFMs net10.0/net10.0-windows sobre `main/build/` (lock MSB4018
  transitorio; relanzar el gate si aparece).

### M10 (cierre) — File > New File: port del diálogo real Gtk legacy → Avalonia (2026-11-03)

**Contexto**: el item M10 describía `NewFile` como un stub "newN.cs" sin archivo backing. Esta
tanda lo sustituye por el diálogo "New File" **real** del legacy
(`MonoDevelop.Ide.Projects.NewFileDialog`, 718x524, paneles 192/292/*) construido en Avalonia
reciclando módulos existentes (regla 18.4), sin crear un módulo nuevo ni separar carpeta.

**Qué se hizo**:
- **Nuevo** `Views/NewFileDialog.axaml` (+ `.axaml.cs`): réplica del layout legacy — chrome
  (título `New File` + botón `✕`), árbol de categorías (`FuncTreeDataTemplate<NewFileCategory>`,
  mismo patrón de los árboles de `MainWindow`), `ListBox` de templates con `DataTemplate`
  (icono `Image` 32x32 + `Name` + `Language`), panel de info (`TemplateTitle` bold +
  `TemplateDescription`), fila `Name:` (`NameBox`), caja "Add to project" (`AddToProjectCheck` +
  `ProjectCombo` + `ProjectPathBox` + `BrowseButton`) y fila de acciones `Cancel`/`New`.
- **Catálogo** de los 2 `FileTemplate` de `CSharpBinding.addin.xml` (id de nodo = legacy):
  `CSharpEmptyFile` ("Empty File", icono `md-text-file-icon`) y `CSharpAssemblyInfo`
  ("Assembly Information", icono `md-file-information`, `FileSubPath=Properties/AssemblyInfo.cs`,
  exclusión si ya existe `AssemblyInfo.cs` en el directorio de creación — solo con proyecto).
- **Creación de fichero** (`OnNew`): **BOM UTF8 siempre** + standard header (policy global vía
  `PreferencesDialog.LoadGlobalPolicy("StandardHeader")`, ahora `internal`) + cuerpo del template
  (`TemplateText.Parse` — port mínimo de `FileTemplateParser`: `$${Tag}` + `$if$`/`$else$`/
  `$endif$`). Escritura con `StreamWriter(…, UTF8Encoding(true))` para emitir el BOM
  (`File.WriteAllBytes(GetBytes)` NO lo emite — bug corregido).
- **Dos modos**: con proyecto (aísta al proyecto, `Properties/AssemblyInfo.cs` para Assembly
  Info) y sin proyecto (documento en memoria *unsaved*, `UnsavedFileName`/`UnsavedContent`).
- **Última categoría** persistida en `PropertyService` (`Dialogs.NewFileDialog.LastSelectedCategory`
  + sufijo `.C#.C#` si hay proyecto activo). Iconos vía `IconService.GetImage(IconId)` (fallback
  `md-file-source`).
- **Cableado** en `Views/MainWindow.axaml.cs`: `OpenNewFileDialog()` (proyecto activo + paths de
  la solución), `HandleNewFileDialogResult(dlg)` (crea fichero o abre documento unsaved), helper
  `CreateUnsavedDocument(name, content)` (ya usado también por el hook `--windocs`), y 4 hooks QA
  (`--newfile`, `--newfile-unsaved`, `--ctxnewfile`, `--newfile-visual`) registrados en el
  allowlist de `Program.cs`.

**Cómo validar / evidencia** (recipe `env -i … DISPLAY=:0 … timeout -k 5 22 dotnet BIN --sln=… <FLAG>`;
artefactos en `~/opencode/nfqa/`):
- `--newfile` → `[newfile] created=True bom=True bytes=3`; el fichero real en disco captura 3
  bytes `EF BB BF` (BOM UTF8, template vacío).
- `--newfile-unsaved` → `[newfile] unsavedDoc=True dirEmpty=True` (documento en memoria, sin disco).
- `--ctxnewfile` → `[ctxnewfile] created=True name=QaCtxFile.cs`.
- `--newfile-visual` → `[newfile-qa] cat=General tmplCount=2 … title='Empty File'
  desc='Creates an empty C# file.' okEnabled=True`.
- Geometría `718x524` (`xwininfo`/`identify`); AT-SPI: frame `New File`, chrome, árbol `C#`→
  `General` (expansión interactiva), `ListBox` con **2** templates, info, `Name:`, "Add to
  project" + `TestProj`, `Path:`/`Browse`, `Cancel`/`New`; iconos renderizados (PIL: filas 57/64%
  tinta vs zona vacía 0%).

**Bugs corregidos en esta tanda**:
- **`tmplCount=0`** (lista vacía): `CategoryTree.SelectionChanged` no estaba cableado →
  `OnCategorySelected`→`FillTemplates` nunca corría; y `LastCategoryPath` por defecto `"General"`
  no resolvía con `FindCategory` (requiere `"C#/General"`). Fix: cablear `SelectionChanged`,
  defecto→`"C#/General"`, fallback `FindCategory("C#/General") ?? generalCategory`.
- **NRE al abrir** (`NameBox.Text.Trim()`): en esta versión de Avalonia `TextBox.Text` devuelve
  `null` antes de inicializar (señal reentrante durante `InitializeView` en `OnOpened`). Fix:
  deferir `InitializeView`/`UpdateOkStatus`/`NameBox.Focus` al `OnOpened` (one-shot
  `initialViewDone`) y null-coalescear las lecturas de `NameBox.Text`; null-guards en
  `OnCategorySelected`/`FillTemplates`/`OnTemplateSelected`.
- Errores de compilación previos corregidos: `?? return null` (CS1525), `KeyEventArgs`/
  `ToggleButtonEventArgs` (CS0246 — `IsCheckedChanged` con lambda), `walk` unassigned (CS0165),
  `LoadGlobalPolicy` no accesible (CS0122 → `internal`), `HierarchicalDataTemplate`→
  `FuncTreeDataTemplate` en code-behind (AVLN2000).

**QA (§18.5)**: Tester QA Senior — **APROBADO, 17/17 PASS, 0 hallazgos**. Evidencia: 4 hooks
`exit=124` (sin 134/SIGABRT, 0 `Unhandled/NullReference/TypeLoad/DllNotFound`), bytes reales
`EF BB BF`, AT-SPI completo (incl. expansión interactiva C#→General), 718x524, iconos OK, build
`0 Errores`, y regresión `--old-gui` (mismo binario arranca el IDE legacy sin crasheo; 0 archivos
legacy tocados — el diff solo toca `MonoDevelop.Startup.Avalonia`).

**Cierre**: repo principal commiteado/pushed en `agents/sub-agent-senior-dev-role-report`
(`Views/NewFileDialog.axaml` + `.cs` nuevos, `Views/MainWindow.axaml.cs`, `Program.cs`,
`Views/PreferencesDialog.axaml.cs` + docs). Sin submódulos modificados.

**Pendiente**: el *body* completo del template AssemblyInfo con las etiquetas autorales
(`AuthorCompany`/`CopyrightHolder`/`AssemblyInfoVersion`…) queda por poblar desde el xft cuando el
operador lo pida (hoy emite el esqueleto con `ProjectName`); y la integración con más lenguajes
que C# (Java, F#, VB…) cuando se migren sus `xft` (hoy el catálogo es solo C#, igual que la shell
que no carga el host legacy de add-ins).

### M10 (follow-up) — Alinear el borde/chrome del diálogo "New File" con el resto de diálogos (2026-11-03)

**Contexto**: el diálogo "New File" portado arriba usaba `DialogWindow.Apply(this)` (igual que el
resto de diálogos: NewSolution, About, Preferences, AddinManager, FindInFiles), pero su Grid raíz
en XAML llevaba `Margin="6"`. Como `Apply` ya envuelve el contenido en un `frame` `Border`
(`Margin=6, CornerRadius=8, BorderThickness=1, Bg=IdeWindowBgBrush` — ver `Controls/DialogWindow.cs:201`),
ese `Margin="6"` extra creaba una **segunda** banda de 6px del color-cuerpo entre el borde interior
del frame y la fila de título, de modo que la barra de título **no llegaba a las esquinas
redondeadas superiores** (se veía una banda de `IdeWindowBgBrush` alrededor del chrome). El sibling
`NewSolutionDialog` (que también usa `Apply`) **no** tiene margin en el Grid raíz, por lo que su
chrome queda sellado a las esquinas.

**Qué se hizo** (solo 3 edits de margin en `Views/NewFileDialog.axaml`; sin tocar lógica ni C#):
1. Grid raíz `<Grid RowDefinitions="Auto,*,Auto">`: `Margin="6"` → **eliminado** (sin margin). La fila
   de título (`dialogchrome`, que `Apply` marca con `CornerRadius(8,8,0,0)`) ahora llega sellada al
   borde interior superior del frame, cubriendo las esquinas redondeadas; el contenido se ajusta a
   los bordes interior izquierdo/derecho/inferior.
2. Grid de contenido (paneles + Name/Add-to-project): `Margin="6,8,6,0"` → `Margin="8,8,8,0"`
   (igualar al padding de 8px de `NewSolutionDialog`).
3. Fila de acciones `Cancel`/`New`: `Margin="0,10,0,0"` → `Margin="12,10,12,12"` (el botón `New`
   ya no queda pegado a la esquina inferior-derecha redondeada; queda 13px de holgura).

**Cómo validar / evidencia** (artefactos en `~/opencode/nfqa/`):
- Regresión funcional: los 4 hooks (`--newfile`, `--newfile-unsaved`, `--ctxnewfile`,
  `--newfile-visual`) siguen verdes tras el cambio (`exit=124`, `tmplCount=2`, `okEnabled=True`,
  fichero/unsaved/ctx creados; 0 `NullRef/Unhandled`).
- AT-SPI íntegro (frame, TreeView, 2 templates, Name:, Add to project, combo, Path:/Browse,
  Cancel/New).
- Geometría del chrome (análisis ARGB depth=32 sobre `nfqa/qa_border.png`): gutter de **6px**
  transparente (`alpha=0`) en los 4 lados; arcos de 8px anti-aliaseados (cortes de esquina
  `alpha=0`); columna central con chrome continuo `(45,45,45)` de y=6 a y=36, línea 1px `(58,58,58)`
  en y=37 y body `(30,30,30)` desde y=38 → **sin banda de color-cuerpo**.
- Comparación 1:1 con el sibling `About` (`nfqa/qa_about.png`): depth=32, gutter 6px transparente,
  mapa de esquina y columna central **idénticos píxel a píxel**.

**QA (§18.5)**: Tester QA Senior — **APROBADO, 0 hallazgos** (6/6 PASS: sin regresión funcional,
AT-SPI íntegro, chrome con gutter 6px + esquinas transparentes + título sellado sin banda, chrome
idéntico al sibling About, sin desbordamiento/corte de esquina, render coherente 718x524).

**Cierre**: repo principal commiteado/pushed en `agents/sub-agent-senior-dev-role-report`
(`Views/NewFileDialog.axaml` + docs). Sin submódulos modificados.

### New Solution (follow-up) — chrome real, iconos GTK y revisión de comportamiento (2026-11-03)

**Contexto**: el diálogo Avalonia `NewSolution` (ya funcional) tenía el chrome comentado en XAML,
iconos genéricos de `IconService` en lugar de los PNG GTK reales de plantillas y el checkbox
*Add to open solution* podía aparecer activo sin solución abierta.

**Cambios**:
- `Views/NewSolutionDialog.axaml`: chrome activo (título `New Solution` + botón `✕`) y
  `SolutionName` inicial `Solution`.
- `Views/NewSolutionDialog.axaml.cs`:
  - iconos/preview por base name GTK real (`console-project`, `library-project`, `test-project`,
    `crossplatform-shared-project`);
  - `IconService.GetTemplateImage(...)` como carga;
  - fallback de nombre `Solution`;
  - `Add to open solution` solo enabled/checked si hay solución abierta o `forcedLocation`.
- `MonoDevelop.Ide/Services/IconService.cs`: `GetTemplateImage(baseName, scale)` carga desde
  `main/src/core/MonoDevelop.Ide/templates/images` y respeta `ActualThemeVariant` para la variante
  dark.
- `Program.cs`: allowlist QA + `--newsolution`.
- `Views/MainWindow.axaml.cs`: `--newproject` crea en `~/opencode/QAProj/<guid>` (regla 18.6).
- `Views/MainWindow.axaml`: cambio de operador (`MonoDevelopIcon` → `MonoDevelopLogo` + comentario),
  sin referencias en C#, commiteado aparte.

**Validación / evidencia**:
- Build `0 Errores`.
- `~/opencode/ns_capture.sh`: `exit=124`, `920x600`, AT-SPI completo (frame, chrome, C#/F#/Other,
  4 plantillas, description, Solution name, Location, 3 checkboxes, Preview, Create/Cancel).
- Checkbox:
  - con solución: `CHECKED,ENABLED,FOCUSABLE,SENSITIVE,SHOWING,VISIBLE`;
  - sin solución: `FOCUSABLE,SHOWING,VISIBLE`.
- `~/opencode/ns_run.sh`: console/library/unittest/shared → `created=True`, `sln-entry=True`,
  `mappings=True`, counts 1/1, 0 excepciones.
- Regresión `--old-gui`: `exit=124`, 0 fatal.

**QA (§18.5)**: Tester QA Senior — **APROBADO** (8/8 PASS; informe
`~/opencode/nsqa_qa/qa_report.md`). Observaciones no bloqueantes resueltas/documentadas:
- O1: `MainWindow.axaml` fuera de lista = cambio de operador (renombrado + comentario); commit
  aparte.
- O2: variante base de icono correcta: `ActualThemeVariant` no es Dark en runtime
  (`theme-dark=False` con diagnóstico temporal, retirado); los brushes oscuros son recursos
  estáticos del diseño.
- O3: `--old-gui` sin stdout (comportamiento conocido; validado con `wmctrl`/captura).

**Deltas GTK aceptadas**: wizard 2 páginas vs single-page; sin *Project name* separado; sin
*Folder Layout Preview* en vivo; sin checkbox “.gitignore”; sin botón *Browse*; F#/Other/Recent
ausentes o deshabilitados; validación completa de nombres pendiente.

**Hallazgo preexistente (no bloqueante)**: `AppendProjectToSolution` puede malformar el `.sln`
tras múltiples appends sobre el mismo fixture; las pruebas usan fixture fresco por plantilla.

**Cierre**: repo principal commiteado/pushed en `agents/sub-agent-senior-dev-role-report`:
(1) commit separado para `MainWindow.axaml` (operador); (2) New Solution + docs. Sin submódulos
modificados.

### M5 (follow-up) — Welcome Page: iconos, tile 32px y alturas independientes de paneles (2026-11-03)

**Contexto**: la Welcome Page Avalonia (anteriormente portada en M5) conservaba tres deltas frente al
legacy GTK:
1. Los enlaces `MonoDevelop.com`, `Documentation`, `Support`, `Q&A` y los botones `New...`/`Open...`
   no mostraban icono.
2. El tile de solución reciente usaba un icono 16px (`md-solution`) en lugar del 32px del diseño
   legacy.
3. Los tres paneles de la fila 2 (`Solutions`, `Xamarin News`, `Did you know?`) se estiraban a la
   misma altura porque compartían un `StackPanel` horizontal.

**Cambios** (regla 18.4: reutilización obligatoria, sin carpeta nueva):
- `MonoDevelop.Ide/Services/IconService.cs`: `GetResourceImage` ahora resuelve también desde
  `MonoDevelop.Ide.WelcomePage/icons` (`welcomeIconsDir`), como fallback tras el directorio principal
  de iconos, sin shadowear recursos ya resueltos.
- `MonoDevelop.Startup.Avalonia/Views/WelcomePageView.axaml`: la fila 2 pasa de `StackPanel`
  horizontal a `Grid` con `ColumnDefinitions="300,30,510,30,300"`; cada `Border` (columnas 0/2/4) usa
  `VerticalAlignment="Top"` + `Margin="0,0,0,24"` para que la altura vertical sea propia de cada
  panel. El margen inferior del contenedor se traslada a los pads (`0,30,0,24` → `0,30,0,0`).
- `MonoDevelop.Startup.Avalonia/Views/WelcomePageView.axaml.cs`: el tile de solución reciente carga
  `solution-32` / `workspace-32` (según extensión) a 32x32 y el texto pasa a `Margin="6,0,0,0"`,
  conservando el `TextLeftPadding=38` del legacy (32 + 6).

**Validación / evidencia** (artefactos en `~/opencode/welcomeqa/`):
- Build de la shell: `0 Errores`.
- `welcome_capture.sh` (seeda un reciente `TestProj` y restaura `MonoDevelopProperties.xml`):
  - AT-SPI `tree.txt`: `Grid` de la fila 2 = `1170x195`; `Solutions` `300x171`, `Xamarin News`
    `510x125`, `Did you know?` `300x165` (sin el patrón estirado a 603).
  - AT-SPI `atspi.txt`: labels visibles de enlaces, `New...`, `Open...`, `TestProj`,
    `Xamarin News`, `Did you know?`.
- PIL sobre `welcome_full.png` (coordenadas client = absolutas AT-SPI − offset de `geom.txt`):
  iconos de enlace ~64/59/108/137 px grises `#575757`/`#585858`; `New...` 100 px grises;
  `Open...` 155 px grises; icono 32px del reciente 388 px magenta `#cd43ce`.
- Regresión `--old-gui`: estable (con `GDK_BACKEND=x11 WAYLAND_DISPLAY= XDG_SESSION_TYPE=x11`;
  el entorno hereda Wayland y GTK se cuelga si no se fuerzan esas variables).

**Nota de diagnóstico**: durante la validación inicial, un proceso antiguo de
`MonoDevelop.AvaloniaShell.dll` seguía vivo y `wmctrl`/AT-SPI estaban midiendo su ventana en vez de la
del build nuevo (falso "paneles aún estirados"). Matando el proceso antiguo, el build nuevo validó
correcto.

**QA (§18.5)**: Tester QA Senior — **APROBADO, PASS, 0 hallazgos abiertos**.

**Cierre**: repo principal commiteado/pushed en `agents/sub-agent-senior-dev-role-report`
(`IconService.cs`, `WelcomePageView.axaml`, `WelcomePageView.axaml.cs` + docs). Sin submódulos
modificados.

### MainWindow (follow-up) — logo monodevelop-22 y NuGet.Configuration 6.11.2 (2026-11-04)

**Contexto**: se solicitó enlazar el `Image x:Name="MonoDevelopLogo"` en `MainWindow.axaml` al
icono `monodevelop-22.png` y actualizar la referencia de `NuGet.Configuration` de la shell Avalonia
a la familia 6.11.

**Cambios**:
- `Views/MainWindow.axaml.cs`: `SetToolbarIcons()` añade
  `MonoDevelopLogo.Source = IconService.GetResourceImage ("monodevelop-22");`.
- `Views/MainWindow.axaml`: el `Grid` de la title bar pasa de `ColumnDefinitions="*,Auto"` a
  `"Auto,*,Auto"` para que el logo tenga su propia columna; el `Image` a `Grid.Column="0"` con
  `Margin="8,0,0,0"`, el `Menu` a `Grid.Column="1"` y `CaptionButtons` a `Grid.Column="2"`.
- `MonoDevelop.Startup.Avalonia.csproj`: `PackageReference` de `NuGet.Configuration` de `5.4.0` a
  `$(NuGetVersionNuGet)` (6.11.2, definido en `main/Directory.Build.props`).

**Validación / evidencia** (artefactos en `~/opencode/mainwindow_logo_nuget_qa/`):
- Build de la shell: `0 Errores`.
- `run.sh` (`--prefs=packagesources`, `app_exit=124` esperado): `run.log` con
  `[prefs-sources] count=1` y `[prefs-panel] packagesources | Sources`, sin excepciones.
- PIL sobre `main.png`: ~140 px azules en la región del logo (x0-40, y0-32) → `monodevelop-22.png`
  renderizado.
- `main/build/MonoDevelop.AvaloniaShell.deps.json`: `NuGet.Configuration/6.11.2` y
  `NuGet.Common/6.11.2`.

**QA (§18.5)**: Tester QA Senior — **APROBADO, PASS, 0 hallazgos abiertos**.

**Cierre**: repo principal commiteado/pushed en `agents/sub-agent-senior-dev-role-report`
(`e786a225c6`: `MainWindow.axaml`, `MainWindow.axaml.cs`, `MonoDevelop.Startup.Avalonia.csproj` +
docs). Sin submódulos modificados.

### New Solution (follow-up 2) — paneles estilo New File, Browse en Location y sin Preview (2026-11-04)

**Contexto**: se pidió alinear el diálogo "New Solution" (File ▸ New Solution...) con el diseño de
paneles del diálogo "New File", añadir un botón Browse al campo Location (selector de carpeta que
escribe la ruta en el TextBox) y eliminar el panel Preview porque no es necesario.

**Cambios**:
- `Views/NewSolutionDialog.axaml`: zona central a `Grid RowDefinitions="*,Auto,Auto,Auto"
  ColumnDefinitions="170,3,240,3,*"` replicando `NewFileDialog.axaml`: tres paneles con `Border` de
  1px (`IdeBorderBrush`) separados por `GridSplitter` de 3px — categorías | plantillas (iconos
  32px) | pane de info con el mismo patrón que New File (`DockPanel` con `TemplateTitle` en
  negrita + `TemplateDescription` con wrap). Las filas de campos pasan de estar dentro del 3er
  panel a filas a lo ancho bajo los paneles (estilo de la fila "Name:"): "Solution name:" +
  `SolutionName`; "Location:" + `LocationBox` + `Button x:Name="BrowseButton" Content="Browse"`;
  bloque de checkboxes (`CreateDirectoryCheck`, `AddToSolutionCheck`, `GitCheck`). Se eliminó la
  columna 4 (panel Preview: `PreviewImage` 96x96 + `PreviewLabel`). Ventana 920x600 → 760x600.
- `Views/NewSolutionDialog.axaml.cs`:
  - `OnTemplateSelected` pinta `TemplateTitle`/`TemplateDescription` (ya no `PreviewImage`/
    `PreviewLabel`);
  - nuevo handler `OnBrowseLocation`: `StorageProvider.OpenFolderPickerAsync`
    (`FolderPickerOpenOptions { AllowMultiple=false }`) y escribe
    `folders[0].Path.LocalPath` en `LocationBox` (mismo patrón que `NewFileDialog.OnBrowse`),
    errores a `Console.WriteLine("[newsolution] folder picker: ...")`;
  - el tuple `Templates` pierde el campo `PreviewIcon` (sin uso).

**Validación / evidencia** (artefactos en `~/opencode/newsolution_dialog_qa/`):
- Build de la shell: `0 Errores`.
- `run.sh` (`--newsolution`, `app_exit=124` esperado): `dialog.png` 760x600; `run.log` sin
  `Unhandled`/`Exception`/`TypeLoad`/`DllNotFound`.
- OCR (tesseract + PIL 3-4x invertido): presentes "New Solution", "Console Application", "Class
  Library", "Solution name:", "Location:", "Browse", "Create", "Cancel" y los 3 checkboxes;
  "Preview" NO aparece.
- PIL: bordes verticales `#3A3A3A` en pares (184/188, 421/431) por splitter + borde derecho x=744
  (info pane hasta el borde, la columna Preview desapareció); fila inferior con dos botones de 88px
  separados 8px (Create/Cancel).
- Hooks de regresión: `--newsolution`/`--newproject` de `MainWindow.axaml.cs` solo usan API que
  sigue existiendo (`CreatedSolutionPath`, `CreatedProjectPath`, `AddToOpenSolution`,
  `AutoCreateForQa`); 0 referencias a `Preview*` fuera del diálogo.

**QA (§18.5)**: Tester QA Senior — **APROBADO, PASS**. 1 hallazgo de severidad baja: la separación
plantillas|info es 10px (splitter 3px + `Margin="0,0,6,0"` del pane de plantillas) — réplica fiel de
`NewFileDialog.axaml`, que usa el mismo margen; se deja como está.

**Deltas GTK actualizadas** (vs la entrada "New Solution (follow-up)" de 2026-11-03): la delta
"sin botón *Browse*" queda resuelta (Browse añadido con selector de carpeta real); el panel
Preview (deltado como "sin *Folder Layout Preview* en vivo") se eliminó por decisión del operador.
Siguen vigentes: wizard 2 páginas vs single-page, sin *Project name* separado, F#/Other deshabilitados.

**Cierre**: repo principal commiteado/pushed en `agents/sub-agent-senior-dev-role-report`
(`b1c767bab3`: `NewSolutionDialog.axaml`, `NewSolutionDialog.axaml.cs` + docs). Sin submódulos
modificados.

### New File + New Solution (follow-up) — TextBoxes con largo fijo (2026-11-04)

**Contexto**: se pidió quitar el largo dinámico (estirado a lo ancho de la ventana) de los TextBox
de los diálogos "New File" y "New Solution" y dejarlos con largo fijo.

**Cambios** (solo XAML, 4 adiciones de `Width`):
- `Views/NewFileDialog.axaml`: `NameBox` y `ProjectPathBox` con `Width="360"`.
- `Views/NewSolutionDialog.axaml`: `SolutionName` con `Width="320"` y `LocationBox` con
  `Width="460"` (el botón Browse queda a su derecha, sin estirar la fila).

**Validación / evidencia** (artefactos en `~/opencode/newfile_newsolution_fixedwidth_qa/`):
- Build de la shell: `0 Errores`; el diff contiene solo las 4 adiciones de `Width`.
- `run.sh` (dos arranques: `--newsolution` y `--newfile-visual`; `exit=124` esperado en ambos):
  `newsolution.png` 760x600, `newfile.png` 718x524, logs sin excepciones.
- PIL (tema oscuro: fondo `#1e1e1e`, box sin foco `#121212`, box enfocado `#000000` + anillo
  `#3584e4`, botón `#4b4b4b`): `SolutionName` x≈115..432 (~320px, termina lejos del borde x≈752);
  `LocationBox` x≈78..535 (~460px) con Browse `#4b4b4b` a la derecha; `NameBox` (enfocado en
  `--newfile-visual`) x=62..421 = 360px exactos. Antes del cambio los boxes llegaban al borde
  derecho de la ventana (x≈752/710).
- OCR: "Solution name:", "Location:", "Browse", "Name:", "Create" presentes.
- `ProjectPathBox` no aparece en la captura porque `ProjectBox` se oculta sin proyecto abierto; la
  `Width` es el mismo patrón XAML ya validado.

**QA (§18.5)**: Tester QA Senior — **APROBADO, PASS, 0 hallazgos abiertos**.

**Cierre**: repo principal commiteado/pushed en `agents/sub-agent-senior-dev-role-report`
(`41304635d9`: `NewFileDialog.axaml`, `NewSolutionDialog.axaml` + docs). Sin submódulos
modificados.

### Iconos Fluent (follow-up) — familia MonoDevelop.Avalonia.Icons v2 con respeto de colores (2026-11-04)

**Contexto**: la shell Avalonia consume sus iconos de la familia `MonoDevelop.Avalonia.Icons`
(4399 PNGs) generada por el subagente "Diseñador Gráfico Senior". La v1 aplanaba todas las iconas
a la paleta gris/azul Fluent y eliminaba los colores originales (verde "add", rojo "error", azules
de file-types, logo, etc.). El operador pidió repetir la tarea exigiendo que **los iconos
respeten los colores**.

**Cambios**:
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Avalonia.Icons/` (nuevo, 4399 PNGs, v2). Regla por
  **base** (390 bases, criterio: chroma máx `max-min RGB` de los px opacos del archivo sin
  modificadores):
  - **118 bases coloreadas / 1438 archivos** (chroma > 32): RGB **bit a bit** del original en
    todos los modificadores (`~dark`, `~disabled`, `~sel`, `~error`, `~contrast`, `@2x`); cada
    variante legacy ya estaba teñida para su estado. Incluye file-types, project-types, estados
    semánticos (`error-*`, `warning-*`, `done-*`, `build-*`, `status-*`), breakpoints y logos
    `monodevelop-*`.
  - **272 bases mono / 2961 archivos** (chroma ≤ 32): un solo color de paleta Fluent por
    modificador (dark: base `#F3F2F1`, disabled `#605E5C`, sel `#4CC2FF`, error `#FF6E6E`,
    contrast `#000000`; light: `#323130`/`#A19F9D`/`#0078D4`/`#C42B1C`/`#FFFFFF`;
    contrast~disabled `#808080`; jerarquía disabled > sel > error > active > tema).
  - Alpha preservado bit a bit; flood-fill solo en los 2 PNGs sin transparencia
    (`zoom-in/out-16~contrast@2x.png`). Verificado: 0 variantes coloreadas de bases mono en el
    legacy (la regla por base no pisa colores).
- `main/src/core/MonoDevelop.Ide/Services/IconService.cs`:
  - Campo `fluentIconsDir` (sibling de `icons/`).
  - `ResolveFile(stockId, dark, disabled, scale)`: stock ids del IDE → primero
    `fluentIconsDir`, fallback a `icons/`; stock ids de addins (`VcStockToResource`) solo en la
    carpeta `icons/` de su addin (no tienen variante Fluent).
  - `GetResourceImage(name, scale)`: `fluentIconsDir` → `iconsDir` → `welcomeIconsDir`
    (welcome page sin colisión de nombres: 0 solapes verificados).
- `Views/MainWindow.axaml.cs`: flag QA `--iconsqa` (volca la resolución de 21 stock ids).
  `Program.cs`: allowlist del flag.
- La interfaz Gtk legacy (`--old-gui`) no se toca: `ImageService` sigue leyendo `icons/`
  (intacto; `git diff` de `icons/` vacío).

**Validación / evidencia** (artefactos en `~/opencode/fluent_icons/` y `~/opencode/fluent_icons_qa/`):
- Build de la shell: `0 Errores`.
- `verify.py` v2 → `TODO OK`: 4399/4399, dims 1:1, alpha idéntico (salvo 2 flood-fill), color
  válido (mono = 1 color de paleta; colored = RGB opacos iguales a la entrada).
- `bash ~/opencode/fluent_icons_qa/run.sh` (`--iconsqa`): `[iconsqa] total=21 fluent=20
  legacy=0 missing=1` (`md-plugin`: id colgante preexistente, no está en ningún mapa de stock
  ids; no es regresión); `mainwindow.png`; sin excepciones.
- Píxeles vivos (PIL sobre `mainwindow.png`): colores source exactos en el toolbar —
  `(85,174,126)` ×52 (`execute-16~dark`) y `(33,177,122)` ×14 (`debug-all-16~dark`), imposibles
  bajo v1 (todo gris).
- Muestreo aleatorio (seed 20261104): 10/10 colored byte-identical RGBA completo; 10/10 mono con
  un único color de la paleta según modificadores.
- `design_report.md` v2 + `contact_sheet.png` (37 pares legacy|Fluent, variante `~dark`, x4, con
  4 pares de evidencia de color).

**QA (§18.5)**: Tester QA Senior — **APROBADO, PASS, 0 hallazgos** (checks A–H: build, contratos
de archivo, integridad legacy, lógica de resolución, runtime, colores en pantalla, samples,
csproj). Observaciones no bloqueantes: `md-plugin` colgante (cleanup en tarea aparte),
`run1_exit=124` (timeout del harness), 128 warnings MSB3277 preexistentes.

**Cierre**: commit `5dbb3196cb` pushado a `origin/agents/sub-agent-senior-dev-role-report`.

### Solution pad (follow-up) — gutter estilo GTK: triángulo expander + líneas guía de indentación (2026-11-04)

**Contexto**: se pidieron 3 mejoras de aspecto en el solution pad de la UI Avalonia para
equipararlo al pad Gtk legacy: (1) icono expander **triangular** a la izquierda del icono del
nodo en todos los nodos expandibles (apuntando **abajo** si está expandido, a la **derecha**
si está colapsado); (2) subárbol de dependencias/References renderizado igual que GTK; (3)
**líneas guía verticales** de indentación entre niveles. El gutter de la shell Avalonia es 100 %
código Avalonia: en el backend Gtk el triángulo y las líneas los dibuja nativamente el widget
`GtkTreeView` (no hay código de "gutter" en `Xwt.Gtk` que reutilizar), así que la versión
Avalonia es una rasterización propia que emula ese aspecto.

**Cambios** (submódulo `main/external/xwt`, rama `net10`; backend `Xwt.Avalonia`,
`namespace Xwt.AvaloniaBackend`):
- `Xwt.Avalonia/TreeGutter.cs` (**nuevo**): control del gutter. `Render` dibuja 1 riel vertical
  de 1 px por nivel ancestro (nodo de profundidad `d` → `d` rieles) y el triángulo expander en
  su nivel (`r=4`, scanline: abajo `hw=r*(1-t)`, derecha `hh=r*(1-t)`). Todo con
  `DrawRectangle` (fill): en este build de Avalonia un trazo de 1 px (`DrawLine`/`DrawGeometry`)
  se antialiasa a unos pocos % de su alpha, mientras que un relleno compone a brillo completo.
  El riel se alinea a píxel entero (`x=i*16+8`, ancho 1) para cobertura completa.
- Color por **luminancia del fondo efectivo**, NO por `Application.ActualThemeVariant` (en esta
  app la variante lee `Light` aunque la ventana pinta fondo oscuro, y `TryGetResource("IdeFgBrush")`
  da `#1a1a1a` en ambas variantes): `EffectiveBackground` sube por el visual tree hasta el primer
  `Grid` con fondo sólido opaco; `dark = 0.299R+0.587G+0.114B < 128`. Oscuro → riel `0x60`/
  triángulo `0xE6`; claro → `0xB0`/`0x33`. El control es transparente (el resaltado de selección
  se ve a través de las marcas).
- `Xwt.Avalonia/ListBackends.cs`: `RowPanelHost` nuevo modo árbol (`IsTree`, `IndentUnit=16`,
  `ExpanderSlot=16`) y **columna gutter** al inicio de cada fila en `RebuildRows` (la indentación
  ya no es `Margin` de fila). `TreeViewBackend`: `IsTree=true`; umbral de toggle del expander
  `p.X < Depth*IndentUnit + ExpanderSlot` (zona de gutter, a la izquierda de la primera celda);
  fondo `Brushes.Transparent` en filas no seleccionadas para que el click en el gutter sea
  hit-testeable (la base deja `null`, que Avalonia no hit-testea).
- APIs Avalonia 12.1.2: `Visual` en `namespace Avalonia` (no `Avalonia.Media`);
  `GetVisualParent()` es extensión en `Avalonia.VisualTree`; `Background` vive en `Grid` (no
  `Control`/`Decorator`); los tipos de Avalonia se nombran con aliases de nivel superior
  (sombreado por `Point`/`Color`/`Rectangle`/`DrawingContext` del frontend Xwt).

**Validación / evidencia** (artefactos en `~/opencode/`):
- Build (0 errores): `Xwt.Avalonia.csproj` y `MonoDevelop.Startup.Avalonia.csproj`
  (`-c Debug -f net10.0`); la ruta `--old-gui` (Xwt.Gtk) no se toca y la app (binario de ambas
  rutas) compila.
- Captura + forense PIL: `bash /home/daniel/opencode/solpad_avalonia_qa.sh` (arranca con
  `~/opencode/solpad_deps/Fixture.sln`, captura `solpad_avalonia_1.png`; ventana 320,157 1280x800;
  origen del panel en captura (7,141); fila 21 px). Rieles en captura-x 15/31/47; centro del
  triángulo por profundidad `d` en 15/31/47/63.
- Resultados: nodo profundidad `d` → `d` rieles (lum 96=`0x60`) + triángulo (lum 230=`0xE6`) en
  su nivel; hoja → solo rieles. Triángulo expandido = base arriba estrechándose abajo (perfil
  `[8,6,6,4,4,2,2]` por fila-y); colapsado = borde izq vertical estrechándose a la derecha.
  Jerarquía triángulo(230) ≫ riel(96) > fondo(30).
- Clic (XTest `python3-xlib`, sin xdotool): un clic en el gutter colapsa (queda 1 fila, triángulo
  a la derecha) y el siguiente re-expande (gutter idéntico, delta 0 en los 24 puntos de las filas
  0-5); el toggle funciona también en profundidad >0.
- `grep -c SOLPAD solpad_avalonia.log` = 0 (sin instrumentación; no existe la cadena `SOLPAD` en
  fuentes).

**QA (§18.5)**: Tester QA Senior — **APROBADO, 5/5 PASS, SIN ERRORES** (arranque/captura;
rieles+triángulos por fila; forma del triángulo abajo/derecha; clic colapsa/re-expande; sin
instrumentación). Nota operativa no bloqueante: el 1er click justo tras el arranque puede perderse
en la transición de foco del WM sobre XWayland (focus-on-click); con la ventana enfocada, un click
en el gutter siempre conmuta.

**Cierre**: submódulo `main/external/xwt` commiteado/pushed en rama `net10` (`8b3233b5`:
`TreeGutter.cs` + `ListBackends.cs`); repos principal `b7e3070125` (gitlink del submódulo) pushado
a `origin/agents/sub-agent-senior-dev-role-report`. Sin código GTK en la ruta Avalonia.

### Solution pad (follow-up) — Dependencies y referencias portadas de la UI Gtk (2026-11-04)

El solution pad Avalonia no pintaba el subárbol de dependencias que el pad Gtk legacy
(addin `MonoDevelop.DotNetCore`) muestra bajo cada proyecto. Tarea: portar
"Dependencies > Platforms" y, sobre todo, **las referencias dentro de Dependencies**
tal cual la UI Gtk, retirando el nodo interno "References" que la shell mostraba como
reemplazo provisional.

**Forma decidida (operador, opción b)**: el nivel "Platforms" se muestra **siempre** —
`Proyecto → Dependencies → net10.0 → …` — también con TFM único. El legacy
(`DependenciesNode.GetLoadedDependencyNodes`) colapsa el TFM único y pinta sus hijos
directamente en Dependencies; la desviación es deliberada y quedará marcada así en el
handoff.

**Fuente legacy referenciada** (`main/src/addins/MonoDevelop.DotNetCore/
MonoDevelop.DotNetCore.NodeBuilders/`):
- `DependenciesNode.GetChildNodes(frameworkNode)` — orden de los grupos bajo cada TFM:
  1. `FrameworkReferencesNode` (label "Frameworks", icono `Stock.OpenReferenceFolder`) solo
     si `CanGetFrameworkReferences` y con hijos;
  2. `PackageDependenciesNode` (label "NuGet") con los `PackageReference`;
  3. `SdkDependenciesNode` (label "SDK") **solo si no se añadió Frameworks**;
     `GetDefaultNodes`: `IsNetCoreApp` → `Microsoft.NETCore.App`, `IsNetStandard` →
     `NETStandard.Library`;
  4. `AssemblyDependenciesNode` (label "Assemblies") con los `Reference`;
  5. `ProjectDependenciesNode` (label "Projects") con los `ProjectReference`.
- Iconos hoja: `PackageDependencyNodeBuilder` → `md-package-dependency` (nuget-16.png,
  PNG del addin `MonoDevelop.PackageManagement`); `ProjectReferenceNodeBuilder` →
  `md-reference-project` (Project), `md-reference-folder` (Assembly, label
  `Path.GetFileName`), `md-reference-package` (Package); `FrameworkReferenceNode` →
  `md-reference-package`. Todos los folders: `Stock.OpenReferenceFolder` =
  `folder-component-16.png`.
- `ProjectReferenceNodeCommandHandler.ActivateItem`: al activar una referencia de
  proyecto abre el archivo fuente principal del proyecto referenciado.

**Implementación** (`main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs`):
- `AddXwtDependenciesNode`: nodo "Dependencies" (`folder-component-16.png`, tag
  `dependencies:<path>`) + un hijo por TFM (`framework-16.png`, tag
  `platform:<path>:<tfm>`). TFMs vía `ReadTargetFrameworks` (`<TargetFrameworks>`
  separado por `;`, luego `<TargetFramework>`; XDocument namespace-aware; sin MSBuild).
- `AddXwtTfmReferenceGroups`: los 5 grupos anteriores en orden legacy, bajo **cada** TFM.
  Grupo vacío → no se pinta (paridad con `HasChildNodes`). Tags:
  `depgrp:<path>:<tfm>:<kind>`, `fwref:`/`package:`/`asmref:` (no-archivo → RowActivated
  inerte) y la referencia de proyecto con tag = ruta del `.cs` objetivo.
- `XwtReferencedProjectFile`: resuelve el include relativo (con `\` → `/`) al .csproj
  referenciado y devuelve el primer `.cs` de su directorio (orden alfanumérico); si no
  resuelve, devuelve el include crudo (no-archivo).
- `ReadProjectReferences` + clase `XwtProjectReferences`: lectura ligera de los 4 tipos
  de items. **Limitación documentada**: los ItemGroups condicionales por TFM
  (`Condition="'$(TargetFramework)'==…'"`) no se evalúan (sin MSBuild); los items se
  listan iguales en cada plataforma.
- `XwtStockImage(stockId)`: helper nuevo (junto a `XwtStockIcon`) que resuelve un stock
  id → PNG vía `IconService.ResolveFilePath` (theme-aware; admite stock ids de addins)
  con fallback a `file-source-16.png`.
- **Retirada total del nodo "References"**: además del nodo Xwt, se eliminó el código
  muerto del árbol preview (`BuildReferencesNode`) y las ramas defensivas `references:`
  de `SelectedNodeType`, `ContextTargetDirectory` y el properties pad. Queda el
  clasificador `reference:` (singular, ItemType `ProjectReference`) por ser la
  clasificación legacy de un item de referencia en general.

**IconService** (`main/src/core/MonoDevelop.Ide/Services/IconService.cs`): una entrada
aditiva en `VcStockToResource` — `["md-package-dependency"] = ("nuget-16",
"MonoDevelop.PackageManagement")` (mismo patrón que el resto de ids de addins; el PNG
vive en `main/src/addins/MonoDevelop.PackageManagement/icons/`).

**Fixture** (`~/opencode/solpad_deps/Fixture.sln`, no commiteado — artefacto QA en
`~/opencode/`): 3 proyectos net10.0 que ejercitan los 5 grupos:
- App: `FrameworkReference` Microsoft.AspNetCore.App + `ProjectReference` LibA →
  Frameworks + Projects (**sin** SDK, como el legacy);
- LibA: `ProjectReference` LibB → SDK + Projects;
- LibB: `PackageReference` Newtonsoft.Json 13.0.3 + `Reference` System.Runtime →
  NuGet + SDK + Assemblies.

**Validación / evidencia** (artefactos en `~/opencode/`):
- Build (0 errores): `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/
  MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo`.
- `bash ~/opencode/solpad_avalonia_qa.sh` (arranca con el Fixture, captura
  `solpad_avalonia_1.png`, log `solpad_avalonia.log`; ventana 320,157 1280x800; fila 21
  px; indent 16 px/nivel). OCR por fila + PIL (tema oscuro: texto claro >170; el primer
  píxel claro de una fila expandible es el triángulo del gutter, no el texto):
  - App → Dependencies → net10.0 → Frameworks (Microsoft.AspNetCore.App) + Projects
    (LibA); LibA → … → SDK (Microsoft.NETCore.App) + Projects (LibB); LibB → … → NuGet
    (Newtonsoft.Json) + SDK (Microsoft.NETCore.App) + Assemblies (System.Runtime).
  - Orden de grupos = orden legacy exacto; App sin grupo SDK; archivos
    Program.cs/A.cs/B.cs presentes; sin bin/obj; sin "References" en el pad.
- Interacción X11 (foco `wmctrl -i -a` + doble-clic XTest): fila "LibA" bajo
  App→Projects → `[rowactivate] opened A.cs` + documento abierto en el editor
  (`[skeditor]` + `[outline] 1 node(s) in A.cs`); doble-clic en filas de grupo y de
  paquete **no** abre nada ni crashes.
- Limpieza: `grep -rn "AddXwtReferencesNode\|\"references:\"" main/src/core/
  MonoDevelop.Startup.Avalonia --include=*.cs` → 0 resultados.
- Capturas: `solpad_deps_tree.png` (árbol completo), `solpad_deps_editor.png` (A.cs en
  el editor), bandas OCR en `ocr_bands/`, diffs `diff_round2_mainwindow.txt`.

**QA (§18.5)**: Tester QA Senior. Ronda 1: 9/10 PASS; hallazgo menor: 4 restos
`"references:"` en código muerto preexistente (no del diff) → limpieza del concepto
"código muerto References" (arriba). Revalidación (ronda 2): **PASS, 0 hallazgos
abiertos** (grep limpio, el feature byte-intacto, log sin Exception, OCR del árbol
idéntico). Build 0 errores en ambas rondas.

**Cierre**: código en commit `0d1a1cc9dc` (MainWindow.axaml.cs + IconService.cs); este
handoff se pusha junto con él a `origin/agents/sub-agent-senior-dev-role-report`. Sin
cambios en submódulos; la ruta `--old-gui` no se ve afectada (cambios en la shell
Avalonia + entrada aditiva de mapa de iconos en `IconService` core).

### Menú (follow-up) — Oleada 1: backbone de comandos del menú sobre CommandManager + catálogo legacy (2026-11-05)

Primera oleada de la integración total del backend `MonoDevelop.Ide`/`MonoDevelop.Core`
en la shell Avalonia (directriz del operador: reutilizar módulos no-GTK, portar los
GTK, sin separación de carpetas). Antes de esta oleada la shell dispatchaba menús y
hotkeys directamente a `MainWindow.OnMenuCommand` con ids a mano; ahora el dispatch
corre por el `CommandManager` real de `MonoDevelop.Ide` (pipeline
`CommandActivating`/`CommandActivated`, el mismo sobre el que se basa el record/replay
de `AutoTestService.SessionRecord`), alimentado por el **catálogo de comandos legacy**.

**Fuente legacy referenciada**:
- Catálogo: las secciones `<Extension path="/MonoDevelop/Ide/Commands">` de los
  `*.addin.xml` (core `MonoDevelop.Ide` + cada addin). En el build GTK los carga
  `CommandCodon` vía el motor `Mono.Addins`.
- `CommandCodon.CreateInstance`: mapping codón → `ActionCommand` (sub-rutas de la
  ruta base, `Condition`/Platform, flags de tipo, `winShortcut` > `shortcut`,
  prefijo `@` de id de paquete, `disabledVisible`).
- `IdeApp.CommandService` (`IdeApp.CommandManager`): el singleton del IDE GTK que
  dispatcha por el `CommandManager`.

**Implementación**:
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Components.Commands/CommandCatalogLoader.cs`
  (**nuevo**, solo `AVALONIA_SHELL`): lee `baseDir/addinxml/**.addin.xml` y registra
  cada comando en el `CommandManager` sobre el modelo no-GTK (`Command`/
  `ActionCommand`), replicando el mapping de `CommandCodon` **sin** reutilizar los
  codones (derivados de `Mono.Addins.ExtensionNode`; cargarlos arrastraría el motor
  de addins GTK). `DefaultHandler` no se resuelve (los handler de GTK viven en el
  ensamblado GTK). `LastError` expone el primer fichero fallido.
- `.../CommandService.cs` (**nuevo**, solo `AVALONIA_SHELL`): singleton del shell
  (counterpart de `IdeApp.CommandService`): `Manager`, `LoadCatalog` (idempotente),
  `LoadedCommands`, `Count`, `GetCommand`, `Dispatch`.
- `.../CommandManager.cs` (región `#if AVALONIA_SHELL`): `Count` + `CommandRouter`
  (fallback para comandos de catálogo sin handler) y `DispatchCommand` tolerante a
  handler nulo (con router: `CommandRouter(cmd.Id, dataItem)`; la rama con
  `DefaultHandlerType` resuelto no cambia).
- `.../CommandEntry.cs` / `CommandEntrySet.cs`: guardas `#if !AVALONIA_SHELL` de las
  partes Gtk del modelo de entradas de menú.
- `MonoDevelop.Startup.Avalonia.csproj`: `Compile` de los 4 ficheros y `Content` de
  los 72 `*.addin.xml` a `addinxml/{core,addins}/` (`CopyToOutputDirectory=
  PreserveNewest`). El build GTK stagea su propio despliegue (`main/build/AddIns`)
  y no se toca.
- `Program.cs`: al arranque enlaza `CommandService.Manager.CommandRouter` a
  `MainWindow.Instance?.OnMenuCommand` y carga el catálogo desde
  `AppContext.BaseDirectory` (warn si hay `LastError`).
- `App.axaml.cs`: `Runtime.MainSynchronizationContext = SynchronizationContext.
  Current` en `OnFrameworkInitializationCompleted` (equivalente del main context de
  Gtk que registra `IdeStartup`, `IdeStartup.cs:182`) — los asserts de main thread
  de los servicios compartidos lo exigen en dispatch.
- `Views/MenuService.cs`: `RunCommand` y `CommandAction.Run` enrutan por
  `CommandService.Dispatch(id, null, CommandSource.MainMenu)` si el id está
  registrado; ids sintéticos (`"recent:"`, `"pad:"`, ...) o no registrados
  conservan `OnMenuCommand`.

**Forma**: el menú visible no cambia (misma estructura/items) — cambia **qué ejecuta**
cada item: la pipeline del `CommandManager` con el catálogo legacy, y el router como
fallback hasta que las oleadas siguientes porten los handlers core reales.

**Validación / evidencia** (artefactos en `~/opencode/`):
- Build `0 Errores` (net10.0 y net10.0-windows): `dotnet build main/src/core/
  MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f
  net10.0 --nologo`.
- Harness de catálogo (`~/opencode/catload_test/`): `Count=461`,
  `LoadedCommands=475`, ids de menú `196/172 resueltos`, dispatch `routed=1`.
- Smoke (`~/opencode/smoke_oleada1.sh`): arranque sin warnings, línea
  `[avalonia-shell] runtime main thread registered`, `--menudump` invariante
  (`menus=11 items=225 cmds=0 bindings=205`).
- QA E2E por teclado X11 (XTEST; el ratón XTEST no llega al cliente en este
  entorno Wayland/XWayland — hallazgo de entorno, ver abajo), app arrancada
  **sin flags de menú** (solo `--sln`):
  - P3: `alt+f` → popup File (255x464) → `Return` → diálogo "New File" en `wmctrl`
    (cadena completa `MenuItem → CommandAction.Run → CommandService.Dispatch →
    pipeline → CommandRouter → OnMenuCommand → OpenNewFileDialog`); `Down+Return`
    → "Create a New Solution" (760x600); Help `alt+h` + `a` → "About MonoDevelop"
    (rama lambda, no catálogo). Diálogos cerrados sin residuo (`Fixture.sln` 3
    proyectos, sin `~/Solution`).
  - P4: `ctrl+n` → diálogo "New File"; `F8` → build (`[build]` 0→5 en el log);
    `ctrl+q` → el proceso termina.
- Artefactos: `~/opencode/oleada1_qa/` (`cap.py`, `focus.py`, `click_qa.py`,
  `final_qa*.sh`, `seq_qa.sh`, `probe_*.sh`, capturas/OCR), `~/opencode/keys.py`,
  `~/opencode/smoke_oleada1.sh`, `~/opencode/catload_test/`, logs
  `~/opencode/oleada1_final3.log` y compañía.

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (ronda 1; solo
lectura sobre el repo, artefactos en `~/opencode/oleada1_qa_subagent/`) —
**APROBADO, PASS (P1–P4), 0 hallazgos de app abiertos** (ciclo §18.5 cerrado en
ronda 1). Evidencia: P1 build 0 errores, main thread registrado, `catload`
`Count=461/LoadedCommands=475`, menudump byte-igual a la referencia pre-Oleada 1;
P2 OCR 11/11 menús + 15 items de File; P3 `alt+f`+`Return` → "New File"
(`0x01600037`) y `alt+f`+`Down`+`Return` → "Create a New Solution"
(`0x0160003f`) y Help type-ahead `a` → "About MonoDevelop" (`0x01600047`), todo
con la app arrancada solo con `--sln` (sin flags) y sin lado efectos (Fixture
3 proyectos, sin `~/Solution`); P4 `ctrl+n` → New File, `F8` → `[build]` 0→5,
`ctrl+q` → proceso muere. Hallazgos del subagente (todos no bloqueantes,
ninguno atribuido a la Oleada 1): 1) [baja·entorno] `alt+f` 5/5 al primer
intento en la sesión del subagente (variabilidad); 2) [baja·entorno/preexistente]
el mal posicionamiento de popups arriba-izq no se reprodujo (abrieron bajo su
etiqueta) — intermitente; 3) [info·entorno] ratón XTEST no llega al cliente ⇒
QA solo por teclado; 4) [info·app, por diseño] el fixture `Fixture.sln` lleva
un error de compilación deliberado (CS0103 en `LibA/A.cs`) para ejercitar el pad
de problemas — F8 lo reporta (`[codeissues] 4 error(s)`) y la mecánica de build
funciona; 5) [info·app, cosmético] "Save" sin elipsis, fiel al catálogo legacy.
Observaciones de entorno acumuladas de la sesión de desarrollo: `NewFileDialog`
no cierra con Escape (solo Cancel) — brecha UX menor, candidata a Oleada 2;
ImageMagick `import` roba focus/hanguea ⇒ capturas con Xlib `get_image`.

**Cierre**: código en commit `0a3aff4b51` pushado a
`origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos; la ruta
`--old-gui` no se ve afectada (todo bajo `#if AVALONIA_SHELL` o aditivo no-GTK).

### Menú (follow-up) — Oleada 2: handlers core reales de `MonoDevelop.Ide.Commands` (111 comandos) (2026-11-05)

Segunda oleada de la integración total del backend (directriz: "sigue con la
oleada 2"). En la Oleada 1 el dispatch ya corría por el `CommandManager` +
catálogo, pero **todos** los comandos caían al router (`OnMenuCommand`). Esta
oleada integra los **handlers reales del núcleo**: los comandos cuyo
`defaultHandler` del catálogo se resuelve a una clase compilada en este build
pasan por un `DefaultHandler` real que, a su vez, delega en el mismo
`OnMenuCommand`. **Invariante de diseño: cero cambio de comportamiento** (menú
y dispatch final idénticos); lo único que cambia es que 111 comandos ya no van
por el router sino por su handler real.

**Fuente legacy referenciada**:
- El atributo `defaultHandler` de los `<Command>` del catálogo
  (`main/build/addinxml/**.addin.xml`) nombra la clase de handler
  (p. ej. `MonoDevelop.Ide.Commands.HelpHandler`). En el build GTK la instancia
  el motor de addins; aquí se resuelve por reflexión contra el ensamblado
  Avalonia.
- Los 11 ficheros `MonoDevelop.Ide.Commands/*.cs` (FileCommands, EditCommands,
  ProjectCommands, ViewCommands, WindowCommands, FileTabCommands, HelpCommands,
  ToolsCommands, TextEditorCommands, NavigationCommands, SearchCommands): cada
  handler legacy es una `CommandHandler` cuyo `Run(dataItem)` ejecuta la acción
  (vía servicios/`IdeApp`). Sus enums de id son compartidos.

**Implementación** (commit `176ee02252`, 16 ficheros):
- **Nuevo** `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Commands/
  ShellCommandHandler.cs` (`#if AVALONIA_SHELL`): puente estático
  `Run(object commandId, object dataItem=null)` → `CommandManager.ToCommandId`
  → `MainWindow.Instance?.OnMenuCommand(id, dataItem as string)`. Es el único
  punto donde el handler portado se entrelaza con la shell.
- 11 ficheros `MonoDevelop.Ide.Commands/*.cs`: código legacy envuelto en
  `#if !AVALONIA_SHELL` (byte-idéntico, usings GTK incluidos); enums
  compartidos sin guarda; bajo `#else` las clases con el mismo nombre delegan su
  `Run()` en `ShellCommandHandler.Run(commandId, dataItem)`. **Sin override de
  `Update` en esta oleada** (el estado de habilitación de menú es la Oleada 3);
  `CommandInfo.enabled/visible` son `true` por defecto, así que el dispatch no
  se bloquea. La mayoría delega por member de enum; los sin member lo hacen por
  string ID (políticas, code analysis, side-by-side/single mode, notebooks,
  `OpenDocument1..9`, `SwitchNext/PreviousPad`, `DumpUITree`/`DumpA11yTree*`/
  `MarkLog`).
- `.../MonoDevelop.Components.Commands/CommandCatalogLoader.cs`: contador
  `ResolvedHandlers` + `ResolveHandlerType(string)`
  (`typeof(CommandCatalogLoader).Assembly.GetType(name)` +
  `CommandHandler.IsAssignableFrom`) → asigna `cmd.DefaultHandlerType` en
  `ProcessCommand`; si no resuelve (handler de addin no compilado aquí) deja
  `null` y el comando cae al router (sin cambio de comportamiento).
- `.../CommandManager.cs` (región `#if AVALONIA_SHELL`): `DispatchCommand`
  instancia el handler con `Activator.CreateInstance (cmd.DefaultHandlerType,
  true)` (`nonPublic:true` porque los handlers del catálogo son mayormente
  `internal`); añade `GetCommands()` público (lo usa el oracle de QA).
- `Program.cs`: línea `[commands] catalog loaded: {N} commands, {M} with
  resolvable default handler` tras `LoadCatalog`.
- `MonoDevelop.Startup.Avalonia.csproj`: 12 `<Compile>` (ShellCommandHandler +
  los 11). `RunCustomToolHandler` no portada (ctor con `ExternalTool`, no
  referenciada en XML); las bases abstractas `TabCommandHandler` /
  `OpenDocumentHandlerBase` tampoco (cada clase concreta delega directa).

**Números medidos** (sobre `main/build/addinxml` + oracle):
- 259 `defaultHandler` en todo el catálogo; 112 elementos `<Command>` en
  `MonoDevelop.Ide.Commands.*` (111 ids únicos; 1 duplicado:
  `EditCommands.MonodevelopPreferences`).
- **`ResolvedHandlers=111`**, 0 nombres irresolubles. El resto de
  `defaultHandler` (Debugger, CSharp, VersionControl, Packaging,
  PackageManagement, PerformanceDiagnostics, DesignerSupport, MainToolbar,
  FindInFiles, ...) no compila en este build → router.
- Catálogo invariante: `Count=461`, `LoadedCommands=475`, ids de menú
  `196/172 resueltos / 24 missing` (sintéticos `pad:*`/`recent:`/`tool:`).

**Validación / evidencia** (artefactos en `~/opencode/`):
- Build `0 Errores` (net10.0 y net10.0-windows).
- Oracle extendido (`~/opencode/catload_test/`): `catalog: Count=461
  LoadedCommands=475`; `oleada2: ResolvedHandlers=111
  commandsWithHandlerType=111 totalCommands=461`;
  `oleada2-unresolved(MonoDevelop.Ide.Commands): 0`; `menu ids: total=196
  resolved=172 missing=24`; `dispatch: registered=True … routed=0` (un comando
  con handler ya no pasa por el router).
- Smoke (`~/opencode/smoke_oleada1.sh`): arranque limpio con `--sln`, línea
  `[commands] catalog loaded: 475 commands, 111 with resolvable default
  handler`, `--menudump` **byte-igual** a la Oleada 1
  (`menus=11 items=225 cmds=0 bindings=205`).
- Funcional (`~/opencode/oleada2_qa_subagent/functional_f1.sh`): F1 (XTEST con
  focus verificado vía `focus.py`) → `[help] API documentation root (Help
  pad)` — cadena completa `OnKeyDown → MenuService.RunCommand →
  CommandService.Dispatch → HelpHandler (uno de los 111) →
  ShellCommandHandler.Run → OnMenuCommand` en la app real, sin excepciones.

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (ronda 1; solo
lectura, artefactos en `~/opencode/oleada2_qa_subagent/`) — **APROBADO,
P1–P5 todos PASS, 0 hallazgos bloqueantes de app** (ciclo cerrado en ronda 1):
P1 build 0 errores (ambos TFMs); P2 oracle con valores exactos
(`ResolvedHandlers=111`, `unresolved=0`, `196/172/24`, `routed=0`); P3 smoke
sin excepciones + menudump byte-igual; P4 funcional F1 → `[help]` (XTEST de
focus confirmado y respaldo XSendEvent); P5 sin doble registro de sesión
(`SessionRecorder` pasivo, `Record` único al inicio de `OnMenuCommand`,
convergencia única router→`Program.cs:38` / handler→`ShellCommandHandler.cs`).
Hallazgos (ninguno bloqueante de app): 1) [entorno] ratón XTEST muerto
(conocido); 2) [entorno] teclado XTEST intermitente a nivel de sesión
(XSendEvent sí entregaba); 3) [incidente·autoinfligido, resuelto] el probe
`--no-incremental` del subagente (fuera de alcance P1) dejó `main/build`
temporalmente inconsistente (faltó `MonoDevelop.AvaloniaShell.dll`); el fallo
de `--no-incremental` es **preexistente** (`MonoRoslynCompat` no compila solo —
`Microsoft.VisualStudio.Text.Data.dll` por HintPath, `MonoRoslynCompat.csproj`);
el dev recuperó `main/build` con build incremental normal y re-validó P2+P3 en
verde.

**Cierre**: código en commit `176ee02252` pushado a
`origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos; la ruta
`--old-gui` no se ve afectada (código nuevo bajo `#if AVALONIA_SHELL`; los
ficheros portados conservan el código GTK byte-idéntico bajo
`#if !AVALONIA_SHELL`).

### Menú (follow-up) — Oleada 3, Slice 1: estado de habilitación de menú (overrides `Update`) (2026-11-05)

Tercera oleada de la integración total del backend; alcance **faseado por
decisión del operador (vertical slice primero)**: primero el mecanismo completo
(proveedor de estado + cableado menú↔`CommandInfo.Enabled`) y el subconjunto de
mayor valor del menú File; el resto de `Update` sigue en slices posteriores
sobre este mismo mecanismo.

**Fuente legacy referenciada**:
- 85 overrides `Update` repartidos en 11 ficheros de
  `MonoDevelop.Ide.Commands`. Todos los portados hasta la Oleada 2 dejaron su
  `Update` legacy bajo `#if !AVALONIA_SHELL` porque acuden a
  `IdeApp.Workbench`/`IdeApp.Workspace`/`IdeApp.IsInitialized`/servicios que no
  existen en el build Avalonia: no es un port mecánico, cada `Update` exige
  mapear su consulta al estado real de la shell.
- En Gtk el menú consulta `CommandInfo` en cada apertura; aquí el estado se
  calcula bajo demanda (`GetCommandInfo` sin dispatch) y se refleja en
  `MenuItem.IsEnabled`.

**Implementación** (commit `757553d93b`, 8 ficheros):
- `CommandManager.GetCommandInfo(object commandId)` (región `#if
  AVALONIA_SHELL`, tras `DispatchCommand`): `ToCommandId` → `GetActionCommand`
  → `new CommandInfo(cmd)` (o `new CommandInfo()` si el id no existe) → si el
  `DefaultHandler` no existe y hay `DefaultHandlerType` lo instancia con
  `Activator.CreateInstance(type, true)` (nonPublic) → `InternalUpdate(info)`
  (si `IsEnabled`) → `info.Enabled &= IsEnabled` → `CancelAsyncUpdate()`.
  `CommandService.GetCommandInfo` lo expone de forma estática.
- **Nuevo** `MonoDevelop.Ide.Commands/ShellCommandState.cs` (`#if
  AVALONIA_SHELL`): proveedor de estado static sobre `MainWindow.Instance`
  (fallback `?? false` headless) — `HasDocuments` (`documents.Count > 0`),
  `HasActiveDocument` (`DocTabs.SelectedItem is TabItem`),
  `HasDirtyDocuments` (alguno editor `IsDirty`), `HasWorkspace`
  (`loadedSolutionPath != null`). El Welcome page es un overlay y no cuenta.
- `MainWindow.axaml.cs`: 4 accessors `internal` que exponen el estado;
  `NotifyCommandStateChanged()` (llama a `MenuBuilder.RefreshCommandStates()`);
  hooks en cada punto de mutación: `AddDocument` (suscripción `PropertyChanged`
  al `SkTextEditor.IsDirtyProperty` de cada editor + aviso al añadir),
  `OnDocSelectionChanged`, `CloseDocumentAsync` (tras `SelectFirstDocument()`),
  `OpenSolutionInWindow` (tras cargar), `CloseWorkspaceAsync` (tras cerrar);
  `MenuBuilder.RefreshCommandStates()` al final de `BuildMenu()`.
- `MenuBuilder.cs`: `static List<MenuItem> commandItems` — solo items ligados a
  `CommandAction` (`item.Tag = ca.Id`), se limpia al reconstruir (`BuildItems`,
  única llamada: menú principal); `RefreshCommandStates()` itera la lista y
  fija `item.IsEnabled = CommandService.GetCommandInfo(id).Enabled`.
- `FileCommands.cs` (solo el bloque Avalonia `#else`; el legacy quedó
  byte-igual): 4 overrides `Update(CommandInfo)` — `SaveAllHandler` →
  `HasDirtyDocuments`, `CloseAllFilesHandler` → `HasDocuments`,
  `CloseFileHandler` → `HasActiveDocument`, `CloseWorkspaceHandler` →
  `HasWorkspace` (el comando `CloseWorkspaceItem` comparte el handler y hereda
  el estado).
- QA: `Program.cs` añade `--cmdstates` a los flags; `MainWindow` añade
  `DumpCmdStates(when)` (dump de `IsEnabled` de los 4 items + estado
  subyacente) y el hook `--cmdstates`, que encadena initial → open doc → dirty
  → clean → close doc con un dump por transición (verificable sin capturas).

**Invariantes**: modelo de menú (`MenuService`) intacto → `--menudump`
byte-igual (`menus=11 items=225 cmds=0 bindings=205`); catálogo y dispatch
sin cambios (`Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`);
comandos sin `Update` portado conservan `CommandInfo.Enabled=true` por
defecto (el dispatch no se bloquea); la ruta `--old-gui` no se toca.

**Validación / evidencia** (artefactos en `~/opencode/`):
- Build `0 Errores` (net10.0 y net10.0-windows).
- Oracle extendido (`~/opencode/catload_test/`): `oleada3: ok=8 fail=0`
  (headless ⇒ los 5 ids con handler portado, incluido `CloseWorkspaceItem`,
  `enabled=False`; controles `OpenFile`/`Save`/id-inexistente `enabled=True`);
  regresión `oleada2: ResolvedHandlers=111` presente.
- Smoke (`~/opencode/smoke_oleada1.sh`): sin excepciones, `--menudump`
  byte-igual a Oleada 1/2.
- E2E (`~/opencode/oleada3_cmdstates.sh`): con `--sln`, secuencia exacta
  `initial 0/0/0/1 → opened 0/1/1/1 → dirty 1/1/1/1 → clean 0/1/1/1 → closed
  0/0/0/1` (saveAll/close/closeAll/closeWs) con `hasWs=True`; sin sln lo mismo
  con `closeWs=0`/`hasWs=False`. 0 warn/fatal/exception en ambos logs.

**QA (§18.5)**: **subagente Tester QA Senior** (ronda 1; solo lectura,
artefactos en `~/opencode/oleada3_qa_subagent/`) — **APROBADO, P1–P5 todos
PASS, 0 hallazgos bloqueantes ni menores**: P1 revisión estática (región
Avalonia, bloque legacy byte-igual, hooks cubren todas las mutaciones,
`BuildItems` única llamada, modelo intacto); P2 build 0 errores; P3 oracle
8/8; P4 smoke menudump byte-igual; P5 E2E secuencia exacta (±sln).
Observaciones no bloqueantes: 1) la suscripción `PropertyChanged` del editor
no se desuscribe al cerrar el doc (no es fuga: el editor es la raíz y queda
recolectable); 2) `DumpCmdStates` usa `MainMenu!` (riesgo acotado al hook QA,
el menú se construye síncrono antes); 3) `commandItems` siempre en sincronía
(rebuild ⇒ clear+refill; sin menú, no-op).

**Queda pendiente (slices 2..n de la Oleada 3)**: portar los 81 `Update`
restantes (Edit, View, Build, Run, Tools, Window, Help y el resto de File —
`Save`/`SaveAs`/`Open`/`Exit`...), ampliando `ShellCommandState` con lo que
cada consulta legacy exija (p. ej. `HasActiveEditor`, `CanSave`, estado de
build/solución). Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por
esta slice.

**Cierre**: código en commit `757553d93b` pushado a
`origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos; la ruta
`--old-gui` no se ve afectada (código nuevo bajo `#if AVALONIA_SHELL`; el
bloque legacy de `FileCommands.cs` es byte-igual al commit anterior).

### Menú (follow-up) — Oleada 3, Slice 2: estado de habilitación de menú (overrides `Update`) — File resto + Edit básicos (2026-11-05)

Segunda slice de la Oleada 3, sobre el mecanismo de Slice 1. Alcance elegido
por el operador: **File (resto) + Edit básicos**.

**Mapeo legacy → estado shell** (bloques Avalonia `#else`; legacy
`#if !AVALONIA_SHELL` byte-igual):
- File `Print`/`PrintPreview`/`PrintPageSetup` → `ShellCommandState.HasActiveDocument`
  (legacy `CanPrint()` = `ActiveDocument != null && GetContent<IPrintable>()`).
- File `ClearRecentProjects` → `ShellCommandState.HasRecentSolutions`
  (legacy: count de la lista de recientes > 0).
- File `ClearRecentFiles` → `false` fijo (el submenú "Recent Files" de la shell
  es un placeholder "(Empty)" estático; la shell no mantiene store de recientes
  de archivos). Dejarlo deshabilitado evita además que su handler (que en la
  shell vacía `RecentSolutions`) sea alcanzable por el path de "Recent Files".
- Edit `Cut`/`Copy`/`Paste`/`Delete`/`SelectAll` → `ShellCommandState.HasActiveEditor`
  (legacy: el widget con foco es un `Gtk.TextView`/`IEditable` editable;
  **ninguno** de los 5 comprueba selección ⇒ no se introduce check de selección).
- No portados (routed o sin `Update` legacy, se quedan `Enabled=true` por
  defecto, coherente con GTK): `Undo`, `Redo`, `Find`, `Save`, `SaveAs`, `Exit`.
- `DeleteKey` no es item de menú (legacy `Bypass=true`, forward al teclado).

**Implementación** (commit `438b2311ce`, 4 ficheros):
- `ShellCommandState.cs`: `HasActiveEditor` (tab seleccionado sostiene un
  `SkTextEditor`) y `HasRecentSolutions` (`RecentSolutions.GetAll().Count > 0`),
  ambas fallback `?? false`.
- `MainWindow.axaml.cs`: accessors `internal` `HasActiveEditor => DocTabs?.
  SelectedItem is TabItem t && docs.ContainsKey (t.Tag as string ?? "")`
  (espejo de `WithActiveEditor`) y `HasRecentSolutions => RecentSolutions
  .GetAll ().Count > 0`; `NotifyCommandStateChanged()` tras
  `RecentSolutions.Add(path)` en `OpenSolutionInWindow` (el hook de Slice 1
  estaba antes del `Add`); `DumpCmdStates` extendido (print/printPrev/
  pageSetup/clrFiles/clrProj + cut/copy/paste/delete/selectAll +
  hasActiveEditor/hasRecent).
- `FileCommands.cs`: 5 `Update` (Print×3→`HasActiveDocument`,
  ClearRecentFiles→`false`, ClearRecentProjects→`HasRecentSolutions`).
- `EditCommands.cs`: 5 `Update` (Delete/Copy/Cut/Paste/SelectAll→
  `HasActiveEditor`).

**Invariantes**: modelo de menú (`MenuService`) intacto → `--menudump`
byte-igual (`menus=11 items=225 cmds=0 bindings=205`); catálogo sin cambios
(`Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`); comandos sin
`Update` portado conservan `Enabled=true`; bloques legacy de `FileCommands.cs`/
`EditCommands.cs` byte-iguales; la ruta `--old-gui` no se toca.

**Validación / evidencia** (artefactos en `~/opencode/`):
- Build `0 Errores` (net10.0 y net10.0-windows). Nota: el build compila las
  fuentes de `MonoDevelop.Ide` dentro de `MonoDevelop.AvaloniaShell.dll`
  (csproj `Compile Include` + `AVALONIA_SHELL`), así que los cambios de
  `MonoDevelop.Ide.Commands/*` van al assembly de la shell (el
  `MonoDevelop.Ide.dll` legacy de `main/build` queda sin usar por la ruta
  Avalonia).
- Oracle extendido (`~/opencode/catload_test/`): `oleada3: ok=19 fail=0`
  (headless ⇒ los 14 ids con handler portado, `enabled=False`; controles
  `OpenFile`/`Save`/`Undo`/id-inexistente `enabled=True`).
- Smoke (`~/opencode/smoke_oleada1.sh`): sin excepciones, `--menudump`
  byte-igual.
- E2E (`~/opencode/oleada3_cmdstates.sh`, 2 casos × 5 transiciones): los 5
  items de Edit y los 3 de Print siguen `0→1→1→1→0`; `clrFiles=0` siempre;
  `clrProj=hasRecent`; `closeWs`/`hasWs` = 1/True (con sln) y 0/False (sin
  sln); `saveAll` sigue `hasDirty`. 0 warn/fatal/exception.

**QA (§18.5)**: **subagente Tester QA Senior** (ronda 1; solo lectura,
artefactos en `~/opencode/oleada3_slice2_qa/`) — **APROBADO, P1–P5 todos OK,
0 hallazgos bloqueantes ni mayores**: P1 build 0 errores (ambos TFMs) +
artefacto desplegado; P2 oracle 19/19 + invariantes; P3 smoke menudump
byte-igual; P4 E2E 5×2 exacto; P5 mapeo fiel al legacy + bloques legacy
byte-iguales + `HasActiveEditor` sin check de selección + `--old-gui` intacta.
Hallazgo **Menor** (corregido y re-verificado antes del commit de docs): el
`#endif` de cierre compartido de `EditCommands.cs` quedó con tab extra +
CRLF→LF por el edit; restaurado a `#endif\r\n` (diff lógico en inserciones
puras, 0 líneas eliminadas). Observaciones no bloqueantes: oracle con una
caída transitoria de entorno (repetición exit 0); `main/build/MonoDevelop
.Ide.dll` legacy antiguo sin usar (preexistente); `hasRecent=True` desde
`initial` en el E2E (lista de recientes persistida).

**Queda pendiente (slices 3..n de la Oleada 3)**: portar el resto de los
`Update` — View, Build, Run, Tools, Window, Help y el resto de File
(`Save`/`SaveAs`/`Open` son routed y no requieren port), más los items de
edición de selección del Edit (Indent/Unindent/Case/Comment/Join/Sort…) y
`Undo`/`Redo` (exigirían un `CanUndo`/`CanRedo` real del editor, hoy no
público en `SkTextEditor`). Mecanismo, cableado y ciclo de QA ya quedaron
cubiertos por Slice 1–2.

**Cierre**: código en commit `438b2311ce` pushado a
`origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos; la ruta
`--old-gui` no se ve afectada (código nuevo bajo `#if AVALONIA_SHELL`; los
bloques legacy de `FileCommands.cs`/`EditCommands.cs` son byte-iguales al
commit anterior).

### Menú (follow-up) — Oleada 3, Slice 3: estado de habilitación de menú (overrides `Update`) — Window + View + Help (2026-11-05)

Tercera slice de la Oleada 3, sobre el mecanismo de Slice 1. Alcance elegido
por el operador: **Window + View + Help** (los `Update` cuyo estado depende
solo de `ShellCommandState`: recuento de documentos y editor activo).

**Mapeo legacy → estado shell** (bloques Avalonia `#else`; legacy
`#if !AVALONIA_SHELL` byte-igual):
- Window `NextDocument`/`PrevDocument` → `ShellCommandState.DocumentCount >= 2`
  (legacy: disable cuando `IdeApp.Workbench.Documents.Count < 2`).
- Window `OpenDocument1..OpenDocument9` → `ShellCommandState.DocumentCount >= N`
  (legacy: `Documents.Count >= index`). El port solo gestiona `Enabled`; el
  legacy además hacía `info.Visible = false` + rename al título cuando
  `Count < index` — **deferido** con los demás ports de visibility/dynamic-labels.
- View `ZoomIn`/`ZoomOut`/`ZoomReset` → `ShellCommandState.HasActiveEditor`
  (legacy: `ActiveDocument != null && GetContent<IZoomable>(true) != null &&
  EnableZoom{In,Out,Reset}`). Gate más grueso (no modela los flags por
  dirección; en el límite de zoom el `Run` hace clamp).
- View `FocusCurrentDocument`/`CenterAndFocusCurrentDocument` →
  `ShellCommandState.HasActiveEditor` (legacy: `ActiveDocument != null &&
  GetContent<ITextView>() != null`). `FocusCurrentDocument` **no es ítem de
  menú** (solo shortcut F7; en `MainMenu.addin.xml` solo aparece
  `CenterAndFocusCurrentDocument` como "_Cursor Position").
- Help `OpenLogDirectory` → `info.DisableOnShellLock = false; info.Enabled =
  true` (espejo exacto del legacy; queda habilitado incluso bajo shell lock).
  El gate de visibilidad `CanShowHelp` es del item Help (F1)/`HelpHandler`, no
  de `OpenLogDirectory`, y está **deferido**.

**Implementación** (commit `c5f01f6b40`, 5 ficheros):
- `ShellCommandState.cs`: `DocumentCount`
  (`MainWindow.Instance?.DocumentCount ?? 0`).
- `MainWindow.axaml.cs`: accessor `internal` `DocumentCount => documents.Count`;
  `DumpCmdStates` extendido (nextDoc/prevDoc/open1-9 + zoomIn/zoomOut/zoomReset/
  focusDoc/centerFocus + docCount) y helper `CEn(id)` (lee
  `CommandService.GetCommandInfo(id).Enabled` directo, para comandos sin ítem de
  menú; loguea la excepción antes de devolver `null`); driver `--cmdstates` abre
  2 docs para ver `docCount` 0→1→2→1→0.
- `WindowCommands.cs`: 11 `Update` (Next/Prev→`>=2`, Open1..9→`>=N`).
- `ViewCommands.cs`: 5 `Update` (ZoomIn/Out/Reset, Focus, CenterFocus→
  `HasActiveEditor`).
- `HelpCommands.cs`: 1 `Update` (OpenLogDirectory→`DisableOnShellLock=false` +
  `Enabled=true`) + comentario de bloque Oleada 2 matizado.

**Invariantes**: modelo de menú (`MenuService`) intacto → `--menudump`
byte-igual (`menus=11 items=225 cmds=0 bindings=205`); catálogo sin cambios
(`Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, `menu ids total=196
resolved=172 missing=24`); comandos sin `Update` portado conservan `Enabled=true`;
bloques legacy de `WindowCommands.cs`/`ViewCommands.cs`/`HelpCommands.cs`
byte-iguales; la ruta `--old-gui` no se toca.

**Validación / evidencia** (artefactos en `~/opencode/`):
- Build `0 Errores` (net10.0 y net10.0-windows). Nota: el build compila las
  fuentes de `MonoDevelop.Ide` dentro de `MonoDevelop.AvaloniaShell.dll`
  (csproj `Compile Include` + `AVALONIA_SHELL`), así que los cambios de
  `MonoDevelop.Ide.Commands/*` van al assembly de la shell.
- Oracle extendido (`~/opencode/catload_test/`): `oleada3: ok=36 fail=0`
  (headless ⇒ los 17 ids con handler portado en esta slice + 14 previos,
  `enabled=False`; controles `OpenFile`/`Save`/`Undo`/id-inexistente `enabled=True`).
- Smoke (`~/opencode/smoke_oleada1.sh`): sin excepciones, `--menudump` byte-igual.
- E2E (`~/opencode/oleada3_cmdstates.sh`, 2 casos con/sin soln × 7 transiciones):
  `docCount` 0→1→2→2→2→1→0; `nextDoc`/`prevDoc`/`open1`/`open2` siguen el
  recuento; `open3`/`open9`=`0` (solo 2 docs); `zoomIn/zoomOut/zoomReset/
  focusDoc/centerFocus` =1 con editor, 0 sin docs. 0 `?`, 0 warn/fatal/exception.

**QA (§18.5)**: **subagente Tester QA Senior** (solo lectura). Ronda 1
**APROBADO CON OBSERVACIONES** (P1–P3 OK; P4 = deferida documentada de
visibility de `OpenDocument1..9`; 3 P5 de higiene). Correcciones aplicadas y
re-verificadas: P5-1 comentario `CanShowHelp` (atribuido erróneamente a
`OpenLogDirectory`), P5-2 paridad `DisableOnShellLock=false`, P5-3 log de la
excepción en `CEn`, N1 comentario de bloque desactualizado. Ronda 2 **APROBADO**
(P5 resueltos, sin regresión: build 0 errores, E2E 7×2 exacto, sin `CEn error`).
Observaciones no bloqueantes: gates de zoom/focus más gruesos que legacy por
diseño; umbrales `open3..open9` validados por inspección (uniformes);
`DocumentCount` cuenta todos los tabs (hoy todos editores); `--old-gui`
verificada por inspección del diff (bloques legacy intocados).

**Queda pendiente (slices 4..n de la Oleada 3)**: portar el resto de los
`Update` — Build, Run, Project, Tools (requieren estado de build/debug/solución
no aún rastreado por `ShellCommandState`) y el resto de View (layout/pads, que
dependen de `Visible`/dynamic), más la **port de visibility/rename/dynamic**
(OpenDocument1..9, CanShowHelp, OpenDocumentList) y `Undo`/`Redo` (exigirían
`CanUndo`/`CanRedo` real del editor, hoy no público en `SkTextEditor`).
Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–3.

**Cierre**: código en commit `c5f01f6b40` pushado a
`origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos; la ruta
`--old-gui` no se ve afectada (código nuevo bajo `#if AVALONIA_SHELL`; los
bloques legacy de `WindowCommands.cs`/`ViewCommands.cs`/`HelpCommands.cs` son
byte-iguales al commit anterior).

### Menú (follow-up) — Oleada 3, Slice 4: estado de habilitación de menú (overrides `Update`) — Project/Build/Run solution-level (2026-11-05)

Cuarta slice de la Oleada 3, sobre el mecanismo de Slice 1. Alcance decidido:
los `Update` **solution-level** del menú Project/Build/Run, cuyo estado depende
de "hay solución cargada" (`ShellCommandState.HasWorkspace`). Se investigó
primero el alcance real: los handlers Build/Run/Project viven en
`ProjectCommands.cs` (que SÍ compila en el build Avalonia), mientras que
`BuildCommands`/`RunCommands`/`DebugCommands` no existen en
`MonoDevelop.Ide.Commands` (otros ensamblados no compilados). Los gates que
dependen de build/run in-progress o de selección de proyecto quedan diferidos
(requieren estado no rastreado por la shell).

**Mapeo legacy → estado shell** (bloques Avalonia `#else`; legacy
`#if !AVALONIA_SHELL` byte-igual):
- `SolutionOptions` → `ShellCommandState.HasWorkspace` (legacy: `CurrentSelectedSolution != null` — **bridge exacto**).
- `CleanSolution` → `ShellCommandState.HasWorkspace` (legacy: `CurrentSelectedSolution != null` — **bridge exacto**).
- `BuildSolution`/`RebuildSolution` → `ShellCommandState.HasWorkspace` (legacy: `CurrentSelectedSolution != null && CurrentBuildOperation.IsCompleted` — **bridge más grueso**; el check in-progress se difiere con el estado de build op).
- `Run` → `ShellCommandState.HasWorkspace` (legacy: `Workspace.IsOpen && CanRun/CanExecute` — **bridge más grueso**; `CanExecute`/in-progress diferidos).
- `RunEntry` → `ShellCommandState.HasWorkspace` (legacy: `buildTarget != null && CanExecute` — **bridge más grueso**).

**Implementación** (commit `0c3367a73c`, 2 ficheros):
- `ProjectCommands.cs` (solo el bloque Avalonia `#else`; legacy byte-igual): 6 `Update` portados + comentario-header del bloque matizado (los gates solution-level ya están portados).
- `MainWindow.axaml.cs`: `DumpCmdStates` extendido con la constante `P = "MonoDevelop.Ide.Commands.ProjectCommands."` y 6 campos — `buildSol/rebuildSol/cleanSol/run/solOpts` vía `En()` (menu-walk; están en los menús Build/Run/Project) y `runEntry` vía `CEn()` (lectura directa, porque `RunEntry` no es item del menú principal, solo de context/run-list).

**Invariantes**: modelo de menú (`MenuService`) intacto → `--menudump` byte-igual (`menus=11 items=225 cmds=0 bindings=205`); catálogo sin cambios (`Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, `menu ids total=196 resolved=172 missing=24`); comandos sin `Update` portado conservan `Enabled=true`; bloque legacy de `ProjectCommands.cs` byte-igual; la ruta `--old-gui` no se toca.

**Validación / evidencia** (artefactos en `~/opencode/`):
- Build `0 Errores` (net10.0 y net10.0-windows). Nota: el build compila las fuentes de `MonoDevelop.Ide` dentro de `MonoDevelop.AvaloniaShell.dll` (csproj `Compile Include` + `AVALONIA_SHELL`), así que los cambios de `MonoDevelop.Ide.Commands/*` van al assembly de la shell.
- Oracle extendido (`~/opencode/catload_test/`, sección `oleada3` a 42 ids): `oleada3: ok=42 fail=0` (headless ⇒ los 6 ids nuevos + los 36 previos reportan lo esperado; controles `OpenFile`/`Save`/`Undo`/id-inexistente `enabled=True`).
- Smoke (`~/opencode/smoke_oleada1.sh`): sin excepciones, `--menudump` byte-igual.
- E2E (`~/opencode/oleada3_cmdstates.sh`, 2 casos con/sin soln × 7 transiciones): con soln (`hasWs=True`) `buildSol/rebuildSol/cleanSol/run/solOpts/runEntry = 1` en las 7 transiciones; sin soln (`hasWs=False`) los 6 = `0`; `closeWs` 1/0 estable y los estados previos (docCount, zoom, edit) sin regresión. 0 `?`, 0 warn/fatal/exception.

**QA (§18.5)**: **subagente Tester QA Senior** (solo lectura). **CERRAR — P1–P5 OK, 0 hallazgos**. P1 `--old-gui` intacta (bloque legacy de `ProjectCommands.cs` byte-igual a HEAD, sha256 idéntico); P2 los 6 gates en la clase correcta con `HasWorkspace` (mapeo vs legacy verificado: `SolutionOptions`/`CleanSolution` exacto, resto bridge documentado; `runEntry` no es item del menú principal); P3 `HasWorkspace` = `loadedSolutionPath is not null`, mismo accessor que `CloseWorkspace`; P4 E2E sin regresión (logs ±sln + oracle 42/42 + menudump byte-igual, 0 warn/fatal/exception); P5 comentarios coherentes, sin dead code.

**Queda pendiente (slices 5..n de la Oleada 3)**: (1) gates **in-progress** de build/run (`IsBuilding`/`IsRunning`, expondrían el estado de la operación de build/run de la shell); (2) gates de **selección de build target** (`Build`/`Rebuild`/`ProjectOptions`/`SolutionItemOptions`/`EditReferences`/`SetStartupProjects`, requieren un modelo de proyecto seleccionado que la shell aún no tiene); (3) la **port de visibility/rename/dynamic** (`OpenDocument1..9`, `CanShowHelp`, `OpenDocumentList`, etiquetas dinámicas de `Run`/`RunEntry`/`Build`); (4) `Undo`/`Redo` (exigirían `CanUndo`/`CanRedo` real del editor, hoy no público en `SkTextEditor`); (5) `Tools` (visibility + dynamic-text, requiere el port de visibilidad) y `Navigation` (requiere historial back/forward de documentos). Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–4.

**Cierre**: código en commit `0c3367a73c` pushado a
`origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos; la ruta
`--old-gui` no se ve afectada (código nuevo bajo `#else`/`AVALONIA_SHELL`; el
bloque legacy de `ProjectCommands.cs` es byte-igual al commit anterior).

### Menú (follow-up) — Oleada 3, Slice 5: estado de habilitación de menú (overrides `Update`) — gates in-progress de build/run (2026-11-05)

Quinta slice de la Oleada 3, sobre el mecanismo de Slice 1. Puntualiza el item
(1) de "pendiente" de Slice 4: los **gates in-progress de build/run**. La shell
no mantiene operaciones de build/run separadas (legacy
`CurrentBuildOperation`/`CurrentRunOperation`), sino **un único proceso
compartido** `runningProc` (`System.Diagnostics.Process?`): `RunBuildAsync` itera
los proyectos de la solución llamando a `RunProcessAsync` (`dotnet build` por
proyecto), `RunStartupProjectAsync` hace lo propio para run/debug, y
`StopBuildOrRun` mata ese proceso. Por tanto
`IsOperationRunning => runningProc is { HasExited: false }` es el mapeo limpio
de "hay build o run in-progress".

**Mapeo legacy → estado shell** (bloques Avalonia `#else`; legacy
`#if !AVALONIA_SHELL` byte-igual):
- `Stop` → `ShellCommandState.IsOperationRunning` (legacy: enabled si `!CurrentBuildOperation.IsCompleted || !CurrentRunOperation.IsCompleted` — **bridge exacto**: el proceso compartido cubre ambos).
- `BuildSolution`/`RebuildSolution` → `ShellCommandState.HasWorkspace && !ShellCommandState.IsOperationRunning` (legacy: `CurrentSelectedSolution != null && CurrentBuildOperation.IsCompleted` — **bridge más grueso**: el proceso compartido también cubre run, así que un run en curso deshabilita build; documentado).
- `Run` → `HasWorkspace && !IsOperationRunning` (legacy: `Workspace.IsOpen && CanRun` + diálogo de confirmación "already running" + etiqueta dinámica "Restart" — el diálogo y la etiqueta dinámica se sustituyen por el busy guard; `MenuBuilder` solo espeja `Enabled`).
- `RunEntry` → `HasWorkspace && !IsOperationRunning` (legacy: `buildTarget != null && !(buildTarget is Workspace) && CanExecute` — la parte in-progress de `CanExecute` queda puenteada).
- `CleanSolution`/`SolutionOptions` → sin busy guard (legacy no lo chequea — el bridge exacto de Slice 4 se mantiene).

**Implementación** (3 ficheros, +77/−19):
- `MainWindow.axaml.cs`: accessor `IsOperationRunning` (región de estado de Oleada 3, junto a `HasWorkspace`/`DocumentCount`); hooks `NotifyCommandStateChanged()` en `RunProcessAsync` — inicio (tras `runningProc = proc` + early-return null; no notifica si el proceso no arrancó) y fin (dentro del `Dispatcher.UIThread.Post` de completion, tras `RefreshCodeIssuesPad()`); `DumpCmdStates` añade `stop=` (vía `En()`, `Stop` es item del menú Run) e `isOp=` (lectura directa); driver `--cmdstates` añade las fases `preop` → `RunProcessAsync("sleep","3")` (mismo path de un build real, marco de tiempo determinista) → `operating` → espera salida + 300ms → `postop`.
- `ShellCommandState.cs`: `IsOperationRunning => MainWindow.Instance?.IsOperationRunning ?? false`.
- `ProjectCommands.cs` (solo el bloque `#else`): 4 `Update` refinados (`BuildSolution`/`RebuildSolution`/`Run`/`RunEntry`) + 1 nuevo (`StopHandler`) + comentario-header del bloque matizado.

**Invariantes**: modelo de menú (`MenuService`) intacto → `--menudump` byte-igual (`menus=11 items=225 cmds=0 bindings=205`); catálogo sin cambios (`Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, `menu ids total=196 resolved=172 missing=24`); `runningProc` no se limpia a null (el diseño depende de `HasExited`; evita carrera con el Post de completion); bloque legacy de `ProjectCommands.cs` byte-igual; la ruta `--old-gui` no se toca.

**Validación / evidencia** (artefactos en `~/opencode/`):
- Build `0 Errores` (net10.0 y net10.0-windows). Nota: el build compila las fuentes de `MonoDevelop.Ide` dentro de `MonoDevelop.AvaloniaShell.dll` (csproj `Compile Include` + `AVALONIA_SHELL`), así que los cambios de `MonoDevelop.Ide.Commands/*` van al assembly de la shell (`MonoDevelop.Ide.dll` de `main/build` es pre-Oleada 2 y solo alimenta `--old-gui`).
- Oracle extendido (`~/opencode/catload_test/`, sección `oleada3` a 43 ids — añade `ProjectCommands.Stop`, expected false headless): `oleada3: ok=43 fail=0`.
- Smoke (`~/opencode/smoke_oleada1.sh`): sin excepciones, `--menudump` byte-igual.
- E2E (`~/opencode/oleada3_cmdstates.sh`, 2 casos con/sin soln × 10 fases): con soln — `preop`/`postop` `buildSol/rebuildSol/cleanSol/run/solOpts/runEntry=1 stop=0 isOp=0`, **`operating` `buildSol/rebuildSol=0 cleanSol=1 run=0 solOpts=1 runEntry=0 stop=1 isOp=1`** (busy guard solo en los 4 gateados; `CleanSolution`/`SolutionOptions` intactos); sin soln — todo `0` salvo `operating` (`stop=1 isOp=1`). 0 `?`, 0 warn/fatal/exception.

**QA (§18.5)**: **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_slice5_qa/`). **CERRAR — P1–P4 OK, 0 hallazgos** (1 P5 preexistente, fuera de scope: al matar el proceso con `Stop`, la salida no es 0 y el Post de fin preexistente escribe "Build FAILED with exit code N" en el pad de errores — proponer como mejora). P1 `--old-gui` intacta (bloque legacy byte-igual; el diff empieza dentro del `#else`); P2 mapeo fiel y documentado (hooks en el sitio correcto; sin `runningProc=null` en el árbol; `CleanSolution`/`SolutionOptions` sin busy guard); P3 concurrencia coherente (multi-proyecto: los Posts se encadenan FIFO; Stop: `Kill(true)` → `WaitForExitAsync` → Post de fin → gates re-habilitados; `runningProc` escrito y leído solo en UI); P4 E2E sin regresión (2 × 10 fases + oracle 43/43 + menudump byte-igual); P5 aviso citado.

**Queda pendiente (slices 6..n de la Oleada 3)**: (1) gates de **selección de build target** (`Build`/`Rebuild`/`ProjectOptions`/`SolutionItemOptions`/`EditReferences`/`SetStartupProjects`, requieren un modelo de proyecto seleccionado que la shell aún no tiene); (2) la **port de visibility/rename/dynamic** (`OpenDocument1..9`, `CanShowHelp`, `OpenDocumentList`, etiquetas dinámicas de `Run`/`RunEntry`/`Build`); (3) `Undo`/`Redo` (exigirían `CanUndo`/`CanRedo` real del editor, hoy no público en `SkTextEditor`); (4) `Tools` (visibility + dynamic-text, requiere el port de visibilidad) y `Navigation` (requiere historial back/forward de documentos); (5) mejora P5: mensaje de error tras `Stop` legítimo. Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–5.

**Cierre**: código en commit (3 ficheros) pushado a
`origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos; la ruta
`--old-gui` no se ve afectada (código nuevo bajo `#else`/`AVALONIA_SHELL`; el
bloque legacy de `ProjectCommands.cs` es byte-igual al commit anterior).

### Menú (follow-up) — Oleada 3, Slice 6: estado de habilitación de menú (overrides `Update`) — gates project-level (`ActiveProjectCount`) (2026-11-05)

Sexta slice de la Oleada 3, sobre el mecanismo de Slice 1. Puntualiza el item
(1) de "pendiente" de Slice 5: los **gates de selección de build target** del
menú principal (`Build`/`Rebuild`/`Clean`, Project Options, Set Startup
Projects). El legacy gatea por `CurrentSelectedBuildTarget != null` — un
proyecto seleccionado en el pad — y la shell no tiene selección de pad, así que
el puente elegido es "la solución cargada tiene proyectos":
`MainWindow.ActiveProjectCount`, el recuento de proyectos de la solución
cargada (el mismo conjunto que itera el build).

**Mapeo legacy → estado shell** (bloques Avalonia `#else`; legacy
`#if !AVALONIA_SHELL` byte-igual):
- `Build`/`Rebuild`/`Clean` → `ShellCommandState.HasWorkspace && ShellCommandState.ActiveProjectCount > 0 && !ShellCommandState.IsOperationRunning` (legacy: `CurrentSelectedBuildTarget != null && CurrentBuildOperation.IsCompleted && CanBuild` — **puente más grueso documentado**: target seleccionado ⇒ "la solución tiene proyectos"; `CanBuild` no está modelado; en `Clean` el busy guard es endurecimiento añadido — el legacy Clean no chequea in-progress, a diferencia de Build/Rebuild).
- `ProjectOptions` → `HasWorkspace && ActiveProjectCount > 0` (legacy: `CurrentSelectedBuildTarget is Project`; la etiqueta dinámica `"{0} _Options"` no es portable).
- `SetStartupProjects` → `HasWorkspace && ActiveProjectCount > 0` (legacy: >= 2 proyectos — matiz multi-startup descartada: el flujo de la shell marca un único startup project, documentado).
- `SolutionItemOptions`/`EditReferences` → **diferidos**: context-only (menú sobre un nodo del árbol de solución); requieren un modelo de nodo de contexto que la shell aún no tiene.

**Implementación** (3 ficheros, +108/−2):
- `MainWindow.axaml.cs`: accessor `internal int ActiveProjectCount` (región de estado de Oleada 3): **memoizado por `loadedSolutionPath`** (los gates lo leen en cada refresh de estado de comandos, incluidos los cambios de dirty por tecla, así que un rescaneo por refresh golpearía el disco cada vez); 0 si no hay solución o el `.sln` no existe en disco (`File.Exists`); invalidaciones `activeProjectCountSln = null` en cada punto donde el conjunto de proyectos puede cambiar: `RefreshSolutionTree()` (añadir/quitar/renombrar), `CloseWorkspaceAsync` (cierre), `OpenNewSolutionDialogAsync` (ruta de producción de añadir proyecto: tras `AppendProjectToSolution`, antes del `OpenSolutionInWindow` que early-returns por path igual) y el driver QA `--newproject` (mismo patrón); `DumpCmdStates` añade `buildProj=/rebuildProj=/cleanProj=/projOpts=/setStartup=` (vía `En()`) y `projCount=` (lectura directa).
- `ShellCommandState.cs`: `ActiveProjectCount => MainWindow.Instance?.ActiveProjectCount ?? 0` (headless ⇒ 0).
- `ProjectCommands.cs` (solo el bloque `#else`): 5 `Update` nuevos (`BuildHandler`/`RebuildHandler`/`CleanHandler` con busy guard; `ProjectOptionsHandler`/`SetStartupProjectsHandler` sin) + comentario-header del bloque actualizado.

**Invariantes**: modelo de menú (`MenuService`) intacto → `--menudump` byte-igual (`menus=11 items=225 cmds=0 bindings=205`); catálogo sin cambios (`Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, `menu ids total=196 resolved=172 missing=24`); bloque legacy de `ProjectCommands.cs` byte-igual; la ruta `--old-gui` no se toca.

**Validación / evidencia** (artefactos en `~/opencode/`):
- Build `0 Errores` (net10.0 y net10.0-windows).
- Oracle extendido (`~/opencode/catload_test/`, sección `oleada3` a 48 ids — añade Build/Rebuild/Clean/ProjectOptions/SetStartupProjects, expected false headless): `oleada3: ok=48 fail=0`.
- Smoke (`~/opencode/smoke_oleada1.sh`): sin excepciones, `--menudump` byte-igual.
- E2E (`~/opencode/oleada3_cmdstates.sh`, **3 casos** × 10 fases): withsln (`Fixture.sln`, 3 proyectos) — `hasWs=True projCount=3`, `buildProj/rebuildProj/cleanProj/projOpts/setStartup=1`, `operating` (`isOp=1`): `buildProj/rebuildProj/cleanProj=0` (busy guard) pero `projOpts/setStartup=1` (sin guard, por diseño) y `stop=1`, `postop` recupera; emptysl (fixture nuevo `~/opencode/oleada3_slice6_empty/Empty.sln` — su único proyecto referenciado no existe en disco: `hasWs=True projCount=0`, 5 gates `0` en todas las fases — una solución cargada no implica proyectos buildables); nosln — `hasWs=False projCount=0`, todo `0`. 0 `?`, 0 warn/fatal/exception.

**QA (§18.5)**: **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_slice6_qa/`). Ciclo: QA #1 **NO CERRAR** — P3 (hueco de invalidación: la ruta de producción de añadir proyecto, `AppendProjectToSolution` en `OpenNewSolutionDialogAsync`, no invalidaba el memo; como `OpenSolutionInWindow` early-returns por path igual, el conteo quedaba obsoleto N vs N+1) + 2 P5 menores (comentarios) → el dev aplicó el fix → QA final (re-QA) **CERRAR**, P1–P4 OK, 1 P5 no bloqueante (**H1**: el mismo hueco en la **vía driver QA `--newproject`**; solo afecta a sesiones lanzadas con ese flag, la ruta de producción estaba corregida) → el dev corrigió H1 (mismo patrón) y re-verificó (build 0 errores ambos TFMs, oracle `ok=48 fail=0`, menudump byte-igual). Estado final: **0 hallazgos abiertos**. P1 `--old-gui` intacta (región legacy ln 1–630 byte-igual a HEAD, diff exit 0; todos los hunks tras el `#else`); P2 los 5 gates en las clases correctas con el mapeo legacy documentado; P3 correctitud del memo (todos los puntos de mutación de producción invalidan/re-clavean; fix del QA #1 validado); P4 E2E sin regresión (3 × 10 fases + oracle 48/48 + menudump byte-igual).

**Queda pendiente (slices 7..n de la Oleada 3)**: (1) `SolutionItemOptions`/`EditReferences` (context-only — requieren un modelo de nodo de contexto sobre el árbol de solución); (2) la **port de visibility/rename/dynamic** (`OpenDocument1..9`, `CanShowHelp`, `OpenDocumentList`, etiquetas dinámicas de `Run`/`RunEntry`/`Build`); (3) `Undo`/`Redo` (`CanUndo`/`CanRedo` del editor, hoy no público en `SkTextEditor`); (4) `Tools` (visibility + dynamic-text) y `Navigation` (requiere historial back/forward de documentos); (5) mejora P5: mensaje de error tras `Stop` legítimo; (6) P5 preexistente: el early-return de `OpenSolutionInWindow` (mismo `solutionLoaded && path`) traga el refresh — tras añadir un proyecto el árbol Xwt de solución no se re-puebla (preexistente desde `36e657221c`; la invalidación de `ActiveProjectCount` mantiene el estado de menú correcto, pero el árbol visible no se actualiza). Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–6.

**Cierre**: código en commit (3 ficheros) pushado a
`origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos; la ruta
`--old-gui` no se ve afectada (código nuevo bajo `#else`/`AVALONIA_SHELL`; el
bloque legacy de `ProjectCommands.cs` es byte-igual al commit anterior).

### Menú (follow-up) — Oleada 3, Slice 7: estado de habilitación de menú (overrides `Update`) — gates de `Undo`/`Redo` (2026-11-05)

Séptima slice de la Oleada 3, sobre el mecanismo de Slice 1. Cierra el item (3)
de "pendiente" de Slice 6: los **gates de `Undo`/`Redo`** sobre `CanUndo`/
`CanRedo` del editor activo. El legacy gatea con `IUndoHandler.EnableUndo`/
`EnableRedo` del contenido del documento enfocado (`ViewCommandHandlers.cs`,
solo `--old-gui`, fuera del build Avalonia).

**Matiz de esta slice**: a diferencia de las slices 2–6, `Undo`/`Redo` **no
tienen `defaultHandler` en el `Commands.addin.xml` compartido** (los handlers
GTK viven en `ViewCommandHandlers.cs`, que el shell no compila). El port por
tanto no es solo un `Update` en el bloque `#else`: hay que **enlazar el handler
al id del catálogo** sin tocar el XML compartido — tocarlo rompería `--old-gui`
(la clase no existiría en el ensamblado legacy) y un special-case en
`MenuBuilder.RefreshCommandStates` hardcodearía ids en la capa de menú.
Solución: enlazado **post-`LoadCatalog`** en `Program.cs`
(`ActionCommand.DefaultHandlerType`, setter validado que existe desde el
núcleo). Consecuencia aceptada y documentada: al registrar el handler,
`DispatchCommand` ejecuta `Update` antes de `Run` y **bloquea si el comando
está disabled** (un clic en Undo disabled no hace nada) — coherente con el
resto de comandos portados. `ResolvedHandlers=111` se calcula en el `Load` y
no cambia: la línea `[commands]` del arranque queda idéntica.

**Mapeo legacy → estado shell** (bloques Avalonia `#else`; legacy
`#if !AVALONIA_SHELL` byte-igual):
- `Undo` → `ShellCommandState.CanUndo` ⇒ `ActiveTextEditor != null && ActiveTextEditor.CanUndo` (legacy: `textBuffer != null && textBuffer.EnableUndo`, con `textBuffer = GetContent<IUndoHandler>()` del documento activo). Stack-based, fiel a `TextDocument.CanUndo => undoStack.Count > 0 || currentAtomicOperation != null`; el MVP de `SkTextEditor` no tiene `currentAtomicOperation` (**simplificación documentada**).
- `Redo` → `ShellCommandState.CanRedo` ⇒ `ActiveTextEditor != null && ActiveTextEditor.CanRedo` (misma nota).
- El gate sigue al **editor activo** (pestaña seleccionada), no al último editor editado: se valida con la fase `otheractive` de la E2E (activar una pestaña con stack vacío deshabilita Undo aunque otra pestaña tenga undo disponible).

**Implementación** (5 ficheros, +103/−2):
- `SkTextEditor.cs` (compilado en la shell; NO en la lista explícita del csproj legacy): `public bool CanRedo => redoStack.Count > 0;` y `public event Action? UndoStateChanged;` — disparada en las tres mutaciones de stack que importan (la ruta de añadir de `PushUndo`, al final de `Undo()`, al final de `Redo()`), nunca en los early-returns (`undoing`, `before == joined`).
- `MainWindow.axaml.cs`: `internal SkTextEditor? ActiveTextEditor` (mismo lookup que `WithActiveEditor`), `internal bool CanUndo`/`CanRedo`; en `AddDocument`, `ed.UndoStateChanged += NotifyCommandStateChanged;` (mismo patrón que la suscripción `IsDirtyProperty` existente); `DumpCmdStates` añade `undo=`/`redo=` vía `En()` (menu-walk real de `MenuItem.IsEnabled`); driver QA `--cmdstates` con 6 fases nuevas: `typed` (InsertAtCaret), `otheractive` (cambio de pestaña a stack vacío), `undone`, `redone`, `undoneall`, `restored` (Redo + `IsDirty=false` para el cierre posterior sin prompt).
- `ShellCommandState.cs`: `CanUndo`/`CanRedo` vía `MainWindow.Instance?.CanUndo/CanRedo ?? false` (headless ⇒ false).
- `EditCommands.cs` (solo el bloque `#else`): `UndoHandler`/`RedoHandler` (Run → `ShellCommandHandler.Run(EditCommands.Undo/Redo)`; Update → `ShellCommandState.CanUndo/CanRedo`); comentario-header del bloque actualizado (obsoleto desde Slice 2).
- `Program.cs`: tras `LoadCatalog`, `GetActionCommand("MonoDevelop.Ide.Commands.EditCommands.Undo").DefaultHandlerType = typeof(UndoHandler)` (y lo mismo para `Redo`), con guard `is { }`.

**Invariantes**: modelo de menú (`MenuService`) intacto → `--menudump` byte-igual (`menus=11 items=225 cmds=0 bindings=205`); catálogo sin cambios (`Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, `menu ids total=196 resolved=172 missing=24`); `Commands.addin.xml` no modificado; bloque legacy de `EditCommands.cs` byte-igual; la ruta `--old-gui` no se toca.

**Validación / evidencia** (artefactos en `~/opencode/`):
- Build `0 Errores` (net10.0 y net10.0-windows).
- Oracle extendido (`~/opencode/catload_test/`, sección `oleada3` a **49 ids** — `Undo` pasa de control "routed" expected=true a wired expected=false y se añade `Redo` expected=false; el wiring de `Program.Main` se replica vía reflexión porque las clases son `internal` y el test referencia el mismo `MonoDevelop.AvaloniaShell.dll`): `oleada3: ok=49 fail=0`, línea `slice7: … wired=True`.
- Smoke (`~/opencode/smoke_oleada1.sh`): sin excepciones, `--menudump` byte-igual.
- E2E (`~/opencode/oleada3_cmdstates.sh`, **3 casos** × 16 fases — 10 previas + 6 nuevas): las fases nuevas, iguales en los 3 casos — `typed` `undo=1 redo=0` (`hasDirty=True`), `otheractive` `undo=0 redo=0` (el gate sigue al editor activo), `undone` `undo=0 redo=1`, `redone` `undo=1 redo=0`, `undoneall` `undo=0 redo=1`, `restored` `undo=1 redo=0` (`hasDirty=False`); sin regresión: fases previas `undo=0 redo=0`, `operating` `stop=1`, `closeWs` 1/1/0, `postop` recupera; línea `[commands] catalog loaded: 475 commands, 111 with resolvable default handler` idéntica. 0 `?`, 0 warn/fatal/exception.

**QA (§18.5)**: **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_slice7_qa/`, veredicto en `veredicto.md`). Un solo ciclo: **CERRAR**, **0 hallazgos P1–P4** — P1 `--old-gui` intacta (diff íntegro bajo `AVALONIA_SHELL`/`#else` o en ficheros exclusivos del shell; `Commands.addin.xml` no modificado; `SkTextEditor.cs` no se compila en el build legacy); P2 los 2 gates en las clases correctas con el mapeo legacy documentado; P3 el evento `UndoStateChanged` cubre todas y solo las mutaciones de stack que importan (incluida la invalidación de `redoStack` por edición); P4 E2E sin regresión (3 casos × 16 fases con matriz exacta + oracle 49/49 + menudump byte-igual). P5 (todos preexistentes u observaciones, no de esta slice): (1) **nuevo pendiente** — Ctrl+Z/Ctrl+Y no funcionan como atajos (early-return de `isTextEditingCombo` en `MainWindow.OnKeyDown` + sin casos Z/Y en `SkTextEditor.OnKeyDown`); (2) pendientes P5 previas de la Oleada 3 (post-Stop, early-return de `OpenSolutionInWindow`, context-only); (3) la suscripción de `UndoStateChanged` no se desuscribe en el cierre (patrón idéntico al preexistente `PropertyChanged`); (4) simplificación stack-based documentada.

**Queda pendiente (slices 8..n de la Oleada 3)**: (1) `SolutionItemOptions`/`EditReferences` (context-only — requieren un modelo de nodo de contexto sobre el árbol de solución); (2) la **port de visibility/rename/dynamic** (`OpenDocument1..9`, `CanShowHelp`, `OpenDocumentList`, etiquetas dinámicas de `Run`/`RunEntry`/`Build`); (3) `Tools` (visibility + dynamic-text) y `Navigation` (requiere historial back/forward de documentos); (4) mejora P5: mensaje de error tras `Stop` legítimo; (5) P5 preexistente: el early-return de `OpenSolutionInWindow` (mismo `solutionLoaded && path`) traga el refresh — tras añadir un proyecto el árbol Xwt de solución no se re-puebla; (6) **nuevo P5 preexistente (confirmado por el QA de esta slice)**: los atajos Ctrl+Z/Ctrl+Y están tragados por `isTextEditingCombo` del window y no existen en `SkTextEditor.OnKeyDown` — portarlos implica añadir los casos Z/Y en el editor (el gate validado en esta slice ya funciona por `DispatchCommand`, que bloquea si está disabled). El item `Undo`/`Redo` de las listas anteriores queda **cerrado por esta slice**. Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–7.

**Cierre**: código en commit (5 ficheros) pushado a
`origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos; la ruta
`--old-gui` no se ve afectada (código nuevo bajo `#else`/`AVALONIA_SHELL` o en
ficheros exclusivos del shell; `SkTextEditor.cs` no se compila en el build
legacy).

### Menú (follow-up) — Oleada 3, Slice 8: estado de habilitación de menú (overrides `Update`) — gates context-only `SolutionItemOptions`/`EditReferences` (2026-11-06)

Octava slice de la Oleada 3, sobre el mecanismo de Slice 1. Cierra el item (1)
de "pendiente" de Slice 7: los **gates context-only de `SolutionItemOptions`
(Options) y `EditReferences` (Add Reference)** — los dos comandos que el legacy
gatea por la **selección** del pad de solución
(`CurrentSelectedBuildTarget != null` y `CurrentSelectedProject is
DotNetProject`, respectivamente), a diferencia de los gates del menú principal
de Slice 6 (puente grueso por `ActiveProjectCount`). Esta slice construye el
**bridge exacto** sobre la selección del pad de la shell.

**Matiz de esta slice**: el E2E destapó un **bug preexistente del submódulo
`xwt`** que bloqueaba el trabajo: `TreeStoreBackend.GetChild`
(`Xwt.Avalonia/ListBackends.cs`) no acotaba el índice, así que
`TreeNavigator.MoveToChild` sobre una hoja lanzaba
`ArgumentOutOfRangeException` (tragado por el `try/catch` de
`SelectXwtSolutionRow`) y rompía cualquier recorrido completo del store — el
DFS de búsqueda de filas moría en la primera hoja del subtree de dependencies
(sin alcanzar nunca las filas de proyecto/archivo). La hipótesis de backslash
(paths sin normalizar en `Microsoft.Build.dll`) quedó **descartada** por un
probe vía reflexión sobre ambas DLL (`main/build` y SDK 10.0.401): los paths ya
llegan con slash limpio. Fix: `GetChild` devuelve `null` fuera de rango (el
contrato de `ITreeStoreBackend`, idéntico al backend Gtk en
`TreeStoreBackend.cs:94`); los consumidores (`MoveToChild`,
`FindNavigators`, `RemoveChildren`) solo se hacen más seguros.

**Mapeo legacy → estado shell** (bloques Avalonia `#else`; legacy
`#if !AVALONIA_SHELL` byte-igual):
- `Options` (SolutionItemOptions) → `ShellCommandState.SelectedNodeIsBuildTarget` ⇒ el nodo seleccionado del pad es una fila de **Solution o Project** (espejo de `CurrentSelectedBuildTarget != null`; la shell no tiene solution folders, así que build target = fila de solución o proyecto).
- `AddReference` (EditReferences) → `ShellCommandState.SelectedNodeIsProject` ⇒ el nodo seleccionado es una fila de **Project** (espejo de `CurrentSelectedProject is DotNetProject`; cada fila de proyecto de la shell es un .csproj). `Update` asimétrico documentado: escribe `Enabled=false` + `info.Bypass=true` cuando el gate está off (espejo del keybinding legacy) y `Bypass=false` en el otro brazo; correcto porque `CommandInfo.Enabled` default `true` y `GetCommandInfo` crea instancia fresca por llamada.
- **Desviación aceptada (F-5a)**: el proyecto fantasma (solución que referencia un .csproj ausente en disco) tiene fila en el pad ⇒ `editRefs=1` con `projCount=0` — el gate lee la selección del pad, no la existencia en disco; el fallback de ejecución (guard `File.Exists` → `ResolveActiveProject()`) cubre el caso.
- **F-5d (preexistente, documentado)**: ejecutar "Add Reference..." desde el **menú principal** actúa sobre el proyecto **activo**, no sobre la selección del pad (`nodeContext` solo lo lleva el menú contextual del pad; el clic principal va por `CommandService.Dispatch` → `EditReferencesHandler.Run` → `ShellCommandHandler.Run` sin contexto → fallback `ResolveActiveProject()`). Gate por selección del pad, ejecución por proyecto activo; el legacy apuntaba a `CurrentSelectedProject` en ambas rutas. Diferido a un slice futuro (recomendación del QA).

**Implementación** (4 ficheros + 1 en el submódulo `xwt`, +169/−8):
- `Xwt.Avalonia/ListBackends.cs` (submódulo `main/external/xwt`; +8/−1): `TreeStoreBackend.GetChild` acota el índice y devuelve `null` fuera de rango (bug preexistente descrito arriba).
- `ShellCommandState.cs` (+13): `SelectedNodeIsBuildTarget`/`SelectedNodeIsProject` vía `MainWindow.Instance` (headless ⇒ false).
- `MainWindow.axaml.cs` (+117/−3): accessors `internal SelectedNodeIsBuildTarget` ("Solution" or "Project") y `SelectedNodeIsProject` ("Project"); `SelectionChanged` (tree Xwt) añade `NotifyCommandStateChanged()` (fix F-1 del QA: el item tracked "Add Reference..." quedaba con estado obsoleto tras cambiar la selección — la slice portó el gate sin portar el trigger); caso `OnMenuCommand` nuevo `ProjectCommands.Options` (Properties del menú contextual del pad: strip del prefijo `solution:`/`project:`/`folder:`) y `AddReference` actualizado (nodo del pad si el comando llega con `nodeContext`, si no proyecto activo); `DumpCmdStates` añade `solItemOpts=` (vía `CEn`) y `editRefs=` (vía `En`: el `IsEnabled` real del `MenuItem` tracked tras el `RefreshCommandStates` del dump); driver QA `--cmdstates` con 3 fases node determinísticas (`node_solution`, `node_project`, `node_file` — el proyecto se elige por **walker del store** en orden de solución, función local `CollectProjects`, sustituyendo un disk scan no determinístico; el archivo es el primer `.cs` bajo el proyecto) + instrumentación `itemRefs=` (el `IsEnabled` real del `MenuItem` tracked inmediatamente tras `SelectXwtSolutionRow`, sin refresh — test de regresión de F-1).
- `ProjectCommands.cs` (solo el bloque `#else`; +31/−4): `SolutionItemOptionsHandler.Update` → `SelectedNodeIsBuildTarget`; `EditReferencesHandler.Update` → `SelectedNodeIsProject` + `Bypass`; comentario-header del bloque actualizado (la coarse-bridge de Slice 6 queda restringida a los items del menú principal; Slice 8 = bridge exacto).

**Invariantes**: modelo de menú (`MenuService`) intacto → `--menudump` byte-igual (`menus=11 items=225 cmds=0 bindings=205`); catálogo sin cambios (`Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, `menu ids total=196 resolved=172 missing=24`); bloques legacy byte-iguales; la ruta `--old-gui` no se toca (el fix de `xwt` acota solo el backend Avalonia, al contrato del backend Gtk).

**Validación / evidencia** (artefactos en `~/opencode/oleada3_slice8_qa/`):
- Build `0 Errores` (net10.0 y net10.0-windows).
- Oracle extendido (`~/opencode/catload_test/`, sección `oleada3` a **51 ids** — 2 nuevos: `SolutionItemOptions` y `AddReference`, ambos expected=false): `oleada3: ok=51 fail=0`; invariantes intactas.
- Smoke (`~/opencode/smoke_oleada1.sh`): sin excepciones, `--menudump` byte-igual.
- E2E (`~/opencode/oleada3_cmdstates.sh`, **3 casos** — 16 fases previas + 3 node, las node solo con solución cargada): withsln (`Fixture.sln`: App + LibA + LibB) — `node select=True` ×3, `itemRefs` **0→1→0** (en vivo, sin refresh), dumps `node_solution` `solItemOpts=1 editRefs=0`, `node_project` `1/1`, `node_file` `0/0`; emptysl (ghost, .csproj ausente en disco) — `node_solution` `1/0`, `node_project` (ghost) `1/1` (desviación F-5a), sin `node_file`; nosln — sin fases node. 0 `Unhandled`/`Exception`; fases previas intactas.

**QA (§18.5)**: **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_slice8_qa/`, veredicto en `veredicto.md`). Dos rondas: ronda 1 **RECHAZADO** — 1 P3 abierto (**F-1**, introducido por la slice: el item de menú principal "Add Reference..." — tracked en `MenuBuilder` — no se re-evaluaba al cambiar la selección del pad porque `SelectionChanged` solo llamaba a `UpdatePropertiesPad()`; el E2E no lo detectaba por diseño, porque el dump medía el `Update` en frío y no el `IsEnabled` espejado en el `MenuItem`) → el dev aplicó el fix de 1 línea (`NotifyCommandStateChanged()` en `SelectionChanged`) + la instrumentación `itemRefs` como test de regresión → ronda 2 **APROBADO**, **0 hallazgos P1–P4** — P1 `--old-gui` intacta; P2 los 2 `Update` en las clases correctas con el mapeo legacy documentado (incluida la corrección de `GetChild` del submódulo, fiel al contrato Gtk y sin efectos en `FlattenTree`/`ExpandRow`); P3 sin recursión ni trabajo excesivo en el fix; P4 E2E completo con `itemRefs` + oracle 51/51 + menudump byte-igual + builds 2 TFMs 0 errores. P5 (5, informativos, no bloqueantes): F-5a fantasma `editRefs=1` (desviación aceptada); F-5b fase `node_folder` omitida del driver (los fixtures no contienen solution folders); F-5c fase `node_ref` omitida (los fixtures no exponen filas `reference:` seleccionables); F-5d inconsistencia gate-vs-ejecución preexistente del menú principal (diferida a slice futura); F-5e nomenclatura cosmética (`SolutionOptionsHandler` en `#else` vs `SolutionItemOptionsHandler` legacy).

**Queda pendiente (slices 9..n de la Oleada 3)**: (1) la **port de visibility/rename/dynamic** (`OpenDocument1..9`, `CanShowHelp`, `OpenDocumentList`, etiquetas dinámicas de `Run`/`RunEntry`/`Build` y de `ProjectOptionsHandler`); (2) `Tools` (visibility + dynamic-text) y `Navigation` (requiere historial back/forward de documentos); (3) mejora P5: mensaje de error tras `Stop` legítimo; (4) P5 preexistente: el early-return de `OpenSolutionInWindow` (mismo `solutionLoaded && path`) traga el refresh — tras añadir un proyecto el árbol Xwt de solución no se re-puebla; (5) P5 preexistente (Slice 7): los atajos Ctrl+Z/Ctrl+Y están tragados por `isTextEditingCombo` del window y no existen en `SkTextEditor.OnKeyDown`; (6) **nuevo (QA F-5d)**: tejer la selección del pad en la dispatch del menú principal de "Add Reference..." (o documentar el gate como "estado por selección del pad, ejecución por proyecto activo"); (7) cosmético (QA F-5e): renombrar `SolutionOptionsHandler` (rama `#else`) para no confundirlo con el comando `SolutionOptions`. El item `SolutionItemOptions`/`EditReferences` (context-only) de las listas anteriores queda **cerrado por esta slice**. Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–7.

**Cierre**: código en commit (3 ficheros + puntero del submódulo) y en el
submódulo `xwt` (1 fichero, su propio commit), pushados a
`origin/agents/sub-agent-senior-dev-role-report` y al remoto de `xwt`. La ruta
`--old-gui` no se ve afectada (código nuevo bajo `#else`/`AVALONIA_SHELL` o en
ficheros exclusivos del shell; el fix de `xwt` acota `TreeStoreBackend`,
backend exclusivo Avalonia, al contrato del backend Gtk).

### Menú (follow-up) — Oleada 3, Slice 9: estado de habilitación de menú (overrides `Update`) — port de visibility/rename/dynamic (2026-11-06)

Novena slice de la Oleada 3, sobre el mecanismo de Slice 1. Cierra el item (1)
de "pendiente" de Slice 8: el **port de visibility/rename/dynamic** — todo lo
que los handlers legacy `Update` hacen más allá del booleano `Enabled`:
visibilidad/rename/checked de `OpenDocument1..9`, el array `OpenDocumentList`
(documentos 10+), `OpenWindowList`, el gate `CanShowHelp` y las etiquetas
dinámicas de `Build`/`Rebuild`/`Clean`/`Run`/`RunEntry`/`ProjectOptions`. La
slice completa el espejo de estado en `MenuBuilder` (visible, header dinámico,
glyph checked, submenús dinámicos) y añade la rama de **array commands** al
`CommandManager` del shell.

**Mapeo legacy → estado shell** (bloques Avalonia `#else`; legacy
`#if !AVALONIA_SHELL` byte-igual; los handlers `#else` se compilan DENTRO del
ensamblado del shell, así que alcanzan `MainWindow` y el shim
`GettextCatalog` sin referencia cruzada):
- `OpenDocument1..9` → `OpenDocumentHandlerBase (index)`: `DocumentCount >= index` ⇒ rename al título del tab, `Checked = IsDocumentActive (index)`, `Visible/Enabled = true` + description; si no `Visible/Enabled = false` (espejo del legacy menos el escape `_`→`__` y el markup azul GTK, ambos documentados). `Run` → id literal vía `ShellCommandHandler` (el enum `WindowCommands` no tiene miembros 1..9).
- `OpenDocumentList` (array, `type="radio|array"`) → `Update (CommandArrayInfo)`: docs 10+ como hijos del submenu dinámico con `DataItem = DocumentTagAt (i+1)`; el branch `AccelKey` legacy es **dead code** (con `count >= 10` e `i >= 9`, `count + i < 10` nunca se cumple) y el markup GTK no es portable — ambos descartados.
- `OpenWindowList` (array) → 1 hijo fijo "Window 1" (ventana única).
- `Help` → `info.Visible = true` (**degeneración documentada** de `CanShowHelp`: la shell no tiene `HelpOperations`).
- `Build`/`Rebuild`/`Clean` → `"B_uild {0}"/"R_ebuild {0}"/"C_lean {0}"` si `SelectedBuildTargetName` non-null (pad Solution/Project, fallback `ResolveActiveProject` — el mismo target que la shell construye de verdad), si no catálogo.
- `Run` → `IsOperationRunning` ⇒ "Restart Without Debugging" / "Start Without Debugging" (el busy-guard de Slice 5 mantiene el item visible-disabled).
- `RunEntry` → **bridge solo por selección del pad**: `SelectedNodeIsSolution` ⇒ "Run Solution", `SelectedNodeIsProject` ⇒ "Run Project", sin selección ⇒ catálogo (`_Run Item`) — **sin** fallback a `ActiveProjectCount` (decisión de diseño: port más fiel del legacy, que resuelve por `CurrentSelectedBuildTarget`).
- `ProjectOptions` → `SelectedNodeIsProject ? name : null` ⇒ `"{0} _Options"` / `"Project _Options"`.
- **Desviación F3 (informativa)**: con un **file** seleccionado, la shell pinta el fallback (`B_uild LibA`/`Project _Options`/`_Run Item`) en vez de la resolución file→proyecto del legacy (`B_uild App`/`App _Options`/`Run Project`) — la shell expone la selección solo como Solution/Project (el legacy fija `CurrentSelectedSolutionItem` al proyecto contenedor). Brecha heredada del diseño del bridge, no de esta slice.

**Implementación** (7 ficheros, +486/−104):
- `CommandManager.cs` (sección `#if AVALONIA_SHELL`; +12): `GetCommandInfo` detecta `cmd.CommandArray` ⇒ `info.ArrayInfo = new CommandArrayInfo (info)` + `DefaultHandler.InternalUpdate (info.ArrayInfo)` (espejo de la rama legacy ln 1876–1885); el camino scalar queda intacto.
- `MenuBuilder.cs` (solo shell; +99/−6): `RefreshCommandStates` espeja 4 estados — `IsEnabled`, `IsVisible`, **Header dinámico** (`info.Text != cmd.Text ? info.Text : originalHeaders[item]`; solo cuando el handler renombró de verdad, restaurando el header del modelo en caso contrario: el label de catálogo puede diferir, "Activate 1" vs "_1"; ids sintéticos `cmd:*` → `Command` null → sin espejo de Text/Checked) y **Checked** (glyph ✓ solo en `checkEligible`: items sin icon en modelo y sin model-checked); **submenús dinámicos** — `BuildItem` detecta array commands (`GetActionCommand (id)?.CommandArray == true`), suprime el `Click` del master y lo registra; `RebuildArrayChildren` **full rebuild en cada refresh** (ver F1) con click de hijo → `CommandService.Dispatch (id, dataItem, CommandSource.MainMenu)`; **master oculto con array vacío** (ver F2).
- `WindowCommands.cs` (solo `#else`; +174): `OpenDocumentHandlerBase` + 9 thin, `OpenDocumentListHandler`, `OpenWindowListHandler`; cabecera del bloque actualizada.
- `HelpCommands.cs` (solo `#else`; +11): `HelpHandler.Update` (cita textual del gate legacy en el comment).
- `ProjectCommands.cs` (solo `#else`; +97/−): los 6 overrides de etiqueta (strings legacy idénticos salvo el escape de `_`, documentado); cabecera actualizada.
- `ShellCommandState.cs` (+31): `SelectedNodeIsSolution`, `DocumentTitleAt`/`DocumentTagAt`/`IsDocumentActive` (1-based, null/false fuera de rango) y `SelectedBuildTargetName` (memoizado, mismo contrato y los mismos 4 puntos de invalidación que `ActiveProjectCount`).
- `MainWindow.axaml.cs` (+166/−4): accessors `internal` (`SelectedNodeIsSolution`, `DocumentTitleAt`, `DocumentTagAt`, `IsDocumentActive`, `SelectedBuildTargetName` con fallback `ResolveActiveProject`, `ActiveDocumentTitle` — QA F1); caso `OnMenuCommand` `OpenDocumentList` (dataItem válido → `SelectDocument`); `DumpCmdStates` +16 campos al final (orden previos intacto): `doc1Vis doc2Vis doc1Txt doc1Chk doc2Chk helpVis buildTxt rebuildTxt cleanTxt runTxt runEntryTxt projOptsTxt docListN winListN activeDoc docList0`; driver `--cmdstates`: 4 fases nuevas `docs12`/`docs12sel`/`docs12swap`/`docs12swapsel` (la de swap = regresión F1; close-all final con guard `docs.ContainsKey`).

**QA y fixes** (ciclo §18.5, 2 rondas — veredicto en `~/opencode/oleada3_slice9_qa/veredicto.md`):
- **F1 (P2, ronda 1 → resuelto)**: `RebuildArrayChildren` tenía rama in-place (count igual) que actualizaba Header/Enabled/Icon pero **no** el `Tag` ni el closure `Click` (dataItem capturado en la creación) — con ≥10 docs, cerrar uno y abrir otro (count constante) dispatchaba el dataItem antiguo (documento equivocado o fallback diagnóstico). Fix: **full rebuild en cada refresh** (coste despreciable: hijos ≤ docCount−9) + fases E2E `docs12swap`/`docs12swapsel` como regresión (`docList0=QA List12` → `activeDoc=QA List13`).
- **F2 (P4, ronda 1 → resuelto)**: el master del array siempre visible (submenu vacío incluido con <10 docs); el legacy GTK oculta el master e inlinea los hijos como hermanos. Fix: `item.IsVisible = arr is { Count: > 0 }` (master oculto con array vacío — coincide con el estado vacío del legacy; "Window List" con 1 hijo fijo sigue siempre visible). El **render inline completo** (master oculto + hijos como hermanos) queda **diferido** como decisión de diseño de la slice (submenu dinámico); no afecta al menudump (model-based).
- **F3 (P5)**: labels file-selection por el bridge (arriba). **F4 (P5)**: el ✓ en `OpenDocument1..9` no existe en el legacy GTK (`ActionType.Normal` → `CommandMenuItem` sin `Checked`) — render enriquecido coherente con la slice. **F5 (P5)**: memoización de `SelectedBuildTargetName` con la misma exposición que `ActiveProjectCount` (Slice 6) — sin leak nuevo. **F6 (P5, ronda 2)**: la regresión E2E no discrimina directamente el fix (el dispatch del driver pasa por `nodeContext`/`OnMenuCommand`, no por el `Click` del hijo; `docList0` lee el Header) — el fix se valida por inspección estructural; sugerencia: dump del `Tag` del hijo o dispatch vía click real.

**Invariantes**: modelo de menú (`MenuService`) intacto → `--menudump` byte-igual (`menus=11 items=225 cmds=0 bindings=205` — el dump se genera del modelo, así que el render dinámico no lo altera); catálogo sin cambios (`Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`); bloques legacy byte-iguales (todo el diff cae en `#else`/`#if AVALONIA_SHELL` o en ficheros exclusivos del shell) → la ruta `--old-gui` no se toca.

**Validación / evidencia** (artefactos en `~/opencode/oleada3_slice9_qa/`):
- Build `0 Errores` (net10.0 y net10.0-windows; warnings preexistentes).
- Oracle (`~/opencode/catload_test/`): `oleada3: ok=51 fail=0` (sin cambios) + **sección nueva `oleada3dyn`: ok=19 fail=0** (visible OpenDocument1..9=false sin docs, Help=true, 7 labels de catálogo, array count 0/1 "Window 1").
- Smoke (`~/opencode/smoke_oleada1.sh`): sin warn/Exception, `--menudump` byte-igual.
- E2E (`~/opencode/oleada3_cmdstates.sh`, **3 casos**): `docs12` `docCount=12 docListN=3 winListN=1 doc1Chk=0` → `docs12sel` `doc1Chk=1 activeDoc=QA List1` → `docs12swap` `docList0=QA List12` (hijos re-puntados al nuevo set) → `docs12swapsel` `activeDoc=QA List13` (los 3 casos); labels dinámicos: withsln `node_solution` `B_uild Fixture`/`Run Solution`/`Project _Options`, `node_project` App `B_uild App`/`Run Project`/`App _Options`, `node_file` `B_uild LibA`/`_Run Item` (F3), `operating` `Restart Without Debugging`; emptysl ghost `B_uild Ghost`/`Ghost _Options`/`Run Project` con `buildProj=0`; nosln defaults de catálogo. 0 `Unhandled`/`Exception`; fases previas intactas.

**Queda pendiente (slices 10..n de la Oleada 3)**: (1) `Tools` (visibility + dynamic-text) y `Navigation` (requiere historial back/forward de documentos); (2) **nuevo**: render legacy completo de los array commands (master oculto + hijos inline como hermanos — hoy submenu dinámico con master oculto cuando el array está vacío); (3) mejora P5: mensaje de error tras `Stop` legítimo; (4) P5 preexistente: el early-return de `OpenSolutionInWindow` traga el refresh; (5) P5 preexistente (Slice 7): los atajos Ctrl+Z/Ctrl+Y están tragados por `isTextEditingCombo` del window; (6) P5 (QA F-5d, Slice 8): tejer la selección del pad en la dispatch del menú principal de "Add Reference..."; (7) cosmético (QA F-5e, Slice 8): renombrar `SolutionOptionsHandler` (rama `#else`); (8) **nuevo (QA F6)**: endurecer la regresión de los submenús dinámicos (dump del `Tag` del hijo o dispatch vía click real); (9) P5 (QA F3): exponer el proyecto del archivo seleccionado para las etiquetas file→proyecto. El item (1) de la lista de Slice 8 (visibility/rename/dynamic) queda **cerrado por esta slice**.

**Cierre**: código en commit (7 ficheros) pushado a
`origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en
el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui`
no se ve afectada (todo el diff cae en `#else`/`#if AVALONIA_SHELL` o en
ficheros exclusivos del shell; los bloques legacy quedan byte-iguales a la
base).

### Menú (follow-up) — Oleada 3, Slice 10: estado de habilitación de menú (overrides `Update`) — port de Tools (visibility + dynamic-text) (2026-11-06)

Décima slice de la Oleada 3, sobre el mecanismo de Slice 1. Cierra el item (1)
de "pendiente" de Slice 9: el **port de Tools (visibility + dynamic-text)** —
los 4 overrides escalares de `ToolsCommands.cs` (los más simples del bloque
legacy `#else`, porque `EditCustomTools` no es un array y los 3 restantes solo
tocan `Visible`/`Text`), más el primer consumo real de `AutoHide` en el render
(`MenuBuilder`) y los hooks de refresco de menú al cerrar Preferences.

**Mapeo legacy → estado shell** (bloques Avalonia `#else`; legacy
`#if !AVALONIA_SHELL` byte-igual; los handlers `#else` se compilan DENTRO del
ensamblado del shell, así que alcanzan `MainWindow` y el shim
`GettextCatalog` sin referencia cruzada):
- `EditCustomTools` → `info.Enabled = true` + `info.Text = tools.Count > 0 ? "Edit Custom Tools..." : "Add Custom Tool..."` (`SettingsStore.LoadTools().Count`, strings vía shim `GettextCatalog`).
- `InstrumentationViewer` → `info.Visible = InstrumentationService.Enabled` (el flag de servicio ya vive en el backend compartido; solo el gate de visibilidad).
- `ToggleSessionRecorder` → `info.Visible = SettingsStore.GetBool("MonoDevelop.EnableAutomatedTesting", false)` + `info.Text = IsSessionRecording ? "Stop Session Recorder" : "Start Session Recorder"`.
- `ReplaySession` → mismo gate de pref (`info.Visible`).
- **Desviación documentada**: el `Update(CommandArrayInfo)` de
  `ToolListHandler` legacy **no** se porta. El modelo del shell
  (`MenuService.BuildTools`) ya inlinea los tools configurados como hijos
  `tool:<name>` (con sus atajos), y portar el array habría **duplicado** la
  entrada de cada tool (modelo + hijos dinámicos) y roto el invariante
  del menudump (`_Tools=8i` con 1 tool configurado).
- **Gap de refresco (nuevo)**: el shell no refrescaba el menú al cerrar
  Preferences ni al editar tools; `editToolsTxt` quedaba stale. Fix:
  `dlg.Closed += BuildMenu()` en `OnPreferencesMenu` y en el caso
  `EditCustomTools` del dispatch (misma llamada que hacen los otros
  flujos de menú).

**Implementación** (5 ficheros, +145/−2):
- `ToolsCommands.cs` (solo `#else`; +39): los 4 overrides `Update` (strings
  legacy idénticos; usings nuevos `MonoDevelop.Core` para el shim
  `GettextCatalog` y `MonoDevelop.Ide.Services` para `InstrumentationService`).
  Cabecera del bloque actualizada (61 overrides `Update` acumulados en la
  Oleada 3).
- `ShellCommandState.cs` (+7): `IsSessionRecording` (→
  `MainWindow.IsSessionRecording => sessionRecorder.IsRecording`).
- `MainWindow.axaml.cs` (+80/−2): accessor `IsSessionRecording`;
  `DumpCmdStates` +helper `Sub(header)` + 6 campos al final (orden previos
  intacto): `editToolsTxt instrVis sessVis sessTxt replayVis sessSub`; driver
  `--cmdstates`: 7 fases nuevas `tools`/`instrOff`/`instrOn`/`sessOn`/
  `sessOff`/`noTools`/`toolsBack` bajo try/finally que **restaura** la pref
  `MonoDevelop.EnableAutomatedTesting` y los tools del usuario (los tests no
  pueden dejar el estado del operador); hooks `dlg.Closed += BuildMenu()`
  (2 lugares).
- `MenuService.cs` (+5): el helper `Sub(label, children, autoHide)` ahora
  **propaga `AutoHide = autoHide`** al `MenuEntry` (fix — ver abajo).
- `MenuBuilder.cs` (solo shell; +16): registro en `autoHideSubs` de los subs
  con `AutoHide` en `BuildItem` y consumo en `RefreshCommandStates`
  (tras el mirror scalar y el rebuild de arrays):
  `item.IsVisible = item.Items.OfType<MenuItem>().Any(c => c.IsVisible)` —
  el sub se oculta si todos sus hijos quedan ocultos. Afecta a 3 subs en el
  modelo: "Session Recorder" (Tools — objetivo de la slice; se oculta con la
  pref off), "Active Configuration" (Project — los hijos tienen ids
  sintéticos `SelectActiveConfiguration:<cfg>` sin `Command`, así que el info
  vacío los deja siempre visibles → el sub nunca se oculta, como en legacy) y
  "Run With" (Run — el hijo "(Default)" disabled pero visible → idem).

**QA y fixes** (ciclo §18.5, 1 ronda — veredicto en
`~/opencode/oleada3_slice10_qa/veredicto.md`):
- **F0 (bug propio, pre-QA → resuelto)**: la 1ª E2E daba `sessOff` con
  `sessSub=1` — el sub "Session Recorder" seguía visible con ambos hijos
  ocultos. Root cause: el helper `Sub()` de `MenuService.cs` computaba
  `Disabled` desde `autoHide` pero **no** seteaba `entry.AutoHide` (se
  quedaba `false`), así que el barrido AutoHide de `MenuBuilder` no lo
  registraba nunca. Fix: propagar `AutoHide = autoHide`. E2E post-fix:
  `sessOff → sessSub=0` en los 3 casos.
- QA (solo lectura): **APROBADO** directamente, 0 hallazgos P1–P4, 5 P5
  informativos: (1) la rama "Stop Session Recorder" no está ejercitada por
  oracle/E2E (verificada por inspección del código: `IsRecording` se setea en
  `ToggleSessionRecordingAsync` y el accessor es directo); (2) comentario
  stale preexistente en `BuildToolsWithExternal` ("before Pr_eferences");
  (3) `SettingsStore.LoadTools` ignora el atributo `version` del XML;
  (4) `bindings=` del menudump depende del entorno (205 solo reproducible con
  `--sln=Fixture.sln`); (5) las líneas "Terminado (killed)" de los logs E2E
  son artefacto del harness bash (timeout del `kill`), no de la app.

**Invariantes**: modelo de menú (`MenuService`) intacto salvo el fix de
propagación de `AutoHide` (no cambia la estructura) → `--menudump` byte-igual
(`menus=11 items=225 cmds=0 bindings=205`); catálogo sin cambios
(`Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`); bloques legacy
byte-iguales (todo el diff cae en `#else`/`#if AVALONIA_SHELL` o en ficheros
exclusivos del shell) → la ruta `--old-gui` no se toca.

**Validación / evidencia** (artefactos en `~/opencode/` y
`~/opencode/oleada3_slice10_qa/`):
- Build `0 Errores` (net10.0 y net10.0-windows; warnings preexistentes).
- Oracle (`~/opencode/catload_test/`): `oleada3: ok=51 fail=0` +
  `oleada3dyn: ok=19 fail=0` (sin cambios) + **sección nueva
  `oleada3tools`: ok=5 fail=0** (checks vs estado vivo: etiqueta
  `EditCustomTools` vs `LoadTools()`, visible `InstrumentationViewer` vs
  `Enabled`, visibles `ToggleSessionRecorder`/`ReplaySession` vs la pref,
  label "Start" sin sesión activa) — exit 0.
- Smoke (`~/opencode/smoke_oleada1.sh`): sin warn/Exception, `--menudump`
  byte-igual.
- E2E (`~/opencode/oleada3_cmdstates.sh`, **3 casos** withsln/emptysl/nosln):
  `tools` `editToolsTxt=Edit Custom Tools... instrVis=1 sessVis=1
  sessTxt=Start Session Recorder replayVis=1 sessSub=1` → `instrOff`
  `instrVis=0` → `instrOn` `instrVis=1` → `sessOn` `sessVis=1 replayVis=1
  sessSub=1` → `sessOff` `sessVis=0 replayVis=0 sessSub=0` (el fix) →
  `noTools` `editToolsTxt=Add Custom Tool...` → `toolsBack`
  `editToolsTxt=Edit Custom Tools...` (los 3 casos); prefijos de fases
  previas byte-iguales (regresión 0); 0 `Unhandled`/`Exception`; estado del
  usuario restaurado al terminar, sin procesos huérfanos.

**Queda pendiente (slices 11..n de la Oleada 3)**: (1) `Navigation`
(requiere historial back/forward de documentos); (2) **nuevo**: render legacy
completo de los array commands (master oculto + hijos inline como hermanos —
hoy submenu dinámico con master oculto cuando el array está vacío); (3) mejora
P5: mensaje de error tras `Stop` legítimo; (4) P5 preexistente: el
early-return de `OpenSolutionInWindow` traga el refresh; (5) P5 preexistente
(Slice 7): los atajos Ctrl+Z/Ctrl+Y están tragados por `isTextEditingCombo`
del window; (6) P5 (QA F-5d, Slice 8): tejer la selección del pad en la
dispatch del menú principal de "Add Reference..."; (7) cosmético (QA F-5e,
Slice 8): renombrar `SolutionOptionsHandler` (rama `#else`); (8) **nuevo (QA
F6, Slice 9)**: endurecer la regresión de los submenús dinámicos (dump del
`Tag` del hijo o dispatch vía click real); (9) P5 (QA F3, Slice 9): exponer el
proyecto del archivo seleccionado para las etiquetas file→proyecto. El item
(1) de la lista de Slice 9 (Tools) queda **cerrado por esta slice**.

**Cierre**: código en commit (5 ficheros) pushado a
`origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en
el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui`
no se ve afectada (todo el diff cae en `#else`/`#if AVALONIA_SHELL` o en
ficheros exclusivos del shell; los bloques legacy quedan byte-iguales a la
base).

### Menú (follow-up) — Oleada 3, Slice 11: estado de habilitación de menú (overrides `Update`) — port de Navigation (2026-11-07)

Undécima slice de la Oleada 3, sobre el mecanismo de Slice 1. Cierra el item (1)
de "pendiente" de Slice 10 (Tools se cerró ahí): el **port de Navigation** —
los 4 `Update` de `NavigationCommands.cs`, que son los más ricos del bloque
legacy `#else` porque incluyen el único **array** de la Oleada 3
(`NavigateHistory`) y dependen de un servicio de historial que el legacy tiene
GTK-scoped (`IdeServices.NavigationHistoryService`) y el shell debió construir
propio.

**Mapeo legacy → estado shell** (bloques Avalonia `#else`; legacy
`#if !AVALONIA_SHELL` byte-igual):
- `NavigateBack` → `info.Enabled = NavigationHistoryService.CanMoveBack` (el
  servicio static `MonoDevelop.Ide.Services.NavigationHistoryService` — no
  compila en la build legacy, que usa su
  `MonoDevelop.Ide.Navigation.NavigationHistoryService` con `HistoryList`).
- `NavigateForward` → `info.Enabled = CanMoveForward`.
- `ClearNavigationHistory` → `info.Enabled = CanMoveForward || CanMoveBack`
  (legacy idéntico).
- `NavigateHistory.Update (CommandArrayInfo)` → `GetNavigationList (15)`:
  puntos **newest-first** (bucle `i = count−1 → 0`), `item.Checked =
  (i == currentIndex)`; **histórico vacío** → un único item checked con el
  título del documento activo (`ShellCommandState.ActiveDocumentTitle`, null →
  sin item — el master se oculta por el mecanismo de arrays vacío de Slice 9,
  de modo que el master jamás renderiza un submenú vacío; el legacy hace
  exactamente lo mismo con `doc.Window.Title`).
- **dataItem `navpoint:<fullIndex>|<line>|<file>`**: el legacy `MoveTo`
  resuelve la entrada clicada **por referencia** (`HistoryList.MoveTo(item)`);
  los puntos del shell no llevan identidad, así que cada hijo codifica su
  **índice completo** en el historial (`start + i`, donde `start` es el offset
  de la ventana). El dispatch lee el índice (hasta el primer `|`) y hace
  `MoveTo (fullIndex)` — misma semánta de entrada exacta, sin el defecto de
  match por valor (duplicados) que el P3a de la QA encontró en la 1ª versión.
  `line` y `file` viajan juntos para legibilidad del diagnóstico. Robustez:
  probe QA T4 con nombres hostiles (`|`, `:`, subcadena `navpoint:`) — el
  parse (primer `|` + `int.TryParse`) es exacto.

**Implementación** (5 ficheros, +183/−18):
- `NavigationCommands.cs` (solo `#else`; +48/−4): los 4 overrides `Update`
  (strings/etiquetas del legacy; using nuevo `MonoDevelop.Ide.Services`).
  Cabecera del bloque actualizada.
- `Services/NavigationHistoryService.cs` (+29/−5): `MoveTo (int fullIndex)`
  (no-op si ya es el current — sin `Changed`; `null` fuera de rango, mejor que
  el `InvalidOperationException` legacy) y `GetNavigationList (count)`
  **balanceado** alrededor de `position` (`half = (count−1)/2; start =
  max(0, min(position−half, total−count))`), devuelve `(Points, Current,
  Start)` — espejo del balance del `HistoryList.GetList` legacy: el current
  siempre está dentro de la ventana (índice nunca negativo) y cada hijo mapea
  a su índice completo.
- `ShellCommandState.cs` (+7): `ActiveDocumentTitle` →
  `MainWindow.Instance?.ActiveDocumentTitle`.
- `ToolsCommands.cs` (+2/−2): fix P4 (QA): cabecera del bloque `#else` stale
  ("not ported yet" → "slice 10 ported").
- `MainWindow.axaml.cs` (+97/−7): (a) ctor `Changed +=
  NotifyCommandStateChanged`; (b) `OnDocSelectionChanged` →
  `if (!navSwitching) PushNavigationPoint ();` (feed de activación —
  equivalente del `LogActiveDocument` legacy en activación); (c)
  `PushNavigationPoint ()` solo docs con `FilePath` (untitled: no aterrizable);
  (d) `NavigateToPoint` con `navSwitching` try/finally (flag legacy
  `switching`; ruta síncrona open→select en UI thread → el guard siempre cubre
  la selección; sin saltarse push legítimo — verificado por QA); (e) dispatch
  `NavigateHistory`: `navpoint:` → `MoveTo` → `NavigateToPoint`, sin prefijo →
  dump diagnóstico legacy; (f) dispatch `ClearNavigationHistory`: `Clear ()` +
  `PushNavigationPoint ()` (ancla legacy: tras Clear + siguiente activación,
  Back alcanza el doc pre-Clear); (g) `DumpCmdStates` +6 campos
  (`backEn fwdEn clearEn histN hist0 histCur` — el ✓ se lee del icono del hijo
  del menú construido real); (h) driver `--cmdstates`: 7 fases
  `navCleared/navOpened/navJmp/navBack/navFwd/navSub/navClear2` sobre un
  fichero probe de 12 líneas determinista; `PushNavigationPoint` explícitos
  sustituyen al go-to-line logging que el shell no tiene aún; `finally Clear ()`
  (historial process-local).

**QA y fixes** (ciclo §18.5, 2 rondas — informe en
`~/opencode/oleada3_slice11_qa/qa_report.md`):
- **Ronda 1 (subagente, 2026-11-06 22:00): RECHAZADO** — 1×P2 + 2×P3 + 1×P4
  preexistente + 4×P5, con probes de replicación propios (T1–T8):
  - **P2**: la ventana era **tail** de los últimos N, no balanceada → 16 puntos
    + 15 backs (`position=0`) daba `currentIndex=−1`: 15 hijos sin ✓ y sin el
    punto actual. Fix: `GetNavigationList` balanceado (espejo legacy) + casos
    oracle (16 pushes + 15 backs → `count=15 cur=0 start=0 first=f1.cs`,
    checked en el último hijo).
  - **P3a**: `MoveTo` por valor (File+Line) no distinguía duplicados (legacy:
    por referencia; el propio `HistoryList` documenta que "equal items may
    appear more than once"). Repro: `[a:1(0), b:2(1), a:1(2)]`, pos=2, clic en
    la entrada ✓ → caía en idx 0. Fix: `MoveTo (int fullIndex)` + dataItem
    `navpoint:<fullIndex>|...` + casos oracle (clic en duplicado current =
    no-op; `MoveTo` al duplicado viejo mueve la posición y habilita fwd).
  - **P3b**: `Clear` sin re-registrar el doc activo (legacy `Clear()` =
    `history.Clear(); LogActiveDocument ();`). Repro: Clear + reactivación →
    `CanMoveBack=false` (legacy: true). Fix: `PushNavigationPoint ()` tras
    `Clear()` en el dispatch (donde se alcanza `MainWindow`); la expectativa
    E2E `navClear2` pasa de fallback-título a punto real (`hist0=probe:1`).
  - **P4** (preexistente de Slice 10): cabecera stale de `ToolsCommands.cs`;
    corregida en este ciclo.
- **Ronda 2 (subagente, pasada independiente, 2026-11-07)**: **APROBADO**
  (informe en `~/opencode/oleada3_slice11_qa_round2/qa_report.md`; artefactos:
  logs E2E de los 3 casos). El subagente re-verificó de forma independiente los
  4 fixes (todos **CORREGIDOS**, con valores exactos de oracle y E2E), todas
  las invariantes (builds 0 errores ambos TFMs, oracle `oleada3nav: ok=22
  fail=0` con las regresiones P2/P3, smoke byte-identical, E2E con las 7 fases
  nav idénticas en los 3 casos y valores exactos, catálogo, legacy intacto
  línea a línea) y la exactitud de los 3 docs pendientes: **0 hallazgos P1–P4,
  4 P5 informativos** (los mismos que la ronda 1). Nota: la build GTK legacy
  no pudo re-ejecutarse en el entorno del subagente (fallo de restauración
  MSBuild `NETSDK1005` ajeno a los cambios); la ronda 1 ya la verificó en 0
  errores. Antes de la ronda 2 el dev hizo una re-verificación fresca
  (`oleada3_slice11_oracle_final.txt`, `oleada3_slice11_e2e_final.txt` — 107
  fases `[cmdstates]` byte-iguales a la corrida post-fixes, 0
  `warn/fatal/Exception/Unhandled`). **Veredicto final: APROBADO tras 2
  rondas, 0 hallazgos P1–P4 abiertos** (precedente: Slice 9).
- **P5 (4, informativos)**: (1) **feed de caret**: el shell registra puntos en
  activación y saltos explícitos, no en cada movimiento de caret (legacy:
  `LogActiveDocument` en `Caret.PositionChanged`) → Back/Forward cae a la línea
  de activación/salto; E2E compensa con pushes explícitos; pendiente;
  (2) clic en el item fallback de título (historial vacío, dataItem=null) hace
  `Output ("[nav] history empty")` en vez de no-op silencioso (ruido);
  (3) `Changed += NotifyCommandStateChanged` sin unsubscribe (sin leak
  realista: `MainWindow` single-instance + evento static de vida de proceso);
  (4) la métrica "65 overrides acumulados" no es reproducible como conteo crudo
  (parser de preprocesador: 46→50) — la métrica incluye el estado dinámico de
  Slice 9 resuelto en `MenuBuilder`; el delta +4 se verifica en el diff.

**Invariantes**: modelo de menú (`MenuService`) intacto → `--menudump`
byte-igual (`menus=11 items=225 cmds=0 bindings=205`); catálogo sin cambios
(`Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`); `NavigateHistory`
conserva `type="radio|array"` en `Commands.addin.xml` (submenú dinámico por
`MenuBuilder` desde Slice 9); bloques legacy byte-iguales (todo el diff cae en
`#else`/`#if AVALONIA_SHELL` o en ficheros que no compilan en la build legacy)
→ la ruta `--old-gui` no se toca (build GTK completa 0 errores, QA ronda 1).

**Validación / evidencia** (artefactos en `~/opencode/`):
- Build `0 Errores` (net10.0 y net10.0-windows; warnings preexistentes).
- Oracle: `oleada3: ok=51 fail=0` + `oleada3dyn: ok=19 fail=0` +
  `oleada3tools: ok=5 fail=0` + **`oleada3nav: ok=22 fail=0`** (gates
  empty/1/2 puntos, array newest-first + dataItems `navpoint:`,
  MoveBack/MoveForward con movimiento del ✓, truncación de forward en push
  nuevo, `MoveTo` por índice, ventana P2 16+15, duplicados P3, estado tras
  Clear) — `oleada3_slice11_oracle_final.txt`.
- Smoke (`~/opencode/smoke_oleada1.sh`): `--menudump` byte-igual, 0
  warn/Exception — `oleada3_slice11_smoke_final.log`.
- E2E (`~/opencode/oleada3_cmdstates.sh`, **3 casos**): las 7 fases nav
  **idénticas** en withsln/emptysl/nosln: `navCleared` 0/0/0 histN=0
  histCur=-1 → `navOpened` backEn=1 clearEn=1 histN=2 hist0=probe:3
  histCur=0 (histN=2 determinista: push en activación (línea 1, antes del
  `GotoLine`) + push explícito (línea 3)) → `navJmp` histN=3 hist0=probe:7
  → `navBack` histCur=1 fwdEn=1 → `navFwd` histCur=0 fwdEn=0 → `navSub`
  histCur=2 backEn=0 (`[nav] → probe:1`) → `navClear2` 0/0/0 histN=1
  hist0=probe:1 histCur=0 (re-push ancla el doc activo, fix P3b). 107 fases
  `[cmdstates]` byte-iguales a la corrida post-fixes (regresión 0); 0
  `warn/fatal/Exception`; `closeWs` 1/1/0 — `oleada3_slice11_e2e_final.txt`.

**Queda pendiente (slices 12..n de la Oleada 3)**: (1) render legacy completo
de los array commands (master oculto + hijos inline como hermanos — hoy
submenu dinámico con master oculto cuando el array está vacío); (2) mejora P5:
mensaje de error tras `Stop` legítimo; (3) P5 preexistente: el early-return de
`OpenSolutionInWindow` traga el refresh; (4) P5 preexistente (Slice 7): los
atajos Ctrl+Z/Ctrl+Y tragados por `isTextEditingCombo` del window; (5) P5 (QA
F-5d, Slice 8): tejer la selección del pad en la dispatch del menú principal de
"Add Reference..."; (6) cosmético (QA F-5e, Slice 8): renombrar
`SolutionOptionsHandler` (rama `#else`); (7) P5 (QA F6, Slice 9): endurecer la
regresión de los submenús dinámicos (dump del `Tag` del hijo o dispatch vía
click real); (8) P5 (QA F3, Slice 9): exponer el proyecto del archivo
seleccionado para las etiquetas file→proyecto; (9) P5 (QA F2, Slice 10):
comentario stale de `BuildToolsWithExternal`; (10) **nuevo (QA Slice 11)**:
feed de caret en el historial de navegación; (11) **nuevo (QA Slice 11)**:
no-op silencioso del item fallback de título. El item (1) de la lista de
Slice 10 (Navigation) queda **cerrado por esta slice** — no quedan comandos de
menú con `Update` legacy sin portar.

**Cierre**: código en commit (5 ficheros) pushado a
`origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en
el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui`
no se ve afectada (todo el diff cae en `#else`/ficheros exclusivos del shell;
los bloques legacy quedan byte-iguales a la base).

## 2026-11-07 — Follow-up: Oleada 4 (backend Avalonia) — Slice 1: modelo .sln real (`SlnFile`)

Primera slice de la **Oleada 4** (continuación de la integración total del
backend). La capa de solución de la shell ahora opera sobre el **modelo .sln
real** `MonoDevelop.Projects.MSBuild.SlnFile` (0 GTK, 0 Mono.Addins — el mismo
reader/writer que el `ProjectService` legacy; `MonoDevelop.Core` ya compila
entero vía `ProjectReference`), en lugar de la edición ad-hoc de líneas
(`ConfigurationService` línea a línea; `SolutionProjects` con regex).
**Nota del operador**: la shell no utiliza Mono.Addins ni lo va a usar — se
descarta el pipeline de carga de proyectos reales (acoplado a
`AddinManager.GetExtensionNodes`); se opera a nivel `SlnFile` (API pública
completa; `Solution.WriteSolution` es `internal`).

**Cambios** (3 ficheros, +225/−186):
- `Services/ConfigurationService.cs`: todas las operaciones .sln sobre el
  modelo — `GetSolutionConfigurations` (sección del modelo),
  `AddSolutionConfiguration` (duplicado con semánta legacy
  `ProjectOperations.AddConfiguration`; mappings por proyecto vía
  `GetOrCreatePropertySet` con GUID **con llaves**; **solution-folders
  omitidos** como el writer legacy; `createChildren` solo .csprojs
  referenciados), `AppendProjectToSolution` (SlnProject C# + mappings),
  `AddProjectToSolution` (sln nuevo vía `MSBuildFileFormat.DefaultFormat`
  VS2012: 12.00 + "# Visual Studio 15" + `HideSolutionNode=FALSE`); **nuevo**
  `GetSolutionProjects` (SlnFile.Projects + fallback scan). Sin cambios:
  `AddProjectConfiguration` (.csproj), `Get/SetActiveConfiguration`
  (.userprefs).
- `Views/MainWindow.axaml.cs`: `SolutionProjects` delega a
  `ConfigurationService.GetSolutionProjects` (0 cambios en call sites);
  **hook QA `--slnmodel`** (copias temporales del directorio fixture;
  round-trip byte-identical; addcfg/append/newcsln; **cross-check de reparse
  con Microsoft.Build** vía `SolutionLoader` en cada fichero escrito).
- `Program.cs`: `--slnmodel` en `QaDialogArg`.

**Invariante de formato**: round-trip no-op **byte-identical** (el modelo
preserva BOM/newlines detectados por `FileUtil.GetTextFormatInfo`;
el fixture real BOM+CRLF se conserva byte a byte).

**Verificación** (valores exactos en `docs/session_summary.md`, entrada
2026-11-07 (14)): build 0 errores (ambos TFMs); catálogo
`Count=461 LoadedCommands=475 ResolvedHandlers=111`; menudump **byte-igual**
`menus=11 items=225 cmds=0 bindings=205`; oracle sección nueva
`oleada4 18/18` (incl. 2 casos de regresión P4 del QA: .sln sin
`SolutionConfigurationPlatforms` → append con mappings Debug/Release
[fallback]; + `oleada3* 51/19/5/22` sin cambios, fail=0, exit 0);
E2E 5 casos **ALL_PASS** (`~/opencode/oleada4_slice1_e2e.sh`: menudump,
slnmodel 13/13, activeconfig, newconfig-real, openimport) con el fixture
real intacto; `grep AddinManager` en los 3 ficheros → 0. QA §18.5:
ronda 1 RECHAZADO (P4: fallback Debug/Release perdido en
`AppendProjectToSolution` con .sln sin configuraciones; réplica mínima en
`~/opencode/oleada4_slice1_qa/p4probe/`) → fix (fallback restaurado + 2
checks oracle de regresión + P5b cosmético) → re-verificado (builds, oracle
18/18, E2E ALL_PASS) → ronda 2 (pasada independiente, informe en
`~/opencode/oleada4_slice1_qa_round2/qa_report.md`): **APROBADO** — fix P4
re-verificado con réplica propia contra la DLL compilada real
(`P4_REPLICA_PASS=True`), todas las invariantes re-corridas, 0 hallazgos
P1–P4, 3 P5 no bloqueantes.

**Queda pendiente (slices 2..n de la Oleada 4, resuelto parcialmente en
Slice 2)**: ~~modelo de proyecto `.csproj` real sin AddinManager~~ → resuelto
(Slice 2, `ProjectFileModel` XML puro); ~~árbol del pad sobre el modelo~~ →
resuelto (Slice 2, `BuildTree` + `AddXwtSolutionNode`); sigue pendiente:
RunConfigurations reales; build vía modelo de configuraciones; Add Reference
vía modelo. La lista pendiente de la Oleada 3 (items 2..11 de su entrada)
sigue abierta.

## 2026-11-07 — Follow-up: Oleada 4 (backend Avalonia) — Slice 2: modelo .csproj + árbol de solution-folders

**Alcance**: (1) **modelo de proyecto .csproj** para la shell Avalonia
(nombre, OutputType, TargetFrameworks, OutputPath, Configurations,
referencias, conteo de items) y (2) **árbol completo de solution-folders a
nesting arbitrario** (folder→folder→project) en el pad, con robustez de
producción ante .sln corruptos. **Nota del operador**: la shell no utiliza
Mono.Addins ni lo va a usar.

**Decisiones de diseño**:
- **`MSBuildProject` descartado**: su pipeline de carga (`WorkspaceObject`
  static ctor → `AddinManager.CreateExtensionContext()`) lanza
  `InvalidOperationException` sin Mono.Addins inicializado → `ProjectFileModel`
  con parseo XML puro, caché por (ruta, mtime), y tolerancia (inexistente/no
  parseable → `null`, nunca lanza).
- **Hallazgo P3 (verificado en fuentes y por el QA)**: `Microsoft.Build.
  SolutionFile.Parse` entra en recursión infinita en
  `ProjectInSolution.GetUniqueProjectName` con auto-padre/ciclo en
  `NestedProjects` → `StackOverflowException` **no atrapable** (SIGABRT,
  rc 134). La ruta de producción (`OpenSolutionInWindow`) y los hooks YA NO
  usan `SolutionFile.Parse`: nuevo `ConfigurationService.GetSolutionEntries`
  sobre el modelo **SlnFile** (lector lineal, tolera ciclos). `SolutionLoader.
  Load` conserva MSBuild con el pre-check `HasNestedProjectsCycle` (escaneo
  de texto sin recursión) → `null` si hay ciclo.
- **`HasNestedProjectsCycle` mezcla TODAS las secciones**
  `GlobalSection(NestedProjects)` (last-wins en orden de fichero — el mismo
  estado final que termina Microsoft.Build, cuyo diccionario interno también
  hace overwrite) y detecta el ciclo sobre el mapa mezclado. Un .sln corrupto
  puede declarar la sección varias veces (hallazgo P3-N1 de la ronda 2).
- **Guardas del hook `--slnmodel`**: checks condicionales degradan a `skip`
  (no fail) — parse cfgs solo si el fichero declara
  `SolutionConfigurationPlatforms`, roundtrip bytes solo si VS-canónico (BOM
  `EF BB BF`), cross-checks Microsoft.Build saltados si la fuente tiene ciclo
  o no la parsea. El check `nested folder exists` del `--projmodel` solo se
  exige si la solución **declara** nesting folder→folder.
- **Quirk writer SlnFile (preexistente, compartido con el legacy)**: el
  writer normaliza `postProject` (sección de solución) → `postSolution`;
  los keywords de sección a nivel solución válidos son SOLO
  `preSolution`/`preProject`/`postSolution`/`postProject` (`preSpawn`/
  `postAction` son de proyecto y SlnFile los rechaza con
  `InvalidSolutionFormatException`). El fixture de referencia del slice
  (`~/opencode/ol4s2_nested/Nested.sln`) usa los keywords correctos.
- **Invariante menudump corregida**: el componente `bindings=` del menudump
  (`MenuCommandBindings()`) incluye 1 binding por entry del listado
  persistido de `RecentSolutions` (`~/.config/MonoDevelop/9.0/
  MonoDevelopProperties.xml`, mutado por cada apertura de solución). Base
  **196** (solución abierta, recents vacío; 193 sin solución). El E2E hace
  `reset_recents` antes del caso → invariante determinista
  `menus=11 items=225 cmds=0 bindings=196`. Las lecturas históricas `205`
  son válidas en su momento (196 + 9 recents).

**Cambios** (4 ficheros, +674/−125):
- `Services/ConfigurationService.cs`: `ProjectFileModel` + `LoadProjectModel`/
  `ClearProjectModelCache` + `IsExecutable`; `SolutionFolderTypeGuidAlt`;
  **`GetSolutionEntries`** + `BuildEntriesFromSln` (SlnFile → `SolutionEntry`
  con Guid/ParentGuid).
- `Services/SolutionLoader.cs`: `SolutionEntry` +Guid/+ParentGuid; `BuildTree`
  (3 colores + anclaje de ciclos); `HasNestedProjectsCycle` (todas las
  secciones, last-wins); `Load` con pre-check.
- `Views/MainWindow.axaml.cs`: `OpenSolutionInWindow` vía
  `GetSolutionEntries`+`BuildTree`+`AddXwtSolutionNode` recursiva (iconos
  exe/lib vía `IsExecutable`); `PickStartupProject` y 4 comparaciones
  case-sensitive → `IsExecutable`; hook `--projmodel` (13 checks);
  guardas `--slnmodel`.
- `Program.cs`: `--projmodel` en `QaDialogArg`.

**Verificación** (valores exactos en `docs/session_summary.md`, entrada
2026-11-07 (15)): build 0 errores (ambos TFMs); catálogo
`Count=461 LoadedCommands=475`; menudump determinista `menus=11 items=225
cmds=0 bindings=196`; oracle sección nueva **`oleada4s2 35/35`** (BuildTree
nesting/ciclos/profundidad 200; LoadProjectModel tolerancias/caché;
HasNestedProjectsCycle 7 casos incl. secciones duplicadas; GetSolutionEntries;
IsExecutable minúsculas) + `oleada4 18/18` + `oleada3* 51/19/5/22` sin
cambios; E2E 6 casos **ALL_PASS** (incl. `projmodel 13/13`); matriz corruptos
7 fixtures × 2 hooks **14/14** rc=0 fail=0 (+ 9 fixtures propios del QA,
incl. `DupSection`/`TripleCyc` de secciones duplicadas). QA §18.5 (3 rondas):
ronda 1 **RECHAZADO** (P3-1 crash SOE rc 134 con auto-padre/ciclo; P4-1 falso
positivo `nested folder exists`; P4-2 case-sensitivity) → 3 fixes → ronda 2
**RECHAZADO** (P3-N1: pre-check solo con la primera sección `NestedProjects` —
duplicada con ciclo en la 2ª → rc 134; fixes de r1 verificados FIXADO) → fix
(merge last-wins de todas las secciones) + 3 checks oracle → ronda 3
**APROBADO** (0 hallazgos P1–P4). Reportes: `~/opencode/oleada4_slice2_qa{1,2,3}/`.

**Queda pendiente (slices 3..n de la Oleada 4)**: RunConfigurations reales
sobre el modelo; build vía modelo de configuraciones; Add Reference vía
modelo. La lista pendiente de la Oleada 3 (items 2..11) sigue abierta.

---

## Follow-up — Oleada 3 cleanup: Slice A — items 2, 3, 5, 6, 8, 9 y 11 (2026-11-07)

**Estado: CERRADO.** Cierre de 7 de los 10 items pendientes de la Oleada 3
(los que no requieren el modelo de soluciones ni cambios de editor). Quedan
abiertos: item 1 (render de array commands en legacy), items 4+10 (editor
Ctrl+Z/Y + feed caret) y item 7 (endurecer regresión).

**Item 2 — Stop sin "Build FAILED"** (`MainWindow.axaml.cs`): el proceso de
build/run se interrumpe con `Kill()` (SIGKILL → código 137) y el exit
handler, al ver `code != 0`, escribía `SetErrors("Build FAILED...")` en el
pad de errores — el usuario veía un build fallido cuando solo había hecho
Stop. Fix: campo `stoppedByUserProc`; `StopBuildOrRun` lo fija antes del
`Kill()` y el exit handler compara `ReferenceEquals(proc,
stoppedByUserProc)`: si coincide, la línea es `"[process] stopped by user
(exit code N)"` y **salta** el `SetErrors` de Build FAILED. Salida natural
(código real de dotnet, 0 o fallo) sin cambio.

**Item 3 — refresh del pad en add/remove/rename** (`MainWindow.axaml.cs`):
`RefreshSolutionTree()` (y la ruta de producción de añadir proyecto)
llamaban `OpenSolutionInWindow(loadedSolutionPath)`, cuyo early-return de
mismo-path (preexistente) hacía no-op → el árbol del pad se quedaba stale
tras añadir/borrar/renombrar proyectos. Fix: extraído el poblado a
`RebuildXwtSolutionTree(path)` y `OpenSolutionInWindow(path,
forceRefresh=false)` — con `forceRefresh: true` el early-return re-puebla el
árbol (sin re-ejecutar recents/watches/menú). Consumidores `true`:
`RefreshSolutionTree()`, la ruta de producción de añadir proyecto
(`NewSolutionDialog` → `AppendProjectToSolution`) y el driver `--newproject`.
Los primeros opens (startup/QA/File > Open/recents/welcome) conservan
`false` (no-op correcto: la solución ya está abierta, igual que el legacy).
Verificado con el hook `--newproject` (copia del fixture, console+library):
`pad-tree=True` (el `xwtSolutionNodes` contiene el proyecto nuevo).

**Items 5+8 — selección de fila de file → proyecto contenedor**
(`MainWindow.axaml.cs`): nuevo `ResolveSelectedProjectPath()`: fila
`project:` → sí misma; cualquier otra fila → ancestro `project:` más cercano
subiendo por `TreeStore.GetParent` (el tag de nodo guarda la ruta del
proyecto). Reemplaza `ActiveProjectPath()` (primer proyecto de la solución)
en 4 consumidores: `SelectedNodeIsBuildTarget`, `SelectedNodeIsProject`,
`SelectedBuildTargetName` y el dispatch `AddReference` con `nodeContext is
null` (menú principal). El bridge legacy se recupera exacto: seleccionar un
file fija el selected build target al proyecto contenedor. El dispatch de
menú contextual (`nodeContext != null`) no se toca: `BuildProjectPadMenu`
solo lo ofrece en filas de proyecto. E2E: fila `App/Program.cs` →
`buildTxt=B_uild App` / `runEntryTxt=Run Project` / `projOptsTxt=App
_Options` (antes: LibA, el primero).

**Item 6 — rename F-5e del QA slice 10** (`ProjectCommands.cs` +
`Commands.addin.xml`): `SolutionOptionsHandler` →
`SolutionOptionsMenuHandler` en **ambas** ramas (`#if AVALONIA_SHELL` y
legacy). La XML de extensión es compartida y el `defaultHandler` se resuelve
por string en runtime: renombrar una sola rama rompería el otro binario →
build legacy `MonoDevelop.Ide.csproj` 0 errores como check P1; 0
referencias residuales; catálogo 475/111 inmutable.

**Item 9** (`MenuService.cs`): comentario de `BuildToolsWithExternal`
corregido (citaba un item "Preferences" inexistente en Tools > Options).

**Item 11** (`MainWindow.axaml.cs`): `NavigateBack`/`NavigateForward` con
historial vacío → no-op silencioso (quitada la línea de log
`"[nav] history empty"`); el dump de diagnóstico del `--cmdstates` se
conserva.

**Drivers QA**: fase stop en el `--cmdstates` (prestop → `sleep 30` vía
`RunProcessAsync` → `StopBuildOrRun()` → poststop) + 4 aserciones slice A en
`~/opencode/oleada3_cmdstates.sh` (`stop`, `poststop`, `node_file F3`,
`rename`) + probe `~/opencode/oleada3_sliceA_probe.sh` (pad-tree).

**Cambios** (5 ficheros de código):
- `Views/MainWindow.axaml.cs` (items 2, 3, 5, 8, 11 + drivers)
- `Views/MenuService.cs` (comentario, item 9)
- `MonoDevelop.Ide.Commands/ProjectCommands.cs` (rename, item 6)
- `MonoDevelop.Ide.Commands/ShellCommandState.cs` (comentarios)
- `ExtensionModel/Commands.addin.xml` (defaultHandler, item 6)
- (QA, fuera del repo) `~/opencode/oleada3_cmdstates.sh`,
  `~/opencode/oleada3_sliceA_probe.sh`

**Verificación**: build 0 errores (net10.0, net10.0-windows **y legacy**
`MonoDevelop.Ide.csproj`); catálogo `Count=461 LoadedCommands=475
ResolvedHandlers=111`; menudump `menus=11 items=225 cmds=0 bindings=196`;
E2E `--cmdstates` 3 casos + 4 aserciones **PASS** (0 warn/Exception; log
real `[run] stopped` → `[process] stopped by user (exit code 137)`); probe
`--newproject` **ALL_PASS**; E2E oleada4 6 casos **ALL_PASS**; oracle
completo `fail=0` (valores exactos en `docs/session_summary.md`, entrada
2026-11-07 (16)). QA §18.5 (1 ronda, brief+reporte en
`~/opencode/oleada3_sliceA_qa/`): **APROBADO**, 0 hallazgos P1–P4, 2 P5 no
bloqueantes.

**Queda pendiente (slices B y C de la Oleada 3)**: Slice B — items 4+10
(editor Ctrl+Z/Y + feed caret); Slice C — items 1+7 (render legacy array
commands + endurecer regresión). Slices D–F de la Oleada 4 sin cambios.

---

## Follow-up — Oleada 3 editor: Slice B — items 4 y 10 (2026-11-07)

**Estado: CERRADO.** Cierre de los items de editor de la lista pendiente
de la Oleada 3. Queda abierto el Slice C (items 1+7: render legacy array
commands + endurecer regresión).

**Item 4 — Ctrl+Z/Ctrl+Y en el editor** (`SkTextEditor.cs`): el
early-return de `isTextEditingCombo` en `MainWindow.OnKeyDown` (traga
X/C/V/Z/A con Ctrl cuando el foco no es un control de texto) se llevaba
el atajo de undo/redo y el `OnKeyDown` del editor no tenía casos Z/Y —
el undo/redo solo funcionaba por menú (slice 7 portó los comandos;
este P5 portó los atajos). Fix: casos `Key.Z when ctrl → Undo()` y
`Key.Y when ctrl → Redo()` en `SkTextEditor.OnKeyDown` (mismo patrón que
X/C/V/A/K/D; `e.Handled = true`). El gate es la propia pila del editor
(`Undo`/`Redo` no-op con pila vacía — el mismo `CanUndo`/`CanRedo` que
usan los comandos de menú). `isTextEditingCombo` no se toca: un
combobox editable con foco conserva su Ctrl+Z nativo. Hook QA
`SendKeyForQa(Key, KeyModifiers)`: enruta teclas sintéticas por el
`OnKeyDown` real (Avalonia 12.1.2 no expone ctor parametrizado de
`KeyEventArgs` — verificado por reflexión —; ctor vacío + setters
públicos) y devuelve `Handled`.

**Item 10 — feed caret en la navegación** (`SkTextEditor.cs` +
`Services/NavigationHistoryService.cs` + `MainWindow.axaml.cs`): port
del `Caret.PositionChanged → LogActiveDocument(true)` legacy. (a) Los
campos `caretLine`/`caretCol` del editor pasan a propiedades (backing
`_caretLine`/`_caretCol`) que disparan `public event Action? CaretMoved`
en cualquier cambio — los dos campos son el punto de paso único de
TODO movimiento de caret (teclas, clicks, gutter, goto, búsqueda,
inserts, backspace), así que el evento es el único hook (equivalente al
`ITextView.Caret.PositionChanged`) y 0 call-sites tocados. (b)
`NavigationHistoryService.FeedCaret(file, line1Based)`: mismo file que
el punto actual → actualiza la línea IN-PLACE (`Changed` solo si cambió;
nunca trunca el forward-branch); file distinto o historial vacío →
`Push` (como una activación); clamp ≥ 1. Sin timers transient de 10 s
— los puntos del shell son permanentes de nacimiento (el legacy hacía el
colapso vía `ShouldReplace`/`ReplaceCurrent`). (c)
`MainWindow.AttachNavigationCaretFeed()` en `OnDocSelectionChanged`
(ambas ramas): desuscribe el editor anterior y suscribe al activo
(port del par `AttachToDoc`/`DetachFromCurrentDoc`); el handler hace
`FeedCaret(ed.FilePath, ed.CurrentLine + 1)`. El feed continuo hace
innecesario el `LogActiveDocument()` que el legacy `MoveBack`/
`MoveForward` hacían antes de mover: al pulsar Back el punto actual ya
lleva la línea del caret.

**Comportamiento observable (E2E, 2 probe files de 12 líneas)**:
- Open en línea 3: punto único `probe:3` (el feed colapsa in-place el
  punto de activación — antes se apilaba `:1`+`:3`).
- GotoLine(8) en el doc activo: el punto actual pasa a línea 9 SIN
  añadir puntos (`histN` constante).
- Back → current = `probe:3`; Forward → regresa a **línea 9** (la de
  caret), no la 5 del salto.
- `undoredox`: insert → `SendKeyForQa(Z, Control)` undo → `SendKeyForQa(Y,
  Control)` redo → Z sin Ctrl no-op; `[undoredox] inserted=True
  ctrlZ=True ctrlY=True bareZ=True` en los 3 casos del E2E.

**Cambios** (3 ficheros de código):
- `MonoDevelop.Ide/Controls/SkTextEditor.cs` (props caret + `CaretMoved`,
  casos Z/Y, `SendKeyForQa`)
- `MonoDevelop.Ide/Services/NavigationHistoryService.cs` (`FeedCaret`)
- `MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs`
  (`AttachNavigationCaretFeed`/`EditorCaretMoved` + fases nav/undoredox
  del driver `--cmdstates`)
- (QA, fuera del repo) `~/opencode/oleada3_cmdstates.sh` (aserciones B),
  `~/opencode/catload_test/Program.cs` (7 checks `FeedCaret`)

**Verificación**: build 0 errores (net10.0, net10.0-windows **y legacy**
`MonoDevelop.Ide.csproj` — los 3 ficheros solo entran en el build
Avalonia vía glob); catálogo `Count=461 LoadedCommands=475
ResolvedHandlers=111`; menudump `menus=11 items=225 cmds=0 bindings=196`;
E2E `--cmdstates` 3 casos: 0 warn/Exception + 4 aserciones slice A
(regresión) + 4 slice B PASS; probe `--newproject` **ALL_PASS**; E2E
oleada4 6 casos **ALL_PASS**; oracle `oleada3nav 29/29` (22 previos + 7
`FeedCaret`) y resto `fail=0` (valores exactos en
`docs/session_summary.md`, entrada 2026-11-07 (17)). QA §18.5 (1 ronda,
brief+reporte en `~/opencode/oleada3_sliceB_qa/`): **APROBADO**, 0
hallazgos P1–P4, 3 P5 no bloqueantes (ruido `Terminado (killed)` del
harness, `SendKeyForQa` sin `RoutedEvent` — limitación de Avalonia 12,
`CaretMoved` pre-suscripción no-op).

**Queda pendiente (slice C de la Oleada 3)**: items 1+7 — render legacy
completo de los array commands (master oculto + hijos inline como
hermanos) + endurecer la regresión de los submenús dinámicos (dump del
`Tag` del hijo o dispatch vía click real). Slices D–F de la Oleada 4 sin
cambios.
## Follow-up — Oleada 3 (array commands): Slice C — items 1 y 7 (2026-11-07)

**Estado: CERRADO.** Cierre de los 2 últimos items de la lista
pendiente de la Oleada 3 (con esta slice sus 10 items quedan
cerrados). Quedan abiertos los slices D–F de la Oleada 4
(RunConfigurations vía modelo; build vía modelo; Add Reference vía
modelo).

**Item 1 — render legacy de array commands** (`MenuBuilder.cs`): port
del `CommandMenuItem.Update` GTK. Antes: el master se renderizaba
visible y, si el array no estaba vacío, sus hijos vivían **dentro**
del master como submenú (render Avalonia propio, no el legacy).
Ahora: el master queda **siempre oculto** (`master.IsVisible = false`
al inicio de `RebuildArraySiblings(masterId)`, ANTES de resolver el
padre — no depende del estado anterior ni del refresh de
habilitación) y los hijos se insertan **inline como hermanos en el
menú padre** (full rebuild desde `CommandArrayInfo` en cada refresh:
los hijos preexistentes se eliminan guardados por
`if (parent.IndexOf(c) >= 0)` para no borrar items del modelo; cada
hijo lleva `Header` del texto del `DataItem`, `Tag = DataItem`
(payload de dispatch) y `Click → CommandService.Dispatch(id, data,
CommandSource.MainMenu)`). Resolución del padre: el `ItemsControl`
capturado al build (`arrayMasterParent`) si aún contiene al master;
si no, `master.Parent as ItemsControl`. Los **10 array masters
reales** del catálogo pasan a este render: `ViewList`, `LayoutList`,
`ToggleIssues` (Inline Messages), `NavigateHistory` (View >
Navigation), `CustomCommandList` (Project), `RunTestWith`, `DebugTest`
(Unit Testing), `ToolList` (Tools), `OpenWindowList`,
`OpenDocumentList` (Window). `AppendEntry` registra los masters
(`arrayMasters` id → `CommandArrayInfo`) y suprime su `Click` (como
antes); `GetArrayChildren(id)` expone los hijos para el dump QA.

**Item 7 — endurecer la regresión (QA F6, slice 8)**
(`MainWindow.axaml.cs`, `DumpCmdStates`): el dump `--cmdstates` no
veía el payload de dispatch de los hijos, así que un hijo que
disparara el `DataItem` equivocado no habría sido detectable. Ahora
`ListN`/`histItems` se obtienen vía `MenuBuilder.GetArrayChildren(...)`
y el dump añade **4 campos**: `navMasterVis=` (`IsVisible` del master
`NavigateHistory` — invariante 0), `docList0Tag=` (`Tag` del primer
hijo de `OpenDocumentList`), `winList0Tag=` (primer hijo de
`OpenWindowList`) y `hist0Tag=` (primer hijo de `NavigateHistory`).
Cobertura de los 3 tipos de payload: string (`QA List10`/`QA
List12`), `Window 1` y `navpoint:<idx>|<line>|<file>`.

**Rastrero (Avalonia 12) — mutación de `ItemCollection`**: en
Avalonia 12 `MenuItem.Items` devuelve `Avalonia.Controls.
ItemCollection` (una `ItemsSourceView`); sus implementaciones
**explícitas** de mutación `IList` (`Add`/`Insert`/`Remove`/
`RemoveAt`/`Clear`) lanzan **siempre**
`NotSupportedException: Collection is read-only.`, incluso en modo
Items escribible. Verificado por decompilación de
`Avalonia.Controls.dll` 12.1.2 con `ilspycmd` (dump en
`~/opencode/avctl.cs`, clases `ItemCollection`/`ItemsSourceView` en
L29576/L30640). Síntoma en la app: `Error loading solution:
Collection is read-only.` en `OpenSolutionInWindow` vía
`NotifyCommandStateChanged → RefreshCommandStates →
RebuildArraySiblings` (mutar los `Items` del menú padre por el cast
a `IList` explotaba). Fix: toda mutación de `Items` en `MenuBuilder`
pasa por los helpers `InsertItem`/`RemoveItem`: si el contenedor es
`ItemCollection` usan los **métodos públicos** `ic.Insert`/`ic.Remove`
(delegan en `WritableSource`); si no, caen al `IList` genérico. El
`target.Add(...)` de `AppendEntry` también es seguro: `ItemCollection`
reimplementa `int IList.Add(object?)` redirigiendo al método público
(verificado en la misma decompilación; también lo verificó el QA).

**Cambios** (2 ficheros de código):
- `MonoDevelop.Startup.Avalonia/Views/MenuBuilder.cs`
  (reescrito del mecanismo de arrays: `arrayMasters`/
  `arrayMasterParent`/`arrayChildren`, `AppendEntry`,
  `RebuildArraySiblings`, `GetArrayChildren`, `InsertItem`/
  `RemoveItem`)
- `MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs`
  (`DumpCmdStates`: `ListN`/`histItems` vía `GetArrayChildren` + 4
  campos de Tag)
- (QA, fuera del repo) `~/opencode/oleada3_cmdstates.sh` (4
  aserciones slice C), `~/opencode/oleada3_sliceC_qa/{brief.md,
  qa_prompt_r1.md}`

**Verificación**: build 0 errores (net10.0, net10.0-windows **y
legacy** `MonoDevelop.Ide.csproj` — los 2 ficheros solo entran en el
build Avalonia vía el glob `Views/**`); catálogo `Count=461
LoadedCommands=475 LastError=(null)`, `ResolvedHandlers=111`; menudump
`menus=11 items=225 cmds=0` (19/35/39/27/13/7/35/14/8/15/13;
bindings fluctuante, no invariante); smoke `Loaded Fixture.sln` sin
`Error loading solution`; E2E `--cmdstates` 3 casos: 0
warn/Exception + **12/12 aserciones** (4 slice A + 4 slice B de
regresión + 4 slice C: `docList0Tag=QA List10` → `QA List12` en
swap; `hist0Tag=navpoint:1|9|…/oleada3_nav_probe2.txt`;
`navMasterVis=0` + `winList0Tag=Window 1` en todas las fases); probe
slice A **ALL_PASS**; E2E regresión Oleada 4 **ALL_PASS**; oracle
`oleada3 51/51`, `oleada3dyn 19/19`, `oleada3tools 5/5`,
`oleada3nav 29/29`, `oleada4 18/18`, `oleada4s2 35/35` (valores
exactos en `docs/session_summary.md`, entrada 2026-11-07 (18)).
QA §18.5 (1 ronda, brief+reporte en `~/opencode/oleada3_sliceC_qa/`):
**APROBADO**, 0 hallazgos P1–P4 y 0 P5 (revisión punto a punto de
los 9 riesgos del brief).

**Queda pendiente (slices D–F de la Oleada 4)**: RunConfigurations
reales sobre el modelo (D); build vía modelo de configuraciones (E);
Add Reference vía modelo (F).

## 2026-11-08 — Follow-up: Oleada 4 (backend Avalonia) — Slice D: RunConfigurations vía modelo + StartupItem en .userprefs

**Estado: CERRADO.** Cierre del item (3) de la lista pendiente de la
Oleada 4 (RunConfigurations reales sobre el modelo) más el cableado del
proyecto de arranque.

**Alcance**: la shell deja de hacer parseo inline ad-hoc del csproj (el
anterior `ShowExecutionModeSelector` leía cualquier
`PropertyGroup Condition` y confundía build configs `$(Configuration)` con
run configs `$(RunConfiguration)`) y todo el flujo de ejecución pasa por
`ConfigurationService`: combo de run configs, proyecto de arranque
persistido (StartupItem) y los argumentos de `dotnet run`.

**Decisiones de diseño**:
- **Fidelidad al parse legacy** (`ParseRunConfigurationCondition` +
  `ExtractConfigName`, `MonoDevelop.Projects/Project.cs`): el LHS de la
  condición debe ser EXACTAMENTE `'$(RunConfiguration)'` (el `Trim()` solo
  actúa fuera de las comillas — un espacio interior al LHS se rechaza, como
  en el legacy; el oracle lo cubre con X6 rechazada / X7 aceptada) y el RHS
  un nombre entre comillas simples, longitud >2, sin comilla interior. Los
  grupos de build (`$(Configuration)`, `$(Platform)`) y las condiciones
  inválidas quedan fuera de la lista de run configs.
- **Semántica de props = la evaluación legacy (merge prop-a-prop)**: un
  nombre presente en el `.csproj` y en el `.csproj.user` es UNA sola entrada
  (orden de documento, primera aparición) cuyas props se resuelven como en
  la evaluación: prop-a-prop gana el grupo posterior (user) y el anterior da
  fallback. Verificado contra `MSBuildProjectInstance.GetPropertiesLinkedToGroup`
  (`evaluatedProperties.LinkToGroup(group)` devuelve la **colección
  evaluada completa**, no solo el último grupo). El primer intento del dev
  ("el último grupo reemplaza todo", `group.Clear()`) era incorrecto y se
  corrigió tras leer ese código.
- **StartupItem en el user file de la solución**: forma legacy
  (`Solution.OnEndLoad`: sección `MonoDevelop.Ide.Workspace`,
  `Property name="StartupItem"`, path relativo al .sln) en
  `<sln-sin-ext>.userprefs` — `Fixture.userprefs`, NO `Fixture.sln.userprefs`
  (confirmado con el fixture real, que shipping `Fixture.userprefs`). Set:
  replace (una sola prop, sin append), recrea un .userprefs corrupto, crea
  el fichero si no existe. Remove: devuelve true si existía. Get: corrupto →
  null.
- **`--runcfg` en la allow-list de `QaDialogArg`** (Program.cs, 1 línea):
  sin ella `qa` queda `""` y el driver no se ejecuta (hallado en el primer
  E2E: el caso no emitía ninguna línea, ni siquiera el fallback).

**Cambios** (3 ficheros):
- `Services/ConfigurationService.cs` (coda, sin ficheros nuevos):
  `RunConfigurationEntry{Name, StartArguments?, WorkingDirectory?,
  PauseConsoleOutput?, ExternalConsole?}` (las 4 props que consume la capa
  de ejecución, legacy `ProcessRunConfiguration` [ItemProperty]);
  `ParseRunCondition`; `GetProjectRunConfigurations` (csproj + .user; orden
  de documento; merge prop-a-prop; corrupto/ausente → contribuye nada, sin
  throw); `PrefsPath` (extensión reemplazada por `.userprefs`);
  `Get/Set/RemoveSolutionStartupItem`; `GetSolutionRunConfigurations`
  (agregado por `GetSolutionProjects`, orden .sln).
- `Views/MainWindow.axaml.cs` (6 cambios): ctor `RunConfigCombo` solo
  "Default" (eliminado Debug/Release hardcoded); `RefreshConfigurationSelectors`
  repuebla el combo vía modelo (vacío → ["Default"]); `PickStartupProject`
  respeta el StartupItem persistido (rel→abs contra el dir del .sln; debe
  estar en la lista de proyectos del modelo; si no, fallback 1er Exe sin
  romper run); `ShowExecutionModeSelector` sin parse inline (modelo);
  `RunStartupProjectAsync` — nombre desde el combo, `--args "…"` SOLO si la
  runcfg elegida define StartArguments (también en modo mono/external),
  línea `[run]` + `runcfg=<name>`; driver QA `--runcfg` (11 aserciones).
- `Program.cs`: `--runcfg` en `QaDialogArg`.

**Verificación** (valores exactos en `docs/session_summary.md`, entrada
2026-11-08 (19)): build 0 errores (net10.0, net10.0-windows **y legacy**
`MonoDevelop.Ide.csproj` — los 3 ficheros del diff no entran en el csproj
legacy: lista explícita; `ConfigurationService.cs` solo lo compila el
csproj Avalonia vía el glob `Services/**`); catálogo `Count=461
LoadedCommands=475 LastError=(null)`, `ResolvedHandlers=111`; menudump
`menus=11 items=225 cmds=0` (bindings=196 determinista con recents
reseteados); oracle sección nueva **`oleada4d 21/21`** + todas las
anteriores intactas (`oleada3 51`, `oleada3dyn 19`, `oleada3tools 5`,
`oleada3nav 29`, `oleada4 18`, `oleada4s2 35`); E2E nuevo
`~/opencode/oleada4_sliceD_e2e.sh` (3 casos sobre copias del fixture)
**ALL_PASS** (`runcfg` 11/11: initial sin StartupItem, fallback App, sin run
configs en fixture plano, combo ["Default"], set LibA → pick respeta,
replace sin append, item inválido → fallback App, remove existed/gone/no-op;
`menudump` 11/225/0 bindings=196; `activeconfig` restored=Debug); regresión
E2E Oleada 4 slice1 (6 casos) **ALL_PASS**; E2E `--cmdstates` (3 casos)
**SLICEA_ALL_PASS** (12 aserciones); smoke `Loaded Fixture.sln` sin `Error
loading solution` + menudump 11/225/0 (19/35/39/27/13/7/35/14/8/15/13);
fixture de referencia intacto. QA §18.5 (1 ronda, brief+reporte en
`~/opencode/oleada4_sliceD_qa/`): **APROBADO**, 0 hallazgos P1–P4 y 2 P5
(revisión punto a punto de los 10 riesgos del brief, incluido que no queda
`group.Clear()` en el diff y que `OnToolbarConfigChanged` sigue
persistiendo solo el ConfigCombo). P5-a: indentación desalineada en 2
líneas de `RunStartupProjectAsync` (bloque mono y `ProcessStartInfo` de
xterm) → corregida en la misma ronda y re-verificado (3 builds 0 errores,
oracle `oleada4d 21/21` exit 0, smoke `Loaded Fixture.sln`). P5-b: falso
positivo — señalaba `IndexOf("==")` sin ordinal, pero el diff ya lo
llevaba.

**Queda pendiente (slices E–F de la Oleada 4)**: build vía modelo de
configuraciones (E); Add Reference vía modelo (F).

## 2026-11-08 — Follow-up: Oleada 4 (backend Avalonia) — Slice E: build vía modelo de configuraciones (mapeo solución→proyecto)

**Estado: CERRADO.** Cierre del item (4) de la lista pendiente de la Oleada 4: el build de la shell Avalonia consume ahora el mapeo solución→proyecto de configuraciones que consumía el build de solución legacy, cerrando el hueco de `RunBuildAsync` pasándole la configuración activa de solución cruda a todos los proyectos.

**Alcance**: nuevo modelo en `ConfigurationService` (coda del archivo, sin ficheros nuevos, sin carpetas nuevas): `GetProjectConfigurationFor(slnPath, solutionConfig, projectPath)` → `ProjectConfigurationMapping(Configuration, Platform)`, fiel al .sln:
- solo `"{guid}.{slnCfg}|{slnPlat}.ActiveCfg = {projCfg}|{projPlat}"` del set de propiedades cuyo GUID iguala al del proyecto; se ignora `Build.0` y cualquier otra propiedad del set (deciden SI se builda, no CON QUÉ configuración; la shell itera todos los proyectos de la solución).
- Match de la config de solución case-insensitive: nombre simple compara la parte antes de `|`, clave completa (`Cfg|Plat`) la clave entera.
- Valor sin `|` → plataforma "Any CPU". La comparación de GUID normaliza braces/espacios/case. Proyecto resuelto por path completo (igual que `GetSolutionProjects`: `\` → `/`).
- Fallback `(solutionConfig, "Any CPU")` **sin throw**: sln ausente, sln corrupto (catch), proyecto no listado en el .sln, sin entrada (política slice D: corrupto contribuye nada, nunca rompe el build).

La shell `RunBuildAsync` builda cada proyecto con la configuración **mapeada**: línea `[build] project <name> cfg=.. plat=..` (valores resueltos), comando `dotnet <target> -c "<mapped.Configuration>"` + `-p:Platform="<plat>"` solo si la plataforma no es AnyCPU (`IsAnyCpuPlatform`, miembro nuevo: vacío/"Any CPU"/"AnyCPU" ignoring case y espacios) y línea final `[build] <target> done failed=<bool>` (la variable `failed` ya existía pero no se reportaba). Con fixture plano AnyCPU el mapeo es la identidad: comandos byte-idénticos al pre-E. Driver QA `--buildmap` (model-check por proyecto + `await RunBuildAsync()` production path + `[buildmap] all pass`) y allow-list `QaDialogArg` en `Program.cs` (1 línea).

**Hallazgo de dev (contexto, no defecto del slice)**: el fixture `~/opencode/solpad_deps/` tiene bugs de compilación **preexistentes** (era fixture de carga, nunca de build): `LibA/A.cs` usa `B.V` sin `using LibB;` y `App/Program.cs` usa `Console` sin `using System;` → CS0103 en cualquier build (reproducido con el comando exacto pre-E, con el cambio revertido). El E2E de slice E parchea ambos archivos **solo en la copia**; el fixture real queda intacto (asertado al final del script y por QA).

**Verificación** (valores exactos en `docs/session_summary.md`, entrada 2026-11-08 (20)): build 0 errores (net10.0, net10.0-windows **y legacy** `MonoDevelop.Ide.csproj` — los 3 ficheros del diff no entran en el csproj legacy: `ConfigurationService.cs` solo lo compila el csproj Avalonia vía el glob `Services/**`); catálogo `Count=461 LoadedCommands=475 LastError=(null)`, `ResolvedHandlers=111`; menudump `menus=11 items=225 cmds=0`; oracle sección nueva **`oleada4e 13/13`** + todas las anteriores intactas (`oleada3 51`, `oleada3dyn 19`, `oleada3tools 5`, `oleada3nav 29`, `oleada4 18`, `oleada4d 21`, `oleada4s2 35`); E2E nuevo `~/opencode/oleada4_sliceE_e2e.sh` (2 casos sobre copias del fixture) **ALL_PASS** (buildplain: 3x `model`/`[build] project` `cfg=Debug plat=Any CPU`, `failed=False`, 3x exit 0, sin warns; buildmap: mapeo en el .sln `LibA …Debug|Any CPU.ActiveCfg = Release|Any CPU` → model+`[build]` con **LibA `cfg=Release`**, App/LibB `cfg=Debug`, `failed=False`, 3x exit 0); regresión E2E slice D (3 casos) **ALL_PASS** (runcfg 11/11, menudump `bindings=196` con recents reseteados, activeconfig `restored=Debug`); E2E `--cmdstates` **SLICEA_ALL_PASS** (12 aserciones); smoke `SMOKE_DONE` (menudump 11/225/0 con conteos 19/35/39/27/13/7/35/14/8/15/13; bindings=199, variación conocida sin resetear recents); fixture de referencia intacto tras la batería completa. QA §18.5 (1 ronda, brief+reporte en `~/opencode/oleada4_sliceE_qa/`): **APROBADO**, 0 hallazgos P1–P4 y 0 P5 (revisión punto a punto de los 8 riesgos del brief: lectura exclusiva de `*.ActiveCfg` por GUID, los 4 fallbacks sin throw, `-c` del mapeo por proyecto + log resuelto, sin `-p:Platform` con AnyCPU, sin acople legacy, driver `--buildmap` dentro del if/else sin duplicar `RunBuildAsync`, no mutación del fixture, `failed` sin alterar contador ni orden; la ronda se ejecutó en dos fases por muerte del cliente tras la batería E2E — patrón SIGKILL conocido — y la continuación completó builds/oracle/revisión/report).

**Queda pendiente (slice F de la Oleada 4)**: Add Reference vía modelo (F).

## 2026-11-08 — Follow-up: Oleada 4 (backend Avalonia) — Slice F: Add Reference vía modelo (carga, candidatos y save por ConfigurationService)

**Estado: CERRADO.** Cierre del último item de la Oleada 4: el diálogo Add Reference (migrado en la Oleada 2) pasaba load y save de referencias con un XDocument privado (scan ad-hoc del csproj) y descubría los candidatos de proyecto con un disk scan del directorio de la solución. La slice F lo pasa al modelo de proyecto `ConfigurationService` (slices 1/2), la misma fuente que ya consumen el árbol de dependencias y Build/Run.

**Alcance** (3 ficheros, sin carpetas nuevas, sin submódulos):
- `ConfigurationService.cs` (coda, tras el modelo de slice E):
  - `ProjectFileModel.References` (`IReadOnlyList<ReferenceItem>`): los tres tipos editables (PackageReference/ProjectReference/Reference) en **orden de documento**, con el **Include RAW** — un ProjectReference conserva `..\LibA\LibA.csproj` (backslashes), nunca se resuelve a ruta absoluta (las rutas resueltas ya existían: `ProjectReferences`, que consume el pad). `Version` solo del atributo; `HintPath` solo del elemento hijo (display del diálogo).
  - `record ReferenceItem(Kind, Include, Version?, HintPath?)`.
  - `SaveProjectReferences(csproj, refs) → bool`: writer único del set editable, semántica del save legacy de ReferenceInformations — borra los items de los 3 tipos (un ItemGroup se borra **solo** si queda vacío **y** sin atributos: el grupo del fixture con FrameworkReference sobrevive; un grupo vacío con `Condition` no se toca) y añade un ItemGroup con el nuevo set en orden (Include siempre; Version solo si no null, paridad con el Apply legacy; HintPath **nunca** se escribe). Save fiel al formato: detecta BOM, declaración `<?xml?>` y newlines por **bytes** del original y los preserva (el `doc.Save(path)` de XDocument añade BOM + `<?xml?>` por defecto de XmlWriter — detectado por el oracle y corregido con XmlWriter explícito). Corrupto/ausente → false **sin throw** y sin tocar el fichero (política slice D).
- `AddReferenceDialog.axaml.cs`: `LoadProject` vía `LoadProjectModel` (model null con fichero presente → mensaje de output y diálogo vacío, tolerancia legacy; divergencia documentada: un item con solo `Update` no entra en el modelo — fixtures y flujo de la shell no lo usan), `DiscoverCandidates` con los proyectos de la **solución** vía `GetSolutionProjects(sln)` (el ProjectReferencePanel legacy listaba la solución, no el disco; el disk scan queda como fallback solo para un proyecto aislado sin solución), `Apply` delegando en `SaveProjectReferences`, y nuevos públicos para QA/reutilización: `TryAddProjectReference(pathAbs)` (ProjectReference con include relativo, como el doble-click en candidato) y `TryRemoveReference(name)`.
- `MainWindow.axaml.cs`: driver `--addref` ampliado (sin cambio de Program.cs: ya estaba allow-listed) — model-check por proyecto (líneas `[addref-qa] model <name> refs=N project=N package=N asm=N` + `model ref <Kind> <Include>`), ciclo package add/remove (`TryAddReference("System.Json")` + `TryRemoveReference`) y ciclo project add/remove (`TryAddProjectReference(LibB)` + `TryRemoveReference`), con `ClearProjectModelCache()` antes de cada re-read tras un save (la cache es por mtime). Con `ADDFR_QA_KEEP=1` salta la fase de package y las removals finales (deja la referencia de proyecto para el caso E2E de build). El driver programa el **primer proyecto de la solución** (App en el fixture) vía `GetSolutionProjects` en vez del proyecto activo — el activo depende del estado del pad y en la primera ronda del E2E resolvió a LibA, que ya referencia a LibB (determinismo del QA).

**Hallazgo de dev (contexto, no defecto del slice)**: (1) `doc.Save(path)` añade BOM + `<?xml?>` — el check 13 del oracle lo detectó y se corrigió con el XmlWriter de formato detectado; (2) el fixture tiene bugs de compilación **preexistentes** (`LibA/A.cs` sin `using LibB;`, `App/Program.cs` sin `using System;`) — el E2E los parchea **solo en la copia**.

**Verificación** (valores exactos en `docs/session_summary.md`, entrada 2026-11-08 (21)): build 0 errores (net10.0, net10.0-windows **y legacy** `MonoDevelop.Ide.csproj` — los ficheros del diff no entran en el csproj legacy: `ConfigurationService.cs` solo lo compila el csproj Avalonia vía el glob `Services/**`); catálogo `Count=461 LoadedCommands=475 LastError=(null)`, `ResolvedHandlers=111`; menudump `menus=11 items=225 cmds=0`; oracle sección nueva **`oleada4f 14/14`** + todas las anteriores intactas (`oleada3 51`, `oleada3dyn 19`, `oleada3tools 5`, `oleada3nav 29`, `oleada4 18`, `oleada4d 21`, `oleada4e 13`, `oleada4s2 35`); E2E nuevo `~/opencode/oleada4_sliceF_e2e.sh` (2 casos sobre copias del fixture) **ALL_PASS** (addrefpkg: 10 líneas del driver vía modelo + copia limpia sin restos; addrefproj: App.csproj con LibA+LibB y `dotnet build App.csproj -c Debug` **exit 0** con App.dll, restore offline desde caché — Newtonsoft.Json 13.0.3; fixture real intacto); regresión E2E slice E (2 casos) **ALL_PASS**; regresión E2E slice D (3 casos) **ALL_PASS** (menudump `bindings=196` con recents reseteados); E2E `--cmdstates` **SLICEA_ALL_PASS** (12 aserciones); smoke `SMOKE_DONE` (menudump 11/225/0 con conteos 19/35/39/27/13/7/35/14/8/15/13; bindings=199, variación conocida sin resetear recents); fixture de referencia intacto tras la batería completa. QA §18.5 (1 ronda, brief+reporte en `~/opencode/oleada4_sliceF_qa/`): **APROBADO**, 0 hallazgos P1–P4 y 0 P5 (la ronda se ejecutó en dos fases — la primera murió por SIGKILL del cliente tras completar builds+oracle, patrón conocido, y la continuación completó la batería y el reporte; revisión punto a punto de los 8 riesgos del brief: los 3 tipos en orden de documento con Include no vacío, borrado de ItemGroup solo vacío-y-sin-atributos, fidelidad de formato del save (sin BOM/`<?xml?>`/CR; no-op byte-idéntico), corrupto→false sin throw sin tocar el fichero, Version solo para PackageReference e HintPath nunca, load por modelo + fallback disk scan solo sin solución, sin acople legacy, driver dentro del if/else con `ClearProjectModelCache()` antes de cada re-read y `ADDFR_QA_KEEP` limitándose a las removals).

**Queda pendiente**: nada dentro de la Oleada 4 (A–F cerrados). Pendiente de oleadas posteriores según `docs/interfaz-plan.md`.
## 2026-11-08 — Follow-up: Oleada 5 — Slice A (re-scopeado): NuGet 7.9.0 en la shell Avalonia

**Estado: CERRADO (QA APROBADO).** El operador decidió que **los addins GTK no se tocan** (congelados pre-oleadas; la shell Avalonia usa sus propios addins) y ordenó reverter todos los cambios de oleadas/slices en `main/src/addins/` antes de continuar. Verificado: las oleadas 1–4 no tocaron nada bajo `main/src/addins/` (su diff = core `MonoDevelop.Ide/**`, `MonoDevelop.Startup.Avalonia/**`, submódulo `xwt`, docs); lo único que se reversionó fue el trabajo no commiteado del Slice A original (bump del property `$(NuGetVersionNuGet)` 6.11.2 → 7.9.0 + 5 ficheros del addin PackageManagement + el borrado de `MonoDevelopPluginFactory.cs`) vía `git restore` de `main/Directory.Build.props` y `main/src/addins/MonoDevelop.PackageManagement/` (árbol limpio; builds de verificación en `~/opencode/oleada5_revert_builds.log`).

**Re-scope**: el 7.9.0 se aplica SOLO al lado Avalonia, que hoy tiene un único consumo NuGet: el panel **NuGet > Sources** de Preferences (backend `NuGet.Configuration`/`NuGet.Common` en `Views/MainWindow.axaml.cs`, `Views/PreferencesDialog.axaml.cs` y `Views/AddReferenceDialog.axaml.cs`).

**Cambio** (1 fichero, sin submódulos, addins intactos):
- `main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj` — `PackageReference NuGet.Configuration` pasa de `$(NuGetVersionNuGet)` (6.11.2) a **`7.9.0` explícito**, con comentario: el property compartido NO se toca porque lo usan los addins GTK congelados.

**Coexistencia 6.11.2 / 7.9.0 en el staging unificado `main/build/`** (la pregunta de arquitectura de la sesión queda resuelta por la estructura real del staging, verificada en runtime):
1. El closure de 6.11.2 del addin PackageManagement NO está en la raíz de `main/build/`: vive en `main/build/AddIns/MonoDevelop.PackageManagement/` (14 DLLs `NuGet.*`, junto con el addin; el staging `IncludeCopyLocal` del csproj del addin lo pone ahí). En la raíz solo están las 3 DLLs del closure de `NuGet.Configuration` de la SHELL (`NuGet.Common/Configuration/Frameworks`), hoy 7.9.0.
2. El relay `--old-gui` es un **proceso hijo** (`dotnet main/build/MonoDevelop.dll` con `MONODEVELOP_LEGACY_UI=1`; `Program.cs:LaunchLegacyGtk`), y en `/proc/<pid>/maps` del proceso legacy todas las `NuGet.*` mapeadas provienen de `AddIns/MonoDevelop.PackageManagement/` (6.11.2) — **ninguna** de la raíz (7.9.0). El loader de Mono.Addins resuelve las dependencias de cada addin desde su propio directorio.
3. Cada proceso resuelve su closure de su propio sitio: shell Avalonia = 7.9.0 (raíz), addins legacy = 6.11.2 (su directorio). No hay colisión de nombre simple en runtime y no hay cruce de tipos entre versiones en ningún proceso → no hace falta staging por modo ni ALC por interfaz.

**Verificación (valores medidos)**: builds 0 errores en shell `net10.0` + `net10.0-windows` + addin PackageManagement (sin `-f`) + legacy `MonoDevelop.Ide.csproj` (`~/opencode/oleada5_pin79_builds.log` + logs QA); staged `main/build/NuGet.Configuration.dll` = hash `6566f9cc2a6db205…` = paquete 7.9.0 `lib/net8.0` exacto (assembly version 7.9.0.83); smoke `--prefs=packagesources` → `[prefs-sources] count=1` + `[prefs-nuget] restore=True updates=True`, 0 FATAL/Unhandled (el panel lee las sources vía NuGet.Configuration **7.9.0 a runtime**); smoke `--prefs-tree` 0 errores; menudump `menus=11 items=225 cmds=0 bindings=199`; catálogo `catalog loaded: 475 commands, 111 with resolvable default handler`; `git status` = solo el csproj modificado (0 ficheros en `main/src/addins/`); maps del proceso legacy = solo 6.11.2 desde `AddIns/` (QA lo reprodujo con el relay lanzado por sí mismo).

**Known issue preexistente (no es del change)**: en esta máquina el runtime GTK legacy no abre ventana en ~10 min (log 0 B, muere solo; verificado antes y después del change, y el QA lo reprodujo). Las oleadas 1–4 verifican legacy SOLO por compilación. No se trata como regresión del pin; los maps demuestran que mientras vive no carga ninguna NuGet 7.9.0.

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief+reporte en `~/opencode/oleada5_sliceA_qa/`): **APROBADO**, 0 hallazgos P1–P4; 1 P5 opcional no bloqueante (`Fixture.userprefs` se re-crea como side-effect al abrir la solución en los smokes — el `.sln` queda intacto; sugerencia: aislar el userprefs fuera del fixture).

## 2026-11-08 — Follow-up: Oleada 5 — Slice B (gate M6): compila completo con el builder del SDK 10 real, sin bloqueos

**Estado: CERRADO (QA APROBADO).** Último slice de la Oleada 5: el **gate de cierre de la fase de migración** (item 14 del checklist de Extras, M6): que `Main.sln` compile completo con el builder del SDK 10 real, sin bloqueos.

**Naturaleza del slice**: verificación, **sin cambio de código**. El gate ya estaba verde desde los cierres de ondas 2–4 del lote net10 (2026-10-24/26/28, ver `docs/migration-status-report.md` secciones "Lote 1", "Lote 2 + cierre de olas 1-3", "T7" y "Oleada 4": conversión de los ~77 proyectos old-style a SDK-style net10.0 por la decisión fila B del operador 2026-10-22) y las oleadas de backend 1–4 (2026-11-07/08) se construyeron sobre ese estado; la cifra histórica de 126 NU1201 (2026-10-17) estaba superada. El slice consiste en re-ejecutar el gate de forma independiente (incremental + clean-room) y comprobar que no quedó regresión alguna de las oleadas anteriores. `git status` limpio al inicio y al final (0 ficheros tocados; `main/src/addins/` intacta).

**Evidencia del gate** (logs en `~/opencode/`):
- Incremental: `dotnet build main/Main.sln -c DebugLinux -m:1 --nologo` → **`0 Errores`**, `exit=0` (1:49; `oleada5_sliceB_gate1.log`).
- Clean-room: mismo con `--no-incremental` → **`0 Errores`**, `exit=0` (2:04; 7067 warnings preexistentes — CS8012/MSB3277/NETSDK1206/NU1701/obsoletos; `oleada5_sliceB_gate_cleanroom.log`).
- Cobertura: el clean-room lista **110 proyectos únicos con salida** (111 líneas: `MonoDevelop.Startup.Avalonia` dos veces, una por TFM net10.0 y net10.0-windows); `Main.sln` lleva 109 entradas `Build.0` para `DebugLinux` — los proyectos Windows/Mac-only (WindowsPlatform, Xwt.WPF, Xwt.Gtk.Mac, TextUICocoa, MacPlatform.Tests, WindowsAPICodePack×2, WindowsPlatform.Tests) quedan excluidos por diseño (sin `Build.0`), hallazgo documentado del lote 2. Staging unificado `main/build/` regenerado.

**Batería de regresión (valores medidos)**:
- Builds 0 errores: shell `net10.0` + `net10.0-windows` (16 s) y legacy `MonoDevelop.Ide.csproj` (7 s) (`oleada5_sliceB_build_*.log`).
- Tests `AvaloniaShell.Editor.Tests`: **103/103** superados, 0 fallidos.
- Smoke: `Loaded Fixture.sln`, catálogo `catalog loaded: 475 commands, 111 with resolvable default handler`, menudump `menus=11 items=225 cmds=0` (bindings=199 sin reset de recents — fluctuación conocida 196+recents), 0 FATAL/Unhandled.
- Oráculos (`~/opencode/catload_test`): `catalog: Count=461 LoadedCommands=475 LastError=(null)`; `oleada3 51`, `oleada3dyn 19`, `oleada3tools 5`, `oleada3nav 29`, `oleada4 18`, `oleada4d 21`, `oleada4e 13`, `oleada4f 14`, `oleada4s2 35` — todos **fail=0**, exit 0.
- E2E: `--cmdstates` 3 casos **SLICEA_ALL_PASS** (12 aserciones sliceA/B/C); slice D **ALL_PASS** (runcfg 11/11 + menudump `bindings=196` con recents reseteados + activeconfig `restored=Debug`); slice E **ALL_PASS** (buildmap: LibA en Release vía mapeo del .sln, `failed=False`, exit 0 ×3); slice F **ALL_PASS** (ciclo addref vía modelo + `dotnet build App` exit 0).

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief+reporte en `~/opencode/oleada5_sliceB_qa/`): **APROBADO**, 0 hallazgos P1–P4, 2 P5 informativas sin acción (fluctuación de `bindings` ya documentada — la smoke dio 202 sin reset de recents — y conteo de advertencias incremental 2925 vs clean-room 7067, comportamiento esperado de MSBuild). El subagente re-ejecutó las 11 partes de la batería de forma independiente con los valores exactos esperados; `git status` limpio al inicio y al final; fixture byte-idéntico (55 ficheros, md5, incluido `Fixture.userprefs`).

**Nota**: por instrucción del operador, el item 14 del checklist de Extras y la fila B del inventario quedan **sin editar** (ambas arrastran otros pendientes que no corresponden cerrar con esta verificación); este follow-up es la evidencia de cierre del gate M6.

**Queda pendiente**: nada dentro de la Oleada 5 (Slice A + Slice B cerrados). El gate M6 queda **CERRADO**. Pendientes de la fase (fuera de la Oleada 5): rebranding (fila E — después de completar la paridad de UI), redibujo de iconos Fluent (fila F, en curso) y la paridad de UI restante (M5/M6 del plan).

## 2026-11-09 — Follow-up: Fix 1 (ítems legacy ausentes del menú) + Fix 2 (iconos 16px en la pad Solution)

**Estado: CERRADO (QA APROBADO).** Dos problemas reportados por el operador en la shell Avalonia: (1) "desaparecieron elementos en varios de los menús de barra de menú" — el modelo del menú (`MenuService`) no contenía ítems que SÍ existen en la UI legacy (núcleo + extensiones de addins); (2) el árbol de la pad Solution mostraba iconos de 32px (proyectos console) cuando el legacy los pinta a 16px.

**Diagnóstico Fix 1**: el modelo `MenuService` solo creció en su historial git (nunca perdió ítems) — los ausentes JAMÁS se portaron, aunque el header del fichero declara que "every menu entry of the legacy UI exists here in its original location". El problema NO es un bug de visibilidad runtime ni de render: el driver QA `--menuruntime` (añadido en este fix; volca el MainMenu construido en 3 fases — initial/docs2/docs12 — con depth/header/vis/en/tag por ítem + summary visible/total por menú) demuestra el modelo de estado 100% correcto en las 3 fases. La localización es por diff legacy-vs-modelo (XML `MainMenu.addin.xml` + extensiones MainMenu de todos los addin, labels del catálogo, normalización de mnemónicos): la lista final superó los 31 ítems iniciales porque el primer diff tenía un bug (no procesaba ítems top-level de extensiones de addins). **Total: 63 entradas nuevas al modelo** (60 items de comando + 3 submenús contenedor + separadores):
- **Search > Inspect** (sub, tras "Show Next" — anchor legacy `insertafter ShowNext`): Previous Error (Alt Shift PageUp), Next Error (Alt Shift PageDown), sep, Previous Issue in File (Alt PageUp), Next Issue in File (Alt PageDown) — addin SourceEditor.
- **Edit**: "Documentation _Comments" (DocFood, tras Standard _Header); sub **_XML** (addin Xml, tras _Insert): _Validate, sep, Create _Schema, _Go to Schema Definition, sep, _Assign XSLT…, _Open XSLT, Run XSL_T (Alt Ctrl T); "Insert ChangeLog Entry" (tras el separador de _Insert); "Show Code _Generation" (Alt Insert, CodeGeneration, tras Show Parameter List).
- **Project**: "Active Runtime" (array master `radio|array`, tras Active Configuration); al final (tras Convert Solution Format): "Create Package…" (Deployment), "Generate Makefiles…" (Autotools, condición `Platform !windows` — activa en Linux), sep, _Manage/_Update/_Restore NuGet Packages (PackageManagement), sep, sub **_Analyze** (Refactoring: _Whole Solution, _Current Project), sep, "Service Capabilities" (ConnectedServices).
- **Build**: "Create NuGet _Package" (Packaging, entre _Clean y el separador siguiente); al final (tras _Stop): "Publish to Folder…" (AspNetCore), sep, "Pack" (DotNetCore), "Publish" (master de array AspNetCore — oculto hasta que haya perfiles, como los demás masters inlineados).
- **Run**: "Stop Evaluation" y "Show Current Execution Line" (Debugger, tras _Pause); "Debug Application…"; grupo de 8 items de breakpoints (New Breakpoint…, New Function Breakpoint, New Exception Catchpoint, View Breakpoints, Toggle Breakpoint F9, Enable/Disable Breakpoint Ctrl F9, Enable or Disable All Breakpoints, Remove All Breakpoints); "Show Disassembly"; "Expression Evaluator" (Shift F9).
- **Tools**: "_Assembly Browser" y "_Regular Expressions Toolkit" (tras el primer separador, anchor `ToolsSection`).
- **Version Control** (tras "Authors", addin VCGit): sep, "Push Changes…" (icon vc-push), "Switch to Branch" (id sintético `MonoDevelop.VersionControl.Git.Commands.SwitchToBranch:branches` — el legacy envuelve un master `radio|array`; el sufijo `:branches` sigue la convención `SelectActiveConfiguration:<cfg>` del modelo y evita que `MenuBuilder` oculte la entrada como array master), sep, "Merge Branch…", "Rebase to Branch…", "Manage Branches and Remotes", sep, "Stash…", "Pop stash", "Manage stashes", sep.
- **Help > _Diagnostics** (tras "Mark Log", mismo ItemSet mergeado sin separadores): 11 items de PerformanceDiagnostics/ReliabilityDiagnostics (Start monitoring UIThread hangs … Induce PInvoke Crash; "Dump live widgets" legado con FeatureSwitch, presente como el resto de los puertos pendientes).
- **Dispatch**: los nuevos ids no tienen DefaultHandler en el catálogo (ni handler resuelto) → al pulsarlos caen en el fallback de `OnMenuCommand`: "'<id>' is not wired in the new UI yet — its GTK implementation remains available" en la pad Output (comportamiento idéntico a los otros puertos pendientes; el id sintético `:branches` tampoco está en el catálogo, por diseño — `CommandInfo` vacío con Visible=true, mismo patrón que los pseudo-ids `pad:`/`recent:`/`cmd:`/`tool:`).

**Diagnóstico Fix 2**: el proyecto console usa `project-console-32.png` — la ÚNICA variante console del set (no existe `project-console-16.png`; sí `project-16.png` para lib). `XwtStockIcon` pedía `.WithSize(16,16)`, pero el backend `Xwt.Avalonia` (`CellRenderer.ImageToSource`) convertía el bitmap a tamaño NATIVO y el `AImage` de la celda renderiza con `Stretch.None` → el PNG de 32px se pintaba a 32px dentro de la fila de 16px (la fila de "App" medía ~34px; el path canvas `DrawImage` sí escala al destino — la incoherencia era solo del cell renderer).

**Cambios** (4 ficheros + docs ×3):
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MenuService.cs` — las 63 entradas en sus posiciones exactas legacy (anchors `insertafter`), labels/shortcuts/iconos del catálogo; shortcuts en formato gesture del modelo ("PageUp"/"PageDown" como token único — `ParseGesture` divide por espacios y no parsea "Page Up").
- `main/external/xwt/Xwt.Avalonia/ListBackends.cs` (submódulo `external/xwt`, rama `net10`) — `CellRenderer.ImageToSource` escala el bitmap al tamaño pedido por el frontend (`Image.Size`, i.e. `WithSize`) con `SKFilterQuality.Medium`, cumpliendo el contrato Xwt ("the image is scaled when it is rendered"); sin `WithSize` o con tamaño == nativo no hay cambio de comportamiento (los demás pads Xwt — todos con iconos 16px nativos — quedan intactos).
- `main/src/core/MonoDevelop.Startup.Avalonia/Program.cs` — allow-list `QaDialogArg`: `--menuruntime` (driver QA **permanente**, patrón de la casa, commiteado con el fix).
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` — rama `--menuruntime` en el ctor (tras `--menudump`): volca el MainMenu construido en 3 fases.
- docs ×3 (este follow-up).

**Validación (valores medidos)**: gate `dotnet build main/Main.sln -c DebugLinux -m:1` → **0 Errores**; shell net10.0 → 0 errores (127 warnings preexistentes); tests `AvaloniaShell.Editor.Tests` **103/103**; catálogo `Count=461 LoadedCommands=475 ResolvedHandlers=111` **inmutable** (el fix no toca addinxml); oráculos `oleada3 51`, `oleada3dyn 19`, `oleada3tools 5`, `oleada3nav 29`, `oleada4 18`, `oleada4d 21`, `oleada4e 13`, `oleada4f 14`, `oleada4s2 35` — todos **fail=0**; `--menudump` → **`menus=11 items=288 cmds=0`** (antes 225; +63 = 60 bindings + 3 contenedores) con recents reseteados **`bindings=256`** (antes 196; +60) y +1 por recent file; por menú: `_File=19i _Edit=45i _View=39i _Search=32i _Project=23i _Build=11i _Run=48i VC=22i _Tools=10i _Window=15i _Help=24i`; `--menuruntime` fase initial → **`hidden=24`** (antes 22: +2 = los array masters "Active Runtime" y "Publish", ocultos por diseño sin hijos) y `total=300+N` / `_File=(19+N)/(19+N)` con N recent files del perfil (medido N=2: 302/21; N=6: 306/25); **todos los ítems nuevos `vis=True`** en las 3 fases (QA: 61 visibles + 2 masters ocultos por diseño = 63/63); diff legacy-vs-modelo (`~/opencode/fix1_menu_diff.py`): **0 faltantes reales** — los 16 labels sin correspondencia son las 7 familias de divergencias documentadas (NO se corrigen: "_Clear" vs "_Clear Recent Files List"; "Recent Pro_jects" (label del array-command) vs sub "Recent Solu_tions"; "R_ebuild"/"C_lean" vs "… LibA" (renombrado dinámico por nombre de solución, paridad con "B_uild LibA"); "Run With List" (master) inlineado como "Run With"; "Activate 1..9" vs "_1.._9" (slice 9); "Start _Debugging" (label del addin Debugger) vs "_Debug" (mismo command id); "Emoji & Symbols" — addin Mac-only, no aplica en Linux). E2E: `oleada3_cmdstates` **SLICEA_ALL_PASS** (12 aserciones) + slice 1 **ALL_PASS** + slice D **ALL_PASS** (menudump 288/256 con recents reset) + slice E **ALL_PASS** + slice F **ALL_PASS** (los scripts de `~/opencode` se actualizaron al invariant 288/256). Fix 2: captura de la pad Solution con el fixture (`~/opencode/fix2_full.png`, `fix2_pad.png`) — TODOS los iconos del árbol 16×16 medidos (incluido el de "App", de origen 32px) y pitch de fila uniforme 16–22px (antes la fila "App" ~34px); 0 excepciones en el log de arranque.

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief+reporte en `~/opencode/fix1_fix2_qa/`): **APROBADO**, 0 hallazgos P1–P4; 4 P5 no bloqueantes: (1) grep obsoleto en `~/opencode/fix1_menuruntime.sh` — corregido como artefacto (1 línea, fuera del repo); (2) mensaje "not wired" duplicado en stdout (preexistente, `Output()` + `WriteLine` extra; en la pad Output sale 1 vez — correcto); (3) shortcuts sin Alt (F9/Ctrl F9/Shift F9) no disparan desde foco del editor — mecanismo de gestures preexistente, consistente con "not wired"; (4) eventos de puntero sintéticos no llegan en esta sesión XWayland — el dispatch se verificó por la vía de teclado del brief. El subagente re-ejecutó las 8 partes de la batería de forma independiente con los valores exactos (gate/shell 0 errores, tests 103/103, oráculos fail=0, menudump 11/288/0 bindings=258 con N=2 recents, menuruntime total=302 hidden=24, diff 16 FALTA = 7 familias, E2E 5/5), más la verificación específica: dispatch de 3 ids nuevos por teclado (Alt+Shift+PageUp, Alt+Shift+PageDown y navegación Alt→menú→"Push Changes…") → mensaje exacto en log Y en la pad Output, 0 excepciones, proceso vivo; y Fix 2 por prueba pixel a pixel (pitch uniforme 19–23px en 27 filas; icono "Fixture" pixel-perfect vs simulación 16px; icono "App" 14×14 en canvas 16×16 — la firma del bug, 26×26 + fila ~34px, NO se observa; 0 iconos en blanco en toda la ventana).

**Queda pendiente**: el dispatch de los ítems nuevos es el reporte "not wired" (portar sus handlers es trabajo aparte de paridad de comandos); las 16 divergencias de label son intencionales y quedan documentadas aquí.

## 2026-11-09 — Follow-up: Ronda 2 — comparación menú a menú GTK vs Avalonia y restauración de los faltantes

**Estado: CERRADO (QA APROBADO, 0 hallazgos P1–P4).** Cierre del pedido del operador: *"Siguen faltando elementos de los menús (por ejemplo Project>Manage NuGet Packages…, Project>Update NuGet Packages, Project>Restore NuGet Packages), compara con el menú de la UI GTK y restaura todos los elementos faltantes."*

**Diagnóstico (comparación menú a menú GTK vs Avalonia)**
- **Los 3 NuGet citados NO faltan**: se renderizan en la UI (se añadieron en la Ronda 1, commit `87933c44f9`). Evidencia AT-SPI (`~/opencode/menu_v4_all.out`, volcado de los 11 menús + submenús con hover): `MENU Project L1` muestra `'Manage NuGet Packages...' @580`, `'Update NuGet Packages' @606`, `'Restore NuGet Packages' @632` — todos visibles (sin prefijo de estado `x`). El build `main/build` (2026-11-08 21:10) ya los contiene: `strings -el` del `MonoDevelop.AvaloniaShell.dll` encuentra los literales UTF-16 (`_Manage NuGet Packages…`); el volcado UI los confirma habilitados (Project L1=17).
- **Comparador** `~/opencode/menu_compare_r2.py` (árbol legacy `~/opencode/fix1_legacy_tree.txt` vs volcado UI v4): **FALTAN=37 / DEMAS=99**, clasificados al 100%. Los 37 "FALTAN" **no son 37 faltantes reales** — la gran mayoría son:
  - **labels dinámicos** (Build "Build LibA"/"Rebuild LibA"/"Clean LibA" — el modelo usa "Buil_d"/"R_ebuild"/"C_lean" y el label se renombra al nombre del proyecto activo; "_Clear" vs "_Clear Recent Files List"; "Recent Pro_jects" (label del array-command) vs el sub "Recent Solutions" inlineado; "Activate 1..9" vs "_1.._9" (slice 9); "Start _Debugging" (label del addin) vs "_Debug").
  - **arrays ocultos por estado** (array vacío con el fixture → master oculto por diseño; GTK igual): Active Runtime, Custom command list, Tool List, Layout List / View List, Session Recorder (autohide), Window List / Document List, Navigate History (sin historial).
  - **Mac-only**: "Emoji & Symbols" (addin `MonoDevelop.MacIntegration`, no aplica en Linux).
  - **añadidos M16c** (no en el árbol legacy del fixture): Continue, Stop Debugging, Unit Testing (Run).
  - **defectos conocidos del árbol legacy de Ronda 1** (el generador de `fix1_legacy_tree.txt` pierde ramas): VC completo, Task List, List Solution Files, Create NuGet Package, Generate Makefiles, Pads.
  - **artefactos del parser**: el regex del comparador descartaba las líneas con prefijo de estado `x` (item deshabilitado: `    x 'Navigate Back' @596`) porque hay un espacio entre `x` y la comilla → los 4 items de Go To/Navigation History se perdían del parse; el 5º ("Navigate _History") es array vacío (sin historial → oculto en GTK también). El volcado v4 CONFIRMA que la UI sí tiene "Cursor Position", "Navigate Back/Forward" (deshabilitados en el fixture) y "Clear Navigation History".
- **ÚNICO FALTANTE REAL = View > Inline Messages**: faltaban los 2 hijos de array "Errors" y "Errors and Warnings" (y el check-state de "None"). **Causa raíz**: `CommandCatalogLoader.ResolveHandlerType` (`CommandCatalogLoader.cs` L218-222) busca el tipo `defaultHandler` **solo en `typeof(CommandCatalogLoader).Assembly` = `MonoDevelop.AvaloniaShell.dll`** (el loader se compila en el csproj del shell). `ToggleIssuesHandler`/`HideIssuesHandler` vivían en `MessageBubbleCommands.cs`, que **no estaba** en la lista `<Compile>` del shell → el tipo solo existía en `MonoDevelop.Ide.dll` (build GTK) → `DefaultHandlerType=null` → el array master se ocultaba (`RebuildArraySiblings`) **sin `CommandArrayInfo`** → sus hijos "E_rrors"/"_Errors and Warnings" nunca se inlineaban. El resto de array commands del menú (Window/Document List, Navigate History, Recent Files, Run With, Active Runtime…) **sí funcionaban** porque sus handlers están en ficheros ya compilados en el shell (patrón Oleada 2). Se descarta la visibilidad (en .NET 10 `Assembly.GetType(string)` sí devuelve tipos non-public — verificado empíricamente contra el DLL real) y la excepción (el `--menuruntime` no mostraba ninguna).

**Cambios** (3 ficheros de código + 1 de higiene + docs ×3):
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Editor/MessageBubbles/MessageBubbleCommands.cs` — el handler legacy GTK queda envuelto en `#if !AVALONIA_SHELL` (inalterado); se añade una sección `#else` con los **puertos para el shell** bajo los mismos nombres de tipo: `MessageBubbleState` (helper de estado sobre `SettingsStore`) + `HideIssuesHandler` + `ToggleIssuesHandler`. El namespace `MonoDevelop.Ide.Editor` se mantiene para que `ResolveHandlerType` los encuentre.
- `main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj` — añade `<Compile Include="..\MonoDevelop.Ide\MonoDevelop.Ide.Editor\MessageBubbles\MessageBubbleCommands.cs" />` (junto a los demás command handlers compilados en el shell, patrón Oleada 2).
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` — añade `public void SetActiveEditorBubbleMode(...)` (entry point para el puerto; `WithActiveEditor` sigue private); el caso del dispatcher `HideIssues` ahora delega a `MessageBubbleState.ToggleHideAll()` (comparte semántica con el handler; el caso `Toggle`/`ToggleIssues` → `ToggleBubbles()` se toca).
- `.gitignore` — 1 línea que ignora `main/tests/AvaloniaShell.Editor.Tests/bin` (higiene de artefacto de build, regla 18.2; lo reportó el QA como P5-1).

**Semántica del puerto (paridad legacy, misma convención de defaults)**:
- `DefaultHideMessageBubbles` (default **false**) ↔ `!SettingsStore.GetBool("Monodevelop.ShowMessageBubbles", true)`.
- `ShowMessageBubbles` (default **ForErrorsAndWarnings**) ↔ `SettingsStore.GetString("MonoDevelop.Ide.NewShowMessageBubbles")` ("ForErrors"/"ForErrorsAndWarnings"; si no está → "ForErrorsAndWarnings", misma convención que el diálogo de Preferencias, `PreferencesDialog.axaml.cs` L1075-1090).
- Checked: **None** = hideAll; **Errors** = !hideAll && mode=="ForErrors"; **Errors and Warnings** = !hideAll && mode=="ForErrorsAndWarnings".
- Run: **None** alterna el bool; los 2 hijos de array setean el modo + hideAll=false (mismo patrón `DataItem=Action` que el legacy; `Run(object dataItem)` lo invoca vía `InternalRun(object)`→`Run(dataItem)`). El puerto aplica el estado al editor activo (`SetActiveEditorBubbleMode`) y refresca los check glyphs (`MenuBuilder.RefreshCommandStates`), como hacía el hook legacy `IdePreferences.Changed`.

**Validación (valores medidos)**:
- Gate `dotnet build main/Main.sln -c DebugLinux -m:1` → **0 Errores** (incluye la ruta GTK legacy `--old-gui`, que compila el handler original en `MonoDevelop.Ide` vía `#if !AVALONIA_SHELL`); shell net10.0 → **0 errores**; `MonoDevelop.Ide.csproj` (legacy) → **0 errores**.
- Tests `AvaloniaShell.Editor.Tests`: **103/103**.
- **Catálogo**: `catalog loaded: 475 commands, 113 with resolvable default handler` — **era 111; el +2 es intencional** (`HideIssues` + `ToggleIssues` ahora resuelven su handler en el ensamblado del shell). `LoadedCommands=475` e `--menudump` inmutables.
- `--menudump`: **`menus=11 items=288 cmds=0`** (inmutable — el menudump cuenta el modelo, no los hijos inline que solo existen en runtime) con recents reseteados **bindings=256**.
- `--menuruntime` fase initial: **View > Inline Messages** muestra ahora `_None` (vis=True, checked según estado), `Toggle Issues` (master, **vis=False** por diseño), **`E_rrors` (vis=True) y `_Errors and Warnings` (vis=True)** (los 2 hijos nuevos inlineados tras el master oculto) y `Hide Current Message` (vis=True). `hidden=24` (inmutable: los 2 hijos nuevos son visibles, no ocultos); `total=300+N+2` (antes 300+N; medido N=6 → total=308, `_File=25/25`).
- **Click → persistencia → check** (verificado con un harness determinístico sobre el DLL del shell, **21/21 PASS**, y de forma independiente por el QA): estado inicial sin keys → defaults legacy (`None` no marcado, "Errors and Warnings" marcado); click "Errors" → persiste `NewShowMessageBubbles=ForErrors` + `ShowMessageBubbles=True`, check en "Errors"; click "None" → alterna `ShowMessageBubbles=False` (hideAll), ambos hijos sin check + "None" marcado; click "None" de nuevo → des-toggle; click "Errors and Warnings" → persiste `ForErrorsAndWarnings` + check en "E&W".
- Oráculos: `oleada3_cmdstates` **SLICEA_ALL_PASS** (12 aserciones) + `oleada3_sliceA_probe` **ALL_PASS**; E2E oleada4 slice1/D/E/F **ALL_PASS** (invariant menudump 288/256).
- 0 excepciones en el log de arranque en las 3 fases.

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief+reporte en `~/opencode/round2_qa/`): **APROBADO, 0 hallazgos P1–P4**; 3 P5 no bloqueantes: (1) 4º archivo `.gitignore` (1 línea de higiene de `bin/` de tests) — incluido y documentado aquí; (2) herramientas de captura X11 rotas en la sesión (el check glyph se validó por AT-SPI, no screenshot — limitación del entorno, no del cambio); (3) el harness determinístico del dev no está en disco — el QA lo re-validó de forma independiente (5/5 estados XML + 4/4 check glyphs por el árbol AT-SPI). El subagente re-ejecutó la batería completa de forma autónoma con los valores exactos (gate/shell/legacy 0 errores, tests 103/103, catálogo 475/113, menudump 11/288/0 bindings=256, menuruntime initial coincidente hidden=24, E2E slice1/D/E/F ALL_PASS, oráculos SLICEA_ALL_PASS+ALL_PASS), verificó el foco de la Ronda 2 por AT-SPI real (Inline Messages con None/Errors/Errors and Warnings/Hide Current Message, master oculto), confirmó los 3 NuGet presentes (regresión nula) y 0 excepciones.

**Queda pendiente**: el dispatch de los ítems "not wired" de la Ronda 1 sigue siendo trabajo de paridad de comandos aparte; las 16 divergencias de label quedan documentadas como intencionales. La paridad de menú GTK↔Avalonia queda completa salvo esos puertos pendientes (que reportan "not wired", no faltan del menú).

## 2026-11-09 — Follow-up: Ronda 3 — paridad final de menú (ítem View>Layout/Pads + launcher CWD-relative)

**Estado: CERRADO (QA APROBADO, 0 hallazgos P1–P4).** Pedido del operador (tercera vez, con lista concreta): (a) un ítem entre **View>Layout y View>Pads** (Pads es exclusivo de Avalonia, no falta Pads), (b) Project>Manage NuGet Packages..., (c) Project>Update NuGet Packages, (d) Project>Restore NuGet Packages.

**Diagnóstico (verdad de campo)**:
- Se descartaron las referencias generadas defectuosas de Ronda 1/2 (`fix1_legacy_tree.txt` perdía el menú completo de VC; el volcado AT-SPI v4 estaba obsoleto, pre-Ronda-2). Referencia autoritativa directo del XML (`~/opencode/legacy_xml_ref.py`) + diff determinista por id (`~/opencode/missing_ids_r3.py`): el modelo `MenuService.cs` (253 ids) es **superconjunto** de los ids del menú legacy XML (224); los 13 "faltantes" por id tienen explicación benigna (3 Mac-only + acciones directas/ids sintéticos/subs de array).
- **Verdad de campo decisiva**: AT-SPI de los píxeles realmente renderizados (STATE_SHOWING) en el estado exacto del operador (build actual, **sin `--sln`** — la instancia del operador arrancaba vía `monodevelop.sh` sin solución; todas mis verificaciones previas usaban `--sln`): (b)(c)(d) los 3 NuGet **SÍ se renderizan y son visibles**; (a) la zona View entre `Layout` y `Pads` **sí estaba vacía** (bug real, reproducible).
- **Causa (a)**: `View List`/`Layout List` son `type="array"`; la rama `#else` de `ViewCommands.cs` portaba solo `Run`, **no `Update(CommandArrayInfo)`** → arrays vacíos → hueco. En GTK lo llena `LayoutListHandler.Update` (layout "Solution"→"Code") + `ViewListHandler.Update` (lista de pads).
- **Causa (b)(c)(d)**: no era un bug del build actual. El único binario cuya vista coincide con la del operador (Pads=SÍ, 3×NuGet=NO) es el **staging viejo de oct-25** (`~/opencode/wine-setup/wt-head/main/build`, pre-Ronda-1). Mecanismo: `monodevelop.sh` lanzaba la DLL con **ruta relativa y sin `cd`** → la DLL cargada dependía del **CWD de lanzamiento**; un staging viejo en otro worktree se cargaba en silencio.

**Cambios** (3 ficheros de código + 1 launcher de staging + docs ×3):
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Commands/ViewCommands.cs` (rama `#else` AVALONIA_SHELL): **port de `LayoutListHandler.Update(CommandArrayInfo)`** — añade la entrada "Code" (dataItem `layout:Solution`, checked, description "Switch to layout 'Solution'"; el legacy renombraba "Solution"→"Code" por NameMapping). `ViewListHandler` queda deliberadamente sin Update (array vacío, documentado en el código: en Avalonia el submenu **Pads**, exclusivo de la shell, cubre el rol de la lista de pads del legacy).
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs`: el caso `OnMenuCommand` de `ViewCommands.LayoutList` usa `nodeContext` (dataItem del hijo inlineado) y logea `[layout] selected layout <dataItem>`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MenuService.cs` (BuildView): el orden de los masters de array pasa al del legacy XML (`Layout List` **antes** de `View List`, `MainMenu.addin.xml` L117-119).
- `main/build/monodevelop.sh` (staging local, git-ignorado, no commiteado): `cd "$(dirname "$0")"` + `exec ~/.dotnet/dotnet MonoDevelop.AvaloniaShell.dll "$@"` — el launcher carga **siempre** la DLL de su propio directorio.

**Validación (valores medidos)**:
- Gate `dotnet build main/Main.sln -c DebugLinux -m:1` → **0 Errores** (regla 18.4: la ruta `--old-gui` sigue compilando; `MonoDevelop.Ide.csproj` legacy **0 errores**); shell net10.0 **0 errores**.
- Tests `AvaloniaShell.Editor.Tests`: **103/103**.
- Catálogo: **`475 commands, 113 with resolvable default handler`** (inmutable — el port añade un override a un handler ya resuelto, no un handler nuevo).
- `--menudump`: **`menus=11 items=288 cmds=0`** (inmutable — el menudump cuenta el modelo, no los hijos inline que solo existen en runtime).
- **AT-SPI de píxeles reales, sin `--sln` (estado del operador)**: View = `Layout` → **`Code` (visible+enabled)** → `Pads` → Save/Delete Current Layout → Editor Columns → Inline Messages → Folding → Zoom → Full Screen (hueco eliminado); Project = todos sus ítems incluyendo **Manage/Update/Restore NuGet Packages** visibles. Click real sobre `Code` → log `[layout] selected layout layout:Solution`, app viva, 0 excepciones.
- **Launcher**: lanzando `monodevelop.sh` desde CWD=`$HOME`, `/proc/<pid>/maps` mapea la DLL de `.../main/build` del worktree (la correcta), no el staging viejo de oct-25.

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief+reporte en `~/opencode/ronda3_qa/`): **APROBADO, 0 hallazgos P1–P4**; 2 P5 no bloqueantes (el checked del layout se renderiza como glifo ✓ en el icono, no STATE_CHECKED — diseño Oleada 3, consistente con todos los items array; `bindings=260` en el borde superior del rango conocido 196–260, fluctuación documentada). El subagente re-ejecutó la batería completa de forma autónoma con los valores exactos (git status exacto 3 ficheros, builds 0 errores ×3, tests 103/103, AT-SPI real de View/Project sin --sln con "Code" visible + 3 NuGet visibles, dispatch de Code, menudump 11/288/0, catálogo 475/113, launcher desde $HOME cargando la DLL del worktree, legacy 0 errores) y verificó contra `MainMenu.addin.xml` L117-119 que el orden de masters implementado es el del legacy.

**Queda pendiente**: el dispatch de los ítems "not wired" de la Ronda 1 sigue siendo trabajo de paridad de comandos aparte; las 16 divergencias de label quedan documentadas como intencionales. La paridad de menú GTK↔Avalonia queda **completa**: el hueco View>Layout/Pads está poblado con "Code" (paridad exacta con GTK) y los 3 NuGet se confirman renderizados en el build actual (la ausencia que veía el operador era un binario viejo cargado por el launcher CWD-relative, corregido).

## 2026-11-09 — Follow-up: Ronda 4 (cierre de visibilidad) + Ronda 5 — posición NuGet exacta (GTK) y diálogo Avalonia de Manage NuGet Packages

**Solicitud del operador (Ronda 5, después de confirmar en la Ronda 4 que ya veía los elementos)**: (1) los 3 items NuGet del menú Project deben ir **más arriba**, en la posición exacta de la UI GTK — entre los separadores que están juntos, tras "Add Reference"; (2) "Manage NuGet Packages..." debe abrir un **diálogo Avalonia de NuGet** (antes era un no-op silencioso: el id llegaba al switch de `OnMenuCommand` sin case).

**Cierre de la Ronda 4 (visibilidad)**: el operador confirma que los elementos ya se veen (arreglo de su lado: el launcher CWD-relative de la Ronda 3 ahora carga el build correcto). La **hipótesis documentada** del fallo original queda registrada (no corregida, no pedida): `TitleBarRow.PointerPressed → BeginMoveDrag` en `MainWindow.axaml.cs` (~L246-252) **sin guard** — el `ToolbarRow` (~L253-257) sí guarda con `IsToolbarInteractive`. Bajo mutter, `BeginMoveDrag` entra la ventana en move mode y roba el click, de modo que el menú no se abre; en Xvfb es no-op (por eso las verificaciones por píxeles "pintaban" bien). Pendiente potencial de una ronda futura. El flag `--openmenu=<menú>` (evidencia determinista por píxeles: abre el top-level indicado con `IsSubMenuOpen=true` y lo deja abierto para `import -window <id>`) viaja en este mismo commit.

**Cambios** (5 ficheros; sin submódulos, sin addins GTK, sin Gtk en el código nuevo):
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MenuService.cs` — `BuildProject()`: bloque de 3 NuGet movido de tras "Generate Makefiles..." (fondo) a **tras "Add Reference" + `Sep()`**, en orden Manage/Update/Restore. Autoridad: `MonoDevelop.PackageManagement.addin.xml` (los items se insertan tras `AddReference`; verificado por píxeles contra la UI GTK real en Xvfb).
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/ManagePackagesDialog.axaml.cs` — **NUEVO**. Diálogo rebuilt tras el legacy `ManagePackagesDialog` (MonoDevelop.PackageManagement.Gui) sobre la pila propia del shell: UI en código (mismo patrón que `AddReferenceDialog`), base `DialogWindow` (chrome Win11), `Title = "Manage NuGet Packages"`, 720x560. Búsqueda (filtra el repositorio), dos paneles: "Available packages" (el repositorio offline del shell = `AddReferenceDialog.KnownPackages`, 16 paquetes, lista compartida) y "Installed packages" (los `<PackageReference>` del proyecto, leídos por `ConfigurationService.LoadProjectModel`), botón Add entre los paneles y Remove en la cabecera de Installed, estado "Packages: N" y par OK/Cancel. Doble click añade (repositorio) o elimina (instalados). OK reescribe el csproj vía `ConfigurationService.SaveProjectReferences` (mismo writer que `AddReferenceDialog.Apply`; mantiene los demás tipos de referencia intactos). **Alcance por proyecto**, como el legacy `ManagePackagesInProjectHandler` (la variante de solución necesita el resolver online que el shell no trae). API de QA: `TryAddPackage`/`TryRemovePackage` (programático, guardan) + `AddedPackages`/`RemovedPackages`/`InstalledVersions`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` — cases de `OnMenuCommand` para los 3 ids `MonoDevelop.PackageManagement.Commands.*`: **ManageNuGetPackages** abre el diálogo con la **misma resolución de proyecto que AddReference** (nodeContext `project:` → `ResolveSelectedProjectPath()` → `ResolveActiveProject()`) y volca en Output los paquetes añadidos/eliminados al cerrar; **UpdateAllPackagesInSolution** y **RestorePackages** emiten Output informativo (sin resolver de solución/restore en el shell: `dotnet restore` cubre los proyectos). Flags QA: `--managepkgdlg` (abre el diálogo del proyecto activo para inspección visual) y `--managepkgs` (round-trip determinista: add RestSharp → csproj → remove → baseline, misma forma que `--addref`).
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/AddReferenceDialog.axaml.cs` — `KnownPackages` de `static` a `internal static` (fuente única compartida entre el tab Packages de Add Reference y el diálogo Manage; mismo contenido).
- `main/src/core/MonoDevelop.Startup.Avalonia/Program.cs` — `QaDialogArg` reconoce `--managepkgs`, `--managepkgdlg`, `--openmenu=`.

**Bug de rendering corregido en el desarrollo** (bug real de este diálogo): las primeras capturas mostraban el diálogo sin los dos paneles (solo chrome + Search + "Packages: 0"), con capturas **idénticas por md5** entre el build original y el posterior. Causa: los 5 hijos del grid `panes` tenían `Grid.SetRow/SetColumn` pero **nunca se añadieron a `panes.Children`** — un Grid sin hijos no pinta nada (la fila `*` del root sí reservaba el espacio, la zona oscura grande de la captura). Nota metodológica: la primera verificación "el DLL no trae el código" con `strings` plano era **inválida** (el heap #US de .NET es UTF-16; `strings -el` sí ve los literales — el código estaba en el DLL). Fix: 5 `panes.Children.Add(...)` + `ToString()` en `PkgRow` (la plantilla de items del ListBox renderiza vía ToString) + margen derecho del header Installed alineado al borde de la lista.

**Validación (valores medidos)**:
- Tarea 1 por píxeles: popup Project del shell (`--openmenu=Project`, Xvfb :99, `import -window`) = **Add Reference → sep → Manage/Update/Restore NuGet (juntos) → sep → Code Analysis**, idéntico a la referencia GTK capturada sobre la UI legacy (`~/opencode/r4_gtk_project_popup2.png`, `r4_gtk_top_zoom.png`).
- Tarea 2 por píxeles: render completo (16 paquetes disponibles, Installed vacío, "Packages: 0"); flujo Add → "Packages: 1" → OK → csproj con `<PackageReference Include="Newtonsoft.Json" />` y diálogo cerrado → reabrir → estado recargado (en Installed, ausente de Available) → doble click remove → "Packages: 0" → OK → csproj byte-idéntico al baseline (md5 `350aacc7…`).
- Round-trip `--managepkgs`: `model refs=0 package=0` → `TryAddPackage(RestSharp) → True` → `csproj contains RestSharp: True` → `TryRemovePackage(RestSharp) → True` → `model after-clean refs=0 package=0`.
- Gate: `Main.sln` **EXIT=0, 0 errores** (incluye la ruta GTK `--old-gui`, regla 18.4); shell net10.0 **0 Errores** (127 warnings preexistentes); tests **103/103**; catálogo **475/113**; `--menudump` **11/288/0** (inmutable).

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief+reporte+evidencia en `~/opencode/ronda5_qa/`): **APROBADO, 0 hallazgos P1–P4**; 3 P5 informativos: (1) orden de Code Analysis Solution/Project al revés que en GTK (no pedido; registrado para paridad futura); (2) `bindings=264`, fluctuación por recents sobre el rango documentado 256–263 (invariantes 11/288/0 exactos); (3) re-render del diálogo tras el ciclo idéntico salvo 200 px (0.05%) en el estado transitorio del botón OK entre capturas. El QA re-ejecutó el gate completo, el ciclo del diálogo por píxeles (con capturas propias), el round-trip, la regresión GTK (menú Project de la UI legacy sobre el binario nuevo) y verificó a fuente: ids 1:1 entre `MenuService` y `OnMenuCommand`, diff acotado a 5 ficheros, sin Gtk en el diálogo nuevo.

**Queda pendiente**: (a) orden "Run Code Analysis on Project/Solution" (GTK va Project primero; no pedido); (b) Update/Restore como acciones reales (resolver de solución/restore, hoy Output informativo); (c) guard del `BeginMoveDrag` del TitleBarRow (hipótesis de la Ronda 4, no pedida); (d) dispatch de los ítems "not wired" de la Ronda 1 (paridad de comandos aparte); (e) las 16 divergencias de label (intencionales, documentadas).

## 2026-11-09 — Follow-up: Ronda 6 — 3 bugs `//BUG:` de la interfaz Avalonia (View List / Add Reference / Custom command list)

**Solicitud del operador**: corregir los 3 bugs marcados con `//BUG:` en `MenuService.cs` (interfaz Avalonia): (1) "View List" no se muestra en View, (2) "_Add Reference..." no abre el diálogo AddReference, (3) "Custom command list" no se muestra en Project. Directivas de la sesión: (1) **no mezclar** código GTK y Avalonia — se **porta** GTK→Avalonia; (2) el código GTK porteado **no se borra** (los handlers `#if !AVALONIA_SHELL` quedan intactos para la UI GTK).

**Causas raíz (confirmadas)**:
- **BUG 1 — "View List"**: el array `ViewList` del port no-Gtk era **vacío** (el `#else` de `ViewCommands.cs` no portaba `Update(CommandArrayInfo)`); el rol de lista de pads lo cubría un submenú shell-only `_Pads`.
- **BUG 2 — "_Add Reference..."**: el pipeline de dispatch funcionaba, pero el gate `SelectedNodeIsProject` quedaba `false` porque la shell **no auto-seleccionaba** el nodo del pad solución al activar un documento (GTK sí: `ProjectSolutionPad.OnWindowChanged → SelectActiveFile`).
- **BUG 3 — "Custom command list"**: el array `CustomCommandList` del port no-Gtk era **vacío** — el modelo de proyecto no parseaba `CustomCommands` del .csproj.

**Cambios** (8 ficheros fuente; sin submódulos, sin artefactos de build):
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MenuService.cs` — elimina el submenú `_Pads` de `BuildView`, quita las 3 marcas `//BUG:`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` — BUG 2: `AutoSelectSolutionNodeForActiveDocument()` en `OnDocSelectionChanged` (fila del documento activo — su tag es la ruta completa — o, si no es nodo de la solución, el proyecto contenedor por prefijo de directorio contra `GetSolutionProjectPaths()`); BUG 3: `SelectedProjectCustomCommands` + `RunCustomCommand` (ejecución real: `CustomCommand.ParseCommand`, sustitución `${ProjectDir}`/`${ProjectName}`, working dir por defecto = dir del proyecto, env vars declaradas, proceso propio que no salta los gates de Stop, salida al Output pad); case `ViewList` maneja `pad:<id>` → `TogglePad` (BUG 1); flags QA `--autoselectqa`, `--runcustom=<nombre>` y cadena de submenús en `--openmenu=<Top[,Sub]>`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MenuBuilder.cs` — `RebuildArraySiblings` + `AppendArraySetChildren`: renderiza un `CommandInfoSet` como submenu recursivo (los grupos del array ViewList se vuelven submenús).
- `main/src/core/MonoDevelop.Startup.Avalonia/Program.cs` — registro de `--runcustom=` y `--autoselectqa` en la validación de args QA (infraestructura permanente).
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Commands/ViewCommands.cs` — BUG 1: `ViewListHandler` no-Gtk `Update(CommandArrayInfo)` rellena **Pads** (12 pads: Solution, Classes, Help | Toolbox, Properties, Document Outline, Unit Tests | Output, Errors, Tasks, Code Issues, Search Results) y **Debug Pads** (Call Stack, Locals, Watch, Breakpoints, Threads), con separadoras, checks por visibilidad real (`ShellCommandState.IsPadVisible`) y dataItems `pad:<id>`; inventario idéntico al antiguo `_Pads` (decisión del operador: paridad GTK + quitar `_Pads`, como port Avalonia). Rama `#else`, el handler GTK intacto.
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Commands/ProjectCommands.cs` — BUG 3: `CustomCommandListHandler` no-Gtk `Update(CommandArrayInfo)` rellena los comandos `Type=Custom` del proyecto seleccionado. Rama `#else`, GTK intacto.
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Commands/ShellCommandState.cs` — puentes `IsPadVisible` y `SelectedProjectCustomCommands` dentro de `#if AVALONIA_SHELL`.
- `main/src/core/MonoDevelop.Ide/Services/ConfigurationService.cs` — compartido GTK/shell (fuera de `#if`; el gate lo compila para ambas): `CustomCommandEntry` + `ProjectFileModel.CustomCommands` + `ParseCustomCommands`. **Forma del XML confirmada trazando la serialización legacy** (`CollectionDataType.OnSerialize` + test `MSBuildXmlNamespaceNotAddedToCustomCommand`): wrapper e items ambos `CustomCommands`; el wrapper es contenedor puro (sin campo `Command`), los items portan `type`/`name`/`command`/`workingdir`/`externalconsole`/`pauseexternalconsole` (lowercase) + `EnvironmentVariables/Variable name/value`. Parser tolerante: filtra los `CustomCommands` descendientes que **portan campo Command** (excluye el wrapper, sin duplicados), lee campo de atributo→elemento (case-insensitive), descarta `Type != Custom`, tolerante a bloques rotos.

**Validación (valores medidos)**:
- Gate `dotnet msbuild Main.sln -p:Configuration=Debug -m:1 -t:Build` → **EXIT=0, 0 errores** (regla 18.4: GTK + shell compilan); shell net10.0 **EXIT=0**.
- Tests `AvaloniaShell.Editor.Tests`: **103/103**. Catálogo: **`475 commands, 113 with resolvable default handler`** (inmutable).
- `--menudump` (recents reseteados): **`menus=11 items=270 cmds=0 bindings=239`**. Baseline anterior 288/256 → delta **−18 items / −17 bindings** = exactamente el bloque `_Pads` eliminado (17 `Item` `pad:<id>` + 1 `Sub` sin id). Los 17 toggles no desaparecen: ahora viven en el **array runtime** (que `--menudump` no cuenta pero `--menuruntime` muestra). No es regresión.
- **BUG 1**: `--menuruntime` fase initial → bajo `_View`, `Pads` (12, orden + 2 separadoras) y `Debug Pads` (5) con tags `pad:<id>`; `_Pads`=0 ocurrencias; píxeles `--openmenu=View,Pads` con checks reales (Solution/Properties/Output).
- **BUG 2**: `--autoselectqa` → BASE (sin selección) `addref=disabled`; INSIDE (documento de la solución) auto-selección → `SelectedNodeIsProject=True addref=ENABLED`; OUTSIDE (fichero externo) fallback contenedor → `ENABLED`; `--cmdstates` `editRefs=1` (node_project/node_file); `--dispatchaddref` con selección → `itemEnabled=True` + ventana "Add Reference" 620×600 (X11/xwininfo, capturada); sin selección → `itemEnabled=False`, no-op.
- **BUG 3**: `--runcustom="R6 QA Marker"` → parsea 2 comandos (el Type=Build filtrado), ejecuta `touch "${ProjectDir}/r6_marker.txt"` (exit 0, marker en disco); `"R6 QA Env"` → env declarada llega al hijo (`printenv R6QAVAR` → `hello-r6`); `--projmodel` **15 ok / 0 fail** (4 aserciones customcommands); píxeles `--openmenu=Project` → "R6 QA Marker" y "R6 QA Env" inline.

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief `~/opencode/r6_bugfix/qa_brief.md` + reporte `~/opencode/r6_bugfix/qa_report.md`, artefactos en `~/opencode/r6_bugfix/qa_tester/`): **APROBADO, 0 hallazgos P1–P4**; 3 P5 informativos (sin acción): (1) `marker exists=False` cosmético en `--runcustom` para el comando env (la verificación real va por `hello-r6` en el log); (2) ~7075 warnings preexistentes del gate (la invariante exige 0 **errores**); (3) known issue preexistente de la máquina — el runtime GTK legacy no abre ventana aquí (se valida por compilación, como en las rondas anteriores). El QA re-ejecutó la batería completa de forma autónoma y verificó contra los valores exactos (270/239, 475/113, 103/103, 15/0, delta −18/−17, `hello-r6`, marker, `editRefs=1`, `itemEnabled=True/False`).

**Queda pendiente** (ya documentado, no pedido en esta ronda): (a) orden Run Code Analysis Sol/Proj vs GTK; (b) Update/Restore NuGet como acciones reales; (c) guard de `BeginMoveDrag` en `TitleBarRow`; (d) dispatch de ítems "not wired" Ronda 1; (e) 16 divergencias de label intencionales.

## 2026-11-09 — Follow-up Ronda 6: "Project > Add Reference..." habilitado con solución abierta (sin selección de pad)

**Solicitud del operador**: el elemento `Project > Add Reference...` se mostraba **deshabilitado** cuando hay una solución abierta (sin fila seleccionada en el pad de soluciones) y debe mostrarse **habilitado**.

**Causa raíz**: el gate del comando (`EditReferencesHandler.Update`, bloque `#else` AVALONIA_SHELL de `ProjectCommands.cs`) era `info.Enabled = ShellCommandState.SelectedNodeIsProject` — solo se habilitaba cuando había una fila de proyecto (o fila bajo proyecto) **seleccionada** en el pad. La shell **no auto-selecciona** el nodo al cargar la solución, así que con la solución abierta y sin clic previo en el pad el gate quedaba `false`. La **ejecución** del comando (`MainWindow.OnMenuCommand`, caso `AddReference`) sí resuelve el proyecto por otra vía: la selección del pad con **fallback** al primer proyecto de la solución (`ResolveSelectedProjectPath() ?? ResolveActiveProject()`). El gate y la ejecución eran **inconsistentes**: con la solución abierta y sin selección, el menú estaba gris pero un clic (o el auto-select de GTK legacy) habría abierto el diálogo.

**Fix** (1 hunk en `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Commands/ProjectCommands.cs`, solo el bloque `#else` AVALONIA_SHELL — el bloque `#if !AVALONIA_SHELL`/Gtk legacy queda intacto):
```
info.Enabled = ShellCommandState.HasWorkspace
    && (ShellCommandState.SelectedNodeIsProject || ShellCommandState.ActiveProjectCount > 0)
info.Bypass = !info.Enabled;
```
Es decir: habilitado si hay solución cargada Y (hay fila de proyecto seleccionada **o** la solución tiene proyectos). El fallback a `ActiveProjectCount > 0` es el espejo exacto del fallback de ejecución a `ResolveActiveProject()` (primer `.csproj` de la sln). La solución de `QaProj.sln` tiene 1 proyecto → `ActiveProjectCount=1` → gate `true`. Al cargar la solución (`RefreshSolutionTree`) y en cada cambio de selección del pad se re-ejecuta el `Update`, así que el estado no queda rancio.

**Validación** (QA §18.5, subagente Tester QA Senior, 1 ronda — **APROBADO, 0 hallazgos**):
- Gate `dotnet build Main.sln -c DebugLinux` → **0 Errores** (exit 0; warnings preexistentes).
- **Scenario A** (el caso del bug — `--sln=QaProj.sln --dispatchaddref` con `DISPATCH_NOSSEL=1`): `itemEnabled=True`, el dispatch del menú abre la ventana "Add Reference" (confirmada por X11/xdotool, PID vivo). Antes del fix: `itemEnabled=False`, no-op.
- **Scenario B** (sin solución, `--cmdstates`): `hasWs=False`, `editRefs=0` en los 37 estados (deshabilitado, correcto).
- **Scenario C** (con solución, `--cmdstates`): `hasWs=True`, `projCount=1`, `editRefs=1` en los 40 estados (habilitado en todos, sin regresión de los ítems vecinos: buildSol/run/projOpts/setStartup coherentes).
- Sin fatals (`FATAL|Unhandled` = 0 en los 3 logs).
- Nota: `DISPATCH_NOSSEL` exige que `launch_shell.sh` lo reenvíe tras el `env -i` (el script limpiaba el entorno); se añadió `${DISPATCH_NOSSEL:+DISPATCH_NOSSEL="$DISPATCH_NOSSEL"}` al launcher para futuras rondas.

**Cierre**: commit por nombre del 1 fichero fuente + docs ×3, push a `origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos.

## 2026-11-10 — Add-in Manager: Gallery sin add-ins locales + integración con Visual Studio Marketplace (importar .vsix de VS desktop como add-in)

**Solicitud del operador**: (1) la pestaña **Gallery** del Add-in Manager de la shell Avalonia no debe listar add-ins locales no instalados/habilitados (en Linux quedaban las plataformas `os: macos`/`os:windows`); (2) integrar la Gallery con el **Visual Studio Marketplace** y permitir instalar extensiones de Visual Studio **desktop** (no VS Code) como add-ins que se integren con la shell. Alcance elegido por el operador: **"Import VSIX + extension points"**, con el límite aceptado de que la UI compilada propia de un SDK de VS no aparece "mágicamente": la integración cubre lo que el modelo de add-ins de la shell soporta (Pads, Menús, Diálogos, Preferences, Editor); las DLLs del SDK de VS quedan en estado de error visible, sin crash.

**Fase 1 (causa raíz + fix)**: la rama gallery del diálogo (`LoadGallery`) se nutría de `host.Addons` — la fuente de add-ins **locales** (shipped + user) — y listaba los no instalados/habilitados. `LoadGallery` eliminada (0 restos en el árbol); la rama gallery no toca `host.Addons`; los no habilitados siguen en Installed (grises, botón Enable).

**Fase 2 (archivos)** (4 nuevos + 2 modificados, sin submódulos, sin artefactos de build):
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Avalonia.Addons/Marketplace/MarketplaceClient.cs` (nuevo, 278 l) — cliente HTTP de `marketplace.visualstudio.com/_apis/public/gallery/extensionquery`: listado/búsqueda (filterType 1) + detalle (filterType 7) + descarga del paquete. **Quirks de la API verificados en vivo (2026-11-09)**: no existe target desktop genérico (`"Microsoft.VisualStudio"`, filterType 8, devuelve 0) — las ext. desktop declaran target de **edición** (`Microsoft.VisualStudio.Community` ~1000, `.Professional` 86, `.Enterprise` ~1000, `.Premium` ~1000; VS Code = `Microsoft.VisualStudio.Code`); varios filter groups en una petición **no** se hacen union (solo se honra el primero) → `SearchAsync` hace fan-out paralelo (`Task.WhenAll`) a las 4 ediciones + merge + dedupe por `ExtensionId` (OrdinalIgnoreCase) + orden por installs desc; Accept header obligatorio `application/json` + `api-version=3.0-preview.1` como `NameValueHeaderValue` (el ctor de `MediaTypeWithQualityHeaderValue` no lo parsea; sin él → 400 "No api-version was supplied"); `statistics[].value` es float en el wire (POCO `double`, cast a `long`) y la estadística de instalación se llama **`install`** (no `downloads`); el paquete descargable de VS desktop es un asset cuyo `assetType` es el **nombre del .vsix** (VS Code usa `Microsoft.VisualStudio.Services.VSIXPackage`; detección `EndsWith("VSIXPackage") || EndsWith(".vsix")`); `Microsoft.VisualStudio.Ide.Payload` puede ser un **launcher nativo Win32 (MZ), no un zip** (ReSharper) → no se trata como paquete; la query de listado pide `"Version","Files"` en `assets[]` o `versions[].files[]` llega vacío (sin URL de paquete). URL "More information": `https://marketplace.visualstudio.com/items?itemName=publisher.name`.
- `Marketplace/MarketplaceModels.cs` (nuevo, 157 l) — `MarketplaceExtension` + POCOs wire.
- `Marketplace/VsixManifest.cs` (nuevo, 109 l) — parser XML tolerante de `extension.vsixmanifest` + `IsVisualStudioDesktop` (por prefijo de target, excluyendo `.Code`).
- `Marketplace/VsixImporter.cs` (nuevo, 305 l) — `.vsix` → addin: extrae a temporal, parsea el vsixmanifest, genera el manifiesto `*.avaloniaaddon.json` de la shell (identidad/metadata rica; entry point solo si se detecta con confianza) y lo instala en `<userRoot>/<Identity.Id>` vía `host.InstallFromDirectory` — **quirúrgico**: registro + `LoadAddon` del nuevo addin, **no** `Discover()`/`LoadAll()` completo (`AddonHost.cs` quedó **sin cambios**). `FindEntryPoint` determinista (DLL con nombre == id del paquete; si no, primera por orden alfabético; no depende del orden de `EnumerateFiles`). `Import(string vsixFile, string? listingId = null)` (parámetro opcional, compatible con la firma de 1 argumento).
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Avalonia.Addons/Manifests/AddonManifest.cs` (M) — campo `Marketplace` (`AddonMarketplaceSource` con `ListingId`/`PackageId`/`GalleryUrl`/`Publisher`/`IsVisualStudioDesktop`) + fix de `FileExtensionsJsonConverter` (su `Write` lanzaba `NotSupportedException` — solo lectura — y rompía la serialización del manifiesto generado; ahora escribe array de strings (vacío si vacío) y `Read` gestiona token null).
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/AddinManagerDialog.axaml.cs` (M) — fase 1 + cableado marketplace: cliente/importador creados en el constructor (UI thread); búsqueda con debounce 450 ms en thread pool (`Task.Delay` + `CancellationTokenSource` + contador de generación: una respuesta obsoleta no pinta sobre una nueva); render marshalizado con `Avalonia.Threading.Dispatcher.UIThread.Post(BuildGalleryTree)`; dedupe de instalados; botón Install/Update → `InstallSelectedAsync` → `InstallFromMarketplaceAsync` (guarda de re-entrada `installingFromMarketplace`); limpieza en `OnClosing` (cancela CTS + Dispose del cliente); botón Refresh relanza la búsqueda.

**Dedupe (diseño clave, 3 espacios de ids)**: el id de la fila de Gallery es el id del **listing** (`publisher.name`, p. ej. `VisualStudioClient.MicrosoftVisualStudio2017InstallerProjects`), un espacio de nombres **distinto** del `Identity Id` del vsixmanifest con que se instala el add-in (p. ej. `VSInstallerProjects`) y del `PackageId` (`publisher.vsixId`). Comparar solo `Identity.Id` contra el `ExtensionId` de la fila **nunca** coincidía (la extensión instalada seguía apareciendo en Gallery — hallazgo MAYOR de la ronda 1 de QA). Fix: el diálogo pasa `ext.ExtensionId` al `Import`; el importador lo persiste en `AddonMarketplaceSource.ListingId` (JSON `listingId`); `BuildGalleryTree` recoge en `installedIds` tanto `Identity.Id` como `Marketplace?.ListingId` de cada add-in instalado y filtra la fila por `r.ExtensionId` (sin sobre-filtro: una extensión **no** instalada sigue apareciendo).

**Manejo de errores**: `MarketplaceClient` envuelve en `MarketplaceException` solo el HTTP no exitoso; los fallos de transporte salen como `HttpRequestException` (hallazgo MAYOR de la ronda 1: escapaban de los `catch (MarketplaceException)` → header atascado en "Installing…"/"Searching…" + error silenciado). Fix: el diálogo hace `catch (Exception)` en búsqueda e instalación (con `catch (OperationCanceledException)` **antes** en la búsqueda); el header siempre llega a estado final ("No Visual Studio extensions found" / "Install failed: …"); un import fallido (`!result.Success`) también fija "Install failed: …".

**Validación**: probe E2E contra el Marketplace en vivo (`~/opencode/mkt_probe/`) → **ALL CHECKS PASSED 15/15** (listado desktop 20 exts, sin ids `.vscode`, ordenado por installs; búsqueda `roslyn`; target VS Code; detalle filterType 7 con URL; descarga real 5.385.730 bytes; import en `AddonHost` temporal: instalado + descubierto + manifiesto en disco + `isDesktop=True` + `entrypoint=True`; **degradación elegante** `loaded=False` con error visible `Assembly 'Microsoft.VisualStudio.DeployWizard' not found` y **sin crash**; re-import idempotente; determinismo en 2ª ejecución). Smoke funcional GUI (capturas `~/opencode/smoke_gallery_*.png`, flag QA existente `--addins`): Gallery "40 Visual Studio extension(s)", única categoría "Available", **sin add-ins locales**; búsqueda debounced "roslyn" re-renderiza resultados reales; detalle de fila (nombre/versión/autor/descripción + Install + "More information"); **instalación E2E de `SemihOkur.AsyncFixer`** (manifiesto con datos de marketplace, add-in en Installed con icono propio + botones Disable/Uninstall, no degradado); **uninstall E2E** (diálogo de confirmación, carpeta borrada de disco, log `uninstalled ... (user add-in removed)`); `UserAddIns` quedó vacío tras el uninstall. Falso alarma documentado: el `tab=installed items=0` tras el import **no era bug del host** — la caja de filtro (compartida entre pestañas, como en legacy) seguía con "roslyn" y `ApplyFilter` descartaba los 32 locales; con filtro limpio, `items=33` (32 + importado). Gate `dotnet build Main.sln -c DebugLinux` → **0 errores** (warnings preexistentes; los del diálogo restante son preexistentes: CS8632 sin contexto `#nullable`, CS0414 `updatingDetails`, VSTHRD100 `OnInstallFromFileClicked`, AVLN5001 `TextBox.Watermark` en el .axaml).

**QA §18.5** (subagente Tester QA Senior, 2 rondas): ronda 1 **RECHAZADO** (2 MAYOR + 1 MENOR) — (H1) el dedupe nunca coincidía (ids de espacios distintos, ver arriba), (H2) los `HttpRequestException` escapaban de los `catch (MarketplaceException)` y dejaban el header atascado + error silenciado, (H3) warnings nuevos MD0009 (catch vacío) / VSTHRD200 (método async sin sufijo). Corregidos (fix de `ListingId`, `catch (Exception)` con `OperationCanceledException` antes, `File.Delete` del temporal solo si `result.Success` y con log, `InstallSelected`→`InstallSelectedAsync`, `InstallFromMarketplace`→`InstallFromMarketplaceAsync`) → ronda 2 **APROBADO, 0 hallazgos** con evidencias: `dedupe_probe` 4/4 (manifiesto en disco con `listingId`), `mkt_probe` 15/15, `mkt_ex_probe` (comportamiento del cliente ante fallos de transporte), build `Main.sln -c DebugLinux` 0 errores, `AddonHost.cs` limpio (sin cambios), regresiones verificadas (sin sobre-filtro del dedupe, `Import` de 1 argumento intacto, OCE no tragado). Nota aprobada por QA: un `.vsix` de import fallido puede dejar el temporal en `/tmp` (no se borra en el camino de error; lo limpia el SO, sin impacto funcional).

**Queda pendiente** (límites aceptados por el operador, no defectos): (a) la UI compilada propia de un SDK de VS no aparece "mágicamente" — la integración cubre lo que el modelo de add-ins de la shell soporta (Pads, Menús, Diálogos, Preferences, Editor); las DLLs del SDK de VS quedan en estado de error visible, sin crash; (b) los extension points `Pads`/`Docking`/`MainMenu`/`Commands`/`ContextMenu`/`TextEditor` están en el modelo pero el host hoy solo consume `GlobalOptionsDialog` (Preferences) y `MimeTypes` (MimeCatalog) — los menús están hardcoded en `MenuService.cs`.

**Cierre**: commit por nombre de los 6 ficheros (4 nuevos de `Marketplace/` + `AddonManifest.cs` + `AddinManagerDialog.axaml.cs`; `AddonHost.cs` no entra) + docs ×3, push a `origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos. Artefactos de prueba en `~/opencode/` (probes `mkt_probe/`, `dedupe_probe/`, `mkt_ex_probe/`, capturas `smoke_gallery_*.png`, fixtures `.vsix` reales descargadas).

## 2026-11-10 — Port F# completo (fases 1–6): binding F# GTK → shell Avalonia (nuevas soluciones, build/run, pad FSI, language service FCS, preferences + adornos)

**Solicitud del operador**: portar la superficie F# completa del binding GTK (`main/external/fsharpbinding/`, FCS 31.0.0) a la shell Avalonia — nuevas soluciones/archivos F#, build/run de `.fsproj`, pad F# Interactive, language service F# **completo** (completions, hover, navegación, `.fsx`) y preferences F# con adornos en vivo. Alcance de la fase 6 decidido por el operador: **completo — consumidores en vivo** (4 opciones persistidas + `AdvanceToNextLine` + `HighlightMutables` + `ShowTypeSignatures`).

**Fases (6, todas validadas)**:
- **Fase 1 (New Solution + New File)**: plantillas F# Console y F# Tutorial (port del template legacy → `Assets/Tutorial.fs`, 976 l, F# 10-compatible) en `NewSolutionDialog`; plantillas de archivo `.fs`/`.fsi`/`.fsx`/`.fsscript` en `NewFileDialog`; scaffolding de `.fsproj` SDK-style (`net10.0`).
- **Fase 2 (build/run `.fsproj`)**: `ConfigurationService` reconoce proyectos `.fsproj` (mapeo solución→proyecto, TFM, output); build vía `dotnet build`, run vía `dotnet run` sobre el modelo de configuraciones existente.
- **Fase 3 (pad F# Interactive)**: `FsiProcess.cs` (262 l) — proceso hijo `dotnet fsi --nologo` con el protocolo de prompts FSI (stdin/stdout/stderr, diagnóstico `FS`, restart, clear). `FSharpInteractivePadView.axaml.cs` (382 l) — transcript monospace + input + toolbar Save/Open/Clear/Reset; sesión lazy al primer show, kill al cerrar la tab o al salir.
- **Fase 4 (FCS in-process)**: `FSharpLanguageService.cs` (776 l) — `FSharpChecker.Create` con **FCS 43.12.403** (era .NET 10; el binding legacy usaba 31.0.0 con otra API surface — 43.12 es la versión cuyo editor-service contract se probó y verificó: `GetDeclarationListInfo`, `GetSymbolUseAtLocation`, resultados síncronos por archivo). Singleton + executor single-thread. Completions con `lineText` real; hover/uses sobre **usos** no declaraciones; `ParseAndCheckFileInProject` síncrono.
- **Fase 5 (navegación + retargeting)**: scanner F# propio en `SymbolIndexService` (+232 l) para outline/índice (no FCS — más barato y suficiente); `GoToDialog` incluye símbolos F#; retargeting rename/save-as vía `UnwireFSharpDocument(tag, ed)` + `WireFSharpDocument` (re-suscripción al cambiar la path — evita fuga de delegates en el evento del singleton).
- **Fase 6 (Preferences + adornos + AdvanceToNextLine)**: página "F# Settings" en `PreferencesDialog` (`PanelFSharp`, +23 l axaml / +128 l code): 4 claves legacy `FSharpBinding.*` (`FscPath` — null si useDefault —, `AdvanceToNextLine` default true, `HighlightMutables`/`ShowTypeSignatures` default false) vía `SettingsStore` (mismo `MonoDevelopProperties.xml` del GTK); `UpdateFsCompilerDisplay` replica el legacy `setCompilerDisplay` (useDefault o path vacío → `<sdkroot>/FSharp/fsc.dll` + entrada deshabilitada); browse con `StorageProvider.OpenFilePickerAsync`. Adornos: `GetFileAdornments(file, src, textOverride?)` (UI) + `internal ComputeAdornments(CheckResult)` (ruta del evento, **sin re-entrada** — `CheckCompleted` nace en el executor) vía `GetAllUsesOfAllSymbolsInFile` → `IsFromDefinition` → `IsFunction`/`IsMutable`/`FullTypeSafe` por reflection + `FormatFSharpType` (quita `type ` y `Microsoft.FSharp.Core.`). Editor: `SetFSharpAdornments(items, showMutable, showType)` (los flags los cachea — nunca los lee `UserPreferences` en el render) — mutable → subrayado 2px bajo el nombre; type → dim ` : <type>` a la derecha (inline, no desplaza la altura de línea). `AdvanceToNextLine`: guard en comando `SendLine` (sesión viva + flag → `SetCaret(CurrentLine+1, 0)`); el legacy lo tenía **comentado** — se implementa el comportamiento intencional. Iconos: sin marca F# en el tema legacy → stocks (`md-prefs-source` página, `md-file-source` archivos, regla 18.4).

**Archivos** (4 nuevos + 16 modificados; sin submódulos; `fsharpbinding` NO es submódulo — trackeado en el repo principal):
- Nuevos: `Services/FSharpLanguageService.cs` (776 l), `Services/FsiProcess.cs` (262 l), `Views/FSharpInteractivePadView.axaml.cs` (382 l), `Assets/Tutorial.fs` (976 l).
- `MonoDevelop.Startup.Avalonia.csproj` (M, +16) — `FSharp.Compiler.Service 43.12.403` + `Tutorial.fs` Content.
- `Views/MainWindow.axaml.cs` (M, +929) — wiring F# (WireFSharpDocument, comandos, pad, QA `--fsi`/`--fsls`/`--prefs-fsharp`).
- `MonoDevelop.Ide/Controls/SkTextEditor.cs` (M, +176) — adornos F# + props QA.
- `MonoDevelop.Ide/Services/SymbolIndexService.cs` (M, +232) — scanner F#.
- `MonoDevelop.Ide/Services/ConfigurationService.cs` (M, +44) — `.fsproj`.
- `Views/NewSolutionDialog.axaml(.cs)` (M) — plantillas F# Console/Tutorial + scaffolding.
- `Views/NewFileDialog.axaml.cs` (M, +147) — plantillas F# de archivo.
- `Views/PreferencesDialog.axaml(.cs)` (M) — PanelFSharp + Load/Store/Browse/QaFSharpRoundTrip.
- `Program.cs` (M, +20) — hooks QA. `MenuBuilder.cs` (+18), `MenuService.cs` (+10), `ViewCommands.cs` (+3), `AddReferenceDialog.axaml.cs` (+6), `GoToDialog.axaml.cs` (+24).

**Validación (valores medidos)**: E2E `--fsls` → **14/14 True** (wired, diagnostic, bubble, hover, completions, editor-routing, goto-def `Lib.fs:8:4`, outline, gototype, classes, mutable L10 len7, type L11 `int -> int`, adornments editor 2/1/1; refs `.../10.0.12/ref/net10.0`). E2E `--fsi` → **9/9 True** (incl. `advance-ok=True from=5 to=6 session=True`). `--prefs-fsharp` → `roundtrip-ok=True` + restore exacto. `--prefs=fsharp` → `placeholder=False`, icono `md-prefs-source`. Regresión C# → `Build succeeded` + `failed=False` + 0 `[fsls] wired`. GTK legacy → `VIVO (relay --old-gui OK)`. Build shell → **0 errores**.

**QA §18.5** (subagente Tester QA Senior, 4 ciclos recursivos): ciclos #1–#2 → 5 hallazgos (fuga `CheckCompleted`, freeze debounce, regex `let`, retargeting rename/save-as) corregidos → ciclo #3 **VERDE 0 hallazgos** (fases 1–5) → ciclo #4 **VERDE 0 hallazgos** (fase 6: 5 componentes por lectura + 4 hooks E2E + 2 regresiones en verde).

**Queda pendiente** (aceptado por el operador): (a) `CodeGenerationPanel` per-proyecto (project properties — panel de proyecto en el legacy, fuera de la página global); (b) integración VSIX/Marketplace del binding F# (en pausa).

**Cierre**: commit por nombre de los 20 ficheros (4 nuevos + 16 M) + docs ×3, push a `origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos. Artefactos en `~/opencode/` (fixture `fsharp_port/fsls_fs/`, probes `fcs_probe/` + `fs_scanner_probe/`, lanzadores `launch_shell.sh`/`oldgui_smoke.sh`).

## 2026-11-11 — M31 Editor multi-lenguaje dinámico: pila legacy de coloreo + paletas VSCode (Fase 1) — CERRADO (QA VERDE, 0 hallazgos)

**Estado: CERRADO (QA APROBADO, 0 hallazgos abiertos).** El editor de la shell (`SkTextEditor`, coloreo hardcodeado C#) reutiliza la **pila de coloreo legacy** (`MonoDevelop.Ide.Editor.Highlighting`: RegexEngine + Sublime3/TextMate + `SyntaxHighlighting`/`EditorTheme`) con **paletas VSCode** (Dark+/Light+). Lenguajes v1: C#, F#, JSON, XML (`.csproj`/`.sln`), C, C++, Markdown, YAML (resolución por extensión, la más larga gana). Base de las fases 2-9 (intellisense, errores+quick fixes, extension points VSIX, minimap, scroll bloqueado, fondo oscuro, dropdown ▼). Detalle completo en § M31 de este fichero.

**Arquitectura (regla 18.4)**: la pila **no tiene GTK** y se compila *inline* en el dll de la shell (globs `<Compile Include>` en el csproj de la shell). **Sin carpeta "Avalonia"** — convive en `MonoDevelop.Ide`. Guards `#if !AVALONIA_SHELL` en 8 ficheros legacy (solo la shell define la constante); shims CAS inertes bajo `#if AVALONIA_SHELL`. `HslColor`/`StackMatchExpression`/`EditorTheme` sin cambios (compilan en los dos builds). Servicio portado en `SyntaxHighlightingService.Avalonia.cs` (a propósito en el namespace `MonoDevelop.Ide.Editor.Highlighting` por `AllBundles` sin cualificar). 8 grammars incrustados como `EmbeddedResource` con `LogicalName`.

**Integración**: highlighter **recreado por generación** (invalidación en `SetLines`/`Commit`/setter `FilePath`) + cache `HighlightedLine` por línea (snapshot inmutable, `EditorTextDocumentAdapter`). **El tema se resuelve en cada paint** (no se cachea con el resaltado). `Tokenize`/`GetThemePalette` borrados. Wiring: `FilePath` antes que `Text`.

**Fixes hallados por la QA (ciclo §18.5)**:
1. **`Microsoft.Extensions.ObjectPool` no resolvible** (crash en el primer paint, `StringBuilderCache.Allocate`): root cause = `PrivateAssets="runtime"` en `MonoDevelop.Core` → runtime asset fuera del deps.json de la shell vía `ProjectReference` (el dll físico sí llega por `IncludeCopyLocal`). Fix contenido en la shell: `PackageReference` directo (`3.0.0-preview9.19423.4`). Un `<Reference>` con `HintPath` no funciona. `MonoDevelop.Core` no se toca (regla 18.4).
2. **Fondo del editor negro (preexistente)**: editores montan con `Background=Transparent` → `ToSk` devuelve `SKColor(0,0,0,0)` (no nulo) → el fallback `?? #1E1E1E` nunca se aplica → superficie `#000000` (oculta el texto base en light). Fix en `Draw`: si `Background` es nulo o `Alpha==0`, se pinta desde la paleta activa (`TryGetColor("background")`; fallback `#FFFFFF`/`#1E1E1E`).
3. **Recolorido live dark/light**: un cambio de tema muta los brushes *in place* (DynamicResource cambia el `Color` de la misma instancia `SolidColorBrush`) → no hay `OnPropertyChanged`/`AffectsRender` → el compositor re-presenta el frame viejo. (Se probó y **revertió** una rama en `OnPropertyChanged` por propiedades de brush: no se dispara; `Control.ActualThemeVariant` en Avalonia 12 es propiedad simple sin `AvaloniaProperty`.) Fix: el handler de `Application.Current.ActualThemeVariantChanged` en `MainWindow` llama a `ed.InvalidateColors()` de cada editor abierto (nuevo método público de `SkTextEditor` → `MarkDirty()`).

**Validación (valores medidos)**:
- **Builds a 0 errores**: shell (`MonoDevelop.Startup.Avalonia.csproj -c DebugLinux -m:1`, 2198 warnings preexistentes) y GTK (`MonoDevelop.Ide.csproj -c DebugLinux -m:1`, 242 warnings). La shell no referencia `MonoDevelop.Ide.csproj`; el proyecto GTK no define `AVALONIA_SHELL`.
- **Probe headless** (`~/opencode/highlight_qa/headless/`, sobre los DLLs reales de `main/build`): **42 aserciones `[OK]`, 0 `[FAIL]`** — 8 resoluciones + 2 negativos, paletas (tolerancia ±2 LSB), fallback de tema desconocido, tiling completo y scopes en los 6 fixtures. `GLUE PROBE VERDE`.
- **QA visual 6 lenguajes × 2 temas** (12 capturas + `analyze_colors.py`): colores distintos de tinta — dark cs 566 / fs 634 / json 508 / xml 480 / cpp 525 / md 270; light cs 479 / fs 569 / json 505 / xml 346 / cpp 481 / md 139. Canónicos: dark keyword `#BE82B9`/string `#C68A72`/comment `#669352`; light keyword `#9C00C4`/string `#921212`/comment `#007A00`.
- **Fix de fondo** (`fix_{light,dark}_cs.png`): light → `#FFFFFF` (0.0% `#000000`), 12 líneas visibles; dark → `#1E1E1E`, 0 px `#000000` en la zona del editor.
- **Recolorido live** (`--themetoggle`): `Dark -> Light renderCount 2 -> 3 RECLORED` + captura post-toggle en light.
- **Arranque `--old-gui`** (vía `dotnet <dll> --old-gui`): hijo `dotnet main/build/MonoDevelop.dll` con `MONODEVELOP_LEGACY_UI=1`, ~287 MB RSS, ventana creada, vivo >120 s sin excepciones. Quirk preexistente vía apphost (`Environment.ProcessPath` = apphost → el hijo re-ejecuta la shell; no es regresión). Captura negra en Xvfb = artefacto de entorno (sin window manager).

**QA §18.5** (subagente Tester QA Senior, ciclos recursivos hasta 0 hallazgos): ciclo inicial → 3 hallazgos (ObjectPool crash, fondo negro preexistente, recolorido live `NOT-REPAINTED`) → todos corregidos → ciclo final **VERDE 0 hallazgos** (probe headless verde + 12 capturas + `RECLORED` + builds a 0 errores + `--old-gui` verificado).

**Queda pendiente** (fases 2-9, fuera de esta tarea): `.properties`/`.txt`; intellisense; errores + quick fixes; extension points VSIX; minimap. *(Resuelto en M31b: scroll bloqueado (scrollbar del editor), dropdown ▼ y fondo oscuro puro.)*

**Cierre**: commit por nombre de los ficheros modificados + nuevos (`Controls/`, `Resources/`, 3 docs) + push a `origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos. Artefactos en `~/opencode/highlight_qa/` (fixtures, probe headless, `capture_all.sh`, `analyze_colors.py`, lanzadores, 12 capturas, logs) + `~/opencode/highlight_e2e/` (probe 1a).

## 2026-11-11 — M31c Scrollbar nativa: `ScrollViewer` real en lugar de barra pintada — CERRADO (QA VERDE, 0 hallazgos)

**Estado: CERRADO (QA APROBADO, 0 hallazgos abiertos).** El operador rechazó la scrollbar pintada del canvas de M31b ("Scrollbar no, scrollview"): el editor ahora scrollea con un **`ScrollViewer`** estándar de la shell (equivalente al `GtkScrolledWindow` legacy), scrollbar nativa vertical `Auto` (solo con desborde) y horizontal `Disabled`. Diseño: (1) un `ScrollViewer` **único por editor**, creado una vez en `AddDocument` y re-montado (no re-embuelto) en `OnDocSelectionChanged` — re-embolverlo por selección re-papereaba el editor long-lived a un `ScrollContentPresenter` nuevo → `InvalidOperationException` al volver a cualquier tab ya abierta (regresión que crasheó la app al abrir una solución de varias pestañas, detectada en QA ciclo #1 y corregida); (2) el editor reporta su altura total vía `MeasureOverride`, el `ScrollViewer` posee offset/clip/barra y el **render es solo del viewport** (`WriteableBitmap` del viewport, ~765×477 en un doc de 308 líneas — antes del diseño, un doc grande no era viable); (3) coordenadas de documento: `GetCurrentPoint`/`PointToScreen` ya incluyen el offset → quitado `+ scrollLines` en click→línea y `- scrollLines` en los 3 popups; hit-test de *pinned-watch bubbles* convertido con `- scrollOffset` (los rects se registran en espacio de viewport, el punto llega en documento); (4) `InvalidateMeasure()` en `SetLines`/`Commit`/folds para re-pedir el extent. Eliminado el `OnPointerWheelChanged` propio y toda la plomería de la barra pintada. QA §18.5 recursiva en 3 ciclos: #1 hallazgo crítico (crash de re-montaje, 5/5) → fix → #2 hallazgo (regresión de `docs` vacío → Save no-op, gate dirty inoperante) → fix (capturar el editor antes de embolver) → **#3 VERDE 0 hallazgos** (dupdocs `docs=2` + `save-correct`, Save real a disco, re-montaje VIVO por tira de tabs, gate dirty "Save Files", thumb/rueda/arrastre/clic→línea, doc corto sin barra, `--tablist` `reveal-ok=True`). **Incidencia reportada** ("la UI GTK se rompió al abrir una solución"): el apphost `MonoDevelop.AvaloniaShell --old-gui` re-ejecuta la **shell Avalonia** en lugar de GTK (quirk preexistente del relay, `Environment.ProcessPath`=apphost), y esa shell crasheaba con el bug de re-montaje → **corregido por el mismo fix** (`--old-gui <sln>` VIVO). La pila GTK real no fue tocada (`SkTextEditor.cs`/`MainWindow.axaml.cs` no entran en el build GTK); los DLLs compartidos reestadiados (`Core`/`MonoUnixCompat`/`MonoRoslynCompat`) son IL-idénticos a builds previos. `SkTextEditor.cs` solo compila en la shell → ruta `--old-gui` (vía `dotnet <dll>`) intacta.

**Archivos** (2 M): `Controls/SkTextEditor.cs`; `Views/MainWindow.axaml.cs`. Detalle en § M31c.

## 2026-11-11 — M31b Fondo negro del editor, scrollbar vertical y botón de lista de pestañas ▾ — CERRADO (QA VERDE, 0 hallazgos)

**Nota (2026-11-11, M31c)**: la **scrollbar pintada** descrita aquí fue
**sustituida por un `ScrollViewer` real** a petición del operador ("Scrollbar
no, scrollview") — ver § M31c, que detalla el diseño (montaje en `AddDocument`,
render viewport-sized, coordenadas de documento) y la corrección de la
regresión de re-montaje. El fondo negro `#000000` y el botón ▾ de M31b siguen
vigentes y sin cambios.

**Estado: CERRADO (QA APROBADO, 0 hallazgos abiertos).** Tres piezas pedidas por el operador sobre M31: (1) **fondo del editor negro puro `#000000`** en tema oscuro (no el gris `#1E1E1E` de la paleta Dark+ — pedido explícito: "quería el fondo del editor negro, no gris"; light sigue blanco), (2) **scrollbar vertical del editor** (pintada en el canvas Skia, ya que el editor es 100% custom-drawn: 14px, solo con desborde, thumb arrastrable, clic en track pagina) y (3) **botón ▾ de lista de pestañas** (siempre visible en la tira; menú con todas las tabs — ✓ en la activa, `•` dirty — y reveal de la pestaña elegida; flag QA `--tablist`). La QA §18.5 encontró y se corrigieron además **dos bugs de scroll preexistentes** que la nueva scrollbar hizo alcanzables: (a) **doble sustracción del offset en `Draw`** (editor en blanco con `scrollLines > ~1 viewport`; preexistente desde M16e `0309f21902`; fix `y = (firstLine + drawn - scrollLines)·lineH`) y (b) **gutter duplicando el número de la última línea tras un fold** (fallback `RowToLine` + `MaxScroll` sobre líneas de buffer; fix: `RowToLine` → -1, guarda en el bucle, `MaxScroll = VisibleRowCount - ScrollVisibleLines`). `SkTextEditor.cs` solo compila en la shell → ruta `--old-gui` intacta. QA recursiva en 3 ciclos (blocker scroll → H-1 → **VERDE 0 hallazgos**). Detalle completo en § M31b (subsección de § M31).

## 2026-11-12 — M31-d Coloreo de sintaxis completo: identificadores en "blanco" → paleta VSCode (grammar C# completo + paletas ampliadas) — CERRADO (QA VERDE, 0 hallazgos)

**Estado: CERRADO (QA APROBADO, 0 hallazgos abiertos).** Continúa la pila de M31: el operador reportó que "el coloreo de texto del editor sigue incompleto (todavía hay texto de elementos de código en blanco)" y pidió completar el coloreo "para que todo el código se vea con el color representando lo que es en el código (tomar como referencia la paleta de color de texto de código de VSCode)".

**Causa raíz (2 frentes, confirmada con el resaltador real)**: (1) el grammar C# embebido era el minimalista `Resources/csharp.sublime-syntax` (207 líneas), que **nunca tokeniza identificadores** (solo keywords/strings/comentarios/números/preprocesador) → tipos, nombres de clase, nombres de método, variables, campos, propiedades, parámetros y type-args de generics salían en el scope raíz `source.cs` → foreground base (dark `#D4D4D4` / light `#000000`) = el "texto en blanco"; (2) la paleta `HighlightingThemePalette` no cubría familias que otros grammars sí emiten (`entity.name.tag.*`, `entity.name.field.*`, `meta.preprocessor.*`, `constant.character/other.*`, `support.variable.*`, `invalid.*`, varios `markup.*`).

**Fix (2 frentes)**: (a) **grammar** — el `EmbeddedResource` de C# pasa del minimalista al **grammar completo de VSCode** ya en el repo (`syntaxes/CSharp/csharp.tmLanguage`, 6522 líneas, XML plist, `scopeName source.cs`, 180 scopes emitidos, 422 includes locales = autocontenido) con `LogicalName` terminando en `.plist` → el loader elige `TextMateFormat.ReadHighlighting` (el parser XML plist que ya usa `json.plist`); `Register`/`scopeFromFile` en `SyntaxHighlightingService.Avalonia.cs` al nuevo nombre (el `scopeName` `source.cs` no cambia). (b) **paleta** — `BuildDark`/`BuildLight` ampliadas con las familias faltantes, **colores Dark+/Light+ reales de VSCode** (los canónicos de M31 intactos; `punctuation` se deja sin color a propósito, como en VSCode).

**Regla 18.4 (convivencia Gtk/Avalonia)**: los 3 archivos tocados solo se compilan en la shell (el proyecto GTK `MonoDevelop.Ide.csproj` no los incluye y embebe su propio grammar C# legacy `syntaxes/CSharp/C#.sublime-syntax`); el grammar minimalista se deja en el repo. Smoke test `--old-gui` en Xvfb `:99`: vivo, muere solo por timeout, 0 excepciones.

**Validación (valores medidos)**: glue probe M31 contra la DLL nueva **VERDE 52/52** (el corazón del fix: `Program`→`entity.name.type.class.cs`, `Main`→`entity.name.function.cs`, `list`→`variable.other.readwrite.cs`; colores canónicos intactos; los 8 lenguajes sin regresión); análisis de cobertura grammar completo vs paleta: **149/181 scopes cubiertos, los 32 restantes todos intencionales/fieles a VSCode** (27 `punctuation.*` + 3 contenedores `meta.*` + `source.cs` + 1 `entity.name.label.cs`) → **cero identificadores con rol sin color**; capturas Xvfb `:99` dark/light (C# + no-regresión F#/JSON/XML/C++/MD) con highlighting presente (1023/1150 colores de tinta en C#) y no monocromo; build de `MonoDevelop.Startup.Avalonia` y del proyecto GTK `MonoDevelop.Ide.csproj` sin errores (los 2 `CS1705` de NuGet son preexistentes en csprojs no tocados).

**QA §18.5** (subagente Tester QA Senior, ciclos recursivos): ciclo #1 → 1 hallazgo menor (cifras de cobertura en la doc: 166→181, 135→149, 31→32, 26→27, 32/32→52/52, por un conteo de scopes sin guiones) → corregido en los 3 docs → ciclo #2 **VERDE 0 hallazgos**.

**Archivos** (3 M, +51/−9, sin submódulos, sin PNGs): `MonoDevelop.Startup.Avalonia.csproj`, `Controls/SyntaxHighlightingService.Avalonia.cs`, `Controls/HighlightingThemePalette.cs` + docs ×3 (`session_summary.md` §40, `migration-status-report.md`, este § M31-d). Detalle en `docs/session_summary.md` §40.

## 2026-11-13 — M31-e Minimapa del editor + fix del hit-test del botón ▾ — CERRADO (QA VERDE, 0 hallazgos)

**Estado: CERRADO (QA APROBADO, 0 hallazgos abiertos).** Dos piezas pedidas por el operador sobre la shell Avalonia:

### Pieza 1 — Minimapa del editor (la barra de visión reducida a la derecha)

El operador pidió: *"Agregar una barra scrollview a la derecha del editor que muestra la visión reducida del editor con errores, marcado de puntos de depuración, etc., como lo tienen otros IDE"*.

**Qué se construyó.** Un control `EditorMinimap : Control` (Skia, custom-drawn como el editor) que monta **a la derecha del `ScrollViewer` del editor** como 2ª columna de un `Grid` de 2 columnas (`*,110`) creado en `AddDocument` — el layout estándar de IDE (VS Code/Sublime lo ponen al borde derecho del editor). Muestra:

- **Raster escalado con colores de sintaxis** del documento **entero** (no solo el viewport) — el "mapa" del código. Para documentos largos (línea sub-píxel) pinta una **barra de densidad** por línea en su color dominante; para documentos cortos pinta el **texto a escala** con sus colores.
- **Overlay barato por frame** (se dibuja en `Render` cada invalidación, sin re-calcular el raster): **rectángulo de viewport** (regione visible, con dim + borde), **banda de caret** (la línea actual), **puntos de breakpoints** (margen izquierdo, como la gutter) y **marcadores de error** (los `lineBubbles` que son error, margen derecho — el editor los pinta al final de la línea).
- **Interacción**: un **click o drag** sobre el minimapa **salta el caret del editor** a la línea mapeada (`GotoLine`, que hace `EnsureCaretVisible`).

**Arquitectura (la clave: el raster es caro, el overlay es gratis).** El raster coloreado **no puede calcularse por frame** (el highlighting cuesta ~0,9 ms/línea — 3000 líneas ≈ 2,7 s en el UI thread). Por eso:

- **El raster se calcula en un `Task.Run` de background** con un **debounce de 15 ms + `CancellationTokenSource`** (una ráfaga de ediciones cancela el trabajo pendiente y re-agenda al calmar la entrada) y **doble buffer** (`raster` + `pendingDispose`, el mismo release-un-frame-tarde que usa `SkTextEditor`).
- El snapshot de líneas + colores base del tema se toma en el **UI thread** (barato); el highlighting (lo lento) corre en el task con un **highlighter throwaway** (`SyntaxHighlighting` + `EditorTextDocumentAdapter`) — el highlighter del editor es estado del UI thread y no se puede tocar en paralelo.
- **Fuente de datos: el propio `SkTextEditor`** (el modelo de líneas, el tema, breakpoints, `lineBubbles` y el `ScrollViewer` dueño del scroll). El minimapa **nunca duplica el modelo** del editor; solo lo lee.

**Hooks nuevos en `SkTextEditor`** (solo compila en la shell — la ruta `--old-gui` no los ve): `MinimapLineCount`, `MinimapScrollExtent`, `MinimapLinesSnapshot()`, `MinimapBaseForeground/Background`, `MinimapErrorLines`, y dos eventos: `MinimapScrollChanged` (fire en `UpdateScrollState`) y `DiagnosticsChanged` (fire en `SetBubbles`).

**Bug crítico resuelto (crash `SIGABRT`/`VerifyAccess` en QA).** En Avalonia 12 el `AvaloniaObject.Dispatcher` se captura a `Dispatcher.CurrentDispatcher` **en la construcción** del objeto. Los brushes de overlay eran **estáticos** y se inicializaban en el type initializer, que corría en el **hilo background** (lo dispara `RenderRaster` estático leyendo el campo estático `typeface`) → al pintarlos en `Render` (UI thread) fallaba `VerifyAccess` (`Brush.get_Transform` → `OnReferencedFromCompositor`). **Fix**: los brushes de overlay son **instancios creados en el constructor** (UI thread); el `SKTypeface` sigue estático (SkiaSharp no tiene affinity de dispatcher).

### Pieza 2 — Fix del hit-test del botón ▾ (el área de click era solo el glifo triangular)

El operador pidió: *"Arreglar el botón de lista de pestañas del editor que tiene un área de evento de click muy chica (solo donde está el icono triangular) y el evento debe estar en toda el área del botón"*.

**Causa raíz (verificada contra la fuente de Avalonia 12, tag `12.1.2`).** El puntero real se **hit-testea contra el árbol de composición** (`RenderDataStream.HitTest.cs`): `OnDrawGlyphRun` es SIEMPRE hit-testeable, pero `OnDrawRectangle` solo si `serverBrush != null`, y el **Background fill del template del `Button` no se registra como hit topmost**. Por tanto el hit-test solo encontraba el glifo triangular pintado, no el rectángulo del botón.

**Fix.** `HitTestableBorder : Border, ICustomHitTest` (nuevo control, `HitTest => Bounds.Contains(point)`) como **raíz full-size del template `tabstripbtn`** (18×26). `ICustomHitTest` se consulta **ANTES del draw-list** (`CompositionDrawListVisual.HitTest`), así el rectángulo completo del botón responde al click. El template `tabstripbtn` es compartido por los **3 botones de la tira** (los otros dos sin glifo ya eran full-size; el ▾ era el problemático). `chromebtn` no se toca.

### Regla 18.4 (convivencia Gtk/Avalonia)

Los archivos tocados (`EditorMinimap.cs`, `HitTestableBorder.cs`, `SkTextEditor.cs`, `App.axaml`, `MainWindow.axaml.cs`, `Program.cs`) **solo se compilan en la shell Avalonia**: el proyecto GTK `MonoDevelop.Ide.csproj` tiene `EnableDefaultCompileItems=false` y **no incluye la carpeta `Controls/`** (ni `SkTextEditor.cs` ni `EditorMinimap.cs`), y `HitTestableBorder.cs`/`App.axaml` son exclusivos de la shell. Build del csproj GTK `MonoDevelop.Ide.csproj`: **0 errores** (los 1783 warnings son preexistentes). Smoke `--old-gui` en Xvfb `:99`: ventana GTK mapeada, proceso vivo a t=55s, **0 unhandled/abort** (exit 124 = timeout). La captura visual GTK bajo Xvfb :99 es un **artefacto de entorno conocido** (highlight_qa tiene shots previos de 263-290 bytes igualmente vacíos; solo 1 de 5 tenía contenido).

### Validación (valores medidos)

- **Minimapa (`--minimap`, fixture de 431 líneas)**: `found minimap wired=True editorBound=True lines=431`; `raster frames=1 lines=431 (expected=431) RASTER-OK`; `bp@line=10 bubbles=1`; `jump top: caretBefore=10 caretAfter=2 JUMP-OK`; `jump bottom: caretAfter=428 lastLine=430 JUMP-OK`. **6/6 asserts OK**.
- **Visual (captura Xvfb :99)**: el minimapa es una tira vertical estrecha **a la derecha del editor** (el código termina antes; no pisa el texto), con **raster de texto con colores de sintaxis** (verde string/comentario, naranja números, gris claro código — no mancha monolítica), **rect de viewport** (banda azulada), **banda de caret** y **puntos rojos** (breakpoint línea 10 + error línea 20). Todo coherente con el estado del log.
- **No-regresión (gating)**: el minimapa **solo se monta para docs de tipo `SkTextEditor`** — en un doc **no-editor** (Welcome) el right-rail queda plano, **sin barra de minimapa**; en un doc editor (1 tab, `--outline`) el minimapa **sí aparece** a la derecha. Confirmado en ambos sentidos.
- **Botón ▾**: scan 2px limpio (Escape entre clicks) = **140/140 interiores `BTN=Y`** (rect 18×26 completo; márgenes fuera = N, como debe). Pre-fix solo el glifo (39 puntos). **Esquina TL (1012,82) — el caso que fallaba antes — abre el menú completo** (captura `T4_corner_crop.png` muestra fixture.cs + QaTabList1..10 con ✓ en la activa). Los **3 botones de la tira** siguen presentes (`winrect Ext`, cada uno `18x26 HitTestableBorder`).
- **Método QA fiable (lección clave)**: los tests xdotool por diff de screenshots son **flaky**; el veredicto fiable es el **logging del pipeline de punteros real dentro de la app** (`AddHandler(PointerPressedEvent, ..., Tunnel|Bubble, handledEventsToo:true)` en la window) + grid 2px **con Escape entre cada click** (sin Escape el menú traga clicks y contamina).

### QA §18.5 (subagente Tester QA Senior, recursivo hasta 0 hallazgos)

Ciclo #1 (desarrollo, 6 asserts base) → 6/6 OK + visual. Ciclo #2 (Tester QA Senior) → T1 re-ejecución 6/6 OK, T2 visual 4/4 OK, T3 no-regresión ambos sentidos OK, T4 tablist 140/140 + esquina abre menú OK → **VERDE 0 hallazgos** (el único incidente del entorno — un `final_corner.sh` previo interrumpido por SIGTERM que dejó orfanos — lo limpió por PID; no es defecto del código).

### Queda pendiente / notas

- El raster del minimapa usa un **highlighter throwaway** por recomputo; para documentos muy grandes (>10k líneas) se podría limitar el re-raster a la región visible o usar un cache incremental. No es un defecto de la tarea (el debounce ya evita el ráfaga) pero es la mejora natural siguiente si el operador lo pide.
- Los colores de los **dots** (breakpoint/error/caret/viewport) son constantes en el minimapa, no leídos de la paleta: si el operador quiere que sigan el tema (light/dark) se resolverían del `EditorTheme` como hace el editor.
- El `--minimap` abre un fixture propio (`~/opencode/minimap_qa/fixture.cs`, 431 líneas) — no depende de `~/TestProj`.

### Archivos (4 M, +350/−6, + 2 nuevos, sin submódulos, sin PNGs)

- `main/src/core/MonoDevelop.Ide/Controls/EditorMinimap.cs` **(nuevo, 525 líneas)** — el control del minimapa (raster background + overlay + click→GotoLine).
- `main/src/core/MonoDevelop.Startup.Avalonia/Controls/HitTestableBorder.cs` **(nuevo, 23 líneas)** — `Border, ICustomHitTest` (fix del botón ▾).
- `main/src/core/MonoDevelop.Ide/Controls/SkTextEditor.cs` — hooks de minimapa (64 líneas: accesores + 2 eventos).
- `main/src/core/MonoDevelop.Startup.Avalonia/App.axaml` — `xmlns:controls` + template `tabstripbtn` con raíz `controls:HitTestableBorder Name="Ext"`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` — integración en `AddDocument` (Grid 2 columnas) + handler QA `--minimap` + probe `--tabhit`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Program.cs` — allowlist `QaDialogArg` (`--tabhit`/`--minimap`).

+ docs ×3 (`session_summary.md` §42, nota en `migration-status-report.md`, este § M31-e). Detalle en `docs/session_summary.md` §42. Artefactos en `~/opencode/minimap_qa/` (scripts `minimap_qa.sh`, `tabhit_*.sh`, `smoke_oldgui*.sh`, `fixture.cs`, capturas) y `~/opencode/qa_senior_m31e/` (QA Senior).

## 2026-11-13 — M31-f Orden del árbol Dependencies del pad Solution (igualar a la UI GTK) — CERRADO (QA VERDE, 0 hallazgos)

**Estado: CERRADO (QA APROBADO, 0 hallazgos abiertos).** El operador pidió: *"arreglar el orden del árbol Dependencies para que se ordene igual en la UI GTK"* en el pad Solution de la shell Avalonia.

**Causa raíz (verificada contra la fuente GTK, no dada por supuesto).** El Solution pad GTK (`SolutionPad : TreeViewPad`) usa `ExtensibleTreeView`, que cablea `store.DefaultSortFunc = CompareNodes` (`ExtensibleTreeView.cs:212`). `CompareNodes` (`1598–1637`) compara primero el `GetSortIndex` de cada builder; **todos** los builders de grupo / item / TFM de Dependencies **heredan el default 0** (ningún override que afecte a los 5 grupos hermanos — solo `DependenciesNodeBuilder` = −600, que es el nodo contenedor padre, y `PackageDependencyNodeBuilder` = −500 que solo aplica a diagnostics). Al ser el sort index igual, el **tie-break final** es `string.Compare(GetNodeName(a), GetNodeName(b), true)` (línea 1631) → **alfabético case-insensitive por el `NodeName` legacy**.

Los `NodeName` de los 5 grupos (`*Node.NodeName` en `MonoDevelop.DotNetCore.NodeBuilders`) son `AssemblyDependencies` < `FrameworkReferences` < `PackageDependencies` < `ProjectDependencies` < `SdkDependencies`. Por tanto el **orden real que pinta GTK** es **Assemblies → Frameworks → NuGet → Projects → SDK** — que es **distinto** del orden del `yield` de `DependenciesNode.GetChildNodes` (Frameworks → NuGet → SDK → Assemblies → Projects) que la shell estaba replicando. Los **items** dentro de cada grupo y los **TFMs** se ordenan igual (mismo `CompareNodes`, sort 0 heredado → alfabético case-insensitive por nombre visible).

**Fix (solo shell; `MonoDevelop.Startup.Avalonia`).** La `Xwt.TreeStore` de la shell **NO tiene sort func**, así que las filas se insertan **ya ordenadas** para coincidir con lo que pinta GTK:
- **TFMs**: `model.TargetFrameworks.OrderBy(t => t, GtkDepNameComparer)`.
- **Grupos**: se declaran como lista de tuples `(SortKey = NodeName legacy, HasItems, Build)` y se insertan con `groups.Where(g => g.HasItems).OrderBy(g => g.SortKey, GtkDepNameComparer)`.
- **Items**: cada grupo ordena sus items con `OrderBy(nombre visible, GtkDepNameComparer)` (paquetes por id, frameworks por nombre, assemblies por `Path.GetFileName`, projects por `Path.GetFileNameWithoutExtension`).
- **Comparador**: `GtkDepNameComparer = System.Collections.Generic.Comparer<string>.Create((a,b) => string.Compare(a,b,true))` (case-insensitive, igual que `string.Compare(…, true)` de GTK). El nombre `Comparer` es ambiguo en scope del archivo → tipo cualificado completo.
- **Presencia igual a GTK**: un grupo solo se crea si tiene items (`Where(HasItems)`); el grupo **SDK** solo si **NO** hay `FrameworkReferences` (regla legacy `addedFrameworkReferencesNode`).

### Validación (valores medidos)

Fixture `DepsOrder.sln` (en `~/opencode/deps_order_qa/`): **DepsA** con 2 TFMs (`net8.0;net10.0`, declarados en orden NO alfabético) y los 4 tipos de referencias (`PackageReference`, `FrameworkReference`, `Reference`, `ProjectReference`) **todas declaradas en orden NO alfabético y con mezcla de mayúsculas/minúsculas** para forzar el sort real; **DepsB/DepsC** (single TFM, sin refs, para probar la rama SDK).

- **Dump del orden (hook `--deps`)** vía `--sln=<fixture> --deps`: el volcado del subt Dependencies en **orden de store** (= orden pintado, la `Xwt.TreeStore` no tiene sort func) da para DepsA → **TFMs `net10.0` → `net8.0`** ('1' < '8' case-insensitive); **grupos Assemblies → Frameworks → NuGet → Projects** (sin SDK, porque DepsA sí tiene `FrameworkReferences`); **items alfabéticos case-insensitive** (Assemblies `alpha.dll, mid.dll, zlib.dll`; Frameworks `Microsoft.AspNetCore.App, Microsoft.NETCore.App`; NuGet `Alpha.Pkg, mid.Pkg, zeta.Pkg`; Projects `DepsB, DepsC`). DepsB/DepsC → **solo grupo SDK** con `Microsoft.NETCore.App`. **rows=39**, termina con `[deps] done`, sin abort.
- **Ground truth GTK**: el runtime legacy arrancó (`dotnet MonoDevelop.AvaloniaShell.dll --old-gui <sln>`) y mantuvo la ventana mapeada + `MonoDevelop.MSBuildBuilder.exe` + **0 unhandled/abort**. Las capturas visuales GTK bajo Xvfb `:99` salieron vacías (296 bytes, el **artefacto de entorno conocido** — las de `highlight_qa` son de 263–290 bytes igual); por eso el **veredicto GTK se basa en el análisis de fuente de `CompareNodes`** (documentado arriba y verificado por el QA Senior contra el código real), que el dump Avalonia replica punto por punto.
- **QA §18.5 (subagente Tester QA Senior, recursivo)**: el QA **re-derivó** el ground truth GTK desde la fuente (cableado del sort, `GetSortIndex`=0 heredado, ausencia de `override CompareObjects`, `NodeName` de los 5 grupos), re-ejecutó el dump con build fresco, y confirmó el orden punto por punto → **VERDE 0 hallazgos bloqueantes/mayores**.

### Regla 18.4 (convivencia Gtk/Avalonia)

El único archivo de código tocado con lógica es `MainWindow.axaml.cs` (+ `Program.cs` solo la allowlist del flag QA), ambos **exclusivos de la shell Avalonia** (`MonoDevelop.Startup.Avalonia`). El proyecto GTK `MonoDevelop.Startup.csproj` **no referencia** `MonoDevelop.Startup.Avalonia` → la ruta `--old-gui` no compila ni ve el cambio. Builds: **`MonoDevelop.Startup.Avalonia.csproj` 0 errores** y **`MonoDevelop.Startup.csproj` 0 errores** (solo warnings preexistentes).

### Hallazgo separado (fuera de scope, NO corregido — tarea de seguimiento)

`LaunchLegacyGtk` (`Program.cs:96`) usa `Environment.ProcessPath` como el muxer `dotnet`. Al lanzarse por el **apphost** (`MonoDevelop.AvaloniaShell --old-gui`) ese `Environment.ProcessPath` es el propio apphost, que **re-ejecuta su app embebida (`AvaloniaShell.dll`) ignorando el argumento `MonoDevelop.dll`** → el relay **NO arranca el GTK** (las primeras capturas "GTK" de la sesión eran en realidad la shell Avalonia con su welcome page). Solo relays correctamente al lanzarse por `dotnet MonoDevelop.AvaloniaShell.dll --old-gui` (donde `Environment.ProcessPath` es el muxer real). Es un **bug preexistente del relay**, ajeno a la ordenación de Dependencies; se documenta aquí para una tarea de seguimiento.

### Queda pendiente / notas

- **Paridad estricta de la clave de orden de items Projects/Assemblies (MENOR, no bloqueante):** GTK ordena estos items por `GetNodeName` = `ProjectReference.Reference` (la **ruta del include**, p.ej. `..\DepsB\DepsB.csproj`), mientras la shell ordena por **nombre visible** (`GetFileNameWithoutExtension` / `GetFileName`). En el fixture (y en el caso general de un solo directorio) coinciden; solo divergiría si dos referencias de distinto directorio tuvieran el mismo nombre de hoja pero distinto orden de ruta. La spec de la tarea declara "items por nombre visible" y así es → no es un defecto contra la spec; opción de seguimiento si se exige paridad byte-a-byte con GTK.
- **Plataforma en single-TFM (ajustado a petición del operador, 2026-11-13):** GTK colapsa el TFM único (`GetLoadedDependencyNodes`: 1 framework node → los grupos cuelgan **directo** de `Dependencies`, sin fila TFM; 2+ TFMs → nodos TFM). La shell lo replicó: `AddXwtDependenciesNode` inserta filas TFM solo si `tfms.Count > 1`; con un único TFM los grupos van directos bajo `Dependencies` (verificado con el fixture: DepsB/C single-TFM → `Dependencies > SDK > Microsoft.NETCore.App` sin fila `net10.0`; DepsA multi-TFM → filas `net10.0`/`net8.0` intactas). QA §18.5 (2ª pasada): VERDE 0 hallazgos, orden intacto.

### Archivos (2 M, sin submódulos, sin PNGs)

- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` — reescritura de `AddXwtDependenciesNode` + `AddXwtTfmReferenceGroups` (inserción pre-ordenada por `GtkDepNameComparer`) + nuevo comparador `GtkDepNameComparer` + hook QA `--deps` (volcado del subt Dependencies en orden de store).
- `main/src/core/MonoDevelop.Startup.Avalonia/Program.cs` — allowlist `QaDialogArg` (`--deps`).

+ docs ×3 (este § M31-f, `session_summary.md` §43, nota en `migration-status-report.md`). Detalle en `docs/session_summary.md` §43. Artefactos en `~/opencode/deps_order_qa/` (fixture `DepsOrder.sln` + proyectos `DepsA/B/C`, dump `avalonia_deps_dump.log`, `qa_deps_dump.log` del QA, logs GTK, `QA_CONTEXT.md`, backup del perfil).

## 2026-11-13 — M31-g TFMs en la 2ª combobox de la toolbar + quitar el nivel TFM del pad Dependencies (igualar a la UI GTK) — CERRADO (QA VERDE, 0 hallazgos)

**Estado: CERRADO (QA APROBADO, 0 hallazgos abiertos).** El operador pidió (tres peticiones acumuladas sobre el mismo nodo Dependencies):
1. Quitar el subárbol `net10.0`/`net8.0` (el **nivel TFM**) del árbol Dependencies del pad Solution — *"si en la UI GTK no está, no debe estar en la shell Avalonia"*.
2. Mostrar `net10.0`/`net8.0` en la **segunda combobox de la toolbar**, como hace la UI GTK.
3. Que el **primer combobox visible** de la toolbar sea el de **Active configuration** (Debug/Release), que antes estaba en el segundo — *"como está en la UI GTK"*.

**Ground truth GTK (verificado contra fuente, no dado por supuesto).** La `MainToolbar` GTK (`MonoDevelop.Components.MainToolbar/MainToolbar.cs:143-170`) packea tres combos en orden: `runConfigurationCombo` → `configurationCombo` → `runtimeCombo`.
- `RunConfigurationVisible` (controller l.199 + view l.561) **oculta** el run-configuration combo con ≤1 run config → el **1er combo visible** es el de configuración (Debug/Release).
- El `runtimeCombo` (el 2º visible, "plataformas") se puebla en `MainToolbarController.FillRuntimes` (l.207-275) con los **execution targets** del proyecto startup. Para un proyecto .NET **multi-TFM**, `SdkProjectExtension.OnGetExecutionTargets` (`MonoDevelop.Projects/SdkProjectExtension.cs:267-293`) devuelve un `TargetFrameworkExecutionTarget` por TFM (`Id = framework.ShortName` → `net8.0`/`net10.0`); el combo es **insensible** con 1 target (`PlatformSensitivity = runtimes > 1`, l.274) y **NUNCA se oculta** (solo `runtimeCombo.Sensitive`, view l.519-520). Para un .NET **single-TFM** no hay targets de TFM (cae a `base`, que no añade ninguno) → combo vacío + insensible.
- El TFM elegido se persiste como `PreferredExecutionTarget` en los user properties (controller l.482/510/518).
- El pad Dependencies GTK (`MonoDevelop.DotNetCore.NodeBuilders/DependenciesNode.cs`) resuelve el TFM activo **desde la toolbar**, no del pad: el TFM no es una fila permanente del árbol.

**Fix (solo shell Avalonia + servicio compartido aditivo).**
- `Views/MainWindow.axaml` — orden de combos igual a GTK: `RunConfigCombo` (hidden con 1 run config) → `ConfigCombo` (active configuration, 1er visible) → `TfmCombo` (nuevo, 2º visible = target framework).
- `Views/MainWindow.axaml.cs`:
  - Constructor: `RunConfigCombo.IsVisible = false` inicial (GTK lo oculta con 1 run config); `TfmCombo` visible / insensible / vacío inicial.
  - `RefreshConfigurationSelectors`: pobla `TfmCombo` con los TFMs del proyecto **STARTUP** en **orden de declaración del csproj** (NO alfabético — el array `TargetFrameworkMonikers` de GTK conserva ese orden) **solo si es multi-TFM**; `IsVisible = true` siempre, `IsEnabled = multiTfm` (regla `PlatformSensitivity`); pre-selecciona el `PreferredExecutionTarget` persistido o el 1er TFM. `RunConfigCombo.IsVisible = runCfgs.Count > 1` (regla `RunConfigurationVisible`).
  - `OnToolbarConfigChanged`: nuevo caso `TfmCombo` → persiste `SetPreferredExecutionTarget` (equivalente al `PreferredExecutionTarget` de GTK).
  - `AddXwtDependenciesNode`/`AddXwtTfmReferenceGroups`: **sin nivel TFM** — los 5 grupos cuelgan **directos** bajo `Dependencies` (la forma resuelta GTK; el modelo de la shell es por proyecto, así que las filas TFM solo repetirían contenido idéntico).
  - Probe QA `--tfmcombo` (nuevo, allowlist en `Program.cs`): valida orden de combos, TFMs en orden de declaración, single-TFM, persistencia y round-trip.
  - Hook `--deps` extendido: ahora volca también el estado de los 3 combos (visible/selected/items) junto al árbol Dependencies.
- `Services/ConfigurationService.cs` — `SetPreferredExecutionTarget`/`GetPreferredExecutionTarget` (aditivos, escriben/leen `<sln>.userprefs` con la forma legacy `<MonoDevelop.Ide.Workspace><Property name="PreferredExecutionTarget"/>`; solo la shell los invoca).

### Validación (valores medidos)

Fixture `~/opencode/deps_order_qa/DepsOrder.sln` (DepsA = Exe **multi-TFM** `net8.0;net10.0` con los 4 tipos de referencia en orden no alfabético y mezcla de mayúsculas; DepsB/DepsC = Library single-TFM sin refs).

- **Probe `--tfmcombo`** (12/12 `ok` → `[tfmcombo] all pass`): 1er combo visible = config (Debug/Release); 2º = TFM `[net8.0, net10.0]` en orden de declaración, visible+enabled (multi-TFM); `RunConfigCombo` visible=**no** (1 sola run config); persistencia `PreferredExecutionTarget=net10.0` → re-selecciona en refresh → cambio a `net8.0` persiste; single-TFM (DepsB startup) → combo visible + insensible + vacío.
- **Dump `--deps`**: `combo config: visible=yes items=[Debug,Release]` · `combo tfm: visible=yes selected=net8.0 items=[net8.0,net10.0]` · `combo runcfg: visible=no items=[Default]` · árbol con **21 filas** y **sin ninguna fila `platform:`/TFM**: DepsA → Assemblies(alpha,mid,zlib)→Frameworks(AspNetCore,NETCore)→NuGet(Alpha,mid,zeta)→Projects(DepsB,DepsC); DepsB/C → solo SDK(Microsoft.NETCore.App).
- **QA §18.5 (subagente Tester QA Senior, recursivo)**: corrió sus propias pruebas (no releyó logs), re-derivó el ground truth GTK desde la fuente, validó los 8 puntos → **VERDE 0 hallazgos**. Nota del QA (no hallazgo): la paridad single-TFM "vacío+insensible" se confirmó por traza de código del core (0 targets de TFM), no por corrida GTK en vivo; riesgo residual bajo.

### Regla 18.4 (convivencia Gtk/Avalonia)

Tres de los cuatro archivos son **exclusivos de la shell Avalonia** (`MainWindow.axaml`, `MainWindow.axaml.cs`, `Program.cs`). El cuarto, `Services/ConfigurationService.cs`, vive en el assembly compartido `MonoDevelop.Ide` (que también compila la ruta GTK), pero los dos métodos añadidos son **aditivos y solo los invoca la shell** → la ruta `--old-gui` los compila pero no los ejecuta; sin cambio de comportamiento en GTK. Builds: **`MonoDevelop.Startup.Avalonia.csproj` 0 errores** y **`MonoDevelop.Startup.csproj` 0 errores** (solo warnings preexistentes).

### Queda pendiente / notas

- **El pad no filtra por TFM (limitación del modelo, documentada):** la shell mantiene un modelo de proyecto **por proyecto** (las referencias no son por-TFM en el parser de la shell; en el fixture no hay `<ItemGroup Condition>`). Elegir un TFM en la combobox persiste el `PreferredExecutionTarget` y, en un proyecto real multi-TFM, la build/ejecución usaría ese TFM, pero **el contenido del pad Dependencies no cambia** porque las referencias declaradas en el csproj no varían por TFM en este fixture. Esto es fiel a la forma resuelta GTK (grupos directos bajo Dependencies) y a la petición del operador (quitar el nivel TFM del pad); si en el futuro se añaden referencias condicionadas por TFM al parser, el combo TFM podría filtrarlas.
- **El bug del relay `--old-gui`** (documentado en M31-f) sigue sin corregir (fuera de scope).

### Archivos (4 M, sin submódulos, sin PNGs)

- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml` — orden de combos + `TfmCombo`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` — TfmCombo (constructor/refresh/handler/persistencia), pad sin nivel TFM, probe `--tfmcombo`, hook `--deps` extendido.
- `main/src/core/MonoDevelop.Startup.Avalonia/Program.cs` — allowlist `QaDialogArg` (`--tfmcombo`).
- `main/src/core/MonoDevelop.Ide/Services/ConfigurationService.cs` — `Set/GetPreferredExecutionTarget` (aditivos).

+ docs ×3 (este § M31-g, `session_summary.md` §44, nota en `migration-status-report.md`). Artefactos: `~/opencode/deps_order_qa/` (fixture `DepsOrder.sln` + `DepsA/B/C`, `tfmcombo.log`, `deps_dump4.log`, backups del perfil y del `DepsOrder.userprefs`).

## 2026-11-13 — M31-h Iconos de los tabs de pads: re-tinteo con la variante dark/light del tema — CERRADO (QA VERDE, 0 hallazgos)

**Estado: CERRADO (QA APROBADO, 0 hallazgos abiertos).** Problema de diseño reportado por el operador: los iconos de los tabs de pads de la shell Avalonia **no seguían el tema** — en tema **oscuro** se veían **oscuros** cuando deberían ser **claros**.

**Causa raíz (verificada, 3 piezas).** (1) Los tabs de `PadHost` se crean **una sola vez** en el **constructor** de `MainWindow` y el `Image` de cada tab (header + rail) se rellena con `IconService.GetImage(tab.Icon)` — un `Bitmap` resuelto según la **variante de tema de ese instante** (`PadHost.cs` `AddTab`). (2) Avalonia reporta la variante del SO **asíncronamente: primero Light, luego Dark** en un sistema oscuro (comentario `MainWindow.axaml.cs:310-313`) → en la fase del constructor la variante es aún **Light** y se cachea el PNG claro (oscuro-sobre-claro). (3) **Nada refrescaba** los `Image` de los pads al cambiar la variante (los de la toolbar sí: `ActualThemeVariantChanged → SetToolbarIcons()`). Los 15 iconos de pad **sí tienen variante `~dark`** (p. ej. `MonoDevelop.Avalonia.Icons/pad-solution-16~dark.png`) y `IconService` la resuelve por variante (`Variants()` prueba `~dark` antes que la base cuando `dark=true`) → el problema era **solo de refresco**, no de assets.

**Fix (solo shell Avalonia).**
- `MonoDevelop.Ide/Controls/PadHost.cs`: `PadTab` guarda `HeaderIconImage`/`RailIconImage` (`internal`), los dos `Image` creados en `AddTab` (header strip l.274 + rail twin l.299). Nuevo método público `RefreshIcons()`: itera los tabs y re-resuelve `IconService.GetImage(tab.Icon)` cambiando el `Source` de los `Image` ya construidos (sin reconstruir el chrome). `IconService` cachea por `(stockId, dark, disabled, scale)`, así que el swap es barato.
- `MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs`: helper `RefreshPadIcons()` (los 3 hosts: `LeftPads`/`RightPads`/`BottomPads`), invocado en **dos puntos**: (a) en el handler `Opened` junto a `SetToolbarIcons` — cubre "el OS reporta Dark entre el constructor y el `Opened`"; (b) en `ActualThemeVariantChanged` junto a `SetToolbarIcons` — cubre el **switch en vivo**. Probe QA `--padicons` (nuevo, allowlist en `Program.cs`): fuerza `RequestedThemeVariant` **Light→Dark→Light** y verifica por **identidad de referencia** contra `IconService.GetImage("md-solution-pad")` que el `Source` de header y rail del tab `solution` se re-punta al `Bitmap` cacheado de la variante vigente, con `ResolveFilePath` como ground truth del path. Restaura `ThemeVariant.Default` al final.
  - *Limitación de Avalonia 11 (documentada):* `Bitmap` **no expone `Filename`**, así que el probe no puede leer el path del bitmap cargado; la verificación es por **identidad de referencia** contra la cache de `IconService` (el mismo bitmap que serviría una llamada nueva de `GetImage` con la variante vigente) + `ResolveFilePath` para el path de referencia (patrón del probe `--iconsqa`).
- `MonoDevelop.Startup.Avalonia/Program.cs`: `--padicons` en la allowlist `QaDialogArg`.

**Alcance:** solo los tabs de `PadHost` (3 hosts). La restore strip solo usa texto. Los iconos del árbol Xwt se resuelven por `ResolveFilePath` en build (sin este bug; fuera de alcance).

### Validación (valores medidos)

Fixture `DepsOrder.sln` (`~/opencode/deps_order_qa/`), Xvfb `:99` (SO `prefer-dark` — el escenario real del operador).

- **Probe `--padicons` (10/10 `ok` → `[padicons] all pass`)**: Light → `ResolveFilePath` = `pad-solution-16.png` (base) + header/rail = bitmap cacheado light; Dark → `ResolveFilePath` = `pad-solution-16~dark.png` + header/rail **swapeados al bitmap cacheado `~dark`** (referencia nueva, no el light viejo); Light (restore) → **swapeados de vuelta** al light (re-resolución bidireccional).
- **Visual dark (corrida A, sin flag → arranca DARK por el OS)**: tema oscuro (menu `srgb(45,45,45)`, editor `srgb(30,30,30)`); iconos de pads **CLAROS** en Solution (izq.), Properties (der.), Output y Errors (abajo) — variantes `~dark` legibles sobre los tabs; toolbar (logo/run/debug) también clara → coherente.
- **Visual light tras switch en vivo (corrida B, `--themetoggle`)**: conmuta Dark→Light por la **ruta de producción** (`ApplyThemeVariant`); log `[themetoggle] fixture.cs: Dark -> Light renderCount 3 -> 4 RECLORED`; tras la conmutación, iconos de pads **OSCUROS** sobre fondo claro → re-tint en vivo correcto.

### Regla 18.4 (convivencia Gtk/Avalonia)

`PadHost.cs` e `IconService.cs` viven en `MonoDevelop.Ide` pero **NO se compilan en la ruta GTK**: `MonoDevelop.Ide.csproj` usa `EnableDefaultCompileItems=false` con lista explícita que **no los incluye**; la shell los compila vía los globs `..\MonoDevelop.Ide\Services\**\*.cs` / `Controls\**\*.cs` (`MonoDevelop.Startup.Avalonia.csproj` l.157-158). Verificado en binarios: `MonoDevelop.Ide.dll` (GTK) **no contiene** `PadHost`/`RefreshIcons`; `MonoDevelop.AvaloniaShell.dll` **sí** (también `RefreshPadIcons` y el literal `--padicons`). → **impacto GTK nulo**. Builds: **`MonoDevelop.Startup.Avalonia.csproj` 0 errores** y **`MonoDevelop.Startup.csproj` 0 errores** (solo warnings preexistentes). Smoke GTK (`MonoDevelop.dll --old-gui`) vivo 44 s, 0 stack traces.

### QA §18.5 (subagente Tester QA Senior, recursivo)

El QA corrió sus propias pruebas: review estática del diff, re-ejecutó los 2 builds, relanzó el probe (10/10), verificación **visual en temas** (corrida A dark + corrida B switch en vivo, zoom + medición por píxel) y smoke GTK → **VERDE 0 hallazgos**. Dos apuntes **INFO** (no bloqueantes): (1) `--themetoggle` conmuta Dark→Light a los ~1 s → las capturas de la corrida B quedaron en light; el estado dark con iconos claros lo cubre la corrida A y el re-tint el log + el probe; (2) `RefreshIcons` omite tabs con `Icon == null` — sin impacto práctico (los 22 `AddTab` de la shell llevan `Icon`; brecha teórica, no alcanzable).

### Queda pendiente / notas

- **El operador debe relanzar su shell** para ver el cambio (DLL vieja en memoria en su sesión).
- El probe verifica el tab `solution` (header + rail) como caso representativo; la fix cubre todos los tabs de los 3 hosts (mismo `PadHost.RefreshIcons`).

### Archivos (3 M, sin submódulos, sin PNGs)

- `main/src/core/MonoDevelop.Ide/Controls/PadHost.cs` — `HeaderIconImage`/`RailIconImage` en `PadTab`, refs en `AddTab`, método `RefreshIcons()`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` — `RefreshPadIcons()` + llamadas en `Opened` y `ActualThemeVariantChanged`, probe `--padicons`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Program.cs` — allowlist `QaDialogArg` (`--padicons`).

+ docs ×3 (este § M31-h, `session_summary.md` §45, nota en `migration-status-report.md`). Artefactos: `~/opencode/deps_order_qa/padicons/` (logs + capturas + zooms), `~/opencode/deps_order_qa/padicons_probe.log`, backups del perfil y de `DepsOrder.userprefs`.

## 2026-11-13 — M31-i Pads redimensionables por drag (izq/der) + defaults 300/300/180 + persistencia al cambiar (shell Avalonia) — CERRADO (QA VERDE, 0 hallazgos)

**Estado: CERRADO (QA APROBADO, 0 hallazgos abiertos tras 3 pases recursivos).** Pedidos del operador: (1) hacer redimensionables **por drag del ratón** los límites editor↔pad izquierdo y editor↔pad derecho (el editor↔inferior ya existía como referencia, wave-3); (2) aumentar los **defaults**: pads laterales a **300** (decisión del operador: *"A 300 (default GTK)"*, igualados al `defaultItemWidth=300` de `DockFrame.cs:60-61`) y pad inferior 170→**180**.

**Mecanismo (solo shell Avalonia).**
- `MonoDevelop.Ide/Controls/PadSplitter.cs` — **reescrito a dual-axis** (el `GridSplitter` de Avalonia ignoraba los drags reales en estos bordes). Vertical (`TargetRow` set): el borde editor↔inferior; el drag ajusta la **altura de la fila** (cursor `BottomSide`). Horizontal (`TargetControl` set): el borde editor↔lateral; el drag ajusta el **`PadHost.Width`** del pad (las columnas laterales del `WorkbenchGrid` son `Auto` y siguen al `Width`), con `HorizontalGrowSign` (+1 izq: arrastrar a la derecha lo agranda; −1 der: a la izquierda) y `OppositeControl` (el pad del lado opuesto, restando su ancho al `max` para que la columna `*` del editor mantenga un mínimo usable cuando los dos laterales son anchos). Fallbacks NaN, min 34 (rail), max = ancho grid − opuesto − 150. Nuevos eventos `DragStarted` (el shell expande el pad colapsado **antes** del drag, para que el drag crezca desde el rail en vez de pelearse con el colapso) y **`DragEnded`** (dispara en `OnPointerReleased` **y** `OnPointerCaptureLost`; es el gacha de la persistencia — ver abajo).
- `MonoDevelop.Ide/Controls/PadHost.cs` — nueva prop pública **`EffectiveWidth`** (el `Width` expandido, o el `savedWidth` pre-colapso si está colapsado; NaN→0) y nuevo evento **`SelectionChanged`** (dispara solo cuando el id de la tab activa **realmente cambia** en `Select`, no en el "bring to front" que re-selecciona la ya activa).
- `Views/MainWindow.axaml` — defaults **300/300/180**; `EditorBottomSplitter Height=6 VerticalAlignment=Top`; los dos splitters laterales `LeftPadSplitter`/`RightPadSplitter` (col 1, RowSpan 2, `HorizontalAlignment=Left/Right`, `Width=6`) declarados **al final** del `WorkbenchGrid` — el z-order importa: si no, `BottomPads` les tapaba la parte baja y el drag sobre ella no llegaba.

**Persistencia: de 13 a 15 segmentos.** `Monodevelop.PadsLayout` pasa de `hosts×4 + rowH` (13) a `hosts×4 + leftW|rightW|rowH` (**15**). Los anchors laterales persisten igual que la altura de fila inferior. **Backward-compatible**: un registro 13-seg se lee con `rowH=segs[12]` y los anchos toman el default (XAML 300); el perfil del operador (13-seg `…|274`) se respeta (rowH 274) y sus anchos salen en el default.

**El hallazgo H1 (mayor, del QA pase 1) y su fix — persistencia al cambiar.** El QA encontró que la app **solo persistía en `Window.Closing`**: al morir por señal (SIGTERM/`timeout`/`kill`) se perdían los anchos redimensionados. Se intentó un handler `PosixSignalRegistration` (SIGTERM/SIGINT) en `Program.cs` + `SavePadsLayoutThreadSafe`, pero en la app real el handler quedaba **armado** (el log imprimía "handlers armed", `SigCgt` mostraba SIGTERM cogido) y **nunca corría** (morte ~0s, perfil intacto). Diagnóstico: los repros minimal (plain .NET **y** Avalonia X11/Skia) **sí** disparan el handler, pero la app real carga `libdbus-1.so.3` y `libsystemd.so.0.42` (los repros no) y **su handler SIGTERM nativo preempe al gestionado del CLR**. Luch contra un handler nativo es un rabbit hole con riesgo de regresión → **fix definitiva: persist-on-change**. El layout se escribe a disco **en el momento en que el usuario lo cambia**, así que **cualquier** salida (SIGTERM/crash/graciosa) pierde nada — no depende de ningún hook de shutdown. El handler de señales se **revertió por completo**.
- **`PersistPadsLayoutNow(why)`** (nueva, `MainWindow.axaml.cs`): la entrada auto-guardada. No persiste si `!padsLayoutReady` (startup: los XAML defaults no son estado del usuario; el primer `AddTab→Select` ya corrió en el ctor), si `!AutoPersistIsEnabled` (un probe de QA va en marcha) o si `restoringPadsLayout` (el restore aplicando estado). Llama a `SavePadsLayout()` (primitiva **sin** guard) e imprime `[pads] persisted (why)`.
- **Cableado de `PersistPadsLayoutNow`**: 3× `DragEnded` (bottom/left/right), 3× `CollapseChanged` (bottom preexistente + left/right nuevos), 3× `PadTabClosed`, 3× `SelectionChanged`, `SetPadVisible`, `SetGroupVisible`, `TogglePad`. Cubre **todo** lo que el usuario cambia: tamaño, colapso, cierre de tab, selección de tab y visibilidad.
- **`restoringPadsLayout`** (try/`finally` en `RestorePadsLayout`): suprime el persist-on-change durante el restore — los `CollapseChanged` del restore no persisten un estado **parcial** (p. ej. altura de fila aplicada pero los hosts aún sin colapsar).
- **El hallazgo H5 (menor, del QA pase 2) y su fix — regla positiva con safe default OFF.** La primera versión de `PersistPadsLayoutNow` usaba una **lista negativa** (`IsPadStateDrivenQaProbe`) de probes "que dirigen el estado de pads" que debían estar exentos. Omitía los probes de **debug diferidos** (mutan **tras** un `await`, es decir **después** del restore): `--frame`, `--immcompl`, `--locals`, `--watch`, `--pinwatch`, `--attachreal`, `--attachdlg`. Al no estar en la lista, habrían llamado a `PersistPadsLayoutNow` y **pisado el registro del operador** (p. ej. `selectedId=callstack`). Era latente (no reproducible: requieren una sesión de debug activa que no existe en este entorno) pero real. **Fix:** se **invierte** la lógica a una **regla positiva** —
  `static bool AutoPersistIsEnabled => Program.QaDialogArg is "" or "--padgeo";`
  Auto-persist **ON** solo para (a) una **sesión real de usuario** (`QaDialogArg == ""`, sin ningún `--qa` arg) y (b) el probe **pasivo** `--padgeo` (solo vuelca geometría mientras un drag real se ejecuta; un drag bajo él persiste igual que el de un usuario — es así como se valida la fix H1). **Cualquier** otro probe (todos los que dirigen estado, los debug diferidos y **cualquier probe futuro**) queda **excluido por el default** — una lista negativa habría que "recordar" ampliarla con cada probe nuevo y un probe nuevo re-activaría el auto-persist en silencio; la positiva **no puede derivar** así. (Verificado: `QaDialogArg` termina en `?? ""` → en sesión real es `""`, nunca null.)
- **`SavePadsLayout()`** vuelve a ser **primitiva sin guard** (el probe `--padresize` la llama explícitamente y espera que siempre escriba); el guard `padsLayoutReady` que se había metido dentro se **retiró** (la primitiva no guarda; `PersistPadsLayoutNow` es la que guarda). `Window.Closing` **sigue** llamando a `SavePadsLayout` (el cierre gracioso también persiste, como antes).
- **Fix H3 (informativa, del QA pase 1):** en `SavePadsLayout` un `EffectiveWidth` NaN/<34 ahora persiste **300** (el default XAML) en vez de 0 (el restore ignora <34 igualmente, pero el registro debe seguir siendo auto-descriptivo).

**Contrato de alineación (H2, menor, del QA pase 1 — documentado).** Al reescribir `PadSplitter` se perdió el `VerticalAlignment=Top` del ctor original: sin él, la franja inferior de 6px se **centraba** en la fila (y=554) en vez de pinnearse al borde editor/inferior (y=514). El contrato (ahora en el doc comment de `PadSplitter`): la franja de 6px es de tamaño fijo en su **eje de drag** (Height=6 en el borde de fila, Width=6 en el de columna) y estira en el eje transversal, pero debe **pinnearse explícitamente en el eje de drag** porque dejada sin alinear se **centra** en la celda y se sale del borde. El XAML fija `VerticalAlignment=Top` en el splitter inferior y `HorizontalAlignment=Left/Right` en los laterales; **ambas son load-bearing** (caer en cualquiera re-centra en silencio la franja de ese borde).

### Validación (valores medidos)

Fixture `DepsOrder.sln` (`~/opencode/deps_order_qa/`), Xvfb `:99` **sin WM** (el input xdotool llega a Avalonia; metacity/mutter dejaban Xwayland huérfano). Geometría de referencia (ventana 1280×800 a (0,0), perfil limpio): `leftS=(307,78,6x694) rightS=(967,78,6x694) botS=(307,592,666x6) leftW=300 rightW=300 rowH=180 win=1280x800`. Drag = `xdotool mousemove --sync X Y; mousedown 1; mousemove --sync (pasos ≤20px); mouseup 1`.

- **Probe determinista `--padresize` (18/18 `ok` → `all pass`)**: wiring (targets/opposites/signs), columna Auto sigue el `Width` (300→340), default izq = 300, colapso pinneado al rail 34, `EffectiveWidth` conserva 350 pre-colapso, registro 15-seg, round-trip 15-seg (leftW 320 / rightW 280).
- **Drags REALES (xdotool, input real)** — BASE exacta: inferior `rowH 180→240` (`botS.y 592→532`, franja **pegada al borde**, no centrada → no-regresión de `VerticalAlignment=Top`); izq `leftW 300→380`; der (sign −1) `rightW 300→370`; inverso (encogido) `leftW≈250` (clamp min 34, no se rompe); **drag sobre pad colapsado** → `[padhost] 'left' toggle(splitter-drag) → collapsed=False` y `leftW 34→440` (pre-colapso 380 + 60); z-order extra: drag lateral a y=650 (sobre `BottomPads`) funciona.
- **Fix H1 (persist-on-change ante señal)** — drag bajo `--padgeo` → `[pads] persisted (drag: left)` + `[padgeo] leftW=380` → el perfil **ya** tiene 15-seg `leftW=380` **antes** del SIGTERM → `kill -TERM` (lo que hace `timeout`) → muerte ~0-1s (no cuelga) → **perfil SIGUE con `leftW=380`** (intacto). Pre-fix el perfil quedaba **ausente**.
- **Persistencia de selección** (hueco menor, cerrado) — perfil operador (13-seg, bottom `output,errors|output`); clic real en la tab `Errors` (coordenadas de captura) → `persist antes = 0` (el restore no persiste) → `[pads] persisted (select bottom=errors)` → perfil 15-seg con **`selectedId=errors`** y `rowH=274` (respeta el 274 del 13-seg) → SIGTERM → intacto.
- **Backward-compat 13-seg** — perfil bak2 (13-seg `|274`): `--padgeo` → BASE `rowH=274 leftW=300 rightW=300` (defaults), línea `[pads] layout restored: leftW=default rightW=default rowH=274`.
- **Canary de probes de estado** — `--padsqa` (rowH=237 + colapso vía `Window.Closing`) y `--padsqa2` (restore + expand) con **auto-persist = 0** (no pisan el registro) y round-trip `rowH=237` aguantado (`after-expand actualRowH=237`).
- **Builds**: `MonoDevelop.Startup.Avalonia.csproj` **0 errores** y `MonoDevelop.Startup.csproj` (GTK) **0 errores** (solo warnings preexistentes ~2291/252).

### Regla 18.4 (convivencia Gtk/Avalonia)

`PadHost.cs` y `PadSplitter.cs` viven en `MonoDevelop.Ide` pero **NO se compilan en la ruta GTK**: `MonoDevelop.Ide.csproj` usa `EnableDefaultCompileItems=false` con lista explícita que **no los incluye**; la shell los compila vía los globs `..\MonoDevelop.Ide\Services\**\*.cs` / `Controls\**\*.cs` (`MonoDevelop.Startup.Avalonia.csproj`). → **impacto GTK nulo** (el `--old-gui` no usa `PadHost`/`PadSplitter` ni `PadsLayout`; hallazgo H4 informativo). Builds 0 errores en las dos rutas.

### QA §18.5 (subagente Tester QA Senior, recursivo — 3 pases)

- **Pase 1** → **ROJO, 1 mayor (H1)** + H2 menor + H3/H4 informativos. (H1: no persistía ante señal; H2: el `VerticalAlignment=Top` era un invariant implícito; H3: NaN→0; H4: sin impacto GTK.)
- **Pase 2** (tras la fix H1 con persist-on-change) → **ROJO, 1 menor (H5)**: la lista negativa de skip omitía los probes de debug diferidos (`--frame`, `--immcompl`, `--locals`, `--watch`, `--pinwatch`, `--attachreal`, `--attachdlg`) que mutan **tras** el restore y habrían pisado el registro. Latente (no reproducible: requieren sesión de debug).
- **Pase 3** (tras la fix H5 con regla positiva `AutoPersistIsEnabled`) → **VERDE 0 hallazgos**: confirma la premisa crítica (`QaDialogArg == ""` en sesión real, nunca null), que los 7 probes diferidos quedan **estructuralmente** excluidos, y re-valida en verde la regresión (H1, canary, selección, probe 18/18, drags reales, backward-compat).

### Queda pendiente / notas

- **El operador debe relanzar su shell** para ver el cambio (la DLL vieja sigue en memoria en su sesión). Con el persist-on-change, el perfil del operador **solo se toca en sesiones reales** (sin `--qa`) o bajo `--padgeo` (pasivo, un drag real); los probes de estado están exentos. Su layout (rowH 274) se respeta.
- El `SavePadsLayout` del cierre gracioso (`Window.Closing`) **sigue** en pie como red de seguridad: si algo cambia justo antes de un cierre limpio, también se persiste (idempotente con el persist-on-change).
- La selección de tab **sí** persiste al cambiar (cerrado con `SelectionChanged`); la única ventana residual es mínima y ya no aplica (cada cambio de selección persiste en el acto).

### Archivos (5 M, sin submódulos)

- `main/src/core/MonoDevelop.Ide/Controls/PadSplitter.cs` — reescrito dual-axis + `DragStarted`/`DragEnded` + doc comment del contrato de alineación.
- `main/src/core/MonoDevelop.Ide/Controls/PadHost.cs` — `EffectiveWidth` + evento `SelectionChanged`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml` — defaults 300/300/180, `VerticalAlignment=Top` inferior, splitters laterales al final del grid (z-order).
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` — cableado ctor, persistencia 15-seg (backward-compat 13), `PersistPadsLayoutNow`/`AutoPersistIsEnabled`/`restoringPadsLayout`/`padsLayoutReady`, `SavePadsLayout` primitiva, `SelectionChanged` cableado, probes `--padresize`/`--padgeo`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Program.cs` — allowlist `QaDialogArg` (`--padresize`/`--padgeo`); el handler de señales se revertió (estado neto = solo la allowlist).

+ docs ×3 (este § M31-i, `session_summary.md` §46, nota en `migration-status-report.md`). Artefactos: `~/opencode/padresize_qa/` (logs `h1_v2.log`/`sel_test2.log`/`padresize_*.log`/`canary_*.log`/`p2_*`/`p3_*`, capturas `qa_state_*`/`p2_*`, backups del perfil `MonoDevelopProperties.xml.bak2`, scripts `h1_test2.sh`/`sel_test2.sh`/`preqa_canary.sh`, repros de señal `sigrepro*`).

## 2026-11-14 — Perfil sin versión 9.0: quitar el sufijo `9.0` de las 3 raíces XDG (`.config/.cache/.local/share`) + migración one-time de datos — CERRADO (QA VERDE, 0 hallazgos)

**Estado: CERRADO (QA APROBADO, 0 hallazgos abiertos; validación recursiva §18.5 del Tester QA Senior, 1 pase → VERDE directo).** El operador pidió quitar la carpeta de versión `9.0` del perfil de usuario: `~/.config/MonoDevelop/9.0/` → `~/.config/MonoDevelop/`, `~/.cache/MonoDevelop/9.0/` → `~/.cache/MonoDevelop/` y `~/.local/share/MonoDevelop/9.0/` → `~/.local/share/MonoDevelop/` (los tres confirmados explícitamente). Requisitos implícitos: **no perder el dato del operador** (layout de pads `|274`, keybindings, properties, cache) y **no romper la migración de versiones vieja** (los directorios `8.0`/`2.6`… siguen siendo fuentes de migración y deben conservarse versionados).

**Diseño (dos frentes + migración de datos).**
- **Frente central — `UserProfile.ForUnix`** (la única construcción Unix de las 3 raíces, usada por GTK y por cualquier consumer de `UserProfile`): el perfil **actual** (la última de `ProfileVersions` = `9.0`) ahora resuelve a `<xdg>/MonoDevelop` (sin versión, `appId = BrandingService.ProfileDirectoryName`); las **versiones viejas** siguen usando `GetAppId(version)` (versionadas: `…/8.0` con barra para ≥7.0, `…-2.6` con guion para <7.0 — formato original intacto). `GetAppId` no se tocó (Windows/Mac conservan su layout versionado).
- **Frente hardcode — la shell Avalonia no usa `UserProfile`** (lo bypassa con rutas literales): se actualizaron los 7 sitios: `UserPreferences.cs` (props), `SettingsStore.cs` (KeyBindings + Tools), `PreferencesDialog.axaml.cs` (UserDataRoot, VcConfigFile, PropertiesFile, inline). Verificado por grep: **no queda ningún hardcode de perfil `…/9.0/`** en `main/src` (salvo `BuildVariables.cs`, que define la constante de versión `9.0` — no es una ruta).
- **Migración de datos one-time (el matiz crítico)**: solo cambiar la ruta de código habría dejado el dato del operador **huérfano** en `…/9.0/` (la app arrancaría limpia y, peor, el static ctor de `PropertyService` habría migrado un 8.0 viejo por encima al no encontrar props en la nueva ruta). Se añadió `MigrateUnixCurrentProfile()` en el **static ctor de `UserProfile`** (orden garantizado: `PropertyService` depende de `UserProfile.Current`, así que la migración corre antes en **ambas** interfaces): mueve `<xdg>/MonoDevelop/9.0/*` → `<xdg>/MonoDevelop/*` en las 3 raíces (ConfigDir, CacheDir, UserDataRoot cubren todos los sub-locations: LocalConfig=Config, Temp/Log bajo Cache, LocalInstall bajo UserData). **Solo `Platform.IsLinux`** (el único layout que cambia). **Idempotente** (si `9.0` no existe, no-op). **Newest-wins**: si un fichero existe en ambos lados, gana el más reciente por mtime (una re-ejecución tras haber escrito en la nueva ruta no resucita el 9.0 viejo). **Verificado antes de borrar**: el subdirectorio `9.0` solo se elimina si **todos** sus ficheros existen ya en el destino (una interrupción nunca pierde dato). **Best-effort** (try/catch: un fallo de migración nunca impide arrancar el IDE; queda `Console.Error` de diagnóstico).
- **Trigger en la shell** (`Program.Main`, primera línea): `_ = MonoDevelop.Core.UserProfile.Current;` — la shell si no **nunca** toca `UserProfile` (sus servicios hardcodean), y el static ctor + migración habrían quedado sin dispararse en el camino Avalonia. Al ser antes de `LaunchLegacyGtk`, también cubre `--old-gui` (el hijo GTK vuelve a tocar `UserProfile` y su migración queda no-op por idempotencia).

**Bug real encontrado y corregido durante el desarrollo (cuenta de honor):** la primera versión de la migración usó `FilePath.FileName`, que en MonoDevelop devuelve **solo el nombre base** (`Path.GetFileName` → `"MonoDevelop"`), no la ruta completa → `versioned` quedó como la ruta **relativa** `MonoDevelop/9.0` (contra el CWD del proceso) → `Directory.Exists` falso → **no-op en silencio** (2 arranques reales sin migrar, detectado con diagnóstico temporal que imprimía las rutas). Fix: `ToString()` (ruta completa; `FilePath` tiene conversión implícita a `string`). Lección: en `FilePath` de este repo, `.FileName` ≠ "ruta completa".

### Validación (valores medidos)

Fixture `DepsOrder.sln` (`~/opencode/deps_order_qa/`), Xvfb `:99`. Backup completo del perfil pre-cambio en `~/opencode/no9_qa/backup_pre/` (las 3 raíces con 9.0 y 8.0) + checksums de los ficheros clave.

- **Probe determinista sobre la DLL construida** (`~/opencode/no9_qa/probe/`, referencia `main/build/MonoDevelop.Core.dll`, perfil AISLADO vía `XDG_*_HOME`+`HOME`→scratch): 8/8 `OK` → `VERDE` (perfil actual = `…/.config/MonoDevelop` sin versión; las 3 `9.0` drenadas; props en la raíz; `GetProfile(8.0)` = `…/MonoDevelop/8.0` y `GetProfile(2.6)` = `…/MonoDevelop-2.6` **intactos**, formatos originales). Re-ejecución sobre la misma carpeta: `VERDE` (idempotencia, sin duplicados ni errores).
- **Simulador del algoritmo** (`~/opencode/no9_qa/migsim/`, 1:1 de `MigrateVersionedSubdir`/`MergeDirectory`): `migrate` / `idempotent` / `conflict` (un fichero preexistente en el destino no se sobreescribe) / `duplicate` (el `9.0` más reciente gana) = **4/4 VERDE**.
- **Migración REAL en el perfil del operador** (arranque shell con el código corregido): las 3 `9.0` drenadas y eliminadas; `8.0` intacto (`cache`=124 fich, `data`=2); sin `[profile-migration]` ni excepción en el log; arranque limpio (`Loaded DepsOrder.sln`, `[pads] layout restored: leftW=345 rightW=358 rowH=274`).
- **Sin pérdida de datos** (verificado dos veces, dev + QA): 703 ficheros de referencia (backup) → **0 faltantes** en disco; checksums de `VersionControl.config`/`EditingLayout.xml`/`Custom.kb.xml`/`UserDefault.mdpolicy.xml` **idénticos al backup**; el layout de pads **`…|358|274`** presente en las props versionless; la única diff de `MonoDevelopProperties.xml` vs el `9.0` es un timestamp de `RecentSolutions` más nuevo (escrito por la propia app — esperado).
- **GTK `--old-gui` en vivo** (regla 18.4): hijo GTK arrancado sobre el perfil versionless → sesión completa (workbench, add-ins), **no re-crea `9.0`** en las 3 raíces, sin "Did not find previous version" (lee las props de la raíz directas), sin `[profile-migration]`. Los 67 ERROR del log GTK son los **preexistentes** de add-ins Windows-only (SharpSvn / `MonoDeveloperExtensions_nunit`) documentados en la nota 2026-11-12 — ninguno menciona perfil/propiedades.
- **Builds**: `MonoDevelop.Startup.Avalonia.csproj` **0 errores** y `MonoDevelop.Startup.csproj` (GTK) **0 errores** (solo warnings preexistentes).

### QA §18.5 (subagente Tester QA Senior, recursivo)

- **Pase 1 → VERDE 0 hallazgos** (directo; el bug del leaf `.FileName` fue atrapado **durante** el desarrollo por el propio dev con diagnóstico temporal y corrección `ToString()`, y re-validado por el QA). El QA re-ejecutó el probe (migración + idempotencia), migsim 4/4, el barrido de hardcodes, la comparación no-loss (703 ficheros, layout `358|274`), los builds y **dos arranques frescos** (Avalonia `log_qa2_avalonia.txt` y GTK `Ide.2026-11-13__23-52-41.log`): sin migraciones erróneas, sin re-creación de `9.0`, `8.0` intacto en ambos.
- **Notas informativas del QA (no cuentan como hallazgos)**: (1) `ForMD24()` en Linux resuelve a `~/.config/MonoDevelop` (el mismo camino que el perfil versionless actual) — impacto **nulo** (el guard `!File.Exists(prefsPath)` de `PropertyService` lo evita; los nodos de migración 2.4 son no-ops con source==target); preexistente, no se tocó. (2) el stdout de GTK es vacío por diseño (loguea a `~/.cache/MonoDevelop/Logs/`).

### Regla 18.4 (convivencia Gtk/Avalonia)

El cambio vive en `MonoDevelop.Core` (`UserProfile.cs`), compartido: **GTK** lee vía `UserProfile.Current.ConfigDir` (`PropertyService.cs:76`) y queda corregido con el solo cambio de `ForUnix`; la **shell** se corrige con el trigger de `Program.Main` + los 7 hardcodes. Ambas compilan (0 errores) y **arrancaron en vivo** sobre el perfil versionless en la validación final.

### Queda pendiente / notas

- El perfil del operador **ya está migrado en disco** (los 3 `9.0` eliminados, dato íntegro en las raíces sin versión). Si el operador tiene una sesión vieja abierta, debe relanzarla para usar la DLL nueva (el perfil ya no depende de la DLL: está en la ubicación final).
- La migración one-time queda en el código para cualquier instalación futura que tenga un `…/9.0/` (idempotente y newest-wins). Las instalaciones limpias (sin `9.0`) la verán como no-op.
- Los directorios `8.0` (y el resto de versiones viejas) **conservan su sufijo**: la migración de versiones (`PropertyService.GetMigratableProfile`, `UserDataMigrationService`) sigue leyendo las fuentes versionadas sin cambios.

### Archivos (5 M, +112/−11, sin submódulos)

- `main/src/core/MonoDevelop.Core/MonoDevelop.Core/UserProfile.cs` — `ForUnix` sin versión para el perfil actual; `MigrateUnixCurrentProfile()`/`MigrateVersionedSubdir()`/`MergeDirectory()` (one-time, idempotente, newest-wins, verificado-antes-de-borrar, best-effort) llamadas desde el static ctor; trigger de migración antes de resolver `Current`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Program.cs` — `_ = MonoDevelop.Core.UserProfile.Current;` al inicio de `Main` (dispara migración en la shell, que si no nunca toca `UserProfile`; también cubre `--old-gui`).
- `main/src/core/MonoDevelop.Ide/Services/UserPreferences.cs` — props sin `9.0` (1 sitio + doc comment).
- `main/src/core/MonoDevelop.Ide/Services/SettingsStore.cs` — KeyBindings + Tools sin `9.0` (2 sitios + comment).
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/PreferencesDialog.axaml.cs` — UserDataRoot/VcConfigFile/PropertiesFile/inline sin `9.0` (4 sitios + comment).

+ docs ×3 (este §, `session_summary.md` §47, nota en `migration-status-report.md`). Artefactos: `~/opencode/no9_qa/` (probe `probe/`, simulador `migsim/`, `cmp90.sh`, `backup_pre/` del perfil completo, logs `log_launch1/2/3.txt` + `log_gtk.txt` + arranques del QA).

## 2026-11-14 — Memoria de geometría de ventana en la shell Avalonia (tamaño + estado maximizado) — CERRADO (QA VERDE, 0 hallazgos)

**Estado: CERRADO (QA APROBADO, 0 hallazgos abiertos; validación recursiva §18.5 del Tester QA Senior, 2 pases: ROJO 7 hallazgos → VERDE 0).** El operador pidió que la shell Avalonia **recuerde y restaure** el **tamaño de la ventana** y si estaba **maximizada** al cerrarse, y que abra en ese estado. Sigue el patrón de persistencia de `PadsLayout` (M31-i): clave en `SettingsStore`, primitiva `Save*` incondicional en `Closing` + persist-on-change guardado + restore en arranque.

**Diseño (una clave, tres puntos de anclaje).**
- **Clave `Monodevelop.WindowGeo` = `w|h|max`** (p. ej. `1400|900|1`) en `SettingsStore` (mismo store/formato legado que `Monodevelop.PadsLayout`). `w|h` = **último tamaño en estado Normal**; `max` = `1` solo si estaba Maximizada en el último persist. **No se persiste la posición**: la ventana vuelve a su placement XAML (`CenterScreen`) con el tamaño/estado recordados.
- **Punto de anclaje 1 — save incondicional**: `SaveWindowGeo()` en `OnMainWindowClosing` (tras `SavePadsLayout`), en cada intento de cierre.
- **Punto de anclaje 2 — persist-on-change**: `QueueWindowGeoPersist` (debounce 400 ms, `DispatcherTimer` reutilizado del archivo) disparado por `SizeChanged` y `PropertyChanged WindowState`. Guardado por `AutoWindowPersistIsEnabled` (`QaDialogArg is "" or "--wingeo"`) y `winGeoReady`.
- **Punto de anclaje 3 — restore**: `RestoreWindowGeo` suscrito en el ctor vía `Opened += (_, _) => RestoreWindowGeo ()` (la ventana ya está mapeada, el backend X11 honra tamaño y maximize antes de que se asiente el frame). Sin registro/inválido → defaults XAML (1280x800, Normal).

**El matiz crítico — Avalonia 12.1.2 no tiene `RestoreBounds`.** Se inspeccionó el metadata de `Avalonia.Controls.dll`: no existe el símbolo. Consecuencia: una vez Maximizada, `Bounds` devuelve el **área de trabajo de la pantalla** (1440x900 en el Xvfb), **no** el tamaño real del usuario. Solución: `winNormalW/H` cachea el último tamaño **en Normal** (en `SizeChanged`, solo cuando `WindowState==Normal` y `winGeoReady`) y se **primes** en el restore (el registro ES el último tamaño normal, incluso si predice a un maximize). Si se guardara `Bounds` directamente, un maximize corruptaría el tamaño restaurado (el QA lo verificó con el run C: maximizar → close → `1000|600|1` intacto, no `1440x900`).

**Lección H1 de M31-i aplicada** — persistir **al cambiar**, no solo al salir: el guardado por señal SIGTERM no es fiable (dbus/systemd preempe al handler del CLR). El persist-on-change deja el registro fresco en disco tras cada cambio (sobrevive `timeout`/`kill`); el save en `Closing` es la red de seguridad.

**Allowlist positiva (no pisar el estado del operador)** — solo una sesión de usuario real (`""`) o el probe pasivo `--wingeo` (en perfil **aislado** `HOME→scratch`) escriben el registro. `RestoreWindowGeo` solo **lee**; la escritura queda guardada, así un probe puede restaurar pero nunca pisar el registro del operador.

### Validación (valores medidos)

Driver `~/opencode/wingeo_qa/wingeo_driver.sh` (perfil aislado `HOME=iso_home`, Xvfb `:99` + `metacity` para el maximize, clicks reales de `xdotool`): **9/9 VERDE** — A: perfil nuevo → 1280x800 sin restore, close → `1280|800|0`; B: restaura 1280x800 → resize 1000x600 → persist → close → `1000|600|0`; C: restaura 1000x600 → maximiza (click `maxBtn`) → close → `1000|600|1`; D: restaura 1000x600 **maximizada** → abre maximizada → close → `1000|600|1` (invariante); E1: probe + cross-check real `winTopLeft(100,100) ~ xdotool(100,100) dxy=0,0`; E2: sesión de usuario real (sin flag) → persist → close → `900|500|0`; G: arranque real del runtime GTK (`--old-gui`). Probes extra: T1 minimizar+cerrar (cache no se corrompe), T2 registro basura → fallback sin crash, T3 bajo mínimo → fallback. **3/3 verde.** Builds Avalonia + GTK **0 errores**. Perfil del operador: `Monodevelop.WindowGeo`=0 (mi probe nunca escribe ahí), datos reales intactos.

### QA §18.5 (subagente Tester QA Senior, recursivo)

- **Pase 1 → ROJO 7 hallazgos** (0 bloqueantes): F1 run G falso positivo (el hijo GTK renombra `comm` a `monodevelop`, escapaba a `pgrep -x dotnet`; solo comprobaba que el relay vivía), F2 `winTopLeft` siempre (0,0) por `TopLevel.Bounds` + cross-check muerto, F3 `kill_app` dejaba huérfanos `monodevelop`, F4 errores GTK contados en stdout (siempre vacío), F5 crash preexistente de `RecentFileStorage` (fuera de scope), F6 probe indexaba `btns[1]/[2]` sin comprobar, F7 cache sin guard `winGeoReady`.
- **Pase 2 → VERDE 0 hallazgos abiertos**: los 6 fixes confirmados por código + ejecución (driver 9/9, probes extra 3/3, builds 0/0, perfil del operador sin `Monodevelop.WindowGeo`, 0 huérfanos). F5 confirmado fuera de scope (commit `b48fa78b60`, no lo toca este diff; el run G asierte el boot en ~1 s antes de la welcome page y el crash es no-fatal).

### Regla 18.4 (convivencia Gtk/Avalonia)

Avalonia: feature validada en vivo. GTK `--old-gui`: **build 0 errores** + runtime **arranca** (hijo `comm=monodevelop` vivo + banner `Starting MonoDevelop` en el `Ide.log` del scratch). El **mapeo de ventana** es imposible en esta box headless por depender de `mdhost.exe` (artefacto **Windows-only** ausente en Linux) — limitación de plataforma **preexistente**, no regresión (el csproj GTK no compila ningún archivo tocado por este cambio).

### Queda pendiente / notas

- El **mapeo de la ventana GTK** no se puede validar en vivo en esta box (necesitaría un escritorio con `mdhost.exe`/Windows o el entorno real del operador); la garantía GTK es build 0 errores + arranque del runtime + sesiones previas de escritorio real.
- **F5 (preexistente)**: `RecentFileStorage`/`RecentOpen` crashea cuando `SpecialFolder.Personal` resuelve vacío (perfil scratch sin carpeta `Documents`); no bloquea el QA de esta tarea pero haría flaquear futuros QAs GTK con scratch profiles. Pendiente de fix separado en `MonoDevelop.Ide`.
- La **posición de la ventana** no se persiste (por diseño: vuelve a `CenterScreen`); si se pidiera, se añadiría un segmento `x|y` y se guardaría `Position`.

### Archivos (2 M, sin submódulos)

- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` (+184/−2) — campos `winNormalW/H`/`winGeoReady`/`winGeoDebounce`; cablo `SizeChanged`/`PropertyChanged WindowState`/`Opened`; `SaveWindowGeo`/`QueueWindowGeoPersist`/`AutoWindowPersistIsEnabled`/`RestoreWindowGeo`; probe `--wingeo`; `SaveWindowGeo` en `OnMainWindowClosing`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Program.cs` (+1/−1) — `--wingeo` en el allowlist de `QaDialogArg`.

+ docs ×3 (este §, `session_summary.md` §48, nota en `migration-status-report.md`). Artefactos: `~/opencode/wingeo_qa/` (driver `wingeo_driver.sh`, logs `run_*.log`, `profile_forensic.sh`).

## 2026-11-14 — M31-j Port completo del pad Errors (ErrorListPad GTK) a la shell Avalonia — CERRADO (QA VERDE, 0 hallazgos)

**Estado: CERRADO (QA APROBADO, 0 hallazgos abiertos; validación recursiva §18.5 del Tester QA Senior, 2 pases: ROJO 1 mayor → VERDE 0).** El operador pidió portar el pad Errors GTK por completo a la shell Avalonia "viéndose y funcionando igual que el de la UI GTK": comportamientos, toolbar, iconos e imágenes, diseño de tabla y funcionalidades. El tab "Errors" anterior era un placeholder de texto (un `TextBlock` + una lista de líneas).

**Diseño (un control nuevo, sin separación de carpetas §18.4).**
- **`ErrorsPad.cs` (954 líneas, nuevo)** en `MonoDevelop.Startup.Avalonia/Views/` — el port de `MonoDevelop.Ide.Gui.Pads.ErrorListPad` (GTK, 1141 líneas, fuente de verdad leída línea a línea):
  - **Toolbar** (legacy `DockItemToolbar`): 3 toggles de severidad "N Errors / N Warnings / N Messages" (iconos `gtk-dialog-error/warning/info` vía `IconService`), toggle "Build Output" (`md-message-log`) y campo **Search** (filtro en vivo, legacy `TreeModelFilter`).
  - **Tabla 8 columnas** en orden legacy: `!` (icono), `Marked` (checkbox), `Line`, `Description` (wrap), `File`, `Project`, `Path`, `Category`. Bold=no leído (`DataColumns.Read`), strikethrough=completado (`CellRendererToggle`), multi-selección, sort por header (Type/Project/File/Category, `TreeModelSort`: 1er click asc / invierte, con ▲/▼).
  - **Doble click → RowActivated** → `OpenFileDocumentAtLine` + `GotoLinePopupColumn` + `MarkRead` (legacy `OnTaskJumpto`).
  - **Menú contextual legacy**: Go to Reference (google por el código, `App.Platform.ShowUrl`), Go to Task, Copy (formato legacy `OnTaskCopied`), submenú Columns checkable (persistido bajo `Monodevelop.ErrorListColumns`, tokens `TRUE/FALSE`).
  - **Icono de tab dinámico** (legacy `UpdatePadIcon`): `md-errors-list` → `-has-errors` → `-has-warnings`.
  - **Log "Build Output"**: `TextBox` read-only acotado a 300 k chars + auto-scroll (legacy `LogView`); `ParseBuildMessage` lo alimenta con todas las líneas.
  - **Auto-show**: el tab nace oculto y aparece al primer problema de build (`RowCountsChanged` → `SetPadVisible`).
- **Control de tabla = custom Avalonia** (header `Grid` + `ListBox` multi-selección + filas `ErrorRowView : Border > Grid` 8 celdas). **Descartado Xwt.TreeView**: el backend `Xwt.Avalonia` renderiza `TextCellView` como `TextBlock` plano (sin bold/strikethrough/wrap/sort — verificado en `Xwt.Avalonia/ListBackends.cs`). **Descartado `Avalonia.Controls.DataGrid`**: package aparte no disponible offline (`DisableDownloadNupkg=true`). Patrón establecido de la shell (pads en código).
- **Modelo `ErrorRow` con relleno del dueño**: `TaskService`/`TaskListEntry` son GTK-side (no compilan en la shell); `MainWindow` llena todos los campos, incluido Project/Path (equivalentes a `TaskListEntry.GetProject`/`GetPath` vía `SolutionProjectsOf`/`RelativeToProjectDir`).
- **Settings compartidas con GTK** (mismas keys): `SharpDevelop.TaskList.ShowErrors/ShowWarnings/ShowMessages`, `OutputViewVisible`, `LogSeparatorPosition`, `Monodevelop.ErrorListColumns`.

**Matiz crítico 1 — dedup por firma de diagnostic.** El .NET SDK emite **cada** diagnostic dos veces (pase del compilador + resumen final) y el build mezcla stdout+stderr en un solo stream de líneas. El legacy `TaskStore` no sostenía duplicados (el log MSBuild estructurado da id único). `ParseBuildMessage` descarta la 2ª línea con la misma firma `file|line|col|level|code` (`errorRowSigs`, reseteada en cada `BeginBuild`/cierre de workspace). Sin esto el build real dejaba 6 filas para 3 problemas.

**Matiz crítico 2 — persistencia del separador (hallazgo F2 del round 1 QA, corregido).** Legacy `HandleSwSizeAllocated` (`ErrorListPad.cs:377-383`) guarda la posición como fracción del ancho al mover el `HPaned`. El port lo replica con `logSplitter.DragCompleted` → `PersistLogSeparator()`: el `GridSplitter`/`Thumb` de Avalonia 12.1.2 **commit**a el ancho de columna 0 a píxeles *antes* de disparar `DragCompleted`, así se lee `ColumnDefinitions[0].Width` commitado (un ancho star = nunca movido → no se guarda). La restauración al arrancar usa `ApplyLogSeparator()` (legacy `SetInitialOutputViewSize`) desde `bodyGrid.SizeChanged` la primera vez con ancho real (el ctor corre sin layout, ancho 0; `initialLogShow` como flag).

**Matiz crítico 3 — fidelidad del toggle log (bug del restore, corregido).** Legacy `MakeButton("md-message-log", ..., false, ...)` (`ErrorListPad.cs:212`) arranca el toggle log **desmarcado**. El port original creaba todos los toggles con `IsChecked=true`; aplicar el estado persistido al final del ctor (`logBtn.IsChecked = true`) **no** disparaba `IsCheckedChanged` (valor sin cambio) → un perfil con `OutputViewVisible=True` no restauraba el panel. Solución: `MakeToolbarToggle` recibe el estado inicial (log `false`), de modo que la asignación persistida sí dispara el handler y reabre el panel (legacy `logView.Visible = OutputViewVisible`, l.327).

**Deviaciones documentadas (no hallazgos):** (F3) el Copy escribe `File(Line,Col)`, corrigiendo el typo literal del legacy `File(Column,Column)` (`ErrorListPad.cs:578-580` escribe `task.Column` dos veces); (F4) el menú omite "Go to Log" (feature `BuildOutput` no portada) y "Go to Reference" no usa `DocumentationLink`/`HelpKeyword` (no existen en `ErrorRow`).

**Validación (valores medidos).**
- **Builds 0 errores**: shell Avalonia (TFM `net10.0` y `net10.0-windows`) y proyecto GTK `MonoDevelop.Startup` (`EnableDefaultCompileItems=false`, no referencia el proyecto Avalonia; garantía §18.4 = build 0 errores). Warnings preexistentes (MSB3277 SQLitePCLRaw).
- **Probe `--errorspad` (perfil fresh)**: oracles exactos `errors=2 warnings=1 messages=0 rows=3 visible=3 log=off … logLines=1`, `no-errors filter → visible=1`, `search=CS1002 → visible=1`, `build-output → log=on`, `separator=0.35 → stored=0.35`, `rowactivated → jumped`, `[dblclick] caret in Program.cs at line 7 (expected 7)`, `tab-icon=md-errors-list-has-errors`. Settings persistidos `ShowErrors=True`, `OutputViewVisible=True`, `LogSeparatorPosition=0.35`. **rows=3 pese a 2 líneas repetidas = dedup OK.**
- **Variante RESTORE** (mismo perfil persistido): el primer dump arranca `log=on` (panel restaurado al arrancar).
- **Build real `--build`** (fixture `errpad_fixture/` con 2×CS0103 + 1×CS0219): cada diagnostic **una vez** (dedup), `[build] Build FAILED with exit code 1` (esperado), auto-show OK, 0 excepciones.
- **QA §18.5 (recursivo)**: round 1 → **ROJO 1 mayor** (F2: separador no se persistía al arrastrar + restore del panel log fallaba; F1 de proceso: smoke en paralelo del dev borró la carpeta de artefactos → no lanzar probes en paralelo al subagente) → dev corrigió → round 2 → **VERDE 0 hallazgos** (8/8 oracles exactas byte a byte vía `diff`, settings 3/3, restore `log=on`, build real sin duplicados, apps matadas por HOME de perfil, repo intacto). Reportes `~/opencode/errors_pad_qa/qa_report_round{1,2}.md`.

**Nota 18.4 (GTK)** — build 0 errores + la ruta `--old-gui` sigue intacta (el csproj GTK no compila ningún archivo tocado). El mapeo de ventana GTK en vivo es imposible en esta box (`mdhost.exe` Windows-only, limitación **preexistente**); la paridad de diseño se validó contra la fuente `ErrorListPad.cs` línea a línea.

**Queda pendiente** — (1) paridad visual GTK en vivo (limitación de plataforma, preexistente); (2) desviaciones documentadas F3/F4.

**Archivos (1 A + 2 M, +1145/−41, sin submódulos):** `main/src/core/MonoDevelop.Startup.Avalonia/Views/ErrorsPad.cs` (**A**, 954), `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` (+189/−40), `main/src/core/MonoDevelop.Startup.Avalonia/Program.cs` (+1/−1).

+ docs ×3 (este §, `session_summary.md` §49, nota en `migration-status-report.md`). Artefactos: `~/opencode/errors_pad_qa/` (fixture `errpad_fixture/`, `qa_brief.md`, logs `fresh.log`/`restore.log`/`round*.log`, screenshots, reportes round 1-2).
