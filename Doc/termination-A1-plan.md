# Plan individual del Bloque A1 — `Mono.Unix` / `Mono.Posix` en Core

> Sub-plan de la parte Terminación (ver `termination-plan.md`). Decisiones del usuario:
> **Estrategia 2 (Módulo interno .NET 8)** — eliminar `Mono.Posix` por completo del núcleo,
> reemplazándolo por BCL .NET 8 + helpers P/Invoke internos y un lector gettext `.mo` interno.

## Objetivo

Eliminar de **core** la referencia a `Mono.Posix` (hoy GAC `/usr/lib/mono/4.5/Mono.Posix.dll`) y todos
los usos de `Mono.Unix` / `Mono.Unix.Native`, sustituyéndolos por equivalentes **nativos .NET 8**
(BCL) y un **módulo interno** de utilidades POSIX con **los mismos nombres de función** que usa el
código actual, para minimizar cambios de llamada.

**Alcance**: solo `MonoDevelop.Core` (proyecto `MonoDevelop.Core.csproj` y sus fuentes) y los
proyectos de nucleó `MonoDevelop.Ide` que usan `Mono.Unix`/`Mono.Unix.Native` y se compilan con la misma
referencia.

**Fuera de alcance (quedan provisionales)**: la **UI** (libstetic, libsteticui, GtkCore, Autotools Gui,
Deployment Gui, ILAsm Gui, GdbSession UI, SourceEditor2) que usa `Mono.Unix.Catalog.GetString` y que aún
referencia GAC `Mono.Posix`; se reemplazará en la parte "Interfaz". `mdhost.csproj` (uso de
`Syscall.chmod`) es parte del IPC (Bloque A2), no se toca aquí.

## Inventario exacto de usos en core (no-UI)

### A1.1 `Mono.Unix.Native.Stdlib` / `Syscall` (llamadas POSIX)

| Archivo:línea | Llamada actual | Reemplazo .NET 8 |
|---|---|---|
| `MonoDevelop.Core/LoggingService.cs:319-366` | `Syscall.open(logFile, OpenFlags.O_WRONLY|O_CREAT|O_TRUNC, FilePermissions.S_IFREG|S_IRUSR|S_IWUSR|S_IRGRP|S_IWGRP)`, `Stdlib.GetLastError()`, `Syscall.dup2(fd, STDOUT_FILENO)`, `Syscall.dup2(fd, STDERR_FILENO)`, `Syscall.close(fd)`, `Syscall.unlink(to)`, `Syscall.symlink(from, to)` | `FileStream` con modo append + P/Invoke `dup2`/`close` para rebindear stdout/stderr; `File.CreateSymbolicLink` (.NET 6+) para symlink; `File.Delete` para unlink |
| `MonoDevelop.Core/FileService.cs:712-728` | `Stdlib.rename(source, dest)`, `Stdlib.GetLastError()` | `File.Move(source, dest, overwrite:true)` (.NET Core 3.0+) |
| `MonoDevelop.Core/Runtime.cs:486` | `Stdlib.GetLastError()` (tras setprocessname) | helper P/Invoke o no-op |
| `MonoDevelop.Core.Execution/ProcessService.cs:426-427` | `Syscall.WIFSIGNALED(code)`, `Syscall.WTERMSIG(code)` | helper interno: `(code & 0x7f) < 0x7f && (code & 0x7f) != 0` para señalado; `WTERMSIG = code & 0x7f` |
| `MonoDevelop.Ide/IdeStartup.cs:497` | `Syscall.kill(pid, Signum.SIGQUIT)` | P/Invoke `kill(pid, SIGQUIT=3)` o `Process.Kill(entireProcessTree:true)` |
| `MonoDevelop.Core.Utilities/SampleProfiler.cs:59` | `Syscall.kill(pid, Signum.SIGINT)` | P/Invoke `kill(pid, SIGINT=2)` |
| `MonoDevelop.Ide/Ide/RemotingService.cs:88` (parte A1; el resto es A2) | `Syscall.chmod(unixRemotingFile, FilePermissions.S_IRUSR|S_IWUSR)` | `File.SetUnixFileMode(path, UnixFileMode.UserRead|UserWrite)` (.NET 7+) |

### A1.2 `using` sin uso real (solo decorativos) — eliminar import

- `MonoDevelop.Ide/MonoDevelop.Components.Commands/KeyBindingSet.cs:35` (`using Unix = Mono.Unix.Native;` sin usar)
- `MonoDevelop.Ide/MonoDevelop.Components.Commands/KeyBindingService.cs:34` (idem)
- `MonoDevelop.Projects.Text/TextFile.cs:35-36` (`using Mono.Unix;` + `using Mono.Unix.Native;`)
- `MonoDevelop.Ide/MonoDevelop.Ide.Desktop/PlatformService.cs:36` (`using Mono.Unix;`) — los `MimeTypeCatalog`/`GettextCatalog` usados no son `Mono.Unix`
- `MonoDevelop.Projects/ProjectService.cs:43` (`using Mono.Unix;`)
- `MonoDevelop.Ide/IdeInstanceConnection.cs:32` (`using Mono.Unix;`) — verificar

### A1.3 `Mono.Unix.Catalog` (gettext) — el punto delicado

`MonoDevelop.Core/Gettext.cs` (clase `GettextCatalog`):
- `Catalog.Init ("monodevelop", catalog)` (línea 136)
- `Catalog.GetString (phrase)` (línea 161)
- `Catalog.GetPluralString (singular, plural, number)` (línea 204)

`MonoDevelop.Ide/MonoDevelop.Components.Docking/DockItem.cs:575-596`: `Catalog.GetString ("Hide"/"Minimize"/"Dock"/"Undock")` — **UI (dock)** → queda provisional (se deja, no se toca).

> La clase `Mono.Unix.Catalog` solo es usada por `Gettext.cs` en core. Se reemplazará por un **módulo
> interno** (`MonoDevelop.Core.Gettext` `Catalog`-like) que lea el formato **`.mo`** (gettext) de la
> misma ruta, con las mismas llamadas `Init`/`GetString`/`GetPluralString`. Como `Gettext.cs` ya envuelve
> todo bajo `GettextCatalog`, el impacto de llamada es nulo para el resto del código.

## Estrategia (módulo interno; mismos nombres de función)

1. Crear un módulo interno de utilidades POSIX con los **mismos nombres** `Mono.Unix.Native.Syscall`,
   `Stdlib`, `FilePermissions`/`OpenFlags`, `Signum` (namespace interno `Mono.Unix.Native` interno NO;
   mejor un namespace propio `MonoDevelop.Core.Platform`), **o** hacer los reemplazos directos en cada
   llamada con BCL. Se prefiere esto último por ser uninventario pequeño y claro (columnas de la tabla
   A1.1): cambios directos, sin emular la API Mono.

2. `Gettext.cs`: sustituir `Mono.Unix.Catalog` por un lector `.mo` interno (módulo
   `GettextCatalog.Load`) que:
   - apoyado en `MOFileInfo`/parsing binario del header gettext;
   - expone `Init(domain, path)`, `GetString`, `GetPluralString`;
   - cuando no encuentra el catálogo devuelve la cadena original (comportamiento actual).

3. En `MonoDevelop.Core.csproj`: **eliminar** la `<Reference Include="Mono.Posix">` GAC.
   (No añadir `Mono.Posix`; no aplicar `Mono.Posix.NETStandard`.)
   Verificar que no haya otras referencias a `Mono.Posix` en el núcleo.

4. En proyectos de núcleo (`MonoDevelop.Core.csproj`, `MonoDevelop.Ide.csproj` según toque): comprobar
   que `monodoc`/`Mono.Security` no dependan transitivamente de `Mono.Posix` (resolver deuda asociada en
   la misma fase si es trivial, si no dejarla documentada).

## Fases

- **F1. Reemplazo `Mono.Unix.Native` en core** (LoggingService, FileService, Runtime, ProcessService,
  IdeStartup, SampleProfiler) por BCL + P/Invoke mínimos. Eliminar usings decorativos (A1.2).
- **F2. Módulo gettext interno** y reemplazo de `Mono.Unix.Catalog` en `Gettext.cs`.
- **F3. Limpieza csproj core**: quitar `Mono.Posix` GAC; revisar transiciones relacionadas.
- **F4. Compilación de verificación** (net8 vía `dotnet build -p:TargetFramework=net8.0`) y probes
  dirigidas de las rutas modificadas; sin romper pruebas.

## Criterio de terminado de A1

- `MonoDevelop.Core` y núcleo ya no referencian ni usan `Mono.Posix`/`Mono.Unix`.
- Solo la UI provisional conserva `Mono.Unix.Catalog` (no se toca).
- `dotnet build -p:TargetFramework=net8.0` OK y pruebas OK en las zonas tocadas.
## Estado (registro de avances)

- **F1 ✔** — Creado `MonoDevelop.Core/Posix.cs` (P/Invoke libc: `Open/Close/Dup2/Unlink/Symlink/Kill/Rename/GetLastError`, constantes errno, flags/modo, SIGINT/SIGQUIT, macros `WIFSIGNALED`/`WTERMSIG`). Reemplazados usos de `Mono.Unix.Native` en `LoggingService` (open/dup2/close/unlink/symlink), `FileService` (`Rename`+`GetLastError`+errno), `Runtime` (`GetLastError` de prctl), `ProcessService` (`WIFSIGNALED`/`WTERMSIG`), `SampleProfiler` (`Kill` SIGINT), `IdeStartup` (`Kill` SIGQUIT). Eliminados usings decorativos `Mono.Unix` en `Gettext.cs`, `TextFile.cs`, `IdeInstanceConnection.cs`, `PlatformService.cs`, `ProjectService.cs`, `KeyBindingSet.cs`, `KeyBindingService.cs`.
  - Semántica verificada contra el GAC `Mono.Posix.dll` (monodis): órdenes de parámetros y valores errno idénticos (`EPERM=1`, `ENOENT=2`, `EACCES=13`, `EXDEV`, `EEXIST=17`, `ENOTDIR=20`, `EINVAL=22`, `ENAMETOOLONG=36`); `syscall.symlink(oldpath,newpath)` es orden C → `Posix.Symlink(target=oldpath, linkpath=newpath)` crea el enlace `newpath`→`oldpath`, idéntico al original.
- **F2 ✔** — Creado `MonoDevelop.Core/Catalog.cs` (lector `.mo` gettext interno, little/big endian: header completo magic/revision/N/O/T/hash*2, tablas (len,off) por seek, `Init/GetString/GetPluralString`, evaluador de plural minimalista con fallback seguro). Creado `MonoDevelop.Core/Unix.Catalog.cs` (facade en namespace `Mono.Unix`, assembly MonoDevelop.Core) que reenvía a `MonoDevelop.Core.Catalog`, para que los `global::Mono.Unix.Catalog.GetString` de MonoDevelop.Ide/Gui (UI provisional) sigan compilando sin Mono.Posix. `Gettext.cs` sin `using Mono.Unix` ahora resuelve a `MonoDevelop.Core.Catalog`.
  - Correcciones aplicadas: lectura del header gettext lee los **7** campos (hash table incluida) y hace **seek** a O y T (antes leía en posición continuada → bug); eliminado `CultureInfo.IsInvariantCulture` (no existe) sustituido por `culture.Equals(CultureInfo.InvariantCulture)`; eliminadas variables muertas (`ready`, `stringCache`, campo `messageList`).
- **F3 ✔** — Eliminada la `<Reference Include="Mono.Posix">` GAC de `MonoDevelop.Core.csproj`. Verificado que solo `MonoDevelop.Core.csproj` referencia Mono.Posix; MonoDevelop.Ide lo obtenía transitivamente.
- **F4 (parcial)** — `dotnet build` de proyecto bloqueado por infraestructura PREEXISTENTE (no por A1): MDBuildTasks net472-only para net8 (`NETSDK1005`); mono-addins Setup roto para net472 (CS0012/CS1579). **Validación dirigida** usada en su lugar: `Catalog.cs`, `Posix.cs` y `Unix.Catalog.cs` compilan con Roslyn SDK contra net8.0 (0 errores, 0 warnings). Call sites cruzados contra la API `Posix` — todos los símbolos existen y los órdenes coinciden.

### Pendiente A1
- Terminar F4: si se consigue un build de verificación del núcleo por otra vía, confirmar rutas modificadas; el build normal sigue bloqueado por la deuda de infraestructura (documentada en `termination-plan.md`).

### Addendum A1 (Cierre de alcance core no-UI)

Tras quitar `Mono.Posix` de `MonoDevelop.Core.csproj`, se detectaron 3 usos más de tipos `Mono.Unix`
en core **no-UI** que rompían la compilación y se resolvieron en A1:

| Archivo:línea | Uso anterior | Reemplazo |
|---|---|---|
| `MonoDevelop.Core.Text/TextFileUtility.cs:304` (`CreateStream`) | `new Mono.Unix.StdioFileStream (tmpPath, FileMode.CreateNew, FileAccess.Write)` | `new FileStream (tmpPath, FileMode.CreateNew, FileAccess.Write)` (BCL; mismo semántica de creación exclusiva) |
| `MonoDevelop.Projects/ProjectService.cs:454` (`GetTargetFile`) | `UnixSymbolicLinkInfo fi = new UnixSymbolicLinkInfo (file); if (fi.IsSymbolicLink) return fi.ContentsPath;` | `var target = MonoDevelop.Core.Posix.ReadLink (file); if (target != null) return target;` |
| `MonoDevelop.Ide.Gui/FileOpenInformation.cs:140` (`ResolveSymbolicLink`) | `new Mono.Unix.UnixSymbolicLinkInfo (fileName); linkInfo.IsSymbolicLink && linkInfo.HasContents; linkInfo.ContentsPath` | resolución por `Posix.ReadLink(fileName)` devolviendo target o null; mismo bucle de ciclo de links |

Para esto se añadió a `Posix.cs` el P/Invoke `readlink` y el helper público
`string Posix.ReadLink (string path)` (crece buffer hasta 1 MiB; devuelve el target crudo que puede
ser relativo, o `null` si no es symlink). `FilePath` tiene conversión implícita `string`↔`FilePath`
(`FilePath.cs:384,389`), por lo que las llamadas funcionan sin casts adicionales.

**Verificación**: `Catalog.cs`, `Posix.cs`, `Unix.Catalog.cs` compilan con SDK Roslyn contra net8.0
(0 errores, 0 warnings). Los 260 `global::Mono.Unix.Catalog.GetString` de los widgets Gui (65 archivos)
quedan cubiertos por el facade `Mono.Unix.Catalog` interno; sin GAC `Mono.Posix`. El único uso core
no-UI restante es `RemotingService.cs` (`Syscall.chmod` + `FilePermissions`, chmod 0600) → delegado al
**Bloque A2** (IPC/seguridad): no se toca en A1.
