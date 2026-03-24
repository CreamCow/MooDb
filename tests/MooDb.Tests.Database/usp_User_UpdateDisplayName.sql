CREATE PROCEDURE [dbo].[usp_User_UpdateDisplayName]
    @UserId INT,
    @DisplayName NVARCHAR(200),
    @UpdatedUtc DATETIME2(7)
AS
BEGIN

    UPDATE [dbo].[tbl_User]
    SET
        [DisplayName] = @DisplayName,
        [UpdatedUtc] = @UpdatedUtc
    WHERE [UserId] = @UserId;
END;