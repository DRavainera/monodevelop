# Linux build report — MonoDevelop core with dotnet msbuild

- **Fecha:** 2026-09-06 (branch `agents/sub-agent-senior-dev-role-report`)
- **Último build:** `dotnet msbuild main/Main.sln -p:Configuration=Debug -m:1 -t:Rebuild -p:DisableDownloadNupkg=true`
  → **EXIT=0, 0 errores** (2119 warnings). Commit: `07763ed47c`.

## Estado

| | cantidad |
|---|---|
| Entradas (`Project`) en `Main.sln` | 218 |
| Proyectos activos (`Build.0`) | **145** |
| Buildables sin `Build.0` | 91 |
| — de plataforma (Windows/Mac/Cocoa/WPF, no aplican a Linux) | 22 |
| — pseudo/tool projects (runners, UI-test harness) | 3 |
| — **cluster de deuda (legacy toolchain)** | **66** |

El **núcleo** (todo lo activo) compila y enlaza limpio: `MonoDevelop.Core`,
`MonoDevelop.Ide`, `MonoDevelop.TextEditor` (con stub Cocoa en Linux),
`MonoDevelop.SourceEditor2`, `vs-editor-api` (cierre WPF/editor), base de
addins (Debugger, HexEditor, Xml, Gettext, PerformanceDiagnostics, etc.).

## Por qué no es "solo añadir más proyectos"

`dotnet msbuild Main.sln` no puede sustituir el toolchain legacy con el que se
compilaba este código (era `xbuild` + `downloadnupkg`/`nuget.exe` + Roslyn
integrado de VS). Hechos constatados:

1. **`BuildProjectReferences=false` implícito en sln**: solo se compila lo
   activo en `Build.0`; el resto se resolvía contra el bin del checkpoint.
2. **`IncludeCopyLocal` no puebla `main/build/bin`** bajo dotnet-msbuild
   (solo 63 dlls residuos). Cada addin debe auto-referenciar por `HintPath`
   al NuGet cache o GAC de Mono (`/usr/lib/mono/4.5`).
3. **Roslyn descompensado**: el código usa `3.4.0-beta4-final` (features,
   editor-features, workspace) y en el cache NuGet SOLO existen
   `3.11.0-4.25056.4` y `4.8.0-7.25569.21` (API incompatible: MEF1
   `ExportCompletionProvider` del repo vs MEF moderno). Sin
   `microsoft.visualstudio.languageservices` 3.4 → CSharpBinding, Refactoring,
   dibujador Stetic de GtkCore, VBNetBinding y RegexToolkit no compilan.
4. **`nuget.exe`/`DownloadNupkg` inservible** en Linux (MSB6006, code 53) →
   se introdujo `-p:DisableDownloadNupkg=true` y se gatearon en
   `MDBuildTasks.targets` los targets `DownloadNupkg`/`_DownloadNupkgIfNeeded`
   y `CopyTestAdapters`. Consecuencia: assets runtime no descargables
   (TestPlatform 16.2.0, Mono.TextTemplating 1.3.1, plantillas).
5. **`NuGet.PackageManagement 5.4.0` API skew** (tipos internos:
   `AmbientAuthenticationState`, `CredentialResponse`,
   `HttpSourceCredentials`, `ICredentialService`; faltan
   `NuGet.Common/Configuration/Core`) → PackageManagement + DotNetCore +
   Packaging + ConnectedServices.
6. **`GuiUnit.exe` 0 bytes** → todo el cluster de tests no tiene harness.

## Cluster de deuda (66 proyectos sin Build.0)

Causas-raíz agrupadas:

- **Roslyn features 3.4 (no disponible):** `CSharpBinding` (completion +
  host de unit-testing), `Refactoring`, `GtkCore` (diseñador Stetic usa
  `MonoDevelop.Refactoring/Deployment/CSharp` + `ICSharpCode.SharpDevelop.Dom`),
  `VBNetBinding`, `RegexToolkit`.
- **PM 5.4 API skew / TestPlatform:** `PackageManagement`, `DotNetCore`,
  `Packaging`, `TextTemplating` (también Mono.TextTemplating 1.3.1),
  `UnitTesting`, `UnitTesting.NUnit`, `NUnit`, `ConnectedServices`, `AspNet`,
  `AspNetCore(+DevCertInstaller)`.
- **Decompiler/dom legacy del bin:** `AssemblyBrowser`, `DocFood`,
  `Deployment(+Linux)`, `ChangeLogAddIn`.
- **Dependencias del cluster / misc:** `VersionControl(+Git/Subversion/Unix)`,
  `Gettext`, `mdmonitor`, `Autotools(+CSharpBinding.AspNet)`.
- **Tests (harness 0-byte):** GuiUnit_NET_4_5, IdeUnitTests, Core.Tests,
  Ide.Tests, Xml.Tests, CSharpBinding.Tests, Reflex.Tests, 20+ (ver
  `Main.sln`).
- **F#/fsproj:** FSharpBinding, FSharp.Shared, FSharpInteractive.Service.
- **Otros sin Build.0 (mantenidos):** projectos de otras plataformas y
  runners UI.

## Cambios entregados (commit `07763ed47c`)

- `main/Main.sln`: poda de `Build.0` (centro del verde; BOM/CRLF respetados).
- `main/msbuild/MDBuildTasks.targets`: gate `DownloadNupkg` /
  `_DownloadNupkgIfNeeded` / `CopyTestAdapters` por `DisableDownloadNupkg`.
- `MonoDevelop.TextEditor`: stub Cocoa Linux `CocoaSupport/CocoaExtras.cs`
  (`IFindPresenter*`, `IInfoBarPresenter*`, `InfoBarAction`,
  `ICocoaTextView.IsKeyboardFocused/Focus`) con `<Compile Remove>` gateado por
  `HaveXamarinMac`.
- Refactor Roslyn self-contained vía HintPath (patrón de `Core.csproj`):
  `CSharpBinding`, `Refactoring`, `Xml`, `Autotools`
  (`microsoft.codeanalysis.{common,csharp,csharp.workspaces,workspaces.common}`
  `3.4.0-beta4-final` + `system.collections.immutable 1.5.0`).
- Referencias `Mono.Cecil 0.10.1` (Debugger.Soft) y
  `Mono.Posix/Mono.Cairo` desde `$(MonoFrameworkNet45Directory)` (Gdb,
  mdmonitor, HexEditor).

## Cómo desbloquear más addins (futuro)

1. Restaurar/compilar el toolchain legacy (xbuild+downloadnupkg) **o**
2. Mojar el repo a una versión de Roslyn/PM/TestPlatform que exista en el
   cache actual (migración API: MEF→System.Composition, features surface), **o**
3. Proveer binarios VS intermedios (3.4.0-beta4-final, decompiler) en un
   feed local y usarlos por HintPath.

## Verificación

- Logs de iteración: `/tmp/opencode/sln_build1.log` … `sln_build26.log`
  (b26 = verde), `pm_restore.log`, `gtkcore_iter.log`.
- `README`/`session_summary.md` con el detalle cronológico y GUIDs.