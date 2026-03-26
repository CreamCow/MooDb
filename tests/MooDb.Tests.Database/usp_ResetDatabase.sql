CREATE PROCEDURE [Tests].[usp_ResetDatabase]
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [dbo].[tbl_Order];
    DELETE FROM [dbo].[tbl_User];
    DELETE FROM [Tests].[tbl_AllTypes];

    DBCC CHECKIDENT ('[dbo].[tbl_Order]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[tbl_User]', RESEED, 0);
    DBCC CHECKIDENT ('[Tests].[tbl_AllTypes]', RESEED, 0);
END;