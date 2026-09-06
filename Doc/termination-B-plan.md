# Bloque B — Referencias de framework .NET legacy → NuGet/BCL (System.Web, System.ServiceModel, System.Configuration, Microsoft.CSharp)

## Objetivo
En el **core** (`MonoDevelop.Core`), reemplazar las referencias compilables a asambleas de framework legacy/custom por lo que ofrece el BCL de .NET 8 (`ref/net8.0`), de modo que el assembly no dependa de asambleas fuera del refpack dispone ni de tipos que ya no existen en .NET.

- Quedan **diferidos a "Interfaz"**: `MonoDevelop.DesignerSupport` (RemoteDesignerProcess), addin `WebReferences` y los demás csproj fuera del core (Ide, AspNet, MacPlatform, RegexToolkit, PackageManagement, DotNetCore, UnitTesting, CSharpBinding) — estas refs se limpian bloque por bloque junto a sus assemblies de destino.

## Criterio de terminado (núcleo del Bloque B, dentro de MonoDevelop.Core)
- Cero referencias `<Reference Include="System.Web|System.ServiceModel|System.Configuration|Microsoft.CSharp" />` en `MonoDevelop.Core.csproj`.
- Cero uso compilable de `HttpUtility`, `System.ServiceModel`, `ConfigurationManager` o `dynamic`/`Microsoft.CSharp` en el código del core.
- El core compila con Roslyn SDK contra net8.0 (validación dirigida, no `dotnet build` de proyecto — bloqueos de infraestructura preexistentes).

## Nota sobre `ref/net8.0`
El refpack de .NET 8 **incluye** `System.Web.dll`, `System.Configuration.dll` y `Microsoft.CSharp.dll` (stubs de compatibilidad) pero **NO incluye `System.ServiceModel`**. Por eso:
- `System.Web`/`System.Configuration`/`Microsoft.CSharp` en el core eran refs redundantes con el refpack → eliminables si el código no requiere ensamblado separado.
- `System.ServiceModel` **no existe** en .NET 8 → la dependencia se debe eliminar del código (no basta con quitar la ref).

## Decisiones por subbloque

### B2 — `System.ServiceModel` (código real en Core: STS/WIF, auth de feeds legacy)
El flujo STS/WIF (`MonoDevelop.Core.Web/`) autenticaba feeds NuGet/paquetería contra TFS/Azure DevOps vía WSTrust + Windows Identity Foundation. En .NET 8 los tipos WIF (`WSTrustChannelFactory`, `WS2007HttpBinding`, `SecurityMode`, `TrustVersion`) no existen. **Decisión del usuario: quitar STS/WIF legacy del Core.**

- `WIFTypeProvider.cs`: el único uso compilable de `System.ServiceModel` era `typeof(System.ServiceModel.EndpointAddress)` (línea 83). Se reemplaza por búsqueda `Type.GetType(QualifyTypeName(...))` (consistente con el resto del archivo). En .NET 8 la asamblea no está → `GetWIFTypes()` devuelve `null` graceful (comportamiento idéntico al actual).
- `STSAuthHelper.cs`: el cuerpo `GetSTSToken` construía `WS2007HttpBinding(SecurityMode.Transport)` y usaba `dynamic` + `TrustVersion`. Ese código era **inalcanzable** en .NET 8 (porque `GetWIFTypes() == null` lanza antes). Se elimina el cuerpo WCF y los helpers ahora muertos `SetProperty`/`GetFieldValue`, y se deja un `throw new NotSupportedException` (funcionalmente equivalente en .NET). Se eliminan los `using System.ServiceModel*`.
- Se elimina `<Reference Include="System.ServiceModel" />` del csproj.
- `RequestHelper.cs` (llamante) **no cambia**: `PrepareSTSRequest`/`TryRetrieveSTSToken` siguen compilando; en .NET el flujo STS simplemente no autentica (ya era el caso).

### B3 — `System.Configuration`
- `PropertyService.cs:65` leía `ConfigurationManager.AppSettings["DataDirectory"]` y caía a un fallback. En .NET 8 ese path legacy estaría vacío → se elimina la lectura y se usa el fallback `Path.Combine(EntryAssemblyPath, "..", "data")`. `ProjectReference.cs:261` usa `"System.Configuration"` solo como **string identificador de asamblea**, no como API → sin cambio.
- Se elimina `<Reference Include="System.Configuration" />` del csproj.

### B4 — `Microsoft.CSharp`
- No hay `dynamic` real como keyword en el core (el único match, en `UpdateChannel.cs:140`, es un comentario/cadena). El refpack de .NET 8 incluye `Microsoft.CSharp.dll` para cubrir `dynamic`.
- Se elimina `<Reference Include="Microsoft.CSharp" />` del csproj.

### B1 — `System.Web`
- El core **no** usa `HttpUtility`/`System.Web` en ningún archivo `.cs` (los usos están en Ide, DesignerSupport, AspNet, MacPlatform, WebReferences, RegexToolkit — fuera del core). La ref en el core era muerta.
- Se elimina `<Reference Include="System.Web" />` del csproj.

## Orden de ejecución (fases; una a la vez)
### B2 — STS/WIF (código + ref) — ✔ COMPLETO
1. `WIFTypeProvider.cs`: `typeof(System.ServiceModel.EndpointAddress)` → `Type.GetType(...)`. [editado]
2. `STSAuthHelper.cs`: quitar `using System.ServiceModel*`, cuerpo WCF de `GetSTSToken`, helpers muertos `SetProperty`/`GetFieldValue` → `NotSupportedException`. [editado]
3. `MonoDevelop.Core.csproj`: quitar `System.ServiceModel` + `System.Web` + `System.Configuration` + `Microsoft.CSharp`. [editado]
4. **Validación**: Roslyn net8 dirigido sobre `MonoDevelop.Core.Web/` (`STSAuthHelper.cs`, `WIFTypeProvider.cs`, `StringExtensions.cs`, `IHttpWebResponse.cs`, `MemoryCache.cs`) → 0 errores. ✔

### B3 — `System.Configuration` — ✔ COMPLETO
1. `PropertyService.cs`: `DataPath` sin `ConfigurationManager.AppSettings`; solo fallback. Se normalizan saltos de línea del bloque a LF y sangría (el editor insertó CRLF/sangría rota; verificado con `cat -A`). ✔
2. csproj: quitar `System.Configuration`. (ya en B2-3) ✔
3. Validación: cambio local, sin tipos nuevos (usa `Path`/`EntryAssemblyPath` ya presentes). ✔

### B4 — `Microsoft.CSharp` — ✔ COMPLETO
1. Confirmado cero `dynamic` real en el core. ✔
2. csproj: quitar `Microsoft.CSharp`. (ya en B2-3) ✔

### B1 — `System.Web` — ✔ COMPLETO
1. Confirmado cero uso de `System.Web`/`HttpUtility` en el core. ✔
2. csproj: quitar `System.Web`. (ya en B2-3) ✔

## Resultado
`MonoDevelop.Core.csproj` ya no referencia ninguna de las cuatro asambleas legacy. El código del core ya no usa `System.ServiceModel` (solo cadenas en `Type.GetType`), `ConfigurationManager` (eliminado), `HttpUtility` (inexistente en core) ni `dynamic` real. El subbloque Core del Bloque B está completo y validado por compilación dirigida Roslyn net8.

## Pendiente (diferido)
- **Interfaz / siguientes bloques**: limpiar las refs `System.Web` (Ide, DesignerSupport, AspNet, MacPlatform, WebReferences, RegexToolkit), `System.ServiceModel` (AspNet, PackageManagement, WebReferences — usar addin WebReferences), `System.Configuration` y `Microsoft.CSharp` de los csproj fuera del core, reemplazando `HttpUtility` por `System.Net.WebUtility` y neutralizando cualquier WCF restante en addins.

## Status log
- B2: STS/WIF neutralizado + ref eliminada; B3/B4/B1: refs eliminadas (uso core cero). Validado Roslyn net8 (Web folder 0 errores).