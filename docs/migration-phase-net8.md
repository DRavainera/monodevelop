# Plan de ejecución — Fase de migración a .NET 8 (M0–M6)

> Documento operativo que convierte la hoja de ruta de `migration-plan.md` (fases 0-9)
> y el gate de smoke `In3` de `interfaz-plan.md` en hitos concretos y ejecutables,
> anclados al estado real del repositorio. Completar `interfaz-plan.md` (In2) = punto de partida.

## 1. Contexto y evidencia de partida

Estado cerrado en la fase defensiva (commit `67f73bb98d`):

- `MonoDevelop.{Core,Ide}` y el eje CSharpBinding/Refactoring compilan **verdes confoffline** con
  `dotnet msbuild -p:DisableDownloadNupkg=true` (0 errores CS) bajo SDK `8.0.424`.
- La cadena de host-services de Roslyn 3.4 quedó sellada bajo MEF (runs 9–11: exit 124, 0 FATAL,
  0 InvalidCast): `RootWorkspace`, `TypeSystemService` y `CompositionManager` se crean.
- La **evaluación de un proyecto C# bajo mono** queda bloqueada por entorno (MSBuild net8 en
  `build/bin` = copias del SDK, inbindables en mono; no hay engine 15.x para mono en Arch). No es
  la cadena Roslyn: es un límite que se disuelve al mover el runtime a .NET 8.
- Las partes del vs-editor del fork (`IGuardedOperations`, `IBufferGraphFactoryService`, …) quedan
  sin implementación en el tree (diferidas).

Anclas del repositorio:

| Ancla | Lugar | Implicación |
|---|---|---|
| Target net framework | `main/msbuild/MonoDevelop.AfterCommon.props:11` `MDFrameworkVersion=v4.7.2` | Todo `main/` compila como net472 hoy |
| Roslyn pin | `main/msbuild/RoslynVersion.props:3` `3.4.0-beta4-19568-04` | `Features`/`EditorFeatures` ausentes; la deuda CSharpBinding/Refactoring depende de este pin |
| Reconciliación net472 bajo Core | `main/Directory.Build.props` (`MSBuildRuntimeType=='Core'` + ReferenceAssemblies) | El build con `dotnet msbuild` ya funciona; extensible a net8 |
| Compat layer | `main/src/compat/MonoRoslynCompat/src/CompatStubs.cs` (~2k líneas) | Eliminable tras subir Roslyn moderno |
| Host services patch | factories de `MonoDevelop.Ide.RoslynServices` y `.TypeSystem` | Se simplifican (o desaparecen) con Roslyn 4.x |
| MSBuild innativo | `MonoDevelop.Projects.Formats.MSBuild` + `RemoteBuildEngineManager.cs:456` (spawn `MonoDevelop.MSBuildBuilder`) | Bajo net8 corre con `dotnet`; se elimina el hack de copias SDK en `build/bin` |
| UI | xwt/Gtk# (`main/external/xwt`) + `MonoDevelop.TextEditor`/`SourceEditor2` | Sustitución por Avalonia 12 (propuesta `ui-technology-proposal.md`) |

## 2. Objetivo de la fase y gate

Objetivo: llevar el IDE a ejecución 100% SDK/Runtime .NET 8 en Linux, sin mono SDK ni runtime Mono;
con la evaluación/compilación de proyectos C# nativa (MSBuild del SDK) y la UI migrada a Avalonia 12.

Criterio de aceptación (gate final, = runsheet In3 de `interfaz-plan.md`):

1. Arranque limpio del IDE en net8 (0 FATAL/Unhandled/TypeLoad en log).
2. Menús de la shell operativos.
3. Diálogo About (con versión y runtime net8).
4. Diálogo Preferences.
5. Add-in Manager (carga de addins).
6. Crear y compilar una C# Library (SDK-style) desde el IDE.
7. Abrir un `.sln` y navegar por el proyecto (SolutionPad, editor con IntelliSense).

Cierre: **0 críticas nuevas** respecto a la línea base.

## 3. Principios de ejecución

- Linux-first; multi-OS y nuevas funcionalidades fuera de alcance.
- Cada hito cierra con evidencia (build exit, log de run, diff reducido) y documentada en `Doc/`.
- Build offline reproducible: `dotnet msbuild -p:DisableDownloadNupkg=true` con SDK pinneado.
- Compatibilidad net472 mantenida como **línea de rollback** hasta el cutover (M6); net8 se introduce
  como target nuevo en paralelo (estructura `net472` + `net8.0` o switch de `MDFrameworkVersion`).
- Sin feature creep; cada cambio es inversible (commit por bloque).

## 4. Hitos

### M0 — Baseline net8 legible (heredado; cerrar huecos)
- Fijar SDK `8.0.424` (verify/`global.json` en raíz o `main`) para el flujo oficial.
- Documentar comandos de build/run gate en README de trabajo; backup de `addin-db-003` y logs ya en `/tmp/opencode`.
- [x] Evidencia actual: commit `67f73bb98d`; runs 9–11 sin FATAL.
- Criterio: `dotnet msbuild main/Main.sln -p:DisableDownloadNupkg=true` verde para el eje probado.

### M1 — Build de toda la solución con `dotnet msbuild` (deuda Linux: 37 proyectos)
- **MSB3644 resuelto (root cause del build limpio)**: el task `GetReferenceAssemblyPaths` de dotnet msbuild NO replica el fallback de mono. Fix en `main/msbuild/MonoDevelop.AfterCommon.props`:
  `TargetFrameworkRootPath=$(NuGetPackageRoot)microsoft.netframework.referenceassemblies.net472\1.0.3\build\` (el task concatena `\.NETFramework\v4.7.2\`; apuntar a `.../build/.NETFramework/` falla).
  Con esto, los builds con `rm -rf obj` + rebuild son REALES (los "verdes" anteriores eran caché stale de obj).
- **Sweep clean offline (2026-09-08) — 24/24 proyectos** con `rm -rf obj` + `dotnet msbuild -p:DisableDownloadNupkg=true -nodeReuse:false`:
  Refactoring, RegexToolkit, mdmonitor, VsCodeDebugProtocol, VBNetBinding, TextTemplating, ChangeLogAddIn, GtkCore, AssemblyBrowser, DocFood, Deployment, NUnitRunner, PackageManagement, DotNetCore, Gettext, Autotools, VersionControl, UnitTesting, UnitTesting.NUnit, VersionControl.Git, VersionControl.Subversion, Deployment.Linux, ConnectedServices, AspNetCore.
- ConnectedServices/AspNetCore: `Reference`+`HintPath` Roslyn (`common`, `csharp`, `workspaces.common`, `csharp.workspaces` 3.4.0-beta4-final), `Newtonsoft.Json` 13.0.3, `System.Net.Http`(+WebRequest) y `Mono.Cairo`; placeholders nupkg de `Microsoft.DotNet.Web.ProjectTemplates.{1.x,2.0}` en `packages/` (target `_CopyExtensionOutputFiles`).
- **Repositorio de addins**: `addins.monodevelop.com` dado de baja (2026). Nuevo repo mantenido por la comunidad: `https://lastexitcode.com/monodevelop-addins/{version}/main.mrep`. Cableado en `MonoDevelop.Core.Setup/AddinSetupService.cs:GetMainRepositoryUrl` y `MonoDevelop.Core/Runtime.cs:GetRepoUrl` (todas las vías paramétricas por nivel/plataforma colapsan a la URL plana por versión `major.minor`). La purga de repos viejos de `Runtime.RegisterAddinRepositories` sigue cubriendo `addins.monodevelop.com` (ya no está en `validUrls`).
- **Mapa de deuda corregido** (parse de `main/Main.sln`, revisado 2026-09-08): 218 entradas =
  164 proyectos reales + 54 solución-folders. Solo **72 proyectos** están cableados a
  `Debug|Any CPU.Build.0` (config por defecto de `dotnet msbuild`); **160** son construibles en
  ALGUNA config (`DebugGnome|DebugMac|DebugWin32|…`); 4 nunca tienen entrada `Build`.
- De los **92** no construidos bajo `Debug|Any CPU`:
  - **37 deuda Linux real** (target M1): Autotools, CSharpBinding, CSharpBinding.AspNet,
    ChangeLogAddIn, Mono.TextEditor.Shared (shproj), MonoDevelop.AspNet, AspNetCore,
    AspNetCore.DevCertInstaller, AssemblyBrowser, MonoDevelop.Autotools, ConnectedServices,
    Debugger.Soft.AspNet, Debugger.VsCodeDebugProtocol, Deployment, Deployment.Linux, DocFood,
    DotNetCore, Gettext, GtkCore, PackageManagement, Packaging, Refactoring, RegexToolkit,
    TextTemplating, UnitTesting, UnitTesting.NUnit, VersionControl, VersionControl.Git,
    VersionControl.Subversion, VersionControl.Subversion.Unix, NUnit, NUnitRunner,
    PerformanceDiagnosticsAddIn, VBNetBinding, mdmonitor, po.
  - **23 test** (M1 secundario, gate de verificación): IdeUnitTests, Core.Tests(+Addin),
    CSharpBinding.Tests, AspNet.Tests, AspNetCore.Tests, DesignerSupport.Tests, DotNetCore.Tests,
    Debugger.Tests/PerfTests, Ide.Tests, Ide.PerfTests, PackageManagement.Tests, Packaging.Tests,
    Refactoring.Tests, TextEditor.Tests, UnitTesting.Tests, VersionControl.*.Tests, Xml.Tests,
    TestRunner, UnitTests, UserInterfaceTests.
  - **22 externals/submódulos** (fuera de Main; se hacen verdes en su repo o se omiten):
    vs-editor-api (BraceCompletionImpl, PresentationCore/Framework, TextUICocoa(+Util),
    UIAutomation{Client,Provider,Types}, WindowsBase), fsharpbinding (4 fsproj), xwt (Gtk.Mac,
    Gtk.Windows, WPF, XamMac), debugger-libs (CorApi, CorApi2, Mono.Debugging.Win32), guiunit.
  - **10 win/mac-only** (no aplican a Linux): MacPlatform(±Tests), WindowsPlatform(±Tests),
    Debugger.Win32, TextEditor.Wpf, VersionControl.Subversion.Win32(±Tests), WindowsAPICodePack
    {Core,Shell}, Xamarin.PropertyEditing.Mac.
- Elevar primero los **37 deuda Linux** (en el orden de dependencia del tree) a compilación verde
  offline bajo dotnet msbuild: cablear `Debug|Any CPU.Build.0` donde falte, y resolver los
  bloqueadores reales de cada bloque (deps no descargables, APIs ausentes).
- Retirar `./configure ; make` del flujo normal; `scripts/configure` y `Makefile` quedan como compat
  decontaminada o se eliminan tras validar el orquestador dotnet.
- Conciliar `main/Directory.Build.props` (ramas `MSBuildRuntimeType=='Core'`) y `main/MonoDevelop.props`
  para cubrir todos los `TargetFrameworkVersion` presentes (net40–net472).
- Criterio: `dotnet build Main.sln` offline verde para el eje probado + los 37; mapa de deuda por
  bloque documentado con evidencia en este documento.

### M2 — Runtime .NET 8 + MSBuild nativo (desbloquea la evaluación C#)
- Introducir net8.0 como target del eje Core/Ide (switch `MDFrameworkVersion=v8.0` o multi-target).
- Revisar APIs mono-only: `Assembly.LoadFrom`/`AppDomain.AssemblyResolve`, `Path`/`Environment`,
  `System.Runtime.InteropServices` (glib/cairo p/Invoke), carga de add-ins (Mono.Addins⇢net8).
- MSBuildBuilder bajo `dotnet`: `RemoteBuildEngineManager` apuntando al builder net8; `GetMSBuildBinPath`
  devuelve el directorio del SDK; **eliminar** las copias del SDK en `build/bin/Microsoft.Build*.dll`
  (el engine real viene del runtime net8).
- **Hecho (bloque 1)**: `MonoDevelop.MSBuildBuilder` convertido a SDK-style `net8.0` (sin import de
  `MonoDevelop.props`; patrón del shell Avalonia), refs a los MSBuild del propio SDK
  (`$(MSBuildBinPath)\Microsoft.Build*.dll`), apphost nativo copiado a `MonoDevelop.MSBuildBuilder.exe`
  (nombre que espera `RemoteBuildEngineManager`). Remoting eliminado: solo eran `using` muertos
  (`System.Runtime.Remoting*`, `System.Net.Configuration`) — el transporte ya era pipes/`BinaryMessage`.
- Evidencia B1: build standalone rc=0; `dotnet MonoDevelop.MSBuildBuilder.dll` y apphost bootean en
  CoreCLR (alcanzan `RemoteProcessServer.Connect`; error esperado = pipe inexistente, NO type-load);
  sln completo DebugLinux rc=0 con el builder net8 integrado (gate requiere `-restore`).
- Gate : un proceso .NET8 (headless) hace `TypeSystemService` + evaluación + compilación de un
  proyecto SDK-style sin mono — cierra el bloqueo de la fase defensiva.
- Criterio: Core/Ide compilan net8; evaluación de `TestProj.sln` (de `/tmp/opencode/testproj`) exitosa.

### M3 — Roslyn moderno y retiro de MonoRoslynCompat
- `main/msbuild/RoslynVersion.props`: 3.4.0-beta4-19568-04 → **4.8.x** (o la versión publicada en
  nuget.org que incluya la familia Features/EditorFeatures).
- Re-bind de `CSharpBinding`, `Refactoring` contra `Microsoft.CodeAnalysis.*Features` modernas:
  desbloquea bind de tipos, IntelliSense, refactorings y la "deuda Features" histórica.
- **Retirar la capa de compat**: `CompatStubs.cs` (~2k líneas), el synth de
  `OptionsExtensions.GetPropertyNames`, y simplificar los factories host-services (los hacks de
  cast/no-op del commit `67f73bb98d` se reemplazan por servicios reales de Roslyn 4.x).
- Reinsertar el vs-editor diferido (`IGuardedOperations`, `IBufferGraphFactoryService`, …) desde los
  paquetes del editor moderno, no como stubs.
- Criterio: 0 referencias a `MonoRoslynCompat` en el eje probado; composición MEF sin 0-exports de
  host-services; plantillas/refactoring compilan.

### M4 — Shell de UI en Avalonia 12 (prototipo)
- Incorporar paquetes Avalonia 12 y un docking shell (menús, comandos, venana principal).
- Mantener xwt/Gtk# como adaptador temporal para el resto de la UI (no se corta Gtk aún).
- Migrar los diálogos del runsheet: `/About`, `Preferences`, `Add-in Manager`.
- Criterio: la shell Avalonia arranca en net8 con los 3 diálogos operativos.

**Avance (2026-09-08) — primer corte del prototipo en `main/src/core/MonoDevelop.Startup.Avalonia`**:
- Avalonia **12.1.2** (net8.0) + FluentTheme dark + `Avalonia.Fonts.Inter`. Shell con menu (File/Edit/View/Help),
  pads laterales (Solution, Properties) e inferior (Output), splitters, status bar; diálogos About/Preferences/Add-in Manager wireados al menú Help.
- **Toolchain**: los generators de Avalonia 12 requieren Roslyn ≥4.14; el SDK 8.0.424 embebe 4.11 y emite 0 código (`CS9057`). Se instaló el SDK
  **10.0.401** (Roslyn 5.9) en `~/.dotnet` y se pineó por directorio:
  `global.json` (raíz y `main/`) = `8.0.424` (`rollForward: disable`) para TODO el flujo legacy;
  `main/src/core/MonoDevelop.Startup.Avalonia/global.json` = `10.0.401` solo para la shell. Legacy verificado: `dotnet --version`=8.0.424 y build VersionControl verde.
- **Evidencia**: build offline `exit=0`; smoke-run headless `xvfb-run dotnet .../MonoDevelop.AvaloniaShell.dll` 12s sin excepción (rc=124, kill por timeout) sobre runtime net8 (8.0.30).
- Pendiente M4: conectar diálogos a servicios reales del IDE (M2/M3), evaluar `Dock.Avalonia` 12.1.0.x para el shell docking definitivo, acercar la shell al runsheet In3.

### M5 — Migración de vistas por módulos (editor al final)
- Portar: SolutionPad/ProjectPad, Output/Pad, locator, Progress, status bar, Toolbar.
- Editor de código al final: evaluar `AvaloniaEdit` frente a port de `SourceEditor2`; el vs-editor
  importado en M3 alimenta la capa de modelo (buffers/views), la vista es Avalonia.
- Criterio: abrir `.sln`, navegar, abrir un documento C# con IntelliSense en la shell Avalonia.

### M6 — Cutover y runsheet (gate In3)
- Retirar xwt/Gtk#/mono del flujo: **mono fuera** del build y del runtime.
- Ejecutar el runsheet completo (1–7) sobre la shell Avalonia en net8; 0 críticas nuevas.
- Rollback: etiqueta/branch de la línea base net472 (M0/M2) verificable.
- Criterio: runsheet 1–7 verde con evidencia (logs) en `Doc/`.

## 5. Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Salto de Roslyn 3.4 → 4.x (API break masiva: `MSBuildWorkspace`, host services, options) | M3 aislado en su propia rama; gate de bind por proyecto; mantener net472 baseline hasta cutover para comparar |
| Editor: Portar editor complejo a Avalonia | Postponer a M5; el modelo (buffers/servicios de M3) es independiente de la vista; opción AvaloniaEdit como fallback/línea base |
| Add-ins: `Mono.Addins` y catálogo MEF bajo net8 (ALC) | Validación dedicada en M2; pruebas de carga por addin; el catálogo del fork vs-editor se revive en M3 |
| MSBuild: forzar el engine correcto bajo net8 | M2 manda: builder via `dotnet`; el path del SDK resuelto en runtime; quitar las copias del SDK de `build/bin` |
| p/Invoke glib/cairo en Core y `Xwt` | Sólo se elimina al cortar Gtk (M6); hasta entonces se mantiene la ruta de compat |
| Alcance (funcionalidad/multi-OS) | Gobernanza: cada hito cierra con criterio explícito; fuera de alcance documentado en §7 |

## 6. Fuera de alcance (explícito)

- Multi-OS (Windows/macOS) — fase posterior al cutover.
- Debuggers net472 (`MonoDevelop.Debugger*`), `MonoDevelop.DotNetCore` depth, templates ASP.NET.
- Reinserción del árbol NuGet 5.4.0 de `PackageManagement`/`UnitTesting`.
- Nuevas funcionalidad es del IDE.
- Rebranding/UX rediseño completo (solo shell Avalonia con look actual).

## 7. Orden de trabajo recomendado

M0 → M1 → M2 (desbloquea C# bajo net8) → M3 (Roslyn moderno, elimina compat) →
M4 (shell Avalonia) → M5 (vistas) → M6 (cutover + runsheet In3).

Cada hito produce: commit por bloque, evidencia en `Doc/`, y actualización de `session_summary.md`.