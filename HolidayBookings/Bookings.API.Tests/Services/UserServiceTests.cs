using Bookings.API.Services;
using Xunit;

namespace Bookings.API.Tests.Services;

public class UserServiceTests : DbTestBase
{
    [Fact]
    public async Task GetUserAsync_ExistingUser_ReturnsUser()
    {
        var id = AddUser(organiser: true);
        using var db = NewContext();

        var user = await new UserService(db).GetUserAsync(id);

        Assert.NotNull(user);
        Assert.Equal(id, user.Id);
        Assert.True(user.Organiser);
    }

    [Fact]
    public async Task GetUserAsync_NonOrganiser_ReturnsOrganiserFalse()
    {
        var id = AddUser(organiser: false);
        using var db = NewContext();

        var user = await new UserService(db).GetUserAsync(id);

        Assert.False(user!.Organiser);
    }

    [Fact]
    public async Task GetUserAsync_MissingUser_ReturnsNull()
    {
        using var db = NewContext();

        Assert.Null(await new UserService(db).GetUserAsync(999));
    }
}