using AutoMapper;
using ContentBLL;
using ContentBLL.MappingProfiles;
using ContentBLL.Services;
using ContentBLL.Services.Interfaces;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Content.Tests.BLL;

public class BLL_DI_Tests
{
    private static IServiceCollection Build() =>
        new ServiceCollection().AddBusinessLayer(new ConfigurationBuilder().Build());

    // ── Returns same IServiceCollection ──────────────────────────────────────

    [Fact]  
    public void AddBusinessLayer_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddBusinessLayer(new ConfigurationBuilder().Build());

        result.Should().BeSameAs(services);
    }

    // ── Service registrations ─────────────────────────────────────────────────

    [Fact]
    public void AddBusinessLayer_RegistersIPostService()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IPostService) &&
            d.ImplementationType == typeof(PostService) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddBusinessLayer_RegistersICommentService()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ICommentService) &&
            d.ImplementationType == typeof(CommentService) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddBusinessLayer_RegistersILikeService()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ILikeService) &&
            d.ImplementationType == typeof(LikeService) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddBusinessLayer_RegistersIPostViewService()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IPostViewService) &&
            d.ImplementationType == typeof(PostViewService) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddBusinessLayer_RegistersISavedPostService()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ISavedPostService) &&
            d.ImplementationType == typeof(SavedPostService) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddBusinessLayer_RegistersITagService()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ITagService) &&
            d.ImplementationType == typeof(TagService) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    // ── AutoMapper ────────────────────────────────────────────────────────────

    [Fact]
    public void AddBusinessLayer_RegistersIMapper()
    {
        var services = Build();

        services.Any(d => d.ServiceType == typeof(IMapper)).Should().BeTrue();
    }

    [Theory]
    [InlineData(typeof(PostMappingProfiles))]
    [InlineData(typeof(CommentMappingProfiles))]
    [InlineData(typeof(LikeMappingProfiles))]
    [InlineData(typeof(TagMappingProfiles))]
    [InlineData(typeof(SavedPostMappingProfiles))]
    public void AddBusinessLayer_EachProfile_ConfigurationIsValid(Type profileType)
    {
        new MapperConfiguration(
            cfg => cfg.AddProfile((Profile)Activator.CreateInstance(profileType)!),
            LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None)))
            .AssertConfigurationIsValid();
    }

    // ── FluentValidation ──────────────────────────────────────────────────────

    [Fact]
    public void AddBusinessLayer_RegistersValidators()
    {
        var services = Build();

        services.Any(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>))
        .Should().BeTrue();
    }
}
