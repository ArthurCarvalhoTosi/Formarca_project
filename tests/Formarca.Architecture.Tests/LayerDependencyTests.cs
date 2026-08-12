using NetArchTest.Rules;
using Xunit;

namespace Formarca.Architecture.Tests;

public class LayerDependencyTests
{
    private const string Domain = "Formarca.Domain";
    private const string Application = "Formarca.Application";
    private const string Infrastructure = "Formarca.Infrastructure";
    private const string Web = "Formarca.Web";

    [Fact]
    public void Domain_Should_Not_Depend_On_Other_Layers()
    {
        var result = Types.InAssembly(typeof(Formarca.Domain.Common.Entity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(Application, Infrastructure, Web)
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Web()
    {
        var result = Types.InAssembly(typeof(Formarca.Application.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Web)
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Web()
    {
        var result = Types.InAssembly(typeof(Formarca.Infrastructure.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOn(Web)
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    private static string Format(TestResult result)
    {
        if (result.FailingTypes is null || !result.FailingTypes.Any())
        {
            return "Architecture rule failed.";
        }

        return "Violations: " + string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
