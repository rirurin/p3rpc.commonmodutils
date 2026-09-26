using System.Drawing;
using Reloaded.Mod.Interfaces;
using riri.modutils.ContextBase;
using riri.modutils.StateManagement;

namespace riri.modutils.Test;

public class MockLogger : ILogger
{
    public Color BackgroundColor { get; set; }
    public Color TextColor { get; set; }
    public Color ColorRed { get; set; }
    public Color ColorRedLight { get; set; }
    public Color ColorGreen { get; set; }
    public Color ColorGreenLight { get; set; }
    public Color ColorYellow { get; set; }
    public Color ColorYellowLight { get; set; }
    public Color ColorBlue { get; set; }
    public Color ColorBlueLight { get; set; }
    public Color ColorPink { get; set; }
    public Color ColorPinkLight { get; set; }
    public Color ColorLightBlue { get; set; }
    public Color ColorLightBlueLight { get; set; }
    public event EventHandler<string>? OnPrintMessage;
    public event EventHandler<(string text, Color color)>? OnWriteLine;
    public event EventHandler<(string text, Color color)>? OnWrite;

    public void WriteLine(string message) => OnWriteLine!(this, (message, TextColor));
    public void WriteLine(string message, Color color) => OnWriteLine!(this, (message, color));
    public void WriteLineAsync(string message) => OnWriteLine!(this, (message, TextColor));
    public void WriteLineAsync(string message, Color color) => OnWriteLine!(this, (message, color));

    public MockLogger() => OnWriteLine += (_, data) => Console.WriteLine(data.text);
}

public class MockContext : BaseContext
{
    
    public override string ModId => "N/A";
    public override string ModName => "N/A";

    public MockConfigurable Config
    {
        get => (MockConfigurable)ConfigInner!;
        set => ConfigInner = value;
    }

    public MockContext(ILoggerContext logger, MockConfigurable config)
    {
        Logger = logger;
        Config = config;
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class Module0 : ModuleBase<MockContext> {}

// ReSharper disable once ClassNeverInstantiated.Global
public class Module1 : ModuleBase<MockContext>
{
    public override bool ShouldLoad() => !Context.Config.LoadModule3;
}

// ReSharper disable once ClassNeverInstantiated.Global
public class Module2 : ModuleBase<MockContext>
{
    public override void PostLoad()
    {
        Assert.IsTrue(GetModule<Module0>(out _));
        Assert.IsTrue(GetModule<Module1>(out _));
        Assert.IsFalse(GetModule<Module3>(out _));
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class Module3 : ModuleBase<MockContext>
{
    public override bool ShouldLoad() => Context.Config.LoadModule3;
}

public class MockConfigurable(bool loadModule3 = false) : IConfigurable
{
    public string ConfigName => nameof(MockConfigurable);
    public Action Save => () => { };
    public bool LoadModule3 { get; } = loadModule3;
}

[TestClass]
public sealed class Runtime
{
    [TestMethod]
    public void TestGetModules()
    {
        var runtime = new ModuleRuntime<MockContext>(new MockContext(new DefaultLogger(new MockLogger()), new MockConfigurable()));
        Assert.IsTrue(runtime.GetModule<Module0>(out var module0));
        Assert.IsTrue(runtime.GetModule<Module1>(out var module1));
        Assert.IsTrue(runtime.GetModule<Module2>(out var module2));
        Assert.IsFalse(runtime.GetModule<Module3>(out _));
        
        Assert.IsTrue(module0!.GetModule<Module1>(out _));
        Assert.IsTrue(module0.GetModule<Module2>(out _));
        Assert.IsFalse(module0.GetModule<Module3>(out _));
        
        Assert.IsTrue(module1!.GetModule<Module0>(out _));
        Assert.IsTrue(module1.GetModule<Module2>(out _));
        Assert.IsFalse(module1.GetModule<Module3>(out _));
        
        Assert.IsTrue(module2!.GetModule<Module1>(out _));
        Assert.IsTrue(module2.GetModule<Module2>(out _));
        Assert.IsFalse(module2.GetModule<Module3>(out _));
        
    }
    
    [TestMethod]
    public void TestGetConfig()
    {
        var runtime = new ModuleRuntime<MockContext>(new MockContext(new DefaultLogger(new MockLogger()), new MockConfigurable()));
        Assert.IsTrue(runtime.GetModule<Module0>(out var module0));
        Assert.IsTrue(runtime.GetModule<Module1>(out var module1));
        Assert.IsTrue(runtime.GetModule<Module2>(out var module2));
        Assert.IsFalse(runtime.GetModule<Module3>(out _));
        
        Assert.IsFalse(runtime.Context.Config.LoadModule3);
        Assert.IsFalse(module0!.Context.Config.LoadModule3);
        Assert.IsFalse(module1!.Context.Config.LoadModule3);
        Assert.IsFalse(module2!.Context.Config.LoadModule3);
    }
    
    [TestMethod]
    public void TestUpdateConfig()
    {
        var runtime = new ModuleRuntime<MockContext>(new MockContext(new DefaultLogger(new MockLogger()), new MockConfigurable()));
        Assert.IsTrue(runtime.GetModule<Module0>(out var module0));
        Assert.IsTrue(runtime.GetModule<Module1>(out _));
        Assert.IsTrue(runtime.GetModule<Module2>(out var module2));
        Assert.IsFalse(runtime.GetModule<Module3>(out _));
        
        runtime.OnConfigUpdated(new MockConfigurable(true));
        
        Assert.IsFalse(runtime.GetModule<Module1>(out _));
        Assert.IsTrue(runtime.GetModule<Module3>(out var module3));
        
        Assert.IsTrue(runtime.Context.Config.LoadModule3);
        Assert.IsTrue(module0!.Context.Config.LoadModule3);
        Assert.IsTrue(module2!.Context.Config.LoadModule3);
        Assert.IsTrue(module3!.Context.Config.LoadModule3);
    }
}