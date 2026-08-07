/*
    Creates an order and returns the new identity via an OUTPUT parameter.
    Demonstrates: schema-qualified name, decimal precision, OUTPUT, defaults.
*/
CREATE OR ALTER PROCEDURE dbo.CreateOrder
    @CustomerId      INT,
    @OrderTotal      DECIMAL(18, 2),
    @Notes           NVARCHAR(MAX) = NULL,
    @PlacedOn        DATETIME2      = NULL,
    @NewOrderId      INT            OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Orders (CustomerId, OrderTotal, Notes, PlacedOn)
    VALUES (@CustomerId, @OrderTotal, @Notes, ISNULL(@PlacedOn, SYSUTCDATETIME()));

    SET @NewOrderId = SCOPE_IDENTITY();
    RETURN 0;
END
GO
