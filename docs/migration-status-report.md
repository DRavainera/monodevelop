# Migration Status Report

## 1. Executive Summary

The MonoDevelop migration to .NET 10 LTS and Avalonia UI is progressing according to the roadmap. We have successfully transitioned from a Mono-dependent build and runtime to a modern .NET 10-based infrastructure. The core architecture has been refactored to support a dual-UI approach, allowing both the legacy GTK interface and the new Avalonia interface to coexist.

## 2. Phase Status

| Phase | Name | Status | Key Achievements |
|---|---|---|---|
| **Phase 0** | Visual Studio Model Alignment | ✅ Completed | Core infrastructure and dependency mapping established. |
| **Phase 1** | Preferences Completeness | ✅ Completed | Migration of essential option panels and tree logic. |
| **Phase 2** | Functional Migration (Wave 2) | ✅ Completed | Migration of core functional add-ins (Pads & Menus). |
| **Phase 3** | Platform Hooks & Cleanup | ✅ Completed | Add-in system refactored; Linux/Windows/Mac platform hooks migrated to Avalonia. |
| **Phase 4** | Build System Migration | ⏳ In Progress | Transitioning from legacy scripts to .NET SDK-style projects. |
| **Phase 5** | Runtime Migration | ⏳ Pending | Full runtime transition to .NET 10 LTS. |

## 3. Technical Details by Phase

### Phase 0: Visual Studio Model Alignment
- **Objective**: Align `AddonHost` with the VS Extension Model.
- **Outcome**: Successful mapping of assets, identity, and dependency ranges.

### Phase 1: Preferences Completeness
- **Objective**: Migrate option panels.
- **Outcome**: Preference dialog structure and key panels (NuGet, Debugger, etc.) are compatible with the new model.

### Phase 2: Functional Migration
- **Objective**: Migrate behavior, commands, and views.
- **Outcome**: Major pads (AssemblyBrowser, UnitTesting, HexEditor) and menu integration established.

### Phase 3: Platform Hooks & Cleanup
- **Objective**: Decouple platform-specific code from the core and modernize the add-in system.
- **Outcome**: 
  - `AddonHost` now uses `AssemblyDependencyResolver`.
  - `AddonManifest` supports NuGet-style versioning.
  - Platform services (Linux, Mac, Windows) are now implemented as Avalonia add-ins.
  - Full isolation between legacy Gtk add-ins and new Avalonia add-ins.

## 4. Identified Risks & Blockers

- **Roslyn/VS Editor API**: Ongoing work to replace private Visual Studio APIs with public `Microsoft.CodeAnalysis.Workspaces` APIs.
- **Legacy Build Dependencies**: Some projects still rely on MSBuild 15.x/xbuild artifacts; Phase 4 aims to eliminate this.
- **UI Parity**: Ensuring all Gtk-specific UI features are accurately replicated in Avalonia.

## 5. Next Milestone: Phase 4
The next focus is the full migration of the build pipeline to a modern .NET SDK-only flow, removing remaining dependencies on `configure`, `make`, and legacy MSBuild properties.