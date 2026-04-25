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
    sVaiTro NVARCHAR(30) NOT NULL
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