using ChronoView.Models;

namespace ChronoView.Core.Nir;

/// <summary>
/// NIR spectrum filtering logic using 5-criteria scoring system.
/// Detects seaweed presence based on comprehensive statistical analysis.
/// </summary>
public static class NirSpectrumFilter
{
    /// <summary>
    /// Result of individual criterion evaluation.
    /// </summary>
    public class CriteriaResult
    {
        public bool Passed { get; set; }
        public double Value { get; set; }
        public double Threshold { get; set; }
    }

    /// <summary>
    /// Result of spectrum filtering analysis with detailed scoring.
    /// </summary>
    public class FilterResult
    {
        public bool PassesFilter { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CriteriaPassed { get; set; } = 0;
        public int CriteriaTotal { get; set; } = 5;
        public Dictionary<string, CriteriaResult>? CriteriaDetails { get; set; } = new();
    }

    /// <summary>
    /// Analyzes a NIR spectrum file and determines if it passes the filter criteria.
    /// </summary>
    /// <param name="txtFilePath">Path to the .txt spectrum file</param>
    /// <returns>FilterResult containing pass/fail status and detailed scoring</returns>
    public static FilterResult AnalyzeSpectrum(string txtFilePath)
    {
        var result = new FilterResult();

        try
        {
            // Parse the spectrum file
            var spectrum = NirSpectrumParser.Parse(txtFilePath);
            if (spectrum == null || !spectrum.IsValid())
            {
                result.Message = "Failed to parse spectrum or insufficient data";
                return result;
            }

            // Evaluate using 5-criteria algorithm
            result = EvaluateSeaweedPresence(spectrum.Wavelengths, spectrum.Intensities);
            return result;
        }
        catch (Exception ex)
        {
            result.Message = $"Analysis error: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Evaluates seaweed presence using 5-criteria scoring system.
    /// Based on Python reference implementation (test_folder.py).
    /// </summary>
    private static FilterResult EvaluateSeaweedPresence(double[] wavelengths, double[] intensities)
    {
        // Filter data to X range: 4500 ~ 6500
        var filteredData = new List<(double X, double Y)>();
        for (int i = 0; i < wavelengths.Length; i++)
        {
            if (wavelengths[i] >= 4500 && wavelengths[i] <= 6500)
            {
                filteredData.Add((wavelengths[i], intensities[i]));
            }
        }

        if (filteredData.Count == 0)
        {
            return new FilterResult
            {
                PassesFilter = false,
                Message = "x 범위 부족 (4500-6500)",
                CriteriaPassed = 0,
                CriteriaTotal = 5
            };
        }

        // Extract Y values
        var yValues = filteredData.Select(d => d.Y).ToArray();

        // Calculate global metrics
        double y_range = yValues.Max() - yValues.Min();
        double y_std = CalculateStandardDeviation(yValues);

        // Sliding window analysis (window=800, stride=100)
        const double windowSize = 800;
        const double stride = 100;

        var windowRanges = new List<double>();
        double xMin = filteredData.Min(d => d.X);
        double xMax = filteredData.Max(d => d.X);

        double currentX = xMin;
        while (currentX + windowSize <= xMax)
        {
            // Get Y values in current window
            var windowYValues = filteredData
                .Where(d => d.X >= currentX && d.X <= currentX + windowSize)
                .Select(d => d.Y)
                .ToList();

            if (windowYValues.Count > 0)
            {
                double yMinWindow = windowYValues.Min();
                double yMaxWindow = windowYValues.Max();
                double range = yMaxWindow - yMinWindow;
                windowRanges.Add(range);
            }

            currentX += stride;
        }

        if (windowRanges.Count == 0)
        {
            return new FilterResult
            {
                PassesFilter = false,
                Message = "window 분석 실패",
                CriteriaPassed = 0,
                CriteriaTotal = 5
            };
        }

        // Calculate window metrics
        double window_800_mean = windowRanges.Average();
        double window_800_std = CalculateStandardDeviation(windowRanges.ToArray());
        double window_800_max = windowRanges.Max();

        // Evaluate 5 criteria
        var criteria = new Dictionary<string, CriteriaResult>
        {
            ["y_range"] = new CriteriaResult
            {
                Value = y_range,
                Threshold = 0.035,
                Passed = y_range >= 0.035
            },
            ["y_std"] = new CriteriaResult
            {
                Value = y_std,
                Threshold = 0.010,
                Passed = y_std >= 0.010
            },
            ["window_800_mean"] = new CriteriaResult
            {
                Value = window_800_mean,
                Threshold = 0.025,
                Passed = window_800_mean >= 0.025
            },
            ["window_800_std"] = new CriteriaResult
            {
                Value = window_800_std,
                Threshold = 0.010,
                Passed = window_800_std >= 0.010
            },
            ["window_800_max"] = new CriteriaResult
            {
                Value = window_800_max,
                Threshold = 0.035,
                Passed = window_800_max >= 0.035
            }
        };

        // Calculate score
        int passedCount = criteria.Count(c => c.Value.Passed);
        const int totalCount = 5;

        // Determine pass/fail (threshold: 4 out of 5)
        bool passesFilter = passedCount >= 4;

        string message = passesFilter
            ? $"김 있음 ({passedCount}/{totalCount} 기준 통과)"
            : $"김 없음 ({passedCount}/{totalCount} 기준만 통과)";

        return new FilterResult
        {
            PassesFilter = passesFilter,
            Message = message,
            CriteriaPassed = passedCount,
            CriteriaTotal = totalCount,
            CriteriaDetails = criteria
        };
    }

    /// <summary>
    /// Calculates standard deviation of a dataset.
    /// </summary>
    private static double CalculateStandardDeviation(double[] values)
    {
        if (values.Length == 0)
            return 0;

        double mean = values.Average();
        double sumOfSquaredDifferences = values.Sum(v => Math.Pow(v - mean, 2));
        double variance = sumOfSquaredDifferences / values.Length;
        return Math.Sqrt(variance);
    }
}
