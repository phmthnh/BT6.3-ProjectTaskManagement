SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
IF DB_ID(N'ProjectTaskDB') IS NULL EXEC(N'CREATE DATABASE [ProjectTaskDB]');
GO
USE [ProjectTaskDB];
GO
IF OBJECT_ID(N'dbo.DuAn',N'U') IS NULL
BEGIN
SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE DuAn(MaDuAn int IDENTITY PRIMARY KEY,TenDuAn nvarchar(150) NOT NULL,NgayBatDau datetime,NgayKetThuc datetime,TrangThai nvarchar(50));
CREATE TABLE ThanhVien(MaThanhVien int IDENTITY PRIMARY KEY,HoTen nvarchar(100) NOT NULL,Email varchar(100) UNIQUE,ChuyenMon nvarchar(100));
CREATE TABLE CongViec(MaTask int IDENTITY PRIMARY KEY,MaDuAn int NOT NULL REFERENCES DuAn,TenTask nvarchar(200) NOT NULL,MoTa nvarchar(max),HanNop datetime,DoUuTien nvarchar(20),TrangThai nvarchar(50) NOT NULL CHECK(TrangThai IN(N'To Do',N'In Progress',N'Review',N'Done')));
CREATE TABLE PhanCong(MaTask int REFERENCES CongViec,MaThanhVien int REFERENCES ThanhVien,NgayGiao datetime NOT NULL DEFAULT GETDATE(),PRIMARY KEY(MaTask,MaThanhVien));
INSERT DuAn(TenDuAn,NgayBatDau,NgayKetThuc,TrangThai) VALUES(N'Dự án mẫu',GETDATE(),DATEADD(day,30,GETDATE()),N'Đang chạy');
INSERT ThanhVien(HoTen,Email,ChuyenMon) VALUES(N'Thành viên mẫu A','a@example.test',N'Phân tích'),(N'Thành viên mẫu B','b@example.test',N'Lập trình');
INSERT CongViec(MaDuAn,TenTask,MoTa,HanNop,DoUuTien,TrangThai) VALUES(1,N'Phân tích yêu cầu',N'Công việc mẫu',DATEADD(day,-1,GETDATE()),N'Cao',N'In Progress'),(1,N'Chuẩn bị dự án',NULL,GETDATE(),N'Thấp',N'Done');
INSERT PhanCong(MaTask,MaThanhVien) VALUES(1,1),(1,2),(2,1);

COMMIT;
END;
GO
