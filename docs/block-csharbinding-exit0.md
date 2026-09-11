# Bloque: CSharpBinding.csproj compila a EXIT=0 vía MonoRoslynCompat (redact)

Fecha: 2026-09-11
Estado: COMPLETADO (todos los consumidores de MonoRoslynCompat a 0 errores)

## Objetivo
- Llevar `src/addins/CSharpBinding/CSharpBinding.csproj` (dependiente de Roslyn) a 0 errores
  extendiendo los stubs de compatibilidad en `MonoRoslynCompat` y `RoslynCompatStubs.cs`.
- Referencias Roslyn: `Microsoft.CodeAnalysis{,.Workspaces,.CSharp,.CSharp.Workspaces}` 4.8.0
  assets `lib/netstandard2.0` (HintPath explícitos, líneas 300-319 del csproj).
- Build orden: compat primero (`MonoRoslynCompat.csproj`), luego los consumidores.

## Mecanismo (lo que se añadió a compat)
- Regla ya probada en el bloque del Ide: para tipos `internal`/`private` de Roslyn, stub `public`
  dentro del **namespace exacto** de Roslyn (shadow por namespace, no por orden de refs).
- CompatStubs.cs (CompatStubs.cs) — bloques añadidos:
  - `IsOrdinaryMethod(this ISymbol)` (antes `IMethodSymbol`).
  - ns `Microsoft.CodeAnalysis`: `SymbolCompatExtensions` (`IsKind(ISymbol, SymbolKind)`,
    `IsMandatoryNamedParameterPosition(SyntaxToken)`), `DocumentCompatExtensions`
    (`WithFrozenPartialSemantics`, `IsOpen`, `SetDocumentContext`), `RelativePathResolver`,
    `IOrganizeImportsService` (`OrganizeImportsAsync`).
  - ns `Microsoft.CodeAnalysis.Shared.Extensions`: `SemanticModelCompatExtensions.GetEnclosingNamedTypeOrAssembly`,
    `LocationExtensions.IsVisibleSourceLocation`.
  - ns `Microsoft.CodeAnalysis.CSharp`: `CSharpCompatSyntaxExtensions` (`IsParentKind`,
    `GetMatchingDirective(DirectiveTriviaSyntax)`) y `CSharpSyntaxFactsService` clase standalone
    (NO implementa `ISyntaxFactsService`; con `Instance`, `IsVerbatimStringLiteral`,
    `IsStringLiteral`, `GetContainingTypeDeclaration`).
  - ns `Microsoft.CodeAnalysis.Options`: `IEditorConfigStorageLocation`.
  - ns `Roslyn.Utilities`: `KeyValuePairUtil.Create`, `SpecializedCollections.EmptyEnumerable<T>`,
    `RoslynUtilitiesExtensions.ToSet<T>`.
  - ns `Microsoft.CodeAnalysis.Host`: `WorkspaceMetadataFileReferenceResolver : MetadataReferenceResolver`
    (sin override `WithRelativePath` — dio CS0115; Roslyn 4.8 no lo tiene virtual).
  - ns `Microsoft.CodeAnalysis.Rename`: `RenamableSymbolInfo` + `RenameLocations.ReferenceProcessing.GetRenamableSymbolAsync`.
  - ns `Microsoft.CodeAnalysis.Host.Mef`: `IMefHostExportProvider`, `ILanguageMetadata`, `LanguageMetadata`,
    `OrderableLanguageMetadata`, `LanguageMetadataExtensions.FilterToSpecificLanguage<TExtension,TMetadata>`
    (versión final devuelve **valores** `IEnumerable<TExtension>`, `Select(e => e.Value)`).
  - `MonoRoslynTextExtensions`: overload `GetOpenDocumentInCurrentContextWithChanges(this SourceText)`
    (fix CS1929 en `CSharpPathedDocumentExtension`).
  - `FatalError.ReportWithoutCrashUnlessCanceled(Exception)`.
  - `UsingsAndExternAliasesDirectiveComparer` (ns `Microsoft.CodeAnalysis.CSharp.Utilities`).
- RoslynCompatStubs.cs (MonoDevelop.Refactoring): `[Flags] enum DisplayNameOptions` y
  `ISyntaxFactsService` ampliado con `GetContainingMemberDeclaration(SyntaxNode,int,bool)` (devuelve
  `SyntaxNode`, no `bool`) y `GetDisplayName(SyntaxNode, DisplayNameOptions)`.

## Fijaciones de call-sites en CSharpBinding
- `AbstractGenerateAction.cs:170`, `CSharpTextEditorIndentation.cs:285`: `WaitAndGetResult(...)`
  ambiguo (CS0121 con `MonoDevelop.Ide.TaskUtil`) → `.GetAwaiter().GetResult()`.
- `DelegateCompletionProvider.cs:319`, `PathedDocumentTextEditorExtension.cs`:
  `GetEnclosingSymbol<INamedTypeSymbol>` (genérico privado en Roslyn) → `as INamedTypeSymbol`.
- `PathedDocumentTextEditorExtension.cs`: añadido `default(CancellationToken)` a `GetInsertionPoints`
  (firma: `(IReadonlyTextDocument, SemanticModel, ITypeSymbol, int, CancellationToken = default)`).
- `SignatureMarkupCreator.cs:115`: `IsTupleType()` → propiedad `IsTupleType`.
- `CSharpFindReferencesProvider.cs:222`: `(await FindImplementationsAsync(...)).ToArray()`.
- `ArgumentSyntaxExtensions.cs:116`: `GetType(...)` → `GetTypeInfo(...).Type`.
- `CompilationUnitSyntaxExtensions.cs`: `AddUsingDirectives(...)` → `Usings.AddRange(...)` + `WithUsings(...)`.
- `PartialGenerator.cs` / `CSharpIndentationTracker.cs`: añadido `using Microsoft.CodeAnalysis;`
  (ver hallazgo IsKind / `GetSyntaxRootSynchronously` abajo).
- `HelperMethods.cs`: añadido `using Microsoft.CodeAnalysis;`.

## Hallazgo clave 1 — `SyntaxToken.IsKind` requiere `using Microsoft.CodeAnalysis;`
- `PartialGenerator.cs:104` fallaba con CS1061 (`"SyntaxToken" no contiene "IsKind"`) pese a tener
  `using Microsoft.CodeAnalysis.CSharp;`.
- Deterministica (probes en `/tmp/opencode/isprobe/`): la extensión `IsKind(SyntaxToken, SyntaxKind)`
  se resuelve desde el namespace **`Microsoft.CodeAnalysis`** (Common), NO desde `.CSharp`; el set de
  4 DLLs Roslyn 4.8 no es el desencadenante; los namespaces dummy MonoDevelop no lo son.
- Fix: añadir `using Microsoft.CodeAnalysis;` a `PartialGenerator.cs`.

## Hallazgo clave 2 — `GetSyntaxRootSynchronously` ya existía en compat
- Está en `CompatStubs.cs` `RoslynCompatExtensions` (pública, ns `Microsoft.CodeAnalysis`, línea ~1520).
- `CSharpIndentationTracker.cs:81` fallaba solo porque al archivo le faltaba `using Microsoft.CodeAnalysis;`
  (NO duplicar la extensión en compat).

## Verificación
- `MonoRoslynCompat` (DebugLinux): 0 errores.
- `MonoDevelop.Refactoring` (valida `ISyntaxFactsService`/`DisplayNameOptions`): 0 errores.
- `CSharpBinding.csproj`: **0 errores** (antes de este bloque: 4 líneas únicas de error en 3 sitios).
- Resto de consumidores de `MonoRoslynCompat` (`MonoDevelop.Core`, `MonoDevelop.Ide`,
  `MonoDevelop.AssemblyBrowser`): 0 errores.

## Pendiente
- Build completo vía `Main.sln` (config `DebugLinux`) hasta EXIT=0.
- Actualizar la hoja de cálculo/tracker de bloques si existe.