using NetArchTest.Rules;
using Xunit;

namespace BookingSystem.UnitTests.Architecture;

/// <summary>
/// Architecture tests to enforce Clean Architecture principles and naming conventions.
/// These tests validate layer dependencies and ensure the codebase follows established patterns.
/// </summary>
public class ArchitectureTests
{
    private const string DomainNamespace = "BookingSystem.Domain";
    private const string ApplicationNamespace = "BookingSystem.Application";
    private const string InfrastructureNamespace = "BookingSystem.Infrastructure";
    private const string ApiNamespace = "BookingSystem.API";

    #region Layer Dependency Tests

    [Fact]
    public void Domain_Should_NotHaveAnyDependencies()
    {
        // Arrange
        var domainAssembly = typeof(BookingSystem.Domain.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"Domain layer should not depend on any other layer. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Application_Should_OnlyDependOnDomain()
    {
        // Arrange
        var applicationAssembly = typeof(BookingSystem.Application.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"Application layer should only depend on Domain. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Infrastructure_Should_NotDependOnAPI()
    {
        // Arrange
        var infrastructureAssembly = typeof(BookingSystem.Infrastructure.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"Infrastructure layer should not depend on API. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void API_Controllers_Should_NotDirectlyUseRepositories()
    {
        // Arrange
        var apiAssembly = typeof(BookingSystem.API.AssemblyReference).Assembly;

        // Act - Controllers should use MediatR, not repositories directly
        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace("BookingSystem.API.Controllers")
            .ShouldNot()
            .HaveDependencyOn("BookingSystem.Infrastructure.Repositories")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"Controllers should not directly depend on repositories (use MediatR). Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}" );
    }

    #endregion

    #region Naming Convention Tests

    [Fact]
    public void Commands_Should_EndWithCommand()
    {
        // Arrange
        var applicationAssembly = typeof(BookingSystem.Application.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .That()
            .AreClasses()
            .And()
            .ResideInNamespace("BookingSystem.Application.Features")
            .And()
            .ResideInNamespace("Commands")
            .And()
            .DoNotHaveNameMatching(".*Handler$")
            .And()
            .DoNotHaveNameMatching(".*Validator$")
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"All command classes should end with 'Command'. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Queries_Should_EndWithQuery()
    {
        // Arrange
        var applicationAssembly = typeof(BookingSystem.Application.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .That()
            .AreClasses()
            .And()
            .ResideInNamespace("BookingSystem.Application.Features")
            .And()
            .ResideInNamespace("Queries")
            .And()
            .DoNotHaveNameMatching(".*Handler$")
            .Should()
            .HaveNameEndingWith("Query")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"All query classes should end with 'Query'. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Handlers_Should_EndWithHandler()
    {
        // Arrange
        var applicationAssembly = typeof(BookingSystem.Application.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .That()
            .AreClasses()
            .And()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"All handler classes should end with 'Handler'. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Validators_Should_EndWithValidator()
    {
        // Arrange
        var applicationAssembly = typeof(BookingSystem.Application.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .That()
            .AreClasses()
            .And()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"All validator classes should end with 'Validator'. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Controllers_Should_EndWithController()
    {
        // Arrange
        var apiAssembly = typeof(BookingSystem.API.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(apiAssembly)
            .That()
            .AreClasses()
            .And()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"All controller classes should end with 'Controller'. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Repositories_Should_EndWithRepository()
    {
        // Arrange
        var infrastructureAssembly = typeof(BookingSystem.Infrastructure.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(infrastructureAssembly)
            .That()
            .AreClasses()
            .And()
            .ResideInNamespace("BookingSystem.Infrastructure.Repositories")
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"All concrete repository classes should end with 'Repository'. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}" );
    }

    #endregion

    #region CQRS Pattern Tests

    [Fact]
    public void Handlers_Should_ResideInCorrectNamespace()
    {
        // Arrange
        var applicationAssembly = typeof(BookingSystem.Application.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespace("BookingSystem.Application.Features")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"All handlers should reside in Features namespace. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void DTOs_Should_ResideInDTOsFolder()
    {
        // Arrange
        var applicationAssembly = typeof(BookingSystem.Application.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .That()
            .HaveNameEndingWith("Request")
            .Or()
            .HaveNameEndingWith("Response")
            .Or()
            .HaveNameEndingWith("Dto")
            .Should()
            .ResideInNamespace("BookingSystem.Application.Features")
            .Or()
            .ResideInNamespace("BookingSystem.Application.Common.Models")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"All DTOs should reside in Features or Common.Models namespace. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Entities_Should_ResideInDomainLayer()
    {
        // Arrange
        var domainAssembly = typeof(BookingSystem.Domain.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespace("BookingSystem.Domain.Entities")
            .Should()
            .BeClasses()
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"All entities should be classes in Domain.Entities namespace. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    #endregion

    #region Interface and Implementation Tests

    [Fact]
    public void Repositories_Should_ImplementIRepository()
    {
        // Arrange
        var infrastructureAssembly = typeof(BookingSystem.Infrastructure.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(infrastructureAssembly)
            .That()
            .ResideInNamespace("BookingSystem.Infrastructure.Repositories")
            .And()
            .HaveNameEndingWith("Repository")
            .Should()
            .BeClasses()
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"All repository implementations should be concrete classes. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Middleware_Should_ResideInAPILayer()
    {
        // Arrange
        var apiAssembly = typeof(BookingSystem.API.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(apiAssembly)
            .That()
            .HaveNameEndingWith("Middleware")
            .Should()
            .ResideInNamespace("BookingSystem.API.Middleware")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"All middleware should reside in API.Middleware namespace. Violations: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
    }

    #endregion
}
