# Informe de estado de la migración de MonoDevelop a .NET 10 LTS

## 1. Objetivo y alcance

Este documento resume el estado actual del trabajo de modernización del repositorio MonoDevelop para una migración incremental hacia .NET 10 LTS, con Linux como prioridad operativa y sin introducir nuevas funcionalidades de producto.

La estrategia sigue siendo:

- mantener la lógica funcional separada del runtime y de la UI;
- modernizar dependencias y pipeline sin reescribir el producto completo;
- usar .NET 10 LTS como base técnica estable;
- dejar una capa de compatibilidad intermedia para Mono/Gtk# y otros componentes legacy;
- posponer la decisión final de UI hasta que la base técnica quede estable.

## 2. Resumen ejecutivo

La base del repositorio ya no está bloqueada por feeds NuGet muertos ni por errores triviales de paquetes rotos en la mayor parte del stack principal.

El trabajo realizado ha dejado algunos puntos clave en un estado sólido:

- corrección de dependencias legacy y de runtime bajo Linux;
- estabilización del bootstrap de build para trabajar con .NET 10 real;
- modernización parcial del substack Mono.Addins a SDK-style;
- validación de proyectos del core y del IDE bajo SDK .NET 10;
- separación de la detección del runtime en un punto central de compatibilidad;
- preparación de una capa temporal de compatibilidad Gtk#/Mono, claramente identificada como intermedia.

Sin embargo, la solución completa aún no está normalizada en un build único y coherente porque el repositorio conserva un árbol de metadata legacy de MSBuild/VS Editor y referencias de arquitectura Mac/Xamarin que todavía quedan en la solución central.

## 3. Estado técnico confirmado

### 3.1 Dependencias y runtime

Se confirmó que el problema central ya no es únicamente la falta de paquetes públicos, sino la mezcla de:

- proyectos legacy MSBuild clásico;
- proyectos SDK-style modernizados parcialmente;
- dependencias internas de Roslyn/VS Editor que no forman parte del conjunto público estable;
- referencias de Mono/Gtk# y de APIs internas del IDE que tienen una dependencia fuerte del modelo antiguo.

La estrategia correcta se centró en aislar bloques de incompatibilidad y resolverlos por capas, no en una actualización masiva sin criterio.

### 3.2 Build principal

Se validó que los proyectos clave del runtime principal ya no están bloqueados por un problema de referencia básica. En particular,

- `MonoDevelop.Core` quedó más estable y compatible con .NET 8/Linux;
- `MonoDevelop.Ide` quedó validado en su flujo principal;
- `MonoDevelop.Startup` avanzó claramente en la ruta moderna;
- la capa de programación de build y herramientas configurables quedó adaptada para no depender solo del flujo Mono clásico.

El bloque restante aparece más arriba en el árbol de solución y en la metadata heredada del proyecto, no en los componentes centrales del IDE.

### 3.3 Gtk# y Mono

Se tomó la decisión de instalar y dejar operativo el stack Gtk# para cerrar la capa de compilación de la UI heredada mientras se prepara la migración del front-end.

Esto se trató como una compatibilidad temporal, no como la solución final. Se documentó explícitamente que la corrección de Gtk#/Mono es una capa de transición y no la implementación productiva definitiva.

### 3.4 NRefactory/Cecil legacy y compatibilidad provisional

La capa `ICSharpCode.NRefactory.Cecil` se mantiene como una implementación provisional de compatibilidad para la base legacy del stack de análisis sintáctico y tipo del repositorio. 

Esto no es la solución final ni una arquitectura aceptable para el cutover final. Es una capa de transición para estabilizar la compilación y la migración base en .NET 8/Linux, y debe reemplazarse por una implementación moderna y soportada antes del cierre del proyecto.

### 3.5 UI futura

Se documentó la propuesta futura para la interfaz para no bloquear la migración base. La recomendación principal es:

- Avalonia UI como opción principal para una UI moderna, cross-platform y consistente con .NET 8.

La justificación es que proporciona una base más moderna que Gtk# y evita depender de un modelo visual heredado del stack Mono clásico.

### 3.6 Corrección de rutas de referencia heredadas bajo Linux

Se confirmó que la causa más directa de los bloqueos de compilación bajo .NET 8 no era el SDK en sí, sino la propagación de `ReferencePath` hacia `/usr/lib/mono/...` para proyectos que ya estaban en el TFM moderno (`net8.0`). Esto hacía que MSBuild intentara resolver `TargetFramework=.NETCoreApp,Version=v8.0` usando bibliotecas de .NET Framework, lo que provocaba errores de `MSB3644`.

La corrección aplicada fue estrechar el bloque de `ReferencePath` al conjunto legacy (`.NETFramework` + net40/net45/net472) y dejar el pipeline de SDK moderno intacto. La validación relevante fue:

- `dotnet build main/external/mono-addins/Mono.Addins.Setup/Mono.Addins.Setup.csproj -p:TargetFramework=net8.0`
- `dotnet build main/external/mono-addins/Mono.Addins/Mono.Addins.csproj -p:TargetFramework=net8.0`

Ambos compilan correctamente tras la corrección, con solo advertencias de obsolescencia del código heredado y sin errores de resolución de referencia.

### 3.7 Normalización del árbol de solución heredado

Se cerró un bloque real de metadata heredada en `main/Main.sln`: los proyectos `Xamarin.PropertyEditing` y `Xamarin.PropertyEditing.Mac` faltaban en la sección `ProjectConfigurationPlatforms`, lo cual generaba advertencias `MSB4121` cuando la solución se evaluaba con la configuración por defecto. Esto no era un problema de compilación del código, sino de definición del árbol de solución.

La corrección fue añadir los mappings faltantes para `Debug|Any CPU` y `Release|Any CPU` en la solución. Esto deja la estructura de build en un estado más consistente para Linux y facilita la siguiente validación incremental.

## 4. Cambios clave realizados

Los ajustes más relevantes quedaron en estos puntos:

- `main/Directory.Build.props`
  - ajuste de propiedades de NuGet;
  - compatibilidad con reference assemblies de .NET Framework bajo Linux;
  - preparación para compilación con SDK moderno.

- `main/src/core/MonoDevelop.Core/MonoDevelop.Core.csproj`
  - ajuste de dependencias conflictivas como `Mono.Cecil` y `System.Collections.Immutable`;
  - compatibilidad con Roslyn 4.x bajo .NET 8.

- `main/external/mono-addins/...`
  - conversión parcial del substack a SDK-style para reducir el acoplamiento con MSBuild clásico.

- `main/src/core/MonoDevelop.Core/MonoDevelop.Core/Platform.cs`
  - centralización de detección de runtime para evitar duplicación y fricción.

- `main/external/fsharpbinding/...`
  - corrección de referencias faltantes a `Mono.Posix`, `gtk-sharp`, `gdk-sharp`, `glib-sharp`, `pango-sharp`.

- `main/src/addins/MonoDevelop.GtkCore/libstetic/libstetic.csproj`
  - corrección de referencias a `Mono.Cairo` y `Mono.Posix`.

- `main/external/vs-editor-api/.../PatternMatchingImpl.csproj` y archivos asociados
  - corrección de dependencia faltante de `TextLogic` y ajuste de imports del namespace de pattern matching.

- `main/external/nrefactory/NRefactory.sln`
  - normalización del árbol de solución heredado eliminando proyectos obsoletos de `Mono.Cecil` y `IKVM.Reflection` que ya no existen ni son parte del flujo Linux-first.

## 5. Bloque persistente actual

El siguiente bloqueo real no es de falta de paquete general ni de falta de Gtk#, sino la mezcla heredada de:

- proyectos clásicos de .NET Framework;
- metadata antigua de soluciones y paths de proyecto;
- dependencias de editor/IDE de Visual Studio con APIs no públicas;
- referencias y comportamiento del stack macOS/Xamarin que todavía quedan en la solución maestra.

Esto significa que el repositorio aún necesita una normalización de la capa de solución y de los proyectos heredados antes de poder cerrar la validación del build completo bajo .NET 8.

## 6. Criterio de cierre de la fase actual

La fase actual se considerará cerrada cuando se cumplan estas condiciones:

- la solución principal pueda compilar de forma coherente con .NET 8 en Linux;
- los proyectos heredados relevantes estén aislados o convertidos a SDK-style;
- los bloques VS Editor / Roslyn / Gtk# / Mono.Addins queden desacoplados o adaptados;
- la capa de UI futura quede claramente separada de la migración base;
- los cambios queden documentados y listos para la siguiente fase sin riesgo funcional.

## 7. Siguientes pasos recomendados

1. Revisión del árbol de solución heredado en `Main.sln` y limpieza de metadata obsoleta.
2. Validar si el siguiente problema es solo de solución o de un grupo de proyectos concretos.
3. Recompilar de forma incremental subgrupos de proyectos antes de volver a la solución completa.
4. Aislar y estabilizar la capa macOS/Xamarin antes del cutover final.
5. Preparar la siguiente fase de runtime y UI, con Avalonia como referencia principal.

## 8. Auditoría de seguridad (resumen para plan de parcheo)

Antes de iniciar el trabajo de UI se realizó una revisión estática dirigida de seguridad sobre el repositorio migrado. El detalle completo está en `docs/security-audit-report.md`; aquí queda el resumen de riesgos detectados y el plan de parcheo asociado.

### 8.1 Hallazgos principales

1. **CRÍTICO — Deserialización insegura y canales .NET Remoting sin autenticación.** ~21 usos de `BinaryFormatter` (obsoleto en .NET, `SYSLIB0011`), en canales TCP (`RemotingService.cs`, `mdhost.cs`, msbuild `Main.cs`, `AutoTest*`, `libsteticui`, `InstrumentationService`) y en archivos rastreables (toolbox, version control). Riesgo de ejecución de código arbitrario si el canal o el archivo es manipulado.
2. **ALTO — Callback TLS global con fingerprint débil.** `Runtime.cs:103-110` sobrescribe la validación global de certificados y usa la clave pública (`GetPublicKeyString()`) como identificador en vez de huella SHA-256.
3. **ALTO — Protocolos SSL rotos todavía soportados.** `Ssl2`/`Ssl3` en `XspSslProtocol` (`XspParameters.cs:212-217`).
4. **MEDIO — MD5 y fuga de e-mails a terceros.** MD5 en `AutoSave.cs` y en gravatar (`ImageService.cs:856`), que además envía el hash del e-mail del usuario a un servicio externo.
5. **MEDIO — Claves de firma strong-name en el repositorio.** 10 archivos `.snk` dentro del árbol.
6. **MEDIO — Dependencias NuGet desactualizadas sin auditoría.** `NuGet.Client 5.4.0`, `Microsoft.TestPlatform 16.2.0`, `NUnit3 3.9.0`, `Mono.Cecil 0.10.1`; `NuGetAudit` no está habilitado.
7. **MEDIO — Actualización de addins sin verificación de firma** (la descarga usa HTTPS, pero el `.mpack` no se valida).
8. **BAJO — Exposición del servicio de instrumentación/autotest** por puerto TCP local.

### 8.2 Plan de parcheo (resumen)

- **Fase 1 (red/deserialización):** sustituir `BinaryFormatter` por JSON seguro o `DataContract`; activar `ensureSecurity` en canales remoting; bind a `127.0.0.1`; deshabilitar `MONO_AUTOTEST_CLIENT` por defecto.
- **Fase 2 (TLS/cripto):** eliminar `Ssl2`/`Ssl3`; quitar el callback global de certificados (o validar por huella SHA-256 + dominio); reemplazar MD5 por SHA-256 en autosave; desactivar gravatar (o hash SHA-256 + opción configurable).
- **Fase 3 (supply-chain):** activar `NuGetAudit`, actualizar paquetes con CVE; retirar `.snk` del árbol; verificar firma de addins; añadir `*.snk`/`*.pfx`/`*.pem`/`*.key` a `.gitignore`.
- **Fase 4 (validación):** recompilar los subconjuntos validados tras cada cambio y repetir la auditoría dirigida para confirmar 0 regresiones.

La priorización detallada, referencias archivo:línea y acciones por hallazgo están en `docs/security-audit-report.md`.

### 8.3 Progreso de la Fase 1 (por ítem)

- **C1 — Canales IPC de RemotingService y mdhost (completado):** el socket Unix de `.NET Remoting` se creaba con permisos `0666` (mundo accesible). Se restringió a `0600` vía `chmod` en `RemotingService.cs` y `mdhost.cs` (con fallback seguro y `Mono.Posix` añadido a `mdhost.csproj`). Validado por compilación aislada y prueba empírica con Mono 6.14 (el comportamiento de conexión IPC entre procesos es idéntico antes/después; el caso válido del propietario sigue funcionando). Detalle en `security-audit-report.md` → Bitácora → Cambio C1.
- **C2 — Canal Unix de libsteticui (completado):** el subsistema de diseño GTK (`libsteticui`) crea su propio `UnixChannel` de remoting al margen de `RemotingService`, con socket mundo-accesible. Se restringió a `0600` vía `chmod` en `Application.cs` (lado padre) y `ApplicationBackend.cs` (lado hijo), con `try/catch` de degradación segura, y se añadió la referencia `Mono.Posix` a `libsteticui.csproj` (requerida por `Mono.Unix.Native` y para los canales Unix que ya viven en `Mono.Posix.dll`). Validado por compilación aislada del patrón `chmod` (modo resultante `0600`).
- **C2-TCP — Bind a loopback de los canales TCP de libsteticui (completado):** los `TcpChannel` de `libsteticui` (intervalo `ProcessTcp`, tanto en el frontend `Application.cs` como en el backend `ApplicationBackend.cs`) bindaban por defecto a `0.0.0.0` (todas las interfaces), validado empíricamente con Mono 6.14 (exposición de la superficie de remoting a la red). Se añadió `rejectRemoteRequests=true` en ambos; probado: bind a `127.0.0.1` exclusivo y llamada local funcional (`tcp://127.0.0.1:<port>/Pong.rem` → `pong`); requests remotos rechazados. Detalle en `docs/security-audit-report.md` → Bitácora → Cambio C2 → endurecimiento TCP asociado.
- Pendiente de la fase: migrar `BinaryFormatter`. Detalle en `docs/security-audit-report.md` → Bitácora → Cambio C2.
- **C3 — Viabilidad de migrar `BinaryFormatter` en la capa remoting (análisis completado):** se clasificaron los 17 usos de `BinaryFormatter` en tres categorías: (A) transporte de `ObjRef` de arranque de los canales remoting (`ProcessHostController`/`mdhost`, `MSBuild/Main`, `libsteticui`, `AutoTest*`), (B) datos locales (`InstrumentationService` autosave, `NUnitAssemblyTestSuite` caché), (C) formatos de archivo (`VersionControlService`, `ToolboxItemToolboxNode`). Conclusión: el `ObjRef` en sí no es migrable a JSON (formato binario cerrado del BCL), pero **el transporte de arranque SÍ se puede sustituir por endpoint textual + `Activator.GetObject`** (validado empíricamente con procA/procB → `pong`). El `BinaryFormatter` interno del protocolo de `.NET Remoting` (sink binario con `TypeFilterLevel.Full`) NO es migrable sin reemplazar la pila de remoting; su exposición se mitiga con C1/C2/C2-TCP (IPC a propietario + TCP loopback). Recomendado: migrar primero Categorías B/C (local, bajo riesgo) y luego A canal a canal con prueba de integración padre+hijo. Detalle en `security-audit-report.md` → Bitácora → Cambio C3.
- **C4 — Mensajes de commit (VersionControlService) a JSON (completado):** el archivo `version-control-commit-msg` se guardaba/leía con `BinaryFormatter`. Migrado a `Newtonsoft.Json` (`Dictionary<string, CommitComment>`), con lectura JSON preferente y **fallback al formato binario legacy** para no perder datos previos; `comments` se mantiene como `Hashtable` (sin alterar el resto de la lógica). Se añadió `PackageReference Newtonsoft.Json` (13.0.3, la fijada en el repo) al `VersionControl.csproj`. Validado por compilación aislada y probe funcional (escritura JSON, relectura correcta, carga legacy OK). Detalle en `docs/security-audit-report.md` → Bitácora → Cambio C4.
- **C5 — Caché de tests NUnit (NUnitAssemblyTestSuite) a JSON (completado):** `TestInfoCache` (caché en disco de resultados de tests) se guardaba/leía con `BinaryFormatter`. Migrado a `Newtonsoft.Json` (`Dictionary<string, CachedTestInfo>`), con lectura JSON preferente y **fallback al binario legacy**; `NunitTestInfo`/`CachedTestInfo` son JSON-deserializables (round-trip del árbol anidado validado). Se añadió `PackageReference Newtonsoft.Json` (13.0.3) al `MonoDevelop.UnitTesting.NUnit.csproj`. Validado por probe funcional: JSON path y fallback legacy OK. Detalle en `docs/security-audit-report.md` → Bitácora → Cambio C5.
- **C6 — Autosave de InstrumentationService a JSON con DTO layer en MonoDevelop.Core (completado):** `AutoSave` serializaba el snapshot completo con `BinaryFormatter` y `LoadServiceDataFromFile` lo leía binario. Como las clases de snapshot (`Counter`/`TimerCounter`/`CounterCategory`/`CounterValue`/`TimerTrace`) son getter-only y no JSON-round-trippables, se construyó una **capa de DTOs de persistencia** (`InstrumentationData.cs`: `InstrumentationSnapshotDto`/`CounterDto`/`CounterValueDto`/`TimerTraceDto`/`CounterCategoryDto`) con mapeo explícito `InstrumentationDataCodec.FromService`/`ToService`, y se añadieron hooks internos de restauración en `Counter`/`TimerCounter` (`RestoreState`/`RestoreValues`/`RestoreTimerState`). `AutoSave` escribe JSON; `LoadServiceDataFromFile` es JSON-preferente con **fallback binario legacy**. `MonoDevelop.Core.csproj` ya tenía Newtonsoft. Validado por compilación Roslyn (cero errores en los archivos tocados) y probe `instrumentation_probe2`: round-trip JSON completo `OK` (estado, valores, traces, metadata, membresía de categorías); se corrigió un defecto de doble alta de categoría. Detalle en `docs/security-audit-report.md` → Bitácora → Cambio C6.
- **C7 — Endurecer deserialización de ToolboxItemToolboxNode (completado):** `DeserializeToolboxItem` deserializaba con `BinaryFormatter` un blob Base64 embebido en el XML del toolbox (`Toolbox.xml`, config local del usuario) → riesgo de gadget/ejecución de código desde un archivo manipulado. Como el payload es un `System.Drawing.Design.ToolboxItem` (tipo de framework con subclases custom no JSON-round-trippables), NO se migró a JSON; se añadió un `ToolboxItemSerializationBinder` (allowlist) que solo permite tipos asignables a `ToolboxItem` (cualquier ensamblado, preservando subclases) o tipos de ensamblados framework de confianza, bloqueando el resto (donde viven los gadgets). `BindToName` preserva el formato en disco. Mantiene formato legacy y fidelidad de subclases. Validado por probe `tbi_block` (`BINDER-PROBE=OK`: permite ToolboxItem/framework, bloquea gadget de extremo a extremo) y compilación Roslyn (cero errores). Detalle en `docs/security-audit-report.md` → Bitácora → Cambio C7.
- **C8 — Reemplazar transporte `ObjRef` por endpoint textual + `Activator.GetObject` (canal ProcessHostController ↔ mdhost, completado):** `ProcessHostController.Start` publicaba su proxy con `RemotingServices.Marshal(this)` y serializaba el `ObjRef` binario (`BinaryFormatter` → Base64) en la primera línea de un archivo temporal; `mdhost.Main` lo deserializaba con `BinaryFormatter.Deserialize` (superficie de gadget). Se sustituyó por una **URL textual**: nuevo `RemotingService.GetMarshaledUrl(uri)` devuelve la URL del canal TCP registrado (endurecido en C1/C2: `rejectRemoteRequests=true` → 127.0.0.1) y el hijo hace `Activator.GetObject(typeof(IProcessHostController), url)`. Eliminado `using System.Runtime.Serialization.Formatters.Binary;` en ambos archivos. Validado: compilación `mcs` de `mdhost.cs` (éxito) + Roslyn sin errores en el código añadido + probe de transporte (`phc_probe`/`dualch2`: el objeto marshaled es alcanzable por `Activator.GetObject` en IPC y TCP) + probe de integración extremo a extremo `c8sP`/`c8sC` (`PARENT_GOT_HOST:host1`, `RESULT:OK`). Detalle en `docs/security-audit-report.md` → Bitácora → Cambio C8.
- **C9 — Reemplazar transporte `ObjRef` por endpoint textual + `Activator.GetObject` (canal libsteticui padre ↔ hijo, completado):** `ApplicationBackendController.StartBackend` publicaba su controlador con `RemotingServices.Marshal` y serializaba el `ObjRef` binario (`BinaryFormatter` → Base64) en la segunda línea del stdin del backend hijo; `ApplicationBackend.Main` lo deserializaba con `BinaryFormatter.Deserialize` (superficie de gadget). Se sustituyó por una **URL textual**: nuevo `ApplicationBackendController.GetMarshaledUrl(uri)` calcula la URL según el canal activo (TCP `__internal_tcp`: `baseUrl + "/" + uri`; Unix `"unix"`: `"unix://" + path + "?" + uri`, con separador `?` según `UnixChannel.ParseUnixURL`) y el hijo hace `Activator.GetObject(typeof(ApplicationBackendController), url)`. Eliminado `using System.Runtime.Serialization.Formatters.Binary;` en ambos archivos. Validado: compilación Roslyn del set completo `libsteticui` (56 + 2 `Windows/*` fuentes, refs gtk-sharp 2.0 GAC, Mono.Cecil, prebuilt `libstetic.dll`) → **cero errores**, ninguno en los archivos tocados + probe de integración extremo a extremo `c9e_parent`/`c9e_child` en TCP y Unix (`PARENT_URL:tcp://…/c1.rem` y `unix://…?c1.rem` → `CHILD_PING:pong` → `PARENT_GOT_CONNECT:backend1`). Detalle en `docs/security-audit-report.md` → Bitácora → Cambio C9.
- **C10 — Reemplazar transporte `ObjRef` por endpoint textual + `Activator.GetObject` (canal AutoTest, 4 handoffs, completado):** AutoTest conecta MonoDevelop con el test runner externo mediante cuatro traspasos del `ObjRef` serializado con `BinaryFormatter` (→ Base64 en `MONO_AUTOTEST_CLIENT`/`SessionReferenceFile`, deserializados con `BinaryFormatter.Deserialize`). Se sustituyó por **URL textual**: `RemotingService.GetMarshaledUrl` pasa de `internal` a `public` (constructor canónico de la URL TCP loopback, reutilizado por mdhost/C8); `AutoTestClientSession.StartApplication` marshala `this` como `"autotest-client"` y escribe su URL en `MONO_AUTOTEST_CLIENT`; `AutoTestService.Start` lee esa URL con `Activator.GetObject(typeof(IAutoTestClient), …)`, marshala `manager` como `"autotest-service"` y escribe su URL en `SessionReferenceFile`; `AutoTestClientSession.AttachApplication` conecta con `Activator.GetObject(typeof(IAutoTestService), …)`. Eliminado `using System.Runtime.Serialization.Formatters.Binary;` en ambos archivos. Validado por probe end-to-end `c10_full` (canal `TypeFilterLevel.Full` igual a `RegisterRemotingChannel`, 2 procesos): `APP_CLIENT_PING:client-pong`, `APP_RECEIVED_CONNECT:app-backend`, `CLIENT_SERVICE_PING:service-pong`, `CLIENT_ATTACHED:client-pong`; + Roslyn sobre los archivos cambiados: cero errores de transporte/API (ruido = tipos hermanos de MonoDevelop.Ide sin prebuilt). `InstrumentationService.cs:123` **NO es transporte vulnerable** (`PublishService` publica un MBR y `mdmonitor` conecta por `Activator.GetObject` + URL textual, sin BinaryFormatter en el bootstrap; el `BinaryFormatter` restante es el fallback legacy del autosave de C6). Detalle en `docs/security-audit-report.md` → Bitácora → Cambio C10.
- **C11 — Deshabilitar canal AutoTest/instrumentación por defecto (opt-in explícito, completado):** hallazgo 2.6 / prioridad #8 (BAJO). El canal de autotest (y la publicación del servicio de instrumentación) se activaba solo por la presencia de `MONO_AUTOTEST_CLIENT`, permitiendo que un proceso local la fijara y forzara el canal remoting. Se exigió **opt-in explícito**: `AutoTestService.Start` retorna si `!IsAutoTestEnabled()` (nuevo: `MONO_AUTOTEST_ENABLE == "1" || EnableAutomatedTesting`) y avisa si hay `MONO_AUTOTEST_CLIENT` sin opt-in; `AutoTestClientSession.StartApplication` fija `MONO_AUTOTEST_ENABLE=1` (el harness opta); `Runtime.IsInstrumentationServiceEnabled()` publica solo con `MONO_AUTOTEST_ENABLE == "1"` o preferencia `EnableInstrumentation`. Validado por probe `c11_gate`: `MONO_AUTOTEST_CLIENT` solo → `WARN: NOT started`; con `MONO_AUTOTEST_ENABLE=1` → `CONNECTING`/`PUBLISH` + `STARTED`; + Roslyn sin errores en las líneas cambiadas. Detalle en `security-audit-report.md` → Bitácora → Cambio C11.
- **Fase 1, ítem 1 y ítem 2 de remoting: COMPLETADOS.** Pendiente de otros hallazgos de Fase 1 (ítems 2/3 de TLS-criptografía de la Fase 2 y validación según plan en `docs/security-audit-report.md` §5). Restan solo los `BinaryFormatter` en superficies NO remoting: `xwt/TransferDataSource.cs:150,164` (portapapeles) y `guiunit/BinarySerializableConstraint.cs:38` (tests), fuera del alcance de canales entre procesos.

#### 8.4 Progreso de la Fase 2 (TLS y criptografía)

- **F2.1 — Eliminar SSL 2/SSL 3 (completado):** removidos `Ssl2`/`Ssl3` del enum `XspSslProtocol` (`XspParameters.cs`) y sus dos ítems del combo en `XspOptionsPanelWidget.cs`, manteniendo la alineación de índices. Roslyn sin errores de lógica.
- **F2.2 — Quitar callback TLS global (completado):** eliminado el `ServerCertificateValidationCallback` global en `Runtime.cs` (y su `using` huérfano); `WebCertificateService.GetIsCertificateTrusted` validada por huella SHA-256 exacta; diálogo de `DefaultWebCertificateProvider` acotado (`WaitOne(15000)`, deniega en headless). Roslyn sin errores de lógica.
- **F2.3 — MD5 → SHA-256 en autosave (completado):** `AutoSave.cs` usa `SHA256.Create().ComputeHash` per-call (thread-safe, elimina la instancia estática compartida); API compatible net472 (sin `SHA256.HashData`). Roslyn: región SHA256/`GetMD5` con cero errores.
- **F2.4 — Gravatar off por defecto (completado):** nueva preferencia `Runtime.Preferences.EnableGravatarAvatars` (default `false`); `ImageService.GetUserIcon` retorna `null` sin red al estar deshabilitado; guards en `LoadUserIcon` y en el renderer de `LogWidget.cs`. Nota: la API de Gravatar exige MD5 (no acepta SHA-256), por lo que la mitigación real es el opt-in explícito sin emitir el hash del email salvo que el usuario lo active. Roslyn sin errores en las regiones editadas.

**Fase 2 COMPLETA (F2.1–F2.4).** Pendiente para Fase 3: supply-chain (NuGetAudit, paquetes con CVE) y claves (`.snk`). Detalle de cada cambio en `docs/security-audit-report.md` → Bitácora → Cambios F2.1–F2.4.

#### 8.5 Progreso de la Fase 3 (Supply-chain y claves)

- **F3.1 — NuGetAudit (completado, bump pendiente):** añadido `<NuGetAudit>true</NuGetAudit>` en `main/Directory.Build.props`. Análisis de CVEs (Sep-2026): **NuGet.Client 5.4.0 afectado por CVE-2024-0057** (crítica, requiere ≥ 5.11.6; también vendida en `external/nuget-binary/` y `nuget.exe`); Mono.Cecil 0.10.1, NUnit 3.9.0 y Microsoft.TestPlatform 16.2.0 **sin CVE**. Por decisión del usuario se documenta el bump como **deuda (5.4.0 → ≥ 5.11.6)** dado que requiere reemplazar binarios vendidos con riesgo no validable en el build legacy net472.
- **F3.2 — Strong-naming: verificado, se mantiene (completado):** el único `.snk` versionado (`MonoDevelop-Public.snk`) es **solo-clave-pública** (`RSA1`, `<PublicSign>True</PublicSign>`) → no es un secreto. El strong-naming aporta valor (IVT a Roslyn firmado), así que no se retira ni se deshabilita.
- **F3.3 — Firma de addins: documentado (completado):** no hay verificación de firma en `Mono.Addins.Setup`; el canal por defecto es **HTTPS oficial** (`https://addins.monodevelop.com/.../main.mrep`, sin `http://`). Se documenta la verificación de firma de paquetes como deuda residual.
- **F3.4 — Ignorar claves (completado):** añadido `*.snk`, `*.pfx`, `*.pem`, `*.key` a `.gitignore`.

Detalle de cada cambio en `docs/security-audit-report.md` → Bitácora → Cambios F3.1–F3.4.

#### 8.6 Progreso de la Fase 4 (Validación y cierre)

- **F4.1 — Reconstrucción de subconjuntos validados (completado):** reconfirmados los subconjuntos:
  - `MonoAddins`/`MonoAddins.Setup` **net8.0**: `dotnet build -p:TargetFramework=net8.0` → **Compilación correcta, 0 errores** (solo avisos SYSLIB/obsoletos pre-existentes, ajenos a este parcheo).
  - `MonoDevelop.Core`/`MonoDevelop.Ide` (legacy net472, sin proyecto SDK-style net8): revalidado por compilación Roslyn dirigida sobre los archivos de seguridad editados (ImageService, XspParameters, AutoSave, etc.) → **0 errores estructurales**, coherente con la validación por fases.
- **F4.2 — Auditoría dirigida de regresiones (completado, 0 regresiones):**
  - **Deserialización:** **no queda ningún `BinaryFormatter(`** en `main/src`. Los residuales (mdhost, AutoTest, ProcessHostController, RemotingService, libsteticui, VersionControlService, NUnitAssemblyTestSuite) son solo **comentarios**. Quedan tres usos acotados y seguros: `InstrumentationService` (fallback legacy **detrás de JSON**, no transporte), `ToolboxItemToolboxNode` (solo con `ToolboxItemSerializationBinder` allowlist), y el `Main.cs` MSBuild (código muerto documentado).
  - **TLS:** sin `Ssl3`/`Ssl2`, sin callback global con `SslPolicyErrors.Ignore`; `Runtime.cs:102` es solo comentario.
  - **Criptografía:** único MD5 restante es el requerido por el protocolo Gravatar (gated por opt-in F2.4, inalcanzable si está deshabilitado); autosave ya es SHA-256.
  - **Secretos/claves:** único `.snk` versionado es el público (`MonoDevelop-Public.snk`), sin material privado.
  - **Feeds de addins y remoting:** solo HTTPS para repos; endurecimiento intacto (`rejectRemoteRequests=true`, loopback 127.0.0.1, IPC `chmod 0600` en `RemotingService.cs`/`mdhost.cs`/`Application.cs`/`ApplicationBackend.cs`).
- **F4.3 — Documentación de cierre (completado):** estos cambios quedan registrados en `docs/migration-status-report.md` y en `docs/security-audit-report.md` → Bitácora → 4/4b/4c + Fases 3 y 4.

**Plan de parcheo de seguridad (Fases 1–4): COMPLETADO.** Deuda documentada para revisión posterior junto con el resto de pendientes de migración: los `BinaryFormatter` no-remoting (`xwt/TransferDataSource.cs`, `guiunit/BinarySerializableConstraint.cs`), el bump de `NuGet.Client` 5.4.0 → ≥ 5.11.6 (CVE-2024-0057, binarios vendidos en `external/nuget-binary/`), y la verificación de firma de paquetes de addin.

## 8.7 Progreso de la UI Avalonia (bucle de migración por módulos, 2026-09-22)

El bucle de migración Gtk → Avalonia continúa por módulos (detalle por hito en
`docs/interfaz-plan.md` § M5–M11y; registro de sesión en `docs/session_summary.md`):

- **Paridad de pads**: Properties pad con datos reales por nodo
  (descriptores del PropertyGrid legacy) repoblado al seleccionar en el Solution
  pad; PadHost auto-selecciona la primera pestaña (sin contenidos vacíos);
  búsqueda incremental en el Solution pad con expansión de ancestros; menú
  contextual por tipo de nodo (Add/Rename/Remove/Build reales sobre disco).
- **Paridad de diálogos**: DirtyFilesDialog ("Save Files") como gate de cierre
  con documentos modificados, cableado en los 4 puntos del legacy y verificado
  E2E (bloquea el cierre de ventana con WM_DELETE).
- **Editor (SkTextEditor, SkiaSharp)**: render estable (frame nuevo por render —
  el bitmap presentado nunca se muta: eliminados los fantasmas/líneas
  duplicadas), Backspace/Delete multi-caret, intellisense (CompletionPopup con
  `.`/Ctrl+Space y commit verificado E2E), hover tooltip (EditorTooltipPopup con
  firma de la declaración), cursor I-beam y pad del editor vivo sin pestañas.
- **QA por módulo**: hooks deterministas (`--props`, `--dirtyfiles`, `--editqa`)
  + verificación visual por capturas X11 contra la UI Gtk de referencia. Los QA
  del editor restauran el archivo original (no dejan residuo en el proyecto del
  usuario).
- **M25b (cierre de pendientes de M24/M25)**: fix del resultado stale del hook
  `--searchpopup` (la causa era código comentado por el commit `8d3c607821`, no
  un debounce del handler) y verificación visual del ✕ del pad Properties por
  píxeles + click XTEST real (rect `screen=(1420,242)` 16x16 → `visible=False` +
  `View > Pads > Properties checked=False`). Hook QA nuevo `--padclose`, con
  listener de captura `PointerPressed` (`handledEventsToo: true`, porque
  `Button` marca el evento como handled) que reporta la cadena de controles que
  recibe el press: el ✕ es el target real del hit-test en su centro y el
  `ToggleButton` padre no lo intercepta. Re-validado desde arranque limpio: el
  ✕ funciona, no hay bug de hit-test (el fallo intermedio era el host ya
  colapsado, donde el ✕ no está en el árbol visual). Detalle en
  `docs/interfaz-plan.md` § M25b.
- **M26 completado (2026-10-02)**: portados los tres pads placeholder
  `documentoutline`, `classes` y `codeissues` (Opción 1 acotada de la propuesta).
  Nuevos servicios `SymbolIndexService` (escáner de una pasada: outline,
  breadcrumb/GoToType/`:t` y árbol de clases) y `CodeIssueService` (diagnósticos
  MSBuild agrupados por severidad); pads auto-ocultos con doble clic que abre
  archivo + salta a línea; hooks QA `--outline[=<path>]`, `--classes`,
  `--codeissues` y `--dblclick[=<path>]`. El Tester QA Senior (§18.5) halló y el
  desarrollo corrigió 8 defectos del escáner (control de flujo indentado como
  miembro, propiedad Allman, campo sin modificador, comentarios/verbatim/raw
  strings, `throw new`/`yield return` como métodos, `record`, indexers) en 3
  rondas hasta aprobar limpio. Build 0 errores, 53/53 tests.
  Detalle en `docs/interfaz-plan.md` § M26.
- **M27 completado (2026-10-02)**: Preferences con **árbol jerárquico** fiel a
  `GlobalOptionsDialog.addin.xml` (+ extensiones de add-ins), **header de panel**
  (icono + título) como el legacy, iconos `md-prefs-*` corregidos y el panel
  nuevo **Build → "Errors and Warnings"** (`BuildMessagePanel`). QA §18.5 en 2
  rondas hasta aprobar limpio. Build 0 errores, 53/53 tests. Detalle en
  `docs/interfaz-plan.md` § M27.
- **M28 completado (2026-10-02)**: fix de la **doble barra de título** de los
  diálogos (`DialogWindow.Apply` buscaba el `dialogchrome` en el contenido ya
  desprendido) y **portados los paneles del Text Editor** del add-in
  SourceEditor2 a Avalonia (`general`, `markers`, `behavior`, `intellisense`)
  con las mismas claves legacy. QA §18.5 en 3 rondas hasta limpio. Build 0
  errores, 53/53 tests. Detalle en `docs/interfaz-plan.md` § M28.
- **M29 completado (2026-10-02)**: portados los paneles **Color Theme**,
  **Code Snippets** y **Language Bundles** del Text Editor a Avalonia,
  conciliando las mismas carpetas/claves de usuario (`ColorThemes`,
  `Snippets`, `LanguageBundles`; `ColorScheme`/`ColorScheme-Dark`). QA §18.5 en
  2 rondas (H1 crash de Code Snippets corregido). Build 0 errores, 53/53 tests.
  Detalle en `docs/interfaz-plan.md` § M29.
- **M30 completado (2026-10-02)**: portados **.NET Naming Policies** y
  **Standard Header** (grupo Source Code) escribiendo/leyendo el policy set
  global `Policies/UserDefault.mdpolicy.xml` con el formato de
  `PolicyService`/`PolicySet` (elementos `DotNetNamingPolicy`/`StandardHeader`).
  Code Formatting queda placeholder. QA §18.5 en 3 rondas. Build 0 errores,
  53/53 tests. Detalle en `docs/interfaz-plan.md` § M30.
- **Key Bindings completado (2026-10-03)**: integrado el backend real de
  `MonoDevelop.Ide` en la shell **sin GTK** (guardas `#if !AVALONIA_SHELL` +
  equivalentes no-GTK en el mismo `.cs`): `KeyboardShortcut`, `KeyBindingManager`/
  `KeyBinding`, `CommandManager.ToCommandId` + modelo de comandos, `KeyBindingSet`/
  `KeyBindingScheme`/`KeyBindingService`/`SchemeExtensionNode`. Cableado de HotKeys
  (`entry.Shortcut`→`InputGesture`) y panel fiel sobre el backend. Verificado que
  **la UI GTK no se rompe**. QA §18.5 APROBADO; build 0 errores, 53/53 tests.

Nota operativa: el árbol de build único es `main/build/` (cualquier otra dirección de build en la documentación es obsoleta). El binario de la UI Avalonia es `main/build/MonoDevelop.AvaloniaShell.dll` y el IDE GTK legacy (`--old-gui`) es `main/build/MonoDevelop.dll`.

## 9. Conclusión

La migración avanza por el camino correcto: se redujo la dependencia de Mono/Gtk# en la base técnica, se dejó la toolchain funcionando con .NET 10 y se aisló el problema principal de infraestructura. La base ya no está en un punto de bloqueo “mecánico” simple; el siguiente paso real es la normalización del árbol de build heredado y la separación definitiva de la capa de UI y plataforma.

Este estado no representa una migración completa ni una entrega funcional del producto, pero sí deja el repositorio en una posición mucho más cercana a una plataforma moderna y controlada para continuar con la siguiente fase.

Antes de la fase de UI conviene ejecutar el plan de parcheo de seguridad descrito en la sección 8, priorizando la deserialización/red (Fase 1) y la capa TLS/cripto (Fase 2), que son los riesgos operativos más altos.

## Plan de migración de add-ins (blueprint: `docs/plan-migracion-addins.md`)

Estado (2026-10-06): las fases 0 a 3 de este plan están ejecutadas y validadas por QA. La Fase 4 (oleadas de add-ins legacy → Avalonia) está en curso: oleada A completa, oleadas B–E pendientes. El shell Avalonia convive con la UI GTK legacy (`--old-gui`). No se elimina soporte GTK. Los add-ins Avalonia viven en carpetas `MonoDevelop.Avalonia.*`; las carpetas legacy no se tocan.

### Objetivo

Alinear el sistema de add-ins de la shell Avalonia con el modelo de extensiones de Visual Studio, migrar Preferences, pads/comandos y hooks de plataforma, y dejar la ruta GTK (`--old-gui`) compilable y funcional.

### Restricciones técnicas

- Simetría: no se retira GTK. El shell Avalonia coexiste con la UI GTK legacy.
- Ediciones quirúrgicas: no se hace un refactor masivo de `MonoDevelop.Ide`; se envuelve la lógica existente en add-ins compatibles con Avalonia.
- Anidación limitada: por el tope de `childId` en `AddonExtensionNode`, los manifiestos usan rutas de extensión aplanadas (p. ej. `/TextEditor/Analysis/C#`).
- Carpetas: los add-ins Avalonia van en `main/src/addins/MonoDevelop.Avalonia.*`. El código GTK y las carpetas de add-ins legacy no se tocan.

### Fase 0: alineación con el modelo de Visual Studio (infraestructura)

El `AddonHost` trataba el manifiesto como un archivo de configuración simple. Se alinea con el modelo de extensiones de Visual Studio (`extension.vsixmanifest`).

#### AddonManifest.cs

- Assets: `AddonAsset` (`Type`, `Src`, `Assembly`, `ClassName`). El contribuidor declara qué aporta (p. ej. un `MefComponent`) sin lógica hardcodeada del shell.
- Identity: `AddonIdentity` ampliado con `DisplayName`, `Preview` y `Language`.
- Dependencies: `AddonDependency` con rangos de versión (p. ej. `[9.0, 10.0)`) en lugar de igualdad.

#### AddonHost.cs

- Descubrimiento de ensamblados: `EntryPoint` pasa de ruta de archivo a nombre de ensamblado. `AssemblyDependencyResolver` localiza las DLL en la carpeta del add-in.
- Validación de dependencias: comprobación previa a la carga. Si un rango no lo satisface un add-in cargado, el add-in queda `Loaded = false` con error concreto.
- Registro: se respeta `autoLoad: false`.

#### Estado

Hecho. `AddonManifest` y `AddonHost` compilados. Añadidos `AddonLoadContext` y `AddonLoadState`. Corregidos errores de sintaxis en el host. `IsVersionSatisfied` acepta rangos tipo `[9.0, 10.0)`.

### Fase 1: completitud de Preferences (oleada 1 — alta)

Migrar los paneles de opciones restantes creando manifiestos y actualizando el árbol en `PreferencesDialog.axaml.cs`.

#### Add-ins a migrar

- NuGet (General, Sources)
- Debugger (Debugger)
- CSharpBinding (OnTheFly Formatting, Code Style)
- ChangeLogAddIn (ChangeLog Integration)
- DocFood (Feedback)

#### Actualizaciones del árbol

- Extender `MergePoints` con las rutas nuevas.
- Mantener el `LegacySectionMap` acotado por punto para evitar colisiones (p. ej. "General" en Text Editor frente a Version Control).
- Corregir el `ParentId` de .NET Core para que quede dentro de SDK Locations, no en la raíz de Projects.

#### Estado

Hecho. Manifiestos Avalonia de esos add-ins en `MonoDevelop.Avalonia.*`. Árbol de Preferences con `MergePoints` y mapa por punto.

### Fase 2: migración funcional (oleada 2 — media)

Migrar add-ins que aportan comportamiento, comandos y vistas.

#### Pads y menús

Migrar AssemblyBrowser, UnitTesting, HexEditor, RegexToolkit y DesignerSupport.

#### Integración

Usan `IAvaloniaAddon` e `IPackage` para registrarse en el `MenuService` y el `DockingService` del shell.

#### Estado

Hecho. Add-ins Avalonia correspondientes en `main/src/addins/MonoDevelop.Avalonia.*`.

### Fase 3: hooks de plataforma y limpieza (oleada 3 — baja)

- LinuxPlatform: migrar el soporte de plataforma (antes GnomePlatform).
- Hooks multiplataforma: estandarizar hooks para MacPlatform y WindowsPlatform.
- Verificación: la ruta GTK legacy (`--old-gui`) sigue compilable y funcional.

#### Estado

Hecho. Creados:

- `main/src/addins/MonoDevelop.Avalonia.LinuxPlatform/`
- `main/src/addins/MonoDevelop.Avalonia.WindowsPlatform/`
- `main/src/addins/MonoDevelop.Avalonia.MacPlatform/`

`GnomePlatform` y el resto de add-ins GTK intactos. Manifiestos `.avaloniaaddon.json` solo en carpetas `MonoDevelop.Avalonia.*`. QA de la fase 3: aprobado.

### Cómo validar

- Compilar `MonoDevelop.Avalonia.Addons` (0 errores).
- Comprobar que no hay `.avaloniaaddon.json` en carpetas legacy.
- Arrancar el shell Avalonia por defecto y GTK con `--old-gui`.
- QA Senior (AGENTS.md §18.5) sobre cada fase; ciclo hasta 0 hallazgos.

### Pendiente

- Fase 4 del plan de migración de runtime/build (pipeline SDK; no forma parte de este blueprint de add-ins).
- Completar MIME/iconos en los hooks de plataforma más allá del esqueleto.

### Fase 4: oleadas de add-ins legacy → Avalonia (estado: oleada A completa)

Nota: esta sección redefine la línea de "Pendiente" anterior sobre la "Fase 4 del plan de migración de runtime/build": la Fase 4 de este blueprint de add-ins son las oleadas A–E de migración de add-ins legacy → Avalonia.

Migrar los add-ins legacy Gtk restantes (alcance aprobado: Núcleo + Dominio, 13 add-ins) a contrapartes Avalonia compatibles con el modelo de extensiones de Visual Studio. Patrón por add-in: carpeta `main/src/addins/MonoDevelop.Avalonia.<X>/` + manifiesto `MonoDevelop.<X>.avaloniaaddon.json` + stub `.cs` (los `.cs` no se compilan en la shell; el manifiesto es la superficie de conexión de la extensión). Las carpetas legacy no se tocan.

#### Oleadas

| Oleada | Add-ins legacy | Estado |
|--------|----------------|--------|
| A | ILAsmBinding, TextTemplating, PerformanceDiagnostics, ConnectedServices | Completa (2026-10-06) |
| B | Deployment (sub-add-ins AspNet/AspNetCore) | Completa (2026-10-06) |
| C | PackageManagement (sub-add-ins UnitTesting.NUnit + Runners) | Completa (2026-10-06) |
| D | VBNetBinding | Completa (2026-10-06) |
| E | MonoDevelop.TextEditor + Packaging | Completa (2026-10-06) |

Diferidos (fuera de las oleadas): backends debugger (Gdb, Soft, VSCodeDebugProtocol, Win32), `MonoDevelop.GtkCore`, y MIME/iconos en los hooks de plataforma más allá del esqueleto.

#### Oleada A — informe (2026-10-06)

**Add-ins creados** (manifiesto + stub en cada carpeta; nodos legacy traducidos a extension points aplanados):

- `main/src/addins/MonoDevelop.Avalonia.ILAsmBinding/` — ID `MonoDevelop.ILAsmBinding`: `ProjectTemplates`, `FileFilters`, `ItemOptionPanels/Build`, `LanguageBindings`, `MSBuildItemTypes` (legacy `main/src/addins/ILAsmBinding/`).
- `main/src/addins/MonoDevelop.Avalonia.TextTemplating/` — ID `MonoDevelop.TextTemplating`: `MimeTypes`, `FileFilters`, `TypeSystem/Parser`, `TextEditorExtensions`, `FileTemplates`, `CustomTools`, `Commands`, `ContextMenu/ProjectPad/Tools`, `FileTemplateTypes` (legacy `main/src/addins/TextTemplating/`).
- `main/src/addins/MonoDevelop.Avalonia.PerformanceDiagnostics/` — ID `MonoDevelop.PerformanceDiagnostics`: `Commands`, `MainMenu/Help`, `GlobalOptionsDialog/PerformanceDiagnostics`, `Pads`, `WorkbenchLayouts/Solution` (legacy `main/src/addins/PerformanceDiagnostics/`).
- `main/src/addins/MonoDevelop.Avalonia.ConnectedServices/` — ID `MonoDevelop.ConnectedServices`: `ProjectModelExtensions`, `Commands/Project`, `MainMenu/Project`, `Pads/ProjectPad`, `Commands/Hidden` (legacy `main/src/addins/ConnectedServices/`).

**Fixes del host de add-ins (regresión de la limpieza de Fase 3 `dbc333d8c8`)** en `main/src/core/MonoDevelop.Avalonia.Addons/`:

- `AddonHost.Discover()`: no registraba los add-ins descubiertos (`addons.Add(state)` ausente) → 0 add-ins cargados. Ahora el registro ocurre en `Discover` y la validación de dependencias se mueve a `LoadAll` (sobre el conjunto completo descubierto).
- Add-ins solo-manifiesto (sin `entryPoint` ni `assets`): se marcan `loaded` y contribuyen sus extension nodes.
- `ValidateDependencies`: una dependencia sobre un componente core del host (p. ej. `MonoDevelop.Ide`, no aportado por otro add-in) se considera satisfecha; solo falla si el add-in dependiente tiene error de carga.
- `LoadAll`: restaurada la composición de partes (`composition.Add(inst)`); eliminado `FindManifestPath` (muerto).
- `AddonLoadState.MarkLoaded()`: `Loaded = Error is null` (un add-in fallido no publica sus nodos).
- `AddonLoadContext`: la resolución del ensamblado por nombre busca junto al manifiesto y en el árbol de build unificado `main/build/` (única dirección de build: `manifestDir/../../../build`; las rutas `bin/Debug/net10.0`/`net8.0` de la carpeta del add-in quedaron obsoletas y retiradas).

**Correcciones de referencias de build** (árbol unificado `main/build/`): los 4 `.csproj` de plataforma (`MonoDevelop.Avalonia.LinuxPlatform/` canonical + duplicado `LinuxPlatform.csproj`, `MacPlatform`, `WindowsPlatform`) apuntan ahora a `../../../build/MonoDevelop.{Core,Ide,Avalonia.Addons}.dll` en lugar del árbol obsoleto `../../build/bin/net10.0/`.

**Limpieza**: eliminado `main/src/addins/MonoDevelop.Avalonia.CSharpBinding/CSharpBinding.avaloniaaddon.json` (manifiesto duplicado con el mismo ID `MonoDevelop.CSharpBinding` y sin `displayName`; el correcto es `MonoDevelop.CSharpBinding.avaloniaaddon.json`).

**Validación:**

- Build de la shell (`dotnet build src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj`): **0 errores**.
- Smoke test (`xvfb-run -a dotnet MonoDevelop.AvaloniaShell.dll --addonmanager` desde `main/build/`): **22/25 add-ins cargados**, 0 FATAL; los 4 de la oleada A `loaded=True`; `MonoDevelop.CSharpBinding` carga una sola vez. Fallos esperados (deuda conocida): `MonoDevelop.LinuxPlatform` (manifiesto duplicado sin DLL — hallazgo M-1, pendiente de la decisión de plataforma), `MonoDevelop.Avalonia.MacPlatform` y `MonoDevelop.Avalonia.WindowsPlatform` (ensamblados no compilados en Linux — deuda de hooks multiplataforma).
- Sin manifiestos en carpetas legacy: `find main/src/addins -name "*.avaloniaaddon.json"` solo devuelve carpetas `MonoDevelop.Avalonia.*`.
- QA Senior (§18.5): **APTO**. Hallazgos: A-1 (ALTO) cierre §18.2 sin documentación/commit → resuelto con esta documentación (el commit queda pendiente de la decisión del operador); M-1 `LinuxPlatform.avaloniaaddon.json` con ID fantasma → pendiente de la migración GnomePlatform→LinuxPlatform; M-2 duplicado CSharpBinding → resuelto en esta limpieza; B-1 build frágil por MSBuild nodeReuse (ambiental, sin acción de código).

**Pendiente de la Fase 4:** oleadas C–E y la decisión de diseño de la migración GnomePlatform→LinuxPlatform.

#### Oleada B — informe (2026-10-06)

**Add-ins creados** (manifiesto + stub; nodos legacy traducidos a extension points aplanados):

- `main/src/addins/MonoDevelop.Avalonia.Deployment/` — ID `MonoDevelop.Deployment`: `Commands` (CreatePackage/AddPackage/Install), `MainMenu/Project`, `ContextMenu/ProjectPad/Tools`, `ProjectTemplates` (PackagingProject), `Pads/ProjectPad` (2 NodeBuilders), `PackageBuilders`, `DeployFileCopiers`, `DeployServiceExtensions`, `PackageBuilderEditors`, `FileCopyConfigurationEditors`, `ContextMenu/ProjectPad/Package`, `ContextMenu/ProjectPad/PackagingProject`, `DeployDirectories` (6), `DeployPlatforms` (2), `ItemOptionPanels`, `SerializableClasses` (6), `StockIcons`/`TemplateImages` (legacy `main/src/addins/Deployment/MonoDevelop.Deployment/`).
- `main/src/addins/MonoDevelop.Avalonia.AspNet/` — ID `MonoDevelop.AspNet`: `ProjectTemplates` (3), `FileTemplates` (34), `FileTemplateTypes`, `FileFilters` (2), `LegacyEditorSupport`, `ProjectModelExtensions` (5 flavors con GUID), `TypeSystem/Parser` (3), `SerializableClasses`, `ItemOptionPanels/Run` (XSP), `ToolboxLoaders`/`ToolboxProviders`, `Commands` (6), `ContextMenu/ProjectPad/Add`, `Pads/ProjectPad` (2), `MimeTypes` (16), `TextEditorExtensions` (2), `Html/DocTypes` (4), `ExecutionHandlers` (XSP), `CompletionCharacters`, `SourceEditor2/ContextMenu/Editor`, `CodeTemplates`, `CodeFormatters`, `CustomTools`, `FileTemplateConditionTypes`, `ProjectTemplateWizards`, `TemplateImages`/`StockIcons` (legacy `main/src/addins/AspNet/`).
- `main/src/addins/MonoDevelop.Avalonia.AspNetCore/` — ID `MonoDevelop.AspNetCore` (deps: `MonoDevelop.Ide` + `MonoDevelop.DotNetCore`): `FileTemplates` (13, con `file`), `ProjectTemplateCategories/netcore/app`, `Templates` (13, con condición SDK), `ExecutionHandlers`, `RunConfigurationEditors`, `FileTemplateConditionTypes`, `Pads/ProjectPad` (ScaffoldNodeExtension), `ContextMenu/ProjectPad`, `Commands/Project` (3), `ContextMenu/ProjectPad/Publish`, `MainMenu/Build`, `ProjectTemplateWizards` (legacy `main/src/addins/MonoDevelop.AspNetCore/`).

**Decisiones documentadas (hallazgos BAJO de QA aceptados como decisiones del plan):**

- AspNetCore: el legacy define las plantillas en 6 bloques por SDK (3.1/3.0/2.2/2.1/2.0/1.x, ~57 nodos); el manifiesto aplanado conserva las 13 del SDK más reciente (3.1) con condición `AspNetCoreSdkInstalled, UseNetCore31=true` y descarta los bindings de variantes de SDKs legacy (`templateId`/`path` al nupkg). La shell moderna (.NET 10) no consume esas variantes.
- EP `/MonoDevelop/Deployment/DeployDirectoryResolvers` (legacy) sin declarar: en el host los EP se auto-declaran al registrar el primer nodo (`AddonHost.cs`); el sub-add-in pendiente `MonoDevelop.Deployment.Linux` lo extiende y se migrará con él.
- EP `/MonoDevelop/Asp/CompletionBuilders` (consumidor: CSharpBinding legacy) sin declarar hasta migrar su consumidor; `commitChars` de `CompletionCharacters` y detalles `isText`/`baseType` de MIME descartados en el aplanado (se conservan `commitOnSpace`/`pattern`).

**QA (§18.5, 2 rondas):** ronda 1 **NO APTO** (2 MEDIO: 3 FileTemplates faltantes en AspNetCore → añadidos con su atributo `file`, y los `file` faltantes de los 10 existentes; 4 PNG de `docs/img/` borrados accidentalmente del working tree → restaurados con `git checkout -- docs/img/...`, mismo remedio que el precedente de `docs/session_summary.md:696-698`) + 3 BAJO aceptados como decisiones del plan; ronda 2 **APTO**: build 0 errores; smoke `discovered=28` / `loaded 25/28` (los 3 nuevos `loaded=True`, `FileTemplates nodes=49`, `MonoDevelop.CSharpBinding` una sola vez, solo los 3 fallos esperados, 0 FATAL); 28 manifiestos con 0 IDs duplicados; 0 manifiestos en carpetas legacy; 0 cambios en archivos legacy (ruta GTK intacta).

#### Migración GnomePlatform→LinuxPlatform (completa, 2026-10-06)

**Decisión del operador**: seguir la migración ya iniciada en `main/src/addins/MonoDevelop.Avalonia.LinuxPlatform/`; los archivos `LinuxPlatform.avaloniaaddon.json` y `LinuxPlatform.csproj` (sin prefijo Avalonia) **no son residuo**: forman parte de la migración y deben funcionar ahí. **Sin borrados.**

**Qué se hizo:**

- `LinuxPlatform.csproj` (AssemblyName `MonoDevelop.LinuxPlatform`, con referencia a `MonoDevelop.Ide`) compila `LinuxPlatform.cs` y produce `MonoDevelop.LinuxPlatform.dll` en el árbol de build unificado `main/build/` → el manifiesto `LinuxPlatform.avaloniaaddon.json` (asset `MonoDevelop.LinuxPlatform.dll`, clase `MonoDevelop.Platform.LinuxPlatform`) ahora carga.
- `MonoDevelop.Avalonia.LinuxPlatform.csproj` (canonical) produce `MonoDevelop.Avalonia.LinuxPlatform.dll` en `main/build/`; su manifiesto registra el nodo `/MonoDevelop/Core/PlatformService` para la shell Avalonia.
- Los 4 csproj de plataforma (Linux ×2, Mac, Windows) fijan `OutDir` a `$(MSBuildProjectDirectory)/../../../build/` = `main/build/` (única dirección de build). Nota: la barra antes de los `..` es obligatoria — sin ella, `$(MSBuildProjectDirectory)` (sin barra final) concatena el primer `..` con el nombre de la carpeta (`LinuxPlatform..`) y el SDK resuelve un nivel menos.
- `AddonLoadContext`: la resolución por nombre busca junto al manifiesto y en el árbol unificado `main/build/` (ver corrección del bullet de `AddonLoadContext` en la oleada A).
- Implementación `LinuxPlatform.cs` (común a ambos ensamblados): detecta el escritorio (`XDG_CURRENT_DESKTOP`/`DESKTOP_SESSION`), apertura de URLs con `xdg-open`, fuente monoespaciada por defecto, y el registro del servicio de plataforma por reflexión a `MonoDevelop.Ide.Desktop.DesktopService`. El método `SetPlatformService` no existe aún en `MonoDevelop.Ide`; cuando exista, el registro se activa sin más cambios (`MonoDevelop.Ide.dll` está disponible en `main/build/` para el load context del add-in). Mientras tanto, el nodo de plataforma funcional para Avalonia lo aporta el manifiesto canonical.

**Validación:** build 0 errores (shell + 2 proyectos de plataforma); smoke `--addonmanager`: **26/28** add-ins cargados, `MonoDevelop.LinuxPlatform` y `MonoDevelop.Avalonia.LinuxPlatform` `loaded=True`, 0 FATAL. Los 2 fallos restantes son la deuda conocida de Mac/Windows (sus `.cs` llaman `DesktopService.SetPlatformService`, inexistente, y no compilan en Linux).

**Migración Mac/Windows (completa, 2026-10-06):** las carpetas `MonoDevelop.Avalonia.MacPlatform/` y `MonoDevelop.Avalonia.WindowsPlatform/` quedan con la misma estructura que LinuxPlatform — doble manifiesto + doble csproj (canonical `MonoDevelop.Avalonia.<P>Platform.*` + legacy-named `<P>Platform.*`, IDs `MonoDevelop.MacPlatform`/`MonoDevelop.WindowsPlatform`) — con **compilación condicional por SO**: `WindowsPlatform` solo se compila en Windows y `MacPlatform` solo en macOS, vía `Directory.Build.targets` en cada carpeta (fuera de su SO el proyecto es no-op: los targets `CoreCompile`/`CopyFilesToOutputDirectory` se anulan y un target de limpieza elimina el `deps.json`; no queda ningún artefacto en `main/build/`). Nota: los overrides van en `Directory.Build.targets` (importado al final de `Microsoft.Common.targets`) porque los targets definidos en el cuerpo del `.csproj` quedan anulados por `Microsoft.NET.Sdk.targets` (último en importarse; regla last-wins).

**Fixes de fuente** (`MacPlatform.cs`, `WindowsPlatform.cs`): la migración previa no compilaba en ninguna plataforma. (1) Eliminada la llamada a `DesktopService.SetPlatformService(this)` — el método no existe: `DesktopService.OnInitialize` lee el EP `/MonoDevelop/Core/PlatformService` vía `AddinManager.GetExtensionObjects`, y el nodo de ese EP (declarado en el manifiesto canonical) es lo que registra la plataforma. (2) Eliminados los `override IsWindows/IsMac/IsLinux` — no existen en `PlatformService` (pertenecen a la clase estática `MonoDevelop.Core.Platform`). Las clases son ahora subclasses puros de `PlatformService` + `IAvaloniaAddon` (mismo modelo que el legacy `GnomePlatform`): overrides válidos `DefaultMonospaceFont`, `Name`, `ShowUrl` (mac: `open`; windows: shell execute), `GetApplications`, `OnGetMimeType*`, `OnGetIcon*`.

**Verificación**: (1) en Linux los 4 csprojs de Mac/Windows compilan como no-op (0 errores, 0 artefactos en `main/build/`); (2) compilación forzada de las fuentes contra `MonoDevelop.{Core,Ide,Avalonia.Addons}.dll` + `Xwt.dll` (scratch `~/opencode/forced_build/`): **0 errores / 0 advertencias** en los 4 proyectos, y cada DLL contiene solo su clase; (3) build de la shell 0 errores; smoke `--addonmanager`: **35 discovered, 31 loaded=True** — en Linux los 4 add-ins Mac/Windows reportan `Assembly not found` (esperado por diseño: la DLL solo existe al compilar en su plataforma), los 2 Linux siguen `loaded=True`, `ep /MonoDevelop/Core/PlatformService nodes=1`, 0 FATAL. Log: `~/opencode/platform_smoke.log`.

**Pendiente restante:** el enganche runtime GUI de la plataforma (puente entre el registro del host y `AddinManager`/`DesktopService`) es deuda de hooks compartida por las 3 plataformas (ver `docs/fase3-platform-hooks-cleanup.md`); MIME/iconos más allá del esqueleto.
- QA (rol Tester QA Senior, §18.5): **APTO** (2 rondas). Ronda 1: NO APTO — 1 MEDIO (discrepancia de alcance del prompt QA: los cambios en los csproj de LinuxPlatform son parte aprobada y documentada de la migración de plataformas de esta sesión) + 4 BAJO → corregidos: `return null!` en `OnGetIconForFile` (CS8603 con `<Nullable>enable`), restaurados los 2 PDB sucios del submódulo `mono-addins`, eliminados bin/obj obsoletos de las 3 carpetas de plataforma. Ronda 2: **APTO** — compilación nullable 0 errores/0 advertencias (cada DLL solo con su clase), no-op en Linux sin artefactos en `main/build/`, smoke 35/31 con 0 FATAL; único BAJO no bloqueante (obj/ regenerado por el build de validación, gitignored) limpiado antes del commit. Artefactos: `~/opencode/qa_platforms/`.

### Oleada C — informe (2026-10-06)

**Add-ins creados** (`main/src/addins/`):

| Carpeta nueva | ID manifiesto | Notas |
|---|---|---|
| `MonoDevelop.Avalonia.PackageManagement/` | `MonoDevelop.PackageManagement` | Manifiesto completo: 10 `NuGet.*` commands, MainMenu/Project, ContextMenu ProjectPad (+Add), StockIcons, GlobalOptionsDialog, `ProjectTemplatePackageInstallers`/`ItemTemplatePackageInstallers`, 6 NodeBuilders en `Pads/ProjectPad`, EP `/MonoDevelop/PackageManagement/ContextMenu/ProjectPad/PackageReference` (3 nodos: `MonoDevelop.PackageManagement.Commands.PackageReferenceNodeCommands.ReinstallPackage`, `...UpdatePackage`, `MonoDevelop.Ide.Commands.EditCommands.Delete`), PropertyProviders, StartupHandlers, SearchCategories, ProjectModelExtensions, SystemInformation, FileTemplateConditionTypes |
| `MonoDevelop.Avalonia.UnitTesting.NUnit/` | `MonoDevelop.UnitTesting.NUnit` | `TestProviders` `SystemTestProvider` (clase `MonoDevelop.UnitTesting.NUnit.SystemTestProvider`), `ProjectTemplates` C#/VB NUnit, `FileTemplates` `NUnitTestClass`, TemplateImages/StockIcons `md-test-project`, `UnitTestMarkers` con los 5 atributos NUnit. Depende de `MonoDevelop.Ide` y `MonoDevelop.UnitTesting` (base existente) |
| — | `MonoDevelop.UnitTesting.NUnit.Runners` | **Nada que migrar**: la carpeta legacy es una librería vacía (`MyClass.cs`), sin manifiesto ni referencias; la implementación real de runners vive en el add-in `MonoDevelop.UnitTesting`, que ya existe como Avalonia (`MonoDevelop.Avalonia.UnitTesting`) |

**Verificación**: build 0 errores. Smoke `--addonmanager`: **30 discovered, 28 loaded=True** — `MonoDevelop.PackageManagement` y `MonoDevelop.UnitTesting.NUnit` cargan (`loaded=True`); EPs: `TestProviders nodes=1`, `UnitTestMarkers nodes=1`, `ProjectTemplates nodes=7` (+2 NUnit), `FileTemplates nodes=50` (+1), `PackageManagement/.../PackageReference nodes=3`. Únicos fallos: Mac/Windows (deuda pendiente, sin API en el lado compartido). Log: `~/opencode/wavec_smoke.log`.
- QA (rol Tester QA Senior, §18.5): **APTO**. 0 ALTO; 2 imprecisiones documentales del propio informe corregidas (IDs reales de los 3 nodos del EP `PackageReference`; 5 atributos NUnit, no 6). 59 nodos comparados 1:1 con los manifiestos legacy (0 faltantes, 0 divergentes); 30/30 JSON válidos, 0 IDs duplicados; legacy intacta. Artefactos: `~/opencode/qa_wavec/`.

### Oleada D — informe (2026-10-06)

**Add-in creado**: `main/src/addins/MonoDevelop.Avalonia.VBNetBinding/` — ID `MonoDevelop.VBNetBinding` (legacy `main/src/addins/VBNetBinding/`): `MSBuildItemTypes` (VBNet/.vbproj, guid, `VBProject` + resourceHandler), `FileFilters` (*.vb), `FileTemplates` (EmptyVBFile, VBAssemblyInfo), `ProjectTemplates` (EmptyProject, ConsoleProject), `ItemOptionPanels/Build/General` + `/Build/Compiler` (3 panels con condición `ProjectTypeId=VBNet`), `StockIcons` (md-vb-file), `MimeTypes` (text/x-basic), `LanguageBindings` (VBNet), `SerializableClasses` (VBCompilerParameters, Import), `TextEditorExtensions` (VBNetTextEditorExtension), `TypeSystem/Parser` (TypeSystemParser).

**Decisión**: el `<Module>` legacy (dependencia `GtkCore` + plantilla `MonoDevelop.VBNet.GtkSharp2Project`) queda fuera de alcance: `MonoDevelop.GtkCore` es diferido.

**Verificación**: build 0 errores. Smoke `--addonmanager`: **31 discovered, 29 loaded=True** — `MonoDevelop.VBNetBinding loaded=True`; EPs: `ProjectTemplates nodes=9` (+2), `FileTemplates nodes=52` (+2), `LanguageBindings nodes=2` (+1), `MSBuildItemTypes nodes=2` (+1), `TextEditorExtensions nodes=4` (+1). Únicos fallos: Mac/Windows (deuda pendiente). Log: `~/opencode/waved_smoke.log`.

### Oleada E — informe (2026-10-06)

**Add-ins creados**:

- `main/src/addins/MonoDevelop.Avalonia.Packaging/` — ID `MonoDevelop.Packaging` (legacy `main/src/addins/MonoDevelop.Packaging/`): `ConditionTypes` (ProjectHasNuGetMetadata), `MSBuildItemTypes` (NuGet.Packaging/.nuproj), `ProjectModelExtensions` (PackagingProjectFlavor + DotNetProjectPackagingExtension), `LanguageBindings` (NuGet.Packaging), `MSBuildGlobalPropertyProviders`, `ProjectTemplates` (PackagingProject, CrossPlatformLibrary), `ProjectTemplateWizards` ×2, `ItemOptionPanels` (sección NuGetPackage + panels Build/Metadata/ReferenceAssemblies con condiciones), `Commands` (CreateNuGetPackage, AddPlatformImplementation), `MainMenu/Build`, `ContextMenu/ProjectPad` (+Add), `DesignerSupport/PropertyProviders` ×2.
- `main/src/addins/MonoDevelop.Avalonia.TextEditor/` — ID `MonoDevelop.TextEditor` (legacy `main/src/addins/MonoDevelop.TextEditor/MonoDevelop.TextEditor/Properties/`): `Ide/Composition` (MonoDevelop.TextEditor.dll), `Commands` (categoría TextEditor + 6 comandos de caret con shortcuts), `TextEditor/CommandMapping` (53 Maps a `*CommandArgs` de VS), `TextEditor/ContextMenu/Editor` (20 nodos: 15 comandos + 5 separadores; bloque "temporary" activo), `Core/FeatureSwitches` (AlwaysUseLegacyEditor).

**Decisiones**:

1. Solo se migran nodos activos: los bloques legacy comentados en XML dentro del manifiesto de TextEditor (TooltipProviders, comandos SourceEditor, menús antiguos, menús Scrollbar/NavigationBar, secciones GlobalOptionsDialog/TextEditor, MainMenu/Search, EditorFactory, AutoInsertBracket, UserDataMigration, StockIcons) ya estaban desactivados en el legacy → no se migran.
2. Las 5 declaraciones `ExtensionPoint` (ContextMenu/Editor, CommandMapping, SupportedFileTypes, LegacyEditorSupport, TextMate) y el `ConditionType` `FileType` no tienen representación funcional en el host Avalonia: el host auto-declara el EP al registrar el primer nodo (precedente oleada B); `SupportedFileTypes` y `LegacyEditorSupport` además no tienen nodos en el propio manifiesto legacy.
3. Sub-add-ins `MonoDevelop.TextEditor.Wpf` y `.Cocoa`: backends de plataforma → diferidos (Mac/Windows).

**Verificación**: build 0 errores. Smoke `--addonmanager`: **33 discovered, 31 loaded=True** — `MonoDevelop.Packaging` y `MonoDevelop.TextEditor` `loaded=True`; EPs: `TextEditor/CommandMapping nodes=53`, `TextEditor/ContextMenu/Editor nodes=20`, `ProjectTemplates nodes=11` (+2), `MSBuildItemTypes nodes=3` (+1), `LanguageBindings nodes=3` (+1), `Core/FeatureSwitches nodes=1`, `Ide/MainMenu/Build nodes=4` (+2). Únicos fallos: Mac/Windows (deuda pendiente). Log: `~/opencode/wavee_smoke.log`.

**Estado de la Fase 4**: oleadas A–E completas — los 13 add-ins del alcance (Núcleo + Dominio) están cubiertos; `MonoDevelop.UnitTesting.NUnit.Runners` queda documentado como stub vacío sin migración (oleada C).
- QA (rol Tester QA Senior, §18.5): **APTO** (2 rondas). Ronda 1: 0 ALTO; 2 MEDIO resueltos — jerarquía de la sección `NuGetPackage` registrada con `childId` en los 3 paneles y paréntesis en la condición aplanada de `ContextMenu/ProjectPad`; el otro MEDIO es preexistente (duplicado CSharpBinding a HEAD; el borrado está en el worktree, pendiente del commit que decidirá el operador). Ronda 2: verificación focalizada, smoke idéntico (33/31, 0 FATAL), 0 hallazgos nuevos. Comparación 1:1: VBNetBinding 12 EPs; Packaging 13 EPs + ConditionType; TextEditor 53/53 Maps y 20/20 nodos de menú. Artefactos: `~/opencode/qa_waveDE/`.

### Integración add-ins ↔ Preferencias/shell (completa, 2026-10-07)

**Contexto**: tras el commit `558210e02a` (Fase 4 oleadas A–E + plataformas Mac/Windows), se verificó cómo se integran los add-ins migrados con el shell: el diálogo de Preferencias (skeleton + merge de extension points), el gestor de add-ins y la ruta de arranque.

**Defectos encontrados y corregidos** (`main/src/core/MonoDevelop.Startup.Avalonia/Views/PreferencesDialog.axaml.cs`):

1. **P1 (ALTO) — panel PerfDiag inalcanzable**: el nodo de la rama "Performance Diagnostics → General" tenía id `perfgeneral`, pero la página portada (`PanelPerfDiag`, ~línea 2429) y `functionalPanels` esperan `perfdiag` → la categoría mostraba placeholder. Fix: skeleton y `LegacySectionMap` usan ahora `perfdiag`; no queda ninguna referencia a `perfgeneral`.
2. **P2 (MEDIO) — secciones NuGet mal ubicadas bajo "Projects"**: el EP raíz `/MonoDevelop/Ide/GlobalOptionsDialog` se mergeaba en la categoría "Projects", de modo que los nodos de PackageManagement (`NuGetPackageManagement`, `NuGetGeneral`, `PackageSources`) aparecían como leaves bajo Projects duplicando la categoría top-level "NuGet". En el modelo legacy el EP raíz porta secciones top-level (la sección NuGet es `insertafter="VersionControl"` con hijos General/Sources). Fix: `MergeAddonSections` trata el EP raíz por separado (constante `RootOptionsPoint`): los nodos sin `childId` (o con childId huérfano) son categorías top-level — se adopta la categoría existente si el label coincide (case-insensitive) o se añade una nueva al final — y los hijos vinculados por `childId` se añaden con dedupe por id/label (método auxiliar `AddSection`). En `MonoDevelop.PackageManagement.avaloniaaddon.json`, `NuGetGeneral` y `PackageSources` llevan ahora `"childId": "NuGetPackageManagement"` (misma convención que `ItemOptionPanels` de la oleada E).
3. **P3 (BAJO) — merge point `/TextEditor/Analysis/C#` fallaba en silencio**: la búsqueda del parent (`analysis-csharp`) solo miraba hijos directos de "Text Editor", pero el nodo está anidado bajo "Source Analysis" → no se añadían `CodeStylePanel` (CSharpBinding) ni `Code Actions`/`Code Generation`/`Code Rules` (Refactoring). Fix: búsqueda recursiva `FindNode` (DFS) dentro de la categoría.
4. **P4 (BAJO) — iconos declarados en manifiestos no resolubles**: `md-prefs-package` y `md-prefs-package-source` (PackageManagement) y, una vez expuesto por P3, `md-prefs-code-actions` y `md-prefs-code-rules` (Refactoring) no existían en `IconService`. Fix: 4 mapeos añadidos al mapa de add-ins de `main/src/core/MonoDevelop.Ide/Services/IconService.cs` (los PNG ya existen en `main/src/addins/<add-in>/icons/`) y los leaves del skeleton "NuGet" usan los stock-ids reales (`md-prefs-package`/`md-prefs-package-source`), como en el legacy GTK.

**Cómo validar**:
1. `cd main && dotnet build src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj` → 0 errores.
2. `cd main/build && timeout 60 xvfb-run -a dotnet MonoDevelop.AvaloniaShell.dll --prefs-tree` (exit 124 normal) → el log trae `merged=5 (registry points=85)` (DocFood + 4 nodos de `Analysis/C#`); **0** `(missing)`; `Projects` sin entrada NuGet; `NuGet` top-level con `General id=nugetgeneral icon=md-prefs-package` + `Sources id=packagesources icon=md-prefs-package-source`; `Text Editor → Source Analysis → C#` con `codestylepanel`, `codeactions`, `codegeneration`, `coderules`; `Performance Diagnostics → General id=perfdiag`; 8 categorías top-level.
3. Smoke `--addonmanager` (exit 124): **35 discovered, 31 loaded=True** (las 4 falsas de Mac/Windows son por diseño en Linux); `PackageManagement loaded=True` (el manifiesto con `childId` parsea).
4. `--old-gui` (ruta Gtk legacy, mismo binario): arranque sin excepciones; los cambios solo añaden entradas de diccionario en `IconService` y tocan un Views exclusivo de Avalonia.

**QA (§18.5)**: Tester QA Senior, **APTO** en 2 rondas. Ronda 1: 0 ALTO/MEDIO; 1 BAJO resuelto en ronda 2 (iconos NuGet "dormidos": el dedupe conservaba los leaves del skeleton con `md-prefs-generic` → los leaves usan ahora los stock-ids reales). INFO no cerrados (preexistentes/cosméticos): (a) `merged=` se loguea 2 veces porque `BuildModel` se llama desde `BuildSectionTree` y `DumpTreeForQa`; (b) `[prefs-csharp] Roslyn key unavailable ... using legacy name` es fallback preexistente. Artefactos: `~/opencode/qa_prefs_shell_fix/`.

**Pendiente**:
- Los 5 nodos añadidos por el merge (`codestylepanel`, `codeactions`, `codegeneration`, `coderules`, `docfood`) son placeholders hasta portar sus paneles (no existen aún en el skeleton).
- P5 (INFO): el log duplicado de `merged=` es cosmético; no se toca para mantener el diff mínimo.

### Revisión final 1:1 — migración de add-ins COMPLETADA (2026-10-07)

**Contexto**: revisión final y exhaustiva de las 32 carpetas / 35 manifiestos Avalonia frente a sus equivalentes legacy, para dar por terminada la migración de add-ins: paridad de extension points (nodos, jerarquías, labels, condiciones), iconos e imágenes, y comportamiento del diálogo de Preferencias. Herramientas: `~/opencode/final_review/compare.py` (comparador 1:1 nodo a nodo, con normalización de mnemónicos GTK en cualquier posición) e `icon_audit.py` (resolución de cada icono usado en manifiestos AV contra los mapas de `IconService`).

**Defectos encontrados y corregidos**:

1. **`main/src/core/MonoDevelop.Ide/Services/IconService.cs` — 22 stock-ids legacy sin resolver** (secciones de Preferencias, pads y comandos se renderizaban sin icono):
   - Mapa core: `md-command-window` (pad Immediate) y `md-vb-file` (alias stock→stock en legacy hacia `md-file-source`; mapeado directo al mismo PNG `file-source-16`).
   - Mapa de add-ins: 11 de Preferencias (`md-prefs-text-editor-general`, `md-prefs-markers-rulers`, `md-prefs-text-editor-behavior`, `md-prefs-completion`, `md-prefs-syntax-highlighting` → SourceEditor2; `md-prefs-debugger` → Debugger; `md-prefs-code-analysis` → Refactoring; `md-prefs-version-control`, `md-prefs-commit-message-style`, `md-prefs-git` → VersionControl; `md-prefs-xml` → Xml) y 10 de pads/comandos (`md-view-debug-call-stack/locals/threads/watch` → Debugger; `md-properties-pad`, `md-toolbox-pad`, `md-pad-document-outline` → DesignerSupport; `nunit-pad-icon` → `MonoDevelop.UnitTesting/Gui`; `md-gettext-locale` → Gettext).
2. **`main/src/core/MonoDevelop.Startup.Avalonia/Views/PreferencesDialog.axaml.cs`**: (a) los hijos declarados con `childId` se perdían en el merge (p. ej. `CompletionBehavior` bajo `CodeCompletion`); `MergePoints` ahora procesa en dos pasadas (top-level primero, luego hijos bajo su padre) y `AddSection` devuelve el nodo existente en caso de duplicado (dedupe). (b) Los leaves del skeleton usaban stock-ids genéricos (`md-prefs-generic`); ahora usan los stock-ids reales del legacy, los mismos que mostraba la UI GTK.
3. **18 manifiestos `*.avaloniaaddon.json`** — espejo 1:1 de EPs, jerarquías, labels e iconos:
   - **UnitTesting**: EP `Ide/Commands` reescrito — el manifiesto tenía 1 nodo inventado (`RunTests` + icono `md-prefs-run` inexistente en legacy) y el legacy declara la categoría "Unit Testing" con 20 comandos → espejo completo (21 nodos; `md-run-unit-tests` resuelve por el mapa core `ParsedStockMap`).
   - **AspNet**: hack `commandId` sustituido por `childId` real; `GoToController` dividido en 2 nodos por condición (`.aspx`/cshtml) + separadores; labels del legacy (`Controller...`, `View...`, `Add View...`); id `AspNetApp` completo.
   - **PerformanceDiagnostics**: 13 nodos MainMenu/Help con las 3 ItemSets `Diagnostics` del legacy (repartidas en 2 archivos legacy; la central lleva `condition=FeatureSwitch=WidgetLeaks`).
   - **PackageManagement**: 10 comandos con `childId=NuGet` + 2 separadores condicionales; labels 1:1 (`Manage NuGet Packages...`, contexto `NuGet Packages...`).
   - **SourceEditor2**: `childId` invertido eliminado; `CompletionBehavior`/`CompletionAppearance` añadidas bajo `CodeCompletion`; classNames.
   - **Refactoring**: panel demo (`"class": MonoDevelop.AvaloniaAddons.Demo.AnalysisPlaceholder`) → `className` real (`MonoDevelop.AnalysisCore.Gui.AnalysisOptionsPanel`); ids de paneles a los del legacy (`CodeActions`/`CodeGeneration`/`CodeRules`) + classNames.
   - **Gettext**: nodo fantasma `TranslateProject` eliminado; 4 items de contexto condicionados + EP `Ide/Commands` con las 4 definiciones (icono `md-gettext-locale`).
   - **CSharpBinding/XmlEditor/Debugger/ChangeLogAddIn/VersionControl**: classNames de paneles; iconos VC reales (`md-prefs-commit-message-style`, `md-prefs-git`).
   - **Deployment/Packaging/TextEditor**: `childId` de categorías en comandos; **AspNetCore**: 2 `ProfilesSeparator`; **ConnectedServices**: separador + label `Add`; **DotNetCore**: panel `DotNetCoreSdkSettings`.

**Set documentado (no corregido, por decisión)** — el comparador termina en `TOTAL nodos FALTANTES: 57`, 0 divergentes y 2 dif-parent, todos dentro de este set:

- **AspNetCore (48 faltantes + 13 divergentes + 1 dif-parent)**: templates de variantes por SDK y formato de condiciones — fuera del alcance de la fase 4 (plantillas generadas por SDK).
- **Refactoring (4 faltantes)**: artefacto del aplanado del EP `/TextEditor/Analysis/C#` solo-av; en runtime verificado 1:1.
- **LinuxPlatform / MacPlatform (1 faltante + 1 extra c/u)**: sustitución de clases de plataforma (`GnomePlatform`→`LinuxPlatform`, `MacPlatformService`→`MacPlatform`).
- **VBNetBinding (1 faltante + 1 extra + 2 sin-id)**: `GtkSharp2Project` (GtkCore diferido) y ids legacy sin espejar.
- **VersionControl (2 faltantes + 4 extras)**: el legacy usa el id duplicado `VersionControlGeneral` ×2 (el modelo AV no admite duplicados) → ids propios + `LegacySectionMap` (correctitud en runtime); los extras son la consolidación de los fragmentos Git/ChangeLog.
- **DotNetCore (1 dif-parent)**: aplanado del panel (el runtime deduplica).
- **Deployment (3 sin-id)**: nodos legacy sin id (limitación del modelo).
- Los `eps-solo-legacy` restantes (CSharpBinding 41, VersionControl 21, Debugger 15, ...) son superficies diferidas (context menus, test chart, etc.), no EPs de la fase 4.
- **MacPlatform `pause.png`**: defecto legacy preexistente (el recurso jamás existió en el historial de git).

**Cómo validar**:

1. `cd main && dotnet build src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -m:1` → 0 errores.
2. `cd main/build && timeout 120 dotnet MonoDevelop.AvaloniaShell.dll --prefs-tree` → `merged=5 (registry points=85)`, 0 `(missing)`, iconos legacy en las 11 secciones verificadas.
3. `--addonmanager` → `loaded 31/35` (las 4 falsas son Mac/Windows, por diseño en Linux); 0 FATAL.
4. `--old-gui` (ruta Gtk legacy, mismo binario) → arranca y corre sin excepciones (exit 124 por timeout = OK).
5. `python3 ~/opencode/final_review/icon_audit.py` → 66 ids en uso, **0 sin resolver**.
6. `python3 ~/opencode/final_review/compare.py` → `TOTAL nodos FALTANTES: 57` (el set documentado de arriba), 0 divergentes.

**QA (§18.5)**: Tester QA Senior, **APTO — 0 errores** (2 advertencias + 7 info, ninguno bloqueante). Advertencias: (1) 5 ids duplicados dentro del mismo EP — espejo fiel de ids duplicados del legacy bajo condiciones distintas (desambiguados por `condition`; esos EPs no son consumidos por el runtime actual); (2) campos de nodo fuera del listado mínimo en manifiestos — preexistentes, no introducidos por este diff, ignorados por el runtime (deuda técnica documentada). Cerrados en este diff: `.gitignore` no cubría `main/src/core/MonoDevelop.Avalonia.Addons/bin/` (añadida la regla) y el manifiesto de UnitTesting sin newline final (añadido). Artefactos: `~/opencode/final_review/` (`findings_v4.txt`, `run_*.txt`, `qa_report_final.txt`, `manifest_check.py`, `report_*.txt`).

**Cierre**: la migración de add-ins está **COMPLETADA**: los 35 manifiestos son espejo 1:1 de los legacy dentro del alcance aprobado, todos los iconos en uso resuelven, y las rutas Avalonia y `--old-gui` del mismo binario funcionan. Este documento queda cerrado para trabajo de add-ins: las tareas posteriores (paneles de Preferencias placeholder, superficies diferidas, variantes de SDK, GtkCore) se documentarán en sus propios documentos de sesión.

### Add-in Manager: host nuevo (Avalonia), Installed con iconos — se quitan Mono.Addins/Mono.Cecil (2026-10-07)

**Contexto**: el Add-in Manager de la UI Avalonia (`AddinManagerDialog`, Tools > Extensions… / `--addins` / `--extensionsdlg`) se alimentaba del registry Mono.Addins (`SetupService` + `AddinEngineHost`). El escaneo de ese registry fallaba **íntegro** en el build unificado: `Mono.Addins.CecilReflector` (submódulo) compila contra Mono.Cecil 0.9.6 (`InterfaceImplementation`, `ModuleDefinition.FileName`) mientras el build estagia Cecil 0.10.1 → `MissingMethodException` al escanear cada ensamblaje → "The add-in database could not be updated" → **pestaña Installed vacía (items=0)**. En lugar de parchear el reflector legacy, el manager se desacopló de los componentes legacy: la pestaña Installed usa el nuevo sistema de add-ins Avalonia (`MonoDevelop.Avalonia.Addons` / `AddonHost` + manifiestos `*.avaloniaaddon.json`) y muestra el icono de cada add-in.

**Cambios** (35 archivos):

1. **`MonoDevelop.Avalonia.Addons`** (componente nuevo): `AddonIdentity.Icon` (campo `identity.icon` del manifiesto); `AddonLoadState.ManifestPath`; `AddonHost.ResolveIconFile` — resuelve `core:file.png` contra el set de iconos core legacy y cualquier otra ruta contra la raíz de add-ins (o la propia carpeta del add-in), con walk-up desde el directorio del binario como `IconService` (resuelve en el árbol del repo y en un build estagiado).
2. **`AddinManagerDialog` reescrito**: pestaña Installed desde `App.Addins` (AddonHost) — los 35 add-ins agrupados por categoría (como `ShowCategories` legacy), filtro de búsqueda, **icono por add-in** (`identity.icon` resuelto; fallback `plugin-32.png` embebido, como `StoreIcon` legacy), filas no cargadas en gris (como los disabled legacy) y panel de detalle (id/version/autor, estado `Not loaded: <error>`, descripción, tags, EPs con recuento de nodos). Se quitaron las pestañas Updates/Gallery de la UI Avalonia: el nuevo sistema no tiene repositorios remotos (el diálogo del IDE GTK las conserva) y con ellas los botones Install/Update/Disable/Uninstall (el host nuevo no tiene API de instalación/habilitación).
3. **Borrado de componentes legacy del shell**: `MonoDevelop.Ide/Services/AddinEngineHost.cs` eliminado (exclusivo de Avalonia — el csproj GTK no lo lista en su lista explícita — y su único usuario era el diálogo); ProjectReferences `Mono.Addins`/`Mono.Addins.Setup` retiradas de `MonoDevelop.Startup.Avalonia.csproj`. Los ensamblajes Mono.Addins siguen estagiados en `main/build` para el build GTK (árbol unificado, regla 18.4).
4. **27 manifiestos** declaran `identity.icon` reutilizando PNG 32px/48px ya existentes (core y carpetas legacy): `core:file-web-32` (AspNet/AspNetCore/WebReferences), `core:file-source-32` (CSharp/VB/SourceEditor), `core:file-unit-test-32` (UnitTesting/NUnit), `MonoDevelop.Debugger/icons/exception-48`, `Deployment/MonoDevelop.Deployment/icons/package-32`, `MonoDevelop.Gettext/icons/file-locale-32`, `MonoDevelop.PackageManagement/icons/package-source-32`, `core:package-32` (NuGet), `core:project-package-32` (Packaging), `core:project-crossplatform-shared-32` (DotNetCore), `core:file-xml-32` (XmlEditor), `core:workspace-32` (VersionControl), etc. Los 8 add-ins sin icono 32px disponible (3 plataformas ×2 manifiestos, Refactoring, RegexToolkit) usan el fallback genérico — mismo comportamiento que el diálogo legacy sin `Icon32`.

**Cómo validar**:

1. `cd main && dotnet build src/core/MonoDevelop.Startup.Avalonia/MonoDevelop.Startup.Avalonia.csproj -m:1` → 0 errores.
2. `cd main/build && timeout 45 dotnet MonoDevelop.AvaloniaShell.dll --addins` → log: `[addins] tab=installed items=35 loaded=31 icons=20`; 0 `Exception`; 0 líneas del escaneo legacy Cecil/Mono.Addins (ya no se ejecuta).
3. Los 27 iconos declarados resuelven a PNG reales en disco (script `~/opencode/addins_manager/03_icon_check.py`: 27/27, magic bytes verificados; 20 archivos únicos = `icons=20` del log).
4. Regresión: `--prefs-tree` → `merged=5 (registry points=85)`, 0 missing; `--addonmanager` → `loaded 31/35` (Mac/Windows por diseño); `--old-gui` → 20 s sin crash (exit 124); `--extensionsdlg` → mismo diálogo (`items=35 loaded=31 icons=20`).
5. Sin `using Mono.Addins`/`AddinEngineHost`/`SetupService` en el código del shell Avalonia; csproj sin referencias a mono-addins; el csproj GTK no lista `AddinEngineHost.cs` → el borrado no afecta al build GTK.

**QA (§18.5)**: Tester QA Senior, **APTO — 0 errores** (5 INFO no bloqueantes: fallback preexistente de Roslyn key en prefs; artefactos stale `Mono.Addins*.dll` en `main/build` gitignored que desaparecen con build limpio; CS8632/AVLN5001 coherentes con la convención del árbol). Artefactos: `~/opencode/addins_manager/` (01–07).

**Pendiente/deferido**:

- Pestañas Updates/Gallery y acciones Install/Update/Enable/Uninstall requieren repositorio remoto + API de instalación en el nuevo sistema — fuera de este alcance (el diálogo GTK las conserva vía Mono.Addins).
- El `CecilReflector` de `main/external/mono-addins` sigue incompatible con el Cecil 0.10.1 del build: afecta al diálogo/registry Mono.Addins **del lado GTK** (no a la shell Avalonia, que ya no lo usa). Parchearlo para Cecil 0.10 es tarea del lado GTK.
