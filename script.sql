-- Script to configure managed identity database access
-- This will be populated by the deployment script with the actual managed identity name

IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'mid-expensemgmt-alqx7owaivpb2')
BEGIN
    DROP USER [mid-expensemgmt-alqx7owaivpb2];
END
GO

CREATE USER [mid-expensemgmt-alqx7owaivpb2] FROM EXTERNAL PROVIDER;
GO

ALTER ROLE db_datareader ADD MEMBER [mid-expensemgmt-alqx7owaivpb2];
GO

ALTER ROLE db_datawriter ADD MEMBER [mid-expensemgmt-alqx7owaivpb2];
GO

GRANT EXECUTE TO [mid-expensemgmt-alqx7owaivpb2];
GO
