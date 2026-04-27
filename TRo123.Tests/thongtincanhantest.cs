using Xunit;
using Moq;
using TRo123.Controllers;
using TRo123.Models;
using TRo123.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TRo123.Tests;

public class ThongTinCaNhan_Tests
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

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 1 – GET ProfileForm
    // ═══════════════════════════════════════════════════════════════════════

    // 1. Chưa đăng nhập → redirect Login
    [Fact]
    public async Task ProfileForm_ChuaDangNhap_RedirectLogin()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildControllerNoSession(repo);

        var result = await ctrl.ProfileForm();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
    }

    // 2. Đăng nhập hợp lệ → trả về view thong_tin_ca_nhan
    [Fact]
    public async Task ProfileForm_DaDangNhap_TraVeView()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayTaiKhoanTheoMaAsync("ND001"))
            .ReturnsAsync(new TaiKhoanDto
            {
                MaTaiKhoan = "ND001",
                SoDienThoai = "0399999999",
                HoTen = "Nguyễn Văn A",
                VaiTro = "NguoiDung"
            });
        var ctrl = BuildController(repo, "ND001", "NguoiDung");

        var result = await ctrl.ProfileForm();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("thong_tin_ca_nhan", view.ViewName);
    }

    // 3. Tài khoản trong session không tồn tại trong DB → xóa session, redirect Login
    [Fact]
    public async Task ProfileForm_TaiKhoanKhongTonTai_XoaSession_RedirectLogin()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayTaiKhoanTheoMaAsync("ND_GHOST"))
            .ReturnsAsync((TaiKhoanDto?)null);
        var ctrl = BuildController(repo, "ND_GHOST", "NguoiDung");

        var result = await ctrl.ProfileForm();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        // Session đã bị xóa
        Assert.False(ctrl.HttpContext.Session.TryGetValue("MaTaiKhoan", out _));
    }

    // 4. Model được điền đúng dữ liệu từ tài khoản
    [Fact]
    public async Task ProfileForm_ModelDienDungDuLieu()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayTaiKhoanTheoMaAsync("CT001"))
            .ReturnsAsync(new TaiKhoanDto
            {
                MaTaiKhoan = "CT001",
                SoDienThoai = "0388888888",
                HoTen = "Trần Thị B",
                VaiTro = "ChuTro"
            });
        var ctrl = BuildController(repo, "CT001", "ChuTro");

        var result = await ctrl.ProfileForm();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CapNhatTaiKhoanViewModel>(view.Model);
        Assert.Equal("CT001", model.MaTaiKhoan);
        Assert.Equal("0388888888", model.SoDienThoai);
        Assert.Equal("Trần Thị B", model.HoTen);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 2 – POST Profile: cập nhật thông tin
    // ═══════════════════════════════════════════════════════════════════════

    // 5. Chưa đăng nhập POST → redirect Login
    [Fact]
    public async Task Profile_ChuaDangNhap_RedirectLogin()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildControllerNoSession(repo);

        var result = await ctrl.Profile(new CapNhatTaiKhoanViewModel());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        repo.Verify(r => r.CapNhatThongTinTaiKhoanAsync(
            It.IsAny<CapNhatTaiKhoanViewModel>()), Times.Never);
    }

    // 6. ModelState lỗi → trả về view, không gọi repo
    [Fact]
    public async Task Profile_ModelStateKhongHopLe_TraVeView()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, "ND001", "NguoiDung");
        ctrl.ModelState.AddModelError("HoTen", "Vui lòng nhập họ tên");

        var result = await ctrl.Profile(new CapNhatTaiKhoanViewModel());

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("thong_tin_ca_nhan", view.ViewName);
        repo.Verify(r => r.CapNhatThongTinTaiKhoanAsync(
            It.IsAny<CapNhatTaiKhoanViewModel>()), Times.Never);
    }

    // 7. Cập nhật thành công → gọi repo, redirect ProfileForm
    [Fact]
    public async Task Profile_HopLe_ThanhCong_RedirectProfileForm()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapNhatThongTinTaiKhoanAsync(It.IsAny<CapNhatTaiKhoanViewModel>()))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "ND001", "NguoiDung");
        var model = new CapNhatTaiKhoanViewModel
        {
            MaTaiKhoan = "ND001",
            HoTen = "Nguyễn Văn A",
            SoDienThoai = "0399999999"
        };

        var result = await ctrl.Profile(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ProfileForm", redirect.ActionName);
        repo.Verify(r => r.CapNhatThongTinTaiKhoanAsync(
            It.IsAny<CapNhatTaiKhoanViewModel>()), Times.Once);
    }

    // 8. Cập nhật thành công → MaTaiKhoan được gán từ session (không từ form)
    [Fact]
    public async Task Profile_MaTaiKhoanLayTuSession()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        CapNhatTaiKhoanViewModel? captured = null;
        repo.Setup(r => r.CapNhatThongTinTaiKhoanAsync(It.IsAny<CapNhatTaiKhoanViewModel>()))
            .Callback<CapNhatTaiKhoanViewModel>(m => captured = m)
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "ND001", "NguoiDung");

        await ctrl.Profile(new CapNhatTaiKhoanViewModel
        {
            MaTaiKhoan = "FAKE_ID", // giả mạo
            HoTen = "Test",
            SoDienThoai = "0399999999"
        });

        Assert.NotNull(captured);
        Assert.Equal("ND001", captured!.MaTaiKhoan); // phải là từ session
    }

    // 9. Cập nhật thành công → TempData có SuccessMessage
    [Fact]
    public async Task Profile_ThanhCong_CoSuccessMessage()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapNhatThongTinTaiKhoanAsync(It.IsAny<CapNhatTaiKhoanViewModel>()))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "ND001", "NguoiDung");

        await ctrl.Profile(new CapNhatTaiKhoanViewModel
        {
            HoTen = "Test",
            SoDienThoai = "0399999999"
        });

        Assert.True(ctrl.TempData.ContainsKey("SuccessMessage"));
    }
}