using System.ComponentModel.DataAnnotations;

namespace TRo123.Models;

public class TaoTinPhongViewModel
{
    public string? MaPhong { get; set; }

    [Required]
    public string MaTaiKhoan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn loại phòng")]
    public string MaLoaiPhong { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
    [StringLength(100)]
    public string TenPhongTro { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public double GiaPhong { get; set; }

    [Range(0, double.MaxValue)]
    public double DienTich { get; set; }

    [Range(0, double.MaxValue)]
    public double GiaDien { get; set; }

    [Range(0, double.MaxValue)]
    public double GiaNuoc { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ")]
    public string SoDienThoai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn Tỉnh/TP")]
    public string MaTinhThanhPho { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn Quận/Huyện")]
    public string MaQuanHuyen { get; set; } = string.Empty;

    public string? MaXaPhuong { get; set; }

    [StringLength(100)]
    public string? DiaChiChiTiet { get; set; }
}

