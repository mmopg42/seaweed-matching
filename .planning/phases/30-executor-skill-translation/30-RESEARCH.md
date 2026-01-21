# Phase 30: Executor Skill Translation - Research

**Researched:** 2026-01-21
**Domain:** Agent skill parsing, CLI command translation, validation
**Confidence:** HIGH

## Summary

This phase enables the test-executor agent to translate skill names from orchestrator prompts into CLI commands. The executor receives prompts like "Execute skill: APP_LAUNCH with args: {...}" and must parse, validate, and translate to actual CLI execution.

The key challenge is implementing a translation layer that:
1. Parses the structured "Execute skill:" format from orchestrator prompts
2. Validates skill names against the 92-skill registry in `test-executor-skills.md`
3. Provides helpful error messages with suggestions for unknown skills
4. Maps skill names to existing CLI commands without duplicating logic
5. Respects the `retryable` flag and includes skill context in error handling

**Primary recommendation:** Implement skill parsing in the test-executor agent's reasoning flow using regex patterns and a simple in-memory skill registry loaded from the markdown file. Do NOT modify the ui_automation CLI — the translation happens entirely at the agent level.

## Standard Stack

### Core
| Component | Version/Location | Purpose | Why Standard |
|-----------|------------------|---------|--------------|
| test-executor.md | `.claude/agents/test-executor.md` | Agent definition | Already defines CLI execution patterns |
| test-executor-skills.md | `.claude/agents/test-executor-skills.md` | Skill registry | 92 skill definitions with CLI mappings (Phase 28) |
| ui_automation.exe | `skills_scripts/ui_automation/` | CLI tool | Existing CLI with all commands already implemented |
| System.CommandLine | .NET 10.0 built-in | CLI parsing | Already used by ui_automation |
| System.Text.RegularExpressions | .NET 10.0 built-in | Regex parsing | Built-in, no external dependencies |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Soenneker.Utils.Strings.LevenshteinDistance | 3.0.26 | Fuzzy matching for skill suggestions | For "did you mean?" feature on unknown skills |
| System.Text.Json | .NET 10.0 built-in | JSON args parsing | Parse args block from prompts |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Levenshtein library | Simple substring matching | Less helpful suggestions, but zero dependency |
| Parsing markdown file | Hardcoded skill list | Avoids file I/O, but harder to maintain |

**Installation:**
```bash
# No new CLI dependencies needed
# Optional: For fuzzy matching suggestions
dotnet add package Soenneker.Utils.Strings.LevenshteinDistance
```

## Architecture Patterns

### Recommended Translation Flow

```
Orchestrator Prompt
    "Execute skill: APP_LAUNCH with args: {"json": true}"
           ↓
    ┌─────────────────────────────────────┐
    │  test-executor Agent Reasoning      │
    │  1. Parse prompt with regex        │
    │  2. Validate skill name            │
    │  3. Load CLI template from registry│
    │  4. Substitute args                │
    │  5. Execute via Bash tool          │
    └─────────────────────────────────────┘
           ↓
    ui_automation.exe app launch --json
           ↓
    CLI Response → Return to orchestrator
```

### Pattern 1: Skill Name Parsing

Parse the orchestrator prompt to extract skill name and optional args.

**Input format:**
```
Execute skill: SKILL_NAME with args: {"key": "value"}
Execute skill: SKILL_NAME
Execute skill: SKILL_NAME (description: What this does)
```

**Regex pattern:**
```csharp
// Primary pattern: Extract skill name and optional args
var pattern = @"Execute skill:\s+(?<skill>[A-Z_]+)(?:\s+with args:\s+(?<args>\{[^}]*\}))?";
var match = Regex.Match(prompt, pattern);

string skillName = match.Groups["skill"].Value;
string argsJson = match.Groups["args"].Value;
```

**Variations handled:**
- `Execute skill: APP_LAUNCH` → skillName = "APP_LAUNCH", argsJson = ""
- `Execute skill: FILE_OPS_MOVE_ROWS with args: {"rows": [0,1,2]}` → skillName = "FILE_OPS_MOVE_ROWS", argsJson = `{"rows": [0,1,2]}`
- `Execute skill: TOOLBAR_START (description: ...)` → skillName = "TOOLBAR_START", description ignored

### Pattern 2: Skill Registry Lookup

Load skill definitions from `test-executor-skills.md` and validate.

```csharp
// Registry entry format (from markdown)
public class SkillDefinition
{
    public string Name { get; init; }        // e.g., "APP_LAUNCH"
    public string CliTemplate { get; init; } // e.g., "app launch [--json]"
    public string Category { get; init; }    // e.g., "APP"
    public bool Retryable { get; init; }     // From skill metadata
}

// Parse markdown file to build registry
Dictionary<string, SkillDefinition> LoadSkillRegistry(string markdownPath)
{
    var registry = new Dictionary<string, SkillDefinition>();
    string[] lines = File.ReadAllLines(markdownPath);

    foreach (var line in lines)
    {
        // Parse: | SKILL_NAME | description | cli command |
        if (line.StartsWith("| ") && line.Contains("|"))
        {
            var parts = line.Split('|').Select(p => p.Trim()).ToArray();
            if (parts.Length >= 4 && parts[1].All(char.IsUpper))
            {
                registry[parts[1]] = new SkillDefinition
                {
                    Name = parts[1],
                    CliTemplate = parts[3],
                    // Category from section header, retryable from metadata
                };
            }
        }
    }
    return registry;
}
```

### Pattern 3: CLI Template Substitution

Convert skill name + args to CLI command string.

```csharp
string BuildCliCommand(SkillDefinition skill, JsonElement args)
{
    string cli = skill.CliTemplate;

    // Substitute known parameters
    foreach (var property in args.EnumerateObject())
    {
        string value = FormatCliValue(property.Value);
        cli = cli.Replace($"{{{property.Name}}}", value);
    }

    // Always add --json if skill supports it
    if (!cli.Contains("--json") && SupportsJson(skill.Name))
    {
        cli += " --json";
    }

    return $"ui_automation.exe {cli}";
}

string FormatCliValue(JsonElement value)
{
    return value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Array => string.Join(",", value.EnumerateArray().Select(v => v.ToString())),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Number => value.ToString(),
        _ => value.ToString()
    };
}
```

### Anti-Patterns to Avoid

- **Parsing CLI help output:** Don't spawn `ui_automation.exe --help` and parse. Use static registry.
- **Modifying ui_automation code:** The CLI is complete. Translation happens at agent level.
- **Complex nested args:** Keep args flat. Array values map to comma-separated CLI args.
- **Dynamic skill discovery:** Registry is static. Loading at startup is fine.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Levenshtein distance | Custom string edit distance algo | `Soenneker.Utils.Strings.LevenshteinDistance` or [Fastenshtein](https://www.nuget.org/packages/Fastenshtein) | Edge cases, performance, tested |
| JSON parsing | String.Split on braces/commas | `System.Text.Json.JsonDocument` | Handles escaping, types, nested structures |
| Regex for skill names | Complex manual char-by-char parsing | `System.Text.RegularExpressions` | Handles whitespace, variations robustly |

**Key insight:** The agent operates at the prompt level, not code level. Use .NET built-ins for any parsing complexity in future tools, but for this phase the agent does text manipulation using its built-in reasoning.

## Common Pitfalls

### Pitfall 1: Prompt Format Variations

**What goes wrong:** Orchestrator prompts may have slight variations in format.

**Why it happens:** AI models don't always output exact string format.

**How to avoid:**
- Make regex flexible on whitespace (`\s+` not ` `)
- Allow optional parentheses for descriptions
- Accept both single and double quotes in JSON

**Warning signs:**
- Skill name is empty after parsing
- Args JSON fails to parse

**Pattern:**
```csharp
// More permissive regex
var pattern = @"Execute\s+skill:\s*(?<skill>[A-Z_][A-Z0-9_]*)(?:\s+with\s+args:\s*(?<args>\{.*?\}))?";
```

### Pitfall 2: Unknown Skills without Suggestions

**What goes wrong:** User gets "Unknown skill: APP_LAUNCHC" with no guidance.

**Why it happens:** Simple existence check without fuzzy matching.

**How to avoid:**
- Implement Levenshtein distance for top 3 similar skills
- Include suggestions in error response
- Show available skills in same category

**Pattern:**
```csharp
// Error response format
{
  "error": "Unknown skill: APP_LAUNCHC",
  "errorCode": 4,
  "suggestions": ["APP_LAUNCH", "APP_STOP", "APP_RESTART"],
  "validSkills": ["APP_LAUNCH", "APP_STOP", ...],
  "category": "APP"
}
```

### Pitfall 3: Arg Type Mismatches

**What goes wrong:** String args where number expected, or wrong format.

**Why it happens:** Args are passed as JSON, but CLI expects specific format.

**How to avoid:**
- Validate arg types against skill schema
- Format arrays as comma-separated values
- Quote strings with spaces

**Warning signs:**
- CLI returns "Invalid argument" exit code 4
- Command executes but behaves unexpectedly

### Pitfall 4: Missing --json Flag

**What goes wrong:** Executor runs command but can't parse response programmatically.

**Why it happens:** Forgetting to add --json flag for structured output.

**How to avoid:**
- Always append --json unless skill explicitly doesn't support it
- Document which skills support --json in registry

### Pitfall 5: Retry Logic on Non-Retryable Skills

**What goes wrong:** Executor retries DELETE operations causing duplicate failures.

**Why it happens:** Not checking the skill's `retryable` flag.

**How to avoid:**
- Add `retryable` boolean to skill metadata
- Only retry on transient failures if retryable=true
- Document retryable status in registry

## Code Examples

### Skill Parsing from Prompt

```csharp
// Source: Based on orchestrator prompt format in test-orchestrator.md
using System.Text.RegularExpressions;

public class SkillPromptParser
{
    private static readonly Regex SkillRegex = new(
        @"Execute\s+skill:\s*(?<skill>[A-Z_][A-Z0-9_]*)(?:\s+with\s+args:\s*(?<args>\{.*?\}))?",
        RegexOptions.IgnoreCase | RegexOptions.Singleline
    );

    public static (string skillName, string argsJson) Parse(string prompt)
    {
        var match = SkillRegex.Match(prompt);
        if (!match.Success)
        {
            throw new FormatException("Prompt does not match expected format: 'Execute skill: SKILL_NAME [with args: {...}]'");
        }

        string skillName = match.Groups["skill"].Value.ToUpperInvariant();
        string argsJson = match.Groups["args"].Value;

        return (skillName, argsJson);
    }
}
```

### Validation with Suggestions

```csharp
// Source: Using Fastenshtein library pattern
public class SkillValidator
{
    private readonly Dictionary<string, SkillDefinition> _registry;

    public ValidationResult ValidateSkill(string skillName)
    {
        if (_registry.TryGetValue(skillName, out var definition))
        {
            return ValidationResult.Success(definition);
        }

        // Find similar skills using Levenshtein distance
        var suggestions = _registry.Keys
            .Select(k => (key: k, distance: Levenshtein.Distance(k, skillName)))
            .OrderBy(p => p.distance)
            .Take(3)
            .Select(p => p.key)
            .ToList();

        string category = InferCategory(skillName);
        var validInCategory = _registry.Values
            .Where(s => s.Category == category)
            .Select(s => s.Name)
            .ToList();

        return ValidationResult.Failure(
            $"Unknown skill: {skillName}",
            suggestions,
            validInCategory,
            category
        );
    }
}
```

### CLI Command Construction

```csharp
// Source: Based on CLI structure in ui_automation/Commands/
public class CliBuilder
{
    public string BuildCommand(SkillDefinition skill, JsonElement args)
    {
        string cli = skill.CliTemplate;

        // Replace parameter placeholders: {rows}, {key}, etc.
        foreach (var prop in args.EnumerateObject())
        {
            string placeholder = $"{{{prop.Name}}}";
            if (cli.Contains(placeholder))
            {
                cli = cli.Replace(placeholder, FormatValue(prop.Value));
            }
            else if (IsRequiredParam(skill, prop.Name))
            {
                throw new ArgumentException($"Required parameter '{prop.Name}' not found in CLI template");
            }
        }

        // Auto-add --json if supported
        if (!cli.Contains("--json") && skill.SupportsJson)
        {
            cli += " --json";
        }

        return $"ui_automation.exe {cli}";
    }

    private static string FormatValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => $"\"{value.GetString()}\"",
            JsonValueKind.Array => string.Join(",", value.EnumerateArray().Select(v => v.ToString())),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => value.ToString()
        };
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Direct CLI commands in prompts | Skill-based intent names | Phase 29 | Orchestrator no longer needs to know CLI syntax |
| CLI syntax knowledge distributed | Centralized in test-executor-skills.md | Phase 28 | Single source of truth for all 92 skills |
| Executor constructs CLI manually | Parser reads registry and translates | Phase 30 (this) | Maintains separation, enables validation |

**Deprecated/outdated:**
- Orchestrator constructing CLI commands directly: Replaced by skill delegation pattern
- test-executor executing arbitrary CLI: Now restricted to skills from registry

## Open Questions

1. **Retryable flag storage**
   - What we know: `retryable` flag needs to be stored per skill
   - What's unclear: Exact format in test-executor-skills.md (add new column? embedded in metadata?)
   - Recommendation: Add `retryable: boolean` field to each skill's TypeScript interface in the registry

2. **Registry loading strategy**
   - What we know: Need to load 92 skills from markdown file
   - What's unclear: Load once at agent start, or lazy load on demand?
   - Recommendation: Load once and cache — registry is small (~2000 lines)

3. **Exit code mapping**
   - What we know: ui_automation uses ExitCodes (0=success, 1=error, 2=not_found, 3=timeout, 4=invalid_arg)
   - What's unclear: Should executor add ExitCode.UnknownSkill (value 4 conflicts)?
   - Recommendation: Executor validates BEFORE calling CLI, so unknown skill never reaches CLI. Keep existing exit codes.

4. **Skills with similar names**
   - What we know: Some skills are very similar (FILE_OPS_SELECT_ALL vs FILE_OPS_SELECT_GROUP_ID)
   - What's unclear: Should suggestions prioritize same-category skills?
   - Recommendation: Yes, show category matches first, then all skills

## Sources

### Primary (HIGH confidence)
- [test-executor-skills.md](C:\workspace\seaweed\gui_kiro_v2\.claude\agents\test-executor-skills.md) - Complete 92-skill registry with CLI mappings
- [test-orchestrator.md](C:\workspace\seaweed\gui_kiro_v2\.claude\agents\test-orchestrator.md) - Orchestrator delegation patterns
- [test-executor.md](C:\workspace\seaweed\gui_kiro_v2\.claude\agents\test-executor.md) - Executor agent definition and CLI patterns
- [AppLifecycleCommands.cs](C:\workspace\seaweed\gui_kiro_v2\skills_scripts\ui_automation\Commands\AppLifecycleCommands.cs) - Example CLI command structure
- [ExitCodes.cs](C:\workspace\seaweed\gui_kiro_v2\skills_scripts\ui_automation\Commands\ExitCodes.cs) - Exit code definitions

### Secondary (MEDIUM confidence)
- [C# RegEx string extraction (Stack Overflow)](https://stackoverflow.com/questions/9436381/c-sharp-regex-string-extraction) - Regex patterns for extracting skill names
- [.NET Regular Expressions Documentation](https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions) - Official regex documentation
- [System.CommandLine Documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/get-started-tutorial) - CLI parsing patterns

### Tertiary (LOW confidence)
- [Fastenshtein NuGet package](https://www.nuget.org/packages/Fastenshtein) - Levenshtein distance library (not verified, suggested alternative)
- [FuzzySearch.Net](https://www.nuget.org/packages/FuzzySearch.Net) - Alternative fuzzy matching (not verified)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All components are existing, in-house code
- Architecture: HIGH - Translation pattern is straightforward text manipulation
- Pitfalls: MEDIUM - Based on common agent/cli integration patterns, unverified in this specific context

**Research date:** 2026-01-21
**Valid until:** 30 days (stable domain, but agent prompt patterns may evolve)
