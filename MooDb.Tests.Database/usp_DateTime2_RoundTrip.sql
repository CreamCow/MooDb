CREATE PROCEDURE [Tests].[usp_DateTime2_RoundTrip]
    @Value DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @Value;
END;