--    данный скрипт создает базу данных toplivo с тремя таблицами и генерирует тестовые записи:
-- 1. виды топлива (Fuels) - 1000 штук
-- 2. список емкостей (Tanks) - 100 штук
-- 3. факты совершения операций прихода, расхода топлива (Operations) - 300000 штук

CREATE DATABASE toplivo

GO

ALTER DATABASE toplivo SET RECOVERY SIMPLE
GO

USE toplivo
-- ================================================
-- создание таблиц
CREATE TABLE dbo.Fuels (FuelID int IDENTITY(1,1) NOT NULL PRIMARY KEY, FuelType nvarchar(50), FuelDensity real) -- виды топлива
CREATE TABLE dbo.Tanks (TankID int IDENTITY(1,1) NOT NULL PRIMARY KEY, TankType nvarchar(20), TankVolume real, TankWeight real, TankMaterial nvarchar(20)) -- емкости
CREATE TABLE dbo.Operations (OperationID int IDENTITY(1,1) NOT NULL PRIMARY KEY, FuelID int, TankID int, Inc_Exp real, [Date] date) -- операции
-- Добавление связей между таблицами
ALTER TABLE dbo.Operations  WITH CHECK ADD  CONSTRAINT FK_Operations_Fuels FOREIGN KEY(FuelID)
REFERENCES dbo.Fuels (FuelID) ON DELETE CASCADE
GO
ALTER TABLE dbo.Operations  WITH CHECK ADD  CONSTRAINT FK_Operations_Tanks FOREIGN KEY(TankID)
REFERENCES dbo.Tanks (TankID) ON DELETE CASCADE
GO


-- ================================================
-- создание представления для отбора данных всех операций
CREATE VIEW [dbo].[View_AllOperations]
AS
SELECT        dbo.Operations.OperationID, dbo.Operations.FuelID, dbo.Operations.TankID, dbo.Operations.Inc_Exp, dbo.Operations.Date, dbo.Fuels.FuelType, 
                         dbo.Tanks.TankType
FROM            dbo.Fuels INNER JOIN
                         dbo.Operations ON dbo.Fuels.FuelID = dbo.Operations.FuelID INNER JOIN
                         dbo.Tanks ON dbo.Operations.TankID = dbo.Tanks.TankID
GO
-- ================================================
-- создание хранимой процедуры для выбора данных одной или нескольких операций по заданным параметрам.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID ( 'dbo.uspGetOperations', 'P' ) IS NOT NULL 
    DROP PROCEDURE dbo.uspGetOperations;
GO
CREATE PROCEDURE dbo.uspGetOperations
	@FuelID int =-100, 
    @FuelType nvarchar(50) ='',
	@TankID int =-100, 
    @TankType nvarchar(20) =''
AS 
    BEGIN
    
	if @TankID>0 and @FuelID>0 	
	SELECT * 
    FROM dbo.View_AllOperations
	WHERE (
	(FuelType Like (@FuelType + '%')) AND 
	(TankType Like (@TankType + '%')) AND
	(TankID=@TankID) AND
    (FuelID=@FuelID)	
	);	
	
	if @TankID>0 and @FuelID<0	
	SELECT * 
    FROM dbo.View_AllOperations
	WHERE (
	(FuelType Like (@FuelType + '%')) AND 
	(TankType Like (@TankType + '%')) AND
	(TankID=@TankID)
	);	
	
	if @TankID<0 and @FuelID>0	
	SELECT * 
    FROM dbo.View_AllOperations
	WHERE (
	(FuelType Like (@FuelType + '%')) AND 
	(TankType Like (@TankType + '%')) AND
	(FuelID=@FuelID)
	);
	
	if @TankID<0 and @FuelID<0	
	SELECT * 
    FROM dbo.View_AllOperations
	WHERE (
	(FuelType Like (@FuelType + '%')) AND 
	(TankType Like (@TankType + '%')) 
	);		
	
	END;
GO