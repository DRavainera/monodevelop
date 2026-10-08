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
1. (Pendiente de aprobar) Commit bloque 1 "núcleo verde": SourceEditor2 2 archivos, CocoaExtras.cs + TextEditor.csproj, CSharpBinding.csproj, MDBuildTasks.targets, mdmonitor.csproj (Cairo), sln (podas), summary. Push a `origin`.
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

**Pendientes**: ~~coloreado de sintaxis~~ (FIX #6, abajo), ~~errores MEF `IDiagnosticService`+`ICodeFixService`~~ (FIX #8, abajo), NRE `HighlightingUsagesExtension` (FIX #6c, abajo), `MonoDevelop.MSBuildBuilder.exe` ausente, tema murrine.

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

**Pendientes**: errores MEF `IDiagnosticService`+`ICodeFixService` (ResultsEditorExtension / CodeActionEditorExtension) → resuelto luego (FIX #8), `MonoDevelop.MSBuildBuilder.exe` ausente, tema murrine.

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

**Pendientes**: NRE `QuickInfoProvider.GetQuickInfoAsync` (hover), `MonoDevelop.MSBuildBuilder.exe` ausente, tema murrine, evaluación de cargar proyecto (MSBuild SDK resolver).

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

**Pendientes**: `MonoDevelop.MSBuildBuilder.exe` ausente, tema murrine, evaluación de cargar proyecto (MSBuild SDK resolver), integración real de quick-fixes (el shim inerte sigue; el `CodeFixService` real exige unificar identidad de tipos: `IThreadingContext`, `IEditorOptionsFactoryService`, ...).

## 2026-09-17 — Migración del tooling: /tmp/opencode → ~/opencode

`/tmp` iba a ser borrado: se copió **todo** el toolkit (1,1 GB, 5.803 archivos) a **`~/opencode/`** (verificado con `diff -rq`), se reescribieron las rutas en scripts/csproj/obj y se añadió `~/opencode/README.md` con el contexto para agentes futuros (mdrefs45, runmd_ef.sh, hover.py, arf, ctprobe). Builds standalone: usar `-p:MonoFrameworkDirectory=~/opencode/mdrefs45`.

## 2026-09-17 — Fix #10b: unificación de identidad de tipos en la composición MEF

**Diagnóstico** (con escáner de metadatos `~/opencode/tscan`): `MonoRoslynCompat.dll` **declara** tipos con los mismos FullNames que los tipos internos reales de EditorFeatures (p.ej. `Microsoft.CodeAnalysis.Editor.Shared.Utilities.IThreadingContext`). Con EditorFeatures stageado, el part real `ThreadingContext` y el export del fork `MonoDevelopThreadingContext` caen en el mismo contrato MEF (importaciones ambiguas, "found 2": 37 en el run anterior). El type-forwarding no es opción: los tipos reales son **internos**.

**Fix** (patrón del fork: drop del part real, export único del fork):
- `CompositionManager.cs`: `partsToDrop` += `Microsoft.CodeAnalysis.Editor.Shared.Utilities.ThreadingContext` (real) y `Microsoft.CodeAnalysis.CodeFixes.CodeFixService` (real; el inerte `InertCodeFixService` queda como export único).
- `CompositionManager.Caching.cs`: `CacheVersion` 4→5.

**Verificación (run 02:44, pid 612419)**: documento abierto, hovers OK, **0 "found 2"** (antes 37), 0 "Expected 1 export", 0 errores de creación de extensiones, 0 FATAL, 0 NRE; coloreado de sintaxis intacto (captura `~/opencode/unify_ef_final.png`: 3.201 colores). Los 92 "MEF composition error" restantes son **latentes** (sin excepción en runtime): parts reales de EditorFeatures que importan `IThreadingContext` (identidad del stub ≠ real) o servicios aún sin export (`IWorkspaceDiagnosticAnalyzerService`-family, `IPreviewService`). Desaparecerán al migrar los consumidores del fork al contrato real o al stagear las exports host que faltan.

**Pendientes**: `MonoDevelop.MSBuildBuilder.exe` ausente, tema murrine, migración profunda de identidad de tipos (consumidores fork → tipos reales de EditorFeatures) para quick fixes/diagnostics reales.

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
  pad-realized=2 → cleanup. Captura `docs/img/locals-pad.png`.
- Attach to Process REHECHO como tab del pad inferior
  (`AttachToProcessPanel`, UserControl): /proc real (603 procesos, skip
  kernels/self), filtro + Refresh + Attach. NUNCA ventanas con borde de OS —
  chrome Avalonia. QA `--attachdlg` verde (pad-realized=1,
  window-chrome=Avalonia). Captura `docs/img/attach-to-process-pad.png`.
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
  `answer + 1 = 43`, remove → rows=1. Captura `docs/img/watch-pad.png`;
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
  **Re-validado desde un arranque limpio** (capturas `padclose_verified.png` y
  `padclose_verified2.png`): el ✕ funciona, no hay bug de hit-test. El intento
  intermedio que falló fue porque el pad derecho ya estaba **colapsado** por un
  click de control sobre el chevron "Collapse pad": con el host colapsado el ✕
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
- **Restauración**: los PNG de `docs/img/` (attach-to-process-pad,
  breakpoints-pad, locals-pad, watch-pad) estaban borrados en el working tree
  por los QA de capturas; restaurados con `git checkout -- docs/img/`.
- **M26 propuesto** (pendiente de aprobación): paridad de los 6 pads
  placeholder, con recomendación de empezar por `documentoutline`, `classes` y
  `codeissues` (reutilizan el índice de símbolos y el parser de diagnósticos ya
  existentes). Detalle en `docs/interfaz-plan.md` § M26.
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

**Cierre**: commit de la Fase 1a (referencia a Core). Fase 1b pendiente de la
decisión (a/b/c).

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
como en el addin.xml. QA: 0 errores, 53/53, `path=/home/daniel/.dotnet/dotnet`,
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
- Completar MIME/iconos en los hooks de plataforma más allá del esqueleto.

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

**Pendiente de la Fase 4:** oleadas C–E y la decisión de diseño de la migración GnomePlatform→LinuxPlatform.

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

**Pendiente restante:** el enganche runtime GUI de la plataforma (puente entre el registro del host y `AddinManager`/`DesktopService`) es deuda de hooks compartida por las 3 plataformas (ver `docs/fase3-platform-hooks-cleanup.md`); MIME/iconos más allá del esqueleto.
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

**Contexto**: el Add-in Manager de la UI Avalonia (`AddinManagerDialog`, Tools > Extensions… / `--addins` / `--extensionsdlg`) se alimentaba del registry Mono.Addins (`SetupService` + `AddinEngineHost`). El escaneo de ese registry fallaba **íntegro** en el build unificado: `Mono.Addins.CecilReflector` (submódulo) compila contra Mono.Cecil 0.9.6 (`InterfaceImplementation`, `ModuleDefinition.FileName`) mientras el build estagia Cecil 0.10.1 → `MissingMethodException` al escanear cada ensamblaje → "The add-in database could not be updated" → **pestaña Installed vacía (items=0)**. El diálogo se desacopló de los componentes legacy: sus 3 pestañas (Installed, Updates, Gallery) conservan la UI original y se alimentan del nuevo sistema de add-ins Avalonia (`MonoDevelop.Avalonia.Addons` / `AddonHost` + manifiestos `*.avaloniaaddon.json`); la Installed muestra además el icono de cada add-in.

**Cambios** (38 archivos):

1. **`MonoDevelop.Avalonia.Addons`** (componente nuevo): `AddonIdentity.Icon` (campo `identity.icon` del manifiesto); `AddonLoadState.ManifestPath`; `AddonHost.ResolveIconFile` — resuelve `core:file.png` contra el set de iconos core legacy y cualquier otra ruta contra la raíz de add-ins (o la propia carpeta del add-in), con walk-up desde el directorio del binario como `IconService` (resuelve en el árbol del repo y en un build estagiado).
2. **`AddinManagerDialog` sobre el host nuevo, UI idéntica a la original**:
   - **Installed**: los 35 add-ins descubiertos por `App.Addins`, agrupados por categoría (como `ShowCategories` legacy), filtro de búsqueda, **icono por add-in** (`identity.icon` resuelto por el host; fallback `plugin-32.png` embebido, como `StoreIcon` legacy), filas no cargadas en gris (como los disabled legacy) y panel de detalle (versión/autor, estado, descripción).
   - **Gallery**: el catálogo de add-ins que trae el build (los 35): los cargados se ven "installed" (plugin-32) y los no cargados "available" (plugin-avail-32); RepoCombo y Refresh como el original (el catálogo del host es local; el Refresh relee el estado del host).
   - **Updates**: versión más nueva del catálogo frente a la cargada (el host estagia un manifiesto por add-in → hoy "No updates found"); header "N updates available" y Update All como el original.
   - Los botones de acción (Install/Update/Disable/Enable/Uninstall/Install from file) conservan la misma lógica de visibilidad original; sus handlers loguean `[addins] ... not available in the new add-in host` (el host aún no expone esas APIs — deferido).
3. **Borrado de componentes legacy del shell**: `MonoDevelop.Ide/Services/AddinEngineHost.cs` eliminado (exclusivo de Avalonia — el csproj GTK no lo lista en su lista explícita — y su único usuario era el diálogo); ProjectReferences `Mono.Addins`/`Mono.Addins.Setup` retiradas de `MonoDevelop.Startup.Avalonia.csproj`. Los ensamblajes Mono.Addins siguen estagiados en `main/build` para el build GTK (árbol unificado, regla 18.4); el diálogo GTK legacy vive en el submódulo `mono-addins` (intacto).
4. **27 manifiestos** declaran `identity.icon` reutilizando PNG 32px/48px ya existentes (core y carpetas legacy): `core:file-web-32` (AspNet/AspNetCore/WebReferences), `core:file-source-32` (CSharp/VB/SourceEditor), `core:file-unit-test-32` (UnitTesting/NUnit), `MonoDevelop.Debugger/icons/exception-48`, `Deployment/MonoDevelop.Deployment/icons/package-32`, `MonoDevelop.Gettext/icons/file-locale-32`, `MonoDevelop.PackageManagement/icons/package-source-32`, `core:package-32` (NuGet), `core:project-package-32` (Packaging), `core:project-crossplatform-shared-32` (DotNetCore), `core:file-xml-32` (XmlEditor), `core:workspace-32` (VersionControl), etc. Los 8 add-ins sin icono (3 plataformas ×2 manifiestos, Refactoring, RegexToolkit) usan el fallback genérico — mismo comportamiento que el diálogo legacy sin `Icon32`.

**Cómo validar**:

1. `cd main && dotnet build src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -m:1` → 0 errores.
2. `cd main/build && timeout 45 dotnet MonoDevelop.AvaloniaShell.dll --addins` → log: `[addins] tab=installed items=35 host=True`; al cambiar de pestaña: `tab=updates items=0` y `tab=gallery items=35`; 0 `Exception`; 0 líneas Cecil/Mono.Addins runtime.
3. Los 27 iconos declarados resuelven a PNG reales en disco (script `~/opencode/addins_manager/03_icon_check.py`: 27/27, magic bytes verificados).
4. Paridad UI: `git diff 3f619973b6..HEAD -- main/src/core/MonoDevelop.Startup.Avalonia/Views/AddinManagerDialog.axaml` = 1 hunk (la plantilla de iconos de la fila: `ShowInstalledIcon`/`ShowAvailableIcon` + `Image` de icono propio); 3 pestañas, headers y botones idénticos al original.
5. Regresión: `--prefs-tree` → `merged=5 (registry points=85)`, 0 missing; `--addonmanager` → `loaded 31/35` (Mac/Windows por diseño); `--old-gui` → 20 s sin crash (exit 124); `--extensionsdlg` → mismo diálogo.
6. Sin `using Mono.Addins`/`AddinEngineHost`/`SetupService` en el código del shell Avalonia; csproj sin referencias a mono-addins; el csproj GTK no lista `AddinEngineHost.cs` → el borrado no afecta al build GTK.

**QA (§18.5)**: Tester QA Senior, **APTO — 0 errores** (INFO no bloqueantes: 8 add-ins sin icono → fallback; gating `CanDisable`/`CanUninstall` legacy eliminado — equivalente al default true; `tab=installed` 2× en el log, patrón original). Artefactos: `~/opencode/addins_manager/` (01–07 y 10_*).

**Pendiente/deferido**:

- Acciones Install/Update/Enable/Disable/Uninstall/Install from file requieren API en el nuevo host (repositorio remoto + instalación + habilitación) — las pestañas y la UI están listas sobre los nuevos componentes.
- El `CecilReflector` de `main/external/mono-addins` sigue incompatible con el Cecil 0.10.1 del build: afecta al diálogo/registry Mono.Addins **del lado GTK** (no a la shell Avalonia, que ya no lo usa). Parchearlo para Cecil 0.10 es tarea del lado GTK.
