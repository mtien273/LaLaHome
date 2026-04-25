using System.ComponentModel.DataAnnotations;

namespace TRo123.Models;

public class TaoToCaoViewModel
{
    [Required]
    public string MaPhong { get; set; } = string.Empty;

    [Required]
    public string MaTaiKhoanNguoiBaoCao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn lý do tố cáo")]
    public string LoaiViPham { get; set; } = string.Empty;

    [StringLength(500)]
    public string? NoiDung { get; set; }
}

