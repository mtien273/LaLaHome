using System.ComponentModel.DataAnnotations;

namespace TRo123.Models;

public class CapNhatTaiKhoanQuanTriViewModel
{
    [Required]
    public string MaTaiKhoan { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string HoTen { get; set; } = string.Empty;

    [Required]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Số điện thoại phải đủ 10 số")]
    public string SoDienThoai { get; set; } = string.Empty;

    public string VaiTro { get; set; } = string.Empty;
}

