namespace TRo123.Models;

public class TinChoDuyetDto
{
    public string MaPhong { get; set; } = string.Empty;
    public string TenPhongTro { get; set; } = string.Empty;
    public string MaTaiKhoan { get; set; } = string.Empty;
    public string TenChuTro { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public double GiaPhong { get; set; }
    public double DienTich { get; set; }
    public DateTime? NgayDang { get; set; }
    public string TrangThaiDuyet { get; set; } = string.Empty;
}

