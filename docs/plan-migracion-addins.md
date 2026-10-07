# Plan de ejecución final: migración completa de add-ins

Estado (2026-10-05): las fases 0 a 3 de este plan están ejecutadas y validadas por QA. El shell Avalonia convive con la UI GTK legacy (`--old-gui`). No se elimina soporte GTK. Los add-ins Avalonia viven en carpetas `MonoDevelop.Avalonia.*`; las carpetas legacy no se tocan.

## Objetivo

Alinear el sistema de add-ins de la shell Avalonia con el modelo de extensiones de Visual Studio, migrar Preferences, pads/comandos y hooks de plataforma, y dejar la ruta GTK (`--old-gui`) compilable y funcional.

## Restricciones técnicas

- Simetría: no se retira GTK. El shell Avalonia coexiste con la UI GTK legacy.
- Ediciones quirúrgicas: no se hace un refactor masivo de `MonoDevelop.Ide`; se envuelve la lógica existente en add-ins compatibles con Avalonia.
- Anidación limitada: por el tope de `childId` en `AddonExtensionNode`, los manifiestos usan rutas de extensión aplanadas (p. ej. `/TextEditor/Analysis/C#`).
- Carpetas: los add-ins Avalonia van en `main/src/addins/MonoDevelop.Avalonia.*`. El código GTK y las carpetas de add-ins legacy no se tocan.

## Fase 0: alineación con el modelo de Visual Studio (infraestructura)

El `AddonHost` trataba el manifiesto como un archivo de configuración simple. Se alinea con el modelo de extensiones de Visual Studio (`extension.vsixmanifest`).

### AddonManifest.cs

- Assets: `AddonAsset` (`Type`, `Src`, `Assembly`, `ClassName`). El contribuidor declara qué aporta (p. ej. un `MefComponent`) sin lógica hardcodeada del shell.
- Identity: `AddonIdentity` ampliado con `DisplayName`, `Preview` y `Language`.
- Dependencies: `AddonDependency` con rangos de versión (p. ej. `[9.0, 10.0)`) en lugar de igualdad.

### AddonHost.cs

- Descubrimiento de ensamblados: `EntryPoint` pasa de ruta de archivo a nombre de ensamblado. `AssemblyDependencyResolver` localiza las DLL en la carpeta del add-in.
- Validación de dependencias: comprobación previa a la carga. Si un rango no lo satisface un add-in cargado, el add-in queda `Loaded = false` con error concreto.
- Registro: se respeta `autoLoad: false`.

### Estado

Hecho. `AddonManifest` y `AddonHost` compilados. Añadidos `AddonLoadContext` y `AddonLoadState`. Corregidos errores de sintaxis en el host. `IsVersionSatisfied` acepta rangos tipo `[9.0, 10.0)`.

## Fase 1: completitud de Preferences (oleada 1 — alta)

Migrar los paneles de opciones restantes creando manifiestos y actualizando el árbol en `PreferencesDialog.axaml.cs`.

### Add-ins a migrar

- NuGet (General, Sources)
- Debugger (Debugger)
- CSharpBinding (OnTheFly Formatting, Code Style)
- ChangeLogAddIn (ChangeLog Integration)
- DocFood (Feedback)

### Actualizaciones del árbol

- Extender `MergePoints` con las rutas nuevas.
- Mantener el `LegacySectionMap` acotado por punto para evitar colisiones (p. ej. "General" en Text Editor frente a Version Control).
- Corregir el `ParentId` de .NET Core para que quede dentro de SDK Locations, no en la raíz de Projects.

### Estado

Hecho. Manifiestos Avalonia de esos add-ins en `MonoDevelop.Avalonia.*`. Árbol de Preferences con `MergePoints` y mapa por punto.

## Fase 2: migración funcional (oleada 2 — media)

Migrar add-ins que aportan comportamiento, comandos y vistas.

### Pads y menús

Migrar AssemblyBrowser, UnitTesting, HexEditor, RegexToolkit y DesignerSupport.

### Integración

Usan `IAvaloniaAddon` e `IPackage` para registrarse en el `MenuService` y el `DockingService` del shell.

### Estado

Hecho. Add-ins Avalonia correspondientes en `main/src/addins/MonoDevelop.Avalonia.*`.

## Fase 3: hooks de plataforma y limpieza (oleada 3 — baja)

- LinuxPlatform: migrar el soporte de plataforma (antes GnomePlatform).
- Hooks multiplataforma: estandarizar hooks para MacPlatform y WindowsPlatform.
- Verificación: la ruta GTK legacy (`--old-gui`) sigue compilable y funcional.

### Estado

Hecho. Creados:

- `main/src/addins/MonoDevelop.Avalonia.LinuxPlatform/`
- `main/src/addins/MonoDevelop.Avalonia.WindowsPlatform/`
- `main/src/addins/MonoDevelop.Avalonia.MacPlatform/`

`GnomePlatform` y el resto de add-ins GTK intactos. Manifiestos `.avaloniaaddon.json` solo en carpetas `MonoDevelop.Avalonia.*`. QA de la fase 3: aprobado.

## Cómo validar

- Compilar `MonoDevelop.Avalonia.Addons` (0 errores).
- Comprobar que no hay `.avaloniaaddon.json` en carpetas legacy.
- Arrancar el shell Avalonia por defecto y GTK con `--old-gui`.
- QA Senior (AGENTS.md §18.5) sobre cada fase; ciclo hasta 0 hallazgos.

## Pendiente

- Fase 4 del plan de migración de runtime/build (pipeline SDK; no forma parte de este blueprint de add-ins).
- Completar MIME/iconos en los hooks de plataforma más allá del esqueleto.

## Fase 4: oleadas de add-ins legacy → Avalonia

Nota: esta sección redefine la línea de "Pendiente" anterior sobre la "Fase 4 del plan de migración de runtime/build (pipeline SDK)": la Fase 4 de este blueprint de add-ins son las oleadas A–E de migración de add-ins legacy → Avalonia.

Migrar los add-ins legacy Gtk restantes (alcance aprobado: Núcleo + Dominio, 13 add-ins) a contrapartes Avalonia compatibles con el modelo de extensiones de Visual Studio. Patrón por add-in: carpeta `main/src/addins/MonoDevelop.Avalonia.<X>/` + manifiesto `MonoDevelop.<X>.avaloniaaddon.json` + stub `.cs` (los `.cs` no se compilan en la shell; el manifiesto es la superficie de conexión de la extensión). Las carpetas legacy no se tocan.

### Oleadas

| Oleada | Add-ins legacy | Estado |
|--------|----------------|--------|
| A | ILAsmBinding, TextTemplating, PerformanceDiagnostics, ConnectedServices | Completa (2026-10-06) |
| B | Deployment (sub-add-ins AspNet/AspNetCore) | Completa (2026-10-06) |
| C | PackageManagement (sub-add-ins UnitTesting.NUnit + Runners) | Completa (2026-10-06) |
| D | VBNetBinding | Completa (2026-10-06) |
| E | MonoDevelop.TextEditor + Packaging | Completa (2026-10-06) |

Diferidos (fuera de las oleadas): backends debugger (Gdb, Soft, VSCodeDebugProtocol, Win32), `MonoDevelop.GtkCore`, y MIME/iconos en los hooks de plataforma más allá del esqueleto.

### Oleada A — informe (2026-10-06)

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

### Oleada B — informe (2026-10-06)

**Add-ins creados** (manifiesto + stub; nodos legacy traducidos a extension points aplanados):

- `main/src/addins/MonoDevelop.Avalonia.Deployment/` — ID `MonoDevelop.Deployment`: `Commands` (CreatePackage/AddPackage/Install), `MainMenu/Project`, `ContextMenu/ProjectPad/Tools`, `ProjectTemplates` (PackagingProject), `Pads/ProjectPad` (2 NodeBuilders), `PackageBuilders`, `DeployFileCopiers`, `DeployServiceExtensions`, `PackageBuilderEditors`, `FileCopyConfigurationEditors`, `ContextMenu/ProjectPad/Package`, `ContextMenu/ProjectPad/PackagingProject`, `DeployDirectories` (6), `DeployPlatforms` (2), `ItemOptionPanels`, `SerializableClasses` (6), `StockIcons`/`TemplateImages` (legacy `main/src/addins/Deployment/MonoDevelop.Deployment/`).
- `main/src/addins/MonoDevelop.Avalonia.AspNet/` — ID `MonoDevelop.AspNet`: `ProjectTemplates` (3), `FileTemplates` (34), `FileTemplateTypes`, `FileFilters` (2), `LegacyEditorSupport`, `ProjectModelExtensions` (5 flavors con GUID), `TypeSystem/Parser` (3), `SerializableClasses`, `ItemOptionPanels/Run` (XSP), `ToolboxLoaders`/`ToolboxProviders`, `Commands` (6), `ContextMenu/ProjectPad/Add`, `Pads/ProjectPad` (2), `MimeTypes` (16), `TextEditorExtensions` (2), `Html/DocTypes` (4), `ExecutionHandlers` (XSP), `CompletionCharacters`, `SourceEditor2/ContextMenu/Editor`, `CodeTemplates`, `CodeFormatters`, `CustomTools`, `FileTemplateConditionTypes`, `ProjectTemplateWizards`, `TemplateImages`/`StockIcons` (legacy `main/src/addins/AspNet/`).
- `main/src/addins/MonoDevelop.Avalonia.AspNetCore/` — ID `MonoDevelop.AspNetCore` (deps: `MonoDevelop.Ide` + `MonoDevelop.DotNetCore`): `FileTemplates` (13, con `file`), `ProjectTemplateCategories/netcore/app`, `Templates` (13, con condición SDK), `ExecutionHandlers`, `RunConfigurationEditors`, `FileTemplateConditionTypes`, `Pads/ProjectPad` (ScaffoldNodeExtension), `ContextMenu/ProjectPad`, `Commands/Project` (3), `ContextMenu/ProjectPad/Publish`, `MainMenu/Build`, `ProjectTemplateWizards` (legacy `main/src/addins/MonoDevelop.AspNetCore/`).

**Decisiones documentadas (hallazgos BAJO de QA aceptados como decisiones del plan):**

- AspNetCore: el legacy define las plantillas en 6 bloques por SDK (3.1/3.0/2.2/2.1/2.0/1.x, ~57 nodos); el manifiesto aplanado conserva las 13 del SDK más reciente (3.1) con condición `AspNetCoreSdkInstalled, UseNetCore31=true` y descarta los bindings de variantes de SDKs legacy (`templateId`/`path` al nupkg). La shell moderna (.NET 10) no consume esas variantes.
- EP `/MonoDevelop/Deployment/DeployDirectoryResolvers` (legacy) sin declarar: en el host los EP se auto-declaran al registrar el primer nodo (`AddonHost.cs`); el sub-add-in pendiente `MonoDevelop.Deployment.Linux` lo extiende y se migrará con él.
- EP `/MonoDevelop/Asp/CompletionBuilders` (consumidor: CSharpBinding legacy) sin declarar hasta migrar su consumidor; `commitChars` de `CompletionCharacters` y detalles `isText`/`baseType` de MIME descartados en el aplanado (se conservan `commitOnSpace`/`pattern`).

**QA (§18.5, 2 rondas):** ronda 1 **NO APTO** (2 MEDIO: 3 FileTemplates faltantes en AspNetCore → añadidos con su atributo `file`, y los `file` faltantes de los 10 existentes; 4 PNG de `docs/img/` borrados accidentalmente del working tree → restaurados con `git checkout -- docs/img/...`, mismo remedio que el precedente de `docs/session_summary.md:696-698`) + 3 BAJO aceptados como decisiones del plan; ronda 2 **APTO**: build 0 errores; smoke `discovered=28` / `loaded 25/28` (los 3 nuevos `loaded=True`, `FileTemplates nodes=49`, `MonoDevelop.CSharpBinding` una sola vez, solo los 3 fallos esperados, 0 FATAL); 28 manifiestos con 0 IDs duplicados; 0 manifiestos en carpetas legacy; 0 cambios en archivos legacy (ruta GTK intacta).

### Migración GnomePlatform→LinuxPlatform (completa, 2026-10-06)

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
