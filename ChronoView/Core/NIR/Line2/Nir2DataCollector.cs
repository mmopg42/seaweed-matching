using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using ChronoView.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.NIR.Line2;

/// <summary>
/// Collection state for the NIR2 data collector.
/// </summary>
public enum Nir2CollectorState
{
    /// <summary>
    /// Collector is stopped and not collecting data.
    /// </summary>
    Stopped,

    /// <summary>
    /// Collector is starting up.
    /// </summary>
    Starting,

    /// <summary>
    /// Collector is actively polling and collecting data.
    /// </summary>
    Running,

    /// <summary>
    /// Collector is shutting down.
    /// </summary>
    Stopping
}

/// <summary>
/// Collects NIR2 data from an API endpoint, records to CSV, and detects chunks.
/// </summary>
public class Nir2DataCollector : IDisposable
{
    private readonly ILogger _logger;
    private readonly Nir2Settings _settings;
    private readonly string _chunkStoragePath;
    private readonly Nir2ChunkDetector _chunkDetector;
    private readonly INir2ChunkFileStorage _chunkStorage;
    private readonly HttpClient _httpClient;
    private Nir2CsvManager? _csvManager;
    private CancellationTokenSource? _cts;
    private Task? _collectionTask;
    private Nir2CollectorState _state;
    private readonly object _lock = new();
    private bool _isDisposed;

    // Statistics
    private int _samplesCollected;
    private int _chunksDetected;
    private int _apiErrors;
    private int _csvErrors;

    /// <summary>
    /// Event raised when the collector state changes.
    /// </summary>
    public event Action<Nir2CollectorState>? StateChanged;

    /// <summary>
    /// Event raised when a chunk is completed.
    /// </summary>
    public event Action<Nir2Chunk>? ChunkDetected;

    /// <summary>
    /// Gets the current state of the collector.
    /// </summary>
    public Nir2CollectorState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Gets the number of samples collected in the current session.
    /// </summary>
    public int SamplesCollected
    {
        get
        {
            lock (_lock)
            {
                return _samplesCollected;
            }
        }
    }

    /// <summary>
    /// Gets the number of chunks detected in the current session.
    /// </summary>
    public int ChunksDetected
    {
        get
        {
            lock (_lock)
            {
                return _chunksDetected;
            }
        }
    }

    /// <summary>
    /// Gets the current CSV file path, if any.
    /// </summary>
    public string? CurrentCsvPath => _csvManager?.CurrentFilePath;

    /// <summary>
    /// Initializes a new instance of the Nir2DataCollector.
    /// </summary>
    /// <param name="settings">NIR2 runtime settings.</param>
    /// <param name="chunkStoragePath">Path where chunk data will be stored. Required for NIR2 parsing.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <param name="chunkStorage">Storage service for saving chunks to disk.</param>
    public Nir2DataCollector(Nir2Settings settings, string chunkStoragePath, ILogger logger, INir2ChunkFileStorage? chunkStorage = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _chunkStoragePath = chunkStoragePath ?? "";
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chunkStorage = chunkStorage ?? new Nir2ChunkFileStorage(logger);

        // Configure HTTP client with timeout
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_settings.HttpTimeout)
        };

        // Initialize chunk detector for Line 2
        _chunkDetector = new Nir2ChunkDetector(lineNumber: 2, logger);
        _chunkDetector.ChunkCompleted += OnChunkCompleted;

        _state = Nir2CollectorState.Stopped;
    }

    /// <summary>
    /// Starts the data collection process.
    /// </summary>
    public async Task StartAsync()
    {
        lock (_lock)
        {
            if (_state != Nir2CollectorState.Stopped)
            {
                _logger.LogWarning("Cannot start collector, current state: {State}", _state);
                return;
            }

            // Validate chunk storage path first - required for NIR2 parsing
            if (string.IsNullOrWhiteSpace(_chunkStoragePath))
            {
                _logger.LogError("NIR2 chunk storage path is not configured. Cannot start collection.");
                SetState(Nir2CollectorState.Stopped);
                return;
            }

            SetState(Nir2CollectorState.Starting);
            _cts = new CancellationTokenSource();
        }

        try
        {
            // Initialize CSV manager
            _csvManager = new Nir2CsvManager(_settings.FullCsvDirectory, _logger);
            var csvPath = _csvManager.CreateNewCsvFile();
            if (csvPath == null)
            {
                _logger.LogError("Failed to create CSV file, cannot start collection");
                SetState(Nir2CollectorState.Stopped);
                return;
            }

            // Reset statistics
            _samplesCollected = 0;
            _chunksDetected = 0;
            _apiErrors = 0;
            _csvErrors = 0;
            _chunkDetector.Reset();

            SetState(Nir2CollectorState.Running);
            _logger.LogInformation("NIR2 data collection started: {CsvPath}", csvPath);

            // Start collection loop
            _collectionTask = RunCollectionLoopAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start NIR2 data collection");
            SetState(Nir2CollectorState.Stopped);
        }
    }

    /// <summary>
    /// Stops the data collection process.
    /// </summary>
    public async Task StopAsync()
    {
        lock (_lock)
        {
            if (_state != Nir2CollectorState.Running)
            {
                return;
            }

            SetState(Nir2CollectorState.Stopping);
        }

        // Cancel the collection task
        _cts?.Cancel();

        // Wait for collection task to complete
        if (_collectionTask != null)
        {
            try
            {
                await _collectionTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when canceling
            }
        }

        // Close CSV file
        _csvManager?.CloseFile();

        lock (_lock)
        {
            SetState(Nir2CollectorState.Stopped);
        }

        _logger.LogInformation("NIR2 data collection stopped. Samples: {Samples}, Chunks: {Chunks}",
            _samplesCollected, _chunksDetected);
    }

    /// <summary>
    /// Main collection loop that polls the API and processes samples.
    /// </summary>
    private async Task RunCollectionLoopAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Poll API for data
                var sample = await PollApiAsync(cancellationToken);

                if (sample != null)
                {
                    // Process sample through chunk detector
                    _chunkDetector.ProcessSample(sample);

                    // Write to CSV
                    if (_csvManager != null && _csvManager.WriteRow(sample))
                    {
                        lock (_lock)
                        {
                            _samplesCollected++;
                        }
                    }
                    else
                    {
                        lock (_lock)
                        {
                            _csvErrors++;
                        }
                    }
                }

                // Wait for the polling interval
                await Task.Delay(_settings.PollingInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in collection loop");
                lock (_lock)
                {
                    _apiErrors++;
                }

                // Wait before retrying (exponential backoff based on error count)
                var backoffDelay = CalculateBackoffDelay(_apiErrors);
                await Task.Delay(backoffDelay, cancellationToken);
            }
        }

        stopwatch.Stop();
        _logger.LogInformation("Collection loop ended after {Duration}", stopwatch.Elapsed);
    }

    /// <summary>
    /// Polls the NIR2 API for a single sample.
    /// Implements retry policy with exponential backoff.
    /// </summary>
    private async Task<Nir2Sample?> PollApiAsync(CancellationToken cancellationToken)
    {
        int retryCount = 0;
        int maxRetries = _settings.MaxRetries;

        while (retryCount <= maxRetries)
        {
            try
            {
                var response = await _httpClient.GetAsync(_settings.ApiUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<Nir2ApiResponse>(json);

                if (apiResponse == null)
                {
                    _logger.LogWarning("Deserialized null API response");
                    return null;
                }

                // Convert API response to Nir2Sample
                var sample = Nir2Sample.FromApiResponse(apiResponse, _logger);

                if (sample == null)
                {
                    _logger.LogWarning("Failed to convert API response to sample");
                    return null;
                }

                // Validate required fields
                if (!sample.HasValidData())
                {
                    // Only log if Presence is not 2 (2 = normal "no object" state, not an error)
                    if (sample.Presence != 2)
                    {
                        _logger.LogDebug("Received invalid sample: Presence={Presence}", sample.Presence);
                    }
                    // Still return the sample; chunk detector needs Presence info
                    return sample;
                }

                return sample;
            }
            catch (HttpRequestException ex)
            {
                retryCount++;
                if (retryCount > maxRetries)
                {
                    _logger.LogError(ex, "API request failed after {Retries} retries", maxRetries);
                    lock (_lock)
                    {
                        _apiErrors++;
                    }
                    return null;
                }

                var delay = CalculateBackoffDelay(retryCount);
                _logger.LogWarning(ex, "API request failed, retry {Retry}/{MaxRetries} after {Delay}ms",
                    retryCount, maxRetries, delay);

                await Task.Delay(delay, cancellationToken);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse API response as JSON");
                lock (_lock)
                {
                    _apiErrors++;
                }
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Calculates exponential backoff delay based on error/retry count.
    /// </summary>
    private static int CalculateBackoffDelay(int errorCount)
    {
        // Base delay of 100ms, doubling with each error, capped at 5 seconds
        const int baseDelay = 100;
        const int maxDelay = 5000;

        var delay = baseDelay * (int)Math.Pow(2, Math.Min(errorCount, 6));
        return Math.Min(delay, maxDelay);
    }

    /// <summary>
    /// Handles chunk completion events from the detector.
    /// </summary>
    private void OnChunkCompleted(Nir2Chunk chunk)
    {
        lock (_lock)
        {
            _chunksDetected++;
        }

        _logger.LogDebug("Chunk completed: {Summary}", chunk.GetSummary());

        // Apply aggregation strategy
        chunk.Aggregate(_settings.AggregationStrategy);

        // Save chunk to disk
        var savedPath = _chunkStorage.SaveChunk(chunk, _chunkStoragePath);
        if (savedPath != null)
        {
            _logger.LogDebug("Chunk saved: {Path}", savedPath);
        }
        else
        {
            _logger.LogWarning("Failed to save chunk {ChunkId}", chunk.ChunkId);
        }

        // Notify subscribers
        ChunkDetected?.Invoke(chunk);
    }

    /// <summary>
    /// Sets the collector state and raises the StateChanged event.
    /// </summary>
    private void SetState(Nir2CollectorState newState)
    {
        _state = newState;
        StateChanged?.Invoke(newState);
    }

    /// <summary>
    /// Disposes resources used by this collector.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _cts?.Cancel();
        _csvManager?.Dispose();
        _httpClient.Dispose();

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
