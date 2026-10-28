# Handoff — T7: Gtk# 2.12 → GtkSharp 3.24.24.95 (cierre del pipeline Gtk legacy `--old-gui`)

Fecha: 2026-10-28 · Rama: `agents/sub-agent-senior-dev-role-report` · Operador: único
consumidor (entorno privado). Documento legible sin contexto de sesión.

## Contexto
La migración .NET 10 de MonoDevelop tiene dos interfaces sobre el mismo backend:
- **Avalonia** (camino por defecto): `main/build/MonoDevelop.AvaloniaShell.dll`.
- **Gtk legacy**: `main/build/MonoDevelop.AvaloniaShell.dll --old-gui` (arranca
  `MonoDevelop.Startup.MonoDevelopMain.Main` → `MonoDevelop.Ide.IdeStartup` sobre GTK).

T7 = migrar la interfaz Gtk legacy de **Gtk# 2.12** (`gtk-sharp.dll`, glue
`libgtksharpglue-2.so`, GTK2) a **GtkSharp 3.24.24.95** (nuget; `GtkSharp.dll`/
`GLibSharp.dll`/`GdkSharp.dll`/`AtkSharp.dll`/`PangoSharp.dll`, GTK3). Los submódulos
(xwt, mono-addins, fsharpbinding, debugger-libs, PropertyEditing) ya estaban en GtkSharp 3
en cierres previos; este handoff cubre el cierre del núcleo y la estabilización de
arranque.

## Qué se hizo (esta sesión, sobre el changeset T7 acumulado de 536 ficheros)
1. **Fix AtkCocoaHelper** — `main/src/core/MonoDevelop.Ide/MonoDevelop.Components.AtkCocoaHelper/AtkCocoaHelper.cs`:
   `ActionDelegate` ahora usa `Attach(action, name, handler)` que consulta
   `g_signal_lookup(name, owner.NativeType.Val) != 0` (P/Invoke a `libgobject-2.0.so.0`;
   `libgobject-2.0.0.dylib` bajo `#if MAC`) antes de `AddSignalHandler`. Por qué: las
   señales Cocoa (`request-actions`, `perform-*`) solo existen en macOS; en Linux el
   `g_signal_connect` emitía `Gtk-Critical: g_signal_connect … signal not found` ×125-128
   por arranque (una por widget). `GLib.Signal.GetSignalId` es `private static` en
   GtkSharp 3 (no accesible) → de ahí el P/Invoke. Verificado: 128→0.
2. **Fix `SetData<T>`** — `MonoDevelop.Components/GtkWorkarounds.cs` y copia del submódulo
   `main/external/xwt/Xwt.Gtk/Xwt.GtkBackend/GtkWorkarounds.cs`: el genérico
   `SetData<T> where T: struct` hacía P/Invoke a `gtk_object_set_data` (función GTK2; en
   la era Gtk# 2.12 el bloque `pdata` se "fugaba" a propósito para que los engines de
   temas C pudieran leer el hint) y además `Marshal.FreeHGlobal(pdata)` (dangling). En
   GtkSharp 3 `GLib.Object.Data[key]` mapea a `g_object_set_data` (libgobject, GTK3) y no
   existe reader C de `"transparent-bg-hint"` en GTK3 (los engines GTK2 desaparecieron);
   el otro consumidor (`AtkCocoaHelperMac.SetNSAccessibilityElement`) usa el overload
   `SetData(GLib.Object, string, IntPtr)` → `g_object_set_data` directo, sin tocar.
   `SetData<T>` quedó como `gtkobject.Data [key] = data;`. Verificado: 40→0.
3. **Fix del resolver de native libs** — `main/src/core/MonoDevelop.Startup/MonoDevelop.Startup/MonoDevelopMain.cs`
   (`ResolveNativeLibrary`): `libgtk-win32-2.0-0.dll` → `libgtk-3` (antes
   `libgtk-x11-2.0`) y `libgdk-win32-2.0-0.dll` → `libgdk-3` (antes `libgdk-x11-2.0`).
   Por qué: los P/Invokes residuales (PangoUtil, GtkWorkarounds, `TextArea`
   `im_multicontext`) cargaban GTK2 **junto** a GTK3; ambas librerías registran
   `GtkWidget`/`GdkWindow` en el type system de GLib del proceso → el registro perdedor
   falla (`cannot register existing type 'GtkWidget'`) y los macros
   `GTK_IS_WIDGET`/`GDK_IS_WINDOW` de una lib fallan sobre widgets de la otra → criticals
   de ciclo de vida, cierres prematuros y document-open flaky (1-2/3 con muerte puntual).
   Verificación por símbolos (`nm -D`): `libgtk-3`/`libgdk-3` exportan todos los símbolos
   que se P/Invocan (incl. `gtk_im_multicontext_*`, `gdk_event_get_scroll_deltas`,
   `gdk_screen_get_monitor_scale_factor`, `gtk_widget_get_scale_factor`); los únicos
   ausentes en ambas (`gtk_icon_source_set_scale`, `gtk_icon_set_render_icon_scaled`,
   `gtk_scrolled_window_set_overlay_policy` desde `LIBQUARTZ`) ya tenían fallback
   `DllNotFoundException`/`EntryPointNotFoundException`. Verificado: doble registración
   3→0, document-open 3/3.
4. **Staging híbrido del layout plano `main/build/`** (procedimiento, no código): el
   build limpio T7 genera `build/AddIns/<name>/` **solo con el ensamblado propio** de
   cada addin (los refs NuGet llevan `<Private>False</Private>` + workaround
   `FilterLocalCopyReferences` que además quita lo demás), mientras el set completo de
   dependencias (p.ej. `Microsoft.Web.XmlTransform.dll`, `NuGet.*.dll`) vive en los
   directorios de nivel superior (`build/MonoDevelop.PackageManagement/`,
   `build/VersionControl/`, …) del staging anterior. Proceso reproducible:
   ```
   rm -rf main/build
   dotnet build main/Main.sln -c DebugLinux
   cd main/build
   tar --skip-old-files -xzf ~/opencode/staging_backup_oct26.tgz
   tar --skip-old-files -xzf ~/opencode/staging_backup_addins_oct26.tgz
   for d in AddIns/*/; do n=$(basename "$d"); [ -d "$n" ] && cp -rn "$n/." "$d/"; done
   ```
   Sin el paso de gap-fill: `FileNotFoundException` de
   `AddIns/MonoDevelop.PackageManagement/Microsoft.Web.XmlTransform.dll` al cargar
   PackageManagement → DotNetCore/AspNetCore/Packaging no cargan → unhandled → cierre.
   Nota: el loader de addins prefiere `build/AddIns/<name>/` sobre el directorio de nivel
   superior cuando ambos existen.

## Ficheros tocados (esta sesión)
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Components.AtkCocoaHelper/AtkCocoaHelper.cs`
- `main/src/core/MonoDevelop.Ide/MonoDevelop.Components/GtkWorkarounds.cs`
- `main/src/core/MonoDevelop.Startup/MonoDevelop.Startup/MonoDevelopMain.cs`
- `main/external/xwt/Xwt.Gtk/Xwt.GtkBackend/GtkWorkarounds.cs` (submódulo)
- `docs/interfaz-plan.md`, `docs/session_summary.md`, este handoff
- El changeset T7 completo (536 ficheros, la migración del núcleo a GtkSharp 3) viaja en
  el mismo commit. Submódulos sucios al cierre: xwt (24 fich) y mono-addins (16 fich),
  ambos rama `net10`, remotes `github.com/DRavainera/{xwt,mono-addins}`. `fsharpbinding`
  **no** es submódulo en este repo (sus 3 ficheros son parte del repo principal).

## Cómo validar
- **Gate**: `dotnet build main/Main.sln -c DebugLinux` → "0 Errores" (SDK en español;
  ~3-6 min; warnings ~4000 son normales, sobre todo NU1701/CS8032).
- **Smoke Gtk** (el criterio de T7): `bash ~/opencode/qa_t7_smoke3.sh` (3× con cleanup
  entre runs) o `bash ~/opencode/qa_t4_gtkdoc.sh` (1×). En cada run: ventana
  `QaB4 – Program.cs – MonoDevelop` (wmctrl), proceso vivo a 75 s, y en el IDE log
  (`~/.cache/MonoDevelop/9.0/Logs/Ide.<timestamp>.log`):
  `grep -c g_signal`=0 · `grep -c gtk_object_set_data`=0 ·
  `grep -c 'cannot register existing type'`=0 ·
  `grep -c 'not assignable'`=0 · `grep -cE 'FATAL|Unhandled exception'`=0.
  Fixture: `/home/daniel/opencode/qa_in3/proj/QaB4.sln`. Entorno del smoke: `env -i`
  con `DISPLAY=:0`, `XAUTHORITY=$(ls /run/user/1000/.mutter-Xwaylandauth.* | head -1)`,
  `FONTCONFIG_FILE=/home/daniel/opencode/fontconfig/c_only.conf`,
  `DOTNET_ROOT=/home/daniel/.dotnet`.
- **Smoke Avalonia**: `bash ~/opencode/qa_t6_avalonia.sh` → ventana `MonoDevelop —
  Avalonia Shell`, proc vivo, fatals=0.
- **Matar procesos IDE**: NUNCA `pkill -f MonoDevelop` (mata la propia shell del agente).
  `ps aux | grep -E "MonoDevelop\.AvaloniaShell|build/MonoDevelop" | grep -v grep |
  awk '{print $2}' | xargs -r kill`.

## Estado de validación (QA wave T7, `~/opencode/qa_t7/`)
- Ronda 1 (build 01:27, antes de los fixes 2-4): Q1 gate OK; Q2 **FALLO** (IDE muere ~4 s
  tras el arranque, sin FATAL; causa: AddIns incompletos + doble mundo GTK).
- Ronda 2 (tras los 4 cambios): **PASS** — Q1 0 Errores (4327 warns); Q2 3/3 (ventana+doc,
  g_signal=0, set_data=0, cannot-register=0, mef=0, fatals=0); Q3 Avalonia OK.
  Informes acumulados en `~/opencode/qa_t7/informe.md` (fuera del repo por §18.6).

## Qué queda pendiente
1. **Known-issue contenedor-leak (no bloqueo)**: persisten ~26-37 `Critical Gtk/Gdk`
   (`GDK_IS_WINDOW` en `gtk_widget_register_window`/`gdk_window_get_scale_factor`/
   `gdk_window_show_internal`, `WIDGET_REALIZED_FOR_EVENT` ×1) +
   `Gdk-WARNING: losing last reference to undestroyed window` ×7-10 durante
   `Creating DefaultWorkbench`, y 1 NRE capturado (`Terminating MonoDevelop? False`) en
   `Mono.TextEditor.TextArea.OnMapped()` al mapear el editor. Es la familia del
   contenedor-leak **BXC #3801**: el workaround original usaba glue de Gtk# 2.12
   (`gtksharp_container_leak_fixed_marker` / `gtksharp_container_override_forall` en
   `libgtksharpglue-2.so`, que tiene `NEEDED libgtk-x11-2.0.so.0` → reactivarlo recarga
   GTK2 y revierte el fix 3) y `MonoDevelop.Components.GtkWorkarounds.FixContainerLeak`
   quedó como **stub vacío** en T7 (se llama desde `TextArea`, `MonoTextEditor`,
   Theatrics, VersionControl compare). La equivalente de GtkSharp 3 no existe (el modelo
   de wrapping/finalización de `GLib.Object` cambió). La app sobrevive y el documento se
   abre; el fix exige diagnóstico dedicado (trace del dispose del workbench bajo
   GtkSharp 3) o portar el override de `Forall` vía vfuncs de GLib.
2. **Riesgo latente MEF — identidad dual `IThreadingContext`**: `MonoRoslynCompat.dll`
   (stub, `main/src/compat/MonoRoslynCompat/src/CompatStubs.cs` L653) y
   `Microsoft.CodeAnalysis.EditorFeatures.dll` (submódulo vs-editor-api) **definen** la
   misma interfaz `Microsoft.CodeAnalysis.Editor.Shared.Utilities.IThreadingContext`;
   `MonoDevelopThreadingContext` exporta el stub y los imports de Roslyn esperan el real →
   "MEF composition error … is not assignable" (29 errores en algunos arranques, 0 en
   otros; la versión de `Microsoft.VisualStudio.Threading.dll` que se resuelve primero
   varía — coexisten 15.8.192 en top, 16.10.56 en addins, 17.7.0.0 referenciado por
   EditorFeatures). Tras el fix 3 no apareció en la ronda 2; el fix completo (unificar
   superficie Roslyn/Threading a una sola versión — bump de
   `main/external/vs-editor-api/build/PackageVersions.targets`) es arquitectónico y de
   alto riesgo → **decisión del operador**.
3. **Fragilidad del staging híbrido**: cualquier `dotnet build Main.sln` futuro regenera
   los `AddIns/<name>/` incompletos; hay que re-hacer el gap-fill (sección "Staging
   híbrido"). La cura definitiva es arreglar el copy-local de las dependencias en los
   csprojs de addins (`<Private>False</Private>` → copy-local real a `AddIns/<name>/`).
4. **T8**: reimplementar funcionalidades quitadas (auditoría T3, 68 items).
5. **T9**: higiene de build (outputs de `vs-editor-api` desde cero; carrera
   `runtimeconfig.json` entre TFMs sobre `main/build/`).

## Convenciones recordatorio (AGENTS.md §18)
- Cierre de tarea: documentar → commitear (main + submódulos, ficheros por nombre, sin
  secretos ni artefactos de build) → pushear (main + submódulos a su rama).
- Submódulos y addins son parte del proyecto: se corrigen y commitean dentro de ellos; el
  repo principal lleva la actualización de puntero.
- Sin carpeta "Avalonia" separada; compilaciones integradas; ambas interfaces deben
  seguir compilando y funcionando.
- Artefactos de agentes en `~/opencode/` (nunca `/tmp/`).
- Los agentes no crean PRs (el operador, manualmente).
