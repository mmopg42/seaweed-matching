using ChronoView.Models;

namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// Defines a service for determining when and which file groups should be evicted
    /// when a new file arrives that falls outside the expected time sequence.
    /// </summary>
    public interface IEvictionService
    {
        /// <summary>
        /// Checks if eviction is needed for a new group based on time sequence constraints.
        /// </summary>
        /// <param name="newGroup">The new file group being added</param>
        /// <param name="config">Application configuration containing data sequence settings</param>
        /// <param name="activeGroups">Currently active file groups</param>
        /// <param name="lineNumber">The line number for filtering groups (line separation)</param>
        /// <returns>EvictionResult indicating if eviction is needed and which groups are affected</returns>
        EvictionResult CheckEvictionNeeded(
            FileGroup newGroup,
            ApplicationConfiguration config,
            IEnumerable<FileGroup> activeGroups,
            int lineNumber);
    }

    /// <summary>
    /// Result of an eviction check.
    /// </summary>
    public record EvictionResult
    {
        /// <summary>
        /// Gets whether eviction should occur.
        /// </summary>
        public bool ShouldEvict { get; init; }

        /// <summary>
        /// Gets the list of victim groups that should be evicted (cascaded).
        /// Null when no eviction is needed.
        /// </summary>
        public List<FileGroup>? VictimGroups { get; init; }

        /// <summary>
        /// Returns a result indicating no eviction is needed.
        /// </summary>
        public static EvictionResult NoEviction => new() { ShouldEvict = false };

        /// <summary>
        /// Returns a result indicating cascading eviction with the specified victim groups.
        /// </summary>
        public static EvictionResult CascadingEviction(List<FileGroup> victims)
        {
            return new EvictionResult { ShouldEvict = true, VictimGroups = victims };
        }
    }
}
