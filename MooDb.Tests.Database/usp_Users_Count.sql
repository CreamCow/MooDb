CREATE PROCEDURE [dbo].[usp_Users_Count]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*)
    FROM [dbo].[tbl_User];
END;