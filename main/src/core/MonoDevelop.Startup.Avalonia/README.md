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
# Build (el generador XAML de Avalonia 12 requiere Roslyn ≥ 4.14 → SDK 10;
# el global.json de ESTE directorio lo pinea — la raíz del repo sigue en 8.0.424)
cd main/src/core/MonoDevelop.Startup.Avalonia
~/.dotnet/dotnet build -v q

# Run (binario real del shell Avalonia)
~/.dotnet/dotnet src/core/MonoDevelop.Startup.Avalonia/bin/Debug/net10.0/MonoDevelop.AvaloniaShell.dll
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
├── Controls/                   # Controles propios
│   ├── SkTextEditor.cs         # Editor de texto SkiaSharp (reemplazo del Mono.TextEditor/Cairo)
│   ├── PadHost.cs              # Host de pads dockable (pestañas + botón hide, como los DockItem legacy)
│   ├── CompletionPopup.cs      # Intellisense (port de CompletionListWindowGtk)
│   └── EditorTooltipPopup.cs   # Hover tooltip (port del pipeline TooltipProvider)
└── Services/                   # Servicios sin UI
    ├── IconService.cs          # PNGs de MonoDevelop.Ide/icons (mapeo StockIcons.addin.xml, variantes ~dark/~disabled/@2x)
    ├── SolutionLoader.cs       # Carga real de .sln/.csproj al árbol del Solution pad
    ├── UserPreferences.cs      # Persistencia MonoDevelop-properties.xml + RecentSolutions
    ├── SettingsStore.cs        # Atajos (Custom.kb.xml) y herramientas externas (MonoDevelop-tools.xml)
    ├── KeyboardShortcutRegistry.cs # Registro de atajos y dispatch de teclado
    ├── NavigationHistoryService.cs # Back/Forward/Zoom de navegación (NavigationCommands legacy)
    ├── TaskScanner.cs          # Escáner de TODO/HACK/… para el pad Tasks
    ├── ExternalToolRunner.cs   # Ejecución de herramientas externas (ToolCommands legacy)
    └── GettextService.cs       # i18n: UserInterfaceLanguage + traducción del menú
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
~/.dotnet/dotnet src/core/MonoDevelop.Startup.Avalonia/bin/Debug/net10.0/MonoDevelop.AvaloniaShell.dll \
  --sln=/ruta/TestProj.sln --<hook> 2>&1 | grep -E "\[<tag>\]"
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

QA visual: los hooks se complementan con capturas X11 (`magick x:<win>`) para
comparar la UI contra la legacy GTK en vivo (ver bitácoras en
`docs/interfaz-plan.md`).

## Estado del bucle de migración

Cada módulo se porta desde el código GTK listando primero sus funcionalidades,
se implementa en Avalonia y pasa QA de paridad antes de avanzar al siguiente.
Completados recientes: Properties pad real (M11w), DirtyFilesDialog (M11x),
editor estable + intellisense + hover tooltip (M11y). Siguientes candidatos:
TipOfTheDay, SelectEncodingsDialog, NewConfigurationDialog/NewLayoutDialog,
ProgressDialog, AttachToProcessDialog (Debugger) y semántica Roslyn real para
completion/tooltip.

Detalle hito por hito: `docs/interfaz-plan.md` § M4–M11y.
