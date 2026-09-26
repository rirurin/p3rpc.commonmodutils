# Changelog

## 2.0.0

Rewritten "commonmodutils" into a modular set of libraries specifically for dividing C# code in Reloaded-II mods into modules that share a context object, a means of defining a module's lifetime with `Load` and `Unload` and can share objects with other modules.

Many of the other functions of commonmodutils are available in [riri.yamlscans](https://github.com/rirurin/riri.yamlscans).

The following libraries have been created:

- `riri.modutils.ContextBase`: Base definitions for a logger context and mod context.
- `riri.modutils.ContextRyoTune`: Base logger/mod contexts for mods that use [RyoTune.Reloaded](https://github.com/RyoTune/RyoTune.Reloaded/)
- `riri.modutils.StateManagement`: Definitions for code modules in `ModuleBase` and the module runtime in `ModuleRuntime`.
- `riri.modutils.Test`: For unit testing