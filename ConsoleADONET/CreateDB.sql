-- Скрипт создания базы данных toplivo_test с таблицами Fuels, Tanks и Operations
USE master;
GO

-- Создание базы данных
IF DB_ID('toplivo_test') IS NOT NULL
    DROP DATABASE toplivo_test;
GO

CREATE DATABASE toplivo_test;
GO

ALTER DATABASE toplivo_test SET RECOVERY SIMPLE;
GO

USE toplivo_test;
GO

-- ================================================
-- Создание таблиц
CREATE TABLE Fuels (
    FuelId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FuelType NVARCHAR(50),
    FuelDensity REAL
); -- Таблица видов топлива

CREATE TABLE Tanks (
    TankId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TankType NVARCHAR(20),
    TankVolume REAL,
    TankWeight REAL,
    TankMaterial NVARCHAR(20)
); -- Таблица емкостей

CREATE TABLE Operations (
    OperationId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FuelId INT,
    TankId INT,
    Inc_Exp REAL,
    [Date] DATE
); -- Таблица операций

-- Добавление связей между таблицами
ALTER TABLE Operations
    ADD CONSTRAINT FK_Operations_Fuels FOREIGN KEY (FuelId)
    REFERENCES Fuels (FuelId) ON DELETE CASCADE;
GO

ALTER TABLE Operations
    ADD CONSTRAINT FK_Operations_Tanks FOREIGN KEY (TankId)
    REFERENCES Tanks (TankId) ON DELETE CASCADE;
GO

-- ================================================
-- Создание представления для отбора данных всех операций
CREATE VIEW View_AllOperations AS
SELECT 
    o.OperationId,
    o.FuelId,
    o.TankId,
    o.Inc_Exp,
    o.Date,
    f.FuelType,
    t.TankType
FROM 
    Operations o
INNER JOIN 
    Fuels f ON o.FuelId = f.FuelId
INNER JOIN 
    Tanks t ON o.TankId = t.TankId;
GO

-- ================================================
-- Создание хранимой процедуры для выбора данных операций
IF OBJECT_ID('uspGetOperations', 'P') IS NOT NULL
    DROP PROCEDURE uspGetOperations;
GO

CREATE PROCEDURE uspGetOperations
    @FuelId INT = -100, 
    @FuelType NVARCHAR(50) = '',
    @TankId INT = -100, 
    @TankType NVARCHAR(20) = ''
AS
BEGIN
    SET NOCOUNT ON;

    -- Универсальный запрос с динамическими условиями
    SELECT * 
    FROM View_AllOperations
    WHERE 
        (@FuelId < 0 OR FuelId = @FuelId) AND
        (@TankId < 0 OR TankId = @TankId) AND
        (FuelType LIKE (@FuelType + '%')) AND
        (TankType LIKE (@TankType + '%'));
END;
GO

-- Проверяем, существует ли процедура, и удаляем её
IF OBJECT_ID('uspInsertTanks', 'P') IS NOT NULL
    DROP PROCEDURE uspInsertTanks;
GO

-- Создаем хранимую процедуру для вставки записей в таблицу Tanks
CREATE PROCEDURE uspInsertTanks
    @TankType NVARCHAR(20),
    @TankWeight REAL,
    @TankVolume REAL,
    @TankMaterial NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Вставляем данные в таблицу Tanks
        INSERT INTO Tanks (TankType, TankWeight, TankVolume, TankMaterial)
        VALUES (@TankType, @TankWeight, @TankVolume, @TankMaterial);

        PRINT 'Запись успешно вставлена в таблицу Tanks.';
    END TRY
    BEGIN CATCH
        -- Обработка ошибок
        PRINT 'Произошла ошибка при вставке данных в таблицу Tanks.';
        THROW;
    END CATCH
END;
GO
