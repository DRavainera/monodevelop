# MonoDevelop.AvaloniaShell — Shell de UI Avalonia

Shell de la nueva interfaz del IDE, migrado desde la UI legacy GTK (`MonoDevelop.Ide`)
a **Avalonia 12.1.2 + .NET 10**, sin dependencias de GTK ni del runtime Mono.
Este documento describe su arquitectura, cómo ejecutarlo y los hooks de QA
automatizada que ejercitan cada módulo migrado.

> Documentación de contexto: plan general en `docs/interfaz-plan.md` (hitos M4 →
> M11y), estado global en `docs/migration-status-report.md`, bitácora de sesión
> en `docs/session_summary.md`.

## Cómo ejecutar

```bash
# Build (la raíz del repo pinea el SDK 10.0.401 en su global.json, que ya
# satisface el requisito de Roslyn ≥ 4.14 de los generators de Avalonia 12)
cd main/src/core/MonoDevelop.Startup.Avalonia
~/.dotnet/dotnet build -v q

# Run (binario real del shell Avalonia — salida unificada en main/build, las DLLs
# Avalonia y el runtime GTK conviven en esa misma carpeta; el proyecto también
# compila como parte de main/Main.sln)
~/.dotnet/dotnet build/MonoDevelop.AvaloniaShell.dll

# UI legacy GTK# (compatibilidad oculta durante la rama 9.x): un solo proyecto y
# una sola carpeta build; --old-gui reenvía la invocación al MonoDevelop.dll de
# main/build propagando argumentos y exit code. MonoDevelop.dll no se puede
# lanzar directo (guardia MONODEVELOP_LEGACY_UI que solo el relay establece).
~/.dotnet/dotnet build/MonoDevelop.AvaloniaShell.dll --old-gui
```

Opciones de línea de comandos:

| Opción | Efecto |
|---|---|
| `--sln=<ruta>` | Abre la solución indicada en el Solution pad al arrancar |
| `--skip-welcome` | Salta la Welcome Page (arranque directo al shell) |
| `--<qa-hook>` | Ejecuta un hook de QA tras el arranque (ver tabla abajo) |

⚠️ **No confundir con el IDE legacy**: `main/build/net10run/MonoDevelop.dll` es
el binario GTK viejo (muere en remoting). El shell Avalonia es SIEMPRE
`MonoDevelop.AvaloniaShell.dll`.

## Arquitectura

```
MonoDevelop.Startup.Avalonia/
├── Program.cs                  # Entry point: args, QA hooks, lifetime clásico
├── App.axaml(.cs)              # Temas (ThemeDictionaries paleta Ide*), estilos (pestañas isla)
├── Views/                      # Ventanas y diálogos (XAML + code-behind)
│   ├── MainWindow.*            # Shell: menú, toolbar, pads, pestañas isla, dispatch de comandos
│   ├── MenuService.cs          # Modelo de menú (estructura/íconos/atajos del MainMenu.addin.xml legacy)
│   ├── MenuBuilder.cs          # Árbol MenuEntry → Menu/MenuItem de Avalonia (mnemónicos _X, separadores)
│   ├── WelcomePageView.*       # Welcome Page réplica del legacy (lista de recientes + secciones)
│   ├── NewSolutionDialog.*     # Diálogo Nueva Solución (plantillas + ubicación)
│   ├── PreferencesDialog.*     # Preferences (árbol de secciones, idioma, temas, build, etc.)
│   ├── AddinManagerDialog.*    # Add-in Manager (chrome Avalonia, sin decoraciones del OS)
│   ├── AboutDialog.*           # Acerca de (imagen de marca incluida)
│   ├── FindInFilesDialog.*     # Find/Replace in Files (sobre archivos reales)
│   ├── AddReferenceDialog.cs   # Add Reference (edita el .csproj real)
│   ├── GoToDialog.cs           # Go To File/Type/Line
│   ├── DirtyFilesDialog.*      # "Save Files": gate de cierre con documentos modificados
│   └── InputDialog.cs          # Input genérico (Rename, New Folder, herramientas…)
└── MonoDevelop.Startup.Avalonia.csproj # XAML + code-behind que conecta; la lógica vive INTEGRADA (ver abajo)

### Integración con MonoDevelop.Ide y MonoDevelop.Debugger (M16e)

El shell NO tiene módulos propios de lógica: todo vive integrado junto a los
demás módulos del IDE (sin carpetas `Avalonia/` ni código duplicado):

- `main/src/core/MonoDevelop.Ide/Services/` — los 10 servicios (ns
  `MonoDevelop.Ide.Services`): IconService, SolutionLoader, UserPreferences,
  SettingsStore, KeyboardShortcutRegistry, NavigationHistoryService,
  TaskScanner, ExternalToolRunner, GettextService, ConfigurationService.
- `main/src/core/MonoDevelop.Ide/Controls/` — los 4 controles (ns
  `MonoDevelop.Ide.Controls`): SkTextEditor, PadHost, CompletionPopup,
  EditorTooltipPopup.
- `main/src/addins/MonoDevelop.Debugger/` — los servicios de debug junto a
  `PinnedWatch*.cs` (ns `MonoDevelop.Debugger.Services`): WatchService,
  BreakpointService, DebugSessionService.

Mientras la superficie Avalonia de `MonoDevelop.Ide` no tome el mando, el
csproj del shell compila esas fuentes con `<Compile Include="..">` — fuente
única, sin copias. `DebugType=embedded` (PDB embebido) evita el lock del
handle fantasma sobre `obj/Debug/*.pdb` que bloqueaba rebuilds tras
reinicios del entorno.
```

### Decisiones clave

- **Chrome propio de Avalonia**: `SystemDecorations=None` +
  `ExtendClientAreaToDecorationsHint` en TODAS las ventanas — bordes de la app,
  nunca del OS; la barra de menú hace de barra de título (drag/doble clic).
- **Render del editor**: cada frame se dibuja en un `WriteableBitmap` NUEVO vía
  SKSurface (el compositor de Avalonia puede leer el frame anterior mientras
  `Render` corre; repintar el bitmap compartido producía líneas fantasma).
- **Paridad con el legacy**: cada ventana/pad/comando se porta leyendo su código
  GTK de referencia (`MonoDevelop.Ide`, `MonoDevelop.SourceEditor2`,
  `MonoDevelop.DesignerSupport`, `MonoDevelop.VersionControl`), respetando
  estructura, íconos (`MonoDevelop.Ide/icons/`), mnemónicos y atajos.
- **Persistencia legacy-compatible**: preferencias, recientes, atajos y
  herramientas viven en los mismos archivos de configuración del IDE
  (`~/.config/MonoDevelop`, `~/.local/share/MonoDevelop`).

## Hooks de QA automatizada

Cada hook abre la app, ejercita un módulo de forma determinista y loguea
`[tag] ...` en stdout. Patrón general:

```bash
~/.dotnet/dotnet build/MonoDevelop.AvaloniaShell.dll \
  --sln=/ruta/TestProj.sln --<hook> 2>&1 | grep -E "\[<tag>\]"
```

⚠️ **Watchdog de arranque**: si la UI no abre en 30s (`MD_STARTUP_WATCHDOG=<secs>`
lo ajusta) el shell imprime `[fatal]` con la causa conocida (bucle de
SkiaSharp/fontconfig con fuentes WOFF/WOFF2 del usuario) y sale con código 2.
⚠️ **Si el arranque se cuelga sin abrir ventana** (gira al 100% CPU sin
imprimir nada): la causa son las fuentes de usuario con directorios
WOFF/WOFF2 (`~/.local/share/fonts/**`) — el `SkFontMgr_fontconfig` de
libSkiaSharp entra en bucle infinito dentro de `FcPatternGetString` al
inicializar Avalonia.Skia (antes de crear la ventana). Workaround no
destructivo para QA: apuntar `FONTCONFIG_FILE` a una config con solo fuentes
de sistema:

```bash
cat > /tmp/fonts-qa.conf <<'EOF'
<?xml version="1.0"?>
<!DOCTYPE fontconfig SYSTEM "fonts.dtd">
<fontconfig>
  <dir>/usr/share/fonts</dir>
  <cachedir>/tmp/fccache-qa</cachedir>
</fontconfig>
EOF
FONTCONFIG_FILE=/tmp/fonts-qa.conf xvfb-run -a -s "-screen 0 1600x1000x24" \
  ~/.dotnet/dotnet build/MonoDevelop.AvaloniaShell.dll --sln=/ruta/TestProj.sln --<hook>
```

### `--editqa` — editor de código (M11y)

Ejercita el `SkTextEditor` de extremo a extremo sin input sintético:

```
[editqa] insert grew: True; backspace removed char: True     # Backspace funciona (multi-caret)
[editqa] hover info on Main: True header='Main  (method)'    # Tooltip hover con firma
[editqa] completion popup visible: True                      # Intellisense tras "Console."
[editqa] committed: True                                     # Commit → "Console.WriteLine" insertado
[editqa] completion items Write*: 1 (expects WriteLine>0)    # Fuente de datos del popup
[editqa] editor pad alive with 1 tab(s)                      # Pad del editor vivo
[editqa] restored text: True                                 # Archivo restaurado (QA no destructivo)
```

Restaura el archivo original al terminar (nunca deja residuo en el proyecto).

### `--props` — Properties pad (M11w)

Selecciona solución → proyecto → archivo en el árbol y verifica que el pad
Properties muestre las filas del descriptor correspondiente (PropertyGrid
legacy: Name/File Path/Root Directory/File Format/Target framework/…).
Verde esperado: filas reales leídas del `.sln`/`.csproj`, 0 excepciones.

### `--dirtyfiles` — gate de cierre (M11x)

Verifica el DirtyFilesDialog de punta a punta: ensucia `Program.cs` → el diálogo
agrupa bajo `Project: TestProj` → "Save and Quit" persiste en disco →
`dirty=False` → el gate bloquea el cierre mientras haya sucios. El hook deja el
documento sucio a propósito para poder probar el diálogo visualmente
(WM_DELETE → "Save Files" debe aparecer).

### Otros hooks (por módulo)

| Hook | Módulo | Tag |
|---|---|---|
| `--about` / `--addins` / `--prefs[=panel]` / `--welcome` / `--newsolution` / `--find` | Diálogos directos | — |
| `--build` / `--buildone` / `--run` | Build/rebuild por proyecto, ejecución | `[build]`/`[run]` |
| `--goto` / `--gotoline[:col]` | Go To File/Type/Line | `[goto]` |
| `--tasks` | Pad Tasks (escáner TODO) | `[tasks]` |
| `--tool` | Herramientas externas | `[tool]` |
| `--editops` | Operaciones de línea/undo/redo | `[editops]` |
| `--windocs` | Menú Window (ciclo de documentos) | `[windocs]` |
| `--navhist` | Back/Forward/Zoom | `[navhist]` |
| `--bookmarks` | Bookmarks + gutter | `[bookmarks]` |
| `--addref` | Add Reference (edita csproj) | `[addref]` |
| `--brace` | Go To Matching Brace | `[brace]` |
| `--mcaret` | Multi-caret (Alt+Shift+. / , / A) | `[mcaret]` |
| `--fmt` | Format Buffer (reindent por llaves) | `[fmt]` |
| `--diff` | Diff VCS real (git) | `[diff]` |
| `--fold` | Plegado de código | `[fold]` |
| `--viewcmds` | Layouts de pads, CenterCaret, resultados | `[viewcmds]` |
| `--bubbles` | Burbujas inline del editor | `[bubbles]` |
| `--compl` | Complete Word / plantillas / surround | `[compl]` |
| `--ctxmenu` | Menú contextual del Solution pad | `[ctx]` |
| `--filter` | Búsqueda incremental del Solution pad | `[filter]` |
| `--totd` | Tip of the Day (tips del XML legacy) | `[totd]` |
| `--progress` | ProgressDialog (tareas anidadas + ShowDone) | `[progress]` |
| `--newconfig-real` | NewConfiguration persiste en .sln/.csproj y limpia | `[newconfig-real]` |
| `--openimport` | File>Open importa un .csproj suelto (wrapper .sln) | `[openimport]` |
| `--activeconfig` | Active Configuration: persistencia en .userprefs + switch | `[activeconfig]` |
| `--newproject` | New Project en modo add-to-solution (temp, no destructivo) | `[newproject]` |
| `--bmkpad` | Pad Bookmarks: toggle/navegación/listado + menú contextual | `[bmkpad]` |
| `--bkpad` | Pad Breakpoints: toggle/persistencia .userprefs/navegación (con `--keepbps` deja el estado para capturas) | `[bkpad]` |
| `--locals` | Run con debug (netcoredbg DAP): parada en bp, resaltado, Locals reales, threads/frames, evaluate | `[locals]`/`[debug]` |
| `--attachdlg` | Tab Attach to Process: /proc real, filtro, Attach | `[attachdlg]` |
| `--watch` | Pad Watch: evaluate DAP real, add/remove expresiones | `[watch]` |
| `--condbp` | Breakpoint condicional + hit count: persistencia + DAP + parada | `[condbp]` |
| `--attachreal` | Attach DAP real a un proceso .NET vivo + detach (proceso sobrevive) | `[attachreal]` |
| `--step` | Stepping: Step Over desde el bp, highlight movido, pads refrescados | `[step]` |
| `--tree` | Locals como árbol expandible (variablesReference, hijos lazy) | `[tree]` |
| `--imm` | Immediate pad: evaluate en el frame + resultado en Output | `[immediate]`/`[imm]` |
| `--gutterbp` | Clic en el gutter = toggle bp (como legacy) + data tip inline + hover del gutter (banda, cursor mano, tooltip "Line N") | `[gutterbp]` |
| `--frame` | Call Stack: seleccionar frame muestra SUS locals (scopes por frameId) | `[frame]` |
| `--immcompl` | Immediate: autocompletado de miembros tras `.` (DAP), commit Tab/Enter | `[immcompl]` |
| `--persistqa` | Sesión de debug persistida: cerrar/reabrir solución restaura bps+watches+config | `[persist]` |
| `--watchedit` | Pad Watch: edición in-place (Esc rollback, Enter reemplaza en sitio) + reevaluación automática tras el step | `[watchedit]` |
| `--pinwatch` | Pinned watches: burbujas por línea, serialización file/line legacy en .userprefs, valor vivo al pausar, unpin, burbujas CLICABLES (menú Remove / Go to line) | `[pinwatch]` |
| `--legacyqa` | Pins escritos por el IDE GTK legacy (XML exacto de PinnedWatchStore): siembra, reabre y verifica restauración + round trip del formato | `[legacyqa]` |

QA visual: los hooks se complementan con capturas X11 (`magick x:<win>`) para
comparar la UI contra la legacy GTK en vivo (ver bitácoras en
`docs/interfaz-plan.md`).

### Configuraciones e importación

- `MonoDevelop.Ide/Services/ConfigurationService.cs` persiste configuraciones donde las guarda
  el legacy: `GlobalSection(SolutionConfigurationPlatforms)` + mapeos por GUID
  en el .sln, y `<PropertyGroup Condition=" '$(Configuration)|$(Platform)'" />`
  en cada .csproj ("Any CPU" ⇄ "AnyCPU"). El New Configuration dialog crea la
  config real y recarga el árbol.
- File > Open enruta `.sln`/`.slnf` → solución, `.csproj` → importación
  (crea el `.sln` wrapper con `AddProjectToSolution`, como
  `ProjectOperations.ImportProject`), resto → pestaña de documento.
- **Configuración activa**: `Get/SetActiveConfiguration` persiste en
  `<sln>.userprefs` (`MonoDevelop.Ide.Workspace/ActiveConfiguration`, formato
  legacy); alimenta el combo de la toolbar y el menú Project > Active
  Configuration (dinámico desde el .sln).
- **New Project añade a la solución**: con solución abierta el diálogo de Nueva
  Solución marca "Add to open solution" y `AppendProjectToSolution` inserta el
  proyecto (GUID + mappings ActiveCfg/Build.0) en el .sln y recarga el árbol.
- **Pad Bookmarks** (zona debug): lista los bookmarks del documento activo;
  doble clic salta a la línea; se refresca al toggle/clear/cambio de documento.
  Menú contextual: Previous/Next Bookmark, Remove bookmark y Remove All
  Bookmarks (semántica del pad del SourceEditor legacy).
- **Breakpoints** (`MonoDevelop.Debugger/BreakpointService.cs`): el pad Breakpoints (zona
  debug, icono `md-breakpoint`) lista `Archivo:línea` de todos los documentos
  con marcadores rojos/grises en el gutter (toggle por clic) y menú Go to /
  Enable-Disable / Remove / Clear All. Persiste en `<sln>.userprefs` bajo
  `MonoDevelop.Ide.DebuggingService.Breakpoints` con el formato XML de
  Mono.Debugging (`<Breakpoint file relfile line column [enabled]>`, líneas
  1-based) y se restauran al abrir cada documento. Build y Run usan la
  configuración activa: `dotnet build -c "<config>"` y
  `dotnet run -c "<config>"`.
- **Debug real con netcoredbg (DAP)** (`MonoDevelop.Debugger/DebugSessionService.cs`):
  netcoredbg vive como submodule `main/external/netcoredbg` (fork de Samsung;
  NUNCA DLLs binarios externos) y se compila desde fuente
  (`cmake + clang`, generador Makefiles; binario en
  `external/netcoredbg/build/src/netcoredbg`). El botón Debug de la toolbar y
  Run > Debug lanzan la sesión DAP con los breakpoints persistidos: el
  proceso para en la línea y el editor la resalta (fondo amarillo); los pads
  Locals/Watch se llenan con los valores reales del frame detenido
  (`Name = Value`); Call Stack muestra los frames navegables (doble clic va
  al frame) y Threads los threads reales (doble clic cambia el stack). El
  Watch reevalúa sus expresiones en cada stop (evaluate DAP, add/remove con
  menú del pad). Continue/Pause/Stop/Detach reutilizan la sesión; el attach
  (comando DAP `attach` — el launch de netcoredbg ignora `mode=attach`)
  pausa con el PID y el detach deja el proceso vivo.
- **Attach to Process**: tab del pad inferior (NUNCA una ventana con borde
  de OS — todo el shell usa chrome Avalonia). Enumera procesos reales de
  /proc (cmdline/comm, skip kernels/self, estado de /proc/<pid>/stat) con
  filtro, Refresh y Attach; Run > Attach to Process abre el tab y Attach
  ejecuta el attach DAP real al PID con los breakpoints persistidos.
- **Breakpoints avanzados**: Condition…/Hit Count…/Tracepoint… en el menú del
  pad; se persisten en `<sln>.userprefs` (`condition`/`hitcount`/
  `tracepoint`, formato Mono.Debugging legacy) y viajan al adaptador DAP
  (`condition`/`hitCondition`/`logMessage`). Las filas del pad muestran
  `when <cond>`, `(hit N)` y `print: <msg>`.
- **Stepping y hover eval**: Step Over/Into/Out (F10/F11/Shift+F11) con
  botones en la toolbar y menú Run completo (Debug F5, Continue, Pause,
  Stop, Detach, Attach to Process). En pausa, el hover del editor muestra
  `word = <valor DAP>` (evaluate en el frame actual) en lugar de la
  descripción estática.
- **Locals/Watch como árboles**: variables con hijos expanden lazy (DAP
  variablesReference), como el árbol del pad legacy; Watch reevalúa sus
  expresiones en cada stop.
- **Immediate pad**: expresiones contra el proceso detenido (Enter o Run);
  el resultado queda en el Output (`[immediate] <expr> = <valor>`).
- **Autocompletado del Immediate**: al teclear `expr.` el prefijo se evalúa
  por DAP y sus miembros llenan un popup (`nombre  valor`); Down/Up mueven,
  Tab/Enter/doble clic confirman (`expr.<miembro>`), Esc oculta.
- **Breakpoint con clic en el gutter**: la franja de iconos del gutter (los
  últimos 18px, donde vive el círculo rojo) hace toggle del bp de la línea
  (Mono.TextEditor ActionTextArea "left margin click"); el resto del gutter
  mueve el caret como siempre.
- **Data tip inline**: al pausar, una burbuja verde en la línea parada muestra
  el primer identificador evaluable con su valor (`greeting = "hello"`,
  evaluate DAP en el frame actual); se limpia en Continue/Step/Stop.
- **Cambio de frame en el Call Stack**: seleccionar un frame recarga los
  Locals con los scopes de ESE frame (`scopes` con frameId → variables), como
  el StackFrame pad legacy; doble clic navega al código.
- **Watch con edición in-place**: doble clic o menú "Edit Watch…" pone un
  TextBox sobre la fila (como el Watch pad legacy); Enter confirma
  (reemplaza la expresión en sitio preservando el orden, dedup, persiste y
  reevalúa), Esc cancela. Tras CADA stop (steps incluidos) el pad reevalúa
  sus expresiones sin intervención.
- **Pinned watches como burbujas (paridad PinnedWatch legacy)**: "Pin Watch"
  en el menú contextual del editor fija la palabra bajo el caret a la línea
  actual; la burbuja ámbar muestra `expr = valor` (evaluado por DAP en cada
  stop) o `expr = ?` fuera de sesión. Las burbujas son CLICABLES: right-click
  sobre una burbuja abre su menú (Remove pinned watch / Go to line). Se
  serializan en la MISMA clave legacy
  `MonoDevelop.Ide.DebuggingService.PinnedWatches` con la ubicación completa
  (file relativo a la solución, line 1-based, column/endLine/endColumn/
  offsetX/offsetY + expression) — el IDE GTK legacy y el shell Avalonia
  comparten el mismo `.userprefs` y los pins escritos por el GTK viejo se
  restauran aquí (y viceversa: `SavePreservingPins` reescribe solo las filas
  del pad sin borrar las del editor).
- **Gutter con hover (como el legacy)**: la línea bajo el cursor se resalta
  (banda completa + refuerzo en la franja de breakpoints), la franja de
  iconos muestra cursor de mano y tooltip "Line N — click to toggle
  breakpoint"; al salir del gutter se limpia todo.
- **Persistencia de la sesión de debug**: breakpoints, watches (pad + pins
  del editor) y config activa viven en `<sln>.userprefs` — los breakpoints
  con el formato Mono.Debugging (`MonoDevelop.Debugger/WatchService.cs`
  comparte la clave legacy `MonoDevelop.Ide.DebuggingService.PinnedWatches`:
  los watches del pad viajan solo con `expression` y los pins del editor con
  la ubicación completa file/line/column del PinnedWatchStore legacy
  (`SavePinned`/`LoadPinned` mezclan ambos tipos de fila). Se restauran al
  reabrir la solución (los breakpoints y los pins al reabrir cada documento,
  como el legacy).

## Estado del bucle de migración

Cada módulo se porta desde el código GTK listando primero sus funcionalidades,
se implementa en Avalonia y pasa QA de paridad antes de avanzar al siguiente.
Completados recientes: Watch editable in-place + reevaluación por step,
pinned watches como burbujas y gutter con hover (M16e), integración
estructural Services/Controls → MonoDevelop.Ide y servicios de debug →
addin MonoDevelop.Debugger. Siguientes candidatos:
TipOfTheDay, SelectEncodingsDialog, NewConfigurationDialog/NewLayoutDialog,
ProgressDialog, AttachToProcessDialog (Debugger) y semántica Roslyn real para
completion/tooltip.

Detalle hito por hito: `docs/interfaz-plan.md` § M4–M16f.
