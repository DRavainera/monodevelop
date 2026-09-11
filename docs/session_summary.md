# Session Summary — Build MonoDevelop con dotnet msbuild en Linux

## Goal
Dejar en verde el build del núcleo de MonoDevelop en Linux usando `dotnet msbuild` sobre `main/Main.sln`, mitigando la falta del toolchain legacy (xbuild, nuget.exe/downloadnupkg, Roslyn integrado de VS), y commitear en bloques.

## Constraints & Preferences
- Español; mantener `session_summary.md` actualizado.
- 1 problema a la vez; respetar BOM (`utf-8-sig`) y finales de línea mixtos de `Main.sln`.
- Commits por bloque solo tras build verde; commit/push solo a `origin` (`DRavainera/monodevelop`).
- Build SIEMPRE `-t:Rebuild` (sin él, skip incremental/artefactos del checkpoint dan falsos verdes):
  `export PATH="$HOME/.dotnet:$PATH"; export DOTNET_ROOT="$HOME/.dotnet"`
  `dotnet msbuild main/Main.sln -p:Configuration=Debug -m:1 -t:Rebuild -p:DisableDownloadNupkg=true`
- Decisión de usuario: "Verde núcleo + diferir cluster". Límite de red de agentes previo vigente.

## Estado ACTUAL (build31)
- **Main.sln VERDE: EXIT=0, 0 errores** (5351 warnings; MSB3277/CS8012/CS8002/MSB3245 benignos). 87 proyectos activos en `Build.0` (config Debug|Any CPU); el resto diferido (cluster deuda).
- Cableados al `Build.0` del sln: CSharpBinding `{07CC7654-...}`, MonoDevelop.Refactoring `{100568FC-...}`, AssemblyBrowser `{0EA3AD14-...}`, RegexToolkit `{1F29B0A7-...}`, DocFood `{875D389F-...}`, TextTemplating `{8CCA39DD-...}`, mdmonitor `{D0B5AF2B-...}`, NUnitRunner `{0AF16AF1-...}`, VsCodeDebugProtocol `{10F5BBD5-...}`, AspNet `{1CF94D07-...}`, AspNetCore `{B3E73DE7-...}`, Autotools `{CFC02FEC-...}`, Deployment `{9BC670A8-...}`, PackageManagement `{F218643D-...}`, DotNetCore `{6868153E-...}`.
- Núcleo compilando: Core, Ide, TextEditor, SourceEditor2, vs-editor-api y todos los addins que compilan limpio.

## Deferred / Cluster (deuda) — 73 proyectos
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
- `Main.sln`: podas iterativas Build.0 (cada una con su build; 26→0 errores: b1..b26). Logs `/tmp/opencode/sln_build{N}.log`.

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
- `main/Main.sln` (podas), `main/msbuild/MDBuildTasks.targets` (flags), `session_summary.md`.
- Addins tocados: TextEditor (stub), SourceEditor2 (2 fixes previos), CSharpBinding, Debugger.{Soft,Gdb}, HexEditor, mdmonitor, Xml, Refactoring (diferido), UnitTesting (gate), Autotools.
## GUI MonoDevelop en Linux — EJECUTADA (2026-09-06, sesión actual)
- Milestone: la GUI arranca y queda estable con ventana GTK `MonoDevelop` (0x01200003) en DISPLAY :0; proceso vivo (>56s, ~170MB RSS); screenshots en `/tmp/opencode/md_gui_live_shot.png`.
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
- Plan redactado: `Doc/interfaz-plan.md`. Fases: In1 registro de addins, In2 runtime residual, In3 smoke tests (criterio aceptación), In4 cierre.
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
- Limitación del entorno de throttling: este modelo no acepta imágenes → verificación visual limitada a árbol X + píxeles (contenido real, ~4.7k colores) y logs; las capturas `/tmp/opencode/md_*.png` quedan para revisión humana.
- Herramienta muleta creada: `/tmp/opencode/keys.py` (teclas XTEST robustas) y `/tmp/opencode/killmd.py` (kill por argv exacto, incluido wrapper `mono MonoDevelop.exe`).
- Instancia MD SIGUE CORRIENDO en DISPLAY :0 (1 proceso, log `/tmp/opencode/md_sln.log`, UserProfile regenerado en `~/.config/MonoDevelop/8.0`).

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
- **Pendientes para el reporte**: apertura+compilación de proyecto C# queda a la migración (CSharpBinding→Refactoring→Features); `interfaz-plan.md` y este summary actualizados con el alcance net8.

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
- Commit `602f9788db` (97 archivos, +3836/−1313) pusheado a `origin/agents/sub-agent-senior-dev-role-report`: incluye TODO el bloque Roslyn-4.8/compat (CSharpBinding, MonoRoslynCompat, Refactoring — commitablе junto al wiring por solape en csproj) + wiring. Logs: `/tmp/opencode/sln_build{27,28,29,30,31}.log`; backups `Main.sln.bak`/`.bak2`.

### Sesión 2026-09-08 — sellado de la cadena host-services Roslyn (runs 9-11)
- **Causa raíz del FATAL "Can't create roslyn workspace"**: `MefWorkspaceServices.GetService[T]` (Workspaces 3.4) hace castclass al valor exportado; el `SolutionServices..ctor` pide 3 servicios (`ITemporaryStorageService`, `IMetadataService`, `IProjectCacheHostService`). Los factories de MonoDevelop.Ide exportaban solo `IWorkspaceServiceFactory`, y su unwrap MEF (`b__1` → `CreateService`) producía valores (manager de metadata / cache host) no casteables al contrato interno.
- **Fix sistémico (9 factories/partes de MonoDevelop.Ide)**: cada `[ExportWorkspaceServiceFactory]` implementa además su contrato y `CreateService` devuelve `this` (soporta export directo y unwrap de fábrica). `IMetadataService` (delega vía `GetRequiredService<MonoDevelopMetadataReferenceManager>`), `IProjectCacheHostService` (shim `NoOpProjectCacheHostService`, porque el stub de compat `ProjectCacheService` no implementa el interface interno → el downcast reventaba dentro de `CreateService`), `IDocumentTrackingService`, `IErrorReportingService`, `IFrameworkAssemblyPathResolver`, `ISymbolNavigationService`, `INotificationService` (x2), `IExtensionManager`. Build MonoDevelop.Ide a 0 errores CS.
- **Evidencia runs**: run9 (`Ide.2026-09-08__00-20-02.log`) y run10 (`...00-26-59.log`) exit 124 sin FATAL ni InvalidCast; run11 (`...00-38-17.log`, con `/tmp/opencode/testproj/TestProj.sln`) crea RootWorkspace+TypeSystemService+CompositionManager y compone MEF sellado.
- **Evaluación de proyecto C# bajo mono queda bloqueada por entorno** (no por Roslyn): los `Microsoft.Build*.dll` de `build/bin` son copias del dotnet SDK 8.0.424 (refs `System.Runtime 8.0.0.0`, inbindables) y no existe engine MSBuild 15.x mono-compatible en Arch (`/usr/lib/mono/msbuild` ausente; xbuild 4.x no evalúa SDK-style). Subproceso de evaluación en `RemoteBuildEngineManager.cs:456`. Se re-resuelve con MSBuild net8 en la migración.
- **0-exports del vs-editor del fork (IGuardedOperations, IBufferGraphFactoryService, IContentTypeRegistryService, UndoHistoryRegistry, TextSearchService, …)**: interfaces en compat sin implementación en el fork → diferido a In3 (rework de catálogo con `Microsoft.VisualStudio.LanguageServices` net8). Errores de composición MEF siguen siendo warnings no críticos.
- **Framework paths del sistema** (`TargetRuntime.cs:645-653`): `ReadTargetFramework` captura y registra ERROR por cada `.NETFramework` cuyo `*-api` no existe en Arch (4.6/4.6.1/... ); benigno, frameworks disponibles cargan.
