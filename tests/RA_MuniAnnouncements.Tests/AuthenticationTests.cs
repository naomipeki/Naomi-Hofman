using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using RA_MuniAnnouncements.Api.DataAccess;
using RA_MuniAnnouncements.Api.DevTools;
using RA_MuniAnnouncements.Api.Models.Exceptions;
using RA_MuniAnnouncements.Api.Models;
using RA_MuniAnnouncements.Api.Security;
using RA_MuniAnnouncements.Tests.TestDoubles;

namespace RA_MuniAnnouncements.Tests;

public sealed class AuthenticationTests
{
    private const string Password = "Test-password-2026!";

    [Fact]
    public async Task Login_IssuesSignedToken_AndOnlyReturnsResidentsOwnRequests()
    {
        using var app = new AuthApplication();
        using var client = app.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "  ReSiDeNt  ", password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        var jwt = new JsonWebToken(login.Token);
        Assert.Equal("012345678", jwt.GetClaim("idNumber").Value);
        Assert.Equal(app.Users.Resident.Id, jwt.Subject);
        Assert.InRange(login.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(59), DateTimeOffset.UtcNow.AddMinutes(61));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        var requests = await client.GetFromJsonAsync<MyRequestsResponse>("/api/my-requests?idNumber=000000018");
        Assert.NotNull(requests);
        Assert.NotEmpty(requests.Requests);
        var other = await client.PostAsJsonAsync("/api/auth/login", new { username = "other", password = Password });
        var otherLogin = (await other.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherLogin.Token);
        var otherRequests = (await client.GetFromJsonAsync<MyRequestsResponse>("/api/my-requests?idNumber=012345678"))!;
        Assert.DoesNotContain(otherRequests.Requests, r => requests.Requests.Any(own => own.RequestId == r.RequestId));
    }

    [Theory]
    [InlineData("resident", "wrong-password")]
    [InlineData("missing", Password)]
    public async Task InvalidCredentials_ReturnSameGeneric401(string username, string password)
    {
        using var app = new AuthApplication();
        using var client = app.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("token", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("resident", "")]
    public async Task MissingCredentials_Return400(string username, string password)
    {
        using var app = new AuthApplication();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", new { username, password })).StatusCode);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("invalid")]
    [InlineData("expired")]
    [InlineData("wrong-key")]
    [InlineData("wrong-issuer")]
    [InlineData("wrong-audience")]
    public async Task ProtectedApi_RejectsInvalidTokens(string kind)
    {
        using var app = new AuthApplication();
        using var client = app.CreateClient();
        if (kind != "none")
        {
            var configured = app.Services.GetRequiredService<JwtSettings>();
            var settings = new JwtSettings
            {
                Issuer = kind == "wrong-issuer" ? "foreign" : configured.Issuer,
                Audience = kind == "wrong-audience" ? "foreign" : configured.Audience,
                SigningKey = kind == "wrong-key" ? new string('X', 40) : configured.SigningKey,
                ExpirationMinutes = 60,
            };
            var clock = new FixedTimeProvider(kind == "expired" ? DateTimeOffset.UtcNow.AddHours(-2) : DateTimeOffset.UtcNow);
            var token = kind == "invalid" ? "not-a-jwt" : new JwtTokenIssuer(settings, clock).Issue(app.Users.Resident).Token;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/my-requests")).StatusCode);
    }

    [Fact]
    public async Task MongoOutage_Returns503_NotInvalidCredentials()
    {
        using var app = new AuthApplication();
        app.Users.Unavailable = true;
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            (await client.PostAsJsonAsync("/api/auth/login", new { username = "resident", password = Password })).StatusCode);
    }

    [Fact]
    public async Task Login_IsRateLimited()
    {
        using var app = new AuthApplication();
        using var client = app.CreateClient();
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.PostAsJsonAsync("/api/auth/login", new { username = "", password = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync("/api/auth/login", new { username = "", password = "" })).StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesBothEndpoints_InDevelopment()
    {
        using var app = new AuthApplication();
        using var client = app.CreateClient();
        var document = await client.GetStringAsync("/openapi/v1.json");
        Assert.Contains("\"/api/auth/login\"", document);
        Assert.Contains("\"/api/my-requests\"", document);
        Assert.DoesNotContain("applicantId", document, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class AuthApplication : WebApplicationFactory<Program>
    {
        public TestUsers Users { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUserRepository>();
                services.AddSingleton<IUserRepository>(Users);
                // Same documents --seed-catalog writes to MongoDB, served from memory → no live database in tests.
                services.RemoveAll<IServiceCatalogRepository>();
                services.AddSingleton<IServiceCatalogRepository>(new SpyCatalogRepository(
                    [.. ServiceCatalogSeeder.Load(ServiceCatalogSeeder.DefaultPath).Select(d => d.ToEntry())]));
            });
        }
    }

    private sealed class TestUsers : IUserRepository
    {
        public User Resident { get; } = Create("resident", "012345678");
        private readonly User other = Create("other", "000000018");
        public bool Unavailable { get; set; }
        public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            if (Unavailable) throw new DependencyUnavailableException("Users");
            return Task.FromResult(new[] { Resident, other }.SingleOrDefault(u => u.NormalizedUsername == User.NormalizeUsername(username)));
        }
        public Task CreateAsync(User user, CancellationToken cancellationToken) => throw new NotSupportedException();
        private static User Create(string username, string idNumber)
        {
            var user = new User { Username = username, NormalizedUsername = User.NormalizeUsername(username), IdNumber = idNumber };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, Password);
            return user;
        }
    }
}
