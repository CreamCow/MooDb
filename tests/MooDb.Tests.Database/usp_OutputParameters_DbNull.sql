CREATE PROCEDURE [Tests].[usp_OutputParameters_DbNull]
    @OutputValue INT OUTPUT,
    @InputOutputText NVARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @OutputValue = NULL;
    SET @InputOutputText = NULL;
END;
