CREATE PROCEDURE [Tests].[usp_ResetDatabase]
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [dbo].[tbl_Order];
    DELETE FROM [dbo].[tbl_User];

    DBCC CHECKIDENT ('[dbo].[tbl_Order]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[tbl_User]', RESEED, 0);
END;