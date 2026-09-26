# riri.modutils
*Previously known as `p3rpc.commonmodutils`/`riri.commonmodutils`*

A set of libraries for dividing C# code in Reloaded-II mods into modules that share a context object, have a defined module lifetime and can share objects with other modules.

## Usage

The following libraries are available on NuGet:
- `riri.modutils.ContextBase`: Base definitions for a logger context and mod context.
- `riri.modutils.ContextRyoTune`: Base logger/mod contexts for mods that use [RyoTune.Reloaded](https://github.com/RyoTune/RyoTune.Reloaded/)
- `riri.modutils.StateManagement`: Definitions for code modules in `ModuleBase` and the module runtime in `ModuleRuntime`.

The context object for your mod must inherit from `IContext`, but it's recommended to inherit from `BaseContext` or `RyoTuneContext` if you use [RyoTune.Reloaded](https://github.com/RyoTune/RyoTune.Reloaded/):

```c#
using sampleMod.Configuration;
using Reloaded.Mod.Interfaces;
using riri.modutils.ContextRyoTune;
using riri.yamlscans.ReloadedII;
using RyoTune.Reloaded;
using UE.Toolkit.Core.Types.Unreal.Factories;
using UE.Toolkit.Interfaces;

namespace p3rpc.trip2.GameTest;

// RyoTuneContext: from riri.modutils.ContextRyoTune
public class SampleContext : RyoTuneContext
{
    public SampleContext(IModConfig modConfig, IModLoader modLoader, ILogger log, IConfigurable userConfig) 
        : base(modConfig, modLoader, log, userConfig, null, null)
    {
        Log.LogLevel = Config.LogLevel;
        UnrealFactory = YamlScans.GetDependency<IUnrealFactory>();
    }

    // from Unreal Toolkit:
    // https://github.com/RyoTune/UE.Toolkit
    public IUnrealFactory UnrealFactory { get; }

    public Config Config
    {
        get => (Config)ConfigInner!;
        set => ConfigInner = value;
    }
}
```

Modules are defined as the following:

```c#
public class SampleModule : ModuleBase<SampleContext>
{
    public override void Load()
    {
        base.Load();
        // Your module's initialization goes here
        // Don't access the contents of other modules in this function since they
        // may not have initialized yet, use PostLoad for that
    }

    public override void PostLoad()
    {
        // Any initialization that relies on objects from other modules goes here
    }

    public override void Unload()
    {
        base.Unload();
        // Your module's destruction goes here
    }

    // Determines if the module will be loaded on init or after the config is updated
    public override bool ShouldLoad() => true;
}
```

It's not necessary to create a constructor for modules or otherwise create any code that instantiates it
since every module is automatically initialized by the module runtime (`ModuleRuntime<SampleContext>`).

When `ModuleRuntime` is instantiated, it will search for all modules either in the same assembly (C# project) that the context is defined in or in a specified assembly and automatically create an instance of each module. It then calls `ShouldLoad` on each module to check if they should be initialized by calling `Load`.

```c#
// Use this if SampleContext is defined in the same C# project as your modules
var runtime = new ModuleRuntime<SampleContext>(new SampleContext(/* ... */));
// Use this if SampleContext is defined in a different C# project from your modules
var runtime = new ModuleRuntime<SampleContext>(new SampleContext(/* ... */), Assembly.GetAssembly(typeof(SampleModule))!);
```

`Load` may also run after the config is updated if the value of `ShouldLoad` has changed. If `ShouldLoad` returns false and the module is loaded, `Unload` is called. If the module is unloaded and `ShouldLoad` is now true, `Load` is called on it.

## Migrating from `commonmodutils`

Move initialization code from your C# constructor with parameters (`TContext`, `Dictionary<string, ModuleBase<TContext>>`) into `Load()`. `Context` and `Modules` are already assigned by the mod runtime.

```c#
// From Femc Reloaded Project:
// https://github.com/MadMax1960/Femc-Reloaded-Project

// Old initialization code (1.x)
public class CampCommon : ModuleAsmInlineColorEdit<FemcContext>
{
    private string UCmpCommonDraw_DrawFemcShadowColor1_SIG = "...";

    public unsafe CampCommon(FemcContext context, Dictionary<string, ModuleBase<FemcContext>> modules) : base(context, modules)
    {
        _context._utils.SigScan(UCmpCommonDraw_DrawFemcShadowColor1_SIG, "UCmpCommonDraw::DrawFemcShadowColor1", _context._utils.GetDirectAddress, addr => {/* ... */});
        // ...
    }
}

// New initialization code (2.x)
public class CampCommon : ModuleAsmInlineColorEdit<FemcContext>
{
    // from riri.yamlscans.ReloadedII
    private SHStatic<byte> UCmpCommonDraw_DrawFemcShadowColor1;

    public unsafe override void Load()
    {
        base.Load();
        UCmpCommonDraw_DrawFemcShadowColor1 = new("UCmpCommonDraw_DrawFemcShadowColor1", x => { /* ... */ });
    }
}
```

Any code that is in `Register()` using `commonmodutils` must be moved into `PostLoad()`.

Since `ModuleRuntime` will automatically search for modules in your project, there's no equivalent to the `ModRuntime.AddModule` method like in commonmodutils. Whether a module should be loaded or not will depend on your `ShouldLoad` value.

```c#
// Old module loading system (1.x)

public Mod()
{
    // ...
    // Create ModRuntime instance
    _modRuntime = new(_context);
    // ...
}

private void InitializeModules() // Rirurin's stuff, don't touch on penalty of death (Ivan is exempt from this) 
{
    // ...
	if (_configuration.EnableCampMenu)
	{
        // ...
		if (!_configuration.DeckCompatibilitySwitch)
		{
            // Add CampCalendar into system
			_modRuntime.AddModule<CampCalendar>();
            // ...
		}
	}
    // Register the modules (calls Register() on each of them)
    _modRuntime.RegisterModules();
    // ...
}

// ...

public class CampCalendar : ModuleAsmInlineColorEdit<FemcContext>
{
    // Called when AddModule is called
    public unsafe CampCalendar(FemcContext context, Dictionary<string, ModuleBase<FemcContext>> modules) : base(context, modules)
    {
        // ...
    }
}

// New module loading system (2.x)

public Mod()
{
    _modRuntime = new(_context);
}

public class CampCalendar : ModuleAsmInlineColorEdit<FemcContext>
{
    public override bool ShouldLoad() => Context.Config.EnableCampMenu && !Context.Config.DeckCompatibilitySwitch;
}

```