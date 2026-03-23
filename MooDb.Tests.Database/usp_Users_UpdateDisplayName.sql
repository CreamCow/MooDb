CREATE PROCEDURE [dbo].[usp_Users_UpdateDisplayName]
    @UserId INT,
    @DisplayName NVARCHAR(200),
    @UpdatedUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[tbl_User]
    SET
        [DisplayName] = @DisplayName,
        [UpdatedUtc] = @UpdatedUtc
    WHERE [UserId] = @UserId;
END;