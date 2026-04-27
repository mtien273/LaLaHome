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
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (ví dụ: 09xxxxxxxx hoặc +84xxxxxxxxx).")]
    public string SoDienThoai { get; set; } = string.Empty;

    public string VaiTro { get; set; } = string.Empty;
}

