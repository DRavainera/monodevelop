# Bloque: MonoDevelop.Ide.csproj compila a EXIT=0 vía MonoRoslynCompat (redact)

Fecha: 2026-09-06
Estado: COMPLETADO (verificado con `idebuild10.log`, EXIT=0)

## Objetivo
- Llevar `src/core/MonoDevelop.Ide/MonoDevelop.Ide.csproj` (config `DebugLinux`) a EXIT=0
  derivando la solucion de compatibilidad de Roslyn en `MonoRoslynCompat`.
- Prohibicion: no usar `/usr/lib/mono/*` como HintPath/referencias.

## Hallazgo clave (mecanismo de la colision CS0433 resuelto)
- El csc .NET SDK resuelve tipos **privados/NotPublic** de `Microsoft.CodeAnalysis.Workspaces 3.4.0-beta4-final`
  sin CS0122 (los ve accesibles). Por tanto, la regla correcta es:
  **NO duplicar en compat ningún nombre de tipo que tambien exista en Workspaces**, aunque sea privado.
- Los CS0433/CS0104 de `idebuild8.log` (41 errores) provinieron de stubs en compat que duplicaban
  tipos de Workspaces (p. ej. `Extensions.IErrorReportingService`, `Options.IOptionService`,
  `Editor.InfoBarUI`, `Host.*`, `Shared.TestHooks.FeatureAttribute`, etc.).
- Compat, en cambio, NO puede *usar* los tipos privados de Workspaces como base/interfaz/parametro/retorno
  (ahi si compila CS0122: `ProjectCacheService`, `EditorTaskSchedulerFactory`).
- WS real define los nombres verificados: `IWorkspaceService` (public), `FeatureAttribute` (private,
  con `InfoBar="InfoBar"`), `OptionKey` (ctor `(IOption,string)`, sin operator implicito), y NO define
  `ProjectCacheService`, `Navigation.IDocumentNavigationService`, `EditorLayerExtensionManager.ExtensionManager`,
  `ILineSeparatorService` (seguros de stubrear).

## Cambios en compat (`Main.sln`/`src/compat/MonoRoslynCompat`)
- Redact de stubs que duplicaban nombres de Workspaces (Eliminados): `Extensions.*`, `ErrorLogger`, `Host.*`,
  `Options.IOptionService`, `Shared.TestHooks` (manteniendo FeatureAttribute momentaneamente) etc.
- `ProjectCacheService` : implementa `Microsoft.CodeAnalysis.Host.IWorkspaceService` (WS public);
  se quito la herencia de `IProjectCacheHostService` (privada en WS) y `CacheObjectIfCachingEnabledForKey<T>`.
- `Navigation.IDocumentNavigationService` : `: IWorkspaceService`.
- `EditorLayerExtensionManager.ExtensionManager` : `: IWorkspaceService`; se elimino ctor 4-args y la interfaz `IExtensionManager`.
- `EditorTaskSchedulerFactory` : clase base simple (sin `IWorkspaceTaskSchedulerFactory`),
  sin ctor `IAsynchronousOperationListenerProvider`, sin metodos virtuales con retorno privado de WS.
- `TextSpanExtensions.ToTextSpan(this Microsoft.VisualStudio.Text.Span)` (nuevo overload).
- `Mono.Unix.FileAccessPermissions`, `Mono.Unix.UnixFileSystemInfo`, `System.Net.Sockets.UnixEndPoint` (stubs).
- Eliminado el stub `Shared.TestHooks.FeatureAttribute` (resuelve desde WS real: `FeatureAttribute.InfoBar`).

## Cambios en el Ide (call sites rotos por el redact)
- `MonoDevelopExtensionManager.cs`: `base()` en vez del ctor 4-args de la ExtensionManager editor.
- `MonoDevelopTaskSchedulerFactory.cs`: `base()` en vez de `base(listenerProvider)`.
- `MonoDevelopDocumentNavigationService.cs:215`: `GetOption(new OptionKey(NavigationOptions.PreferProvisionalTab)) is bool ...`
  (WS 3.4 no tiene conversion implicita PerLanguageOption -> OptionKey).
- `PlatformService.cs`: restaurado `using Mono.Unix;` (tipos resueltos desde los stubs de compat).
- `LineSeparatorTextEditorExtension.cs`: `using Microsoft.CodeAnalysis.Editor.Shared.Extensions;`
  (namespace real del stub `ILineSeparatorService`).
- `WorkspaceTaskQueue.cs`, `DiagnosticsCompat.cs`: integrados (companero de la superficie compat en el contribuyente).

## Verificacion
- Compat `DebugLinux`: EXIT=0.
- Loop Ide (`-p:BuildProjectReferences=false`): `idebuild8.log` 41 errores -> `idebuild9.log` 13 errores -> `idebuild10.log` **0 errores, EXIT=0**.

## Pendiente
- Bloque `CSharpBinding.csproj` a 0 errores: COMPLETADO (ver `docs/block-csharbinding-exit0.md`).
- Build completo via `Main.sln` (config `DebugLinux`) hasta EXIT=0.
- Documentar y comitear bloques subsiguientes.
