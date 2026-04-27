using System.ComponentModel.DataAnnotations;

namespace TRo123.Models;

public class DangNhapViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (ví dụ: 09xxxxxxxx hoặc +84xxxxxxxxx).")]
    public string SoDienThoai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    public string MatKhau { get; set; } = string.Empty;
}
