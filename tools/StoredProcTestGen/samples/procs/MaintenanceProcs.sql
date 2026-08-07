-- Two procedures in one file, plus edge cases:
--   * no parameters
--   * parameters without wrapping parentheses
--   * a comment containing '@fake' and the word AS to test noise stripping

-- Rebuilds all indexes. Takes no parameters. AS @not_a_param
CREATE PROCEDURE dbo.usp_RebuildAllIndexes
AS
BEGIN
    SET NOCOUNT ON;
    EXEC sp_MSforeachtable @command1 = 'ALTER INDEX ALL ON ? REBUILD';
END
GO

-- Parameter list is NOT wrapped in parentheses (valid T-SQL).
CREATE PROC usp_PurgeOldLogs
    @OlderThan DATETIME2,
    @BatchSize INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    DELETE TOP (@BatchSize) FROM dbo.AppLog WHERE CreatedOn < @OlderThan;
END
GO
