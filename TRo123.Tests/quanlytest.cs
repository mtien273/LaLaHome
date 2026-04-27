using Xunit;
using Moq;
using TRo123.Controllers;
using TRo123.Models;
using TRo123.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TRo123.Tests;

public class QuanLyTinChuTro_Tests
{
    // ─── Helpers ────────────────────────────────────────────────────────────

    private static HomeController BuildController(
        Mock<ILaLaHomeRepository> repo,
        string maTaiKhoan,
        string vaiTro)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Session = new FakeSession(new Dictionary<string, string>
        {
            ["MaTaiKhoan"] = maTaiKhoan,
            ["VaiTro"] = vaiTro
        });
        var controller = new HomeController(repo.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
        controller.TempData = new TempDataDictionary(
            httpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    private static HomeController BuildControllerNoSession(Mock<ILaLaHomeRepository> repo)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Session = new FakeSession(new Dictionary<string, string>());
        var controller = new HomeController(repo.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
        controller.TempData = new TempDataDictionary(
            httpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    // ════════════════════════════════════════════════S═══════════════════════
    // NHÓM 1 – MyPosts
    // ═══════════════════════════════════════════════════════════════════════

    // 1. Chưa đăng nhập → redirect Login
    [Fact]
    public async Task MyPosts_ChuaDangNhap_RedirectLogin()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildControllerNoSession(repo);

        var result = await ctrl.MyPosts();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        repo.Verify(r => r.LayDanhSachTinCuaToiAsync(It.IsAny<string>()), Times.Never);
    }

    // 2. Không phải ChuTro → redirect Index
    [Fact]
    public async Task MyPosts_KhongPhaiChuTro_RedirectIndex()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, "ND001", "NguoiDung");

        var result = await ctrl.MyPosts();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.LayDanhSachTinCuaToiAsync(It.IsAny<string>()), Times.Never);
    }

    // 3. ChuTro hợp lệ → trả về view với danh sách tin
    [Fact]
    public async Task MyPosts_LaChuTro_TraVeView()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayDanhSachTinCuaToiAsync("CT001"))
            .ReturnsAsync(new List<TinCuaToiDto>
            {
                new() { MaPhong = "P001", TenPhongTro = "Phòng A" }
            });
        var ctrl = BuildController(repo, "CT001", "ChuTro");

        var result = await ctrl.MyPosts();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<TinCuaToiDto>>(view.Model);
        Assert.Single(model);
    }

    // 4. ChuTro hợp lệ → gọi repo với đúng MaTaiKhoan từ session
    [Fact]
    public async Task MyPosts_GoiRepoVoiMaTaiKhoanTuSession()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayDanhSachTinCuaToiAsync("CT001"))
            .ReturnsAsync(new List<TinCuaToiDto>());
        var ctrl = BuildController(repo, "CT001", "ChuTro");

        await ctrl.MyPosts();

        repo.Verify(r => r.LayDanhSachTinCuaToiAsync("CT001"), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 2 – EditPostForm (GET)
    // ═══════════════════════════════════════════════════════════════════════

    // 5. Chưa đăng nhập → redirect Login
    [Fact]
    public async Task EditPostForm_ChuaDangNhap_RedirectLogin()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildControllerNoSession(repo);

        var result = await ctrl.EditPostForm("P001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        repo.Verify(r => r.LayTinPhongDeSuaAsync(It.IsAny<string>()), Times.Never);
    }

    // 6. Không phải ChuTro → redirect Index
    [Fact]
    public async Task EditPostForm_KhongPhaiChuTro_RedirectIndex()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, "ND001", "NguoiDung");

        var result = await ctrl.EditPostForm("P001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.LayTinPhongDeSuaAsync(It.IsAny<string>()), Times.Never);
    }

    // 7. Tin không phải của mình → redirect Detail
    [Fact]
    public async Task EditPostForm_TinKhongPhaicuaMiNh_RedirectDetail()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayTinPhongDeSuaAsync("P001"))
            .ReturnsAsync(new TaoTinPhongViewModel { MaPhong = "P001", MaTaiKhoan = "CT999" });
        var ctrl = BuildController(repo, "CT001", "ChuTro");

        var result = await ctrl.EditPostForm("P001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
    }

    // 8. Tin không tồn tại → redirect Listings
    [Fact]
    public async Task EditPostForm_TinKhongTonTai_RedirectListings()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayTinPhongDeSuaAsync("P_GHOST"))
            .ReturnsAsync((TaoTinPhongViewModel?)null);
        var ctrl = BuildController(repo, "CT001", "ChuTro");

        var result = await ctrl.EditPostForm("P_GHOST");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Listings", redirect.ActionName);
    }

    // 9. ChuTro sửa tin của mình → trả về view
    [Fact]
    public async Task EditPostForm_ChuTroSuaTinCuaMiNh_TraVeView()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayTinPhongDeSuaAsync("P001"))
            .ReturnsAsync(new TaoTinPhongViewModel
            {
                MaPhong = "P001",
                MaTaiKhoan = "CT001",
                MaTinhThanhPho = "HN",
                MaQuanHuyen = "HBT"
            });
        repo.Setup(r => r.LayLoaiPhongAsync()).ReturnsAsync(new List<LoaiPhongDto>());
        repo.Setup(r => r.LayTinhThanhPhoAsync()).ReturnsAsync(new List<TinhThanhPhoDto>());
        repo.Setup(r => r.LayQuanHuyenAsync("HN")).ReturnsAsync(new List<QuanHuyenDto>());
        repo.Setup(r => r.LayXaPhuongAsync("HBT")).ReturnsAsync(new List<XaPhuongDto>());
        var ctrl = BuildController(repo, "CT001", "ChuTro");

        var result = await ctrl.EditPostForm("P001");

        Assert.IsType<ViewResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 3 – EditPost (POST)
    // ═══════════════════════════════════════════════════════════════════════

    // 10. Chưa đăng nhập → redirect Login, không gọi repo
    [Fact]
    public async Task EditPost_Post_ChuaDangNhap_RedirectLogin()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildControllerNoSession(repo);

        var result = await ctrl.EditPost(new TaoTinPhongViewModel { MaPhong = "P001" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        repo.Verify(r => r.CapNhatTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()), Times.Never);
    }

    // 11. Không phải ChuTro → redirect Index, không gọi repo
    [Fact]
    public async Task EditPost_Post_KhongPhaiChuTro_RedirectIndex()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, "ND001", "NguoiDung");

        var result = await ctrl.EditPost(new TaoTinPhongViewModel { MaPhong = "P001" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.CapNhatTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()), Times.Never);
    }

    // 12. Tin không phải của mình → redirect Detail, không gọi repo
    [Fact]
    public async Task EditPost_Post_TinKhongPhaicuaMiNh_RedirectDetail()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayTinPhongDeSuaAsync("P001"))
            .ReturnsAsync(new TaoTinPhongViewModel { MaPhong = "P001", MaTaiKhoan = "CT999" });
        var ctrl = BuildController(repo, "CT001", "ChuTro");
        var model = new TaoTinPhongViewModel
        {
            MaPhong = "P001",
            MaLoaiPhong = "LP01",
            TenPhongTro = "Test",
            GiaPhong = 2000000,
            DienTich = 20,
            GiaDien = 3500,
            GiaNuoc = 15000,
            SoDienThoai = "0399999999",
            MaTinhThanhPho = "HN",
            MaQuanHuyen = "HBT"
        };

        var result = await ctrl.EditPost(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
        repo.Verify(r => r.CapNhatTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()), Times.Never);
    }

    // 13. ChuTro sửa tin của mình → gọi repo, redirect Detail
    [Fact]
    public async Task EditPost_Post_ChuTroSuaTinCuaMiNh_ThanhCong()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayTinPhongDeSuaAsync("P001"))
            .ReturnsAsync(new TaoTinPhongViewModel { MaPhong = "P001", MaTaiKhoan = "CT001" });
        repo.Setup(r => r.CapNhatTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "CT001", "ChuTro");
        var model = new TaoTinPhongViewModel
        {
            MaPhong = "P001",
            MaLoaiPhong = "LP01",
            TenPhongTro = "Phòng test",
            GiaPhong = 2000000,
            DienTich = 20,
            GiaDien = 3500,
            GiaNuoc = 15000,
            SoDienThoai = "0399999999",
            MaTinhThanhPho = "HN",
            MaQuanHuyen = "HBT"
        };

        var result = await ctrl.EditPost(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
        repo.Verify(r => r.CapNhatTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()), Times.Once);
    }

    // 14. ChuTro sửa tin của mình → có SuccessMessage trong TempData
    [Fact]
    public async Task EditPost_Post_ThanhCong_CoSuccessMessage()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayTinPhongDeSuaAsync("P001"))
            .ReturnsAsync(new TaoTinPhongViewModel { MaPhong = "P001", MaTaiKhoan = "CT001" });
        repo.Setup(r => r.CapNhatTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "CT001", "ChuTro");

        await ctrl.EditPost(new TaoTinPhongViewModel
        {
            MaPhong = "P001",
            MaLoaiPhong = "LP01",
            TenPhongTro = "Phòng test",
            GiaPhong = 2000000,
            DienTich = 20,
            GiaDien = 3500,
            GiaNuoc = 15000,
            SoDienThoai = "0399999999",
            MaTinhThanhPho = "HN",
            MaQuanHuyen = "HBT"
        });

        Assert.True(ctrl.TempData.ContainsKey("SuccessMessage"));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 4 – DeletePost (POST)
    // ═══════════════════════════════════════════════════════════════════════

    // 15. Chưa đăng nhập → redirect Login, không gọi repo
    [Fact]
    public async Task DeletePost_ChuaDangNhap_RedirectLogin()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildControllerNoSession(repo);

        var result = await ctrl.DeletePost("P001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        repo.Verify(r => r.XoaPhongAsync(It.IsAny<string>()), Times.Never);
    }

    // 16. Không phải ChuTro/QuanTri → redirect Index, không gọi repo
    [Fact]
    public async Task DeletePost_KhongPhaiChuTroHoacQuanTri_RedirectIndex()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, "ND001", "NguoiDung");

        var result = await ctrl.DeletePost("P001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.XoaPhongAsync(It.IsAny<string>()), Times.Never);
    }

    // 17. Tin không phải của mình → redirect Detail, không gọi repo
    [Fact]
    public async Task DeletePost_TinKhongPhaicuaMiNh_RedirectDetail()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayChuPhongTheoMaPhongAsync("P001")).ReturnsAsync("CT999");
        var ctrl = BuildController(repo, "CT001", "ChuTro");

        var result = await ctrl.DeletePost("P001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
        repo.Verify(r => r.XoaPhongAsync(It.IsAny<string>()), Times.Never);
    }

    // 18. ChuTro xóa tin của mình → gọi repo đúng 1 lần, redirect MyPosts
    [Fact]
    public async Task DeletePost_ChuTroXoaTinCuaMiNh_RedirectMyPosts()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayChuPhongTheoMaPhongAsync("P001")).ReturnsAsync("CT001");
        repo.Setup(r => r.XoaPhongAsync("P001")).Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "CT001", "ChuTro");

        var result = await ctrl.DeletePost("P001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("MyPosts", redirect.ActionName);
        repo.Verify(r => r.XoaPhongAsync("P001"), Times.Once);
    }

    // 19. ChuTro xóa tin của mình → có SuccessMessage trong TempData
    [Fact]
    public async Task DeletePost_ThanhCong_CoSuccessMessage()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayChuPhongTheoMaPhongAsync("P001")).ReturnsAsync("CT001");
        repo.Setup(r => r.XoaPhongAsync("P001")).Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "CT001", "ChuTro");

        await ctrl.DeletePost("P001");

        Assert.True(ctrl.TempData.ContainsKey("SuccessMessage"));
    }
}