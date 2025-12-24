using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// Priority-based event channel that processes events based on their priority level.
    /// High priority events are returned before medium and low priority events.
    /// Within the same priority level, events are returned in FIFO order.
    /// </summary>
    public class PriorityEventChannel
    {
        private readonly Channel<FileSystemEventArgs> _highPriorityChannel;
        private readonly Channel<FileSystemEventArgs> _mediumPriorityChannel;
        private readonly Channel<FileSystemEventArgs> _lowPriorityChannel;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the channel writer for writing events with priority.
        /// </summary>
        public PriorityChannelWriter Writer { get; }

        /// <summary>
        /// Initializes a new instance of the PriorityEventChannel.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public PriorityEventChannel(ILogger<PriorityEventChannel>? logger = null)
        {
            var options = new UnboundedChannelOptions
            {
                SingleReader = false,  // Multiple workers can read
                SingleWriter = false   // Multiple sources can write
            };

            _highPriorityChannel = Channel.CreateUnbounded<FileSystemEventArgs>(options);
            _mediumPriorityChannel = Channel.CreateUnbounded<FileSystemEventArgs>(options);
            _lowPriorityChannel = Channel.CreateUnbounded<FileSystemEventArgs>(options);

            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PriorityEventChannel>.Instance;

            Writer = new PriorityChannelWriter(this);
        }

        /// <summary>
        /// Tries to write an event to the channel with the specified priority.
        /// </summary>
        /// <param name="args">Event arguments to write.</param>
        /// <param name="priority">Priority level for the event.</param>
        /// <returns>true if the event was written successfully; otherwise, false.</returns>
        public bool TryWrite(FileSystemEventArgs args, EventPriority priority)
        {
            // Select channel based on priority
            Channel<FileSystemEventArgs> targetChannel = priority switch
            {
                EventPriority.High => _highPriorityChannel,
                EventPriority.Medium => _mediumPriorityChannel,
                EventPriority.Low => _lowPriorityChannel,
                _ => _mediumPriorityChannel // Default to medium
            };

            // Write to selected channel
            bool success = targetChannel.Writer.TryWrite(args);

            if (success)
            {
                _logger.LogDebug("Event written to {Priority} priority channel: {Path}", 
                    priority, args.FullPath);
            }
            else
            {
                _logger.LogWarning("Failed to write event to {Priority} priority channel: {Path}", 
                    priority, args.FullPath);
            }

            return success;
        }

        /// <summary>
        /// Reads all events from the channel in priority order.
        /// High priority events are returned first, then medium, then low.
        /// Within the same priority, events are returned in FIFO order.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop reading.</param>
        /// <returns>Async enumerable of file system events.</returns>
        public async IAsyncEnumerable<FileSystemEventArgs> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                FileSystemEventArgs? nextEvent = null;

                // Try HIGH priority first
                if (_highPriorityChannel.Reader.TryRead(out var highEvent))
                {
                    nextEvent = highEvent;
                }
                // Then MEDIUM priority
                else if (_mediumPriorityChannel.Reader.TryRead(out var medEvent))
                {
                    nextEvent = medEvent;
                }
                // Finally LOW priority
                else if (_lowPriorityChannel.Reader.TryRead(out var lowEvent))
                {
                    nextEvent = lowEvent;
                }
                // No events available, wait for any
                else
                {
                    // Use WaitToReadAsync on all channels with Task.WhenAny
                    Task<bool> highTask = _highPriorityChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();
                    Task<bool> medTask = _mediumPriorityChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();
                    Task<bool> lowTask = _lowPriorityChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();

                    Task completedTask;
                    try
                    {
                        completedTask = await Task.WhenAny(highTask, medTask, lowTask);
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancellation requested, exit gracefully
                        yield break;
                    }

                    // Try reading from the channel that signaled availability
                    if (completedTask == highTask && await highTask)
                    {
                        if (_highPriorityChannel.Reader.TryRead(out highEvent))
                            nextEvent = highEvent;
                    }
                    else if (completedTask == medTask && await medTask)
                    {
                        if (_mediumPriorityChannel.Reader.TryRead(out medEvent))
                            nextEvent = medEvent;
                    }
                    else if (completedTask == lowTask && await lowTask)
                    {
                        if (_lowPriorityChannel.Reader.TryRead(out lowEvent))
                            nextEvent = lowEvent;
                    }
                    else
                    {
                        // All channels completed (no more events)
                        yield break;
                    }
                }

                // Yield event if found
                if (nextEvent != null)
                {
                    yield return nextEvent;
                }
            }
        }

        /// <summary>
        /// Completes all channel writers, signaling no more events will be written.
        /// </summary>
        public void Complete()
        {
            _highPriorityChannel.Writer.Complete();
            _mediumPriorityChannel.Writer.Complete();
            _lowPriorityChannel.Writer.Complete();

            _logger.LogInformation("PriorityEventChannel completed");
        }

        /// <summary>
        /// Custom channel writer that supports priority-based writes.
        /// </summary>
        public class PriorityChannelWriter
        {
            private readonly PriorityEventChannel _channel;

            internal PriorityChannelWriter(PriorityEventChannel channel)
            {
                _channel = channel;
            }

            /// <summary>
            /// Tries to write an event with the specified priority.
            /// </summary>
            /// <param name="args">Event arguments to write.</param>
            /// <param name="priority">Priority level.</param>
            /// <returns>true if written successfully; otherwise, false.</returns>
            public bool TryWrite(FileSystemEventArgs args, EventPriority priority)
            {
                return _channel.TryWrite(args, priority);
            }

            /// <summary>
            /// Completes all underlying channels.
            /// </summary>
            public void Complete()
            {
                _channel.Complete();
            }
        }
    }
}
