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
