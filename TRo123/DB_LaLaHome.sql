-- ==============================================================
CREATE DATABASE QL_NhaTro;
GO

USE QL_NhaTro;
GO
-- ==============================================================

-- Bảng Tài Khoản
CREATE TABLE tblTaiKhoan (
    PK_MaTaiKhoan NVARCHAR(10) PRIMARY KEY,
    sMatKhau NVARCHAR(12) NOT NULL,
    sSDT NVARCHAR(10) NOT NULL,
    sHoTen NVARCHAR(30) NOT NULL,
    sVaiTro NVARCHAR(30) NOT NULL,
    bTrangThai BIT NOT NULL CONSTRAINT DF_tblTaiKhoan_bTrangThai DEFAULT(1) -- 1: hoạt động, 0: vô hiệu hóa
);

-- Bảng Loại Phòng (Suy luận từ bảng danh sách thực thể [5] và Sơ đồ [23])
CREATE TABLE tblLoaiPhong (
    PK_MaLoaiPhong NVARCHAR(10) PRIMARY KEY,
    sTenLoaiPhong NVARCHAR(50) NOT NULL
);

-- Bảng Kiểm Duyệt (Dựa vào Sơ đồ quan hệ [23])
CREATE TABLE tblKiemDuyet (
    PK_MaKiemDuyet NVARCHAR(15) PRIMARY KEY,
    sTrangThaiDuyet NVARCHAR(50)
);

-- Bảng Dịch Vụ
CREATE TABLE tblDichVu (
    PK_MaDichVu NVARCHAR(10) PRIMARY KEY, 
    sTenDichVu NVARCHAR(15) NOT NULL
);

-- Bảng Tỉnh Thành Phố
CREATE TABLE tblTinhThanhPho (
    PK_MaTinhThanhPho NVARCHAR(10) PRIMARY KEY,
    sTenTinhThanhPho NVARCHAR(30) NOT NULL
);


-- ==============================================================
-- 2. TẠO CÁC BẢNG PHỤ THUỘC (CÓ CHỨA KHÓA NGOẠI)
-- ==============================================================

-- Bảng Quận Huyện
CREATE TABLE tblQuanHuyen (
    PK_MaQuanHuyen NVARCHAR(10) PRIMARY KEY,
    sTenQuanHuyen NVARCHAR(50) NOT NULL, 
    FK_MaTinhThanhPho NVARCHAR(10) NOT NULL, -- Đã đồng bộ với PK_MaTinhThanhPho
    FOREIGN KEY (FK_MaTinhThanhPho) REFERENCES tblTinhThanhPho(PK_MaTinhThanhPho)
);

-- Bảng Xã Phường Thị Trấn
CREATE TABLE tblXaPhuongThiTran (
    PK_MaXaPhuongThiTran NVARCHAR(10) PRIMARY KEY,
    sTenXaPhuongThiTran NVARCHAR(50) NOT NULL,
    FK_MaQuanHuyen NVARCHAR(10) NOT NULL, -- Đã đồng bộ với PK_MaQuanHuyen
    FOREIGN KEY (FK_MaQuanHuyen) REFERENCES tblQuanHuyen(PK_MaQuanHuyen)
);

-- Bảng Phòng (Bảng trung tâm)
CREATE TABLE tblPhong (
    PK_MaPhong NVARCHAR(10) PRIMARY KEY,
    FK_MaLoaiPhong NVARCHAR(10) NOT NULL,
    FK_MaTaiKhoan NVARCHAR(10) NOT NULL, -- Đã đồng bộ với PK_MaTaiKhoan (10 thay vì 15)
    FK_MaKiemDuyet NVARCHAR(15) NOT NULL,
    sTenPhongTro NVARCHAR(100), -- Kiểu dữ liệu giả định
    fGiaPhong FLOAT,            -- Kiểu dữ liệu giả định
    fGiaDien FLOAT,             -- Kiểu dữ liệu giả định
    fGiaNuoc FLOAT,             -- Kiểu dữ liệu giả định
    dNgayDang DATE,             -- Kiểu dữ liệu giả định
    sSDT NVARCHAR(15),          -- Kiểu dữ liệu giả định
    fDienTich FLOAT,            -- Kiểu dữ liệu giả định
    bTrangThai BIT,             -- Kiểu dữ liệu giả định (True/False)
    
    FOREIGN KEY (FK_MaLoaiPhong) REFERENCES tblLoaiPhong(PK_MaLoaiPhong),
    FOREIGN KEY (FK_MaTaiKhoan) REFERENCES tblTaiKhoan(PK_MaTaiKhoan),
    FOREIGN KEY (FK_MaKiemDuyet) REFERENCES tblKiemDuyet(PK_MaKiemDuyet)
);

-- Bảng tblPhong_DichVu (Bảng trung gian phân giải quan hệ n-n từ Sơ đồ [23])
CREATE TABLE tblPhong_DichVu (
    PK_MaPhong NVARCHAR(10) NOT NULL,
    PK_MaDichVu NVARCHAR(10) NOT NULL,
    PRIMARY KEY (PK_MaPhong, PK_MaDichVu),
    FOREIGN KEY (PK_MaPhong) REFERENCES tblPhong(PK_MaPhong),
    FOREIGN KEY (PK_MaDichVu) REFERENCES tblDichVu(PK_MaDichVu)
);

-- Bảng Ảnh
CREATE TABLE tblAnh (
    PK_MaAnh NVARCHAR(10) PRIMARY KEY,
    FK_MaPhong NVARCHAR(10) NOT NULL, -- Đã đồng bộ với PK_MaPhong
    FK_MaTaiKhoan NVARCHAR(10) NOT NULL,
    sDuongDan NVARCHAR(255) NOT NULL, -- Khuyến nghị mở rộng từ 30 lên 255 để lưu đủ URL
    FOREIGN KEY (FK_MaPhong) REFERENCES tblPhong(PK_MaPhong),
    FOREIGN KEY (FK_MaTaiKhoan) REFERENCES tblTaiKhoan(PK_MaTaiKhoan)
);

-- Bảng Địa Chỉ
CREATE TABLE tblDiaChi (
    PK_MaDiaChi NVARCHAR(10) PRIMARY KEY,
    FK_MaPhong NVARCHAR(10) NOT NULL,
    FK_MaQuanHuyen NVARCHAR(10) NOT NULL,
    FK_MaTinhThanhPho NVARCHAR(10) NOT NULL,
    FK_MaXaPhuongThiTran NVARCHAR(10),
    sDiaChiChiTiet NVARCHAR(100), -- Mở rộng từ 30 để ghi chi tiết số nhà
    
    FOREIGN KEY (FK_MaPhong) REFERENCES tblPhong(PK_MaPhong),
    FOREIGN KEY (FK_MaQuanHuyen) REFERENCES tblQuanHuyen(PK_MaQuanHuyen),
    FOREIGN KEY (FK_MaTinhThanhPho) REFERENCES tblTinhThanhPho(PK_MaTinhThanhPho),
    FOREIGN KEY (FK_MaXaPhuongThiTran) REFERENCES tblXaPhuongThiTran(PK_MaXaPhuongThiTran)
);


USE QL_NhaTro;
GO

-- ==============================================================
-- XÓA DỮ LIỆU CŨ (theo đúng thứ tự khóa ngoại)
-- ==============================================================
DELETE FROM tblPhong_DichVu;
DELETE FROM tblAnh;
DELETE FROM tblDiaChi;
DELETE FROM tblPhong;
DELETE FROM tblTaiKhoan;
DELETE FROM tblKiemDuyet;
DELETE FROM tblLoaiPhong;
DELETE FROM tblDichVu;
DELETE FROM tblXaPhuongThiTran;
DELETE FROM tblQuanHuyen;
DELETE FROM tblTinhThanhPho;
GO

-- ==============================================================
-- DỮ LIỆU MẪU - QL_NhaTro
-- ==============================================================

-- 1. LOẠI PHÒNG
INSERT INTO tblLoaiPhong (PK_MaLoaiPhong, sTenLoaiPhong) VALUES
(N'LP001', N'Phòng trọ'),
(N'LP002', N'Căn hộ mini'),
(N'LP003', N'Nhà nguyên căn'),
(N'LP004', N'Căn hộ chung cư'),
(N'LP005', N'Phòng ở ghép');

-- 2. KIỂM DUYỆT
INSERT INTO tblKiemDuyet (PK_MaKiemDuyet, sTrangThaiDuyet) VALUES
(N'KD001', N'Đã duyệt'),
(N'KD002', N'Chờ duyệt'),
(N'KD003', N'Từ chối');

-- 3. DỊCH VỤ
INSERT INTO tblDichVu (PK_MaDichVu, sTenDichVu) VALUES
(N'DV001', N'Wifi'),
(N'DV002', N'Điều hòa'),
(N'DV003', N'Máy giặt'),
(N'DV004', N'Bãi xe'),
(N'DV005', N'Camera'),
(N'DV006', N'Bảo vệ'),
(N'DV007', N'Tủ lạnh'),
(N'DV008', N'Bình nóng lạnh');

-- 4. TỈNH THÀNH PHỐ
INSERT INTO tblTinhThanhPho (PK_MaTinhThanhPho, sTenTinhThanhPho) VALUES
(N'TP001', N'Hà Nội'),
(N'TP002', N'TP. Hồ Chí Minh'),
(N'TP003', N'Đà Nẵng'),
(N'TP004', N'Cần Thơ'),
(N'TP005', N'Hải Phòng');

-- 5. QUẬN HUYỆN
INSERT INTO tblQuanHuyen (PK_MaQuanHuyen, sTenQuanHuyen, FK_MaTinhThanhPho) VALUES
(N'QH001', N'Cầu Giấy',   N'TP001'),
(N'QH002', N'Đống Đa',    N'TP001'),
(N'QH003', N'Thanh Xuân', N'TP001'),
(N'QH004', N'Hoàn Kiếm',  N'TP001'),
(N'QH005', N'Quận 1',     N'TP002'),
(N'QH006', N'Quận 3',     N'TP002'),
(N'QH007', N'Bình Thạnh', N'TP002'),
(N'QH008', N'Gò Vấp',     N'TP002'),
(N'QH009', N'Hải Châu',   N'TP003'),
(N'QH010', N'Thanh Khê',  N'TP003');

-- 6. XÃ PHƯỜNG THỊ TRẤN
INSERT INTO tblXaPhuongThiTran (PK_MaXaPhuongThiTran, sTenXaPhuongThiTran, FK_MaQuanHuyen) VALUES
(N'PX001', N'Phường Dịch Vọng',      N'QH001'),
(N'PX002', N'Phường Nghĩa Tân',      N'QH001'),
(N'PX003', N'Phường Mai Dịch',       N'QH001'),
(N'PX004', N'Phường Láng Hạ',        N'QH002'),
(N'PX005', N'Phường Khâm Thiên',     N'QH002'),
(N'PX006', N'Phường Nhân Chính',     N'QH003'),
(N'PX007', N'Phường Thanh Xuân Nam', N'QH003'),
(N'PX008', N'Phường Bến Nghé',       N'QH005'),
(N'PX009', N'Phường Bến Thành',      N'QH005'),
(N'PX010', N'Phường 25',             N'QH007'),
(N'PX011', N'Phường 26',             N'QH007'),
(N'PX012', N'Phường Hải Châu I',     N'QH009'),
(N'PX013', N'Phường Thạch Thang',    N'QH009');

-- 7. TÀI KHOẢN (mật khẩu tối đa 12 ký tự)
INSERT INTO tblTaiKhoan (PK_MaTaiKhoan, sMatKhau, sSDT, sHoTen, sVaiTro, bTrangThai) VALUES
(N'TK0001', N'123456',   N'0901234567', N'Nguyễn Văn An',  N'ChuTro',     1),
(N'TK0002', N'123456',   N'0912345678', N'Trần Thị Bình',  N'ChuTro',     1),
(N'TK0003', N'123456',   N'0923456789', N'Lê Văn Cường',   N'ChuTro',     1),
(N'TK0004', N'mk123456', N'0934567890', N'Phạm Thị Dung',  N'NguoiDung',  1),
(N'TK0005', N'mk123456', N'0945678901', N'Hoàng Văn Em',   N'NguoiDung',  1),
(N'TK0006', N'admin123', N'0909090909', N'Admin Hệ Thống', N'QuanTri',    1);

-- 8. PHÒNG
INSERT INTO tblPhong (PK_MaPhong, FK_MaLoaiPhong, FK_MaTaiKhoan, FK_MaKiemDuyet,
    sTenPhongTro, fGiaPhong, fGiaDien, fGiaNuoc, dNgayDang, sSDT, fDienTich, bTrangThai) VALUES
(N'P0001', N'LP001', N'TK0001', N'KD001', N'Phòng trọ giá rẻ gần ĐH Bách Khoa',       2500000, 3500, 15000, '2025-12-01', N'0901234567', 18, 1),
(N'P0002', N'LP002', N'TK0001', N'KD001', N'Căn hộ mini full nội thất Cầu Giấy',       4500000, 3500, 15000, '2025-12-10', N'0901234567', 30, 1),
(N'P0003', N'LP001', N'TK0002', N'KD001', N'Phòng trọ sạch sẽ yên tĩnh Đống Đa',      2000000, 3500, 10000, '2026-01-05', N'0912345678', 15, 1),
(N'P0004', N'LP003', N'TK0002', N'KD001', N'Nhà nguyên căn 3 phòng ngủ Thanh Xuân',   8000000, 3500, 15000, '2026-01-15', N'0912345678', 60, 1),
(N'P0005', N'LP004', N'TK0003', N'KD001', N'Căn hộ chung cư cao cấp Quận 1',         12000000, 3000, 10000, '2026-02-01', N'0923456789', 55, 1),
(N'P0006', N'LP001', N'TK0003', N'KD001', N'Phòng trọ Bình Thạnh gần siêu thị',       3000000, 3500, 15000, '2026-02-10', N'0923456789', 20, 1),
(N'P0007', N'LP002', N'TK0001', N'KD001', N'Căn hộ mini Hải Châu view biển Đà Nẵng',  5000000, 3500, 15000, '2026-02-20', N'0901234567', 35, 1),
(N'P0008', N'LP005', N'TK0002', N'KD002', N'Phòng ở ghép tiện nghi Gò Vấp',           1500000, 3500, 10000, '2026-03-01', N'0912345678', 12, 0),
(N'P0009', N'LP001', N'TK0003', N'KD001', N'Phòng trọ giá rẻ sinh viên Quận 3',       1800000, 3500, 10000, '2026-03-10', N'0923456789', 14, 1),
(N'P0010', N'LP002', N'TK0001', N'KD001', N'Căn hộ mini mới xây Nghĩa Tân',           4000000, 3500, 15000, '2026-04-01', N'0901234567', 28, 1);

-- 9. ĐỊA CHỈ
INSERT INTO tblDiaChi (PK_MaDiaChi, FK_MaPhong, FK_MaQuanHuyen, FK_MaTinhThanhPho, FK_MaXaPhuongThiTran, sDiaChiChiTiet) VALUES
(N'DC001', N'P0001', N'QH001', N'TP001', N'PX001', N'Số 12 Ngõ 5 Dịch Vọng'),
(N'DC002', N'P0002', N'QH001', N'TP001', N'PX001', N'Số 45 Đường Cầu Giấy'),
(N'DC003', N'P0003', N'QH002', N'TP001', N'PX004', N'Số 8 Ngõ 23 Láng Hạ'),
(N'DC004', N'P0004', N'QH003', N'TP001', N'PX006', N'Số 100 Đường Nguyễn Trãi'),
(N'DC005', N'P0005', N'QH005', N'TP002', N'PX008', N'Số 5 Đường Lê Lợi'),
(N'DC006', N'P0006', N'QH007', N'TP002', N'PX010', N'Số 22 Đường Xô Viết Nghệ Tĩnh'),
(N'DC007', N'P0007', N'QH009', N'TP003', N'PX012', N'Số 99 Đường Trần Phú'),
(N'DC008', N'P0008', N'QH008', N'TP002', NULL,      N'Số 33 Đường Quang Trung'),
(N'DC009', N'P0009', N'QH006', N'TP002', NULL,      N'Số 17 Đường Võ Văn Tần'),
(N'DC010', N'P0010', N'QH001', N'TP001', N'PX002',  N'Số 60 Đường Nghĩa Tân');

-- 10. ẢNH
INSERT INTO tblAnh (PK_MaAnh, FK_MaPhong, FK_MaTaiKhoan, sDuongDan) VALUES
(N'ANH001', N'P0001', N'TK0001', N'https://placehold.co/600x400?text=Phong+Tro+1'),
(N'ANH002', N'P0002', N'TK0001', N'https://placehold.co/600x400?text=Can+Ho+Mini+1'),
(N'ANH003', N'P0003', N'TK0002', N'https://placehold.co/600x400?text=Phong+Tro+2'),
(N'ANH004', N'P0004', N'TK0002', N'https://placehold.co/600x400?text=Nha+Nguyen+Can'),
(N'ANH005', N'P0005', N'TK0003', N'https://placehold.co/600x400?text=Can+Ho+Chung+Cu'),
(N'ANH006', N'P0006', N'TK0003', N'https://placehold.co/600x400?text=Phong+Tro+3'),
(N'ANH007', N'P0007', N'TK0001', N'https://placehold.co/600x400?text=Can+Ho+Da+Nang'),
(N'ANH008', N'P0008', N'TK0002', N'https://placehold.co/600x400?text=Phong+O+Ghep'),
(N'ANH009', N'P0009', N'TK0003', N'https://placehold.co/600x400?text=Phong+Sinh+Vien'),
(N'ANH010', N'P0010', N'TK0001', N'https://placehold.co/600x400?text=Can+Ho+Moi');

-- 11. DỊCH VỤ PHÒNG
INSERT INTO tblPhong_DichVu (PK_MaPhong, PK_MaDichVu) VALUES
(N'P0001', N'DV001'), (N'P0001', N'DV004'), (N'P0001', N'DV008'),
(N'P0002', N'DV001'), (N'P0002', N'DV002'), (N'P0002', N'DV003'), (N'P0002', N'DV007'),
(N'P0003', N'DV001'), (N'P0003', N'DV008'),
(N'P0004', N'DV001'), (N'P0004', N'DV002'), (N'P0004', N'DV003'), (N'P0004', N'DV004'), (N'P0004', N'DV005'),
(N'P0005', N'DV001'), (N'P0005', N'DV002'), (N'P0005', N'DV003'), (N'P0005', N'DV004'), (N'P0005', N'DV005'), (N'P0005', N'DV006'),
(N'P0006', N'DV001'), (N'P0006', N'DV004'), (N'P0006', N'DV008'),
(N'P0007', N'DV001'), (N'P0007', N'DV002'), (N'P0007', N'DV007'), (N'P0007', N'DV008'),
(N'P0008', N'DV001'), (N'P0008', N'DV003'),
(N'P0009', N'DV001'), (N'P0009', N'DV008'),
(N'P0010', N'DV001'), (N'P0010', N'DV002'), (N'P0010', N'DV004');

-- ==============================================================
-- KIỂM TRA DỮ LIỆU
-- ==============================================================
SELECT N'Tài khoản' AS [Bảng], COUNT(*) AS [Số dòng] FROM tblTaiKhoan UNION ALL
SELECT N'Loại phòng',  COUNT(*) FROM tblLoaiPhong  UNION ALL
SELECT N'Phòng',       COUNT(*) FROM tblPhong       UNION ALL
SELECT N'Địa chỉ',     COUNT(*) FROM tblDiaChi      UNION ALL
SELECT N'Ảnh',         COUNT(*) FROM tblAnh          UNION ALL
SELECT N'Dịch vụ',     COUNT(*) FROM tblDichVu       UNION ALL
SELECT N'Phòng-DV',    COUNT(*) FROM tblPhong_DichVu;