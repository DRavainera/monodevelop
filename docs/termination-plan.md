# Plan maestro de la parte "TERMINACIÓN"

> Documento guía de la parte **Terminación**. Debe leerse junto con `migration-plan.md`,
> `migration-execution-plan.md`, `migration-phase-3-5-execution-plan.md`,
> `migration-status-report.md`, `security-audit-report.md` y `ui-technology-proposal.md`.

## 1. Objetivo

Reemplazar **todo** componente legacy de **Mono** (dependencias y librerías de Mono, tanto de
compilación como de ejecución) por **equivalentes .NET 8**, de modo que el proyecto ya **no
necesite el SDK Mono** para compilar ni ejecutar — solo el SDK .NET 8.

**Excepciones provisionales (se conservan hasta la parte "Interfaz"):**
- La **UI legacy** Gtk#/Mono (libstetic, libsteticui, Xwt.Gtk, Mono.Addins.Gui, gnome-sharp, etc.) y,
  por tanto, las dependencias Mono que arrastra a sus proyectos UI (gtk-sharp, gdk-sharp, glib-sharp,
  pango-sharp, atk-sharp, Mono.Posix/Mono.Cairo en esos proyectos UI).
- **`ICSharpCode.NRefactory.Cecil`** (capa provisional de compatibilidad del análisis sintáctico/tipo),
  que se reemplazará en la parte "Interfaz" junto con la UI.

El **criterio de terminado** de esta parte:
- Toda dependencia Mono (excepto UI y `ICSharpCode.NRefactory.Cecil`) reemplazada y removida.
- Todas las pruebas dan **OK** y el programa **se ejecuta** (se abre y se ve la ventana, y es utilizable).
- Solo se necesita el SDK .NET 8 para compilar y ejecutar.

## 2. Principios de ejecución

1. **Un único ítem a la vez**, cada uno con su **plan individual con fases**.
2. **Primero buscar en Internet** si existe una extensión/framework .NET 8 (o conjunto) que reemplace
   el componente legacy. Si existe, adaptar el código y **implementar el reemplazo**.
3. Si **no existe** reemplazo directo, resolver por una de estas vías en orden de preferencia:
   - **(1)** **Forkear** el componente legacy Mono (si su licencia lo permite), modificarlo para
     separarlo de Mono e integrarlo como **módulo interno** con soporte .NET 8.
   - **(2)** Si no se puede forkear (licencia u otro problema), **construir desde 0** un componente de
     reemplazo en .NET 8 funcionalmente compatible (mismos comportamientos y **mismos nombres de
     funciones**) e integrarlo como **módulo interno**.
4. Los cambios **no deben romper la aplicación**; cada fase cierra con compilación y evidencia de pruebas.
5. Respetar **siempre** las medidas de la parte **Seguridad** (remoting endurecido: IPC 0600, TCP
   loopback + `rejectRemoteRequests`, transporte textual + `Activator.GetObject`, binder allowlist,
   canal AutoTest opt-in `MONO_AUTOTEST_ENABLE=1`; TLS SHA-256 thumbprint; SHA-256 autosave; Gravatar
   off; NuGetAudit; `.snk` solo-clave-pública; feeds HTTPS-only).
6. **Documentar todo** (cada plan y su progreso) para que lo lean otros agentes.

## 3. Decisión sobre la UI

La UI queda **desacoplada** con compatibilidad provisional Gtk#/Mono y con
`ICSharpCode.NRefactory.Cecil` como dependencia provisional ligada a la UI. Esto permite hacer las
pruebas finales de la parte Terminación sobre esa UI provisional y dejarla **preparada** para
reemplazarla por Avalonia UI (opción recomendada) en la parte **"Interfaz"**.

## 4. Inventario base de dependencias Mono a reemplazar

Fuente: inventario técnico realizado al inicio de Terminación (referencias en csproj/fsproj,
namespaces `Mono.*`, invocaciones del binario `mono`, variables `MONO_*`, detección de runtime).

### Bloque A — Runtime/acciones núcleo (crítico en .NET 8)
- **A1. `Mono.Unix` / `Mono.Unix.Native`** (~20 archivos en core + addins): `Syscall.chmod`,
  `Syscall.kill`, `Syscall.open`, `Syscall.dup2`, `Syscall.symlink`, `FilePermissions`,
  `FileUserInfo`, `Syscall.WIFSIGNALED`, `Syscall.WTERMSIG`, `Mono.Unix.UnixSignal`, etc.
  Archivos core: `FileService.cs`, `LoggingService.cs`, `Gettext.cs`, `RemotingService.cs`,
  `ProjectService.cs`, `TextFile.cs`, `PlatformService.cs`, `IdeInstanceConnection.cs`,
  `DockItem.cs`, `IdeStartup.cs`, `ProcessService.cs`, `SampleProfiler.cs`. Addins:
  `MonoDevelop.Autotools/SolutionDeployer.cs`, `AspNetCore DevCertInstaller`, `GdbSession.cs`,
  `UIThreadMonitorDaemon`, `Deployment/*`, `SourceEditor2 ModeHelpWindow`, etc.
- **A2. `System.Runtime.Remoting`** (subsistema IPC completo): `RemotingService.cs`,
  `ProcessHostController.cs`, `RemoteProcessObject.cs`, `DisposerFormatterSink.cs`,
  `InstrumentationService.cs`, `Counter.cs`, `RemoteLogger.cs`, `ProgressStatusMonitor.cs`,
  `mdhost.cs`, `TextTemplatingFileGenerator.cs` (`CallContext.LogicalSetData`),
  `ILogWriter.cs`, `BuildEngine.Shared.cs`, `MonoDevelop.Projects.MSBuild/Main.cs`,
  `AutoTest*`, `SyncContext*.cs`, `IToolboxLoader.cs`, `SolutionItemTypeNode.cs`
  (`Remoting.Messaging`).
- **A3. `Mono.Remoting.Channels.Unix`** (`UnixChannel`, 3 archivos en GtkCore + mdhost):
  `Application.cs`, `ApplicationBackend.cs`, `mdhost.cs`. **Sin equivalente .NET 8** → depende de A2
  (reemplazo del transporte remoting por Named Pipes / Unix Domain Sockets).
- **A4. `Mono.CSharp`** (compilador embebido): `MonoDevelop.CSharp.Parser/McsParser.cs`.
- **A5. `Mono.Cecil`** (usos en `main/src/`, aparte de `ICSharpCode.NRefactory.Cecil` provisional):
  `CompiledAssemblyProject.cs`, `CecilTypeResolver.cs`, `CecilToolboxItemLoader.cs`,
  `SoftDebuggerEngine.cs`. Ya disponible como **NuGet `Mono.Cecil`** (netstandard) — compatible.
- **A6. `Mono.TextTemplating`** (PackageReference 1.3.1 en `MonoDevelop.TextTemplating.csproj`):
  verificar compatibilidad .NET 8 y/o alternativa.
- **A7. `Mono.Security`** (referencia en `MonoDevelop.Core.csproj`): evaluar si los usos reales
  desaparecen con la migración (es un residuo de la capa de certificados de F2.2).

### Bloque B — Librerías .NET Framework que cambian en .NET 8 (NuGet equivalentes)
- **B1. `System.Web`** (referencia en 7 csproj; `HttpUtility` en ~5 archivos de código).
- **B2. `System.ServiceModel`** (WCF; 4 csproj + 5 archivos de código).
- **B3. `System.Configuration`** (6 csproj; `ConfigurationManager.AppSettings` en `PropertyService.cs`).
- **B4. `Microsoft.CSharp`** (7 csproj).
> **Estado (Ver `termination-B-plan.md`)**: el subbloque **Core** está **COMPLETO** — quitadas las 4 refs
> de `MonoDevelop.Core.csproj`; `System.ServiceModel` (STS/WIF) neutralizado en el core y `System.Configuration`
> eliminado; validado por compilación dirigida Roslyn net8 (Web folder, 0 errores). El resto de csproj
> (Ide, AspNet, DesignerSupport, MacPlatform, WebReferences, RegexToolkit, PackageManagement, DotNetCore,
> UnitTesting, CSharpBinding) queda **diferido a "Interfaz"/siguientes bloques**.

### Bloque C — Detección de runtime y rutas/ejecución Mono  »  **EN CURSO** (detalle: `termination-C-plan.md`)
- **C1. `Type.GetType("Mono.Runtime")`** (10 archivos): `Platform.cs`, `Runtime.cs`, `MonoRuntimeInfo.cs`,
  `MsNetTargetRuntimeFactory.cs`, `GLibLogging.cs`, `SyncContextAttribute.cs`,
  `MonoDevelop.GtkCore/AssemblyResolver.cs`, templates ASP.NET. En .NET 8 debe evaluarse a `false`
  (revisar ramas "else"). — **[CORE] ✔ auditado** (Core): `Platform`, `Runtime.cs:507`,
  `MonoRuntimeInfo.cs:176`, `MsNetTargetRuntimeFactory.cs:39` toman rama .NET correcta; el resto (GtkCore,
  templates, GLibLogging/Ide) diferido a Interfaz.
- **C2. `Type.GetType("System.MonoType")`**: `ConsoleCrayon.cs`. — ✔ verificado (ramas false correctas).
- **C3. Clases de gestión de runtime Mono**: `MonoRuntimeInfo`, `MonoTargetRuntime`,
  `MonoPlatformExecutionHandler` (rutas `/lib/mono`, binarios `mono`/`mono64`/`mono32`) → eliminar o
  dejar como dead-code gestionado; sustituir por rutas .NET (`Microsoft.NETCore.App`).
  — **[CORE] ✔**: nuevo `DotNetTargetRuntime` (cross-platform, `IsRunning` siempre) + `MsNetTargetRuntimeFactory`
  reescrito (siempre el runtime .NET; `MsNetTargetRuntime` solo en Windows). Csproj con `<Compile>`.
  Validado Roslyn net8 (0 errores). `Mono*` se mantienen como dead-code gestionado (decisión usuarix).
- **C4. Invocación del binario `mono`/`mono64`**: `UIThreadMonitor.cs`, `LinuxDeployExtension.cs`,
  `MonoDevelopPluginFactory.cs`, `DotNetCoreDevCertsTool.cs`, `MSBuildProcessService.cs`.
  — **[CORE] ✔**: `MSBuildProcessService.StartMSBuild` en no-Windows usa `dotnet`, eliminado `GetMonoPath`
  (mono64→NRE con `DotNetTargetRuntime`). Validado Roslyn net8 (0 errores). El resto → addins/Ide, diferido.
- **C5. Variables de entorno Mono (`MONO_GAC_PREFIX`, `MONO_PATH`, `MONO_IOMAP`, `MONO_LOG_LEVEL`,
  `XBUILD_FRAMEWORK_FOLDERS_PATH`)**: `ILAsmCompilerManager.cs`, `CSharpBindingCompilerManager.cs`,
  `MD1DotNetProjectHandler.cs`, `MonoRuntimeInfo.cs`, `MonoExecutionParameters.cs`. — ✔ [CORE] auditado:
  `MonoExecutionParameters`/`MonoRuntimeInfo`/`MD1DotNetProjectHandler` = dead-code gestionado (Mono),
  diferido al rework de run/Interfaz.

### Bloque D — Pendientes de la parte Seguridad que se atienden en Terminación  »  **DIFERIDO** (detalle: `termination-D-plan.md`)
Los tres ítems viven en **submódulos `mono/*` o un addin**, y dependen de **upstream** — no son accionables en el
núcleo de este fork. Se documentan con dueño y paso de cierre; **no** se introducen binarios/firmas ajenos.

- **D1. Bump `NuGet.Client` → ≥5.11.6** (CVE-2024-0057): `main/external/nuget-binary` es **submódulo**
  (`mono/nuget-binary`, HEAD 2018 con NuGet **4.9.1**, ni siquiera 5.4.0) consumido por el addin
  `MonoDevelop.PackageManagement`. El bump requiere que **upstream** publique/pin binarios ≥5.11.6 y validar
  en net472. → **DEFERRED** (dueño: Interfaz/aporte upstream).
- **D2. `BinaryFormatter` no-remoting residual**: `xwt/TransferDataSource.cs:150,164` (submódulo, portapapeles
  de UI → alcance Interfaz) y `guiunit/BinarySerializableConstraint.cs:38` (submódulo, tests). → **DEFERRED**
  (dueño: Interfaz/upstream `mono/xwt`, `mono/guiunit`).
- **D3. Verificación de firma/checksum del `.mpack`**: vive en `Mono.Addins.Setup` (submódulo `mono/mono-addins`);
  el canal ya es HTTPS-only; la validación criptográfica es competencia de upstream. → **DEFERRED** (dueño:
  upstream `mono/mono-addins`/Interfaz).

### Bloque E — Build/scripts que invocan `mono`
- **E1. `scripts/configure.*`**: ya migrado a `configure.net8.csproj` (parte Migración). Verificar que
  no queden invocaciones a `mono`/`mcs`.
- **E2. Scripts de submodulos externos** (`external/fsharpbinding/build.sh`,
  `external/vs-editor-api/*`, `external/mono-tools/*`, `build/MacOSX/*`): fuera del alcance Linux-first
  (macOS/tools de muestra); se documentan, no se mantienen.

## 5. Criterio de prioridad y orden sugerido

1. **Bloque A** (crítico en runtime .NET 8) — empezar por **A1 (Mono.Unix)**, el más tangible y con
   reemplazo directo conocido (ver §6).
2. **Bloque B** (referencias de framework — reemplazo de bajo riesgo por NuGet).
3. **Bloque C** (detección/rutas Mono — dead-code y condicionales).
4. **Bloque D** (seguridad deuda).
5. **Bloque A2/A3 (IPC/remoting)** — el **más grande y sensible**; se decide su momento en el plan
   individual (ver §A2). Se prioriza tras despejar el resto para minimizar riesgo, pero su enunciado
   de diseño se documenta ya.

## 6. Investigación de reemplazos .NET 8 (estado inicial)

| Componente | Opción de reemplazo .NET 8 | Estado |
|---|---|---|
| `Mono.Unix`/`Mono.Unix.Native` (**A1**) | **`Mono.Posix.NETStandard`** (NuGet, netstandard2.0, MIT; repo `mono/mono.posix`, que supersede el código legacy, P/Invoke POSIX). Mantiene el namespace `Mono.Unix` → mínimos cambios de código. Alternativas BCL: `UnixFileMode`/`RandomAccess` (.NET 7+), `Process`/`FileStream` para kill/open | Investigar compatibilidad exacta (Syscall.*, FilePermissions, FileUserInfo, UnixSignal) |
| `System.Runtime.Remoting` (**A2**) | (a) **`Net4x.Runtime.Remoting`** (MIT; capa de compatibilidad .NET 8/10 que mantiene API `ChannelServices`/`TcpServerChannel`/`MarshalByRefObject` y reemplaza transporte+serializador sin `BinaryFormatter`). (b) `Menees.Remoting` / `Remoting-Replacement` (RPC transparente estilo .NET Remoting, MIT). (c) **gRPC** (Apache-2.0) sobre Named Pipes / Unix Domain Sockets / loopback HTTP — más invasivo (exige declarar APIs) | Elegir en plan A2. La opción (a) preserva la API y es coherente con C8/C9/C10 (transporte textual/Activator). Debe respetar C1–C7 (loopback, 0600, allowlist, sin BinaryFormatter) |
| `Mono.Remoting.Channels.Unix` (`UnixChannel`) (**A3**) | No tiene equivalente directo .NET 8. Depende de A2 (usar Named Pipes / Unix Domain Sockets con la capa de compatibilidad) | Se resuelve dentro de A2 |
| `Mono.CSharp`/`McsParser` (**A4**) | **`Microsoft.CodeAnalysis.CSharp` (Roslyn)** (MIT): reescribir `McsParser` sobre las syntax/semantics APIs. Microsoft ya recomienda Roslyn para reemplazar el compilador Mono | Investigar superficie de `McsParser` |
| `Mono.Cecil` (**A5**) | **`Mono.Cecil`** NuGet (netstandard) — compatible con .NET 8 | Confirmar paquete/versión |
| `Mono.TextTemplating` (**A6**) | Paquete NuGet `Mono.TextTemplating`; verificar net8 o **`Microsoft.VisualStudio.TextTemplating`**/`dotnet-t4` | Verificar en plan A6 |
| `System.Web` (**B1**) | **`System.Net.WebUtility`** (BCL) para URL/HTML; o paquete **`System.Web.HttpUtility`** cuando requiera `ParseQueryString` | Investigar casos |
| `System.ServiceModel` (**B2**) | Paquete **`System.ServiceModel.*`** (CoreWCF para servidor, o remover si solo cliente) | Investigar casos |
| `System.Configuration` (**B3**) | Paquete **`System.Configuration.ConfigurationManager`** | Directo |
| `Microsoft.CSharp` (**B4**) | Paquete **`Microsoft.CSharp`** | Directo |
| `Type.GetType("Mono.Runtime")` (**C1**), `System.MonoType` (**C2**) | Evaluar `false` en .NET 8; reemplazar por detección de framework (.NET) en `Platform` | Simple |
| rutas/binario `mono` (**C3–C4**) | Sustituir por rutas del SDK .NET (`dotnet`, `Microsoft.NETCore.App`, `DOTNET_ROOT`) | Según caso |
| `NuGet.Client` (**D1**) | bump a ≥5.11.6 (reemplazo de binarios vendidos) | Pendiente |
| `BinaryFormatter` no-remoting (**D2**) | JSON (Newtonsoft) con fallback, o eliminar | Pendiente |
| firma addin (**D3**) | validación de firma/checksum del `.mpack` | Pendiente |

> Nota metodológica: cada bloque de la tabla se materializa en un **plan individual** (archivo
> `termination-<id>-plan.md`) con sus fases, evidencia y documentación, siguiendo la regla
> "1 problema a la vez". Este maestro se actualiza a medida que cierran los planes.