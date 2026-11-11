# Session Summary — Build MonoDevelop con dotnet msbuild en Linux

## Goal
Dejar en verde el build del núcleo de MonoDevelop en Linux usando `dotnet msbuild` sobre `main/Main.sln`, mitigando la falta del toolchain legacy (xbuild, nuget.exe/downloadnupkg, Roslyn integrado de VS), y commitear en bloques.

## Constraints & Preferences
- Español; mantener `docs/session_summary.md` actualizado.
- 1 problema a la vez; respetar BOM (`utf-8-sig`) y finales de línea mixtos de `Main.sln`.
- Commits por bloque solo tras build verde; commit/push solo a `origin` (`DRavainera/monodevelop`).
- Build SIEMPRE `-t:Rebuild` (sin él, skip incremental/artefactos del checkpoint dan falsos verdes):
  `export PATH="$HOME/.dotnet:$PATH"; export DOTNET_ROOT="$HOME/.dotnet"`
  `dotnet msbuild main/Main.sln -p:Configuration=Debug -m:1 -t:Rebuild -p:DisableDownloadNupkg=true`
- Decisión de usuario: "Verde núcleo + diferir cluster". Límite de red de agentes previo vigente.

## Estado ACTUAL (build37)
- **Main.sln VERDE: EXIT=0, 0 errores** (6696 warnings; MSB3277/CS8012/CS8002/MSB3245 benignos). **104 proyectos** activos en `Build.0` (config Debug|Any CPU).
- **FASE M2 EN CURSO (migración .NET 8)**: `MonoDevelop.Core` retarget a **net8.0 SDK-style** (bloque 5a, commit `b7418c960b`) — `dotnet build` EXIT=0, 0 errores; load-smoke net8: 567 tipos cargan, probes OK. El sln net472 sigue verde con el arranque previo (`source` de Core ahora es net8; el sln net472 deja de construir `MonoDevelop.Core` durante la migración — rollback vía git).
- Siguen SIN cablear (justificado): 29 tests (GuiUnit 0 bytes), 21 proyectos Mac/Win/WPF (Xwt.*, TextUICocoa*, CorApi*, UIAutomation*, Presentation{Core,Framework}, WindowsBase, WindowsPlatform/MacPlatform, Debugger.Win32, Mono.Debugging.Win32, Xamarin.PropertyEditing.Mac, Core=WindowsAPICodePack, Shell), 7 externos (FSharp fsprojs en `external/fsharpbinding`, BraceCompletionImpl, GuiUnit_NET_4_5), `po` mdproj (gettext), y 2 exes Mac-only (PerformanceDiagnosticsAddIn y AspNetCore.DevCertInstaller, ambos refs `Xamarin.Mac.dll` incondicionales + Mono.Security).
- GtkCore, UnitTesting, UnitTesting.NUnit, NUnit (MonoDeveloperExtensions_nunit), Packaging, ConnectedServices, Gettext, ChangeLogAddIn, Deployment.Linux, CSharpBinding.AspNet, Autotools, Debugger.Soft.AspNet, VBNetBinding y el stack VersionControl ya compilan dentro del sln.
- Cableados al `Build.0` del sln: CSharpBinding `{07CC7654-...}`, MonoDevelop.Refactoring `{100568FC-...}`, AssemblyBrowser `{0EA3AD14-...}`, RegexToolkit `{1F29B0A7-...}`, DocFood `{875D389F-...}`, TextTemplating `{8CCA39DD-...}`, mdmonitor `{D0B5AF2B-...}`, NUnitRunner `{0AF16AF1-...}`, VsCodeDebugProtocol `{10F5BBD5-...}`, AspNet `{1CF94D07-...}`, AspNetCore `{B3E73DE7-...}`, Autotools `{CFC02FEC-...}`, Deployment `{9BC670A8-...}`, PackageManagement `{F218643D-...}`, DotNetCore `{6868153E-...}`.
- Núcleo compilando: Core, Ide, TextEditor, SourceEditor2, vs-editor-api y todos los addins que compilan limpio.

## Deferred / Cluster (deuda) — HISTÓRICO (73 proyectos, en su mayoría ya cableados)
> Nota 2026-09-11: la lista de 73 proyectos era el estado pre-cableado (build26). Casi todos los addins de esa lista ya compilan en el sln (ver "Estado ACTUAL"). Queda deuda real solo en: tests (GuiUnit 0 bytes), plataformas Mac/Win/WPF, FSharp (submódulo externo) y los 2 exes Mac-only. Se conserva como registro histórico de las causas originales.
- **CSharpBinding** `{07CC7654-27D6-421D-A64C-0FFA40456FA2}`: necesita Roslyn EditorFeatures 3.4.0-beta4-final + MonoDevelop.{Refactoring,UnitTesting} (unit-test host, completion). 1391→209 residuos antes de diferir.
- **UnitTesting** `{A7A4246D-CEC4-42DF-A3C1-C31B9F51C4EC}` + **UnitTesting.NUnit** `{6224D87E-2AC1-4D9F-91ED-714F797297BF}` + **NUnit** `{376889B5-6504-46A1-9D18-A9E4B4A50F49}` (+NUnitRunner `{0AF16AF1-...}`, NUnit3Runner `{D2A4E99E-...}`): TestPlatform 16.2.0 no descargable + PM + Newtonsoft.
- **Refactoring** `{100568FC-F4E8-439B-94AD-41D11724E45B}`: Roslyn Features (`csharp.features`/`editorfeatures.text`) solo en cache 3.11.0/4.8.0, no en 3.4.0-beta4-final.
- **GtkCore** `{7FCDB0D9-AA7D-44E4-BE74-55312B432389}`: el diseñador Stetic (GuiBuilder) usa MonoDevelop.{Refactoring,Deployment,CSharp} + ICSharpCode.SharpDevelop.Dom (legacy bin). Diferido entero.
- **PackageManagement** `{98F4461F-84D0-4C57-A11F-47E871BD04A0}`: API skew NuGet.PackageManagement 5.4.0 (tipos internos AmbientAuthenticationState/CredentialResponse/HttpSourceCredentials/ICredentialService; faltan NuGet.Common/Configuration/Core).
- **DotNetCore** `{6868153E-41EA-43A4-A81A-C1E7256373F7}`, **Packaging** `{443311BF-766D-4863-B5A1-AFAA7F41DBDA}`: dependen de PM.
- **TextTemplating** `{8CCA39DD-8412-4547-BE7F-0C3D3ACC6FAC}`: necesita Mono.TextTemplating 1.3.1 tools (descarga deshabilitada).
- **AspNet** `{1CF94D07-5480-4D10-A3CD-2EBD5E87B02E}`, **AspNetCore** `{B3E73DE7-8AFC-429A-9B68-5699B1E63A02}`, **ConnectedServices** `{BF71E2BB-9838-4E0C-B0E7-80559E3909BB}`: PM/DotNetCore/CSharpBinding.
- **Deployment** `{9BC670A8-1851-40EC-9685-279F4C98433D}` (Mono.Unix.Native + ICSharpCode), **Deployment.Linux**, **AssemblyBrowser** `{0EA3AD14-404A-4D3F-979B-F087E2E70C82}` (ICSharpCode.Decompiler), **DocFood** `{875D389F-48D1-4D46-BFC6-998837DD6AE0}` (CSharpBinding/Refactoring): decompiler/dom legacy bin.
- **VersionControl** `{19DE0F35-D204-4FD8-A553-A19ECE05E24D}` (Cairo/Humanizer/Newtonsoft), **Git**/Git.Tests, **Subversion**/Subversion.Unix/Subversion.Win32.Tests, **ChangeLogAddIn**, **Autotools** `{CFC02FEC-...}`.
- **VBNetBinding** `{EF91D0B8-...}`, **RegexToolkit** `{1F29B0A7-...}` (Roslyn), **VsCodeDebugProtocol** `{10F5BBD5-...}`, **mdmonitor** `{D0B5AF2B-...}` (arreglado Cairo pero sigue diferido), **CSharpBinding/Autotools** `{F79A67A1-4BA2-48F8-A7DD-A72E316EF6CD}`.
- **29 test projects** (podados en masa; lista con GUIDs en historial): GuiUnit, IdeUnitTests, MacPlatform.Tests, AspNet.Tests, AspNetCore.Tests, CSharpBinding.Tests, Core.Tests(+Addin), Debugger.{Perf,Tests}, DesignerSupport.Tests, DotNetCore.Tests, FSharp.Tests, Ide.{Perf,Tests}Tests, PackageManagement.Tests, Packaging.Tests, Refactoring.Tests, TextEditor.Tests, UnitTesting.Tests, VC.Git.Tests, VC.Subversion.Tests, Xml.Tests, TestRunner, UnitTests, UserInterfaceTests, Subversion.Win32.Tests, WindowsPlatform.Tests, tests.
- **GuiUnit.exe 0 bytes** → cluster tests entero sin soporte.

## Fixes aplicados (bloque commit)
- `CocoaSupport/CocoaExtras.cs` (stub Linux): IFindPresenterFactory/IFindPresenter, IInfoBarPresenterFactory/IInfoBarPresenter, InfoBarAction, InfoBarViewModel, ICocoaTextView(IsKeyboardFocused+Focus).
- `MonoDevelop.TextEditor.csproj`: `<Compile Remove> CocoaSupport\CocoaExtras.cs` condicionado `'$(HaveXamarinMac)'=='true'`. Mismo patrón invertido considerado para GtkCore-designer (descartado, se difirió GtkCore).
- `CSharpBinding.csproj`: refs Roslyn HintPath (C: `microsoft.codeanalysis.{common,csharp,csharp.workspaces,workspaces.common}/3.4.0-beta4-final/lib/netstandard2.0` + `system.collections.immutable/1.5.0`).
- `MDBuildTasks.targets`: condición `'$(DisableDownloadNupkg)'!='true'` en `DownloadNupkg`/`_DownloadNupkgIfNeeded`; `UnitTesting.csproj` `CopyTestAdapters` gateado igual (evita MSB6006/53 y MSB3030).
- HintPath por patrón: `Debugger.Soft`→mono.cecil 0.10.1 net40; `Debugger.Gdb`/`mdmonitor`/`HexEditor`→`$(MonoFrameworkNet45Directory)\Mono.{Posix,Cairo}.dll` (`/usr/lib/mono/4.5`).
- `Refactoring.csproj`/`Xml.csproj`/`Autotools.csproj`/`GtkCore.csproj` (revertido GtkCore): refs Roslyn+Immutable.
- `Main.sln`: podas iterativas Build.0 (cada una con su build; 26→0 errores: b1..b26). Logs `~/opencode/sln_build{N}.log`.

## Lecciones clave
- `dotnet msbuild Main.sln` impone BuildProjectReferences=false → solo compila Build.0; los demás dependen de bin legacy.
- `IncludeCopyLocal` NO puebla `main/build/bin` (63 dlls) → cada addin debe auto-referenciar vía HintPath.
- Roslyn features del repo (3.4.0-beta4-final) NO están en cache (solo 3.11.0/4.8.0, API incompatible) → Refactoring/CSharpBinding/desьSimple(diseñador) no compilan sin bin VS.
- nuget.exe/downloadnupkg inservible (MSB6006 regex off-by-one? code 53) → flag `DisableDownloadNupkg=true`; assets runtime (TestPlatform 16.2.0, TextTemplating 1.3.1) no disponibles.
- Main.sln: BOM utf-8-sig; indentación 2 tabs; GUIDs con llaves.
- Límite: ~24 proyectos activos + 0 errores alcanzable; el resto es deuda estructural (toolchain legacy) no programática.

## Next steps (post-decisión)
1. ~~(Pendiente de aprobar) Commit bloque 1 "núcleo verde": SourceEditor2 2 archivos, CocoaExtras.cs + TextEditor.csproj, CSharpBinding.csproj, MDBuildTasks.targets, mdmonitor.csproj (Cairo), sln (podas), summary. Push a `origin`.~~ → **HECHO**: commit `07763ed47c` ("Make MonoDevelop core build on Linux with dotnet msbuild (EXIT=0)", incluye CocoaExtras.cs, CSharpBinding.csproj, MDBuildTasks.targets, mdmonitor.csproj, Main.sln), en la rama del worktree.
2. Report final con cluster de deuda.
3. Opcional: `scripts/configure` y otros residuos.

## Relevant files
- `main/Main.sln` (podas), `main/msbuild/MDBuildTasks.targets` (flags), `docs/session_summary.md`.
- Addins tocados: TextEditor (stub), SourceEditor2 (2 fixes previos), CSharpBinding, Debugger.{Soft,Gdb}, HexEditor, mdmonitor, Xml, Refactoring (diferido), UnitTesting (gate), Autotools.
## GUI MonoDevelop en Linux — EJECUTADA (2026-09-06, sesión actual)
- Milestone: la GUI arranca y queda estable con ventana GTK `MonoDevelop` (0x01200003) en DISPLAY :0; proceso vivo (>56s, ~170MB RSS); screenshots en `~/opencode/md_gui_live_shot.png`.
- Admite ejecución SIN `timeout` (loop GTK normal tras el banner "Starting MonoDevelop 9.0 Alpha Preview").
- Causa raíz de los crashers previos y fixes (todos en `main/build/bin`):
  1. `MonoRoslynCompat`/`System.Collections.Immutable` (dll aportadas) y `Mono.Cecil.dll` corrupto → reemplazado por net40 0.10.1 (cache).
  2. Root de addins: `MONODEVELOP_DEV_CONFIG=<abs/main/build/bin>` (dir del `.exe.addins`) + `MONODEVELOP_DEV_ADDINS=/tmp/mddev/registry2`; UserProfile viejo borrado. Con ello Mono.Addins crea host = exe, escanea bin+AddIns, y registra raíces.
  3. `MonoDevelop.Core` no se registraba: faltaban `Newtonsoft.Json` 13.0.3, `Microsoft.Extensions.ObjectPool` 3.0.0-preview9.19423.4, `System.Memory` 4.6.3, `System.Reflection.Metadata` 1.3.0 (netstandard2.0/netstandard1.1) → copiadas del cache NuGet; queda `MonoDevelop.Core,9.0.maddin`.
  4. Roslyn 3.4.0.0: copiadas `Microsoft.CodeAnalysis{,CSharp,CSharp.Workspaces,VisualBasic,VisualBasic.Workspaces}.dll` desde `microsoft.codeanalysis.*/3.4.0-beta4-final/lib/netstandard2.0`.
  5. `Microsoft.Build` 15.1.0.0 (y Framework/Tasks.Core/Utilities.Core) copiadas desde `~/.dotnet/sdk/8.0.424`.
- Resto del viaje previo: bin legacy no es fiable; reparar dlls desde el cache NuGet es el patrón que destraba cada etapa.
- Deuda/trabajo pendiente que NO bloquea la GUI: MSBuild ya presente; TypeSystemService dio 1 TypeError en run10 (pre-MSBuild, probablemente resuelto). Verificar apertura de proyectos.
- `README.md` restaurado a la raíz por decisión del usuario (NO es documentación): `git mv Doc/README.md README.md` (staged, sin commitear).
- Arranque: `cd main/build/bin && export MONODEVELOP_DEV_CONFIG=$PWD MONODEVELOP_DEV_ADDINS=/tmp/mddev/registry2 && mono MonoDevelop.exe`.

## Plan "Interfaz" (estabilización GUI Gtk#) — iniciado
- Decisión del usuario (2026-09-06): tras GUI ejecutable, alcance "Interfaz" = **estabilizar la GUI actual**, sin reescritura.
- Plan redactado: `docs/interfaz-plan.md`. Fases: In1 registro de addins, In2 runtime residual, In3 smoke tests (criterio aceptación), In4 cierre.
- Evidencias de sesión:
  - Smoke baseline: ventana principal, About, Preferences (Edit>P) y AddinManager (Tools>A) abren; diálogos mapeados con XTEST + python-xlib (sin libXtst).
  - About: 1 Gtk-Critical transitorio (`assertion WIDGET_REALIZED_FOR_EVENT`), la ventana se mapea y responde (0 críticas al interactuar).
  - Addins registrados: 14 (Core, Ide, GnomePlatform, ExtensionTool, Debugger*, DesignerSupport, HexEditor, ILAsmBinding, SourceEditor2, TextEditor, WebReferences, Xml).
  - GtkCore addin NO construido (solo libstetic*.dll; falta MonoDevelop.GtkCore.dll+addin.xml); MonoDeveloperExtensions bloqueado por cadena Refactoring (falta _nunit.dll); FSharpBinding diferido.
  - Warnings benignos confirmados: red offline, "Setting process memory limit", templates.

### Sesión 2026-09-07 — validación GUI In1/In2/In3 (parcial)
- Arranque limpio con UNA instancia = SIN ventana About espontánea (el About anterior fue artefacto de secuencias de teclas del primer smoke run).
- Diálogos verificados abriendo: Preferences (Edit>P) y Add-in Manager (Tools>A) generan top-levels nuevos en el árbol X; 0 FATAL/TypeLoad en titras.
- `MonoDeveloperExtensions`: causa del bloqueo aislada → sub-proyecto `NUnit/NUnit.csproj` (OutputPath=`build/AddIns/MonoDeveloperExtensions/`, AssemblyName=`MonoDeveloperExtensions_nunit`) no se produce; build standalone arrastra `MonoDevelop.Refactoring` (cluster deuda, Roslyn 3.11) → falla. Queda diferido salvo desacoplar refs.
- `MonoDevelop.GtkCore`: sigue sin addin construido (solo libstetic*.dll en build/AddIns). Diferido a evaluación separada.
- Intento de abrir-sln vía GUI (Ctrl+O + chooser) y vía arg posicional (`mono MonoDevelop.exe /tmp/mdsample/Sample.sln`): el chooser se abre (ventana "File to Open") pero la ruta no se confirma; con arg posicional no se registró la solución en user-profile (~/.config/MonoDevelop/8.0/MonoDevelopProperties.xml sin Sample). **Item de In3 pendiente de validar** (compilar un proyecto real).
- Build (Alt+B, b): sin artefactos en `/tmp/mdsample/bin/Debug` (no había solución activa).
- Limitación del entorno de throttling: este modelo no acepta imágenes → verificación visual limitada a árbol X + píxeles (contenido real, ~4.7k colores) y logs; las capturas `~/opencode/md_*.png` quedan para revisión humana.
- Herramienta muleta creada: `~/opencode/keys.py` (teclas XTEST robustas) y `~/opencode/killmd.py` (kill por argv exacto, incluido wrapper `mono MonoDevelop.exe`).
- Instancia MD SIGUE CORRIENDO en DISPLAY :0 (1 proceso, log `~/opencode/md_sln.log`, UserProfile regenerado en `~/.config/MonoDevelop/8.0`).

### Sesión 2026-09-07 (noche) — fixes de causa raíz para apertura de solución + aclaración de alcance
- **Aclaración del usuario**: el plan "Interfaz" es la **migración de la GUI a .NET 8 SDK+Runtime**; lo actual es **solo preparación**. La app debe funcionar 100% con SDK/Runtime .NET 8, sin mono SDK ni ejecución con Runtime Mono → a partir de aquí la verificación es a nivel de build (`dotnet msbuild`), no de GUI bajo mono.
- **Fixes de causa raíz (cadena Runtime/Roslyn que impedía abrir una solución)**:
  1. `System.Runtime.CompilerServices.Unsafe.dll` (6.0.0, net6.0) copiada a `main/build/bin` → eliminó un FileNotFound al parsear .sln.
  2. `CompatStubs.cs`: `FeatureOnOffOptions`, `ServiceFeatureOnOffOptions.ClosedFileDiagnostic`, `CompletionOptions` → `PerLanguageOption<T>` con `defaultValue:` + `CompletionOptions.HideAdvancedMembers` añadido → **fixed** `System.ArgumentException: No se puede especificar un nombre de lenguaje para esta opción` en `OptionKey..ctor` (cause del "Could not load solution").
  3. `OptionsExtensions.GetPropertyNames`: con `StorageLocations` vacío (todas las options 3.4) sintetiza `"Roslyn.<Name>[.<Lang>]"` → **fixed** `System.ArgumentNullException: Value cannot be null. Parameter name: name` en `CoreConfigurationProperty<T>..ctor` (`Wrap<T>`→`TypeSystemService..cctor`). Probado con probe reflexivo (SL2: per=True, storage=EMPTY en las 10 options compat).
- **Resultado**: la solución `/tmp/mdsample/Sample.sln` **ya entra al workspace** (evidencia strace `fix3.log`: RecentFiles escribe `file:///tmp/mdsample/Sample.sln` con mime `application/x-sln`); ya NO hay "Could not load solution". El **proyecto** C# clásico queda como "Unknown solution item type" porque `CSharpBinding` nunca se desplegó (carpeta build/AddIns/CSharpBinding vacía).
- **CSharpBinding bloqueado por deuda Roslyn FEATURES (evidencia)**: dependencia `MonoDevelop.Refactoring` usa `Microsoft.CodeAnalysis.ChangeSignature|GenerateType|PickMembers|ProjectManagement|FindUsages|ExtractInterface|CodeActions.WorkspaceServices|CodeFixes.Suppression` + contratos (`ICodFixService`, `IThreadingContext`, `IStreamingFindUsagesPresenter`, `ChangeSignatureOptionsResult`, `PreviewWorkspace`, …) inexistentes en `3.4.0-beta4-final` (Features solo en 3.11/4.8 en caché, incompatibles con Workspaces 3.4). Añadir `MonoRoslynCompat`+`System.Composition.AttributedModel` a Refactoring redujo la lista pero el resto es la API Features completa → diferido a migración (ahí se adopta Roslyn moderno vía NuGet).
- **Addin facilitador offline**: `MonoDevelop.Debugger.VsCodeDebugProtocol.csproj` — `PackageReference` no resolvía para csc (legacy) → sustituido por `Reference`+`HintPath` al caché (`microsoft.visualstudio.shared.vscodedebugprotocol/15.8.20719.1`, `newtonsoft.json/13.0.3`). Compila → `build/AddIns/MonoDevelop.Debugger.VsCodeDebugProtocol/*.dll`.
- **Verificación final**: `MonoDevelop.{Core,Ide}` re-compilan verdes offline con `.NET 8 SDK`; el `LoadMSBuildLibraries` argnull (`path1`) es ruido capturado de hosts mono sin `/usr/lib/mono/msbuild/15.0/bin` y no bloquea (irrelevante en destino net8).
- **Pendientes para el reporte**: apertura+compilación de proyecto C# queda a la migración (CSharpBinding→Refactoring→Features); `docs/interfaz-plan.md` y este summary actualizados con el alcance net8.

### Sesión 2026-09-11 — bloque wiring + deuda System.Collections.Immutable (commit 602f9788db)
- **Contexto**: tras el commit `d519de06a` el sln seguía verde (build27), pero al cablear addins al `Build.0` (build28) fallaron `MonoDevelop.Autotools` (26 errs, depende de `MonoDevelop.Deployment`) y `MonoDevelop.AspNetCore` (20 errs, depende de `MonoDevelop.DotNetCore`/`PackageManagement`), con CS0234 `'DotNetCore' no existe en 'MonoDevelop'` y CS0246 `DotNetCoreVersion`. Error standalone de esos dependientes = CS1705 (`System.Collections.Immutable`).
- **Causa raíz CS1705/CS0012**: tras subir Roslyn a 4.8.0, Core/Refactoring referencian `System.Collections.Immutable 7.0.0`, pero 10 addins antiguos apuntaban a la 1.2.3.0 (`build\bin\System.Collections.Immutable.dll` o `system.collections.immutable\1.5.0`). Bonus: `PackageManagement` requería además `System.Memory 4.5.5` (CS0012 `Span<>`/`ReadOnlySpan<>`).
- **Fixes**:
  - Bump a `$(NuGetPackageRoot)system.collections.immutable/7.0.0/...` en 10 csproj (AspNetCore, Gettext, UnitTesting, UnitTesting.NUnit, PackageManagement, VersionControl, Deployment, Packaging, ConnectedServices, DotNetCore — patrón por regex python, tanto `\` como `/`).
  - `Deployment.csproj`: corregido HintPath `..\..\..\..\$(NuGetPackageRoot)...` → `$(NuGetPackageRoot)...` (mi regex dejó el prefijo relativo residual).
  - `PackageManagement.csproj`: añadido `<Reference Include="System.Memory">` 4.5.5 (patrón de CSharpBinding).
- **Cableado de 3 dependencias** en `Main.sln` (tras los 12 previos): Deployment, PackageManagement, DotNetCore (Insert de `{GUID}.Debug|Any CPU.Build.0` con python, BOM preservado).
- **Incidente BOM**: mi script de escritura duplicó el BOM y metió el literal `ï»¿` → `MSB5010: No se encuentra ningún encabezado de formato de archivo`. Normalizado a BOM único (`EF BB BF` + texto, verificado por bytes).
- **Resultado**: build29 MSB5010 → build30 CS0012 → build31 **EXIT=0**. Autotools y AspNetCore compilan verdes dentro del sln.
- Commit `602f9788db` (97 archivos, +3836/−1313) pusheado a `origin/agents/sub-agent-senior-dev-role-report`: incluye TODO el bloque Roslyn-4.8/compat (CSharpBinding, MonoRoslynCompat, Refactoring — commitablе junto al wiring por solape en csproj) + wiring. Logs: `~/opencode/sln_build{27,28,29,30,31}.log`; backups `Main.sln.bak`/`.bak2`.

### Sesión 2026-09-11 (parte 3) — inicio de la fase de migración .NET 8 (M2 bloque 5a: Core → net8)
- **Bloque 5a (commit `b7418c960b`)**: `MonoDevelop.Core.csproj` → **SDK-style `net8.0`**. Patrón: `Sdk="Microsoft.NET.Sdk"` + override `MDFrameworkVersion=v8.0`/`MDTargetFramework=net8.0`/`TargetFrameworkVersion=v8.0` DESPUÉS del import de `MonoDevelop.props` (que si no fuerza `TargetFramework=net472` vía `AfterCommon.props`).
- Resolver de reference-assemblies: net8 SDK-style resuelve contra `Microsoft.NETCore.App.Ref` 8.0.30 del propio root de dotnet (sin tocar los resolver v4.x de `Directory.Build.props`). El MSB4086 del probe old-style (`_TargetFrameworkVersionWithoutV` vacío con `TargetFrameworkIdentifier=.NETCoreApp`) confirma por qué "retarget old-style descartado".
- Fixes de deps: `SharpZipLib 1.4.2` (Mono.Addins.Setup net8 la exige → NU1605), `System.CodeDom 5.0.0` cacheado (CS1069: `System.CodeDom.Compiler` no está en el ref pack net8), quitadas 10 `<Reference>` muertas de System* (MSB3245; las provee el shared framework).
- Fixes de API net8: `Debug.Listeners` → `Trace.Listeners` (LoggingService), `RegistryHive.DynData`/HKEY_DYN_DATA → `ArgumentException` (IntrinsicFunctions), `NoWarn SYSLIB0011` (BinaryFormatter legacy).
- `AssemblyInfo.cs` sustituido por `GenerateAssemblyInfo` del SDK (Version 2.6.0.0; IVT vía items).
- **Gates**: `dotnet build` EXIT=0, 0 errores, 185 avisos. Load-smoke net8 (`~/opencode/net8_core_smoke`): `MonoDevelop.Core 2.6.0.0 PKT=3ead7498f347467b`, 567 tipos exportados, probes OK.
- Siguiente (bloque 5b): repetir el patrón con `main/MonoDevelop.Ide` + addins clave; luego gate E2E de evaluación C# con Core net8 (M2 completo).
- **método**: lista de los 76 proyectos con `ActiveCfg` sin `Build.0` (GUID→nombre vía script); lotes de 8→3-proyectos; cada lote validado con Rebuild del sln (commits `598aa7b744` y `1cd6e143d1`). Escritura del sln siempre BOM-safe (bytes: descodificar utf-8-sig, editar texto, escribir `BOM+utf-8`).
- **build32** (lote1: Packaging, ConnectedServices, Gettext, ChangeLogAddIn, Deployment.Linux, CSharpBinding.AspNet, PerformanceDiagnosticsAddIn, VersionControl): EXIT=1 → causas: (a) `VersionControl.csproj` tenía el mismo bug de HintPath residual `..\..\..\..\$(NuGetPackageRoot)...` que arreglé en Deployment (mi regex del bump solo quitó `build\bin`, no el prefijo); (b) `ConnectedServices` necesitaba `System.Memory 4.5.5` (mismo CS0012 `Span<>/ReadOnlySpan<>` que PM); (c) `PerformanceDiagnosticsAddIn` es **Mac-only** (ref incondicional `Xamarin.Mac.dll`, código `Foundation`/`ObjCRuntime`) → **des-cableada**. build33 **EXIT=0** (94 activos).
- **build34** (lote2: VC.Git, VC.Subversion, VC.Subversion.Unix, Autotools, GtkCore, Debugger.Soft.AspNet, VBNetBinding, UnitTesting): EXIT=1 → solo `VBNetBinding` CS0246 `VisualBasicCompilationOptions` (Roslyn VB en cache es `4.8.0-7.25569.21`, NO `4.8.0`). Fix: `<NuGetVersionRoslynVB>4.8.0-7.25569.21</NuGetVersionRoslynVB>` en `RoslynVersion.props` + aplicado a las 3 hintpaths VB (VBNetBinding, MonoDevelop.Ide, MonoDevelop.DesignerSupport) → **build35 EXIT=0** (102 activos), y de paso desaparecen los MSB3245 de `Microsoft.CodeAnalysis.VisualBasic.Workspaces`.
- **build36** (lote3: UnitTesting.NUnit, NUnit/MonoDeveloperExtensions_nunit, AspNetCore.DevCertInstaller): EXIT=1 → `DevCertInstaller` es **Mac-only** (refs `Xamarin.Mac.dll` + `Mono.Security.X509`; comentario "Mac's AuthorizationExecuteWithPrivileges") → **des-cableada**. **build37 EXIT=0** (104 activos), 0 errores, 6696 warnings.
- **Veredicto**: cubierto todo el addin de producto Linux del sln. Lo no cableado es estructural: 29 tests (GuiUnit 0 bytes), plataformas Mac/Win/WPF, FSharp (submódulo `external/fsharpbinding`), `po` (gettext) y los 2 exes Mac-only. GtkCore compila el propio addin (sin diseñador Stetic ni ICSharpCode.SharpDevelop.Dom: ese bin legacy se requería solo en su sub-diseñador, ya asumido diferido).

### Sesión 2026-09-08 — sellado de la cadena host-services Roslyn (runs 9-11)
- **Causa raíz del FATAL "Can't create roslyn workspace"**: `MefWorkspaceServices.GetService[T]` (Workspaces 3.4) hace castclass al valor exportado; el `SolutionServices..ctor` pide 3 servicios (`ITemporaryStorageService`, `IMetadataService`, `IProjectCacheHostService`). Los factories de MonoDevelop.Ide exportaban solo `IWorkspaceServiceFactory`, y su unwrap MEF (`b__1` → `CreateService`) producía valores (manager de metadata / cache host) no casteables al contrato interno.
- **Fix sistémico (9 factories/partes de MonoDevelop.Ide)**: cada `[ExportWorkspaceServiceFactory]` implementa además su contrato y `CreateService` devuelve `this` (soporta export directo y unwrap de fábrica). `IMetadataService` (delega vía `GetRequiredService<MonoDevelopMetadataReferenceManager>`), `IProjectCacheHostService` (shim `NoOpProjectCacheHostService`, porque el stub de compat `ProjectCacheService` no implementa el interface interno → el downcast reventaba dentro de `CreateService`), `IDocumentTrackingService`, `IErrorReportingService`, `IFrameworkAssemblyPathResolver`, `ISymbolNavigationService`, `INotificationService` (x2), `IExtensionManager`. Build MonoDevelop.Ide a 0 errores CS.
- **Evidencia runs**: run9 (`Ide.2026-09-08__00-20-02.log`) y run10 (`...00-26-59.log`) exit 124 sin FATAL ni InvalidCast; run11 (`...00-38-17.log`, con `~/opencode/testproj/TestProj.sln`) crea RootWorkspace+TypeSystemService+CompositionManager y compone MEF sellado.
- **Evaluación de proyecto C# bajo mono queda bloqueada por entorno** (no por Roslyn): los `Microsoft.Build*.dll` de `build/bin` son copias del dotnet SDK 8.0.424 (refs `System.Runtime 8.0.0.0`, inbindables) y no existe engine MSBuild 15.x mono-compatible en Arch (`/usr/lib/mono/msbuild` ausente; xbuild 4.x no evalúa SDK-style). Subproceso de evaluación en `RemoteBuildEngineManager.cs:456`. Se re-resuelve con MSBuild net8 en la migración.
- **0-exports del vs-editor del fork (IGuardedOperations, IBufferGraphFactoryService, IContentTypeRegistryService, UndoHistoryRegistry, TextSearchService, …)**: interfaces en compat sin implementación en el fork → diferido a In3 (rework de catálogo con `Microsoft.VisualStudio.LanguageServices` net8). Errores de composición MEF siguen siendo warnings no críticos.
- **Framework paths del sistema** (`TargetRuntime.cs:645-653`): `ReadTargetFramework` captura y registra ERROR por cada `.NETFramework` cuyo `*-api` no existe en Arch (4.6/4.6.1/... ); benigno, frameworks disponibles cargan.

### Sesión 2026-09-15 (noche) — mono sin Mono: GTK# reconstruida desde el clon 2.12 en net10run
- **Contexto**: usuario desinstaló Mono; la app (`main/build/net10run/MonoDevelop.dll`, .NET 10.0.12) ya pasaba MEF/CompositionManager y cargaba glue; el bloqueo era un SIGSEGV en `g_markup_escape_text` (pid 805338, core `md16.core`).
- **Diagnóstico del crash (cierre de la causa raíz)** con `ilspycmd` (herramienta global, `$HOME/.dotnet/tools/ilspycmd`): la `glib-sharp.dll` **del fork** declaraba `g_markup_escape_text (IntPtr text, int len)` — `int` (32-bit) contra el `gssize` nativo (64-bit) → en CoreCLR x64 los bits altos del registro quedan basura → NUL-scan gigante → SIGSEGV. En Mono no explotaba. `Marshaller.g_utf16_to_utf8` del fork también declaraba `IntPtr items_written` by-value (sin `&`), difiriendo del clon.
- **Decisión**: no parchear por IL (el intento previo está en `main/build/gtk-sharp-patch/`), sino **reconstruir todo el managed GTK# desde el clon `mono/gtk-sharp` 2.12** (`~/opencode/gtksharp-2-12`), que usa `IntPtr`/`out IntPtr` correctos.
- **Reconstrucción** (`~/opencode/gsbuild/`): csprojs SDK-style net10, firmados con `gtk-sharp.snk` para los 5 (glib/pango/gdk/atk/gtk → assemblies `glib-sharp`… `gtk-sharp`, v2.12.0.0) y `mono.snk` para `Mono.Cairo` (v4.0.0.0). Requisitos: `DefineConstants GTK_SHARP_2_6;2_8;2_10;2_12` (si no, CS0102/CS0234), `PublicSign=true` (OpenSSL 3 rechaza la firma SHA-1 del snk), excluir los `generated/*.cs` guardados, excluir `AssemblyInfo.cs` original de cairo, y `InternalsVisibleTo("gtk-sharp, PublicKey=…")` en glib-sharp (lo generaba `glib/Makefile.am`).
- **Identidades verificadas con `AssemblyName.GetAssemblyName`** (herramienta `~/opencode/pktcheck`): glib/pango/gdk/atk/gtk-sharp 2.12.0.0 `PKT=35E10195DAB3C99F`; Mono.Cairo 4.0.0.0 `PKT=0738EB9F132ED756` — idénticas a los stageados del fork/xwt.
- **Falsos arranques**: el SIGABRT de `cairo_restore`/`cairo_pattern_destroy` en run17a era por **Mono.Cairo reconstruida** (la del clon 2.12 no satisface a Xwt.Gtk); restaurada la `Mono.Cairo.dll` del fork (`~/opencode/gtksharp-fork-backup/`).
- **run17b (aceptación)**: la app arranca con la nueva cadena glib/pango/gdk/atk/gtk-sharp + Mono.Cairo del fork y queda **estable >10 min**; ventana X mapeada con título `HelloWorld – Main.cs – MonoDevelop` (documento abierto con texto); MEF OK (`IEditorOperationsFactoryService`, `IEditorOptionsFactoryService`), MainWindow inicializada, ~33 addins cargados.
- **Residuos no críticos**: `MonoPosixHelper.so` no encontrado (warning `Mono.Unix.Catalog` + un excepción no fatal en AddinManagerDialog por `Mono.Unix.Native.Stdlib`); diálogo "Acerca de" necesita `System.Xaml 4.0.0.0` (ausente en CoreCLR) → falla no bloqueante; `MonoDeveloperExtensions_nunit.dll` referida en manifest pero no construida (deuda NUnit del sln); motor de temas `murrine` ausente (cosmético).
- **Siguiente**: decidir si reconstruir también `Mono.Cairo` desde una fuente que satisfaga Xwt (o mantener la del fork + gitignore el backup), y revisar los 3 símbolos glue faltantes (`gtksharp_gtk_style_set_mid_gc`, `pangosharp_attr_size_get_absolute`, `pangosharp_attr_size_get_size`).

---

## 2026-09-15 — Fix #3 MEF cerrado: build de MonoDevelop.Ide sin Mono y editor verificable

**Causa raíz #3 confirmada y corregida**
- Cotidiano NRE `EditorPreferences.Wrap[T]` (`ERROR: View failed to load`, `md_run19.log`, `md_run17b.log:249-250`): faltaba la opción MEF `BraceCompletion/Enabled` (`DefaultTextViewOptions.BraceCompletionEnabledOptionId`). En stock la exporta `Microsoft.VisualStudio.Text.BraceCompletion.Implementation.dll` (nunca se construye en Linux; MD usa su propia implementación en `MonoDevelop.SourceEditor2`). Probe `optprobe`: 75 opciones OK, única MISS `BraceCompletion/Enabled`.
- **Fix**: nuevo export en `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Composition/VSEditorShellParts.cs` (namespace `MonoDevelop.Ide.Composition`): `[Export(typeof(EditorOptionDefinition))] sealed class BraceCompletionEnabledOption : EditorOptionDefinition<bool>` con `Key => DefaultTextViewOptions.BraceCompletionEnabledOptionId`, `Default => true` (contrato de tipo verificado: `EditorOptionDefinition` vive solo en `Microsoft.VisualStudio.Text.Logic.dll`).

**Build standalone de MonoDevelop.Ide (entorno sin Mono)**
- El sistema perdió Mono (`/usr/lib/mono/4.5` esqueleto vacío); los HintPath hardcodeados a `/usr/lib/mono/4.5/*` del csproj ya no resolvían → MSB3245/CS0246. `ReferencePath` no se busca por defecto (`Directory.Build.targets:9` define `AssemblySearchPaths` sin `{ReferencePath}`) y **sobreescribir `AssemblySearchPaths` global rompe mscorlib** (CS0518 masivo); el `ReferencePath` global sin override no se consulta.
- **Solución (commit-teable)**: `MonoDevelop.Ide.csproj` — los HintPath de `Mono.Cairo`, `System.Design`, `System.Web`, `System.Web.Services`, `System.Windows.Forms`, `System.Xaml` pasan a `$(MonoFrameworkNet45Directory)/…` (patrón ya usado por el port, ver docs/linux-build-report#2). Dir de refs: **`~/opencode/mdrefs45/`** = `System.*` desde `Microsoft.NETFramework.ReferenceAssemblies.net472/1.0.3` (NuGet cache, sin BCL para no romper System.Runtime) + **`Mono.Cairo.dll` del fork** (md5 `3dfffe2b964f115cc7ec854046ef5d23`, NO la reconstruida que SIGABRT).
- Build OK con: `dotnet msbuild MonoDevelop.Ide.csproj -p:Configuration=Debug -m:1 -t:Build -p:DisableDownloadNupkg=true -p:BuildProjectReferences=false -p:MonoGtkSharpDirectory=~/opencode/mdrefs -p:MonoFrameworkNet45Directory=~/opencode/mdrefs45` → **EXIT=0** (`~/opencode/ide_build_fix4.log`). Salida en `main/build/bin/net10.0/MonoDevelop.Ide.dll` (23:21:00 hoy, contiene `BraceCompletionEnabledOption`).

**Stage + verificación del run fijo**
- `MonoDevelop.Ide.dll/.pdb` stageados a `main/build/net10run/` (backup `.bak092909`); relanzado (`md_run_fix4.log`, pid 369538).
- `optprobe` sobre net10run: `BraceCompletion/Enabled` → **OK**.
- Sin `View failed to load`/NRE. Errores restantes = los pre-existentes: 2 warnings de manifest `MonoDeveloperExtensions_nunit.dll`, assert `MonoDevelopMetadataReferenceManager` y falta de `MonoDevelop.MSBuildBuilder.exe` (todos presentes en run19).
- **Render verificado por píxeles** (modelo sin visión): región editor pasó de 3 colores planos (`appwin_live.png`: #333) a **1419 colores con glifos** (fondo #000 + texto antialias); tab strip pasó de plano a **112 colores con acento azul** `(52,149,215)` — editor con texto y pestañas renderizando.

**Notas/decisiones nuevas**
- El build sln completo sigue roto por proyectos net472 que referencian Core/Ide net10; el flujo real es build por proyecto + stage (confirmado en docs/cold-boot-fix-report.md).
- `~/.var/` (pool wine-mono) quedó **prohibido** por el usuario; las refs BCL se obtienen solo de `~/.nuget` + pack del SDK + fork stageado.
- Siguientes: tema GTK `murrine` (workbench gris), probe headless NuGet 5.4.0, warnings manifest nunit; ya no es necesario reconstruir Mono.Cairo (fork OK).

## 2026-09-16 — Fix #4 (manifest CSharpBinding) y Fix #5 (NuGet net472) + editor plano

**Causa raíz #4: `View failed to load` por tipo faltante en add-in CSharpBinding (ya en `md_run_fix4.log:220-230`)**
- Cadena de extensiones del editor (`TextEditor.InitializeExtensionChain`, `TextEditor.cs:1188`) falla con `Type 'MonoDevelop.CSharp.UnitTestTextEditorExtension' not found in add-in 'MonoDevelop.CSharpBinding,9.0'`.
- Manifest `CSharpBinding.addin.xml:156` declara `<Class fileExtensions=".cs" class=...UnitTestTextEditorExtension/>` pero `CSharpBinding.csproj:176/236` ya había **excluido** `UnitTestTextEditorExtension.cs` ("NUnit integration excluded", depende de `MonoDevelop.UnitTesting`). La clase vive en `MonoDevelop.CSharp.UnitTests/` y extiende `AbstractUnitTestTextEditorExtension` (de `MonoDevelop.UnitTesting`).
- **Fix en fuente**: nodo eliminado de `CSharpBinding.addin.xml` (línea 156). Rebuild completo de CSharpBinding no es viable standalone (CS0006: falta `obj/ref` de Debugger/DesignerSupport/Refactoring/SourceEditor2/TextEditor).
- **Fix de binarios stageados**: parche Cecil en `~/opencode/csbprobe/` (resolver con TODOS los subdirs de net10run) sobre `AddIns/CSharpBinding/MonoDevelop.CSharpBinding.dll` y `.../net10.0/...`: recurso embebido `CSharpBinding.addin.xml` sin el nodo. `grep -c UnitTestTextEditorExtension` = 0; backups `.bak092915`. OJO: `RuntimeAddin.GetType`/`LoadModule` solo recorre las assemblies importadas en el manifest → un stub dll suelto en el dir del add-in no se resolvería; por eso el fix es a nivel manifest.

**Verificación con solución abierta (`md_run_sln.log`, pid 416711)**
- Sin `View failed to load` ni errores de `UnitTestTextEditorExtension`. Ventana: "ConsoleApplication – Program.cs – MonoDevelop".
- Errores restantes no-fatal: `ResultsEditorExtension` (falta export MEF `IDiagnosticService`) y `CodeActionEditorExtension` (falta `ICodeFixService`), ambos en `TextEditor.InitializeExtensionChain` (cadena se recupera por nodo). `GetContentTypeFromMimeType null leg: text/x-csharp` → el registry MEF del editor NO declara el content type `C#` (solo code/UNKNOWN/text/any/intellisense/sighelp/inert/plaintext/sighelp-doc/projection).
- **Estado visual reportado por el usuario**: texto visible pero sin coloreado de sintaxis (plano). Las pestañas renderizan.

**Fix #5: diálogo "Administrar paquetes NuGet" — `StsAuthenticationHandler` no en netstandard2.0**
- Error usuario: `Could not load type 'NuGet.Protocol.StsAuthenticationHandler' from assembly 'NuGet.Protocol, Version=5.4.0.3'` al abrir "Administrar paquetes NuGet" en solución.
- Causa: `MonoDevelop.PackageManagement` compila contra `lib/net472/NuGet.Protocol.dll` (5.4.0, SÍ tiene la clase; usado en `MonoDevelopHttpHandlerResourceV3Provider.cs:62`), pero el staging copió la build **netstandard2.0** (sin `StsAuthenticationHandler`). Runtime → TypeLoadException.
- **Fix**: sustituidas las `NuGet.Protocol.dll` stageadas (`AddIns/MonoDevelop.PackageManagement/NuGet.Protocol.dll` y `net10.0/...`) por la build **net472** del paquete 5.4.0 (backups `.bak092017`). Verificado con probe de carga real en .NET 10: LoadContext=Default, `NuGet.Protocol.StsAuthenticationHandler` resuelve.
- Repro GUI (XTEST): menú Proyecto (~Alt+P) → ítem "Administrar paquetes NuGet" → diálogo `Administrar paquetes NuGet: solución` (0xe0057d). **Sin** error de TypeLoad ni "Unable to load the service index" en `md_run_nuget.log`.
- Deuda nueva: `Unhandled Exception in HighlightingUsagesExtension` (NRE en `ResolveAsync`) al interactuar con el editor.

**Pendientes**: ~~coloreado de sintaxis~~ (FIX #6, abajo), ~~errores MEF `IDiagnosticService`+`ICodeFixService`~~ (FIX #8, abajo), NRE `HighlightingUsagesExtension` (FIX #6c, abajo), `MonoDevelop.MSBuildBuilder.exe` ausente (resuelto luego: presente en main/build y arrancando en net10; verificado 2026-10-17), tema murrine.

## 2026-09-16 (noche) — Fix #6: coloreado de sintaxis del editor + guard NRE HighlightUsages

**Causa raíz #6a: no existía la definición MEF del content type `CSharp`**
- El registry del editor (`ContentTypeRegistryImpl` en `Microsoft.VisualStudio.CoreUtilityImplementation`) construye content types SOLO desde exports `[Export] ContentTypeDefinition` + `[Name]/[BaseDefinition]`. En el árbol stageado la única fuente era `BufferFactoryService` (any/text/code/plaintext/projection/inert) + `DefaultSignatureHelpPresenterProvider` (intellisense/sighelp/sighelp-doc). Nadie definía `CSharp`.
- Cadena: `MimeTypeCatalog.GetContentTypeForMimeType("text/x-csharp")` → null (log "GetContentTypeFromMimeType null leg") → buffer queda con content type `text` → `TextDocument.MimeType` resuelve `text/plain` → `InitializeSyntaxMode` no encuentra definición → `DefaultSyntaxHighlighting` (todo foreground = texto plano).
- **Patrón MEF clave (para otros agentes)**: el registry importa `Lazy<ContentTypeDefinition, IContentTypeDefinitionMetadata>` → el valor exportado debe SER un `ContentTypeDefinition`. Un `[Export]` sobre una clase plain usa la clase como contrato y nunca casa (falso verde compilando). Patrón correcto = campos tipados `ContentTypeDefinition` con `[Export][Name][BaseDefinition]` (como `BufferFactoryService`), verificado con probe reflexivo (`~/opencode/ctprobe2`).
- **Fix**: nuevo part `LanguageContentTypes` en `VSEditorShellParts.cs` (MonoDevelop.Ide) con campos `CSharp` y `VisualBasic` (base `code`).

**Causa raíz #6b: el clasificador del agregador no tiene taggers en este host**
- `ClassifierTaggerProvider` está en `partsToDrop` del catálogo MEF (decisión previa documentada: sus parts rompen la composición), así que `TagBasedSyntaxHighlighting.GetHighlightedLineAsync` (que instala `HighlightUsagesExtension` con scope `source.cs`) siempre recibía 0 classification spans → texto plano aunque el fallback TextMate existiera.
- **Fix**: `TagBasedSyntaxHighlighting` crea ahora un `fallbackHighlighting` (TextMate embebido `C#.sublime-syntax` via `SyntaxHighlightingService.GetSyntaxHighlightingDefinition(FileName)`) y lo usa cuando el clasificador no devuelve spans; reenvía `HighlightingStateChanged` y lo libera en `Dispose`.

**Fix #6c: guard NRE en `HighlightUsagesExtension.ResolveAsync` (CSharpBinding)**
- `highlightsService` (IDocumentHighlightsService) puede ser null si el servicio Roslyn no está en el catálogo → NRE no manejada en cada caret move (`DelayedTooltipShow`). Ahora devuelve `ImmutableArray<DocumentHighlights>.Empty` si es null.
- Bonus: `CSharpBinding.csproj` compiló standalone a 0 errores por primera vez (HintPath de Mono.Cairo apuntaba a `/usr/lib/mono/4.5/Mono.Cairo.dll`, inexistente; movido a `$(MonoFrameworkNet45Directory)`; se tuvieron que poblar `obj/Debug/net10.0/ref/` de Debugger/DesignerSupport/Refactoring/TextEditor con los dlls stageados).

**Builds**: MonoDevelop.Ide (22:41), MonoDevelop.SourceEditor (22:06), CSharpBinding (22:07) → 0 errores; stageados a `net10run` (backups `.bak2210`).

**Verificación (run 22:47, pid 279354, `md_run_syntax3.log`)**
- `dotnet MonoDevelop.dll ~/opencode/ctprobe/Program.cs` → ventana `Program.cs – MonoDevelop` (0xc00341).
- **Coloreado OK**: análisis de píxeles de la región del editor (`~/opencode/editor_syntax.png`): 2.726 colores distintos; ~5.5k px azules (keywords), ~1.4k verdes (strings/comments), ~2.4k naranjas (tipos). Antes: región plana `#333`.
- `GetContentTypeFromMimeType null leg`: **0** ocurrencias (antes: 2 por apertura). NRE `HighlightingUsagesExtension`: **0** (antes: 10+). FATAL: 0.
- Quedan (siguiente fix): `ResultsEditorExtension`/`CodeActionEditorExtension` fallan al instanciarse por falta de exports MEF `IDiagnosticService` e `ICodeFixService` (la cadena del editor se recupera por nodo, pero conviene proveerlos o stubbarlos).
- Artefactos: diálogo File→Open NO acepta rutas tipeadas (files-asociations de GTK filtran: "Archivo no encontrado: ...7tmp7..." — los `/` llegan como `7`); abrir vía arg posicional funciona.

**Pendientes**: errores MEF `IDiagnosticService`+`ICodeFixService` (ResultsEditorExtension / CodeActionEditorExtension) → resuelto luego (FIX #8), `MonoDevelop.MSBuildBuilder.exe` ausente (resuelto luego: presente en main/build y arrancando en net10; verificado 2026-10-17), tema murrine.

## 2026-09-16 (noche) — Fix #7: perfil del usuario caía en ~/Documentos (SpecialFolder.Personal)

**Causa raíz**: `UserProfile.ForUnix` resolvía el home con `Environment.GetFolderPath (Environment.SpecialFolder.Personal)`. En Mono/.NET Framework eso devolvía `$HOME`, pero en .NET Core/5+ `Personal` mapea a `XDG_DOCUMENTS_DIR` (`~/Documentos` con user-dirs en español). Verificado con probe: `Personal=~/Documentos`, `UserProfile=~`, `HOME=~`.
- Resultado: `.config/MonoDevelop`, `.cache/MonoDevelop` y `.local/share/MonoDevelop` se creaban DENTRO de `~/Documentos` (logs de sesión en `~/Documentos/.cache/MonoDevelop/9.0/Logs/`).
- **Fix**: `UserProfile.cs` — nuevo helper `GetUnixHome()` que usa `SpecialFolder.UserProfile` (el que significa `$HOME` en .NET Core) con fallback a la env var `HOME` y por último `Personal`. Aplicado en `ForUnix`.
- Otros usos de `Personal` en core/Ide son default-paths de UI (FileSelector, FileScout, IDEStyleOptionsPanel, ExportProjectPolicyDialog, RecentFileStorage `.recently-used`, ProjectsDefaultPath) — se comportan igual en Mono que antes (documentos), no rompen el perfil; se dejan (candidatos a normalizar si se quiere `$HOME`).
- **Build**: MonoDevelop.Core 0 errores (23:01), stageado a net10run (backup `.bak2301`).
- **Verificación (run 23:03, pid 312168)**: logs nuevos en `~/.cache/MonoDevelop/9.0/Logs/`, config en `~/.config/MonoDevelop/9.0/`, datos en `~/.local/share/MonoDevelop/9.0/`; `~/Documentos` ya no recibe nada nuevo; ventana OK, 0 FATAL; sin migración previa ("Did not find previous version from which to migrate data") — perfil viejo en `~/Documentos` queda como basura eliminable.
- NOTA XDG: si existieran `XDG_CONFIG_HOME`/`XDG_CACHE_HOME`/`XDG_DATA_HOME` definidas, `ForUnix` ya las respeta (no cambia con este fix).
- **Limpieza del perfil huérfano (23:07)**: borrados `~/Documentos/.config/MonoDevelop`, `~/Documentos/.cache/MonoDevelop` (26MB, incluía el `8.0` viejo) y `~/Documentos/.local/share/MonoDevelop` con la app parada. Cold boot posterior verificado: perfil regenerado en `~/.config`/`~/.cache`/`~/.local/share`, ventana OK, 0 FATAL, `~/Documentos` sin rastros de MonoDevelop.

## 2026-09-17 (madrugada) — Fix #8: exports MEF de IDiagnosticService / ICodeFixService / ICodeRefactoringService

**Diagnóstico** (probes de reflexión sobre los binarios stageados):
- El Roslyn 4.8 staged **no trae** los servicios que importan las extensiones del editor viejo:
  - `ICodeFixService` vive en `Microsoft.CodeAnalysis.Editor` (EditorFeatures), **no stageado** por decisión del fork.
  - `ICodeRefactoringService` real existe en `Microsoft.CodeAnalysis.Features` pero es **interno** y con firma distinta (`TextDocument`+`CodeActionOptionsProvider`, 6 parámetros) — el stub de 3 parámetros de MonoRoslynCompat nunca lo satisfará.
  - `IDiagnosticService` (contrato de la época del fork `SystemTools`) ya no existe; su equivalente moderno es el `IDiagnosticAnalyzerService` interno (push/pull diferente).
- Resultado: al abrir cualquier documento, `ResultsEditorExtension` y `CodeActionEditorExtension` fallaban con `Expected 1 export(s) with contract name ... but found 0` (venía del log `md_run_sln.log` del 09-15).

**Fix** (patrón shim inerte, como el telemetría `NoOpLoggingServiceInternal`):
- `MonoDevelop.Refactoring/MonoDevelop.Refactoring/MefExportShims.cs` (NUEVO): `InertDiagnosticService`, `InertCodeFixService`, `InertCodeRefactoringService` — exports `[Export(typeof(...))]` de los contratos stub, con comportamiento vacío (0 fixes / 0 refactorings / 0 diagnostics). Las extensiones componen y arrancan; la funcionalidad de fixes/diagnósticos queda degradada hasta portar el pipeline de Features/EditorFeatures.
- `MonoDevelop.Refactoring.csproj`: `<Compile Include>` del nuevo archivo (EnableDefaultCompileItems=false — **lección: un .cs nuevo no entra al build si no se registra**; el build anterior "0 errores" no lo incluía).
- `CompositionManager.cs`: `partsToDrop` += `Microsoft.CodeAnalysis.CodeRefactorings.CodeRefactoringService` (el part real interno de Features también exporta bajo el AQN del contrato stub y colisiona: "found 2").
- `CompositionManager.Caching.cs`: `CacheVersion` 2→3 (invalida cache MEF por el cambio de catálogo).
- **Build**: MonoRoslynCompat 0 errores (con `-p:NuGetPackageRoot=$HOME/.nuget/packages/` con trailing slash — sin él, `NuGetPackageRoot` vacío rompe todos los HintPath de Roslyn), MonoDevelop.Ide 0 errores, MonoDevelop.Refactoring 0 errores (con `-p:MonoFrameworkDirectory=~/opencode/mdrefs45` para el HintPath Mono.Cairo). Stageados a `net10run` con backups `.bak-shim`.
- **Verificación (run 00:35, pid 535966)**: documento `Program.cs` abierto, ventana viva >2 min, log con **0** "Expected 1 export", **0** "Error while creating text editor extension", 0 FATAL; MEF compone desde cache (v3).
- **Pendiente nuevo detectado**: NRE en `QuickInfoProvider.GetQuickInfoAsync` (tooltip hover, vía editor nuevo) — separado de este fix.
- **Nota de proceso**: la app muere silenciosamente si el proceso queda hijo del shell del tool; lanzar con `nohup setsid` para que sobreviva.

**Pendientes**: NRE `QuickInfoProvider.GetQuickInfoAsync` (hover), `MonoDevelop.MSBuildBuilder.exe` ausente (resuelto luego: presente en main/build y arrancando en net10; verificado 2026-10-17), tema murrine, evaluación de cargar proyecto (MSBuild SDK resolver).

## 2026-09-17 — Fix #9: NRE en QuickInfoProvider.GetQuickInfoAsync (tooltip hover)

**Causa raíz**: `CSharpBinding` compila contra el contrato viejo `Microsoft.CodeAnalysis.ISymbolDisplayService` (namespace 3.x de MonoRoslynCompat), pero el Roslyn 4.8 staged registra el servicio de lenguaje bajo `Microsoft.CodeAnalysis.LanguageService.ISymbolDisplayService` (interno en Features). `GetLanguageServices(...).GetService<ISymbolDisplayService>()` resuelve contra el contrato viejo → `null` → NRE al construir el tooltip. Segundo consumidor con el mismo patrón: `ProtocolMemberCompletionProvider`.

**Fix**:
- `QuickInfoProvider.cs` (CSharpBinding): guard `descriptionService == null` → fallback con `SignatureMarkupCreator` (markup propio del fork, sin depender del servicio Roslyn ausente).
- `ProtocolMemberCompletionProvider.cs` (CSharpBinding): mismo guard en la ruta de completion.
- Stageado `MonoDevelop.CSharpBinding.dll` (build 00:53).
- **Verificación (run 02:13)**: hovers repetidos con XTEST sobre el editor (`~/opencode/hover.py`, barrido de 6 posiciones) → **0** "Object reference not set", **0** "GetQuickInfoAsync" en el log.

## 2026-09-17 — Fix #10: Microsoft.CodeAnalysis.EditorFeatures stageado a net10run

**Origen del binario**: el metapaquete NuGet `Microsoft.CodeAnalysis.EditorFeatures 4.8.0-7.25569.21` no trae `lib/` (solo nuspec+icono); el DLL real vive en `Microsoft.CodeAnalysis.EditorFeatures.Common` (se usó el flavor `netstandard2.0`, coherente con el resto de Roslyn stageado). Toda la clausura de dependencias ya estaba en el cache NuGet local.

**Stageados (16 dlls, ninguno sobrescribe un staged 16.0 de vs-editor)**: `Microsoft.CodeAnalysis.{EditorFeatures, EditorFeatures.Text, Remote.Workspaces, LanguageServer.Protocol, InteractiveHost, Scripting}`, `Microsoft.CommonLanguageServerProtocol.Framework`, `Microsoft.VisualStudio.LanguageServer.{Protocol, Protocol.Internal, Protocol.Extensions, Client}`, `Microsoft.VisualStudio.Debugger.Contracts`, `Nerdbank.Streams`, `Microsoft.ServiceHub.{Framework, Client}`, `System.IO.Pipelines`.

**Ajustes**:
- `CompositionManager.cs`: `partsToDrop` += `Microsoft.CodeAnalysis.Diagnostics.DiagnosticService` (part del assembly LanguageServer.Protocol que exporta bajo el FullName del contrato stub `IDiagnosticService` → colisión "found 2" con el shim inerte y `ResultsEditorExtension` roto).
- `CompositionManager.Caching.cs`: `CacheVersion` 3→4.
- Rebuild de MonoDevelop.Ide a 0 errores (`-p:MonoFrameworkDirectory=~/opencode/mdrefs45` para el HintPath de Mono.Cairo), stageado con backup `.bak-ef1`.

**Verificación (run 02:13, pid 592647)**: ventana "Program.cs – MonoDevelop" viva >8 min con documento abierto y hovers de prueba; **0 FATAL, 0 "View failed", 0 "Expected 1 export", 0 "Error while creating text editor extension", 0 NRE**; solo los 2 ERROR preexistentes de manifest (`MonoDeveloperExtensions_nunit.dll`). Los 118 parts de EditorFeatures entran al catálogo (visible con `MD_LOG_MEF_HOST=1`). Los 37 "found 2" restantes son duplicidad de `IThreadingContext`/`ICodeFixService` (stub MonoRoslynCompat vs real de EditorFeatures) que no lanza excepciones en runtime.

**Cobertura de referencias** (escáner System.Reflection.Metadata sobre 1036 dlls del árbol): los únicos faltantes son gaps preexistentes ya tolerados por carga perezosa (Humanizer, Elfie, SQLitePCLRaw.batteries_v2, MessagePack, ServiceHub.Resources/Telemetry); `System.Runtime.CompilerServices.Unsafe` lo provee el shared framework; WindowsBase/Presentation* solo afectan al camino WPF no usado en GTK.

**Pendientes**: `MonoDevelop.MSBuildBuilder.exe` ausente (resuelto luego: presente en main/build y arrancando en net10; verificado 2026-10-17), tema murrine, evaluación de cargar proyecto (MSBuild SDK resolver), integración real de quick-fixes (el shim inerte sigue; el `CodeFixService` real exige unificar identidad de tipos: `IThreadingContext`, `IEditorOptionsFactoryService`, ...).

## 2026-09-17 — Migración del tooling: /tmp/opencode → ~/opencode

`/tmp` iba a ser borrado: se copió **todo** el toolkit (1,1 GB, 5.803 archivos) a **`~/opencode/`** (verificado con `diff -rq`), se reescribieron las rutas en scripts/csproj/obj y se añadió `~/opencode/README.md` con el contexto para agentes futuros (mdrefs45, runmd_ef.sh, hover.py, arf, ctprobe). Builds standalone: usar `-p:MonoFrameworkDirectory=~/opencode/mdrefs45`.

## 2026-09-17 — Fix #10b: unificación de identidad de tipos en la composición MEF

**Diagnóstico** (con escáner de metadatos `~/opencode/tscan`): `MonoRoslynCompat.dll` **declara** tipos con los mismos FullNames que los tipos internos reales de EditorFeatures (p.ej. `Microsoft.CodeAnalysis.Editor.Shared.Utilities.IThreadingContext`). Con EditorFeatures stageado, el part real `ThreadingContext` y el export del fork `MonoDevelopThreadingContext` caen en el mismo contrato MEF (importaciones ambiguas, "found 2": 37 en el run anterior). El type-forwarding no es opción: los tipos reales son **internos**.

**Fix** (patrón del fork: drop del part real, export único del fork):
- `CompositionManager.cs`: `partsToDrop` += `Microsoft.CodeAnalysis.Editor.Shared.Utilities.ThreadingContext` (real) y `Microsoft.CodeAnalysis.CodeFixes.CodeFixService` (real; el inerte `InertCodeFixService` queda como export único).
- `CompositionManager.Caching.cs`: `CacheVersion` 4→5.

**Verificación (run 02:44, pid 612419)**: documento abierto, hovers OK, **0 "found 2"** (antes 37), 0 "Expected 1 export", 0 errores de creación de extensiones, 0 FATAL, 0 NRE; coloreado de sintaxis intacto (captura `~/opencode/unify_ef_final.png`: 3.201 colores). Los 92 "MEF composition error" restantes son **latentes** (sin excepción en runtime): parts reales de EditorFeatures que importan `IThreadingContext` (identidad del stub ≠ real) o servicios aún sin export (`IWorkspaceDiagnosticAnalyzerService`-family, `IPreviewService`). Desaparecerán al migrar los consumidores del fork al contrato real o al stagear las exports host que faltan.

**Pendientes**: `MonoDevelop.MSBuildBuilder.exe` ausente (resuelto luego: presente en main/build y arrancando en net10; verificado 2026-10-17), tema murrine, migración profunda de identidad de tipos (consumidores fork → tipos reales de EditorFeatures) para quick fixes/diagnostics reales.

## 2026-09-17 — Fix #11: carga de proyectos desbloqueada (resolver de SDK de MSBuild)

**Síntoma**: abrir un `.csproj` moderno abortaba con `UserException: No se encuentra el SDK "Microsoft.NET.SDK.WorkloadAutoImportPropsLocator"` → `MSBuild project could not be evaluated` + `Load operation failed` (log 2026-09-16 23:35:17).

**Diagnóstico** (causa raíz, cadena completa):
1. `Microsoft.NET.Sdk.props` del SDK 8/10 activa `MSBuildEnableWorkloadResolver=true` por defecto (salvo sentinel `DisableWorkloadResolver.sentinel`) → importa `Microsoft.NET.Sdk.ImportWorkloads.props`.
2. Ese .props importa `AutoImport.props` con `Sdk="Microsoft.NET.SDK.WorkloadAutoImportPropsLocator"`: un **SDK virtual** que el resolver real de .NET sirve en memoria (no existe carpeta en `Sdks/`); con MSBuild real el preprocesado lo muestra saltado/comentado, sin error.
3. El motor de evaluación in-process del fork (`DefaultMSBuildEngine.GetImportFiles`) llamaba `SdkResolution.GetSdkPath` que lanza `UserException` si ningún resolver resuelve → la evaluación entera moría. Nota: el resolver real `WorkloadSdkResolver` se cargaba pero chocaba con 2 bugs adicionales del host fork: `SdkResultFactory.IndicateSuccess(IEnumerable...)` **NotImplementedException** (firma multi-path sin implementar en `SdkResolution.SdkResultFactoryImpl`) y `FileLoadException NuGet.Common 6.11.2.1` (conflicto de versión con el NuGet in-process).

**Fix** (mínimo, un punto de llamada único; el `.csproj` con `<Project Sdk=...>` resuelve por otra vía — `GetImplicitlyImportedSdks` — y no pasa por aquí):
- `DefaultMSBuildEngine.cs` (`GetImportFiles`): capturar la excepción de resolución de SDK **solo para imports** (`<Import Sdk="..."/>`), loguear WARNING "...the import will be skipped" y continuar la evaluación — replicando la tolerancia del MSBuild real con imports no resolubles. Los errores del SDK *principal* del proyecto no pasan por este camino.
- Rebuild de `MonoDevelop.Core` a 0 errores y stageado.

**Verificación (run 21:50, pid 290956, `ctprobe.csproj` por línea de comandos)**:
- **0 "Load operation failed", 0 "could not be evaluated", 0 FATAL** (antes: carga abortada).
- Los imports de workloads se saltan con el WARNING esperado: `Could not resolve SDK 'Microsoft.NET.SDK.WorkloadAutoImportPropsLocator' ... will be skipped` (igual para `WorkloadManifestTargetsLocator`). La evaluación continúa y completa.
- El proyecto **se carga en el workspace**: se ejecuta el target `ResolvePackageDependencies` y Roslyn inicializa sus servicios de workspace (IAnalyzerService, IDocumentationProviderService, IPersistentStorageLocationService...).
- El único error posterior es el pendiente ya conocido: `Did not find MSBuild builder .../MonoDevelop.MSBuildBuilder.exe` (builder remoto out-of-proc, necesario para **ejecutar targets**; la **evaluación** del proyecto ya funciona).
- Los 78 ERROR del log son de esa familia `[MSBuild]` (resolvers reales de .NET que el fork carga y no puede satisfacer: NotImplementedException/NuGet.Common) — logueados y tolerados, sin abortar nada.

**Pendientes**: tema murrine, migración profunda de identidad de tipos, sanear los resolvers SDK externos (IndicateSuccess multi-path + NuGet.Common) para que el resolver real sirva los SDK virtuales en lugar de saltarse los imports.

## 2026-09-17 — Fix #12: MonoDevelop.MSBuildBuilder.exe operativo (targets MSBuild en marcha)

**Estado previo**: la evaluación de proyectos funcionaba (Fix #11) pero ejecutar targets moría con `Did not find MSBuild builder .../MonoDevelop.MSBuildBuilder.exe`.

**Trabajo realizado (3 capas):**

1. **Builder remoto construido y stageado**. El fuente existe (`MonoDevelop.Projects.Formats.MSBuild/MonoDevelop.MSBuildBuilder.csproj`) pero el árbol no tenía el `.dll` managed (solo un apphost huérfano del 11/09). Se creó un envoltorio de build **durable** (`~/opencode/msbbuilder/MonoDevelop.MSBuildBuilder.build.csproj`) que compila exactamente los mismos fuentes contra los `Microsoft.Build` del SDK 10.0.401 (los que el builder carga en runtime). Artefactos stageados a `net10run`: `MonoDevelop.MSBuildBuilder.exe` (apphost renombrado, mismo truco que `CopyAppHostAsExe` del csproj original), `.dll`, `.runtimeconfig.json`, `.deps.json`.

2. **Fix .NET Core en GettextCatalog.UICulture** (`Gettext.cs`): en .NET 5+ leer `Thread.CurrentUICulture` del main thread desde un worker lanza `InvalidOperationException` (en Mono era legal). Ahora la cultura se cachea en `uiCulture` durante el static ctor y `SetLocale`; `RemoteBuildEngineManager` la lee para `InitializeRequest.CultureName` sin tocar el Thread. Único consumidor en todo el src.

3. **Fix propiedad reservada MSBuildSDKsPath** (builder): MSBuild 17 define `MSBuildSDKsPath` como toolset property derivada de `MSBUILD_EXE_PATH`; inyectarla como global property mata la evaluación con "La propiedad 'MSBuildSDKsPath' es global y no se puede modificar". Se filtra en `BuildEngine.SetGlobalProperties` y `ProjectBuilder.Run` (paths Initialize/SetGlobalProperties/per-build).

4. **Fix selección de SDK por versión real** (`DotNetTargetRuntime.ResolveSdkMSBuildPath`): ordenaba los directorios del SDK **lexicográficamente** y elegía 8.0.424 sobre 10.0.401 ('8' > '1' como texto) → el builder evaluaba el proyecto con el SDK 8.0.424 y `GenerateRestoreGraphFile` fallaba con `NETSDK1045` (no soporta net10.0) → `.dg` vacío → restore con "No se pueden crear especificaciones de paquete". Ahora ordena por `Version.TryParse` (10.0.401 > 8.0.424). Lección: cualificar `System.Version` explícitamente (dentro de `DotNetTargetRuntime`, `Version` resuelve a la propiedad de instancia).

5. **Diagnóstico permanente**: `[RestoreDiag]` en `MSBuildPackageSpecCreator.GetDependencyGraphSpec` registra en el log del IDE el resultado del target (errors, existencia y tamaño del .dg) — la ruta restore antes era invisible (los errores iban a un NullLogger de NuGet).

**Verificación (run 23:14, pid 457827 + builder pid 469178)**: `[RestoreDiag] GenerateRestoreGraphFile: result=ok, errors=0, dgExists=True, dgLen=21131` — el builder remoto ejecutó el target con el SDK 10 correcto, NuGet completó el restore **sin excepciones** (0 NETSDK1045, 0 "No se pudieron restaurar", 0 FATAL). Los errores restantes del log son la familia conocida: resolvers SDK del SDK 8.0.424 cargados in-proc (NuGet.Common/NotImplementedException, logueados y tolerados), `ToolLocationHelper` con TargetFrameworkVersion vacía (14 evaluaciones in-proc toleradas), manifest `MonoDeveloperExtensions_nunit` (preexistente) y el assertion de `MonoDevelopMetadataReferenceManager` (no bloquea).

**Probe durable**: `~/opencode/msbprobe/` replica el flujo del builder (env vars, resolver ALC hacia el SDK, ProjectCollection con globals del IDE) y confirma el target `GenerateRestoreGraphFile` OK con el SDK 10 (BUILD RESULT: True, .dg 21131 bytes). Útil para depurar futuros problemas del builder sin arrancar el IDE.

**Pendientes**: tema murrine, migración profunda de identidad de tipos, sanear los resolvers SDK externos (IndicateSuccess multi-path + NuGet.Common), verificar build completo F7 de un proyecto en UI.

## 2026-09-18 — Fix #13: SDK por proyecto (global.json) en el builder remoto

**Requisito**: MonoDevelop compila/ejecuta solo con .NET 10, pero **cada proyecto abierto debe poder elegir el SDK con que se compila/debuggea**.

**Hueco encontrado**: la **evaluación** in-proc ya honraba global.json (vía `SdkResolution`), pero el **builder remoto** recibía siempre `BinDir` del SDK más alto (`DotNetTargetRuntime.sdkMSBuildPath`, 10.0.401) — un proyecto con `global.json` → SDK 8.0.424 se habría compilado con el SDK equivocado. Síntoma observado: el build de un proyecto net8.0 quedaba estancado con el builder de SDK 10.

**Fix (2 piezas):**

1. `DotNetTargetRuntime.GetProjectSdkMSBuildPath (projectDirectory)`: resuelve el SDK del proyecto subiendo por los directorios padres buscando `global.json` (caché por directorio); si la versión pedida está instalada devuelve ese SDK, si no (o sin global.json) cae al más alto. Log INFO `Project SDK 'X' selected from <global.json>` y WARNING si la versión pedida no está instalada.

2. `RemoteBuildEngineManager.GetRemoteProjectBuilder`: resuelve el SDK por proyecto y lo usa como `BinDir` del `InitializeRequest`; la clave del pool pasa a incluir el SDK (`runtime # solution # group # sdkDir`), así cada SDK tiene su propio builder con MSBuild coherente.

**Viabilidad probada antes de integrar** (`~/opencode/msbprobe` + `~/opencode/sdk8probe`): el apphost .NET 10 con Microsoft.Build 17.14 del SDK 10, apuntando env/resolver al SDK 8.0.424, ejecutó `Restore` + `Build` completos de un proyecto net8.0 (`NETCoreSdkVersion=8.0.424` escrito por un target del propio proyecto) — mezcla host-10/SDK-8 es estable.

**Verificación runtime**: proyecto `sdk8probe` (net8.0 + `global.json` 8.0.424, `rollForward: disable`) cargado en el IDE. Log: `Project SDK '8.0.424' selected from ~/opencode/sdk8probe/global.json`; `/proc/<builder>/maps` muestra que **los builders remotos cargan `~/.dotnet/sdk/8.0.424/Microsoft.Build.dll`** (no el 10). La ruta por defecto (sin global.json → SDK 10) quedó probada en Fix #12 con ctprobe (build real desde UI). Nota UI: el disparo F8 por automatización XTEST es poco fiable (foco del editor GTK), pero la cadena de selección queda probada por maps/artefactos.

**Pendientes**: tema murrine, migración profunda de identidad de tipos, sanear resolvers SDK externos, depurar el disparo de build desde UI (foco GTK) y el estancamiento ocasional del primer build (se recupera reintentando).

## 2026-09-18 — Fix #14: resolvers SDK externos sanados (0 errores [MSBuild] en arranque)

**Estado previo**: el arranque acumulaba 52 errores `[MSBuild]` tolerados: 26× `NotImplementedException` (los resolvers reales del SDK llamaban los overloads multi-path de `SdkResultFactory`, que en la base son `virtual { throw NotImplementedException(); }` y el fork no los sobreescribía) + 26× `SDK not found` (consecuencia: los SDK virtuales de workload quedaban sin resolver) + el `NuGetSdkResolver` no cargaba (`NuGet.Common 7.9` pedido frente al 6.11 ya cargado en el contexto default). Los imports de workloads se saltaban en evaluación.

**Fix (2 piezas en `SdkResolution.cs`):**

1. **`SdkResultFactoryImpl` completo**: se implementan los 4 overloads multi-path (`IndicateSuccess` con `paths`/`propertiesToAdd`/`itemsToAdd`/`warnings`/`environmentVariablesToAdd`, y el variante de un path con props/items) y `SdkResultImpl` gana un ctor multi-path: primer path → propiedad `Path` (virtual), resto → `AdditionalPaths` (setter público), más `PropertiesToAdd`/`ItemsToAdd`/`EnvironmentVariablesToAdd` vía setters protegidos, y se pobla el `SdkReference` base (antes nulo). Firmas tomadas por reflexión del `Microsoft.Build.Framework.dll` real (probe `~/opencode/fwprobe`).

2. **`SdkResolverLoadContext`**: los resolvers externos (`SdkResolvers/<name>/<name>.dll`) ya no cargan con `Assembly.LoadFrom` (contexto default, colisión de versiones) sino en su propio `AssemblyLoadContext` con `AssemblyDependencyResolver` sobre su `<name>.deps.json` y probe del propio directorio con guarda de versión mayor/menor; las assemblies del host (Microsoft.Build*, System.*, Mono*, MonoDevelop*, mscorlib/netstandard) devuelven null para unificar tipos con el proceso.

**Verificación (sdk8probe con global.json 8.0.424)**: **0 errores `[MSBuild]`** (antes 52), **0 NotImplementedException**, **0 "SDK not found"**, NuGetSdkResolver **carga**, **0 imports de workloads saltados** (los SDK virtuales ahora se resuelven de verdad en evaluación, como el MSBuild real), 0 FATAL, selector de SDK intacto (`Project SDK '8.0.424' selected`). Sanity en la ruta por defecto (ctprobe net10 sin global.json → SDK 10): 0 FATAL, 0 errores, carga normal.

**Pendientes**: tema murrine (ruido GTK del arranque), migración profunda de identidad de tipos, disparo de build desde UI (foco GTK/XTEST) y estancamiento ocasional del primer build (probablemente ya resuelto con Fix #13; confirmar).

## 2026-09-18 — Fix #15: murrine silenciado, evaluador MSBuild reforzado y builder con MSBuild del SDK del proyecto

**1. Murrine**: el tema GTK2 del usuario exige el engine murrine (no instalado) → Gtk-WARNING por dos vías (parseo del gtkrc durante `Gtk.Application.Init` y `GLibLogging.LoggerMethod`). Se filtra el mensaje estable del engine ausente en ambos puntos (`IdeTheme.cs` instala un `LogFunc` temprano con delegado mantenido estáticamente; `GLibLogging.cs` filtra en la vía tardía). Sin cambios de theming. Verificado: 0 menciones en log/stdout.

**2. Evaluador in-proc reforzado** (la cadena del "estancamiento" era en realidad restore corrupto):

- **Causa raíz descubierta con binlogs** (patrón `~/opencode/binlogreader`): el SDK moderno deriva `TargetFrameworkIdentifier` con `$([MSBuild]::GetTargetFrameworkIdentifier/Version(...))` y normaliza con `$(TargetFrameworkVersion.TrimStart('vV'))`; el evaluador del fork no soportaba nada de esto → `_TargetFrameworkVersionWithoutV` quedaba sin resolver → `WarningLevel=$(_TargetFrameworkVersionWithoutV.Split('.')[0])` producía el literal `$(TargetFrameworkVersion` → FormatException al leer la configuración → **popup al cargar sdk8probe**.

- **Fixes**:
  a. `TargetFrameworkIntrinsics.cs` (nuevo): `GetTargetFrameworkIdentifier/Version` con el parser **NuGet.Frameworks real** cargado por reflexión (preferencia por el ensamblado ya cargado; candidatos junto a MSBUILD_EXE_PATH y SDKs instalados) y fallback portado verificado contra el MSBuild real (net+1–4 dígitos → .NETFramework; 5+ → .NETCoreApp; netstandard; versión con padding). Semántica extraída con probe (`~/opencode/tfprobe`).
  b. `IntrinsicFunctions.Extensions.cs`: los dos forwards de métodos.
  c. `MSBuildEvaluationContext.cs`: invocación de métodos con parámetros opcionales (defaults rellenados), **ranking unificado de sobrecargas** (menos defaults gana, params compite, conversión escalar→array estilo MSBuild, string→char[] por fan-out con `ToCharArray` — p.ej. `TrimStart('vV')`), conversión string→char de 1 caracter.

- **Popup eliminado**: sdk8probe carga sin errores (0 FATAL, 0 FormatException, 0 WarningLevel).

**3. Builder remoto con el MSBuild del SDK del proyecto (extensión del Fix #13)**:

- **Hallazgo**: aunque el builder recibía el `BinDir` del SDK 8, el `Microsoft.Build.dll` (y su `NuGetFrameworkWrapper`) que cargaba era la **copia junto al exe** (net10run, mezcla host-10/SDK-8). El dg del restore salía bien del builder pero el **round-trip IDE** (NuGet 5.4 del addin) reinterpretaba `net8.0` como .NETFramework (alias `net80`) → assets `['.NETFramework,Version=v8.0']` → NETSDK1005 → build fallido silencioso.

- **Fixes**:
  a. `Main.cs` del builder: `PreloadSdkMsBuildAssemblies (msg.BinDir)` en `Initialize` — precarga `Microsoft.Build{,.Framework,.Tasks.Core,.Utilities.Core}` y `NuGet.Frameworks` **del SDK pasado por IPC** antes de que nada bindée las copias locales. Dump del builder confirma `~/.dotnet/sdk/8.0.424/{Microsoft.Build.dll,NuGet.Frameworks.dll}` cargados; binlog: `NuGetTargetMoniker='.NETCoreApp,Version=v8.0'`.
  b. Runtime: las dos copias de `NuGet.Frameworks.dll` del addin PackageManagement actualizadas 5.4 → **6.11.2.1** (la del SDK 8; entiende net5.0+ y es compatible hacia arriba con el stack NuGet del addin) — el stack del IDE ya no reinterpreta TFMs modernos.
  c. `MSBuildPackageSpecCreator`: binlog de diagnóstico del target restore con `MONODEVELOP_NUGET_RESTORE_VERBOSE=1` (mismo espíritu que `[RestoreDiag]`).

- **Verificación end-to-end (F8 real)**: dgspec/assets con **`net8.0`** (antes `net80`/`.NETFramework,Version=v8.0`), **0 NETSDK1005**, y **dll final en `bin/Debug/net8.0/`** (sdk8probe.dll + apphost) compilado por el builder con el SDK 8.0.424. Sanity ruta por defecto (ctprobe net10 → SDK 10): recompila y 0 NETSDK1005/0 FATAL.

- **Pendiente nuevo detectado**: la run-configuration de ejecución intenta lanzar `bin/Debug/net8.0/sdk8probe.exe` (extensión .exe que no existe en Linux/.NET) — corregir la selección del ejecutable para proyectos .NET.

**4. Fix de seguimiento (mismo día 22:58): `TargetException` al resolver intrínsecos en el IDE**:

- **Causa**: `TargetFrameworkIntrinsics` invocaba los *getters de instancia* de `NuGetFramework` (`Framework`, `Version`, `Platform`, `PlatformVersion`) con `Invoke(null, args)` → `TargetException: Non-static method requires a target` (56 errores "MSBuild property evaluation failed" IDE-side al cargar cualquier proyecto). Apareció al activar la preferencia por el `NuGet.Frameworks` ya cargado (6.11 del addin) — esa ruta antes no se ejercitaba.
- **Fix**: los cuatro `Invoke` pasan la instancia (`nugetGetFramework.Invoke(fw, null)` etc.).
- **Verificación**: arranque IDE con sdk8probe → **0** errores de evaluación (antes 56); restore-on-load `result=ok, errors=0` con `runtimeIdentifierGraphPath` del **SDK 8.0.424** y assets `targets: ['net8.0']`; dll final verificado en `bin/Debug/net8.0/` (corrida F8 22:39–22:42 con 0 errores de build).

**5. Fix workspace: export inerte de `IDiagnosticUpdateSourceRegistrationService` (commit 9c5aeba09d)**:

- **Causa del popup al cargar proyectos**: `ProjectSystemHandler` importa el stub compat (`DiagnosticsCompat.cs`) para registrar el `HostDiagnosticUpdateSource` al cargar un workspace, pero nada lo exportaba desde que se retiró el fork Roslyn SystemTools → "found 0" → `Could not load parser database` → diálogo de error al usuario. El contrato real es internal a `Microsoft.CodeAnalysis.Features` con otra identidad de tipo (no satisfacía la importación).
- **Fix**: shim inerte `InertDiagnosticUpdateSourceRegistrationService` en `MefExportShims.cs` (patrón existente) + bump del MEF `CacheVersion` 5 → 6.
- **Verificación**: arranque con sdk8probe → 0 `Could not load parser`, proyecto cargado, restore ok. Nota: el bump revela 92 `MEF composition error` INFO-tolerados preexistentes (familia `MonoDevelopThreadingContext`/host VS ausente) que el camino cacheado no re-logueaba.

**6. Fix launcher de ejecución .NET: apphost sin extensión en Unix**:

- **Causa**: el camino genérico de ejecución (`DotNetProject.OnCreateExecutionCommand`) usaba `CompiledOutputName`, que añade `.exe` para ejecutables no-librería; en Linux el SDK .NET produce un **apphost nativo sin extensión** → `Win32Exception: No existe el fichero` al ejecutar (F5).
- **Fix**: si el destino termina en `.exe` y no existe, se usa el apphost sin extensión del mismo directorio si está presente.

**Pendientes restantes**: migración profunda de identidad de tipos (errores MEF tolerados), y AvaloniaUI 12 en espera de señal explícita del usuario.

## 2026-09-22 — UI Avalonia: migración en cadena M11v → M11y (pads, diálogos y editor)

Bucle de migración Gtk → Avalonia (`main/src/core/MonoDevelop.Startup.Avalonia`,
net10.0) documentado en detalle en `docs/interfaz-plan.md` § M11v–M11y:

- **M11v (b98f6f5b57)** — `PadHost.AddTab` auto-selecciona la primera pestaña:
  los pads RightPads/BottomPads nacían con contenido vacío (`SelectedId=null`).
- **M11w (846703135a)** — Properties pad con **datos reales del nodo
  seleccionado** (descriptores del PropertyGrid legacy: Solution/Project/
  ProjectFolder/ProjectFile leen el `.sln`/`.csproj` en disco; repuebla con la
  selección del árbol; fix de filas duplicadas por controles reutilizados).
  QA `--props` (23 filas) + captura.
- **M11x (cc2024c15c)** — **DirtyFilesDialog ("Save Files")**: gate de cierre
  con documentos modificados, portado del legacy (checkboxes con cascada y
  tri-state, agrupación "Project: X", Save and Quit/Close · Quit/Close ·
  Cancel). Cableado en CloseDocument/Close Workspace/Exit/cierre de ventana
  (`Window.Closing` ≡ `OnDeleteEvent`). QA determinista `--dirtyfiles` + E2E
  real con WM_DELETE (bloquea el cierre) y E2E del botón (guarda y cierra).
- **M11y (c4fd345c44)** — **Editor**: (1) fix de líneas fantasma/duplicadas —
  cada frame se renderiza en un WriteableBitmap NUEVO (el compositor seguía
  leyendo el bitmap en mutación al teclear); (2) Backspace/Delete multi-caret y
  eliminación del flag `applyingCommit` (se quedaba pegado y tragaba refreshes);
  (3) **CompletionPopup** (port CompletionListWindowGtk: `.` y Ctrl+Space,
  iconos element-*, filtrado en vivo, navegación, Enter/Tab/Escape) con commit
  E2E verificado (`Console.` → popup → `WriteLine` insertado); (4)
  **EditorTooltipPopup** (port TooltipProvider: hover 500 ms con firma de la
  declaración, icono legacy, posición PointToScreen); (5) cursor I-beam, editor
  pad abierto sin pestañas y QA `--editqa` no destructivo (restaura el archivo).

**Build/run**: el binario vivo es
`src/core/MonoDevelop.Startup.Avalonia/bin/Debug/net10.0/MonoDevelop.AvaloniaShell.dll`
(`~/.dotnet/dotnet … --sln=… [--skip-welcome|--editqa|--props|--dirtyfiles]`);
`build/net10run/MonoDevelop.dll` es el IDE GTK legacy (muere en remoting) — no
usarlo para probar la UI Avalonia.

**Siguientes en la cadena**: SelectEncodingsDialog (Preferences > Encodings),
NewConfigurationDialog/NewLayoutDialog, AttachToProcessDialog (Debugger),
semántica Roslyn real para completion/tooltip (actualmente palabras del
documento + keywords).

## 2026-09-22 (b) — M11z: tests xunit del editor + Diff en pad + TipOfTheDay/ProgressDialog

- **Tests** (`main/tests/AvaloniaShell.Editor.Tests`, `dotnet test`, 16 en
  verde): modelo del editor — líneas, carets, backspace multi-caret, undo/redo,
  dirty. Encontró 2 bugs reales: `BackspaceForQa` single-caret vs handler
  multi-caret (unificados en `DeleteBackwardAtCarets`) y modelo vacío al
  construir (default de TextProperty no dispara OnPropertyChanged; el ctor
  ahora siembra `SetLines`). README del shell actualizado con la tabla de hooks.
- **Diff**: de ventana modal con borde OS → pestaña "Diff" en el pad inferior
  (icono `vc-diff`), reemplazo de contenido en cada ejecución y auto-selección,
  como el visor interno del legacy. Verificado con 14 líneas de patch reales.
- **TipOfTheDayDialog**: tips del XML legacy, primer tip aleatorio, Next cicla,
  checkbox persiste la preferencia invertida (GetBool/SetBool nuevos).
- **ProgressDialog**: barra + Cancel/Close + expander Details con log de tareas
  indentado (BeginTask/EndTask/WriteText) + ShowDone con los 3 estados finales;
  `--totd` y `--progress` los ejercitan de forma determinista. Capturas
  verificadas. Commit `b9b2fd0e42`.

## 2026-09-23 — M13: configs reales (.sln/.csproj) + File>Open importa proyectos

- `Services/ConfigurationService.cs`: lectura/escritura de configuraciones como
  el ProjectService legacy (SolutionConfigurationPlatforms + ProjectConfigurationPlatforms
  por GUID en .sln; PropertyGroup Condition 'Name|AnyCPU' en .csproj; Any CPU⇄AnyCPU).
- NewConfigurationDialog persiste la config real y recarga el árbol
  (QA `--newconfig-real`: created=True sln-entry=True csproj-entries=True + cleanup).
- File > Open enruta .sln/.csproj/documentos; el .csproj suelto se importa creando
  el .sln wrapper (`AddProjectToSolution`) — QA `--openimport` + captura del árbol.
- El .sln wrapper ya no se lista como archivo del proyecto en el Solution pad.
- Cableados los últimos comandos sueltos: BuildSolution, RunCodeAnalysis×2,
  FindNextSelection. Tests 16/16 (gate pre-commit).
- Docs: M13 en interfaz-plan.md + sección de configs en el README del shell.

## 2026-09-23 (b) — M14: Active Configuration real + New Project a la solución + pad Bookmarks

- Config activa persistida en .userprefs (formato legacy exacto); toolbar combo
  y menú Project > Active Configuration dinámicos desde el .sln cargado
  (QA --activeconfig: persist+switch+verify+restore).
- New Solution dialog: "Add to open solution" + AppendProjectToSolution
  (GUID, mappings por config, sección creada si falta; QA --newproject con
  AutoCreateForQa no destructivo).
- Pad Bookmarks en la zona debug (filas por bookmark, doble clic salta,
  refresco en toggle/clear/cambio de doc; QA --bmkpad + captura).
- Tests 16/16. Docs + README actualizados.

## 2026-09-23 (c) — M15: pad Breakpoints con persistencia + Bookmarks contextual + build/run con config activa

- Pad Breakpoints en la zona debug (filas icono+`Archivo:línea`, menú Go to/
  Enable-Disable/Remove/Clear All) y gutter markers rojos/grises con toggle por
  clic en SkTextEditor.
- Persistencia real en `<sln>.userprefs` bajo
  `MonoDevelop.Ide.DebuggingService.Breakpoints` (formato Mono.Debugging,
  líneas 1-based); restauración al abrir cada documento.
- Pad Bookmarks: menú contextual Previous/Next/Remove/Remove All.
- Build (`dotnet build -c "<config>"`) y Run (`dotnet run -c "<config>"`) usan
  la Active Configuration persistida.
- QA `--bkpad` (filas, XML, enable/disable, navegación, cleanup) + `--keepbps`
  para capturas. Tests 16/16. Docs + README actualizados.

## 2026-09-23 (d) — M15b: build unificada en main/build + --old-gui + fix popup GTK

- Un solo proyecto y una sola carpeta: las DLLs Avalonia compilan directo a
  `main/build/` junto a todo el runtime GTK (net10run stageado ahí; conflicto
  de MonoRoslynCompat/Mono.Addins viejos resuelto stageando el runtime), y
  entra en Main.sln con mapeos de las 8 configs; la solución valida en verde.
- `--old-gui` SOLO en AvaloniaShell: reenvía a la UI GTK legacy de la misma
  carpeta con MONODEVELOP_LEGACY_UI=1 (solo el relay lo pone); el GTK directo
  se rechaza con exit 2. Verificado en X11 (ventana NORMAL titulada, sin
  popup).
- 4 pads: edición + Solution + Properties + un pad inferior con TODOS los
  tabs de debug (Call Stack/Locals/Watch/Threads/Bookmarks/Breakpoints);
  DebugPads (inferior-derecha) eliminado — y con ello el tab breakpoints
  duplicado que dejaba el pad sin filas.
- Fix: el popup fatal "No se pudo iniciar MonoDevelop (Remoting channels…)",
  causado por AutoTestService.Start con EnableAutomatedTesting=True sobre el
  stub de remoting, ahora degrada a aviso en consola y el IDE arranca.
- Tests 16/16. Reconstrucción GTK verificada con mdrefs45+gtksharp-fixed
  (0 errores) y stageo de MonoDevelop.Core/Ide a net10run.

## 2026-09-24 (a) — M16: Locals/Watch runtime (netcoredbg DAP) + Attach to Process + Run con debug

- netcoredbg como submodule `main/external/netcoredbg` (fork de Samsung,
  regla: nunca DLLs binarios externos); compilado desde fuente con cmake+clang
  (generador Makefiles). Smoke DAP: launch → bp línea 10 → Locals
  (answer=42). El adaptador exige `source.path` en setBreakpoints.
- `DebugSessionService` (DAP stdio): initialize/launch/setBreakpoints/
  configurationDone, `LastStop` bufferado para QA determinista (la suscripción
  tardía perdía el stop por carrera con el launch).
- Pads Locals/Watch llenan con valores reales del proceso detenido; línea de
  ejecución resaltada en el editor (SetExecutionLine). QA `--locals` verde:
  stopped@10 → highlight=True → values=answer=42,greeting=null →
  pad-realized=2 → cleanup.
- Attach to Process REHECHO como tab del pad inferior
  (`AttachToProcessPanel`, UserControl): /proc real (603 procesos, skip
  kernels/self), filtro + Refresh + Attach. NUNCA ventanas con borde de OS —
  chrome Avalonia. QA `--attachdlg` verde (pad-realized=1,
  window-chrome=Avalonia).
- Run con debug respeta breakpoints persistidos: para en la línea y el IDE la
  resalta (mismo flujo del DebuggingService legacy).
- Staging automático del runtime GTK tras Main.sln (`after.Main.sln.targets`
  → StageUnifiedRuntime); TipsOfTheDay.xml agregado a main/data → Welcome
  Page del GTK arranca en vivo (logo, recientes, New/Open).
- Tests 16/16.

## 2026-09-24 (b) — M16b: Threads/Call Stack reales, attach DAP, Watch y breakpoints avanzados

- Fix clave del attach: el handler `launch` de netcoredbg ignora
  `mode=attach`; el attach real exige el comando DAP `attach` con
  `processId`. Verificado con proceso .NET de larga vida: attach → pause
  (PID como threadId, con reintentos) → `threads=3` → detach y el proceso
  sobrevive (como DetachFromProcess legacy). QA `--attachreal`.
- Pads Threads y Call Stack con datos reales de la sesión (threads con
  `(stopped)`, frames navegables con doble clic). QA `--locals` ampliado
  (`threads=1 frames=1`, `evaluate(answer)=42`).
- Watch con evaluate DAP real: add/remove (menú del pad + InputDialog
  Avalonia), reevaluación en cada stop. QA `--watch`: `answer = 42`,
  `answer + 1 = 43`, remove → rows=1.
  `MD_QA_HOLD` mantiene pads en pantalla para capturas.
- Breakpoints condicionales + hit count + tracepoints: persisten en
  .userprefs (`condition`/`hitcount`/`tracepoint`, formato Mono.Debugging),
  menú Condition…/Hit Count…/Tracepoint…, filas `when … (hit N) print: …`,
  y viajan al adaptador DAP. QA `--condbp` verde de punta a punta.
- Tests 16/16.

## 2026-09-25 (a) — M16c: hover eval, stepping, árbol de variables, Immediate

- Hover eval: en pausa, el tooltip del editor muestra `word = <valor DAP>`
  (evaluate en el frame actual); sin pausa cae a la descripción Roslyn.
- Stepping: Step Over/Into/Out (DAP next/stepIn/stepOut) con botones en la
  toolbar (iconos legacy md-step-*-debug), menú Run completo con atajos
  F10/F11/Shift+F5/F5 y dispatcher. QA `--step`: 13 → Step Over → 14 con
  highlight movido y pads refrescados.
- Locals/Watch como árboles expandibles: hijos lazy vía variablesReference.
  QA `--tree`: roots=3, expandir List → 13 hijos (`_items = {int[4]}`).
- Immediate pad: expresiones evaluadas en el frame, resultado en el Output
  (`[immediate] answer + 1 = 43`, `greeting = "hello"`); sin sesión, mensaje
  honesto. QA `--imm` verde.
- Hallazgo documentado: un bp sobre una asignación para ANTES de ejecutarla
  (locals 0/null); los QAs usan la línea de Console.WriteLine y esperan
  CurrentFrameId antes de evaluar (sin frame → scope estático).
- Tests 16/16.

## 2026-09-25 (b) — M16d: gutter bp, data tip, cambio de frame, completado del Immediate, persistencia de debug

- Breakpoint con clic en el gutter: la franja de iconos (últimos 18px del
  gutter) hace toggle del bp de la línea, como el left margin del legacy;
  `ToggleBreakpointAtGutter` expone la misma ruta. QA `--gutterbp` verde
  (toggle on/off + persistencia + burbuja).
- Data tip inline: al pausar, burbuja verde en la línea parada con el primer
  identificador evaluable (`greeting = "hello"`); limpia en Continue/Step/
  Stop. Hallazgo: la primera palabra de la línea puede ser un tipo (Console)
  que no evalúa — se prueban hasta 5 identificadores en orden.
- Call Stack con cambio de frame: seleccionar un frame recarga los Locals con
  los scopes de ESE frameId (`GetLocalsForFrameAsync`), como el StackFrame
  legacy. Fixture ampliado (`Main` → `Double(list.Count)`) para tener 2
  frames gestionados. QA `--frame`: `Double() | Main()`, selección del 2º →
  `locals of Main() — 3 rows`.
- Autocompletado de miembros en el Immediate: `expr.` evalúa el prefijo por
  DAP y lista sus miembros en un popup (Tab/Enter/doble clic confirman, Esc
  oculta). Fix del handler DoubleTapped acumulativo del popup (rebind -=/+=).
  QA `--immcompl`: 13 miembros de List<int>, commit `list.Count`,
  `[immediate] list.Count = 3`.
- Persistencia de la sesión de debug: breakpoints (ya existía) + watches +
  config activa en `<sln>.userprefs`. Nuevo `WatchService` con la clave
  legacy `MonoDevelop.Ide.DebuggingService.PinnedWatches` (solo
  `expression`; el legacy también serializa la ubicación del pin, que el pad
  del shell no usa). Carga al abrir solución, persiste en add/remove y al
  cerrar el workspace. QA `--persistqa`: cerrar/reabrir → watches y bp
  restaurados, config intacta.
- Nota de entorno: builds que fallan con "pdb is being used by another
  process" (contienda entre TFMs del mismo build) se resuelven con
  `dotnet build-server shutdown` y `-m:1`.
- Tests 16/16.

## 2026-09-25 (c) — M25b: fix del hook `--searchpopup` y verificación visual del ✕ de Properties

- **Fix `:t Program` stale en `--searchpopup`**: la causa NO era debounce ni
  retrigger del handler (diagnóstico que M25 había dejado escrito). El commit
  `8d3c607821` fusionó el comentario con la llamada, dejando
  `OnToolbarSearchTextChanged (":t Program")` y su `Output` DENTRO del
  comentario `// first hit opens its file and jumps to the declaration line.`
  (verificado con `cat -A` y `git log -L`). Por eso el `Output` de `:t Program`
  mostraba el estado de `:s TODO` y la activación usaba los resultados de
  `:t Double` (0 hits). Fix: separar comentario y llamada + re-indentar el
  bloque. QA `--searchpopup` verde: `:t Program` → `results=1 first=Program
  (class) | Program.cs : 8`, `:t Main` → `Main (method) | Program.cs : 10 — in
  Program`, `:t Double` → 0, `activated → tab selected: Program.cs`.
- **Verificación visual del ✕ del pad Properties** (pendiente de M24):
  `PadHost.PadTab` ganó `internal Button? CloseButton` y `LogCollapseChrome()`
  emite `[padclose] '<host>' tab=<id> visible=... bounds=... screen=(x,y)`.
  Hook nuevo `--padclose` (registrado en `QaDialogArg`). Rect medido:
  `screen=(1420,242) size=16x16 content='✕' tip='Close pad'`. El glifo se
  renderiza (crop del rect: dominante `(0,120,215)` del botón + 48 px de
  `(15,75,122)` del glifo). Click XTEST real sobre el centro →
  `properties tab visible=False selected=properties visibleTabs=[]` y
  `View > Pads > Properties checked=False`; el pad desaparece del lado derecho.
  **Re-validado desde un arranque limpio**: el ✕ funciona, no hay bug de hit-test
  . El intento intermedio que falló fue porque el pad derecho ya estaba **colapsado**
  por un click de control sobre el chevron "Collapse pad": con el host colapsado el ✕
  no está en el árbol visual y no es hittable. El QA del ✕ debe correr sobre un
  arranque limpio.
- **Listener de captura en `--padclose`**: se añadió un `PointerPressed` con
  `handledEventsToo: true` que imprime la cadena de controles que recibe el
  press (`[padclose] press at (x,y) → ...`), para distinguir "el click no llega
  a la ventana" de "el click llega pero el handler no corre". **Hallazgo**: un
  `PointerPressed +=` normal nunca ve el click sobre el ✕ porque `Button` marca
  el evento como handled; hace falta `AddHandler (PointerPressedEvent, ...,
  RoutingStrategies.Tunnel | Bubble, handledEventsToo: true)`. Con eso el press
  se reporta como `AccessText < ContentPresenter < Button < StackPanel <
  ContentPresenter < ToggleButton < ... < PadHost#RightPads`: el ✕ **sí** es el
  target del hit-test en su centro (window-relative 1108,93) y el `ToggleButton`
  padre no lo intercepta.
- **Hallazgos de instrumentación** (para futuras QAs):
  - `PointToScreen` lanza `ArgumentException: Visual does not belong to a
    visual tree` si el visual no está attached → todo el dump va detrás de
    `IsAttachedToVisualTree()`, incluido el loop por tab (las tabs ocultas
    `classes`/`help` tienen su ✕ fuera del árbol y cortaban el dump).
  - `InputHitTest` devuelve `null` para el ✕ aunque el click real funcione
    (confirma la nota de `PadHost.cs:111`). Autoritativo: `new Rect
    (control.Bounds.Size).Contains (local)` — `Visual.Bounds` está en
    coordenadas del PADRE, no propias.
- **Quirk de entorno (Wayland/XWayland)**: `xtest.fake_input (MotionNotify)`
  NO mueve el puntero real; hay que usar `root.warp_pointer (x, y)` + `sync` +
  espera, y recién entonces `ButtonPress/ButtonRelease`. Confirmar con
  `root.query_pointer ()` (el `child` debe ser el window id del shell). El
  **foco** también importa: `_NET_ACTIVE_WINDOW` puede cambiar a otra ventana
  entre comandos; reactivar con `wmctrl -i -a 0x0260000f` antes de cada click.
- **Artefactos de QA de agentes**: `~/opencode/` (no `/tmp/`, que es tmpfs).
  Scripts nuevos: `qa_searchpopup.sh`, `qa_padclose.sh`.
- **M26** (~~propuesto, pendiente de aprobación~~ → **ejecutado**): paridad de pads placeholder — portados `documentoutline`, `classes` y `codeissues` (reutilizan el índice de símbolos y el parser de diagnósticos ya existentes); quedan como cáscara `help`, `toolbox` y `unittests` (frente A del inventario de trabajo abierto, `interfaz-plan.md`). Detalle en `docs/interfaz-plan.md` § M26.
- Build del shell: 0 errores. Tests del editor: 16/16.

## 2026-10-02 — M26: paridad de pads `documentoutline`, `classes`, `codeissues`

El usuario aprobó la **Opción 1 acotada** de la propuesta M26 (tres pads) y se
cerró la implementación + QA que había quedado a medias en el working tree.

**Implementación (código ya presente, cerrado y verificado):**
- `MonoDevelop.Ide/Services/SymbolIndexService.cs` (nuevo): escáner de una pasada
  sobre C# (tipos + miembros con línea/INDENT); alimenta `ScanSymbols`
  (breadcrumb/GoToType/`:t`), `BuildOutline` (Document Outline) y
  `BuildClassTree` (Classes: project ▸ namespace ▸ tipo ▸ miembro, orden legacy
  `ClassNodeBuilder`). Cache por timestamp en `ScanFile`.
- `MonoDevelop.Ide/Services/CodeIssueService.cs` (nuevo): parsea las líneas
  MSBuild `file(line,col): severity CODE: message`, agrupa por severidad en el
  orden legacy (error → warning → info → hidden) y forma fila/resumen.
- `MainWindow.axaml.cs`: pads `documentoutline` (RightPads), `classes`
  (LeftPads) y `codeissues` (BottomPads); árboles con doble clic que abre
  archivo + salta a línea; auto-ocultos por defecto (paridad `Pads.addin.xml`).
  El Document Outline sigue al documento activo vía el timer del breadcrumb.
- Hooks QA: `--outline[=<path>]`, `--classes`, `--codeissues` y
  `--dblclick[=<path>]`.

**Hueco encontrado y corregido:** el hook `--dblclick` estaba implementado en
`MainWindow` pero **no registrado en `Program.QaDialogArg`** → nunca disparaba
(el log previo `m26_dblclick.log` en realidad venía de un arranque normal con
`--sln`, no del hook). Registrado `--dblclick` y `--dblclick=` en
`QaDialogArg`.

**QA (fixture `~/opencode/m26fix`: `Widget` + `IThing`; logs
`~/opencode/m26_qa_{outline,classes,codeissues,dblclick}.log`, 0 FATAL):**
- `--outline` → 10 nodos: `class Widget @5`, fields `counter@7`/`Max@8`,
  `event Changed@9`, `property Value@10`/`Name@11`, `method Run@13`/`Double@19`,
  `interface IThing @22`, `method Go@24` (el escáner corregido ahora sí detecta
  el método de interfaz `void Go ();`, que antes se perdía).
- `--classes` → `project m26fix ▸ namespace M26Fix ▸ interface IThing
  (Program.cs:22)` (con `method Go`) + `class Widget (Program.cs:5)` con miembros
  en orden legacy.
- `--codeissues` → `1 error(s), 2 warning(s), 1 info(s)` / `rows=4` (CS0103,
  CS0219, CS0168, CS8019).
- `--dblclick` → activa las tres rutas y verifica caret: `interface IThing @22`
  → línea 22; `class Widget @5` → línea 5; `error CS0103 @12` → línea 12.
- Estados vacíos cubiertos (`No solution loaded`, `(empty state)`).

**Hallazgos del Tester QA Senior (§18.5) corregidos antes del cierre** (3 rondas
hasta aprobar limpio):
1. **(bloqueante H1)** el escáner leía `if (true)` / `while` / `foreach` / `get` /
   `set` indentados como miembros: la clase de "tipo" del regex incluía `\s`, así
   que la indentación hacía de tipo. Fix: los regex corren sobre la línea
   **sin indentación** + guard de palabras clave de sentencia.
2. **(mayor H2)** propiedad estilo Allman (`public int Prop`, `{` en la línea
   siguiente) no se detectaba → `allmanPropertyRegex`.
3. **(menor H3)** campo sin modificador (`int Field;`) → el campo admite cero
   modificadores.
4. **(menor H4)** cuerpos de comentarios de bloque y strings verbatim →
   `UpdateLexState` rastrea `/* */` y `@"..."` (`""` escapado).
5. **(mayor N1, regresión del rediseño)** `throw new X ();` / `yield return
   X ();` caían como métodos porque el guard comparaba el tipo entero →
   guard **token-aware** (cualquier token keyword rechaza).
6. **(mayor N2)** `record` / `record class` / `record struct` no se reconocían
   (caían como método) → `typeDeclRegex` con `record`, kind `record` en el
   Classes pad y en el orden legacy.
7. **(menor N3)** indexers (`public int this [int i] => i;`) → `indexerDeclRegex`
   → `property this[]`.
8. **(menor N4)** raw strings `"""..."""` (C# 11) no se rastreaban → cuarto
   estado en `UpdateLexState`.
   Regresión cubierta con 8 tests nuevos (`SymbolIndexTests`) y verificada por
   GUI sobre el fixture adversarial ampliado (`~/opencode/m26_adv_fixture/Adv.cs`
   con `record Point`, `throw new`, `yield return`, indexer): el outline da
   exactamente `record Point @6`, `class Adv @8`, `property Prop @10`, `method
   Run @16`, `method It @32`, `property this[] @37` — 0 falsos positivos.
   Veredicto final del Tester QA Senior (ronda 3): **M26 PASA**; arnés
   adversarial `~/opencode/qa_m26_adv/` → `ALL CHECKS PASSED`.
   Build del shell **0 errores**; tests **53/53** (`AvaloniaShell.Editor.Tests`).

**Cierre**: documentación M26 en `interfaz-plan.md` (+ README del shell) y
commit+push del bloque.

## 2026-10-02 (b) — M27: Preferences, paridad con el árbol legacy + "Errors and Warnings"

Segunda pasada sobre la ventana Preferences. Se analizó la Preferences GTK
legacy (`OptionsDialog` + `ExtensionModel/GlobalOptionsDialog.addin.xml` + las
extensiones de add-ins) para reconstruir árbol, header y componentes.

**Cambios:**
- Navegación **jerárquica real** (`TreeView`, antes `ListBox` plano): categorías
  → secciones → subpaneles, fiel a `GlobalOptionsDialog.addin.xml`. Modelo en
  `PreferencesDialog.axaml.cs` (`PrefsNode`/`BuildModel`).
- **Header de panel** (icono 28px + título) como `OptionsDialogHeader` legacy;
  títulos duplicados eliminados.
- Iconos corregidos: `Feedback`/`MonoDevelop Maintenance` ya usan
  `md-prefs-feedback`/`md-prefs-maintenance`; cada sección mapea su `md-prefs-*`
  (0 `(missing)`).
- **Panel nuevo `Build → Errors and Warnings`** (legacy `BuildMessagePanel`):
  JumpToFirst / ShowErrorPadAfterBuild / ShowMessageBubbles con las claves
  `MonoDevelop.Ide.NewJumpToFirstErrorOrWarning`, `NewShowErrorPadAfterBuild`,
  `NewShowMessageBubbles` (se corrigió el off-by-one del legacy, que casteaba el
  índice del combo al enum).
- Secciones/subpaneles añadidos: `IntelliSense → Behavior/Appearance`,
  `Source Analysis → C#`, `XML Schemas`, `F# Settings`; el selector de idioma se
  movió **dentro de Visual Style** (como el legacy), no como sección propia.
- **QA**: hook `--prefs-tree` (dump `[prefs-tree]`) + telemetría `[prefs-panel]`;
  `--prefs=<id>` abre cualquier panel (incluidos anidados).

**Deuda**: los paneles de add-in (Text Editor, Source Code, Version Control,
NuGet, .NET Runtimes, SDK Locations, Debugger, GTK# Designer, Performance, F#)
figuran en el árbol con label/icono correctos pero como placeholder (su lógica
usa el stack de add-in que el shell no referencia). `.NET Runtimes` (legacy bajo
`RUNTIME_SELECTOR optIn`) se muestra siempre (net10-only) — desviación
documentada.

**QA (§18.5, 2 rondas)**: ronda 1 → 4 hallazgos menores corregidos; ronda 2 →
**PASA limpio**. Build 0 errores; tests **53/53**. Logs
`~/opencode/prefs_qa_{tree,buildmessages}.log`, captura `prefs_tree.png`.

**Cierre**: documentación M27 en `interfaz-plan.md`/`migration-status-report.md`
(+ README del shell) y commit+push.

## 2026-10-02 (c) — M28: fix doble barra de título + paneles Text Editor (add-in SourceEditor2)

**Fix de la doble barra de título**: `DialogWindow.Apply` buscaba la fila XAML
`dialogchrome` en `window` **después** de `window.Content = null`, no la
encontraba y añadía una barra propia → dos barras. Fix: recorrer
`LogicalExtensions.GetLogicalDescendants(oldContent)`. Afecta a todos los
diálogos con `DialogWindow.Apply`. Verificado por píxeles (una sola banda) y
`_NET_FRAME_EXTENTS` ausente.

**Integración del add-in SourceEditor2**: sus paneles son widgets GTK, no
hosteables en Avalonia → se **portan** leyendo/escribiendo las mismas claves de
`MonoDevelopProperties.xml` que `DefaultSourceEditorOptions`/`EditorPreferences`:
- `general` (GeneralOptionsPanel): LineEndingConversion/ShowFoldMargin/
  DefaultRegionsFolding/DefaultCommentFolding/WordWrapStyle.
- `markers` (MarkerPanel): ShowLineNumberMargin/ShowRuler/HighlightCaretLine/
  HighlightMatchingBracket/EnableHighlightUsages/ShowBlockStructure/
  EnableQuickDiff/ShowProcedureLineSeparators/EnableAnimations/ShowWhitespaces/
  IncludeWhitespaces.
- `behavior` (BehaviorPanel): IndentStyle/WordNavigationStyle/
  AutoInsertMatchingBracket/SmartSemicolonPlacement/TabIsReindent/SmartBackspace/
  AutoFormatDocumentOnSave/AutoSetPatternCasing/EnableSelectionWrappingKeys/
  GenerateFormattingUndoStep.
- `intellisense` (CompletionOptionsPanel): EnableAutoCodeCompletion/
  AddImportedItemsToCompletionList/IncludeKeywordsInCompletionList/
  IncludeCodeSnippetsInCompletionList/ForceCompletionSuggestionMode.
`[Flags]` se serializa como el `EnumConverter` legacy (`All`/`None`/lista) y se
preservan miembros válidos fuera del conjunto gestionado (p.ej.
`WordWrapStyles.AutoIndent`).

**QA (§18.5, 3 rondas)**: barra de título aprobada; paneles aprobados con H1/H2
(`IncludeWhitespaces` no reconocía `"All"`/default) y H3 (se descartaba
`AutoIndent`), corregidos y re-verificados con probe real de `EnumConverter`.
Build 0 errores; tests **53/53**; 4 paneles `placeholder=False`, 0 FATAL.

**Cierre**: documentación M28 en `interfaz-plan.md`/`migration-status-report.md`
(+ README) y commit+push. Deuda: el resto de paneles de add-in siguen como
placeholder con label/icono, a portar por módulos.

## 2026-10-02 (d) — M29: Text Editor — Color Theme + Code Snippets + Language Bundles

Los tres paneles GTK dependen de `SyntaxHighlightingService`/`CodeTemplateService`
(pesados); portados conciliando las mismas carpetas/claves de `UserDataRoot`
(`~/.local/share/MonoDevelop/9.0`):
- `colortheme` (HighlightingPanel): built-ins + `ColorThemes/`; Add/Remove/Open
  folder; persiste `ColorScheme` (light) / `ColorScheme-Dark` (dark).
- `codesnippets` (CodeTemplatePane): lista `Snippets/*.template.xml`, preview,
  Remove (Add/Edit pendiente del EditTemplateDialog).
- `languagebundles` (TextMateBundleOptionsPanelWidget): lista `LanguageBundles/`,
  Add/Remove; built-ins los provee el IDE.

**QA (§18.5, 2 rondas)**: ronda 1 → H1 bloqueante (`FontFamily="monospace"` en
`SnippetPreview` abortaba con fontconfig mínimo) + H2/H3 del harness; corregidos
(fuente por defecto, script QA case-insensitive/sin truncar) → ronda 2 **APTO**
(6/6 adversariales sin crash). Build 0 errores; tests **53/53**; 3 paneles
`placeholder=False`.

**Cierre**: documentación M29 en `interfaz-plan.md`/`migration-status-report.md`
(+ README) y commit+push.

## 2026-10-02 (e) — M30: Source Code — .NET Naming Policies + Standard Header

Los paneles GTK son `PolicyOptionsPanel<T>`; el shell escribe/lee el policy set
global en `Policies/UserDefault.mdpolicy.xml` con los elementos/nombres de
`[DataItem]`/`[ItemProperty]` (`DotNetNamingPolicy`, `StandardHeader`),
compatible con `PolicySet.LoadFromXml`/`PolicyService.DiffDeserializeXml`:
- `naming`: asociación namespaces↔carpetas, raíz, flat/hierárquico, y
  `ResourceNamePolicy` de 3 estados (FileFormatDefault/FileName/MSBuild);
  default efectivo `PrefixedHierarchical`.
- `standardheader`: texto + incluir en archivos nuevos.
- Escritura en una pasada (conserva otras políticas, normaliza raíz `<PolicySet>`
  directa sin perder hermanos, copia `.previous`).
- `codeformatting` queda placeholder (panel grande).
Hook QA `--prefs-sourcewrite` (dry-run con backup/restore).

**QA (§18.5, 3 rondas)**: formato APTO; corregidos default de naming, pérdida de
`FileName`, `.previous` intermedio y raíz directa. Build 0 errores; tests
**53/53**; `naming`/`standardheader` `placeholder=False`.

**Cierre**: documentación M30 en `interfaz-plan.md`/`migration-status-report.md`
(+ README) y commit+push.

## 2026-10-03 — Key Bindings (Fase 1a): integración de MonoDevelop.Core

Directiva del usuario: reutilizar el backend existente de la UI GTK (no crear
uno nuevo); `MonoDevelop.Core` y `MonoDevelop.Ide` son parte de la shell.

**Análisis del backend de comandos/atajos** (mapa de acoplamiento GTK):
- **GTK-free (reutilizables tal cual):** `KeyBindingSet.cs`, `KeyBindingScheme.cs`,
  `Command.cs`, `ActionCommand.cs`, `KeyBindingService.cs`, `SchemeExtensionNode.cs`.
- **Acoplados a GTK:** `CommandManager.cs` (13 Gdk/54 Gtk) y `KeyBindingManager.cs`
  (152 Gdk); el panel GTK `KeyBindingsPanel.cs` es el widget a reemplazar.

**Fase 1a (hecha):** el shell referencia `MonoDevelop.Core` (ProjectReference);
compila a **0 errores**, tests 53/53, QA 0 FATAL. `MonoDevelop.Core.dll` net10 se
copia a `main/build/` (misma carpeta unificada).

**Bloqueo detectado (Fase 1b):** integrar `MonoDevelop.Ide` no es directo:
- Compilar las fuentes del backend → 378 errores (Gdk/Gtk/CommandInfo/handlers).
- ProjectReference a `MonoDevelop.Ide.csproj` → arrastra `Mono.Addins.Gui` (GTK) y
  da 272 errores (sin refs GTK en este contexto).
Opciones a decidir: (a) referenciar el `MonoDevelop.Ide.dll` **ya compilado** en
`main/build/` (evita reconstruir y sus deps GTK), (b) añadir las refs GTK al
build del shell, o (c) guardas `#if AVALONIA_SHELL` en `CommandManager`/
`KeyBindingManager` (invasivo, 378 errores de superficie).

**Cierre**: commit de la Fase 1a (referencia a Core). Fase 1b ~~pendiente de la decisión (a/b/c)~~ → **ejecutada con la opción (c)** (guardas `#if AVALONIA_SHELL`, sin GTK) — ver sección siguiente "2026-10-03 (b) — Key Bindings (Fase 1b): plan por pasos (opción c, sin GTK)".

## 2026-10-03 (b) — Key Bindings (Fase 1b): plan por pasos (opción c, sin GTK)

Decisión: opción (c) + **sin GTK en la shell**; donde haya GTK se añade un
equivalente no-GTK **en el mismo `.cs`** de `MonoDevelop.Ide`. Símbolo
`AVALONIA_SHELL` añadido al csproj del shell (guarda `#if !AVALONIA_SHELL`).

**Hallazgo del Paso 1**: el modelo está acoplado a GTK en la raíz:
- `KeyboardShortcut` se define en `MonoDevelop.Components/GtkWorkarounds.cs:1409`
  y su ctor toma `Gdk.Key`/`Gdk.ModifierType`.
- `Command` (Command.cs) usa `KeyBinding` (definido en `KeyBindingManager.cs:909`)
  y `CommandManager.ToCommandId`; `KeyBinding` usa `KeyboardShortcut`.
Por eso el orden de pasos es:

1. `GtkWorkarounds.cs`: añadir equivalente no-GTK de `KeyboardShortcut` (guarda) —
   es la raíz del modelo.
2. `KeyBindingManager.cs`: guardar los métodos GTK (`AccelLabelFromKey(Gdk.EventKey)`,
   `AccelToKey`, `BindingToKeys`, cctor con `Gdk.ModifierType`); dejar el modelo
   portable (`KeyBinding`, `Binding`, `BindingToDisplayLabel`, `FixChordSeparators`).
3. `CommandManager.cs`: guardar GTK/command-service; dejar `ToCommandId`.
4. Compilar `Command`/`ActionCommand`/`KeyBindingSet`/`KeyBindingScheme`/
   `KeyBindingService`/`SchemeExtensionNode`.
5. Panel Avalonia fiel + cableado de HotKeys (`InputGesture` desde el binding).

Estado: árbol **verde** (define añadido, sin archivos del backend aún).

## 2026-10-03 (c) — Key Bindings COMPLETO (backend reuse sin GTK + panel fiel)

Cadena ejecutada por pasos (todos verdes y commiteados), reutilizando el backend
real de `MonoDevelop.Ide` **sin añadir GTK** (guardas `#if !AVALONIA_SHELL` +
equivalente no-GTK **en el mismo `.cs`**):
- **Paso 1** `GtkWorkarounds.cs`: `KeyboardShortcut` sin GTK (Key/Modifier int).
- **Paso 2** `KeyBindingManager.cs`/`KeyBinding`: equivalentes no-GTK
  (`Binding`/`FixChordSeparators`/`BindingToDisplayLabel` + modelo).
- **Paso 3** `CommandManager.cs` (solo `ToCommandId`) + `Command`/`ActionCommand`/
  `ActionType`/`CommandHandler`/`CommandInfo(Set)`/`CommandArrayInfo`.
- **Paso 4** `KeyBindingSet`/`KeyBindingScheme`/`KeyBindingService`/
  `SchemeExtensionNode` (backend completo compilando en la shell).
- **Paso 5a** HotKeys: `MenuBuilder` parsea `entry.Shortcut`→`InputGesture`
  (antes nunca se aplicaba: los atajos no existían).
- **Paso 5b** panel fiel (`PreferencesDialog.axaml(.cs)`): combo de esquemas +
  Custom, separador, búsqueda, caja de aviso + View Conflicts, árbol
  Command/Key Binding/Description con chips y color de duplicado, mensaje inline,
  Edit Binding + Apply/Add-Delete. Usa `Command`/`KeyBindingSet` (+
  `CheckKeyBindingConflicts`)/`KeyBindingManager`; esquemas desde `options/*.xml`
  con `KeyBindingSet.LoadScheme`; `MainWindow.MenuCommandCatalog` agrupa por
  categoría. QA: `commands=178 schemes=5 conflicts=2`, 0 FATAL.

**Garantía GTK**: el Tester QA Senior reconstruyó cada archivo compartido sin
`AVALONIA_SHELL` y lo comparó con el original: los cuerpos GTK son **idénticos**
(solo diferencias cosméticas de `using`/líneas en blanco), balance de guardas
correcto y sin borrados GTK. **La UI GTK no se rompe.**

**QA (§18.5)**: APROBADO. Build 0 errores, 53/53 tests, 4 casos adversariales sin
crash. Hallazgos 1–3 cosméticos/nits (cerrados/documentados), 4 limitación de
entorno preexistente (MDBuildTasks net472).


## 2026-10-03 (d) — Visual Style: opción "System" (sigue el tema del SO)

Añadido el radio **System** (junto a Dark/Light) al panel Visual Style.
Persiste en la clave legacy `MonoDevelop.Ide.UserInterfaceTheme` (`""`=System/
Default — default en Linux, `"Dark"`, `"Light"`), igual que la UI GTK; System →
`ThemeVariant.Default`. Se aplica al arranque (`App`) y en vivo al cambiar el
radio; `StoreThemePanel` en OK.

**QA (§18.5, 2 rondas)**: H1 (ALTA) — `MainWindow.OnOpened` fijaba
`RequestedThemeVariant` con el valor concreto, anulando `Default`/System (la UI
arrancaba clara en SO oscuro); corregido (no pin; refresco vía
`ActualThemeVariantChanged`). Verificado: con System y SO `prefer-dark` arranca
oscuro y `variant=Default`; Dark/Light OK; round-trip con click real OK; H2
(System persiste como clave ausente) aceptado como equivalente al default legacy.
Build 0 errores, 53/53 tests, 0 FATAL. **PASA LIMPIO.**

## 2026-10-03 (e) — Preferences: Version Control → General

Panel portado del add-in (`VersionControlGeneralOptionsPanel`): checkbox
"Disable Version Control globally" que persiste en
`~/.config/MonoDevelop/9.0/VersionControl.config` (`VersionControlConfiguration`
vía `XmlDataSerializer`). **QA (§18.5, 2 rondas)**: H1 (ALTO) — el formato
canónico de la GTK es el **atributo** raíz `<VersionControlConfiguration
Disabled="True">`, no un elemento; la shell solo leía el elemento (interop
GTK→shell rota). Corregido: lee atributo primero (fallback elemento) y escribe el
atributo canónico eliminando el hijo; repositorios preservados. **PASA LIMPIO**.
Build 0 errores, 53/53 tests.

## 2026-10-03 (f) — Preferences: Version Control → Commit Message Style

Panel portado del add-in (`VersionControlPolicyPanel`/`CommitMessageStylePanelWidget`):
header + 8 checkboxes que mapean a los campos de `CommitMessageStyle`; preview
aproximado; persiste en el policy set global (`<VersionControlPolicy><CommitMessageStyle>`).
**QA (§18.5, 2 rondas)**: H1 (media-alta) — `XDocument.Load` sin
`LoadOptions.PreserveWhitespace` descartaba `<Indent>\t</Indent>`; corregido en
`LoadGlobalPolicy`/`StoreGlobalPolicies`. Formato **compatible** con el policy
framework GTK (verificado con probe del serializador real). **PASA LIMPIO**.
Build 0 errores, 53/53 tests.

## 2026-10-03 (g) — Preferences: Version Control → ChangeLog Integration

Refactor: `CmStyleEditor` compartido (header + 8 toggles + preview) entre Commit
Message Style y ChangeLog. ChangeLogPanel: radios UpdateMode + checkboxes
VcsIntegration + editor CM incrustado; persiste `<ChangeLogPolicy>`.
**QA (§18.5, 2 rondas)**: H1 (bloqueante) — el `[ItemProperty]` se nombra por la
**propiedad**, así `ChangeLogPolicy.MessageStyle` es `<MessageStyle>` (no
`<CommitMessageStyle>`); parametrizado `BuildElement(name)`. H2 sensibilidad
(Integrate/Require según UpdateMode) y H3 telemetría corregidos. **PASA LIMPIO**.
Build 0 errores, 53/53 tests.

## 2026-10-03 (h) — NuGet Sources + Debugger

- **NuGet → Sources** (vía A, backend real): referencia `NuGet.Configuration` 5.4.0
  en el shell y uso de `PackageSourceProvider.LoadPackageSources/SavePackageSources`
  (misma API que `RegisteredPackageSourcesViewModel`). Lista + Add/Remove/
  Move Up/Down; persiste en `NuGet.Config`.
- **Debugger**: portado `DebuggerOptionsPanel` (Scope/Evaluation/Advanced). Los
  `DebuggerSessionOptions` de `DebuggingService` son claves planas de
  `PropertyService` (`MonoDevelop.Debugger.DebuggingService.*`), así que se leen/
  escriben directamente. Incluye el combo `AutomaticSourceDownload` (Ask/Always/
  Never), el timeout de evaluación y la sensibilidad de "AllowToString".
QA: build 0 errores, 53/53 tests, `placeholder=False`, 0 FATAL en ambos.

## 2026-10-03 (i) — Projects → SDK Locations → .NET Core

Portado `DotNetCoreSdkLocationPanel`: ruta del runtime (.NET Core) con Browse y
listado de runtimes instalados. Persiste en la clave legacy
`DotNetCoreRuntimeFileName`; `.NET Core` queda anidada bajo `SDK Locations`
como en el addin.xml. QA: 0 errores, 53/53, `path=~/.dotnet/dotnet`,
placeholder=False, 0 FATAL.

## 2026-10-03 (j) — Projects → .NET Runtimes

Portado `MonoRuntimePanel`: lista los runtimes registrados por el servicio de
runtimes de Core (`Runtime.SystemAssemblyService.GetTargetRuntimes`), marca
(Default)/(running) y persiste el default en la clave legacy
`MonoDevelop.Ide.DefaultTargetRuntime`. Como la shell no arranca el motor de
add-ins que registra las fábricas de runtime, la consulta va en try/catch y cae al
.NET detectado en disco (`~/.dotnet/dotnet`), que es el único runtime del host.
QA: 0 errores, 53/53, `count=1 selected=__current`, 0 FATAL.

## 2026-10-03 (k) — UI Designer (renombrado) + Performance Diagnostics

- El nodo **"GTK# Designer"** pasa a **"UI Designer"** (id `uidesigner`), ya que
  la sección no depende de GTK en la shell.
- **Performance Diagnostics → General**: portado `GlobalOptionsPanel`
  (directorio de salida) con la clave legacy
  `PerformanceDiagnosticsAddIn.OutputPath` (default `~/Desktop`) + Browse.
QA: 0 errores, 53/53, ambos paneles `placeholder=False`, 0 FATAL.

## 2026-10-03 (l) — Text Editor: XML, IntelliSense Behavior/Appearance

Sin saltar paneles: **Behavior → XML** (`XmlEditorOptions`, props anidadas bajo
`XmlEditor.AddIn.Options`: AutoCompleteElements/AutoInsertFragment/
ShowSchemaAnnotation), **IntelliSense → Behavior** (mismo contenido que el
panel padre `intellisense` = `CompletionOptionsPanel`) e **IntelliSense →
Appearance** (`CompletionOptionsHideAdvancedMembers`, checkbox "filter by
browsable"). QA: 0 errores, 53/53, los tres `placeholder=False`, 0 FATAL
(`[prefs-xml] complete=False fragments=True schema=False` = defaults legacy).

## 2026-10-03 (m) — Text Editor → Source Analysis

Portado `AnalysisOptionsPanel`: "Enable source analysis of open files"
(`MonoDevelop.AnalysisCore.AnalysisEnabled_V2`, default true) y "Enable text editor
unit test integration" (`Testing.EnableUnitTestEditorIntegration`, default false).
El checkbox "whole solution" depende de una clave Roslyn por lenguaje
(`SolutionCrawlerClosedFileDiagnostic`) y queda visible pero deshabilitado
documentado, a la espera de referenciar `Microsoft.CodeAnalysis.Workspaces`
(ver M-n).

**Bloqueo confirmado de las claves Roslyn por lenguaje**: `Option<T>`/`OptionKey`/
`GetPropertyName()` viven en `Microsoft.CodeAnalysis.Workspaces.dll`, que no está
en el cierre de compilación de la shell; `MonoRoslynCompat` solo aporta
`FeatureOnOffOptions`/`ServiceFeatureOnOffOptions`. Afecta a **Behavior → C#**
(`OnTheFlyFormattingPanel`, 7 opciones Roslyn) y al checkbox "whole solution".
QA: 0 errores, 53/53, `placeholder=False`, `openFiles=True unitTest=False`, 0 FATAL.

## 2026-10-03 (n) — Text Editor → Behavior → C# (on-the-fly formatting)

Portado `OnTheFlyFormattingPanel` (7 checkboxes) con las opciones Roslyn reales
(`FeatureOnOffOptions.AutoFormattingOnTyping/Semicolon/CloseBrace/FormatOnPaste`,
`FormattingOptions.AutoFormattingOnReturn`, `CompletionOptions.
ShowCompletionItemFilters/TriggerOnDeletion`). Para que las claves coincidan con
la GTK se calculan vía `OptionKey.GetPropertyName()` (API **interna** de Roslyn,
solo visible para `MonoDevelop.Ide`), invocada por reflexión: se añadieron las
referencias `MonoRoslynCompat` (FeatureOnOffOptions/CompletionOptions) y
`Microsoft.CodeAnalysis.Workspaces.dll` (Option<T>/OptionKey) al shell.

**Caveat de fidelidad**: en este host la extensión interna no es invocable
(`TargetInvocationException`: depende de `Microsoft.Bcl.AsyncInterfaces`, ausente),
así que la clave cae al nombre legacy `"C#.<OptionId>"`. El panel es funcional y
persiste round-trip, pero **las claves no coinciden** con las Roslyn de la GTK
mientras no se resuelva esa dependencia. QA: 0 errores, 53/53,
`placeholder=False`, 0 FATAL.

## 2026-10-03 (o) — Diagnóstico de la clave Roslyn (csharpformat)

QA integral del día: **APTO** (0 errores, 53/53 tests, 17 paneles con
`placeholder=False` y 0 FATAL, About y tema System verificados, sin regresión GTK).

Diagnóstico del fallback de clave Roslyn: el error real es
`ArgumentException: A language name cannot be specified for this option` — en este
Roslyn 4.8 **todas** estas opciones rechazan el nombre de idioma, así que la clave
correcta es la **agnóstica de lenguaje** (la que produce el fallback de
`RoslynKey` del add-in). `OptionKey` no expone constructores públicos, y localizarlos
por reflexión no fue concluyente en este host, por lo que el panel **mantiene el
fallback legacy** (`"C#.<OptionId>"`), funcional y round-trip, con la divergencia
de claves documentada.

## 2026-10-03 (p) — Source Code → Code Formatting

Portado `CodeFormattingPanel` sobre las propiedades de `TextStylePolicy`
(sin `[DataItem]` → elemento `TextStylePolicy`) del set global: TabsToSpaces
(radios Espacios/Tabs), IndentWidth, TabWidth, FileWidth, RemoveTrailingWhitespace
y NoTabsAfterNonTabs. El árbol de selección de policy-set **por MimeType** del
panel legacy requiere `AddinManager.GetExtensionNodes("/MonoDevelop/ProjectModel/
Gui/MimeTypePolicyPanels")` (motor de add-ins) → queda fuera y documentado.
QA: 0 errores, 53/53, `placeholder=False`, `tabsToSpaces=False indent=4 tab=4
file=120` (defaults), 0 FATAL.

## 2026-10-03 (q) — Alojar Mono.Addins en la shell

Nuevo `MonoDevelop.Ide/Services/AddinEngineHost.cs`: engine **único y perezoso**
(`AddinEngine.Initialize` con las rutas del IDE + `Registry.Update`), fachada
`AddinManager.Initialize(configDir, addinsDir)` y sonda de extension nodes.
Se invoca en `App.OnFrameworkInitializationCompleted` y el `AddinManagerDialog`
deja de crear su propia engine (reutiliza el host compartido).

**Estado**: el motor arranca y el registro se actualiza (avisos/errors de add-ins
GTK incompletos, preexistentes: `MonoDevelop.GtkCore` sin `libstetic*.dll`), pero
la sonda devuelve `MimeTypePolicyPanels=0` y `GlobalOptionsDialog=0`: el catálogo
no registra los extension models de los add-ins (ensamblados GTK que no cargan +
manifiestos no escaneados). Por tanto **Code Formatting** (árbol de policy-set
por MimeType) y **XML Schemas** siguen pendientes: requieren que el registro
resuelva extension nodes, no solo claves.
QA: 0 errores, 53/53, keybindings/style siguen placeholder=False y 0 FATAL.

## 2026-10-03 (r) — Manifiesto de add-ins propio (extension nodes siguen a 0)

El `.addins` del stock (`MonoDevelop.exe.addins`) solo declara
`<Directory include-subdirs="true">./AddIns</Directory>`, por lo que los
**ensamblados raíz** (`MonoDevelop.Ide.dll`, que define
`/MonoDevelop/Ide/GlobalOptionsDialog`) nunca se escanean. `AddinEngineHost`
genera ahora un manifiesto propio en `<configDir>/shelladdins/shell.addins` que
declara esos ensamblados + el árbol `AddIns/**` y lo pasa como `startupDir`.

**Resultado medido**: la sonda sigue en `MimeTypePolicyPanels=0
GlobalOptionsDialog=0`: `MonoDevelop.Ide.dll` **no carga** en el proceso Avalonia
(dependencias GTK), por lo que sus extension models nunca se registran.
Consecuencia: **Code Formatting** (árbol de policy-set por MimeType) y
**XML Schemas** siguen bloqueados; no son resolubles con solo hosting del motor,
requieren que el ensamblado del add-in cargue sin GTK (o desacoplar esos paneles
de la carga del ensamblado). Sin regresión: 53/53 tests, keybindings
placeholder=False, 0 FATAL.

## 2026-10-03 (s) — Nuevo sistema de add-ins para la shell Avalonia (sustituye a Mono.Addins)

Nuevo módulo `main/src/core/MonoDevelop.Avalonia.Addons/` (net10.0, sin GTK),
**independiente de Mono.Addins** (que queda para la UI GTK). Modelado sobre las
extensiones de **Visual Studio** (no VSCode):
- **Manifiesto JSON en disco** con forma `extension.vsixmanifest`
  (`identity`, `assetType`, `installationTarget`, `tags`, `categories`,
  `dependencies`, `entryPoint`, `autoLoad`, `extensions`).
- **Contenedor MEF-like**: `[Export]`, `[Import]`, `[ImportMany]` + `CompositionHost`.
- **Ciclo de vida VS**: `IAvaloniaAddon`/`AvaloniaAddon`, `IPackage`/`Package`,
  `ProvideAutoLoad`, `ProvideOptionPage`.
- **Aislamiento**: `AssemblyLoadContext` por add-in (`AddonLoadContext`).
- **Registro de extension points**: `IAddonExtensionRegistry.GetExtensionNodes(path)`
  + `ExtensionPoints` (GlobalOptionsDialog, MimeTypePolicyPanels, StartupHandlers,
  Pads, Docking) — la misma forma que usaba el código legacy.
- **Host** (`AddonHost`): descubre `*.avaloniaaddon.json` (`MONODEVELOP_AVALONIA_ADDINS`,
  `Addins.Avalonia` junto al binario, o `main/src/addins` en el repo), compone y activa.

**Primer add-in**: `main/src/addins/MonoDevelop.Avalonia.Refactoring/`
(manifiesto JSON)Sized para contribuir al árbol de Preferences
(`/MonoDevelop/Ide/GlobalOptionsDialog/TextEditor → Analysis/Source Analysis`).

Verificado en runtime: `loaded 1/1 add-in(s)`, `MonoDevelop.Refactoring v9.0.0
loaded=True`, `ep /MonoDevelop/Ide/GlobalOptionsDialog/TextEditor nodes=1`.

**QA (§18.5, 3 rondas)**: ronda 1 (sin informe) → no pasó; ronda 2 **NO APTO**
(NRE con `entryPoint` inexistente + semántica `Loaded`); ronda 3 **APTO** tras
corregir ambos (guarda de ALC + `MarkLoaded()` + registro de nodos solo para
add-ins cargados). **UI GTK intacta**: `git diff` confirma que ni
`MonoDevelop.Ide` ni los add-ins legacy cambian. Build 0 errores, 53/53 tests.

## 2026-10-03 (t) — Preferences data-driven desde el registro de add-ins

`BuildModelCore()` = esqueleto de secciones core; `BuildModel()` =
`MergeAddonSections(BuildModelCore())`. `MergeAddonSections` consulta
`App.Addins?.Extensions.GetExtensionNodes(path)` para 6 extension points
(`GlobalOptionsDialog`, `…/TextEditor`, `…/Projects/SdkLocations`,
`…/VersionControl`, `…/Other`, `…/TextEditor/Analysis/C#`), mapea el id **legacy**
del add-in al id de panel Avalonia (`LegacySectionMap`) y añade las secciones que
aún no existen (sin duplicar las ya implementadas). El add-in de Refactoring
contribuye `Analysis` + `Code Actions`/`Code Generation`/`Code Rules`.

Verificado: `merged=3`, `ep …/TextEditor/Analysis/C# nodes=3`; la sección fusionada
`codeactions` se renderiza con su icono (`md-prefs-code-actions`) y cae en
placeholder (panel aún no implementado). Regresión: `analysis`/`intellisense`/
`keybindings` siguen `placeholder=False`, 0 FATAL. QA §18.5 **APTO** (sin
hallazgos). Build 0 errores, 53/53. UI GTK intacta.

## 2026-10-03 (u) — Add-in Manager de la shell Avalonia

Nuevo `Views/AddonManagerDialog.axaml(.cs)`: lista los add-ins del host nuevo
(`App.Addins`) con **estado** (loaded / not loaded / error), id+versión+publisher,
descripción, tags y los **extension nodes** que aportan. Chrome propio de Avalonia
(sin decoraciones del OS), como el resto de diálogos. El diálogo legacy de
Mono.Addins sigue siendo el de la GTK. Hook QA: `--addonmanager`.

Verificado: `MonoDevelop.Refactoring v9.0.0 loaded=True`,
`[avalonia-addons-mgr] … state=loaded nodes=4`, ventana "Add-ins" presente, 0 FATAL.
Regresión: `keybindings`/`analysis` siguen `placeholder=False`. QA §18.5 **APTO**
(solo INFO). Build 0 errores, 53/53. UI GTK intacta.

## 2026-10-03 (v) — Add-in Xml + panel XML Schemas

Nuevo add-in `main/src/addins/MonoDevelop.Avalonia.XmlEditor/` (manifiesto JSON)
que contribuye `XmlSchemas` y `XmlFormattingOptions` a `…/GlobalOptionsDialog/
TextEditor` y `…/TextEditor/Behavior`. `MergeAddonSections` incorpora ese punto.

Panel **XML Schemas** (`XmlSchemasPanel` legacy): lista los esquemas de usuario
de `~/.local/share/MonoDevelop/9.0/XmlSchemas/*.xsd` con Add/Remove, y muestra
las asociaciones `Association*` de `XmlEditor.AddIn.Options` — mismas rutas y
claves que la GTK.

Verificado: `loaded 2/2` (Refactoring + XmlEditor), `schemas=1`,
`addon sections merged=4`, `placeholder=False`, 0 FATAL; regresión de `analysis`/
`keybindings`/`xml`/`intellisense` OK. QA §18.5 **APTO**. Build 0 errores, 53/53.
UI GTK intacta.

## 2026-10-03 (w) — Add-in VersionControl + menú Tools del Add-in Manager

Nuevo add-in `main/src/addins/MonoDevelop.Avalonia.VersionControl/` (manifiesto
JSON) que contribuye 4 secciones a `/MonoDevelop/Ide/GlobalOptionsDialog/
VersionControl` (General, Commit Message Style, Git, ChangeLog Integration). Se
conectó el **Add-in Manager (Avalonia)** al menú **Tools > "Add-ins (Avalonia)..."**
(`MenuService.OpenAvaloniaAddins` → `MainWindow.OnAvaloniaAddonManagerMenu`);
"Extensions…" sigue siendo el diálogo legacy de Mono.Addins (GTK).

Verificado: `loaded 3/3` add-ins (Refactoring, XmlEditor, VersionControl),
`ep …/VersionControl nodes=4`, `addon sections merged=4` (sin duplicar paneles VC),
`git`/`keybindings` `placeholder=False` 0 FATAL, diálogo "Add-ins" abre 0 FATAL.
QA §18.5 **APTO** (el primer intento del subagente falló por servicio → reintento).
Build 0 errores, 53/53. UI GTK intacta.

## 2026-10-04 (Th) — Add-ins DotNetCore + SourceEditor2 + Fix LegacySectionMap Bug

Migración de los add-ins **DotNetCore** y **SourceEditor2** al nuevo sistema de manifiestos Avalonia.
Se implementó una corrección crítica en `PreferencesDialog.axaml.cs` para evitar colisiones de IDs
globales (ej: "General") entre diferentes extension points.

**Cambios realizados:**
- **Nuevo add-in** `main/src/addins/MonoDevelop.Avalonia.DotNetCore/` (contribuye `.NET Core` en `/Projects/SdkLocations`).
- **Nuevo add-in** `main/src/addins/MonoDevelop.Avalonia.SourceEditor2/` (contribuye `General`, `Markers`, `Behavior`,
  `IntelliSense` y `Color Theme` en `/TextEditor`).
- **Refactor de `LegacySectionMap`**: Ahora es `Dictionary<point, Dictionary<id, mappedId>>`. Esto evita que el
  "General" de Text Editor mapee erróneamente a `vcgeneral`.
- **Refactor de `MergePoints`**: Ahora soporta un `ParentId`. Permite que las secciones se fusionen en un
  nodo descendiente (ej: `netcore` dentro de `sdklocations`) en lugar de siempre en la categoría raíz.

**Verificación:**
- Build Avalonia Shell (net10.0): **0 errores**.
- Carga de add-ins: **5/5** (Refactoring, XmlEditor, VersionControl, DotNetCore, SourceEditor2).
- Árbol de Preferences: `addon sections merged=0` (sin duplicados; IDs correctos por contexto).
- QA §18.5: **APTO**.

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
- Completar MIME/iconos en los hooks de plataforma más allá del esqueleto → **CERRADO en B.4 (2026-10-17)**: `MimeCatalog` + nodos `/MonoDevelop/Core/MimeTypes` de los manifiestos + iconos del árbol vía `IPlatformHooks`.

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

**Pendiente de la Fase 4:** ~~oleadas C–E y la decisión de diseño de la migración GnomePlatform→LinuxPlatform.~~ → **CERRADO**: oleadas C, D y E completadas e informadas abajo (2026-10-06) y la migración GnomePlatform→LinuxPlatform completa (2026-10-06, sección "Migración GnomePlatform→LinuxPlatform" de este registro).

#### Oleada B — informe (2026-10-06)

**Add-ins creados** (manifiesto + stub; nodos legacy traducidos a extension points aplanados):

- `main/src/addins/MonoDevelop.Avalonia.Deployment/` — ID `MonoDevelop.Deployment`: `Commands` (CreatePackage/AddPackage/Install), `MainMenu/Project`, `ContextMenu/ProjectPad/Tools`, `ProjectTemplates` (PackagingProject), `Pads/ProjectPad` (2 NodeBuilders), `PackageBuilders`, `DeployFileCopiers`, `DeployServiceExtensions`, `PackageBuilderEditors`, `FileCopyConfigurationEditors`, `ContextMenu/ProjectPad/Package`, `ContextMenu/ProjectPad/PackagingProject`, `DeployDirectories` (6), `DeployPlatforms` (2), `ItemOptionPanels`, `SerializableClasses` (6), `StockIcons`/`TemplateImages` (legacy `main/src/addins/Deployment/MonoDevelop.Deployment/`).
- `main/src/addins/MonoDevelop.Avalonia.AspNet/` — ID `MonoDevelop.AspNet`: `ProjectTemplates` (3), `FileTemplates` (34), `FileTemplateTypes`, `FileFilters` (2), `LegacyEditorSupport`, `ProjectModelExtensions` (5 flavors con GUID), `TypeSystem/Parser` (3), `SerializableClasses`, `ItemOptionPanels/Run` (XSP), `ToolboxLoaders`/`ToolboxProviders`, `Commands` (6), `ContextMenu/ProjectPad/Add`, `Pads/ProjectPad` (2), `MimeTypes` (16), `TextEditorExtensions` (2), `Html/DocTypes` (4), `ExecutionHandlers` (XSP), `CompletionCharacters`, `SourceEditor2/ContextMenu/Editor`, `CodeTemplates`, `CodeFormatters`, `CustomTools`, `FileTemplateConditionTypes`, `ProjectTemplateWizards`, `TemplateImages`/`StockIcons` (legacy `main/src/addins/AspNet/`).
- `main/src/addins/MonoDevelop.Avalonia.AspNetCore/` — ID `MonoDevelop.AspNetCore` (deps: `MonoDevelop.Ide` + `MonoDevelop.DotNetCore`): `FileTemplates` (13, con `file`), `ProjectTemplateCategories/netcore/app`, `Templates` (13, con condición SDK), `ExecutionHandlers`, `RunConfigurationEditors`, `FileTemplateConditionTypes`, `Pads/ProjectPad` (ScaffoldNodeExtension), `ContextMenu/ProjectPad`, `Commands/Project` (3), `ContextMenu/ProjectPad/Publish`, `MainMenu/Build`, `ProjectTemplateWizards` (legacy `main/src/addins/MonoDevelop.AspNetCore/`).

**Decisiones documentadas (hallazgos BAJO de QA aceptados como decisiones del plan):**

- AspNetCore: el legacy define las plantillas en 6 bloques por SDK (3.1/3.0/2.2/2.1/2.0/1.x, ~57 nodos); el manifiesto aplanado conserva las 13 del SDK más reciente (3.1) con condición `AspNetCoreSdkInstalled, UseNetCore31=true` y descarta los bindings de variantes de SDKs legacy (`templateId`/`path` al nupkg). La shell moderna (.NET 10) no consume esas variantes.
- EP `/MonoDevelop/Deployment/DeployDirectoryResolvers` (legacy) sin declarar: en el host los EP se auto-declaran al registrar el primer nodo (`AddonHost.cs`); el sub-add-in pendiente `MonoDevelop.Deployment.Linux` lo extiende y se migrará con él.
- EP `/MonoDevelop/Asp/CompletionBuilders` (consumidor: CSharpBinding legacy) sin declarar hasta migrar su consumidor; `commitChars` de `CompletionCharacters` y detalles `isText`/`baseType` de MIME descartados en el aplanado (se conservan `commitOnSpace`/`pattern`).

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

- Acciones Install/Update/Enable/Disable/Uninstall/Install from file requieren API en el nuevo host (repositorio remoto + instalación + habilitación) — las pestañas y la UI están listas sobre los nuevos componentes.
- El `CecilReflector` de `main/external/mono-addins` sigue incompatible con el Cecil 0.10.1 del build: afecta al diálogo/registry Mono.Addins **del lado GTK** (no a la shell Avalonia, que ya no lo usa). Parchearlo para Cecil 0.10 es tarea del lado GTK.

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
- El add-in de plataforma **Linux** compila en el host (Linux) pero no fue objeto de la validación de esta task (el alcance era Mac + Windows).
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

### In3 — Runsheet de smoke tests de GUI (criterio de aceptación): ejecutado, gate CERRADO (2026-10-17)

**Contexto**: In3 es el gate de aceptación de la fase de migración (runsheet: 1 arranque limpio; 2 menús; 3 About; 4 Preferences; 5 AddinManager; 6 crear/compilar C# Library; 7 abrir `.sln` y navegar; cierre: 0 críticas nuevas). Quedó pendiente "hasta correr la GUI bajo runtime .NET 10"; con la shell Avalonia net10 funcionando (B.4 cerrada) se volvió ejecutable. Se preparó el harness en `~/opencode/qa_in3/` (`smoke_in3.sh`, fixture `proj/` = copia de QaB4 con `Program.cs` real de 21 líneas) y se extendió la shell con dos flags QA: `--newproject=<template>` (console|library) y `--menudump` (volcado del modelo de menús de `MenuService`; AT-SPI no está disponible en el entorno — sin `at-spi-bus-launcher`).

**Fixes de producto encontrados y resueltos por el runsheet**:

1. `NewSolutionDialog.axaml.cs`: la etiqueta interna de plantilla (`library`) se pasaba tal cual a `dotnet new`, cuyo short name en el SDK es `classlib` → el scaffold fallaba en silencio (el stderr del CLI se descartaba). Ahora: mapeo de etiquetas (console/classlib/nunit), scaffold mínimo `.shproj`/`.projitems` para `shared` (formato legacy `SharedAssetsProject`, GUID `D954291E-…`), log del stderr/exit del CLI (`[newsolution] dotnet exit=…`) y el fallback de `.sln` referencia el fichero realmente creado con su GUID de proyecto.
2. `MainWindow.axaml.cs` (`RunBuildAsync`): enumeraba los proyectos por escaneo del directorio del `.sln` (`Directory.GetFiles`), de modo que un proyecto referenciado fuera de ese directorio (p. ej. uno añadido desde otra ubicación) nunca se compilaba. Ahora parsea las referencias `Project(…) = …, "ruta"` del `.sln` (con fallback al escaneo si el sln no aporta nada).

**Cómo validar** (evidencia completa en `~/opencode/qa_in3/`, índice `qa_report_in3.md`):

1. **7/7 PASS** ejecutados por el subagente Tester QA Senior en un run independiente (logs `smoke/in3_r*.log`, capturas `smoke/in3_q*.png`): arranque limpio (0 FATAL/Unhandled, `LinuxPlatform ready`, 30/32 addins, pads pintados); menús (`menus=11 items=203 bindings=180-181`, 11 menús, barra pintada); About (ventana 560×520, 99,9 % de píxel pintado); Preferences (paneles reales: keybindings 180 cmds/5 schemes, themes 4/Dark, cfmt, vc; 0 missing); AddinManager (`tab=installed items=32 host=True`); crear/compilar C# Library (`created=True sln-entry=True mappings=True`, build de la solución compila QaB4 + QAAdded —fuera del dir del sln—, ambos 0 errores); abrir `.sln` y navegar (classes→caret línea 5, outline→caret línea 5, code issues→caret línea 12, 0 errores).
2. **Checks estáticos**: 0 `TypeLoadException`/`MissingMethodException` en los 8 logs; 176 IDs de comando de menú vs. los `case` de `OnMenuCommand` (solo el dummy deshabilitado `...` sin case; los 3 de Session Recording son stubs que loguean, `MainWindow.axaml.cs:6306-6314`); 0 errores no-NU1201 en el build de la solución del eje net10.

**QA (§18.5)**: ciclo Tester QA Senior → dev → revalidación. La primera pasada del dev (pre-subagente) detectó y corrigió los 2 fixes de producto del ítem 6; el subagente repitió el runsheet completo de forma independiente: **7/7 PASS, 0 críticas nuevas**. Hallazgos no bloqueantes (ver sección In3 de `docs/interfaz-plan.md`): colisión de documentos por nombre de fichero (medio, refactor diferido), `RunStartupProjectAsync` con escaneo de directorio (bajo/medio), stubs de Session Recording (bajo, uno engañoso), y 4 menores (umbrales de estado en el conteo de bindings, fixture de code issues intencional, warning Roslyn-key preexistente, líneas duplicadas/cosméticas).

**Cierre**: el gate de aceptación de la fase de migración **queda superado**: la shell Avalonia net10 cumple los 7 ítem del runsheet sin críticas nuevas. Documentación en la sección *In3* de `docs/interfaz-plan.md` (con el inventario actualizado: filas B/C/G cerradas o reescritas, y las filas I/J nuevas para los hallazgos no bloqueantes).

**Pendiente / follow-up**:

- Fila I: **CERRADA (2026-10-18)** — refactor del modelo de documentos (clave por ruta completa); ver la sesión *Fila I* de abajo.
- Fila J: **CERRADA (2026-10-19)** — build/run desde las referencias del `.sln` con prioridad Exe; ver la sesión *Fila J + Fila C* de abajo.
- Fila C: **CERRADA (2026-10-19)** — los 3 comandos de Session Recording con comportamiento real en el shell (grabador en formato XML legacy, viewer in-proceso, replay por re-dispatch); ver la sesión *Fila J + Fila C* de abajo.
- Frente B (verificado esta sesión): el bloqueo original `Mono.Addins.Setup` (CS1503/CS0012) está resuelto desde B.5; el build completo de `Main.sln` (221 proyectos) sigue sin ser verde por 126 NU1201 (eje legacy net472 vs. core net10.0-only, consecuencia del cutover). El eje legacy queda congelado (DLLs estagiadas de septiembre funcionan — `--old-gui` verificado — pero el fuente no recompila). La decisión D determina si se restaura el multi-TFM del core o se jubila el fuente legacy.

### Fila I — Modelo de documentos claveado por ruta completa (2026-10-18)

**Contexto**: hallazgo no bloqueante del runsheet In3 (fila I del inventario): `OpenFileDocument` claveaba documentos y pestañas por `Path.GetFileName`, y si ya existía un documento con ese nombre hacía early-return activándolo — abrir `A/Program.cs` y luego `B/Program.cs` dejaba editando el primero en vez de abrir el segundo (defecto de corrección: el usuario cree que edita B y edita A).

**Cambios** (`main/src/core/MonoDevelop.Startup.Avalonia/`, 2 ficheros):

1. `MainWindow.axaml.cs` — `OpenFileDocument`: la clave del documento/pestaña pasa a ser `DocKey (path)` (= `Path.GetFullPath`); helper `DocKey` compartido. El header de la pestaña muestra `Path.GetFileName (tag)`; el botón de cierre lee la `Tag` de la pestaña **al hacer click** (antes capturaba el valor y se obsoletaba tras un rename).
2. `UpdateDocTabTitle`: el buscador de la etiqueta del tab ahora desciende al `StackPanel` del header — hallado durante la tarea: **el marcador de sucio " •" nunca se pintaba** en pestañas de fichero (buscaba hijos directos del `Panel` del header, donde solo hay un `StackPanel`).
3. Rename de fichero con documento abierto: re-clavea `docs`, la lista `documents` y la `Tag` de la pestaña a la nueva ruta (antes la pestaña quedaba huérfana de su editor: `SelectDocument`/cierre/operaciones por tag fallaban).
4. Save-As: re-clavea in-place a la nueva ruta (antes quitaba entradas y re-creaba la pestaña con el nombre corto, dejando la pestaña vieja atrás).
5. Migración de los ~100 sitios de llamada: 24 handlers QA (su `name` ahora es la clave), lookups por ruta (`OpenFileDocumentAtLine`, `IsEditorDirty`, `VerifyCaret`, message bubbles del build, error pad, watch), buscador de toolbar (`CollectFileResults`/`AddFileResult`: dedupe por `DocKey`, filas "(open document)" resueltas por clave, títulos con nombre corto), y literales sueltos (`"Program.cs"` en handlers QA → `DocKey (file)`).
6. `Program.cs` + handler `--dupdocs` (QA): crea `A/Note.txt` y `B/Note.txt` en un tmp dir y aserta 2 pestañas/2 docs, editores independientes, label de pestaña = nombre corto, buffers independientes, saves por fichero correctos y cierre de A sin afectar a B.
7. Robustez de harness (hallazgos de QA): NRE preexistente de `--gutterbp` sin sln (la línea `stored=` ahora guarda `loadedSolutionPath is null`) y `--mcaret` deja de asumir la línea 4 (localiza `class Program` dinámicamente, inmune a la deriva del fixture compartido).

**Cómo validar** (evidencia completa en `~/opencode/qa_in3/qa_report_fila_i.md`):

1. **Build 0 errores** de `MonoDevelop.Startup.Avalonia.csproj`.
2. **`--dupdocs`** (caso central): `tabs=2 docs=2`, `both-independent=True`, `tab-label=Note.txt`, `independent-buffers=True`, `save-correct=True`, `after-close docs=1 B-open=True` — 0 FATAL.
3. **Regresión de flujos de documentos** (QA Senior, iteración 1): baseline, `--sln`+`--dblclick` (3 carets verificados), `--bmkpad`, `--dirtyfiles`, `--editorpad`, `--windocs`, `--persistqa`, `--mcaret`, `--gutterbp`, `--goto` — todos PASS.

**QA (§18.5)**: 3 iteraciones del Tester QA Senior. Iteración 1: caso central + regresión PASS; hallazgos F1 (regresión real del buscador "(open document)" causada por la migración), F2 (dedupe/título del buscador), F3 (fixture compartido dependiente de líneas), F4 (NRE preexistente de `--gutterbp` sin sln). Iteración 2: F1/F2/F4 corregidos y verificados; F3 abierto (harness). Iteración 3: F3 corregido (linea independiente) y verificado en el estado de fixture que fallaba. **Veredicto final: 4/4 hallazgos CERRADOS — la fila I queda cerrada.**

**Cierre**: el modelo de documentos de la shell Avalonia clavea por ruta completa: dos ficheros con el mismo nombre en directorios distintos son documentos independientes (abrir, editar, guardar y cerrar por separado); rename y save-as re-clavean la pestaña sin dejarla huérfana; y el marcador de sucio de las pestañas por fin se pinta.

**Pendiente / follow-up**:

- Fila J: **CERRADA (2026-10-19)** — `RunStartupProjectAsync` elige el proyecto de arranque desde las referencias del `.sln` con prioridad a `<OutputType>Exe</OutputType>`; ver la sesión *Fila J + Fila C* de abajo.

### Fila J + Fila C — Build/run desde referencias `.sln` + Session Recording real (2026-10-19)

**Contexto**: cierre de dos hallazgos no bloqueantes del runsheet In3 en la misma sesión: **Fila J** (`RunStartupProjectAsync` elegía el proyecto de arranque por escaneo del directorio del `.sln`: una librería listada antes que la consola ganaba, y un proyecto referenciado fuera del directorio nunca era candidato) y **Fila C** (los 3 comandos de Session Recording eran stubs que solo logueaban; `ToggleSessionRecorder` era engañoso).

Directrices del operador: (1) ningún cambio de `MonoDevelop.Ide.*`/`MonoDevelop.Core.*` que rompa la shell GTK legacy (`--old-gui`); (2) sin `#if !AVALONIA_SHELL` nuevos en `MonoDevelop.Ide`; (3) la integración **total** del backend no-GTK en el shell Avalonia (reutilizar módulos no-GTK + portar los GTK a Avalonia) es fase posterior, cuando terminen todos los pendientes — por eso Session Recording corre sobre un grabador local al shell que habla el mismo formato en disco legacy, no sobre el subsistema `AutoTest` completo.

**Cambios** (5 ficheros en `main/src/core/MonoDevelop.Startup.Avalonia/` + `CommandManager.cs` en `MonoDevelop.Ide`):

1. **Fila J** (`MainWindow.axaml.cs`): `SolutionProjects (sln)` (parsea referencias `Project(…)` del sln resueltas contra el directorio del sln; fallback al escaneo) y `PickStartupProject` (prioridad `<OutputType>Exe</OutputType>`, fallback al primero) extraídos del `RunBuildAsync` de In3; `RunStartupProjectAsync` los usa y loguea `(Exe)` / `(no OutputType=Exe)`.
2. **Fila C** — `Views/SessionRecorder.cs` (nuevo): graba los ids que pasan por `OnMenuCommand` (único dispatch: menús, hotkeys, replay) en el formato XML del legacy `SessionRecord` (`xs-event-replay-log` / `event type="CommandEvent"` / `commandID` / `dataItemIndex`); excluye `ToggleSessionRecorder`/`ReplaySession`; al cargar omite eventos Gdk (KeyPress/String) → compatible con grabaciones GTK.
3. **Fila C** — `Views/InstrumentationViewerDialog.axaml(.cs)` (nuevos): viewer in-proceso (mdmonitor no se empaqueta); sondea `InstrumentationService.GetCategories()` cada 500 ms; columnas del monitor legacy.
4. **Fila C** (`MainWindow.axaml.cs`): hook `sessionRecorder.Record` al inicio de `OnMenuCommand`; 3 casos reales en el switch (toggle gateado por la preferencia `MonoDevelop.EnableAutomatedTesting` con Save/Cancel-keep-recording del legacy; label Start/Stop re-derivado en cada `BuildMenu`; viewer no modal tras `PublishService` una vez; replay por re-dispatch con 50 ms/evento); counters `Documents.Opened`/`Documents.Closed`/`Builds` en los flujos reales; QA flags `--sessionrec=`/`--sessionreplay=`/`--instrview` (`Program.cs` + ctor).
5. **`CommandManager.cs` (MonoDevelop.Ide)**: `CommandActivationEventArgs` + `CommandSource` a zona compartida (fuera de #if, sin Gdk) y, bajo `#if AVALONIA_SHELL`, la clase estática helper se amplía a clase instancia no-GTK (registry, `DispatchCommand` con eventos `CommandActivating`/`CommandActivated`, `IsEnabled`, `ToCommandId`). La clase GTK no tocada. Superficie aún sin consumidores en el shell: es el primer bloque no-GTK reutilizable de la fase posterior de backend.

**Cómo validar** (artefactos en `~/opencode/`: `qa_filac_run.sh`, fixture `qa_filac/QaFix.sln` — lib primero, Exe después, proyecto externo `qa_filac_ext/ExtProj` — logs `qa_filac_*.log`, `qa_filac/session_qa.xml`): build 0 errores; J1 `--build` compila los 3 proyectos en orden del sln (incluido el externo) 0 errores; J2 `--run` → `startup project ExeAfter.csproj (Exe)` + `QA_EXE_AFTER_RAN` + exit 0; C1 `--sessionrec` → 2 eventos con el XML legacy exacto; C2 `--sessionreplay` → `replaying 2 events …` → `replay finished`; C3 `--instrview` → monitor abierto + `rows=3` + `docsOpened=1 docsClosed=1 builds=1`; REG `--menudump` → `menus=11 items=203 bindings=184` sin regresión; R2 compatibilidad GTK verificada a fuente (build GTK no construible en el entorno — preexistente).

**QA (§18.5)**: Tester QA Senior, 1 iteración: **10/10 validaciones PASAN**; 1 hallazgo menor sin acción (`SessionRecorder.Clear()` sin consumidores — API de paridad legacy); 0 bloqueantes.

**Cierre**: build/run consumen las referencias del `.sln` (con proyectos fuera del directorio) y el run prioriza el Exe; los 3 comandos de Session Recording tienen comportamiento real sin stubs (grabación en XML legacy intercambiable con GTK, replay por re-dispatch, monitor in-proceso con counters vivos).

**Pendiente / follow-up**:

- **Fase posterior (directriz del operador)**: integración total del backend `MonoDevelop.Ide.*` + `MonoDevelop.Core.*` en el shell Avalonia (módulos no-GTK reutilizados + GTK→Avalonia); en ella el grabador local puede sustituirse por el subsistema `AutoTest` real (mismo XML).
- Resto del inventario In3 sin cerrar: filas A (pads — `unittests` cerrado en la sesión de abajo, quedan `help`/`toolbox`), F (iconos), H (NRefactory/Cecil), decisiones D/E y EP `DeployDirectoryResolvers`.

### Fila A — Pad "Unit Tests" real (2026-10-20)

**Contexto**: cierre del frente de mayor valor de la fila A del inventario: el pad `unittests` era la última cáscara de placeholder tras M26. Directriz del operador: **el QA lo hace el subagente Tester QA Senior, no el agente Dev** (ciclo §18.5); artefactos en `~/opencode/`; cierre en commit+push.

**Cambios** (10 ficheros):

1. `main/src/core/MonoDevelop.Ide/Services/UnitTestDiscoveryService.cs` (nuevo, ~790 líneas, sin Gtk): `UnitTestNode` (solución ▸ proyecto ▸ namespace ▸ clase ▸ test; FQN, archivo/línea, `Framework`, `Explicit`, `State`, `Message`, `DurationMs`; grupos agregan estado) y `UnitTestState`. Descubrimiento por **escaneo de texto** (regex de atributos, enfoque anclado de `SymbolIndexService`, reutilizando su estado léxico de comentarios/cadenas ahora `internal`): NUnit `[Test]/[TestCase]/[TestCaseSource]/[ValueSource]/[RandomDataSource]/[Retry]`+`[Ignore]`/`[Explicit]`, xUnit `[Fact]/[Theory]/[SkippableFact]`+`Skip=`, MSTest `[TestMethod]/[DataTestMethod]`; marcadores de clase `[TestFixture]/[TestClass]/[Collection]`; `IsTestProject` por `Microsoft.NET.Test.Sdk`/`IsTestProject`/paquetes. Expansión de casos: cada `[TestCase]/[InlineData]/[DataRow]` → nodo propio (`SplitArgs` respeta literales y anidación; `CaseDisplayName` por framework: NUnit `M (1, 2)`, xUnit `M (a: 1, b: 2)` con nombres de parámetro, MSTest `M(1, 2)`); `[TestCaseSource]` no se expande. `MethodName` + `FilterKey` (un run de caso ejecuta el método entero: los FQN de caso no son portables entre adaptadores). `BuildFilter` (FQN `|`), `ResolveNodes` (mapeo línea→nodos por FQN/sufijo de caso/nombre corto) y `TestRunParser` (líneas de test con duraciones `[< 1 ms]`/`[15 s]`/`[00:01:02]`, summary, ubicación de fallo `in File.cs:line N`).
2. `MainWindow.axaml.cs` (~740 líneas): pad "Unit Tests" (dock derecho, icono `nunit-pad-icon`) — árbol con icono de estado por nodo, menú contextual (Run/Show code/Go to failure/Show details), botones Run all/Run/Stop, doble clic ejecuta, panel de detalle con mensaje de fallo; `RefreshUnitTestsPad` al cargar solución. Motor: `RunUnitTestsAsync` (reset de estados, `[Explicit]` excluido de runs de grupo salvo nodo único — paridad `UnitTestGroup.OnRun`), un proceso `dotnet test -c <config> <csproj> --nologo -v minimal [--filter …]` por proyecto; `RunTestProcessAsync` drena stdout/stderr con `ReadLinesAsync` antes de `WaitForExitAsync` (evita el intercalado por buffer), variable de loop declarada en el cuerpo (closure) y `await UIThread.InvokeAsync` tras el proceso (evita la carrera de pool thread: `exited`/`run finished`/`totals` por delante de las líneas en cola); `HandleTestRunLine` (bajo `-v minimal` solo hay líneas Failed/Skipped → en la summary, lo que sigue `Running` pasa a `Success`, o a `Error "no tests matched the filter"` si `Total: 0`; captura del mensaje de fallo; scope en error si el proceso no emite summary); acumuladores `TestRunLineState` de totals entre proyectos. Comandos de paridad en `OnMenuCommand` (`MonoDevelop.UnitTesting.Commands.*`: RunAll/Run/Rerun/ShowCode/GoToFailure/SelectInTree/ShowDetails/RunWith + caret `RunTestAtCaret`/`SelectTestAtCaret` vía `CaretUnitTestNode`). QA: flags `--unittests` (descubrir+volcar) y `--unittests-run` (+ run real esperando `unitTestRunTask`, 240 s), guard de `RestorePadsLayout` para `--unittests*` (el layout persistido no oculta la pestaña al final del flujo) y `SetPadVisible` idempotente.
3. `MenuService.cs`: Run — "Run _Unit Tests" (Ctrl T, `md-run-unit-tests`) + subcategoría "Unit _Testing" (21 nodos espejo del manifiesto del addin; los de pad cableados, los de Debug/TestChart por paridad se reportan como no portados). Go To Type pierde Ctrl T (colisión; el legacy usa el acorde Ctrl+K,Ctrl+V, inexprésable con gesto único).
4. `IconService.cs`: 6 stock ids de estado de nodo (PNG `unit-*-16` legacy de `MonoDevelop.UnitTesting/Gui`).
5. `SymbolIndexService.cs`: `UpdateLexState` → `internal`.
6. `SolutionLoader.cs`: superficie la excepción interna de `TargetInvocationException` al fallar la carga (los `.slnx` — default de `dotnet new sln` en SDK 10 — no son parseables; fixture con `--format sln`).
7. `Program.cs`: `QaDialogArg` acepta `--unittests*`.
8. Tests nuevos (`UnitTestDiscoveryTests.cs` + `TestRunParserTests.cs`): suite **83/83** (4 nuevos).

**Desviaciones documentadas** (sin modelo de proyecto ni test SDK en el shell): descubrimiento por escaneo de texto y ejecución por `dotnet test` (VSTest) con filtro FQN; no portados TestChart/regresiones/debug de tests (menú por paridad); run de caso = método entero; `[TestCaseSource]` sin expandir; GotoType sin atajo.

**Cómo validar** (artefactos en `~/opencode/qa_unittests/`): fixture `QaFixture.sln` (`--format sln`) con **QaXunit** (xUnit 2.9.2, 5 nodos), **QaNunit** (NUnit 4.3.2, 7 nodos) y **QaConsole** (consola, ausente del pad); `SHELL_LOG=<log> ~/opencode/launch_shell.sh --sln=…/QaFixture.sln --unittests-run`; kill `pkill -9 -f "[A]valoniaShell.dll"`. Números esperados (R5, 2/2): `2 test project(s), 12 test(s) discovered`; run lines `QaNunit.csproj (6 test(s))` (7−1 Explicit) / `QaXunit.csproj (5 test(s))`; `run finished: 11 test(s)`; `totals: 8 passed, 2 failed, 2 skipped, 12 total`; 1 bloque `SetPadVisible unittests=True`; pestaña visible al final.

**QA (§18.5)**: 5 rounds del Tester QA Senior (no toca el repositorio). R1: 7 hallazgos (casos colapsados 10/13, nombres cortos NUnit + duración `[< 1 ms]` no parseadas, tests que pasan se quedaban `Running` bajo `-v minimal`, sin summary agregada, faltaban líneas por test, colisión Ctrl T, carrera de orden del buffer de vstest) → corregidos. R2: F1 **Crítico** (closure del drenado: variable de loop por referencia) → variable en el cuerpo; F2 error del plan (5 nodos, no 4). R3: F-R3-1 **Major** (carrera de marshalling: continuation en pool emitía `exited`/`run finished`/`totals` antes que las líneas en cola) → `await UIThread.InvokeAsync`; F-R3-2 **Minor** (burst no determinista de `SetPadVisible` por restore de layout) → guard `RestorePadsLayout` + idempotencia. R4: 4/4 limpias, logs idénticos salvo timing; F-R4-1 no-defecto (criterio del plan corregido a "1 bloque"). R5: **PASS — 0 hallazgos** (`qa_round5_report.md`): 83/83 y 14/14 criterios en 2 ejecuciones, sin cuelgues (~13 s), kill limpio. Entre rounds: 2 bugs iniciales de descubrimiento (atributos apilados: `Clear()`→`AddRange`; `Mixed` contaba como fallo en `AggregatedState`) y 2 problemas de build de entorno (carrera TFM en `obj/…AssemblyInfo.cs`; file-lock `main/build` → `dotnet build-server shutdown`).

**Cierre**: el pad "Unit Tests" es real (descubrimiento NUnit/xUnit/MSTest con nodos por caso, ejecución `dotnet test` por proyecto con filtro FQN, árbol en vivo con estados/duraciones, mensajes de fallo, comandos de paridad del addin). Fila A cerrada en su frente de mayor valor; `help`/`toolbox` siguen en el inventario.

**Pendiente / follow-up**:

- Fila A (resto): cáscaras `help` y `toolbox`.
- Paridad diferida de Unit Testing: TestChart, regresiones, debug de tests.
- Limitación documentada: `[TestCaseSource]` sin expandir a nodos de caso.
- Resto del inventario In3 sin cerrar: F (iconos), H (NRefactory/Cecil), decisiones D/E y EP `DeployDirectoryResolvers`.

### Fila A — Pads Help y Toolbox reales (2026-10-21)

**Contexto**: cierre del resto de la fila A: las dos cáscaras de placeholder restantes, `help` (`(documentation index)`) y `toolbox` (`(toolbox items)`). Directriz del operador (invariante): **el QA lo hace el subagente Tester QA Senior, no el agente Dev** (ciclo §18.5); artefactos en `~/opencode/`; cierre en commit+push (sin PR).

**Investigación**: legacy `help` = `MonodocTreePad` sobre `Monodoc.RootTree`, que en el build net10 es un stub de compat (`MonoRoslynCompat/CompatStubs.cs`) y no hay datos Monodoc en la máquina → el pad legacy está vacío aquí; el shell parsea el formato Monodoc (`monodoc.xml` + archivos de hijos) directamente. Legacy `toolbox` = providers por EPs (GtkCore/AspNet/CodeTemplates) del addin DesignerSupport: lo portable al shell es el contenido de code templates (el editor `SkTextEditor` ya tiene el set builtin y los user templates viven en `UserDataRoot/Snippets/*.xml` v3.0, como el legacy). El csproj del shell globbea `MonoDevelop.Ide/Services/**` y `Controls/**`: los servicios nuevos entran solos en el build del shell; `MonoDevelop.Ide.csproj` usa lista explícita (1249 `Compile Include`, sin estos ficheros) → el build legacy `--old-gui` no se ve afectado.

**Cambios** (7 ficheros):

1. `main/src/core/MonoDevelop.Ide/Services/HelpTreeService.cs` (nuevo, sin Gtk): `HelpNode` + `HelpTreeService` static — `LoadTree(dirs)` fusión de los índices `monodoc.xml` bajo raíz única "Mono Documentation" (comportamiento legacy) con `Searched`/`Errors`; `LoadChildren(node)` perezoso (como el `RowExpanded` legacy); `ResolveUrl` (absolutas tal cual, relativas contra el directorio del índice declarante); `ParseIndexFile` (`<node file=…>` → lazy, `<node url=…>` → página; tolerante a nodos sin nombre; malformado → `Errors`); `DefaultSourceDirs()` = app dir + drop-in `UserDataRoot/monodoc`.
2. `main/src/core/MonoDevelop.Ide/Services/ToolboxService.cs` (nuevo, sin Gtk): `ToolboxItem` + `ToolboxService` static — `GetItems(snippetsDir?)` = builtin (`SkTextEditor.EditorTemplates`) + user (formato legacy `CodeTemplates` v3.0; user gana por shortcut — merge legacy); `GetCategories` (orden del árbol legacy); `Filter` (substring case-insensitive, el `filterTextChanged` legacy); `LoadUserTemplates` (`_Group`/`_Description`/`Shortcut`; templates sin Header o no-Standard omitidos con log).
3. `main/src/core/MonoDevelop.Ide/Controls/SkTextEditor.cs`: `EditorTemplates` static (single source of truth) y `ExpandTemplateAtCaret(string[])` (selección o caret, indent de la línea, `$end$`→caret, `$selected$`→selección, 1 undo step); `ExpandCodeTemplate()` refactor delegando en ella.
4. `MainWindow.axaml.cs` (~250 líneas): pad **Help** (izquierda, auto-hide): TreeView con carga perezosa por expansión, tooltips de summary, estado vacío con rutas buscadas, doble clic → `OpenHelpNode` (local → documento del shell; http(s) → navegador; sin URL → log). Pad **Toolbox** (derecha, auto-hide): búsqueda en vivo + checkbox categorías + árbol categorías▸items (o plano); doble clic/Enter → `UseToolboxItem` (expansión en el editor activo; sin documento → log). F1 (`HelpCommands.Help`) ahora muestra el pad Help real (antes stub). QA: `--helptree`/`--toolbox` (recarga+muestra+volcado) con fuentes `--helpsrc=`/`--toolboxsnippets=`.
5. `Program.cs`: `HelpSourceArg`/`SnippetsDirArg` + `QaDialogArg` acepta `--helptree`/`--toolbox`.
6. Tests nuevos: `HelpTreeServiceTests.cs` (10) + `ToolboxServiceTests.cs` (11) → suite **103/103** (20 nuevos).
7. Fixes de la R1 del QA en `MainWindow.axaml.cs`: **H1 (crítico)** — el tema Fluent oculta el expander de rows `:empty` y `Expanded` de un row vacío nunca se dispara → `MakeHelpItem` materializa al crearse los hijos inline y siembra un placeholder en los nodos lazy (se sustituye al primer `Expanded`); **H2 (mayor)** — `--helptree`/`--toolbox` añadidos a la exención de `RestorePadsLayout` (el layout persistido re-ocultaba la pestaña).

**Desviaciones documentadas**: parse del índice sin Monodoc.dll (stub); viewer interno Monodoc no portado (páginas locales → documento del shell, absolutas → navegador); providers de toolbox por addin (GtkCore/AspNet) y diálogo "Add items" no portados (requieren designer/Gtk#); `QaDialogArg` solo procesa el primer flag QA (diseño existente, no reestructurado); overlay Welcome cubre el workbench hasta abrir un documento (diseño documentado).

**Cómo validar** (artefactos en `~/opencode/qa_help/`, `~/opencode/qa_toolbox/`, `~/opencode/qa_help_toolbox/`): fixture Monodoc (`monodoc.xml` + `api.xml` lazy + 3 HTML con marcadores `QA-HELP-*`) y fixture de snippets (`qasay` en "QA Group" + override de `cw`); `SHELL_LOG=<log> ~/opencode/launch_shell.sh --helptree --helpsrc=~/opencode/qa_help/MonodocDocs` y `… --toolbox --toolboxsnippets=~/opencode/qa_toolbox/snippets`. Esperado (verificado R2): `[help] documentation tree loaded (5 nodes)` + dump de 5 niveles; expansión `Guides→(Getting started, Reference)`, `Reference→API`, `API→(Overview, Types)`; doble clic `Getting started` → `[help] opened getting-started.html`; `[toolbox] 7 item(s)` + `QA Group: qasay` + 6 `Text Snippets: …`; filtro `qas` → solo qasay; checkbox off → plano; activación → `[toolbox] used qasay at caret (line N)` + `Console.WriteLine("QA-TOOLBOX-");` sin literal `$end$`; sin documento → `[toolbox] open a text document first`; sin fixture → `[help] no monodoc.xml in: …` + estado vacío.

**QA (§18.5)**: 2 rounds del Tester QA Senior (artefactos `~/opencode/qa_help_toolbox/`). **R1** — FAIL, 4 hallazgos: H1 crítico (árbol no expandible bajo la raíz — expander oculto en rows `:empty`, `Expanded` nunca dispara la carga perezosa) y H2 mayor (flags no exentos del restore de layout) corregidos por el dev; H3 (overlay Welcome — diseño documentado) y H4 (primer flag QA manda — diseño existente) aceptados como diseño. **R2** — **PASS, 0 hallazgos**: 103/103; árbol expandible en todos los niveles (lazy `API→(Overview, Types)` al primer `Expanded`); apertura de documentos con marcadores verificados; H2 con layout persistido que no incluye la pestaña (sin `[pads] layout restored`); toolbox + filtro + plano OK; activación OK (`$end$`→caret); arranque default y `--old-gui` sin excepciones; layout persistido restaurado (diff limpio).

**Cierre**: la fila A queda **completamente cerrada** (6/6 pads con contenido real). El Help muestra el árbol de documentación Monodoc con carga perezosa y apertura de páginas; el Toolbox lista los code templates builtin+user con categorías, filtro y activación en el caret (reglas de merge legacy).

**Pendiente / follow-up**:

- Fila A: **CERRADA (2026-10-21)** — ver la sesión *Fila A — Pads Help y Toolbox reales* de arriba.
- Paridad diferida de `toolbox`: providers por addin (GtkCore/AspNet) y diálogo "Add items" (requieren designer/Gtk#).
- Paridad diferida de `help`: viewer interno Monodoc (las páginas locales abren como documento del shell).
- Paridad diferida de Unit Testing: TestChart, regresiones, debug de tests.
- Resto del inventario In3 sin cerrar: F (iconos), H (NRefactory/Cecil), decisiones D/E y EP `DeployDirectoryResolvers`.

### Fila A — Help: páginas Monodoc como web tab (WebView) (2026-10-22)

**Contexto**: mejora solicitada por el operador sobre el pad Help: al abrir una página Monodoc el HTML se veía en el editor de código (markup); debe abrirse **renderizada como página web** en una pestaña del área de edición (como el viewer interno del legacy, no portado).

**Cambios** (2 ficheros):

1. `MonoDevelop.Startup.Avalonia.csproj`: `Avalonia.Controls.WebView 12.1.1` (paquete oficial AvaloniaUI — en Avalonia 12 el control WebView salió de `Avalonia.Browser` 11.x, que en 12.x es el kit "Avalonia en el navegador" JS-based). Runtime: WebKitGTK 4.1 (2.54.1, verificado instalado con sus procesos en `/usr/libexec/webkit2gtk-4.1/`).
2. `MainWindow.axaml.cs`: `OpenHelpNode` → `OpenHelpWebTab(url)` — pestaña de documento tag `help:<url>` (reabrir el mismo nodo reactiva sin duplicar) con `NativeWebView { Source = … }` vía `AddDocument`; `NavigationCompleted` loguea `[help] web tab loaded|reloaded|FAILED <url>` (el re-attach al conmutar pestañas re-emite el evento: 1.ª carga `loaded`, siguientes `reloaded`); fallback al editor de código si no hay adapter (páginas locales, log `web view unavailable`). URLs http(s) pasan del navegador externo a la pestaña web interna. Hook QA `--helptree` abre el primer nodo con URL (depth-first) para ejercitar la ruta sin UI.

**Desviaciones**: el render lo hace el adapter del sistema (WebKitGTK), no un viewer propio; sin adapter, locales → editor de código con log.

**Cómo validar** (artefactos `~/opencode/qa_help_toolbox/`, prefijo `webtab_`): `SHELL_LOG=<log> ~/opencode/launch_shell.sh --helptree --helpsrc=~/opencode/qa_help/MonodocDocs` → `[help] opened getting-started.html (web tab)` + `[help] web tab loaded file:///…`; pestaña con el HTML renderizado (marcador `QA-HELP-GETTING-STARTED` como texto, sin markup — verificado por OCR); doble clic `Overview` → 2.ª pestaña; re-doble clic → sin duplicar; ✕ → cierre sin crash; sin fixture → estado vacío, sin web tabs; arranque default → 0 excepciones.

**QA (§18.5)**: 1 round del Tester QA Senior — **PASS, 0 hallazgos funcionales** (103/103; web tab completo con OCR; dedupe; cierre; sin fixture; arranque). 1 observación menor (re-emisión de `NavigationCompleted` en reactivación → línea extra de log) corregida por el dev como cambio de log (`loaded`/`reloaded`), re-verificada en smoke.

**Cierre**: las páginas de ayuda Monodoc se abren como web tabs renderizadas en el área de edición (paridad de experiencia con el viewer legacy), sin tocar el mecanismo de documentos.

## 2026-10-23 (a) — Lote 1: conversión de 9 proyectos net472 old-style a SDK-style net10.0

**Qué se hizo**: 9 proyectos de `main/external/nrefactory` y `main/external/debugger-libs` pasaron de old-style `net472` a SDK-style `net10.0` (`dotnet build -c DebugLinux`, **0 errores en los 9**). Submódulos `nrefactory` y `debugger-libs`: sus commits y push (rama `net10`) forman parte de esta tarea.

| Proyecto | Comp | Resultado |
|---|---|---|
| ICSharpCode.NRefactory | 209 | OK |
| ICSharpCode.NRefactory.Cecil | 3 | OK |
| ICSharpCode.NRefactory.CSharp | 300 | OK |
| CorApi | 5 | OK |
| CorApi2 | 68 | OK |
| Mono.Debugger.Soft | 77 | OK |
| Mono.Debugging | 81 | OK |
| Mono.Debugging.Soft | 19 | OK |
| Mono.Debugging.Win32 | 21 | OK |

**Decisiones / notas técnicas**:
- Cabecera SDK estándar **sin xmlns**: en el SDK 10.0.401 la combinación `Sdk` + `xmlns` en `<Project>` falla con `MSB4041` (comportamiento del MSBuild actual). ADVERTENCIA heredada: `MonoDevelop.GtkCore.csproj` usa `Sdk` + `xmlns` y falla con MSB4041 (fuera del alcance de esta tarea).
- `EnableDefaultItems=false`, `EnableDefaultCompileItems=false`, `GenerateAssemblyInfo=false`; refs BCL implícitas y `netstandard` eliminadas; firmas conservadas. `PublicSign` en CorApi, CorApi2 y Mono.Debugging.Win32 (snk RSA-1024 → la firma fuerte real falla con `invalid digest` en OpenSSL 3).
- Constantes de preprocesado movidas a `DefineConstants` incondicionales (los PGs condicionales no cubrían `-c DebugLinux`); `LangVersion 7.3` en NRefactory.CSharp/CorApi2/Mono.Debugging.Soft/Win32 (la palabra clave `field` de C# 13/14 rompe fuentes legacy).
- Referencias a paquetes vía HintPath a `$(NuGetPackageRoot)`; NRefactory.CSharp con `System.CodeDom` 5.0.0 `PrivateAssets=runtime`.

**Fixes mínimos en fuentes** (archivos dentro de los submódulos, commits en rama `net10`):
- NRefactory: `UnresolvedSecurityDeclarationBlob.cs` — `typeof(PermissionSetAttribute)` (CAS, eliminado de .NET 10) → `ReflectionHelper.ParseReflectionName(…)`; capa codegen/driver del fork mcs con guards `#if !NET` sobre `AddDeclarativeSecurity`, `DefineDynamicModule`, `DefineUnmanagedResource`, `Builder.Save`, `AssemblyBuilderAccess` (→ `RunAndCollect`), etc.
- debugger-libs: eliminados `using System.Runtime.Remoting.Messaging;`; `AsyncResult` y `RemotingServices.Disconnect` guardados con `#if !NET` / `#else throw PlatformNotSupportedException` (coherente: el APM `Begin*` lanza `PlatformNotSupportedException` en net10).

**Validación** (`--no-incremental`): los 9 compilan con 0 errores (warnings de deprecación OK).

**Pendiente / notas**: `MonoDevelop.GtkCore.csproj` (Sdk + xmlns) requiere corrección por el agente principal; caminos APM de `VirtualMachineManager` lanzan `PlatformNotSupportedException` en runtime net10 (limitación de .NET); los proyectos compilan a `bin\DebugLinux\net10.0\`.

## 2026-10-23 (b) — Lote 2: conversión de 5 proyectos a net10.0 + cierre de olas 1-3 (`Main.sln` a 0 errores)

**Qué se hizo**: 5 proyectos pasaron a SDK-style `net10.0` (`dotnet build -c DebugLinux`). El código legacy no se borra: se convierte en el sitio. Submódulos modificados (commit + push en rama `net10` + puntero): `external/guiunit`, `external/Xamarin.PropertyEditing`.

| # | Proyecto | Tipo | Resultado |
|---|---|---|---|
| 1 | `fsharpbinding/MonoDevelop.FSharp.Gui.csproj` | old-style net472 → SDK | **OK, 0 errores** |
| 2 | `guiunit/src/framework/GuiUnit_NET_4_5.csproj` (submódulo) | old-style v4.5 Exe → SDK | **OK, 0 errores** |
| 3 | `MonoDevelop.MSBuildResolver.csproj` | old-style v4.6.1/v4.7.2 → SDK | **OK, 0 errores** |
| 4 | `Xamarin.PropertyEditing.csproj` (submódulo) | SDK netstandard2.0 → net10.0 | **OK, 0 errores** |
| 5 | `MonoDevelop.TextTemplating.csproj` | old-style net472 → SDK | **Verificado 0/0 en aislado; el build quedó bloqueado por upstream (resuelto al cerrarse la cadena Xwt/MonoDevelop.Ide en el cierre de ola 1)** |

**Decisiones / notas técnicas**:
- Cabecera: proyectos que importan `MonoDevelop.props` usan `<Project DefaultTargets="Build" Sdk="…" xmlns="…">` (correcto en el SDK 10.0.401); `GuiUnit` (sin import) usa `<Project Sdk="…">` conservando sus props.
- `TargetFramework net10.0`, `MDFrameworkVersion v10.0`, `EnableDefaultItems=false`, `EnableDefaultCompileItems=false`, `GenerateAssemblyInfo=false` (excepto FSharp.Gui).
- gtk-sharp 2.12 (FSharp.Gui, TextTemplating): `<Import Project="$(ReferencesGtk)" />`. `Mono.Posix` (FSharp.Gui) vía `$(MonoFrameworkNet45Directory)/Mono.Posix.dll` + `Private=False`.
- MSBuildResolver: HintPath `$(MSBuildBinPath)\Microsoft.Build*.dll`. TextTemplating: ref directa a `mono.texttemplating/1.3.1/lib/net45/` de la caché (el `PackageReference` net45-only da NU1202 en net10.0) + `System.CodeDom` 5.0.0.
- GuiUnit: `DefineConstants` incondicionales `NET_4_5;CLR_4_0;NUNITLITE` + `PublicSign` + `NoWarn SYSLIB0011` (`BinaryFormatter`).

**Fixes mínimos en fuentes**: guiunit `TestExecutionContext.cs` (eliminado `using System.Runtime.Remoting.Messaging;`); Xamarin.PropertyEditing — ambigüedad CS0104 nueva en net10.0 (`System.Collections.Generic.OrderedDictionary` vs `Cadenza.Collections.OrderedDictionary`), se cualificó el tipo Cadenza en 4 ficheros; TextTemplating `TextTemplatingFileGenerator.cs` (`CallContext` no existe en net10.0 → se dejó el comportamiento real, warning + no-op).

**Validación**: 1-4 → 0 errores; el 5 → 0/0 en aislado (14 fuentes contra las dlls net10.0 stageadas).

### Cierre de olas 1-3 (2026-10-24) — `Main.sln` a 0 errores

**Estado final del gate**: `dotnet build main/Main.sln -c DebugLinux` → **0 Errores** (incremental + `--no-incremental` clean-room; 97 proyectos para `DebugLinux`, el resto Windows/Mac-only excluidos vía ausencia de `Build.0`).

**Hallazgo clave — `WindowsPlatform` no bloquea el gate**: los 4 proyectos Windows-only llevan `ActiveCfg` para `DebugLinux` pero **sin línea `Build.0`** en `Main.sln` → el gate no los compila. Los "9 errores" de `WindowsPlatform.csproj` en aislado venían de una config no-Win32. Igual para los 5 Mac-only.

**Fixes del gate**: `MonoDevelop.GtkCore.GuiBuilder/CombinedDesignView.cs` — `TextEditor` (CS0118) cualificado a `MonoDevelop.Ide.Editor.TextEditor`; `MonoDevelop.GtkCore/GtkDesignInfo.cs` — `RemotingServices.Disconnect/Marshal` (CS0234) → `#if !NET`; `MonoDevelop.Startup.Avalonia` — `MSB4018` era un flake por nodos MSBuild huérfanos (al limpiar → 0, no es código).

**QA Senior (§18.5)** (headless `xvfb-run -a`, logs `~/opencode/migrate/qa-senior/`): el subagente re-ejecutó el gate de forma independiente → 0 Errores (exit 0); Avalonia por defecto (`--menudump`) OK (add-ins `loaded=True`, 0 FATAL/Unhandled); Gtk legacy (`--old-gui`) OK (proceso vivo, 0 FATAL/Exception); Limpieza OK. **Veredicto: APROBADO, 0 hallazgos de código.**

**Infraestructura compartida** (usada en toda la migración): `main/msbuild/LegacyReferenceAssemblies.targets` (target `PreferLegacyReferenceAssemblies` — resuelve el conflicto RAR "stub net10.0 vs reference net472"); `EnsureLegacyReferenceStaging` en `main/Directory.Build.targets` (copia las 5 facades net472 a `main/build/`); `DownloadNupkg.cs` (omite `nuget.exe` si los paquetes ya están extraídos — esta máquina no tiene mono); Xwt (submódulo): target `RemoveExtraWpfThemeAssemblies` (deja solo `Aero` en WPF, CS0433) + `PublicSign` en Xwt.WPF/Xwt.Gtk.Windows.

**Estado de ola 1 al cierre** (todo 0 Errores): Lote 1 (nrefactory x3, debugger-libs x6); xwt (Xwt, Xwt.Gtk, Xwt.WPF, Xwt.Gtk.Windows); mono-addins (4 proyectos); Lote 2 (FSharp.Gui, GuiUnit, MSBuildResolver, Xamarin.PropertyEditing, TextTemplating).

**Cierre**: olas 1-3 cerradas sobre `Main.sln` a 0 errores. Quedan pendientes las ondas 2-4 del plan (62 proyectos old-style restantes, cluster `vs-editor-api`, cadena WPF, exclusión Mac-only) → resueltas en la ola 4 de abajo (2026-10-26). T7 → cerrada en la sesión de 2026-10-28.

### Oleada 4 — cierre: P1 Mono.Posix, T1 WPF, T4 Roslyn 5.9.0, T5 NuGet 6.11.2, T6 Cairo-MIG (2026-10-26)
(Los cierres de olas 1-3 — 2026-10-23/24 — quedaron en las entradas "Lote 1" y "Lote 2 + cierre de olas 1-3" de esta sección; la ola 4 se documenta también en `docs/migration-status-report.md`. )

**Qué se hizo** (79 M + 2 D + 1 carpeta nueva en main + 3 submódulos, rama `net10`):
1. **P1 — Mono.Posix → MonoUnixCompat**: nuevo compat `main/src/compat/MonoUnixCompat/`
   (facades sobre `System.IO.Unix`/P-Invoke), integrado por `ProjectReference` desde
   `MonoDevelop.Core.csproj`; eliminados `core/MonoDevelop.Core/MonoDevelop.Core/{Catalog,Unix.Catalog}.cs`;
   `Mono.Debugging.Soft.csproj` (debugger-libs) y `Mono.Addins.Gui.csproj` (mono-addins) cambian
   `<Reference Mono.Posix>` → `ProjectReference` a MonoUnixCompat.
2. **T1 — WPF net10.0-windows (opción A)**: 5 csprojs (WindowsAPICodePack Core/Shell,
   MonoDevelop.TextEditor.Wpf, WindowsPlatform, Xwt.WPF); 3/5 build limpio en Linux + 2 latentes
   preexistentes documentados (NU1102 `DiagnosticMargin`; `_wpftmp` de WindowsPlatform).
   `xwt`: `AccessibleBackend.AccessibilityHelper.cs` `#if NET47` → `#if NET47 || NET10_0`.
3. **T4 — Roslyn 4.8.0 → 5.9.0**: `RoslynVersion.props`; `CompatStubs.cs` (stubs + adaptador MEF;
   fix stub `ValueSource<T>` que rompía document-open); `MonoDevelopMetadataReferenceManager.cs`
   reescrito stream-based; null-guards de `ISolutionCrawlerRegistrationService` (eliminada en
   5.9.0); staging 5.9.0 md5-verificado en `main/build/`; `libgit2-71efcb3.so` compilado de
   `external/libgit2` (bloqueador preexistente de document-open en Gtk).
4. **T5 — NuGet 5.4.0 → 6.11.2**: `Directory.Build.props` + 10 ficheros a la API 6.11.2.
5. **T6 — Mono.Cairo (ámbito MIG)**: `HslColor.ToRgb` public, `RtfWriter` a `List<HslColor>`,
   overload Cairo muerto ×2, `MonoDevelopHexEditorStyle` a HslColor, 6 `using Cairo;` muertos;
   referencia `Mono.Cairo` retirada de HexEditor + CSharpBinding (Gtk) + Avalonia.CSharpBinding.
   Bloqueador CS0012 (C1): `using MonoDevelop.Components;` innecesario en 4 panel-files forzaba
   cargar firmas de extension methods Cairo (`HelperMethods`) → borrado del using + cualificación
   de `MonoDevelop.Components.Control` en `CreatePanelWidget()`. Residual G1 → T7.

**Cómo validar**: gate `dotnet build main/Main.sln -c DebugLinux` = 0 Errores; smoke Gtk
`--old-gui` con QaB4 (sol+doc, fatals=0, errors=0, ruta semántica 5.9.0 vía class outline:
167 refs in-process); smoke Avalonia `--sln=QaB4` (solución carga de verdad, fatals=0).
Ojo: en modo Avalonia la solución solo se abre con `--sln=<path>` (posicional no aplica).

**QA (§18.5)**: Tester QA Senior, 8 partes encadenadas con informe persistente
(`~/opencode/qa_wave4/`): Q1 gate · Q2 Gtk · Q3 Avalonia (r2) · Q4 WPF · Q5 Mono.Posix ·
Q6 NuGet · Q7 Cairo (r2) · Q8 estado repos — **APROBADO, 0 hallazgos abiertos**. Hallazgos
resueltos en el ciclo: smoke Avalonia con argumento posicional (defecto del script de QA);
estado rancio de `vs-editor-api` (`bin/Editor/Text/Def/TextData/DebugLinux` ausente → CS0234
en MonoRoslynCompat en rebuild forzado; `TextData` está en Main.sln y un build limpio lo
regenera — nota T9); copia rancia de `MonoRoslynCompat.dll` en `main/build/` (normalizada).

**Cierre**: olas 1-4 sobre `Main.sln` a 0 errores; submódulos debugger-libs/mono-addins/xwt
commiteados y pushed en su rama `net10` (forks DRavainera); punteros actualizados en el commit
de ola 4 del repo principal.

**Pendiente**: T8 (reimplementar funcionalidades quitadas, auditoría T3), T9 (higiene de
build: outputs de `vs-editor-api` desde cero; carrera runtimeconfig entre TFMs sobre
`main/build/`). T7 → cerrada en la sesión de 2026-10-28 (abajo).

## 2026-10-28 — T7: Gtk# 2.12 → GtkSharp 3.24.24.95, cierre del pipeline Gtk legacy `--old-gui`
**Qué se hizo**:
1. **Fix AtkCocoaHelper**: `ActionDelegate.Attach` guarda `g_signal_lookup(name,
   owner.NativeType.Val) != 0` (P/Invoke `libgobject-2.0`) antes de `AddSignalHandler`
   (`GLib.Signal.GetSignalId` es `private static` en GtkSharp 3). Las señales Cocoa
   (`request-actions`, `perform-*`) no existen en Linux → skip. Críticos `g_signal`:
   128→0.
2. **Fix `SetData<T>`**: eliminado el P/Invoke legado `gtk_object_set_data` (función GTK2
   + `FreeHGlobal` sobre el puntero → dangling + criticals ×40). Queda
   `gtkobject.Data[key] = data` (GtkSharp 3 lo mapea a `g_object_set_data`; no hay reader
   C del hint en GTK3; el consumidor Mac usa el overload `IntPtr` → `g_object_set_data`
   directo). Aplicado en `MonoDevelop.Components/GtkWorkarounds.cs` y
   `xwt/Xwt.Gtk/Xwt.GtkBackend/GtkWorkarounds.cs`. Críticos: 40→0.
3. **Fix resolver de native libs** (`MonoDevelopMain.cs`): `libgtk-win32-2.0-0.dll` →
   `libgtk-3` (antes `libgtk-x11-2.0`), `libgdk-win32-2.0-0.dll` → `libgdk-3`. Elimina la
   carga de GTK2 junto a GTK3 (doble registración de tipos `GtkWidget`/`GdkWindow` en el
   type system de GLib → `cannot register existing type`, `GTK_IS_WIDGET`/`GDK_IS_WINDOW`
   fallando entre librerías, cierres prematuros y document-open flaky). Verificado con
   `nm -D`: GTK3 es superset de los símbolos P/Invocados; los ausentes ya tenían fallback
   `EntryPointNotFoundException`.
4. **Staging híbrido** del layout plano: build limpio + `tar --skip-old-files` de los
   backups `~/opencode/staging_backup{,_addins}_oct26.tgz` + `cp -rn <top>/.
   AddIns/<top>/` por addin (los csprojs tienen `<Private>False</Private>` → el build
   solo emite el ensamblado propio de cada addin; sin el gap-fill,
   `FileNotFoundException` de `Microsoft.Web.XmlTransform.dll` y DotNetCore/AspNetCore/
   Packaging no cargan).

**Cómo validar**: gate `dotnet build main/Main.sln -c DebugLinux` 0 Errores;
`~/opencode/qa_t7_smoke3.sh` ×3 → ventana `QaB4 – Program.cs – MonoDevelop` en los 3,
`g_signal`=0, `gtk_object_set_data`=0, `cannot register existing type`=0, MEF=0,
fatals=0; `~/opencode/qa_t6_avalonia.sh` → ventana Avalonia + proc vivo + fatals=0.

**QA (§18.5)**: wave `~/opencode/qa_t7/`. Ronda 1: Q2 FALLO (cierre prematuro ~4 s;
layout AddIns incompleto + doble mundo GTK). Ronda 2 (tras los fixes): **PASS** — Q1
0 Errores, Q2 3/3, Q3 Avalonia OK.

**Known-issue (no bloqueo)**: ~26-37 `Critical Gtk/Gdk` (`GDK_IS_WINDOW`,
`WIDGET_REALIZED_FOR_EVENT` ×1) + `losing last reference to undestroyed window` ×7-10
en `Creating DefaultWorkbench` + 1 NRE capturado en `TextArea.OnMapped()` — familia del
contenedor-leak BXC #3801 (el workaround de glue Gtk# 2.12 no aplica a GtkSharp 3;
`FixContainerLeak` quedó stub). La app sobrevive y el documento abre. Riesgo latente:
identidad dual `IThreadingContext` (stub `MonoRoslynCompat` vs real
`EditorFeatures`) con 3 versiones de `Microsoft.VisualStudio.Threading.dll` en el árbol —
sin aparición tras el fix 3; unificación arquitectónica pendiente de decisión del
operador.

**Cierre**: commit del changeset T7 completo (536 ficheros) + submódulos xwt (24 fich) y
mono-addins (16 fich) commiteados/pushed en rama `net10` (forks DRavainera), punteros
actualizados en el commit del repo principal.

**Pendiente**: T8 (reimplementar funcionalidades quitadas, auditoría T3), T9 (higiene de
build), known-issue del contenedor-leak (dedicado) y unificación Roslyn/Threading
(decisión del operador).

## 2026-10-29 — UI legacy Gtk (`--old-gui`): corrección de defectos de editor, diálogo, NRE e iconos
Auditoría de la interfaz Gtk legacy tras T7. **Qué se hizo** (3 defectos corregidos; 2
frentes verificados como no-defecto en el build actual):
1. **Editor no pintaba el código** — dos causas:
   - `TextArea` es contenedor *windowless* en GTK3: `HasWindow = false` en el ctor +
     `base.OnRealized()/OnMapped()/OnUnmapped()` (mismo patrón que `DockContainer`,
     `SectionList`, `AnimatedWidget`). Sin esto el widget quedaba unrealizado/invisible y
     el NRE de `TextArea.OnMapped()` (known-issue T7) persistía.
   - `ExposeEventInternal` creaba un 2.º `Cairo.Context(cr.Target)` para el margen de texto
     que **no compita fiable a la ventana** (empírico: 22 rect. de prueba → solo 3 sobre
     `textViewCr`, 22/22 sobre `cr`). Fix: `RenderMargins` pinta **todos** los márgenes sobre
     `cr`; el margen de texto aísla su clip con `cr.Save()/Restore()`; `cairoArea=(0,0,w,h)`
     (el `cr` de `draw` es local al widget, no toplevel). [TextArea.cs, TextViewMargin.cs]
2. **Diálogo "Nueva solución" a 1507 px** (banda en blanco a la derecha): label `Wrap=true`
   cuyo ancho *natural* en GTK3 es el texto completo sin envolver (~843 px). Fix:
   `MaxWidthChars = 25` en `templateNameLabel`/`templateDescriptionLabel` → 908×632.
   [GtkNewProjectDialogBackend.UI.cs]
3. **NRE en `HasToplevelFocus`** al enrutar comandos: el binding precompilado hace
   `(bool)GetProperty("has-toplevel-focus")`; con `GLib.Value` vacío el unbox lanza NRE y
   abortaba el enrutado (p. ej. "Nueva solución…"). Fix: `SafeHasToplevelFocus(Gtk.Window)`
   try/catch→false en `Window.HasFocus`/`HasTopLevelFocus`. [Window.cs]
4. **Iconos de pads — VERIFICADO no-defecto**: diagnóstico temporal en
   `Xwt.Gtk/ImageHandler.cs` (añadido/retirado, submódulo `xwt` queda limpio) confirmó que
   los iconos de mime se resuelven del tema (`gnome-fs-regular` 16×16; `HasIcon`/`LoadIcon`
   OK) y **todas** las filas del árbol (17/17) pintan su icono en su sangría (x=6/24/42/58/94).
   El análisis previo por columnas fijas fallaba por la sangría variable del árbol. Sin cambio.
5. **PathBar/breadcrumb — observación**: la banda oscura superior (`(28,30,31)`) es el área
   del editor (el fixture abre `data.json` de 8 bytes → casi vacío) o un PathBar sin entradas
   (`OnDrawn` pinta fondo y nada si `leftPath` está vacío, que es el comportamiento correcto).
   Sin defecto de render confirmado.

**Cómo validar**: build 0 errores; smoke `--old-gui` con `~/opencode/qa_in3/proj/QaB4.sln` →
(1) editor con el código pintado (no solo nº de línea), (2) "Archivo → Nueva solución…" a
~908 px sin banda en blanco, (3) 0 `NullReferenceException` en el log al navegar el menú,
(4) pads con iconos en todas las filas del árbol.

**QA (§18.5)**: verificación por capturas (PIL) + log IDE (`~/.cache/.../Ide.*.log`) +
diagnóstico in-app. Build final: 0 `NullReferenceException`, 0 FATAL/Unhandled, 0 warnings
de iconos ("Unknown icon"/"Can't lookup icon"). Iconos y PathBar documentados como
no-defecto/observación (puntos 4 y 5).

**Cierre**: commit `5d50a87880` (13 ficheros: 3 fixes UI + refuerzos GTK3 windowless ya en
árbol `DockContainer`/`SectionList`/`AnimatedWidget` con `HasWindow=false`, `Gettext` ruta
locale del layout de build, `MainWindow.axaml.cs` que quita las rutas `/home/daniel/…`
absolutas del operador + docs) pushed a `agents/sub-agent-senior-dev-role-report`.
Submódulo `xwt` sin cambio (puntero invariado; el diagnóstico se añadió y retiró).

**Pendiente**: (a) confirmar PathBar con un documento largo si se considera defecto;
(b) líneas del editor con solo llaves `{`/`}` aún sin píxeles (layout de ancho 0 en glyph
único) — bajo impacto, no bloqueante.

## 2026-11-01 — UI legacy Gtk (`--old-gui`): fix del doble scroll de rueda (GtkViewport) y QA de scroll

Auditoría y corrección del scroll por rueda del editor Gtk legacy. **Qué se hizo**:

1. **Doble scroll de rueda corregido (90 → 45 px por notch)**.
   - Causa raíz: cadena `ScrolledWindow` → `Gtk.Viewport` → `MonoTextEditor`
     (IScrollable *windowless*) → `TextArea` (`HasWindow = false`). Aunque
     `MonoTextEditor` implementa `Gtk.IScrollable` a nivel C#, el binding
     precompilado de GtkSharp 3.24 falla la comprobación nativa
     `GTK_IS_SCROLLABLE` y GTK3 lo envuelve en un `Gtk.Viewport`. El
     `GtkViewport` de GTK3 (también windowless) dibuja al hijo **trasladado**
     por `(-hadj.value, -vadj.value)`, y `TextArea` además desplaza sus líneas
     por la **misma** adjustment al dibujar (empieza en la línea calculada con
     `vadj`). Ambas aplicaciones del mismo delta → el contenido se movía dos
     veces por cada paso de scroll.
   - Evidencia (diagnóstico `[D]` temporal, ya retirado): 1 notch de rueda =
     exactamente 1 `ScrollEvent` (entregado al Viewport) + exactamente 1 cambio
     de adjustment de 45 px; el render de `TextArea` con `vadj=45` arranca en la
     línea 4 (offset propio correcto); en pantalla el contenido se movía 90 px;
     el allocation de `TextArea` permanece (0,0) siempre → el segundo 45 px
     venía de la traslación del Viewport, sin otros intermediarios.
   - Fix en `DecoratedScrolledWindow.SetTextEditor` [SourceEditorWidget.cs]: las
     adjustments del Viewport quedan **claveadas a cero** (nuevos objetos
     `Adjustment(0,0,0,0,0,0)`), de modo que el Viewport no traslada al hijo y
     el offset de líneas del editor es la única fuente de scroll. Las
     adjustments del `ScrolledWindow` (compartidas con el editor y las
     barras de scroll) no se tocan.
2. **QA de scroll completo (Tester QA Senior, §18.5) — 0 hallazgos**:
   - T-secuencia: 1 notch = **45 px** netos (antes 90); mse bajo y estable.
   - Reversibilidad: 20 notches abajo/arriba → retorno al origen exacto
     (mse 0.000).
   - Repro de "editor en blanco" (drag + 4 abajo + 1 arriba): **sin blank**
     (mean de contenido 0.2058 ≈ línea base 0.205); offset neto exacto 135 px
     (3 notches netos × 45).
   - Marathon: 90 notches abajo → clamp en `upper-page = 3716 px` pintando el
     final del documento correctamente (OCR: Method240-259 + líneas 266/267);
     90 arriba → clamp en 0.
   - Proporcionalidad: la secuencia de adjustment (log `[D]` durante la QA)
     cuadra 1:1 con el offset medido en pantalla por screenshot
     (`py_offset.py`).
   - Sanity final con binario limpio (sin diagnósticos): 45 px/notch, mse 0.000
     en retorno, 0 líneas `[D]` en el log.
3. **Falsa alarma documentada — "regresión de arranque a 5 líneas"**: ese
   estado era el InfoBar de `ShowAutoSaveWarning` [SourceEditorWidget.cs] con
   el editor oculto (`TextEditor.Visible = false`), disparado por un archivo de
   autosave (`~/.cache/MonoDevelop/9.0/AutoSave/*.sav`) dejado por un `kill -9`
   de una instancia con cambios sin guardar. Sus botones (VButtonBox a la
   derecha) quedaban **fuera de pantalla** (ventana 1820 px sobre pantalla de
   1280 px) y XTest clampa el puntero al borde (verificado: pedir (1444,196)
   deja el puntero en (1279,196)). Con `XMoveWindow` se trajo el botón a zona
   visible, se clickó "_Use original file" (elimina el autosave) y el editor
   pintó las 35 líneas de forma sana (verificado por OCR). El autosave ya no
   existe; los arranques posteriores son limpios (sin asterisco en el título).
4. **Trampa de entorno documentada**: lanzar la app con el entorno de shell
   heredado (`WAYLAND_DISPLAY=wayland-0` + `gnome-shell` vivo) hace que GTK se
   conecte a Wayland en lugar de Xvfb :99 → sin ventana ("colgado"). Receta
   obligatoria para lanzamientos manuales: `env -i … DISPLAY=:99
   GDK_BACKEND=x11 WAYLAND_DISPLAY= FONTCONFIG_FILE=~/opencode/fontconfig/c_only.conf
   dotnet main/build/MonoDevelop.AvaloniaShell.dll --old-gui <doc>`.

**Cómo validar**: build 0 errores (`dotnet build main/src/addins/MonoDevelop.SourceEditor2/MonoDevelop.SourceEditor.csproj -c DebugLinux`);
arranque `--old-gui` con el fixture `~/opencode/qa_scroll/LongDoc.cs` (266 líneas);
`bash ~/opencode/qa_scroll/qa_scroll_check.sh` (T-secuencia, reversibilidad, repro
blank) y `bash ~/opencode/qa_scroll/qa_marathon.sh` (maraton y clamps). Medición de
offset: `~/opencode/probe_sw/py_offset.py antes.png despues.png` (s>0 = scroll
abajo; sano: 45 px por notch).

**Pendiente**: (a) el "flood" de smooth-scroll emulado (un notch como ráfaga de
eventos Smooth en Xvfb/XTEST) no se reprodujo en las últimas sesiones de QA
(220/220 notches llegaron discretos); el burst-cap defensivo (3 líneas / 120 ms
en `ClampSmoothBurst`) se mantiene; (b) XRecord sigue inutilizable en este Xvfb
(`XRecord_BadCategory` 154/146.5) — el origen de eventos se trazó con el
interceptor XCB (v15) y los diagnósticos in-app; (c) pendiente heredada de
2026-10-29: líneas con una sola llave `{`/`}` sin píxeles (bajo impacto).

## 2026-11-02 — UI legacy Gtk (`--old-gui`): 3 defectos de render vs la referencia Windows (fondos de pads, ventana de estado 600 px, barra superior)

Auditoría de render `--old-gui` contra la referencia de Windows `~/opencode/ide_win.png`
(1920×1009); captura actual 1820×930 en Xvfb :99 (fixture `~/opencode/qa_b4/proj/QaB4.sln`).
Se corrigieron 2 defectos reales (a y b), se cerró 1 como no-defecto (c) y se documentaron
4 diferencias restantes conocidas.

**Qué se hizo / por qué / archivos** (8 ficheros en `main/` + 1 fix por submódulo):

1. **(a) Fondos de pads (43,43,43) = `#2B2B2B` — CORREGIDO y verificado.** El cuerpo del
   ProjectPad (izq) y del PropertyPad (der) pintaba (82,82,82)/(51,51,51) en vez del color
   base (43,43,43). Tres causas acumuladas del port GTK2→GTK3:
   - `Styles.LoadStyle`: en GTK2 `GetStyleBackgroundColor(Normal)` de un widget plano
     devolvía el color *base* del gtkrc; en GTK3 el fondo de un widget plano es el color de
     ventana (51,51,51) → `BaseBackgroundColor` quedaba igual que `BackgroundColor`. Fix:
     leer el color base de un `Gtk.TreeView` (widget de color base), como leía el color
     "base" del gtkrc en GTK2. [Styles.cs]
   - `DefaultWorkbench.LoadDockStyles`: en GTK2 el área del árbol (widget con ventana)
     pintaba siempre el color base del tema, así que el override de `TreeBackgroundColor`
     no tenía efecto visible; en GTK3 el árbol sí respeta el override y se pintaba con el
     color del pad. Fix: `style.TreeBackgroundColor = Styles.BaseBackgroundColor` para
     ProjectPad/ClassPad. [DefaultWorkbench.cs]
   - `PropertyGrid` (right pad): `PackEnd(vpaned, false, false, 0)` — en GTK3 un hijo con
     expand=false solo recibe su tamaño natural (32 px sin filas) y el cuerpo del pad
     quedaba sin pintar (se veía el fondo (82,82,82)). Fix: `PackEnd(vpaned, true, true, 0)`.
     [PropertyGrid.cs]
   Resultado (QA): cuerpo izq (43,43,43) 95,56 % (resto = texto del árbol/splitter);
   der 99,67 % + divisor vertical (75,75,75) 1 px a lo largo del body (x=1642 en la
   actual; la ref mide el mismo color y ancho en x=1742).
2. **(b) Ventana de estado 600 px centrada en el MainToolbar — CORREGIDO y verificado.**
   Enfoque anterior: `StatusArea` envuelto en un `Gtk.Alignment` con `Left/RightPadding`
   dinámico en `SizeAllocated`; en GTK3 ese padding dejaba de reasignar al hijo → la
   ventana no era 600 px centrada. Fix: [MainToolbar.cs] empaquetar `StatusArea` directo
   (`expand/fill=true`); [StatusArea.cs] nuevo `GetStatusWindow()` (ventana de 600 px en
   el centro del toplevel, coordenadas relativas al padre) + `OnSizeAllocated` reasigna
   el hijo real (`mainAlign`) a esa ventana + `OnDrawn` calcula `ChildAllocation` como
   `StatusArea.Allocation + messageBox.Allocation` (en GTK3 cada contenedor acumula su
   offset en los allocations de sus hijos) y aplica `cr.Translate(-alloc.X,-alloc.Y)`
   antes de `theme.Render` (el tema pinta en coordenadas absolutas de ventana).
   Resultado (QA): ancho 600 exacto, centro 909,5 (ventana 1820 → esperado 910, Δ=0,5);
   fill (38-39,38-39,38-39), idéntico al de la ref (ref: x661-1259, centro 960 = 1920/2);
   icono + texto de estado dentro de la ventana.
3. **(c) Fila superior sin title bar/botones — CERRADO como NO-DEFECTO.** La fila superior
   de la ref (y0-31) **no es una title bar sino la barra de menú** ("Archivo Editar Ver
   Buscar Proyecto Compilar Ejecutar Herramientas Ventana Ayuda"), sin botones de
   minimizar/maximizar/cerrar. La captura actual replica exactamente: OCR 10/10 ítems,
   0 píxeles-glifo en la mitad derecha, fondo (40,40,40) vs ref (41,41,41) (Δ=1).
   Sin cambio de código.
4. **Distribución de espacio Gtk3 (familia compartida, necesaria para 1-2 y para los
   diálogos)**: en GTK2 un `Box` repartía el espacio sobrante entre hijos sin expand; en
   GTK3 no lo hace → áreas centrales colapsadas. Fix: `Expand=true`/`Fill=true`
   explícitos en los `BoxChild`: [NewFileDialog.cs] (hpaned1 en vbox2, nameEntry en
   hbox2, vbox2 en w1); [SelectReferenceDialog.cs] (scrolledwindow2, hbox4, hpaned1,
   vbox5); [mono-addins AddinManagerDialog.cs] (scrolledwindow13/scrolledUpdates/
   scrolledGallery, notebook/hbox72/hbox65/vbox93, vbox93 en w1).
5. **ImageView**: en GTK3 el `Cairo.Context` ya llega trasladado al origen local del
   widget; sumar `Allocation.X/Y` (relativos al padre) desplazaba el icono fuera del
   clip → pintar en coordenadas locales. [ImageView.cs]
6. **xwt `CanvasBackendGtk3`**: el preferred size usaba los allocations en caché
   ((0,0,0,0) antes del primer layout, obsoletos después) y la rama de ancho usaba
   `Height` en vez de `Width` (bug preexistente). Fix: `CalcPreferredContent` pregunta
   `GetPreferredWidth/Height` a cada hijo visible y extiende hasta su offset; añadidos
   `OnGetPreferredHeightForWidth`/`OnGetPreferredWidthForHeight`.

**Cómo validar**: build 0 errores (`dotnet build main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.csproj -c DebugLinux`);
arranque `--old-gui` (receta `env -i` de la sección 2026-11-01) con el fixture QaB4;
medición PIL: (1) cuerpo de pads dominante (43,43,43) ±2, (2) ventana de estado de 600 px
centrada en 910, (3) OCR de la barra de menú 10/10 ítems sin botones a la derecha.

**QA (§18.5)**: Tester QA Senior, comparativa región a región ref vs actual (PIL + OCR,
informe y crops en `~/opencode/qa_ui4/report/`) — **APROBADO, 0 hallazgos abiertos**.
(a) PASS: izq 95,56 % / der 99,67 % (43,43,43) + divisor (75,75,75); (b) PASS: 600 px
exactos, centro Δ=0,5, fill idéntico a la ref (el spec citaba (36,36,36) como valor
aproximado; ref y actual miden (38-39)); (c) PASS: 10/10 ítems, 0 glifos de botón, fondo
exacto. 4/4 regresiones PASS (menú legible sin degradado, editor (28,30,31) 99,58 %, dock
inferior (43,43,43), sin franjas (82,82,82) horizontales extra en los pads).

**Diferencias restantes conocidas (no defectos de esta tarea)**:
- **Toolbar principal plano vs gradiente**: GTK pinta (51,51,51) plano (y28-75) frente al
  gradiente (66,66,66)→(53,53,53) de la ref Windows. Root cause confirmado con probe
  temporal (añadido y retirado): `GetStyleBackgroundColor(Normal)` del `EventBox` de
  `MainToolbar` devuelve **transparente (0,0,0,0)** en este entorno → el gradiente del
  código (`topColor → topColor.AddLight(-0.1)`) se dibuja totalmente transparente y se ve
  el fondo de ventana (51,51,51). El color exacto de la ref no es derivable del tema GTK
  (paleta del build Windows). Follow-up: fallback para `topColor.A == 0` (paleta a
  decidir).
- **Iconos del toolbar monocromos** en GTK vs a color en la ref Windows (resolución de
  iconos distinta entre toolkits). Follow-up.
- **Barra autohide izquierda** (27 px, (82,82,82)) solo en la captura actual: estado de
  layout de esta máquina (ClassPad autohide a la izquierda), no diferencia de código.
- **Mensaje de estado distinto** ("Paquetes restaurados correctamente." vs
  "Compilación: 1 error, 0 warnings"): estado de build, no defecto.

**Cierre**: 8 ficheros en `main/` + submódulos `xwt` (CanvasBackendGtk3.cs) y
`mono-addins` (AddinManagerDialog.cs) commiteados y pushed en su rama `net10` (forks
DRavainera); punteros de submódulos actualizados en el commit del repo principal
(rama `agents/sub-agent-senior-dev-role-report`).

**Pendiente**: (a) gradiente del toolbar principal (decisión de paleta para el fallback
transparente); (b) iconos a color del toolbar; (c) heredadas de 2026-10-29 (líneas con
una sola llave `{`/`}`) y 2026-11-01 (flood de smooth-scroll, XRecord).

## 2026-11-02 (2) — UI legacy Gtk (`--old-gui`): segunda tanda — posición del icono de estado y barra de título (título + botones)

Segunda tanda de la misma auditoría contra `~/opencode/ide_win.png`. Captura de trabajo:
1826×980 en Xvfb :99 (fixture `~/opencode/qa_b4/proj/QaB4.sln`; WID 0x200008). Dos
defectos corregidos y verificados: (d) posición del icono púrpura de estado, (e) barra
de título con título y botones minimizar/maximizar/cerrar.

**Qué se hizo / por qué / archivos** (2 ficheros en `main/`):

1. **(d) Icono púrpura de estado al borde derecho de la ventana 600 px — CORREGIDO y
   verificado.** El icono se pintaba en el borde derecho del GdkWindow de la toolbar
   (x~1800) en vez del borde derecho de la ventana de 600 px centrada (x1213). Root
   cause: en este binding GtkSharp `Widget.SizeAllocate`/`Widget.Allocation` operan en
   **coordenadas absolutas de la ventana** (el GdkWindow del EventBox de la toolbar),
   no relativas al padre; el código anterior hacía de menos/de más `alloc.X/Y`
   (tratándolas como relativas). Fix [StatusArea.cs]: `OnSizeAllocated` pasa
   `GetStatusWindow()` tal cual a `mainAlign.SizeAllocate(box)`; `OnDrawn` usa
   `messageBox.Allocation` tal cual para `ChildAllocation` (ya es absoluto) y conserva
   `cr.Translate(-alloc.X,-alloc.Y)` porque el tema pinta en coordenadas de ventana.
   Verificado (app35/app44): clúster púrpura x1194-1205, y91-102 = borde derecho de la
   ventana 600 px (613-1213); 13 px, idéntico app35→app44.
2. **(e) Barra de título con título + botones — CORREGIDO y verificado.** Sin un WM que
   pinte decoraciones (Xvfb) la banda superior de la ventana quedaba negra y vacía; y en
   este binding GTK no genera los botones CSD automáticamente (un HeaderBar con
   `ShowCloseButton` queda sin hijos). Dos piezas:
   - [WorkbenchWindow.cs] (ctor): headerbar manual — `ShowCloseButton=false`,
     `DecorationLayout="minimize,maximize,close"`, título = `Gtk.Label` centrado como
     `CustomTitle` (sincronizado con `Title` por un poller `GLib.Timeout.Add(300)`: en
     este binding no existe signal `TitleChanged`, `Title` no es virtual — verificado
     por reflexión — y `PropertyNotifyEventArgs` no expone la PSpec para filtrar), y
     tres botones `AddTitleBarButton` (`window-{minimize,maximize,close}-symbolic`,
     classes `titlebutton`+`flat`, tooltip en español, acciones `Iconify()` /
     `Maximize()`/`Unmaximize()` / `Close()`; `PackEnd` en orden close→max→min para
     dejar [min,max,close] izquierda→derecha). `Titlebar=headerbar` +
     `headerbar.ShowAll()`, porque la ventana se muestra con un simple Show sin cascada
     (`Workbench.Present()` → `RootWindow.Visible=true`+`Deiconify`).
   - [WorkbenchWindow.cs] (`ForAll`): **el root cause del "headerbar vacío"**. El
     override C# (itera `Child`+`topLevels`, nunca llama a base) sustituye el vfunc
     `for-all` de la ventana; los mecanismos internos de GTK3 (draw/map/size) lo llaman
     vía `gtk_container_forall` con `include_internals=TRUE`, y el `gtk_window_forall`
     nativo incluye la `title_box` en esa iteración. Al omitirla, el headerbar quedaba
     sin mapear ni pintar (probe: visible=TRUE, allocation correcta, título correcto,
     0 píxeles pintados). Fix: `if (include_internals && Titlebar != null) callback
     (Titlebar);` (mismas condiciones nativas). Diagnóstico apoyado en la fuente de GTK
     3.24.43 (`gtk_window_forall`, `gtk_window_draw` — deja la banda del titlebar a
     pintar por el propio titlebar —, `gtk_container_draw` — itera por el vfunc
     `for_all` —) y con una sonda CSS de fondo rojo
     (`CssProvider.LoadFromData`+`StyleContext.AddProviderForScreen`, prioridad 1000):
     roja en el repro mínimo (sin override) y no roja en el workbench → la causa es la
     iteración, no el estilo.

Todos los probes temporales (wb_dbg.log, sonda roja, sa_dbg.log) fueron retirados;
binario verificado limpio con `strings -e l` (0 hits de strings de probe).

**Cómo validar**: build 0 errores (`dotnet build main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.csproj -c DebugLinux`);
arranque `--old-gui` con el fixture QaB4 (receta env de 2026-11-01/02); captura
`import -window <WID>` (WID vía `xwininfo -root -tree | grep -i qab4`). En la ventana
1826×980: (1) banda y1-44 = headerbar pintado: fondo (40,40,40)=#282828, título
centrado (px brillantes r,g,b>150 en la zona central, ~430; centro x912 = centro de la
ventana), 3 glyphs a x1709/1747/1785 (min~8, max~28, close~15 px); (2) clúster púrpura
x1194-1205, y91-102; (3) 0 px rojos en y1-44; (4) diff global vs el baseline anterior
limitado a y1-44.

**QA (§18.5)**: Tester QA Senior — **APROBADO, 0 hallazgos abiertos** (informe
`~/opencode/qa_ui4/report_qa_ui4.md`, crops/diffs en `~/opencode/qa_ui4/`): D1 PASS
(clúster púrpura 13 px idéntico x1194-1205 y91-102); D2 PASS (fondo #282828 79429 px,
título 433 px centrado en x912, botones [min·línea 8, max·cuadrado 28, close·X 16] en
orden [1709,1747,1785]; pre-fix app38 = 0 px en título y botones); regresiones R1-R5
PASS (menubar 806 px, toolbar 53090 px (51,51,51), púrpura idéntico, diff global
app35→app44 = 902 px 100% en y1-44 y 0 en y45+, nombre WM "QaB4 – types.ts –
MonoDevelop"). Pendiente opcional (no bloquea): cambio de glyph maximizar↔restore, no
verificado para no romper el estado capturado.

**Diferencias restantes conocidas**: las de la tanda anterior de 2026-11-02 siguen
vigentes (toolbar plano vs gradiente, iconos monocromos, barra autohide, mensaje de
estado).

**Cierre**: 2 ficheros en `main/` (WorkbenchWindow.cs, StatusArea.cs) commiteados y
pushed en la rama `agents/sub-agent-senior-dev-role-report` (sin cambios de
submódulos).

**Pendiente**: (a) gradiente del toolbar principal (decisión de paleta); (b) iconos a
color del toolbar; (c) heredadas de 2026-10-29 y 2026-11-01; (d) glyph maximizar/restore
no verificado en QA (opcional).

## 2026-11-02 (3) — UI legacy Gtk (`--old-gui`): 7 defectos de diálogos vs ref Windows

Tercera tanda de la auditoría de UI legacy-Gtk. Se corrigieron y verificaron 7 defectos
de diálogos (10 puntos de validación): Nueva solución (filas/iconos + botones),
Extensiones → Galería, Acerca de, Directivas, NuGet, Opciones de solución → Encabezado
estándar y Preferencias (Depurador, Encabezado estándar, NuGet → Orígenes).

**Qué se hizo / por qué / archivos** (7 ficheros en `main/` + 1 fichero en el submódulo
`main/external/mono-addins`):

1. **Nueva solución — filas pequeñas / iconos recortados (1a) y botones (1b) —
   CORREGIDO y verificado.**
   - `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Projects/GtkTemplateCellRenderer.cs`:
     en GTK3 la altura de fila se toma de `get-preferred-height` /
     `get-preferred-height-for-width`, no solo del vfunc legacy `OnGetSize`. Se añadieron
     `OnGetPreferredHeight` y `OnGetPreferredHeightForWidth` devolviendo
     `icono + 2*Ypad` cuando hay icono (o el padding de cabecera de grupo/recente si no
     hay), de modo que la fila deja de quedar a la altura del texto (~18 px) y recortar
     los iconos de plantilla de 32 px.
   - `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Projects/GtkNewProjectDialogBackend.UI.cs`:
     el grupo `previousNextButtonBox` se packea con `PackEnd` en la barra inferior y los
     botones `Anterior`/`Siguiente` con `PackStart` dentro del grupo, para que queden a
     la derecha del diálogo (Cancelar a la izquierda). Antes el grupo entero se packeaba
     con `PackStart` y quedaba pegado a Cancelar.
2. **Herramientas → Extensiones → Galería: “listbox corto” (2) — CORREGIDO y
   verificado.**
   - `main/external/mono-addins/Mono.Addins.Gui/Gui/Mono.Addins.Gui.AddinManagerDialog.cs`:
     `DefaultWidth=920` / `DefaultHeight=640` (antes 700x550, que dejaba el árbol de la
     galería corto) y `Expand=true; Fill=true` en los tres paneles lista/info
     (`hbox2[vbox6]`, `boxUpdates[vboxUpdates]`, `hbox8[vboxGallery]`), porque en GTK3 un
     hijo de `Gtk.Box` sin `Expand` no recibe el espacio sobrante.
3. **Ayuda → Acerca de: versión de GTK# (3) — CORREGIDO y verificado.**
   - `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide/IdeVersionInfo.cs`: nuevo
     `GetGtkSharpVersion()` (versión del assembly de `Gtk.Global`) y la línea de versión
     pasa a imprimir `GTK# <ver> (<tema>)` en vez de `GTK+ <ver>`.
4. **Editar → Directivas...: panel izquierdo (4) — CORREGIDO y verificado.**
   - Se revalidó el diálogo compartido de opciones (`Dialogs.Options.Categories`) a
     tamaño completo del panel izquierdo (`260x581` en un diálogo de 960x680). No requirió
     cambio propio adicional más allá de los fixes de paneles compartidos de esta tanda.
5. **Proyecto → Administrar paquetes NuGet...: panel derecho corto (5) — CORREGIDO y
   verificado.**
   - `main/src/addins/MonoDevelop.PackageManagement/MonoDevelop.PackageManagement.Gui/ManagePackagesDialog.UI.cs`:
     `packageInfoContainerVBox` pasa a `VerticalPlacement = WidgetPlacement.Fill` y
     `HorizontalPlacement = WidgetPlacement.Fill` (antes placement `Start`, que dejaba el
     panel de info colgado arriba sin rellenar el área).
6. **Proyecto → Opciones de la solución → Encabezado estándar (6) — CORREGIDO y
   verificado.**
   - `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.StandardHeader/StandardHeaderPolicyPanel.cs`:
     el ancho de 200 px del árbol de plantillas se pone en
     `GtkScrolledWindow1.MinContentWidth` (no en `TreeView.WidthRequest`, que en un
     `ScrolledWindow` deja el child a ~0 px) y el `headerText` pasa a
     `WrapMode.WordChar` para que el editor no fuerce un ancho mínimo excesivo.
7. **Editar → Preferencias... (7a/7b/7c) — CORREGIDO y verificado.**
   - `main/src/addins/MonoDevelop.Debugger/MonoDevelop.Debugger/DebuggerOptionsPanelWidget.cs`
     (7a Depurador): se elimina el `HeightRequest` que forzaba el recorte vertical de
     checkboxes/labels.
   - `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.StandardHeader/StandardHeaderPolicyPanel.cs`
     (7b Encabezado estándar): mismo fix que en (6), revalidado en su segundo consumidor.
   - `main/src/addins/MonoDevelop.PackageManagement/Gui/MonoDevelop.PackageManagement.PackageSourcesWidget.cs`
     (7c NuGet → Orígenes): `packageSourceListHBox[GtkScrolledWindow]` y
     `mainVBox[packageSourceListHBox]` con `Expand=true; Fill=true`, para que la tabla de
     fuentes rellene el área vertical.

**Cómo validar**: build 0 errores (`dotnet build main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.csproj -c DebugLinux`
y `dotnet build main/src/addins/MonoDevelop.PackageManagement/MonoDevelop.PackageManagement.csproj -c DebugLinux`);
arranque `--old-gui` con QaB4 en Xvfb :99; capturas `import -window <WID>` y extents Atspi.
En la sesión: (1a) `newsol4.png` 904x632, md5 estable `30ec4b82894004c6fc2a1da9ba7231db`,
iconos en runs y104-133/138-167/172-201 (~29-32 px); (1b) OCR `newsol5.png`: Cancelar
x40 y580, Anterior x706 y581, Siguiente x810 y581; (2) `addins_gallery3.png` 920x640,
`galleryTreeView` `@524,291 589x473`; (3) About muestra `GTK# 3.24.24.95 (Win11-round-Dark theme)`;
(4) `directivas2.png` 960x680, `Dialogs.Options.Categories @480,249 260x581`; (5)
`nuget2.png` 886x528, panel derecho `355x418` / scroll `354x366`; (6) `solopts4.png`
960x680, `StandardHeaderPanel.HeaderText @811,300 406x418` y `TemplateTree @1225,300 200x418`;
(7a) `prefs_debugger2.png` 960x687, md5-ídento en vivo; (7b) `prefs_header2.png` 960x687,
`HeaderText 438x425`, `TemplateTree 200x425`; (7c) `prefs_sources2.png` 960x687, tabla de
orígenes `646x512`.

**QA (§18.5)**: Tester QA Senior — **APROBADO, 0 hallazgos funcionales abiertos**
(informe `~/opencode/qa_ui5/report_qa_ui5.md`): 10/10 puntos PASS (1a, 1b, 2, 3, 4, 5, 6,
7a, 7b, 7c); builds IDE/PM 0 errores; sin regresiones en la ruta `--old-gui`. Los únicos
hallazgos eran procedimentales (cambios sin commitear) y quedan resueltos por este cierre.

**Cierre**: 7 ficheros en `main/` + el submódulo `main/external/mono-addins`
(`Mono.Addins.Gui/Gui/Mono.Addins.Gui.AddinManagerDialog.cs`) commiteados y pushed (rama
`net10` del submódulo + rama `agents/sub-agent-senior-dev-role-report` del repo principal,
con puntero de submódulo actualizado).

**Pendiente**: las diferencias restantes conocidas de las tandas anteriores (gradiente
del toolbar, iconos a color, barra autohide, glyph maximizar/restore opcional) siguen
vigentes; no están en el alcance de esta tanda.

## 2026-11-02 (4) — UI legacy Gtk (`--old-gui`): cierre de residuos Addins/NuGet

Cierre de los 3 residuos funcionales/visuales detectados después de la tanda de 7
defectos: búsqueda de Addins, altura de la fila de paquete en Addins y recorte/solape
del panel derecho de NuGet.

**Qué se hizo / por qué / archivos**:
1. **Herramientas → Extensiones: búsqueda arriba a la derecha — CORREGIDO.**
   - `main/external/mono-addins/Mono.Addins.Gui/Mono.Addins.Gui/AddinManagerDialog.cs`:
     se elimina el enfoque frágil de `RepositionFilter()` + `filterEntry.SetAllocation()`
     sobre el `notebook` (inestable en GtkSharp 3.24 y no dejaba el campo visible en la
     barra superior). Ahora `InsertFilterEntry()` crea un `HBox filterBar`, packea un
     `Label` expansible a la izquierda y el `SearchEntry` con `PackEnd`, e inserta ese
     `filterBar` en `vbox93` en la posición 0 (antes del `notebook`/`hbox65`). El
     `TextChanged` sigue filtrando `tree`, `galleryTree` y `updatesTree` y expandiendo
     las vistas instaladas/galería.
2. **Herramientas → Extensiones → Instalados: fila de paquete a altura completa —
   CORREGIDO.**
   - `main/external/mono-addins/Mono.Addins.Gui/Mono.Addins.Gui/AddinTreeWidget.cs`:
     el nombre del paquete pasa a usarse con `Ellipsize = None`, `WrapMode = Word`,
     `MaxWidthChars = 45` e `YPad = 2` en el `CellRendererText` principal; el icono
     añade `YPad = 2`; y la columna de versión usa un `CellRendererText` propio
     (`Ellipsize = End`, `YPad = 2`) para no compartir el renderer envuelto con la
     columna de nombre. La fila expandida muestra nombre + descripción y mide 53 px.
3. **Proyecto → Administrar paquetes NuGet: panel derecho sin recorte/solape —
   CORREGIDO.**
   - `main/src/addins/MonoDevelop.PackageManagement/MonoDevelop.PackageManagement.Gui/ManagePackagesDialog.UI.cs`:
     `packageInfoContainerVBox` queda en `VerticalPlacement = Start` y
     `HorizontalPlacement = Fill`; el `packageInfoScrollView` usa
     `HorizontalScrollPolicy = Never` y `VerticalScrollPolicy = Automatic`. Con ello el
     contenido del panel derecho se desplaza dentro de su viewport y no invade la
     barra de acciones inferior.

**Build/staging**:
- `dotnet build main/external/mono-addins/Mono.Addins.Gui/Mono.Addins.Gui.csproj -c Debug`
  → EXIT=0. El build directo de `Mono.Addins.Gui` NO actualiza automáticamente
  `main/build/Mono.Addins.Gui.dll`; se copió manualmente desde
  `Mono.Addins.Gui/bin/net10.0/` a `main/build/`.
  - `main/build/Mono.Addins.Gui.dll` md5 `b0e2f715a12f6b6f3b13753820fd7759`
  - `main/external/mono-addins/Mono.Addins.Gui/bin/net10.0/Mono.Addins.Gui.dll` md5
    `b0e2f715a12f6b6f3b13753820fd7759`
- `dotnet build main/src/addins/MonoDevelop.PackageManagement/MonoDevelop.PackageManagement.csproj -c Debug`
  → EXIT=0, stageado a `main/build/AddIns/MonoDevelop.PackageManagement/`.
  - `main/build/AddIns/MonoDevelop.PackageManagement/MonoDevelop.PackageManagement.dll`
    md5 `eb47b040936a5fc67a004c7d2e2eaa1e`

**Cómo validar / evidencia final**:
- Limpieza de procesos antigua (el GUI real es un hijo `MonoDevelop.dll`, no solo el
  padre AvaloniaShell):
  `pkill -f '[M]onoDevelop.dll'; pkill -f '[M]onoDevelop.MSBuildBuilder.exe'; pkill -f '[M]onoDevelop.AvaloniaShell.dll'`.
- Arranque final `--old-gui` con `QaB4.sln` en Xvfb `:99` (log `~/opencode/qa_ui5/app16.log`).
- Addins (ventana `0x200770`, 920x640):
  - barra superior con búsqueda: `filler @14,14 892x29`, panel de búsqueda
    `@692,14 208x29`, campo `@694,16 168x25`, iconos `@863,17 16x23` y `@881,17 16x23`.
  - fila expandida en Instalados: `table cell @82,123 529x53`, texto
    `GDB Debugger Support` + descripción `@114,123 557x53`.
- NuGet (ventana `0x2009b7`, 980x640):
  - viewport derecho `@626,54 354x478` (termina en y≈532);
  - `Nueva versión:` `@640,545 95x20` y combo `13.0.4 (última versión estable)`
    `@750,545 215x20`;
  - barra de acciones `@8,596 958x34` con `Add Package @846,596` y `Cerrar @721,596`;
    sin solape entre el contenido del panel derecho y la barra inferior.

**QA (§18.5)**: Tester QA Senior — **APROBADO, 0 hallazgos funcionales abiertos** para
los 3 residuos. Informe local actualizado en `~/opencode/qa_ui5/report_qa_ui5.md`.
Limitación residual no funcional: la pestaña Galería depende del repositorio remoto
`https://lastexitcode.com/monadevelop-addins/9.0/main.mrep`, que falla en el entorno de
prueba; la altura de fila se validó en Instalados, que usa el mismo `AddinTreeWidget`.

**Cierre**: submódulo `main/external/mono-addins` commiteado y pushed en `net10`
(commit `8f1a15a`); repo principal commiteado y pushed en
`agents/sub-agent-senior-dev-role-report` con el puntero del submódulo actualizado y
`ManagePackagesDialog.UI.cs` + docs.

**Pendiente**: las diferencias restantes conocidas de las tandas anteriores (gradiente
del toolbar, iconos a color, barra autohide, glyph maximizar/restore opcional) siguen
vigentes; no están en el alcance de esta tanda.

## 2026-11-03 — UI legacy Gtk (`--old-gui`): alturas de celdas — texto visible en Addins (Galería/Instalados) y NuGet

Continuación de la auditoría de UI legacy-Gtk. Objetivo del operador: que el texto de
las celdas/listboxes sea totalmente visible (sin recortes). Esta tanda corrige las
alturas restantes: renderers de celdas del árbol (XWT + Mono.Addins.Gui), el combo
"Repositorio:" de Galería (un GtkComboBox puro, no XWT) y las filas de metadatos del
panel derecho del diálogo NuGet.

**Qué se hizo / por qué / archivos**:
1. **XWT (submódulo `main/external/xwt`, rama `net10`)** — `Ypad = 4` en los
   `Gtk.CellRendererText` de los backends, para que las celdas de texto en
   `GtkTreeView` tengan relleno vertical:
   - `Xwt.Gtk/Xwt.GtkBackend/ComboBoxBackend.cs`: renderer por defecto del ComboBox.
   - `Xwt.Gtk/Xwt.GtkBackend/ListBoxBackend.cs`: renderer por defecto de la columna
     del ListView.
   - `Xwt.Gtk/Xwt.GtkBackend.CellViews/CustomCellRendererText.cs`: renderer de celdas
     XWT usado por las cell views custom.
2. **Mono.Addins.Gui (submódulo `main/external/mono-addins`, rama `net10`)**:
   - `Mono.Addins.Gui/Mono.Addins.Gui/AddinManagerDialog.cs`: el combo "Repositorio:"
     de Galería es un **GtkComboBox puro** (no pasa por XWT): se añade `crt.Ypad = 6`
     a su `CellRendererText` (~línea 100).
   - `Mono.Addins.Gui/Mono.Addins.Gui/AddinTreeWidget.cs`: `Ypad = 2` → `Ypad = 6`
     en los 3 renderers del árbol de Addins (pixbuf, nombre, versión).
3. **PackageManagement (repo principal)** — `MinHeight` en las filas del panel
   derecho del diálogo NuGet:
   - `main/src/addins/MonoDevelop.PackageManagement/MonoDevelop.PackageManagement.Gui/ManagePackagesDialog.UI.cs`:
     `MinHeight = 28` en los 8 HBoxes de metadatos (ID/Autor/Fecha/Descargas/Licencia/
     Página/Dependencias/Versión actual) y `MinHeight = 80` en la lista de dependencias;
     `MinHeight = 28` en `packageVersionComboBox`.
   - `main/src/addins/MonoDevelop.PackageManagement/MonoDevelop.PackageManagement.Gui/ManagePackagesDialog.cs`:
     `MinHeight = 60` en `projectsListView` (ListView XWT de la página Consolidate).

**Lecciones operativas (pitfalls del build)**:
- El output real de `Xwt.Gtk` es `Xwt.Gtk/bin/Debug/net10.0/Xwt.Gtk.dll` (targetea
  net10.0). El `Xwt.Gtk/bin/Debug/Xwt.Gtk.dll` es obsoleto (17 oct, 326144 bytes) y
  NO debe stagearse.
- `git diff --check`: los `.UI.cs` generados llevan finales CRLF; al editar se
  normalizan las líneas tocadas a LF (28 líneas en esta tanda).
- `AddinManagerDialog.cs` usa tabs de sangría; `ComboBoxBackend.cs` usa 4 tabs.

**Build/staging** (todos con 0 errores, `dotnet build -c Debug`):
- `main/external/xwt/Xwt.Gtk/Xwt.Gtk.csproj` (rebuild con `-t:Rebuild`).
- `main/external/mono-addins/Mono.Addins.Gui/Mono.Addins.Gui.csproj`.
- `main/src/addins/MonoDevelop.PackageManagement/MonoDevelop.PackageManagement.csproj`.

Binarios stageados a `main/build/` (md5 verificados):
- `main/build/Xwt.Gtk.dll` → `cd09d564b80233eead0f181c86e64786`
- `main/build/Mono.Addins.Gui.dll` → `877b541d54d24fc3411ba12b8ff7561e`
- `main/build/AddIns/MonoDevelop.PackageManagement/MonoDevelop.PackageManagement.dll`
  → `84508f4c42649247b61cfb7c8285168f`

**Cómo validar / evidencia final**: arranque limpio `--old-gui` con `QaB4.sln` en Xvfb
`:99` con a11y (Atspi + OCR + perfiles de tinta; el modelo no ve imágenes). Log
`~/opencode/qa_ui5/app20.log`.
- Addins (ventana 920x640):
  - Galería: combo "Repositorio:" 21 → **28 px**, banda de tinta 11 px centrada,
    sin recorte. Celda de mensaje vacío 42 → **50 px** (banda 13 px centrada).
  - Instalados: filas de categoría 31 px; fila expandida (Debugging → GDB Debugger
    Support) 61 px con texto de 2 líneas totalmente legible y centrado.
  - Búsqueda: visible, filtra y limpia correctamente.
- NuGet (ventana 980x640):
  - Metadatos derecha: 20 → **28 px**, OCR legible completo (ID/Autor/Fecha/Descargas/
    Licencia/Página/Dependencias).
  - Combo "Nueva versión:" 20 → **32 px**, OCR legible: "13.0.4 (última versión
    estable)".
  - Sin solapes: metadatos (hasta y≈380) → caja deps (386..526) → combo (537..569) →
    barra inferior (596..630).
  - Lista izquierda de paquetes: filas 99 px legibles.
  - Prueba funcional: se instaló Newtonsoft.Json 13.0.4 en QaB4 vía "Add Package"
    (el diálogo se cierra solo al terminar).

**QA (§18.5)**: Tester QA Senior — **APROBADO, 0 hallazgos funcionales abiertos**
(informe `~/opencode/qa_ui5/report_qa_alturas.md`): 9/10 ítems OK; el 1 minor
(fila expandida 61 px vs meta estimada 50-55 px) **cerrado como aceptado**: el
50-55 era estimación propia del checklist, no especificación del operador; la spec
es "texto visible" y se cumple (2 líneas legibles y centradas, sin recorte). El 61 px
viene de `Ypad=6` + 2 líneas en la fila expandida.

**Quirks de entorno documentados (no defectos)**:
- Galería vacía: el repositorio remoto
  `https://lastexitcode.com/monadevelop-addins/9.0/main.mrep` está caído en el
  entorno de prueba (diálogo de error de red documentado). Las filas reales de
  Galería no se pudieron ver; se valida con el widget compartido (Instalados).
- La página Consolidate de NuGet se quedó en "Cargando" (el `projectsListView` XWT
  no fue validable visualmente; el mecanismo de cell renderers está validado en el
  resto de listas).

**Cierre**: submódulos `main/external/xwt` y `main/external/mono-addins` commiteados
y pushed en `net10`; repo principal commiteado y pushed en
`agents/sub-agent-senior-dev-role-report` con los punteros de submódulo actualizados,
`ManagePackagesDialog.UI.cs` + `ManagePackagesDialog.cs` y la documentación.

 **Pendiente**: los mismos que en las tandas anteriores (gradiente del toolbar, iconos
a color, barra autohide, heredadas 2026-10-29/2026-11-01, glyph maximizar/restore
opcional) — fuera del alcance de esta tanda.

## 2026-11-03 (2) — UI Avalonia: port del diálogo "New File" (File ▸ New File) desde Gtk legacy

**Contexto**: el item M10 del `interfaz-plan.md` describía `NewFile` como un stub "newN.cs" sin
archivo backing. Esta tanda lo sustituye por el diálogo "New File" **real** del legacy
(`MonoDevelop.Ide.Projects.NewFileDialog`, 718x524, paneles 192/292/*), portado a Avalonia
reciclando módulos existentes (regla 18.4: reutilización obligatoria, sin separación de carpetas).

**Qué se hizo** (archivos):
- **Nuevo** `main/src/core/MonoDevelop.Startup.Avalonia/Views/NewFileDialog.axaml` (+ `.axaml.cs`):
  réplica del layout legacy — chrome (título `New File` + `✕`), árbol de categorías
  (`FuncTreeDataTemplate<NewFileCategory>`), `ListBox` de templates (`DataTemplate` con icono
  `Image` 32x32 + `Name` + `Language`), panel de info (`TemplateTitle` bold + `TemplateDescription`),
  fila `Name:`, caja "Add to project" (checkbox + combo + `Path:` + `Browse`), botones `Cancel`/`New`.
- **Catálogo** de los 2 `FileTemplate` de `CSharpBinding.addin.xml`: `CSharpEmptyFile` ("Empty
  File") y `CSharpAssemblyInfo` ("Assembly Information" → `Properties/AssemblyInfo.cs`, excluido
  si ya existe `AssemblyInfo.cs` con proyecto).
- **Creación** (`OnNew`): BOM UTF8 + standard header (policy global) + cuerpo del template
  (sustituciones `$${Tag}`/`$if$`/`$else$`/`$endif$`); `StreamWriter(…, UTF8Encoding(true))` para
  emitir el BOM. Dos modos: con proyecto (aísta) y sin proyecto (documento *unsaved* en memoria).
- **MOD** `Views/MainWindow.axaml.cs`: `OpenNewFileDialog()`, `HandleNewFileDialogResult(dlg)`,
  `CreateUnsavedDocument(name, content)` (también usado por `--windocs`), hooks QA
  `--newfile`/`--newfile-unsaved`/`--ctxnewfile`/`--newfile-visual`.
- **MOD** `Program.cs`: allowlist +4 flags QA. **MOD** `Views/PreferencesDialog.axaml.cs`:
  `LoadGlobalPolicy` → `internal static` (para leer la policy del standard header).

**Cómo validar**: recipe `env -i … DISPLAY=:0 XAUTHORITY=… DBUS_SESSION_BUS_ADDRESS=… 
FONTCONFIG_FILE=~/opencode/fontconfig/c_only.conf timeout -k 5 22 dotnet main/build/MonoDevelop.
AvaloniaShell.dll --sln=~/opencode/TestProj/TestProj.sln <FLAG>`. Evidencia:
- `--newfile` → `[newfile] created=True bom=True bytes=3` (fichero real 3 bytes `EF BB BF`).
- `--newfile-unsaved` → `[newfile] unsavedDoc=True dirEmpty=True`.
- `--ctxnewfile` → `[ctxnewfile] created=True name=QaCtxFile.cs`.
- `--newfile-visual` → `tmplCount=2`, título/descripción poblados, `okEnabled=True`.
- 718x524 (`xwininfo`/`identify`); AT-SPI completo; iconos renderizados (PIL 57/64% vs 0%).

**Bugs corregidos en esta tanda**:
1. **`tmplCount=0`** — `CategoryTree.SelectionChanged` sin cablear (`FillTemplates` no corría) y
   `LastCategoryPath` por defecto `"General"` (requiere `"C#/General"`). Fix: cablear señal +
   defecto/fallback a `"C#/General"`.
2. **NRE al abrir** — en esta versión de Avalonia `TextBox.Text` devuelve `null` antes de
   inicializar (señal reentrante durante `InitializeView` en `OnOpened`). Fix: mover la vista
   inicial a `OnOpened` (one-shot `initialViewDone`) + null-coalescear `NameBox.Text` + null-guards
   en `OnCategorySelected`/`FillTemplates`/`OnTemplateSelected`.
3. Errores de compilación previos: `?? return null`, `KeyEventArgs`/`ToggleButtonEventArgs`,
   `walk` unassigned, `LoadGlobalPolicy` no accesible, `HierarchicalDataTemplate`→`FuncTreeDataTemplate`.

**QA (§18.5)**: Tester QA Senior — **APROBADO, 17/17 PASS, 0 hallazgos**. 4 hooks `exit=124` (sin
crasheos), bytes reales `EF BB BF`, AT-SPI (incl. expansión interactiva C#→General), 718x524,
iconos OK, build `0 Errores`, regresión `--old-gui` OK (0 archivos legacy tocados).

**Cierre**: repo principal commiteado y pushed en `agents/sub-agent-senior-dev-role-report`
(`Views/NewFileDialog.axaml` + `.cs` nuevos, `Views/MainWindow.axaml.cs`, `Program.cs`,
`Views/PreferencesDialog.axaml.cs` + docs). Sin submódulos modificados.

**Pendiente**: poblar el *body* completo del template AssemblyInfo con etiquetas autoriales y
extender el catálogo a más lenguajes (Java/F#/VB) cuando se migren sus `xft` — hoy solo C# (igual
que la shell, que no carga el host legacy de add-ins).

## 2026-11-03 (3) — UI Avalonia: alinear el borde/chrome del diálogo "New File" con el resto de diálogos

**Contexto**: el New File dialog portado arriba usa `DialogWindow.Apply(this)` (como NewSolution,
About, Preferences, AddinManager, FindInFiles), pero su Grid raíz llevaba `Margin="6"`. Como
`Apply` (`Controls/DialogWindow.cs:201`) ya envuelve el contenido en un frame `Border`
(`Margin=6, CornerRadius=8, BorderThickness=1`), ese margin extra creaba una segunda banda de 6px
del color-cuerpo que impedía que la barra de título llegara a las esquinas redondeadas superiores.
El sibling `NewSolutionDialog` (también `Apply`) no tiene margin en el Grid raíz.

**Cambios** (solo 3 margins en `Views/NewFileDialog.axaml`, sin C#):
1. Grid raíz `Margin="6"` → eliminado (título sellado a las esquinas).
2. Grid de contenido `Margin="6,8,6,0"` → `Margin="8,8,8,0"` (igualar a NewSolution, 8px).
3. Fila de acciones `Margin="0,10,0,0"` → `Margin="12,10,12,12"` (botón `New` sin pegarse a la
   esquina inferior-derecha redondeada; 13px de holgura).

**Cómo validar**: 4 hooks `exit=124` (sin regresión: `tmplCount=2`, `okEnabled=True`, ficheros
creados); AT-SPI íntegro; chrome (ARGB depth=32) con gutter **6px** transparente (`alpha=0`), arcos
8px anti-aliaseados, chrome continuo `(45,45,45)` y **sin banda de color-cuerpo**; 1:1 píxel a
píxel con el sibling `About`. Artefactos en `~/opencode/nfqa/` (`qa_border.png`, `qa_about.png`).

**QA (§18.5)**: Tester QA Senior — **APROBADO, 0 hallazgos** (6/6 PASS).

**Cierre**: repo principal commiteado y pushed en `agents/sub-agent-senior-dev-role-report`
(`Views/NewFileDialog.axaml` + docs). Sin submódulos modificados.

## 2026-11-03 (4) — UI Avalonia: diálogo New Solution — chrome real, iconos GTK y revisión de comportamiento

**Contexto**: el diálogo Avalonia `NewSolution` ya existía funcionalmente, pero su chrome estaba
comentado en XAML, usaba iconos genéricos del set de `IconService` (no los PNG reales de las
plantillas GTK) y el checkbox *Add to open solution* podía quedar visible activo sin solución
abierta. Esta tanda alinea el diálogo con el resto de diálogos Avalonia y documenta los deltas
restantes frente al wizard GTK.

**Qué se hizo** (archivos):
- `Views/NewSolutionDialog.axaml`: activó el chrome (título `New Solution` + botón `✕`) y cambió
  el valor inicial de `SolutionName` de `TestProj` a `Solution`.
- `Views/NewSolutionDialog.axaml.cs`:
  - catálogo de plantillas con los base names GTK reales (`console-project`, `library-project`,
    `test-project`, `crossplatform-shared-project`);
  - carga de iconos y preview vía `IconService.GetTemplateImage(...)`;
  - fallback de nombre `Solution`;
  - `Add to open solution` solo se habilita/marca cuando hay solución abierta o `forcedLocation`
    no nulo.
- `MonoDevelop.Ide/Services/IconService.cs`: nuevo `GetTemplateImage(baseName, scale)` que carga
  los PNG de `main/src/core/MonoDevelop.Ide/templates/images` y respeta la variante dark de
  `ActualThemeVariant`.
- `Program.cs`: allowlist QA + `--newsolution`.
- `Views/MainWindow.axaml.cs`: el hook QA `--newproject` crea ahora bajo `~/opencode/QAProj/<guid>`
  (regla 18.6: artefactos en `~/opencode/`, no en `/tmp`).
- `Views/MainWindow.axaml`: cambio de operador (renombrado `MonoDevelopIcon` → `MonoDevelopLogo` +
  comentario); no hay referencias en C# y se commitea aparte.

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.
  csproj -c Debug -f net10.0 --nologo` → `0 Errores`.
- `bash /home/daniel/opencode/ns_capture.sh`: `exit=124`, geometría `920x600`, chrome activo
  (label `New Solution` + button `✕`), AT-SPI completo (C#/F#/Other, 4 plantillas, description,
  solution name, location, 3 checkboxes, preview, Create/Cancel), 0 `NullReference|Unhandled|
  TypeLoad|DllNotFound`.
- Checkbox *Add to open solution* (AT-SPI states):
  - con solución: `CHECKED,ENABLED,FOCUSABLE,SENSITIVE,SHOWING,VISIBLE`
  - sin solución: `FOCUSABLE,SHOWING,VISIBLE`
- `bash /home/daniel/opencode/ns_run.sh`: `console`, `library`, `unittest`, `shared` →
  `created=True ... sln-entry=True mappings=True`, `sln-QAAdded-count=1`,
  `sln-mappings-count=1`, 0 excepciones.
- Regresión `--old-gui`: `exit=124`, 0 `Fatal|Unhandled|TypeLoad|DllNotFound|NullReference`.

**Deltas GTK aceptadas** (no reportadas como defectos): wizard de 2 páginas GTK vs single-page
Avalonia; sin campo separado de *Project name* (Avalonia reutiliza *Solution name*); sin *Folder
Layout Preview* en vivo; sin checkbox “.gitignore”; sin botón *Browse* junto a *Location*;
F#/Other/Recent ausentes o deshabilitados; validación completa de nombres pendiente.

**QA (§18.5)**: Tester QA Senior — **APROBADO** (8/8 PASS; informe
`~/opencode/nsqa_qa/qa_report.md`). Observaciones no bloqueantes resueltas/documentadas:
- **O1**: `MainWindow.axaml` aparecía fuera de la lista esperada; es un cambio de operador
  (renombrado del `Image` y comentario), sin efecto funcional; se commitea aparte.
- **O2**: los iconos se renderizan en variante base porque `Application.Current.ActualThemeVariant`
  no es Dark en runtime (diagnóstico temporal `theme-dark=False`, retirado después de validar);
  la variante base es la correcta para el theme efectivo. Los brushes oscuros del diálogo son
  recursos estáticos del diseño y no cambian el `ThemeVariant` de Avalonia.
- **O3**: `--old-gui` no emite stdout en la ruta Gtk (comportamiento conocido); validado con
  `exit=124` + `wmctrl`/captura en el informe QA.

**Hallazgo preexistente (no bloqueante)**: `AppendProjectToSolution` puede dejar el `.sln`
malformado tras múltiples appends sobre el mismo fixture. Las pruebas de esta tanda usan fixture
fresco por plantilla, por lo que no se trata como defecto de New Solution.

**Cierre**: repo principal commiteado y pushed en `agents/sub-agent-senior-dev-role-report`:
(1) commit separado para `Views/MainWindow.axaml` (cambio de operador); (2) commit de New Solution
(`NewSolutionDialog.axaml` + `.axaml.cs`, `IconService.cs`, `Program.cs`, `MainWindow.axaml.cs` +
docs). Sin submódulos modificados.

## 2026-11-03 (5) — UI Avalonia: Welcome Page — iconos faltantes, tile 32px y alturas independientes de paneles

**Contexto**: la Welcome Page Avalonia mostraba la barra de enlaces y los botones `New...`/`Open...`
sin icono, el tile de solución reciente usaba un icono 16px en lugar del 32px del legacy, y la fila 2
(`Solutions`/`Xamarin News`/`Did you know?`) usaba un `StackPanel` horizontal que dejaba los tres
`Border` estirados a la misma altura (patrón visual 603px). Durante el diagnóstico hubo un falso fallo:
un proceso antiguo de `MonoDevelop.AvaloniaShell.dll` seguía vivo y `wmctrl`/AT-SPI estaban midiendo su
ventana en vez de la del build nuevo; matar el proceso antiguo dejó la validación coherente.

**Qué se hizo** (archivos):
- `main/src/core/MonoDevelop.Ide/Services/IconService.cs`: `GetResourceImage` ahora busca también en
  `MonoDevelop.Ide.WelcomePage/icons` (`welcomeIconsDir`) tras el directorio principal de iconos, de
  modo que `welcome-link-md-16`, `welcome-link-info-16`, `welcome-link-support-16`,
  `welcome-link-chat-16`, `welcome-new-solution-16` y `welcome-open-solution-16` resuelven sin shadowear
  iconos del set principal.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/WelcomePageView.axaml`: la fila 2 pasó de un
  `StackPanel` horizontal a un `Grid` con `ColumnDefinitions="300,30,510,30,300"`; cada `Border`
  (`Solutions`, `Xamarin News`, `Did you know?`) queda en columnas 0/2/4 con
  `VerticalAlignment="Top"` y `Margin="0,0,0,24"`, por lo que cada panel conserva su altura de
  contenido. El margen inferior del contenedor pasó de `0,30,0,24` a `0,30,0,0` (ahora lo llevan los
  pads).
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/WelcomePageView.axaml.cs`: el tile de solución
  reciente usa icono 32px (`solution-32`, o `workspace-32` para `.mdw`) vía
  `IconService.GetResourceImage`; el margen del texto pasa de `38 - 16` a `6`, manteniendo el
  `TextLeftPadding=38` del legacy (32px de icono + 6px de gap).

**Cómo validar**:
- Build:
  `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo`
  → `0 Errores` (advertencias preexistentes).
- Captura QA: `bash /home/daniel/opencode/welcomeqa/welcome_capture.sh` (seeda un reciente `TestProj`
  en `MonoDevelopProperties.xml`, arranca `--welcome`, captura PNG/AT-SPI y restaura las properties).
  Artefactos en `~/opencode/welcomeqa/`.
- AT-SPI (`tree.txt`): fila 2 = `Grid` `1170x195`; `Solutions` Border `300x171`, `Xamarin News`
  Border `510x125`, `Did you know?` Border `300x165` (no el patrón 603).
- AT-SPI (`atspi.txt`): visibles `MonoDevelop.com`, `Documentation`, `Support`, `Q&A`, `Solutions`,
  `New...`, `Open...`, `TestProj`, `Xamarin News`, `Did you know?`.
- Iconos (PIL sobre `welcome_full.png`, coordenadas client = absolutas AT-SPI − offset de
  `geom.txt`): iconos de enlace con ~64/59/108/137 px grises `#575757`/`#585858`; `New...` 100 px
  grises; `Open...` 155 px grises; icono 32px del reciente con 388 px magenta `#cd43ce`.
- Regresión `--old-gui`: estable. Nota de entorno: en este escritorio hay que forzar
  `GDK_BACKEND=x11 WAYLAND_DISPLAY= XDG_SESSION_TYPE=x11`; si se hereda `XDG_SESSION_TYPE=wayland`,
  GTK se cuelga antes de abrir la ventana.

**QA (§18.5)**: Tester QA Senior — **APROBADO, PASS, 0 hallazgos abiertos**.

**Cierre**: repo principal commiteado y pushed en `agents/sub-agent-senior-dev-role-report`
(`IconService.cs`, `WelcomePageView.axaml`, `WelcomePageView.axaml.cs` + docs). Sin submódulos
modificados.

## 2026-11-04 — UI Avalonia: MainWindow logo monodevelop-22 y NuGet.Configuration 6.11.2

**Contexto**: se solicitó enlazar el `Image x:Name="MonoDevelopLogo"` en `MainWindow.axaml` al icono `monodevelop-22.png` y actualizar la referencia de `NuGet.Configuration` de la shell Avalonia a la familia 6.11.

**Qué se hizo** (archivos):
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs`: en `SetToolbarIcons()` se añadió `MonoDevelopLogo.Source = IconService.GetResourceImage ("monodevelop-22");`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml`: se cambió el `Grid` de la title bar de `ColumnDefinitions="*,Auto"` a `"Auto,*,Auto"` para que el logo tenga su propia columna; el `Image` pasa a `Grid.Column="0"` con `Margin="8,0,0,0"`, el `Menu` a `Grid.Column="1"` y los `CaptionButtons` a `Grid.Column="2"`.
- `main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj`: `PackageReference` de `NuGet.Configuration` actualizado de `5.4.0` a `$(NuGetVersionNuGet)` (6.11.2).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores`.
- Captura QA: `bash /home/daniel/opencode/mainwindow_logo_nuget_qa/run.sh` (arranca `--prefs=packagesources`, captura PNG del main window y del prefs dialog, y deja el log en `run.log`). Artefactos en `~/opencode/mainwindow_logo_nuget_qa/`.
- Logo: análisis PIL de `main.png` muestra ~140 px azules en la región del logo (x0-40, y0-32), confirmando que `monodevelop-22.png` está renderizado.
- NuGet Sources: `run.log` contiene `[prefs-sources] count=1` y no hay excepciones.
- `deps.json`: `NuGet.Configuration` y `NuGet.Common` en `6.11.2`.

**QA (§18.5)**: Tester QA Senior — **APROBADO, PASS, 0 hallazgos abiertos**.

**Cierre**: commit `e786a225c6` pushado a `origin/agents/sub-agent-senior-dev-role-report`.

## 2026-11-04 (2) — UI Avalonia: diálogo New Solution — paneles estilo New File, Browse en Location y sin Preview

**Contexto**: se pidió alinear el diálogo "New Solution" (File ▸ New Solution...) con el diseño de paneles del diálogo "New File", añadir un botón Browse al campo Location (selector de carpeta que escribe la ruta en el TextBox) y eliminar el panel Preview porque no es necesario.

**Qué se hizo** (archivos):
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/NewSolutionDialog.axaml`:
  - Layout de la zona central a `Grid RowDefinitions="*,Auto,Auto,Auto" ColumnDefinitions="170,3,240,3,*"` replicando `NewFileDialog.axaml`: tres paneles con `Border` de 1px (`IdeBorderBrush`) separados por `GridSplitter` de 3px — categorías | plantillas (iconos 32px) | pane de info.
  - Pane de info con el mismo patrón que New File: `DockPanel` con `TemplateTitle` en negrita + `TemplateDescription` con wrap (el título del template que antes vivía en el panel Preview).
  - Las filas de campos pasan de estar dentro del 3er panel a filas a lo ancho bajo los paneles (estilo de la fila "Name:"): "Solution name:" + `SolutionName`; "Location:" + `LocationBox` + `Button x:Name="BrowseButton" Content="Browse"`; bloque de checkboxes (`CreateDirectoryCheck`, `AddToSolutionCheck`, `GitCheck`).
  - Se eliminó la columna 4 (panel Preview: `PreviewImage` 96x96 + `PreviewLabel`). Ventana de 920x600 a 760x600.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/NewSolutionDialog.axaml.cs`:
  - `OnTemplateSelected` pinta `TemplateTitle`/`TemplateDescription` (ya no `PreviewImage`/`PreviewLabel`).
  - Nuevo handler `OnBrowseLocation`: `StorageProvider.OpenFolderPickerAsync(FolderPickerOpenOptions { AllowMultiple=false })` y escribe `folders[0].Path.LocalPath` en `LocationBox` (mismo patrón que `NewFileDialog.OnBrowse`); errores a `Console.WriteLine("[newsolution] folder picker: ...")`.
  - El tuple `Templates` pierde el campo `PreviewIcon` (sin uso).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores`.
- Captura QA: `bash /home/daniel/opencode/newsolution_dialog_qa/run.sh` (arranca `--newsolution`, captura `dialog.png` por el título "New Solution" y deja `run.log`). Artefactos en `~/opencode/newsolution_dialog_qa/`.
- OCR (tesseract + PIL 3-4x invertido): presentes "New Solution", "Console Application", "Class Library", "Solution name:", "Location:", "Browse", "Create", "Cancel" y los tres checkboxes; "Preview" NO aparece.
- PIL: ventana 760x600; bordes verticales #3A3A3A en pares (184/188, 421/431) por splitter + borde derecho x=744 (info pane hasta el borde); fila inferior con dos botones de 88px separados 8px.
- `run.log`: sin `Unhandled`/`Exception`/`TypeLoad`/`DllNotFound`; `app_exit=124` (timeout esperado del script).

**QA (§18.5)**: Tester QA Senior — **APROBADO, PASS**. 1 hallazgo de severidad baja: la separación plantillas|info es 10px (splitter 3px + `Margin="0,0,6,0"` del pane de plantillas) — réplica fiel de `NewFileDialog.axaml` que usa el mismo margen; se deja como está.

**Cierre**: commit `b1c767bab3` pushado a `origin/agents/sub-agent-senior-dev-role-report`.

## 2026-11-04 (3) — UI Avalonia: TextBoxes de New File y New Solution con largo fijo

**Contexto**: se pidió quitar el largo dinámico (estirado a lo ancho de la ventana) de los TextBox de los diálogos "New File" y "New Solution" y dejarlos con largo fijo.

**Qué se hizo** (archivos, solo XAML):
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/NewFileDialog.axaml`: `NameBox` y `ProjectPathBox` con `Width="360"`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/NewSolutionDialog.axaml`: `SolutionName` con `Width="320"` y `LocationBox` con `Width="460"` (el botón Browse queda a su derecha, sin estirar la fila).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores`.
- Captura QA: `bash /home/daniel/opencode/newfile_newsolution_fixedwidth_qa/run.sh` (arranca `--newsolution` y `--newfile-visual`; genera `newsolution.png` 760x600, `newfile.png` 718x524 y `run1.log`/`run2.log`).
- Medición PIL (tema oscuro: fondo `#1e1e1e`, box sin foco `#121212`, box enfocado `#000000` + anillo `#3584e4`): `SolutionName` x≈115..432 (~320px), `LocationBox` x≈78..535 (~460px) con Browse `#4b4b4b` a la derecha; `NameBox` (enfocado) x=62..421 = 360px exactos. Ningún box llega al borde derecho de la ventana (antes sí, hasta x≈752/710).
- `run1.log`/`run2.log`: sin `Unhandled`/`Exception`/`TypeLoad`/`DllNotFound`; `exit=124` (timeout esperado del script).
- OCR: "Solution name:", "Location:", "Browse", "Name:", "Create" presentes.

**QA (§18.5)**: Tester QA Senior — **APROBADO, PASS, 0 hallazgos abiertos** (el diff contiene solo las 4 adiciones de `Width`).

**Cierre**: commit `41304635d9` pushado a `origin/agents/sub-agent-senior-dev-role-report`.

## 2026-11-04 (4) — Iconos Fluent v2: respeto de colores originales + integración en IconService

**Contexto**: el subagente "Diseñador Gráfico Senior" había generado la familia `MonoDevelop.Avalonia.Icons` (4399 PNGs) aplanando TODAS las iconas a la paleta gris/azul Fluent (v1), lo que eliminaba los colores originales (verde "add", rojo "error", azules de file-types, etc.). El operador pidió repetir la tarea exigiendo que **los iconos respeten los colores**.

**Qué se hizo**:
- `~/opencode/fluent_icons/generate.py` (v2): regla por **base** (archivo sin modificadores):
  - **Bases coloreadas** (chroma máx de px opacos > 32; 118 bases / 1438 archivos): conservan el RGB **bit a bit** del original en todos sus modificadores (`~dark`, `~disabled`, `~sel`, `~error`, `~contrast`, `@2x`); cada variante legacy ya estaba teñida para su estado.
  - **Bases mono** (chroma ≤ 32; 272 bases / 2961 archivos): un solo color de paleta Fluent por modificador (dark: base `#F3F2F1`, disabled `#605E5C`, sel `#4CC2FF`, error `#FF6E6E`, contrast `#000000`; light: `#323130`/`#A19F9D`/`#0078D4`/`#C42B1C`/`#FFFFFF`; contrast~disabled `#808080`). Jerarquía: disabled > sel > error > active > tema.
  - Flood-fill solo en los 2 PNGs sin alpha (`zoom-in/out-16~contrast@2x.png`), como v1.
  - Verificado: 0 variantes coloreadas de bases mono en el legacy (la regla por base no pisa colores).
- Regenerados los 4399 PNGs en `main/src/core/MonoDevelop.Ide/MonoDevelop.Avalonia.Icons/` (carpeta vaciada antes). `icons/` legacy intacto.
- `~/opencode/fluent_icons/verify.py` (v2): contrato de color por tipo (mono = 1 color de paleta; colored = RGB opacos iguales a la entrada); `contact_sheet.py` (37 pares, incluye 4 pares de evidencia de color); `design_report.md` reescrito (v2).
- Integración en el shell Avalonia:
  - `main/src/core/MonoDevelop.Ide/Services/IconService.cs`: campo `fluentIconsDir` (sibling de `icons/`); `ResolveFile` busca primero `fluentIconsDir` para stock ids del IDE (fallback a `icons/`); stock ids de addins (`VcStockToResource`) solo resuelven en la carpeta `icons/` de su addin; `GetResourceImage` en orden `fluentIconsDir` → `iconsDir` → `welcomeIconsDir` (iconas de welcome sin colisión de nombres: 0 solapes verificados).
  - Flag QA `--iconsqa` (`Views/MainWindow.axaml.cs` + allowlist en `Program.cs`): volca la resolución de 21 stock ids representativos.
  - `--old-gui` (Gtk) no tocada: `ImageService` sigue leyendo `icons/`.

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores`.
- `python3 ~/opencode/fluent_icons/verify.py` → `RESULTADO: TODO OK` (4399/4399, dims 1:1, alpha idéntico salvo los 2 flood-fill, color válido v2).
- `bash ~/opencode/fluent_icons_qa/run.sh` (`--iconsqa`): `[iconsqa] total=21 fluent=20 legacy=0 missing=1` (`md-plugin` es id colgante preexistente, no regresión); `mainwindow.png`; sin excepciones en `run1.log`; `run1_exit=124` (timeout esperado).
- Píxeles vivos (PIL sobre `mainwindow.png`): colores source exactos en el toolbar `(85,174,126)` ×52 (`execute-16~dark`) y `(33,177,122)` ×14 (`debug-all-16~dark`) — imposibles bajo v1 (todo gris).
- Artefactos: `~/opencode/fluent_icons/` (scripts v2, `stats.json`, `generate.log`, `design_report.md`, `contact_sheet.png`) y `~/opencode/fluent_icons_qa/` (logs, `mainwindow.png`, reportes del QA).

**QA (§18.5)**: Tester QA Senior — **APROBADO, PASS, 0 hallazgos**. Observaciones no bloqueantes: `md-plugin` id colgante (cleanup en tarea aparte), `run1_exit=124` (timeout del harness), 128 warnings MSB3277 preexistentes.

**Cierre**: commit `5dbb3196cb` pushado a `origin/agents/sub-agent-senior-dev-role-report`.

## 2026-11-04 (5) — UI Avalonia: Solution pad — gutter estilo GTK (triángulo expander + líneas guía de indentación)

**Contexto**: se pidieron 3 mejoras de aspecto en el solution pad de la UI Avalonia para equipararlo al pad Gtk legacy: (1) icono expander **triangular** a la izquierda del icono del nodo en todos los nodos expandibles (apuntando **abajo** si está expandido, a la **derecha** si está colapsado); (2) subárbol de dependencias/References renderizado igual que GTK; (3) **líneas guía verticales** de indentación entre niveles. El gutter de la shell Avalonia es 100 % código Avalonia (no hay código GTK que reutilizar: en el backend Gtk el triángulo y las líneas los dibuja nativamente el widget `GtkTreeView`).

**Qué se hizo** (ambos en el **submódulo** `main/external/xwt`, rama `net10`; backend `Xwt.Avalonia`, `namespace Xwt.AvaloniaBackend`):
- `Xwt.Avalonia/TreeGutter.cs` (**nuevo**): control del gutter. `Render` dibuja 1 riel vertical de 1 px por nivel ancestro (nodo de profundidad `d` → `d` rieles) y el triángulo expander en su nivel (`r=4`, scanline: abajo `hw=r*(1-t)`, derecha `hh=r*(1-t)`). Todo con `DrawRectangle` (fill): en este build de Avalonia un trazo de 1 px (`DrawLine`/`DrawGeometry`) se antialiasa a unos pocos % de su alpha, mientras que un relleno compone a brillo completo. El riel se alinea a píxel entero (`x=i*16+8`, ancho 1) para cobertura completa (un riel en media-píxel se mitiga a la mitad).
- Color por **luminancia del fondo efectivo**, NO por `Application.ActualThemeVariant` (en esta app la variante lee `Light` aunque la ventana pinta fondo oscuro, y `TryGetResource("IdeFgBrush")` da `#1a1a1a` en ambas variantes): `EffectiveBackground` sube por el visual tree hasta el primer `Grid` con fondo sólido opaco; `dark = 0.299R+0.587G+0.114B < 128`. Oscuro → riel `0x60`/triángulo `0xE6`; claro → `0xB0`/`0x33`. El control es transparente (el resaltado de selección se ve a través).
- `Xwt.Avalonia/ListBackends.cs`: `RowPanelHost` nuevo modo árbol (`IsTree`, `IndentUnit=16`, `ExpanderSlot=16`) y **columna gutter** al inicio de cada fila en `RebuildRows` (la indentación ya no es `Margin` de fila). `TreeViewBackend`: `IsTree=true`; umbral de toggle del expander `p.X < Depth*IndentUnit + ExpanderSlot` (zona de gutter, a la izquierda de la primera celda); fondo `Brushes.Transparent` en filas no seleccionadas para que el click en el gutter sea hit-testeable (la base deja `null`, que Avalonia no hit-testea).
- APIs Avalonia 12.1.2: `Visual` en `namespace Avalonia` (no `Avalonia.Media`); `GetVisualParent()` es extensión en `Avalonia.VisualTree`; `Background` vive en `Grid` (no `Control`/`Decorator`); los tipos de Avalonia se nombran con aliases de nivel superior (sombreado por `Point`/`Color`/`Rectangle`/`DrawingContext` del frontend Xwt).

**Cómo validar**:
- Build (0 errores): `dotnet build main/external/xwt/Xwt.Avalonia/Xwt.Avalonia.csproj -c Debug -f net10.0 --nologo` y `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo`.
- Captura + forense PIL: `bash /home/daniel/opencode/solpad_avalonia_qa.sh` (arranca con `~/opencode/solpad_deps/Fixture.sln`, captura `solpad_avalonia_1.png`; ventana 320,157 1280x800; origen del panel en captura (7,141); fila 21 px). Rieles en captura-x 15/31/47; centro del triángulo por profundidad `d` en 15/31/47/63.
- Resultados: nodo profundidad `d` → `d` rieles (lum 96=`0x60`) + triángulo (lum 230=`0xE6`) en su nivel; hoja → solo rieles. Triángulo expandido = base arriba estrechándose abajo (perfil `[8,6,6,4,4,2,2]`); colapsado = borde izq vertical estrechándose a la derecha. Jerarquía 230≫96>fondo 30.
- Clic (XTest `python3-xlib`, sin xdotool): un clic en el gutter colapsa (queda 1 fila, triángulo a la derecha) y el siguiente re-expande (gutter idéntico, delta 0); funciona también en profundidad >0.
- `grep -c SOLPAD solpad_avalonia.log` = 0 (sin instrumentación).

**QA (§18.5)**: Tester QA Senior — **APROBADO, 5/5 PASS, SIN ERRORES** (arranque/captura; rieles+triángulos por fila; forma del triángulo abajo/derecha; clic colapsa/re-expande; sin instrumentación).

**Cierre**: submódulo `main/external/xwt` commiteado/pushed en rama `net10` (`8b3233b5`: `TreeGutter.cs` + `ListBackends.cs`); repos principal `b7e3070125` (gitlink) pushado a `origin/agents/sub-agent-senior-dev-role-report`. Sin código GTK en la ruta Avalonia.

## 2026-11-04 (6) — Solution pad Avalonia: "Dependencies > Platforms" + referencias portadas de la UI Gtk (retirada del nodo "References")

**Contexto**: el solution pad Avalonia no mostraba el subárbol de dependencias que el pad Gtk legacy (addin `MonoDevelop.DotNetCore`) pinta bajo cada proyecto. El operador pidió portar "Dependencies > Platforms" y, en un paso siguiente, sustituir el nodo interno "References" por Dependencies portando **todo** el contenido de referencias desde la UI Gtk (los grupos que el legacy pinta bajo cada Target Framework).

**Qué se hizo**:
- **Decisión de forma (operador, opción b)**: el nivel "Platforms" se muestra **siempre** (`Proyecto → Dependencies → net10.0 → …`), también con TFM único; el legacy colapsa el TFM único directamente en Dependencies (desviación deliberada y documentada).
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs`:
  - `AddXwtDependenciesNode`: nodo "Dependencies" (icono `folder-component-16.png` = `Stock.OpenReferenceFolder` del legacy) con un hijo por TFM (icono `framework-16.png` = `md-framework-dependency`, tag `platform:<path>:<tfm>`).
  - `AddXwtTfmReferenceGroups`: bajo cada TFM, los 5 grupos legacy en el orden exacto de `DependenciesNode.GetChildNodes`: **Frameworks** (`<FrameworkReference>`, solo si existen) → **NuGet** (`<PackageReference>`, icono `md-package-dependency`) → **SDK** (solo si NO hay Frameworks: `net*` → `Microsoft.NETCore.App`, `netstandard*` → `NETStandard.Library`, como `SdkDependenciesNode.GetDefaultNodes`) → **Assemblies** (`<Reference>`, icono `md-reference-folder`) → **Projects** (`<ProjectReference>`, icono `md-reference-project`). Grupos vacíos no se pintan (legacy `HasChildNodes`).
  - `ReadProjectReferences` + `XwtProjectReferences`: lectura XML ligera del csproj (XDocument, namespace-aware, sin MSBuild) de los 4 tipos de items; ItemGroups condicionales por TFM no evaluados (limitación documentada).
  - `XwtReferencedProjectFile`: port de `ProjectReferenceNodeCommandHandler.ActivateItem` — doble-clic en una referencia de proyecto abre el primer `.cs` del proyecto referenciado (include relativo con `\` normalizado a `/`; si no resuelve, tag no-archivo y RowActivated no hace nada).
  - **Retirada del nodo "References"**: eliminados el nodo clásico y TODO su rastro (método muerto `BuildReferencesNode` del árbol preview, ramas defensivas `references:` en `SelectedNodeType`/`ContextTargetDirectory`/properties pad). El concepto "References" ya no existe en la shell Avalonia; su contenido vive en Dependencies.
- `main/src/core/MonoDevelop.Ide/Services/IconService.cs`: entrada aditiva en `VcStockToResource` — `["md-package-dependency"] = ("nuget-16", "MonoDevelop.PackageManagement")` (el PNG vive en el addin `MonoDevelop.PackageManagement/icons/`); helper `XwtStockImage(stockId)` en MainWindow que resuelve stock id → PNG vía `IconService.ResolveFilePath` (theme-aware, admite addins).
- Fixture `~/opencode/solpad_deps/Fixture.sln` ampliado para ejercitar los 5 grupos: App (+`FrameworkReference` Microsoft.AspNetCore.App), LibB (+`PackageReference` Newtonsoft.Json 13.0.3, +`Reference` System.Runtime).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores`.
- `bash /home/daniel/opencode/solpad_avalonia_qa.sh` + OCR por fila (tema oscuro: texto claro >170; fila 21 px; indent 16 px/nivel; el "primer píxel claro" de una fila con expander es el triángulo, no el texto):
  - App → Dependencies → net10.0 → Frameworks (Microsoft.AspNetCore.App) + Projects (LibA) — **sin SDK** (el legacy suprime SDK cuando hay Frameworks);
  - LibA → Dependencies → net10.0 → SDK (Microsoft.NETCore.App) + Projects (LibB);
  - LibB → Dependencies → net10.0 → NuGet (Newtonsoft.Json) + SDK (Microsoft.NETCore.App) + Assemblies (System.Runtime) — orden legacy;
  - archivos Program.cs/A.cs/B.cs presentes, sin bin/obj, sin "References" en el pad.
- Interacción X11 (foco con `wmctrl -i -a` + XTest doble-clic): fila "LibA" bajo App→Projects → log `[rowactivate] opened A.cs` + editor abierto (`[outline] 1 node(s) in A.cs`); doble-clic en grupos/paquetes sin rowactivate ni crash.
- Limpieza: `grep -rn "AddXwtReferencesNode\|\"references:\"" main/src/core/MonoDevelop.Startup.Avalonia --include=*.cs` → 0 resultados.

**QA (§18.5)**: Tester QA Senior — ronda 1: 9/10 PASS, 1 hallazgo menor (restos `references:` preexistentes en código muerto del árbol preview). Corregido con la limpieza; **revalidación: PASS, 0 hallazgos abiertos** (grep limpio, diff no toca el feature, log sin Exception, árbol OCR intacto). Artefactos: `~/opencode/solpad_avalonia_1.png`, `solpad_deps_tree.png`, `solpad_deps_editor.png`, `solpad_avalonia.log`, `build_solpad_deps.log`, `ocr_solpad.py`, `ocr_bands/`, `diff_round2_mainwindow.txt`.

**Cierre**: código en commit `0d1a1cc9dc` (MainWindow.axaml.cs + IconService.cs); este
handoff se pusha junto con él a `origin/agents/sub-agent-senior-dev-role-report`. Sin
cambios en submódulos; la ruta `--old-gui` no se ve afectada (cambios aditivos en la
shell Avalonia + 1 entrada de mapa de iconos en core).

## 2026-11-05 (1) — Oleada 1 (backend Avalonia): backbone de comandos del menú — CommandManager no-GTK + catálogo legacy

**Contexto**: primera oleada de la integración total del backend `MonoDevelop.Ide`/`MonoDevelop.Core` en la shell Avalonia (directriz del operador: reutilizar los módulos no-GTK, portar los GTK). Objetivo de la oleada: que los menús y hotkeys de la shell dispatchen a través del `CommandManager` real (pipeline `CommandActivating`/`CommandActivated` de `MonoDevelop.Ide`), alimentado por el **catálogo de comandos legacy** (las secciones `/MonoDevelop/Ide/Commands` de los `*.addin.xml` de `MonoDevelop.Ide` core + todos los addins), en vez de ir directo a `OnMenuCommand` con ids a mano. Los comandos de catálogo sin `DefaultHandler` resuelto (los handler de GTK viven en el ensamblado GTK) caen a un router que la shell enlaza a su dispatcher de menús.

**Qué se hizo** (commit `0a3aff4b51`, 9 ficheros):
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Components.Commands/CommandManager.cs`: en la región `#if AVALONIA_SHELL` se añade `Count` y `CommandRouter` (`Action<object,object>`, fallback de dispatch), y `DispatchCommand` tolera `DefaultHandler == null`: sin router conserva el comportamiento anterior (disabled/invisible), con router ejecuta `CommandRouter(cmd.Id, dataItem)`; la rama con `DefaultHandlerType` resuelto no cambia.
- **Nuevo** `CommandCatalogLoader.cs` (solo `AVALONIA_SHELL`): lee los `*.addin.xml` bajo `baseDir/addinxml` y registra en el `CommandManager` cada comando de las extensiones `/MonoDevelop/Ide/Commands` sobre el modelo no-GTK (`Command`/`ActionCommand`). Replica el mapping de `CommandCodon.CreateInstance` **sin** reutilizar los codones (derivados de `Mono.Addins.ExtensionNode`, arrastrarían el motor de addins GTK): sub-rutas de la ruta base, `Condition` (Platform), flags de tipo (menu item/radio/check), `winShortcut` > `shortcut`, prefijo `@` (id de paquete) y `disabledVisible`. `LastError` expone el primer fichero fallido. `DefaultHandler` no se resuelve (clases de handler GTK).
- **Nuevo** `CommandService.cs` (solo `AVALONIA_SHELL`): singleton del shell (counterpart de `IdeApp.CommandService`) — `Manager`, `LoadCatalog(baseDir)` (idempotente), `LoadedCommands`, `Count`, `GetCommand`, `Dispatch(commandId, dataItem, source)`.
- `CommandEntry.cs` / `CommandEntrySet.cs`: guardas `#if !AVALONIA_SHELL` alrededor de las partes Gtk del modelo de entradas de menú (el resto compila en la shell).
- `MonoDevelop.Startup.Avalonia.csproj`: `Compile` de los 4 ficheros de `MonoDevelop.Components.Commands` (2 nuevos + 2 adaptados, ya compilados por referencia directa como los de Key Bindings) y `Content` de los 72 `*.addin.xml` (`main/src/core/**` → `addinxml/core/`, `main/src/addins/**` → `addinxml/addins/`) con `CopyToOutputDirectory=PreserveNewest`. El build GTK stagea su propio despliegue de addins (`main/build/AddIns`) y no se toca.
- `Program.cs`: al arranque enlaza `CommandService.Manager.CommandRouter` a `MainWindow.Instance?.OnMenuCommand(...)` y llama `CommandService.LoadCatalog(AppContext.BaseDirectory)` (warn a stderr si hay `LastError`).
- `App.axaml.cs`: registra `SynchronizationContext.Current` (contexto de UI de Avalonia) en `Runtime.MainSynchronizationContext` en `OnFrameworkInitializationCompleted` — lo mismo que el `IdeStartup` GTK hace con el main context de Gtk (`IdeStartup.cs:182`); sin esto los asserts de main thread de servicios compartidos fallarían en dispatch.
- `Views/MenuService.cs`: `RunCommand` y `CommandAction.Run` enrutan por `CommandService.Dispatch(id, null, CommandSource.MainMenu)` cuando `GetCommand(id) != null`; ids sintéticos (`"recent:"`, `"pad:"`, ...) o no registrados conservan el dispatch legacy `OnMenuCommand`.

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (también `net10.0-windows`).
- Harness de carga de catálogo (`~/opencode/catload_test/`): `Count=461` / `LoadedCommands=475`, ids de menú `196/172 resueltos`, dispatch `routed=1` (comando sin handler pasa por el router).
- Smoke (`~/opencode/smoke_oleada1.sh`): arranque limpio con `--sln`, 0 warnings, línea `[avalonia-shell] runtime main thread registered`, `--menudump` invariante respecto a pre-Oleada 1 (`menus=11 items=225 cmds=0 bindings=205` — el menú no cambió de forma, solo de qué ejecuta).
- QA P1–P4 (ver abajo), app arrancada **sin flags de menú** (solo `--sln`): la única vía de abrir un diálogo es menús/hotkeys → dispatch.

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (solo lectura sobre el repo; ciclo recursivo de §18.5 — ronda 1). Veredicto: **APROBADO — P1–P4 y estado final todos PASS, 0 hallazgos de app abiertos** (el ciclo cierra en ronda 1).
- **P1 arranque/invariantes: PASS** — build `0 Errores` (net10.0); arranque con solo `--sln` (ventana en <2 s); log con `[avalonia-shell] runtime main thread registered (context=AvaloniaSynchronizationContext)`, 0 `[warn]`, 0 `Unhandled|Exception|TypeLoadException|DllNotFound`; harness `catload_test`: `Count=461 LoadedCommands=475 LastError=(null)`, ids de menú `total=196 resolved=172 missing=24` (los 24 son sintéticos `pad:*`/`recent:`/`tool:` — van por router, no por catálogo), dispatch `routed=1`; `--menudump` **byte-igual** a la referencia pre-Oleada 1 (`[menudump] menus=11 items=225 cmds=0 bindings=205 _File=19i/0c _Edit=35i/0c _View=39i/0c _Search=27i/0c _Project=13i/0c _Build=7i/0c _Run=35i/0c Versio_n Control=14i/0c _Tools=8i/0c _Window=15i/0c _Help=13i/0c`).
- **P2 menubar + render File: PASS** — OCR (tesseract, franja superior 3×): 11/11 menús (File Edit View Search Project Build Run VersionControl Tools Window Help); File abierto con `alt+f`: 15 items OCR (New File... Ctrl+N, New Solution... Ctrl+Shift+N, Open... Ctrl+O, Recent Files/Solutions, Save, Save As..., Save All, Revert, Page Setup, Print Preview, Print..., Close Workspace, Close, Quit); Escape cierra.
- **P3 dispatch por menú: PASS** — app solo con `--sln` (sin flags: la única vía es menú → dispatch); focus verificado antes de cada envío; `alt+f` → popup 255x464 → `Return` (item 1 preseleccionado) → `wmctrl -l`: `0x01600037 New File` — cadena completa `MenuItem → CommandAction.Run → CommandService.Dispatch (FileCommands.NewFile) → pipeline CommandManager → CommandRouter → OnMenuCommand → OpenNewFileDialog`; `alt+f` → `Down` → `Return` → `0x0160003f Create a New Solution` (`FileCommands.NewProject`); Help: `alt+h` → type-ahead `a` → `0x01600047 About MonoDevelop` (rama lambda, no catálogo). Todos los diálogos cerrados y verificados; no se pulsó New/Create (sin creación en disco).
- **P4 hotkeys: PASS** — `ctrl+n` → `0x0160004b New File`; `F8` → líneas `[build]` **antes=0 → después=5** (build por App/LibA/LibB, `[process] exited with 0` del bloque final); `ctrl+q` → proceso muerto (último paso).
- **Estado final: PASS** — sin instancias de la app; `Fixture.sln` = 3 proyectos; sin `~/Solution`; `git status` limpio.
- **Hallazgos del subagente (ninguno bloqueante; ninguno atribuido a la Oleada 1)**: 1) [baja·entorno] `alt+f` 5/5 al primer intento en esta sesión (la caracterización ~50% es variabilidad de entorno); 2) [baja·entorno/preexistente] el mal posicionamiento de popups arriba-izq **no se reprodujo** (File en (357,196) y Help en (939,196), bajo su etiqueta) — intermitente según sesión, sin evidencia de regresión; 3) [info·entorno] ratón XTEST no llega al cliente (QA solo por teclado, como prescribe la caracterización); 4) [info·app, por diseño] F8 sobre el fixture registra `[process] exited with 1` en LibA/LibB + `[codeissues] 4 error(s), 12 warning(s)`: el fixture `Fixture.sln` lleva un error de compilación deliberado (CS0103 en `LibA/A.cs`) para ejercitar el pad de problemas; la mecánica F8 → `RunBuildAsync` funciona; 5) [info·app, cosmético] "Save" sin elipsis — fiel a la etiqueta legacy `_Save` del catálogo.
- Hallazgos de entorno acumulados de la sesión de desarrollo (contexto del plan de QA, ya caracterizados): XTEST mouse muerto para el cliente (Wayland/XWayland); `NewFileDialog` no cierra con Escape (solo botón Cancel) — brecha UX menor, candidata a Oleada 2; ImageMagick `import` roba focus/hanguea ⇒ capturas con Xlib `get_image`.
- Artefactos QA (en `~/opencode/`, no commiteados): **subagente** `~/opencode/oleada1_qa_subagent/` (logs `app_main.log`, capturas, OCR); desarrollo: `~/opencode/oleada1_qa/` (`cap.py`, `focus.py`, `click_qa.py`, `final_qa*.sh`, `seq_qa.sh`, `probe_*.sh`), `~/opencode/keys.py`, `~/opencode/smoke_oleada1.sh` (+`smoke_oleada1.log`), `~/opencode/catload_test/`, logs `oleada1_final3.log`, `clicktest.log`, `focustest*.log`, `menudebug.log`.

**Cierre**: código en commit `0a3aff4b51` pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (todo el nuevo código está bajo `#if AVALONIA_SHELL` o es aditivo en el modelo no-GTK; el build GTK no compila `CommandCatalogLoader`/`CommandService` ni el staging de `addinxml`).

## 2026-11-05 (2) — Oleada 2 (backend Avalonia): handlers core reales `MonoDevelop.Ide.Commands` (111 comandos)

**Contexto**: segunda oleada de la integración total del backend (directriz del operador: "sigue con la oleada 2"). En la Oleada 1 el dispatch del menú de la shell ya corría por el `CommandManager` + catálogo, pero **todos** los comandos caían al router (`OnMenuCommand`). Esta oleada integra los **handlers reales del núcleo** (`MonoDevelop.Ide.Commands`) en el ensamblado Avalonia: los 111 comandos del catálogo cuyo `defaultHandler` se resuelve a una clase compilada en este build pasan ahora por un `DefaultHandler` real (que delega en el mismo `OnMenuCommand`) en vez del router. **Invariante: cero cambio de comportamiento** (menú idéntico, dispatch final idéntico).

**Qué se hizo** (commit `176ee02252`, 16 ficheros):
- **Nuevo** `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Commands/ShellCommandHandler.cs` (`#if AVALONIA_SHELL`): puente estático `Run(object commandId, object dataItem=null)` → `CommandManager.ToCommandId` → `MainWindow.Instance?.OnMenuCommand(id, dataItem as string)` (mismo destino que el router).
- 11 ficheros `MonoDevelop.Ide.Commands/*.cs` portados: el código legacy queda envuelto en `#if !AVALONIA_SHELL` (byte-idéntico, usings GTK incluidos); los enums son compartidos y no llevan guarda; bajo `#else` las clases con el mismo nombre delegan su `Run()` en `ShellCommandHandler.Run` (sin override de `Update` en esta oleada — el estado de habilitación es Oleada 3; `CommandInfo.enabled/visible` son `true` por defecto, así que el dispatch no se bloquea). La mayoría delega por member de enum; los que no tienen member delegan por string ID (políticas, code analysis, side-by-side/single mode, notebooks, `OpenDocument1..9`, `SwitchNext/PreviousPad`, `DumpUITree`/`DumpA11yTree*`/`MarkLog`).
- `CommandCatalogLoader.cs`: contador `ResolvedHandlers` + `ResolveHandlerType` (`typeof(CommandCatalogLoader).Assembly.GetType(name)` + `CommandHandler.IsAssignableFrom`) → asigna `cmd.DefaultHandlerType`; si no resuelve → `null` → router (sin cambio de comportamiento).
- `CommandManager.cs` (región `#if AVALONIA_SHELL`): `DispatchCommand` instancia el handler con `Activator.CreateInstance (type, true)` (`nonPublic:true`, porque los handlers del catálogo son mayormente `internal`); añade `GetCommands()` público (lo usa el oracle de QA).
- `Program.cs`: línea `[commands] catalog loaded: {N} commands, {M} with resolvable default handler` tras `LoadCatalog`.
- `MonoDevelop.Startup.Avalonia.csproj`: 12 `<Compile>` (ShellCommandHandler + los 11). `RunCustomToolHandler` no portada (ctor con `ExternalTool`, no referenciada en XML); las bases abstractas `TabCommandHandler`/`OpenDocumentHandlerBase` tampoco (cada clase concreta delega directa).

**Números clave** (medidos sobre `main/build/addinxml` + oracle):
- 259 `defaultHandler` en todo el catálogo; solo 112 elementos `<Command>` en el namespace `MonoDevelop.Ide.Commands.*` (111 ids únicos, 1 duplicado: `EditCommands.MonodevelopPreferences`).
- **`ResolvedHandlers=111`**, 0 nombres irresolubles; el resto de `defaultHandler` (Debugger, CSharp, VersionControl, Packaging, PackageManagement, PerformanceDiagnostics, DesignerSupport, MainToolbar, FindInFiles, ...) no compila en este build → router.
- Catálogo invariante: `Count=461`, `LoadedCommands=475`, ids de menú `196/172 resueltos / 24 missing` (ids sintéticos `pad:*`/`recent:`/`tool:`).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (también `net10.0-windows`).
- Oracle (`~/opencode/catload_test/`): `catalog: Count=461 LoadedCommands=475`, `oleada2: ResolvedHandlers=111 commandsWithHandlerType=111 totalCommands=461`, `oleada2-unresolved(MonoDevelop.Ide.Commands): 0`, `menu ids: total=196 resolved=172 missing=24`, `dispatch: registered=True … routed=0` (un comando con handler ya no pasa por el router).
- Smoke (`~/opencode/smoke_oleada1.sh`): arranque limpio con `--sln`, línea `[commands] catalog loaded: 475 commands, 111 with resolvable default handler`, `--menudump` **byte-igual** a la Oleada 1 (`menus=11 items=225 cmds=0 bindings=205`).
- Funcional (`~/opencode/oleada2_qa_subagent/functional_f1.sh`): F1 (XTEST, con focus verificado) → `[help] API documentation root (Help pad)` — la cadena completa `OnKeyDown → MenuService.RunCommand → CommandService.Dispatch → HelpHandler (uno de los 111) → ShellCommandHandler.Run → OnMenuCommand` funciona en la app real.

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (solo lectura; ronda 1). Veredicto: **APROBADO — P1–P5 todos PASS, 0 hallazgos bloqueantes de app** (ciclo cerrado en ronda 1).
- **P1 build: PASS** — `0 Errores` en net10.0 y net10.0-windows.
- **P2 oracle: PASS** — valores exactos: `Count=461 LoadedCommands=475`, `ResolvedHandlers=111`, `unresolved=0`, menú `196/172/24`, `routed=0`.
- **P3 smoke: PASS** — ventana en ~3 s, sin excepciones, menudump invariante byte-igual.
- **P4 funcional: PASS** — F1 → `[help] API documentation root` (verificado con XTEST de focus confirmado y, como respaldo, XSendEvent); `[commands] 475/111` presente.
- **P5 sin doble registro de sesión: PASS** — `SessionRecorder` pasivo (no se suscribe a `CommandActivating`/`CommandActivated`); `sessionRecorder.Record(commandId)` es la 1ª sentencia de `OnMenuCommand` (único call site); ambos paths (handler y router) convergen ahí exactamente una vez.
- Hallazgos del subagente (ninguno bloqueante de app): 1) [entorno] ratón XTEST muerto en la sesión (conocido); 2) [entorno] el teclado XTEST murió a nivel de sesión en un punto (XSendEvent sí entregaba) — intermitente de entorno, no de app; 3) [incidente·autoinfligido, resuelto] el probe `--no-incremental` del subagente (fuera del alcance de P1) dejó `main/build` temporalmente inconsistente (faltó `MonoDevelop.AvaloniaShell.dll`); el fallo de `--no-incremental` es **preexistente** (`MonoRoslynCompat` no compila solo — `Microsoft.VisualStudio.Text.Data.dll` por HintPath); el dev recuperó `main/build` con un build incremental normal y **re-validó P2+P3 en verde tras la recuperación**.
- Artefactos QA (en `~/opencode/`, no commiteados): `~/opencode/oleada2_qa_subagent/` (`functional_f1.sh`, `f1debug.sh`, probes `xtest_probe.py`/`xdeliver_test.py`/`xsend_test.py`, logs `p1_*`, `p2_catload_rerun.log`, `p3_smoke_rerun.log`, `p4_*`), `~/opencode/smoke_oleada1.sh` (+log), `~/opencode/catload_test/` (extendido con la línea `oleada2` y el diagnóstico `unresolved`).

**Cierre**: código en commit `176ee02252` pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (todo el nuevo código está bajo `#if AVALONIA_SHELL` y los ficheros portados conservan el código GTK byte-idéntico bajo `#if !AVALONIA_SHELL`).

## 2026-11-05 (3) — Oleada 3 (backend Avalonia): estado de habilitación de menú (Update) — Slice 1

**Contexto**: tercera oleada de la integración total del backend (el operador dio "continua" tras el cierre de la Oleada 2; alcance elegido por el operador: **faseada, vertical slice primero**). En las Oleadas 1–2 el menú dispatcha por el `CommandManager` con handlers reales, pero el estado de habilitación seguía **estático**: `MenuEntry.Disabled` (bool hardcodeado) y `MenuItem.IsEnabled` fijado una sola vez al construir; `CommandInfo.Enabled` no se consultaba en el render. Los 85 overrides `Update` legacy de `MonoDevelop.Ide.Commands` acuden a `IdeApp.Workbench`/`IdeApp.Workspace`/`IdeApp.IsInitialized`, que **no existen en el build Avalonia** — no es un port mecánico. Esta slice pone el mecanismo (proveedor de estado + cableado menú↔`CommandInfo.Enabled`) y porta el subconjunto de mayor valor del menú File (4 handlers). El resto de `Update` queda en slices siguientes sobre el mismo mecanismo.

**Qué se hizo** (commit `757553d93b`, 8 ficheros):
- `CommandManager.cs` (región `#if AVALONIA_SHELL`, tras `DispatchCommand`): `GetCommandInfo(object commandId)` — convierte el id (`ToCommandId`), obtiene el `ActionCommand`, crea `CommandInfo`, instancia el `DefaultHandler` si no existe (`Activator.CreateInstance(type, true)`, nonPublic), ejecuta `InternalUpdate(info)` **sin despachar** (si `IsEnabled`), aplica `info.Enabled &= IsEnabled` y `CancelAsyncUpdate()`. Comandos sin handler resuelto o ids desconocidos devuelven el `CommandInfo` por defecto (`Enabled=true`), igual que antes en dispatch.
- `CommandService.cs`: `GetCommandInfo(object commandId)` estático delegado al manager.
- **Nuevo** `MonoDevelop.Ide.Commands/ShellCommandState.cs` (`#if AVALONIA_SHELL`, namespace `MonoDevelop.Ide.Commands`): proveedor de estado static — `HasDocuments` (`MainWindow.Instance?.documents.Count > 0` vía accessor), `HasActiveDocument` (tab seleccionado), `HasDirtyDocuments` (alguno `IsDirty`), `HasWorkspace` (`loadedSolutionPath != null`); todas con fallback `?? false` (headless ⇒ false).
- `MonoDevelop.Startup.Avalonia.csproj`: `<Compile Include>` de `ShellCommandState.cs`.
- `MainWindow.axaml.cs`: 4 accessors `internal` (miran `documents`, `DocTabs.SelectedItem`, `docs.Values`, `loadedSolutionPath`); `NotifyCommandStateChanged()` (llama a `MenuBuilder.RefreshCommandStates()`); hooks en cada punto de cambio de estado: `AddDocument` (suscripción `PropertyChanged` al `SkTextEditor.IsDirtyProperty` de cada editor + aviso al añadir), `OnDocSelectionChanged`, `CloseDocumentAsync` (tras `SelectFirstDocument()`), `OpenSolutionInWindow` (tras cargar), `CloseWorkspaceAsync` (tras cerrar); `MenuBuilder.RefreshCommandStates()` al final de `BuildMenu()`. QA: helper `DumpCmdStates(when)` (dump de `IsEnabled` de los 4 items + estado subyacente) y hook `--cmdstates` que encadena initial→open→dirty→clean→closed.
- `MenuBuilder.cs`: `static List<MenuItem> commandItems` (solo items ligados a `CommandAction`; `item.Tag = ca.Id`; se limpia en `BuildItems` — única llamada: el menú principal); `RefreshCommandStates()` refleja `CommandService.GetCommandInfo(id).Enabled` en `item.IsEnabled`.
- `FileCommands.cs` (solo el bloque Avalonia `#else`; el legacy `#if !AVALONIA_SHELL` quedó byte-igual): 4 overrides `Update(CommandInfo)` — `SaveAllHandler`→`HasDirtyDocuments`, `CloseAllFilesHandler`→`HasDocuments`, `CloseFileHandler`→`HasActiveDocument`, `CloseWorkspaceHandler`→`HasWorkspace` (`CloseWorkspaceItem` comparte el handler y hereda el estado).
- `Program.cs`: `--cmdstates` añadido a la lista de flags QA.

**Números clave**:
- 85 overrides `Update` en 11 ficheros del namespace; **4 portados** en esta slice (SaveAll, CloseAllFiles, CloseFile, CloseWorkspace) — el resto en slices siguientes.
- Catálogo y menú invariantes: `Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, menudump `menus=11 items=225 cmds=0 bindings=205` (byte-igual a Oleada 1/2 — el modelo `MenuService` no se toca).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (también `net10.0-windows`).
- Oracle (`~/opencode/catload_test/`, extendido con la sección `oleada3`): headless (sin `MainWindow`) ⇒ los 5 ids con handler portado (incluido `CloseWorkspaceItem`) reportan `enabled=False expected=False`; controles `OpenFile` (handler sin override), `Save` (routed) y id desconocido `enabled=True expected=True` → `oleada3: ok=8 fail=0`, exit 0; regresa `oleada2: ResolvedHandlers=111`.
- Smoke (`~/opencode/smoke_oleada1.sh`): `--menudump` **byte-igual** (`menus=11 items=225 cmds=0 bindings=205 _File=19i/0c … _Help=13i/0c`), 0 líneas `[warn]/[fatal]/Exception`.
- E2E (`~/opencode/oleada3_cmdstates.sh`, 2 casos): con `--sln` `closeWs=1`/`hasWs=True` en las 5 transiciones (initial `0/0/0/1`, opened `0/1/1/1`, dirty `1/1/1/1`, clean `0/1/1/1`, closed de vuelta a `0/0/0/1`); sin sln idéntico con `closeWs=0`/`hasWs=False`. Secuencia exacta + 0 excepciones en ambos.

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_qa_subagent/`; ronda 1). Veredicto: **APROBADO — P1–P5 todos PASS, 0 hallazgos bloqueantes ni menores** (ciclo cerrado en ronda 1).
- **P1 revisión estática: PASS** — `GetCommandInfo` dentro de la región Avalonia (sobrecargas GTK L1817–1851 intactas); bloque legacy de `FileCommands.cs` byte-igual a HEAD; los 5 hooks + suscripción `IsDirty` cubren todas las mutaciones de estado; `BuildItems` solo llamada desde `BuildMenu` (la lista estática no se contamina); `MenuService.cs` intocado; Welcome = overlay (no cuenta en `documents`).
- **P2 build: PASS** — `0 Errores` en net10.0 y net10.0-windows.
- **P3 oracle: PASS** — 8/8 `oleada3 OK`, `ok=8 fail=0`, exit 0; `ResolvedHandlers=111` presente.
- **P4 smoke: PASS** — menudump byte-igual (diff vacío), 0 warn/exception.
- **P5 E2E: PASS** — secuencias `SEQ_EXACT_MATCH` en con-sln y sin-sln, 0 warn/exception en ambos logs.
- Observaciones del subagente (no bloqueantes, sin acción requerida): 1) la suscripción `PropertyChanged` del editor no se desuscribe al cerrar el doc — no es fuga real (el editor es la raíz de la cadena y queda recolectable); 2) `DumpCmdStates` usa `MainMenu!` — riesgo acotado al hook QA (el menú se construye síncrono antes); 3) `commandItems` está siempre en sincronía (rebuild ⇒ `Clear()` + refill; sin menú la lista está vacía y el método es no-op).
- Artefactos QA (en `~/opencode/`, no commiteados): `~/opencode/oleada3_qa_subagent/` (`p3_oracle.log`, `p4_smoke.log`, `p5_e2e.log`), `~/opencode/oleada3_cmdstates.sh` (+ logs `oleada3_cmdstates_withsln.log`/`_nosln.log`), `~/opencode/catload_test/` (extendido con la sección `oleada3`), `~/opencode/smoke_oleada1.sh` (+log).

**Queda pendiente (slices siguientes de la Oleada 3)**: portar el resto de los 81 `Update` pendientes (Edit, View, Build, Run, Tools, Window, Help y el resto de File — p. ej. `Save`/`SaveAs`/`Open`), cada uno mapeando sus consultas legacy (`IdeApp.Workbench.ActiveView`, `IdeApp.Workspace`, `IdeApp.IsInitialized`, ...) a `ShellCommandState` (ampliando el proveedor con lo que haga falta, p. ej. `HasActiveEditor`, `CanSave`, estado de build). Mecanismo, cableado y QA ya están cubiertos por esta slice.

**Cierre**: código en commit `757553d93b` pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (todo el nuevo código está bajo `#if AVALONIA_SHELL` y el bloque legacy de `FileCommands.cs` es byte-igual al commit anterior).

## 2026-11-05 (4) — Oleada 3 (backend Avalonia): estado de habilitación de menú (Update) — Slice 2

**Contexto**: segunda slice de la Oleada 3 sobre el mecanismo ya instalado en Slice 1 (proveedor `ShellCommandState` + cableado menú↔`CommandInfo.Enabled`). Alcance elegido por el operador: **File (resto) + Edit básicos**. Se portaron los overrides `Update` restantes del menú File que no son routed ni de Oleada 3-slice-1, más los 5 items de edición del menú Edit que el legacy gatea por "widget editable con foco". Cada consulta legacy (`IdeApp.Workbench.ActiveDocument`, `RecentFiles`, foco `Gtk.TextView/IEditable`) se mapea al estado equivalente de la shell ampliando `ShellCommandState` con lo que faltaba.

**Qué se hizo** (commit `438b2311ce`, 4 ficheros):
- `ShellCommandState.cs`: 2 propiedades nuevas — `HasActiveEditor` (el tab seleccionado sostiene un `SkTextEditor`, espejo de `WithActiveEditor`) y `HasRecentSolutions` (`RecentSolutions.GetAll().Count > 0`); ambas con fallback `?? false`.
- `MainWindow.axaml.cs`: 2 accessors `internal` (`HasActiveEditor => DocTabs?.SelectedItem is TabItem t && docs.ContainsKey (t.Tag as string ?? "")`, `HasRecentSolutions => RecentSolutions.GetAll ().Count > 0`); un `NotifyCommandStateChanged()` tras `RecentSolutions.Add(path)` en `OpenSolutionInWindow` (el hook previo de Slice 1 estaba antes del `Add`, y `HasRecentSolutions` requiere refrescar después); extensión del helper QA `DumpCmdStates` (ahora vuelca `print/printPrev/pageSetup/clrFiles/clrProj` + `cut/copy/paste/delete/selectAll` + `hasActiveEditor/hasRecent`).
- `FileCommands.cs` (solo el bloque Avalonia `#else`; el legacy `#if !AVALONIA_SHELL` quedó byte-igual): 5 overrides `Update` — `PrintHandler`/`PrintPreviewHandler`/`PrintPageSetupHandler`→`HasActiveDocument` (legacy `CanPrint()`), `ClearRecentFilesHandler`→`false` (el submenú "Recent Files" de la shell es un placeholder "(Empty)" estático, sin store de recientes de archivos), `ClearRecentProjectsHandler`→`HasRecentSolutions`.
- `EditCommands.cs` (solo el bloque Avalonia `#else`; legacy byte-igual): 5 overrides `Update` — `DefaultDeleteHandler`/`DefaultCopyHandler`/`DefaultCutHandler`/`DefaultPasteHandler`/`DefaultSelectAllHandler`→`HasActiveEditor`. El legacy de estos 5 gatea por "el widget con foco es un `Gtk.TextView`/`IEditable` editable" y **ninguno comprueba selección**, así que no se introduce check de selección (fidelidad al legacy).

**Decisiones de mapeo**:
- `DeleteKey` **no** se porta: no es item de menú (en legacy `Bypass=true`, forward al teclado).
- `Undo`/`Redo`/`Find`/`Save`/`SaveAs`/`Exit` **no** se portan: routed (sin `defaultHandler`) o sin override `Update` legacy → se quedan `Enabled=true` por defecto, coherente con GTK.
- `ClearRecentFiles`→`false` fijo: la shell no mantiene lista de recientes de archivos (submenú estático); dejarlo siempre deshabilitado evita además que su handler (que en la shell vacía `RecentSolutions`) sea alcanzable por el path de "Recent Files".

**Números clave**:
- 10 overrides `Update` portados en esta slice (5 File resto + 5 Edit) → **14 de los 85** acumulados en la Oleada 3; el resto en slices siguientes.
- Catálogo y menú invariantes: `Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, menudump `menus=11 items=225 cmds=0 bindings=205` (byte-igual a Oleada 1/2/3-slice-1 — el modelo `MenuService` no se toca; el estado habilitado no forma parte del menudump).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (también `net10.0-windows`). El build compila las fuentes de `MonoDevelop.Ide` directamente dentro de `MonoDevelop.AvaloniaShell.dll` (csproj `Compile Include` + `AVALONIA_SHELL`), por lo que los cambios de `MonoDevelop.Ide.Commands/*` van al assembly de la shell, no al `MonoDevelop.Ide.dll` legacy (que queda sin usar en la ruta Avalonia).
- Oracle (`~/opencode/catload_test/`, sección `oleada3` extendida a 19 ids): headless ⇒ los 14 ids con handler portado (10 de esta slice + 4 de Slice 1 + `CloseWorkspaceItem`) reportan `enabled=False expected=False`; controles `OpenFile` (sin override), `Save`/`Undo` (routed) e id desconocido `enabled=True expected=True` → `oleada3: ok=19 fail=0`, exit 0; regresa `ResolvedHandlers=111`.
- Smoke (`~/opencode/smoke_oleada1.sh`): `--menudump` **byte-igual** (`menus=11 items=225 cmds=0 bindings=205 _File=19i/0c … _Help=13i/0c`), 0 líneas `[warn]/[fatal]/Exception`.
- E2E (`~/opencode/oleada3_cmdstates.sh`, 2 casos × 5 transiciones initial→opened→dirty→clean→closed): los 5 items de Edit (`cut/copy/paste/delete/selectAll`) y los 3 de Print (`print/printPrev/pageSetup`) siguen `0→1→1→1→0` (ligados a `hasActive`/`hasActiveEditor`); `clrFiles=0` siempre; `clrProj=hasRecent`; `closeWs`/`hasWs` = 1/True en con-sln y 0/False en sin-sln; `saveAll` sigue `hasDirty`. Secuencia exacta + 0 excepciones en ambos casos.

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_slice2_qa/`; ronda 1). Veredicto: **APROBADO — P1–P5 todos OK, 0 hallazgos bloqueantes ni mayores**.
- **P1 Build: OK** — `0 Errores` en net10.0 y net10.0-windows; `MonoDevelop.AvaloniaShell.dll` de `main/build` contiene `ShellCommandState`/`HasActiveEditor` (código nuevo desplegado).
- **P2 Oracle: OK** — `ok=19 fail=0`, exit 0; `Count=461 LoadedCommands=475`, `ResolvedHandlers=111`.
- **P3 Smoke: OK** — menudump byte-igual a la baseline, 0 warn/exception.
- **P4 E2E: OK** — las 5 filas de con-sln y las 5 de sin-sln coinciden exactamente con lo especificado; `clrFiles=0` en las 10 filas; 0 `?` (los 15 ids resuelven); 0 warn/fatal/Exception.
- **P5 Código: OK** — mapeo fiel al legacy (verificado bloque a bloque); bloques legacy byte-idénticos (`cmp` de `#if !AVALONIA_SHELL` de Edit/File vs HEAD); `HasActiveEditor` sin check de selección (espejo de `WithActiveEditor`); `--old-gui` intacta.
- Hallazgo **Menor** (corregido y re-verificado antes del commit de docs): el `#endif` de cierre compartido de `EditCommands.cs` había quedado con una tab extra + CRLF→LF por el edit; se restauró a `#endif\r\n` (columna 0) → el diff lógico quedó en inserciones puras (0 líneas eliminadas) y el bloque legacy es byte-igual al HEAD. Re-verificado: build `0 Errores` + oracle `ok=19 fail=0`.
- Observaciones del subagente (no bloqueantes, sin acción): 1) la 1.ª ejecución del oracle murió a las 44/76 líneas sin excepción y la repetición completó con exit 0 (transitorio de entorno); 2) `main/build/MonoDevelop.Ide.dll` es un artefacto legacy GTK antiguo no usado por la ruta Avalonia (preexistente, no regresión de esta slice); 3) `hasRecent=True` desde `initial` en el E2E proviene de la lista de soluciones recientes persistida (coherente).

**Queda pendiente (slices siguientes de la Oleada 3)**: portar el resto de los `Update` pendientes — View, Build, Run, Tools, Window, Help y el resto de File (`Save`/`SaveAs`/`Open` son routed y no requieren port), más los items de edición de selección del Edit (Indent/Unindent/Case/Comment/Join/Sort…) y `Undo`/`Redo` (que exigirían un `CanUndo`/`CanRedo` real del editor, hoy no público en `SkTextEditor`). El mecanismo, el cableado y el QA ya están cubiertos por Slice 1–2.

**Cierre**: código en commit `438b2311ce` pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (todo el nuevo código está bajo `#if AVALONIA_SHELL` y los bloques legacy de `FileCommands.cs`/`EditCommands.cs` son byte-iguales al commit anterior).

## 2026-11-05 (5) — Oleada 3 (backend Avalonia): estado de habilitación de menú (Update) — Slice 3

**Contexto**: tercera slice de la Oleada 3 sobre el mecanismo ya instalado en Slice 1 (proveedor `ShellCommandState` + cableado menú↔`CommandInfo.Enabled`). Alcance elegido por el operador: **Window + View + Help**. Se portaron los overrides `Update` de esos tres menús cuyo estado depende solo de `ShellCommandState` (recuento de documentos y editor activo), mapeando las consultas legacy (`IdeApp.Workbench.Documents.Count`, `ActiveDocument` + `IZoomable`/`ITextView`, lock de shell) al estado equivalente de la shell.

**Qué se hizo** (commit `c5f01f6b40`, 5 ficheros):
- `ShellCommandState.cs`: 1 propiedad nueva — `DocumentCount` (`MainWindow.Instance?.DocumentCount ?? 0`), espejo de `IdeApp.Workbench.Documents.Count`.
- `MainWindow.axaml.cs`: 1 accessor `internal` (`DocumentCount => documents.Count`); extensión del helper QA `DumpCmdStates` (ahora vuelca `nextDoc/prevDoc/open1/open2/open3/open9` + `zoomIn/zoomOut/zoomReset/focusDoc/centerFocus` + `docCount`) y un reader directo `CEn(id)` vía `CommandService.GetCommandInfo(id).Enabled` para comandos portados **sin** ítem de menú (el walk de menú devolvería `null`); el driver `--cmdstates` ahora abre 2 documentos para ejercitar la transición `docCount` 0→1→2→1→0.
- `WindowCommands.cs` (solo el bloque Avalonia `#else`; legacy byte-igual): 11 overrides `Update` — `NextDocument`/`PrevDocument`→`DocumentCount >= 2` (legacy `Documents.Count < 2` → disable); `OpenDocument1..OpenDocument9`→`DocumentCount >= N` (legacy `Documents.Count >= index`).
- `ViewCommands.cs` (solo `#else`; legacy byte-igual): 5 overrides `Update` — `ZoomIn`/`ZoomOut`/`ZoomReset`/`FocusCurrentDocument`/`CenterAndFocusCurrentDocument`→`HasActiveEditor` (legacy `ActiveDocument != null` + `IZoomable`/`ITextView`).
- `HelpCommands.cs` (solo `#else`; legacy byte-igual): 1 override `Update` — `OpenLogDirectory`→`info.DisableOnShellLock = false; info.Enabled = true` (espejo exacto del legacy).

**Decisiones de mapeo**:
- `FocusCurrentDocument` **no es ítem de menú** (en legacy `MainMenu.addin.xml` solo aparece `CenterAndFocusCurrentDocument` como "_Cursor Position"; `FocusCurrentDocument` es solo shortcut F7). Por eso el E2E lo lee vía `CEn` (estado directo del comando), no por el walk de menú; el override sí está portado y gatea por `HasActiveEditor`.
- Gates **más gruesos que legacy** (diseño acordado, no defecto): `ZoomIn/Out/Reset` no modelan `EnableZoomIn/Out/Reset` por dirección (en el límite de zoom el ítem queda habilitado y el `Run` hace clamp); `FocusCurrentDocument`/`CenterAndFocusCurrentDocument` no modelan `ITextView` (usan `HasActiveEditor` = tab con `SkTextEditor`).
- **Deferidas documentadas en el código** (port de visibility/dynamic-labels, fuera de scope del slice): `OpenDocument1..9` quedan visibles-deshabilitados (legacy hacía `Visible=false` + rename al título cuando `Count < index`); el gate `CanShowHelp` del item Help (F1)/`HelpHandler`; el submenú dinámico `OpenDocumentList`.
- `OpenLogDirectory` **no** es el comando con `CanShowHelp` (ese es el item Help/F1); su port replica exactamente `DisableOnShellLock=false` + `Enabled=true`.

**Números clave**:
- 17 overrides `Update` portados en esta slice (11 Window + 5 View + 1 Help) → **31 acumulado en la Oleada 3** (Slice 1: 4, Slice 2: 10, Slice 3: 17); el resto (Build, Run, Project, Tools y el resto de View/File/Edit) en slices siguientes.
- Catálogo y menú invariantes: `Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, `menu ids total=196 resolved=172 missing=24`, menudump `menus=11 items=225 cmds=0 bindings=205` (byte-igual a Oleada 1/2 y Slices 1–2 de la Oleada 3 — el modelo `MenuService` no se toca; el estado habilitado no forma parte del menudump).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (también `net10.0-windows`). El build compila las fuentes de `MonoDevelop.Ide` dentro de `MonoDevelop.AvaloniaShell.dll` (csproj `Compile Include` + `AVALONIA_SHELL`), así que los cambios de `MonoDevelop.Ide.Commands/*` van al assembly de la shell.
- Oracle (`~/opencode/catload_test/`, sección `oleada3` extendida a 36 ids): headless ⇒ los 17 ids con handler portado en esta slice + los 14 previos reportan `enabled=False expected=False`; controles `OpenFile` (sin override), `Save`/`Undo` (routed) e id desconocido `enabled=True expected=True` → `oleada3: ok=36 fail=0`, exit 0; invariantes `Count=461 LoadedCommands=475 ResolvedHandlers=111`.
- Smoke (`~/opencode/smoke_oleada1.sh`): `--menudump` **byte-igual** (`menus=11 items=225 cmds=0 bindings=205 _File=19i/0c … _Help=13i/0c`), 0 líneas `[warn]/[fatal]/Exception`.
- E2E (`~/opencode/oleada3_cmdstates.sh`, 2 casos con/sin soln × 7 transiciones initial→opened→opened2→dirty→clean→closed1→closed2): `docCount` transcurre 0→1→2→2→2→1→0; `nextDoc`/`prevDoc`=`0` hasta `docCount>=2` (entonces `1`); `open1`=`1` con `docCount>=1`, `open2`=`1` con `docCount>=2`, `open3`/`open9`=`0` (solo 2 docs abiertos); `zoomIn/zoomOut/zoomReset/focusDoc/centerFocus`=`1` con editor activo y `0` sin docs. 0 `?`, 0 excepciones en ambos casos.

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/`).
- **Ronda 1**: **APROBADO CON OBSERVACIONES** (P1 build / P2 oracle / P3 smoke OK; P4 = deferida documentada de visibility de `OpenDocument1..9`, out-of-scope; 3 P5 de higiene).
- **Correcciones aplicadas y re-verificadas**:
  - **P5-1**: comentario de `OpenLogDirectory` que atribuía `CanShowHelp` a este comando → corregido (ese gate es del item Help/F1).
  - **P5-2**: falta de paridad `info.DisableOnShellLock = false` (legacy) → añadida al port (paridad estricta; hoy sin efecto observable porque la shell Avalonia nunca incrementa `guiLock`).
  - **P5-3**: `CEn` trataba la excepción en silencio → ahora loguea `Output("[cmdstates] CEn <id> error: <msg>")` antes de devolver `null`.
  - **N1** (nuevo, bajo): comentario de bloque Oleada 2 en `HelpCommands.cs` que decía "los Update no portados" → matizado a "salvo `OpenLogDirectoryHandler`".
- **Ronda 2**: **APROBADO** (los 3 P5 resueltos, sin regresión: build 0 errores, E2E 7×2 exacto, sin líneas `CEn error`).
- Observaciones no bloqueantes (sin acción): (1) P4 = `OpenDocument1..9` visibles-deshabilitados en vez de ocultos (port de visibility diferido); (2) gates de zoom/focus más gruesos que legacy por diseño; (3) los umbrales `open3..open9` (>=3..>=9) se validan por inspección (uniformes), no con 3+ docs abiertos; (4) `DocumentCount` cuenta todos los tabs (hoy todos son editores); (5) la ruta `--old-gui` se verificó por inspección del diff (bloques legacy intocados), no por build Gtk.

**Queda pendiente (slices 4..n de la Oleada 3)**: portar el resto de los `Update` pendientes — Build, Run, Project, Tools (requieren estado de build/debug/solución aún no rastreado por `ShellCommandState`), y el resto de View (items de layout/pads, que dependen de `Visible`/dynamic y del port de visibilidad), más la **port de visibility/rename/dynamic** (OpenDocument1..9, CanShowHelp, OpenDocumentList) y `Undo`/`Redo` (exigirían `CanUndo`/`CanRedo` real del editor, hoy no público en `SkTextEditor`). Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–3.

**Cierre**: código en commit `c5f01f6b40` pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (todo el nuevo código está bajo `#if AVALONIA_SHELL` y los bloques legacy de `WindowCommands.cs`/`ViewCommands.cs`/`HelpCommands.cs` son byte-iguales al commit anterior).

## 2026-11-05 (6) — Oleada 3 (backend Avalonia): estado de habilitación de menú (Update) — Slice 4

**Contexto**: cuarta slice de la Oleada 3 sobre el mecanismo ya instalado en Slice 1 (proveedor `ShellCommandState` + cableado menú↔`CommandInfo.Enabled`). Alcance decidido: los overrides `Update` **solution-level** del menú Project/Build/Run, cuyo estado depende de "hay solución cargada" (`ShellCommandState.HasWorkspace`). Se investigó primero el alcance real: los handlers Build/Run/Project viven en `ProjectCommands.cs` (que SÍ compila en el build Avalonia), mientras que `BuildCommands`/`RunCommands`/`DebugCommands` no existen en `MonoDevelop.Ide.Commands` (están en otros ensamblados no compilados en el build Avalonia). Se portó el subconjunto solution-level; los gates que requieren build/run in-progress o selección de proyecto quedan diferidos (estado no rastreado por la shell).

**Qué se hizo** (commit `0c3367a73c`, 2 ficheros):
- `ProjectCommands.cs` (solo el bloque Avalonia `#else`; legacy byte-igual): 6 overrides `Update` → `info.Enabled = ShellCommandState.HasWorkspace;` en `BuildSolutionHandler`, `RebuildSolutionHandler`, `CleanSolutionHandler`, `RunHandler`, `RunEntryHandler`, `SolutionOptionsHandler`. Se actualizó el comentario-header del bloque `#else` (los gates solution-level ya están portados; el resto no).
- `MainWindow.axaml.cs`: `DumpCmdStates` extendido — constante `P = "MonoDevelop.Ide.Commands.ProjectCommands."` y 6 campos: `buildSol/rebuildSol/cleanSol/run/solOpts` vía `En()` (menu-walk; están en los menús Build/Run/Project) y `runEntry` vía `CEn()` (lectura directa, porque `RunEntry` no es item del menú principal, solo de context/run-list).

**Decisiones de mapeo**:
- `SolutionOptions`/`CleanSolution` = **bridge exacto** (legacy `CurrentSelectedSolution != null` ⇒ `HasWorkspace`).
- `BuildSolution`/`RebuildSolution`/`Run`/`RunEntry` = **bridge más grueso** (legacy además chequea `CurrentBuildOperation.IsCompleted` / `CurrentRunOperation.IsCompleted` / `CanExecute` / `CurrentSelectedBuildTarget`); esos refinamientos se difieren con el estado de build/run y la selección de proyecto. Diseño acordado (mismo precedente que los gates de zoom/focus de Slice 3).
- `RunEntry` **no es ítem del menú principal** (en `MainMenu.addin.xml` solo aparece `Run`; `RunEntry` va en context/run-list), por eso el E2E lo lee vía `CEn` (estado directo del comando); el override sí está portado y gatea por `HasWorkspace`.
- `HasWorkspace` = `MainWindow.loadedSolutionPath is not null`, el mismo accessor que ya usa `CloseWorkspace` (consistencia).

**Números clave**:
- 6 overrides `Update` portados en esta slice → **37 acumulado en la Oleada 3** (Slice 1: 4, Slice 2: 10, Slice 3: 17, Slice 4: 6); el resto (in-progress build/run, selected-build-target, visibility/dynamic, Tools, Navigation, Undo/Redo) en slices siguientes.
- Catálogo y menú invariantes: `Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, `menu ids total=196 resolved=172 missing=24`, menudump `menus=11 items=225 cmds=0 bindings=205` (byte-igual — el estado habilitado no forma parte del menudump).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (también `net10.0-windows`).
- Oracle (`~/opencode/catload_test/`, sección `oleada3` extendida a 42 ids): los 6 ids nuevos reportan `enabled=False expected=False` (headless ⇒ `HasWorkspace=false`); con los 36 previos → `oleada3: ok=42 fail=0`, exit 0; invariantes `Count=461 LoadedCommands=475 ResolvedHandlers=111`.
- Smoke (`~/opencode/smoke_oleada1.sh`): `--menudump` **byte-igual** (`menus=11 items=225 … _Help=13i/0c`), 0 líneas `[warn]/[fatal]/Exception`.
- E2E (`~/opencode/oleada3_cmdstates.sh`, 2 casos con/sin soln × 7 transiciones initial→opened→opened2→dirty→clean→closed1→closed2): con soln (`hasWs=True`) `buildSol/rebuildSol/cleanSol/run/solOpts/runEntry = 1` en las 7 transiciones; sin soln (`hasWs=False`) los 6 = `0`; `closeWs` 1/0 estable; los estados previos (docCount, zoom, edit) sin regresión. 0 `?`, 0 warn/fatal/exception en ambos casos.

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_qa/`).
- **CERRAR — P1–P5 OK, 0 hallazgos**: P1 `--old-gui` intacta (bloque legacy de `ProjectCommands.cs` byte-igual a HEAD, sha256 idéntico en HEAD y worktree; todo el cambio entre `#else` y `#endif`); P2 los 6 gates en la clase correcta con `HasWorkspace` (mapeo vs legacy verificado; `runEntry` no es item del menú principal, verificado en `MenuService.cs`); P3 `HasWorkspace` = `loadedSolutionPath is not null` (mismo accessor que `CloseWorkspace`); P4 E2E sin regresión (logs ±sln + oracle `ok=42 fail=0` + menudump byte-igual, 0 warn/fatal/Exception); P5 comentarios coherentes, sin dead code/TODO.
- Observación informativa (sin acción): `ClearRecentProjects` gatea por `HasRecentSolutions` (no `HasWorkspace`) — es su gate propio desde Slice 2, no una incoherencia.

**Queda pendiente (slices 5..n de la Oleada 3)**: (1) gates **in-progress** de build/run (`IsBuilding`/`IsRunning`); (2) gates de **selección de build target** (`Build`/`Rebuild`/`ProjectOptions`/`SolutionItemOptions`/`EditReferences`/`SetStartupProjects`); (3) **port de visibility/rename/dynamic** (`OpenDocument1..9`, `CanShowHelp`, `OpenDocumentList`, etiquetas de `Run`/`RunEntry`/`Build`); (4) `Undo`/`Redo` (`CanUndo`/`CanRedo` del editor, hoy no público en `SkTextEditor`); (5) `Tools` (visibility + dynamic-text) y `Navigation` (historial back/forward de documentos). Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–4.

**Cierre**: código en commit `0c3367a73c` pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (todo el nuevo código está bajo `#else`/`AVALONIA_SHELL` y el bloque legacy de `ProjectCommands.cs` es byte-igual al commit anterior).

## 2026-11-05 (7) — Oleada 3 (backend Avalonia): estado de habilitación de menú (Update) — Slice 5

**Contexto**: quinta slice de la Oleada 3. Puntualiza el item (1) de "pendiente" de Slice 4: los **gates in-progress de build/run**. Se investigó el estado real de la shell: no hay operaciones de build/run separadas (legacy `CurrentBuildOperation`/`CurrentRunOperation`), sino **un único proceso compartido** `runningProc` (`System.Diagnostics.Process?`) que usan tanto `RunBuildAsync` (itera proyectos con `dotnet build` por proyecto) como `RunStartupProjectAsync` (run/debug), y que `StopBuildOrRun` mata. Por tanto `IsOperationRunning => runningProc is { HasExited: false }` es el mapeo limpio de "hay build o run in-progress" (el legacy `Stop` = `!CurrentBuildOperation.IsCompleted || !CurrentRunOperation.IsCompleted` queda como bridge exacto).

**Qué se hizo** (3 ficheros, +77/−19):
- `MainWindow.axaml.cs`:
  - Accessor `internal bool IsOperationRunning => runningProc is { HasExited: false };` junto a `HasWorkspace`/`DocumentCount` (región de estado de Oleada 3).
  - Hooks `NotifyCommandStateChanged()` en `RunProcessAsync`: (a) tras `runningProc = proc` + early-return null (inicio — no notifica si el proceso no arrancó), (b) dentro del bloque de completion `Dispatcher.UIThread.Post`, tras `RefreshCodeIssuesPad()` (fin). Con esto el menú se re-evalúa al arrancar y al terminar cualquier build/run.
  - `DumpCmdStates`: añade `stop=` (vía `En()` — `Stop` sí es item del menú Run) e `isOp=` (lectura directa de `IsOperationRunning`).
  - Driver `--cmdstates`: tras la secuencia de documentos añade la subsecuencia in-progress `preop` → `RunProcessAsync("sleep","3")` (mismo path `RunProcessAsync` que un build real; `sleep 3` da un marco de tiempo determinista) → polling `IsOperationRunning` → `operating` → espera salida + 300ms (para que corra el Post de completion) → `postop`.
- `ShellCommandState.cs`: `public static bool IsOperationRunning => MainWindow.Instance?.IsOperationRunning ?? false;`.
- `ProjectCommands.cs` (solo el bloque Avalonia `#else`; legacy byte-igual):
  - 4 `Update` **refinados** (de Slice 4, `HasWorkspace` → `HasWorkspace && !IsOperationRunning`): `BuildSolutionHandler`, `RebuildSolutionHandler`, `RunHandler`, `RunEntryHandler`.
  - 1 `Update` **nuevo**: `StopHandler` → `info.Enabled = ShellCommandState.IsOperationRunning;` (el legacy solo lo deshabilitaba cuando ambas ops estaban completadas).
  - `CleanSolutionHandler`/`SolutionOptionsHandler` **sin busy-guard** (el legacy no lo chequea — bridge exacto se mantiene).
  - Comentario-header del bloque `#else` actualizado (se portaron los gates in-progress; quedan selected-build-target y dynamic labels).

**Decisiones de mapeo**:
- `Stop` = **bridge exacto**: legacy `!buildOp.IsCompleted || !runOp.IsCompleted` ⇒ shell `IsOperationRunning` (un solo proceso cubre ambos).
- `BuildSolution`/`RebuildSolution` = legacy `CurrentSelectedSolution != null && CurrentBuildOperation.IsCompleted` ⇒ `HasWorkspace && !IsOperationRunning`; **bridge más grueso** (documentado): el proceso compartido también cubre run, así que un run en curso deshabilita build (legacy solo deshabilitaba build durante build).
- `Run` = legacy `Workspace.IsOpen && CanRun` + diálogo de confirmación "already running" + etiqueta dinámica "Restart" ⇒ busy guard `HasWorkspace && !IsOperationRunning` (sin diálogo; la etiqueta dinámica no es portable — `MenuBuilder` solo espeja `Enabled`).
- `RunEntry` = legacy `buildTarget != null && !(buildTarget is Workspace) && CanExecute` ⇒ busy guard (la parte in-progress de `CanExecute` queda puenteada por `IsOperationRunning`).
- `runningProc` **no se limpia a null** al terminar: el diseño depende de `HasExited` (un proceso finalizado sigue dando `IsOperationRunning=false`), y evita una carrera con el Post de completion.

**Números clave**:
- 1 override `Update` nuevo + 4 refinados en esta slice → **38 overrides `Update` acumulados en la Oleada 3** (Slice 1: 4, Slice 2: 10, Slice 3: 17, Slice 4: 6, Slice 5: 1 nuevo + 4 refinados).
- Catálogo y menú invariantes: `Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, `menu ids total=196 resolved=172 missing=24`, menudump `menus=11 items=225 cmds=0 bindings=205` (byte-igual — el estado habilitado no forma parte del menudump).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (también `net10.0-windows`).
- Oracle (`~/opencode/catload_test/`, sección `oleada3` extendida a 43 ids con `ProjectCommands.Stop`, expected false headless): `oleada3: ok=43 fail=0`, exit 0; invariantes `Count=461 LoadedCommands=475 ResolvedHandlers=111`.
- Smoke (`~/opencode/smoke_oleada1.sh`): `--menudump` **byte-igual** (`menus=11 items=225 … _Help=13i/0c`), 0 líneas `[warn]/[fatal]/Exception`.
- E2E (`~/opencode/oleada3_cmdstates.sh`, 2 casos con/sin soln × 10 fases — las 7 de documentos + `preop`/`operating`/`postop`):
  - con soln: `preop`/`postop` `buildSol=rebuildSol=cleanSol=run=solOpts=runEntry=1 stop=0 isOp=0`; **`operating` `buildSol=rebuildSol=0 cleanSol=1 run=0 solOpts=1 runEntry=0 stop=1 isOp=1`** (busy guard solo en los 4 comandos gateados; `CleanSolution`/`SolutionOptions` intactos, puente exacto).
  - sin soln: todo `0` en las 10 fases salvo `operating` (`stop=1 isOp=1`).
  - 0 `?`, 0 warn/fatal/exception en ambos casos.

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_slice5_qa/`).
- **CERRAR — P1–P4 OK, 0 hallazgos; 1 P5 preexistente** (fuera de scope): P1 `--old-gui` intacta (bloque legacy de `ProjectCommands.cs` byte-igual; el diff empieza dentro del `#else`); P2 mapeo legacy fiel y documentado (hooks de inicio/fin en el sitio correcto; sin `runningProc=null` en el árbol; `CleanSolution`/`SolutionOptions` sin busy guard); P3 concurrencia coherente (multi-proyecto: los Posts se encadenan FIFO y el estado final refleja el último proceso; Stop: `Kill(true)` → `WaitForExitAsync` completa → Post de fin → gates se re-habilitan; `runningProc` escrito y leído solo en UI); P4 E2E sin regresión (2 casos × 10 fases + oracle `ok=43 fail=0` + menudump byte-igual, 0 warn/fatal/Exception); P5 (aviso) mensaje preexistente en el flujo Stop: al matar el proceso la salida no es 0 y el Post de fin escribe "Build FAILED with exit code N" en el pad de errores (líneas preexistentes, sin tocar por este diff) — proponer como mejora posterior, no bloquea.
- Nota informativa del QA: `main/build/MonoDevelop.Ide.dll` es del 2 nov (pre-Oleada 2) y no contiene `IsOperationRunning`; no es defecto — el csproj del shell compila las fuentes de comandos directamente en `MonoDevelop.AvaloniaShell.dll`, que sí fue reconstruida y contiene ambos símbolos.

**Queda pendiente (slices 6..n de la Oleada 3)**: (1) gates de **selección de build target** (`Build`/`Rebuild`/`ProjectOptions`/`SolutionItemOptions`/`EditReferences`/`SetStartupProjects`); (2) **port de visibility/rename/dynamic** (`OpenDocument1..9`, `CanShowHelp`, `OpenDocumentList`, etiquetas de `Run`/`RunEntry`/`Build`); (3) `Undo`/`Redo` (`CanUndo`/`CanRedo` del editor, hoy no público en `SkTextEditor`); (4) `Tools` (visibility + dynamic-text) y `Navigation` (historial back/forward de documentos); (5) mejora P5: mensaje de error tras `Stop` legítimo. Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–5.

**Cierre**: código en commit (3 ficheros) pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (todo el nuevo código está bajo `#else`/`AVALONIA_SHELL` y el bloque legacy de `ProjectCommands.cs` es byte-igual al commit anterior).

## 2026-11-05 (8) — Oleada 3 (backend Avalonia): estado de habilitación de menú (Update) — Slice 6

**Contexto**: sexta slice de la Oleada 3. Puntualiza el item (1) de "pendiente" de Slice 5: los **gates de selección de build target** del menú principal (`Build`/`Rebuild`/`Clean`, Project Options, Set Startup Projects). El legacy gatea por `CurrentSelectedBuildTarget != null` (un proyecto seleccionado en el pad), pero la shell no tiene selección de pad: el puente elegido es "la solución cargada tiene proyectos" — `MainWindow.ActiveProjectCount`, el recuento de proyectos de la solución cargada (el mismo conjunto que itera el build). `SolutionItemOptions`/`EditReferences` quedan diferidos: son context-only (menú sobre un nodo del árbol de solución) y exigen un modelo de nodo de contexto que la shell aún no tiene.

**Qué se hizo** (3 ficheros, +108/−2):
- `MainWindow.axaml.cs`:
  - Accessor `internal int ActiveProjectCount` (región de estado de Oleada 3, junto a `HasWorkspace`/`DocumentCount`/`IsOperationRunning`): **memoizado por `loadedSolutionPath`** (campos `activeProjectCountSln`/`activeProjectCountValue`); 0 si no hay solución o el `.sln` no existe en disco (`File.Exists`); rescaneo `SolutionProjects(sln).Count` solo cuando el path cambia — los gates leen el valor en cada refresh de estado de comandos (incluidos los cambios de dirty por tecla), así que un rescaneo por refresh golpearía el disco cada vez.
  - Invalidaciones del memo (`activeProjectCountSln = null`) en todos los puntos donde el conjunto de proyectos puede cambiar: `RefreshSolutionTree()` (añadir/quitar/renombrar), `CloseWorkspaceAsync` (cierre de workspace), `OpenNewSolutionDialogAsync` (ruta de producción de añadir proyecto nuevo: tras `AppendProjectToSolution`, antes del `OpenSolutionInWindow` que early-returns por path igual — **fix del P3 del QA #1**) y el driver QA `--newproject` (mismo patrón — **fix del H1 del QA final**).
  - `DumpCmdStates`: campos nuevos `buildProj=/rebuildProj=/cleanProj=/projOpts=/setStartup=` (vía `En()` — los 5 son items de los menús Build/Project) y `projCount=` (lectura directa del accessor).
- `ShellCommandState.cs`: `public static int ActiveProjectCount => MainWindow.Instance?.ActiveProjectCount ?? 0;` (headless ⇒ 0 por null-coalesce).
- `ProjectCommands.cs` (solo el bloque Avalonia `#else`; legacy byte-igual): 5 `Update` nuevos:
  - `BuildHandler`/`RebuildHandler`/`CleanHandler` (comandos `Build`/`Rebuild`/`Clean`) → `HasWorkspace && ActiveProjectCount > 0 && !IsOperationRunning`.
  - `ProjectOptionsHandler`/`SetStartupProjectsHandler` → `HasWorkspace && ActiveProjectCount > 0` (sin busy guard).
  - Comentario-header del bloque `#else` actualizado (gates project-level portados como puente a `ActiveProjectCount`; quedan las etiquetas dinámicas y los context-only `SolutionItemOptions`/`EditReferences`).

**Decisiones de mapeo**:
- Legacy `CurrentSelectedBuildTarget != null` (proyecto seleccionado en el pad) ⇒ `ActiveProjectCount > 0` (**puente más grueso, documentado** por handler: la shell no tiene selección de pad, así que "la solución tiene proyectos" hace las veces de "hay build target seleccionado").
- `CanBuild` (legacy Build/Rebuild/Clean) no está modelado en la shell ⇒ omitido, documentado.
- Busy guard `!IsOperationRunning` en Build/Rebuild/Clean: en Build/Rebuild es el puente del legacy `CurrentBuildOperation.IsCompleted`; en `Clean` es **endurecimiento añadido** (el legacy Clean no chequea in-progress, a diferencia de Build/Rebuild — documentado por handler).
- `SetStartupProjects` legacy exige >= 2 proyectos (multi-startup) ⇒ matiz descartada: el flujo de la shell marca un único startup project (documentado).
- Etiquetas dinámicas (`"{0} _Options"`, `"Build {0}"`) no portables: `MenuBuilder` solo espeja `CommandInfo.Enabled`.
- `SolutionItemOptions`/`EditReferences` = **context-only** ⇒ diferidos (requieren modelo de nodo de contexto).

**Números clave**:
- 5 overrides `Update` nuevos en esta slice → **43 overrides `Update` acumulados en la Oleada 3** (Slice 1: 4, Slice 2: 10, Slice 3: 17, Slice 4: 6, Slice 5: 1 nuevo + 4 refinados, Slice 6: 5).
- Catálogo y menú invariantes: `Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, `menu ids total=196 resolved=172 missing=24`, menudump `menus=11 items=225 cmds=0 bindings=205` (byte-igual — el estado habilitado no forma parte del menudump).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (también `net10.0-windows`).
- Oracle (`~/opencode/catload_test/`, sección `oleada3` extendida a 48 ids — añade Build/Rebuild/Clean/ProjectOptions/SetStartupProjects, expected false headless): `oleada3: ok=48 fail=0`, exit 0; invariantes `Count=461 LoadedCommands=475 ResolvedHandlers=111`.
- Smoke (`~/opencode/smoke_oleada1.sh`): `--menudump` **byte-igual**, 0 líneas `[warn]/[fatal]/Exception`.
- E2E (`~/opencode/oleada3_cmdstates.sh`, **3 casos** × 10 fases — los 2 de Slice 5 + caso nuevo con solución "ghost"):
  - withsln (`Fixture.sln`, 3 proyectos): `hasWs=True projCount=3`; `buildProj/rebuildProj/cleanProj/projOpts/setStartup = 1`; fase `operating` (`isOp=1`): `buildProj/rebuildProj/cleanProj = 0` (busy guard) pero `projOpts/setStartup = 1` (sin guard, por diseño) y `stop=1`; `postop` recupera.
  - emptysl (fixture nuevo `~/opencode/oleada3_slice6_empty/Empty.sln`: su único proyecto referenciado no existe en disco): `hasWs=True projCount=0` y los 5 gates = `0` en todas las fases (el filtro `File.Exists` descarta el ghost — una solución cargada no implica proyectos buildables).
  - nosln: `hasWs=False projCount=0`, 5 gates = `0`.
  - 0 `?`, 0 warn/fatal/exception; `[cmdstates] done` en los 3 casos.

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_slice6_qa/`).
- **QA #1 — NO CERRAR**: P3 (hueco de invalidación: la ruta de producción de añadir proyecto, `AppendProjectToSolution` en `OpenNewSolutionDialogAsync`, no invalidaba el memo — como `OpenSolutionInWindow` early-returns por path igual, el conteo quedaba obsoleto N vs N+1 tras añadir un proyecto) + 2 P5 menores (comentarios). El agente dev aplicó el fix (invalidación en `OpenNewSolutionDialogAsync`).
- **QA final (re-QA) — CERRAR** (P1–P4 OK): P1 `--old-gui` intacta (región legacy ln 1–630 byte-igual a HEAD, diff exit 0; todos los hunks tras el `#else`; build 0 errores ambos TFMs); P2 los 5 `Update` en las clases correctas con los gates esperados y el mapeo legacy documentado (incl. endurecimiento de Clean y matiz multi-startup); P3 correctitud del memo (todos los puntos de mutación de producción invalidan/re-clavean; fix del QA #1 validado; headless ok=48); P4 E2E sin regresión (3 casos × 10 fases + oracle `ok=48 fail=0` + menudump byte-igual). 1 hallazgo P5 no bloqueante: **H1** — el mismo hueco de invalidación en la **vía driver QA `--newproject`** (`AppendProjectToSolution` sin invalidación; solo afecta a sesiones lanzadas con ese flag; la ruta de producción estaba corregida).
- H1 resuelto en el mismo ciclo: el dev añadió la invalidación en la vía `--newproject` (mismo patrón que la ruta de producción) y re-verificó: build `0 Errores` en ambos TFMs, oracle `ok=48 fail=0`, menudump byte-igual. Estado final: **0 hallazgos abiertos**.

**Queda pendiente (slices 7..n de la Oleada 3)**: (1) `SolutionItemOptions`/`EditReferences` (context-only — requieren un modelo de nodo de contexto sobre el árbol de solución que la shell aún no tiene); (2) la **port de visibility/rename/dynamic** (`OpenDocument1..9`, `CanShowHelp`, `OpenDocumentList`, etiquetas dinámicas de `Run`/`RunEntry`/`Build`); (3) `Undo`/`Redo` (`CanUndo`/`CanRedo` del editor, hoy no público en `SkTextEditor`); (4) `Tools` (visibility + dynamic-text) y `Navigation` (historial back/forward de documentos); (5) mejora P5: mensaje de error tras `Stop` legítimo; (6) P5 preexistente: el early-return de `OpenSolutionInWindow` (mismo `solutionLoaded && path`) traga el refresh — tras añadir un proyecto el árbol Xwt de solución no se re-puebla (preexistente desde `36e657221c`, fuera del scope de esta slice; la invalidación de `ActiveProjectCount` mantiene el estado de menú correcto, pero el árbol visible no se actualiza). Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–6.

**Cierre**: código en commit (3 ficheros) pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (todo el nuevo código está bajo `#else`/`AVALONIA_SHELL` y el bloque legacy de `ProjectCommands.cs` es byte-igual al commit anterior).

## 2026-11-05 (9) — Oleada 3 (backend Avalonia): estado de habilitación de menú (Update) — Slice 7

**Contexto**: séptima slice de la Oleada 3. Cierra el item (3) de "pendiente" de Slice 6: los **gates de `Undo`/`Redo`** sobre `CanUndo`/`CanRedo` del editor activo. El legacy gatea con `IUndoHandler.EnableUndo/EnableRedo` del contenido del documento enfocado (`ViewCommandHandlers.cs`, solo `--old-gui`, fuera del build Avalonia). Matiz clave de esta slice: a diferencia de las slices 2–6, `Undo`/`Redo` **no tienen `defaultHandler` en el `Commands.addin.xml` compartido** (los handlers GTK viven en `ViewCommandHandlers.cs`, que el shell no compila), así que el mecanismo de port no es solo un `Update` en el bloque `#else`: hace falta **enlazar el handler al id del catálogo** sin tocar el XML compartido (tocarlo rompería `--old-gui`, donde la clase no existiría en el ensamblado legacy).

**Qué se hizo** (5 ficheros, +103/−2):
- `SkTextEditor.cs` (compilado en la shell; NO incluido en el csproj legacy):
  - `public bool CanRedo => redoStack.Count > 0;` (ya existía `CanUndo`).
  - `public event Action? UndoStateChanged;` disparado en las tres mutaciones de stack que importan: la ruta de **añadir** de `PushUndo` (tras `redoStack.Clear()`; no en los early-returns `undoing`/`before == joined`), al final de `Undo()`, y al final de `Redo()`.
- `MainWindow.axaml.cs`:
  - `internal SkTextEditor? ActiveTextEditor` (mismo lookup que `WithActiveEditor`) + `internal bool CanUndo`/`CanRedo` (`?? false`).
  - `AddDocument`: `ed.UndoStateChanged += NotifyCommandStateChanged;` (mismo patrón que la suscripción `IsDirtyProperty` existente).
  - `DumpCmdStates`: campos nuevos `undo=`/`redo=` (vía `En()` — ambos son items del menú Edit; valida el espejo completo Update→Estado→Menú, no solo `GetCommandInfo`).
  - Driver QA `--cmdstates`: 6 fases nuevas tras "clean" — `typed` (InsertAtCaret en "QA Cmd"), `otheractive` (activa "QA Cmd2", stack vacío), `undone`, `redone`, `undoneall`, `restored` (Redo + `IsDirty=false`, para que el cierre posterior no pida guardar).
- `ShellCommandState.cs`: `CanUndo`/`CanRedo` vía `MainWindow.Instance?.CanUndo/CanRedo ?? false` (headless ⇒ false).
- `EditCommands.cs` (solo el bloque Avalonia `#else`; legacy byte-igual): `UndoHandler`/`RedoHandler` (Run → `ShellCommandHandler.Run(EditCommands.Undo/Redo)`; Update → `ShellCommandState.CanUndo/CanRedo`); comentario-header del bloque actualizado (decía "Update overrides not ported yet", obsoleto desde Slice 2).
- `Program.cs` (tras `LoadCatalog`): `GetActionCommand("…EditCommands.Undo").DefaultHandlerType = typeof(UndoHandler)` y lo mismo para `Redo` (guard `is { }`). Post-load a propósito: `ResolvedHandlers=111` se calcula en el `Load` y no cambia (la línea `[commands]` del arranque queda idéntica).

**Decisiones de mapeo**:
- Legacy `IUndoHandler.EnableUndo/EnableRedo` (contenido del documento activo) ⇒ `ActiveTextEditor != null && CanUndo/CanRedo` (**stack-based, fiel a `TextDocument.CanUndo`**); simplificación documentada: el MVP de `SkTextEditor` no tiene `currentAtomicOperation` (la legacy cuenta también una operación atómica en curso).
- El gate sigue al **editor activo** (pestaña seleccionada), no al último editor editado: `otheractive` lo valida (activar una pestaña con stack vacío deshabilita Undo aunque otra pestaña tenga undo disponible).
- Comportamiento derivado aceptado: al registrar `DefaultHandlerType`, `DispatchCommand` ejecuta `Update` antes de `Run` y **bloquea si está disabled** (un clic en Undo disabled no hace nada) — coherente con el resto de comandos portados.
- Enlazado post-load en `Program.cs` (no en el XML): el XML compartido no puede referenciar una clase que solo existe en el ensamblado Avalonia.

**Números clave**:
- 2 overrides `Update` nuevos en esta slice → **45 overrides `Update` acumulados en la Oleada 3** (Slice 1: 4, Slice 2: 10, Slice 3: 17, Slice 4: 6, Slice 5: 1 nuevo + 4 refinados, Slice 6: 5, Slice 7: 2).
- Catálogo y menú invariantes: `Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, `menu ids total=196 resolved=172 missing=24`, menudump `menus=11 items=225 cmds=0 bindings=205` (byte-igual).
- Oracle: 48 → **49 ids** (`Undo` pasa de control "routed" expected=true a wired expected=false; se añade `Redo` expected=false).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (también `net10.0-windows`).
- Oracle (`~/opencode/catload_test/`, réplica del wiring vía reflexión — las clases son `internal` y el test referencia el mismo `MonoDevelop.AvaloniaShell.dll`): `oleada3: ok=49 fail=0`, exit 0; línea `slice7: … wired=True`; invariantes `Count=461 LoadedCommands=475 ResolvedHandlers=111`.
- Smoke (`~/opencode/smoke_oleada1.sh`): `--menudump` **byte-igual**, 0 líneas `[warn]/[fatal]/Exception`.
- E2E (`~/opencode/oleada3_cmdstates.sh`, **3 casos** × 16 fases — 10 previas + 6 nuevas):
  - Fases nuevas (iguales en los 3 casos): `typed` `undo=1 redo=0` (`hasDirty=True`); `otheractive` `undo=0 redo=0` (el gate sigue al editor activo); `undone` `undo=0 redo=1`; `redone` `undo=1 redo=0`; `undoneall` `undo=0 redo=1`; `restored` `undo=1 redo=0` (`hasDirty=False`).
  - Sin regresión: fases previas `undo=0 redo=0`, `operating` `stop=1`, `closeWs` 1/1/0, `postop` recupera; línea `[commands] catalog loaded: 475 commands, 111 with resolvable default handler` idéntica.

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_slice7_qa/`, veredicto en `veredicto.md`). Un solo ciclo: **CERRAR**, **0 hallazgos P1–P4** — P1 `--old-gui` intacta (diff íntegro bajo `AVALONIA_SHELL`/`#else` o en ficheros exclusivos del shell; `Commands.addin.xml` no modificado; `SkTextEditor.cs` no está en la lista explícita del csproj legacy); P2 los 2 `Update` en las clases correctas con el mapeo legacy documentado; P3 el evento `UndoStateChanged` se dispara en todas y solo las mutaciones de stack que importan; P4 E2E sin regresión (3 casos × 16 fases con matriz exacta + oracle `ok=49 fail=0` + menudump byte-igual). Hallazgos P5, todos preexistentes u observaciones (no de esta slice): (P5-1) **Ctrl+Z/Ctrl+Y no funcionan como atajos** — el early-return de `isTextEditingCombo` en `MainWindow.OnKeyDown` traga Ctrl+Z y `SkTextEditor.OnKeyDown` no gestiona Z/Y; Undo/Redo son hoy solo por menú (nuevo pendiente registrado); (P5-2) pendientes P5 previas de la Oleada 3 (post-Stop, early-return de `OpenSolutionInWindow`, context-only); (P5-3) la suscripción no se desuscribe en el cierre (patrón idéntico al preexistente `PropertyChanged`); (P5-4) simplificación stack-based documentada.

**Queda pendiente (slices 8..n de la Oleada 3)**: (1) `SolutionItemOptions`/`EditReferences` (context-only — requieren un modelo de nodo de contexto sobre el árbol de solución); (2) la **port de visibility/rename/dynamic** (`OpenDocument1..9`, `CanShowHelp`, `OpenDocumentList`, etiquetas dinámicas de `Run`/`RunEntry`/`Build`); (3) `Tools` (visibility + dynamic-text) y `Navigation` (historial back/forward de documentos); (4) mejora P5: mensaje de error tras `Stop` legítimo; (5) P5 preexistente: el early-return de `OpenSolutionInWindow` (mismo `solutionLoaded && path`) traga el refresh; (6) **nuevo P5 preexistente (confirmado por el QA de esta slice)**: los atajos Ctrl+Z/Ctrl+Y están tragados por `isTextEditingCombo` del window y no existen en `SkTextEditor.OnKeyDown` — portarlos implicaría añadir los casos Z/Y en el editor (el gate validado en esta slice ya funciona por `DispatchCommand`, que bloquea si está disabled). El item (3) de la lista anterior (`Undo`/`Redo`) queda **cerrado por esta slice**. Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–7.

**Cierre**: código en commit (5 ficheros) pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (todo el nuevo código está bajo `#else`/`AVALONIA_SHELL` o en ficheros exclusivos del shell; `SkTextEditor.cs` no se compila en el build legacy).

## 2026-11-06 (10) — Oleada 3 (backend Avalonia): estado de habilitación de menú (Update) — Slice 8

**Contexto**: octava slice de la Oleada 3. Cierra el item (1) de "pendiente" de Slice 7: los **gates context-only de `SolutionItemOptions` (Options) y `EditReferences` (Add Reference)** — los dos comandos que el legacy gatea por la **selección** del pad de solución (`CurrentSelectedBuildTarget != null` y `CurrentSelectedProject is DotNetProject`, respectivamente), a diferencia de los gates del menú principal de Slice 6 (bridge más grueso por `ActiveProjectCount`). Esta slice construye el **bridge exacto** sobre la selección del pad de la shell. El trabajo destapó (y corrigió en el submódulo `xwt`) un **bug preexistente** que bloqueaba el E2E: `TreeStoreBackend.GetChild` (`Xwt.Avalonia/ListBackends.cs`) no acotaba el índice, así que `TreeNavigator.MoveToChild` sobre una hoja lanzaba `ArgumentOutOfRangeException` y rompía cualquier recorrido completo del store. La hipótesis de backslash (paths sin normalizar en `Microsoft.Build.dll`) quedó **descartada** por un probe vía reflexión sobre ambas DLL (`main/build` y SDK 10.0.401): los paths ya llegan con slash limpio.

**Qué se hizo** (4 ficheros + 1 en el submódulo `xwt`, +169/−8):
- `Xwt.Avalonia/ListBackends.cs` (submódulo `main/external/xwt`; +8/−1): `TreeStoreBackend.GetChild` ahora acota (`index >= 0 && index < children.Count`) y devuelve `null` fuera de rango — el contrato de `ITreeStoreBackend` (idéntico al backend Gtk, `TreeStoreBackend.cs:94`), en el que `MoveToChild` se apoya para reportar "sin hijos". El indexer sin acotar convertía cada hoja en excepción y rompía los recorridos completos del store (búsqueda por tag, `FindNavigators`, `RemoveChildren`).
- `ShellCommandState.cs` (+13): `SelectedNodeIsBuildTarget`/`SelectedNodeIsProject` vía `MainWindow.Instance` (headless ⇒ false).
- `MainWindow.axaml.cs` (+117/−3):
  - Accessors `internal SelectedNodeIsBuildTarget` (nodo "Solution" o "Project") y `SelectedNodeIsProject` ("Project") — la shell no tiene solution folders, así que build target = fila de solución o proyecto.
  - `SelectionChanged` (tree Xwt): `UpdatePropertiesPad()` **y** `NotifyCommandStateChanged()` (QA F-1: sin ese refresco el item tracked "Add Reference..." quedaba con estado obsoleto tras cambiar la selección).
  - `OnMenuCommand`: caso nuevo `ProjectCommands.Options` ("Properties" del menú contextual del pad — strip del prefijo `solution:`/`project:`/`folder:` y reporte del objetivo; el panel de opciones completo es el port de Preferences); caso `ProjectCommands.AddReference` actualizado: usa el nodo de proyecto del pad cuando el comando llega con `nodeContext` (`project:<path>` — solo el menú contextual del pad), si no el proyecto activo (fallback legacy; guard `File.Exists` para el fantasma).
  - `DumpCmdStates`: campos nuevos `solItemOpts=` (vía `CEn`, `GetCommandInfo` directo) y `editRefs=` (vía `En`, el `IsEnabled` del propio `MenuItem` tracked tras el `RefreshCommandStates` inicial del dump — valida el espejo completo, no solo el handler).
  - Driver QA `--cmdstates`: 3 fases node nuevas (`node_solution`, `node_project`, `node_file`) — el proyecto se elige con un **walker determinístico del store** (función local `CollectProjects` que recorre `xwtSolutionStore` y recolecta tags `project:` en orden de solución, sustituyendo un disk scan no determinístico) y el archivo es el primer `.cs` bajo el proyecto (las filas de archivo llevan el path de disco como tag). Instrumentación `itemRefs=`: el `IsEnabled` real del `MenuItem` tracked **inmediatamente** tras `SelectXwtSolutionRow`, sin refresh (test de regresión de F-1). El estado "sin selección" lo cubren todas las fases previas.
- `ProjectCommands.cs` (solo el bloque `#else` Avalonia; legacy byte-igual; +31/−4): `SolutionItemOptionsHandler.Update` → `info.Enabled = ShellCommandState.SelectedNodeIsBuildTarget`; `EditReferencesHandler.Update` → `Enabled=false` + `info.Bypass=true` cuando el gate está off (espejo del comportamiento legacy del keybinding), `Bypass=false` en el otro brazo (el `Update` asimétrico es correcto: `CommandInfo.Enabled` default `true` y `GetCommandInfo` crea instancia fresca por llamada); comentario-header del bloque actualizado.

**Decisiones de mapeo**:
- **Bridge exacto (a diferencia de Slice 6)**: el nodo seleccionado del pad clasifica filas Solution/Project igual que el tipo de item `IBuildTarget` legacy; cada fila de proyecto de la shell es un .csproj ⇒ `CurrentSelectedProject is DotNetProject` ⇒ `SelectedNodeIsProject`.
- **F-1 (QA ronda 1, P3 — introducido por esta slice)**: el gate sobre el item tracked necesitaba su trigger de refresco; fix de 1 línea (`NotifyCommandStateChanged()` en `SelectionChanged`) + instrumentación `itemRefs` como test de regresión (`0→1→0` en el E2E, sin refresh).
- **Desviación aceptada (F-5a)**: el proyecto fantasma (solución que referencia un .csproj ausente en disco) tiene fila de proyecto en el pad ⇒ `editRefs=1` con `projCount=0` — el gate lee la selección del pad, no la existencia en disco; el fallback de ejecución (`File.Exists` → `ResolveActiveProject()`) cubre el caso.
- **F-5d (preexistente, documentado)**: ejecutar "Add Reference..." desde el **menú principal** actúa sobre el proyecto **activo**, no sobre la selección del pad (`nodeContext` solo lo lleva el menú contextual del pad; el clic principal va por `CommandService.Dispatch` → `EditReferencesHandler.Run` → `ShellCommandHandler.Run` sin contexto). Gate por selección del pad, ejecución por proyecto activo; el legacy apuntaba a `CurrentSelectedProject` en ambas rutas. Queda para un slice futuro (recomendación del QA).

**Números clave**:
- 2 overrides `Update` nuevos en esta slice → **47 overrides `Update` acumulados en la Oleada 3** (Slice 1: 4, Slice 2: 10, Slice 3: 17, Slice 4: 6, Slice 5: 1 nuevo + 4 refinados, Slice 6: 5, Slice 7: 2, Slice 8: 2).
- Catálogo y menú invariantes: `Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, menudump `menus=11 items=225 cmds=0 bindings=205` (byte-igual).
- Oracle: 49 → **51 ids** (2 nuevos: `SolutionItemOptions` y `AddReference`, ambos expected=false — sin solución cargada ⇒ sin selección de pad).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (también `net10.0-windows`).
- Oracle (`~/opencode/catload_test/`): `oleada3: ok=51 fail=0`, exit 0; invariantes `Count=461 LoadedCommands=475 ResolvedHandlers=111`.
- Smoke (`~/opencode/smoke_oleada1.sh`): `--menudump` **byte-igual**, 0 líneas `[warn]/[fatal]/Exception`.
- E2E (`~/opencode/oleada3_cmdstates.sh`, **3 casos** — 16 fases previas + 3 node, las node solo con solución cargada):
  - withsln (`Fixture.sln`: App + LibA + LibB): `node select=True` ×3; `itemRefs` **0→1→0** (en vivo, sin refresh); dumps `node_solution` `solItemOpts=1 editRefs=0`, `node_project` `1/1`, `node_file` `0/0` (App = 1.º proyecto en orden de solución; file = `App/Program.cs`).
  - emptysl (`Empty.sln`, `Ghost/Ghost.csproj` ausente en disco): `node_solution` `1/0`; `node_project` (ghost) `1/1` (desviación F-5a aceptada); sin `node_file` (no hay `.cs` en disco).
  - nosln: sin fases node, `done`.
  - 0 `Unhandled`/`Exception` en los logs; fases previas intactas (sin regresión).

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_slice8_qa/`, veredicto en `veredicto.md`). Dos rondas: ronda 1 **RECHAZADO** — 1 P3 abierto (F-1: el item de menú principal "Add Reference..." quedaba con `Enabled` obsoleto tras cambiar la selección del pad — la slice portó el gate sin portar el trigger de refresco; el E2E no lo detectaba por diseño porque el dump medía el `Update` en frío, no el `IsEnabled` espejado en el `MenuItem`) → el dev aplicó el fix de 1 línea + la instrumentación de regresión → ronda 2 **APROBADO**, **0 hallazgos P1–P4** — P1 `--old-gui` intacta (diff íntegro bajo `AVALONIA_SHELL`/`#else` o en ficheros exclusivos del shell; el fix de `xwt` solo acorta el backend Avalonia al contrato `ITreeStoreBackend`/Gtk existente y sus consumidores solo se hacen más seguros); P2 los 2 `Update` en las clases correctas con el mapeo legacy documentado; P3 sin recursión ni trabajo excesivo en el fix; P4 E2E completo con `itemRefs` (3 casos) + oracle `ok=51 fail=0` + menudump byte-igual + builds 2 TFMs 0 errores. P5 (5, informativos, no bloqueantes): F-5a fantasma `editRefs=1` (desviación aceptada); F-5b fase `node_folder` omitida del driver (los fixtures no contienen solution folders); F-5c fase `node_ref` omitida (los fixtures no exponen filas `reference:` seleccionables); F-5d inconsistencia gate-vs-ejecución preexistente del menú principal (diferida a slice futura); F-5e nomenclatura cosmética (`SolutionOptionsHandler` en `#else` vs `SolutionItemOptionsHandler` legacy).

**Queda pendiente (slices 9..n de la Oleada 3)**: (1) la **port de visibility/rename/dynamic** (`OpenDocument1..9`, `CanShowHelp`, `OpenDocumentList`, etiquetas dinámicas de `Run`/`RunEntry`/`Build` y de `ProjectOptionsHandler`); (2) `Tools` (visibility + dynamic-text) y `Navigation` (historial back/forward de documentos); (3) mejora P5: mensaje de error tras `Stop` legítimo; (4) P5 preexistente: el early-return de `OpenSolutionInWindow` (mismo `solutionLoaded && path`) traga el refresh; (5) P5 preexistente (Slice 7): los atajos Ctrl+Z/Ctrl+Y están tragados por `isTextEditingCombo` del window y no existen en `SkTextEditor.OnKeyDown`; (6) **nuevo (QA F-5d)**: tejer la selección del pad en la dispatch del menú principal de "Add Reference..." (o documentar el gate como "estado por selección del pad, ejecución por proyecto activo"); (7) cosmético (QA F-5e): renombrar `SolutionOptionsHandler` (rama `#else`) para no confundirlo con el comando `SolutionOptions`. El item (1) de la lista anterior (`SolutionItemOptions`/`EditReferences`, context-only) queda **cerrado por esta slice**. Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–7.

**Cierre**: código en commit (3 ficheros + puntero del submódulo) y en el submódulo `xwt` (1 fichero), pushados a `origin/agents/sub-agent-senior-dev-role-report` y a su remoto; handoff + veredicto del QA en el commit de docs que le sigue. La ruta `--old-gui` no se ve afectada: los cambios en ficheros compartidos están bajo `#else`/`AVALONIA_SHELL` (bloques legacy byte-iguales) y el fix de `xwt` acota `TreeStoreBackend` (backend exclusivo Avalonia) al contrato del backend Gtk.

## 2026-11-06 (11) — Oleada 3 (backend Avalonia): estado de habilitación de menú (Update) — Slice 9: visibility/rename/dynamic

**Contexto**: novena slice de la Oleada 3. Cierra el item (1) de "pendiente" de Slice 8: el **port de visibility/rename/dynamic al shell Avalonia** — todo lo que los handlers legacy `Update` hacen más allá del booleano `Enabled`: visibilidad/rename/checked de `OpenDocument1..9`, el array `OpenDocumentList` (documentos 10+), `OpenWindowList`, el gate `CanShowHelp` y las etiquetas dinámicas de `Build`/`Rebuild`/`Clean`/`Run`/`RunEntry`/`ProjectOptions`. La slice completa el mecanismo de espejo de estado en `MenuBuilder` (visible, header dinámico, glyph checked, submenús dinámicos) y añade la rama de **array commands** al `CommandManager` del shell.

**Qué se hizo** (7 ficheros, +486/−104):
- `CommandManager.cs` (sección `#if AVALONIA_SHELL`; +12): `GetCommandInfo` detecta ahora array commands (`cmd.CommandArray`) → crea `info.ArrayInfo = new CommandArrayInfo (info)` y llama `DefaultHandler.InternalUpdate (info.ArrayInfo)` (espejo de la rama legacy, ln 1876–1885); el camino scalar queda intacto.
- `MenuBuilder.cs` (solo shell; +99/−6):
  - `RefreshCommandStates` espeja ahora **4 estados**: `IsEnabled` (Slice 1), `IsVisible` (nuevo), **Header dinámico** (nuevo: `info.Text != cmd.Text ? info.Text : originalHeaders[item]` — solo cuando el handler renombró de verdad, restaurando el header del modelo en caso contrario; el label de catálogo puede diferir del del modelo, p. ej. "Activate 1" vs "_1") y **Checked** (nuevo: glyph ✓ solo en items `checkEligible` — sin icon en modelo y sin model-checked).
  - **Submenús dinámicos** (nuevo): `BuildItem` detecta array commands vía `CommandService.Manager.GetActionCommand (id)?.CommandArray == true` → suprime el `Click` del item y lo registra en `dynamicSubmenus`; `RebuildArrayChildren` hace **full rebuild en cada refresh** (headers, checked y el `DataItem` de dispatch salen siempre del `CommandArrayInfo` actual — ver QA F1), el click del hijo hace `CommandService.Dispatch (id, dataItem, CommandSource.MainMenu)` y el **master se oculta con el array vacío** (`item.IsVisible = arr is { Count: > 0 }` — ver QA F2).
- `WindowCommands.cs` (solo bloque `#else`; +174): `OpenDocumentHandlerBase (int index)` + 9 subclasses thin `OpenDocument1..9` (`Update`: `DocumentCount >= index` → rename al título, `Checked = IsDocumentActive (index)`, `Visible/Enabled = true` + description; si no `Visible/Enabled = false`; `Run` → `ShellCommandHandler.Run` con el id literal); `OpenDocumentListHandler.Update (CommandArrayInfo)` (port del legacy: docs 10+ como hijos con `DataItem = DocumentTagAt (i+1)`; se descarta el branch `AccelKey` legacy —dead code— y el markup azul GTK); `OpenWindowListHandler` (ventana única → 1 hijo "Window 1"); cabecera del bloque actualizada.
- `HelpCommands.cs` (solo `#else`; +11): `HelpHandler.Update` → `info.Visible = true` (degeneración documentada del gate `CanShowHelp`, la shell no tiene `HelpOperations`).
- `ProjectCommands.cs` (solo `#else`; +97/−): 6 etiquetas dinámicas vía `GettextCatalog` (shim `CoreGettextShim`, fallback al msgid): `ProjectOptions` (`SelectedNodeIsProject ? name : null` → `"{0} _Options"` / `"Project _Options"`), `Build`/`Rebuild`/`Clean` (`"B_uild {0}"`/`"R_ebuild {0}"`/`"C_lean {0}"` si `SelectedBuildTargetName` non-null), `Run` (`IsOperationRunning` → "Restart Without Debugging" / "Start Without Debugging"), `RunEntry` (**bridge solo por selección del pad**: `SelectedNodeIsSolution` → "Run Solution", `SelectedNodeIsProject` → "Run Project", si no catálogo — **sin** fallback a `ActiveProjectCount`, decisión de diseño documentada).
- `ShellCommandState.cs` (+31): 5 accessors nuevos — `SelectedNodeIsSolution`, `DocumentTitleAt (int)` (1-based), `DocumentTagAt (int)`, `IsDocumentActive (int)` y `SelectedBuildTargetName` (memoizado con el mismo contrato y los mismos 4 puntos de invalidación que `ActiveProjectCount` de Slice 6).
- `MainWindow.axaml.cs` (+166/−4): accessors `internal` (incluida `SelectedBuildTargetName`: selección del pad Solution/Project con fallback a `ResolveActiveProject`); caso `OnMenuCommand` para `OpenDocumentList` (`nodeContext`/dataItem válido → `SelectDocument`); `DumpCmdStates` ampliado con **16 campos añadidos al final** (orden previos intacto): `doc1Vis doc2Vis doc1Txt doc1Chk doc2Chk helpVis buildTxt rebuildTxt cleanTxt runTxt runEntryTxt projOptsTxt docListN winListN activeDoc docList0`; accessor `ActiveDocumentTitle` (QA F1); driver `--cmdstates`: 4 fases nuevas — `docs12` (abre 12 docs), `docs12sel` (dispatch del hijo con dataItem "QA List1"), `docs12swap` (intercambio de 2 docs manteniendo count=12 — regresión de QA F1) y `docs12swapsel` (dispatch "QA List13").

**Decisiones de mapeo**:
- **RunEntry bridge solo por selección del pad** (sin fallback a `ActiveProjectCount`): port más fiel del legacy, que resuelve por `CurrentSelectedBuildTarget`. Consecuencia (QA F3, informativo): con un **file** seleccionado la shell pinta el label de fallback (`B_uild LibA`/`Project _Options`/`_Run Item`) en vez de la resolución file→proyecto del legacy (`B_uild App`/`App _Options`/`Run Project`) — la shell expone la selección solo como Solution/Project; el label coincide con lo que la shell construye de verdad (`ResolveActiveProject`).
- **Degeneración de `CanShowHelp`**: `info.Visible = true` (la shell no tiene `HelpOperations`).
- **Array = submenu dinámico** (diseño documentado de la slice; QA F2): el legacy GTK oculta el master e **inlinea** los hijos como hermanos; la shell los renderiza en un submenu del master, ahora **oculto con el array vacío** (fix de la ronda 2). El render inline completo queda diferido a una slice futura (no afecta al menudump: este se genera del modelo).
- **Render enriquecido (QA F4, informativo)**: el ✓ en `OpenDocument1..9` no existe en el legacy GTK (en el catálogo son `ActionType.Normal` y `CommandMenuItem` no pinta `Checked`); la shell lo pinta, coherente con el diseño de la slice.

**Números clave**:
- 10 overrides `Update` nuevos en esta slice (8 scalar + 2 `Update (CommandArrayInfo)`) → **57 overrides `Update` acumulados en la Oleada 3** (Slice 1: 4, Slice 2: 10, Slice 3: 17, Slice 4: 6, Slice 5: 1 nuevo + 4 refinados, Slice 6: 5, Slice 7: 2, Slice 8: 2, Slice 9: 10).
- Catálogo y menú invariantes: `Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, menudump `menus=11 items=225 cmds=0 bindings=205` (byte-igual).
- Oracle: `oleada3` sigue en **51 ids** (sin cambios) + **sección nueva `oleada3dyn` con 19 checks** (visible de OpenDocument1..9, Help, 7 labels de catálogo, array count de OpenDocumentList=0 / OpenWindowList=1 "Window 1").

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (también `net10.0-windows`).
- Oracle (`~/opencode/catload_test/`): `oleada3: ok=51 fail=0` + `oleada3dyn: ok=19 fail=0`, exit 0.
- Smoke (`~/opencode/smoke_oleada1.sh`): `--menudump` **byte-igual**, 0 líneas `[warn]/[fatal]/Exception`.
- E2E (`~/opencode/oleada3_cmdstates.sh`, **3 casos** — 16 fases previas + 4 fases docs-dinámicas + 3 node, las node solo con solución cargada):
  - `docs12` (los 3 casos): `docCount=12 doc1Vis=1 doc2Vis=1 doc1Txt=QA List1 doc1Chk=0 docListN=3 winListN=1 docList0=QA List10`.
  - `docs12sel`: `doc1Chk=1 activeDoc=QA List1` (el dispatch con dataItem selecciona el doc).
  - `docs12swap` (regresión QA F1: cerrar List10/List11 + abrir List13/List14, count permanece 12): `docListN=3 docList0=QA List12` (los hijos se re-puntan al NUEVO set); `docs12swapsel` (dispatch "QA List13"): `activeDoc=QA List13` (el dispatch usa el DataItem actual).
  - Labels dinámicos: withsln `node_solution` `B_uild Fixture`/`Run Solution`/`Project _Options`, `node_project` App `B_uild App`/`Run Project`/`App _Options`, `node_file` `B_uild LibA`/`_Run Item` (desviación F3), `operating` `Restart Without Debugging`; emptysl ghost `B_uild Ghost`/`Ghost _Options`/`Run Project` con `buildProj=0` (el label se pinta aunque el gate sea falso, igual que el legacy); nosln defaults de catálogo (`Buil_d`/`R_ebuild`/`C_lean`/`Start Without Debugging`/`_Run Item`/`Project _Options`).
  - 0 `Unhandled`/`Exception`; fases previas intactas (sin regresión).

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_slice9_qa/`, veredicto en `veredicto.md`). Dos rondas: ronda 1 **CERRAR** (1 P2 + 1 P4) → fixes + regresión → ronda 2 **APROBADO**, **0 hallazgos P1–P4**:
- **F1 (P2, ronda 1, resuelto)**: `DataItem` stale en la rama in-place de `RebuildArrayChildren` — con ≥10 docs, cerrar uno y abrir otro (count constante) dispatchaba el dataItem antiguo capturado en el closure del hijo (seleccionaba el documento equivocado o caía al fallback diagnóstico). Fix: **full rebuild en cada refresh** (Tag + closure siempre re-vinculados desde el `CommandArrayInfo` actual) + fases E2E `docs12swap`/`docs12swapsel` como test de regresión.
- **F2 (P4, ronda 1, resuelto)**: el master del array submenu siempre visible (submenu vacío incluido); el legacy GTK lo oculta. Fix: `item.IsVisible = arr is { Count: > 0 }` (master oculto con array vacío — se acerca al estado vacío del legacy); el render inline completo (master oculto + hijos como hermanos) documentado y diferido a una slice futura (decisión de diseño del operador).
- P5 (4, informativos, no bloqueantes): F3 labels en file-selection (consecuencia del bridge, documentado); F4 el ✓ en `OpenDocument1..9` no existe en el legacy GTK (render enriquecido); F5 memoización de `SelectedBuildTargetName` (mismo contrato/exposición que Slice 6, sin leak nuevo); **F6 (nuevo, ronda 2)**: la fase de regresión E2E no discrimina directamente el fix (el dispatch pasa por `nodeContext`, no por el `Click` del hijo, y `docList0` lee el Header) — el fix se valida por inspección estructural; sugerencia futura: dump del `Tag` del hijo o dispatch vía click real.

**Queda pendiente (slices 10..n de la Oleada 3)**: (1) `Tools` (visibility + dynamic-text) y `Navigation` (requiere historial back/forward de documentos); (2) **nuevo**: render legacy completo de los array commands (master oculto + hijos inline como hermanos — hoy es submenu dinámico con master oculto cuando el array está vacío); (3) mejora P5: mensaje de error tras `Stop` legítimo; (4) P5 preexistente: el early-return de `OpenSolutionInWindow` (mismo `solutionLoaded && path`) traga el refresh; (5) P5 preexistente (Slice 7): los atajos Ctrl+Z/Ctrl+Y están tragados por `isTextEditingCombo` del window y no existen en `SkTextEditor.OnKeyDown`; (6) P5 (QA F-5d, Slice 8): tejer la selección del pad en la dispatch del menú principal de "Add Reference..."; (7) cosmético (QA F-5e, Slice 8): renombrar `SolutionOptionsHandler` (rama `#else`); (8) **nuevo (QA F6, informativo)**: endurecer la regresión de los submenús dinámicos (dump del `Tag` del hijo o dispatch vía click real); (9) P5 (QA F3): exponer el proyecto del archivo seleccionado para las etiquetas file→proyecto. El item (1) de la lista de Slice 8 (visibility/rename/dynamic) queda **cerrado por esta slice**. Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–8.

**Cierre**: código en commit (7 ficheros) pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (todo el diff cae en `#else`/`#if AVALONIA_SHELL` o en ficheros exclusivos del shell; los bloques legacy quedan byte-iguales a la base).

## 2026-11-06 (12) — Oleada 3 (backend Avalonia): estado de habilitación de menú (Update) — Slice 10: Tools (visibility + dynamic-text)

**Contexto**: décima slice de la Oleada 3. Cierra la mitad **Tools** del item (1) de "pendiente" de Slice 9 (queda `Navigation`): el **port de los `Update` de los 4 comandos Tools escalares** — la etiqueta dinámica `EditCustomTools` ("Edit Custom Tools..."/"Add Custom Tool...") y los gates de visibilidad de `InstrumentationViewer` (`InstrumentationService.Enabled`), `ToggleSessionRecorder` (pref `EnableAutomatedTesting` + label Start/Stop) y `ReplaySession` (misma pref). Incluye además el **consumo en el render del flag `AutoHide` del modelo** (el sub "Session Recorder" desaparece con sus hijos invisibles) y los **hooks de refresco del menú al cerrar el diálogo de Preferences** (paridad legacy: los cambios de opciones se reflejan en el menú).

**Qué se hizo** (5 ficheros, +145/−2):
- `ToolsCommands.cs` (solo bloque `#else`; +39): 4 overrides `Update (CommandInfo)` portados del bloque legacy `#if !AVALONIA_SHELL` del mismo fichero — `EditCustomTools` (`SettingsStore.LoadTools ().Count > 0` → etiqueta; el shell lee el mismo `MonoDevelop-tools.xml` que el `ExternalToolService` legacy, misma fuente), `InstrumentationViewer` (`info.Visible = InstrumentationService.Enabled`), `ToggleSessionRecorder` (`info.Visible = SettingsStore.GetBool ("MonoDevelop.EnableAutomatedTesting", false)` + `info.Text = ShellCommandState.IsSessionRecording ? "Stop..." : "Start..."`) y `ReplaySession` (misma pref). `using` añadidos al bloque: `MonoDevelop.Core` (shim `GettextCatalog`) y `MonoDevelop.Ide.Services`. **No** se porta `ToolListHandler.Update (CommandArrayInfo)` a propósito: el modelo del shell ya inlinea los tools externos como items propios (`MenuService.ExternalToolItems()`, ids `tool:<name>`); portar el array duplicaría los tools y rompería el menudump `_Tools=8i`.
- `ShellCommandState.cs` (+7): accessor `IsSessionRecording` (delega en `MainWindow.Instance?.IsSessionRecording`) — equivalente shell del `AutoTestService.CurrentRecordSession != null` legacy.
- `MainWindow.axaml.cs` (+80/−2): (a) accessor `internal bool IsSessionRecording => sessionRecorder.IsRecording;`; (b) `DumpCmdStates`: helper `Sub (header)` (item por header — el sub "Session Recorder" no lleva command Tag) + const `T` + **6 campos al final** (orden previos intacto): `editToolsTxt instrVis sessVis sessTxt replayVis sessSub`; (c) driver `--cmdstates`: 7 fases nuevas al final (tras las node) — `tools` (baseline), `instrOff`/`instrOn` (toggle `InstrumentationService.Enabled`), `sessOn`/`sessOff` (toggle la pref en el store real), `noTools`/`toolsBack` (vaciar/restaurar `MonoDevelop-tools.xml` vía `SaveTools`), todo bajo **try/finally que restaura el estado del usuario**; (d) hooks `dlg.Closed += BuildMenu ()` en los 2 paths de apertura de `PreferencesDialog` del usuario (`OnPreferencesMenu` y el case `EditCustomTools` de `OnMenuCommand`) — los cambios de opciones (tools, `EnableAutomatedTesting`) refrescan el menú al cerrar, como en el legacy GTK (precedente: diálogo RunWith).
- `MenuBuilder.cs` (+16): consume el flag `AutoHide` del modelo — `autoHideSubs` (registrado en `BuildItem` para subs con `entry.AutoHide`) + barrido al final de `RefreshCommandStates` (`item.IsVisible = item.Items.OfType<MenuItem> ().Any (c => c.IsVisible)`, tras el mirror de visibilidad y el rebuild de arrays). Afecta a 3 subs: "Session Recorder" (Tools — el objetivo), "Active Configuration" (Project — hijos con ids sintéticos → siempre visibles → no se esconde) y "Run With" (Run — hijo disabled pero visible → no se esconde).
- `MenuService.cs` (+5): el helper `Sub (label, children, autoHide)` ahora **propaga `AutoHide = autoHide`** al `MenuEntry` (antes solo computaba el `Disabled` estático y dejaba `AutoHide=false` — bug que dejaba el sub "Session Recorder" visible con sus 2 hijos ocultos; hallado en la 1ª corrida E2E: `sessOff` daba `sessSub=1`).

**Decisiones de mapeo**:
- **`ToolList` no se porta** (ver arriba): el render del array legacy (master oculto + hijos inline) queda como está documentado en Slice 9; los tools externos siguen viniendo del modelo inlineado y el item "Tool List" queda master de array vacío (oculto por el mecanismo de Slice 9).
- **`AutoHide` = consumo dinámico del flag ya existente en el modelo** (no es una invención de esta slice): "Session Recorder" lo llevaba desde la creación del modelo; el barrido solo lo hace efectivo en el render. Sin impacto en el menudump (model-based).
- **Hooks `Closed += BuildMenu ()` solo en los 2 paths de usuario**: los paths QA (`--prefs*`) quedan sin hook (el proceso termina; aceptable, verificado por QA).

**Números clave**:
- 4 overrides `Update` nuevos en esta slice → **61 overrides `Update` acumulados en la Oleada 3** (Slice 1: 4, Slice 2: 10, Slice 3: 17, Slice 4: 6, Slice 5: 1 nuevo + 4 refinados, Slice 6: 5, Slice 7: 2, Slice 8: 2, Slice 9: 10, Slice 10: 4).
- Catálogo y menú invariantes: `Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, menudump `menus=11 items=225 cmds=0 bindings=205` (byte-igual; `_Tools=8i` intacto).
- Oracle: `oleada3` sigue en **51 ids** (sin cambios) + `oleada3dyn` **19 checks** (sin cambios) + **sección nueva `oleada3tools` con 5 checks** (etiqueta `EditCustomTools` vs tools del usuario, visible de `InstrumentationViewer` vs `InstrumentationService.Enabled`, visibles de `ToggleSessionRecorder`/`ReplaySession` vs la pref, label "Start" sin sesión) — expectativas calculadas desde la misma fuente viva que el handler (check de acuerdo handler↔fuente, no tautológico).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (también `net10.0-windows`).
- Oracle (`~/opencode/catload_test/`): `oleada3: ok=51 fail=0` + `oleada3dyn: ok=19 fail=0` + `oleada3tools: ok=5 fail=0`, exit 0.
- Smoke (`~/opencode/smoke_oleada1.sh`): `--menudump` **byte-igual** al baseline, 0 líneas `[warn]/[fatal]/Exception`.
- E2E (`~/opencode/oleada3_cmdstates.sh`, **3 casos** — 19 fases previas + 7 fases Tools): en los 3 casos, con el estado baseline del usuario (`EnableAutomatedTesting=True`, 1 tool, `InstrumentationService.Enabled=true` por defecto):
  - `tools`: `editToolsTxt=Edit Custom Tools... instrVis=1 sessVis=1 sessTxt=Start Session Recorder replayVis=1 sessSub=1`.
  - `instrOff`: `instrVis=0` (resto intacto); `instrOn`: `instrVis=1`.
  - `sessOn`: `sessVis=1 replayVis=1 sessSub=1`; `sessOff`: `sessVis=0 replayVis=0 sessSub=0` (el AutoHide oculta el sub).
  - `noTools`: `editToolsTxt=Add Custom Tool...`; `toolsBack`: `editToolsTxt=Edit Custom Tools...` (restaurado).
  - Prefijos de todas las fases previas byte-iguales a la corrida anterior (regresión 0); estado del usuario restaurado post-E2E (prefs True, 1 tool, sin procesos huérfanos).

**QA (§18.5)**: ejecutado por el **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_slice10_qa/`, veredicto en `veredicto.md`). Una ronda: **APROBADO**, **0 hallazgos P1–P4**. El bug del AutoHide no propagado (`sessSub=1` en `sessOff`) lo atrapó la 1ª corrida E2E del dev (antes del QA) — fix en `MenuService.Sub` + re-corrida. P5 (5, informativos, no bloqueantes): F1 la rama "Stop Session Recorder" no está ejercitada por oracle/E2E (nadie inicia grabación; verificada por código — `ToggleSessionRecordingAsync` + `SetSessionRecorderMenuLabel` + el port de `Update`); F2 comentario stale preexistente en `BuildToolsWithExternal` ("before Pr_eferences" — el item está en Edit; `FindIndex=-1` → tools al final; sin impacto); F3 `SettingsStore.LoadTools` ignora el atributo `version` (equivalente en todo estado alcanzable); F4 `bindings=` del menudump depende del entorno (+1 por reciente, +1 por tool, +2 por configs solo con `--sln=Fixture.sln` — así lo reproduce el smoke); F5 las líneas `Terminado (killed)` del log E2E son artefacto del harness bash.

**Queda pendiente (slices 11..n de la Oleada 3)**: (1) `Navigation` (requiere historial back/forward de documentos); (2) render legacy completo de los array commands (master oculto + hijos inline como hermanos — hoy es submenu dinámico con master oculto cuando el array está vacío); (3) mejora P5: mensaje de error tras `Stop` legítimo; (4) P5 preexistente: el early-return de `OpenSolutionInWindow` (mismo `solutionLoaded && path`) traga el refresh; (5) P5 preexistente (Slice 7): los atajos Ctrl+Z/Ctrl+Y están tragados por `isTextEditingCombo` del window y no existen en `SkTextEditor.OnKeyDown`; (6) P5 (QA F-5d, Slice 8): tejer la selección del pad en la dispatch del menú principal de "Add Reference..."; (7) cosmético (QA F-5e, Slice 8): renombrar `SolutionOptionsHandler` (rama `#else`); (8) P5 (QA F6, Slice 9): endurecer la regresión de los submenús dinámicos (dump del `Tag` del hijo o dispatch vía click real); (9) P5 (QA F3, Slice 9): exponer el proyecto del archivo seleccionado para las etiquetas file→proyecto; (10) **nuevo (QA F2, Slice 10)**: corregir el comentario stale de `BuildToolsWithExternal`. El item (1) de la lista de Slice 9 queda **cerrado en su mitad Tools por esta slice** (queda `Navigation`). Mecanismo, cableado y ciclo de QA ya quedaron cubiertos por Slice 1–9.

**Cierre**: código en commit (5 ficheros) pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (todo el diff cae en `#else`/ficheros exclusivos del shell; los bloques legacy quedan byte-iguales a la base).

## 2026-11-07 (13) — Oleada 3 (backend Avalonia): estado de habilitación de menú (Update) — Slice 11: Navigation

**Contexto**: undécima slice de la Oleada 3. Cierra el item (1) restante de "pendiente" de Slice 10 (Tools quedó cerrado ahí): el **port de los `Update` de los 4 comandos Navigation** (`NavigationCommands.cs`) sobre el `NavigationHistoryService` del shell (static, `MonoDevelop.Ide.Services` — el servicio legacy es GTK-scoped). Incluye la **ventana balanceada** del submenú "Navigate History" (espejo del `HistoryList.GetList(15)` legacy, con el punto actual siempre dentro y marcado ✓), el **`MoveTo` por índice completo** (dataItem `navpoint:<fullIndex>|<line>|<file>` — semánta de referencia del legacy sin duplicados), el **re-push tras `Clear`** (ancla del `LogActiveDocument` legacy), el **guard `navSwitching`** (el salto no re-registra el punto aterrizado) y el **feed de activación** (punto en cada activación de documento con file). Implementación y ciclo de QA en la sesión de 2026-11-06; verificación final fresca y cierre en 2026-11-07.

**Qué se hizo** (5 ficheros, +183/−18):
- `NavigationCommands.cs` (+48/−4, solo bloque `#else`): 4 overrides `Update` — `NavigateBack` → `NavigationHistoryService.CanMoveBack`; `NavigateForward` → `CanMoveForward`; `ClearNavigationHistory` → `CanMoveForward || CanMoveBack`; `NavigateHistory.Update (CommandArrayInfo)` → `GetNavigationList (15)`: histórico vacío → un único item checked con `ShellCommandState.ActiveDocumentTitle` (null → sin item; el master se oculta por el mecanismo de arrays de Slice 9), no vacío → hijos **newest-first** (bucle `i = count−1 → 0`) con dataItem `"navpoint:" + (start + i) + "|" + line + "|" + file` (el `fullIndex` nombra la entrada exacta — el dispatch resuelve por índice, como el `MoveTo` por referencia del legacy) y `item.Checked = (i == currentIndex)`. Cabecera del bloque `#else` actualizada (región legacy byte-igual).
- `Services/NavigationHistoryService.cs` (+29/−5): (a) `GetNavigationList (count)` **balanceado** alrededor de `position` (`half = (count−1)/2; start = max(0, min(position−half, total−count))`), devuelve `(Points, Current, Start)` — espejo del `HistoryList.GetList` legacy: el current siempre está dentro de la ventana (índice nunca negativo) y cada hijo mapea a su índice completo (`start + i`). **Fix del QA P2** (antes: tail de los últimos N, con `Current` negativo en 16+ puntos + 15 backs → submenú sin ✓ ni current). (b) `MoveTo (int fullIndex)`: mueve el puntero a la entrada exacta; no-op si ya es el current (sin `Changed`), `null` fuera de rango. **Fix del QA P3a** (antes: match por valor File+Line — el clic en un duplicado caía en el más antiguo).
- `ShellCommandState.cs` (+7): `ActiveDocumentTitle` → `MainWindow.Instance?.ActiveDocumentTitle` (título del documento activo; null sin documento de texto activo — el legacy tampoco añadiría item en ese caso).
- `ToolsCommands.cs` (+2/−2): **fix P4 del QA** (stale de Slice 10): la cabecera del bloque `#else` decía "the Update overrides ... are not ported yet"; ahora "Oleada 3 (slice 10) ported the Update overrides" (comentario dentro de `#else`; no afecta al legacy).
- `MainWindow.axaml.cs` (+97/−7): (a) ctor: `NavigationHistoryService.Changed += NotifyCommandStateChanged` (gates + submenú se re-evalúan en cada mutación del historial — mismo canal que los hooks de estado previos). (b) `OnDocSelectionChanged`: `if (!navSwitching) PushNavigationPoint ();` — el feed de activación (equivalente shell del `LogActiveDocument` legacy; degradación documentada: sin feed de caret). (c) `PushNavigationPoint ()` solo registra docs con `FilePath` no vacía (un punto de doc untitled jamás sería aterrizable por archivo — degradación documentada). (d) `NavigateToPoint` con guard `navSwitching` en try/finally (flag legacy `switching`: el cambio de tab del salto no re-registra el punto aterrizado; la ruta open→select es síncrona en UI thread, el guard siempre cubre la selección — verificado por QA). (e) dispatch `NavigateHistory`: dataItem `navpoint:` → parsea el `fullIndex` (hasta el **primer** `|`; robusto a `|`/`:` en la ruta — probe QA T4 con 3 nombres hostiles) → `MoveTo (fullIndex)` → `NavigateToPoint`; sin prefijo → path diagnóstico legacy (dump de la lista). (f) dispatch `ClearNavigationHistory`: `Clear ()` + `PushNavigationPoint ()` — **fix del QA P3b** (el `Clear` legacy re-registra el doc activo: queda anclado como punto 0 y Back alcanza el doc pre-Clear tras la siguiente activación; en el dispatch, no en el servicio, que no conoce el doc). (g) `DumpCmdStates` +6 campos al final (orden previos intacto): `backEn fwdEn clearEn histN hist0 histCur` (histCur = índice del hijo con icono ✓ — el submenú se lee del menú construido real, no del modelo). (h) driver `--cmdstates`: 7 fases nuevas `navCleared..navClear2` sobre un fichero probe de 12 líneas determinista (`~/opencode/oleada3_nav_probe.txt`); `PushNavigationPoint` explícitos sustituyen al log de go-to-line que el shell aún no tiene (declarado en el comentario del driver); `finally Clear ()` deja el proceso limpio.

**Decisiones de mapeo**:
- **Feed solo de activación** (limitación acordada, P5 QA): el shell registra puntos en activación de documento y saltos explícitos; no hay feed de caret (legacy: `LogActiveDocument` en `Caret.PositionChanged`). Consecuencia: Back/Forward cae a la línea registrada en la apertura/salto, no al último caret. E2E compensa con pushes explícitos. Feed de caret = pendiente.
- **Docs untitled fuera del historial** (degradación documentada): solo docs con `FilePath` crean puntos (el shell restaura un punto abriendo su archivo; un punto untitled no sería aterrizable y dead-endearía el back/forward). Legacy registra por referencia de documento.
- **Histórico vacío = título checked** (legacy): el submenú con historial vacío muestra el título del doc activo como único item checked; sin título → sin item → master oculto (mecanismo Slice 9). El master jamás renderiza un submenú vacío.
- **`Clear` re-push en el dispatch**: el servicio no conoce el documento activo; la re-ancla vive donde sí se alcanza `MainWindow`.

**Números clave**:
- 4 overrides `Update` nuevos → **65 overrides `Update` acumulados en la Oleada 3** (Slice 1: 4, Slice 2: 10, Slice 3: 17, Slice 4: 6, Slice 5: 1 nuevo + 4 refinados, Slice 6: 5, Slice 7: 2, Slice 8: 2, Slice 9: 10, Slice 10: 4, Slice 11: 4). Nota (P5 QA): el conteo crudo reproducible (parser de preprocesador, solo métodos `Update` en los 11 ficheros de handlers) da 46→50 — la métrica acumulada incluye además el estado dinámico resuelto en `MenuBuilder` (Slice 9: visible/rename/checked OpenDocument1..9, etiquetas Build/Run, arrays). El delta +4 se verifica directamente en el diff.
- Catálogo y menú invariantes: `Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`, menudump `menus=11 items=225 cmds=0 bindings=205` (byte-igual).
- Oracle: `oleada3` **51** (sin cambios), `oleada3dyn` **19** (sin cambios), `oleada3tools` **5** (sin cambios) + **sección nueva `oleada3nav` con 22 checks** (gates empty/1/2 puntos, submenú newest-first + dataItems `navpoint:`, MoveBack/MoveForward con movimiento del ✓, truncación de forward en push nuevo, `MoveTo` por índice, **ventana P2** 16 puntos + 15 backs → `count=15 cur=0 start=0 first=f1.cs` + checked en el último hijo, **duplicados P3** clic-current no-op / `MoveTo` al duplicado viejo, estado tras Clear).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (también `net10.0-windows`). Sin `--no-incremental` (fallo preexistente MonoRoslynCompat).
- Oracle (`~/opencode/catload_test/`): `oleada3: ok=51 fail=0` + `oleada3dyn: ok=19 fail=0` + `oleada3tools: ok=5 fail=0` + `oleada3nav: ok=22 fail=0`, exit 0 (artefacto final: `~/opencode/oleada3_slice11_oracle_final.txt`).
- Smoke (`~/opencode/smoke_oleada1.sh`): `--menudump` **byte-igual** al baseline, 0 líneas `[warn]/[fatal]/Exception`.
- E2E (`~/opencode/oleada3_cmdstates.sh`, **3 casos** — 19 fases previas + 7 Tools + 7 nav nuevas): las 7 fases nav **idénticas en withsln/emptysl/nosln**: `navCleared` backEn=0 fwdEn=0 clearEn=0 histN=0 hist0=- histCur=-1 → `navOpened` backEn=1 fwdEn=0 clearEn=1 histN=2 hist0=probe:3 histCur=0 (histN=2 artefacto determinista: push en activación (línea 1, antes del `GotoLine`) + push explícito (línea 3) — `OpenFileDocumentAtLine` es síncrona open→select→GotoLine) → `navJmp` histN=3 hist0=probe:7 histCur=0 → `navBack` histCur=1 fwdEn=1 → `navFwd` histCur=0 fwdEn=0 → `navSub` histCur=2 backEn=0 fwdEn=1 (`[nav] → probe:1` — fullIndex 0 = el punto de la línea 1) → `navClear2` backEn=0 fwdEn=0 clearEn=0 histN=1 hist0=probe:1 histCur=0 (`[nav] history cleared` — el re-push ancla el doc activo, fix P3b). Las **107 fases `[cmdstates]`** de los 3 casos byte-iguales a la corrida post-fixes (regresión 0, diff vacío); 0 `warn/fatal/Exception/Unhandled`; `closeWs` 1/1/0 (withsln/emptysl/nosln). Artefactos: `~/opencode/oleada3_slice11_e2e_final.txt` + `oleada3_cmdstates_{withsln,emptysl,nosln}.log`.
- GTK legacy: **sin rebuild** — los 2 ficheros con diff que entran en la build legacy (`NavigationCommands.cs`, `ToolsCommands.cs`) cambian **solo** en región `#else` (excluida de la compilación bajo `!AVALONIA_SHELL`; el preprocesador no parsea el contenido) y `Services/NavigationHistoryService.cs` + `ShellCommandState.cs` no entran en el csproj legacy (el legacy usa su propio `MonoDevelop.Ide.Navigation/NavigationHistoryService.cs`, sin tocar); la build GTK completa la dejó en 0 errores la QA de ronda 1 (21:55).

**QA (§18.5)**: **subagente Tester QA Senior** (solo lectura, artefactos en `~/opencode/oleada3_slice11_qa/`, informe en `qa_report.md`) — **2 rondas**:
- **Ronda 1 (2026-11-06 22:00): RECHAZADO** — 1×P2 + 2×P3 + 1×P4 preexistente + 4×P5, con probes de replicación propios (T1–T8): **P2** la ventana del submenú era tail de los últimos N, no balanceada → con 16+ puntos + 15 backs no había current ni ✓ (`currentIndex` negativo); **P3a** `MoveTo` por valor (File+Line) no distinguía duplicados (legacy: por referencia) — clic en el duplicado actual caía en el viejo; **P3b** `Clear` sin re-registrar el doc activo (legacy: `Clear()` = clear + `LogActiveDocument`); **P4** cabecera stale de `ToolsCommands.cs` (heredado de Slice 10). La QA además validó los invariantes (builds 3 TFMs 0 errores, oracle, E2E, smoke byte-igual, catálogo, legacy intacto, sin NRE, guard `navSwitching` siempre efectivo, truncación `MaxPoints=100` invariante, rama forward conservada como en legacy, ruta `--old-gui` sin preocupación) y los probes OK (T4 nombres hostiles en el dataItem, T5 truncación, T6 `MoveTo` fuera de rango sin excepción, T7 `Push` dedupe, T8 `Changed` solo en mutaciones efectivas).
- **Fixes** (dev, 2026-11-06): ventana balanceada (`GetNavigationList` espejo legacy + caso oracle), `MoveTo (int fullIndex)` + dataItem `navpoint:<fullIndex>|<line>|<file>` (probes oracle), `PushNavigationPoint ()` tras `Clear` en el dispatch (expectativa E2E `navClear2` actualizada a punto real), cabecera `ToolsCommands.cs`. DLL recompileada 22:11:49; oracle y E2E re-corridos tras los fixes (`oleada3_slice11_oracle_rerun2.txt`, `oleada3_slice11_e2e_rerun.txt`).
- **Ronda 2 (2026-11-07, subagente, pasada independiente)**: **APROBADO** (informe en `~/opencode/oleada3_slice11_qa_round2/qa_report.md`, artefactos: logs E2E de los 3 casos). El subagente re-verificó de forma independiente los 4 fixes (P2/P3a/P3b/P4 — todos **CORREGIDOS**, con valores exactos de oracle y E2E), todas las invariantes (builds 0 errores ambos TFMs, oracle `oleada3nav: ok=22 fail=0` con los casos de regresión P2/P3, smoke byte-identical, E2E con las 7 fases nav idénticas en los 3 casos y valores exactos, catálogo, legacy intacto línea a línea) y la exactitud de los 3 docs pendientes: **0 hallazgos P1–P4, 4 P5 informativos** (los mismos que la ronda 1: feed de caret, ruido `history empty`, suscripción sin unsubscribe, métrica de overrides). Nota: la build GTK legacy no pudo re-ejecutarse en el entorno del subagente (fallo de restauración MSBuild `NETSDK1005` ajeno a los cambios); la ronda 1 ya la verificó en 0 errores y el diff no toca la compilación legacy. Antes de la ronda 2 el dev hizo una re-verificación fresca (builds, oracle `oleada3_slice11_oracle_final.txt`, smoke, E2E `oleada3_slice11_e2e_final.txt` — 107 fases byte-iguales a la corrida post-fixes). **Veredicto final: APROBADO tras 2 rondas, 0 hallazgos P1–P4 abiertos, 4 P5 informativos** (precedente: Slice 9, APROBADO tras 2 rondas).
- **P5 (4, informativos)**: (1) **feed de caret**: Back/Forward cae a la línea registrada en activación/salto, no al último caret (limitación de feed acordada; pendiente); (2) clic en el item fallback de título (historial vacío, dataItem=null) hace `Output ("[nav] history empty")` en vez de no-op silencioso (ruido de log; el legacy no hace nada); (3) `Changed += NotifyCommandStateChanged` sin unsubscribe (sin leak: `MainWindow` single-instance + evento static de vida de proceso); (4) la métrica "65 overrides" no es reproducible como conteo crudo (46→50 — ver Números clave).

**Queda pendiente (slices 12..n de la Oleada 3)**: (1) render legacy completo de los array commands (master oculto + hijos inline como hermanos — hoy submenu dinámico con master oculto cuando el array está vacío); (2) mejora P5: mensaje de error tras `Stop` legítimo; (3) P5 preexistente: el early-return de `OpenSolutionInWindow` traga el refresh; (4) P5 preexistente (Slice 7): los atajos Ctrl+Z/Ctrl+Y tragados por `isTextEditingCombo` del window; (5) P5 (QA F-5d, Slice 8): tejer la selección del pad en la dispatch del menú principal de "Add Reference..."; (6) cosmético (QA F-5e, Slice 8): renombrar `SolutionOptionsHandler` (rama `#else`); (7) P5 (QA F6, Slice 9): endurecer la regresión de los submenús dinámicos (dump del `Tag` del hijo o dispatch vía click real); (8) P5 (QA F3, Slice 9): exponer el proyecto del archivo seleccionado para las etiquetas file→proyecto; (9) P5 (QA F2, Slice 10): comentario stale de `BuildToolsWithExternal`; (10) **nuevo (QA Slice 11)**: feed de caret en el historial de navegación (los puntos de salto quedan anclados a la línea de activación); (11) **nuevo (QA Slice 11)**: no-op silencioso del item fallback de título (hoy `Output` de ruido). El item (1) de la lista de Slice 10 (Navigation) queda **cerrado por esta slice** — no quedan comandos de menú con `Update` legacy sin portar.

**Cierre**: código en commit (5 ficheros) pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (todo el diff cae en `#else`/ficheros exclusivos del shell; los bloques legacy quedan byte-iguales a la base).

## 2026-11-07 (14) — Oleada 4 (backend Avalonia): modelo de solución — Slice 1: .sln real vía `SlnFile`

**Contexto**: primera slice de la **Oleada 4** (continuación de la integración total del backend; Oleada 3 cerrada en Slice 11). Alcance: conectar la capa de solución de la shell al **modelo .sln real** `MonoDevelop.Projects.MSBuild.SlnFile` (0 GTK, 0 Mono.Addins — `MonoDevelop.Core` ya compila entero vía `ProjectReference`), sustituyendo la edición ad-hoc de líneas que la shell llevaba desde oleadas previas (`ConfigurationService` línea a línea; `MainWindow.SolutionProjects` con regex `Project(`). **Nota del operador (imprescindible)**: la shell Avalonia tiene su propio módulo Addin (`MonoDevelop.AvaloniaAddons.AddonHost`), no utiliza Mono.Addins ni lo va a usar. Verificado en el diseño: el pipeline de carga de proyectos reales (`ProjectService`/`MSBuildProjectService`) está acoplado a Mono.Addins (static ctor con `AddinManager.ExtensionChanged` + `LoadExtensionData` vía `GetExtensionNodes`) → descartado; el modelo objeto completo (`Solution`+`Project` cargados) queda fuera — la capa de archivo `.sln` es AddinManager-free (y `Solution.WriteSolution` es `internal` con `InternalsVisibleTo` solo tests/mdhost, así que se opera a nivel `SlnFile`, que es la API pública completa). También descartados para slices: `MonoDevelop.Ide.Gui.Documents` (stack VS Text Platform/WPF — oleada entera) y `RootWorkspace` (DI + workbench GTK).

**Qué se hizo** (3 ficheros, +225/−186):
- `Services/ConfigurationService.cs` (reescritura de la parte .sln, +~130/−~150): todas las operaciones de .sln ahora sobre el **modelo real `SlnFile`** (el mismo reader/writer que usa el `ProjectService` legacy; 0 GTK, 0 Mono.Addins). (a) `GetSolutionConfigurations` → `SolutionConfigurationsSection.Keys` del modelo (antes: scan de líneas). (b) `AddSolutionConfiguration` → detección de duplicado **real** (semánta legacy `ProjectOperations.AddConfiguration`: `SolutionConfigurationsSection.ContainsKey("Name|Plataforma")` O cualquier project-set contiene `"Name|AnyCPU.ActiveCfg"` → `false`); escribe vía `SolutionConfigurationsSection[id]=id` + `ProjectConfigurationsSection.GetOrCreatePropertySet(p.Id, ignoreCase:true)` con `Name|AnyCPU.ActiveCfg`/`Build.0` por proyecto (GUID **con llaves**, como los guarda `SlnProject.Id`); **salta solution-folders** (`TypeGuid {2150E333-8FDC-42A3-9474-1A3956D4EEDE}`, con llaves) en los mappings (el writer legacy omite items que no soportan build — el código ad-hoc anterior sí les metía líneas); `createChildren` → `AddProjectConfiguration` solo para los `.csproj` **referenciados** en el .sln (antes: scan de TODO el árbol). (c) `AppendProjectToSolution` → añade `SlnProject { TypeGuid C# con llaves, Name, FilePath relativo con /, Id = Guid.ToString("B") mayúsculo (con llaves) }` + mappings `cfg|AnyCPU.ActiveCfg`/`Build.0` por cada configuración existente (fallback Debug/Release), todo vía modelo. (d) `AddProjectToSolution` (.sln **nuevo**) → genera vía modelo con `MSBuildFileFormat.DefaultFormat` (VS2012: `FormatVersion=12.00` + comentario `# Visual Studio 15`) + `SolutionProperties`/`HideSolutionNode=FALSE` (antes: StringBuilder a mano). (e) **NUEVO** `GetSolutionProjects(sln)` → los `.csproj` referenciados vía `SlnFile.Projects` (rutas relativas resueltas contra el directorio del .sln, filtro `File.Exists`) con fallback a scan de directorio si el modelo no arroja nada. **Bug hallado y corregido en el desarrollo**: los TypeGuids ad-hoc sin llaves producían líneas `Project("FAE04EC0-...")` inválidas para MonoDevelop (MSBuild las toleraba); `SlnFile` guarda el `TypeGuid` **verbatim** (con llaves, dentro de las comillas del .sln) — el hook E2E lo detectó (`msbuild reparse` OK pero `append visible` FAIL). **Sin cambios**: `AddProjectConfiguration` (PropertyGroup en .csproj), `Get/SetActiveConfiguration` (.userprefs), consts `AnyCpuSolution`/`AnyCpuProject`.
- `Views/MainWindow.axaml.cs` (+~120/−~25): (a) `SolutionProjects(sln)` — usado por Build/Run/proyecto de arranque — **delega** a `ConfigurationService.GetSolutionProjects` (antes: regex `Project("...)` sobre líneas; API y semántica idénticas, 0 cambios en call sites). (b) **Hook QA `--slnmodel`** (nuevo): sobre **copias temporales** (copia el **directorio completo** del fixture — no solo el .sln — para que las rutas relativas resuelvan): parse cfgs/projs vía modelo; **round-trip no-op byte-identical** (Read+Write sin cambios); addcfg (crea + visible + dup refused + mapping en el fichero); append (visible + reparse Microsoft.Build); newcsln (cfgs/projs + reparse); **cross-check**: `SolutionLoader.Load` (Microsoft.Build por reflexión) reparsea **cada fichero escrito** (la salida del writer del modelo sigue siendo .sln válido para MSBuild). Cierra la app al terminar.
- `Program.cs` (+1/−1): `--slnmodel` añadido a `QaDialogArg` (el primer flag QA gana; `--sln=` se salta como antes).

**Decisiones de mapeo**:
- **Nivel `SlnFile`, no `Solution`**: el modelo objeto completo requiere el pipeline acoplado a Mono.Addins (nota del operador) y `Solution.WriteSolution` es `internal`; la capa de archivo .sln cubre lo que la shell necesita hoy (configs, proyectos, import, append) con la API pública del modelo legacy.
- **Round-trip no-op byte-identical** (invariante): `SlnFile.Read` detecta BOM/newlines (`FileUtil.GetTextFormatInfo`) y `Write` los reproduce (`TextFile.WriteFile` con el `ByteOrderMark` detectado) — un .sln escrito sin cambios sale **byte-igual** (lo cubren oracle y E2E; el fixture real tiene BOM+CRLF y se preserva).
- **`createChildren` solo referenciados**: mejora frente al scan de todo el árbol — el legacy opera sobre los proyectos de la solución (los referenciados).
- **Solution-folders sin mappings**: el writer legacy omite items sin build; el shell lo iguala (el código ad-hoc anterior les añadía líneas de mapping — inválidas semánticamente).
- **Fallback de `GetSolutionProjects`** (scan de directorio) conservado: mismo comportamiento Build/Run que antes cuando el .sln no arroja proyectos.

**Números clave**:
- Build **0 errores** ambos TFMs (`net10.0` + `net10.0-windows`). Catálogo invariante: `Count=461`, `LoadedCommands=475`, `ResolvedHandlers=111`. Menudump **byte-igual**: `menus=11 items=225 cmds=0 bindings=205`.
- Oracle (`~/opencode/catload_test/`): **sección nueva `oleada4` con 18 checks** — parse cfgs/projs (fixture 3 proyectos); **round-trip no-op byte-identical**; addcfg (created / visible / **dup refused** / línea de solución / **ActiveCfg+Build.0 ×3 proyectos** / **prefijo de GUID con llaves**); append (4 projs / **línea Project con TypeGuid C# con llaves** / mappings / **regresión P4: sln sin `SolutionConfigurationPlatforms` → append lleva mappings Debug/Release (fallback)** ×2); newcsln (2 cfgs 1 proj / **formato writer legacy 12.00+VS15+HideSolutionNode** / mappings); PropertyGroup .csproj (ruta sin cambios). `oleada3*` sin cambios: `51/19/5/22`, `fail=0`, exit 0.
- E2E (`~/opencode/oleada4_slice1_e2e.sh`, 5 casos, cada uno un run de la shell sobre una **copia** del fixture `~/opencode/solpad_deps/`): `menudump` PASS (invariante byte-igual); `slnmodel` **13/13 ok** (incl. los 3 cross-checks de reparse Microsoft.Build); `activeconfig` (initial=Debug → after-switch=Release persisted=True → restored=Debug); `newconfig-real` (created=True sln-entry=True csproj-entries=True (3 projects) + cleanup auto); `openimport` (wrapper-sln=True loaded=True). **ALL_PASS**, 0 líneas warn/fatal/Exception. El fixture real queda **intacto**.

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (y `-f net10.0-windows`). Sin `--no-incremental` (fallo preexistente MonoRoslynCompat).
- Oracle: `cd ~/opencode/catload_test && dotnet run` → exit 0 con `oleada4: ok=18 fail=0` (incl. los 2 casos de regresión P4 "empty-cfgs sln") + `oleada3: ok=51 fail=0` + `oleada3dyn: ok=19 fail=0` + `oleada3tools: ok=5 fail=0` + `oleada3nav: ok=22 fail=0` + `catalog: Count=461 LoadedCommands=475`.
- E2E: `bash ~/opencode/oleada4_slice1_e2e.sh` → `E2E oleada4 slice1: ALL_PASS` (5 casos; logs en `~/opencode/oleada4_slice1_e2e_*.log`).
- Fixture: `grep -c "QAConfig\|QAExtra" ~/opencode/solpad_deps/Fixture.sln` → 0 (los hooks operan en `~/opencode/oleada4_e2e_*` y `/tmp/slnmodel_*`).
- Sin Mono.Addins: `grep -rn AddinManager` sobre los 3 ficheros modificados → 0 coincidencias; `SlnFile`/`MSBuildFileFormat` (las APIs usadas) no lo invocan.

**QA (§18.5)**: subagente Tester QA Senior (solo lectura, brief `~/opencode/oleada4_slice1_qa_brief.md`, artefactos en `~/opencode/oleada4_slice1_qa/`) — **ronda 1 (2026-11-07): RECHAZADO** — 1×P4 bloqueante + 4×P5 informativos, con réplica mínima ejecutable (`~/opencode/oleada4_slice1_qa/p4probe/`): **P4** `AppendProjectToSolution` había perdido el **fallback Debug/Release** cuando el .sln no tiene `SolutionConfigurationPlatforms` (contratado en el brief y presente en la implementación previa) — un .sln sin configs dejaba el proyecto anexado **sin** mappings `ActiveCfg`/`Build.0`. Todas las invariantes pasaban (builds 0 errores ambos TFMs, oracle completo, E2E `ALL_PASS` 5 casos, logs limpios, fixture intacto, round-trip byte-identical, sin acoplamiento `Mono.Addins`, legacy no compila el fichero). **Fix** (dev): fallback restaurado en `AppendProjectToSolution` (`cfgs.Count == 0 → Debug/Release`) + comentario `///`→`//` (P5b cosmético) + **2 checks nuevos de regresión P4 en el oracle** (sln vacío-de-configs → append con project y mappings Debug/Release). Re-verificado: builds 0 errores ambos TFMs, oracle `oleada4 ok=18 fail=0`, E2E `ALL_PASS`. Los 4 **P5** (no bloqueantes): (1) el catch-all de `GetSolutionProjects` traga la excepción de parseo y cae al scan de directorio (equivalente al comportamiento previo para .sln corrupto; pendiente: log diagnóstico); (2) comentario `///` en contexto no-doc (**corregido en el fix**); (3) el hook `--slnmodel` copia el directorio completo del fixture (aceptable como hook QA; pendiente si se reutiliza con fixtures grandes); (4) `Path.GetDirectoryName(slnPath)!` null-forgiving (riesgo teórico bajo). **Ronda 2 (2026-11-07, subagente, pasada independiente — informe en `~/opencode/oleada4_slice1_qa_round2/qa_report.md`)**: **APROBADO** — el fix P4 re-verificado (a) lectura del código (fallback presente y aplicado antes de escribir mappings, comentario `//`), (b) **réplica propia contra la DLL compilada real** (`~/opencode/oleada4_slice1_qa_round2/p4replica/` — `AppendProjectToSolution` sobre un sln sin configs: `hasProject/hasDebugActiveCfg/hasReleaseActiveCfg/reparseOk=True`, `P4_REPLICA_PASS=True`, exit 0), (c) oracle `oleada4 ok=18` con los 2 checks de regresión P4, (d) el fallback **no dispara** en slns con configs (append normal usa las configs existentes). Todas las invariantes re-corridas por el subagente (builds 0 errores ambos TFMs, oracle completo, E2E `ALL_PASS` con métricas exactas, logs 0 warn/fatal/Exception, fixture intacto, round-trip byte-identical, sin `Mono.Addins`, legacy no compila el fichero, **docs exactos y coherentes**): **0 hallazgos P1–P4, P5b corregido, 3 P5 abiertos no bloqueantes** (catch-all `GetSolutionProjects`, `CopyDir` del hook, null-forgiving `Path.GetDirectoryName`). **Veredicto final: APROBADO tras 2 rondas, 0 hallazgos P1–P4 abiertos.**

**Queda pendiente (slices 2..n de la Oleada 4)**: (1) modelo de proyecto real `.csproj` sin AddinManager (carga `MSBuildProject` vía modelo — el static ctor de `ProjectService`/`MSBuildProjectService` está acoplado a `GetExtensionNodes`; hay que trazar una vía de carga AddinManager-free); (2) árbol del pad de solución sobre el modelo real (con solution-folders como jerarquía, hoy `SolutionLoader` solo para el pad); (3) RunConfigurations reales sobre el modelo; (4) build vía modelo de configuraciones; (5) Add Reference vía modelo. La lista pendiente de la Oleada 3 (entrada anterior, items 2..11) sigue abierta y no depende de esta oleada.

**Cierre**: código en commit (3 ficheros) pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA (2 rondas: RECHAZADO por P4 → fix → APROBADO) en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (los 3 ficheros con diff son exclusivos del shell Avalonia; el legacy no compila `ConfigurationService.cs`).

## 2026-11-07 (15) — Oleada 4 (backend Avalonia): modelo .csproj + árbol de solution-folders — Slice 2

**Contexto**: segunda slice de la **Oleada 4** (Slice 1 cerrada: capa .sln sobre el modelo real `SlnFile`). Alcance: (a) **modelo de proyecto .csproj** para la shell (nombre, OutputType, TargetFrameworks, OutputPath, Configurations, referencias, conteo de items) y (b) **árbol completo de solution-folders a nesting arbitrario** (folder→folder→project) en el pad, con robustez de producción ante .sln corruptos (ciclos de padres, secciones duplicadas, orphans). **Nota del operador (en vigor)**: la shell no utiliza Mono.Addins.

**Qué se hizo** (4 ficheros, +674/−125):
- `Services/ConfigurationService.cs`: **`ProjectFileModel`** + `LoadProjectModel`/`ClearProjectModelCache` (parseo XML **puro** — ver decisión 1: Name desde AssemblyName→RootNamespace→fileName; OutputType; TargetFrameworks (multi gana sobre single); OutputPath/BaseOutputPath; Configurations (regex sobre `Condition` de PropertyGroup, fallback Debug/Release); Package/Project/Reference (Include; ProjectReferences resueltos a absolutos si existen); conteo Compile/None/Content/EmbeddedResource; caché por (ruta, mtime); inexistente/no parseable → `null`, nunca lanza); **`ProjectFileModel.IsExecutable`** (OrdinalIgnoreCase "Exe"/"WinExe"); `SolutionFolderTypeGuidAlt` (`{2150E333-8FDC-42A3-9474-1A3956D46DE8}`, variante de 9 dígitos finales presente en .sln de VS); **nuevo `GetSolutionEntries`** + `BuildEntriesFromSln` (modelo **SlnFile** → lista de `SolutionEntry` con Guid/ParentGuid; `null` + log diagnóstico si el .sln no se lee).
- `Services/SolutionLoader.cs`: `SolutionEntry` +Guid/+ParentGuid; **`BuildTree`** (nesting arbitrario preservando orden del .sln; orphan (padre desconocido) → root; ciclo de padres → anclaje + corte de back-edge con recorrido de 3 colores: nada se pierde, nada cuelga); **`HasNestedProjectsCycle`** (público; escaneo de texto sin recursión — ver hallazgo P3: detecta auto-padre y ciclos encadenados; **escanea y mezcla TODAS las secciones `GlobalSection(NestedProjects)`**, last-wins en orden de fichero — el mismo estado final en el que termina `Microsoft.Build.SolutionFile.Parse` — y detecta el ciclo sobre el mapa mezclado); `Load` con pre-check: si hay ciclo → `null` sin invocar `SolutionFile.Parse`.
- `Views/MainWindow.axaml.cs`: `OpenSolutionInWindow` ahora vía `GetSolutionEntries` + `BuildTree` + **`AddXwtSolutionNode` recursiva** (iconos exe/lib desde `IsExecutable`); `PickStartupProject` y las 4 comparaciones case-sensitive `OutputType is "Exe" or "WinExe"` → `IsExecutable`; **hook QA `--projmodel`** (13 checks: entries/projs/models, TFM multi, refs resueltas, caché, missing→null, startup pick, tree count, folders kept, orphans, nested folder exists [solo si la solución declara nesting folder→folder: `hasDeclaredFolderNesting`], nested projects); hook `--slnmodel` (slice 1) con **guardas**: checks condicionales degradan a `skip` (no fail) — parse cfgs solo si el fichero declara `SolutionConfigurationPlatforms`, roundtrip bytes solo si VS-canónico (BOM), cross-checks Microsoft.Build saltados si la fuente tiene ciclo de padres o no la parsea.
- `Program.cs`: `--projmodel` añadido a `QaDialogArg`.

**Hallazgos clave y decisiones**:
1. **`MSBuildProject` descartado**: su pipeline de carga (`WorkspaceObject` static ctor → `AddinManager.CreateExtensionContext()`) lanza `InvalidOperationException` sin Mono.Addins inicializado; la shell no lo usa (nota del operador) → modelo XML propio (`ProjectFileModel`) equivalente a lo que la shell consume.
2. **HALLAZGO P3 (verificado en fuentes y por el QA)**: `Microsoft.Build.SolutionFile.Parse` entra en **recursión infinita** en `ProjectInSolution.GetUniqueProjectName` cuando `NestedProjects` declara un auto-padre o un ciclo → `StackOverflowException` **no atrapable** (el CLR aborta el proceso: SIGABRT, rc 134; ningún try/catch la evita). **Consecuencia de diseño**: la ruta de producción (`OpenSolutionInWindow`) y los hooks QA ya NO usan `SolutionLoader.Load`/`SolutionFile.Parse` para leer el .sln: usan `GetSolutionEntries` (SlnFile, lector lineal, tolera ciclos sin SOE). `SolutionLoader.Load` (quien la siga usando) va blindada con el pre-check `HasNestedProjectsCycle`.
3. **Quirk writer SlnFile (preexistente, compartido con el legacy, no se toca)**: el writer normaliza la sección de solución `ProjectConfigurationPlatforms = postProject` → `postSolution`; por eso el roundtrip byte-identical del hook `--slnmodel` solo se exige sobre ficheros **VS-canónicos** (BOM `EF BB BF`) y los fixtures propios usan los keywords que VS escribe (`preSolution`/`postProject` a nivel solución — `preSpawn`/`postAction` son de proyecto y SlnFile los rechaza con `InvalidSolutionFormatException`).
4. **Invariante menudump corregida (no era regresión)**: el componente `bindings=` del menudump (`MenuCommandBindings()`) depende del estado persistido de `RecentSolutions` (`~/.config/MonoDevelop/9.0/MonoDevelopProperties.xml`): base **196** (con solución abierta y recents vacío; 193 sin solución) + **1 por cada entry del listado de recientes**, que cada ejecución de la app muta (`OpenSolutionInWindow` → `RecentSolutions.Add`). La lectura histórica `bindings=205` era 196 + 9 recents del momento y fluctuaba entre ejecuciones (203/205/206...). El E2E (`~/opencode/oleada4_slice1_e2e.sh`) hace ahora `reset_recents` antes del caso y la invariante es **determinista**: `menus=11 items=225 cmds=0 bindings=196`. Las entradas `bindings=205` de oleadas previas son mediciones históricas válidas en su momento.

**QA §18.5 (3 rondas, subagente Tester QA Senior; briefs/reportes en `~/opencode/oleada4_slice2_qa{1,2,3}/`)**:
- **Ronda 1: RECHAZADO** — **P3-1**: crash SOE rc 134 con auto-padre/ciclo en `NestedProjects` (répro `SelfParent.sln`/`Cycle.sln`; la ruta `--projmodel` y la apertura en producción crasheaban porque aún usaban `SolutionLoader.Load`); **P4-1**: falso positivo del check `nested folder exists` con 2 folders planos sin nesting; **P4-2**: `PickStartupProject` case-sensitive (`<OutputType>exe</OutputType>` en minúsculas no se detectaba). → **3 fixes** (sección anterior + `IsExecutable` en las 4 comparaciones + guardas del hook) + corrección del fixture propio del slice (`Nested.sln`: `preSpawn`/`postAction` → `preSolution`/`postProject`).
- **Ronda 2: RECHAZADO** — todos los fixes de ronda 1 verificados **FIXADO** (répros propios contra las copias, matriz 14/14, batería completa), pero 1 hallazgo **nuevo P3-N1**: `HasNestedProjectsCycle` solo escaneaba la **primera** sección `GlobalSection(NestedProjects)`; .sln corrupto con la sección **duplicada** y el ciclo en la segunda → pre-check false → `SolutionFile.Parse` → SOE rc 134 (hook `--slnmodel`; ruta de producción SlnFile no afectada — su propio `DupSection.sln` lo demuestra: `--projmodel` rc=0). → **Fix**: merge last-wins de **todas** las secciones + 3 checks nuevos de regresión en el oracle (ciclo en 2ª sección → true; `Load` → null sin SOE; 2ª sección que sobrescribe el ciclo de la 1ª → false, coherente con el estado final de MSBuild).
- **Ronda 3: APROBADO** — 0 hallazgos P1–P4. Repro `DupSection.sln` verificado (antes rc=134 → ahora rc=0, `skip msbuild cross-checks (source NestedProjects has a parent cycle)`); fixture extra propio del QA (3 secciones duplicadas, ciclo en la 3ª) pasa en ambos hooks; batería completa re-corrida (builds 2 TFMs 0 errores, oracle `oleada4 18/18` + `oleada4s2 35/35` + `oleada3* 51/19/5/22` + catalog 461/475, E2E `ALL_PASS` 6 casos, matriz 14/14 rc=0 fail=0, sus 9 fixtures de ronda 2 repasados); diff acotado a los 4 ficheros, sin Gtk, sin Mono.Addins.

**Números clave**:
- Build **0 errores** ambos TFMs (`net10.0` + `net10.0-windows`). Catálogo invariante: `Count=461`, `LoadedCommands=475`.
- Oracle (`~/opencode/catload_test/`): sección nueva **`oleada4s2` con 35 checks** — `BuildTree` (nesting arbitrario 3 niveles, orden, orphan, ciclo A↔B anclado sin pérdida, auto-padre, GUIDs duplicados, profundidad 200); `LoadProjectModel` (xmlns, multi-TFM gana, cfgs condicionales, refs resueltas, truncado/vacío/BOM → null, caché mtime, `IsExecutable` minúsculas); `HasNestedProjectsCycle` (7 casos: well-formed/self-parent/A↔B/orphan/ciclo en 2ª sección duplicada/overwrite que elimina el ciclo/`Load`→null con ciclo); `GetSolutionEntries` (5 entries/2 folders/padre resuelto, missing→null). `oleada4 18/18` y `oleada3* 51/19/5/22` sin cambios, exit 0.
- E2E (`~/opencode/oleada4_slice1_e2e.sh`, 6 casos): `ALL_PASS` — menudump (invariante determinista `bindings=196`), slnmodel 13/13, activeconfig, newconfig-real, openimport, **projmodel 13/13**.
- Matriz corruptos: **7 fixtures × 2 hooks = 14/14** rc=0 fail=0 (NoNested, SameName, NoFolder, Nested, SelfParent, Cycle, Orphan) + 9 fixtures propios del QA (Cycle3, ProjectSelfParent, DupCycleFirst, DupSection, TripleCyc [r3], UpperWellFormed, NoNestedSection, OneProject, ParentIsProject, ExeLower).

**Cómo validar**:
- Build: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` → `0 Errores` (y `-f net10.0-windows`). Sin `--no-incremental`.
- Oracle: `cd ~/opencode/catload_test && dotnet run -c Debug --nologo` → exit 0 con `oleada4: ok=18 fail=0`, `oleada4s2: ok=35 fail=0`, `oleada3: ok=51 fail=0`, `oleada3dyn: ok=19 fail=0`, `oleada3tools: ok=5 fail=0`, `oleada3nav: ok=22 fail=0`, `catalog: Count=461 LoadedCommands=475`.
- E2E: `bash ~/opencode/oleada4_slice1_e2e.sh` → `E2E oleada4 slice1: ALL_PASS` (6 casos; el script hace `reset_recents` antes del caso menudump).
- Robustez: matriz de la sección anterior (lanzador tipo E2E sobre copias de los fixtures; logs en `~/opencode/oleada4_slice2_qa3_pre/` y `~/opencode/oleada4_slice2_qa3/matrix/`).
- Sin Mono.Addins: `grep -rn AddinManager` sobre los 4 ficheros → solo comentarios explicativos; `ProjectFileModel`/`GetSolutionEntries`/`BuildTree`/`HasNestedProjectsCycle` no lo invocan.

**Queda pendiente (slices 3..n de la Oleada 4)**: RunConfigurations reales sobre el modelo; build vía modelo de configuraciones; Add Reference vía modelo. La lista pendiente de la Oleada 3 (items 2..11) sigue abierta.

**Cierre**: código en commit (4 ficheros) pushado a `origin/agents/sub-agent-senior-dev-role-report`; handoff + veredicto del QA (3 rondas: RECHAZADO P3-1/P4-1/P4-2 → fixes → RECHAZADO P3-N1 → fix + 3 checks oracle → APROBADO) en el commit de docs que le sigue. Sin cambios en submódulos; la ruta `--old-gui` no se ve afectada (los 4 ficheros con diff: 2 de `MonoDevelop.Ide/Services` — el legacy usa lista explícita de compilación y no incluye los nuevos métodos, y los bloques legacy son byte-iguales — y 2 exclusivos del shell Avalonia).

## 2026-11-07 (16) — Oleada 3 (cleanup): Slice A — items 2, 3, 5, 6, 8, 9 y 11

### Contexto

La lista pendiente de la Oleada 3 (items 2..11 de `docs/interfaz-plan.md`)
quedó abierta tras los slices 1-11. El plan de cierre la divide en slices:
**A (este): items 2, 3, 5, 6, 8, 9, 11** (cleanup + pad); B: items 4+10
(editor Ctrl+Z/Y + feed caret); C: items 1+7 (render legacy array commands +
endurecer regresión). Este slice no toca el modelo de soluciones ni los
comandos: son 7 items de comportamiento/cleanup en el shell.

### Qué se hizo (5 ficheros de código)

**`MainWindow.axaml.cs`** (todos los items del shell):
- **Item 2 (Stop sin "Build FAILED")**: el proceso de build/run se mata con
  `Kill()` (SIGKILL en Linux → código 137), y el exit handler compartía el
  camino `code != 0` → `SetErrors("Build FAILED...")` con un fallo de build
  real. Nuevo campo `stoppedByUserProc`; `StopBuildOrRun` lo fija antes del
  `Kill()`; el exit handler compara `ReferenceEquals(proc,
  stoppedByUserProc)` y, si coincide, escribe `"[process] stopped by user
  (exit code N)"` y **salta** el `SetErrors` de Build FAILED (el proceso fue
  interrumpido, no falló). `code == 0` y la salida natural por error de
  build (código real de dotnet) siguen por su camino.
- **Item 3 (refresh del pad en add/remove/rename)**: el poblado del árbol del
  pad se extrajo de `OpenSolutionInWindow` a `RebuildXwtSolutionTree(path)`
  (misma lógica: `GetSolutionEntries` → `BuildTree` → nodos Xwt; `path` es el
  tag de la raíz — en el flujo completo `loadedSolutionPath == path` en ese
  punto, así que la tag es idéntica a la anterior). `OpenSolutionInWindow`
  ganó `forceRefresh = false`: el early-return de solución ya cargada
  (mismo-path) ahora re-puebla el árbol si `forceRefresh` (el proyecto set
  cambió bajo el mismo .sln), sin re-ejecutar recents/watches/menú.
  Consumidores de `forceRefresh: true`: `RefreshSolutionTree()` (flujos de
  rename/borrar/añadir), la ruta de producción de añadir proyecto
  (`NewSolutionDialog` → `AppendProjectToSolution`) y el driver QA
  `--newproject`. El primer open (startup/QA/`OpenFileOrProject`/`recent:`/
  welcome) conserva `false` — el early-return = no-op, coherente con el
  legacy "la solución ya está abierta".
- **Items 5+8 (selección de fila de file → proyecto contenedor)**: nuevo
  `ResolveSelectedProjectPath()`: la fila seleccionada es `project:` → sí
  misma; cualquier otra fila (`file:`/`folder:`/`reference:`) → sube por el
  store (`TreeStore.GetParent`) hasta el ancestro `project:` más cercano (el
  tag de nodo guarda la ruta del proyecto). Reemplaza `ActiveProjectPath()`
  (primer proyecto de la solución — el bug: seleccionar un file dejaba el
  target en el proyecto primero) en 4 consumidores: `SelectedNodeIsBuildTarget`
  (gates `buildSol`/`rebuildSol`/`cleanSol`/`runEntry`/`stop`...),
  `SelectedNodeIsProject` (gate `editRefs`), `SelectedBuildTargetName`
  (labels `B_uild App`/`Run Project`/`App _Options`) y el dispatch
  `AddReference` cuando `nodeContext is null` (menú principal). El dispatch de
  menú contextual (`nodeContext != null`) no se toca: ese menú solo se ofrece
  en filas de proyecto (`BuildProjectPadMenu` gatea por nodeType), así que no
  hay regresión. El bridge legacy (seleccionar file fija el selected build
  target al proyecto contenedor) se recupera exacto.
- **Item 11 (nav con historial vacío)**: el dispatch `NavigateBack`/
  `NavigateForward` con `CanMoveBack/Forward == false` antes emitía
  `"[nav] history empty"` (ruido de log sin valor de diagnóstico); ahora es
  no-op silencioso. El dump de la lista de navegación con puntos del
  `--cmdstates` se conserva.
- **Driver `--cmdstates`**: fase stop (prestop → arrancar `sleep 30` vía
  `RunProcessAsync` → esperar `IsOperationRunning` → `StopBuildOrRun()` →
  esperar salida → poststop) que ejercita el item 2 de verdad (el botón de
  stop con un proceso corriendo no se podía ejercer antes sin ese driver).
- **Hook `--newproject`**: aserción `pad-tree=` (el `xwtSolutionNodes`
  contiene `project:<csproj>` tras el force-refresh — verifica el item 3 en
  la ruta de añadir proyecto) y `OpenSolutionInWindow(..., forceRefresh:
  true)` (antes: open de mismo-path silencioso, árbol stale).

**`ProjectCommands.cs`** — **item 6 (F-5e QA slice 10)**: rename
`SolutionOptionsHandler` → `SolutionOptionsMenuHandler` en **ambas** ramas
(`#if AVALONIA_SHELL` y legacy) + comentario. La XML de extensión
(`Commands.addin.xml`) es **compartida** entre ambos binarios y el
`defaultHandler` se resuelve **por string en runtime**: renombrar solo una
rama rompería el otro binario.

**`Commands.addin.xml`**: `defaultHandler` de `Solution.Options` →
`SolutionOptionsMenuHandler`.

**`MenuService.cs`** — **item 9**: comentario de `BuildToolsWithExternal`
corregido (citaba un item "Preferences" inexistente en Tools > Options).

**`ShellCommandState.cs`**: 3 comentarios stale actualizados (
`ActiveProjectCount`, `SelectedNodeIsBuildTarget`, `SelectedNodeIsProject`).

### Hallazgos y decisiones

- **El rename exige build legacy como check P1**: como la XML es compartida,
  la prueba fuerte es que `MonoDevelop.Ide.csproj` (sin `AVALONIA_SHELL`)
  compile → 0 errores. También 0 referencias residuales al nombre antiguo.
- **El early-return de `OpenSolutionInWindow` no es nuevo** (era el
  comportamiento desde hace slices): el bug del item 3 es que
  `RefreshSolutionTree`/add-project dependían de él para refrescar el árbol y
  el early-return hacía no-op. El fix es el flag, no quitar el early-return
  (los opens de mismo-path del menú File > Open / recents / welcome siguen
  siendo no-ops correctos).
- **El dispatch `AddReference` del menú contextual no cambia**: `nodeContext`
  no es null solo en filas de proyecto (el menú contextual gatea por
  nodeType), así que `ResolveSelectedProjectPath` solo afecta al menú
  principal — exactamente el bug reportado (F3 del QA slice 10: seleccionar
  file → el target era el primer proyecto).
- **La aserción del item 2 en el log es `stopped by user`**: el
  `SetErrors("Build FAILED...")` escribe en el pad de errores, no en la
  consola, así que el log de consola solo muestra la línea del process.

### Números clave

- Build net10.0 y net10.0-windows: **0 errores**.
- Build **legacy** `MonoDevelop.Ide.csproj` (sin `AVALONIA_SHELL`): **0
  errores** (check P1 del rename).
- Catálogo: `Count=461 LoadedCommands=475 ResolvedHandlers=111` (sin cambio;
  el handler renombrado sigue resolviéndose).
- Menudump (tras `reset_recents`): `menus=11 items=225 cmds=0 bindings=196`
  (byte-igual a HEAD).
- E2E `--cmdstates` (3 casos): 0 `[warn]`/`[fatal]`/Exception + 4 aserciones
  slice A PASS (`stop`: log con `stopped by user` y sin `Build FAILED`;
  `poststop`: fase corrió + `stop=0` + `buildSol=1`; `node_file F3`: la fase
  `(node_file)` muestra `editRefs=1 solItemOpts=1 buildTxt=B_uild App
  runEntryTxt=Run Project projOptsTxt=App _Options` — proyecto contenedor,
  no LibA; `rename`: `(node_solution)` mantiene `solOpts=1`).
- Log real de la fase stop: `[run] stopped` → `[process] stopped by user
  (exit code 137)`.
- Probe `--newproject` (copia del fixture; console + library):
  `created=True sln-entry=True mappings=True pad-tree=True` → **ALL_PASS**.
- E2E oleada4 (6 casos, binario final): **ALL_PASS**.
- Oracle: `oleada3 ok=51`, `oleada3dyn ok=19`, `oleada3tools ok=5`,
  `oleada3nav ok=22`, `oleada4 ok=18`, `oleada4s2 ok=35`, `fail=0` en todo,
  exit 0.

### Cómo validar

- Builds: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` (y `-f net10.0-windows`); legacy: `dotnet build main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.csproj -c Debug -f net10.0 --nologo` (sin `AVALONIA_SHELL`).
- E2E: `bash ~/opencode/oleada3_cmdstates.sh` → `SLICEA_ALL_PASS` (4
  aserciones sobre el log con-solución) + `E2E_DONE` + 0 warn en los 3 logs.
- Probe: `bash ~/opencode/oleada3_sliceA_probe.sh` → `ALL_PASS`
  (`pad-tree=True` en console y library).
- E2E Oleada 4: `bash ~/opencode/oleada4_slice1_e2e.sh` → `ALL_PASS`.
- Oracle: `cd ~/opencode/catload_test && dotnet run -c Debug --nologo` →
  exit 0, todas las secciones `fail=0`.
- 0 referencias residuales: `grep -rn "SolutionOptionsHandler" main/src`
  → solo el comentario del rename en `ProjectCommands.cs`.

### QA (§18.5)

Brief + reporte en `~/opencode/oleada3_sliceA_qa/` (`brief.md`,
`qa_report_r1.md`). **Ronda 1: APROBADO — 0 hallazgos P1-P4**, 2 P5 no
bloqueantes:
- **P5-1 (preexistente)**: el hook interactivo `--newconfig` llama a
  `OpenSolutionInWindow(loadedSolutionPath)` sin `forceRefresh` tras
  `AddSolutionConfiguration` → el combo de configuraciones no se refresca
  hasta el siguiente open completo. Solo afecta al diálogo QA interactivo;
  el hook automatizado `--newconfig-real` aserta sobre disco. No se corrige
  en este slice (fuera de alcance; documentado).
- **P5-2 (carrera milisegundos)**: si el proceso terminara de forma natural
  entre el chequeo `HasExited == false` y el `Kill()` en `StopBuildOrRun`,
  el log diría "stopped by user" sobre una salida natural. El guard
  `HasExited` cierra la ventana realista; el efecto residual es una línea de
  log inexacta.

### Queda pendiente

- **Slice B (Oleada 3)**: items 4+10 — Ctrl+Z/Y en el editor (undo/redo del
  backend) + feed caret en el historial de navegación.
- **Slice C (Oleada 3)**: items 1+7 — render de array commands en legacy +
  endurecer la regresión.
- **Slices D-F (Oleada 4)**: RunConfigurations vía modelo (D); build vía
  modelo de configuraciones (E); Add Reference vía modelo (F).

### Cierre

Commit de código (5 ficheros) pushado a
`origin/agents/sub-agent-senior-dev-role-report`; docs en este commit.
Sin cambios en submódulos. La ruta `--old-gui` no se toca: el único fichero
con diff que entra en la build legacy es `ProjectCommands.cs` (rename puro
verificado con build legacy 0 errores) y `Commands.addin.xml` (string
resuelta en runtime, verificada por el catálogo 475/111 en runtime y el
menudump).

## 2026-11-07 (17) — Oleada 3 (editor): Slice B — items 4 y 10

### Contexto

El plan de cierre de la Oleada 3 reparte lo que quedaba tras el Slice A:
**B (este): items 4+10** (Ctrl+Z/Y en el editor + feed caret en el
historial de navegación); C: items 1+7 (render legacy array commands +
endurecer regresión). El item 4 era el P5 del slice 7 ("los atajos
Ctrl+Z/Ctrl+Y están tragados por isTextEditingCombo del window y no
existen en SkTextEditor.OnKeyDown") y el item 10 el P5 del slice 11
("Feed solo en activación de documento; sin feed de movimiento de caret").
El slice no toca el modelo: solo el editor, el servicio de navegación del
shell y el main window.

### Qué se hizo (3 ficheros de código)

**`SkTextEditor.cs`** (editor):
- **Item 10 (evento caret)**: los campos `caretLine`/`caretCol` pasan a
  propiedades (backing `_caretLine`/`_caretCol`) que disparan
  `public event Action? CaretMoved` en CUALQUIER cambio. Todos los
  movimientos de caret (teclas, clicks, gutter, goto, búsqueda, inserts,
  backspace) pasan por esos dos campos, así que el evento es el único
  punto de enganche — port del `ITextView.Caret.PositionChanged` legacy.
  Mismo nombre y tipo en las ~20 asignaciones: 0 call-sites tocados.
- **Item 4 (Ctrl+Z/Ctrl+Y)**: casos en `OnKeyDown`: `Key.Z when ctrl →
  Undo()`, `Key.Y when ctrl → Redo()` (mismo patrón que los casos
  X/C/V/A/K/D existentes; `e.Handled = true`). El gate es la propia pila
  del editor: `Undo()`/`Redo()` no-op con pila vacía — el mismo gate que
  los comandos de menú (`CanUndo`/`CanRedo` = `undoStack/redoStack.Count >
  0`). `isTextEditingCombo` del window NO se toca: un combobox editable
  con foco conserva su Ctrl+Z nativo.
- **`SendKeyForQa(Key, KeyModifiers)`**: hook QA que enruta una tecla
  sintética por el `OnKeyDown` REAL (Avalonia 12.1.2 no expone ctor
  parametrizado de `KeyEventArgs` — verificado por reflexión —; el evento
  se construye con ctor vacío + setters públicos Key/PhysicalKey/
  KeyModifiers) y devuelve `e.Handled`.

**`Services/NavigationHistoryService.cs`** (servicio del shell):
- **`FeedCaret(file, line1Based)`**: port del `LogActiveDocument(true)`
  legacy sobre `Caret.PositionChanged`: si el punto actual es el mismo
  file → actualiza la línea IN-PLACE (`Changed` solo si cambió; NUNCA
  trunca el forward-branch); file distinto o historial vacío → `Push`
  (como una activación); línea clamp ≥ 1. Sin timers transient de 10 s:
  los puntos del shell son permanentes de nacimiento (el legacy hacía el
  colapso vía `ShouldReplace`/`ReplaceCurrent`).

**`MainWindow.axaml.cs`** (wiring shell + driver):
- **`AttachNavigationCaretFeed()`** en las dos ramas de
  `OnDocSelectionChanged` (selección y vacía): desuscribe el editor
  anterior (`CaretMoved -= EditorCaretMoved`) y suscribe al editor del tab
  seleccionado si existe con `FilePath` no vacío. `EditorCaretMoved()` →
  `FeedCaret(ed.FilePath, ed.CurrentLine + 1)`. Port del par legacy
  `AttachToDoc`/`DetachFromCurrentDoc`; idempotente. El feed continuo
  hace innecesario el `LogActiveDocument()` que el legacy
  `MoveBack`/`MoveForward` hacen antes de mover: al pulsar Back el punto
  actual ya lleva la línea del caret.
- **Driver `--cmdstates`**: fase nav reescrita con 2 probe files (12
  líneas cada uno, deterministas): open `probe.txt:3` (el feed colapsa el
  punto de activación in-place — `histN=1 hist0=probe:3`, sin apilar
  `:1`+`:3`), salto a `probe2.txt:5`, `GotoLine(8)` en el doc activo
  (feed in-place → línea 9, `histN` se mantiene 2), `NavigateBack`
  (current = probe:3, `fwdEn=1`), `NavigateForward` (regresa a
  probe2:**9** — la línea de caret, NO la 5 del salto), submenu
  `navpoint:0|3|`, clear. Los `PushNavigationPoint()` explícitos se
  quedan: los duplicados se ignoran, así que son no-ops bajo el feed.
  Fase nueva `undoredox`: insertar `_qa` en el caret →
  `SendKeyForQa(Z, Control)` (undo) → `SendKeyForQa(Y, Control)` (redo) →
  `SendKeyForQa(Z, None)` (debe NO undo) → `Undo()` final que deja el
  probe limpio; emite `[undoredox] inserted=… ctrlZ=… ctrlY=… bareZ=…`.

### Hallazgos y decisiones

- **Propiedades en vez de llamadas explícitas en cada sitio**: los dos
  campos son el punto de paso único de cualquier movimiento de caret
  (teclado, ratón, gutter, programático), así que el hook único en los
  setters es equivalente al `Caret.PositionChanged` legacy sin tocar los
  ~20 call-sites.
- **Double-fire benigno**: un movimiento que cambia línea Y columna
  dispara el evento dos veces; la segunda `FeedCaret` ve mismo file +
  misma línea → no-op sin `Changed` (sin rebuild del submenú Navigate
  History).
- **`navSwitching` no puede corromper un aterrizaje**: `MoveBack`/
  `MoveForward`/`MoveTo` actualizan `position` ANTES de ejecutar
  `NavigateToPoint`, y `OpenFileDocumentAtLine` aterriza exactamente en
  `p.Line` (`GotoLine(p.Line)`) → cualquier feed que dispare durante el
  switch ve mismo file + misma línea = no-op. Confirmado en el E2E:
  `histN` se mantiene 2 durante back/fwd/submenú.
- **El undo/redo por tecla mueve el caret** (el stack restaura
  `(text, line, col)`) → el feed actualiza el punto de navegación a la
  línea post-undo: comportamiento EXACTO del legacy (undo mueve el caret
  → `Caret.PositionChanged` → `LogActiveDocument`).
- **Builds**: `SkTextEditor.cs` y `Services/NavigationHistoryService.cs`
  solo entran en el build Avalonia vía los glob `Controls/**` y
  `Services/**` del `MonoDevelop.Startup.Avalonia.csproj`; el legacy
  `MonoDevelop.Ide.csproj` (lista explícita,
  `EnableDefaultCompileItems=false`) NO los incluye → la ruta
  `--old-gui` no compila los ficheros de este slice (build legacy 0
  errores).

### Números clave

- Builds: net10.0 **0 errores**; net10.0-windows **0 errores**; legacy
  `MonoDevelop.Ide.csproj` **0 errores** (solo warnings preexistentes).
- Catálogo: `Count=461 LoadedCommands=475 ResolvedHandlers=111` (sin
  cambio).
- Menudump (tras `reset_recents`): `menus=11 items=225 cmds=0
  bindings=196` (sin cambio).
- E2E `--cmdstates` (3 casos): 0 `[warn]`/`[fatal]`/Exception en los 3
  logs; 4 aserciones slice A PASS (regresión: stop, poststop, node_file
  F3, solution-options rename) + **4 aserciones slice B PASS**:
  `navopened fed` (`histN=1 hist0=oleada3_nav_probe.txt:3 backEn=0`),
  `navfeed` (`histN=2 hist0=oleada3_nav_probe2.txt:9 backEn=1`),
  `navfwd fed line` (`hist0=oleada3_nav_probe2.txt:9 backEn=1 fwdEn=0`),
  `ctrl+Z/Y keys` (`[undoredox] inserted=True ctrlZ=True ctrlY=True
  bareZ=True` — idéntico en los 3 casos).
- Probe `--newproject` (copia del fixture): **ALL_PASS** (regresión del
  pad).
- E2E oleada4 (6 casos): **ALL_PASS** (incluye menudump determinista).
- Oracle: `oleada3 51`, `oleada3dyn 19`, `oleada3tools 5`, **`oleada3nav
  29`** (22 previos + 7 `FeedCaret` nuevos: push-vacío con `Changed`,
  no-op mismo punto sin `Changed`, in-place con `Changed`, clamp ≥ 1,
  file distinto → push, back-point in-place con forward intacto,
  `MoveForward` tras back-feed), `oleada4 18`, `oleada4s2 35`, `fail=0`
  en todo, exit 0.

### Cómo validar

- Builds: `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c Debug -f net10.0 --nologo` (y `-f net10.0-windows`); legacy: `dotnet build main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.csproj -c Debug --nologo`.
- E2E: `bash ~/opencode/oleada3_cmdstates.sh` → `SLICEA_ALL_PASS` +
  `E2E_DONE` + 0 warn/Exception en los 3 logs (4 aserciones slice A + 4
  slice B).
- Probe: `bash ~/opencode/oleada3_sliceA_probe.sh` → `ALL_PASS`.
- E2E Oleada 4: `bash ~/opencode/oleada4_slice1_e2e.sh` → `ALL_PASS`.
- Oracle: `cd ~/opencode/catload_test && dotnet run -c Debug` → exit 0,
  `oleada3nav: ok=29 fail=0`.
- Nota: no ejecutar dos harness E2E a la vez (cada caso mata
  `main/build/MonoDevelop.AvaloniaShell.dll`).

### QA (§18.5)

Brief + reporte en `~/opencode/oleada3_sliceB_qa/` (`brief.md`,
`qa_report_r1.md`, logs `run_r1.log`/`run_r1b.log`). **Ronda 1:
APROBADO — 0 hallazgos P1–P4**, 3 P5 no bloqueantes:
- **P5-1 (harness)**: líneas `Terminado (killed)` de job-control de bash
  en la salida del E2E al matar los procesos `nohup` de los scripts — no
  son `[warn]`/`[fatal]`/Exception de la aplicación (los 3 logs limpios).
- **P5-2 (API)**: `SendKeyForQa` construye `KeyEventArgs` sin
  `RoutedEvent` (limitación del API de Avalonia 12 — no hay ctor
  parametrizado); funciona (E2E verde) pero una futura versión de
  Avalonia podría ser más estricta en `base.OnKeyDown`.
- **P5-3 (diseño)**: `CaretMoved` dispara desde constructor/`SetLines`
  sin suscriptores: no-op null-conditional, protegido, documentado.

Nota operativa: el subagente QA corrió con `logfare/kimi-k2.7-code` (vía
`opencode run --standalone`): el modelo por defecto de opencode local
(`iceman-free`) tenía la **API key expirada** (OpenCode Console), y el
provider `logfare` requirió registrar los modelos en el catálogo de la
config + `env: ["LOGFARE_API_KEY"]` (la key vive en
`~/.config/opencode-cli/opencode.json`). La sesión se cortó una vez
(tras el oracle, sin error registrado) y se reanudó con `--session` para
completar los E2E y el veredicto.

### Queda pendiente

- **Slice C (Oleada 3)**: items 1+7 — render legacy array commands +
  endurecer regresión.
- **Slices D-F (Oleada 4)**: RunConfigurations vía modelo (D); build vía
  modelo de configuraciones (E); Add Reference vía modelo (F).

### Cierre

Commits de código (3 ficheros) + docs pushados a
`origin/agents/sub-agent-senior-dev-role-report`. Sin cambios en
submódulos. La ruta `--old-gui` no se toca: ninguno de los 3 ficheros
del slice se compila en el build legacy (verificado con build legacy 0
errores + lista explícita del csproj).
## 2026-11-07 (18) — Oleada 3 (array commands): Slice C — items 1 y 7

### Contexto

Último slice de la Oleada 3. Item 1: **render legacy completo de los
array commands** — el legacy GTK (`CommandMenuItem.Update`) renderiza el
menú de un array command con el **master siempre oculto** y los **hijos
inline como hermanos** en el menú padre (no como submenú del master).
Item 7 (QA F6 del slice 8): **endurecer la regresión** de los submenús
dinámicos — el dump `--cmdstates` no veía el payload de dispatch (`Tag`
del hijo), así que un hijo que disparara el `DataItem` equivocado no
habría sido detectable por el E2E.

Los **10 array masters reales** del catálogo (verificado en vivo):
`ViewList`, `LayoutList`, `ToggleIssues` (Inline Messages),
`NavigateHistory` (View > Navigation), `CustomCommandList` (Project),
`RunTestWith`, `DebugTest` (Unit Testing), `ToolList` (Tools),
`OpenWindowList`, `OpenDocumentList` (Window).

### Qué se hizo (2 ficheros de código)

**`MonoDevelop.Startup.Avalonia/Views/MenuBuilder.cs`** (reescrito del
mecanismo de arrays):
- `arrayMasters` (id → `CommandArrayInfo`), `arrayMasterParent`
  (id → `ItemsControl` capturado al build) y `arrayChildren`
  (id → hijos actuales).
- `AppendEntry`: al construir el menú, un item cuyo id es array master
  se registra como tal (y como antes se suprime su `Click`); ya no se
  construye un submenú propio.
- `RebuildArraySiblings(masterId)`: **full rebuild desde
  `CommandArrayInfo`** en cada refresh (mismo patrón que el antiguo
  `RebuildArrayChildren`, pero los hijos son **hermanos en el menú
  padre**, no hijos del master). Primero `master.IsVisible = false`
  **antes** de resolver el padre (el master nunca es visible, ni con
  array vacío). Resolución del padre: el `ItemsControl` capturado en
  `arrayMasterParent` si aún contiene al master; si no,
  `master.Parent as ItemsControl` (fallback defensivo). Cada hijo:
  `Header` del texto del `DataItem`, `Tag = DataItem` (payload de
  dispatch) y `Click → CommandService.Dispatch(id, data,
  CommandSource.MainMenu)`. Los hijos preexistentes se eliminan con
  `if (parent.IndexOf(c) >= 0)` para no borrar items del modelo.
- `GetArrayChildren(id)`: expone `List<MenuItem>` para el dump QA.
- **Helpers `InsertItem`/`RemoveItem`** (raíz del rastrero, ver
  hallazgos): si el contenedor es `Avalonia.Controls.ItemCollection`
  usan los **métodos públicos** `ic.Insert`/`ic.Remove` (que delegan en
  `WritableSource`); si no, caen al `IList` genérico.

**`MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs`** (dump
`--cmdstates`, `DumpCmdStates`): `ListN` y `histItems` pasan a
obtenerse vía `MenuBuilder.GetArrayChildren(...)` (antes navegaban el
submenú del master) y se añaden **4 campos**:
- `navMasterVis=`: `IsVisible` del master `NavigateHistory` (invariante
  0).
- `docList0Tag=`: `Tag` del primer hijo de `OpenDocumentList` (string
  `DataItem`).
- `winList0Tag=`: `Tag` del primer hijo de `OpenWindowList`.
- `hist0Tag=`: `Tag` del primer hijo de `NavigateHistory`
  (`navpoint:<idx>|<line>|<file>`).

### Hallazgos y decisiones

- **ROOT CAUSE (rastrero Avalonia 12 — mutación de `ItemCollection`)**:
  en Avalonia 12 `MenuItem.Items` devuelve `Avalonia.Controls.
  ItemCollection` (una `ItemsSourceView`); sus implementaciones
  **explícitas** de mutación `IList` (`Add`/`Insert`/`Remove`/
  `RemoveAt`/`Clear`) lanzan **siempre**
  `NotSupportedException: Collection is read-only.`, incluso en modo
  Items escribible. Verificado por decompilación de
  `Avalonia.Controls.dll` 12.1.2 (`~/.nuget/packages/avalonia/12.1.2/
  lib/net10.0/Avalonia.Controls.dll` con `ilspycmd`; dump en
  `~/opencode/avctl.cs`, clases en L29576/L30640). El síntoma era
  `Error loading solution: Collection is read-only.` en
  `OpenSolutionInWindow` vía `NotifyCommandStateChanged →
  RefreshCommandStates → RebuildArraySiblings` (mutar los `Items` del
  menú padre por el cast a `IList` explotaba). Fix: toda mutación de
  `Items` en `MenuBuilder` pasa por `InsertItem`/`RemoveItem`
  (métodos públicos de `ItemCollection` → `WritableSource`).
- **Master SIEMPRE oculto**: el legacy oculta el master con array
  vacío y el master de array inline **no es visible nunca** (los hijos
  son los ítems visibles). Se fija `master.IsVisible = false` al
  inicio de cada rebuild, por lo que no depende del estado anterior ni
  del refresh de habilitación.
- **Tag como contrato de dispatch**: el hijo lleva `Tag = DataItem` y
  el click dispara `Dispatch(id, Tag)`. El dump expone el `Tag` del
  primer hijo de 3 familias (docs/windows/nav) para que el E2E
  aserte el payload, no solo la visibilidad.
- Sin acoplamiento a `Mono.Addins` y sin archivos nuevos; el diff se
  limita a los 2 ficheros (ambos solo entran en el build Avalonia vía
  el glob `Views/**` del csproj Avalonia — el build legacy
  `MonoDevelop.Ide.csproj` usa lista explícita y no los compila).

### Números clave

- Build: net10.0, net10.0-windows y legacy `MonoDevelop.Ide.csproj` →
  **0 errores** (3/3).
- Oracle (`~/opencode/catload_test`): catálogo `Count=461
  LoadedCommands=475 LastError=(null)`, `ResolvedHandlers=111`;
  secciones `oleada3 ok=51`, `oleada3dyn ok=19`, `oleada3tools ok=5`,
  `oleada3nav ok=29`, `oleada4 ok=18`, `oleada4s2 ok=35` — todo
  `fail=0`.
- E2E `~/opencode/oleada3_cmdstates.sh` (3 casos): `SLICEA_ALL_PASS`,
  **12/12 aserciones** (4 slice A + 4 slice B de regresión + 4 slice
  C): `docList0Tag docs12=QA List10`, `docList0Tag swap=QA List12`,
  `hist0Tag navfeed=navpoint:1|9|…/oleada3_nav_probe2.txt`,
  `navMasterVis=0` + `winList0Tag=Window 1`; **0** líneas
  `[warn]`/`[fatal]`/`Exception` en los 3 logs.
- Smoke (`~/opencode/smoke_oleada1.sh`): `Loaded Fixture.sln` **sin**
  `Error loading solution`; menudump `menus=11 items=225 cmds=0`
  (19/35/39/27/13/7/35/14/8/15/13); `bindings=202` (fluctuante por
  entorno, no invariante).
- Probe slice A: `ALL_PASS`. E2E regresión Oleada 4: `ALL_PASS`.

### Cómo validar

1. Los 3 builds de arriba (nunca `--no-incremental`).
2. `cd ~/opencode/catload_test && dotnet run -c Debug` (todo `fail=0`).
3. `bash ~/opencode/oleada3_cmdstates.sh` → `SLICEA_ALL_PASS` + 12
   aserciones PASS.
4. `bash ~/opencode/smoke_oleada1.sh` → `Loaded Fixture.sln` sin error
   + menudump 11/225/0.

### QA (§18.5)

Subagente Tester QA Senior, **1 ronda**: **APROBADO**, **0 hallazgos
P1–P4 y 0 P5**. Revisión punto a punto de los 9 riesgos del brief
(master nunca visible; payload de dispatch `Tag`; colisión de ids en
`Mi`/`En`; menudump del modelo intacto; `--old-gui` no rota; rastrera
`ItemCollection` — verificó por decompilación que `ItemCollection`
reimplementa `int IList.Add(object?)` redirigiendo al método público,
por lo que el `target.Add` de `AppendEntry` tampoco explota; carga de
solución; idempotencia del rebuild; sin acoplamiento `Mono.Addins` ni
archivos nuevos). Brief + prompt + logs + reporte en
`~/opencode/oleada3_sliceC_qa/` (`qa_report_r1.md`).

Nota operativa: `opencode run` (v2.2.6) lee el config de
`~/.config/opencode/opencode.json`, pero el provider `logfare` vivía en
`~/.config/opencode-cli/opencode.json` → "Model unavailable" al
lanzar la QA; se copió el config a la ruta correcta (chmod 600). El
cliente de la QA fue SIGKILL 3 veces por causa externa sin rastro de
OOM (journal limpio); el 3.º lanzamiento (cliente delgado, sin
`--standalone`) dejó la sesión corriendo en el daemon
`opencode serve --service`, que completó el veredicto de forma
autónoma.

### Queda pendiente

- **Slices D–F (Oleada 4)**: RunConfigurations vía modelo (D); build
  vía modelo de configuraciones (E); Add Reference vía modelo (F).
- Con este slice quedan **cerrados los 10 items de la lista pendiente
  de la Oleada 3**.

### Cierre

Commits de código (2 ficheros) + docs pushados a
`origin/agents/sub-agent-senior-dev-role-report`. Sin cambios en
submódulos. La ruta `--old-gui` no se toca (build legacy 0 errores +
los 2 ficheros no se compilan en el csproj legacy).

## 2026-11-08 (19) — Oleada 4 (backend Avalonia): RunConfigurations vía modelo — Slice D

**Contexto**: tercera slice de la **Oleada 4** (Slice 1: capa .sln sobre
`SlnFile`; Slice 2: modelo .csproj + árbol de solution-folders). Alcance: el
item (3) de la lista pendiente — **RunConfigurations reales sobre el modelo**
— más el cableado del proyecto de arranque: la shell ya no lleva un parseo
inline ad-hoc del csproj (que confundía build configs `$(Configuration)` con
run configs `$(RunConfiguration)`); todo pasa por `ConfigurationService`.
**Nota del operador (en vigor)**: la shell no utiliza Mono.Addins.

**Decisiones de diseño** (detalle en `~/opencode/oleada4_sliceD_design.md`):
- **Modelo fiel al legacy** (`ParseRunConfigurationCondition`/
  `ExtractConfigName` de `Project.cs`): LHS de la condición debe ser
  EXACTAMENTE `'$(RunConfiguration)'` (el trim solo actúa fuera de las
  comillas — un espacio interior al LHS se rechaza, como en el legacy) y el
  RHS un nombre entre comillas simples sin comilla interior y longitud >2.
  Los build groups (`$(Configuration)`, `$(Platform)`) quedan fuera.
- **Semántica de props = la evaluación legacy (merge prop-a-prop)**: un
  nombre presente en el `.csproj` y en el `.csproj.user` es una sola entrada;
  prop-a-prop gana el grupo posterior (user) y el anterior da fallback.
  Verificado contra `MSBuildProjectInstance.GetPropertiesLinkedToGroup`, que
  devuelve la **colección evaluada completa** (linkada al grupo, no limitada
  a él). El primer intento del dev ("el último grupo reemplaza todo" con
  `group.Clear()`) era incorrecto y se corrigió al leer el código legacy.
- **StartupItem en `.userprefs`**: la forma legacy
  (`Solution.OnEndLoad`: sección `MonoDevelop.Ide.Workspace`,
  `Property name="StartupItem"`, path relativo al .sln) en el user file de la
  solución, que es `<sln-sin-ext>.userprefs` — p. ej. `Fixture.userprefs`,
  NO `Fixture.sln.userprefs` (confirmado con el fixture real). Set hace
  replace (una sola prop, sin append), recrea un .userprefs corrupto, crea el
  fichero si no existe; Remove devuelve si existía.
- **`--runcfg` en la allow-list de `QaDialogArg`** (Program.cs, 1 línea): sin
  ella `qa` queda `""` y el driver no se ejecuta (hallado en el primer E2E:
  el caso no emitía ni la línea de fallback).

**Cambios** (3 ficheros):
- `MonoDevelop.Ide/Services/ConfigurationService.cs` (coda, sin ficheros
  nuevos): `RunConfigurationEntry{Name, StartArguments?, WorkingDirectory?,
  PauseConsoleOutput?, ExternalConsole?}`; `ParseRunCondition`;
  `GetProjectRunConfigurations` (csproj + .user; orden de documento; merge
  prop-a-prop; corrupto/ausente → contribuye nada, sin throw); `PrefsPath`;
  `Get/Set/RemoveSolutionStartupItem`; `GetSolutionRunConfigurations`
  (agregado por `GetSolutionProjects`, orden .sln).
- `MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` (6 cambios): ctor
  `RunConfigCombo` solo "Default" (sin Debug/Release hardcoded);
  `RefreshConfigurationSelectors` repuebla el combo vía modelo (vacío →
  ["Default"]); `PickStartupProject` respeta el StartupItem persistido
  (rel→abs contra el dir del .sln; debe estar en la lista de proyectos; si
  no, fallback 1er Exe); `ShowExecutionModeSelector` sustituye el parse
  inline por `GetProjectRunConfigurations`; `RunStartupProjectAsync` toma el
  nombre del combo y añade `--args "…"` SOLO si la runcfg elegida define
  StartArguments (también en modo mono/external); línea `[run]` +
  `runcfg=<name>`; driver QA `--runcfg` (11 aserciones).
- `MonoDevelop.Startup.Avalonia/Program.cs`: `--runcfg` en `QaDialogArg`.

**Verificación** (ejecutada en dev, antes de QA):
- Builds 0 errores: net10.0, net10.0-windows y el build legacy
  `MonoDevelop.Ide.csproj` (los 3 ficheros del diff NO entran en el csproj
  legacy — lista explícita; `ConfigurationService.cs` solo lo compila el
  csproj Avalonia vía el glob `Services/**` → `--old-gui` no toca nada del
  diff).
- Oracle `catload_test` exit 0: catálogo `Count=461 LoadedCommands=475
  LastError=(null)`, `ResolvedHandlers=111`; secciones `oleada3 51/51`,
  `oleada3dyn 19/19`, `oleada3tools 5/5`, `oleada3nav 29/29`, `oleada4
  18/18`, **`oleada4d 21/21`** (nueva: nombres/orden de documento; user-wins
  prop-a-prop — Default: args del user + ConsolePause del csproj; grupo puro
  MyCfg; grupo solo-user UserOnly; build group `$(Configuration)` rechazado;
  condiciones inválidas X1–X6 rechazadas y X7 aceptada por el trim exterior;
  corruptos csproj/user/.userprefs sin throw; agregado por solución en orden
  .sln; round-trip StartupItem set/replace/remove/recreate/sin .userprefs) y
  `oleada4s2 35/35`.
- E2E nuevo `bash ~/opencode/oleada4_sliceD_e2e.sh` (3 casos sobre copias del
  fixture) → **ALL_PASS**: `runcfg` 11/11 (initial sin StartupItem; fallback
  App; sin run configs en fixture plano; combo ["Default"]; set LibA → pick
  respeta; replace sin append en el .userprefs; item inválido → fallback
  App; remove existed/gone/no-op); `menudump` `menus=11 items=225 cmds=0
  bindings=196`; `activeconfig` `restored=Debug` (regresión: el path de
  config activo no se tocó).
- Regresiones: `bash ~/opencode/oleada4_slice1_e2e.sh` (6 casos) →
  **ALL_PASS**; `bash ~/opencode/oleada3_cmdstates.sh` (3 casos) →
  **SLICEA_ALL_PASS** (12 aserciones); `bash ~/opencode/smoke_oleada1.sh` →
  `Loaded Fixture.sln` sin `Error loading solution` + menudump 11/225/0 con
  conteos 19/35/39/27/13/7/35/14/8/15/13.
- Fixture de referencia intacto (`~/opencode/solpad_deps/` sin mutar: los
  E2E trabajan sobre copias).

### QA (§18.5)

Subagente Tester QA Senior, **1 ronda**: **APROBADO**, 0 hallazgos P1–P4,
2 P5 (revisión punto a punto de los 10 riesgos del brief: fidelidad del
parse; user-wins prop-a-prop — verificó que no queda `group.Clear()` en el
diff; corruptos; forma legacy del StartupItem; PickStartupProject con
fallback; RunConfigCombo y OnToolbarConfigChanged; Run With sin parse
inline; `--args` condicional; sin acoplamiento legacy ni Mono.Addins;
fixture intacto). P5-a: indentación desalineada en 2 líneas de
`RunStartupProjectAsync` (bloque mono y `ProcessStartInfo` de xterm) →
corregida en esta misma ronda. P5-b: falsamente positivo — señalaba
`IndexOf("==")` sin `StringComparison.Ordinal`, pero el código ya lo llevaba
(verificado en la línea real del diff). Tras el fix de indentación,
re-verificado: 3 builds 0 errores, oracle `oleada4d 21/21` exit 0 y smoke
`Loaded Fixture.sln` sin error. Brief + prompt + reporte en
`~/opencode/oleada4_sliceD_qa/` (`qa_report_r1.md`).

### Queda pendiente (slices E–F de la Oleada 4)

- **Slice E**: build vía modelo de configuraciones.
- **Slice F**: Add Reference vía modelo.

### Cierre

Commits de código (3 ficheros) + docs pushados a
`origin/agents/sub-agent-senior-dev-role-report`. Sin cambios en submódulos.
La ruta `--old-gui` no se toca (build legacy 0 errores + ninguno de los 3
ficheros del diff entra en el csproj legacy).

## 2026-11-08 (20) — Oleada 4 (backend Avalonia): build vía modelo de configuraciones (mapeo solución→proyecto) — Slice E

**Contexto**: cuarta slice de la **Oleada 4** (Slice 1: modelo .sln real sobre `SlnFile`; Slice 2: modelo .csproj + árbol de solution-folders; Slice D: RunConfigurations vía modelo). Alcance: el item (4) de la lista pendiente — **build vía modelo de configuraciones**: `RunBuildAsync` (shell Avalonia) ya usaba la configuración activa persistida (slice 1) pero le pasaba la configuración de solución cruda a todos los proyectos; faltaba el mapeo que consume el build de solución legacy: la sección `ProjectConfigurationPlatforms` del .sln (`"{guid}.{slnCfg}|{slnPlat}.ActiveCfg = {projCfg}|{projPlat}"`), de modo que un proyecto pueda buildarse con una configuración distinta a la de la solución (p. ej. solución Debug → proyecto Release).

**Cambio** (3 ficheros, sin ficheros nuevos, sin carpetas nuevas; sin submódulos):
- `main/src/core/MonoDevelop.Ide/Services/ConfigurationService.cs` (coda L~740-817): `GetProjectConfigurationFor(slnPath, solutionConfig, projectPath)` → `record ProjectConfigurationMapping(Configuration, Platform)`. Lee SOLO propiedades `*.ActiveCfg` del set de propiedades cuyo GUID iguala al del proyecto (case/space/brace-insensitive); ignora `Build.0` (ese flag decide SI se builda, no CON QUÉ configuración). Match de la config de solución case-insensitive: nombre simple compara la parte antes de `|`, clave completa la clave entera. Valor sin `|` → plataforma "Any CPU". Fallback `(solutionConfig, "Any CPU")` SIN throw: sln ausente, sln corrupto (catch), proyecto no listado en el .sln, proyecto sin entrada.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs`: `RunBuildAsync` resuelve por proyecto vía el modelo; línea `[build] project <name> cfg=.. plat=..` (valores resueltos, no crudos); comando `dotnet <target> -c "<mapped.Configuration>"` + `-p:Platform="<plat>"` solo si la plataforma no es AnyCPU (nuevo miembro `IsAnyCpuPlatform`: vacío/"Any CPU"/"AnyCPU" ignoring case y espacios); línea final `[build] <target> done failed=<bool>` (la variable `failed` ya existía pero no se reportaba). Driver QA `--buildmap` (model-check por proyecto `[buildmap] model <name> cfg=.. plat=..` + `await RunBuildAsync()` production path + `[buildmap] all pass`).
- `main/src/core/MonoDevelop.Startup.Avalonia/Program.cs`: `--buildmap` en la allow-list `QaDialogArg` (1 línea).

**Compatibilidad**: con fixture plano AnyCPU (todo `Debug|Any CPU`/`Release|Any CPU`) el mapeo es la identidad → comandos byte-idénticos al pre-E (`dotnet build -c "Debug" "<proj>"` sin `-p:Platform`).

**Hallazgo de dev (contexto, no defecto del slice)**: el fixture `~/opencode/solpad_deps/` tiene bugs de compilación **preexistentes** (era fixture de carga, nunca de build): `LibA/A.cs` usa `B.V` sin `using LibB;` (B está en el namespace `LibB`) y `App/Program.cs` usa `Console` sin `using System;` → CS0103 en cualquier build (reproducido manualmente con el comando exacto pre-E, con el cambio revertido). El E2E de slice E parchea ambos archivos **solo en la copia**; el fixture real queda intacto (asertado al final del script y por QA).

**Verificación (valores medidos)**: 0 errores en net10.0, net10.0-windows **y legacy** `MonoDevelop.Ide.csproj` (los 3 ficheros del diff no entran en el csproj legacy — `ConfigurationService.cs` solo lo compila el csproj Avalonia vía el glob `Services/**`); catálogo `Count=461 LoadedCommands=475 LastError=(null) ResolvedHandlers=111`; menudump `menus=11 items=225 cmds=0`; oracle sección nueva **`oleada4e 13/13`** (fixture LibA Debug, fixture App Release, mapeo LibA Debug→Release, otros proyectos inafectados, Release intacto, sin entrada, sln ausente, sln corrupto, proyecto fuera del sln, plataforma x86, case-insensitive "debug", clave completa "Debug|Any CPU", valor sin plataforma) + todas las anteriores intactas (`oleada3 51`, `oleada3dyn 19`, `oleada3tools 5`, `oleada3nav 29`, `oleada4 18`, `oleada4d 21`, `oleada4s2 35`); E2E nuevo `~/opencode/oleada4_sliceE_e2e.sh` (2 casos sobre copias del fixture; parches `A.cs`/`Program.cs` solo en la copia) **ALL_PASS** (buildplain: 3x `model`/`[build] project` `cfg=Debug plat=Any CPU`, `failed=False`, 3x exit 0, sin warns; buildmap: mapeo en el .sln `LibA …Debug|Any CPU.ActiveCfg = Release|Any CPU` → model+`[build]` con **LibA `cfg=Release`**, App/LibB `cfg=Debug`, `failed=False`, 3x exit 0; fixture intact al final); regresión E2E slice D **ALL_PASS** (runcfg 11/11, menudump `bindings=196` con recents reseteados, activeconfig `restored=Debug`); regresión `--cmdstates` **SLICEA_ALL_PASS** (12 aserciones); smoke `SMOKE_DONE` (menudump 11/225/0 con conteos 19/35/39/27/13/7/35/14/8/15/13; bindings=199, variación conocida sin resetear recents); fixture real intacto tras la batería completa.

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief+reporte en `~/opencode/oleada4_sliceE_qa/`): **APROBADO**, 0 hallazgos P1–P4 y 0 P5 (revisión punto a punto de los 8 riesgos del brief: lectura exclusiva de `*.ActiveCfg` por GUID, los 4 fallbacks sin throw, `-c` del mapeo por proyecto + log resuelto, sin `-p:Platform` con AnyCPU, sin acople legacy, driver `--buildmap` dentro del if/else sin duplicar `RunBuildAsync`, no mutación del fixture, `failed` sin alterar contador ni orden). La ronda se ejecutó en dos fases: la sesión inicial (cliente SIGKILL, patrón conocido) completó la batería E2E y sus logs quedan como artefacto; la continuación re-ejecutó builds/oracle/E2E slice E, verificó los artefactos de regresión y emitió la revisión de código y el reporte.

**Queda pendiente (Oleada 4)**: Add Reference vía modelo (F).

### Cierre

Commits de código (3 ficheros) + docs pushados a `origin/agents/sub-agent-senior-dev-role-report`. Sin cambios en submódulos. La ruta `--old-gui` no se toca (build legacy 0 errores + los 3 ficheros no se compilan en el csproj legacy).

## 2026-11-08 (21) — Oleada 4 (backend Avalonia) — Slice F: Add Reference vía modelo

**Estado: CERRADO (QA APROBADO).** Último slice de la Oleada 4. El diálogo Add Reference pasa del XDocument privado (load/save ad-hoc + disk scan de candidatos) al modelo de proyecto `ConfigurationService`: `ProjectFileModel.References` + `record ReferenceItem(Kind, Include, Version?, HintPath?)` + writer `SaveProjectReferences(csproj, refs) → bool`. El Include es el valor **RAW** del atributo (un ProjectReference conserva `..\LibA\LibA.csproj`; las rutas resueltas ya existían en `ProjectReferences`, que consume el pad).

**Cambio** (3 ficheros, sin carpetas nuevas, sin submódulos, `Program.cs` intacto):
1. `main/src/core/MonoDevelop.Ide/Services/ConfigurationService.cs` — `References` en `ProjectFileModel` (L~405; colección en `ParseProjectFile` L~624: solo PackageReference/ProjectReference/Reference con Include no vacío, en orden de documento), `ReferenceItem` (L~439) y `SaveProjectReferences` (L~451): borra los items de los 3 tipos (ItemGroup solo si queda vacío **y** sin atributos — FrameworkReference y PropertyGroups intactos), añade un ItemGroup con el set (Include siempre; Version solo si no null; HintPath nunca — paridad con el rewrite legacy). Save fiel: detecta BOM/`<?xml?>`/newlines por bytes del original (el `doc.Save(path)` añadiría BOM+decl — corregido con XmlWriter explícito). Corrupto/ausente → false sin throw y sin tocar el fichero (política slice D).
2. `main/src/core/MonoDevelop.Startup.Avalonia/Views/AddReferenceDialog.axaml.cs` — `LoadProject` (L~159) vía `LoadProjectModel` (model null + fichero presente → output de error y diálogo vacío, tolerancia legacy; divergencia documentada: item con solo `Update` no entra en el modelo), `DiscoverCandidates` (L~192) con los proyectos de la solución vía `GetSolutionProjects` (disk scan solo sin solución), `Apply` (L~389) vía `SaveProjectReferences` (Version solo PackageReference con Secondary; HintPath nunca), nuevos `TryAddProjectReference` (L~418, include relativo) y `TryRemoveReference` (L~440).
3. `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` — driver `--addref` (L~2874-2932) ampliado: model-check + ciclo package add/remove + ciclo project add/remove vía modelo, `ClearProjectModelCache()` antes de cada re-read (cache por mtime), `ADDFR_QA_KEEP=1` salta la fase de package y las removals. Programa el **primer proyecto de la solución** (determinista; el activo depende del estado del pad — en la primera ronda del E2E resolvió a LibA, que ya referencia LibB, y `TryAddProjectReference` devolvió False por "ya referenciado": comportamiento correcto, falta de determinismo del driver).

**Hallazgo de dev (no defecto del slice)**: BOM/decl de `doc.Save(path)` detectado por el oracle (check 13) y corregido; bugs de compilación preexistentes del fixture parcheados solo en la copia del E2E.

**Verificación (valores medidos)**: 0 errores en net10.0, net10.0-windows **y legacy** `MonoDevelop.Ide.csproj` (los ficheros del diff no entran en el csproj legacy — `ConfigurationService.cs` solo lo compila el csproj Avalonia vía el glob `Services/**`); catálogo `Count=461 LoadedCommands=475 LastError=(null) ResolvedHandlers=111`; menudump `menus=11 items=225 cmds=0`; oracle sección nueva **`oleada4f 14/14`** (App 1 ProjectRef raw `..\LibA\LibA.csproj`; App package/asm=0 + FrameworkReference intacto; LibA raw; LibB doc-order package v=13.0.3 + reference con Version/HintPath null; save package+project con Include/Version/raw en fichero; re-read tras save con mtime cache; borrar todo: 0 editables + Sdk/PropertyGroup/Framework intactos + sin ItemGroup vacío; no-op byte-idéntico; corrupto→false intacto; inexistente→false; round-trip; salida sin BOM/CR) + todas las anteriores intactas (`oleada3 51`, `oleada3dyn 19`, `oleada3tools 5`, `oleada3nav 29`, `oleada4 18`, `oleada4d 21`, `oleada4e 13`, `oleada4s2 35`); E2E nuevo `~/opencode/oleada4_sliceF_e2e.sh` **ALL_PASS** (addrefpkg: model `refs=1 project=1 package=0 asm=0` → add/remove package → add LibB → `model after-add refs=2 project=2` → remove → `model after-clean refs=1`, copia limpia sin System.Json/LibB; addrefproj con `ADDFR_QA_KEEP=1`: App.csproj con LibA+LibB y `dotnet build App.csproj -c Debug` **exit 0** + App.dll (restore offline: Newtonsoft.Json 13.0.3 en caché); fixture real intacto); regresión E2E slice E **ALL_PASS**; regresión E2E slice D **ALL_PASS** (runcfg 11/11, menudump `bindings=196` con recents reseteados, activeconfig `restored=Debug`); regresión `--cmdstates` **SLICEA_ALL_PASS** (12 aserciones); smoke `SMOKE_DONE` (menudump 11/225/0 con conteos 19/35/39/27/13/7/35/14/8/15/13; bindings=199, variación conocida sin resetear recents); fixture real intacto tras la batería completa.

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief+reporte en `~/opencode/oleada4_sliceF_qa/`): **APROBADO**, 0 hallazgos P1–P4 y 0 P5 (la ronda se ejecutó en dos fases — la primera murió por SIGKILL del cliente tras completar builds+oracle, patrón conocido, y la continuación completó la batería y el reporte; revisión punto a punto de los 8 riesgos del brief: los 3 tipos en orden de documento con Include no vacío, borrado de ItemGroup solo vacío-y-sin-atributos, fidelidad de formato del save, corrupto→false sin throw, Version solo PackageReference + HintPath nunca, load por modelo + fallback disk scan solo sin solución, sin acople legacy, driver dentro del if/else con `ClearProjectModelCache()` y `ADDFR_QA_KEEP` limitado a removals).

**Queda pendiente**: nada dentro de la Oleada 4 (A–F cerrados).

### Cierre

Commits de código (3 ficheros) + docs pushados a `origin/agents/sub-agent-senior-dev-role-report`. Sin cambios en submódulos. La ruta `--old-gui` no se toca (build legacy 0 errores + los ficheros no se compilan en el csproj legacy).

## 2026-11-08 (22) — Oleada 5: Slice A (re-scopeado) — NuGet 7.9.0 en la shell Avalonia

**Estado: CERRADO (QA APROBADO).** El operador decidió que **los addins GTK no se tocan** (congelados pre-oleadas; la shell Avalonia usa sus propios addins) y ordenó reverter todos los cambios de oleadas/slices en `main/src/addins/` antes de continuar.

**Revert (verificado)**: las oleadas 1–4 NO tocaron nada bajo `main/src/addins/` (su diff = core `MonoDevelop.Ide/**`, `MonoDevelop.Startup.Avalonia/**`, submódulo `xwt`, docs) — lo único que existía eran los cambios no commiteados del Slice A original (bump del property `$(NuGetVersionNuGet)` 6.11.2→7.9.0 + 5 ficheros del addin PackageManagement + el borrado de `MonoDevelopPluginFactory.cs`). `git restore main/Directory.Build.props main/src/addins/MonoDevelop.PackageManagement/` → árbol limpio (0 ficheros; `NuGetVersionNuGet` de vuelta a 6.11.2). Builds de verificación del revert: addin 0 errores (37 s), legacy 0 errores, shell net10.0 0 errores (`~/opencode/oleada5_revert_builds.log`). El submódulo `main/external/xwt` (puntero cambiado en Oleada 3 Slice 8) NO es un addin: no revertido, señalado al operador.

**Re-scope del item 15**: 7.9.0 SOLO en el lado Avalonia. Consumo NuGet actual del lado Avalonia: el panel **NuGet > Sources** de Preferences (backend `NuGet.Configuration`/`NuGet.Common`; `using` en `Views/MainWindow.axaml.cs`, `Views/PreferencesDialog.axaml.cs`, `Views/AddReferenceDialog.axaml.cs`).

**Cambio** (1 fichero, sin submódulos, addins intactos):
1. `main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj` — `PackageReference NuGet.Configuration` de `$(NuGetVersionNuGet)` (6.11.2) a **`7.9.0` explícito** con comentario (el property compartido NO se toca: lo usan los addins GTK congelados). La shell es el único consumidor 7.9.0 y stagea su propio closure en la raíz de `main/build/`.

**Coexistencia 6.11.2/7.9.0 en el staging unificado (resuelto, sin staging por modo ni ALC)**: el closure 6.11.2 del addin PackageManagement vive en `main/build/AddIns/MonoDevelop.PackageManagement/` (14 DLLs NuGet.* junto con el addin; staging `IncludeCopyLocal` del csproj del addin), NO en la raíz; la raíz solo lleva las 3 DLLs del closure de la shell (hoy 7.9.0). El relay `--old-gui` es un proceso hijo (`dotnet main/build/MonoDevelop.dll` + `MONODEVELOP_LEGACY_UI=1`, `Program.cs:LaunchLegacyGtk`) y en `/proc/<pid>/maps` del legacy **todas** las NuGet.* mapeadas vienen de `AddIns/…` (6.11.2) — ninguna de la raíz: el loader de Mono.Addins resuelve dependencias de cada addin desde su propio directorio. Cada proceso resuelve su closure de su sitio → sin colisión de nombre simple en runtime, sin cruce de tipos entre versiones.

**Verificación (valores medidos)**: 0 errores en shell `net10.0` (12 s) + `net10.0-windows` (10 s) + addin PackageManagement (sin `-f`) + legacy `MonoDevelop.Ide.csproj` (`~/opencode/oleada5_pin79_builds.log` + `~/opencode/oleada5_sliceA_qa/qa_build_*.log`); staged `main/build/NuGet.Configuration.dll` sha256 `6566f9cc2a6db205…` = paquete 7.9.0 `lib/net8.0` exacto (assembly version 7.9.0.83); smoke `--prefs=packagesources` → `[prefs-sources] count=1` + `[prefs-nuget] restore=True updates=True`, 0 FATAL/Unhandled (el panel lee las sources vía **7.9.0 a runtime**); smoke `--prefs-tree` 0 FATAL/Unhandled (árbol completo); menudump `menus=11 items=225 cmds=0 bindings=199`; catálogo `catalog loaded: 475 commands, 111 with resolvable default handler`; `git status` = solo el csproj modificado; maps legacy = solo 6.11.2 desde `AddIns/` (el QA lo reprodujo lanzando el relay por sí mismo). Known issue preexistente (no del change): el runtime legacy no abre ventana en esta máquina (~10 min, muere solo; verificado antes y después del pin; las oleadas 1–4 verifican legacy solo por compilación).

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief+reporte en `~/opencode/oleada5_sliceA_qa/`): **APROBADO**, 0 hallazgos P1–P4; 1 P5 opcional no bloqueante: `Fixture.userprefs` se genera/reescribe como side-effect al abrir la solución en los smokes (el `.sln` queda intacto; sugerencia: aislar el userprefs fuera del fixture).

### Cierre

Commits de código (1 fichero) + docs pushados a `origin/agents/sub-agent-senior-dev-role-report`. Sin cambios en submódulos. Los addins GTK quedan congelados en 6.11.2 (0 ficheros tocados bajo `main/src/addins/`); la ruta `--old-gui` no se ve afectada (build legacy 0 errores + maps sin NuGet 7.9.0). Queda pendiente en la Oleada 5 el Slice B (gate M6 — compila completo con builder del SDK 10 real, sin bloqueos; item 14 del checklist de Extras).

## 2026-11-08 (23) — Oleada 5: Slice B (gate M6) — compila completo con el builder del SDK 10 real, sin bloqueos

**Estado: CERRADO (QA APROBADO).** Último slice de la Oleada 5: el **gate de cierre de la fase de migración** (item 14 del checklist de Extras, M6).

**Naturaleza**: slice de **verificación, sin cambio de código**. El gate ya estaba verde desde los cierres de ondas 2–4 del lote net10 (2026-10-24/26/28 — ver las secciones "Lote 1", "Lote 2 + cierre de olas 1-3", "T7" y "Oleada 4" de `docs/migration-status-report.md`; conversión de los ~77 proyectos old-style a SDK-style net10.0 por la decisión fila B del operador 2026-10-22) y las oleadas de backend 1–4 (2026-11-07/08) se construyeron sobre ese estado. La cifra histórica de 126 NU1201 (2026-10-17) estaba superada — el estado real del gate solo se sabe con `dotnet build` (el `dotnet restore` no-op no revela nada). `git status` limpio al inicio y al final.

**Evidencia del gate** (logs en `~/opencode/`):
- `dotnet build main/Main.sln -c DebugLinux -m:1 --nologo` → **`0 Errores`, exit 0** — incremental (1:49, `oleada5_sliceB_gate1.log`) y clean-room `--no-incremental` (2:04, 7067 warnings preexistentes — CS8012/MSB3277/NETSDK1206/NU1701/obsoletos; `oleada5_sliceB_gate_cleanroom.log`).
- Cobertura del clean-room: **110 proyectos únicos con salida** (111 líneas: `MonoDevelop.Startup.Avalonia` dos veces, una por TFM net10.0 y net10.0-windows). `Main.sln`: 162 csprojs únicos / 222 entradas de proyecto; 109 con `Build.0` para `DebugLinux` (mapeadas a `Debug|Any CPU`); los Windows/Mac-only (WindowsPlatform, Xwt.WPF, Xwt.Gtk.Mac, TextUICocoa, MacPlatform.Tests, WindowsAPICodePack×2, WindowsPlatform.Tests) quedan excluidos por diseño (sin `Build.0`), hallazgo documentado del lote 2. Staging unificado `main/build/` regenerado.

**Batería de regresión (valores medidos)**:
- Builds 0 errores: shell `net10.0` + `net10.0-windows` (16 s) y legacy `MonoDevelop.Ide.csproj` (7 s) (`oleada5_sliceB_build_shell.log` / `..._build_legacy.log`).
- Tests `main/tests/AvaloniaShell.Editor.Tests`: **103/103** superados, 0 fallidos (`oleada5_sliceB_tests.log`).
- Smoke (`bash ~/opencode/smoke_oleada1.sh`): `Loaded Fixture.sln`; `[commands] catalog loaded: 475 commands, 111 with resolvable default handler`; `[menudump] menus=11 items=225 cmds=0` (bindings=199 sin reset de recents — fluctuación conocida 196+recents); 0 FATAL/Unhandled (`oleada5_sliceB_smoke.log`).
- Oráculos (`cd ~/opencode/catload_test && dotnet run -c Debug`): `catalog: Count=461 LoadedCommands=475 LastError=(null)`; `oleada3: ok=51 fail=0`, `oleada3dyn: ok=19 fail=0`, `oleada3tools: ok=5 fail=0`, `oleada3nav: ok=29 fail=0`, `oleada4: ok=18 fail=0`, `oleada4d: ok=21 fail=0`, `oleada4e: ok=13 fail=0`, `oleada4f: ok=14 fail=0`, `oleada4s2: ok=35 fail=0`; exit 0 (`oleada5_sliceB_oracle.log`).
- E2E: `bash ~/opencode/oleada3_cmdstates.sh` → **SLICEA_ALL_PASS** (12 aserciones sliceA/B/C; `oleada5_sliceB_e2e_cmdstates.log`); `oleada4_sliceD_e2e.sh` → **ALL_PASS** (runcfg 11/11, menudump `bindings=196` con recents reseteados, activeconfig `restored=Debug`); `oleada4_sliceE_e2e.sh` → **ALL_PASS** (buildmap: `model LibA.csproj cfg=Release` vía mapeo del .sln, `build done failed=False`, exit 0 ×3); `oleada4_sliceF_e2e.sh` → **ALL_PASS** (ciclo addref vía modelo + `dotnet build App exit 0` + App.dll).

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief+reporte en `~/opencode/oleada5_sliceB_qa/`): **APROBADO**, 0 hallazgos P1–P4, 2 P5 informativas sin acción (fluctuación de `bindings` ya documentada — la smoke dio 202 sin reset de recents — y conteo de advertencias incremental 2925 vs clean-room 7067, comportamiento esperado de MSBuild). El subagente re-ejecutó las 11 partes de la batería de forma independiente con los valores exactos esperados; `git status` limpio al inicio y al final; fixture byte-idéntico (55 ficheros, md5, incluido `Fixture.userprefs`).

**Nota (instrucción del operador)**: el informe se añade al **final** de los 3 docs (este entry, el follow-up de `docs/interfaz-plan.md` y la entrada de `docs/migration-status-report.md`); no se edita el item 14 del checklist de Extras ni la fila B del inventario, que arrastran otros pendientes que no corresponden cerrar con esta verificación.

### Cierre

Commit de docs (3 ficheros) pushado a `origin/agents/sub-agent-senior-dev-role-report` (slice sin cambio de código). Sin cambios en submódulos (punteros en HEAD). El **gate M6 queda CERRADO** y con él la Oleada 5 (Slice A + Slice B). Pendientes de la fase (fuera de la Oleada 5): rebranding (fila E — después de la paridad de UI completa), redibujo de iconos Fluent (fila F) y paridad de UI restante (M5/M6).

## 2026-11-09 (24) — Fix 1 + Fix 2: ítems legacy ausentes del menú + iconos 16px en la pad Solution

**Estado: CERRADO (QA APROBADO).** Cierre de los 2 problemas reportados por el operador sobre la shell Avalonia.

**Fix 1 — ítems de menú**: el modelo `MenuService` no contenía ítems que existen en la UI legacy (núcleo + extensiones de addins); el header del fichero los declara como presentes. Diagnóstico: NO era visibilidad runtime ni render (el driver `--menuruntime` de 3 fases demuestra el estado del modelo correcto en las 3 fases) — era AUSENCIA: los ítems nunca se portaron. Lista final: **63 entradas** (60 items + 3 submenús + separadores) en Search (sub Inspect, 4 items), Edit (DocFood + sub _XML con 7 items + ChangeLog + CodeGeneration), Project (Active Runtime + Create Package + Generate Makefiles + NuGet×3 + sub _Analyze con 2 items + Service Capabilities), Build (Create NuGet Package + Publish to Folder + Pack + Publish), Run (Stop Evaluation + Show Current Execution Line + Debug Application + breakpoints×8 + Show Disassembly + Expression Evaluator), Tools (Assembly Browser + Regex Toolkit), Version Control (VCGit 8 items, con `SwitchToBranch:branches` como id sintético de array-master por la convención `:<suffix>` del modelo) y Help>_Diagnostics (11 items de diagnostics). Dispatch: los ids sin DefaultHandler caen en el fallback "not wired in the new UI yet" (como los otros puertos pendientes). Driver QA permanente `--menuruntime` (allow-list `Program.cs` + rama en el ctor de `MainWindow`).

**Fix 2 — iconos 16px**: la pad Solution pintaba el icono del proyecto console a 32px (`project-console-32.png`, única variante console; no existe la de 16). Causa raíz: el cell renderer de `Xwt.Avalonia` ignoraba `WithSize` (bitmap a tamaño nativo + `AImage` con `Stretch.None`). Fix en `CellRenderer.ImageToSource` (submódulo `external/xwt`, rama `net10`): escala el bitmap al tamaño pedido por el frontend (contrato Xwt, `SKFilterQuality.Medium`); sin `WithSize` o con tamaño == nativo, sin cambio de comportamiento (los demás pads Xwt quedan intactos).

**Validación (valores medidos)**: gate `Main.sln` 0 errores; shell net10.0 0 errores (127 warnings preexistentes); tests 103/103; catálogo 461/475/111 inmutable; oráculos 51/19/5/29/18/21/13/14/35 todos fail=0; `--menudump` **11/288/0** (antes 225; +63) con base `bindings=256` (antes 196) +1 por recent file y 11 conteos por menú estrictos; `--menuruntime` initial **hidden=24** (antes 22: +2 array masters "Active Runtime"/"Publish" por diseño), total=300+N con N recents, **todos los ítems nuevos vis=True** en las 3 fases; diff legacy-vs-modelo **0 faltantes reales** (16 labels = 7 familias de divergencias documentadas: _Clear, Recent Pro_jects, R_ebuild/C_lean dinámicos, Run With List, Activate 1..9 vs _1.._9, Start _Debugging vs _Debug, Emoji & Symbols Mac-only); E2E cmdstates SLICEA_ALL_PASS + slices 1/D/E/F ALL_PASS (invariant actualizado a 288/256 en los scripts de `~/opencode`). Fix 2 verificado por captura + medición PIL: icono "App" (origen 32px) 14×14 en canvas 16×16, icono "Fixture" pixel-perfect, pitch de fila uniforme 19–23px en 27 filas (antes ~34px la fila "App"), 0 iconos en blanco, 0 excepciones.

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief+reporte en `~/opencode/fix1_fix2_qa/`): **APROBADO**, 0 hallazgos P1–P4, 4 P5 no bloqueantes (grep obsoleto en el script QA — corregido como artefacto; "not wired" duplicado en stdout — preexistente, 1 vez en la pad; shortcuts sin Alt no disparan desde foco del editor — preexistente; puntero sintético sin efecto en XWayland — vía de teclado cubre el dispatch). El subagente re-ejecutó las 8 partes de la batería con los valores exactos + dispatch de 3 ids nuevos por teclado (mensaje exacto en log y pad Output) + prueba pixel a pixel del Fix 2.

### Cierre

Commits pushados: (1) submódulo `external/xwt` (rama `net10`: `Xwt.Avalonia/ListBackends.cs` + QA) a `origin/net10`; (2) repo principal — `Views/MenuService.cs` + `Program.cs` + `Views/MainWindow.axaml.cs` + docs ×3 + puntero del submódulo — a `origin/agents/sub-agent-senior-dev-role-report`. Pendiente: portar los handlers de los ítems nuevos (hoy reportan "not wired") — trabajo de paridad de comandos aparte; las 16 divergencias de label quedan documentadas como intencionales.
## 2026-11-09 (25) — Ronda 2: comparación menú a menú GTK vs Avalonia y restauración de los faltantes

**Estado: CERRADO (QA APROBADO, 0 hallazgos P1–P4).** Cierre del pedido del operador: *"Siguen faltando elementos de los menús (por ejemplo Project>Manage NuGet Packages…, Project>Update NuGet Packages, Project>Restore NuGet Packages), compara con el menú de la UI GTK y restaura todos los elementos faltantes."*

**Diagnóstico**: comparación menú a menú GTK (autoridad = XML `main/build/addinxml/`) vs la UI Avalonia (volcado AT-SPI `~/opencode/menu_v4_all.out`). Los **3 NuGet citados NO faltan** — se renderizan en la UI (se añadieron en la Ronda 1, commit `87933c44f9`): Project L1 muestra `Manage NuGet Packages...`/`Update NuGet Packages`/`Restore NuGet Packages` visibles (evidencia en `menu_v4_all.out` L178-180; `strings -el` del DLL encuentra los literales UTF-16). El comparador `~/opencode/menu_compare_r2.py` dio FALTAN=37/DEMAS=99, **clasificados al 100%**: la inmensa mayoría son labels dinámicos (Build "…LibA", "_Clear", "Activate 1..9", "Start _Debugging"…), arrays ocultos por estado (array vacío con el fixture → master oculto por diseño, GTK igual: Active Runtime, Tool List, Layout/View List, Window/Document List, Navigate History, Session Recorder autohide), Mac-only (Emoji & Symbols), añadidos M16c (Continue/Stop Debugging/Unit Testing), **defectos conocidos del árbol legacy de Ronda 1** (el generador de `fix1_legacy_tree.txt` pierde VC completo, Pads, Task List, etc.) y **artefactos del parser** (el regex descartaba las líneas con prefijo de estado `x` — item deshabilitado — porque hay un espacio entre `x` y la comilla; el volcado v4 confirma que la UI sí tiene Cursor Position/Navigate Back/Forward/Clear Navigation History). El **único faltante REAL = View > Inline Messages**: faltaban los 2 hijos de array "Errors" y "Errors and Warnings" (y el check-state de "None"). **Causa raíz**: `CommandCatalogLoader.ResolveHandlerType` busca el tipo `defaultHandler` solo en el ensamblado del shell (`MonoDevelop.AvaloniaShell.dll`), pero `ToggleIssuesHandler`/`HideIssuesHandler` vivían en `MessageBubbleCommands.cs`, que no estaba compilado en el csproj del shell → handler no resuelto → array master oculto sin `CommandArrayInfo` → los hijos nunca se inlineaban. (Se descartó la visibilidad — en .NET 10 `Assembly.GetType` sí devuelve tipos non-public — y la excepción — el `--menuruntime` no mostraba ninguna.)

**Cambios** (3 de código + 1 higiene + docs ×3): `MessageBubbleCommands.cs` (handler legacy en `#if !AVALONIA_SHELL` inalterado + puertos `#else` con `MessageBubbleState` sobre `SettingsStore` + los 2 handlers); `MonoDevelop.Startup.Avalonia.csproj` (`<Compile>` del fichero); `MainWindow.axaml.cs` (`SetActiveEditorBubbleMode` público + caso del dispatcher `HideIssues` delega al punto de entrada compartido; el caso `Toggle`/`ToggleIssues` intacto); `.gitignore` (1 línea de higiene de `bin/` de tests, P5-1 del QA).

**Semántica del puerto** (paridad legacy, misma convención de defaults): `DefaultHideMessageBubbles` (default false) ↔ `!SettingsStore.GetBool("Monodevelop.ShowMessageBubbles", true)`; `ShowMessageBubbles` (default ForErrorsAndWarnings) ↔ `SettingsStore.GetString("MonoDevelop.Ide.NewShowMessageBubbles")` (unset → ForErrorsAndWarnings, como el diálogo de Preferencias). Checked: None=hideAll, Errors=!hideAll&&mode=ForErrors, ErrorsAndWarnings=!hideAll&&mode=ForErrorsAndWarnings. Run: None alterna el bool; los hijos setean modo + hideAll=false (patrón `DataItem=Action` legacy). El puerto aplica el estado al editor activo y refresca los check glyphs (como el hook legacy `IdePreferences.Changed`).

**Validación (valores medidos)**: gate `Main.sln` **0 errores** (incluye la ruta GTK legacy `--old-gui`, que compila el handler original) + shell net10.0 **0 errores** + `MonoDevelop.Ide.csproj` legacy **0 errores**; tests **103/103**; **catálogo `475 commands, 113 with resolvable default handler`** (era 111; el +2 es intencional: HideIssues + ToggleIssues resuelven su handler); `--menudump` **11/288/0** (inmutable — el menudump cuenta el modelo, no los hijos inline runtime) con recents reseteados **bindings=256**; `--menuruntime` fase initial: **View > Inline Messages** con `_None`/`Toggle Issues` (master, **vis=False** por diseño)/**`E_rrors` (vis=True)**/**`_Errors and Warnings` (vis=True)**/`Hide Current Message`, `hidden=24` (inmutable: los 2 hijos nuevos son visibles) y `total=300+N+2` (medido N=6 → total=308); **click→persistencia→check** verificado con un harness determinístico **21/21 PASS** (5/5 estados XML + check glyph en 4/4); oráculos `oleada3_cmdstates` **SLICEA_ALL_PASS** + `oleada3_sliceA_probe` **ALL_PASS**; E2E oleada4 slice1/D/E/F **ALL_PASS**; **0 excepciones** en las 3 fases.

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief+reporte en `~/opencode/round2_qa/`): **APROBADO, 0 hallazgos P1–P4**; 3 P5 no bloqueantes (`.gitignore` 4º archivo — incluido y documentado; captura X11 rota — el check se validó por AT-SPI; harness del dev no en disco — re-validado de forma independiente por el QA con 5/5 XML + 4/4 check). El subagente re-ejecutó la batería completa de forma autónoma con los valores exactos, verificó el foco por AT-SPI real (Inline Messages completo, master oculto), confirmó los 3 NuGet presentes (regresión nula) y 0 excepciones.

### Cierre

Commits pushados a `origin/agents/sub-agent-senior-dev-role-report`: (1) 3 ficheros de código + `.gitignore` + docs ×3. Sin cambios en submódulos (punteros en HEAD). La paridad de menú GTK↔Avalonia queda **completa**: el único faltante real de la comparación (Inline Messages) está restaurado; los 3 NuGet citados ya se renderizaban desde la Ronda 1. Pendiente (fuera de esta ronda): portar los handlers de los ítems "not wired" de la Ronda 1 (trabajo de paridad de comandos aparte) y las 16 divergencias de label (intencionales, documentadas).

## 2026-11-09 (26) — Ronda 3: paridad final de menú — ítem View>Layout/Pads + launcher CWD-relative

**Estado: CERRADO (QA APROBADO, 0 hallazgos P1–P4).** Pedido del operador (tercera vez, con lista concreta): *"Aún siguen sin verse varios elementos de los menús"* → (a) un ítem entre **View>Layout y View>Pads** (Pads es exclusivo de Avalonia, no falta Pads), (b) Project>Manage NuGet Packages..., (c) Project>Update NuGet Packages, (d) Project>Restore NuGet Packages.

**Diagnóstico (con verdad de campo, no con referencias generadas)**:
- Se abandonaron las referencias generadas defectuosas (`fix1_legacy_tree.txt` perdía el menú completo de Version Control y sub-contaba addins; el volcado v4 AT-SPI estaba obsoleto, pre-Ronda-2). Se construyó la referencia autoritativa directo del XML (`~/opencode/legacy_xml_ref.py`) y se hizo el diff determinista por id de comando (`~/opencode/missing_ids_r3.py`): el modelo `MenuService.cs` (253 ids) es **superconjunto** de los ids de comando del menú legacy XML (224); los 13 "faltantes" por id tienen explicación benigna (3 Mac-only + acciones directas/ids sintéticos/subs de array).
- **Verdad de campo decisiva**: volcado AT-SPI de los píxeles realmente renderizados (STATE_SHOWING) en el estado exacto del operador (build actual, **sin `--sln`** — la instancia del operador arrancaba vía `main/build/monodevelop.sh` sin solución, mientras todas mis verificaciones previas usaban `--sln`): (b)(c)(d) los 3 NuGet **SÍ se renderizan y son visibles** (Project y=580/606/632); (a) la zona View entre el header `Layout` (y=201) y `Pads` (y=245) **sí estaba vacía** (bug real, reproducible).
- **Causa (a)**: `View List`/`Layout List` son comandos `type="array"`; la rama `#else` (AVALONIA_SHELL) de `ViewCommands.cs` portaba solo `Run` y **no `Update(CommandArrayInfo)`** → arrays vacíos → los masters renderizan ocultos y la zona queda sin contenido. En GTK ese hueco lo llena `LayoutListHandler.Update` (layout "Solution"→"Code") + `ViewListHandler.Update` (lista de pads).
- **Causa (b)(c)(d)**: no era un bug del build actual. El único binario del sistema cuya vista coincide con la del operador (Pads=SÍ, 3×NuGet=NO) es el **staging viejo de oct-25** (`~/opencode/wine-setup/wt-head/main/build`, pre-Ronda-1). Mecanismo: `main/build/monodevelop.sh` lanzaba `~/.dotnet/dotnet MonoDevelop.AvaloniaShell.dll` con **ruta relativa y sin `cd`** → la DLL cargada dependía del **CWD de lanzamiento**, no del script; un staging viejo en otro worktree se cargaba en silencio.
- El volcado `--menuruntime` (proxy del modelo) mostraba `vis=True` en los 3 NuGet con y sin `--sln`; la contradicción con el operador se resolvió con el AT-SPI de píxeles reales + el análisis del binario (`strings -el`: el DLL de main/build SÍ contiene los 3 ids; el viejo NO).

**Cambios** (3 ficheros de código + 1 launcher de staging + docs ×3):
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Commands/ViewCommands.cs` (rama `#else`): **port de `LayoutListHandler.Update(CommandArrayInfo)`** — añade la entrada "Code" (dataItem `layout:Solution`, checked, description "Switch to layout 'Solution'"; el legacy renombraba "Solution"→"Code" por `NameMapping`); y `ViewListHandler` queda deliberadamente sin Update (array vacío documentado: en Avalonia el submenu **Pads**, exclusivo de la shell, cubre el rol de la lista de pads del legacy — duplicarla sería redundante).
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs`: el caso `OnMenuCommand` de `ViewCommands.LayoutList` usa `nodeContext` (el dataItem del hijo inlineado) y logea `[layout] selected layout <dataItem>`.
- `main/src/core/MonoDevelop.Startup.Avalonia/Views/MenuService.cs` (BuildView): el orden de los masters de array pasa al del legacy XML (`Layout List` **antes** de `View List`, `MainMenu.addin.xml` L117-119); ambos entre el header `Layout` y el separador que precede a `Pads`.
- `main/build/monodevelop.sh` (staging local, git-ignorado, no commiteado): `cd "$(dirname "$0")"` + `exec ~/.dotnet/dotnet MonoDevelop.AvaloniaShell.dll "$@"` — el launcher carga **siempre** la DLL de su propio directorio, eliminando la dependencia del CWD (el bug que hacía que el operador viera un build obsoleto).

**Validación (valores medidos)**:
- Gate `dotnet build main/Main.sln -c DebugLinux -m:1` → **0 Errores** (regla 18.4: la ruta `--old-gui` sigue compilando; `MonoDevelop.Ide.csproj` legacy **0 errores**); shell net10.0 **0 errores**.
- Tests `AvaloniaShell.Editor.Tests`: **103/103**.
- Catálogo: **`475 commands, 113 with resolvable default handler`** (inmutable — el port añade un override a un handler ya resuelto, no un handler nuevo).
- `--menudump`: **`menus=11 items=288 cmds=0`** (inmutable — el menudump cuenta el modelo; los hijos inline son runtime).
- **AT-SPI de píxeles reales, sin `--sln` (estado del operador)**: View = `Layout` → **`Code` (visible+enabled)** → `Pads` → Save/Delete Current Layout → Editor Columns → Inline Messages → Folding → Zoom → Full Screen (hueco eliminado); Project = todos sus ítems incluyendo **Manage/Update/Restore NuGet Packages** visibles. Click real sobre `Code` → log `[layout] selected layout layout:Solution`, app viva, 0 excepciones.
- **Launcher**: lanzando `monodevelop.sh` desde CWD=`$HOME`, `/proc/<pid>/maps` mapea la DLL de `.../main/build` del worktree (la correcta), no el staging viejo de oct-25.

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief+reporte en `~/opencode/ronda3_qa/`): **APROBADO, 0 hallazgos P1–P4**; 2 P5 no bloqueantes (el checked del layout se renderiza como glifo ✓ en el icono, no STATE_CHECKED — diseño Oleada 3, consistente con todos los items array; `bindings=260` en el borde superior del rango conocido 196–260, fluctuación documentada). El subagente re-ejecutó la batería completa de forma autónoma con los valores exactos (git status exacto 3 ficheros, builds 0 errores ×3, tests 103/103, AT-SPI real de View/Project sin --sln con "Code" visible + 3 NuGet visibles, dispatch de Code, menudump 11/288/0, catálogo 475/113, launcher desde $HOME cargando la DLL del worktree, legacy 0 errores) y verificó contra `MainMenu.addin.xml` L117-119 que el orden de masters implementado es el del legacy.

**Cierre**: commits pushados a `origin/agents/sub-agent-senior-dev-role-report` — 3 ficheros de código + docs ×3 (sin submódulos; punteros en HEAD). El launcher `main/build/monodevelop.sh` es staging local git-ignorado (no commiteable). La paridad de menú GTK↔Avalonia queda **completa**: el hueco View>Layout/Pads está poblado con "Code" (paridad exacta con GTK) y los 3 NuGet se confirman renderizados en el build actual (la ausencia que veía el operador era un binario viejo cargado por el launcher CWD-relative, corregido).

## 2026-11-09 (27) — Ronda 4 (cierre) + Ronda 5: posición NuGet exacta (GTK) + diálogo Avalonia de Manage NuGet Packages

**Solicitud del operador**: en la Ronda 4 el operador confirma que ya ve los elementos del menú Project (visibilidad resuelta de su lado; se le explicó el launcher CWD-relative de la Ronda 3). La Ronda 5 pide: (1) los 3 items NuGet van **más arriba**, en la posición exacta de la UI GTK (entre los separadores que están juntos, tras "Add Reference"); (2) "Manage NuGet Packages..." debe abrir un **diálogo Avalonia de NuGet** (antes: no-op silencioso — el id llegaba al switch de `OnMenuCommand` sin case).

**Causa (Ronda 4, documentada, no corregida — no pedida)**: `TitleBarRow.PointerPressed → BeginMoveDrag` en `MainWindow.axaml.cs` (~L246-252) **sin guard** (el `ToolbarRow`, ~L253-257, sí guarda con `IsToolbarInteractive`). Bajo mutter, `BeginMoveDrag` entra la ventana en move mode y roba el click → el menú no se abre; en Xvfb es no-op (por eso mis capturas "pintaban" bien). El flag `--openmenu=<menú>` (evidencia por píxeles determinista de la Ronda 4) viaja en este commit.

**Cambios** (5 ficheros; sin submódulos, sin addins GTK, sin Gtk en el código nuevo):
1. `Views/MenuService.cs` — `BuildProject()`: bloque de 3 NuGet movido de tras "Generate Makefiles..." a **tras "Add Reference" + `Sep()`** (Manage/Update/Restore, juntos). Autoridad: `MonoDevelop.PackageManagement.addin.xml` (inserta tras `AddReference`) + verificación por píxeles contra la UI GTK real.
2. `Views/ManagePackagesDialog.axaml.cs` (**NUEVO**) — diálogo rebuilt tras el legacy `ManagePackagesDialog` (PackageManagement.Gui) sobre la pila del shell: UI en código (patrón `AddReferenceDialog`), `DialogWindow` (chrome Win11), 720x560, "Manage NuGet Packages". Search + "Available packages" (repositorio offline del shell = `AddReferenceDialog.KnownPackages`, 16 paquetes, ahora `internal` compartido) + "Installed packages" (`<PackageReference>` del proyecto vía `ConfigurationService.LoadProjectModel`) + Add (entre paneles) / Remove (cabecera Installed) + "Packages: N" + OK/Cancel. Doble click añade/elimina. OK reescribe vía `ConfigurationService.SaveProjectReferences` (mismo writer de `AddReferenceDialog.Apply`). **Alcance por proyecto** (como `ManagePackagesInProjectHandler` legacy; la variante de solución necesita resolver online, que el shell no trae). API QA: `TryAddPackage`/`TryRemovePackage` + `AddedPackages`/`RemovedPackages`/`InstalledVersions`.
3. `Views/MainWindow.axaml.cs` — cases `OnMenuCommand` para los 3 ids `MonoDevelop.PackageManagement.Commands.*`: **Manage** abre el diálogo con la MISMA resolución de proyecto que AddReference (nodeContext `project:` → `ResolveSelectedProjectPath` → `ResolveActiveProject`) + Output de añadidos/eliminados al cerrar; **Update/Restore** → Output informativo (sin resolver de solución/restore en el shell). Flags QA `--managepkgdlg` (diálogo para inspección) y `--managepkgs` (round-trip).
4. `Views/AddReferenceDialog.axaml.cs` — `KnownPackages`: `static` → `internal static` (compartido; mismo contenido).
5. `Program.cs` — `QaDialogArg`: `--managepkgs`, `--managepkgdlg`, `--openmenu=`.

**Bug de rendering corregido en el desarrollo**: capturas iniciales del diálogo SIN los dos paneles (solo chrome + Search + "Packages: 0"), **idénticas por md5** entre builds sucesivos. Causa: los 5 hijos del grid `panes` tenían `Grid.SetRow/SetColumn` pero **nunca entraron en `panes.Children`** — Grid sin hijos pinta nada (la fila `*` del root sí reservaba el espacio oscuro). Metodología: la verificación previa "el DLL no trae el código" con `strings` plano era **inválida** (heap #US de .NET = UTF-16; `strings -el` sí ve los literales — el código estaba). Fix: 5 `panes.Children.Add` + `ToString()` en `PkgRow` (plantilla de items del ListBox) + margen del header Installed.

**Validación (valores medidos)**:
- Tarea 1 (píxeles): popup Project del shell (`--openmenu=Project`, Xvfb :99) = Add Reference → sep → Manage/Update/Restore NuGet (juntos) → sep → Code Analysis; **idéntico a la referencia GTK** (`~/opencode/r4_gtk_project_popup2.png`/`r4_gtk_top_zoom.png`, capturados sobre la UI legacy en :99).
- Tarea 2 (píxeles): render completo (16 disponibles, Installed vacío, "Packages: 0"); Add → "Packages: 1" → OK → csproj `<PackageReference Include="Newtonsoft.Json" />` + diálogo cerrado; reabrir → estado recargado (en Installed, ausente de Available); doble click remove → "Packages: 0" → OK → csproj **byte-idéntico al baseline** (md5 `350aacc7…`).
- Round-trip `--managepkgs` (fixture `~/opencode/r5_managepkgs/`): `refs=0 package=0` → `TryAddPackage(RestSharp) → True` → `csproj contains RestSharp: True` → `TryRemovePackage(RestSharp) → True` → `refs=0 package=0`.
- Gate: `Main.sln` **EXIT=0, 0 errores** (incluye `--old-gui`, regla 18.4) + shell net10.0 **0 Errores** (127 warnings preexistentes) + tests **103/103** + catálogo **475/113** + `--menudump` **11/288/0** (inmutable).

**QA §18.5**: Tester QA Senior, 1 ronda — **APROBADO, 0 hallazgos P1–P4**; 3 P5 informativos (orden Code Analysis Solution/Project vs GTK — no pedido, registrado; `bindings=264` fluctuación por recents; re-render del diálogo idéntico salvo 200 px en estado transitorio del botón OK). Re-ejecución autónoma: gate completo, ciclo del diálogo por píxeles con capturas propias, round-trip, regresión GTK (menú Project de la UI legacy sobre el binario nuevo), ids 1:1 `MenuService`↔`OnMenuCommand`, diff acotado a 5 ficheros, sin Gtk en el diálogo. Brief+reporte+evidencia en `~/opencode/ronda5_qa/`.

**Cierre**: commits pushados a `origin/agents/sub-agent-senior-dev-role-report` — 5 ficheros (4 M + 1 nuevo) + docs ×3. Sin submódulos. Queda pendiente: orden "Run Code Analysis on Project/Solution" (GTK va Project primero), Update/Restore como acciones reales, guard del `BeginMoveDrag` del TitleBarRow (hipótesis Ronda 4), dispatch de ítems "not wired" de la Ronda 1, 16 divergencias de label (intencionales).

## 2026-11-09 (28) — Ronda 6: 3 bugs `//BUG:` de la interfaz Avalonia (View List / Add Reference / Custom command list)

**Estado: CERRADO (QA APROBADO, 0 hallazgos P1–P4).** Pedido del operador: corregir los 3 bugs marcados con `//BUG:` en `MenuService.cs` (interfaz Avalonia). Directivas de la sesión: (1) **no mezclar** código GTK y Avalonia — se **porta** GTK→Avalonia; (2) el código GTK porteado **no se borra** (los handlers `#if !AVALONIA_SHELL` quedan intactos para la UI GTK).

**Causas raíz (confirmadas)**:
- **BUG 1 — "View List" (View) no se mostraba**: el array `ViewList` del port no-Gtk era **vacío** (el `#else` de `ViewCommands.cs` no portaba `Update(CommandArrayInfo)`); el rol de lista de pads lo cubría un submenú shell-only `_Pads`.
- **BUG 2 — "_Add Reference..." no abría el diálogo**: el pipeline de dispatch funcionaba, pero el gate `SelectedNodeIsProject` quedaba `false` porque la shell **no auto-seleccionaba** el nodo del pad solución al activar un documento (GTK sí: `ProjectSolutionPad.OnWindowChanged → SelectActiveFile`).
- **BUG 3 — "Custom command list" (Project) no se mostraba**: el array `CustomCommandList` del port no-Gtk era **vacío** (sin parse de `CustomCommands` del .csproj en el modelo).

**Cambios** (8 ficheros fuente; sin submódulos, sin artefactos de build): `MenuService.cs` (elimina `_Pads` de `BuildView`, quita las 3 `//BUG:`); `MainWindow.axaml.cs` (BUG 2 `AutoSelectSolutionNodeForActiveDocument` en `OnDocSelectionChanged`; BUG 3 `SelectedProjectCustomCommands` + `RunCustomCommand` ejecución real; case `ViewList` → `TogglePad`; flags QA `--autoselectqa`/`--runcustom=`/`--openmenu=<Top[,Sub]>`); `MenuBuilder.cs` (`RebuildArraySiblings` + `AppendArraySetChildren`: renderiza un `CommandInfoSet` como submenu recursivo); `Program.cs` (registro de `--runcustom=` y `--autoselectqa`); `ViewCommands.cs` (BUG 1: `ViewListHandler` no-Gtk `Update` rellena Pads/Debug Pads — rama `#else`, GTK intacto); `ProjectCommands.cs` (BUG 3: `CustomCommandListHandler` no-Gtk `Update`); `ShellCommandState.cs` (puentes `IsPadVisible` + `SelectedProjectCustomCommands`, `#if AVALONIA_SHELL`); `ConfigurationService.cs` (compartido GTK/shell, fuera de `#if`: `CustomCommandEntry` + `ProjectFileModel.CustomCommands` + `ParseCustomCommands`).

**Decisiones clave**:
- **BUG 1 — contenido (decisión del operador)**: paridad GTK + quitar `_Pads`. Grupos **Pads** (12 pads: Solution, Classes, Help | Toolbox, Properties, Document Outline, Unit Tests | Output, Errors, Tasks, Code Issues, Search Results) y **Debug Pads** (Call Stack, Locals, Watch, Breakpoints, Threads), con separadoras, checks por visibilidad real, inventario idéntico al antiguo `_Pads` pero como **port Avalonia** (no un Pad GTK).
- **BUG 3 — forma del XML en disco (confirmada trazando la serialización legacy)**: el writer (`CollectionDataType.OnSerialize`) anida un grupo `CustomCommands` cuyo **nombre de elemento es el mismo que el de los items** (ambos `CustomCommands`, fijado por el test `MSBuildXmlNamespaceNotAddedToCustomCommand`); el wrapper es contenedor puro (sin campo `Command`), los items portan `type`/`name`/`command`/`workingdir`/`externalconsole`/`pauseexternalconsole` (lowercase) + `EnvironmentVariables/Variable name/value`. El parser **tolerante** filtra los `CustomCommands` descendientes que **portan campo Command** (excluye el wrapper, sin duplicados), lee campo de atributo→elemento (case-insensitive) y descarta `Type != Custom`.
- **BUG 3 — ejecución (paridad legacy `CustomCommand`)**: `CustomCommand.ParseCommand` (token inicial entrecomillado = exe), sustitución `${ProjectDir}`/`${ProjectName}`, working dir por defecto = dir del proyecto, exe no enraizado se resuelve contra el dir del proyecto, env vars declaradas aplicadas. **Proceso propio** (no el `runningProc` de build/run) → no salta gates de Stop; salida al Output pad.

**Validación (valores medidos)**:
- Gate `dotnet msbuild Main.sln -p:Configuration=Debug -m:1 -t:Build` → **EXIT=0** (regla 18.4: GTK + shell compilan); shell net10.0 **EXIT=0**.
- Tests `AvaloniaShell.Editor.Tests`: **103/103**.
- Catálogo: **`475 commands, 113 with resolvable default handler`** (inmutable).
- `--menudump` (recents reseteados): **`items=270 bindings=239`**. Baseline anterior 288/256 → delta **−18 items / −17 bindings** = exactamente el bloque `_Pads` eliminado (17 `Item` `pad:<id>` + 1 `Sub` sin id). Los 17 toggles no desaparecen: ahora viven en el **array runtime** (menudump no lo cuenta, `--menuruntime` sí). **No es regresión.**
- **BUG 1**: `--menuruntime` fase initial → `Pads` (12, orden + 2 separadoras) + `Debug Pads` (5) con `pad:<id>`; `_Pads`=0; píxeles `--openmenu=View,Pads` (checks reales Solution/Properties/Output).
- **BUG 2**: `--autoselectqa` BASE `disabled` → INSIDE auto-selección → `addref=ENABLED` → OUTSIDE fallback contenedor → `ENABLED`; `--cmdstates` `editRefs=1` (node_project/node_file); `--dispatchaddref` con selección `itemEnabled=True` + ventana "Add Reference" (X11/xwininfo, 620x600 capturada); sin selección `itemEnabled=False`.
- **BUG 3**: `--runcustom="R6 QA Marker"` ejecuta `touch "${ProjectDir}/r6_marker.txt"` (marker en disco, exit 0); `"R6 QA Env"` env llega al hijo (`printenv R6QAVAR` → `hello-r6`); `--projmodel` **15 ok / 0 fail** (4 aserciones customcommands); píxeles `--openmenu=Project` (2 comandos inline, Build filtrado).

**QA §18.5** (subagente Tester QA Senior, 1 ronda; brief `~/opencode/r6_bugfix/qa_brief.md` + reporte `~/opencode/r6_bugfix/qa_report.md`, artefactos `~/opencode/r6_bugfix/qa_tester/`): **APROBADO, 0 hallazgos P1–P4**; 3 P5 informativos (sin acción): `marker exists=False` cosmético en `--runcustom` para el comando env; ~7075 warnings preexistentes del gate; known issue preexistente del runtime legacy (no abre ventana — se valida por compilación). Re-ejecución autónoma con los valores exactos (270/239, 475/113, 103/103, 15/0, delta −18/−17, `hello-r6`, marker, `editRefs=1`, `itemEnabled=True/False`).

**Cierre**: commit por nombre de los 8 ficheros fuente + docs ×3 (`migration-status-report.md`, `interfaz-plan.md` y esta entrada), push a `origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos. Flags QA permanentes añadidos (se quedan): `--autoselectqa`, `--runcustom=<nombre>`, cadena de submenús en `--openmenu=`. Pendientes de rondas futuras (ya documentados, no pedidos): orden Run Code Analysis Sol/Proj vs GTK; Update/Restore NuGet como acciones reales; guard de `BeginMoveDrag` en `TitleBarRow`; dispatch de ítems "not wired" Ronda 1; 16 divergencias de label intencionales.

## 2026-11-09 (29) — Follow-up Ronda 6: "Project > Add Reference..." habilitado con solución abierta (sin selección de pad)

**Estado: CERRADO (QA APROBADO, 0 hallazgos P1–P4).** Pedido del operador: el elemento `Project > Add Reference...` se mostraba deshabilitado cuando hay una solución abierta y debe mostrarse habilitado.

**Causa raíz**: el gate (`EditReferencesHandler.Update`, bloque `#else` AVALONIA_SHELL de `ProjectCommands.cs`) era solo `SelectedNodeIsProject` (fila de proyecto seleccionada en el pad). La shell no auto-selecciona el nodo al cargar la solución, así que con la solución abierta y sin clic previo el gate quedaba `false`; la ejecución del comando, en cambio, resuelve la selección del pad con fallback al primer proyecto de la solución (`ResolveActiveProject()`). Gate y ejecución inconsistentes.

**Fix** (1 hunk en `ProjectCommands.cs`, solo el bloque `#else` — el Gtk legacy queda intacto): `info.Enabled = HasWorkspace && (SelectedNodeIsProject || ActiveProjectCount > 0)`, `info.Bypass = !Enabled`. El `ActiveProjectCount > 0` es el espejo del fallback de ejecución a `ResolveActiveProject()`. Al cargar la solución y en cada cambio de selección del pad se re-ejecuta el `Update`.

**Validación** (QA §18.5, 1 ronda — **APROBADO, 0 hallazgos**): gate `Main.sln` → 0 Errores; escenario del bug (`--sln=QaProj.sln --dispatchaddref` + `DISPATCH_NOSSEL=1`) → `itemEnabled=True` + ventana "Add Reference" abierta (X11); sin solución (`--cmdstates`) → `editRefs=0` en los 37 estados; con solución → `editRefs=1` en los 40 estados (sin regresión en los ítems vecinos); 0 fatals. Nota: `DISPATCH_NOSSEL` exige que `launch_shell.sh` lo reenvíe tras el `env -i` (se añadió la línea al launcher).

**Cierre**: commit por nombre del 1 fichero fuente + docs ×3, push a `origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos.

## 2026-11-10 (30) — Add-in Manager Avalonia: Gallery sin add-ins locales + integración con Visual Studio Marketplace (importar .vsix de VS desktop)

**Estado: CERRADO (QA APROBADO, 0 hallazgos tras 2 rondas).** Pedido del operador: (1) la pestaña **Gallery** del Add-in Manager de la shell Avalonia **no muestre** add-ins locales no instalados/habilitados (en Linux quedaban las plataformas `os: macos`/`os:windows`); (2) integrar la Gallery con el **Visual Studio Marketplace** para instalar extensiones de Visual Studio **desktop** (no VS Code) como add-ins que se integren con la shell. Alcance elegido por el operador: **"Import VSIX + extension points"**; límite aceptado: la UI compilada propia de un SDK de VS no aparece "mágicamente" — la integración cubre lo que el modelo de add-ins de la shell soporta (Pads, Menús, Diálogos, Preferences, Editor) y las DLLs del SDK de VS quedan en estado de error visible, sin crash.

**Fase 1 (causa raíz + fix)**: la rama gallery del diálogo (`LoadGallery`, eliminada — 0 restos en el árbol) se nutría de `host.Addons` (add-ins **locales** shipped+user) y listaba los no instalados/habilitados. La rama gallery ya no toca `host.Addons`; los no habilitados siguen en Installed (grises, botón Enable).

**Fase 2 (nuevo módulo `MonoDevelop.Ide/MonoDevelop.Avalonia.Addons/Marketplace/` + cableado del diálogo)** (4 ficheros nuevos + 2 modificados; sin submódulos):
- `MarketplaceClient.cs` (278 l) — API `extensionquery`: listado/búsqueda (filterType 1) + detalle (filterType 7) + descarga del paquete. **Quirks de la API verificados en vivo (2026-11-09)**: no existe target desktop genérico (`"Microsoft.VisualStudio"`, filterType 8, devuelve 0) — las ext. desktop declaran target de **edición** (`Microsoft.VisualStudio.Community` ~1000, `.Professional` 86, `.Enterprise` ~1000, `.Premium` ~1000; VS Code = `Microsoft.VisualStudio.Code`) y varios filter groups en una petición **no** se hacen union (solo se honra el primero) → `SearchAsync` hace fan-out paralelo (`Task.WhenAll`) a las 4 ediciones + merge + dedupe por `ExtensionId` (OrdinalIgnoreCase) + orden por installs desc; Accept `application/json` + `api-version=3.0-preview.1` añadido como `NameValueHeaderValue` (el ctor de `MediaTypeWithQualityHeaderValue` no lo parsea; sin él → 400); `statistics[].value` es float en el wire (POCO `double`, cast a `long`) y la estadística se llama **`install`**; el paquete descargable de VS desktop es un asset cuyo `assetType` es el **nombre del .vsix** (VS Code usa `Microsoft.VisualStudio.Services.VSIXPackage`; detección `EndsWith("VSIXPackage") || EndsWith(".vsix")`); `Microsoft.VisualStudio.Ide.Payload` **puede ser un launcher nativo Win32 (MZ), no un zip** (ReSharper) → no se trata como paquete; la query pide `"Version","Files"` en `assets[]` o `versions[].files[]` llega vacío (sin URL de paquete).
- `MarketplaceModels.cs` (157 l) — `MarketplaceExtension` + POCOs wire.
- `VsixManifest.cs` (109 l) — parser XML tolerante de `extension.vsixmanifest` + `IsVisualStudioDesktop` (por prefijo de target, excluyendo `.Code`).
- `VsixImporter.cs` (305 l) — `.vsix` → addin: extrae a temporal, parsea el vsixmanifest, genera el manifiesto `*.avaloniaaddon.json` de la shell (identidad/metadata rica; entry point solo si se detecta con confianza) y lo instala en `<userRoot>/<Identity.Id>` vía `host.InstallFromDirectory` — **quirúrgico**: registro + `LoadAddon` del nuevo addin, **no** `Discover()`/`LoadAll()` completo (`AddonHost.cs` quedó **sin cambios**, verificado en ambas rondas de QA). `FindEntryPoint` determinista (DLL con nombre == id del paquete; si no, primera por orden alfabético; no depende del orden de `EnumerateFiles`). `Import(string vsixFile, string? listingId = null)` (parámetro opcional, compatible con la firma de 1 argumento).
- `AddonManifest.cs` (M) — campo `Marketplace` (`AddonMarketplaceSource` con `ListingId`/`PackageId`/`GalleryUrl`/`Publisher`/`IsVisualStudioDesktop`) + fix de `FileExtensionsJsonConverter` (su `Write` lanzaba `NotSupportedException` — solo lectura — y rompía la serialización del manifiesto generado; ahora escribe array de strings (vacío si vacío) y `Read` gestiona token null).
- `AddinManagerDialog.axaml.cs` (M) — fase 1 + cableado: cliente/importador creados en el constructor (UI thread); búsqueda con debounce 450 ms en thread pool (`Task.Delay` + `CancellationTokenSource` + contador de generación: una respuesta obsoleta no pinta sobre una nueva); render marshalizado con `Avalonia.Threading.Dispatcher.UIThread.Post(BuildGalleryTree)`; dedupe de instalados; botón Install/Update → `InstallSelectedAsync` → `InstallFromMarketplaceAsync` (guarda de re-entrada `installingFromMarketplace`); limpieza en `OnClosing` (cancela CTS + Dispose del cliente); botón Refresh relanza la búsqueda.

**Dedupe (diseño clave, 3 espacios de ids)**: el id de la fila de Gallery es el id del **listing** (`publisher.name`, p. ej. `VisualStudioClient.MicrosoftVisualStudio2017InstallerProjects`), un espacio de nombres **distinto** del `Identity Id` del vsixmanifest con que se instala el add-in (p. ej. `VSInstallerProjects`) y del `PackageId` (`publisher.vsixId`). Comparar solo `Identity.Id` contra el `ExtensionId` de la fila **nunca** coincidía (la extensión instalada seguía apareciendo en Gallery — hallazgo MAYOR de la ronda 1 de QA). Fix: el diálogo pasa `ext.ExtensionId` al `Import`; el importador lo persiste en `AddonMarketplaceSource.ListingId` (JSON `listingId`); `BuildGalleryTree` recoge en `installedIds` tanto `Identity.Id` como `Marketplace?.ListingId` de cada add-in instalado y filtra la fila por `r.ExtensionId` (sin sobre-filtro: una extensión **no** instalada sigue apareciendo).

**Manejo de errores**: `MarketplaceClient` envuelve en `MarketplaceException` solo el HTTP no exitoso; los fallos de transporte salen como `HttpRequestException` (hallazgo MAYOR de la ronda 1: escapaban de los `catch (MarketplaceException)` → header atascado en "Installing…"/"Searching…" + error silenciado). Fix: el diálogo hace `catch (Exception)` en búsqueda e instalación (con `catch (OperationCanceledException)` **antes** en la búsqueda); el header siempre llega a estado final ("No Visual Studio extensions found" / "Install failed: …"); un import fallido (`!result.Success`) también fija "Install failed: …".

**Validación (valores medidos)**: probe E2E contra el Marketplace en vivo (`~/opencode/mkt_probe/`, `dotnet run`) → **ALL CHECKS PASSED 15/15** (listado desktop 20 exts, sin ids `.vscode`, ordenado por installs; búsqueda `roslyn`; target VS Code; detalle filterType 7 con URL; descarga real 5.385.730 bytes; import en `AddonHost` temporal: instalado + descubierto + manifiesto en disco + `isDesktop=True` + `entrypoint=True`; **degradación elegante** `loaded=False` con error visible `Assembly 'Microsoft.VisualStudio.DeployWizard' not found` y **sin crash**; re-import idempotente; determinismo en 2ª ejecución). Smoke funcional GUI (capturas `~/opencode/smoke_gallery_*.png`, flag QA existente `--addins`): Gallery "40 Visual Studio extension(s)", única categoría "Available", **sin add-ins locales**; búsqueda debounced "roslyn" re-renderiza resultados reales; detalle de fila (nombre/versión/autor/descripción + Install + "More information"); **instalación E2E de `SemihOkur.AsyncFixer`** (manifiesto con datos de marketplace, add-in en Installed con icono propio + botones Disable/Uninstall, no degradado); **uninstall E2E** (diálogo de confirmación, carpeta borrada de disco, log `uninstalled ... (user add-in removed)`); `UserAddIns` quedó vacío tras el uninstall. Falso alarma documentado: el `tab=installed items=0` tras el import **no era bug del host** — la caja de filtro (compartida entre pestañas, como en legacy) seguía con "roslyn" y `ApplyFilter` descartaba los 32 locales; con filtro limpio, `items=33` (32 + importado). Gate `dotnet build Main.sln -c DebugLinux` → **0 errores** (warnings preexistentes; los del diálogo restante son preexistentes: CS8632 sin contexto `#nullable`, CS0414 `updatingDetails`, VSTHRD100 `OnInstallFromFileClicked`, AVLN5001 `TextBox.Watermark` en el .axaml).

**QA §18.5** (subagente Tester QA Senior, 2 rondas): ronda 1 **RECHAZADO** (2 MAYOR + 1 MENOR) — (H1) el dedupe nunca coincidía (ids de espacios distintos, ver arriba), (H2) los `HttpRequestException` escapaban de los `catch (MarketplaceException)` y dejaban el header atascado + error silenciado, (H3) warnings nuevos MD0009 (catch vacío) / VSTHRD200 (método async sin sufijo). Corregidos (fix de `ListingId`, `catch (Exception)` con `OperationCanceledException` antes, `File.Delete` del temporal solo si `result.Success` y con log, `InstallSelected`→`InstallSelectedAsync`, `InstallFromMarketplace`→`InstallFromMarketplaceAsync`) → ronda 2 **APROBADO, 0 hallazgos** con evidencias: `dedupe_probe` 4/4 (manifiesto en disco con `listingId`), `mkt_probe` 15/15, `mkt_ex_probe` (comportamiento del cliente ante fallos de transporte), build `Main.sln -c DebugLinux` 0 errores, `AddonHost.cs` limpio (sin cambios), regresiones verificadas (sin sobre-filtro, `Import` de 1 argumento intacto, OCE no tragado). Nota aprobada por QA: un `.vsix` de import fallido puede dejar el temporal en `/tmp` (no se borra en el camino de error; lo limpia el SO, sin impacto funcional).

**Cierre**: commit por nombre de los 6 ficheros (4 nuevos de `Marketplace/` + `AddonManifest.cs` + `AddinManagerDialog.axaml.cs`; `AddonHost.cs` no entra) + docs ×3, push a `origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos. Artefactos de prueba en `~/opencode/` (probes `mkt_probe/`, `dedupe_probe/`, `mkt_ex_probe/`, capturas `smoke_gallery_*.png`, fixtures `.vsix` reales descargadas).

## 2026-11-10 (31) — Port F# completo (fases 1–6): binding F# GTK → shell Avalonia (nuevas soluciones, build/run, pad FSI, language service FCS, preferences + adornos)

**Estado: CERRADO (QA APROBADO, ciclo #4 VERDE 0 hallazgos).** Pedido del operador: portar la superficie F# completa del binding GTK (`main/external/fsharpbinding/`) a la shell Avalonia — nuevas soluciones/archivos F#, build/run de `.fsproj`, pad F# Interactive, language service F# completo (completions, hover, navegación, `.fsx`), preferences F# + adornos. Alcance fase 6 decidido por el operador: **completo — consumidores en vivo** (4 opciones persistidas + `AdvanceToNextLine` + `HighlightMutables` + `ShowTypeSignatures`).

**Fases (6, todas validadas)**:
- **Fase 1 (New Solution + New File)**: plantillas F# Console y F# Tutorial (port del `xpt` legacy → `Assets/Tutorial.fs`, 976 l) en `NewSolutionDialog`; plantillas `.fs`/`.fsi`/`.fsx`/`.fsscript` en `NewFileDialog`; scaffolding de `.fsproj` (SDK-style, `net10.0`, `FSharp.Core` implícito).
- **Fase 2 (build/run `.fsproj`)**: `ConfigurationService` reconoce proyectos `.fsproj` (mapeo solución→proyecto, TFM, output); build vía `dotnet build` y run vía `dotnet run` (el modelo de configuraciones ya lo orquestaba; se extendió el reconocimiento de extensión).
- **Fase 3 (pad F# Interactive)**: `FsiProcess.cs` (262 l) — proceso hijo `dotnet fsi --nologo` que habla el protocolo de prompts FSI (stdin/stdout/stderr, diagnóstico FS, restart, clear). `FSharpInteractivePadView.axaml.cs` (382 l) — pad (transcript monospace + input + toolbar Save/Open/Clear/Reset); sesión arranca lazy al primer show, muere al cerrar la tab o al salir.
- **Fase 4 (FCS in-process)**: `FSharpLanguageService.cs` (776 l) — `FSharpChecker.Create` (FCS **43.12.403**, la era .NET 10; el binding legacy usaba FCS 31.0.0 con otra API surface) singleton + executor single-thread; completions (`GetDeclarationListInfo` con `lineText` real), hover (`GetSymbolUseAtLocation` sobre **usos**, no declaraciones), go-to-definition, diagnostics (`ParseAndCheckFileInProject` síncrono por archivo). Contratos FCS verificados por probe (`~/opencode/fcs_probe/`).
- **Fase 5 (navegación + retargeting)**: scanner F# propio en `SymbolIndexService` (+232 l) para outline/índice (no FCS — más barato y suficiente para navegación); `GoToDialog` incluye símbolos F#; retargeting rename/save-as: `UnwireFSharpDocument(tag, ed)` + `WireFSharpDocument` (re-suscribe al cambiar la path).
- **Fase 6 (Preferences + adornos + AdvanceToNextLine)**: página "F# Settings" en `PreferencesDialog` (`PanelFSharp` en .axaml, +23 l; lógica en .axaml.cs, +128 l) — 4 claves legacy `FSharpBinding.*` (`FscPath` null si useDefault, `AdvanceToNextLine` default true, `HighlightMutables`/`ShowTypeSignatures` default false) vía `SettingsStore` (mismo `MonoDevelopProperties.xml` del GTK). Adornos: `FSharpLanguageService.GetFileAdornments` (UI, `executor.Invoke`) + `internal ComputeAdornments(CheckResult)` (ruta del evento, sin re-entrada — `CheckCompleted` nace en el executor) → `GetAllUsesOfAllSymbolsInFile` → `IsFromDefinition` → `IsFunction`/`IsMutable`/`FullTypeSafe` por reflection + `FormatFSharpType`. Editor: `SetFSharpAdornments(items, showMutable, showType)` en `SkTextEditor` (+176 l) — mutable → subrayado 2px; type → dim ` : <type>` a la derecha. `AdvanceToNextLine`: guard en comando `SendLine` (sesión viva + flag → `SetCaret(CurrentLine+1, 0)`).

**Archivos** (4 nuevos + 16 modificados; sin submódulos; `fsharpbinding` NO es submódulo — trackeado en el repo principal):
- `Services/FSharpLanguageService.cs` (nuevo, 776 l) — FCS in-process + adornos.
- `Services/FsiProcess.cs` (nuevo, 262 l) — proceso hijo `dotnet fsi`.
- `Views/FSharpInteractivePadView.axaml.cs` (nuevo, 382 l) — pad F# Interactive (todo en code, sin .axaml).
- `Assets/Tutorial.fs` (nuevo, 976 l) — template F# Tutorial (Content, `CopyToOutputDirectory=PreserveNewest`).
- `MonoDevelop.Startup.Avalonia.csproj` (M, +16) — `FSharp.Compiler.Service 43.12.403` + `Tutorial.fs` como Content.
- `MainWindow.axaml.cs` (M, +929) — wiring F# (WireFSharpDocument, FsiQaAsync, FslsQaAsync, comandos FSI/FS, pad, dispatch `--fsi`/`--fsls`/`--prefs-fsharp`).
- `SkTextEditor.cs` (M, +176) — adornos F# (SetFSharpAdornments, DrawFSharpAdornments, props QA).
- `SymbolIndexService.cs` (M, +232) — scanner F# para outline/navegación.
- `ConfigurationService.cs` (M, +44) — reconocimiento `.fsproj`.
- `NewSolutionDialog.axaml(.cs)` (M, +32/+172) — plantillas F# Console + Tutorial.
- `NewFileDialog.axaml.cs` (M, +147) — plantillas F# de archivo.
- `PreferencesDialog.axaml(.cs)` (M, +23/+128) — PanelFSharp + Load/Store/Browse + QaFSharpRoundTrip.
- `Program.cs` (M, +20) — hooks QA `--fsi`, `--fsls`, `--prefs-fsharp`.
- `MenuBuilder.cs` (+18), `MenuService.cs` (+10), `ViewCommands.cs` (+3) — menús/comandos F#.
- `AddReferenceDialog.axaml.cs` (+6), `GoToDialog.axaml.cs` (+24) — F# en add-reference y go-to.

**Validación (valores medidos)**:
- **E2E `--fsls`** (canónico, `launch_shell.sh --sln=$HOME/opencode/fsharp_port/fsls_fs/FsLs.sln --fsls`): **14 aserciones True, 0 False** — `wired=True srcfiles=2`, `diagnostic-ok=True`, `bubble-ok=True`, `hover-ok=True`, `hover-assert=True`, `member-completion-ok=True`, `editor-routing-ok=True`, `goto-def-ok=True → Lib.fs:8:4`, `goto-def-assert=True`, `outline-assert=True`, `gototype-assert=True`, `classes-assert=True`, `fsls-mutable-ok=True` (L10 len7), `fsls-type-ok=True` (L11 `int -> int`), `fsls-adornments-ok=True` (editor-adornments=2 mutable=1 type=1). Framework refs: `.../10.0.12/ref/net10.0`.
- **E2E `--fsi`**: **9/9 True** — `ready=True running=True`, `result-ok=True`, `diagnostic-ok=True`, `restart-ready=True`, `restart-result-ok=True`, `clear-empty=True`, `advance-ok=True from=5 to=6 session=True`, `done transcript-lines=1`.
- **`--prefs-fsharp`**: `roundtrip-ok=True` + restore exacto (valores restaurados).
- **`--prefs=fsharp`**: `placeholder=False`, icono `md-prefs-source`, `[prefs-fsharp] loaded: advance=True mutables=False typesigs=False fsc=(default)`.
- **Regresión C#**: `Build succeeded.` + `[build] build done failed=False` + 0 líneas `[fsls] wired`.
- **GTK legacy**: `GTK VIVO (relay --old-gui OK)`.
- **Build**: `dotnet build ... -c DebugLinux -m:1` → **0 errores** (~2100 warnings preexistentes).

**QA §18.5** (subagente Tester QA Senior, 4 ciclos recursivos): ciclo #1 → 4 hallazgos (fuga `CheckCompleted` en close, freeze por debounce 1.5 s, regex `let` procedural, retargeting rename/save-as). Corregidos. Ciclo #2 → 1 hallazgo (fuga residual). Corregido. Ciclo #3 → **VERDE 0 hallazgos** (fases 1–5 cerradas). Ciclo #4 (fase 6) → **VERDE 0 hallazgos** con evidencias: 5 componentes verificados por lectura (sin CS0122, sin deadlock, flags cacheados, firma consistente) + 4 hooks E2E + 2 regresiones en verde + build 0 errores.

**Queda pendiente** (límites aceptados por el operador, no defectos): (a) `CodeGenerationPanel` per-proyecto (project properties, fuera de la página global de preferences — el binding legacy lo tenía en `FSharpOptionsPanels.fs` como panel de proyecto); (b) integración VSIX/Marketplace del binding F# (en pausa por decisión del operador).

**Cierre**: commit por nombre de los 20 ficheros (4 nuevos: `FSharpLanguageService.cs`, `FsiProcess.cs`, `FSharpInteractivePadView.axaml.cs`, `Assets/Tutorial.fs` + 16 M) + docs ×3, push a `origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos modificados. Artefactos de prueba en `~/opencode/` (fixture `fsharp_port/fsls_fs/`, probes `fcs_probe/`, `fs_scanner_probe/`, lanzadores `launch_shell.sh`, `oldgui_smoke.sh`, log `shell_qa.log`).

## 2026-11-11 (32) — M31: editor multi-lenguaje dinámico — pila legacy de coloreo + paletas VSCode (Fase 1)

**Estado: CERRADO (QA APROBADO, 0 hallazgos abiertos).** Pedido: transformar el editor de la shell Avalonia (`SkTextEditor`, coloreo hardcodeado C# con `Tokenize`/`GetThemePalette`) en un editor dinámico multi-lenguaje que **reutilice la pila de coloreo legacy** (`MonoDevelop.Ide.Editor.Highlighting`: RegexEngine + Sublime3/TextMate + `SyntaxHighlighting`/`EditorTheme`) con **paletas VSCode** (Dark+/Light+), como base de las fases 2-9 (intellisense, errores+quick fixes, extension points VSIX, minimap, scroll bloqueado, fondo oscuro, dropdown de pestañas ▼). Lenguajes v1: C#, F#, JSON, XML (`.csproj`/`.sln`), C, C++, Markdown, YAML.

**Arquitectura (regla 18.4)**:
- **Compilación integrada**: los ficheros de la pila se compilan *inline* en el dll de la shell (globs `<Compile Include="..\MonoDevelop.Ide\MonoDevelop.Ide.Editor.Highlighting\...">` en `MonoDevelop.Startup.Avalonia.csproj`). **Sin carpeta "Avalonia"**; convive en el árbol original de `MonoDevelop.Ide`.
- **Guards `#if !AVALONIA_SHELL`** (solo la shell define la constante) en: `SyntaxHighlightingService.cs`, `SemanticHighlighting.cs`, `TemplateCodon.cs`, `TemplateExtensionNodeLoader.cs`, `Formats/OldFormat.cs` (completo) y `TextMateFormat.cs`, `IEditorThemeProvider.cs`, `IReadonlyTextDocument.cs` (parcial). `HslColor.cs`, `StackMatchExpression.cs`, `EditorTheme.cs` **sin cambios** (compilan en los dos builds).
- **Shims CAS inertes** en `Controls/HighlightingCasShims.cs` bajo `#if AVALONIA_SHELL` (`SecurityCriticalAttribute`, `PermissionSetAttribute`, `ReflectionPermission`, `CodeAccessPermission`, `HostProtectionAttribute`).
- **Servicio portado** `Controls/SyntaxHighlightingService.Avalonia.cs` — a propósito en el namespace `MonoDevelop.Ide.Editor.Highlighting` (la `SyntaxHighlightingDefinition` legacy resuelve scope-include contra `SyntaxHighlightingService.AllBundles` sin cualificar). API mínima: `GetSyntaxHighlightingDefinition(FilePath)` (resolución por extensión, la más larga gana) + `GetEditorTheme(name)`.
- **8 grammars** en `MonoDevelop.Ide.Editor.Highlighting/Resources/` (`csharp`/`fsharp`/`markdown`/`yaml`.sublime-syntax + `c`/`cpp`/`xml`.json + `json.plist`), incrustados como `EmbeddedResource` con `LogicalName="MonoDevelop.AvaloniaShell.Highlighting.Resources.<fichero>"`, cargados con el `ResourceStreamProvider` legacy.

**Integración (`SkTextEditor`, región `Dynamic syntax highlighting`)**:
- Highlighter **recreado por generación**: `InvalidateHighlighter()` (descarta highlighter + cache) en `SetLines` (cada edición), `Commit` y setter `FilePath` (un open/rename puede cambiar el lenguaje). Cache `HighlightedLine` por línea.
- **Adapter** `Controls/EditorTextDocumentAdapter.cs` (`IReadonlyTextDocument`) — snapshot inmutable.
- **El tema se resuelve en cada paint** (`CurrentEditorTheme`): no se cachea con el resaltado → recolorido dark/light en vivo sin re-highlight.
- `ToSegments`: `ColoredSegment` (scope→`ColorStyleKey`) → `theme.TryGetColor(scope,"foreground")` → `HslColor.ToRgba()` → `SKColor`. `CurrentCommentColor` (adornos F#) y `CurrentThemeVariantName` (`Light+`/`Dark+`). `Tokenize`/`GetThemePalette` **borrados**; `Keywords` se conserva (iconos de completado/hover, no coloreo).
- **Paletas** `Controls/HighlightingThemePalette.cs`: Dark+/Light+ como datos puros → `EditorTheme` (modelo legacy `ThemeSetting`/`StackMatchExpression`).
- **Wiring**: `OpenFileDocument` asigna `FilePath` **antes** que `Text`.

**Fixes hallados por la QA (ciclo §18.5)**:
1. **`Microsoft.Extensions.ObjectPool` no resolvible** — crash del primer paint (`StringBuilderCache.Allocate`): `MonoDevelop.Core` declara el paquete con `PrivateAssets="runtime"` → el runtime asset **no fluye** al deps.json de la shell vía `ProjectReference` (aunque el dll físico sí llega por `IncludeCopyLocal` a `main/build`). La pintura dinámica es el primer código de la shell que toca `StringBuilderCache` (usado por `StackMatchExpression.Parse`). **Fix contenido en la shell**: `PackageReference Microsoft.Extensions.ObjectPool 3.0.0-preview9.19423.4` directo en el csproj de la shell (emite la entrada de runtime en deps.json). Un `<Reference>` con `HintPath` NO funciona (el compilador sí, el host no). `MonoDevelop.Core` no se toca (regla 18.4).
2. **Fondo del editor negro (bug preexistente, ejercitado por la QA)** — los editores de documento se montan con `Background = Brushes.Transparent` (`OpenFileDocument` l.6165, código base); `ToSk(Transparent)` devuelve `SKColor(0,0,0,0)` (no nulo) → el fallback `?? #1E1E1E` nunca se aplicaba y el fill transparente dejaba la superficie `#000000`. Invisible en dark (base-fg claro), crítico en light (base-fg `#1A1A1A` → texto plano negro sobre negro invisible; solo se veían tokens coloreados). **Fix en `Draw`**: si `Background` es nulo o `Alpha==0`, el fondo se pinta desde la paleta activa (`theme.TryGetColor("background")`; fallback por variante light `#FFFFFF` / dark `#1E1E1E`). Cubre además la requirement "fondo negro en tema oscuro".
3. **Recolorido live dark/light** (requirement: el tema NO está en la cache) — un cambio de tema muta los brushes vinculados *in place* (la re-resolución del `DynamicResource` cambia el `Color` de la misma instancia `SolidColorBrush`) → `ForegroundProperty` no cambia → no hay `AffectsRender` ni `OnPropertyChanged` del control → el compositor re-presenta el frame viejo (`renderCount 2 -> 2 NOT-REPAINTED`). Se probó (y **revertió**) una rama en `OnPropertyChanged` por propiedades de brush (no se dispara); `Control.ActualThemeVariant` en Avalonia 12 es una propiedad simple sin `AvaloniaProperty`. **Fix definitivo**: el handler existente de `Application.Current.ActualThemeVariantChanged` en `MainWindow` (que ya hacía `SetToolbarIcons`) llama además a `ed.InvalidateColors()` de cada editor en `docs`; `InvalidateColors()` (método público nuevo en `SkTextEditor`) → `MarkDirty()`.
- **Driver QA `--themetoggle[=<path>]`**: registro en `Program.QaDialogArg` + rama en la cadena QA de `MainWindow`; abre un fixture y alterna el tema por la **ruta real de producción** (`ApplyThemeVariant`, la misma del menú de tema) verificando `QaRenderCount`. Mismo estilo que `--outline`/`--editqa`.

**Validación (valores medidos)**:
- **Builds canónicos a 0 errores**: shell `dotnet build main/src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -c DebugLinux -m:1` (2198 warnings preexistentes) y GTK `dotnet build main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.csproj -c DebugLinux -m:1` (242 warnings). La shell **no** referencia `MonoDevelop.Ide.csproj` (la pila se compila inline); el proyecto GTK no define `AVALONIA_SHELL` (los guards lo excluyen).
- **Probe headless del glue** (`~/opencode/highlight_qa/headless/glue_probe`, sobre los DLLs reales de `main/build`): **42 aserciones `[OK]`, 0 `[FAIL]`** — 8 resoluciones por extensión + 2 negativos, paletas Dark+/Light+ (tolerancia ±2 LSB; el round-trip HSL del `HslColor` pierde 1-2 bits, idéntico al mundo GTK), fallback de tema desconocido, tiling completo y scopes esperados en los 6 fixtures. `GLUE PROBE VERDE`. Re-verificado sobre el build definitivo final.
- **QA visual 6 lenguajes × 2 temas** (12 capturas `shot_{cs,fs,json,xml,cpp,md}_{dark,light}.png` + `analyze_colors.py`): colores distintos de tinta — dark cs 566 / fs 634 / json 508 / xml 480 / cpp 525 / md 270; light cs 479 / fs 569 / json 505 / xml 346 / cpp 481 / md 139. Canónicos presentes (dark cs: keyword `#BE82B9`, string `#C68A72`, comment `#669352`; light cs: keyword `#9C00C4`, string `#921212`, comment `#007A00`). "Canónicos faltantes" en json/md/xml esperados (los fixtures no tienen esos tokens).
- **Fix de fondo verificado** (`fix_light_cs.png` / `fix_dark_cs.png`): light → fondo `#FFFFFF`, 12 líneas visibles (0.0% `#000000`); dark → fondo `#1E1E1E`, 0 px de `#000000` en la zona del editor.
- **Recolorido live en la misma sesión** (`--themetoggle`): `Dark -> Light renderCount 2 -> 3 RECLORED` + captura post-toggle con UI y editor completos en light (paleta Light+ sin reabrir el documento).
- **Arranque `--old-gui` verificado**: vía `dotnet <dll> --old-gui` el hijo es `dotnet main/build/MonoDevelop.dll` con `MONODEVELOP_LEGACY_UI=1`, carga ~287 MB RSS, crea ventana, vivo >120 s sin excepciones. Quirk preexistente: vía apphost, `Environment.ProcessPath` devuelve el apphost → el "hijo" re-ejecuta la shell Avalonia (no es regresión; el diff no toca `LaunchLegacyGtk`). Captura negra en Xvfb = **artefacto de entorno** (sin window manager: `twm`/openbox ausentes) → la evidencia de arrancada GTK es proceso vivo + RSS + `xwininfo -root -tree`, no píxeles.

**Decisiones de diseño (trade-offs)**:
1. **Invalidación por generación + cache por línea, sin invalidación incremental**: el adapter es un snapshot inmutable (no `ITextDocument` con cambios), cada edición recrea el highlighter y descarta la cache; el coste se amortigua con la cache `HighlightedLine` por línea (resaltado por línea independiente). Trade-off: documento grande se re-highlighta entero tras cada edición; a cambio, cero riesgo de desincronización. Paso natural futuro: adapter incremental.
2. **Sync-over-async en `Draw` es seguro**: `GetColoredSegments`/`GetHighlightedLineAsync` son 100% síncronos (`Task.FromResult`); el `GetAwaiter().GetResult()` no puede deadlock.
3. **Round-trip HSL de `HslColor` da ±1-2 LSB por canal** (modelo HSL: `Parse→ToRgba` pierde 1-2 bits); el mundo GTK hace el mismo round-trip (aserciones de QA con tolerancia ±2).
4. **El `PackageReference` de ObjectPool en el csproj de la shell es un fix de empaquetado contenido en la shell** (root cause en `MonoDevelop.Core`, no se toca — regla 18.4).
5. **`HslColor` ambiguo**: existe `Avalonia.Media.HslColor`; las referencias del glue están cualificadas como `MonoDevelop.Components.HslColor`.

**Archivos**:
- **Nuevos** (solo compilan en la shell; `Controls/` por el glob `Controls\**\*.cs`): `Controls/EditorTextDocumentAdapter.cs`, `Controls/HighlightingCasShims.cs` (bajo `#if AVALONIA_SHELL`), `Controls/HighlightingThemePalette.cs`, `Controls/SyntaxHighlightingService.Avalonia.cs`, `MonoDevelop.Ide.Editor.Highlighting/Resources/` (8 grammars).
- **Modificados**: `Controls/SkTextEditor.cs` (integración del highlighter dinámico, borrado de `Tokenize`/`GetThemePalette`, invalidación por generación, fix de fondo, `InvalidateColors()`); guards `#if !AVALONIA_SHELL` en `MonoDevelop.Ide.Editor.Highlighting/` (`SyntaxHighlightingService.cs`, `SemanticHighlighting.cs`, `TemplateCodon.cs`, `TemplateExtensionNodeLoader.cs`, `Formats/OldFormat.cs`, `TextMateFormat.cs`, `IEditorThemeProvider.cs`, `IReadonlyTextDocument.cs`); `MonoDevelop.Startup.Avalonia.csproj` (compile set de la pila, 8 `EmbeddedResource` con `LogicalName`, `Reference` Roslyn core, `PackageReference Microsoft.Extensions.ObjectPool`); `Program.cs` (flag QA `--themetoggle` en `QaDialogArg`); `Views/MainWindow.axaml.cs` (`InvalidateColors()` en `ActualThemeVariantChanged` + flag QA `--themetoggle`).
- **Sin cambios (deliberado)**: `HslColor.cs`, `StackMatchExpression.cs`, `EditorTheme.cs` (compilan en los dos builds); `MonoDevelop.Core` (regla 18.4).

**Queda pendiente** (fases 2-9, fuera de esta tarea): lenguajes adicionales del alcance original (`.properties` y `.txt` — el servicio ya resuelve por extensión); intellisense; errores + quick fixes; extension points VSIX (aplicables a toda la shell Avalonia); minimap siempre visible a la derecha (clic/arrastre mueve viewport; muestra errores y breakpoints); scroll bloqueado hasta que las líneas superan el alto del editor; dropdown de pestañas ▼ a la derecha de la barra de pestañas (comportamiento GTK).

**Cierre**: commit por nombre de los ficheros modificados + nuevos (`Controls/`, `Resources/`) + docs ×3, push a `origin/agents/sub-agent-senior-dev-role-report`. Sin submódulos modificados. Artefactos de prueba en `~/opencode/highlight_qa/` (fixtures, probe headless `headless/glue_probe`, `capture_all.sh`, `analyze_colors.py`, lanzadores `launch_and_shot.sh`/`test_oldgui.sh`/`test_gtk_direct.sh`/`test_gtk_final.sh`, 12 capturas + `fix_{light,dark}_cs.png` + `themetoggle2_after.png`, `MonoDevelopProperties.xml.bak`, logs) y `~/opencode/highlight_e2e/` (probe 1a de referencia).
