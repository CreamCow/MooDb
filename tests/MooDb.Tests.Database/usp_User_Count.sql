CREATE PROCEDURE [dbo].[usp_User_Count]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*)
    FROM [dbo].[tbl_User];
END;