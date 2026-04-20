namespace TRo123.Models;

public class PhongTroItemViewModel
{
    public string MaPhong { get; set; } = string.Empty;
    public string TenPhong { get; set; } = string.Empty;
    public double GiaPhong { get; set; }
    public double DienTich { get; set; }
    public string DiaChiDayDu { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string? DuongDanAnh { get; set; }
}
