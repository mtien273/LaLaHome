using System.ComponentModel.DataAnnotations;

namespace TRo123.Models;

public class DangNhapViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    public string SoDienThoai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    public string MatKhau { get; set; } = string.Empty;
}
