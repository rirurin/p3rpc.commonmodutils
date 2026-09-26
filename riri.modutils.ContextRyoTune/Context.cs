using System.Drawing;
using Reloaded.Mod.Interfaces;
using riri.modutils.ContextBase;
using RyoTune.Reloaded;
using ILogger = Reloaded.Mod.Interfaces.ILogger;

namespace riri.modutils.ContextRyoTune;

internal static class LogLevelUtils
{
    public static LogLevel ToRyoTune(this Microsoft.Extensions.Logging.LogLevel logLevel)
        => logLevel switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => LogLevel.Verbose,
            Microsoft.Extensions.Logging.LogLevel.Debug => LogLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Information or
                Microsoft.Extensions.Logging.LogLevel.None => LogLevel.Information,
            Microsoft.Extensions.Logging.LogLevel.Warning => LogLevel.Warning,
            _ => LogLevel.Error,
        };
}

/// <summary>
/// Logger context wrapper intended for integration with RyoTune.Reloaded's logger singleton
/// </summary>
public class RyoTuneLogger : ILoggerContext
{
    /// <inheritdoc/>
    public void Verbose(string message) => Log.Verbose(message);

    /// <inheritdoc/>
    public void Debug(string message) => Log.Debug(message);

    /// <inheritdoc/>
    public void Information(string message) => Log.Information(message);

    /// <inheritdoc/>
    public void Warning(string message) => Log.Warning(message);

    /// <inheritdoc/>
    public void Error(string message) => Log.Error(message);

    /// <summary>
    /// Does nothing, cannot change RyoTune logger's color on demand
    /// </summary>
    public void SetColor(Color value) {}

    /// <summary>
    /// Does nothing, cannot change RyoTune logger's async preference on demand
    /// </summary>
    public void SetAsync(bool value) {}

    /// <inheritdoc/>
    public void SetLevel(Microsoft.Extensions.Logging.LogLevel level) => Log.LogLevel = level.ToRyoTune();
}

/// <summary>
/// Base context
/// </summary>
public class RyoTuneContext : BaseContext
{
    /// <inheritdoc/>
    public override string ModId => Project.Instance.AppId;

    /// <inheritdoc/>
    public override string ModName => Project.Instance.Name;
    
    /// <summary>
    /// Constructor for RyoTuneContext
    /// </summary>
    public RyoTuneContext(IModConfig modConfig, IModLoader modLoader, ILogger log, IConfigurable userConfig, Color? color, bool? useAsyncLog)
    {
        Project.Initialize(modConfig, modLoader, log, color ?? Color.White, useAsyncLog ?? false);
        Logger = new RyoTuneLogger();
        ConfigInner = userConfig;
    }
}