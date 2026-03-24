CREATE PROCEDURE [dbo].[usp_User_List]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [UserId],
        [Email],
        [DisplayName],
        [Age],
        [IsActive],
        [CreatedUtc],
        [UpdatedUtc]
    FROM [dbo].[tbl_User]
    ORDER BY [UserId];
END;