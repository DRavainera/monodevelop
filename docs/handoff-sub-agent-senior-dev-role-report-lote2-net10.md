# Handoff — Conversión de 5 proyectos a net10.0 (subagente senior-dev, lote 2)

## Qué se hizo
Se convirtieron 5 proyectos a SDK-style `net10.0` (`dotnet build -c DebugLinux`).
El código legacy no se borra: se convierte en el sitio. El resto del árbol no se tocó.

| # | Proyecto | Tipo | Resultado `dotnet build -c DebugLinux` |
|---|---|---|---|
| 1 | `main/external/fsharpbinding/MonoDevelop.FSharp.Gui/MonoDevelop.FSharp.Gui.csproj` | old-style net472 → SDK | **OK, 0 errores** |
| 2 | `main/external/guiunit/src/framework/GuiUnit_NET_4_5.csproj` (submódulo `guiunit`, rama `net10`) | old-style v4.5 Exe → SDK | **OK, 0 errores** |
| 3 | `main/src/core/MonoDevelop.Projects.Formats.MSBuild/MonoDevelop.MSBuildResolver/MonoDevelop.MSBuildResolver.csproj` | old-style v4.6.1/v4.7.2 → SDK | **OK, 0 errores** |
| 4 | `main/external/Xamarin.PropertyEditing/Xamarin.PropertyEditing/Xamarin.PropertyEditing.csproj` (submódulo, rama `net10`) | SDK netstandard2.0 → net10.0 | **OK, 0 errores** |
| 5 | `main/src/addins/TextTemplating/MonoDevelop.TextTemplating/MonoDevelop.TextTemplating.csproj` | old-style net472 → SDK | **Conversión verificada 0/0 en aislado; el build del proyecto queda bloqueado por upstream (ver pendiente)** |

Submódulos modificados (commit + push en rama `net10` + puntero en el repo principal):
`external/guiunit`, `external/Xamarin.PropertyEditing`.

## Por qué / decisiones
- Cabecera: proyectos del árbol `main/src` y `main/external` que importan
  `MonoDevelop.props` usan `<Project DefaultTargets="Build" Sdk="Microsoft.NET.Sdk"
  xmlns="http://schemas.microsoft.com/developer/msbuild/2003">` (combinación `Sdk` +
  `xmlns` **correcta** — funciona en el SDK 10.0.401; si aparece MSB4041, el xmlns está
  mal escrito). `GuiUnit` (submódulo que no importa `MonoDevelop.props`) usa
  `<Project Sdk="Microsoft.NET.Sdk">` sin import, conservando sus props propios.
- Profundidad de imports (verificada por estructura, no por suposición):
  FSharp.Gui → `..\..\..\MonoDevelop.props` (3, = `main/`); MSBuildResolver →
  `..\..\..\..\MonoDevelop.props` (4, = `main/`); TextTemplating →
  `..\..\..\..\MonoDevelop.props` (4, = `main/`). Nota: la tarea decía 4 y 5 niveles
  respectivamente; el `main/src/MonoDevelop.props` no existe, el de `main/` sí.
- `TargetFramework net10.0`, `MDFrameworkVersion v10.0`, `MDTargetFramework net10.0`,
  `EnableDefaultItems=false`, `EnableDefaultCompileItems=false`,
  `Configurations=Debug;Release;DebugMac;ReleaseMac;DebugWin32;ReleaseWin32`,
  `Platforms=AnyCPU`, `GenerateAssemblyInfo=false` (excepto FSharp.Gui, que no tiene
  `AssemblyInfo.cs`: el SDK la genera).
- BCL implícitas eliminadas (`System`, `System.Core`, `System.Xml`, `System.Xml.Linq`,
  `System.Data`, `mscorlib`). `AssemblyName`/`RootNamespace`/`ProjectGuid`/`OutputPath`
  conservados. En GuiUnit NO se cambió el nombre ni `AssemblyName` (otros proyectos lo
  referencian por `GuiUnit_NET_4_5.csproj`).
- gtk-sharp 2.12 (FSharp.Gui, TextTemplating): `<Import Project="$(ReferencesGtk)" />`
  en vez de las 5-6 refs explícitas (sin duplicar).
- `Mono.Posix` (FSharp.Gui): la fuente usa `Mono.Unix.Catalog.GetString` (llamadas
  cualificadas, no `using`) → ref vía `$(MonoFrameworkNet45Directory)/Mono.Posix.dll`
  (resuelve a `main/build/Mono.Posix.dll` en esta máquina) + `Private=False`, mismo
  patrón que `MonoDevelop.Autotools`/`Deployment`.
- `Microsoft.Build*` (MSBuildResolver): HintPath `$(MSBuildBinPath)\Microsoft.Build*.dll`
  (patrón del hermano ya convertido `MonoDevelop.MSBuildBuilder.csproj`; el SDK 10 trae
  esos ensamblables). Se eliminó la propiedad `MSBuild_OSS_BinDir` con sus conditions
  xbuild/msbuild-15. `PlatformTarget x64` conservado (se carga dentro del proceso de
  MSBuild; warning CS8012 inofensivo, mismo que el hermano).
- `Mono.TextTemplating` (TextTemplating): no hay dll stageada en `main/build/`; el
  paquete `mono.texttemplating/1.3.1` sí existe en `~/.nuget/packages` pero solo con
  `lib/net45` (no hay `dotnet-texttemplating` ni versión netstandard). Ref directa
  `<Reference>` con `HintPath $(NuGetPackageRoot)mono.texttemplating/1.3.1/lib/net45/
  Mono.TextTemplating.dll` (patrón de ref a dll net4x de la caché; precedentado por
  `Microsoft.Build.Locator net46` en `MonoDevelop.Core`). Se quitó el
  `<PackageReference>` antiguo (paquete net45-only → NU1202 en net10.0) y el
  `<Reference>` duplicado. `IncludeCopyLocal`/`ExtensionContent`/`DownloadNupkg`
  conservados íntegros.
- `System.CodeDom` (TextTemplating): no existe en el BCL/ref pack de net10 →
  `<PackageReference Include="System.CodeDom" Version="5.0.0" PrivateAssets="runtime" />`
  (mismo patrón que `MonoDevelop.Core.csproj:140`; `NRefactory.CSharp` ya lo usaba).
- GuiUnit: `DefineConstants` incondicionales `NET_4_5;CLR_4_0;NUNITLITE` (la fuente usa
  `#if NET_4_5`/`#if NUNITLITE`/`#if CLR_4_0`; los PGs condicionales Debug/Release
  antiguos no se aplican a `-c DebugLinux` y además traían un `DefineConstants` con
  comas/espacios que dejaba las constantes mal definidas). El SDK aporta `DEBUG`/`TRACE`.
  Se añadieron `PublicSign=true` + `NoWarn SYSLIB0011` (ver fixes). Se eliminaron
  props de ClickOnce/bootstrapper/publish, `DocumentationFile`, `IntermediateOutputPath`
  y `OutputPath` custom (submódulos → bin\ default, como Xwt/Mono.Addins).
- Xamarin.PropertyEditing: cambio mínimo `netstandard2.0` → `net10.0`. No hay
  PackageReferences ni conditions sobre TargetFramework. El `Directory.Build.props` del
  submódulo es deliberadamente vacío (no hereda el de `main/`) y su
  `Directory.Build.targets` ya pone `SignAssembly`/`PublicSign` con su propio snk.

## Fixes mínimos en fuentes .cs (documentados, sin tocar más)
- Submódulo `guiunit`:
  - `src/framework/Internal/TestExecutionContext.cs` — se eliminó
    `using System.Runtime.Remoting.Messaging;` (namespace inexistente en net10.0, CS0234).
    Era un vestigio: el único tipo que podía usar (`ILogicalThreadAffinative`) ya está
    comentado en la declaración de la clase (línea ~57).
- Submódulo `Xamarin.PropertyEditing` (ambigüedad CS0104 nueva en net10.0:
  `System.Collections.Generic.OrderedDictionary` no existía en netstandard2.0, ahora sí,
  y choca con `Cadenza.Collections.OrderedDictionary`; se cualificó el tipo Cadenza, el
  que el código usa de toda la vida):
  - `Xamarin.PropertyEditing/ObservableLookup.cs` (2 sitios)
  - `Xamarin.PropertyEditing/ViewModels/PanelViewModel.cs` (1 línea, decl + instanc.)
  - `Xamarin.PropertyEditing/ViewModels/BrushPropertyViewModel.cs` (1 sitio)
  - `Xamarin.PropertyEditing/ViewModels/SimpleCollectionView.cs` (2 líneas)
- Árbol main, `src/addins/TextTemplating/MonoDevelop.TextTemplating/
  TextTemplatingFileGenerator.cs` — `LogicalSetData`: `CallContext` no existe en
  net10.0 (CS0234). En Mono, `CallContext.LogicalSetData` tiraba
  `NotImplementedException` (el propio FIXME lo dice) y el catch hacía warning + no-op;
  además `NamespaceHint` se escribe pero nunca se lee en el árbol. Se dejó el
  comportamiento real: warning único + no-op, sin la llamada muerta.

## Cómo se valida
```
cd main
~/.dotnet/dotnet build external/fsharpbinding/MonoDevelop.FSharp.Gui/MonoDevelop.FSharp.Gui.csproj -c DebugLinux -v:m -nologo
~/.dotnet/dotnet build external/guiunit/src/framework/GuiUnit_NET_4_5.csproj -c DebugLinux -v:m -nologo
~/.dotnet/dotnet build src/core/MonoDevelop.Projects.Formats.MSBuild/MonoDevelop.MSBuildResolver/MonoDevelop.MSBuildResolver.csproj -c DebugLinux -v:m -nologo
~/.dotnet/dotnet build external/Xamarin.PropertyEditing/Xamarin.PropertyEditing/Xamarin.PropertyEditing.csproj -c DebugLinux -v:m -nologo
~/.dotnet/dotnet build src/addins/TextTemplating/MonoDevelop.TextTemplating/MonoDevelop.TextTemplating.csproj -c DebugLinux -v:m -nologo
```
Resultado (2026-10-23): 1-4 → `Compilación correcta`, 0 errores (solo warnings:
MSB3245 glade-sharp inexistente en `main/build` — inofensivo, la fuente no usa glade;
NU1903 de `Microsoft.Build.Tasks.Core 15.9.20` en `MDBuildTasks` — preexistente;
CS8012/CA1416/SYSLIB0006 en legacy). 5 → ver "pendiente".

Validación aislada del proyecto 5 (el proyecto mismo no alcanza a compilar porque la
cadena de ProjectReferences cae antes, ver pendiente): se compilaron sus 14 fuentes en
un proyecto scratch en `~/opencode/t4verify/` contra las dlls net10.0 ya construidas
(`MonoDevelop.Core.dll` — el namespace `MonoDevelop.Projects` hoy vive dentro de
`MonoDevelop.Core` —, `MonoDevelop.Ide.dll`, `MonoDevelop.DesignerSupport.dll`,
`Mono.Addins.dll`), `Mono.TextTemplating` net45 de la caché NuGet, `System.CodeDom`
5.0.0 y gtk-sharp 2.12 de `main/build`: **0 errores, 0 warnings**.

## Qué queda pendiente / notas (para el agente principal)
- **Build 5 bloqueado por upstream (no por esta conversión)**: la cadena
  `TextTemplating → MonoDevelop.Ide → Xwt.Gtk / Mono.Debugging` falla hoy:
  - `main/external/xwt/Xwt.Gtk/Xwt.Gtk.csproj` (old-style, fuera de este lote): el
    PropertyGroup de la config usa `Condition=" '$(Configuration)|$(Platform)' ==
    'DebugLinux|Any CPU' "` (con espacio) y el build pasa `Platform=AnyCPU` →
    "BaseOutputPath/OutputPath no está establecida" en `-c DebugLinux`.
  - `main/external/debugger-libs/Mono.Debugging` (ya convertido por el lote anterior):
    `RemoteFrameObject.cs`/`ObjectValue.cs` usan `System.Runtime.Remoting.RemotingServices`
    (CS0234) — el fix `#if !NET` del lote anterior no cubre esos dos sitios.
  Cuando ambos queden, el proyecto 5 debe compilar (fuentes ya verificadas en aislado).
- **`DownloadNupkg` en Linux**: el target compartido
  (`main/msbuild/MDBuildTasks.targets`) ejecuta `main/external/nuget-binary/nuget.exe`
  (PE de Windows); en Linux falla con `MSB6006` (código 53). Es preexistente y afecta a
  los 4 addins que traen `DownloadNupkg` (UnitTesting incluido); la convención
  documentada del build en Linux es `-p:DisableDownloadNupkg=true`
  (ver `docs/linux-build-report.md`). Con esa propiedad, la copia de
  `ExtensionContent` de `TextTransform.exe` queda cubierta porque se stageó
  `main/packages/Mono.TextTemplating.1.3.1/tools/TextTransform.exe` desde el nupkg de la
  caché (idéntico contenido al que descargaría `nuget.exe`; `main/packages` está
  gitignoreado y NO se commitea). La copia de esos archivos a la salida solo ocurre vía
  el target `_CopyExtensionOutputFiles` (hook `PrepareForRun`), igual que antes de la
  conversión: la conversión no cambia ese comportamiento.
- **GuiUnit / BinaryFormatter**: `Constraints/BinarySerializableConstraint.cs` usa
  `BinaryFormatter`; en net10.0 es error por defecto (SYSLIB0011) → suprimido en
  `NoWarn` del csproj (no se tocó la fuente). En runtime, si alguien usa ese constraint,
  `BinaryFormatter` lanza (limitación de .NET, no de la conversión). El assembly queda
  `PublicSign` (publica la key de `guiunit.snk` sin firmar con RSA-SHA1, que OpenSSL 3
  rechaza: `invalid digest`); la identidad strong-name se conserva.
- **OutputPath + TFM**: los addins convierten a `main/build/AddIns/<Nombre>/net10.0/`
  (el SDK apela el TFM a `OutputPath`); igual que el canónico GtkCore. El staging
  solution-level (`after.Main.sln.targets` / `Main.sln`) es responsabilidad del agente
  principal.
- No se ejecutó `dotnet build Main.sln` (lo hace el agente principal). No se tocó la
  shell Avalonia ni fuentes de UIs Gtk ajenas a los 5 proyectos.

## Actualización final (cierre de ola 1 — 2026-10-23, agente principal)

Los bloqueos upstream listados arriba quedaron resueltos y **los 5 proyectos del
lote 2 compilan ahora con 0 errores** (`dotnet build -c DebugLinux`), incluido el
proyecto 5 (TextTemplating) con toda su cadena: `MonoDevelop.Core → MonoDevelop.Ide →
Xwt.Gtk → Xwt → TextTemplating`.

Resolución de los bloqueos:
- `Xwt.Gtk`: convertido a SDK net10.0 (submódulo `xwt`, rama `net10`); el warning
  MSB3245 de glade-sharp desapareció al stagear `glade-sharp.dll` (y `pango-sharp.dll`)
  en `main/build/` desde el build `~/opencode/gtksharp-fixed/` (fuente
  mono/gtk-sharp 2.12).
- `Mono.Debugging` (RemotingServices): ya resuelto por el lote 1 (commit previo).
- `MonoDevelop.Ide` (primera vez que entra en el grafo de este lote): dos clases de
  fallos, ambos resueltos:
  1. Referencias legacy por HintPath `$(MonoFrameworkNet45Directory)/...`
     (System.Design, System.Web, System.Web.Services, System.Windows.Forms,
     System.Xaml): el sistema no trae perfil 4.5 (`/usr/lib/mono/4.5` vacio, GAC vacio)
     y `main/build` no tenia esas facades → **nuevo target
     `EnsureLegacyReferenceStaging`** (`main/Directory.Build.targets`) copia las 5
     facades del pack net472 de NuGet a `main/build/` si no existen (reproducible;
     `main/build` sigue gitignored). `LegacyReferenceAssembliesDir` se define ahora
     siempre en `main/Directory.Build.props` (antes solo con el opt-in).
  2. `RegexEngine/RegexCompiler.cs` (motor Regex vendored): APIs de emit eliminadas en
     .NET Core (`AppDomain.DefineDynamicAssembly`, `AssemblyBuilderAccess.RunAndSave`,
     `AssemblyBuilder.DefineUnmanagedResource`, `AssemblyBuilder.Save(string)`).
     Fix `#if NET` mínimo: `AssemblyBuilder.DefineDynamicAssembly(..., Run, ...)`
     (in-memoria), `resourceFile` → `ArgumentOutOfRangeException` (como el branch
     `FEATURE_PAL`), y `Save()` → `NotSupportedException` (el .NET Core no puede
     escribir ensamblados dinamicos a disco; `Regex.Compile` in-memoria sigue
     funcionando; solo la exportacion a disco del tool queda fuera).

Infraestructura compartida nueva (se usa en toda la migracion, no solo en este lote):
- **`main/msbuild/LegacyReferenceAssemblies.targets`** → target
  `PreferLegacyReferenceAssemblies` (AfterTargets `ResolveReferences`, BeforeTargets
  `FindReferenceAssembliesForReferences` — el orden importa: Csc consume
  `@(ReferencePathWithRefAssemblies)`, copia que ese target genera). Resuelve el
  conflicto RAR "stub net10.0 vs reference net472": el pack
  `Microsoft.NETCore.App.Ref` trae facades con version de archivo 10.x (WindowsBase
  vacio 16KB, System.Web solo con HttpUtility, ...) y RAR unifica por nombre simple
  eligiendo la version mayor → ganaba el stub (CS0234). El target quita del set los
  items del pack net10.0 de la lista (WindowsBase, PresentationCore,
  PresentationFramework, System.Xaml, System.Web, System.Web.HttpUtility,
  System.Web.Services, System.Design, System.Windows.Forms) y reanade la reference
  net472 completa (compile-only, `Private=false`) solo si queda sin version. Se
  importa desde los `Directory.Build.targets` de `main/`, `external/xwt/`,
  `external/vs-editor-api/` y `external/Xamarin.PropertyEditing/` (la busqueda de
  `Directory.Build.targets` se detiene en la mas cercana).
  - `System.Web.HttpUtility` (asamblea standalone net10) va con System.Web: si no se
    quita, duplica el tipo `HttpUtility` contra la reference net472 (CS0433).
- **`main/msbuild/MDBuildTasks/DownloadNupkg.cs`**: si los paquetes
  `@(DownloadNupkg)` ya estan extraidos en `PackagesDirectory`, se omite
  `nuget.exe` (PE .NET Framework que necesita mono; esta maquina no tiene mono y
  `nuget.exe restore` salia con codigo 53). `main/packages/Mono.TextTemplating.1.3.1/`
  se stageo completo desde `~/.nuget/packages/mono.texttemplating/1.3.1/`.
- **Xwt (submódulo)**:
  - `Xwt.WPF` (net10.0-windows + UseWPF + UseWindowsForms + EnableWindowsTargeting):
    en WPF .NET 5+ todos los ensamblados de tema se referencian a la vez y duplican
    los tipos chrome (`ButtonChrome`, CS0433 entre `PresentationFramework.Aero` y
    `Aero2`). Nuevo target `RemoveExtraWpfThemeAssemblies`
    (BeforeTargets `FindReferenceAssembliesForReferences`, match por identidad: los
    items del pack vienen con path completo) deja solo `Aero` (el que referenciaba el
    Xwt legacy). `NoWarn WFO1000` (aviso design-time del analizador WinForms sobre
    `ColorDialogWithTitle.Title`; solo afecta al diseñador).
  - `Xwt.Gtk.Windows` (net10.0 + `UseLegacyReferenceAssemblies`): el swap de
    `PreferLegacyReferenceAssemblies` le da la `WindowsBase` net472 completa en vez
    del stub net10.0 vacio (CS0234 `System.Windows.Input.ModifierKeys`/`Key`).
  - `Xwt.WPF`/`Xwt.Gtk.Windows`: faltaba la linea
    `<PublicSign Condition="Exists(...MonoDevelop.props)">true</PublicSign>` que ya
    llevan `Xwt`/`Xwt.Gtk`; sin ella la firma fuerte completa con `xwt.snk` falla en
    OpenSSL 3 (`invalid digest`, clave RSA vieja).

Estado de ola 1 al cierre (todo `0 Errores` con `-c DebugLinux`):
- Lote 1 (ya commiteado antes): nrefactory x3, debugger-libs x6.
- `xwt`: Xwt, Xwt.Gtk, Xwt.WPF, Xwt.Gtk.Windows.
- `mono-addins`: Mono.Addins, Mono.Addins.Setup, Mono.Addins.CecilReflector,
  Mono.Addins.Gui.
- Lote 2: FSharp.Gui, GuiUnit_NET_4_5, MonoDevelop.MSBuildResolver,
  Xamarin.PropertyEditing, MonoDevelop.TextTemplating.

Pendiente: las ondas 2-4 del plan (`docs/interfaz-plan.md`): ~62 proyectos
old-style restantes de `src/addins` + `src/tools` + tests, el cluster
`vs-editor-api` (TFM net472 en `build/Project.props`), la cadena WPF
`WindowsPlatform` → `net10.0-windows`, exclusion de proyectos Mac-only de
`Main.sln`, build completo de `Main.sln` a 0 errores y validaciones (suite
  unitaria + smoke `--old-gui` + smoke Avalonia).

## Cierre de ondas 2-3 (2026-10-24, agente principal) — `Main.sln` a 0 errores

### Estado final del gate
`dotnet build main/Main.sln -c DebugLinux` → **`Compilación correcta.`, 0 Errores**. Verificado en
incremental y en `--no-incremental` (clean-room): 97 proyectos compilan para `DebugLinux`; el resto
son proyectos Windows/Mac-only excluidos vía ausencia de `Build.0`.

### Hallazgo clave: `WindowsPlatform` no bloquea el gate
Los 4 proyectos Windows-only (`WindowsPlatform`, `WindowsAPICodePack/Core`, `WindowsAPICodePack/Shell`,
`WindowsPlatform.Tests`) llevan `ActiveCfg` para `DebugLinux` pero **sin línea `Build.0`** en
`Main.sln` (solo `DebugWin32`/`ReleaseWin32` la tienen). Por eso el gate `DebugLinux` NO los compila.
Los "9 errores" de `WindowsPlatform.csproj` al compilarlo en aislado venían de una config no-Win32
(`WIN32` no definido → `GtkWPFWidget`, que vive tras `#if WIN32` en `MonoDevelop.Ide`, queda excluido
→ CS0246/CS0234; + CS0122 IVT + CS0115 override de Xwt). Irrelevantes para el gate Linux.
Igual para los 5 Mac-only (`Xwt.Gtk.Mac`, `Xwt.XamMac`, `MacPlatform.Tests`, `TextUICocoa`,
`TextUICocoaUtil`): 0 líneas `Build.0` para `DebugLinux` → excluidos, no requieren touch en el `.sln`.

### Fixes del gate en esta sesión
- `main/src/addins/MonoDevelop.GtkCore/MonoDevelop.GtkCore.GuiBuilder/CombinedDesignView.cs:194` —
  `TextEditor` (CS0118): el namespace `MonoDevelop.TextEditor` (visible por lookup de namespace
  externo, `MonoDevelop` → sub-namespace `TextEditor`) sombreaba al tipo `MonoDevelop.Ide.Editor.TextEditor`
  importado por using → se cualificó `MonoDevelop.Ide.Editor.TextEditor`.
- `main/src/addins/MonoDevelop.GtkCore/MonoDevelop.GtkCore/GtkDesignInfo.cs` (~líneas 106 y 151) —
  `System.Runtime.Remoting.RemotingServices.Disconnect/Marshal` (CS0234; `MonoDevelop.GtkCore` no
  referencia la facade net472 de `System.Runtime.Remoting`, a diferencia de `libstetic`/`libsteticui`
  que la llevan) → `#if !NET` (no-op en net10.0; patrón receta §4).
- `MonoDevelop.Startup.Avalonia` — el `MSB4018` ("file being used by another process" en
  `obj/DebugLinux/Avalonia/resources`) era un flake por nodos MSBuild huérfanos reteniendo el handle;
  al limpiar los nodos y recompilar → 0 errores (no es código).

### Validación
- Gate: `dotnet build main/Main.sln -c DebugLinux` = `Compilación correcta.`, `0 Errores`
  (incremental + `--no-incremental`).
- Logs: `~/opencode/migrate/gate_debuglinux_full2_*.log` (incremental),
  `~/opencode/migrate/gate_cleanroom_*.log` (clean-room, 17955 líneas, 97 proyectos, 0 errores CS/MSB).

### Validación — Tester QA Senior (subagente, AGENTS.md §18.5)
El subagente QA Senior validó **antes** de documentar/commit/push (ciclo §18.5). Headless con
`xvfb-run -a` (sin ventanas en la sesión del operador); logs en `~/opencode/migrate/qa-senior/`.
El subagente re-ejecutó el gate de forma independiente:
`dotnet build main/Main.sln -c DebugLinux` → `Compilación correcta.`, `0 Errores` (exit 0).
- **Gate** : OK — exit=0, 0 errores.
- **Avalonia (por defecto, `--menudump`)**: OK — proceso vivo, add-ins `loaded=True` (30:
  `HexEditor`/`SourceEditor`/`TextEditor`/`XmlEditor`/...), extension points poblados (FileTemplates=52),
  pads bottom/right renderizados, `0` FATAL/Unhandled.
- **Gtk legacy (`--old-gui`)**: OK — el relay `MonoDevelop.AvaloniaShell.dll --old-gui` lanza el hijo
  `dotnet main/build/MonoDevelop.dll` (con `MONODEVELOP_LEGACY_UI=1`); el proceso Gtk IDE quedó vivo
  sobre `Xvfb`, `0` FATAL/Exception/Gtk-error.
- **Limpieza**: OK — 0 procesos residuales (smokes + Xvfb).

**Veredicto del subagente: APROBADO, 0 hallazgos de código** (única nota informativa: `kill` en bucle
`pgrep` es interceptado por el sandbox → usar `kill -9 <PID>` directo).

## Cierre de ola 4 (2026-10-26, agente principal) — P1, T1, T4, T5, T6
Estado: `Main.sln` a 0 errores; QA Senior 8/8 partes, 0 hallazgos abiertos. Detalle por tarea
(informe completo del QA en `~/opencode/qa_wave4/`, fuera del repo por §18.6):

### P1 — Mono.Posix → MonoUnixCompat (compat propio)
- Nuevo proyecto `main/src/compat/MonoUnixCompat/` (facades `Catalog`/`UnixCompat` sobre
  `System.IO.Unix`/P-Invoke net10.0), integrado por `ProjectReference` desde
  `MonoDevelop.Core.csproj` (patrón igual que `MonoRoslynCompat`); además el proyecto
  **entra en Main.sln** (carpeta "Core", GUID `F6BD06D7-5C70-4D19-B5BA-A5397CC4FBDA`, 8
  mapeos de configuración) — añadido tras el cierre de ola 4 a petición del operador y
  verificado en QA QS1 (gate 0 Errores + smokes OK).
- Eliminados `core/MonoDevelop.Core/MonoDevelop.Core/{Catalog.cs,Unix.Catalog.cs}` (era el
  glue Mono.Posix) y sus entradas `<Compile>`.
- Submódulos: `Mono.Debugging.Soft.csproj` (debugger-libs) y `Mono.Addins.Gui.csproj`
  (mono-addins) pasan de `<Reference Mono.Posix>` a `ProjectReference` a MonoUnixCompat
  (ruta relativa `..\..\..\src\compat\MonoUnixCompat\…`).

### T1 — WPF a net10.0-windows (opción A)
- 5 csprojs en `net10.0-windows` (WindowsAPICodePack Core/Shell, MonoDevelop.TextEditor.Wpf,
  WindowsPlatform, Xwt.WPF); cross-target desde Linux: 3/5 build limpio; 2 latentes
  preexistentes documentados (NU1102 `DiagnosticMargin` fuente privada; `_wpftmp` de
  WindowsPlatform).
- `xwt/Xwt.WPF/Xwt.WPFBackend/AccessibleBackend.AccessibilityHelper.cs`: `#if NET47` →
  `#if NET47 || NET10_0` (accesibilidad WPF reactivada en net10.0-windows).

### T4 — Roslyn 4.8.0 → 5.9.0
- `main/msbuild/RoslynVersion.props` → 5.9.0; `MonoRoslynCompat/src/CompatStubs.cs` (stubs 5.9.0 +
  adaptador `MefWorkspaceServices`); `MonoDevelop.Ide.TypeSystem/MonoDevelopMetadataReferenceManager.cs`
  reescrito stream-based; null-guards por `ISolutionCrawlerRegistrationService` (eliminada en
  5.9.0); fix stub `ValueSource<T>` (dispatch invertido rompía document-open con
  `Contract.ThrowIfNull`).
- Staging 5.9.0 byte-idéntico (md5) en `main/build/`; línea VS 4.8.0 inerte (composition errors
  INFO tolerados, iguales que en 4.8.0).
- `libgit2-71efcb3.so` compilado de `main/external/libgit2` (bloqueador preexistente de
  document-open en Gtk) y stageado a `main/build/runtimes/linux-x64/native/` +
  `AddIns/VersionControl/net10.0/` (puntero del submódulo no cambia).
- Evidencia semántica in-process: 167 referencias vía el manager reescrito (class outline →
  `GetSemanticModelAsync`), 0 fatals.

### T5 — NuGet 5.4.0 → 6.11.2
- `main/Directory.Build.props` → `<NuGetVersionNuGet>6.11.2</NuGetVersionNuGet>`; 10 ficheros
  adaptados a la API 6.11.2 (PackageManagement, etc.). Bug Roslyn-10 CS0534/CS0114 documentado
  (no bloqueo).

### T6 — Mono.Cairo → retiro según función (ámbito MIG)
- `HslColor.ToRgb` → public; `RtfWriter` a `List<HslColor>`; overload Cairo muerto de
  `SignatureMarkupCreator` (×2 addins); `MonoDevelopHexEditorStyle` a HslColor; 6 `using Cairo;`
  muertos borrados en core.
- Referencia `Mono.Cairo` retirada de `MonoDevelop.HexEditor.csproj`, `CSharpBinding.csproj`
  (Gtk) y `MonoDevelop.Avalonia.CSharpBinding/CSharpBinding.csproj`. Bloqueador CS0012 resuelto:
  `using MonoDevelop.Components;` innecesario en 4 panel-files forzaba cargar firmas de
  extension methods Cairo del namespace (HelperMethods) → se borra el using y se cualifica
  `MonoDevelop.Components.Control` en `CreatePanelWidget()` (conversión implícita desde
  `Gtk.Widget`).
- Residual G1 (core `MonoDevelop.Ide.csproj` + 14 addins Gtk de renderizado) → T7.

### Validación — Tester QA Senior (subagente, AGENTS.md §18.5)
8 partes encadenadas con informe persistente (`~/opencode/qa_wave4/`), todas **APROBADAS**:
- Q1 gate `DebugLinux` 0 Errores · Q2 smoke Gtk `--old-gui` (QaB4 sol+doc, fatals=0, errors=0)
  · Q3 smoke Avalonia `--sln=QaB4` (solución carga de verdad, fatals=0) · Q4 WPF 5/5 en estado
  documentado · Q5 Mono.Posix (builds 0 err, 0 usos activos de Mono.Posix) · Q6 NuGet 6.11.2
  (PM 0 err, `--no-incremental` 0 err) · Q7 Cairo (3 addins sin Mono.Cairo, output sin dll,
  MonoRoslynCompat normalizado md5 `da0c1292…`) · Q8 estado repos (3 submódulos 1 fichero c/u,
  sin secretos ni artefactos).
- Hallazgos del QA (todos resueltos antes del cierre): (1) smoke Avalonia usaba argumento
  posicional en vez de `--sln=<path>` (defecto del script; en modo Avalonia la solución solo se
  abre con `--sln=` — `Program.cs:172-176`); (2) estado rancio de `vs-editor-api` (faltaba
  `bin/Editor/Text/Def/TextData/DebugLinux` → CS0234 en MonoRoslynCompat al rebuild forzado;
  el proyecto `TextData` SÍ está en Main.sln y un build limpio lo regenera — nota T9);
  (3) copia rancia de `MonoRoslynCompat.dll` en `main/build/` (normalizada al DebugLinux).
- Nota QA: el lock MSB4018 transitorio de `MonoDevelop.AvaloniaShell.runtimeconfig.json` (carrera
  entre TFMs net10.0/net10.0-windows sobre `main/build/`) → si reaparece, relanzar el gate.

### Qué queda pendiente
- T7 (bloqueado): Gtk# 3.x — falta binding gtk-sharp 3.x; retira el residual G1 de Mono.Cairo.
- T8: reimplementar funcionalidades quitadas (auditoría T3, 68 items).
- T9: higiene de build — regeneración de outputs de `vs-editor-api` desde cero; carrera de
  runtimeconfig entre TFMs.
- Submódulos commiteados/pushed en su rama `net10` (forks DRavainera): debugger-libs,
  mono-addins, xwt; punteros actualizados en este commit.
