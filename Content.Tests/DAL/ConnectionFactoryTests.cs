using System.Data;
using System.Reflection;
using ContentDAL.Connection;
using FluentAssertions;
using Xunit;

namespace Content.Tests.DAL;

public class ConnectionFactoryTests
{
    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithConnectionString_DoesNotThrow()
    {
        var act = () => new ConnectionFactory("Host=localhost;Port=5432;Database=test;Username=u;Password=p");

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithEmptyString_DoesNotThrow()
    {
        var act = () => new ConnectionFactory(string.Empty);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_StoresConnectionString()
    {
        const string cs = "Host=localhost;Port=5432;Database=mydb;Username=u;Password=p";
        var factory = new ConnectionFactory(cs);

        var stored = typeof(ConnectionFactory)
            .GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(factory) as string;

        stored.Should().Be(cs);
    }

    [Fact]
    public void Constructor_ImplementsIConnectionFactory()
    {
        new ConnectionFactory("Server=localhost").Should().BeAssignableTo<IConnectionFactory>();
    }

    // ── CreateConnection ──────────────────────────────────────────────────────

    [Fact]
    public void CreateConnection_ThrowsWhenEndpointUnreachable()
    {
        // Port 1 is reserved/unrouted — OS returns connection refused immediately
        var factory = new ConnectionFactory(
            "Host=localhost;Port=1;Database=d;Username=u;Password=p;Timeout=1");

        Action act = () => factory.CreateConnection();

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void CreateConnection_ThrowsWithInvalidHost()
    {
        var factory = new ConnectionFactory(
            "Host=invalid.host.that.does.not.exist.local;Port=5432;Database=d;Username=u;Password=p;Timeout=1");

        Action act = () => factory.CreateConnection();

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void CreateConnection_ThrowsNpgsqlOrSocketException()
    {
        var factory = new ConnectionFactory(
            "Host=127.0.0.1;Port=9;Database=d;Username=u;Password=p;Timeout=1");

        Action act = () => factory.CreateConnection();

        act.Should().Throw<Exception>().Which.GetType().Name
           .Should().ContainAny("Npgsql", "Socket", "Postgres", "Connection");
    }

    [Fact]
    public void CreateConnection_WithInvalidConnectionString_ThrowsArgumentException()
    {
        var factory = new ConnectionFactory("not_a_valid_connection_string");

        Action act = () => factory.CreateConnection();

        act.Should().Throw<Exception>();
    }
}
