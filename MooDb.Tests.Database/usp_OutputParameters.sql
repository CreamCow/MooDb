CREATE PROCEDURE [Tests].[usp_OutputParameters]
    @InputValue INT,
    @OutputValue INT OUTPUT,
    @InputOutputText NVARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @OutputValue = @InputValue + 1;
    SET @InputOutputText = @InputOutputText + N'_Processed';
END;