using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChronoView.Core.Configuration;

/// <summary>
/// Manages application configuration with JSON persistence.
/// Stores configuration in platform-specific user data directory.
/// </summary>
public class ConfigurationManager : IConfigurationManager
{
    private readonly string _appAuthor;
    private readonly JsonSerializerOptions _jsonOptions;
    private Models.ApplicationConfiguration? _configuration;

    public string AppDataDirectory { get; }
    public string ConfigurationFilePath { get; }
    public string AppName => _configuration?.ProgramName ?? "AI 데이터 통합 관제 솔루션";

    public string LogsDirectory => Path.Combine(AppDataDirectory, "Logs");
    public string HistoryFilePath => Path.Combine(AppDataDirectory, "abnormal_history.json");

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Initializes a new instance of ConfigurationManager.
    /// </summary>
    /// <param name="appAuthor">Application author for directory creation.</param>
    public ConfigurationManager(string appAuthor = "prische")
    {
        _appAuthor = appAuthor;

        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        // Load configuration using default program name for initial path
        AppDataDirectory = GetUserDataDirectory("AI 데이터 통합 관제 솔루션");
        Directory.CreateDirectory(AppDataDirectory);

        ConfigurationFilePath = Path.Combine(AppDataDirectory, "config.json");

        // Load configuration to get actual program name
        _configuration = LoadConfiguration<Models.ApplicationConfiguration>();
    }

    /// <summary>
    /// Gets platform-specific user data directory.
    /// </summary>
    private string GetUserDataDirectory(string programName)
    {
        string baseDir;

        if (OperatingSystem.IsWindows())
        {
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        else if (OperatingSystem.IsMacOS())
        {
            baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support"
            );
        }
        else // Linux
        {
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            baseDir = !string.IsNullOrEmpty(xdgDataHome)
                ? xdgDataHome
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        return Path.Combine(baseDir, _appAuthor, programName);
    }

    /// <summary>
    /// Loads configuration from JSON file.
    /// </summary>
    public async Task<T> LoadConfigurationAsync<T>() where T : class, new()
    {
        if (!File.Exists(ConfigurationFilePath))
        {
            return new T();
        }

        try
        {
            // Use FileShare.ReadWrite to prevent locking conflicts when event handlers reload during save
            await using var stream = new FileStream(ConfigurationFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var config = await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions).ConfigureAwait(false);
            return config ?? new T();
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Failed to parse configuration file: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new ConfigurationException($"Failed to read configuration file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Loads configuration from JSON file synchronously.
    /// </summary>
    public T LoadConfiguration<T>() where T : class, new()
    {
        if (!File.Exists(ConfigurationFilePath))
        {
            return new T();
        }

        try
        {
            // Use FileShare.ReadWrite to prevent locking conflicts when event handlers reload during save
            using var stream = new FileStream(ConfigurationFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var config = JsonSerializer.Deserialize<T>(stream, _jsonOptions);
            return config ?? new T();
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Failed to parse configuration file: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new ConfigurationException($"Failed to read configuration file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves configuration to JSON file.
    /// </summary>
    public async Task SaveConfigurationAsync<T>(T configuration) where T : class
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        try
        {
            // Validate configuration before saving
            ValidateConfiguration(configuration);

            // Write file and ensure stream is closed before firing event
            await using (var stream = File.Create(ConfigurationFilePath))
            {
                await JsonSerializer.SerializeAsync(stream, configuration, _jsonOptions).ConfigureAwait(false);
            }
            
            // Raise configuration changed event AFTER stream is closed
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(typeof(T)));
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Failed to serialize configuration: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new ConfigurationException($"Failed to write configuration file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves configuration to JSON file synchronously.
    /// </summary>
    public void SaveConfiguration<T>(T configuration) where T : class
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        try
        {
            // Validate configuration before saving
            ValidateConfiguration(configuration);

            // Write file and ensure stream is closed before firing event
            using (var stream = File.Create(ConfigurationFilePath))
            {
                JsonSerializer.Serialize(stream, configuration, _jsonOptions);
            }
            
            // Raise configuration changed event AFTER stream is closed
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(typeof(T)));
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Failed to serialize configuration: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new ConfigurationException($"Failed to write configuration file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validates configuration object.
    /// </summary>
    private void ValidateConfiguration<T>(T configuration) where T : class
    {
        // Basic validation - can be extended with more specific rules
        if (configuration == null)
        {
            throw new ConfigurationValidationException("Configuration cannot be null");
        }

        // Add type-specific validation if needed
        if (configuration is Models.ApplicationConfiguration appConfig)
        {
            ValidateApplicationConfiguration(appConfig);
        }
    }

    /// <summary>
    /// Validates ApplicationConfiguration specific rules.
    /// </summary>
    private void ValidateApplicationConfiguration(Models.ApplicationConfiguration config)
    {
        if (config.ImageSettings != null)
        {
            if (config.ImageSettings.ThumbnailWidth <= 0)
                throw new ConfigurationValidationException("ThumbnailWidth must be positive");
            if (config.ImageSettings.ThumbnailHeight <= 0)
                throw new ConfigurationValidationException("ThumbnailHeight must be positive");
            if (config.ImageSettings.ThumbnailQuality < 1 || config.ImageSettings.ThumbnailQuality > 100)
                throw new ConfigurationValidationException("ThumbnailQuality must be between 1 and 100");
        }

        // MatchingSettings validation removed - legacy ZScoreThreshold no longer exists

        // Validate DataSequenceSettings if present
        if (config.DataSequenceSettings != null)
        {
            if (!config.DataSequenceSettings.Validate(out var errors))
            {
                var errorMessage = string.Join("; ", errors);
                throw new ConfigurationValidationException($"DataSequenceSettings validation failed: {errorMessage}");
            }
        }

        if (config.WorkflowSettings != null)
        {
            if (config.WorkflowSettings.WatcherBufferSize <= 0)
                throw new ConfigurationValidationException("WatcherBufferSize must be positive");
        }
    }

    /// <summary>
    /// Opens the application data directory in the system file explorer.
    /// </summary>
    public void OpenAppDataDirectory()
    {
        OpenFolder(AppDataDirectory);
    }

    /// <summary>
    /// Opens a specified folder in the system file explorer.
    /// </summary>
    public void OpenFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = path,
                    UseShellExecute = true
                });
            }
            else // Linux
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = path,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            throw new ConfigurationException($"Failed to open folder: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Exception thrown when configuration operations fail.
/// </summary>
public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when configuration validation fails.
/// </summary>
public class ConfigurationValidationException : ConfigurationException
{
    public ConfigurationValidationException(string message) : base(message) { }
}
