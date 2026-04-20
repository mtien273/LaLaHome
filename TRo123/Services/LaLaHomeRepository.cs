using Microsoft.Data.SqlClient;
using System.Text.Json;
using TRo123.Models;

namespace TRo123.Services;

public class LaLaHomeRepository(IConfiguration configuration) : ILaLaHomeRepository
{
    private static readonly SemaphoreSlim ReportLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static string ReportStorePath => Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "reports.json");

    private readonly string _connectionString = configuration.GetConnectionString("LaLaHomeDb")
        ?? throw new InvalidOperationException("Thiếu ConnectionStrings:LaLaHomeDb trong appsettings.json");

    public async Task<List<PhongTroItemViewModel>> LayDanhSachPhongAsync(int soLuong = 30)
    {
        const string sql = """
            SELECT TOP (@SoLuong)
                p.PK_MaPhong,
                ISNULL(p.sTenPhongTro, N'Phòng trọ') AS sTenPhongTro,
                ISNULL(p.fGiaPhong, 0) AS fGiaPhong,
                ISNULL(p.fDienTich, 0) AS fDienTich,
                ISNULL(dc.sDiaChiChiTiet, N'') + N' ' +
                ISNULL(xa.sTenXaPhuongThiTran, N'') + N' ' +
                ISNULL(qh.sTenQuanHuyen, N'') + N' ' +
                ISNULL(tp.sTenTinhThanhPho, N'') AS DiaChiDayDu,
                ISNULL(p.sSDT, N'') AS sSDT,
                (SELECT TOP 1 sDuongDan FROM tblAnh a WHERE a.FK_MaPhong = p.PK_MaPhong) AS sDuongDan
            FROM tblPhong p
            LEFT JOIN tblDiaChi dc ON dc.FK_MaPhong = p.PK_MaPhong
            LEFT JOIN tblXaPhuongThiTran xa ON xa.PK_MaXaPhuongThiTran = dc.FK_MaXaPhuongThiTran
            LEFT JOIN tblQuanHuyen qh ON qh.PK_MaQuanHuyen = dc.FK_MaQuanHuyen
            LEFT JOIN tblTinhThanhPho tp ON tp.PK_MaTinhThanhPho = dc.FK_MaTinhThanhPho
            WHERE p.FK_MaKiemDuyet = N'KD001' AND ISNULL(p.bTrangThai, 0) = 1
            ORDER BY p.dNgayDang DESC, p.PK_MaPhong DESC;
            """;

        var result = new List<PhongTroItemViewModel>();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SoLuong", soLuong);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new PhongTroItemViewModel
            {
                MaPhong = reader["PK_MaPhong"]?.ToString() ?? string.Empty,
                TenPhong = reader["sTenPhongTro"]?.ToString() ?? string.Empty,
                GiaPhong = Convert.ToDouble(reader["fGiaPhong"]),
                DienTich = Convert.ToDouble(reader["fDienTich"]),
                DiaChiDayDu = reader["DiaChiDayDu"]?.ToString()?.Trim() ?? string.Empty,
                SoDienThoai = reader["sSDT"]?.ToString() ?? string.Empty,
                DuongDanAnh = reader["sDuongDan"] as string
            });
        }

        return result;
    }

    public async Task<ChiTietPhongViewModel?> LayChiTietPhongAsync(string maPhong)
    {
        const string sql = """
            SELECT TOP 1
                p.PK_MaPhong,
                ISNULL(p.sTenPhongTro, N'Phòng trọ') AS sTenPhongTro,
                ISNULL(p.fGiaPhong, 0) AS fGiaPhong,
                ISNULL(p.fDienTich, 0) AS fDienTich,
                ISNULL(p.fGiaDien, 0) AS fGiaDien,
                ISNULL(p.fGiaNuoc, 0) AS fGiaNuoc,
                p.dNgayDang,
                ISNULL(p.sSDT, N'') AS sSDT,
                ISNULL(lp.sTenLoaiPhong, N'') AS sTenLoaiPhong,
                ISNULL(dc.sDiaChiChiTiet, N'') + N' ' +
                ISNULL(xa.sTenXaPhuongThiTran, N'') + N' ' +
                ISNULL(qh.sTenQuanHuyen, N'') + N' ' +
                ISNULL(tp.sTenTinhThanhPho, N'') AS DiaChiDayDu,
                (SELECT TOP 1 sDuongDan FROM tblAnh a WHERE a.FK_MaPhong = p.PK_MaPhong) AS sDuongDan
            FROM tblPhong p
            LEFT JOIN tblLoaiPhong lp ON lp.PK_MaLoaiPhong = p.FK_MaLoaiPhong
            LEFT JOIN tblDiaChi dc ON dc.FK_MaPhong = p.PK_MaPhong
            LEFT JOIN tblXaPhuongThiTran xa ON xa.PK_MaXaPhuongThiTran = dc.FK_MaXaPhuongThiTran
            LEFT JOIN tblQuanHuyen qh ON qh.PK_MaQuanHuyen = dc.FK_MaQuanHuyen
            LEFT JOIN tblTinhThanhPho tp ON tp.PK_MaTinhThanhPho = dc.FK_MaTinhThanhPho
            WHERE p.PK_MaPhong = @MaPhong AND p.FK_MaKiemDuyet = N'KD001' AND ISNULL(p.bTrangThai, 0) = 1;
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaPhong", maPhong);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new ChiTietPhongViewModel
        {
            MaPhong = reader["PK_MaPhong"]?.ToString() ?? string.Empty,
            TenPhong = reader["sTenPhongTro"]?.ToString() ?? string.Empty,
            GiaPhong = Convert.ToDouble(reader["fGiaPhong"]),
            DienTich = Convert.ToDouble(reader["fDienTich"]),
            GiaDien = Convert.ToDouble(reader["fGiaDien"]),
            GiaNuoc = Convert.ToDouble(reader["fGiaNuoc"]),
            NgayDang = reader["dNgayDang"] == DBNull.Value ? null : Convert.ToDateTime(reader["dNgayDang"]),
            SoDienThoai = reader["sSDT"]?.ToString() ?? string.Empty,
            LoaiPhong = reader["sTenLoaiPhong"]?.ToString() ?? string.Empty,
            DiaChiDayDu = reader["DiaChiDayDu"]?.ToString()?.Trim() ?? string.Empty,
            DuongDanAnh = reader["sDuongDan"] as string
        };
    }

    public async Task<ChiTietPhongViewModel?> LayChiTietPhongChoPhepAsync(string maPhong, string? maTaiKhoan, bool laQuanTri)
    {
        // Nếu là quản trị hoặc chủ tin -> xem được cả tin chưa duyệt / bị tắt
        const string sql = """
            SELECT TOP 1
                p.PK_MaPhong,
                ISNULL(p.sTenPhongTro, N'Phòng trọ') AS sTenPhongTro,
                ISNULL(p.fGiaPhong, 0) AS fGiaPhong,
                ISNULL(p.fDienTich, 0) AS fDienTich,
                ISNULL(p.fGiaDien, 0) AS fGiaDien,
                ISNULL(p.fGiaNuoc, 0) AS fGiaNuoc,
                p.dNgayDang,
                ISNULL(p.sSDT, N'') AS sSDT,
                ISNULL(lp.sTenLoaiPhong, N'') AS sTenLoaiPhong,
                ISNULL(dc.sDiaChiChiTiet, N'') + N' ' +
                ISNULL(xa.sTenXaPhuongThiTran, N'') + N' ' +
                ISNULL(qh.sTenQuanHuyen, N'') + N' ' +
                ISNULL(tp.sTenTinhThanhPho, N'') AS DiaChiDayDu,
                (SELECT TOP 1 sDuongDan FROM tblAnh a WHERE a.FK_MaPhong = p.PK_MaPhong) AS sDuongDan
            FROM tblPhong p
            LEFT JOIN tblLoaiPhong lp ON lp.PK_MaLoaiPhong = p.FK_MaLoaiPhong
            LEFT JOIN tblDiaChi dc ON dc.FK_MaPhong = p.PK_MaPhong
            LEFT JOIN tblXaPhuongThiTran xa ON xa.PK_MaXaPhuongThiTran = dc.FK_MaXaPhuongThiTran
            LEFT JOIN tblQuanHuyen qh ON qh.PK_MaQuanHuyen = dc.FK_MaQuanHuyen
            LEFT JOIN tblTinhThanhPho tp ON tp.PK_MaTinhThanhPho = dc.FK_MaTinhThanhPho
            WHERE p.PK_MaPhong = @MaPhong
              AND (
                    (p.FK_MaKiemDuyet = N'KD001' AND ISNULL(p.bTrangThai, 0) = 1)
                    OR (@LaQuanTri = 1)
                    OR (p.FK_MaTaiKhoan = @MaTaiKhoan)
                  );
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaPhong", maPhong);
        cmd.Parameters.AddWithValue("@LaQuanTri", laQuanTri ? 1 : 0);
        cmd.Parameters.AddWithValue("@MaTaiKhoan", (object?)maTaiKhoan ?? DBNull.Value);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new ChiTietPhongViewModel
        {
            MaPhong = reader["PK_MaPhong"]?.ToString() ?? string.Empty,
            TenPhong = reader["sTenPhongTro"]?.ToString() ?? string.Empty,
            GiaPhong = Convert.ToDouble(reader["fGiaPhong"]),
            DienTich = Convert.ToDouble(reader["fDienTich"]),
            GiaDien = Convert.ToDouble(reader["fGiaDien"]),
            GiaNuoc = Convert.ToDouble(reader["fGiaNuoc"]),
            NgayDang = reader["dNgayDang"] == DBNull.Value ? null : Convert.ToDateTime(reader["dNgayDang"]),
            SoDienThoai = reader["sSDT"]?.ToString() ?? string.Empty,
            LoaiPhong = reader["sTenLoaiPhong"]?.ToString() ?? string.Empty,
            DiaChiDayDu = reader["DiaChiDayDu"]?.ToString()?.Trim() ?? string.Empty,
            DuongDanAnh = reader["sDuongDan"] as string
        };
    }

    public async Task<string> TaoTaiKhoanAsync(DangKyTaiKhoanViewModel model)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var maMoi = await TaoMaTaiKhoanMoiAsync(conn);
        const string sql = """
            INSERT INTO tblTaiKhoan(PK_MaTaiKhoan, sMatKhau, sSDT, sHoTen, sVaiTro)
            VALUES(@MaTaiKhoan, @MatKhau, @SoDienThoai, @HoTen, @VaiTro);
            """;
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaTaiKhoan", maMoi);
        cmd.Parameters.AddWithValue("@MatKhau", model.MatKhau);
        cmd.Parameters.AddWithValue("@SoDienThoai", model.SoDienThoai);
        cmd.Parameters.AddWithValue("@HoTen", model.HoTen);
        cmd.Parameters.AddWithValue("@VaiTro", string.Equals(model.VaiTro, "ChuTro", StringComparison.OrdinalIgnoreCase) ? "ChuTro" : "NguoiDung");
        await cmd.ExecuteNonQueryAsync();
        return maMoi;
    }

    public async Task<bool> KiemTraDangNhapAsync(DangNhapViewModel model)
    {
        return await LayTaiKhoanTheoDangNhapAsync(model) is not null;
    }

    public async Task<TaiKhoanDto?> LayTaiKhoanTheoDangNhapAsync(DangNhapViewModel model)
    {
        const string sql = """
            SELECT TOP 1 PK_MaTaiKhoan, sSDT, sHoTen, sVaiTro
            FROM tblTaiKhoan
            WHERE sSDT = @SoDienThoai AND sMatKhau = @MatKhau;
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SoDienThoai", model.SoDienThoai);
        cmd.Parameters.AddWithValue("@MatKhau", model.MatKhau);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new TaiKhoanDto
        {
            MaTaiKhoan = reader["PK_MaTaiKhoan"]?.ToString() ?? string.Empty,
            SoDienThoai = reader["sSDT"]?.ToString() ?? string.Empty,
            HoTen = reader["sHoTen"]?.ToString() ?? string.Empty,
            VaiTro = reader["sVaiTro"]?.ToString() ?? string.Empty
        };
    }

    public async Task<TaiKhoanDto?> LayTaiKhoanTheoMaAsync(string maTaiKhoan)
    {
        const string sql = """
            SELECT TOP 1 PK_MaTaiKhoan, sSDT, sHoTen, sVaiTro
            FROM tblTaiKhoan
            WHERE PK_MaTaiKhoan = @MaTaiKhoan;
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaTaiKhoan", maTaiKhoan);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new TaiKhoanDto
        {
            MaTaiKhoan = reader["PK_MaTaiKhoan"]?.ToString() ?? string.Empty,
            SoDienThoai = reader["sSDT"]?.ToString() ?? string.Empty,
            HoTen = reader["sHoTen"]?.ToString() ?? string.Empty,
            VaiTro = reader["sVaiTro"]?.ToString() ?? string.Empty
        };
    }

    public async Task CapNhatThongTinTaiKhoanAsync(CapNhatTaiKhoanViewModel model)
    {
        const string sql = """
            UPDATE tblTaiKhoan
            SET sHoTen = @HoTen,
                sMatKhau = COALESCE(NULLIF(@MatKhauMoi, N''), sMatKhau)
            WHERE PK_MaTaiKhoan = @MaTaiKhoan;
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaTaiKhoan", model.MaTaiKhoan);
        cmd.Parameters.AddWithValue("@HoTen", model.HoTen);
        cmd.Parameters.AddWithValue("@MatKhauMoi", (object?)model.MatKhauMoi ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<TaiKhoanDto>> LayDanhSachTaiKhoanAsync()
    {
        const string sql = """
            SELECT PK_MaTaiKhoan, sSDT, sHoTen, sVaiTro
            FROM tblTaiKhoan
            ORDER BY PK_MaTaiKhoan DESC;
            """;

        var result = new List<TaiKhoanDto>();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new TaiKhoanDto
            {
                MaTaiKhoan = reader["PK_MaTaiKhoan"]?.ToString() ?? string.Empty,
                SoDienThoai = reader["sSDT"]?.ToString() ?? string.Empty,
                HoTen = reader["sHoTen"]?.ToString() ?? string.Empty,
                VaiTro = reader["sVaiTro"]?.ToString() ?? string.Empty
            });
        }
        return result;
    }

    public async Task CapNhatTaiKhoanBoiQuanTriAsync(CapNhatTaiKhoanQuanTriViewModel model)
    {
        const string sql = """
            UPDATE tblTaiKhoan
            SET sHoTen = @HoTen,
                sSDT = @SoDienThoai
            WHERE PK_MaTaiKhoan = @MaTaiKhoan;
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaTaiKhoan", model.MaTaiKhoan);
        cmd.Parameters.AddWithValue("@HoTen", model.HoTen);
        cmd.Parameters.AddWithValue("@SoDienThoai", model.SoDienThoai);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task CapLaiMatKhauAsync(string maTaiKhoan, string matKhauMoi)
    {
        const string sql = """
            UPDATE tblTaiKhoan
            SET sMatKhau = @MatKhauMoi
            WHERE PK_MaTaiKhoan = @MaTaiKhoan;
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaTaiKhoan", maTaiKhoan);
        cmd.Parameters.AddWithValue("@MatKhauMoi", matKhauMoi);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<LoaiPhongDto>> LayLoaiPhongAsync()
    {
        const string sql = """
            SELECT PK_MaLoaiPhong, sTenLoaiPhong
            FROM tblLoaiPhong
            ORDER BY PK_MaLoaiPhong;
            """;

        var result = new List<LoaiPhongDto>();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new LoaiPhongDto(
                reader["PK_MaLoaiPhong"]?.ToString() ?? string.Empty,
                reader["sTenLoaiPhong"]?.ToString() ?? string.Empty));
        }
        return result;
    }

    public async Task<List<TinhThanhPhoDto>> LayTinhThanhPhoAsync()
    {
        const string sql = """
            SELECT PK_MaTinhThanhPho, sTenTinhThanhPho
            FROM tblTinhThanhPho
            ORDER BY PK_MaTinhThanhPho;
            """;

        var result = new List<TinhThanhPhoDto>();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new TinhThanhPhoDto(
                reader["PK_MaTinhThanhPho"]?.ToString() ?? string.Empty,
                reader["sTenTinhThanhPho"]?.ToString() ?? string.Empty));
        }
        return result;
    }

    public async Task<List<QuanHuyenDto>> LayQuanHuyenAsync(string maTinhThanhPho)
    {
        const string sql = """
            SELECT PK_MaQuanHuyen, sTenQuanHuyen
            FROM tblQuanHuyen
            WHERE FK_MaTinhThanhPho = @MaTinh
            ORDER BY PK_MaQuanHuyen;
            """;

        var result = new List<QuanHuyenDto>();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaTinh", maTinhThanhPho);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new QuanHuyenDto(
                reader["PK_MaQuanHuyen"]?.ToString() ?? string.Empty,
                reader["sTenQuanHuyen"]?.ToString() ?? string.Empty));
        }
        return result;
    }

    public async Task<List<XaPhuongDto>> LayXaPhuongAsync(string maQuanHuyen)
    {
        const string sql = """
            SELECT PK_MaXaPhuongThiTran, sTenXaPhuongThiTran
            FROM tblXaPhuongThiTran
            WHERE FK_MaQuanHuyen = @MaQuan
            ORDER BY PK_MaXaPhuongThiTran;
            """;

        var result = new List<XaPhuongDto>();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaQuan", maQuanHuyen);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new XaPhuongDto(
                reader["PK_MaXaPhuongThiTran"]?.ToString() ?? string.Empty,
                reader["sTenXaPhuongThiTran"]?.ToString() ?? string.Empty));
        }
        return result;
    }

    public async Task<string> TaoTinPhongAsync(TaoTinPhongViewModel model)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();

        try
        {
            var maPhongMoi = await TaoMaPhongMoiAsync(conn, tx);
            var maDiaChiMoi = await TaoMaDiaChiMoiAsync(conn, tx);

            const string sqlPhong = """
                INSERT INTO tblPhong
                    (PK_MaPhong, FK_MaLoaiPhong, FK_MaTaiKhoan, FK_MaKiemDuyet,
                     sTenPhongTro, fGiaPhong, fGiaDien, fGiaNuoc, dNgayDang, sSDT, fDienTich, bTrangThai)
                VALUES
                    (@MaPhong, @MaLoaiPhong, @MaTaiKhoan, N'KD002',
                     @TenPhongTro, @GiaPhong, @GiaDien, @GiaNuoc, CAST(GETDATE() AS DATE), @SoDienThoai, @DienTich, 0);
                """;

            await using (var cmd = new SqlCommand(sqlPhong, conn, tx))
            {
                cmd.Parameters.AddWithValue("@MaPhong", maPhongMoi);
                cmd.Parameters.AddWithValue("@MaLoaiPhong", model.MaLoaiPhong);
                cmd.Parameters.AddWithValue("@MaTaiKhoan", model.MaTaiKhoan);
                cmd.Parameters.AddWithValue("@TenPhongTro", model.TenPhongTro);
                cmd.Parameters.AddWithValue("@GiaPhong", model.GiaPhong);
                cmd.Parameters.AddWithValue("@GiaDien", model.GiaDien);
                cmd.Parameters.AddWithValue("@GiaNuoc", model.GiaNuoc);
                cmd.Parameters.AddWithValue("@SoDienThoai", model.SoDienThoai);
                cmd.Parameters.AddWithValue("@DienTich", model.DienTich);
                await cmd.ExecuteNonQueryAsync();
            }

            const string sqlDiaChi = """
                INSERT INTO tblDiaChi
                    (PK_MaDiaChi, FK_MaPhong, FK_MaQuanHuyen, FK_MaTinhThanhPho, FK_MaXaPhuongThiTran, sDiaChiChiTiet)
                VALUES
                    (@MaDiaChi, @MaPhong, @MaQuan, @MaTinh, @MaXa, @DiaChiChiTiet);
                """;

            await using (var cmd = new SqlCommand(sqlDiaChi, conn, tx))
            {
                cmd.Parameters.AddWithValue("@MaDiaChi", maDiaChiMoi);
                cmd.Parameters.AddWithValue("@MaPhong", maPhongMoi);
                cmd.Parameters.AddWithValue("@MaQuan", model.MaQuanHuyen);
                cmd.Parameters.AddWithValue("@MaTinh", model.MaTinhThanhPho);
                cmd.Parameters.AddWithValue("@MaXa", string.IsNullOrWhiteSpace(model.MaXaPhuong) ? DBNull.Value : model.MaXaPhuong);
                cmd.Parameters.AddWithValue("@DiaChiChiTiet", (object?)model.DiaChiChiTiet ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return maPhongMoi;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<TaoTinPhongViewModel?> LayTinPhongDeSuaAsync(string maPhong)
    {
        const string sql = """
            SELECT TOP 1
                p.PK_MaPhong,
                p.FK_MaTaiKhoan,
                p.FK_MaLoaiPhong,
                p.sTenPhongTro,
                ISNULL(p.fGiaPhong, 0) AS fGiaPhong,
                ISNULL(p.fDienTich, 0) AS fDienTich,
                ISNULL(p.fGiaDien, 0) AS fGiaDien,
                ISNULL(p.fGiaNuoc, 0) AS fGiaNuoc,
                ISNULL(p.sSDT, N'') AS sSDT,
                dc.FK_MaTinhThanhPho,
                dc.FK_MaQuanHuyen,
                dc.FK_MaXaPhuongThiTran,
                dc.sDiaChiChiTiet
            FROM tblPhong p
            LEFT JOIN tblDiaChi dc ON dc.FK_MaPhong = p.PK_MaPhong
            WHERE p.PK_MaPhong = @MaPhong;
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaPhong", maPhong);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new TaoTinPhongViewModel
        {
            MaPhong = reader["PK_MaPhong"]?.ToString(),
            MaTaiKhoan = reader["FK_MaTaiKhoan"]?.ToString() ?? string.Empty,
            MaLoaiPhong = reader["FK_MaLoaiPhong"]?.ToString() ?? string.Empty,
            TenPhongTro = reader["sTenPhongTro"]?.ToString() ?? string.Empty,
            GiaPhong = Convert.ToDouble(reader["fGiaPhong"]),
            DienTich = Convert.ToDouble(reader["fDienTich"]),
            GiaDien = Convert.ToDouble(reader["fGiaDien"]),
            GiaNuoc = Convert.ToDouble(reader["fGiaNuoc"]),
            SoDienThoai = reader["sSDT"]?.ToString() ?? string.Empty,
            MaTinhThanhPho = reader["FK_MaTinhThanhPho"]?.ToString() ?? string.Empty,
            MaQuanHuyen = reader["FK_MaQuanHuyen"]?.ToString() ?? string.Empty,
            MaXaPhuong = reader["FK_MaXaPhuongThiTran"] == DBNull.Value ? null : reader["FK_MaXaPhuongThiTran"]?.ToString(),
            DiaChiChiTiet = reader["sDiaChiChiTiet"] == DBNull.Value ? null : reader["sDiaChiChiTiet"]?.ToString()
        };
    }

    public async Task CapNhatTinPhongAsync(TaoTinPhongViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.MaPhong))
        {
            throw new ArgumentException("Thiếu mã phòng để cập nhật.", nameof(model));
        }

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();

        try
        {
            const string sqlPhong = """
                UPDATE tblPhong
                SET FK_MaLoaiPhong = @MaLoaiPhong,
                    sTenPhongTro = @TenPhongTro,
                    fGiaPhong = @GiaPhong,
                    fGiaDien = @GiaDien,
                    fGiaNuoc = @GiaNuoc,
                    sSDT = @SoDienThoai,
                    fDienTich = @DienTich
                WHERE PK_MaPhong = @MaPhong;
                """;

            await using (var cmd = new SqlCommand(sqlPhong, conn, tx))
            {
                cmd.Parameters.AddWithValue("@MaPhong", model.MaPhong);
                cmd.Parameters.AddWithValue("@MaLoaiPhong", model.MaLoaiPhong);
                cmd.Parameters.AddWithValue("@TenPhongTro", model.TenPhongTro);
                cmd.Parameters.AddWithValue("@GiaPhong", model.GiaPhong);
                cmd.Parameters.AddWithValue("@GiaDien", model.GiaDien);
                cmd.Parameters.AddWithValue("@GiaNuoc", model.GiaNuoc);
                cmd.Parameters.AddWithValue("@SoDienThoai", model.SoDienThoai);
                cmd.Parameters.AddWithValue("@DienTich", model.DienTich);
                await cmd.ExecuteNonQueryAsync();
            }

            const string sqlDiaChi = """
                UPDATE tblDiaChi
                SET FK_MaTinhThanhPho = @MaTinh,
                    FK_MaQuanHuyen = @MaQuan,
                    FK_MaXaPhuongThiTran = @MaXa,
                    sDiaChiChiTiet = @DiaChiChiTiet
                WHERE FK_MaPhong = @MaPhong;

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO tblDiaChi
                        (PK_MaDiaChi, FK_MaPhong, FK_MaQuanHuyen, FK_MaTinhThanhPho, FK_MaXaPhuongThiTran, sDiaChiChiTiet)
                    VALUES
                        (@MaDiaChi, @MaPhong, @MaQuan, @MaTinh, @MaXa, @DiaChiChiTiet);
                END
                """;

            await using (var cmd = new SqlCommand(sqlDiaChi, conn, tx))
            {
                cmd.Parameters.AddWithValue("@MaPhong", model.MaPhong);
                cmd.Parameters.AddWithValue("@MaTinh", model.MaTinhThanhPho);
                cmd.Parameters.AddWithValue("@MaQuan", model.MaQuanHuyen);
                cmd.Parameters.AddWithValue("@MaXa", string.IsNullOrWhiteSpace(model.MaXaPhuong) ? DBNull.Value : model.MaXaPhuong);
                cmd.Parameters.AddWithValue("@DiaChiChiTiet", (object?)model.DiaChiChiTiet ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MaDiaChi", await TaoMaDiaChiMoiAsync(conn, tx));
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<TinCuaToiDto>> LayDanhSachTinCuaToiAsync(string maTaiKhoan)
    {
        const string sql = """
            SELECT
                p.PK_MaPhong,
                ISNULL(p.sTenPhongTro, N'') AS sTenPhongTro,
                ISNULL(p.fGiaPhong, 0) AS fGiaPhong,
                ISNULL(p.fDienTich, 0) AS fDienTich,
                p.dNgayDang,
                ISNULL(kd.sTrangThaiDuyet, N'') AS sTrangThaiDuyet,
                ISNULL(p.bTrangThai, 0) AS bTrangThai
            FROM tblPhong p
            LEFT JOIN tblKiemDuyet kd ON kd.PK_MaKiemDuyet = p.FK_MaKiemDuyet
            WHERE p.FK_MaTaiKhoan = @MaTaiKhoan
            ORDER BY p.dNgayDang DESC, p.PK_MaPhong DESC;
            """;

        var result = new List<TinCuaToiDto>();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaTaiKhoan", maTaiKhoan);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new TinCuaToiDto
            {
                MaPhong = reader["PK_MaPhong"]?.ToString() ?? string.Empty,
                TenPhongTro = reader["sTenPhongTro"]?.ToString() ?? string.Empty,
                GiaPhong = Convert.ToDouble(reader["fGiaPhong"]),
                DienTich = Convert.ToDouble(reader["fDienTich"]),
                NgayDang = reader["dNgayDang"] == DBNull.Value ? null : Convert.ToDateTime(reader["dNgayDang"]),
                TrangThaiDuyet = reader["sTrangThaiDuyet"]?.ToString() ?? string.Empty,
                TrangThaiHienThi = Convert.ToInt32(reader["bTrangThai"]) == 1
            });
        }
        return result;
    }

    public async Task<List<TinChoDuyetDto>> LayDanhSachTinChoDuyetAsync()
    {
        const string sql = """
            SELECT
                p.PK_MaPhong,
                ISNULL(p.sTenPhongTro, N'') AS sTenPhongTro,
                p.FK_MaTaiKhoan,
                ISNULL(tk.sHoTen, N'') AS sHoTen,
                ISNULL(p.sSDT, N'') AS sSDT,
                ISNULL(p.fGiaPhong, 0) AS fGiaPhong,
                ISNULL(p.fDienTich, 0) AS fDienTich,
                p.dNgayDang,
                ISNULL(kd.sTrangThaiDuyet, N'') AS sTrangThaiDuyet
            FROM tblPhong p
            LEFT JOIN tblTaiKhoan tk ON tk.PK_MaTaiKhoan = p.FK_MaTaiKhoan
            LEFT JOIN tblKiemDuyet kd ON kd.PK_MaKiemDuyet = p.FK_MaKiemDuyet
            WHERE p.FK_MaKiemDuyet = N'KD002'
            ORDER BY p.dNgayDang DESC, p.PK_MaPhong DESC;
            """;

        var result = new List<TinChoDuyetDto>();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new TinChoDuyetDto
            {
                MaPhong = reader["PK_MaPhong"]?.ToString() ?? string.Empty,
                TenPhongTro = reader["sTenPhongTro"]?.ToString() ?? string.Empty,
                MaTaiKhoan = reader["FK_MaTaiKhoan"]?.ToString() ?? string.Empty,
                TenChuTro = reader["sHoTen"]?.ToString() ?? string.Empty,
                SoDienThoai = reader["sSDT"]?.ToString() ?? string.Empty,
                GiaPhong = Convert.ToDouble(reader["fGiaPhong"]),
                DienTich = Convert.ToDouble(reader["fDienTich"]),
                NgayDang = reader["dNgayDang"] == DBNull.Value ? null : Convert.ToDateTime(reader["dNgayDang"]),
                TrangThaiDuyet = reader["sTrangThaiDuyet"]?.ToString() ?? string.Empty
            });
        }

        return result;
    }

    public async Task CapNhatTrangThaiDuyetTinAsync(string maPhong, string maKiemDuyet, bool trangThaiHoatDong)
    {
        const string sql = """
            UPDATE tblPhong
            SET FK_MaKiemDuyet = @MaKiemDuyet,
                bTrangThai = @TrangThai
            WHERE PK_MaPhong = @MaPhong;
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaPhong", maPhong);
        cmd.Parameters.AddWithValue("@MaKiemDuyet", maKiemDuyet);
        cmd.Parameters.AddWithValue("@TrangThai", trangThaiHoatDong ? 1 : 0);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<string> UploadAnhAsync(IFormFile file, IWebHostEnvironment env)
    {
        var tenFile = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var folder = Path.Combine(env.WebRootPath, "uploads", "phong");
        Directory.CreateDirectory(folder);
        var duongDanDayDu = Path.Combine(folder, tenFile);
        await using var stream = new FileStream(duongDanDayDu, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/phong/{tenFile}";
    }

    public async Task LuuAnhVaoDbAsync(string maPhong, string maTaiKhoan, string duongDan)
    {
        const string sql = """
            INSERT INTO tblAnh (PK_MaAnh, FK_MaPhong, FK_MaTaiKhoan, sDuongDan)
            VALUES (@MaAnh, @MaPhong, @MaTaiKhoan, @DuongDan);
            """;
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaAnh", $"ANH{Guid.NewGuid().ToString()[..6].ToUpper()}");
        cmd.Parameters.AddWithValue("@MaPhong", maPhong);
        cmd.Parameters.AddWithValue("@MaTaiKhoan", maTaiKhoan);
        cmd.Parameters.AddWithValue("@DuongDan", duongDan);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<string> TaoMaTaiKhoanMoiAsync(SqlConnection conn)
    {
        const string sql = """
            SELECT TOP 1 PK_MaTaiKhoan
            FROM tblTaiKhoan
            WHERE PK_MaTaiKhoan LIKE 'TK%'
            ORDER BY PK_MaTaiKhoan DESC;
            """;
        await using var cmd = new SqlCommand(sql, conn);
        var maCuoi = (string?)await cmd.ExecuteScalarAsync();
        if (string.IsNullOrWhiteSpace(maCuoi))
        {
            return "TK0001";
        }

        var so = 0;
        _ = int.TryParse(maCuoi.Replace("TK", string.Empty), out so);
        return $"TK{(so + 1):D4}";
    }

    private static async Task<string> TaoMaPhongMoiAsync(SqlConnection conn, SqlTransaction tx)
    {
        const string sql = """
            SELECT TOP 1 PK_MaPhong
            FROM tblPhong
            WHERE PK_MaPhong LIKE 'P%'
            ORDER BY PK_MaPhong DESC;
            """;

        await using var cmd = new SqlCommand(sql, conn, tx);
        var maCuoi = (string?)await cmd.ExecuteScalarAsync();
        if (string.IsNullOrWhiteSpace(maCuoi))
        {
            return "P0001";
        }

        var so = 0;
        _ = int.TryParse(maCuoi.Replace("P", string.Empty), out so);
        return $"P{(so + 1):D4}";
    }

    private static async Task<string> TaoMaDiaChiMoiAsync(SqlConnection conn, SqlTransaction tx)
    {
        const string sql = """
            SELECT TOP 1 PK_MaDiaChi
            FROM tblDiaChi
            WHERE PK_MaDiaChi LIKE 'DC%'
            ORDER BY PK_MaDiaChi DESC;
            """;

        await using var cmd = new SqlCommand(sql, conn, tx);
        var maCuoi = (string?)await cmd.ExecuteScalarAsync();
        if (string.IsNullOrWhiteSpace(maCuoi))
        {
            return "DC0001";
        }

        var so = 0;
        _ = int.TryParse(maCuoi.Replace("DC", string.Empty), out so);
        return $"DC{(so + 1):D4}";
    }

    public async Task<string> TaoToCaoAsync(TaoToCaoViewModel model)
    {
        await ReportLock.WaitAsync();
        try
        {
            var list = await ReadReportsAsync();
            var maMoi = TaoMaToCaoMoi(list);
            list.Add(new ReportRecord
            {
                MaToCao = maMoi,
                MaPhong = model.MaPhong,
                MaTaiKhoanNguoiBaoCao = model.MaTaiKhoanNguoiBaoCao,
                LoaiViPham = model.LoaiViPham,
                NoiDung = model.NoiDung,
                NgayTao = DateTime.UtcNow,
                MaKiemDuyet = "KD002"
            });
            await WriteReportsAsync(list);
            return maMoi;
        }
        finally
        {
            ReportLock.Release();
        }
    }

    public async Task<List<ToCaoChoDuyetDto>> LayDanhSachToCaoChoDuyetAsync()
    {
        var reports = await ReadReportsAsync();
        var pending = reports
            .Where(x => string.Equals(x.MaKiemDuyet, "KD002", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.NgayTao)
            .ToList();
        return await MapReportsAsync(pending);
    }

    public async Task<List<ToCaoChoDuyetDto>> LayDanhSachToCaoCuaToiAsync(string maTaiKhoan)
    {
        var reports = await ReadReportsAsync();
        var mine = reports
            .Where(x => string.Equals(x.MaTaiKhoanNguoiBaoCao, maTaiKhoan, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.NgayTao)
            .ToList();
        return await MapReportsAsync(mine);
    }

    public async Task CapNhatTrangThaiDuyetToCaoAsync(string maToCao, string maKiemDuyet)
    {
        await ReportLock.WaitAsync();
        try
        {
            var list = await ReadReportsAsync();
            var item = list.FirstOrDefault(x => string.Equals(x.MaToCao, maToCao, StringComparison.OrdinalIgnoreCase));
            if (item is not null)
            {
                item.MaKiemDuyet = maKiemDuyet;
                await WriteReportsAsync(list);
            }
        }
        finally
        {
            ReportLock.Release();
        }
    }

    public async Task XoaToCaoAsync(string maToCao)
    {
        await ReportLock.WaitAsync();
        try
        {
            var list = await ReadReportsAsync();
            list.RemoveAll(x => string.Equals(x.MaToCao, maToCao, StringComparison.OrdinalIgnoreCase));
            await WriteReportsAsync(list);
        }
        finally
        {
            ReportLock.Release();
        }
    }

    public async Task XoaPhongAsync(string maPhong)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();

        try
        {
            await ExecAsync(conn, tx, "DELETE FROM tblPhong_DichVu WHERE PK_MaPhong = @MaPhong;", ("@MaPhong", maPhong));
            await ExecAsync(conn, tx, "DELETE FROM tblAnh WHERE FK_MaPhong = @MaPhong;", ("@MaPhong", maPhong));
            await ExecAsync(conn, tx, "DELETE FROM tblDiaChi WHERE FK_MaPhong = @MaPhong;", ("@MaPhong", maPhong));
            await ExecAsync(conn, tx, "DELETE FROM tblPhong WHERE PK_MaPhong = @MaPhong;", ("@MaPhong", maPhong));
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        await ReportLock.WaitAsync();
        try
        {
            var list = await ReadReportsAsync();
            list.RemoveAll(x => string.Equals(x.MaPhong, maPhong, StringComparison.OrdinalIgnoreCase));
            await WriteReportsAsync(list);
        }
        finally
        {
            ReportLock.Release();
        }
    }

    public async Task XoaTaiKhoanAsync(string maTaiKhoan)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();
        var dsPhong = new List<string>();

        try
        {
            // Lấy danh sách phòng của tài khoản
            const string sqlPhong = "SELECT PK_MaPhong FROM tblPhong WHERE FK_MaTaiKhoan = @MaTaiKhoan;";
            await using (var cmd = new SqlCommand(sqlPhong, conn, tx))
            {
                cmd.Parameters.AddWithValue("@MaTaiKhoan", maTaiKhoan);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    dsPhong.Add(reader["PK_MaPhong"]?.ToString() ?? string.Empty);
                }
            }

            // Xóa các bản ghi phụ thuộc theo phòng
            foreach (var maPhong in dsPhong.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                await ExecAsync(conn, tx, "DELETE FROM tblPhong_DichVu WHERE PK_MaPhong = @MaPhong;", ("@MaPhong", maPhong));
                await ExecAsync(conn, tx, "DELETE FROM tblAnh WHERE FK_MaPhong = @MaPhong;", ("@MaPhong", maPhong));
                await ExecAsync(conn, tx, "DELETE FROM tblDiaChi WHERE FK_MaPhong = @MaPhong;", ("@MaPhong", maPhong));
                await ExecAsync(conn, tx, "DELETE FROM tblPhong WHERE PK_MaPhong = @MaPhong;", ("@MaPhong", maPhong));
            }

            // Xóa tài khoản
            await ExecAsync(conn, tx, "DELETE FROM tblTaiKhoan WHERE PK_MaTaiKhoan = @MaTaiKhoan;", ("@MaTaiKhoan", maTaiKhoan));

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        await ReportLock.WaitAsync();
        try
        {
            var list = await ReadReportsAsync();
            list.RemoveAll(x =>
                string.Equals(x.MaTaiKhoanNguoiBaoCao, maTaiKhoan, StringComparison.OrdinalIgnoreCase) ||
                dsPhong.Any(p => string.Equals(p, x.MaPhong, StringComparison.OrdinalIgnoreCase)));
            await WriteReportsAsync(list);
        }
        finally
        {
            ReportLock.Release();
        }
    }

    private static async Task ExecAsync(SqlConnection conn, SqlTransaction tx, string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = new SqlCommand(sql, conn, tx);
        foreach (var p in parameters)
        {
            cmd.Parameters.AddWithValue(p.Name, p.Value);
        }
        await cmd.ExecuteNonQueryAsync();
    }

    private static string TaoMaToCaoMoi(List<ReportRecord> list)
    {
        var maCuoi = list
            .Select(x => x.MaToCao)
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith("TC", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(maCuoi))
        {
            return "TC00000001";
        }

        var so = 0;
        _ = int.TryParse(maCuoi.Replace("TC", string.Empty), out so);
        return $"TC{(so + 1):D8}";
    }

    private async Task<List<ToCaoChoDuyetDto>> MapReportsAsync(List<ReportRecord> reports)
    {
        var mapTenPhong = await LayMapTenPhongAsync();
        return reports.Select(x => new ToCaoChoDuyetDto
        {
            MaToCao = x.MaToCao,
            MaPhong = x.MaPhong,
            TenPhongTro = mapTenPhong.TryGetValue(x.MaPhong, out var ten) ? ten : x.MaPhong,
            MaTaiKhoanNguoiBaoCao = x.MaTaiKhoanNguoiBaoCao,
            LoaiViPham = x.LoaiViPham,
            NoiDung = x.NoiDung,
            NgayTao = x.NgayTao,
            TrangThaiDuyet = x.MaKiemDuyet switch
            {
                "KD001" => "Đã duyệt",
                "KD003" => "Từ chối",
                _ => "Chờ duyệt"
            }
        }).ToList();
    }

    private async Task<Dictionary<string, string>> LayMapTenPhongAsync()
    {
        const string sql = "SELECT PK_MaPhong, ISNULL(sTenPhongTro, N'') AS sTenPhongTro FROM tblPhong;";
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var ma = reader["PK_MaPhong"]?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(ma))
            {
                result[ma] = reader["sTenPhongTro"]?.ToString() ?? string.Empty;
            }
        }

        return result;
    }

    private static async Task<List<ReportRecord>> ReadReportsAsync()
    {
        if (!File.Exists(ReportStorePath))
        {
            return [];
        }

        var json = await File.ReadAllTextAsync(ReportStorePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<ReportRecord>>(json, JsonOptions) ?? [];
    }

    private static async Task WriteReportsAsync(List<ReportRecord> list)
    {
        var dir = Path.GetDirectoryName(ReportStorePath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(list, JsonOptions);
        await File.WriteAllTextAsync(ReportStorePath, json);
    }

    private class ReportRecord
    {
        public string MaToCao { get; set; } = string.Empty;
        public string MaPhong { get; set; } = string.Empty;
        public string MaTaiKhoanNguoiBaoCao { get; set; } = string.Empty;
        public string LoaiViPham { get; set; } = string.Empty;
        public string? NoiDung { get; set; }
        public DateTime NgayTao { get; set; }
        public string MaKiemDuyet { get; set; } = "KD002";
    }
}