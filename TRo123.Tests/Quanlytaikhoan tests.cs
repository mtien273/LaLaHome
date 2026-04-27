using Xunit;
using Moq;
using TRo123.Controllers;
using TRo123.Models;
using TRo123.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TRo123.Tests;

public class QuanLyTaiKhoan_Tests
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
    // NHÓM 1 – AccountManagementList (GET): danh sách tài khoản
    // ═══════════════════════════════════════════════════════════════════════

    // 1. QuanTri → trả về view danh sách tài khoản
    [Fact]
    public async Task AccountManagementList_VaiTroQuanTri_TraVeView()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayDanhSachTaiKhoanAsync())
            .ReturnsAsync(new List<TaiKhoanDto>
            {
                new() { MaTaiKhoan = "ND001", HoTen = "User A", VaiTro = "NguoiDung" },
                new() { MaTaiKhoan = "CT001", HoTen = "Chủ B",  VaiTro = "ChuTro"    }
            });
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.AccountManagementList();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("quan_ly_tai_khoan_nguoi_dung", view.ViewName);
        repo.Verify(r => r.LayDanhSachTaiKhoanAsync(), Times.Once);
    }

    // 2. Không phải QuanTri → redirect Index, không gọi repo
    [Theory]
    [InlineData("ND001", "NguoiDung")]
    [InlineData("CT001", "ChuTro")]
    public async Task AccountManagementList_KhongPhaIQuanTri_RedirectIndex(string ma, string vaiTro)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, ma, vaiTro);

        var result = await ctrl.AccountManagementList();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.LayDanhSachTaiKhoanAsync(), Times.Never);
    }

    // 3. Chưa đăng nhập → redirect Index
    [Fact]
    public async Task AccountManagementList_ChuaDangNhap_RedirectIndex()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildControllerNoSession(repo);

        var result = await ctrl.AccountManagementList();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 2 – AccountEdit (GET): xem form sửa tài khoản
    // ═══════════════════════════════════════════════════════════════════════

    // 4. QuanTri xem tài khoản hợp lệ → trả về view với đúng model
    [Fact]
    public async Task AccountEdit_Get_QuanTri_TraVeViewVoiModel()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayTaiKhoanTheoMaAsync("ND001"))
            .ReturnsAsync(new TaiKhoanDto
            {
                MaTaiKhoan = "ND001",
                HoTen = "Nguyễn Văn A",
                SoDienThoai = "0399999999",
                VaiTro = "NguoiDung"
            });
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.AccountEdit("ND001");

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("cap_nhat_tai_khoan_nguoi_dung", view.ViewName);
        var model = Assert.IsType<CapNhatTaiKhoanQuanTriViewModel>(view.Model);
        Assert.Equal("ND001", model.MaTaiKhoan);
        Assert.Equal("Nguyễn Văn A", model.HoTen);
        Assert.Equal("0399999999", model.SoDienThoai);
        Assert.Equal("NguoiDung", model.VaiTro);
    }

    // 5. Không phải QuanTri → redirect Index
    [Theory]
    [InlineData("ND001", "NguoiDung")]
    [InlineData("CT001", "ChuTro")]
    public async Task AccountEdit_Get_KhongPhaIQuanTri_RedirectIndex(string ma, string vaiTro)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, ma, vaiTro);

        var result = await ctrl.AccountEdit("ND001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.LayTaiKhoanTheoMaAsync(It.IsAny<string>()), Times.Never);
    }

    // 6. Tài khoản không tồn tại → redirect AccountManagementList
    [Fact]
    public async Task AccountEdit_Get_TaiKhoanKhongTonTai_RedirectList()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayTaiKhoanTheoMaAsync("GHOST"))
            .ReturnsAsync((TaiKhoanDto?)null);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.AccountEdit("GHOST");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AccountManagementList", redirect.ActionName);
        Assert.True(ctrl.TempData.ContainsKey("ErrorMessage"));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 3 – AccountEdit (POST): cập nhật tài khoản
    // ═══════════════════════════════════════════════════════════════════════

    // 7. QuanTri cập nhật hợp lệ → gọi repo, redirect AccountEdit
    [Fact]
    public async Task AccountEdit_Post_QuanTri_ThanhCong()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapNhatTaiKhoanBoiQuanTriAsync(
            It.IsAny<CapNhatTaiKhoanQuanTriViewModel>()))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");
        var model = new CapNhatTaiKhoanQuanTriViewModel
        {
            MaTaiKhoan = "ND001",
            HoTen = "Nguyễn Văn A",
            SoDienThoai = "0399999999",
            VaiTro = "NguoiDung"
        };

        var result = await ctrl.AccountEdit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AccountEdit", redirect.ActionName);
        Assert.Equal("ND001", redirect.RouteValues?["id"]);
        repo.Verify(r => r.CapNhatTaiKhoanBoiQuanTriAsync(
            It.IsAny<CapNhatTaiKhoanQuanTriViewModel>()), Times.Once);
    }

    // 8. Không phải QuanTri POST → redirect Index, không gọi repo
    [Theory]
    [InlineData("ND001", "NguoiDung")]
    [InlineData("CT001", "ChuTro")]
    public async Task AccountEdit_Post_KhongPhaIQuanTri_RedirectIndex(string ma, string vaiTro)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, ma, vaiTro);

        var result = await ctrl.AccountEdit(new CapNhatTaiKhoanQuanTriViewModel
        {
            MaTaiKhoan = "ND001",
            HoTen = "Test",
            SoDienThoai = "0399999999"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.CapNhatTaiKhoanBoiQuanTriAsync(
            It.IsAny<CapNhatTaiKhoanQuanTriViewModel>()), Times.Never);
    }

    // 9. ModelState lỗi → trả về view, không gọi repo
    [Fact]
    public async Task AccountEdit_Post_ModelStateKhongHopLe_TraVeView()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, "QT001", "QuanTri");
        ctrl.ModelState.AddModelError("HoTen", "Vui lòng nhập họ tên");

        var result = await ctrl.AccountEdit(new CapNhatTaiKhoanQuanTriViewModel());

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("cap_nhat_tai_khoan_nguoi_dung", view.ViewName);
        repo.Verify(r => r.CapNhatTaiKhoanBoiQuanTriAsync(
            It.IsAny<CapNhatTaiKhoanQuanTriViewModel>()), Times.Never);
    }

    // 10. Cập nhật thành công → TempData có SuccessMessage
    [Fact]
    public async Task AccountEdit_Post_ThanhCong_CoSuccessMessage()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapNhatTaiKhoanBoiQuanTriAsync(
            It.IsAny<CapNhatTaiKhoanQuanTriViewModel>()))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        await ctrl.AccountEdit(new CapNhatTaiKhoanQuanTriViewModel
        {
            MaTaiKhoan = "ND001",
            HoTen = "Test",
            SoDienThoai = "0399999999",
            VaiTro = "NguoiDung"
        });

        Assert.True(ctrl.TempData.ContainsKey("SuccessMessage"));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 4 – ResetPassword: cấp lại mật khẩu
    // ═══════════════════════════════════════════════════════════════════════

  

    // 11. Không phải QuanTri → redirect Index, không gọi repo
    [Theory]
    [InlineData("ND001", "NguoiDung")]
    [InlineData("CT001", "ChuTro")]
    public async Task ResetPassword_KhongPhaIQuanTri_RedirectIndex(string ma, string vaiTro)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, ma, vaiTro);

        var result = await ctrl.ResetPassword("ND001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.CapLaiMatKhauAsync(
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // 12. Reset nhiều tài khoản khác nhau → mỗi lần gọi đúng maTaiKhoan
    [Theory]
    [InlineData("ND001")]
    [InlineData("CT001")]
    [InlineData("ND999")]
    public async Task ResetPassword_NhieuTaiKhoan_GoiDungMa(string maTaiKhoan)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapLaiMatKhauAsync(maTaiKhoan, "123456"))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        await ctrl.ResetPassword(maTaiKhoan);

        repo.Verify(r => r.CapLaiMatKhauAsync(maTaiKhoan, "123456"), Times.Once);
    }

    // 13. Reset thành công → TempData có SuccessMessage kèm maTaiKhoan
    [Fact]
    public async Task ResetPassword_ThanhCong_CoSuccessMessage()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapLaiMatKhauAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        await ctrl.ResetPassword("ND001");

        Assert.True(ctrl.TempData.ContainsKey("SuccessMessage"));
        Assert.Contains("ND001", ctrl.TempData["SuccessMessage"]!.ToString());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 5 – DeleteAccount: xóa tài khoản
    // ═══════════════════════════════════════════════════════════════════════

    // 14. QuanTri xóa tài khoản người khác → gọi XoaTaiKhoan, redirect List
    [Fact]
    public async Task DeleteAccount_QuanTri_XoaTaiKhoanNguoiKhac_ThanhCong()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.XoaTaiKhoanAsync("ND001")).Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.DeleteAccount("ND001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AccountManagementList", redirect.ActionName);
        repo.Verify(r => r.XoaTaiKhoanAsync("ND001"), Times.Once);
    }

    // 15. QuanTri xóa chính mình → bị chặn, không gọi XoaTaiKhoan
    [Fact]
    public async Task DeleteAccount_QuanTriXoaChinhMiNh_BiChan()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.DeleteAccount("QT001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AccountEdit", redirect.ActionName);
        Assert.True(ctrl.TempData.ContainsKey("ErrorMessage"));
        repo.Verify(r => r.XoaTaiKhoanAsync(It.IsAny<string>()), Times.Never);
    }

    // 16. Không phải QuanTri → redirect Index, không gọi XoaTaiKhoan
    [Theory]
    [InlineData("ND001", "NguoiDung")]
    [InlineData("CT001", "ChuTro")]
    public async Task DeleteAccount_KhongPhaIQuanTri_RedirectIndex(string ma, string vaiTro)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, ma, vaiTro);

        var result = await ctrl.DeleteAccount("ND999");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.XoaTaiKhoanAsync(It.IsAny<string>()), Times.Never);
    }

    // 17. Xóa thành công → TempData có SuccessMessage kèm maTaiKhoan
    [Fact]
    public async Task DeleteAccount_ThanhCong_CoSuccessMessage()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.XoaTaiKhoanAsync("ND001")).Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        await ctrl.DeleteAccount("ND001");

        Assert.True(ctrl.TempData.ContainsKey("SuccessMessage"));
        Assert.Contains("ND001", ctrl.TempData["SuccessMessage"]!.ToString());
    }

    // 18. Xóa tài khoản case-insensitive (QT001 vs qt001) → vẫn bị chặn
    [Theory]
    [InlineData("QT001")]
    [InlineData("qt001")]
    [InlineData("Qt001")]
    public async Task DeleteAccount_XoaChinhMiNh_CaseInsensitive_BiChan(string maXoa)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.DeleteAccount(maXoa);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AccountEdit", redirect.ActionName);
        repo.Verify(r => r.XoaTaiKhoanAsync(It.IsAny<string>()), Times.Never);
    }
}