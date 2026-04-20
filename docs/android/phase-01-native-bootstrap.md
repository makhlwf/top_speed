# Android Port - Phase 01: Native Bootstrap Foundation

## Objective
Establish a native-library bootstrap path that works on Android without requiring a desktop-style `lib` folder layout.

This phase only covers native library resolution and platform detection. It does **not** introduce Android UI hosting, touch gestures, sensors, or packaging/publish flow yet.

## What Was Implemented
1. `TopSpeed/Runtime/NativeLibraryBootstrap.cs`
- Removed the non-framework early exit when `AppContext.BaseDirectory/lib` is missing.
- Kept resolver installation active even when there is no `lib` folder.
- Added fallback loading by library name (`NativeLibrary.Load`) after directory probing.
- Added Android-aware candidate sets for:
  - `miniaudioex` (`libminiaudioex.so`, `miniaudioex`)
  - `phonon` (`libphonon.so`, `phonon`)
  - `prism` (`libprism.so`, `prism`)

2. `TS.Sdl/Interop/Library.cs`
- Added Android detection (`OSPlatform.Create("ANDROID")`).
- Added Android `dlopen`/`dlerror` bindings via `libdl.so`.
- Extended SDL candidate probing with both:
  - full filename (`libSDL3.so`)
  - short system name (`SDL3`)

3. `TopSpeed/Speech/Prism/Native.cs`
- Added explicit Android platform branch and mapped it to `LinuxMethods` (same `libprism.so` ABI path used by current wrapper design).

## Verification Gates For This Phase
1. The existing desktop targets still compile:
- `dotnet build top_speed_net/TopSpeed.sln -c Debug`
2. No behavior change required in gameplay/race/menu logic.

## Next Phase (Phase 02)
Create Android app host scaffolding and wire lifecycle only:
1. Add a dedicated Android entry project (Activity/lifecycle owner).
2. Define Android implementation of:
- window host lifecycle bridge
- text input bridge
- clipboard bridge
- file dialog bridge (temporary fallback behavior allowed)
3. Keep current game loop and core logic unchanged; only host adapters should be new.

## Exit Criteria Before Moving To Phase 03
1. App starts on Android device/emulator.
2. Main loop ticks and speaks a known startup phrase.
3. Graceful close/background handling works without crash.
