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
