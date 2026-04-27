using Xunit;
using Moq;
using TRo123.Controllers;
using TRo123.Models;
using TRo123.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TRo123.Tests;

public class KiemDuyet_Tests
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
    // NHÓM 1 – Moderation (GET): xem danh sách tin chờ duyệt
    // ═══════════════════════════════════════════════════════════════════════

    // 1. QuanTri → trả về view danh sách tin chờ duyệt
    [Fact]
    public async Task Moderation_VaiTroQuanTri_TraVeView()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayDanhSachTinChoDuyetAsync())
            .ReturnsAsync(new List<TinChoDuyetDto>
            {
                new() { MaPhong = "P001", TenPhongTro = "Phòng 1", TrangThaiDuyet = "KD002" }
            });
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.Moderation();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("kiem_duyet_tin", view.ViewName);
        repo.Verify(r => r.LayDanhSachTinChoDuyetAsync(), Times.Once);
    }

    // 2. Không phải QuanTri → redirect Index
    [Theory]
    [InlineData("ND001", "NguoiDung")]
    [InlineData("CT001", "ChuTro")]
    public async Task Moderation_KhongPhaIQuanTri_RedirectIndex(string ma, string vaiTro)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, ma, vaiTro);

        var result = await ctrl.Moderation();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.LayDanhSachTinChoDuyetAsync(), Times.Never);
    }

    // 3. Chưa đăng nhập → redirect Index (không có quyền)
    [Fact]
    public async Task Moderation_ChuaDangNhap_RedirectIndex()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildControllerNoSession(repo);

        var result = await ctrl.Moderation();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 2 – ApprovePost: duyệt tin đăng
    // ═══════════════════════════════════════════════════════════════════════

    // 4. QuanTri duyệt tin → gọi CapNhatTrangThaiDuyetTin, redirect Moderation
    [Fact]
    public async Task ApprovePost_QuanTri_ThanhCong()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapNhatTrangThaiDuyetTinAsync("P001", "KD001", true))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.ApprovePost("P001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Moderation", redirect.ActionName);
        repo.Verify(r => r.CapNhatTrangThaiDuyetTinAsync("P001", "KD001", true), Times.Once);
    }

    // 5. Không phải QuanTri → redirect Index, không gọi repo
    [Theory]
    [InlineData("ND001", "NguoiDung")]
    [InlineData("CT001", "ChuTro")]
    public async Task ApprovePost_KhongPhaIQuanTri_RedirectIndex(string ma, string vaiTro)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, ma, vaiTro);

        var result = await ctrl.ApprovePost("P001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.CapNhatTrangThaiDuyetTinAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    // 6. Duyệt nhiều tin khác nhau → mỗi tin gọi đúng maPhong
    [Theory]
    [InlineData("P001")]
    [InlineData("P002")]
    [InlineData("P999")]
    public async Task ApprovePost_NhieuTin_GoiDungMaPhong(string maPhong)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapNhatTrangThaiDuyetTinAsync(maPhong, "KD001", true))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        await ctrl.ApprovePost(maPhong);

        repo.Verify(r => r.CapNhatTrangThaiDuyetTinAsync(maPhong, "KD001", true), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 3 – RejectPost: từ chối tin đăng
    // ═══════════════════════════════════════════════════════════════════════

    // 7. QuanTri từ chối tin → gọi CapNhatTrangThaiDuyetTin với KD003, redirect Moderation
    [Fact]
    public async Task RejectPost_QuanTri_ThanhCong()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapNhatTrangThaiDuyetTinAsync("P001", "KD003", false))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.RejectPost("P001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Moderation", redirect.ActionName);
        repo.Verify(r => r.CapNhatTrangThaiDuyetTinAsync("P001", "KD003", false), Times.Once);
    }

    // 8. Không phải QuanTri → redirect Index, không gọi repo
    [Theory]
    [InlineData("ND001", "NguoiDung")]
    [InlineData("CT001", "ChuTro")]
    public async Task RejectPost_KhongPhaIQuanTri_RedirectIndex(string ma, string vaiTro)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, ma, vaiTro);

        var result = await ctrl.RejectPost("P001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.CapNhatTrangThaiDuyetTinAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 4 – ReportManagement (GET): xem danh sách tố cáo chờ duyệt
    // ═══════════════════════════════════════════════════════════════════════

    // 9. QuanTri → trả về view danh sách tố cáo
    [Fact]
    public async Task ReportManagement_VaiTroQuanTri_TraVeView()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.LayDanhSachToCaoChoDuyetAsync())
            .ReturnsAsync(new List<ToCaoChoDuyetDto>
            {
                new() { MaToCao = "TC001", MaPhong = "P001", LoaiViPham = "Spam" }
            });
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.ReportManagement();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("kiem_duyet_to_cao", view.ViewName);
        repo.Verify(r => r.LayDanhSachToCaoChoDuyetAsync(), Times.Once);
    }

    // 10. Không phải QuanTri → redirect Index
    [Theory]
    [InlineData("ND001", "NguoiDung")]
    [InlineData("CT001", "ChuTro")]
    public async Task ReportManagement_KhongPhaIQuanTri_RedirectIndex(string ma, string vaiTro)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, ma, vaiTro);

        var result = await ctrl.ReportManagement();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.LayDanhSachToCaoChoDuyetAsync(), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 5 – ApproveReport: duyệt tố cáo
    // ═══════════════════════════════════════════════════════════════════════

    // 11. QuanTri duyệt tố cáo → gọi CapNhatTrangThaiDuyetToCao với KD001
    [Fact]
    public async Task ApproveReport_QuanTri_ThanhCong()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapNhatTrangThaiDuyetToCaoAsync("TC001", "KD001"))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.ApproveReport("TC001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ReportManagement", redirect.ActionName);
        repo.Verify(r => r.CapNhatTrangThaiDuyetToCaoAsync("TC001", "KD001"), Times.Once);
    }

    // 12. Không phải QuanTri → redirect Index, không gọi repo
    [Theory]
    [InlineData("ND001", "NguoiDung")]
    [InlineData("CT001", "ChuTro")]
    public async Task ApproveReport_KhongPhaIQuanTri_RedirectIndex(string ma, string vaiTro)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, ma, vaiTro);

        var result = await ctrl.ApproveReport("TC001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.CapNhatTrangThaiDuyetToCaoAsync(
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 6 – RejectReport: từ chối tố cáo
    // ═══════════════════════════════════════════════════════════════════════

    // 13. QuanTri từ chối tố cáo → gọi CapNhatTrangThaiDuyetToCao với KD003
    [Fact]
    public async Task RejectReport_QuanTri_ThanhCong()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapNhatTrangThaiDuyetToCaoAsync("TC001", "KD003"))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.RejectReport("TC001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ReportManagement", redirect.ActionName);
        repo.Verify(r => r.CapNhatTrangThaiDuyetToCaoAsync("TC001", "KD003"), Times.Once);
    }

    // 14. Không phải QuanTri → redirect Index, không gọi repo
    [Theory]
    [InlineData("ND001", "NguoiDung")]
    [InlineData("CT001", "ChuTro")]
    public async Task RejectReport_KhongPhaIQuanTri_RedirectIndex(string ma, string vaiTro)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, ma, vaiTro);

        var result = await ctrl.RejectReport("TC001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.CapNhatTrangThaiDuyetToCaoAsync(
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 7 – DeleteReport: xóa tố cáo
    // ═══════════════════════════════════════════════════════════════════════

    // 15. QuanTri xóa tố cáo → gọi XoaToCao, redirect ReportManagement
    [Fact]
    public async Task DeleteReport_QuanTri_ThanhCong()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.XoaToCaoAsync("TC001")).Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.DeleteReport("TC001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ReportManagement", redirect.ActionName);
        repo.Verify(r => r.XoaToCaoAsync("TC001"), Times.Once);
    }

    // 16. Không phải QuanTri → redirect Index, không gọi XoaToCao
    [Theory]
    [InlineData("ND001", "NguoiDung")]
    [InlineData("CT001", "ChuTro")]
    public async Task DeleteReport_KhongPhaIQuanTri_RedirectIndex(string ma, string vaiTro)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, ma, vaiTro);

        var result = await ctrl.DeleteReport("TC001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.XoaToCaoAsync(It.IsAny<string>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 8 – DeletePostFromReport: xóa bài đăng từ tố cáo
    // ═══════════════════════════════════════════════════════════════════════

    // 17. QuanTri xóa bài đăng từ tố cáo → gọi XoaPhong, redirect ReportManagement
    [Fact]
    public async Task DeletePostFromReport_QuanTri_ThanhCong()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.XoaPhongAsync("P001")).Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.DeletePostFromReport("P001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ReportManagement", redirect.ActionName);
        repo.Verify(r => r.XoaPhongAsync("P001"), Times.Once);
    }

    // 18. Không phải QuanTri → redirect Index, không gọi XoaPhong
    [Theory]
    [InlineData("ND001", "NguoiDung")]
    [InlineData("CT001", "ChuTro")]
    public async Task DeletePostFromReport_KhongPhaIQuanTri_RedirectIndex(string ma, string vaiTro)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, ma, vaiTro);

        var result = await ctrl.DeletePostFromReport("P001");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.XoaPhongAsync(It.IsAny<string>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 9 – Kiểm tra TempData SuccessMessage
    // ═══════════════════════════════════════════════════════════════════════

    // 19. ApprovePost → TempData có SuccessMessage
    [Fact]
    public async Task ApprovePost_CoSuccessMessage()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapNhatTrangThaiDuyetTinAsync("P001", "KD001", true))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        await ctrl.ApprovePost("P001");

        Assert.True(ctrl.TempData.ContainsKey("SuccessMessage"));
    }

    // 20. RejectPost → TempData có SuccessMessage
    [Fact]
    public async Task RejectPost_CoSuccessMessage()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapNhatTrangThaiDuyetTinAsync("P001", "KD003", false))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        await ctrl.RejectPost("P001");

        Assert.True(ctrl.TempData.ContainsKey("SuccessMessage"));
    }

    // 21. ApproveReport → TempData có SuccessMessage
    [Fact]
    public async Task ApproveReport_CoSuccessMessage()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapNhatTrangThaiDuyetToCaoAsync("TC001", "KD001"))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        await ctrl.ApproveReport("TC001");

        Assert.True(ctrl.TempData.ContainsKey("SuccessMessage"));
    }

    // 22. RejectReport → TempData có SuccessMessage
    [Fact]
    public async Task RejectReport_CoSuccessMessage()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.CapNhatTrangThaiDuyetToCaoAsync("TC001", "KD003"))
            .Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        await ctrl.RejectReport("TC001");

        Assert.True(ctrl.TempData.ContainsKey("SuccessMessage"));
    }

    // 23. DeleteReport → TempData có SuccessMessage
    [Fact]
    public async Task DeleteReport_CoSuccessMessage()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.XoaToCaoAsync("TC001")).Returns(Task.CompletedTask);
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        await ctrl.DeleteReport("TC001");

        Assert.True(ctrl.TempData.ContainsKey("SuccessMessage"));
    }
}