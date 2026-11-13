USE CloudColorDB;
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock int;
    EXEC @lock = sys.sp_getapplock @Resource = N'CloudColorSchemaMigration',
        @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 60000;
    IF @lock < 0 THROW 52200, 'No se pudo adquirir el bloqueo de migracion.', 1;

    DECLARE @version nvarchar(20) = (SELECT CONVERT(nvarchar(20), value)
        FROM sys.extended_properties WHERE class = 0 AND name = N'CloudColorDockerSchemaVersion');
    IF @version IS NULL OR @version NOT IN (N'1', N'2')
        THROW 52201, 'Version de esquema ausente o no compatible.', 1;

    IF @version = N'1' AND OBJECT_ID(N'dbo.AspNetDataProtectionKeys', N'U') IS NOT NULL
        THROW 52203, 'La tabla de claves ya existe en un esquema version 1; revise manualmente.', 1;

    IF @version = N'1'
        CREATE TABLE dbo.AspNetDataProtectionKeys
        (
            Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetDataProtectionKeys PRIMARY KEY,
            FriendlyName nvarchar(256) NOT NULL,
            Xml nvarchar(max) NOT NULL,
            CreatedUtc datetime2 NOT NULL CONSTRAINT DF_AspNetDataProtectionKeys_CreatedUtc DEFAULT SYSUTCDATETIME()
        );

    IF OBJECT_ID(N'dbo.AspNetDataProtectionKeys', N'U') IS NULL
       OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.AspNetDataProtectionKeys')
            AND name=N'Id' AND system_type_id=TYPE_ID(N'int') AND is_identity=1 AND is_nullable=0)
       OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.AspNetDataProtectionKeys')
            AND name=N'FriendlyName' AND system_type_id=TYPE_ID(N'nvarchar') AND (max_length=-1 OR max_length>=512) AND is_nullable=0)
       OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.AspNetDataProtectionKeys')
            AND name=N'Xml' AND system_type_id=TYPE_ID(N'nvarchar') AND max_length=-1 AND is_nullable=0)
       OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.AspNetDataProtectionKeys')
            AND name=N'CreatedUtc' AND system_type_id=TYPE_ID(N'datetime2') AND is_nullable=0 AND default_object_id<>0)
        THROW 52202, 'La tabla de claves no existe o tiene una estructura incompatible.', 1;

    IF @version = N'1'
        EXEC sys.sp_updateextendedproperty @name=N'CloudColorDockerSchemaVersion', @value=N'2';
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
