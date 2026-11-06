-- Agri-Stream Azure SQL schema
-- Run once against the Azure SQL database (Query editor in the portal or SSMS).

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Username     NVARCHAR(100)     NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
        PasswordHash NVARCHAR(256)     NOT NULL,
        Role         NVARCHAR(20)      NOT NULL CONSTRAINT CK_Users_Role CHECK (Role IN ('Farmer', 'Agronomist', 'Admin')),
        CreatedAt    DATETIME2         NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF OBJECT_ID('dbo.DeviceRegistry', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeviceRegistry
    (
        DeviceId          NVARCHAR(50)  NOT NULL CONSTRAINT PK_DeviceRegistry PRIMARY KEY,
        FieldId           NVARCHAR(50)  NOT NULL,
        DeviceType        NVARCHAR(50)  NOT NULL,
        OperationalStatus NVARCHAR(30)  NOT NULL CONSTRAINT DF_DeviceRegistry_Status DEFAULT 'Idle',
        LastCommand       NVARCHAR(200) NULL,
        LastUpdated       DATETIME2     NOT NULL CONSTRAINT DF_DeviceRegistry_LastUpdated DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- Seed irrigation controllers and soil sensors for the demo fields.
MERGE dbo.DeviceRegistry AS target
USING (VALUES
    ('VALVE-001', 'FIELD-A', 'IrrigationValve'),
    ('VALVE-002', 'FIELD-B', 'IrrigationValve'),
    ('VALVE-003', 'FIELD-C', 'IrrigationValve'),
    ('SENSOR-A1', 'FIELD-A', 'SoilSensor'),
    ('SENSOR-B1', 'FIELD-B', 'SoilSensor'),
    ('SENSOR-C1', 'FIELD-C', 'SoilSensor')
) AS source (DeviceId, FieldId, DeviceType)
ON target.DeviceId = source.DeviceId
WHEN NOT MATCHED THEN
    INSERT (DeviceId, FieldId, DeviceType) VALUES (source.DeviceId, source.FieldId, source.DeviceType);
GO
