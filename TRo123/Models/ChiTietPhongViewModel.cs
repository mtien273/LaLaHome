namespace TRo123.Models;

public class ChiTietPhongViewModel : PhongTroItemViewModel
{
    public string LoaiPhong { get; set; } = string.Empty;
    public double GiaDien { get; set; }
    public double GiaNuoc { get; set; }
    public DateTime? NgayDang { get; set; }
}
