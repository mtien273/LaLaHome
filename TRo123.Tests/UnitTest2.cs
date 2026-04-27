using Xunit;
using Moq;
using TRo123.Controllers;
using TRo123.Models;
using TRo123.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Hosting;

namespace TRo123.Tests;

public class CreatePost_Tests
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

    private static void SetupDanhMuc(Mock<ILaLaHomeRepository> repo)
    {
        repo.Setup(r => r.LayLoaiPhongAsync()).ReturnsAsync(new List<LoaiPhongDto>());
        repo.Setup(r => r.LayTinhThanhPhoAsync()).ReturnsAsync(new List<TinhThanhPhoDto>());
        repo.Setup(r => r.LayQuanHuyenAsync(It.IsAny<string>())).ReturnsAsync(new List<QuanHuyenDto>());
        repo.Setup(r => r.LayXaPhuongAsync(It.IsAny<string>())).ReturnsAsync(new List<XaPhuongDto>());
    }

    private static TaoTinPhongViewModel ValidModel() => new()
    {
        MaTaiKhoan = "CT001",
        MaLoaiPhong = "LP001",
        TenPhongTro = "Phòng trọ quận 1",
        GiaPhong = 3_000_000,
        DienTich = 25,
        GiaDien = 3500,
        GiaNuoc = 15000,
        SoDienThoai = "0399999999",
        MaTinhThanhPho = "01",
        MaQuanHuyen = "001",
        MaXaPhuong = "0001",
        DiaChiChiTiet = "123 Đường ABC"
    };

    private static IFormFile FakeImage(string fileName, string contentType, long sizeBytes)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(sizeBytes);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[sizeBytes]));
        mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock.Object;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 1 – GET CreatePostForm: kiểm tra quyền truy cập
    // ═══════════════════════════════════════════════════════════════════════

    // 1. Chưa đăng nhập → redirect Login
    [Fact]
    public async Task CreatePostForm_ChuaDangNhap_RedirectLogin()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildControllerNoSession(repo);

        var result = await ctrl.CreatePostForm();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
    }

    // 2. Vai trò NguoiDung → redirect Index
    [Fact]
    public async Task CreatePostForm_VaiTroNguoiDung_RedirectIndex()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, "ND001", "NguoiDung");

        var result = await ctrl.CreatePostForm();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    // 3. Vai trò ChuTro → trả về view dang_tin_moi
    [Fact]
    public async Task CreatePostForm_VaiTroChuTro_TraVeView()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        SetupDanhMuc(repo);
        repo.Setup(r => r.LayTaiKhoanTheoMaAsync("CT001"))
            .ReturnsAsync(new TaiKhoanDto
            {
                MaTaiKhoan = "CT001",
                SoDienThoai = "0399999999",
                HoTen = "Chủ Trọ",
                VaiTro = "ChuTro"
            });
        var ctrl = BuildController(repo, "CT001", "ChuTro");

        var result = await ctrl.CreatePostForm();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("dang_tin_moi", view.ViewName);
    }

    // 4. Vai trò QuanTri → cũng được phép vào trang đăng tin
    [Fact]
    public async Task CreatePostForm_VaiTroQuanTri_TraVeView()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        SetupDanhMuc(repo);
        repo.Setup(r => r.LayTaiKhoanTheoMaAsync("QT001"))
            .ReturnsAsync(new TaiKhoanDto
            {
                MaTaiKhoan = "QT001",
                SoDienThoai = "0388888888",
                HoTen = "Quản Trị",
                VaiTro = "QuanTri"
            });
        var ctrl = BuildController(repo, "QT001", "QuanTri");

        var result = await ctrl.CreatePostForm();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("dang_tin_moi", view.ViewName);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 2 – POST CreatePost: kiểm tra quyền
    // ═══════════════════════════════════════════════════════════════════════

    // 5. Chưa đăng nhập POST → redirect Login, không gọi TaoTinPhong
    [Fact]
    public async Task CreatePost_ChuaDangNhap_RedirectLogin()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildControllerNoSession(repo);
        var env = Mock.Of<IWebHostEnvironment>();

        var result = await ctrl.CreatePost(ValidModel(), null, env);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        repo.Verify(r => r.TaoTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()), Times.Never);
    }

    // 6. Vai trò NguoiDung POST → redirect Index, không gọi TaoTinPhong
    [Fact]
    public async Task CreatePost_VaiTroNguoiDung_RedirectIndex()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        var ctrl = BuildController(repo, "ND001", "NguoiDung");
        var env = Mock.Of<IWebHostEnvironment>();

        var result = await ctrl.CreatePost(ValidModel(), null, env);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        repo.Verify(r => r.TaoTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 3 – POST CreatePost: ModelState không hợp lệ
    // ═══════════════════════════════════════════════════════════════════════

    // 7. ModelState lỗi → trả về view, không gọi TaoTinPhong
    [Fact]
    public async Task CreatePost_ModelStateKhongHopLe_TraVeView()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        SetupDanhMuc(repo);
        var ctrl = BuildController(repo, "CT001", "ChuTro");
        ctrl.ModelState.AddModelError("TenPhongTro", "Vui lòng nhập tiêu đề");
        var env = Mock.Of<IWebHostEnvironment>();

        var result = await ctrl.CreatePost(new TaoTinPhongViewModel(), null, env);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("dang_tin_moi", view.ViewName);
        repo.Verify(r => r.TaoTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 4 – POST CreatePost: đăng bài thành công
    // ═══════════════════════════════════════════════════════════════════════

    // 8. Đăng tin thành công, không có ảnh → redirect Detail
    [Fact]
    public async Task CreatePost_HopLe_KhongAnh_ThanhCong()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.TaoTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()))
            .ReturnsAsync("P9999");
        var ctrl = BuildController(repo, "CT001", "ChuTro");
        var env = Mock.Of<IWebHostEnvironment>();

        var result = await ctrl.CreatePost(ValidModel(), null, env);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
        Assert.Equal("P9999", redirect.RouteValues?["id"]);
        repo.Verify(r => r.TaoTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()), Times.Once);
        repo.Verify(r => r.UploadAnhAsync(It.IsAny<IFormFile>(), It.IsAny<IWebHostEnvironment>()), Times.Never);
    }

    // 9. Đăng tin thành công có ảnh hợp lệ → redirect Detail, gọi UploadAnh + LuuAnh
    [Fact]
    public async Task CreatePost_HopLe_CoAnh_ThanhCong()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.TaoTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()))
            .ReturnsAsync("P9999");
        repo.Setup(r => r.UploadAnhAsync(It.IsAny<IFormFile>(), It.IsAny<IWebHostEnvironment>()))
            .ReturnsAsync("/uploads/anh.jpg");
        repo.Setup(r => r.LuuAnhVaoDbAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var ctrl = BuildController(repo, "CT001", "ChuTro");
        var env = Mock.Of<IWebHostEnvironment>();
        var anh = FakeImage("anh.jpg", "image/jpeg", 1 * 1024 * 1024);

        var result = await ctrl.CreatePost(ValidModel(), anh, env);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
        repo.Verify(r => r.UploadAnhAsync(It.IsAny<IFormFile>(), It.IsAny<IWebHostEnvironment>()), Times.Once);
        repo.Verify(r => r.LuuAnhVaoDbAsync("P9999", "CT001", "/uploads/anh.jpg"), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 5 – POST CreatePost: kiểm tra ảnh
    // ═══════════════════════════════════════════════════════════════════════

    // 10. Ảnh sai định dạng (exe) → trả về view, có lỗi anhPhong
    [Fact]
    public async Task CreatePost_AnhSaiDinhDang_TraVeViewCoLoi()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        SetupDanhMuc(repo);
        var ctrl = BuildController(repo, "CT001", "ChuTro");
        var env = Mock.Of<IWebHostEnvironment>();
        var anh = FakeImage("file.exe", "application/octet-stream", 500 * 1024);

        var result = await ctrl.CreatePost(ValidModel(), anh, env);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("dang_tin_moi", view.ViewName);
        Assert.True(ctrl.ModelState.ContainsKey("anhPhong"));
        repo.Verify(r => r.TaoTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()), Times.Never);
    }

    // 11. Ảnh vượt quá 10MB → trả về view, có lỗi anhPhong
    [Fact]
    public async Task CreatePost_AnhQua10MB_TraVeViewCoLoi()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        SetupDanhMuc(repo);
        var ctrl = BuildController(repo, "CT001", "ChuTro");
        var env = Mock.Of<IWebHostEnvironment>();
        var anh = FakeImage("anh.jpg", "image/jpeg", 11 * 1024 * 1024);

        var result = await ctrl.CreatePost(ValidModel(), anh, env);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("dang_tin_moi", view.ViewName);
        Assert.True(ctrl.ModelState.ContainsKey("anhPhong"));
        repo.Verify(r => r.TaoTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()), Times.Never);
    }

    // 12. Các extension ảnh hợp lệ → đăng thành công
    [Theory]
    [InlineData("anh.png", "image/png")]
    [InlineData("anh.gif", "image/gif")]
    [InlineData("anh.webp", "image/webp")]
    [InlineData("anh.bmp", "image/bmp")]
    [InlineData("anh.jfif", "image/jpeg")]
    public async Task CreatePost_ExtensionAnhHopLe_ThanhCong(string fileName, string mime)
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.TaoTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()))
            .ReturnsAsync("P9999");
        repo.Setup(r => r.UploadAnhAsync(It.IsAny<IFormFile>(), It.IsAny<IWebHostEnvironment>()))
            .ReturnsAsync("/uploads/" + fileName);
        repo.Setup(r => r.LuuAnhVaoDbAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var ctrl = BuildController(repo, "CT001", "ChuTro");
        var env = Mock.Of<IWebHostEnvironment>();
        var anh = FakeImage(fileName, mime, 1 * 1024 * 1024);

        var result = await ctrl.CreatePost(ValidModel(), anh, env);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
    }

    // 13. Upload ảnh ném exception → đăng tin vẫn thành công, TempData có ErrorMessage
    [Fact]
    public async Task CreatePost_UploadAnhThatBai_TinVanThanhCong()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        repo.Setup(r => r.TaoTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()))
            .ReturnsAsync("P9999");
        repo.Setup(r => r.UploadAnhAsync(It.IsAny<IFormFile>(), It.IsAny<IWebHostEnvironment>()))
            .ThrowsAsync(new Exception("Lỗi upload"));

        var ctrl = BuildController(repo, "CT001", "ChuTro");
        var env = Mock.Of<IWebHostEnvironment>();
        var anh = FakeImage("anh.jpg", "image/jpeg", 1 * 1024 * 1024);

        var result = await ctrl.CreatePost(ValidModel(), anh, env);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
        Assert.Equal("P9999", redirect.RouteValues?["id"]);
        Assert.True(ctrl.TempData.ContainsKey("ErrorMessage"));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NHÓM 6 – POST CreatePost: lỗi từ repository
    // ═══════════════════════════════════════════════════════════════════════

    // 14. TaoTinPhongAsync ném exception → trả về view, ModelState có lỗi
    [Fact]
    public async Task CreatePost_RepositoryNemException_TraVeViewCoLoi()
    {
        var repo = new Mock<ILaLaHomeRepository>();
        SetupDanhMuc(repo);
        repo.Setup(r => r.TaoTinPhongAsync(It.IsAny<TaoTinPhongViewModel>()))
            .ThrowsAsync(new Exception("DB error"));

        var ctrl = BuildController(repo, "CT001", "ChuTro");
        var env = Mock.Of<IWebHostEnvironment>();

        var result = await ctrl.CreatePost(ValidModel(), null, env);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("dang_tin_moi", view.ViewName);
        Assert.False(ctrl.ModelState.IsValid);
    }
}

// ─── FakeSession: thay thế Mock<ISession> để tránh lỗi out-param ────────────
public class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public FakeSession(Dictionary<string, string> init)
    {
        foreach (var kv in init)
            _store[kv.Key] = System.Text.Encoding.UTF8.GetBytes(kv.Value);
    }

    public bool IsAvailable => true;
    public string Id => "fake-session";
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;

    public bool TryGetValue(string key, out byte[] value)
    {
        if (_store.TryGetValue(key, out var v)) { value = v; return true; }
        value = Array.Empty<byte>();
        return false;
    }
}