# Changelog

## 2.2.0

`riri.modutils.ContextBase`:
- Added `IMod` to `IContext`.

`riri.modutils.StateManagement`:
- Added `MustGetModule` method to `StateObject` which throws an exception if a module cannot be retrieved.

## 2.1.0

`riri.modutils.ContextBase`:
- Added `ModConfig` and `ModLoader` fields to `IContext`. In `RyoTuneContext`, `ModConfig` and `ModLoader` are always non-null. For `BaseContext`, it's the responsibility of the mod developer to do dependency injection.

`riri.modutils.ContextRyoTune`:
- Fixed `ModId` getting the executable name from `RyoTune.Project` instead of the mod ID

`riri.modutils.StateManagement`:
- Added the mod loading events `OnModLoaded`, `OnModLoading`, `OnModUnloading` and `OnModLoaderInitialized` to `StateObject`.

## 2.0.0

Rewritten "commonmodutils" into a modular set of libraries specifically for dividing C# code in Reloaded-II mods into modules that share a context object, a means of defining a module's lifetime with `Load` and `Unload` and can share objects with other modules.

Many of the other functions of commonmodutils are available in [riri.yamlscans](https://github.com/rirurin/riri.yamlscans).

The following libraries have been created:

- `riri.modutils.ContextBase`: Base definitions for a logger context and mod context.
- `riri.modutils.ContextRyoTune`: Base logger/mod contexts for mods that use [RyoTune.Reloaded](https://github.com/RyoTune/RyoTune.Reloaded/)
- `riri.modutils.StateManagement`: Definitions for code modules in `ModuleBase` and the module runtime in `ModuleRuntime`.
- `riri.modutils.Test`: For unit testing