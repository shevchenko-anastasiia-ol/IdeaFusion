using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ServiceDefaults.Extensions;
using Xunit;

namespace ServiceDefaults.Tests.Extensions;

public class AutoMapperExtensionsTests
{
    [Fact]
    public void AddAutoMapperWithLogging_SingleAssembly_RegistersIMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapperWithLogging(typeof(AutoMapperExtensionsTests).Assembly);

        var sp = services.BuildServiceProvider();
        sp.GetService<IMapper>().Should().NotBeNull();
    }

    [Fact]
    public void AddAutoMapperWithLogging_MultipleAssemblies_RegistersIMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapperWithLogging(
            typeof(AutoMapperExtensionsTests).Assembly,
            typeof(AutoMapperExtensions).Assembly);

        var sp = services.BuildServiceProvider();
        sp.GetService<IMapper>().Should().NotBeNull();
    }

    [Fact]
    public void AddAutoMapperWithLogging_SingleAssembly_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var result = services.AddAutoMapperWithLogging(typeof(AutoMapperExtensionsTests).Assembly);
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddAutoMapperWithLogging_MultipleAssemblies_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var result = services.AddAutoMapperWithLogging(typeof(AutoMapperExtensionsTests).Assembly);
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddAutoMapperWithLogging_RegisteredAsSingleton_ReturnsSameInstanceTwice()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapperWithLogging(typeof(AutoMapperExtensionsTests).Assembly);

        var sp = services.BuildServiceProvider();
        var mapper1 = sp.GetService<IMapper>();
        var mapper2 = sp.GetService<IMapper>();

        mapper1.Should().BeSameAs(mapper2);
    }

    [Fact]
    public void AddAutoMapperWithLogging_ParamsOverload_RegistersIMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapperWithLogging(new[] { typeof(AutoMapperExtensionsTests).Assembly });

        var sp = services.BuildServiceProvider();
        sp.GetService<IMapper>().Should().NotBeNull();
    }
}
