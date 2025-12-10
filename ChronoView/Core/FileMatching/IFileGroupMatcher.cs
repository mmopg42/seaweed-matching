using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChronoView.Models;

namespace ChronoView.Core.FileMatching
{
    /// <summary>
    /// Interface for file group matching service that correlates files by timestamp
    /// </summary>
    public interface IFileGroupMatcher
    {
        /// <summary>
        /// Match files from unmatched collection into file groups
        /// </summary>
        Task<IEnumerable<FileGroup>> MatchFilesAsync(UnmatchedFiles unmatchedFiles);

        /// <summary>
        /// Update an existing group with a new file
        /// </summary>
        Task<FileGroup> UpdateGroupAsync(FileGroup group, string filePath, FileType fileType);

        /// <summary>
        /// Get or set the matching configuration
        /// </summary>
        MatchingConfiguration Configuration { get; set; }

        /// <summary>
        /// Reset the consumed NIR keys tracking
        /// </summary>
        void ResetConsumedNirKeys();

        /// <summary>
        /// Add a NIR key to the consumed set
        /// </summary>
        void AddConsumedNirKey(string nirKey);
    }

    /// <summary>
    /// Configuration for file matching behavior
    /// </summary>
    public class MatchingConfiguration
    {
        /// <summary>
        /// Maximum time difference in seconds for NIR matching
        /// </summary>
        public double NirMatchTimeDiff { get; set; } = 1.0;

        /// <summary>
        /// Use time-based matching for composite cameras
        /// </summary>
        public bool UseCamTimeMatching { get; set; } = true;

        /// <summary>
        /// Minimum time difference in seconds for camera matching
        /// </summary>
        public double CamMatchMinDiff { get; set; } = 4.0;

        /// <summary>
        /// Maximum time difference in seconds for camera matching
        /// </summary>
        public double CamMatchMaxDiff { get; set; } = 6.0;

        /// <summary>
        /// Use folder suffix for line separation (_0 for line 1, _1 for line 2)
        /// </summary>
        public bool UseFolderSuffix { get; set; } = false;

        /// <summary>
        /// NIR path for monitoring
        /// </summary>
        public string NirPath { get; set; } = "";

        /// <summary>
        /// Normal path for monitoring
        /// </summary>
        public string NormalPath { get; set; } = "";

        /// <summary>
        /// Camera 1 path
        /// </summary>
        public string Camera1Path { get; set; } = "";

        /// <summary>
        /// Camera 2 path
        /// </summary>
        public string Camera2Path { get; set; } = "";

        /// <summary>
        /// Camera 3 path
        /// </summary>
        public string Camera3Path { get; set; } = "";

        /// <summary>
        /// Camera 4 path
        /// </summary>
        public string Camera4Path { get; set; } = "";

        /// <summary>
        /// Camera 5 path
        /// </summary>
        public string Camera5Path { get; set; } = "";

        /// <summary>
        /// Camera 6 path
        /// </summary>
        public string Camera6Path { get; set; } = "";

        /// <summary>
        /// Get camera path by number (1-6)
        /// </summary>
        public string GetCameraPath(int cameraNumber)
        {
            return cameraNumber switch
            {
                1 => Camera1Path,
                2 => Camera2Path,
                3 => Camera3Path,
                4 => Camera4Path,
                5 => Camera5Path,
                6 => Camera6Path,
                _ => ""
            };
        }
    }

    /// <summary>
    /// File type enumeration for group updates
    /// </summary>
    public enum FileType
    {
        Normal,
        Normal2,
        Nir,
        Nir2,
        Cam1,
        Cam2,
        Cam3,
        Cam4,
        Cam5,
        Cam6
    }
}
