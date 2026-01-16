using Xunit;
using FsCheck;
using FsCheck.Xunit;
using System.Reflection;

namespace ChronoView.Tests;

/// <summary>
/// Property-based tests for project structure validation
/// **Feature: python-gui-to-csharp-migration, Property 14: Architecture Documentation Accuracy**
/// **Validates: Requirements 6.1**
/// </summary>
public class ProjectStructureTests
{
    /// <summary>
    /// Property: For any module in the ChronoView assembly, the namespace should follow
    /// the documented architecture pattern (ChronoView.Core.*, ChronoView.UI.*, etc.)
    /// </summary>
    [Property(MaxTest = 100)]
    public void AllTypesFollowNamespaceConvention()
    {
        var assembly = typeof(App).Assembly;
        var types = assembly.GetTypes();

        var invalidTypes = types.Where(type =>
        {
            if (type.Namespace == null)
                return false; // Skip types without namespace

            var ns = type.Namespace;

            // WPF/XAML 빌드 과정에서 자동 생성되는 네임스페이스는 아키텍처 레이어 규칙 대상에서 제외
            if (ns.StartsWith("XamlGeneratedNamespace", StringComparison.Ordinal))
                return false;

            // Valid namespace patterns according to design document
            var validPrefixes = new[]
            {
                "ChronoView.Core",
                "ChronoView.UI",
                "ChronoView.Infrastructure",
                "ChronoView.Models",
                "ChronoView" // Root namespace for App and MainWindow
            };

            return !validPrefixes.Any(prefix => ns.Equals(prefix) || ns.StartsWith(prefix + "."));
        }).ToList();

        Assert.Empty(invalidTypes);
    }

    /// <summary>
    /// Property: For any namespace in the assembly, it should map to one of the documented
    /// architectural layers (Core, UI, Infrastructure, Models)
    /// </summary>
    [Property(MaxTest = 100)]
    public void AllNamespacesMapToDocumentedLayers()
    {
        var assembly = typeof(App).Assembly;
        var namespaces = assembly.GetTypes()
            .Where(t => t.Namespace != null)
            .Select(t => t.Namespace!)
            .Distinct()
            .ToList();

        // WPF/XAML 자동 생성 네임스페이스 제외
        namespaces = namespaces
            .Where(ns => !ns.StartsWith("XamlGeneratedNamespace", StringComparison.Ordinal))
            .ToList();

        // According to design document, these are the valid architectural layers
        var validLayers = new[]
        {
            "ChronoView", // Root
            "ChronoView.Core",
            "ChronoView.Core.Configuration",
            "ChronoView.Core.FileWatching",
            "ChronoView.Core.ImageProcessing",
            "ChronoView.Core.FileMatching",
            "ChronoView.Core.NIR",
            "ChronoView.Core.FileOperations",
            "ChronoView.Core.Analytics",
            "ChronoView.UI",
            "ChronoView.UI.ViewModels",
            "ChronoView.UI.Views",
            "ChronoView.UI.Controls",
            "ChronoView.Infrastructure",
            "ChronoView.Models"
        };

        var invalidNamespaces = namespaces.Where(ns =>
            !validLayers.Any(layer => ns.Equals(layer) || ns.StartsWith(layer + "."))).ToList();

        Assert.Empty(invalidNamespaces);
    }

    /// <summary>
    /// Property: The assembly should have dependency injection configured
    /// (verified by checking that App class has a Services property)
    /// </summary>
    [Fact]
    public void AppClassHasServicesProperty()
    {
        var appType = typeof(App);
        var servicesProperty = appType.GetProperty("Services", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(servicesProperty);
        Assert.Equal(typeof(IServiceProvider), servicesProperty.PropertyType);
    }

    /// <summary>
    /// Property: The assembly should have logging configured
    /// (verified by checking that MainWindow accepts ILogger in constructor)
    /// </summary>
    [Fact]
    public void MainWindowAcceptsLoggerInConstructor()
    {
        var mainWindowType = typeof(MainWindow);
        var constructors = mainWindowType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        var hasLoggerConstructor = constructors.Any(c =>
        {
            var parameters = c.GetParameters();
            return parameters.Any(p => p.ParameterType.IsGenericType &&
                                      p.ParameterType.GetGenericTypeDefinition().Name.Contains("ILogger"));
        });

        Assert.True(hasLoggerConstructor, "MainWindow should have a constructor that accepts ILogger");
    }

    /// <summary>
    /// Property: Required Microsoft.Extensions packages should be referenced
    /// </summary>
    [Fact]
    public void RequiredMicrosoftExtensionsPackagesAreReferenced()
    {
        var assembly = typeof(App).Assembly;
        var referencedAssemblies = assembly.GetReferencedAssemblies();

        // Check for Microsoft.Extensions packages (DependencyInjection and Logging)
        var hasMicrosoftExtensions = referencedAssemblies.Any(a => 
            a.Name != null && a.Name.Contains("Microsoft.Extensions"));

        Assert.True(hasMicrosoftExtensions, 
            "Microsoft.Extensions packages (DependencyInjection, Logging) should be referenced");
    }
}
