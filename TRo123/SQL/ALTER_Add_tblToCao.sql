USE QL_NhaTro;
GO

IF OBJECT_ID('dbo.tblToCao', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblToCao (
        PK_MaToCao NVARCHAR(12) PRIMARY KEY,
        FK_MaPhong NVARCHAR(10) NOT NULL,
        FK_MaTaiKhoan NVARCHAR(10) NOT NULL,
        FK_MaKiemDuyet NVARCHAR(15) NOT NULL,
        sLoaiViPham NVARCHAR(100) NOT NULL,
        sNoiDung NVARCHAR(500),
        dNgayTao DATE NOT NULL,
        CONSTRAINT FK_tblToCao_tblPhong FOREIGN KEY (FK_MaPhong) REFERENCES dbo.tblPhong(PK_MaPhong),
        CONSTRAINT FK_tblToCao_tblTaiKhoan FOREIGN KEY (FK_MaTaiKhoan) REFERENCES dbo.tblTaiKhoan(PK_MaTaiKhoan),
        CONSTRAINT FK_tblToCao_tblKiemDuyet FOREIGN KEY (FK_MaKiemDuyet) REFERENCES dbo.tblKiemDuyet(PK_MaKiemDuyet)
    );
END
GO

