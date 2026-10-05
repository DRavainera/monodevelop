# Plan "Interfaz" — Migración de la GUI y estabilización preparatoria

Estado del plan (2026-10-05, actualizado):
- **Objetivo del plan**: migración de la interfaz (GUI) de la aplicación — actualmente **Gtk#/Mono** — a **Avalonia UI 12.1.2**, produciendo todo con el **.NET 10 SDK + Runtime**.
- **Iteración actual**: Ejecución de la Fase 3 (Platform Hooks & Cleanup) completada. El sistema de addins para Avalonia está estabilizado y los hooks de plataforma implementados.
- La decisión de tecnología se mantiene: **Avalonia UI 12**.

Restricciones de entorno:
- Compilar SIEMPRE con `dotnet` (SDK 10). NO usar mono SDK ni `mono` para ejecutar la aplicación.

## Enunciado del plan

1. **Estabilización de la UI legacy**: Comprendida y mantenida para coexistir con la nueva UI.
2. **Migración incremental**: Implementación por módulos con validación constante.
3. **Dependencias**: Evaluación de Mono.Cairo vs SkiaSharp (en curso).
4. **Coexistencia**: Los módulos legacy se mantienen hasta el cutover final.
5. **Cero Dependencias Mono**: Al final, el código no dependerá de SDK o Runtime Mono.
6. **Rebranding**: Pendiente de decisión de nombre y logo.

## Fases de Ejecución

### Fase 0 - Inventario y línea base ✅
- Inventario de dependencias y mapeo de proyectos legacy.

### Fase 1 - Estabilización de la base actual ✅
- Build reproducible y estado de referencia para rollback.

### Fase 2 - Modernización de dependencias ✅
- Actualización de paquetes NuGet y resolución de conflictos de Roslyn/VS Editor.

### Fase 3 - Platform Hooks & Cleanup ✅
- **Saneamiento de Addins**: 
  - Refactorización de `AddonHost` y `AddonManifest`.
  - Implementación de `AssemblyDependencyResolver` y `AddonLoadContext` para resolución de ensamblados por nombre.
  - Soporte de rangos de versiones NuGet-style ([9.0, 10.0)).
- **Aislamiento de Estructura**:
  - Movimiento de todos los manifiestos `.avaloniaaddon.json` a carpetas `MonoDevelop.Avalonia.*`.
  - Preservación de carpetas legacy para compatibilidad con `--old-gui`.
- **Platform Hooks**:
  - Implementación de `LinuxPlatform`, `WindowsPlatform` y `MacPlatform` como addins de Avalonia.
  - Migración de funcionalidad desde `GnomePlatform` eliminando dependencias de GIO/GTK.

### Fase 4 - Migración del sistema de build (Siguiente)
- Pasar de `configure` + `xbuild` a pipeline basado en `.NET SDK`.

### Fase 5 - Migración del runtime a .NET 8/10
- Llevar el runtime base a .NET 10 LTS.

### Fase 6 a 9 - Validación y Cutover
- Linux-first validation $ightarrow$ Multi-OS preparation $ightarrow$ UI Decision $ightarrow$ Cutover final.

## Extras del usuario (Checklist)

| # | Extra | Estado | Evidencia |
|---|-------|---------|-----------|
| 1 | Bordes de ventana propios de Avalonia | **HECHO** | `SystemDecorCations=None` |
| 2 | Botones min/max/close integrados | **HECHO** | `CaptionButtons` implementados |
| 3 | Pestañas isla | **HECHO** | `TabItem.island` |
| 4 | Soporte nativo Wayland y X11 | **HECHO** | `Avalonia.Desktop` |
| 5 | Temas claro y oscuro | **HECHO** | `ThemeDictionaries` |
| 6 | Mono.Cairo $ightarrow$ SkiaSharp | **EN CURSO** | `SkTextEditor` implementada |
| 7 | Módulos Mono.* fork $ightarrow$ .NET 10 | **EN CURSO** | Submódulos enlazados a rama `net10` |
| 8 | Xwt y módulos Gtk/Mac | PENDIENTE | Evaluación por módulo |
| 9 | Rediseño iconos PNG Fluent | **EN CURSO** | `IconService` implementada |
| 10 | MonoDevelop.Ide 2.6.0.0 $ightarrow$ 12.1.2.0 | **HECHO** | commit 72816594c7 |
| 11 | Migración en bucle | **EN CURSO** | QA por módulo |
| 12 | Subagente QA senior | **EN CURSO** | Validación de Fase 3 aprobada |
| 13 | Seguimiento en docs | **EN CURSO** | Synchronized docs |
| 14 | Compilación SDK 10 real sin bloqueos | PENDIENTE | Gate de cierre |
| 15 | NuGet de addins $ightarrow$ 7.9 | PENDIENTE | Cierre final |
