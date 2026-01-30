using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.NIR.Line2;

/// <summary>
/// Manages CSV file operations for NIR2 data collection.
/// Creates a new CSV file for each parsing session with timestamp-based naming.
/// </summary>
public class Nir2CsvManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _csvDirectory;
    private StreamWriter? _writer;
    private string? _currentFilePath;
    private bool _isDisposed;
    private readonly object _lock = new();
    private int _writesSinceFlush;

    /// <summary>
    /// CSV header columns.
    /// </summary>
    private const string CsvHeader = "Timestamp,Protein,Moisture,Presence";

    /// <summary>
    /// Number of writes before flushing to disk.
    /// At 100ms polling interval, 50 writes = 5 seconds of data.
    /// This balances I/O performance with acceptable data loss risk.
    /// </summary>
    private const int FlushBufferSize = 50;

    /// <summary>
    /// Initializes a new instance of the Nir2CsvManager.
    /// </summary>
    /// <param name="csvDirectory">Directory where CSV files will be stored.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public Nir2CsvManager(string csvDirectory, ILogger logger)
    {
        _csvDirectory = csvDirectory;
        _logger = logger;
    }

    /// <summary>
    /// Gets whether a CSV file is currently open for writing.
    /// </summary>
    public bool IsFileOpen => _writer != null;

    /// <summary>
    /// Gets the path of the currently open CSV file, if any.
    /// </summary>
    public string? CurrentFilePath => _currentFilePath;

    /// <summary>
    /// Creates a new CSV file with a timestamp-based filename.
    /// File format: sensor_data_yyyyMMdd_HHmmss.csv
    /// </summary>
    /// <returns>The full path of the created file, or null if creation failed.</returns>
    public string? CreateNewCsvFile()
    {
        lock (_lock)
        {
            // Close any existing file first and reset counter
            CloseFileInternal();
            _writesSinceFlush = 0;

            try
            {
                // Ensure directory exists
                if (!Directory.Exists(_csvDirectory))
                {
                    Directory.CreateDirectory(_csvDirectory);
                    _logger.LogInformation("Created NIR2 CSV directory: {Directory}", _csvDirectory);
                }

                // Generate filename with current timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"sensor_data_{timestamp}.csv";
                _currentFilePath = Path.Combine(_csvDirectory, filename);

                // Create and open the file
                _writer = new StreamWriter(_currentFilePath);

                // Write CSV header
                _writer.WriteLine(CsvHeader);
                _writer.Flush(); // Ensure header is written immediately

                _logger.LogInformation("Created NIR2 CSV file: {FilePath}", _currentFilePath);
                return _currentFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create NIR2 CSV file in directory: {Directory}", _csvDirectory);
                _currentFilePath = null;
                _writer = null;
                return null;
            }
        }
    }

    /// <summary>
    /// Writes a single NIR2 sample to the CSV file.
    /// </summary>
    /// <param name="sample">The sample data to write.</param>
    /// <returns>True if the write was successful, false otherwise.</returns>
    public bool WriteRow(Nir2Sample sample)
    {
        lock (_lock)
        {
            if (_writer == null)
            {
                _logger.LogWarning("Attempted to write to CSV file but no file is open.");
                return false;
            }

            try
            {
                // Format: Timestamp,Protein,Moisture,Presence
                // Use ISO 8601 format for timestamp, invariant culture for numbers
                string line = string.Format(CultureInfo.InvariantCulture,
                    "{0},{1:F4},{2:F4},{3}",
                    sample.Timestamp.ToString("o"), // ISO 8601 with full precision
                    sample.Protein,
                    sample.Moisture,
                    sample.Presence);

                _writer.WriteLine(line);

                // Buffer writes and flush periodically for better performance.
                // At 100ms polling with 50-write buffer, flush occurs every 5 seconds.
                // CloseFileInternal always flushes, so no data is lost on normal shutdown.
                _writesSinceFlush++;
                if (_writesSinceFlush >= FlushBufferSize)
                {
                    _writer.Flush();
                    _writesSinceFlush = 0;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to NIR2 CSV file: {FilePath}", _currentFilePath);
                return false;
            }
        }
    }

    /// <summary>
    /// Writes multiple NIR2 samples to the CSV file in a single operation.
    /// </summary>
    /// <param name="samples">Collection of samples to write.</param>
    /// <returns>Number of samples successfully written.</returns>
    public int WriteRows(IEnumerable<Nir2Sample> samples)
    {
        lock (_lock)
        {
            if (_writer == null)
            {
                _logger.LogWarning("Attempted to write to CSV file but no file is open.");
                return 0;
            }

            int writtenCount = 0;
            try
            {
                foreach (var sample in samples)
                {
                    if (WriteRow(sample))
                    {
                        writtenCount++;
                    }
                }

                return writtenCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk write to NIR2 CSV file.");
                return writtenCount;
            }
        }
    }

    /// <summary>
    /// Closes the currently open CSV file.
    /// </summary>
    public void CloseFile()
    {
        lock (_lock)
        {
            CloseFileInternal();
        }
    }

    /// <summary>
    /// Internal implementation for closing the file (must be called within lock).
    /// </summary>
    private void CloseFileInternal()
    {
        if (_writer != null)
        {
            try
            {
                _writer.Flush();
                _writer.Dispose();
                _logger.LogInformation("Closed NIR2 CSV file: {FilePath}", _currentFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing NIR2 CSV file: {FilePath}", _currentFilePath);
            }
            finally
            {
                _writer = null;
            }
        }

        _currentFilePath = null;
    }

    /// <summary>
    /// Disposes resources used by this manager.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        lock (_lock)
        {
            CloseFileInternal();
            _isDisposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer to ensure resources are cleaned up.
    /// </summary>
    ~Nir2CsvManager()
    {
        Dispose();
    }
}
