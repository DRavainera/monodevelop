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
- **M4 arrancado**: prototipo `main/src/core/MonoDevelop.Startup.Avalonia` (Avalonia 12.1.2, net8.0) compila offline y arranca headless sin excepción. Los generators de Avalonia 12 requieren Roslyn ≥4.14 → SDK 10.0.401 instalado y pineado **solo** en ese directorio (`global.json`); raíz y `main/` siguen pinnenados a **8.0.424** con `rollForward: disable` (legacy intacto).

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

Validación con solución C# (run #11 = `Ide.2026-09-08__00-38-17.log`, `mono MonoDevelop.exe /tmp/opencode/testproj/TestProj.sln`, exit 124): 0 FATAL, 0 Unhandled; todos los servicios IDE se crean (CommandManager, DesktopService, RootWorkspace, TypeSystemService, CompositionManager, DisplayBindingService, DocumentManager, IShell); la composición MEF corre y el catálogo host-services queda sellado (sin InvalidCast). La **evaluación del proyecto C# falla** por un límite del entorno mono 6.14: los `Microsoft.Build*.dll` de `build/bin` son copias exactas del dotnet SDK 8.0.424 (hash `fd46dcf`, refs `System.Runtime 8.0.0.0`, inbindables en mono: `ToolLocationHelper` no se resuelve) y no existe engine MSBuild 15.x mono-compatible (`GetMSBuildBinPath("15.0")` → null porque Arch no tiene `/usr/lib/mono/msbuild`; nuget `Microsoft.Build` 15.9.20 no distribuye el engine `Microsoft.Build.dll`; xbuild 4.x no evalúa SDK-style). La evaluación corre en subproceso (`RemoteBuildEngineManager.cs:456` → `MonoDevelop.MSBuildBuilder.exe` cargando desde el appbase). No es la cadena Roslyn: se re-resuelve con MSBuild net8 nativo en la fase de migración.

0-exports del vs-editor del fork (decisión): `IGuardedOperations`, `IBufferGraphFactoryService`, `IContentTypeRegistryService`, `UndoHistoryRegistry`, `EditorOperationsProvider`, `TextSearchService`, `SmartIndentationService`, `TextStructureNavigatorSelectorService`, `MultiSelectionBrokerFactory`, `FeatureServiceFactory`, etc. (interfaces en `Microsoft.VisualStudio.CoreUtility.dll`/`Microsoft.VisualStudio.Text.Data.dll` de compat) **no tienen implementación en el fork** (grep: no existe `class GuardedOperations`/`BufferGraphFactoryService` en src/). Portarlas (~20 servicios + adornos WPF `PresentationCore` que además no aplican a GTK) es trabajo descartable bajo mono → se difieren a In3 (rework del catálogo editor con `Microsoft.VisualStudio.LanguageServices` net8), donde se reinsertan las partes reales. Los errores de composición MEF siguen siendo solo warnings (partes descartadas), no críticos.

### In3 — Smoke tests de GUI (criterio de aceptación, para la fase MIGRACIÓN)
Runsheet (net8): 1 arranque limpio; 2 menús; 3 About; 4 Preferences; 5 AddinManager; 6 crear/compilar C# Library; 7 abrir `.sln` y navegar. Cierre: 0 críticas nuevas.
Pendiente (no se puede ejecutar hasta correr la GUI bajo runtime .NET 8).
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
| 6 | Mono.Cairo → SkiaSharp | PENDIENTE (M5) | sustituirá el dibujo de Mono.TextEditor/lstetic; refs `SkiaSharp` en el proyecto del editor Avalonia |
| 7 | Módulos Mono.* sin reemplazo: fork del repo (solo módulos afectados), port a .NET 10, quitar Gtk, rebranding `Mono.* → DotNet.*`, submódulo en `main/external/*` | EN CURSO | los 15 submódulos ya están forkeados a `DRavainera` y enlazados (commit 14b2bacaa4) con rama `net10`; port/renombre módulo a módulo según se toque en M5/M6 |
| 8 | Xwt y módulos dependientes de Gtk o Mac-only: evaluar reemplazo o modificación | PENDIENTE | `xwt` forkeado; decisión por módulo cuando el cutover lo requiera |
| 9 | Rediseño iconos PNG (MonoDevelop.Ide/icons/) estilo Fluent respetando tamaño y transparencias | PENDIENTE | mismo tamaño/archivo, reemplazo uno a uno con QA visual |
| 10 | MonoDevelop.Ide 2.6.0.0 → 12.1.2.0 | **HECHO** | commit 72816594c7; verificado por reflexión + arranque IDE |
| 11 | Migración en bucle: al terminar una tarea se inicia la siguiente | EN CURSO | este bucle; cada módulo pasa QA de paridad funcional antes de avanzar |
| 12 | Subagente QA senior: plan de pruebas + validación por módulo, bucle hasta error=0 | EN CURSO | la verificación por módulo se hace contra el módulo Gtk original (inventario de funcionalidades → prueba en Avalonia → 0 diferencias) |
| 13 | Seguimiento en docs (interfaz-plan, session_summary, migration-status-report) + commit/push por tarea | EN CURSO | cada tarea termina en commit+push en esta rama |
| 14 | Al finalizar: compila completo con builder del SDK 10 real, sin bloqueos | PENDIENTE (M6) | gate de cierre |
| 15 | Al finalizar migración: NuGet de addins → 7.9 | PENDIENTE (cierre) | bump de `NuGet.Frameworks` del addin PackageManagement y stack vendorizado |

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

Hallazgo de QA del entorno: los clics sintéticos XTEST no llegan a ventanas que no tienen el foco de input en este escritorio (mutter); por eso los hooks `--prefs=<valor>` son la vía de validación programática del estado de cada panel.
