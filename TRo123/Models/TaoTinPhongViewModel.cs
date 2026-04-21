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

    [Required(ErrorMessage = "Vui lòng nhập giá cho thuê")]
    [Range(1000, double.MaxValue, ErrorMessage = "Giá cho thuê phải lớn hơn 0")]
    public double? GiaPhong { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập diện tích")]
    [Range(1, double.MaxValue, ErrorMessage = "Diện tích phải lớn hơn 0")]
    public double? DienTich { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá điện")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá điện không hợp lệ")]
    public double? GiaDien { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá nước")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá nước không hợp lệ")]
    public double? GiaNuoc { get; set; }

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

