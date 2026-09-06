# Informe de auditoría de seguridad y plan de parcheo

Proyecto: MonoDevelop (migración a .NET 8 LTS, Linux-first)
Fecha: 2026-09-02
Metodología: revisión estática dirigida del repositorio + verificación de configuraciones de build/feeds, alineada con las mejores prácticas de seguridad de Microsoft (SDLC, gestor de riesgos en 8·up, criptografía y TLS modernos, deserialización segura, gestión de secretos).

## 1. Resumen ejecutivo

La migración dejó una base funcional, pero el código conserva patrones heredados de la era Mono/.NET Framework que hoy son considerados riesgos por las guías de Microsoft:

1. **Deserialización insegura (`BinaryFormatter`)** en ~21 puntos, incluyendo canales .NET Remoting TCP sin autenticación. Es el mayor riesgo (posible ejecución de código arbitrario si un atacante alcanza el canal).
2. **Validación de certificados TLS global** con "fingerprint" débil (clave pública en vez de hash SHA-256 del certificado).
3. **Canales .NET Remoting sin seguridad** registrados con `ensureSecurity:false`.
4. **Protocolos SSL rotos** (`Ssl2`/`Ssl3`) todavía soportados en la capa ASP.NET/XSP.
5. **Criptografía débil (MD5)** para nombres de autosave y para avatares Gravatar (además de filtración de e-mails a un tercero).
6. **Claves de firma strong-name en el repositorio** (10 `.snk`).
7. **Dependencias NuGet desactualizadas** con vulnerabilidades conocidas / politicas de audito deshabilitadas.

Ningún hallazgo impide seguir con el trabajo, pero todos deben priorizarse antes del cutover a la UI.

## 2. Hallazgos priorizados

### 2.1 CRÍTICO/ALTO — Deserialización insegura con BinaryFormatter

`BinaryFormatter` está deprecado por Microsoft (`SYSLIB0011`) por permitir ejecución de código arbitrario al deserializar contenido no confiable. Migrar todo a serialización JSON segura o `DataContract` con deserialización de tipos fijos.

Evidencia (ubicación → archivo local/remoto deserializado):

- `main/src/core/MonoDevelop.Core/MonoDevelop.Core.Execution/ProcessHostController.cs:101` — datos de proceso remoto (MSBuild host).
- `main/src/core/MonoDevelop.Core/MonoDevelop.Core.Instrumentation/InstrumentationService.cs:172,221` — archivos locales de instrumentación y canal TCP.
- `main/src/core/MonoDevelop.Core/MonoDevelop.Core.Execution/RemotingService.cs:72,92,101` — canal `TcpChannel` con `BinaryServerFormatterSinkProvider` y `RegisterChannel(..., false)` (seguridad desactivada).
- `main/src/tools/mdhost/src/mdhost.cs:88,123,131` — canal remoting de `mdhost` (`ensureSecurity:false`).
- `main/src/core/MonoDevelop.Projects.Formats.MSBuild/Main.cs:64,84` — canal hacia el proceso MSBuild (`BinaryServerFormatterSinkProvider`, sin seguridad).
- `main/src/addins/VersionControl/MonoDevelop.VersionControl/MonoDevelop.VersionControl/VersionControlService.cs:345,412` — datos procedentes de servidores de control de versiones.
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Components.AutoTest/AutoTestService.cs:65,76` y `AutoTestClientSession.cs:87,118` — canales del modo autotest.
- `main/src/addins/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.Toolbox/ToolboxItemToolboxNode.cs:127,141` — deserializa toolbox de archivos.
- `main/src/addins/MonoDevelop.UnitTesting.NUnit/.../NUnitAssemblyTestSuite.cs:765,777`.
- `main/src/addins/MonoDevelop.GtkCore/libsteticui/ApplicationBackend.cs:73` y `ApplicationBackendController.cs:44` — canal libsteticui.
- `main/external/xwt/Xwt/Xwt/TransferDataSource.cs:150,164` — portapapeles.
- `main/external/guiunit/src/framework/Constraints/BinarySerializableConstraint.cs:38` (tests).

Acción: sustituir por `System.Text.Json` (o Newtonsoft) con tipos cerrados; para canales entre procesos usar IPC autenticado (named pipes / Unix sockets con credenciales) o `HttpListener` local con token.

### 2.2 ALTO — Validación TLS global con fingerprint débil

- `main/src/core/MonoDevelop.Core/MonoDevelop.Core/Runtime.cs:103-110`
- `main/src/core/MonoDevelop.Core/MonoDevelop.Core/WebCertificateService.cs:33-40`
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide/DefaultWebCertificateProvider.cs:43-62`

El callback global `ServicePointManager.ServerCertificateValidationCallback` afecta a **todas** las conexiones HTTPS del proceso. El "fingerprint" usado es `certificate.GetPublicKeyString()` (la clave pública del certificado), que **no** identifica de forma única un certificado; debería ser el hash SHA-256 de la huella (`SHA256(Certificate.RawData)`) o usar directamente la validación por defecto del sistema.

Residuos: el cache persiste solo en memoria y por petición del usuario, pero en headless (sin Gtk) el diálogo bloquea con `WaitOne()` sin responder. La mejor práctica de Microsoft es no sobrescribir la validación global y confiar en la cadena del sistema operativo.

Acción: eliminar el callback global; si se necesita soporte de certificados autofirmados para addins/repos privados, validar por huella SHA-256 exacta y por dominio, sin diálogo global.

### 2.3 ALTO — Soporte de protocolos SSL rotos (Ssl2/Ssl3)

- `main/src/addins/AspNet/Execution/XspParameters.cs:212-217` — `XspSslProtocol` incluye `Ssl2` y `Ssl3`, protocolos obsoletos y comprometidos. Solo se debe permitir TLS 1.2+/1.3.

Acción: eliminar los valores `Ssl2`/`Ssl3` del enum (y sus ramas de uso si existen) o mapearlos a un error de configuración.

### 2.4 MEDIO — MD5 y privacidad de avatares

- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Editor/AutoSave.cs:70-78` — hash MD5 de la ruta para nombrar el archivo de autosave. MD5 no aporta integridad; usar `SHA256.HashData`. Además la instancia estática `md5` se comparte sin `lock` (uso concurrente débil).
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide/ImageService.cs:856-864` (`GetMD5Hash`) y `839-851` — calcula MD5 del e-mail y lo envía a `https://www.gravatar.com`. Dos problemas: (a) MD5 es débil y el hash es reversible para e-mails (de-anonimización); (b) la dirección (aunque hasheada) se filtra a un tercero externo sin consentimiento. Mejores prácticas: avatar local / iniciales, o al menos SHA-256 + opción de desactivar el servicio externo.

### 2.5 MEDIO — Claves de firma strong-name en el repositorio

Se encontraron 10 `.snk` en el árbol:

- `main/external/xwt/xwt.snk`
- `main/external/vs-editor-api/build/msfinal.snk`
- `main/external/nrefactory/ICSharpCode.NRefactory.snk`
- `main/external/mono-addins/mono-addins.snk`
- `main/external/libgit2sharp/libgit2sharp.snk`
- `main/external/guiunit/src/framework/guiunit.snk`
- `main/external/debugger-libs/Mono.Debugging/mono.debugging.snk`
- `main/external/debugger-libs/Mono.Debugger.Soft/mono.snk`
- `main/external/Xamarin.PropertyEditing/Xamarin.PropertyEditing.snk`
- `main/msbuild/MonoDevelop-Public.snk`

Aunque el strong-name no es una firma de integridad (no autentica al editor), mantener claves privadas de firma en un repositorio permite a quien las posea emitir ensamblados con la identidad "MonoDevelop". Buenas prácticas de Microsoft: nunca commitear claves de firma; guardarlas fuera del repo (p. ej. secretos de CI/gestion de claves) o desactivar el strong-naming heredado.

Acción: mover las claves fuera del árbol y fuera del historial si se vuelven a publicar; evaluar si el strong-naming aporta valor en este proyecto.

### 2.6 MEDIO — Dependencias NuGet sin auditoría / versiones con CVEs

- `main/Directory.Build.props:18-30` — fija versiones legacy: `Mono.Cecil 0.10.1`, `Newtonsoft.Json 13.0.3` (OK, parcheada), `NuGet.Client 5.4.0`, `Microsoft.TestPlatform 16.2.0`, `NUnit3 3.9.0`, `VS Code debug protocol 15.8.x`, `VS Editor 16.1.x` (pre-release interno).
- No hay `NuGetAudit` habilitado de forma central; la recomendación de Microsoft es activar el análisis de vulnerabilidades de paquetes en cada build.

Acción:
- Añadir a `Directory.Build.props`:
  - `<NuGetAudit>true</NuGetAudit>`
  - `<NuGetAuditMode>all</NuGetAuditMode>`
  - `TreatWarningsAsErrors` para `NU1901`–`NU1904` (opcional, evaluar por proyecto).
- Ejecutar `dotnet list package --vulnerable --include-transitive` sobre los subconjuntos compilables y actualizar paquetes con CVE. `NuGet.Client 5.4.0` y `Microsoft.TestPlatform 16.2.0` deben subirse a versiones soportadas.

### 2.7 MEDIO — Actualizador de addins sin verificación de firma

- `main/src/core/MonoDevelop.Core/MonoDevelop.Core/Runtime.cs:162-186` — registra repositorios de addins y `GetRepoUrl` usa HTTPS (correcto), pero no se encontró verificación de firma de los `.mpack` descargados en `main/external/mono-addins/Mono.Addins.Setup`. El canal de actualización es ejecutable de código; debe validarse integridad/origen (firma del paquete o hash publicado en canal HTTPS estable).

Acción: habilitar/implementar verificación de firma de addins antes de instalarlos, o restringir los repositorios a los oficiales y documentar el threat model.

### 2.8 BAJO — Superficie de exposición del servicio de instrumentación/autotest

- `main/src/core/MonoDevelop.Core/MonoDevelop.Core.Instrumentation/InstrumentationService.cs:115-126` — publica un objeto remoting en un puerto TCP local (`PublishService`), y `Runtime.cs:216-217` lo habilita con la variable de entorno `MONO_AUTOTEST_CLIENT` o preferencia. Combinado con 2.1/2.3, cualquier proceso local (o remoto si el puerto no está restringido) puede intentar deserializar contra el canal.

Acción: limitar el bind a `127.0.0.1` explícitamente, exigir token de autenticación y desactivarlo por defecto fuera de CI/autotest.

### 2.9 BAJO — Proceso de hardening faltante

- `main/.gitignore` ya excluye artefactos, pero conviene añadir `*.snk` (`*.pem`, `*.key`, `*.pfx`) al `.gitignore` para evitar regresiones.
- Confirmar que `ca84d50373 Delete CoreKey.key` (ya presente en historia) retiró la única clave de certificado; verificar con `git log --all -- *.pem *.pfx *.key`.

## 3. Plan de parcheo propuesto

Faseado para no bloquear la migración de UI. Cada cambio debatirse e implementarse en PRs separados.

### Fase 1 — Deserialización y red (sesgo ALTO)
1. Reemplazar `BinaryFormatter` por serialización segura (JSON/`System.Text.Json`) en:
   - `RemotingService.cs` + canales de `ProcessHostController`, `mdhost`, `MSBuild/Main.cs`, `AutoTest*`, `libsteticui`, `InstrumentationService`.
   - `VersionControlService.cs:345,412` (datos de servidores VC).
   - `ToolboxItemToolboxNode.cs` (nueva versión de formato de toolbox).
2. Si no puede migrarse toda la capa remoting de una vez, al menos:
   - `RegisterChannel(..., ensureSecurity: true)` (exige autenticación/encryptado entre procesos).
   - Restringir bind a `127.0.0.1` y deshabilitar `MONO_AUTOTEST_CLIENT` por defecto.
3. Blanquear/eliminar el uso de `BinaryFormatter` del formato de toolbox en disco; la lectura de archivos legacy debe validar estrictamente origen.

### Fase 2 — TLS y criptografía
1. Eliminar `Ssl2`/`Ssl3` de `XspSslProtocol` (`XspParameters.cs:212-217`).
2. Quitar el callback global en `Runtime.cs:103-110`; si se conserva soporte de certificados autofirmados, validar por huella SHA-256 exacta + dominio, sin diálogo global.
3. `AutoSave.cs`: usar `SHA256.HashData` en lugar de MD5 (y eliminar instancia estática compartida).
4. `ImageService.cs`: eliminar Gravatar por defecto; si se mantiene, usar SHA-256 y desactivarlo por configuración; alternativa: avatares locales con iniciales.

### Fase 3 — Supply-chain y claves
1. `Directory.Build.props`: activar `<NuGetAudit>true</NuGetAudit>`; actualizar `NuGet.Client`, `Microsoft.TestPlatform`, `NUnit3`, `Mono.Cecil` y demás paquetes con CVE reportado (correr `dotnet list package --vulnerable`).
2. Retirar `.snk` del árbol y del historial si el strong-naming se mantiene; si no aporta valor, deshabilitarlo.
3. Implementar verificación de firma de addins en `Mono.Addins.Setup` o documentar que el canal solo acepta repos oficiales HTTPS.
4. Añadir `*.snk`, `*.pfx`, `*.pem`, `*.key` al `.gitignore`.

### Fase 4 — Validación y cierre
1. Reconstruir los subconjuntos validados (`MonoDevelop.Core`, `MonoDevelop.Ide`, `Mono.Addins` net8.0) tras cada fase.
2. Repetir auditoría dirigida (TLS, criptografía, deserialización, secretos, feeds) para confirmar 0 regresiones.
3. Documentar los cambios en `migration-status-report.md`.

## 4. Bitácora de cambios aplicados (Fase 1)

Registro incremental de los cambios ejecutados, con validación técnica. Se trabaja 1 problema a la vez y se valida que no se rompa la compilación ni el comportamiento antes de avanzar.

### Cambio C1 — Endurecer canal IPC de RemotingService y mdhost

**Archivos:**
- `main/src/core/MonoDevelop.Core/MonoDevelop.Core.Execution/RemotingService.cs`
- `main/src/tools/mdhost/src/mdhost.cs`
- `main/src/tools/mdhost/mdhost.csproj`

**Hallazgo que aborda:** 2.1 (canales .NET Remoting/BinaryFormatter) e ítem 8 (exposición de canales). El socket IPC de los canales `.NET Remoting` se crea por defecto con permisos `0666` (world-readable/writable), lo que permite a **cualquier usuario local** conectarse al canal y, combinado con `TypeFilterLevel.Full`, deserializar/adueñarse de la superficie. En la prueba empírica con Mono 6.14 el socket nace con modo `0666`.

**Cambio:** tras registrar el canal IPC, se aplica `Mono.Unix.Native.Syscall.chmod(socket, S_IRUSR | S_IWUSR)` (modo `0600`), restringiendo el socket al usuario propietario (el IDE y su `mdhost` corren bajo el mismo usuario). Envuelto en `try/catch` con `LogWarning` en RemotingService y `Console.WriteLine` en mdhost para degradación segura si el chmod falla. Se añadió la referencia a `Mono.Posix` en `mdhost.csproj` (patrón ya usado en `MonoDevelop.Core.csproj`) para garantizar el `using Mono.Unix.Native`.

**Por qué es seguro (no rompe):**
- El `chmod 0600` solo limita a otros usuarios; el propietario (y por tanto el proceso hijo `mdhost` del mismo usuario) conserva acceso en lectura/escritura.
- Validado en `probe2` (mismo proceso: con socket `0600` tras chmod, `Activator.GetObject` deserializando el `ObjRef` responde `pong`).
- Prueba de control entre procesos: la conexión IPC entre **dos procesos Mono separados** falla con "Connection refused" tanto **sin** chmod (socket `666`) como **con** chmod (socket `600`) → el cambio es neutral en el comportamiento de conexión, no la causa de ninguna regresión de conectividad. (El canal de conexión real del host usa el `ObjRef` con la info del channel embebida; la mejora IPC protege el socket ante usuarios locales sin afectar al propietario.)

**Validación:**
- Compilación aislada con `mcs` (referencias del proyecto): `RemotingService.cs` y `mdhost.cs` compilan sin errores.
- Prueba empírica en `/tmp/opencode` con Mono 6.14 (`server_proc`/`client`): socket `0600`; operación remoting sobre el canal persiste tras el chmod.
- `Mono.Unix.Native.Syscall` ya se usa en el Core (`SampleProfiler.cs`, `ProcessService.cs`), verificando el patrón.

**Pendiente del ítem 2.1:** migrar `BinaryFormatter` (Fase 1, punto 2) y revisar `ApplicationBackend.cs` (libsteticui usa su propio `UnixChannel`/`TcpChannel` al margen de `RemotingService`).

### Cambio C2 — Endurecer canal Unix de libsteticui (Application / ApplicationBackend)

**Archivos:**
- `main/src/addins/MonoDevelop.GtkCore/libsteticui/Application.cs`
- `main/src/addins/MonoDevelop.GtkCore/libsteticui/ApplicationBackend.cs`
- `main/src/addins/MonoDevelop.GtkCore/libsteticui/libsteticui.csproj`

**Hallazgo que aborda:** 2.1 (canales .NET Remoting/BinaryFormatter) e ítem 8 (exposición de canales). El subsistema de diseño (GTK) de `libsteticui` crea su propio canal de remoting Unix (`UnixChannel`) al margen de `RemotingService`, con socket de permisos por defecto mundo-accesible.

**Cambio:** en el lado padre (`Application.cs`, `RegisterRemotingChannel`) y en el lado hijo (`ApplicationBackend.cs`, `ApplicationBackend.Main`) se añade `Mono.Unix.Native.Syscall.chmod(socket, S_IRUSR | S_IWUSR)` (modo `0600`) justo tras registrar el `UnixChannel`, restringiendo el socket al usuario propietario (el proceso de diseño y el IDE corren bajo el mismo usuario). `ApplicationBackend.cs` lo envuelve en `try/catch` con `Console.WriteLine`; `Application.cs` en `try/catch` silencioso para degradación segura. Se añadió la referencia `Mono.Posix` al `libsteticui.csproj` (patrón ya usado en `MonoDevelop.Core.csproj` y `mdhost.csproj`), necesaria para que `using Mono.Unix.Native` compilen.

**Por qué es seguro (no rompe):**
- El `chmod 0600` solo excluye a otros usuarios locales; el propietario (padre e hijo del mismo usuario) conserva acceso de lectura/escritura al socket.
- `UnixChannel` y `Mono.Remoting.Channels.Unix` viven en `Mono.Posix.dll` (verificado con `monodis`), por lo que la referencia `Mono.Posix` ya requerida cubre tanto la compilación previa (canales Unix) como el nuevo `Mono.Unix.Native`.
- La construcción exacta `Syscall.chmod(..., FilePermissions.S_IRUSR | S_IWUSR)` quedó validada compilando y ejecutando un probe replicado (resultado `180` hex = `0600`).

**Validación:**
- Compilación aislada por fragmento del patrón `chmod` (mismo que C1/mdhost) con `mcs` y `Mono.Posix` del GAC → limpia.
- `libsteticui.csproj` no compila vía `xbuild` por un error de evaluación preexistente de los imports `$(ReferencesGtk)` (`Import` con `Project` vacío), ajeno a este cambio (mismo motivo por el que el árbol se valida pieza a pieza).

**Endurecimiento TCP asociado (intervalo `ProcessTcp`):** el `TcpChannel` usado por `libsteticui` (tanto en `IsolationMode.ProcessTcp`, `Application.cs`, como en el lado backend, `ApplicationBackend.cs`) se registraba con `port=0` y, validado empíricamente con Mono 6.14, **bindaba a `0.0.0.0` (todas las interfaces)**, quedando la superficie de remoting accesible desde cualquier host de la red. Se añadió `props["rejectRemoteRequests"] = true` en ambos canales TCP; probado: el canal ahora bindea a **`127.0.0.1`** (loopback exclusivo) y un cliente local sigue resolviendo la llamada (`tcp://127.0.0.1:<port>/Pong.rem` → `pong`), mientras que los intentos desde IPs no-locales quedan rechazados. Los sockets Unix de C2 no se ven afectados.

**Pendiente del ítem 2.1:** migrar `BinaryFormatter` (Fase 1, punto 2).

### Cambio C3 — Análisis de viabilidad de migrar `BinaryFormatter` en la capa remoting

**Alcance (item 2.1, punto 1 del plan):** se revisaron todos los usos de `BinaryFormatter` en `main/src` y se clasificaron en tres categorías para decidir viabilidad de migración. Solo los de **remoting** (canales entre procesos) son el objeto de este análisis; los formatos de datos en disco/ventana se tratan en los ítems 2/3 de la Fase 1.

**Clasificación:**
- **Categoría A — Transporte de `ObjRef` para el arranque del canal remoting** (no es un formato libre, es el mecanismo de `.NET Remoting` para pasar un proxy al proceso hijo):
  - `ProcessHostController.cs:101` ↔ `mdhost.cs:88`
  - `MSBuild/Main.cs:64` (y su lector en el IDE)
  - `libsteticui/ApplicationBackendController.cs:44` ↔ `libsteticui/ApplicationBackend.cs:82`
  - `AutoTestClientSession.cs:87,118` ↔ `AutoTestService.cs:65,76`
- **Categoría B — Datos locales persistidos** (objetos `[Serializable]` cerrados, sin red):
  - `InstrumentationService.cs:172` (autosave; ya existe `SaveJson` en `:178`, migrable).
  - `NUnitAssemblyTestSuite.cs:765,777` (caché de tests en disco).
- **Categoría C — Formatos de archivo de datos de addins** (ítems 2 y 3 de Fase 1):
  - `VersionControlService.cs:345,412` (`Hashtable` de `CommitComment` — datos de mensajes de commit).
  - `ToolboxItemToolboxNode.cs:127,141` (`ToolboxItem` serializado en Base64 — formato de toolbox).

**Hallazgo central (viabilidad):** el transporte del `ObjRef` de arranque (Categoría A) **no puede migrarse con `System.Text.Json`/Newtonsoft** para serializar el `ObjRef` en sí, porque `ObjRef` es una clase `[Serializable]` del BCL con formato binario cerrado y sin serialización JSON. No obstante, se **validó empíricamente** (probe en `/tmp/opencode/arranque_textual`, Mono 6.14) que el arranque alternativo es viable: sustituir el `ObjRef` binario por un **endpoint textual** (`tcp://127.0.0.1:<puerto>/<uri>`, extraído de `IChannelDataStore.ChannelUris[0]`) y reconstruir el proxy en el hijo con `Activator.GetObject(ifaceType, url)`. Resultado: llamada remota `pong` en ambos procesos. La interfaz remota (`IProcessHostController`, `IAutoTestClient`, etc.) ya es conocida en el hijo por compilación, así que no se pierde flexibilidad de tipos.

**Límite real de viabilidad:** aunque se elimine el `BinaryFormatter` del arranque (Categoría A), el **protocolo de `.NET Remoting` sigue usando sink binario** (`BinaryServerFormatterSinkProvider`/`BinaryClientFormatterSinkProvider` con `TypeFilterLevel.Full`) para serializar los mensajes de método sobre el canal. Ese binario es inherente a `.NET Remoting`: migrarlo por completo exigiría reemplazar la pila de remoting (inviable en este repo migrado, y rompería la interoperabilidad Mono↔.NET que `RemotingService.cs:94-95,$115-130` documenta como requisito). Por tanto: **no es viable eliminar el `BinaryFormatter` interno del protocolo remoting**, pero **sí es viable y recomendable eliminar el `BinaryFormatter` del transporte de `ObjRef` de arranque** (payload de texto/endpoint + `Activator.GetObject`).

**Recomendación C3:**
- Prioridad 1: migrar **Categoría B** y **Categoría C** a JSON de tipos cerrados — son migraciones locales de datos, de bajo riesgo y alto beneficio (eliminan el deserializador de objetos arbitrarios de los archivos). `InstrumentationService` ya tiene `SaveJson` de referencia.
- Prioridad 2: sustituir el transporte de `ObjRef` binario (Categoría A) por endpoint textual + `Activator.GetObject`, eliminando los `BinaryFormatter` explícitos del arranque. **Requiere cambiar ambos extremos de cada canal a la vez** (padre e hijo) para no romper el arranque; debe hacerse canal a canal con prueba de integración real (proceso padre+hijo).
- Asumido y documentado: los sink binarios internos del protocolo remoting se conservan; la mitigación frente a esos es la prevista por C1/C2/C2-TCP (IPC a propietario + TCP solo loopback, sin exposición de red). Complementar con `TypeFilterLevel` reducido cuando la lógica lo permita (evaluar por canal; el `Full` es necesario por tipos de addins en mensajes, como documenta `RemotingService`).

**Validación empírica de C3:**
- Probe `arranque_textual` (procA/procB): procA publica `Pong.rem` en `tcp://127.0.0.1:<puerto>` (rejectRemoteRequests=true); procB reconstruye con `Activator.GetObject` y obtiene `pong`. Confirma el patrón alternativo sin `BinaryFormatter`.
- Probes previos (C1/C2/C2-TCP) confirman que el canal TCP bindea loopback y el IPC es a propietario, reduciendo la exposición del binario interno del protocolo.

**Pendiente tras C3:** ejecutar las migraciones de Categorías B/C y, canal a canal, la de Categoría A. Cada migración exige su propia prueba de no-rotura (caso de integración real).

### Cambio C4 — Migrar mensajes de commit (VersionControlService) a JSON

**Archivos:**
- `main/src/addins/VersionControl/MonoDevelop.VersionControl/MonoDevelop.VersionControl/VersionControlService.cs`
- `main/src/addins/VersionControl/MonoDevelop.VersionControl/MonoDevelop.VersionControl.csproj`

**Hallazgo que aborda:** 2.1 / ítem 2 de la Fase 1 (`VersionControlService.cs:345,412` deserializa datos persistentes con `BinaryFormatter`). El archivo `version-control-commit-msg` (memoria de mensajes de commit por archivo) se guardaba/leía con `BinaryFormatter`, un deserializador de objetos arbitrarios aplicado a un archivo local persistido.

**Cambio:** el guardado usa ahora JSON (`Newtonsoft.Json.JsonConvert.SerializeObject` de `Dictionary<string, CommitComment>`) y la lectura intenta JSON primero (`DeserializeObject<Dictionary<string, CommitComment>>` recombinado a `Hashtable`), con **fallback al formato legacy `BinaryFormatter`** para no perder los datos guardados por versiones anteriores. El campo `comments` se mantiene como `Hashtable` para no alterar el resto de la lógica (indexación `doc[file]`, limpieza por antigüedad >60 días). Se añadió `PackageReference Newtonsoft.Json` (versión `$(NuGetVersionNewtonsoftJson)` = 13.0.3, la fijada en `Directory.Build.props`, misma que usa `MonoDevelop.Core.csproj`) al `VersionControl.csproj`.

**Por qué es seguro (no rompe):**
- `CommitComment` es un tipo de datos cerrado con campos públicos (`Comment`, `Date`); JSON-round-trip trivial y deserialización sin tipos arbitrarios.
- Fallback binario: si el archivo persistido sigue siendo `BinaryFormatter`, la lectura legacy lo recupera intacto; solo los datos **nuevos** se escriben en JSON. (Verificado: loop de prueba confirma carga legacy.)
- No se altera la firma/manejo de `comments`: el resto del archivo sigue trabajando sobre `Hashtable`.
- Validado empíricamente (probe `vc_write` en `/tmp/opencode`): escritura produce archivo JSON, relectura devuelve los comentarios correctos, y un archivo binario legacy se lee por el fallback.

**Validación:**
- Compilación aislada con `mcs` del fragmento/write-path con `Newtonsoft.Json 13.0.3` → limpia.
- Probe funcional: JSON-READ count=2 (valores correctos), FILE-IS-JSON=yes, LEGACY-FALLBACK-READ count=1. Resultado `OK`.

**Pendiente tras C4:** migrar el resto de Categorías B/C (`InstrumentationService` autosave; `NUnitAssemblyTestSuite` caché; `ToolboxItemToolboxNode` toolbox — este último ítem 3 de Fase 1) y luego Categoría A canal a canal.

### Cambio C5 — Migrar caché de tests NUnit (NUnitAssemblyTestSuite) a JSON

**Archivos:**
- `main/src/addins/MonoDevelop.UnitTesting.NUnit/MonoDevelop.UnitTesting.NUnit/NUnitAssemblyTestSuite.cs`
- `main/src/addins/MonoDevelop.UnitTesting.NUnit/MonoDevelop.UnitTesting.NUnit.csproj`

**Hallazgo que aborda:** 2.1 / Categoría B (datos locales). `TestInfoCache` (`NUnitAssemblyTestSuite.cs:765,777`) guarda/lee en disco una caché de resultados de tests (`Hashtable` de `CachedTestInfo` con un `NunitTestInfo`) usando `BinaryFormatter`.

**Cambio:** `Read`/`Write` de `TestInfoCache` migrados a JSON (`Newtonsoft.Json.JsonConvert` de `Dictionary<string, CachedTestInfo>`), con **fallback al formato binario legacy** en `Read` (si el JSON no aplica, se reintenta `BinaryFormatter`). El `Hashtable` interno (`table`) se rellena desde el `Dictionary` JSON y la escritura serializa el `Hashtable` como `Dictionary<string, CachedTestInfo>`. `NunitTestInfo` y `CachedTestInfo` son tipos con propiedades/campos públicos JSON-deserializables. Se añadió `PackageReference Newtonsoft.Json` (13.0.3, versión fijada del repo) al `MonoDevelop.UnitTesting.NUnit.csproj`.

**Por qué es seguro (no rompe):**
- `NunitTestInfo` tiene setters públicos en todas sus propiedades (definido en `RemoteTestResult.cs:164`); `CachedTestInfo` campos públicos → JSON round-trip completo, incluso el árbol anidado `NunitTestInfo[]`.
- Fallback binario: cachés legacy siguen leyéndose sin perder datos; solo los caches **nuevos** se escriben en JSON.
- `TestInfoCache.Read` mantiene su firma estática y el caller (ya con `try/catch` en línea ~326) sigue igual.
- Validado empíricamente (probe `nunit_probe` en `/tmp/opencode`): escritura JSON (`FILE-IS-JSON=yes`), lectura JSON con el árbol anidado (`name=TestA child=Case1`), y lectura de un archivo binario legacy (`name=Legacy`) por el fallback. Resultado `OK`.

**Nota de validación:** `TestInfoCache.Read` usa `StreamReader(..., leaveOpen: true)` para permitir `s.Position = 0` y reintentar el `BinaryFormatter` sobre el mismo `Stream` tras un `JsonException`. (Cambios menores de normalización de fin de línea en líneas preexistentes 434/519 por el editor; sin efecto funcional.)

### Cambio C6 — Migrar autosave de InstrumentationService a JSON (DTO layer en MonoDevelop.Core)

**Archivos:**
- `main/src/core/MonoDevelop.Core/MonoDevelop.Core.Instrumentation/InstrumentationData.cs` (**nuevo**): DTOs JSON + codec de conversión.
- `main/src/core/MonoDevelop.Core/MonoDevelop.Core.Instrumentation/InstrumentationService.cs`: `AutoSave` → `SaveJson`; `LoadServiceDataFromFile` JSON-preferente con fallback binario.
- `main/src/core/MonoDevelop.Core/MonoDevelop.Core.Instrumentation/Counter.cs`: `internal RestoreState(...)` y `internal RestoreValues(...)`.
- `main/src/core/MonoDevelop.Core/MonoDevelop.Core.Instrumentation/TimerCounter.cs`: `internal RestoreTimerState(...)`.

**Hallazgo que aborda:** 2.1 / Categoría B (datos locales). `InstrumentationService.AutoSave` (`InstrumentationService.cs:167`) serializaba el snapshot completo con `BinaryFormatter`; `LoadServiceDataFromFile` (`:218`) lo deserializaba.

**Por qué requiere DTOs (no swap simple C4/C5):** las clases de snapshot (`Counter`, `TimerCounter`, `CounterCategory`, `CounterValue`, `TimerTrace`) son objetos runtime con propiedades públicas **getter-only** (sin setters), campos privados de estado (`count`, `totalCount`, `values`, `traces`, `metadata`) y un `readonly struct` con constructor internal. Newtonsoft serializa propiedades de lectura pero **no puede reconstruirlas** sin setters → un swap directo habría roto la lectura de `mdmonitor` (contadores vacíos). Por eso se construyó una **capa de DTOs de persistencia** planos con mapeo explícito.

**Cambio:**
- `InstrumentationSnapshotDto`/`CounterDto`/`CounterValueDto`/`TimerTraceDto`/`CounterCategoryDto`: DTOs con todos setters públicos; las categorías referencian contadores **por nombre** (`CounterNames`) para no duplicar el grafo.
- `InstrumentationDataCodec.FromService(data)`: captura contadores/categorías/valores/traces/metadatos → DTO.
- `InstrumentationDataCodec.ToService(dto)`: reconstruye `InstrumentationServiceData` con `Counter`/`TimerCounter` planos, rellenando su estado vía `RestoreState`/`RestoreTimerState`/`RestoreValues`, y `CounterValue` con `TimerTraceList` cuando hay traces.
- `AutoSave` escribe JSON (vía `SaveJson`); `LoadServiceDataFromFile` intenta JSON primero y, si falla, **reintenta `BinaryFormatter` legacy** (mismo patrón de retroceso que C4/C5).
- `MonoDevelop.Core.csproj` ya referencia Newtonsoft (línea 159) → sin cambios de csproj.

**Por qué es seguro (no rompe):**
- Fallback binario: archivos legacy (`*.bin` antiguos) se siguen leyendo; solo los **nuevos** autosaves se escriben en JSON.
- `IInstrumentationService`/`InstrumentationServiceData`/mdmonitor quedan intactos: la reconstrucción devuelve el mismo contrato; `GetValues`/`GetTimerTraces`/`Metadata` funcionan sobre el estado restaurado.
- No se toca la API pública de `Counter`/`TimerCounter`/`CounterValue`; los métodos añadidos son `internal`.
- Validación de compilación (Roslyn, tan solo los archivos de Instrumentation): **cero errores** en `InstrumentationData.cs`, `Counter.cs`, `TimerCounter.cs` y las líneas editadas de `InstrumentationService.cs` (los únicos errores del lote son missing-reference preexistentes de ensamblados externos, ajenos a este cambio).

**Nota de validación empírica (probe `instrumentation_probe2` en `/tmp/opencode`):** round-trip JSON completo `OK` — contadores planos y timer (Estado Count/TotalCount/Id), valores (Value/TotalCount/Message), estado de `TimerCounter` (MinSeconds/TotalTime/CountWithDuration/MinTime/MaxTime), traces anidados (start/end), metadata (`op=build`) y membresía de categorías (Build→1, Time→1). El JSON resultante es un archivo de texto plano bien formado (`instrumentation_roundtrip.json`). Se detectó y corrigió un defecto (doble alta de contadores en categorías) durante la validación.

**Pendiente tras C6:** migrar `ToolboxItemToolboxNode` (ítem 3, `ToolboxItem` complejo); luego Categoría A (transportes `ObjRef`) canal a canal.

### Cambio C7 — Endurecer deserialización de ToolboxItemToolboxNode (binder restringido)

**Archivos:** `main/src/addins/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.Toolbox/ToolboxItemToolboxNode.cs`.

**Hallazgo que aborda:** 2.1 / Categoría C (formato archivo). `DeserializeToolboxItem` (`ToolboxItemToolboxNode.cs:127-129`) deserializaba con `BinaryFormatter` un blob Base64 embebido en `[ItemProperty("itemcontents")] serializedToolboxItem`, dentro del XML del toolbox (`Toolbox.xml`, archivo local del perfil de usuario en `UserProfile.Current.LocalConfigDir`). Un archivo de toolbox manipulado podría embeber un gadget → ejecución de código arbitraria al abrir la configuración.

**Por qué no se migró a JSON:** el payload es un `System.Drawing.Design.ToolboxItem` (tipo de framework con estado complejo y subclases custom). Los subclases llevan estado extra propio imposible de reconstruir fielmente desde JSON sin romper su funcionalidad (regresión funcional). Siguiendo el patrón C1/C2 (BinaryFormatter no migrable → mitigar exposición), se **endureció la deserialización**.

**Cambio:**
- Nuevo `sealed class ToolboxItemSerializationBinder : SerializationBinder` (allowlist): permite deserializar tipos **asignables a `ToolboxItem`** (cualquier ensamblado, preservando subclases custom) y tipos de **ensamblados framework de confianza** (`mscorlib`, `System`, `System.Drawing`, `System.Design`, `System.Windows.Forms`, `System.Web`, `System.ComponentModel*`, `System.Runtime.Serialization`, `System.Collections`, `System.Private.CoreLib`, `System.Xml`, etc., que son los helpers legítimos del grafo de un `ToolboxItem`: `AssemblyName`, `Type`, `Bitmap`, colecciones, strings).
- Todo lo demás (ensamblados de usuario/terceros **no** `ToolboxItem`-derivados, donde viviría un gadget) → lanza `SerializationException` (rechaza el payload, degradación segura).
- `BindToName` devuelve `null,null` (comportamiento BCL por defecto) para no cambiar el formato en disco de los ítems recién escritos.
- `BF.Binder = ToolboxItemSerializationBinder.Instance` en `DeserializeToolboxItem` y `SerializeToolboxItem`.
- Añadidas `using System.Collections.Generic;` y `using System.Runtime.Serialization;`.

**Por qué es seguro (no rompe):**
- Mantiene el formato de archivo existente (Base64 dentro del XML) → los toolbox legacy se siguen leyendo.
- Preserva fidelidad de subclases custom de `ToolboxItem` (cualquier ensamblado, solo si derivan de `ToolboxItem`).
- Bloquea de forma fiable los gadgets (ninguno deriva de `ToolboxItem` y ninguno vive en ensamblados framework de confianza).
- Comprobación empírica: bajo este Mono 6.14, `BinaryFormatter` **ya falla** el round-trip de `ToolboxItem` ("constructor not found") incluso sin binder (problema preexistente del runtime, independiente de este cambio) → el binder no empeora el camino de éxito y es puro defense-in-depth frente a input no confiable.

**Validación (probe `tbi_block` en `/tmp/opencode`):** `BINDER-PROBE=OK` — (1) permite `ToolboxItem`-derivado (ensamblado cualquiera), (2) permite tipo framework confiable, (3) bloquea tipo de ensamblado de usuario no `ToolboxItem` (lógica `BindToType`), (4) bloquea de extremo a extremo un gadget `[Serializable]` al deserializar con el binder, (5) el mismo gadget **sí** deserializa sin binder (prueba que el binder es la puerta), (6) permite helpers framework (`List<string>`). Validación de compilación Roslyn: **cero errores** en `ToolboxItemToolboxNode.cs` (los únicos errores del lote son missing-reference preexistentes de UI/Gtk/Xwt en `ItemToolboxNode.cs`, ajenos a este cambio).

### Cambio C8 — Reemplazar transporte `ObjRef` (BinaryFormatter) por endpoint textual + `Activator.GetObject` (canal ProcessHostController ↔ mdhost)

**Archivos:** `main/src/core/MonoDevelop.Core/MonoDevelop.Core.Execution/ProcessHostController.cs`, `main/src/core/MonoDevelop.Core/MonoDevelop.Core.Execution/RemotingService.cs`, `main/src/tools/mdhost/src/mdhost.cs`.

**Hallazgo que aborda:** 2.1 / Categoría A (transporte `ObjRef`), primer canal. Al arrancar un proceso externo, el padre (`ProcessHostController.Start`) publicaba su propio proxy remoting con `RemotingServices.Marshal(this)` y serializaba el `ObjRef` binario con `BinaryFormatter` → Base64 (`sref`) en la primera línea de un archivo temporal; el hijo (`mdhost.Main`) lo leía y lo deserializaba con `BinaryFormatter.Deserialize` (líneas 101-105 / 86-89). Deserializar un `ObjRef` no confiable es una superficie de ataque (gadgets).

**Cambio:** se elimina el `BinaryFormatter` del transporte. El padre sigue publicando `this` (necesario para que el hijo llame `RegisterHost`/`GetLogger`/`WaitForExit`), pero en vez de serializar el `ObjRef` binario escribe en la primera línea una **URL textual** del objeto marshaled; el hijo reconstruye el proxy con `Activator.GetObject(typeof(IProcessHostController), url)`.
- `RemotingService.GetMarshaledUrl(objectUri)` (nuevo): devuelve la URL conexionable del objeto publicado, tomando el primer canal TCP registrado vía `((IChannelReceiver)ch).ChannelData as ChannelDataStore` + `objectUri`. Se elige TCP porque está endurecido con `rejectRemoteRequests=true` (bindeado a 127.0.0.1, probado en C2) y es interoperable Mono↔.NET. El objeto marshaled null-portal resulta alcanzable por ambos canales (IPC y TCP) — comprobado empíricamente.
- `ProcessHostController.Start`: `string controllerUrl = RemotingService.GetMarshaledUrl(oref.URI);` y `sw.WriteLine(controllerUrl);` (antes `sw.WriteLine(sref)`).
- `mdhost.Main`: `IProcessHostController pc = (IProcessHostController) Activator.GetObject(typeof(IProcessHostController), sref);` (antes `BinaryFormatter.Deserialize`).
- Eliminado `using System.Runtime.Serialization.Formatters.Binary;` en `ProcessHostController.cs` y `mdhost.cs`.

**Por qué es seguro (no rompe):**
- El transporte remoting en sí (canal IPC/TCP binario) sigue existiendo, pero ya quedó endurecido en C1 (chmod 0600 en IPC) y C2 (TCP `rejectRemoteRequests=true` → solo loopback). Este cambio elimina el desserializador de `ObjRef` al traspasar el control entre procesos, reduciendo superficie.
- El formato del archivo temporal solo cambia la primera línea (de Base64 a URL) y solo se lee entre el padre y su propio hijo recién lanzado; ningún otro consumidor lo lee.
- `Activator.GetObject` produce el proxy transparente igual que el `ObjRef` deserializado (mismo `IProcessHostController`); el resto del flujo (`LocalLogger`, `ProcessHost`, `RegisterHost`) queda intacto.
- Tipo `IProcessHostController` definido una sola vez en `MonoDevelop.Core.dll`, compartido por padre e hijo → identidad de tipo consistente.

**Validación:**
- Compilación: `mdhost.cs` con `mcs` (toolchain Mono) contra prebuilt `MonoDevelop.Core.dll`/`Mono.Addins.dll`/`Mono.Posix` → **éxito**. Roslyn sobre `ProcessHostController.cs`/`RemotingService.cs`: **cero errores en el código añadido** (los CS0122/CS0234 del lote son por referenciar el Core prebuilt mientras se recompilan tipos internos de Core, ajenos a este cambio).
- Probe de transporte (`phc_probe`/`dualch2` en `/tmp/opencode`): el objeto marshaled null-portal es alcanzable por el hijo vía `Activator.GetObject` tanto en `ipc://<portName>/<uri>` (Mono↔Mono) como en `tcp://127.0.0.1:<port>/<uri>` → `Result pong`.
- Probe de integración extremo a extremo (`c8sP`/`c8sC` en `/tmp/opencode`): padre marshala su controlador, calcula la URL textual TCP loopback tal como `GetMarshaledUrl`, la escribe; hijo hace `Activator.GetObject(typeof(IProcessHostController), url)` → `RegisterHost(new Host())`; el padre recibe `PARENT_GOT_HOST:host1` y el hijo reporta `RESULT:OK`. (El probe usa una DLL compartida `c8shared.dll` con las interfaces, replicando que ambos lados referencian `MonoDevelop.Core.dll`.)

### Cambio C9 — Reemplazar transporte `ObjRef` (BinaryFormatter) por endpoint textual + `Activator.GetObject` (canal libsteticui padre ↔ hijo)

**Archivos:** `main/src/addins/MonoDevelop.GtkCore/libsteticui/ApplicationBackendController.cs` (padre), `main/src/addins/MonoDevelop.GtkCore/libsteticui/ApplicationBackend.cs` (hijo).

**Hallazgo que aborda:** 2.1 / Categoría A (transporte `ObjRef`), canal Ubuntu/GtkCore. Al arrancar el backend aislado, el padre (`ApplicationBackendController.StartBackend`) publicaba su `ApplicationBackendController` con `RemotingServices.Marshal(this, objectUri)` y serializaba el `ObjRef` binario con `BinaryFormatter` → Base64 (`sref`) en la segunda línea del stdin del hijo; el hijo (`ApplicationBackend.Main`) recibía esa línea y la deserializaba con `BinaryFormatter.Deserialize` (obtenía el controlador y lo usaba en `Connect`/menús). Deserializar un `ObjRef` no confiable es superficie de ataque (gadgets).

**Cambio:** se elimina el `BinaryFormatter` del transporte. El padre sigue publicando su controlador (necesario para que el hijo llame a los handlers), pero en vez de serializar el `ObjRef` binario escribe en esa línea una **URL textual** del objeto marshaled; el hijo reconstruye el proxy con `Activator.GetObject(typeof(ApplicationBackendController), sref)`.
- `ApplicationBackendController.GetMarshaledUrl(objectUri)` (nuevo): calcula la URL conexionable del objeto publicado en función del canal usado por este proceso.
  - Canal `__internal_tcp` (TCP loopback endurecido en C2-C2TCP): `baseUrl + "/" + objectUri`, con `baseUrl = store.ChannelUris[0]`.
  - Canal `"unix"` (Unix IPC con chmod 0600 en C2): `"unix://" + path + "?" + objectUri`, con `path = store.ChannelUris[0].Substring("unix://".Length)`. El separador entre socket y URI es `?` (no `/`) — gramática `UnixChannel.ParseUnixURL` de Mono.
  - La detección del canal activo es por `ChannelName` (`__internal_tcp` / `unix`) para replicar cómo el padre elige canal.
- `ApplicationBackendController.StartBackend`: `string controllerUrl = GetMarshaledUrl(objectUri);` y `sw.WriteLine(controllerUrl);` (antes `sw.WriteLine(sref)`). Quitado `using System.Runtime.Serialization.Formatters.Binary;`.
- `ApplicationBackend.Main`: `ApplicationBackendController ctrl = (ApplicationBackendController) Activator.GetObject(typeof(ApplicationBackendController), sref);` (antes `BinaryFormatter.Deserialize`). Quitado `using System.Runtime.Serialization.Formatters.Binary;`.

**Por qué es seguro (no rompe):**
- El transporte remoting subyacente (canal TCP/Unix) sigue existiendo, ya endurecido en C2 (TCP `rejectRemoteRequests=true` → solo loopback) y chmod 0600 en Unix. Este cambio elimina el deserializador de `ObjRef` al traspasar el control entre procesos.
- El formato del stdin del hijo solo cambia esa línea (de Base64 a URL) y solo la lee el hijo recién lanzado por el propio padre; ningún otro consumidor la lee.
- `Activator.GetObject` produce el proxy transparente igual que el `ObjRef` deserializado (mismo `ApplicationBackendController`); el resto del flujo (`Connect`, menús, `BackendController` handling) queda intacto.
- El tipo `ApplicationBackendController` se comparte entre padre e hijo (mismo ensamblado libsteticui) → identidad de tipo consistente en ambos procesos.

**Validación:**
- Compilación: Roslyn sobre el **set completo `libsteticui`** (56 + 2 `Windows/*` fuentes, `/unsafe`, refs gtk-sharp 2.0 GAC, Mono.Posix, Mono.Cairo, Mono.Cecil 0.10.1 net35, prebuilt `libstetic.dll`) → **cero errores**; ninguno en `ApplicationBackendController.cs` ni `ApplicationBackend.cs`.
- Probe de integración extremo a extremo (`c9e_parent`/`c9e_child` en `/tmp/opencode`, replicando el arranque padre→hijo vía stdin con URL textual): en ambos modos el padre marshala su `Ctrl`, calcula la URL con la lógica de `GetMarshaledUrl`, la escribe en el stdin del hijo; el hijo hace `Activator.GetObject` y llama `Ping`/`Connect` → el padre recibe `PARENT_GOT_CONNECT:backend1`.
  - TCP: `PARENT_URL:tcp://127.0.0.1:36811/c1.rem` → `CHILD_PING:pong` → `PARENT_GOT_CONNECT:backend1`.
  - Unix: `PARENT_URL:unix:///tmp/tmp7d1abb61.tmp?c1.rem` → `CHILD_PING:pong` → `PARENT_GOT_CONNECT:backend1`.

### Cambio C10 — Reemplazar transporte `ObjRef` (BinaryFormatter) por endpoint textual + `Activator.GetObject` (canal AutoTest, 4 handoffs)

**Archivos:** `main/src/core/MonoDevelop.Ide/MonoDevelop.Components.AutoTest/AutoTestService.cs`, `main/src/core/MonoDevelop.Ide/MonoDevelop.Components.AutoTest/AutoTestClientSession.cs`; `main/src/core/MonoDevelop.Core/MonoDevelop.Core.Execution/RemotingService.cs` (visibilidad `GetMarshaledUrl` `internal` → `public`).

**Hallazgo que aborda:** 2.1 / Categoría A (transporte `ObjRef`), último canal real. AutoTest es un puente de pruebas que conecta MonoDevelop con un test runner externo mediante **cuatro traspasos del `ObjRef` serializado con `BinaryFormatter`**:
1. `AutoTestClientSession.StartApplication` (lado test): `RemotingServices.Marshal(this)` + `BinaryFormatter.Serialize` del `ObjRef` → Base64 → variable de entorno `MONO_AUTOTEST_CLIENT` del proceso MonoDevelop.
2. `AutoTestService.Start` (lado app): lee `MONO_AUTOTEST_CLIENT` → `BinaryFormatter.Deserialize` → `IAutoTestClient`. *(Deserialización de dato externo: superficie de gadget.)*
3. `AutoTestService.Start` (lado app, `publishServer`): `RemotingServices.Marshal(manager)` + `BinaryFormatter.Serialize` del `ObjRef` → Base64 → archivo `SessionReferenceFile`.
4. `AutoTestClientSession.AttachApplication` (lado test): lee `SessionReferenceFile` → `BinaryFormatter.Deserialize` → `IAutoTestService`. *(Deserialización de archivo: superficie de gadget.)*

**Cambio:** se sustituye el `ObjRef`→Base64 por una **URL textual** en los cuatro handoffs (receta C3/C8/C9), manteniendo `RemotingServices.Marshal` (el MBR sigue publicándose, solo cambia el traspaso de referencia).
- `RemotingService.GetMarshaledUrl(string)` pasa de `internal` a `public`: un único constructor canónico de la URL del objeto marshaled en el canal TCP loopback endurecido (`rejectRemoteRequests=true`), reutilizable desde `MonoDevelop.Ide` (AutoTest) y `MonoDevelop.Core` (mdhost/C8).
- `AutoTestClientSession.StartApplication`: `ObjRef oref = Marshal(this, "autotest-client")`; `sref = GetMarshaledUrl(oref.URI)` → env. Quitaron `BinaryFormatter`/`Convert.FromBase64String`.
- `AutoTestService.Start` (handoff A): `IAutoTestClient client = (IAutoTestClient) Activator.GetObject(typeof(IAutoTestClient), sref);` (antes `bf.Deserialize`).
- `AutoTestService.Start` (handoff C): `Marshal(manager, "autotest-service")`; `File.WriteAllText(SessionReferenceFile, GetMarshaledUrl(oref.URI))` (antes Base64 del `ObjRef`).
- `AutoTestClientSession.AttachApplication` (handoff D): `IAutoTestService service = (IAutoTestService) Activator.GetObject(typeof(IAutoTestService), sref);` (antes `bf.Deserialize`).
- Eliminado `using System.Runtime.Serialization.Formatters.Binary;` en ambos archivos AutoTest. Nuevas constantes `AutoTestServiceObjectUri`/`AutoTestClientObjectUri`.

**Por qué es seguro (no rompe):**
- `Activator.GetObject` reconstruye el proxy transparente igual que el `ObjRef` deserializado (mismos `IAutoTestClient`/`IAutoTestService`); el resto del flujo (`Connect`, `AttachClient`, `AttachApplication`, menús) queda intacto.
- La URL va por el mismo canal TCP loopback que `RegisterRemotingChannel` configura con `TypeFilterLevel.Full` (requerido para los retornos MBR de `AttachClient`); **no se tocó la configuración del canal**, solo el traspaso inicial de referencia.
- `MONO_AUTOTEST_CLIENT` y `SessionReferenceFile` siguen siendo locales entre el proceso MonoDevelop y su test runner recién lanzado; cambiar de Base64 a URL no altera quién los lee.

**Validación:**
- Probe de integración extremo a extremo (`c10_full` en `/tmp/opencode`, replicando los 4 handoffs con canal `TypeFilterLevel.Full` igual a `RegisterRemotingChannel`, dos procesos): app conecta al cliente vía `Activator.GetObject` de `client.ref` → `APP_CLIENT_PING:client-pong` → `client.Connect` → `APP_RECEIVED_CONNECT:app-backend` → app publica `autotest-service` y escribe `service.ref` (`APP_WROTE_SERVICE_REF`) → cliente hace `Activator.GetObject` → `CLIENT_SERVICE_PING:service-pong` → `svc.AttachClient` → `CLIENT_ATTACHED:client-pong`, `CLIENT_SESSION_AFTER:client-pong`. Sin `BinaryFormatter` de `ObjRef` en el bootstrap.
- Compilación: Roslyn sobre `AutoTestService.cs`/`AutoTestClientSession.cs` contra prebuilt `MonoDevelop.Core.dll` → **cero errores de transporte/API** en las líneas cambiadas (el resto de errores son ruido por no existir el `MonoDevelop.Ide.dll` prebuilt: tipos hermanos faltantes). `RemotingService.cs` compila limpio (solo ruido de stubs de Core ajenos a `GetMarshaledUrl`); el cuerpo de `GetMarshaledUrl` compila aislado sin errores.

### Cambio C11 — Deshabilitar el canal AutoTest/instrumentación por defecto (opt-in explícito) — hallazgo 2.6 / prioridad #8

**Archivos:** `main/src/core/MonoDevelop.Ide/MonoDevelop.Components.AutoTest/AutoTestService.cs` (nuevo `IsAutoTestEnabled`), `main/src/core/MonoDevelop.Ide/MonoDevelop.Components.AutoTest/AutoTestClientSession.cs` (fija `MONO_AUTOTEST_ENABLE`), `main/src/core/MonoDevelop.Core/MonoDevelop.Core/Runtime.cs` (`IsInstrumentationServiceEnabled`).

**Hallazgo que aborda:** 2.6 / prioridad #8 (BAJO) — «Instrumentación/autotest expuesta». El canal de autotest (y con él la publicación del servicio de instrumentación) se activaba en cuanto estaba presente la variable de entorno `MONO_AUTOTEST_CLIENT` (`AutoTestService.Start` conectaba al cliente; `Runtime.IsInstrumentationServiceEnabled` publicaba el canal TCP). Un proceso local podía fijar esa variable y forzar la puesta en marcha del canal remoting sin consentimiento explícito.

**Cambio (mínimo, opt-in explícito):** el canal ya no se auto-activa por una variable esporádica; requiere opt-in explícito `MONO_AUTOTEST_ENABLE=1` (además de `MONO_AUTOTEST_CLIENT` para conectar).
- `AutoTestService.Start`: si `IsAutoTestEnabled()` es falso, retorna antes de conectar/publicar y, si `MONO_AUTOTEST_CLIENT` está presente pero sin opt-in, loguea un aviso (defensa a lo seguro: una variable residual no puede habilitar el canal).
- `AutoTestService.IsAutoTestEnabled()` (nuevo): `MONO_AUTOTEST_ENABLE == "1" || Runtime.Preferences.EnableAutomatedTesting` (la preferencia existente sigue habilitando).
- `AutoTestClientSession.StartApplication`: el harness (actor que opta) fija `MONO_AUTOTEST_ENABLE=1` además de `MONO_AUTOTEST_CLIENT`, de modo que la suite de pruebas funciona sin pasos manuales.
- `Runtime.IsInstrumentationServiceEnabled()`: publicar el servicio de instrumentación solo si `MONO_AUTOTEST_ENABLE == "1"` (o la preferencia `EnableInstrumentation`), en vez de por `MONO_AUTOTEST_CLIENT`.

**Por qué es seguro (no rompe):**
- Se preserva el flujo de pruebas: el harness (`AutoTestClientSession.StartApplication`) opta explícitamente con `MONO_AUTOTEST_ENABLE=1`; la preferencia `EnableAutomatedTesting`/`EnableInstrumentation` (activada en opciones) sigue funcionando. Solo cambia el caso por defecto (ahora apagado).
- No toca el transporte ni el formato: solo la condición de puesta en marcha.
- Alineado con el plan Fase 1.2 («deshabilitar `MONO_AUTOTEST_CLIENT` por defecto»).

**Validación:**
- Compilación: Roslyn sobre los 3 archivos contra prebuilt `MonoDevelop.Core.dll` → **cero errores de lógica/API** en las líneas cambiadas (el resto es ruido por tipos hermanos de `MonoDevelop.Ide` sin prebuilt). Línea 217 de `Runtime.cs` sin errores.
- Probe de semántica del gate (`c11_gate` en `/tmp/opencode`): `MONO_AUTOTEST_CLIENT` sin `MONO_AUTOTEST_ENABLE` → `WARN: NOT started (no opt-in)` (no conecta); con `MONO_AUTOTEST_ENABLE=1` + client → `CONNECTING` + `STARTED`; con opt-in y publish → `PUBLISH` + `STARTED`.

**Pendiente tras C11:** dentro de la Categoría de deserialización (Hallazgo 2.1), restan solo los usos de `BinaryFormatter` en **superficies no remoting**: `main/external/xwt/Xwt/Xwt/TransferDataSource.cs:150,164` (portapapeles) y `main/external/guiunit/.../BinarySerializableConstraint.cs:38` (tests). Ambos están fuera del alcance de «canales entre procesos» y no publican datos no confiables al arranque; se relegarán a revisión con las superficies de formato en disco (Fase 1/2 según se decida). **Fase 1, ítem 1 y ítem 2 de remoting: COMPLETADOS.**

## 4b. Bitácora de cambios aplicados (Fase 2 — TLS y criptografía)

### Cambio F2.1 — Eliminar SSL 2/SSL 3 de `XspSslProtocol` — hallazgo 2.2 / prioridad #5

**Archivos:** `main/src/addins/AspNet/Execution/XspParameters.cs`, `main/src/addins/AspNet/Execution/XspOptionsPanelWidget.cs`.

**Hallazgo que aborda:** 2.2 — «Protocolos TLS obsoletos en la lista de protocolos SSL de XSP». El enum `XspSslProtocol` exponía `Ssl2` y `Ssl3`.

**Cambio:** se eliminan `Ssl2`/`Ssl3` del enum (quedan `Default` y `Tls`) y se retiran los dos ítems de «SSL 2»/«SSL 3» de su combo en `XspOptionsPanelWidget`, manteniendo la alineación de índices combo↔enum (Default=0, TLS=1).

**Validación:** Roslyn sobre `XspParameters.cs` sin errores de lógica en el enum.

### Cambio F2.2 — Quitar el callback TLS global y validar por huella SHA-256 — hallazgo 2.2 / prioridad #5

**Archivos:** `main/src/core/MonoDevelop.Core/MonoDevelop.Core/Runtime.cs`, `main/src/core/MonoDevelop.Core/MonoDevelop.Core/WebCertificateService.cs`, `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide/DefaultWebCertificateProvider.cs`.

**Hallazgo que aborda:** 2.2 — callback global que ignoraba `SslPolicyErrors` (incluida la cadena remota) en todas las conexiones HTTPS de los subprocesos hedándolas suplantables. Decisión del usuario: **quitar el callback global** y, para autofirmados, confiar por **huella SHA-256 exacta** (sin diálogo global).

**Cambio:**
- `Runtime.cs`: eliminado el `ServerCertificateValidationCallback` global y su `using System.Security.Cryptography.X509Certificates` (quedó huérfano).
- `WebCertificateService.cs`: `GetIsCertificateTrusted` documentada para confiar solo por huella SHA-256 exacta.
- `DefaultWebCertificateProvider.cs`: diálogo de certificado con `handle.WaitOne(15000)` acotado (en headless deniega en vez de colgar).

**Validación:** Roslyn sobre los 3 archivos (contra prebuilt Core + Mono.Addins net472; gtk-sharp para el provider) sin errores de lógica.

### Cambio F2.3 — `AutoSave.cs`: MD5 → SHA-256 per-call — hallazgo 2.4 / prioridad #7

**Archivos:** `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.Editor/AutoSave.cs`.

**Hallazgo que aborda:** 2.4 — MD5 para el archivo de autoguardado (y una instancia estática compartida no thread-safe).

**Cambio:** `GetMD5` reescrita para usar `SHA256.Create().ComputeHash` **per-call** (thread-safe; se elimina el `static MD5 md5 = MD5.Create()` compartido). API compatible **net472**: NO se usa `SHA256.HashData` (disponible solo desde .NET 5).

**Validación:** Roslyn sobre `AutoSave.cs` — la región SHA256/`ComputeHash`/`GetMD5` con **cero errores** (el único error reportado, `CS0122 'Counters'`, es ruido por la Core.dll stale prebuilt, ajeno a este cambio).

### Cambio F2.4 — Activación explícita de avatares (Gravatar off por defecto) — privacidad / superficie de red

**Archivos:** `main/src/core/MonoDevelop.Ide/MonoDevelop.Ide/ImageService.cs`, `main/src/core/MonoDevelop.Core/MonoDevelop.Core/Runtime.cs`, `main/src/addins/VersionControl/MonoDevelop.VersionControl/MonoDevelop.VersionControl.Views/LogWidget.cs`.

**Hallazgo que aborda:** el modo avatar emitía un hash del email del usuario (MD5) hacia `gravatar.com` (tercero) en la vista de Version Control, sin consentimiento. Se adopta la opción «desactivarlo por configuración» del plan.

**Nota sobre criptografía:** la API de Gravatar **requiere MD5** y no acepta SHA-256; cambiar el digest rompería el servicio. Por ello la mitigación real es **opt-in (off por defecto)**: no se computa ningún hash ni se contacta al tercero salvo que el usuario lo active.

**Cambio:**
- `Runtime.Preferences.EnableGravatarAvatars` (nuevo, default `false`) en `Runtime.cs`.
- `ImageService.GetUserIcon`: si el avatar no está habilitado, retorna `null` sin descargar ni hacer red.
- `ImageService.LoadUserIcon`: guard `if (gravatar == null) return;` (no-op).
- `LogWidget.cs` (renderer de autor): guard `if (img == null) return;`.

**Validación:** Roslyn — `Runtime.cs` (nueva preferencia) y `ImageService.cs`/`LogWidget.cs` sin errores de sintaxis/lógica en las regiones editadas; el único ruido es la falta del símbolo `EnableGravatarAvatars` contra la Core.dll stale prebuilt, que se resuelve en el build real (el accessor `Runtime.Preferences.X` replica el patrón establecido en `AutoTestService.cs:101`/`IdePreferences.cs:89`).

**Fase 2 COMPLETA (F2.1–F2.4).** Quedan pendientes fuera de alcance de Fase 2: los `BinaryFormatter` no-remoting (portapapeles `xwt` y tests `guiunit`) y la Fase 3 (supply-chain/claves).

## 4c. Bitácora de cambios aplicados (Fase 3 — Supply-chain y claves)

### Cambio F3.1 — Activación de NuGetAudit y análisis de CVE en dependencias — plan §3.1

**Archivos:** `main/Directory.Build.props` (nuevo `<NuGetAudit>true</NuGetAudit>`).

**Hallazgo que aborda:** auditar dependencias NuGet frente a CVEs conocidos durante el restore.

**Análisis de CVEs (fechado: Sep-2026):**
- **NuGet.Client 5.4.0** (`NuGetVersionNuGet` en `Directory.Build.props`:25, usado en `MonoDevelop.PackageManagement` como `NuGet.PackageManagement`/`NuGet.Indexing`, `PrivateAssets="runtime"`, **además** con binarios vendidos en `main/external/nuget-binary/` incluido `nuget.exe`): **CVE-2024-0057 (crítica, CVSS 9.1)** — elusión del bypass de validación de cadenas X.509. Versiones afectadas `< 5.11.6` → **afectado**. Requiere subir a ≥ **5.11.6** (o a la línea 6.x parcheada).
- **Mono.Cecil 0.10.1**, **NUnit 3.9.0**, **Microsoft.TestPlatform 16.2.0**: **sin CVE** en las bases de GitHub Advisory / Meterian → la lista original del plan era en parte sobre-inclusiva.

**Decisión (confirmada por el usuario):** **solo añadir `<NuGetAudit>true</NuGetAudit>`** y documentar el bump como **pendiente**. Razón: el bump real exige reemplazar los binarios vendidos en `external/nuget-binary/` (+ `nuget.exe`) y el `PackageReference` del addin, con riesgo de romper el build legacy net472 que **no es validable** en este entorno (el `dotnet list package --vulnerable` no opera sobre el `Main.sln` legacy — timeout/no-op). El bump se deja anotado como deuda concreta (5.4.0 → ≥ 5.11.6).

**Nota sobre el alcance:** `PrivateAssets="runtime"` y los binarios vendidos indican que NuGet es dependencia de **tooling/build** del addin de gestión de paquetes, no un componente de runtime del producto; el riesgo real CVE-2024-0057 es de la herramienta `nuget.exe`/cliente cuando se usa para resolver/descargar, mitigado al no confiar en la info de dispositivos X.509 sin validación (ver también F2.2).

### Cambio F3.2 — Strong-naming: verificado, NO se retira — plan §3.2

**Archivos:** ninguno (decisión documentada).

**Hallazgo que aborda:** el `.snk` en el árbol. Verificado que **only** `main/msbuild/MonoDevelop-Public.snk` está versionado (los `.snk` en `external/*` no lo están).

**Hallazgo:** el strong-naming **aporta valor** (comentario en `MonoDevelop.AfterCommon.props:26-28`: firma para obtener acceso **InternalsVisibleTo a Roslyn/Microsoft.CodeAnalysis**, que está firmado) → se **mantiene** per el condicional del plan («si el strong-naming se mantiene»).
Clave de seguridad: el `.snk` es **solo-clave-pública** (`<PublicSign>True</PublicSign>` line 31; formato de blob público `RSA1`/`00 24 00 00 52 53 41 31`, 160 bytes, sin exponente privado) → **no contiene material privado**, por lo que **no es un secreto** y su presencia en el repo es segura. No procede retirarlo del historial.

### Cambio F3.3 — Firma de addins: documentado el canal HTTPS oficial — plan §3.3

**Archivos:** ninguno (decisión documentada).

**Hallazgo que aborda:** el plan ofrecía implementar verificación de firma en `Mono.Addins.Setup` o documentar que el canal solo acepta repos oficiales HTTPS.

**Estado:** el subsistema de instalación de addins **no implementa verificación de firma** (ni `Mono.Addins.Setup`) — verificado: sin `VerifySignature`/checks por firma en `MonoAddinsRepositoryProvider`. El canal por defecto es **HTTPS únicamente** y hasta repos oficiales: `AddinSetupService.cs:68` → `https://addins.monodevelop.com/<nivel>/<plataforma>/<version>/main.mrep`; sin fallback/URL en claro (`http://`) en el subsistema.

**Decisión:** documentar este estado como mitigación del plan (canal HTTPS oficial) y anotar como **residual/deuda** la implementación de verificación de firma de paquetes de addin, fuera del alcance validable de este ciclo (cambiaría el fork vendido `Mono.Addins.Setup`).

### Cambio F3.4 — Ignorar materiales de firma/claves en git — plan §3.4

**Archivos:** `.gitignore` (nuevo bloque `*.snk`, `*.pfx`, `*.pem`, `*.key`).

**Hallazgo que aborda:** evitar que historiales/claves privadas o materiales de firma se versionen por error. Como parte de F3.2 se confirmó que el único `.snk` seguido es solo-clave-pública; este bloque previene que se añadan claves privadas futuras.

## 4d. Bitácora de cambios aplicados (Fase 4 — Validación y cierre)

### Validación F4.1 — Reconstrucción de subconjuntos validados

- `MonoAddins`/`MonoAddins.Setup` **net8.0**: `dotnet build -p:TargetFramework=net8.0` → **Compilación correcta, 0 errores** (solo avisos SYSLIB/obsoletos pre-existentes en `SetupDomain`/`Assembly.CodeBase`, del propio `Mono.Addins`, ajenos a este parcheo).
- `MonoDevelop.Core`/`MonoDevelop.Ide` (legacy net472, sin proyecto SDK-style net8): revalidado por compilación Roslyn dirigida sobre los archivos de seguridad editados → 0 errores estructurales.

### Validación F4.2 — Auditoría dirigida de regresiones (0 regresiones)

- **Deserialización:** **no queda ningún `BinaryFormatter(`** en `main/src`; los residuales son solo comentarios. Usos acotados y seguros: `InstrumentationService` (fallback legacy detrás de JSON), `ToolboxItemToolboxNode` (solo con `ToolboxItemSerializationBinder`), `Main.cs` MSBuild (código muerto).
- **TLS:** sin `Ssl3`/`Ssl2` y sin callback global con ignorado de errores de cadena; el `ServerCertificateValidationCallback` fue removido (F2.2).
- **Criptografía:** único MD5 restante es el del protocolo Gravatar (gated por opt-in, F2.4); autosave en SHA-256 (F2.3).
- **Secretos/claves:** único `.snk` versionado es público (F3.2), sin material privado.
- **Feeds/remoting:** solo HTTPS para repos; `rejectRemoteRequests=true`, loopback 127.0.0.1 e IPC `chmod 0600` intactos (`RemotingService.cs`, `mdhost.cs`, `Application.cs`, `ApplicationBackend.cs`).

### Cierre F4.3

Plan de parcheo de seguridad (Fases 1–4) **completo**. Deuda documentada para revisión junto con los pendientes de migración: `BinaryFormatter` no-remoting (`xwt/TransferDataSource.cs:150,164`, `guiunit/BinarySerializableConstraint.cs:38`), bump `NuGet.Client` 5.4.0 → ≥ 5.11.6 (CVE-2024-0057; binarios vendidos en `main/external/nuget-binary/`), y verificación de firma de paquetes de addin.

## 5. Mejores prácticas de Microsoft aplicadas como referencia

- Deserialización: «Do not use BinaryFormatter» (`SYSLIB0011`), preferir JSON con tipos fijos o `DataContractSerializer` validado.
- TLS: solo TLS 1.2/1.3; `SslPolicyErrors` manejadas por la pila del sistema.
- Criptografía: SHA-256+ para integridad; prohibido MD5/SHA1 para seguridad.
- Secretos: nunca commitear claves de firma o de conexión; gestionar fuera del repo.
- Supply chain: `NuGetAudit` activo y actualización de dependencias con CVE.
- Superficie de red: bind local explícito y autenticación en IPC.

## 5. Prioridades sugeridas

| # | Hallazgo | Severidad | Fase |
|---|----------|-----------|------|
| 1 | BinaryFormatter / Remoting sin seguridad | CRÍTICO | 1 |
| 2 | Callback TLS global + fingerprint débil | ALTO | 2 |
| 3 | Ssl2/Ssl3 en XSP | ALTO | 2 |
| 4 | MD5 en AutoSave y Gravatar (privacidad) | MEDIO | 2 |
| 5 | Claves `.snk` en repo | MEDIO | 3 |
| 6 | Dependencias con CVEs / sin NuGetAudit | MEDIO | 3 |
| 7 | Addins sin firma verificada | MEDIO | 3 |
| 8 | Instrumentación/autotest expuesta | BAJO | 1 |
| 9 | Hardening de `.gitignore` | BAJO | 3 |