using System.Collections.Immutable;
using System.Text.Json;
using static UiAutomation.Commands.ExitCodes;

namespace UiAutomation.Commands;

/// <summary>
/// Validation logic for dry-run mode.
/// Validates skill names and argument schemas without executing commands.
/// </summary>
public static class DryRunValidator
{
    private const string SkillsRegistryPath = ".claude/agents/test-executor-skills.md";
    private static ImmutableArray<string>? _cachedSkills;

    /// <summary>
    /// Validates a skill name against the registry.
    /// </summary>
    /// <param name="skillName">Skill name to validate</param>
    /// <returns>Validation result with error if skill not found</returns>
    public static ValidationResult ValidateSkill(string skillName)
    {
        var skills = GetRegisteredSkills();

        if (!skills.Any(s => string.Equals(s, skillName, StringComparison.Ordinal)))
        {
            // Find similar skills for suggestion
            var suggestions = FindSimilarSkills(skillName, skills).Take(3).ToArray();
            var suggestionText = suggestions.Length > 0
                ? $"Did you mean: {string.Join(", ", suggestions)}? See {SkillsRegistryPath}"
                : $"See {SkillsRegistryPath} for complete skill registry.";

            return ValidationResult.Error(
                $"Unknown skill: {skillName}",
                INVALID_ARGUMENT,
                suggestionText
            );
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates argument types against schema.
    /// </summary>
    /// <param name="args">Arguments to validate (JSON string)</param>
    /// <param name="schema">Expected schema (for future expansion)</param>
    /// <returns>Validation result with error if invalid</returns>
    public static ValidationResult ValidateArguments(string? args, string? schema = null)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            return ValidationResult.Success(); // No args is valid
        }

        try
        {
            // Try to parse as JSON to verify syntax
            JsonDocument.Parse(args);
            return ValidationResult.Success();
        }
        catch (JsonException ex)
        {
            return ValidationResult.Error(
                $"Invalid argument syntax: {ex.Message}",
                INVALID_ARGUMENT,
                "Arguments must be valid JSON. Example: '{\"rows\": [0,1,2]}'"
            );
        }
    }

    /// <summary>
    /// Gets all registered skill names from test-executor-skills.md
    /// </summary>
    private static ImmutableArray<string> GetRegisteredSkills()
    {
        if (_cachedSkills.HasValue)
        {
            return _cachedSkills.Value;
        }

        var skillsBuilder = ImmutableList<string>.Empty;

        try
        {
            if (File.Exists(SkillsRegistryPath))
            {
                var content = File.ReadAllText(SkillsRegistryPath);
                // Parse skill names from markdown tables (| SKILL_NAME | format)
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    content,
                    @"\|\s*([A-Z_][A-Z0-9_]*)\s*\|"
                );

                var uniqueSkills = new HashSet<string>();
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    uniqueSkills.Add(match.Groups[1].Value);
                }

                skillsBuilder = uniqueSkills.ToImmutableList();
            }
        }
        catch
        {
            // If reading fails, return empty list (validation will fail)
        }

        _cachedSkills = skillsBuilder.ToImmutableArray();
        return _cachedSkills.Value;
    }

    /// <summary>
    /// Finds skill names similar to the input using Levenshtein distance.
    /// </summary>
    private static IEnumerable<string> FindSimilarSkills(
        string input,
        ImmutableArray<string> skills)
    {
        var inputUpper = input.ToUpperInvariant();
        var category = inputUpper.Split('_')[0];

        // First, try exact category match
        var categoryMatches = skills
            .Where(s => s.StartsWith(category + "_"))
            .Take(2);

        // Then, find closest by string similarity
        var closestSkills = skills
            .Select(s => new { Skill = s, Distance = LevenshteinDistance(inputUpper, s) })
            .Where(x => x.Distance <= 3 && x.Distance > 0)
            .OrderBy(x => x.Distance)
            .Select(x => x.Skill)
            .Take(3);

        return categoryMatches.Concat(closestSkills).Distinct().Take(3);
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings.
    /// </summary>
    private static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var matrix = new int[a.Length + 1, b.Length + 1];

        for (var i = 0; i <= a.Length; i++) matrix[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) matrix[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }

        return matrix[a.Length, b.Length];
    }
}

/// <summary>
/// Result of validation operation.
/// </summary>
public readonly record struct ValidationResult(
    bool IsValid,
    string? ErrorMessage = null,
    int? ErrorCode = null,
    string? Suggestion = null
)
{
    public static ValidationResult Success() => new(true);
    public static ValidationResult Error(string message, int errorCode, string? suggestion = null)
        => new(false, message, errorCode, suggestion);
}
