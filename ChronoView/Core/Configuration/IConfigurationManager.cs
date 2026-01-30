namespace ChronoView.Core.Configuration;

/// <summary>
/// Interface for managing application configuration with JSON persistence.
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Loads configuration from the JSON file.
    /// </summary>
    /// <typeparam name="T">The configuration type to load.</typeparam>
    /// <returns>The loaded configuration or a new instance if file doesn't exist.</returns>
    Task<T> LoadConfigurationAsync<T>() where T : class, new();

    /// <summary>
    /// Loads configuration from the JSON file synchronously.
    /// </summary>
    /// <typeparam name="T">The configuration type to load.</typeparam>
    /// <returns>The loaded configuration or a new instance if file doesn't exist.</returns>
    T LoadConfiguration<T>() where T : class, new();

    /// <summary>
    /// Saves configuration to the JSON file.
    /// </summary>
    /// <typeparam name="T">The configuration type to save.</typeparam>
    /// <param name="configuration">The configuration object to save.</param>
    Task SaveConfigurationAsync<T>(T configuration) where T : class;

    /// <summary>
    /// Saves configuration to the JSON file synchronously.
    /// </summary>
    /// <typeparam name="T">The configuration type to save.</typeparam>
    /// <param name="configuration">The configuration object to save.</param>
    void SaveConfiguration<T>(T configuration) where T : class;

    /// <summary>
    /// Gets the path to the application data directory.
    /// </summary>
    string AppDataDirectory { get; }

    /// <summary>
    /// Gets the display name for this application.
    /// </summary>
    string AppName { get; }

    /// <summary>
    /// Gets the path to the configuration file.
    /// </summary>
    string ConfigurationFilePath { get; }

    /// <summary>
    /// Gets the directory path where log files are stored.
    /// </summary>
    string LogsDirectory { get; }

    /// <summary>
    /// Gets the full path to the abnormal history file.
    /// </summary>
    string HistoryFilePath { get; }

    /// <summary>
    /// Event raised when configuration changes are saved.
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Opens the application data directory in the system file explorer.
    /// </summary>
    void OpenAppDataDirectory();

    /// <summary>
    /// Opens a specified folder in the system file explorer.
    /// </summary>
    /// <param name="path">The folder path to open.</param>
    void OpenFolder(string path);
}

/// <summary>
/// Event arguments for configuration change notifications.
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    public Type ConfigurationType { get; }
    public DateTime ChangedAt { get; }

    public ConfigurationChangedEventArgs(Type configurationType)
    {
        ConfigurationType = configurationType;
        ChangedAt = DateTime.UtcNow;
    }
}
