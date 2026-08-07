-- Table-valued parameter (READONLY) plus a WITH options clause before AS.
CREATE PROCEDURE [dbo].[usp_BulkImportCustomers]
    @Customers dbo.CustomerImportType READONLY,
    @Source    VARCHAR(50) = 'api'
WITH RECOMPILE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Customers (Name, Email, IsActive)
    SELECT Name, Email, 1
    FROM @Customers;
END
GO
