# Handoff — Ronda 6: 3 bugs `//BUG:` de la interfaz Avalonia (subagente senior-dev)

- **Fecha:** 2026-11-09
- **Rama:** `agents/sub-agent-senior-dev-role-report`
- **Ámbito:** solo la interfaz Avalonia (shell). El código GTK legacy no se toca
  (regla 18.4: ambas interfaces siguen compilando y funcionando).

## Qué se hizo
Se corrigieron los 3 bugs marcados por el operador con `//BUG:` en
`MenuService.cs`. El marcador `//BUG:` se eliminó de cada línea al cerrar. Los
cambios son **puertos a Avalonia** (no se mezcla código GTK con Avalonia): todo lo
añadido vive en la rama `#if AVALONIA_SHELL` (shell) o en el modelo de comandos
neutral de toolkit; los handlers GTK `#if !AVALONIA_SHELL` quedan intactos para la
UI GTK.

| Bug | Síntoma (antes) | Causa raíz | Fix |
|-----|-----------------|------------|-----|
| 1 — "View List" (View) | No se mostraba en el menú | El array `ViewList` del port no-Gtk era vacío; el rol de lista de pads lo cubría un submenú shell-only `_Pads` | El array `ViewList` renderiza los submenús **Pads** y **Debug Pads** (los toggles `pad:<id>`, con checks por visibilidad real y separadoras, inventario idéntico a `_Pads`); se elimina el submenú `_Pads` de `MenuService.BuildView` |
| 2 — "_Add Reference..." (Project) | No abría el diálogo AddReference | El pipeline de dispatch funcionaba, pero el gate `SelectedNodeIsProject` quedaba `false`: la shell no auto-seleccionaba el nodo del pad solución al activar un documento (GTK sí: `ProjectSolutionPad.OnWindowChanged → SelectActiveFile`) | `AutoSelectSolutionNodeForActiveDocument()` en `OnDocSelectionChanged` selecciona la fila del documento activo (o el proyecto contenedor como fallback), de modo que el gate resuelve |
| 3 — "Custom command list" (Project) | No se mostraba en el menú | El array `CustomCommandList` del port no-Gtk era vacío | `ConfigurationService.ParseCustomCommands` parsea `CustomCommands` (Type=`Custom`) del .csproj → `ProjectFileModel.CustomCommands`; puente `ShellCommandState.SelectedProjectCustomCommands`; el handler no-Gtk rellena el array; `MainWindow.RunCustomCommand` **ejecuta de verdad** (proceso propio, salida al Output pad) |

## Por qué / decisiones
- **Directivas del operador (esta sesión):** (1) no mezclar código GTK y Avalonia —
  se **porta** GTK→Avalonia; (2) el código GTK porteado **no se borra**, se queda
  para la UI GTK (los handlers `#if !AVALONIA_SHELL` se mantienen intactos).
- **BUG 1 — contenido del submenú (decisión del operador):** paridad GTK + quitar
  `_Pads`. Los grupos se derivan del inventario de pads de la propia shell (el mismo
  conjunto y orden que el antiguo `_Pads`), ahora bajo **Pads / Debug Pads** como en
  GTK, **con separadoras** (un port Avalonia, no un Pad GTK). `MenuBuilder` aprende a
  renderizar un `CommandInfoSet` como submenu recursivo (`RebuildArraySiblings` +
  `AppendArraySetChildren`); cada hoja dispatchea el id del master (`ViewList`) con su
  propio dataItem, de modo que `OnMenuCommand` hace `TogglePad`. Los checks se refrescan
  en la cadena `TogglePad → SetPadVisible → BuildMenu → RefreshCommandStates →
  RebuildArraySiblings → Update`.
- **BUG 3 — parsing (forma del XML en disco, confirmada en la fuente de la
  serialización):** el writer legacy (`CollectionDataType.OnSerialize`) anida los
  comandos como un grupo `CustomCommands` cuyo **nombre de elemento es el mismo que el
  de los items** (ambos `CustomCommands` — fijado por el test
  `MSBuildXmlNamespaceNotAddedToCustomCommand`). El wrapper es un contenedor puro (no
  tiene campo `Command`); cada item porta `type`/`name`/`command`/`workingdir`/
  `externalconsole`/`pauseexternalconsole` (lowercase, nombres de la reflexión) +
  `EnvironmentVariables/Variable name/value`. El parser tolerante filtra los
  `CustomCommands` descendientes que **portan un campo Command** (excluye el wrapper,
  sin duplicados), lee cada campo primero de atributo y luego de elemento hijo
  (case-insensitive), y descarta los `Type != Custom`. Tolerante: un bloque roto no
  rompe el modelo del proyecto (mismo criterio que el resto de `ParseProjectFile`).
- **BUG 3 — ejecución (paridad con el legacy `CustomCommand`):** se parsea con la regla
  `CustomCommand.ParseCommand` (token inicial entrecomillado = exe, resto = args; si no,
  el primer split por espacio), se sustituyen los tags `${ProjectDir}`/`${ProjectName}`,
  el working dir por defecto es el directorio del proyecto, un exe no enraizado se resuelve
  contra el directorio del proyecto (back-compat legacy) y se aplican las env vars
  declaradas. Es un **proceso propio** (no el `runningProc` de build/run), por lo que no
  salta los gates de Stop/IsOperationRunning; la salida va al Output pad. `ExternalConsole`
  / `PauseExternalConsole` no tienen "consola separada" en la shell headless: se reconocen
  en el log (no se ignoran en silencio).
- **BUG 2 — auto-selección (port de la paridad GTK):** al activar un documento, la shell
  selecciona la fila del fichero en el pad solución (su tag es la ruta completa, la misma
  que el build del árbol guarda para las filas de fichero) o, si el fichero no es nodo de
  la solución, el proyecto contenedor (prefijo de directorio contra
  `GetSolutionProjectPaths()`). Los documentos sin título / no-fichero y el estado sin
  solución son no-ops.

## Archivos tocados (8, todos fuentes; sin submódulos, sin artefactos de build)
| Fichero | Cambio |
|---------|--------|
| `MonoDevelop.Startup.Avalonia/Views/MenuService.cs` | Elimina el submenú `_Pads` de `BuildView`, quita las 3 marcas `//BUG:`, actualiza el comentario del menú View |
| `MonoDevelop.Startup.Avalonia/Views/MainWindow.axaml.cs` | `AutoSelectSolutionNodeForActiveDocument` (BUG 2) + llamada en `OnDocSelectionChanged`; `SelectedProjectCustomCommands` + `RunCustomCommand` (BUG 3, ejecución real); case `ViewList` maneja `pad:<id>` → `TogglePad` (BUG 1); case `CustomCommandList` ejecuta (BUG 3); flags QA `--autoselectqa`, `--runcustom=`, y `--openmenu=` con cadena de submenús |
| `MonoDevelop.Startup.Avalonia/Views/MenuBuilder.cs` | `RebuildArraySiblings` renderiza `CommandInfoSet` como submenu recursivo + `AppendArraySetChildren` |
| `MonoDevelop.Startup.Avalonia/Program.cs` | Registro de `--runcustom=` y `--autoselectqa` en la validación de args QA (infraestructura QA permanente) |
| `MonoDevelop.Ide/MonoDevelop.Ide.Commands/ViewCommands.cs` | `ViewListHandler` no-Gtk: `Update(CommandArrayInfo)` rellena Pads/Debug Pads (rama `#else`, GTK intacto) |
| `MonoDevelop.Ide/MonoDevelop.Ide.Commands/ProjectCommands.cs` | `CustomCommandListHandler` no-Gtk: `Update(CommandArrayInfo)` rellena los comandos (rama `#else`, GTK intacto) |
| `MonoDevelop.Ide/MonoDevelop.Ide.Commands/ShellCommandState.cs` | Puentes `IsPadVisible` (BUG 1) y `SelectedProjectCustomCommands` (BUG 3) dentro de `#if AVALONIA_SHELL` |
| `MonoDevelop.Ide/Services/ConfigurationService.cs` | `CustomCommandEntry` record + `ProjectFileModel.CustomCommands` + `ParseCustomCommands` y helpers (fichero compartido GTK/shell, fuera de `#if`; el gate lo compila para ambas) |

## Cómo se valida
Arranque (Xvfb `:99`): binario `main/build/MonoDevelop.AvaloniaShell.dll`,
`--sln=~/opencode/r6_bugfix/QaProj.sln`. Scripts de arranque/captura y fixture en
`~/opencode/r6_bugfix/` (`qa_run.sh`, `cap_menu.sh`, `cap_addref_dialog.sh`, `QaProj.*`,
`AutoSelectProbe.cs`). Los artefactos de QA están en `~/opencode/r6_bugfix/` y
`~/opencode/r6_bugfix/qa_tester/` (nunca en `/tmp`, regla 18.6).

**BUG 1 (View List):**
- `--menuruntime` fase initial: bajo `_View` → `Pads` (12 pads, orden + 2 separadoras:
  Solution, Classes, Help | Toolbox, Properties, Document Outline, Unit Tests | Output,
  Errors, Tasks, Code Issues, Search Results) y `Debug Pads` (Call Stack, Locals, Watch,
  Breakpoints, Threads), todos con tag `pad:<id>`; `_Pads` = 0 ocurrencias.
- Por píxeles `--openmenu=View,Pads` (checks reales en Solution/Properties/Output) y
  `--openmenu=View,Debug Pads`.

**BUG 2 (Add Reference):**
- `--autoselectqa`: `BASE` (sin selección) `addref=disabled`; `INSIDE` (documento de la
  solución abierto) auto-selecciona la fila → `SelectedNodeIsProject=True addref=ENABLED`;
  `OUTSIDE` (fichero fuera) fallback al proyecto contenedor → `addref=ENABLED`.
- `--cmdstates`: `node_project` y `node_file` con `editRefs=1` (`node_solution` `editRefs=0`).
- `--dispatchaddref`: con selección `itemEnabled=True` y se abre la ventana "Add Reference"
  (X11/xwininfo); sin selección (`DISPATCH_NOSSEL=1`) `itemEnabled=False` y no se abre nada.

**BUG 3 (Custom command list):**
- `--runcustom="R6 QA Marker"`: parsea 2 comandos (Type=Build filtrado), ejecuta
  `touch "${ProjectDir}/r6_marker.txt"` (sustitución `${ProjectDir}`), `marker exists=True`.
- `--runcustom="R6 QA Env"`: la env var declarada llega al proceso hijo
  (`printenv R6QAVAR` → `hello-r6`).
- `--projmodel`: 4 aserciones `customcommands` ok, total `15 ok / 0 fail`.
- Por píxeles `--openmenu=Project`: "R6 QA Marker" y "R6 QA Env" inline (Build filtrado).

## Invariantes de gate (medidas, no asumidas)
| Invariante | Esperado | Medido |
|------------|----------|--------|
| gate full `dotnet msbuild Main.sln -p:Configuration=Debug -m:1 -t:Build -p:DisableDownloadNupkg=true` | 0 errores (GTK + shell, reg 18.4) | `EXIT=0`, 0 líneas `: error` (solo warnings preexistentes) |
| shell `dotnet build ... -c DebugLinux -f net10.0` | 0 errores | `EXIT=0` |
| tests `AvaloniaShell.Editor.Tests -c DebugLinux -f net10.0` | 103/103 | **Superado: 103, Con error: 0** |
| catálogo (startup) | 475 commands, 113 resolvable | `catalog loaded: 475 commands, 113 with resolvable default handler` |
| `--menudump` (recents reseteados) | items=270, bindings=239 | `menus=11 items=270 cmds=0 bindings=239` |

**Cambio de baseline de `--menudump` (intencional, explicado):** antes 288 items / 256
bindings; ahora **270 / 239**. Delta = **−18 items / −17 bindings**, que es exactamente el
bloque `_Pads` eliminado: 17 `Item` (`pad:<id>`, ids de comando distintos) + 1 `Sub`
(sin id). Los 17 toggles de pads no desaparecen: ahora viven en el **array runtime**
(Pads/Debug Pads bajo View List), que `--menudump` (modelo estático) no cuenta pero
`--menuruntime` muestra. No es regresión.

## Validación — Tester QA Senior (subagente, AGENTS.md §18.5)
Encargo (`~/opencode/r6_bugfix/qa_brief.md`) y reporte (`~/opencode/r6_bugfix/qa_report.md`),
artefactos en `~/opencode/r6_bugfix/qa_tester/` (logs `t_*.log` + capturas `t_*.png`).
**Veredicto: APROBADO. Hallazgos P1–P4: 0.** 3 P5 informativos (sin acción): (F1) el flag
`--runcustom` imprime `marker exists=False` también para el comando env (ruido cosmético,
la verificación del env va por `hello-r6` en el log); (F2) ~7075 warnings del gate
(todos de tipos preexistentes documentados; la invariante exige 0 **errores**); (F3) known
issue preexistente de la máquina — el runtime GTK legacy no abre ventana aquí (se valida por
compilación, como en las rondas anteriores). El subagente re-ejecutó la batería completa de
forma autónoma y verificó contra los valores exactos (270/239, 475/113, 103/103, 15/0,
delta −18/−17, `hello-r6`, marker, `editRefs=1`, `itemEnabled=True/False`).

## Qué queda pendiente / notas
- **Nada bloqueante.** La Ronda 6 cierra con commit+push de los 8 ficheros.
- **Flags QA permanentes añadidos** (infraestructura, se quedan): `--autoselectqa`,
  `--runcustom=<nombre>`, y la cadena de submenús en `--openmenu=<Top[,Sub]>`. Son el
  mecanismo de verificación determinista de estos 3 bugs; el `--dispatchaddref` de la
  misma ronda ya estaba documentado.
- **Incertidumbre residual resuelta:** la forma exacta del XML `CustomCommands` en disco se
  confirmó trazando la serialización legacy (`CollectionDataType` + `ItemProperty`): wrapper e
  items ambos `CustomCommands`, campos como elementos hijos (lowercase). El parser tolerante
  (atributo o elemento, case-insensitive) cubre ambas formas y se validó con un fixture real.
- Pendientes de rondas futuras (ya documentados, no pedidos en esta): (a) orden Run Code
  Analysis Sol/Proj vs GTK; (b) Update/Restore NuGet como acciones reales; (c) guard de
  `BeginMoveDrag` en `TitleBarRow`; (d) dispatch de ítems "not wired" Ronda 1; (e) 16
  divergencias de label intencionales.
