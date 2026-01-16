using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ChronoView.Helpers
{
    /// <summary>
    /// Centralized helper for Normal folder validation and line number determination.
    /// Used to eliminate duplicated suffix/path logic across components.
    /// </summary>
    public static class NormalFolderHelper
    {
        // Normal folder pattern: C + YYMMDD + T + HHMMSS (suffix is optional)
        // Example: C251216T214727 or C251216T214727_0
        private static readonly Regex BasicPatternRegex = new Regex(
            @"^C\d{6}T\d{6}(_\d+)?$", 
            RegexOptions.Compiled);

        /// <summary>
        /// Determines line number based on suffix or path.
        /// </summary>
        /// <param name="folderPath">Full path or folder name</param>
        /// <param name="useSuffix">If true, check _0/_1 suffix first</param>
        /// <param name="normal1Path">Path for Line 1 Normal folders</param>
        /// <param name="normal2Path">Path for Line 2 Normal folders</param>
        /// <returns>1 for Line 1, 2 for Line 2</returns>
        public static int DetermineLineNumber(
            string folderPath,
            bool useSuffix,
            string? normal1Path,
            string? normal2Path)
        {
            if (string.IsNullOrEmpty(folderPath))
                return 1;

            var folderName = Path.GetFileName(folderPath);

            // Suffix-based determination (if enabled)
            if (useSuffix)
            {
                if (folderName.EndsWith("_0"))
                    return 1;
                if (folderName.EndsWith("_1"))
                    return 2;
                // Suffix not found, fallback to path
            }

            // Path-based determination (fallback or primary if useSuffix=false)
            var parentDir = Path.GetDirectoryName(folderPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                if (!string.IsNullOrEmpty(normal1Path) && IsSubPath(parentDir, normal1Path))
                    return 1;
                if (!string.IsNullOrEmpty(normal2Path) && IsSubPath(parentDir, normal2Path))
                    return 2;
            }

            // Also check the folder itself (for cases where folderPath IS directly under Normal1/2)
            if (!string.IsNullOrEmpty(normal1Path) && IsSubPath(folderPath, normal1Path))
                return 1;
            if (!string.IsNullOrEmpty(normal2Path) && IsSubPath(folderPath, normal2Path))
                return 2;

            return 1; // Default to Line 1
        }

        /// <summary>
        /// Validates if a folder name matches the Normal folder pattern.
        /// </summary>
        /// <param name="name">Folder name (not path)</param>
        /// <param name="useSuffix">If true, require suffix when expectedLine is specified</param>
        /// <param name="expectedLine">Optional: 1 requires _0, 2 requires _1</param>
        /// <returns>True if valid</returns>
        public static bool IsValidNormalFolder(string name, bool useSuffix, int? expectedLine = null)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            // Basic pattern check
            if (!BasicPatternRegex.IsMatch(name))
                return false;

            // If suffix checking is disabled, accept any valid pattern
            if (!useSuffix)
                return true;

            // Suffix checking enabled
            bool hasZero = name.EndsWith("_0");
            bool hasOne = name.EndsWith("_1");

            if (expectedLine.HasValue)
            {
                // Strict match for expected line
                return expectedLine.Value switch
                {
                    1 => hasZero,
                    2 => hasOne,
                    _ => hasZero || hasOne
                };
            }

            // No expected line specified: require either suffix
            return hasZero || hasOne;
        }

        /// <summary>
        /// Checks if a child path is under a parent path.
        /// </summary>
        private static bool IsSubPath(string child, string parent)
        {
            if (string.IsNullOrEmpty(child) || string.IsNullOrEmpty(parent))
                return false;

            try
            {
                var normalizedParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) 
                                       + Path.DirectorySeparatorChar;
                var normalizedChild = Path.GetFullPath(child).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) 
                                      + Path.DirectorySeparatorChar;

                return normalizedChild.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
