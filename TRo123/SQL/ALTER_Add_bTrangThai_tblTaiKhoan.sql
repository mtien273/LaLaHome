USE QL_NhaTro;
GO

IF COL_LENGTH('dbo.tblTaiKhoan', 'bTrangThai') IS NULL
BEGIN
    ALTER TABLE dbo.tblTaiKhoan
    ADD bTrangThai BIT NOT NULL CONSTRAINT DF_tblTaiKhoan_bTrangThai DEFAULT(1);
END
GO

-- Nếu đã có dữ liệu cũ (NULL), đảm bảo đồng nhất
UPDATE dbo.tblTaiKhoan SET bTrangThai = 1 WHERE bTrangThai IS NULL;
GO

