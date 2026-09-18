# Fixes: arranque frío + diálogo About colapsado (net10/Linux)

Fecha: 2026-09-13
Estado: arranque COMPLETADO y verificado (warm + 2 cold boots, 0 errores fatales);
About dialog fijado y verificado (454×549).

## Objetivo
- Que el arranque **frío** (sin la db de addins) se comporte igual que el caliente:
  el addin `MonoDevelop.Ide` debe cargar sus extension points en el mismo primer boot.
- Sin `MD_MONO_LIB_DIR=/tmp/opencode/smoke/mono-lib` el boot **Segfaultea** en
  `g_markup_escape_text` (dlls gtk-sharp 2.24 parcheadas fuera); esa env var es
  prerrequisito de arranque, no parte de este fix.

## Síntoma (arranque frío pre-fix)
- Ventana nunca aparece; proceso muere a los ~10-20 s.
- `Ide.*.log` termina en:
  ```
  FATAL ERROR [..]: System.InvalidOperationException: Extension node not found in path: /MonoDevelop/Core/WebCredentialProviders
    at ... MonoDevelop.Core.Web.WebRequestHelper.Initialize ...
  ```
  (y a veces el segundo: `/MonoDevelop/Ide/KeyBindingSchemes`).
- Con la db ya existente (arranque caliente) el mismo binario funcionaba: no escaneo,
  sin crash.

## Causa raíz
`external/mono-addins/Mono.Addins/Mono.Addins.Database/SetupDomain.cs` en .NET Core.

Flujo del arranque frío:
1. `AddinEngine.Initialize`: `CreateHostAddinsFile` + `registry.Update` (escaneo de addins,
   crea `addin-db-003`) se ejecutan ANTES de `ActivateRoots` (mono-addins).
2. El escaneo corre **in-process** en .NET Core: `SetupDomain.GetDomain()` produce un
   `RemoteSetupDomain` en el MISMO AppDomain (`#else new RemoteSetupDomain ();`).
3. `RemoteSetupDomain.Scan` fija `AddinDatabase.RunningSetupProcess = true` ... y en .NET
   Core **nadie lo resetea**: en .NET Framework el flag se fijaba dentro del AppDomain
   remoto que luego se descartaba con `ReleaseDomain` (el estado no sobrevivía al dominio
   principal); en Core el flag queda en el dominio principal.
4. `CheckHostAssembly` (bucle de `ActivateRoots`, `AddinEngine.cs`):
   `if (AddinDatabase.RunningSetupProcess || asm is AssemblyBuilder || asm.IsDynamic) return;`
   → las 64 assemblies cargadas se saltan → los addins **root** (`MonoDevelop.Core`,
   `MonoDevelop.Ide`) nunca se insertan en el árbol de extensiones.
5. Sus extension points/nodesets no existen → primeros accesos runtime explotan:
   `WebRequestHelper.Initialize()` → `Extension node not found: /MonoDevelop/Core/WebCredentialProviders`.

Evidencia del trace instrumentado (`MADDINS_TRACE`, `/tmp/opencode/smoke/addins_trace.log`):
```
ACTIVATE: assemblies=64 RunningSetupProcess=True
CHK-SKIP: RunningSetupProcess=True  (x64)
ActivateRoots DONE                  (sin un solo INSERT)
```
Tras el fix, el mismo trace mostró `INSERT: MonoDevelop.VBBinding ...` etc.

## Fix aplicado
`external/mono-addins/Mono.Addins/Mono.Addins.Database/SetupDomain.cs`:
- En los `finally` de `Scan`, `GenerateScanDataFiles` y `GetAddinDescription` se añadió
  `AddinDatabase.RunningSetupProcess = false;` antes de `ReleaseDomain();` (con comentario
  explicando por qué es necesario en .NET Core).
- Se añadieron guards `#if NETFRAMEWORK` a `System.Runtime.Remoting.RemotingServices.Disconnect`
  (API no disponible/vacía en Core) y a la creación/destrucción del AppDomain en
  `GetDomain`/`ReleaseDomain` (esto ya estaba parcialmente en el worktree; aquí se completa
  y se blindan las llamadas a Remoting en los 3 métodos de escaneo).

NOTA: `AddinEngine.cs` NO quedó modificado. La instrumentación temporal y el refactor
`hostFileCreated` introducidos durante el diagnóstico se revirtieron por completo para dejar
el diff mínimo.

## Fix de versión (asociado)
- La app reportaba perfil/cache en `MonoDevelop/8.0` pero versión `9.0 Alpha Preview`.
- `src/core/MonoDevelop.Core/MonoDevelop.Core/UserProfile.cs` `ProfileVersions[]`:
  añadido `"9.0"` al final (el último elemento es el perfil actual); ahora
  `... "8.0", "9.0"`.
- Resultado: perfil/cache en `~/.cache/MonoDevelop/9.0/`, log
  `Starting MonoDevelop 9.0 Alpha Preview (9.0)`.
- Origen del 9.0: `BuildVariables.cs` generado por `configure` (`Version = "9.0"`,
  `VersionLabel = "9.0 Alpha Preview"`). La única dureza "8.0" de versión que quedaba en
  `src/**/*.cs` era `UserProfile.cs`.

## Archivos tocados en esta sesión
- `external/mono-addins/Mono.Addins/Mono.Addins.Database/SetupDomain.cs` — fix (flag reset + guards).
- `src/core/MonoDevelop.Core/MonoDevelop.Core/UserProfile.cs` — `"9.0"` en `ProfileVersions`.
- `build/net10run/Mono.Addins.dll` — rebuild + staging manual:
  `external/mono-addins/Mono.Addins/bin/net8.0/Mono.Addins.dll` → `build/net10run/`.
- `build/net10run/MonoDevelop.Core.dll` + `.pdb` — rebuild + staging desde el output del csproj.
- El resto de diffs del submódulo mono-addins (migración SDK-style net8/net472, `#if NETFRAMEWORK`
  wiring, InternalsVisibleTo, csproj) son modificaciones de sesiones previas del worktree, NO de esta.

## Verificación (2026-09-13)
Lanzamiento (siempre así):
```
cd build/net10run
MD_MONO_LIB_DIR=/tmp/opencode/smoke/mono-lib DISPLAY=:0 setsid ~/.dotnet/dotnet MonoDevelop.dll
```
Build durante el fix: `~/.dotnet/dotnet msbuild <csproj> -p:Configuration=Debug -t:Build`
(SDK correcto: `/home/daniel/.dotnet` con **10.0.401** — NO el 10.0.111 de `/usr/bin/dotnet`,
que no cumple `global.json` `"version": "10.0.401", "rollForward": "disable"`).

| Scenario | Db | Resultado |
|---|---|---|
| Cold pre-fix | `addin-db-003` borrado | FATAL `Extension node not found` (~10-20 s), sin ventana |
| Cold post-fix #1 | `addin-db-003` borrado | Ventana `0x00e00003` ~10 s, vivo 2:28+, 0 FATAL, db regenerada, Core+Ide cargados |
| Cold post-fix #2 | `addin-db-003` borrado | idem, vivo 2:06+, 0 FATAL |
| Warm post-fix | db existente | ventana visible, vivo 3:03+, 0 FATAL |

Criterio de error (scan sobre `Logs/Ide.*.log`):
```
grep -cE "FATAL|Extension node not found|Unknown node set" <log>   # → 0
```

Capturas: `/tmp/opencode/smoke/shot_warmboot.png`, `shot_coldboot1.png`, `shot_coldboot2.png`.

### Check UI: popup de búsqueda (as-you-type)
- El popup flotante que aparecía sobre la ventana (`0xe00295`, 484×43, tema oscuro)
  es el panel de búsqueda del IDE: icono de logo + texto **"No matches"** (confirmado
  visualmente sobre `popui_zoom.png`); idéntico (md5 `28f09366…`) al capturado como
  `ui_prefs.png`.
- Resultado final: la búsqueda probada devolvió **No matches** (ninguna coincidencia).

## Errores preexistentes NO relacionados (persisten, no fatales)
- `MonoDeveloperExtensions_nunit.dll` referenciado en el manifest y ausente (WARNING/ERROR
  en el dump del manifesto; no bloquea el boot).
- `System.NullReferenceException` en `ProxyCache.IsSystemProxySet` (Welcome page) — no fatal,
  el boot continúa.
- Warnings GTK themes: motor `murrine` no encontrado, `pk-gtk-module`.
- `WARNING: The add-in 'MonoDevelop.Gettext' ... missing dependencies`.
- `MonoDevelop.CSharpBinding` extendiendo un extension point inexistente
  (`/MonoDevelop/UnitTesting/NUnitSourceCodeLocationFinder`) por el addin NUnit ausente.

## Repro rápido del bug/fix
1. `rm -rf ~/.cache/MonoDevelop/9.0/addin-db-003`
2. Lanzar MD (comando de arriba), esperar ventana.
3. Pre-fix: crash con FATAL a los ~10-20 s. Post-fix: ventana y proceso estable, log sin FATAL.

## Fix: diálogo About colapsado (64×101)

### Síntoma
- Help → About abre un diálogo de 64×101 con el contenido prácticamente invisible
  (solo un "strip" vertical 14px que parecía scrollbar).
- `import -window <id>` confirmó: Allocation 14×14 del Dialog, `Notebook vis=False alloc=1x1`
  (req real 400×417). El contenido (imagen About 400×237, texto de versión, botón Show Details)
  existía pero nunca se mostraba.

### Causa raíz
- `MessageService.RunCustomDialog` (MessageService.cs:398) delega en
  `GtkWorkarounds.RunDialogWithNotification`, que en Linux hace simplemente `dialog.Run ();`.
- En GTK3 `Gtk.Dialog.Run()` solo hace `Show()` (no `ShowAll()` de los hijos).
- El ctor de `CommonAboutDialog` ya NO llama a `ShowAll()`: el commit upstream `c9d5ea4922`
  (2019) lo quitó del ctor. En Mac el diálogo se muestra vía el branch `#if MAC` que llama
  `instance.ShowAll ()` explícitamente dentro del handler; en Linux nadie lo hacía → colapso.
- Diagnóstico in-process con probe C# (`/tmp/opencode/smoke/probe/probe.cs`) instanciando
  `AboutMonoDevelopTabPage`/`CommonAboutDialog` sobre `build/net10run/MonoDevelop.Ide.dll`:
  - `ShowAll()`/`Resize` → diálogo sano 404×462 (imagen 400×237, notebook 400×417, botón 97×31).
  - `Show()` solo → diálogo 14×14, contenido sin mostrar → 64×101 con la pill caption.

### Fix aplicado
`src/core/MonoDevelop.Ide/MonoDevelop.Components/GtkWorkarounds.cs` (RunDialogWithNotification, ~línea 227):
```csharp
public static int RunDialogWithNotification (Gtk.Dialog dialog)
{
#if MAC
        MacRequestAttention (dialog.Modal);
#else
        // Gtk.Dialog.Run() only shows the dialog window, not its contents.
        // Ensure all children are visible so the dialog gets its natural size.
        dialog.ShowAll ();
#endif

        return dialog.Run ();
}
```
Beneficio: al arreglarlo en el punto común (`RunDialogWithNotification`), TODOS los diálogos
modales que pasan por `RunCustomDialog` quedan sanos, no solo About.

### Implementación y verificación
- Rebuild: `dotnet build src/core/MonoDevelop.Ide/MonoDevelop.Ide.csproj -c Debug -v m -nologo`
  → 0 errores (warnings CA2022/CA2200/MD0003/MD0009 preexistentes).
- Staging manual: `src/core/MonoDevelop.Ide/obj/Debug/net10.0/MonoDevelop.Ide.dll` (md5 `7c53b6d7…`)
  y `.pdb` → `build/net10run/`.
- Relanzado modo real (ventana `0xc001d0`): Help (`alt+h`), "About MonoDevelop" → **454×549** (antes 64×101).
- Interactividad probada: Enter activa el botón default "Show Details" → pasa a la página Version
  Information (el logo azul 400×237 desaparece y se muestra la lista) sin error.
- Capturas: `/tmp/opencode/smoke/about_fixed.png`, `/tmp/opencode/smoke/about_detail.png`.

### Nota: Add-in Manager (no bloquea el fix)
- Tools → "Extensions..." (MainMenu.addin.xml:249, primer ítem Linux) no abrió ventana tras
  click repetido (submenú de 2 ítems sí se despliega). No apareció diálogo en 15 s.
- El path usa `AddinsUpdateHandler.ShowManager → OpenAddinManagerWindow → AddinManagerWindow.Run`
  (mono-addins `Mono.Addins.Gui`), que también usa `dlg.Run()`; la ventana no llega a crearse
  (posiblemente `SetupService`/db de addins del entorno). Es un camino distinto al de
  `RunCustomDialog` y NO se ve afectado ni arreglado por el fix, queda como pendiente ajeno.

## Pendiente / próximos pasos
- Considerar llevar el fix a upstream de monoaddins: el flag `RunningSetupProcess` debe
  limpiarse al terminar el escaneo aunque corra in-process (.NET Core).
- Documentación transversal de la sesión: este doc + `docs/session_summary.md`
  (traza de la instrumentación en `/tmp/opencode/smoke/addins_trace.log`, logs de evidencia
  en `~/.cache/MonoDevelop/9.0/Logs/`).

## Fix: NRE de la Welcome page (feed de noticias) — 2026-09-14

### Síntoma
- Cada arranque log geo un `System.NullReferenceException` (no fatal, el boot continuaba)
  en `MonoDevelop.Core.Web.ProxyCache` durante la descarga del feed:
  `Updating Welcome Page from 'http://software.xamarin.com/Service/News'` →
  `WebRequestHelper.ProxyCache.GetProxy(uri)` → `IsSystemProxySet` → NRE en `line 124`.
- Evidencia original: `~/.cache/MonoDevelop/9.0/Logs/Ide.2026-09-13__21-19-12.log:89-91`.

### Causa raíz
- `WebRequest.DefaultWebProxy.GetProxy(uri)` devuelve `null` en .NET Core para URIs de
  acceso directo; el código hacía `new Uri (proxy.GetProxy (uri).AbsoluteUri)` sin guardar
  ese case → NRE en cada boot.

### Fix aplicado
`src/core/MonoDevelop.Core/MonoDevelop.Core.Web/ProxyCache.cs`:
- `IsSystemProxySet`: `Uri proxyUri = proxy.GetProxy (uri); if (proxyUri != null && ...)`
  antes de tocar `.AbsoluteUri`.
- `GetCredentialInternal`: guard `if (correctedProxyAddress != null && ...)`.

### Extra: fallo DNS del servicio muerto
- `software.xamarin.com` ya no existe; el feed nunca descarga. Con el fix de ProxyCache el
  error quedaba como un `HttpRequestException` con stack por boot.
- `src/core/MonoDevelop.Ide/MonoDevelop.Ide.WelcomePage/WelcomePageNewsFeed.cs`: rama para
  `HttpRequestException` con `inner SocketException` (`HostNotFound`/`ConnectionRefused`/...)
  → warning amigable `Welcome Page news server could not be reached.` y retorno limpio.
  Añadido `using System.Net.Http;` y `using System.Net.Sockets;`.

## Fix: combinación de idioma "(Default)" abría en inglés — 2026-09-14

### Síntoma
- Con `LANG=es_AR.UTF-8` (única var de locale del SO) y sin `UserInterfaceLanguage` en
  `MonoDevelopProperties.xml`, la UI arrancaba en inglés pese a existir
  `build/locale/es/LC_MESSAGES/monodevelop.mo`.
- El combo Saving/Load/Idioma de "*Preferences → General" mostraba "(Default)" y no aplicaba
  el locale del sistema como idioma visible.

### Causa raíz
- NO era detección de cultura: `CheckIsDefault`/`UICulture` ya derivaba `es-AR` del SO.
- Sí era la **ruta del catálogo** en el layout de build: el ctor de `Gettext` calculaba el
  catálogo como `Utils.PathCombine (prefix, "share", "locale")` (estilo instalación). En el
  worktree el runtime vive en `build/net10run/` y los `.mo` en `build/locale/` →
  `Directory.Exists(prefix/share/locale) == false` → `Gettext` quedaba con catálogo nulo y
  `Catalog.GetString` devolvía los msgids sin traducir (inglés).
- (En sesiones anteriores esto se camuflaba forzando `MONODEVELOP_LOCALE_PATH`.)

### Fix aplicado
`src/core/MonoDevelop.Core/MonoDevelop.Core/Gettext.cs` (ctor de `Gettext`):
```csharp
string [] candidates = {
    Utils.PathCombine (prefix, "share", "locale"),                      // instalado
    Path.GetFullPath (Path.Combine (locationDir, "..", "locale"))       // build: net10run/../locale
};
string catalog = candidates.FirstOrDefault (Directory.Exists);
```
- `locationDir` = directorio de la propia `MonoDevelop.Core` (build layout); `FirstOrDefault
  (Directory.Exists)` → sin excepción si ninguna candidata existe; añadido `using System.Linq;`.
- Al restaurar la ruta correcta, el ctor ya reporta `UICulture=es-AR` y traduce.

### Verificación (esta sesión)
- Instrumentación temporal `[GTDBG]` en el ctor (retirada al finalizar):
  `catalog=…/build/net10run/../locale exists=True`, `UICulture=es-AR CurrentCulture=es-AR`,
  `File='Archivo'` (log `Ide.2026-09-14__21-13-29.log`).
- Probe autónomo `/tmp/opencode/smoke/catprobe/` (referencia a `build/net10run/MonoDevelop.Core.dll`):
  con `CurrentUICulture=es-AR` + `Catalog.Init("monodevelop", build/locale)` traduce
  File→Archivo, Edit→Editar, View→Ver, Build→Compilación, Run→Ejecución, Tools→Herramientas,
  Solution→Solución, Documentation→Documentación.
- Verificación visual por píxeles (sin OCR): `welcome_1_raw.png` (pre-fix, EN) vs
  `final_es_arg.png` (post-fix): bandas y150+ difieren (max diff 255), el contenido localizado
  de la Welcome page ya no coincide con el inglés; la franja y81-151 (que parecía "menú
  idéntico") es el wordmark/header estático (`MonoDevelop`), no la barra de menús (los menús
  globales viven en el toolbar, sin texto visible → no son comparables por píxeles).

| Run (2026-09-14) | Estado |
|---|---|
| Pre-fix (02:11) | NRE en ProxyCache por boot; UI en inglés |
| Post-fix A+B con GTDBG (21:13) | log `Ide.2026-09-14__21-13-29.log`: catalog ok, es-AR, File='Archivo' |
| Aceptación final (21:26) | sin NRE, sin GTDBG, warning amigable del feed; welcome en español |
| Catprobe | traducción catálogo es confirmada |

Criterio: `grep -cE "NullReferenceException|GTDBG|HttpRequestException" {Ide.*.log}` → 0
(solo quedan los ERROR pre-existentes de `MonoDeveloperExtensions_nunit.dll`, ajenos).

### Archivos tocados / staged en esta sesión
- `src/core/MonoDevelop.Core/MonoDevelop.Core.Web/ProxyCache.cs` — Fix NRE (2 guards null).
- `src/core/MonoDevelop.Core/MonoDevelop.Core/Gettext.cs` — Fix ruta de catálogo (+Linq).
- `src/core/MonoDevelop.Ide/MonoDevelop.Ide.WelcomePage/WelcomePageNewsFeed.cs` — warning amigable DNS.
- `build/net10run/MonoDevelop.Core.dll(.pdb)` — rebuild + staging (md5 `fc9fcf04…`).
- `build/net10run/MonoDevelop.Ide.dll` — rebuild + staging (md5 `8d413291…`).
- Capturas/Sondeos: `final_es_arg.png`, `catprobe/`, `resprobe/` (4798 recursos embebidos en
  MonoDevelop.Ide.dll, incl. `welcome-*.png`), logs en `~/.cache/MonoDevelop/9.0/Logs/`.

## Pendiente / próximos pasos
- Considerar llevar el fix a upstream de monoaddins: el flag `RunningSetupProcess` debe
  limpiarse al terminar el escaneo aunque corra in-process (.NET Core).
- Documentación transversal de la sesión: este doc + `docs/session_summary.md`
  (traza de la instrumentación en `/tmp/opencode/smoke/addins_trace.log`, logs de evidencia
  en `~/.cache/MonoDevelop/9.0/Logs/`).
- Estabilización tras los 2 fixes de esta sesión (vigilar warm/cold boot y el warning amigable
  del feed), o pasar a la **migración de UI**.