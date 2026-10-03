using System.Xml.Linq;

namespace UltimateHospital.ArchitectureTests;

public sealed class ProjectBoundaryTests
{
    private static readonly string Root = FindRoot();
    private static readonly string[] SourceFolders = ["main", "src", "tests", "modules"];

    [Fact]
    public void ActualSolutionContainsAllSevenProjectsWithOnlyPlannedEdges()
    {
        var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["UltimateHospital.RuntimeInfrastructure"] = [],
            ["UltimateHospital.ServiceDefaults"] = [],
            ["UltimateHospital.HttpApi.Host"] = ["UltimateHospital.RuntimeInfrastructure", "UltimateHospital.ServiceDefaults"],
            ["UltimateHospital.DbMigrator"] = ["UltimateHospital.RuntimeInfrastructure", "UltimateHospital.ServiceDefaults"],
            ["UltimateHospital.AppHost"] = ["UltimateHospital.DbMigrator", "UltimateHospital.HttpApi.Host"],
            ["UltimateHospital.ArchitectureTests"] = [],
            ["UltimateHospital.IntegrationTests"] = ["UltimateHospital.AppHost", "UltimateHospital.RuntimeInfrastructure"]
        };
        var solution = XDocument.Load(Path.Combine(Root, "UltimateHospital.slnx"));
        var paths = solution.Descendants("Project").Select(p =>
            Path.GetFullPath(Path.Combine(Root, p.Attribute("Path")!.Value))).ToArray();
        Assert.Equal(expected.Count, paths.Length);
        foreach (var path in paths)
        {
            Assert.True(File.Exists(path), $"Missing solution project: {path}");
            var name = Path.GetFileNameWithoutExtension(path);
            Assert.True(expected.ContainsKey(name), $"Unclassified project: {name}");
            var project = XDocument.Load(path);
            var references = project.Descendants("ProjectReference")
                .Select(r => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, r.Attribute("Include")!.Value)))
                .ToArray();
            Assert.All(references, reference => Assert.Contains(reference, paths));
            Assert.Equal(expected[name].Order(StringComparer.Ordinal),
                references.Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal));
        }
        var discovered = SourceFolders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(Root, folder), "*.csproj", SearchOption.AllDirectories))
            .Where(p => !p.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"))
            .Select(Path.GetFullPath).Order(StringComparer.Ordinal);
        Assert.Equal(paths.Order(StringComparer.Ordinal), discovered);
    }

    [Fact]
    public void ActualProductionProjectsDoNotReferenceTestPackagesOrFakeDatabaseProviders()
    {
        foreach (var folder in new[] { "main", "src" })
        {
            foreach (var path in Directory.EnumerateFiles(Path.Combine(Root, folder), "*.csproj", SearchOption.AllDirectories))
            {
                var packages = XDocument.Load(path).Descendants("PackageReference")
                    .Select(p => p.Attribute("Include")!.Value);
                Assert.DoesNotContain(packages, p => p.StartsWith("xunit", StringComparison.OrdinalIgnoreCase)
                    || p.Contains("Testing", StringComparison.OrdinalIgnoreCase)
                    || p.Contains("InMemory", StringComparison.OrdinalIgnoreCase)
                    || p.Contains("Sqlite", StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Theory]
    [InlineData(ProjectKind.Module, "patients", ProjectKind.Module, "facilities", "runtime-to-runtime")]
    [InlineData(ProjectKind.Module, "patients", ProjectKind.Host, "host", "module-to-host")]
    [InlineData(ProjectKind.Contract, "patients", ProjectKind.Module, "patients", "contract-to-runtime")]
    [InlineData(ProjectKind.Infrastructure, "runtime", ProjectKind.Test, "tests", "production-to-test")]
    public void ForbiddenDependencyFixtureIsRejected(ProjectKind sourceKind, string sourceOwner,
        ProjectKind targetKind, string targetOwner, string expectedViolation)
    {
        Assert.Equal(expectedViolation, BoundaryViolation(sourceKind, sourceOwner, targetKind, targetOwner));
    }

    [Fact]
    public void PublishedContractIsAnAllowedCrossModuleDoorway()
    {
        Assert.Null(BoundaryViolation(ProjectKind.Module, "patients", ProjectKind.Contract, "facilities"));
    }

    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Volo.Abp.EntityFrameworkCore")]
    [InlineData("Microsoft.AspNetCore.Mvc.Core")]
    [InlineData("Npgsql")]
    public void ContractsCannotPublishPersistenceOrHostDependencies(string package)
    {
        Assert.False(IsPureContractPackage(package));
    }

    private static string? BoundaryViolation(ProjectKind source, string sourceOwner, ProjectKind target, string targetOwner)
    {
        if (source != ProjectKind.Test && target == ProjectKind.Test)
        {
            return "production-to-test";
        }
        if (source == ProjectKind.Module && target == ProjectKind.Module)
        {
            // Runtime isolation applies even within one module's technical layer split.
            return "runtime-to-runtime";
        }
        if (source == ProjectKind.Module && target == ProjectKind.Host)
        {
            return "module-to-host";
        }
        if (source == ProjectKind.Contract && target != ProjectKind.Contract)
        {
            return "contract-to-runtime";
        }
        _ = sourceOwner;
        _ = targetOwner;
        return null;
    }

    private static bool IsPureContractPackage(string package) =>
        !package.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase)
        && !package.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase)
        && !package.StartsWith("Npgsql", StringComparison.OrdinalIgnoreCase);

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UltimateHospital.slnx")))
            {
                return directory.FullName;
            }
        }
        throw new DirectoryNotFoundException("Repository solution was not found; architecture tests cannot evaluate the source.");
    }

    public enum ProjectKind
    {
        Host,
        Module,
        Contract,
        Infrastructure,
        Test
    }
}
