CREATE PROCEDURE [dbo].[usp_User_GetById]
    @UserId INT
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
    WHERE [UserId] = @UserId;
END;