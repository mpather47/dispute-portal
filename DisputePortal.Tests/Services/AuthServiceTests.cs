using DisputePortal.Api.DTOs;
using Xunit;
using DisputePortal.Api.Models;
using DisputePortal.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DisputePortal.Tests.Services;

public class AuthServiceTests
{
    private const string TestJwtKey = "super-secret-test-key-for-unit-tests-min-32-chars!!";

    private static (AuthService service, Mock<UserManager<ApplicationUser>> userManagerMock) CreateService()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();

        var service = new AuthService(
            userManagerMock.Object,
            config,
            NullLogger<AuthService>.Instance);

        return (service, userManagerMock);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsNull()
    {
        var (service, mock) = CreateService();
        mock.Setup(x => x.FindByEmailAsync("nobody@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await service.LoginAsync(new LoginRequest("nobody@test.com", "pass"));

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        var (service, mock) = CreateService();
        var user = new ApplicationUser { Id = "u1", FullName = "Test", Email = "u@test.com" };
        mock.Setup(x => x.FindByEmailAsync("u@test.com")).ReturnsAsync(user);
        mock.Setup(x => x.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        var result = await service.LoginAsync(new LoginRequest("u@test.com", "wrong"));

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
    {
        var (service, mock) = CreateService();
        var user = new ApplicationUser { Id = "u1", FullName = "Alice", Email = "alice@test.com" };
        mock.Setup(x => x.FindByEmailAsync("alice@test.com")).ReturnsAsync(user);
        mock.Setup(x => x.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
        mock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(["Customer"]);

        var result = await service.LoginAsync(new LoginRequest("alice@test.com", "correct"));

        Assert.NotNull(result);
        Assert.Equal("u1", result.UserId);
        Assert.Equal("Alice", result.FullName);
        Assert.Equal("Customer", result.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task LoginAsync_UserWithNoRole_DefaultsToCustomer()
    {
        var (service, mock) = CreateService();
        var user = new ApplicationUser { Id = "u2", FullName = "Bob", Email = "bob@test.com" };
        mock.Setup(x => x.FindByEmailAsync("bob@test.com")).ReturnsAsync(user);
        mock.Setup(x => x.CheckPasswordAsync(user, "pass")).ReturnsAsync(true);
        mock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([]);

        var result = await service.LoginAsync(new LoginRequest("bob@test.com", "pass"));

        Assert.NotNull(result);
        Assert.Equal("Customer", result.Role);
    }
}
