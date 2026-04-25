using TRo123.Models;

namespace TRo123.Services;

public interface ILaLaHomeRepository
{
    Task<List<PhongTroItemViewModel>> LayDanhSachPhongAsync(int soLuong = 30);
    Task<List<PhongTroItemViewModel>> TimKiemPhongAsync(TimKiemPhongViewModel boLoc, int soLuong = 200);
    Task<ChiTietPhongViewModel?> LayChiTietPhongAsync(string maPhong);
    Task<ChiTietPhongViewModel?> LayChiTietPhongChoPhepAsync(string maPhong, string? maTaiKhoan, bool laQuanTri);
    Task<string> TaoTaiKhoanAsync(DangKyTaiKhoanViewModel model); 
    Task<bool> KiemTraSoDienThoaiTonTaiAsync(string soDienThoai);
    Task<bool> KiemTraDangNhapAsync(DangNhapViewModel model);
    Task<TaiKhoanDto?> LayTaiKhoanTheoDangNhapAsync(DangNhapViewModel model);
    Task<TaiKhoanDto?> LayTaiKhoanTheoMaAsync(string maTaiKhoan);
    Task CapNhatThongTinTaiKhoanAsync(CapNhatTaiKhoanViewModel model);
    Task<List<TaiKhoanDto>> LayDanhSachTaiKhoanAsync();
    Task CapNhatTaiKhoanBoiQuanTriAsync(CapNhatTaiKhoanQuanTriViewModel model);
    Task CapLaiMatKhauAsync(string maTaiKhoan, string matKhauMoi);

    Task<List<LoaiPhongDto>> LayLoaiPhongAsync();
    Task<List<TinhThanhPhoDto>> LayTinhThanhPhoAsync();
    Task<List<QuanHuyenDto>> LayQuanHuyenAsync(string maTinhThanhPho);
    Task<List<XaPhuongDto>> LayXaPhuongAsync(string maQuanHuyen);

    Task<string> TaoTinPhongAsync(TaoTinPhongViewModel model);
    Task<TaoTinPhongViewModel?> LayTinPhongDeSuaAsync(string maPhong);
    Task CapNhatTinPhongAsync(TaoTinPhongViewModel model);
    Task<List<TinCuaToiDto>> LayDanhSachTinCuaToiAsync(string maTaiKhoan);
    Task<List<TinChoDuyetDto>> LayDanhSachTinChoDuyetAsync();
    Task CapNhatTrangThaiDuyetTinAsync(string maPhong, string maKiemDuyet, bool trangThaiHoatDong);

    Task<string> TaoToCaoAsync(TaoToCaoViewModel model);
    Task<List<ToCaoChoDuyetDto>> LayDanhSachToCaoChoDuyetAsync();
    Task<List<ToCaoChoDuyetDto>> LayDanhSachToCaoCuaToiAsync(string maTaiKhoan);
    Task<ToCaoChoDuyetDto?> LayToCaoTheoMaAsync(string maToCao);
    Task<bool> CapNhatToCaoChoNguoiDungAsync(string maToCao, string maTaiKhoanNguoiBaoCao, string loaiViPham, string? noiDung);
    Task<bool> XoaToCaoChoNguoiDungAsync(string maToCao, string maTaiKhoanNguoiBaoCao);
    Task CapNhatTrangThaiDuyetToCaoAsync(string maToCao, string maKiemDuyet);
    Task XoaToCaoAsync(string maToCao);
    Task XoaPhongAsync(string maPhong);
    Task<string?> LayChuPhongTheoMaPhongAsync(string maPhong);

    Task XoaTaiKhoanAsync(string maTaiKhoan);

    Task<string> UploadAnhAsync(IFormFile file, IWebHostEnvironment env);
    Task LuuAnhVaoDbAsync(string maPhong, string maTaiKhoan, string duongDan);
}
