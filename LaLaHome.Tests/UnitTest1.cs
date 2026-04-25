using Xunit;
using Moq;
using TRo123.Controllers;
using TRo123.Models;
using TRo123.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class Register_SQL_Tests
{
    private readonly Mock<ILaLaHomeRepository> _repoMock;
    private readonly HomeController _controller;

    public Register_SQL_Tests()
    {
        _repoMock = new Mock<ILaLaHomeRepository>();
        _controller = new HomeController(_repoMock.Object);
    }

    // 1. Đăng ký thành công với SĐT mới, mật khẩu hợp lệ, vai trò hợp lệ
    [Fact]
    public async Task Register_NewPhone_ValidPassword_ValidRole_Success()
    {
        var model = new DangKyTaiKhoanViewModel
        {
            HoTen = "User Test",
            SoDienThoai = "0399999999",
            MatKhau = "abc12345",
            VaiTro = "NguoiDung"
        };
        _repoMock.Setup(r => r.TaoTaiKhoanAsync(It.IsAny<DangKyTaiKhoanViewModel>()))
            .ReturnsAsync("TAIKHOAN_MOI");

        var result = await _controller.Register(model);

        Assert.IsType<RedirectToActionResult>(result);
        _repoMock.Verify(r => r.TaoTaiKhoanAsync(It.IsAny<DangKyTaiKhoanViewModel>()), Times.Once);
    }

    // 2. Đăng ký thất bại do trùng SĐT
    [Fact]
    public async Task Register_DuplicatePhone_Fail()
    {
        var model = new DangKyTaiKhoanViewModel
        {
            HoTen = "User Test",
            SoDienThoai = "0900000001",
            MatKhau = "abc12345",
            VaiTro = "NguoiDung"
        };
        _repoMock.Setup(r => r.TaoTaiKhoanAsync(It.IsAny<DangKyTaiKhoanViewModel>()))
            .ReturnsAsync((string)null);

        var result = await _controller.Register(model);

        Assert.IsType<ViewResult>(result);
        _repoMock.Verify(r => r.TaoTaiKhoanAsync(It.IsAny<DangKyTaiKhoanViewModel>()), Times.Once);
    }

    // 3. SĐT sai định dạng (không khớp regex)
    [Theory]
    [InlineData("0111111111")]
    [InlineData("1234567890")]
    [InlineData("098765432")] // thiếu số
    [InlineData("abcdefghij")] // không phải số
    public async Task Register_InvalidPhoneFormat_Fail(string invalidPhone)
    {
        var model = new DangKyTaiKhoanViewModel
        {
            HoTen = "User",
            SoDienThoai = invalidPhone,
            MatKhau = "abc12345",
            VaiTro = "NguoiDung"
        };
        _controller.ModelState.AddModelError("SoDienThoai", "Sai định dạng");

        var result = await _controller.Register(model);

        Assert.IsType<ViewResult>(result);
        _repoMock.Verify(r => r.TaoTaiKhoanAsync(It.IsAny<DangKyTaiKhoanViewModel>()), Times.Never);
    }

    // 4. Mật khẩu không hợp lệ (chỉ số, chỉ chữ, quá ngắn, quá dài, không có cả chữ và số)
    [Theory]
    [InlineData("123456")] // chỉ số
    [InlineData("abcdef")] // chỉ chữ
    [InlineData("a1")]     // quá ngắn
    [InlineData("a1b2c3d4e5f6g7")] // quá dài
    [InlineData("!!!!!!")] // ký tự đặc biệt, không có chữ/số
    public async Task Register_InvalidPassword_Fail(string invalidPassword)
    {
        var model = new DangKyTaiKhoanViewModel
        {
            HoTen = "User",
            SoDienThoai = "0398888888",
            MatKhau = invalidPassword,
            VaiTro = "NguoiDung"
        };
        _controller.ModelState.AddModelError("MatKhau", "Sai format");

        var result = await _controller.Register(model);

        Assert.IsType<ViewResult>(result);
        _repoMock.Verify(r => r.TaoTaiKhoanAsync(It.IsAny<DangKyTaiKhoanViewModel>()), Times.Never);
    }

    // 5. Vai trò không hợp lệ (null, rỗng, không đúng giá trị)
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AdminFake")]
    public async Task Register_InvalidRole_Fail(string invalidRole)
    {
        var model = new DangKyTaiKhoanViewModel
        {
            HoTen = "User",
            SoDienThoai = "0398888888",
            MatKhau = "abc12345",
            VaiTro = invalidRole
        };
        _controller.ModelState.AddModelError("VaiTro", "Vai trò không hợp lệ");

        var result = await _controller.Register(model);

        Assert.IsType<ViewResult>(result);
        _repoMock.Verify(r => r.TaoTaiKhoanAsync(It.IsAny<DangKyTaiKhoanViewModel>()), Times.Never);
    }

    // 6. Thiếu dữ liệu (tất cả trường rỗng)
    [Fact]
    public async Task Register_EmptyModel_Fail()
    {
        var model = new DangKyTaiKhoanViewModel();
        _controller.ModelState.AddModelError("All", "Thiếu dữ liệu");

        var result = await _controller.Register(model);

        Assert.IsType<ViewResult>(result);
        _repoMock.Verify(r => r.TaoTaiKhoanAsync(It.IsAny<DangKyTaiKhoanViewModel>()), Times.Never);
    }

    // 7. Họ tên rỗng hoặc null
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Register_EmptyName_Fail(string emptyName)
    {
        var model = new DangKyTaiKhoanViewModel
        {
            HoTen = emptyName,
            SoDienThoai = "0398888888",
            MatKhau = "abc12345",
            VaiTro = "NguoiDung"
        };
        _controller.ModelState.AddModelError("HoTen", "Vui lòng nhập họ tên");

        var result = await _controller.Register(model);

        Assert.IsType<ViewResult>(result);
        _repoMock.Verify(r => r.TaoTaiKhoanAsync(It.IsAny<DangKyTaiKhoanViewModel>()), Times.Never);
    }

    // 8. SĐT rỗng hoặc null
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Register_EmptyPhone_Fail(string emptyPhone)
    {
        var model = new DangKyTaiKhoanViewModel
        {
            HoTen = "User",
            SoDienThoai = emptyPhone,
            MatKhau = "abc12345",
            VaiTro = "NguoiDung"
        };
        _controller.ModelState.AddModelError("SoDienThoai", "Vui lòng nhập số điện thoại");

        var result = await _controller.Register(model);

        Assert.IsType<ViewResult>(result);
        _repoMock.Verify(r => r.TaoTaiKhoanAsync(It.IsAny<DangKyTaiKhoanViewModel>()), Times.Never);
    }

    // 9. Mật khẩu rỗng hoặc null
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Register_EmptyPassword_Fail(string emptyPassword)
    {
        var model = new DangKyTaiKhoanViewModel
        {
            HoTen = "User",
            SoDienThoai = "0398888888",
            MatKhau = emptyPassword,
            VaiTro = "NguoiDung"
        };
        _controller.ModelState.AddModelError("MatKhau", "Vui lòng nhập mật khẩu");

        var result = await _controller.Register(model);

        Assert.IsType<ViewResult>(result);
        _repoMock.Verify(r => r.TaoTaiKhoanAsync(It.IsAny<DangKyTaiKhoanViewModel>()), Times.Never);
    }
}
