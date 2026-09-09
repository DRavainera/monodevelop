# Bloque C — Detección de runtime y rutas/ejecución Mono

## Objetivo
En el **core** (`MonoDevelop.Core`), alinear la detección y el modelado de runtime con el hecho de que MonoDevelop ya no corre bajo `mono/net462`, sino bajo el **runtime compartido de .NET (net8.0)**. Las comprobaciones `Type.GetType("Mono.Runtime")` / `Type.GetType("System.MonoType")` deben evaluar a `false` (ramas `else` correctas), y el runtime en ejecución debe **registrarse** de forma cross-platform para que `SystemAssemblyService` no falle al arrancar.

- Quedan **diferidos a "Interfaz"/siguientes bloques**: los archivos de addins (PackageManagement, AspNetCore, LinuxDeploy, UIThreadMonitor, compiladores ILAsm/CSharp, templates ASP.NET) y la lógica legacy de ejecución/run (MonoExecutionParameters, MD1DotNetProjectHandler). Las clases Mono (`MonoRuntimeInfo`/`MonoTargetRuntime`/`MonoPlatformExecutionHandler`/`MonoTargetRuntimeFactory`) se **mantienen como dead-code gestionado** (referenciadas por ~30 archivos; no removibles sin cascada).

## Criterio de terminado (núcleo del Bloque C, dentro de MonoDevelop.Core)
- En arranque, `SystemAssemblyService.Initialize()` registra al menos un `TargetRuntime` con `IsRunning == true` en **cualquier plataforma** (ya no `LogFatalError("Could not create runtime info for current runtime")`).
- `Runtime.cs:LoadMSBuildLibraries` obtiene `Microsoft.Build*.dll` del MSBuild del SDK de .NET.
- Sin invocación del binario `mono`/`mono64` desde el core (se usa `dotnet`).
- El core compila con Roslyn SDK contra net8.0 (validación dirigida, no `dotnet build` de proyecto — bloqueos de infraestructura preexistentes).

## Hallazgo clave (motiva C3)
En .NET 8/Linux, `InitializeRuntimes` no encontraba **ningún** runtime:
- `MonoTargetRuntimeFactory.CreateRuntimes()`: `MonoRuntimeInfo.FromCurrentRuntime()` → null (no `Mono.Runtime`), y necesita feature `RUNTIME_SELECTOR` o instalación Mono → no produce nada.
- `MsNetTargetRuntimeFactory.CreateRuntimes()`: `if (!Platform.IsWindows) yield break;` → no produce nada en Linux/Mac.

Resultado: `CurrentRuntime == null` → `LogFatalError`. Además, `MsNetTargetRuntime` modela el **.NET Framework de Windows** (registry, `winDir`, `GetProgramFilesX86`, GAC, `MSBuildLocator` VS + `Registry`), inadecuado para .NET 8 cross-platform.

## Decisiones por subbloque

### C1 — `Type.GetType("Mono.Runtime")` (verificación; correctas en .NET 8) — ✔ COMPLETO
Los 6 sitios del core (`Platform.cs` `GetMonoDisplayName`/`IsMonoRuntime`/`GetRuntimeDescription`, `Runtime.cs:507`, `MonoRuntimeInfo.FromCurrentRuntime`, `MsNetTargetRuntimeFactory`, `GLibLogging` en Ide) evalúan `null` en .NET 8 y toman la rama `else`/false correcta (`.NET`). **No requieren edición** — son comprobaciones de runtime genuinas y la rama .NET es la correcta. `Platform.IsMonoRuntime` devolverá `false`, correcto.

### C2 — `Type.GetType("System.MonoType")` (ConsoleCrayon.cs) — ✔ COMPLETO (verificación)
`ConsoleCrayon.cs:216` usa el check para elegir color en consola; en .NET 8 evalúa `null` → `runtime_is_mono=false` (usa la ruta de color .NET). Correcto; sin edición.

### C3 — Runtime en ejecución .NET 8 (rework) — ✔ COMPLETO
**Decisión del usuario: "Rework net8 del runtime"** — registrar el runtime .NET actual cross-platform; mantener Mono* como dead-code gestionado (sin eliminar).

1. **Nuevo `MonoDevelop.Core.Assemblies/DotNetTargetRuntime.cs`**: representa el runtime .NET en ejecución.
   - `IsRunning => true` (siempre; la app corre sobre el runtime compartido).
   - `RuntimeId => ".NET"`, `Version => Environment.Version`, `DisplayRuntimeName => "Microsoft .NET (Core)"`.
   - `GetMSBuildBinPath(toolsVersion)`/`GetMSBuildToolsPath` → dir del **SDK de .NET** que contiene `MSBuild.dll` (`$DOTNET_ROOT/sdk/<version>/`), sin nuevas dependencias. **IMPORTANTE**: `MSBuildLocator.QueryDotNetSdk()` **no existe en 1.1.2** (verificado por reflexión: solo `QueryVisualStudioInstances`/`RegisterDefaults`/`RegisterInstance`/`RegisterMSBuildPath`) → NO se usa; se resuelve por filesystem (`OrderByDescending` directorios `sdk/<v>` con `MSBuild.dll`) + fallback `dotnet --version`.
   - `GetMSBuildExtensionsPath` → `$DOTNET_ROOT/sdk` (con guard null).
   - `GetGacDirectories` → vacío (.NET Core no tiene GAC).
   - `GetExecutionHandler` → `NativePlatformExecutionHandler`.
   - `CreateBackend` → `null` → `NotSupportedFrameworkBackend` (manejo de target-framework .NET pendiente; diferido). `OnInitialize` → vacío.
   - `GetAssemblyDebugInfoFile` → `.pdb`.
2. **`MsNetTargetRuntimeFactory.CreateRuntimes()`**: ahora siempre produce `new DotNetTargetRuntime()` (el runtime en ejecución) en **cualquier plataforma**; conserva el yield de `MsNetTargetRuntime(false)` solo en Windows (targeting .NET Framework). Esto registra `IsRunning` → `DefaultRuntime`/`CurrentRuntime`, eliminando el `LogFatalError`.
3. **csproj**: añadido `<Compile Include="...DotNetTargetRuntime.cs" />`.
4. **Validación**: Roslyn net8 dirigido (`DotNetTargetRuntime.cs` + `MsNetTargetRuntimeFactory.cs`) contra stubs fieles del contrato `TargetRuntime` → **0 errores**.

### C4 — Invocación del binario `mono`/`mono64` (Core) — ✔ COMPLETO (la parte de Core)
- `MonoDevelop.Projects.MSBuild/MSBuildProcessService.cs` (Core): `StartMSBuild` en no-Windows invocaba `Path.Combine(monoRuntime.Prefix, "bin", "mono64")` vía `GetMonoPath()` (cast `DefaultRuntime as MonoTargetRuntime` → **null en .NET 8** → NRE). Ahora usa `dotnet <MSBuild.dll>` y se elimina `GetMonoPath()`. Se valida contra stubs → 0 errores.
- Resto de C4 (`MonoDevelopPluginFactory`, `DotNetCoreDevCertsTool`, `LinuxDeployExtension`, `UIThreadMonitor`, compiladores) → **addins/Ide, diferido a Interfaz**.

### C5 — Variables de entorno `MONO_*` (Core) — ✔ auditado; diferido el resto
- `MonoTargetRuntime.GetToolsExecutionEnvironment`/`MonoRuntimeInfo` establecen `MONO_*` (dead-code Mono, no se usan con el nuevo `DotNetTargetRuntime`).
- `MonoExecutionParameters.cs` (Core): genera `MONO_*` env + args `mono` desde opciones de run. Es **data/serialización de UI** (referenciado por configuraciones de run en Ide) → se deja como dead-code gestionado, diferido al rework de run/Interfaz.
- `MD1DotNetProjectHandler.cs` (Core): `MONO_IOMAP="drive"` solo en proyectos MD1 legacy → dead-code gestionado, diferido.
- Compiladores/addins `MONO_PATH`/`XBUILD_*` → diferidos a Interfaz.

## Orden de ejecución
1. C1/C2 verificación ✔
2. C3 `DotNetTargetRuntime` + factory rework + csproj ✔ (validado Roslyn net8: 0 errores)
3. C4 Core `MSBuildProcessService` mono64→dotnet ✔ (validado Roslyn net8: 0 errores)
4. C5 audit + diferir ✔

## Resultado
En .NET 8, `SystemAssemblyService.Initialize()` registra `DotNetTargetRuntime` (IsRunning) como runtime por defecto en cualquier plataforma → desaparece el `LogFatalError`. El MSBuild local del core se invoca vía `dotnet`, no `mono64`. Los tipos Mono siguen compilando (dead-code gestionado) sin romper la cascada de ~30 referencias.

## Pendiente (diferido)
- **Interfaz / siguientes bloques**: rework completo del run/execution (`MonoExecutionParameters`, run handlers), target-framework handling .NET (.NETCoreApp reference packs), addins que invocan `mono` (`MonoDevelopPluginFactory`, `DotNetCoreDevCertsTool`, `LinuxDeployExtension`, `UIThreadMonitor`), compiladores (ILAsm/CSharp `MONO_*`), templates ASP.NET, `ConsoleCrayon`/Ide logging (C2 restante).

## Status log
- C1/C2: verificados (ramas else correctas en .NET 8). C3: `DotNetTargetRuntime` + factory cross-platform + csproj, validado Roslyn net8 (0 errores). C4 (Core): `MSBuildProcessService`→`dotnet`, validado (0 errores). C5: audit + diferido.