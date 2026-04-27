using System.ComponentModel.DataAnnotations;

namespace TRo123.Models;

public class DangKyTaiKhoanViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(30)]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (ví dụ: 09xxxxxxxx hoặc +84xxxxxxxxx).")]
    public string SoDienThoai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [StringLength(12, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 12 ký tự")]
    public string MatKhau { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn loại tài khoản")]
    public string VaiTro { get; set; } = "NguoiDung"; // NguoiDung: người tìm kiếm, ChuTro: người cho thuê
}
