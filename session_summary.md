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

## Estado ACTUAL (build26)
- **Main.sln VERDE: EXIT=0, 0 errores** (2119 warnings). 218 proyectos en sln; 145 activos en `Build.0`; **73 diferidos** (cluster de deuda técnica).
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