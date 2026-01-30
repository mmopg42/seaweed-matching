using Microsoft.Extensions.Logging;

namespace ChronoView.Core.NIR.Line2;

/// <summary>
/// State enum for NIR2 chunk detection state machine.
/// </summary>
public enum Nir2ChunkState
{
    /// <summary>
    /// No chunk is currently being collected.
    /// </summary>
    Idle,

    /// <summary>
    /// Currently collecting samples for a chunk (Presence == 1).
    /// </summary>
    Collecting,

    /// <summary>
    /// Chunk collection completed, waiting for processing.
    /// </summary>
    Complete
}

/// <summary>
/// Detects and manages NIR2 chunks using a state machine.
/// Transitions: Idle → Collecting (Presence 2→1), Collecting → Complete (Presence 1→2)
/// </summary>
public class Nir2ChunkDetector
{
    private readonly ILogger _logger;
    private readonly int _lineNumber;
    private Nir2ChunkState _state;
    private Nir2Chunk? _currentChunk;
    private int _previousPresence;
    private readonly object _lock = new();

    /// <summary>
    /// Event raised when a chunk is completed.
    /// </summary>
    public event Action<Nir2Chunk>? ChunkCompleted;

    /// <summary>
    /// Gets the current state of the detector.
    /// </summary>
    public Nir2ChunkState State
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
    /// Gets the current chunk being collected (if any).
    /// </summary>
    public Nir2Chunk? CurrentChunk
    {
        get
        {
            lock (_lock)
            {
                return _currentChunk;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the Nir2ChunkDetector.
    /// </summary>
    /// <param name="lineNumber">Line number (1 or 2) for this detector.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public Nir2ChunkDetector(int lineNumber, ILogger logger)
    {
        _lineNumber = lineNumber;
        _logger = logger;
        _state = Nir2ChunkState.Idle;
        _previousPresence = 2; // Assume starting with no object present
    }

    /// <summary>
    /// Processes a new sample and updates the state machine.
    /// </summary>
    /// <param name="sample">The sample to process.</param>
    public void ProcessSample(Nir2Sample sample)
    {
        lock (_lock)
        {
            if (!sample.IsValid)
            {
                // Invalid sample (Presence != 1), check if we need to complete chunk
                if (_state == Nir2ChunkState.Collecting && sample.Presence == 2)
                {
                    CompleteChunk();
                }
                _previousPresence = sample.Presence;
                return;
            }

            switch (_state)
            {
                case Nir2ChunkState.Idle:
                    // Transition to Collecting on first valid sample (Presence 2→1)
                    if (sample.Presence == 1 && _previousPresence == 2)
                    {
                        StartNewChunk(sample);
                        _state = Nir2ChunkState.Collecting;
                    }
                    break;

                case Nir2ChunkState.Collecting:
                    // Add samples while Presence remains 1
                    if (sample.Presence == 1)
                    {
                        AddToChunk(sample);
                    }
                    // Transition to Complete on Presence 1→2
                    else if (sample.Presence == 2)
                    {
                        _state = Nir2ChunkState.Complete;
                        CompleteChunk();
                        _state = Nir2ChunkState.Idle;
                    }
                    break;

                case Nir2ChunkState.Complete:
                    // Should not receive samples in Complete state
                    // Reset to Idle if we get here
                    _logger.LogWarning("ChunkDetector in Complete state received sample, resetting to Idle");
                    _state = Nir2ChunkState.Idle;
                    break;
            }

            _previousPresence = sample.Presence;
        }
    }

    /// <summary>
    /// Starts a new chunk with the given sample.
    /// </summary>
    private void StartNewChunk(Nir2Sample sample)
    {
        _currentChunk = new Nir2Chunk
        {
            ChunkId = Guid.NewGuid().ToString("N"),
            LineNumber = _lineNumber,
            StartedAt = sample.Timestamp
        };
        _currentChunk.AddSample(sample);

        _logger.LogDebug("Started new chunk {ChunkId} at {Timestamp}",
            _currentChunk.ChunkId, sample.Timestamp);
    }

    /// <summary>
    /// Adds a sample to the current chunk.
    /// </summary>
    private void AddToChunk(Nir2Sample sample)
    {
        if (_currentChunk == null)
        {
            // Only log if this is unexpected (i.e., Presence=1 but no chunk)
            // Presence=2 is normal "no object" state, no need to warn
            if (sample.Presence == 1)
            {
                _logger.LogWarning("Attempted to add valid sample to null chunk");
            }
            return;
        }

        _currentChunk.AddSample(sample);
    }

    /// <summary>
    /// Completes the current chunk and raises the ChunkCompleted event.
    /// </summary>
    private void CompleteChunk()
    {
        if (_currentChunk == null)
            return;

        if (_currentChunk.SampleCount == 0)
        {
            _logger.LogWarning("Completing chunk with no samples");
            return;
        }

        _logger.LogDebug("Completed chunk {ChunkId} with {SampleCount} samples",
            _currentChunk.ChunkId, _currentChunk.SampleCount);

        // Raise event outside the lock to avoid potential deadlocks
        var completedChunk = _currentChunk;
        _currentChunk = null;

        // Invoke event handlers
        ChunkCompleted?.Invoke(completedChunk);
    }

    /// <summary>
    /// Resets the detector to Idle state.
    /// Any current chunk will be completed before resetting.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            if (_state == Nir2ChunkState.Collecting && _currentChunk != null)
            {
                CompleteChunk();
            }

            _state = Nir2ChunkState.Idle;
            _currentChunk = null;
            _previousPresence = 2;
        }
    }

    /// <summary>
    /// Gets diagnostic information about the current state.
    /// </summary>
    public string GetDiagnosticInfo()
    {
        lock (_lock)
        {
            return $"State: {_state}, " +
                   $"PreviousPresence: {_previousPresence}, " +
                   $"CurrentChunkSamples: {_currentChunk?.SampleCount ?? 0}";
        }
    }
}
