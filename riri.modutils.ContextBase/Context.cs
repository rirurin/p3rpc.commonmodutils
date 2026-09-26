using System.Drawing;
using Reloaded.Mod.Interfaces;

namespace riri.modutils.ContextBase;

/// <summary>
/// Base interface for implementing a mod logger
/// </summary>
public interface ILoggerContext
{
    /// <summary>
    /// Prints a message on a new line with the log level <c>Verbose</c>
    /// </summary>
    /// <param name="message">Message to print</param>
    void Verbose(string message);
    /// <summary>
    /// Prints a message on a new line with the log level <c>Debug</c>
    /// </summary>
    /// <param name="message">Message to print</param>
    void Debug(string message);
    /// <summary>
    /// Prints a message on a new line with the log level <c>Information</c>
    /// </summary>
    /// <param name="message">Message to print</param>
    void Information(string message);
    /// <summary>
    /// Prints a message on a new line with the log level <c>Warning</c>
    /// </summary>
    /// <param name="message">Message to print</param>
    void Warning(string message);
    /// <summary>
    /// Prints a message on a new line with the log level <c>Error</c>
    /// </summary>
    /// <param name="message">Message to print</param>
    void Error(string message);
    /// <summary>
    /// Set the color that messages from this mod are printed out using
    /// </summary>
    /// <param name="value">Color to use</param>
    void SetColor(Color value);
    /// <summary>
    /// Set whether to use Reloaded's <c>ILogger.WriteLine</c> or <c>ILogger.WriteLineAsync</c>
    /// </summary>
    /// <param name="value">If true, use <c>WriteLineAsync</c></param>
    void SetAsync(bool value);
    /// <summary>
    /// Sets the logger's log level. Messages that are lower priority than the set log level are not printed.
    /// </summary>
    /// <param name="level">New log level</param>
    void SetLevel(Microsoft.Extensions.Logging.LogLevel level);
}

/// <summary>
/// Default logger adapter used for Reloaded-II
/// </summary>
public class DefaultLogger(ILogger logger) : ILoggerContext
{
    /// <summary>
    /// String to prefix before every message printed by this mod
    /// </summary>
    public string Prefix = string.Empty;
    /// <summary>
    /// Color to use for all messages printed by this mod
    /// </summary>
    public Color Color { get; private set; }
    /// <summary>
    /// Whether to use <c>ILogger.WriteLine</c> or <c>ILogger.WriteLineAsync</c>
    /// </summary>
    public bool UseAsync { get; private set; }
    /// <summary>
    /// Current log level for the given mod
    /// </summary>
    public Microsoft.Extensions.Logging.LogLevel Level { get; private set; } = Microsoft.Extensions.Logging.LogLevel.Information;
    /// <summary>
    /// Reloaded-II ILogger instance
    /// </summary>
    protected readonly ILogger? Logger = logger;

    private void WriteLine(string message)
    {
        var prefix = Prefix == string.Empty ? "" : $"[{Prefix}] ";
        if (UseAsync)
        {
            Logger!.WriteLineAsync($"{prefix}{message}", Color);
        }
        else
        {
            Logger!.WriteLine($"{prefix}{message}", Color);
        }
    }

    /// <inheritdoc/>
    public void Verbose(string message)
    {
        if (Level <= Microsoft.Extensions.Logging.LogLevel.Trace)
            WriteLine(message);
    }

    /// <inheritdoc/>
    public void Debug(string message)
    {
        if (Level <= Microsoft.Extensions.Logging.LogLevel.Debug)
            WriteLine(message);
    }

    /// <inheritdoc/>
    public void Information(string message)
    {
        if (Level <= Microsoft.Extensions.Logging.LogLevel.Information)
            WriteLine(message);
    }

    /// <inheritdoc/>
    public void Warning(string message)
    {
        if (Level <= Microsoft.Extensions.Logging.LogLevel.Warning)
            WriteLine(message);
    }

    /// <inheritdoc/>
    public void Error(string message)
    {
        if (Level <= Microsoft.Extensions.Logging.LogLevel.Critical)
            WriteLine(message);
    }

    /// <inheritdoc/>
    public void SetColor(Color value) => Color = value;

    /// <inheritdoc/>
    public void SetAsync(bool value) => UseAsync = value;

    /// <inheritdoc/>
    public void SetLevel(Microsoft.Extensions.Logging.LogLevel level) => Level = level;
}

/// <summary>
/// Interface for creating shared state between modules
/// </summary>
public interface IContext
{
    /// <summary>
    /// The identifier for the mod (e.g p3rpc.femc)
    /// </summary>
    string ModId { get; }
    /// <summary>
    /// The name of the mod (e.g Femc Project)
    /// </summary>
    string ModName { get; }
    /// <summary>
    /// The logger context
    /// </summary>
    ILoggerContext? Logger { get; }
}

/// <summary>
/// Base context for storing shared state
/// </summary>
public abstract class BaseContext : IContext
{
    /// <inheritdoc/>
    public abstract string ModId { get; }
    /// <inheritdoc/>
    public abstract string ModName { get; }
    /// <inheritdoc/>
    public ILoggerContext? Logger { get; protected init; }
    
    /// <summary>
    /// Singleton handling the mod's configuration.
    /// This property is not public to prevent mod developers from accessing the config as the IConfigurable base type.
    /// Make a public property for "Config" in your subclass of Context that returns your mod's Config type.
    /// </summary>
    protected IConfigurable? ConfigInner { get; set; }

    /// <summary>
    /// Called when the game's config is updated (<c>ConfigurationUpdated</c> is called from your Reloaded mod).
    /// </summary>
    /// <param name="newConfig">New config state</param>
    public virtual void OnConfigUpdated(IConfigurable newConfig) => ConfigInner = newConfig;
}