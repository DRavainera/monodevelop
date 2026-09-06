# Bloque A2 — Migración del core sobre el IPC moderno (System.Runtime.Remoting → BinaryMessage)

## Objetivo
Reemplazar todo el uso de `System.Runtime.Remoting`/`System.Runtime.Remoting.Channels.*`/`Mono.Remoting.Channels.Unix` en el **core** (fuera de UI Gtk#/Mono y libstetic), reaprovechando el IPC **ya existente y activo** construido para MSBuild: `RemoteProcessConnection`/`RemoteProcessServer`/`BinaryMessage` (TCP loopback).

- Quedan **diferidos a "Interfaz"**: libstetic (UI GtkCore), UI Gtk#/Mono.
- No se añaden dependencias nuevas (decisión del usuario). Los candidatos de paquete drop-in (`hexagon-oss/Remoting-Replacement`, `pieroviano/Net4x.Runtime.Remoting`) quedan **descartados**.

## Criterio de terminado (parte A2)
- Cero referencias compilables a `System.Runtime.Remoting` en el core.
- Host de ejecución, mdhost, MSBuild legacy, AutoTest e InstrumentationService funcionando sobre el IPC de mensajes loopback con endurecimiento de seguridad (loopback-only + rechazo de remote + socket 0600 — equivalente binario a `chmod`/`rejectRemoteRequests` ya presente).
- El core compila con Roslyn SDK contra net8.0 (validación dirigida, no `dotnet build` de proyecto — bloqueos de infraestructura preexistentes).

## Decisión arquitectónica central (documentar para otros agentes)
Los `RemoteProcessConnection`/`RemoteProcessServer`/`BinaryMessage` existentes **no** ofrecen proxy transparente ni invocación remota de métodos arbitrarios (lo que sí daba el remoting vía `MarshalByRefObject` + `ObjRef`). Despachan **tipos de mensaje fijos** mediante reflexión `[MessageHandler]`.

Por tanto:
- Para **MSBuild** (B2/B3) **no hace falta nada nuevo**: el host activo es `MonoDevelop.Projects.MSBuild/Main.cs` (ya moderno, `RemoteProcessServer`). Y el host legacy `core/MonoDevelop.Projects.Formats.MSBuild/Main.cs` es **código muerto no compilado** → se **elimina**.
- Para el **host de ejecución** (`ProcessHostController.CreateInstance` + objetos arbitrarios remoted), la migración se resuelve en la fase A2-6 con alcance **MINIMALISTA** (decisión del usuario): se migra la **infraestructura del host** (padre `RemoteProcessConnection` + hijo `RemoteProcessServer`, objetos por ID + `RemoteProcessObjectHandle : IDisposable` exponiendo solo el contrato base `Dispose`/`Shutdown`), dejando el core con **cero referencias a remoting**. El **proxying arbitrario de métodos** de objetos remotos (consumidores acoplados a UI/Gtk) se **difiere a "Interfaz"**, donde se reconstruye sobre el mismo bus. Diseño y decisión en la sección A2-6a.
- Para **InstrumentationService** y **AutoTest**, se sustituye el canal remoting por un **listener TCP loopback propio** sobre el mismo modelo de mensajes (patrón `RemoteProcessServer`).

## Orden de ejecución (fases, por subsistema; una a la vez)
Se prioriza por bajo riesgo y alto valor: primero lo autocontenido y verificable, después los transportes, por último el RPC genérico.

### A2-1 — Limpieza de código muerto / residuos (alto valor, bajo riesgo)
1. **Eliminar** `main/src/core/MonoDevelop.Projects.Formats.MSBuild/Main.cs` (host MSBuild **legacy**): usa `System.Runtime.Remoting` + `BinaryFormatter` para serializar un `ObjRef` (superficie de deserialización). No está referenciado por ningún csproj/sln/targets; el host activo es `MonoDevelop.Projects.MSBuild/Main.cs` (moderno). Verificado con `rg` en `*.csproj`/`*.sln`/`*.targets`. ✔ ya verificado.
2. **Limpiar `using System.Runtime.Remoting*` vestigiales** en archivos que no usan remoting (leer antes de editar):
   - `MonoDevelop.Projects.MSBuild/Main.cs` (child) — confirmar presente.
   - `MonoDevelop.Ide/.../SolutionItemTypeNode.cs` (E4, residuo).
   - `MonoDevelop.Core/.../SyncContext.cs` (E6, residuo) — tratar junto a A2-3 (usa remoting Context).
3. Verificar que ningún otro archivo del core importa remoting sin usarlo.

### A2-2 — `CallContext.LogicalSetData/GetData` → `AsyncLocal<T>` (autocontenido)
- Sitios: `MonoDevelop.Projects.MSBuild/MSBuildProjectService.cs` y `WorkspaceObject.cs` usan `CallContext.LogicalSetData("MonoDevelop.DelayItemInitialization", ...)` / `CallContext.GetData("MonoDevelop.DelayItemInitialization")`.
- `CallContext.LogicalSetData` (net462/remoting-era) se elimina. `AsyncLocal<bool>` fluye de forma nativa por el `async` y sustituye la semántica lógica.
- Crear un holder estático compartido (p. ej. `AsyncLocal<bool> DelayItemInitialization`) en un sitio común (este archivo) y usar `Value` en vez de `LogicalSetData`/`GetData`.
- Validación: compilación dirigida Roslyn net8.

### A2-3 — `SyncContextAttribute`/`SyncContext` sobre `SynchronizationContext`
- Hoy `SyncContextAttribute : ContextAttribute` (remoting) + objetos `ContextBoundObject` que redirigen llamadas. Reescribir sobre `SynchronizationContext` estándar (sin `System.Runtime.Remoting.Contexts`).

### A2-4 — InstrumentationService
- Hoy expone un canal `tcp://<address>/InstrumentationService` para monitorización remota. Sustituir por listener loopback sobre el modelo de mensajes (`RemoteProcessServer` como plantilla). Respetar loopback-only/rejectRemote.

### A2-5 — AutoTest
- Cliente (`AutoTestClientSession`) y servidor (`AutoTestService`/`AutoTestSession`) hablan por remoting. Migrar a transporte de mensajes loopback.

### A2-6 — Host de ejecución (el núcleo duro)
- `RemotingService.cs` (incl. **chmod en :88** y `rejectRemoteRequests`/`CreateChannel`), `ProcessHostController.cs`, `RemoteProcessObject.cs`, `DisposerFormatterSink.cs`, `tools/mdhost/src/mdhost.cs` (276 l).
- Requiere diseñar un **RPC genérico por mensajes**: identificación de objetos remotos por ID, invocación por reflexión, retorno de valores, eventos/one-way. Documentar el diseño antes de codificar. Adopta el endurecimiento ya presente (loopback, reject, socket 0600).

#### A2-6a — Diseño del host de ejecución sobre el transporte de mensajes (documentar antes de codificar)

**Alcance decidido (decisión del usuario): MINIMALISTA — solo la infraestructura del host.**
Se migra la infraestructura del host de ejecución para que el core quede con **cero referencias a
`System.Runtime.Remoting`**, reutilizando el transporte loopback moderno
(`RemoteProcessConnection`/`RemoteProcessServer`/`BinaryMessage`, el mismo patrón que ya usa el
host MSBuild moderno). El **proxying arbitrario de métodos de objetos remotos** (consumidores
acoplados a UI/Gtk: `CodeGeneratorProcess`, `RemoteDesignerProcess`, `ExternalLoader`,
y AutoTest) se **difiere a la fase "Interfaz"**, donde esas APIs se reconstruyen sobre el mismo bus.

**Topología (igual que MSBuild moderno):**
- **Padre = IDE** (`ProcessHostController`, en `MonoDevelop.Core`): es el que *lanza* `mdhost.exe`.
  Hoy lo hace con remoting (publica un `ObjRef` del controller en un canal IPC/TCP). Se cambia a
  **`RemoteProcessConnection`** (padre): crea listener TCP loopback, pasa `port+debug` al hijo,
  escribe un archivo de arranque con la info (id, pid padre, runtime, ensamblados Mono.Addins),
  y correlaciona request/response por `msg.Id`.
- **Hijo = `mdhost`** (`tools/mdhost`): se cambia a **`RemoteProcessServer`** (hijo): abre TCP
  loopback al puerto del padre y registra un listener con `[MessageHandler]` para servir
  `LoadAddins`, `CreateInstance`, `DisposeObject`, y el flujo de logging.
- **Bidireccionalidad**: el mismo hilo de conexión atiende mensajes entrantes de ambos lados.
  El padre `AddListener(controller)` para recibir `RegisterHost`/`WaitForExit`/`GetLogger`;
  el hijo sirve `CreateInstance`/`LoadAddins`/`DisposeObject` y, ante `CreateInstance`, devuelve
  un **ID de objeto** en la respuesta.

**Identidad de objetos remotos:**
- Sin proxy transparente ni `MarshalByRefObject`. Cada instancia creada en el hijo se registra en
  una tabla `id -> object` del lado servidor. El padre recibe el `id` y lo envuelve en
  `RemoteProcessObjectHandle : IDisposable` (`ProcessHostController`).
- El handle expone el contrato base del host: `Dispose()` (envía `ObjectDispose` → el hijo llamó
  `DisposeObject`) y `Shutdown()` (envía `ObjectShutdown` → el padre/común cancela el proceso).
- Los **métodos específicos** de los objetos remotos (p. ej. `CodeGeneratorProcess.CreateWidget`,
  `ExternalLoader.LoadItems`) quedan en el alcance de "Interfaz": hoy se invocan a través del proxy
  transparente de remoting; en "Interfaz" se reconstruyen llamadas tipadas sobre el mismo bus.
  Documentado para que el agente de "Interfaz" los rehaga sin romper `CreateExternalProcessObject`.

**Contratos como mensajes tipados** (`[MessageDataType]` + `[MessageHandler]`):
- Hijo (mdhost) `IProcessHost` → instancia `ProcessHost` registrada con `TargetId`.
  - `LoadAddins(string[])`
  - `CreateInstanceByType` (por `Type`/`fullTypeName`) → devuelve `int instanceId`
  - `CreateInstance` (por `assemblyPath, typeName`) → devuelve `int instanceId`
  - `DisposeObject(int instanceId)`
- Padre `IProcessHostController` → listener del `RemoteProcessConnection`.
  - `RegisterHost` (mdhost anuncia su `ProcessHost`)
  - `WaitForExit`
  - `GetLogger` → en vez de devolver un MBR remoto, el **logging se hace por mensaje one-way**
    (`Log(level, message)`), de modo que `LoggingService.RemoteLogger` deja de necesitar
    `MarshalByRefObject` y `LocalLogger` envía el mensaje al padre.
- Callbacks de refcount/shutdown: el padre responde a `ObjectDispose`/`ObjectShutdown` (análogos a
  `RemoteProcessObjectDisposing`/`RemoteProcessObjectShuttingDown`), disparando el refcount
  `references--` y el temporizador de apagado (`stopDelay`), sin `IMethodCallMessage`.

**Seguridad (invariantes mantenidas):**
- TCP **loopback** (`IPAddress.Loopback`), pinger y correlación por `msg.Id` ya heredados de
  `RemoteProcessConnection`. Sin `BinaryFormatter`, sin `TypeFilterLevel.Full`, sin canales IPC/TCP
  de remoting, sin `ObjRef`. El deserializador controlado de `BinaryMessage` es el único parser de
  wire.
- Se elimina el chmod de socket 0600 (era específico del socket IPC de remoting; el transporte de
  mensajes es TCP loopback bound a 127.0.0.1, que ya no crea socket de archivo).
- `rejectRemoteRequests` queda implícito en el listener loopback.

**Archivos afectados (A2-6):**
- `RemotingService.cs`: se reduce drásticamente (o se elimina su estado de canal). Ya no registra
  canales IPC/TCP de remoting ni produce `GetMarshaledUrl`. Se revisa cada consumidor
  (`ProcessHostController`, AutoTest ya migrado, Instrumentation ya migrado).
- `ProcessHostController.cs`: refactor a `RemoteProcessConnection` (padre) + `RemoteProcessObjectHandle`.
- `RemoteProcessObject.cs`: deja de ser `MarshalByRefObject`; base plana `RemoteProcessObject : IDisposable`
  (mantiene `Dispose()`/`Shutdown()`). Los subclases (diferidas) siguen compilando.
- `DisposerFormatterSink.cs`: **se elimina** (era el sink del canal remoting para interceptar
  `Dispose`/`Shutdown`; el nuevo bus lo sustituye por mensajes `ObjectDispose`/`ObjectShutdown`).
- `tools/mdhost/src/mdhost.cs`: reescrito como `RemoteProcessServer` (hijo) con `ProcessHost` como
  listener tipado + `LocalLogger` por mensaje.
- `RemoteLogger.cs` (Core): pierde `MarshalByRefObject` (queda `: ILogger`).

## Seguridad (invariantes que se mantienen)
- TCP/UNIX loopback **solo para el usuario actual**; `rejectRemoteRequests = true`.
- Socket IPC `chmod 0600` (equivalente a `S_IRUSR|S_IWUSR`), ya implementado en la infra.
- Sin `BinaryFormatter` ni `TypeFilterLevel.Full`; mensajes deserializados por el deserializador controlado de `BinaryMessage`.
- `NuGetAudit` activo, gating en AutoTest.

## Registro de estado
- **(iniciado)** Decisión de estrategia registrada; plan de fases escrito.
- **(hecho, A2-1)** Eliminado `core/MonoDevelop.Projects.Formats.MSBuild/Main.cs` (host MSBuild legacy, muerto: remoting+BinaryFormatter). Limpiado `using System.Runtime.Remoting.Messaging;` vestigial en `SolutionItemTypeNode.cs`. Verificado que ningún csproj/sln lo referenciaba.
- **(hecho, A2-2)** `CallContext.LogicalSetData/LogicalGetData("MonoDevelop.DelayItemInitialization")` → `WorkspaceObject.DelayItemInitialization` (`internal static AsyncLocal<bool>`). Escritor en `MSBuildProjectService.CreateUninitializedInstance`, lector en `WorkspaceObject.Initialize<T>`. Comentario actualizado. Validado: sin errores en las líneas editadas con Roslyn net8 + Mono.Addins net8 (los errores restantes son infra preexistente por DLL net472 precompilada).
- **(hecho, A2-3)** Reemplazado el mecanismo remoting de sincronización por `SynchronizationContext`:
  - `SyncObject` dejó de ser `ContextBoundObject` (clase plana).
  - `GuiSyncObject` perdió `[SyncContext(typeof(GuiSyncContext))]` y ahora expone `protected DispatchOnGuiThread(Action)` que vuelca a `Runtime.MainSynchronizationContext.Send` (con atajo `Runtime.IsMainThread`) — exactamente el comportamiento que daba `GuiSyncContext.Dispatch`.
  - `InternalMessageService` (`MessageService.cs`) usa `DispatchOnGuiThread` explícitamente en `GenericAlert`/`GetTextResponse` (GUI bodies movidos a `GenericAlertGui`/`GetTextResponseGui`).
  - **Eliminado** `SyncContextAttribute.cs` (attribute + `SyncContextDispatchSink` + `DummySink`, el armazón remoting) + su entrada en `MonoDevelop.Ide.csproj`. Verificado: ninguna referencia residual a `SyncContextAttribute`/`SyncContextDispatchSink`/`DummySink`.
  - Limpiados usings remoting vestigiales en `SyncContext.cs`. Único consumidor real del mecanismo era `MessageService.InternalMessageService` (los `[AsyncDispatch]` en `MultiTaskDialogProgressMonitor` eran marcadores inertes).
  - Validado: 4 archivos autocontenidos compilan con cero errores (Roslyn net8 + MonoDevelop.Core). `MessageService.cs`: sin errores de sintaxis ni de las líneas editadas.
  - Reflexión: `[FreeDispatch]`/`[AsyncDispatch]` siguen como atributos planos (sin remoting, referenciados por `MultiTaskDialogProgressMonitor`).
- **(hecho, A2-4)** `InstrumentationService` migrado de remoting a **TCP loopback + snapshot JSON**:
  - `PublishService()`: `TcpListener(IPAddress.Loopback, 0)` (loopback-only, sin `BinaryFormatter`/`TcpChannel`), hilo de aceptación (`AcceptLoop`), y `HandleClient` atiende frames con longitud-prefijada de petición `GET` devolviendo un snapshot JSON (reutiliza `InstrumentationDataCodec.FromService`).
  - `GetRemoteService(hostAndPort)`: devuelve `InstrumentationServiceRemote : IInstrumentationService`, que por cada llamada hace un `GET` loopback y sirve desde el snapshot (`ToService`). Sustituye al proxy `Activator.GetObject` remoting.
  - **Eliminado** `InstrumentationServiceBackend : MarshalByRefObject, IInstrumentationService` + `InitializeLifetimeService`.
  - Limpiados usings remoting. Único consumidor cliente = `tools/mdmonitor/Main.cs` (UI Gtk#, **diferido a Interfaz**), contrato (`GetRemoteService`/`GetCategories`) sin cambios → sigue funcionando sin tocar.
  - Nota: `System.Runtime.Serialization.Formatters.Binary` se mantiene solo para parseo de archivos legacy en `LoadServiceDataFromFile` (no es canal de proceso; revisar con la D de seguridad).
  - Validado: cero errores en `InstrumentationService.cs` (Roslyn net8 + Mono.Addins net8 + Newtonsoft 13.0.3); ninguno en constructos/líneas editados.
- **(en curso, A2-6)** Diseño A2-6a documentado (host sobre `RemoteProcessConnection`/`RemoteProcessServer`, objetos por ID + `RemoteProcessObjectHandle`, logging por mensaje one-way, sin remoting/ObjRef/BinaryFormatter; proxying arbitrario de objetos remotos diferido a "Interfaz" por decisión del usuario). Falta: refactor de `ProcessHostController`, `RemotingService`, `RemoteProcessObject`, `DisposerFormatterSink`, `RemoteLogger`, y reescritura de `mdhost`.
- **(hecho, A2-6 host — infraestructura del host)** Migrada la infraestructura del host de ejecución al IPC loopback (`RemoteProcessConnection`/`RemoteProcessServer`/`BinaryMessage`), dejando **cero referencias a `System.Runtime.Remoting`** en el core:
  - **`ProcessHostController.cs`**: refactor de `MarshalByRefObject` MBR a **padre `RemoteProcessConnection`** (spawn `mdhost.exe`, config vía env var `MONODEVELOP_MDHOST_CONFIG` + archivo temporal, correlación por `msg.Id`, pinger). Ya no publica `ObjRef` ni `GetMarshaledUrl`. `CreateInstance` envía `LoadAddins`/`CreateInstance` hacia el target `"ProcessHost"` y devuelve un **ID de objeto**; los objetos remotos se exponen como **`RemoteProcessObjectHandle : IDisposable`** con `Dispose()`/`Shutdown()` que envían `DisposeObject`/desconexión. Refcount + temporizador `stopDelay`/shutdown preservados (sin `IMethodCallMessage`). Handlers `RegisterHost`/`Log` (`[MessageHandler]`) reciben logging one-way.
  - **`tools/mdhost/src/mdhost.cs`**: reescrito como **hijo `RemoteProcessServer`** con listener tipado `ProcessHost : MessageListener` (`TargetId="ProcessHost"`, handlers `CreateInstance`→`InstanceId`, `LoadAddins`, `DisposeObject`; tabla `id → IDisposable`) + `LocalLogger` que reenvía el log como mensaje `Log` one-way (sin `MarshalByRefObject`). `WatchParentProcess` preservado. Eliminados canales IPC/TCP remoting y el chmod de socket 0600 (el transporte es TCP loopback, sin socket de archivo).
  - **`DisposerFormatterSink.cs`**: **eliminado** + su `<Compile>` en el csproj.
  - **`MonoDevelop.Core.csproj`**: quitado `<Reference Include="System.Runtime.Remoting" />`; **`mdhost.csproj`**: quitada la ref `System.Runtime.Remoting` y `Mono.Posix` (chmod ya removido).
  - **`RemotingService.cs`**: stub remoting-free (`RegisterRemotingChannel` no-op, `GetMarshaledUrl` lanza `NotSupportedException`, `Dispose` no-op); eliminados canales/ObjRef/callbacks (`CallbackData`, `CallingMethodCallback`, `CalledMethodCallback`, `RegisterMethodCallback`, `RegisterAssemblyForSimpleResolve`).
  - **Validación (compilación dirigida Roslyn net8.0)**: `ProcessHostController.cs` + `mdhost.cs` compilan contra el transporte real (`BinaryMessage`, `RemoteProcessConnection`, `RemoteProcessServer`) + stubs mínimos de la superficie externa (`Runtime`, `Counters`, `LoggingService`, `AsyncOperation`, `IExecutionHandler`, `ExecutionCommand`) → **0 errores**.
  - **Nota / tensión documentada**: AutoTest (A2-5, `MonoDevelop.Ide`) sigue llamando `RemotingService.RegisterRemotingChannel()`/`GetMarshaledUrl`; al quedar `RemotingService` como stub, **AutoTest no compila** hasta su migración → queda **diferido a "Interfaz"** (diseñado contra el mismo bus). `CreateExternalProcessObject` sigue devolviendo `IDisposable` (ahora un `RemoteProcessObjectHandle`); los consumidores diferidos (`CodeGeneratorProcess`, `RemoteDesignerProcess`, `ExternalLoader`) compilan sin cambios y se reconstruyen en "Interfaz".
- Pendiente: fases A2-5 (AutoTest, diferido a "Interfaz" tras la decisión de mínimo) … A2-6 (implementación).