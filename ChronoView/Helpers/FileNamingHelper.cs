using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ChronoView.Helpers
{
    /// <summary>
    /// Utility class for file naming conventions and timestamp extraction
    /// </summary>
    public static class FileNamingHelper
    {
        // Normal folder pattern: C + YYMMDD + T + HHMMSS + optional _N suffix
        // Example: C251216T214727 or C251216T214727_0
        private static readonly Regex NormalFolderRegex = new Regex(@"^C(\d{2})(\d{2})(\d{2})T(\d{2})(\d{2})(\d{2})(_\d+)?$", RegexOptions.Compiled);

        // Camera file pattern: YYYYMMDD_HHMMSS_XXX.bmp/jpg/png
        // Example: 20250120_143052_001.jpg
        private static readonly Regex CameraFileRegex = new Regex(@"(\d{8})_(\d{6})", RegexOptions.Compiled);

        // NIR file pattern: run_N + YYYYMMDD + T + HHMMSS
        // Example: run_120251201T140542
        private static readonly Regex NirFileRegex = new Regex(@"(\d{8}T\d{6})", RegexOptions.Compiled);

        /// <summary>
        /// Checks if a folder name matches the Normal camera folder pattern
        /// </summary>
        public static bool IsNormalFolder(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return false;
            return NormalFolderRegex.IsMatch(folderName);
        }

        /// <summary>
        /// Extracts timestamp from a Normal folder name
        /// </summary>
        public static DateTime? ExtractTimestampFromNormalFolderName(string folderName)
        {
            try
            {
                if (string.IsNullOrEmpty(folderName)) return null;

                var match = NormalFolderRegex.Match(folderName);
                if (!match.Success) return null;

                int year = 2000 + int.Parse(match.Groups[1].Value);
                int month = int.Parse(match.Groups[2].Value);
                int day = int.Parse(match.Groups[3].Value);
                int hour = int.Parse(match.Groups[4].Value);
                int minute = int.Parse(match.Groups[5].Value);
                int second = int.Parse(match.Groups[6].Value);

                return new DateTime(year, month, day, hour, minute, second);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Alias for ExtractTimestampFromNormalFolderName for broader compatibility
        /// </summary>
        public static DateTime? ExtractTimestampFromFolderName(string folderPath)
        {
            return ExtractTimestampFromNormalFolderName(Path.GetFileName(folderPath));
        }

        /// <summary>
        /// Extracts timestamp from a camera file name
        /// </summary>
        public static DateTime? ExtractTimestampFromCameraFileName(string fileName)
        {
            try
            {
                var match = CameraFileRegex.Match(fileName);
                if (!match.Success) return null;

                if (DateTime.TryParseExact(
                    $"{match.Groups[1].Value}_{match.Groups[2].Value}",
                    "yyyyMMdd_HHmmss",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out var dt))
                {
                    return dt;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extracts timestamp from a NIR file name
        /// </summary>
        public static DateTime? ExtractTimestampFromNirFileName(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName)) return null;

                var match = NirFileRegex.Match(fileName);
                if (!match.Success) return null;

                if (DateTime.TryParseExact(
                    match.Groups[1].Value,
                    "yyyyMMddTHHmmss",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out var dt))
                {
                    return dt;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if a file is the stitched original image
        /// </summary>
        public static bool IsStitchedImage(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            return fileName.Equals("stitched_original.png", StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// Generic timestamp extraction based on FileType
        /// </summary>
        public static DateTime? ExtractTimestamp(string filePath, string fileType)
        {
            try
            {
                if (fileType == "Nir")
                {
                    var fileName = Path.GetFileName(filePath);
                    return ExtractTimestampFromNirFileName(fileName);
                }
                
                if (fileType == "Normal")
                {
                    return ExtractTimestampFromFolderName(filePath);
                }
                
                if (fileType == "Camera")
                {
                    var fileName = Path.GetFileName(filePath);
                    return ExtractTimestampFromCameraFileName(fileName);
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
