using System;
using System.Collections.Generic;
using System.Linq;

namespace ChronoView.Models
{
    /// <summary>
    /// Data type enumeration for file sequence configuration
    /// Cam1-3: Line 1 cameras, Cam4-6: Line 2 cameras
    /// Settings UI shows only Cam1-3 (Cam4-6 auto-mapped for Line 2)
    /// </summary>
    public enum DataType
    {
        NIR,
        Normal,
        Cam1,   // Line 1: Cam1
        Cam2,   // Line 1: Cam2
        Cam3,   // Line 1: Cam3
        Cam4,   // Line 2: Auto-mapped from Cam1
        Cam5,   // Line 2: Auto-mapped from Cam2
        Cam6,   // Line 2: Auto-mapped from Cam3
        Camera  // Generic camera category
    }

    /// <summary>
    /// Configuration for data arrival sequence
    /// </summary>
    public class DataSequenceSettings
    {
        public List<DataSequenceItem> Sequence { get; set; } = new List<DataSequenceItem>();

        /// <summary>
        /// If true, Cam2/Cam3 use Cam1 (and Cam5/6 use Cam4) as the timestamp reference
        /// instead of the immediately preceding sequence item.
        /// </summary>
        public bool CompareToReferenceCamera { get; set; } = true;

        /// <summary>
        /// Get configuration item for a specific data type
        /// </summary>
        public DataSequenceItem? GetByType(DataType type)
        {
            if (Sequence == null || Sequence.Count == 0)
                return null;

            return Sequence.FirstOrDefault(item => item.Type == type);
        }

        /// <summary>
        /// Get the order (priority) for a specific data type
        /// </summary>
        public int GetOrder(DataType type)
        {
            return GetByType(type)?.Order ?? int.MaxValue;
        }

        /// <summary>
        /// Get minimum delay in seconds for a data type
        /// </summary>
        public double GetMinDelay(DataType type)
        {
            var item = GetByType(type);
            return item?.MinDelaySeconds ?? 0; // Default: no minimum delay
        }

        /// <summary>
        /// Get maximum delay in seconds for a data type
        /// </summary>
        public double GetMaxDelay(DataType type)
        {
            var item = GetByType(type);
            return item?.MaxDelaySeconds ?? 10; // Default:  10 seconds
        }

        /// <summary>
        /// Get ordered list of enabled data types
        /// </summary>
        public List<DataType> GetOrderedTypes()
        {
            if (Sequence == null || Sequence.Count == 0)
                return new List<DataType>();

            return Sequence
                .Where(item => item.Enabled)
                .OrderBy(item => item.Order)
                .Select(item => item.Type)
                .ToList();
        }

        /// <summary>
        /// Get ordered list of enabled data sequence items with full details
        /// </summary>
        public List<DataSequenceItem> GetOrderedItems()
        {
            if (Sequence == null || Sequence.Count == 0)
                return new List<DataSequenceItem>();

            return Sequence
                .Where(item => item.Enabled)
                .OrderBy(item => item.Order)
                .ToList();
        }

        /// <summary>
        /// Get ordered list of ALL data sequence items (both enabled and disabled).
        /// Enabled flag controls matching STRATEGY, not inclusion.
        /// </summary>
        public List<DataSequenceItem> GetAllOrderedItems()
        {
            if (Sequence == null || Sequence.Count == 0)
                return new List<DataSequenceItem>();

            return Sequence
                .OrderBy(item => item.Order)
                .ToList();
        }

        /// <summary>
        /// Get ordered list of ALL data types (both enabled and disabled).
        /// </summary>
        public List<DataType> GetAllOrderedTypes()
        {
            if (Sequence == null || Sequence.Count == 0)
                return new List<DataType>();

            return Sequence
                .OrderBy(item => item.Order)
                .Select(item => item.Type)
                .ToList();
        }

        /// <summary>
        /// Check if a data type uses sequence-based matching (Enabled=true)
        /// or timestamp-only matching (Enabled=false)
        /// </summary>
        public bool IsSequenceMatching(DataType type)
        {
            var item = GetByType(type);
            return item != null && item.Enabled;  // null guard: return false if type not found
        }

        /// <summary>
        /// Validate sequence configuration
        /// </summary>
        /// <param name="errors">Output list of validation errors</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (Sequence == null || Sequence.Count == 0)
            {
                errors.Add("Sequence cannot be empty");
                return false;
            }

            // Rule 1: Sequence items must exist (Enabled status only affects matching strategy, not validity)
            // All items can be disabled (timestamp-only matching mode for all types)

            // Rule 2: Unique Order values
            var orderGroups = Sequence.GroupBy(item => item.Order);
            foreach (var group in orderGroups)
            {
                if (group.Count() > 1)
                {
                    errors.Add($"Duplicate Order value: {group.Key}");
                }
            }

            // Rule 2: Unique Type values
            var typeGroups = Sequence.GroupBy(item => item.Type);
            foreach (var group in typeGroups)
            {
                if (group.Count() > 1)
                {
                    errors.Add($"Duplicate DataType: {group.Key}");
                }
            }

            // Rule 3: Valid delay ranges
            foreach (var item in Sequence)
            {
                if (item.MinDelaySeconds < 0)
                {
                    errors.Add($"{item.Type}: MinDelay must be >= 0");
                }
                if (item.MaxDelaySeconds < 0)
                {
                    errors.Add($"{item.Type}: MaxDelay must be >= 0");
                }
                if (item.MinDelaySeconds > item.MaxDelaySeconds)
                {
                    errors.Add($"{item.Type}: MinDelay ({item.MinDelaySeconds}) must be <= MaxDelay ({item.MaxDelaySeconds})");
                }
            }

            return errors.Count == 0;
        }
    }

    /// <summary>
    /// Single item in the data sequence configuration
    /// </summary>
    public class DataSequenceItem
    {
        /// <summary>
        /// Type of data (NIR, Normal, Camera)
        /// </summary>
        public DataType Type { get; set; }

        /// <summary>
        /// Order in sequence (1-based, lower = earlier)
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Minimum delay in seconds from previous data type
        /// Example: For Camera1 after Normal, MinDelay=4 means "Normal + 4 seconds minimum"
        /// </summary>
        public double MinDelaySeconds { get; set; }

        /// <summary>
        /// Maximum delay in seconds from previous data type
        /// Example: For Camera1 after Normal, MaxDelay=6 means "Normal + 6 seconds maximum"
        /// Combined with MinDelay: Camera1 must arrive between Normal+4s and Normal+6s
        /// </summary>
        public double MaxDelaySeconds { get; set; }

        /// <summary>
        /// Whether this data type is enabled in the sequence
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
