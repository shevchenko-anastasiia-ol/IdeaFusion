using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using IdentityBLL.DTOs;
using Xunit;

namespace Identity.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(IdentityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static RegisterDto BuildUser(string tag) => new()
    {
        UserName = $"user_{tag}",
        Email    = $"user_{tag}@test.com",
        Password = "Password123!"
    };

    private async Task<AuthResponseDto> RegisterAndLoginAsync(string tag)
    {
        await _client.PostAsJsonAsync("/api/auth/register", BuildUser(tag));
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email    = $"user_{tag}@test.com",
            Password = "Password123!"
        });
        return (await resp.Content.ReadFromJsonAsync<AuthResponseDto>())!;
    }

    // ──────────────────────────────────── REGISTER ────────────────────────────

    [Fact]
    public async Task Register_ValidData_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", BuildUser("reg_ok"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var dto = new RegisterDto { UserName = "x", Email = "not-an-email", Password = "Password123!" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_PasswordTooShort_Returns400()
    {
        var dto = new RegisterDto { UserName = "x", Email = "short@test.com", Password = "short" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var dto = BuildUser("dup");
        await _client.PostAsJsonAsync("/api/auth/register", dto);

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────────────────────────── LOGIN ───────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        var user = BuildUser("login_ok");
        await _client.PostAsJsonAsync("/api/auth/register", user);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email    = user.Email,
            Password = user.Password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrEmpty();
        tokens.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var user = BuildUser("login_bad_pwd");
        await _client.PostAsJsonAsync("/api/auth/register", user);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email    = user.Email,
            Password = "WrongPassword999!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email    = "ghost@nowhere.com",
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────── GET CURRENT USER ────────────────────────────

    [Fact]
    public async Task GetCurrentUser_WithValidToken_Returns200WithUserInfo()
    {
        var tokens = await RegisterAndLoginAsync("me_ok");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be("user_me_ok@test.com");
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────── REFRESH TOKEN ───────────────────────────────

    [Fact]
    public async Task RefreshToken_ValidToken_Returns200WithNewTokens()
    {
        var tokens = await RegisterAndLoginAsync("refresh_ok");

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new TokenRequestDto
        {
            RefreshToken = tokens.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newTokens = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        newTokens.Should().NotBeNull();
        newTokens!.AccessToken.Should().NotBeNullOrEmpty();
        newTokens.RefreshToken.Should().NotBe(tokens.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new TokenRequestDto
        {
            RefreshToken = "completely-invalid-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_AlreadyUsedToken_Returns401()
    {
        var tokens = await RegisterAndLoginAsync("refresh_used");

        // First use — valid
        await _client.PostAsJsonAsync("/api/auth/refresh", new TokenRequestDto
        {
            RefreshToken = tokens.RefreshToken
        });

        // Second use of same token — must fail
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new TokenRequestDto
        {
            RefreshToken = tokens.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────── REVOKE ─────────────────────────────────

    [Fact]
    public async Task Revoke_ValidToken_Returns204()
    {
        var tokens = await RegisterAndLoginAsync("revoke_ok");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/auth/revoke", new TokenRequestDto
        {
            RefreshToken = tokens.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Revoke_InvalidToken_Returns400()
    {
        var tokens = await RegisterAndLoginAsync("revoke_bad");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/auth/revoke", new TokenRequestDto
        {
            RefreshToken = "no-such-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────────────────────── LOGOUT ─────────────────────────────────

    [Fact]
    public async Task Logout_WithValidToken_Returns204()
    {
        var tokens = await RegisterAndLoginAsync("logout_ok");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await _client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
