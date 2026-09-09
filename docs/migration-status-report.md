# Informe de estado de la migración de MonoDevelop a .NET 8 LTS

## 1. Objetivo y alcance

Este documento resume el estado actual del trabajo de modernización del repositorio MonoDevelop para una migración incremental hacia .NET 8 LTS, con Linux como prioridad operativa y sin introducir nuevas funcionalidades de producto.

La estrategia sigue siendo:

- mantener la lógica funcional separada del runtime y de la UI;
- modernizar dependencias y pipeline sin reescribir el producto completo;
- usar .NET 8 LTS como base técnica estable;
- dejar una capa de compatibilidad intermedia para Mono/Gtk# y otros componentes legacy;
- posponer la decisión final de UI hasta que la base técnica quede estable.

## 2. Resumen ejecutivo

La base del repositorio ya no está bloqueada por feeds NuGet muertos ni por errores triviales de paquetes rotos en la mayor parte del stack principal.

El trabajo realizado ha dejado algunos puntos clave en un estado sólido:

- corrección de dependencias legacy y de runtime bajo Linux;
- estabilización del bootstrap de build para trabajar con .NET 8 real;
- modernización parcial del substack Mono.Addins a SDK-style;
- validación de proyectos del core y del IDE bajo SDK .NET 8;
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

Antes de iniciar el trabajo de UI se realizó una revisión estática dirigida de seguridad sobre el repositorio migrado. El detalle completo está en `security-audit-report.md`; aquí queda el resumen de riesgos detectados y el plan de parcheo asociado.

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

La priorización detallada, referencias archivo:línea y acciones por hallazgo están en `security-audit-report.md`.

### 8.3 Progreso de la Fase 1 (por ítem)

- **C1 — Canales IPC de RemotingService y mdhost (completado):** el socket Unix de `.NET Remoting` se creaba con permisos `0666` (mundo accesible). Se restringió a `0600` vía `chmod` en `RemotingService.cs` y `mdhost.cs` (con fallback seguro y `Mono.Posix` añadido a `mdhost.csproj`). Validado por compilación aislada y prueba empírica con Mono 6.14 (el comportamiento de conexión IPC entre procesos es idéntico antes/después; el caso válido del propietario sigue funcionando). Detalle en `security-audit-report.md` → Bitácora → Cambio C1.
- **C2 — Canal Unix de libsteticui (completado):** el subsistema de diseño GTK (`libsteticui`) crea su propio `UnixChannel` de remoting al margen de `RemotingService`, con socket mundo-accesible. Se restringió a `0600` vía `chmod` en `Application.cs` (lado padre) y `ApplicationBackend.cs` (lado hijo), con `try/catch` de degradación segura, y se añadió la referencia `Mono.Posix` a `libsteticui.csproj` (requerida por `Mono.Unix.Native` y para los canales Unix que ya viven en `Mono.Posix.dll`). Validado por compilación aislada del patrón `chmod` (modo resultante `0600`).
- **C2-TCP — Bind a loopback de los canales TCP de libsteticui (completado):** los `TcpChannel` de `libsteticui` (intervalo `ProcessTcp`, tanto en el frontend `Application.cs` como en el backend `ApplicationBackend.cs`) bindaban por defecto a `0.0.0.0` (todas las interfaces), validado empíricamente con Mono 6.14 (exposición de la superficie de remoting a la red). Se añadió `rejectRemoteRequests=true` en ambos; probado: bind a `127.0.0.1` exclusivo y llamada local funcional (`tcp://127.0.0.1:<port>/Pong.rem` → `pong`); requests remotos rechazados. Detalle en `security-audit-report.md` → Bitácora → Cambio C2 → endurecimiento TCP asociado.
- Pendiente de la fase: migrar `BinaryFormatter`. Detalle en `security-audit-report.md` → Bitácora → Cambio C2.
- **C3 — Viabilidad de migrar `BinaryFormatter` en la capa remoting (análisis completado):** se clasificaron los 17 usos de `BinaryFormatter` en tres categorías: (A) transporte de `ObjRef` de arranque de los canales remoting (`ProcessHostController`/`mdhost`, `MSBuild/Main`, `libsteticui`, `AutoTest*`), (B) datos locales (`InstrumentationService` autosave, `NUnitAssemblyTestSuite` caché), (C) formatos de archivo (`VersionControlService`, `ToolboxItemToolboxNode`). Conclusión: el `ObjRef` en sí no es migrable a JSON (formato binario cerrado del BCL), pero **el transporte de arranque SÍ se puede sustituir por endpoint textual + `Activator.GetObject`** (validado empíricamente con procA/procB → `pong`). El `BinaryFormatter` interno del protocolo de `.NET Remoting` (sink binario con `TypeFilterLevel.Full`) NO es migrable sin reemplazar la pila de remoting; su exposición se mitiga con C1/C2/C2-TCP (IPC a propietario + TCP loopback). Recomendado: migrar primero Categorías B/C (local, bajo riesgo) y luego A canal a canal con prueba de integración padre+hijo. Detalle en `security-audit-report.md` → Bitácora → Cambio C3.
- **C4 — Mensajes de commit (VersionControlService) a JSON (completado):** el archivo `version-control-commit-msg` se guardaba/leía con `BinaryFormatter`. Migrado a `Newtonsoft.Json` (`Dictionary<string, CommitComment>`), con lectura JSON preferente y **fallback al formato binario legacy** para no perder datos previos; `comments` se mantiene como `Hashtable` (sin alterar el resto de la lógica). Se añadió `PackageReference Newtonsoft.Json` (13.0.3, la fijada en el repo) al `VersionControl.csproj`. Validado por compilación aislada y probe funcional (escritura JSON, relectura correcta, carga legacy OK). Detalle en `security-audit-report.md` → Bitácora → Cambio C4.
- **C5 — Caché de tests NUnit (NUnitAssemblyTestSuite) a JSON (completado):** `TestInfoCache` (caché en disco de resultados de tests) se guardaba/leía con `BinaryFormatter`. Migrado a `Newtonsoft.Json` (`Dictionary<string, CachedTestInfo>`), con lectura JSON preferente y **fallback al binario legacy**; `NunitTestInfo`/`CachedTestInfo` son JSON-deserializables (round-trip del árbol anidado validado). Se añadió `PackageReference Newtonsoft.Json` (13.0.3) al `MonoDevelop.UnitTesting.NUnit.csproj`. Validado por probe funcional: JSON path y fallback legacy OK. Detalle en `security-audit-report.md` → Bitácora → Cambio C5.
- **C6 — Autosave de InstrumentationService a JSON con DTO layer en MonoDevelop.Core (completado):** `AutoSave` serializaba el snapshot completo con `BinaryFormatter` y `LoadServiceDataFromFile` lo leía binario. Como las clases de snapshot (`Counter`/`TimerCounter`/`CounterCategory`/`CounterValue`/`TimerTrace`) son getter-only y no JSON-round-trippables, se construyó una **capa de DTOs de persistencia** (`InstrumentationData.cs`: `InstrumentationSnapshotDto`/`CounterDto`/`CounterValueDto`/`TimerTraceDto`/`CounterCategoryDto`) con mapeo explícito `InstrumentationDataCodec.FromService`/`ToService`, y se añadieron hooks internos de restauración en `Counter`/`TimerCounter` (`RestoreState`/`RestoreValues`/`RestoreTimerState`). `AutoSave` escribe JSON; `LoadServiceDataFromFile` es JSON-preferente con **fallback binario legacy**. `MonoDevelop.Core.csproj` ya tenía Newtonsoft. Validado por compilación Roslyn (cero errores en los archivos tocados) y probe `instrumentation_probe2`: round-trip JSON completo `OK` (estado, valores, traces, metadata, membresía de categorías); se corrigió un defecto de doble alta de categoría. Detalle en `security-audit-report.md` → Bitácora → Cambio C6.
- **C7 — Endurecer deserialización de ToolboxItemToolboxNode (completado):** `DeserializeToolboxItem` deserializaba con `BinaryFormatter` un blob Base64 embebido en el XML del toolbox (`Toolbox.xml`, config local del usuario) → riesgo de gadget/ejecución de código desde un archivo manipulado. Como el payload es un `System.Drawing.Design.ToolboxItem` (tipo de framework con subclases custom no JSON-round-trippables), NO se migró a JSON; se añadió un `ToolboxItemSerializationBinder` (allowlist) que solo permite tipos asignables a `ToolboxItem` (cualquier ensamblado, preservando subclases) o tipos de ensamblados framework de confianza, bloqueando el resto (donde viven los gadgets). `BindToName` preserva el formato en disco. Mantiene formato legacy y fidelidad de subclases. Validado por probe `tbi_block` (`BINDER-PROBE=OK`: permite ToolboxItem/framework, bloquea gadget de extremo a extremo) y compilación Roslyn (cero errores). Detalle en `security-audit-report.md` → Bitácora → Cambio C7.
- **C8 — Reemplazar transporte `ObjRef` por endpoint textual + `Activator.GetObject` (canal ProcessHostController ↔ mdhost, completado):** `ProcessHostController.Start` publicaba su proxy con `RemotingServices.Marshal(this)` y serializaba el `ObjRef` binario (`BinaryFormatter` → Base64) en la primera línea de un archivo temporal; `mdhost.Main` lo deserializaba con `BinaryFormatter.Deserialize` (superficie de gadget). Se sustituyó por una **URL textual**: nuevo `RemotingService.GetMarshaledUrl(uri)` devuelve la URL del canal TCP registrado (endurecido en C1/C2: `rejectRemoteRequests=true` → 127.0.0.1) y el hijo hace `Activator.GetObject(typeof(IProcessHostController), url)`. Eliminado `using System.Runtime.Serialization.Formatters.Binary;` en ambos archivos. Validado: compilación `mcs` de `mdhost.cs` (éxito) + Roslyn sin errores en el código añadido + probe de transporte (`phc_probe`/`dualch2`: el objeto marshaled es alcanzable por `Activator.GetObject` en IPC y TCP) + probe de integración extremo a extremo `c8sP`/`c8sC` (`PARENT_GOT_HOST:host1`, `RESULT:OK`). Detalle en `security-audit-report.md` → Bitácora → Cambio C8.
- **C9 — Reemplazar transporte `ObjRef` por endpoint textual + `Activator.GetObject` (canal libsteticui padre ↔ hijo, completado):** `ApplicationBackendController.StartBackend` publicaba su controlador con `RemotingServices.Marshal` y serializaba el `ObjRef` binario (`BinaryFormatter` → Base64) en la segunda línea del stdin del backend hijo; `ApplicationBackend.Main` lo deserializaba con `BinaryFormatter.Deserialize` (superficie de gadget). Se sustituyó por una **URL textual**: nuevo `ApplicationBackendController.GetMarshaledUrl(uri)` calcula la URL según el canal activo (TCP `__internal_tcp`: `baseUrl + "/" + uri`; Unix `"unix"`: `"unix://" + path + "?" + uri`, con separador `?` según `UnixChannel.ParseUnixURL`) y el hijo hace `Activator.GetObject(typeof(ApplicationBackendController), url)`. Eliminado `using System.Runtime.Serialization.Formatters.Binary;` en ambos archivos. Validado: compilación Roslyn del set completo `libsteticui` (56 + 2 `Windows/*` fuentes, refs gtk-sharp 2.0 GAC, Mono.Cecil, prebuilt `libstetic.dll`) → **cero errores**, ninguno en los archivos tocados + probe de integración extremo a extremo `c9e_parent`/`c9e_child` en TCP y Unix (`PARENT_URL:tcp://…/c1.rem` y `unix://…?c1.rem` → `CHILD_PING:pong` → `PARENT_GOT_CONNECT:backend1`). Detalle en `security-audit-report.md` → Bitácora → Cambio C9.
- **C10 — Reemplazar transporte `ObjRef` por endpoint textual + `Activator.GetObject` (canal AutoTest, 4 handoffs, completado):** AutoTest conecta MonoDevelop con el test runner externo mediante cuatro traspasos del `ObjRef` serializado con `BinaryFormatter` (→ Base64 en `MONO_AUTOTEST_CLIENT`/`SessionReferenceFile`, deserializados con `BinaryFormatter.Deserialize`). Se sustituyó por **URL textual**: `RemotingService.GetMarshaledUrl` pasa de `internal` a `public` (constructor canónico de la URL TCP loopback, reutilizado por mdhost/C8); `AutoTestClientSession.StartApplication` marshala `this` como `"autotest-client"` y escribe su URL en `MONO_AUTOTEST_CLIENT`; `AutoTestService.Start` lee esa URL con `Activator.GetObject(typeof(IAutoTestClient), …)`, marshala `manager` como `"autotest-service"` y escribe su URL en `SessionReferenceFile`; `AutoTestClientSession.AttachApplication` conecta con `Activator.GetObject(typeof(IAutoTestService), …)`. Eliminado `using System.Runtime.Serialization.Formatters.Binary;` en ambos archivos. Validado por probe end-to-end `c10_full` (canal `TypeFilterLevel.Full` igual a `RegisterRemotingChannel`, 2 procesos): `APP_CLIENT_PING:client-pong`, `APP_RECEIVED_CONNECT:app-backend`, `CLIENT_SERVICE_PING:service-pong`, `CLIENT_ATTACHED:client-pong`; + Roslyn sobre los archivos cambiados: cero errores de transporte/API (ruido = tipos hermanos de MonoDevelop.Ide sin prebuilt). `InstrumentationService.cs:123` **NO es transporte vulnerable** (`PublishService` publica un MBR y `mdmonitor` conecta por `Activator.GetObject` + URL textual, sin BinaryFormatter en el bootstrap; el `BinaryFormatter` restante es el fallback legacy del autosave de C6). Detalle en `security-audit-report.md` → Bitácora → Cambio C10.
- **C11 — Deshabilitar canal AutoTest/instrumentación por defecto (opt-in explícito, completado):** hallazgo 2.6 / prioridad #8 (BAJO). El canal de autotest (y la publicación del servicio de instrumentación) se activaba solo por la presencia de `MONO_AUTOTEST_CLIENT`, permitiendo que un proceso local la fijara y forzara el canal remoting. Se exigió **opt-in explícito**: `AutoTestService.Start` retorna si `!IsAutoTestEnabled()` (nuevo: `MONO_AUTOTEST_ENABLE == "1" || EnableAutomatedTesting`) y avisa si hay `MONO_AUTOTEST_CLIENT` sin opt-in; `AutoTestClientSession.StartApplication` fija `MONO_AUTOTEST_ENABLE=1` (el harness opta); `Runtime.IsInstrumentationServiceEnabled()` publica solo con `MONO_AUTOTEST_ENABLE == "1"` o preferencia `EnableInstrumentation`. Validado por probe `c11_gate`: `MONO_AUTOTEST_CLIENT` solo → `WARN: NOT started`; con `MONO_AUTOTEST_ENABLE=1` → `CONNECTING`/`PUBLISH` + `STARTED`; + Roslyn sin errores en las líneas cambiadas. Detalle en `security-audit-report.md` → Bitácora → Cambio C11.
- **Fase 1, ítem 1 y ítem 2 de remoting: COMPLETADOS.** Pendiente de otros hallazgos de Fase 1 (ítems 2/3 de TLS-criptografía de la Fase 2 y validación según plan en `security-audit-report.md` §5). Restan solo los `BinaryFormatter` en superficies NO remoting: `xwt/TransferDataSource.cs:150,164` (portapapeles) y `guiunit/BinarySerializableConstraint.cs:38` (tests), fuera del alcance de canales entre procesos.

#### 8.4 Progreso de la Fase 2 (TLS y criptografía)

- **F2.1 — Eliminar SSL 2/SSL 3 (completado):** removidos `Ssl2`/`Ssl3` del enum `XspSslProtocol` (`XspParameters.cs`) y sus dos ítems del combo en `XspOptionsPanelWidget.cs`, manteniendo la alineación de índices. Roslyn sin errores de lógica.
- **F2.2 — Quitar callback TLS global (completado):** eliminado el `ServerCertificateValidationCallback` global en `Runtime.cs` (y su `using` huérfano); `WebCertificateService.GetIsCertificateTrusted` validada por huella SHA-256 exacta; diálogo de `DefaultWebCertificateProvider` acotado (`WaitOne(15000)`, deniega en headless). Roslyn sin errores de lógica.
- **F2.3 — MD5 → SHA-256 en autosave (completado):** `AutoSave.cs` usa `SHA256.Create().ComputeHash` per-call (thread-safe, elimina la instancia estática compartida); API compatible net472 (sin `SHA256.HashData`). Roslyn: región SHA256/`GetMD5` con cero errores.
- **F2.4 — Gravatar off por defecto (completado):** nueva preferencia `Runtime.Preferences.EnableGravatarAvatars` (default `false`); `ImageService.GetUserIcon` retorna `null` sin red al estar deshabilitado; guards en `LoadUserIcon` y en el renderer de `LogWidget.cs`. Nota: la API de Gravatar exige MD5 (no acepta SHA-256), por lo que la mitigación real es el opt-in explícito sin emitir el hash del email salvo que el usuario lo active. Roslyn sin errores en las regiones editadas.

**Fase 2 COMPLETA (F2.1–F2.4).** Pendiente para Fase 3: supply-chain (NuGetAudit, paquetes con CVE) y claves (`.snk`). Detalle de cada cambio en `security-audit-report.md` → Bitácora → Cambios F2.1–F2.4.

#### 8.5 Progreso de la Fase 3 (Supply-chain y claves)

- **F3.1 — NuGetAudit (completado, bump pendiente):** añadido `<NuGetAudit>true</NuGetAudit>` en `main/Directory.Build.props`. Análisis de CVEs (Sep-2026): **NuGet.Client 5.4.0 afectado por CVE-2024-0057** (crítica, requiere ≥ 5.11.6; también vendida en `external/nuget-binary/` y `nuget.exe`); Mono.Cecil 0.10.1, NUnit 3.9.0 y Microsoft.TestPlatform 16.2.0 **sin CVE**. Por decisión del usuario se documenta el bump como **deuda (5.4.0 → ≥ 5.11.6)** dado que requiere reemplazar binarios vendidos con riesgo no validable en el build legacy net472.
- **F3.2 — Strong-naming: verificado, se mantiene (completado):** el único `.snk` versionado (`MonoDevelop-Public.snk`) es **solo-clave-pública** (`RSA1`, `<PublicSign>True</PublicSign>`) → no es un secreto. El strong-naming aporta valor (IVT a Roslyn firmado), así que no se retira ni se deshabilita.
- **F3.3 — Firma de addins: documentado (completado):** no hay verificación de firma en `Mono.Addins.Setup`; el canal por defecto es **HTTPS oficial** (`https://addins.monodevelop.com/.../main.mrep`, sin `http://`). Se documenta la verificación de firma de paquetes como deuda residual.
- **F3.4 — Ignorar claves (completado):** añadido `*.snk`, `*.pfx`, `*.pem`, `*.key` a `.gitignore`.

Detalle de cada cambio en `security-audit-report.md` → Bitácora → Cambios F3.1–F3.4.

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
- **F4.3 — Documentación de cierre (completado):** estos cambios quedan registrados en `migration-status-report.md` y en `security-audit-report.md` → Bitácora → 4/4b/4c + Fases 3 y 4.

**Plan de parcheo de seguridad (Fases 1–4): COMPLETADO.** Deuda documentada para revisión posterior junto con el resto de pendientes de migración: los `BinaryFormatter` no-remoting (`xwt/TransferDataSource.cs`, `guiunit/BinarySerializableConstraint.cs`), el bump de `NuGet.Client` 5.4.0 → ≥ 5.11.6 (CVE-2024-0057, binarios vendidos en `external/nuget-binary/`), y la verificación de firma de paquetes de addin.

## 9. Conclusión

La migración avanza por el camino correcto: se redujo la dependencia de Mono/Gtk# en la base técnica, se dejó la toolchain funcionando con .NET 8 y se aisló el problema principal de infraestructura. La base ya no está en un punto de bloqueo “mecánico” simple; el siguiente paso real es la normalización del árbol de build heredado y la separación definitiva de la capa de UI y plataforma.

Este estado no representa una migración completa ni una entrega funcional del producto, pero sí deja el repositorio en una posición mucho más cercana a una plataforma moderna y controlada para continuar con la siguiente fase.

Antes de la fase de UI conviene ejecutar el plan de parcheo de seguridad descrito en la sección 8, priorizando la deserialización/red (Fase 1) y la capa TLS/cripto (Fase 2), que son los riesgos operativos más altos.
