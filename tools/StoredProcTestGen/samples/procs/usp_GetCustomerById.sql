-- Returns a single customer by primary key.
CREATE PROCEDURE [dbo].[usp_GetCustomerById]
    @CustomerId INT,
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CustomerId, Name, Email, IsActive
    FROM dbo.Customers
    WHERE CustomerId = @CustomerId
      AND (@IncludeInactive = 1 OR IsActive = 1);
END
GO
