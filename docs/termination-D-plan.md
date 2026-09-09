# Bloque D — Pendientes de la parte Seguridad que se atienden en Terminación

## Objetivo
Saldar tres deudas de seguridad registradas en el plan maestro: **D1** bump de NuGet (CVE-2024-0057),
**D2** `BinaryFormatter` no-remoting residual, y **D3** verificación de firma de paquetes de addin (.mpack).

## Resultado del análisis (bloque D no accionable en el núcleo de este fork)
Los tres ítems viven **fuera de `MonoDevelop.Core`** a tiro de este plan: dos en **submódulos** y uno en un
**addin**, y además dependen de coordinación con **upstream** (`mono/*`). Por la disciplina del plan
("1 problema a la vez", no mantener submódulos/upstream, alcance Core-first), el Bloque D se **documenta y se
difiere**, precisando el dueño y los pasos de cierre para cada ítem. **No se introducen binarios ni firmas
ajenos en este fork.**

### D1 — Bump de `NuGet.Client` 5.4.0 → ≥5.11.6 (CVE-2024-0057) — ⚠ DEFERRED (upstream/submodule)
- **Ubicación**: `main/external/nuget-binary/` es un **submódulo** (gitlink `7871fa2...`, repo
  `https://github.com/mono/nuget-binary.git`), no binarios commiteados en el main repo.
- **Sobre qué se consume**: `main/src/addins/MonoDevelop.PackageManagement/MonoDevelop.PackageManagement.csproj`
  (+ `MDBuildTasks.targets`, `Makefile.am`). Es un **addin** (territorio "Interfaz", fuera del núcleo).
- **Versión real actual**: el submodule HEAD es `7871fa2` ("Add NuGet.exe **4.9.1**...", 2018-11-30). Ni siquiera
  está en 5.4.0; está en la línea **4.x/4.9.1**. La deuda es más profunda que el bump del enunciado.
- **Por qué no es accionable aquí**: el bump exige que **upstream `mono/nuget-binary`** publique binarios ≥5.11.6
  (o pedir un bump de la submodule pin a un commit que los traiga), y que se valide en el build net472. Eso es
  coordinación de dependencias `mono/*`, ajena al alcance de este fork (que solo push a `origin` DRavainera y no
  mantiene submódulos).
- **Quién/paso de cierre (dueño: Interfaz / aporte upstream)**: 1) liberar/pin `mono/nuget-binary` con NuGet
  ≥5.11.6 (corrige CVE-2024-0057); 2) apuntar la submodule pin del main repo al commit nuevo; 3) validar
  `PackageManagement` sobre net8 (puede exigir ajustar llamadas a la API NuGet al refactor 5.x).

### D2 — `BinaryFormatter` no-remoting residual — ⚠ DEFERRED (submódulos/UI + tests)
- **xwt** (portapapeles): `main/external/xwt/Xwt/Xwt/TransferDataSource.cs:150,164` — `BinaryFormatter.Serialize/
  DeserializeValue` para popitar el clipboard de la UI. `xwt` es **submódulo** y pertenece a la **Interfaz**
  (toolkit UI, deferred). El `BinaryFormatter` aquí solo actúa sobre contenido local al portapapeles (no es
  tráfico remoto), pero queda como deuda (API obsoleta/insegura en .NET 8/candidate a `NotSupported`).
- **guiunit** (tests): `main/external/guiunit/src/framework/Constraints/BinarySerializableConstraint.cs:38`
  (`BinaryFormatter` en un constraint de aserción para tests de serialización). **Submódulo** `mono/guiunit`.
- **Por qué no es accionable aquí**: ambos viven en repos externos (`mono/xwt`, `mono/guiunit`); no se deben
  editar desde este fork ni duplicar. Además xwt cae en el alcance "Interfaz".
- **Quién/paso de cierre (dueño: Interfaz / upstream)**: en `xwt` reemplazar por serialización segura
  (p. ej. JSON con `System.Text.Json` + `DataFormat`/`BlobToDataObject`) o limitar a texto; en `guiunit`, reescribir
  el constraint sin `BinaryFormatter` (o marcarlo obsoleto). Debe unirse a la migración global "sin BinaryFormatter".

### D3 — Verificación de firma de paquetes de addin (.mpack) — ⚠ DEFERRED (submodule mono-addins)
- **Ubicación**: la firma/checksum de `.mpack` vive en `Mono.Addins.Setup` dentro del **submódulo**
  `main/external/mono-addins` (`SetupService.cs`, `AddinStore.cs`, `DownloadFileRequest.cs`, `SetupTool.cs`).
  Es la dependencia `mono/mono-addins`, no código nuestro.
- **Por qué no es accionable aquí**: canal ya es HTTPS-only (hecho en Migración); la validación criptográfica del
  `.mpack` es responsabilidad de `Mono.Addins.Setup` (upstream). No se parchea un submódulo desde este fork.
- **Quién/paso de cierre (dueño: upstream `mono/mono-addins` / Interfaz)**: habilitar verificación de
  firma/checksum del `.mpack` en `Mono.Addins.Setup` y activarla en la cadena de update del addin.

## Criterio de terminado
- [x] D1: analizado → **diferido** (submodule + upstream; ver D1).
- [x] D2: analizado → **diferido** (submódulos xwt/guiunit; xwt→Interfaz).
- [x] D3: analizado → **diferido** (submodule mono-addins/upstream).
- No se introducen binarios vendidos, ni se parchean submódulos `mono/*`, ni se añade código de firma en el fork.

## Status log
- Bloque D documentado como **deuda diferida**, con dueño y paso de cierre por ítem. Sin cambios de código en
  `MonoDevelop.Core`. Se registra en `termination-plan.md` (Bloque D → DEFERRED).