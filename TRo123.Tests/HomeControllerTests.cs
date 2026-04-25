using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TRo123.Controllers;
using TRo123.Services;
using Xunit;

namespace TRo123.Tests;

public class HomeControllerTests
{
    private static HomeController CreateController(Mock<ILaLaHomeRepository> repo, ISession session)
    {
        var controller = new HomeController(repo.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = session }
        };
        return controller;
    }

    [Fact]
    public async Task DeletePost_WhenNotLoggedIn_RedirectsToLogin()
    {
        var repo = new Mock<ILaLaHomeRepository>(MockBehavior.Strict);
        var session = new TestSession();
        var controller = CreateController(repo, session);

        var result = await controller.DeletePost("P0001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
    }

    [Fact]
    public async Task DeletePost_WhenChuTroButNotOwner_RedirectsToDetail()
    {
        var repo = new Mock<ILaLaHomeRepository>(MockBehavior.Strict);
        repo.Setup(x => x.LayChuPhongTheoMaPhongAsync("P0001")).ReturnsAsync("TK0002");

        var session = new TestSession();
        var context = new DefaultHttpContext { Session = session };
        context.Session.SetString("MaTaiKhoan", "TK0001");
        context.Session.SetString("VaiTro", "ChuTro");

        var controller = new HomeController(repo.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = await controller.DeletePost("P0001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
        Assert.Equal("P0001", redirect.RouteValues?["id"]);
    }
}

