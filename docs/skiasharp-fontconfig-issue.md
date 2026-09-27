# Issue upstream SkiaSharp — colgado del font manager fontconfig con WOFF/WOFF2

Listo para copiar a https://github.com/mono/SkiaSharp/issues (texto en inglés,
como se publica upstream). Datos extraídos de docs/interfaz-plan.md §M16e/M16f/M16g.

---

## Title

**[Linux] SkFontMgr_fontconfig spins at 100% CPU in `FcPatternGetString` (via `GetFamilyNames`) when the user font directories contain WOFF/WOFF2 files — startup hangs before any UI is shown**

## Environment

- SkiaSharp **3.119.4** (`libSkiaSharp` bundling its own libfontconfig usage), Avalonia 12.1.2 on top (Avalonia.Skia), .NET 10 console/desktop app
- Linux x64, GNOME (Wayland + XWayland), fontconfig system-wide
- User font folders under `~/.local/share/fonts/` that contain **WOFF/WOFF2 web-font containers** (e.g. `~/.local/share/fonts/VictorMono/`, JetBrains Mono Nerd dirs shipping `.woff2`)

## Summary

Any SkiaSharp/Avalonia app whose user font directories contain WOFF/WOFF2
containers hangs at Skia initialization — **before a single line of UI runs** —
spinning one core at 100% CPU. The process never paints a window. Removing the
WOFF/WOFF2 files, or pointing `FONTCONFIG_FILE` at a config that excludes them,
makes the app start instantly.

## Repro

1. Put a font directory containing `.woff2` files under `~/.local/share/fonts/`
   (several popular nerd-font distributions ship WOFF2 variants there).
2. Run any app that resolves `SKFontManager.Default` (or starts Avalonia with
   Avalonia.Skia) on Linux.
3. The app hangs before `Program.Main` UI work completes; CPU is pinned at 100%.

Attaching gdb to the spinning process shows a stable native stack (no managed
frames yet):

```
#0  strcmp () in libc
#1  FcObjectTypeLookup () in libfontconfig
#2  FcPatternGetString () in libfontconfig
#3  SkFontMgr_fontconfig::GetFamilyNames () in libSkiaSharp
```

`FcPatternGetString` never returns: the enumeration inside
`SkFontMgr_fontconfig::GetFamilyNames` does not make progress with the patterns
fontconfig produces for those directories.

## Empirical findings (weeks of matrix testing)

- `fc-list` and `fc-cache` **do not hang** on the same directories: fontconfig
  itself tolerates the files; the hang is in the *interaction* between Skia's
  fontconfig font manager and those fontconfig patterns.
- A minimal `FONTCONFIG_FILE` that only includes `/usr/share/fonts` (plus a
  private `<cachedir>`) unblocks the app; the same app with the default config
  hangs.
- Rejecting the containers via fontconfig itself also unblocks the app:

```xml
<fontconfig>
  <dir>/usr/share/fonts</dir>
  <selectfont>
    <rejectfont>
      <glob>*.woff</glob>
      <glob>*.woff2</glob>
    </rejectfont>
  </selectfont>
</fontconfig>
```

- The **shared fontconfig cache** carries the state: a config that reuses
  `~/.cache/fontconfig` (which holds entries for the WOFF files) hangs even
  when the config no longer lists the user dirs; a config with a fresh private
  cache dir + rejectfont globs works.
- Workaround propagation detail that cost us a day: setting the variable from
  managed code with `Environment.SetEnvironmentVariable` does **not** reach
  libfontconfig (it reads `getenv(3)` at native init, and the managed call does
  not write the process environ block). The workaround must `setenv(3)` via
  P/Invoke **before** any Skia type loads.

## Expected behavior

A malformed/unsupported font container should be skipped during enumeration —
the font manager must never loop forever inside fontconfig, whatever the
pattern content is. `fc-list` proves fontconfig itself can enumerate the same
directories without hanging.

## Proposed fix (in Skia, surfaced by SkiaSharp)

1. **Harden `SkFontMgr_fontconfig::GetFamilyNames`**: validate each pattern
   before/after `FcPatternGetString` — if the family object type lookup fails
   (`FcObjectTypeLookup` → unexpected/foreign object value) or the string is
   null, `continue` to the next pattern instead of retrying. A hard bound on
   the enumeration (e.g. bail out after N non-progressing iterations) would
   also convert the infinite loop into a degraded-but-working startup.
2. **Skip non-TT containers at the manager level**: `SkFontMgr_fontconfig`
   could either build its `FcFontList` request with a config that rejects
   `*.woff`/`*.woff2` (they are *web* containers; system font managers gain
   nothing from them), or filter patterns whose file extension is not a
   scalable font type Skia can decode.
3. **SkiaSharp-side mitigation** (if touching Skia is too slow): expose the
   already-platform-specific fontconfig config so a consumer can inject the
   rejectfont globs + private cache dir before the font manager initializes —
   today we had to P/Invoke `setenv` for `FONTCONFIG_FILE` with a hand-written
   XML file, which is fragile and undocumented.

## Workaround we ship today

`FontconfigSanitizer.Apply()` in our app (MIT, happy to PR as a test harness):
writes a minimal fontconfig XML (explicit `<dir>` entries + rejectfont globs
for `*.woff`/`*.woff2` + private `<cachedir>`), then `setenv(3)`s
`FONTCONFIG_FILE` via libc **before** any Avalonia/Skia type is touched, and
respects an externally-provided `FONTCONFIG_FILE`.

## Impact

Every .NET desktop app that uses SkiaSharp/Avalonia on Linux shares the user
`~/.local/share/fonts` directory; nerd-font and web-font distribution
conventions make WOFF2 files there common. One bad directory turns the whole
application into an unbootable 100%-CPU process with no UI and no diagnostic.
