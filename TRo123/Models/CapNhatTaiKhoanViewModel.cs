using System.ComponentModel.DataAnnotations;

namespace TRo123.Models;

public class CapNhatTaiKhoanViewModel
{
    [Required]
    public string MaTaiKhoan { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string HoTen { get; set; } = string.Empty;

    [Required]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Số điện thoại phải đủ 10 số")]
    public string SoDienThoai { get; set; } = string.Empty;

    [StringLength(12, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 12 ký tự")]
    public string? MatKhauMoi { get; set; }
}

