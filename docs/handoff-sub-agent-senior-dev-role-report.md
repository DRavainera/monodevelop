# Handoff — Conversión de 9 proyectos net472 old-style a SDK-style net10.0 (subagente senior-dev)

## Qué se hizo
Se convirtieron los 9 proyectos asignados de old-style `net472` a SDK-style `net10.0`
(`dotnet build -c DebugLinux`, 0 errores en los 9). Los submódulos son
`main/external/nrefactory` y `main/external/debugger-libs`; sus commits y push forman
parte de esta tarea.

Proyectos (todos convertidos, 0 errores):

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

## Por qué / decisiones
- Cabecera SDK estándar (`<Project Sdk="Microsoft.NET.Sdk">`) **sin xmlns**: en el SDK
  10.0.401 (pinned por `global.json`) la combinación de atributos `Sdk` + `xmlns` en el
  elemento `<Project>` falla con `MSB4041` (depende del orden de atributos y de la
  presencia del prólogo XML; es un bug/comportamiento del MSBuild actual).
  **ADVERTENCIA para el agente principal**: `main/src/addins/MonoDevelop.GtkCore/MonoDevelop.GtkCore.csproj`
  usa `<Project DefaultTargets="Build" Sdk="Microsoft.NET.Sdk" xmlns="...">` y falla con
  MSB4041 en este SDK; hay que corregir su cabecera (fuera del alcance de esta tarea).
- `EnableDefaultItems=false`, `EnableDefaultCompileItems=false`,
  `GenerateAssemblyInfo=false`; lista explícita de `Compile` conservada íntegra
  (incl. `AssemblyInfo.cs` y `GlobalAssemblyInfo.cs` enlazado).
- Se quitaron las refs BCL implícitas (`mscorlib`, `System`, `System.Core`, `System.Xml`,
  `System.Xml.Linq`, `System.Data`, `System.Data.DataSetExtensions`, `Microsoft.CSharp`,
  `System.Net.Http`) y la ref `netstandard` (facade net472).
- Firmas conservadas. Se añadió `PublicSign` a CorApi, CorApi2 y Mono.Debugging.Win32:
  el snk `mono.debugging.snk` es RSA-1024 y el firma fuerte real (SHA-1 + RSA-1024)
  falla con `OpenSslCryptographicException: invalid digest` (OpenSSL 3); `PublicSign`
  es el patrón que ya usan NRefactory*, Mono.Debugging y Mono.Debugger.Soft.
- Constantes de preprocesado que el código usa y que los PGs condicionales
  Debug/Release no cubrían con `-c DebugLinux` se movieron a `DefineConstants`
  incondicionales: `SIGNED` (NRefactory, NRefactory.Cecil — `InternalsVisibleTo` con
  public key en `AssemblyInfo.cs`), `FULL_AST;NET_4_0` (NRefactory.CSharp),
  `MONO_DATACONVERTER_STATIC_METHODS;ENABLE_CECIL` (Mono.Debugger.Soft),
  `MDBG_FEATURE_INTEROP;MDBGENG_FEATURE_TRACECALLS;MDBGENG_FEATURE_CAPTUREOUTPUT;CORAPI_EXPOSE_RAW_INTERFACES`
  (CorApi2). Nota: el SDK no define `DEBUG` para configuraciones distintas de `Debug`
  (solo `TRACE`); los `#if DEBUG` de estas fuentes son código diagnóstico local, sin
  impacto de compilación.
- `LangVersion 7.3` en NRefactory.CSharp, CorApi2, Mono.Debugging.Soft y
  Mono.Debugging.Win32: la palabra clave `field` de C# 13/14 (default de net10) se une
  al backing field sintético dentro de accessores y rompe fuentes legacy
  (`CS9273`/`CS9258` + errores en cadena, p. ej. `FieldReference.cs` Win32 y
  `FieldValueReference.cs` Soft).
- `NoWarn` mínimos (receta: solo si `TreatWarningsAsErrors` los convierte en error):
  CorApi2 `<NoWarn>SYSLIB0004;SYSLIB0051;CA1416</NoWarn>` (CER/serialización
  obsoletos + COM Windows-only de SymStore, inherente al proyecto).
- `TreatWarningsAsErrors` de CorApi/CorApi2 conservado.
- Referencias a paquetes vía HintPath a `$(NuGetPackageRoot)` (versión literal, como ya
  tenían): Mono.Cecil 0.10.1 (net40), Roslyn 3.4.0-beta4-final (netstandard2.0, sin
  cambiar versión por decisión del operador), Newtonsoft.Json 13.0.3 (netstandard2.0,
  según receta), System.Reflection.Metadata 1.6.0, System.Collections.Immutable 1.5.0,
  System.Buffers 4.5.0.
- NRefactory.CSharp: `<PackageReference Include="System.CodeDom" Version="5.0.0"
  PrivateAssets="runtime" />` — `System.CodeDom` no existe en el BCL net10; mismo
  patrón que `MonoDevelop.Core.csproj:140`.
- Mono.Debugging.Soft: ref `Mono.Posix` (condicionada a no-Windows) vía
  `$(MonoFrameworkNet45Directory)/Mono.Posix.dll` (resuelve a `main/build/Mono.Posix.dll`
  en esta máquina; contiene `Mono.Unix.UnixSymbolicLinkInfo` usado en
  `SoftDebuggerSession.cs`). Se eliminaron las refs `Mono.Posix`/`netstandard` legacy
  que la fuente no usaba.
- Se conservó el `Import` de `..\Mono.Debugging.settings` (punto de extensión
  Mono.Debugging.overrides; no-op hoy).

## Fixes mínimos en fuentes .cs (archivos dentro de los submódulos)
NRefactory (submódulo `nrefactory`, commit en rama `net10`):
- `ICSharpCode.NRefactory/TypeSystem/Implementation/UnresolvedSecurityDeclarationBlob.cs:30`
  — `typeof(PermissionSetAttribute)` (CAS, eliminado de .NET 10, no existe en el runtime)
  → `ReflectionHelper.ParseReflectionName("System.Security.Permissions.PermissionSetAttribute, mscorlib")`.
  Solo se usa el nombre del tipo (ruta de compatibilidad .NET 1.0); evita además un
  `TypeLoadException` en tiempo de ejecución que una ref legacy causaría.
- `ICSharpCode.NRefactory.CSharp/Parser/mcs/` — capa codegen/driver del fork mcs
  (NRefactory solo usa el parser; estas APIs de Mono/net472 no existen en Microsoft
  net10). Guards `#if !NET` / `#else` mínimos:
  - `method.cs` — `AddDeclarativeSecurity` (3 sitios; `#else`→`#elif !NET` en el patrón
    `#if STATIC` existente) y `GetToken()` (2 sitios en `WriteDebugSymbol`, net10 usa
    `int t = 0`).
  - `class.cs` — `TypeBuilder.AddDeclarativeSecurity` (`#else`→`#elif !NET`).
  - `assembly.cs` — `DefineDynamicModule` (3→1 arg), `DefineUnmanagedResource`,
    `DefineVersionInfoResource`, `DefineManifestResource` (3→0 args), `AddResourceFile`,
    `Builder.Save(...)` → `throw new NotSupportedException()`, bloque `PEFileKinds`
    (tipo específico de Mono, no definido en el fork) → `#if !NET`,
    `SetEntryPoint(2 args)` → `#if !NET` / `else throw`.
  - `reflection.cs` — `DefineDynamicModule` (2→1 arg), `AppDomain.DefineDynamicAssembly`
    (2 sitios) → `AssemblyBuilder.DefineDynamicAssembly`, `Builder.Save(...)` →
    `throw new NotSupportedException()`.
  - `eval.cs` — `AssemblyBuilderAccess.RunAndSave` (2 sitios) →
    `RunAndCollect` en net10 (en net10 el enum solo tiene `Run`/`RunAndCollect`).
  - `driver.cs` — `AssemblyBuilderAccess.Save` → `RunAndCollect` en net10.

debugger-libs (submódulo `debugger-libs`, commit en rama `net10`):
- `Mono.Debugger.Soft/Mono.Debugger.Soft/ObjectMirror.cs:3` y
  `Mono.Debugger.Soft/VirtualMachineManager.cs:7` — se eliminó `using
  System.Runtime.Remoting.Messaging;` (namespace eliminado en .NET; el using estaba en
  desuso).
- `Mono.Debugger.Soft/Mono.Debugger.Soft/VirtualMachineManager.cs` — `EndLaunch`/
  `EndListen`/`EndConnect` (3 sitios): el cast a `System.Runtime.Remoting.Messaging.AsyncResult`
  (tipo público de Mono, interno/inexistente en .NET) → `#if !NET` (código original) /
  `#else throw new PlatformNotSupportedException()`. Coherente: el `c.BeginInvoke(...)`
  APM de los `Begin*` lanza `PlatformNotSupportedException` en runtime net10, así que
  esos caminos no funcionan en net10 de todas formas (es el debugger soft async API;
  sin impacto en la compilación de la IDE).
- `Mono.Debugging/Mono.Debugging.Client/ObjectValue.cs:~ObjectValue` y
  `Mono.Debugging/Mono.Debugging.Evaluation/RemoteFrameObject.cs:DisconnectAll` —
  `System.Runtime.Remoting.RemotingServices.Disconnect(...)` (remoting legacy, no
  existe en .NET) → guardado con `#if !NET`. En sesiones in-proceso era un no-op.

## Cómo se valida
```
cd main
~/.dotnet/dotnet build external/nrefactory/ICSharpCode.NRefactory/ICSharpCode.NRefactory.csproj -c DebugLinux -v:m -nologo
~/.dotnet/dotnet build external/nrefactory/ICSharpCode.NRefactory.Cecil/ICSharpCode.NRefactory.Cecil.csproj -c DebugLinux -v:m -nologo
~/.dotnet/dotnet build external/nrefactory/ICSharpCode.NRefactory.CSharp/ICSharpCode.NRefactory.CSharp.csproj -c DebugLinux -v:m -nologo
~/.dotnet/dotnet build external/debugger-libs/CorApi/CorApi.csproj -c DebugLinux -v:m -nologo
~/.dotnet/dotnet build external/debugger-libs/CorApi2/CorApi2.csproj -c DebugLinux -v:m -nologo
~/.dotnet/dotnet build external/debugger-libs/Mono.Debugger.Soft/Mono.Debugger.Soft.csproj -c DebugLinux -v:m -nologo
~/.dotnet/dotnet build external/debugger-libs/Mono.Debugging/Mono.Debugging.csproj -c DebugLinux -v:m -nologo
~/.dotnet/dotnet build external/debugger-libs/Mono.Debugging.Soft/Mono.Debugging.Soft.csproj -c DebugLinux -v:m -nologo
~/.dotnet/dotnet build external/debugger-libs/Mono.Debugging.Win32/Mono.Debugging.Win32.csproj -c DebugLinux -v:m -nologo
```
Resultado de la validación final (`--no-incremental`, 2026-10-23): los 9 compilan con
0 errores (warnings de deprecación OK: SYSLIB0003/0050/0051, CS0618, etc.).

## Qué queda pendiente / notas
- La cabecera de `MonoDevelop.GtkCore.csproj` (Sdk + xmlns) falla con MSB4041 en el SDK
  10.0.401; requiere corrección por parte del agente principal (fuera del alcance).
- Los caminos APM (`BeginLaunch`/`BeginListen`/`BeginConnect` de
  `VirtualMachineManager`) lanzan `PlatformNotSupportedException` en runtime net10
  (limitación de .NET, no de esta conversión).
- Los proyectos compilan a `bin\DebugLinux\net10.0\` (default SDK; OutputPath por
  configuración old-style eliminado, como en Xwt/Mono.Addins).
- No se tocó ninguna fuente de las UIs Gtk (`--old-gui`) ni de la shell Avalonia.
- No se ejecutó `dotnet build Main.sln` (lo hace el agente principal).
