namespace TRo123.Models;

public class TinCuaToiDto
{
    public string MaPhong { get; set; } = string.Empty;
    public string TenPhongTro { get; set; } = string.Empty;
    public double GiaPhong { get; set; }
    public double DienTich { get; set; }
    public DateTime? NgayDang { get; set; }
    public string TrangThaiDuyet { get; set; } = string.Empty;
    public bool TrangThaiHienThi { get; set; }
}

