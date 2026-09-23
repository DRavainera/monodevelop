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
| 6 | Mono.Cairo → SkiaSharp | **EN CURSO (M5/M11y)** | `SkTextEditor` (Avalonia) renderiza vía SkiaSharp (frame nuevo por render + SKSurface) con gutter, caret, multi-caret, folding, burbujas, intellisense (CompletionPopup) y hover tooltip (EditorTooltipPopup); ref base para sustituir el dibujo Cairo de Mono.TextEditor |
| 7 | Módulos Mono.* sin reemplazo: fork del repo (solo módulos afectados), port a .NET 10, quitar Gtk, rebranding `Mono.* → DotNet.*`, submódulo en `main/external/*` | EN CURSO | los 15 submódulos ya están forkeados a `DRavainera` y enlazados (commit 14b2bacaa4) con rama `net10`; port/renombre módulo a módulo según se toque en M5/M6 |
| 8 | Xwt y módulos dependientes de Gtk o Mac-only: evaluar reemplazo o modificación | PENDIENTE | `xwt` forkeado; decisión por módulo cuando el cutover lo requiera |
| 9 | Rediseño iconos PNG (MonoDevelop.Ide/icons/) estilo Fluent respetando tamaño y transparencias | EN CURSO | PNG ya integrados en la nueva UI vía `IconService` (menú principal, secciones de Preferences); redibujo uno a uno continúa con QA visual |
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
- **QA E2E**: clics reales XTEST abren el menú File de Avalonia (popup renderizado con iconos) y Cancel/Create del diálogo funcionan; `dotnet new console` creó `~/TestProj/TestProj.sln` real; `--sln` carga la solución (`Loaded TestProj.sln`), puebla el pad Solution, persiste el reciente y reconstruye el menú. Build 0 errores.

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
- **NewFile** (legacy AddFileDialog vacío): documento "newN.cs" sin archivo backing, persistible con Save As.
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
