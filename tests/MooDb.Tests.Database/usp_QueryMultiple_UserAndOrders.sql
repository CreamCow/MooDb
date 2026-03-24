CREATE PROCEDURE [Tests].[usp_QueryMultiple_UserAndOrders]
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

    SELECT
        [OrderId],
        [UserId],
        [OrderNumber],
        [Total],
        [CreatedUtc]
    FROM [dbo].[tbl_Order]
    WHERE [UserId] = @UserId
    ORDER BY [OrderId];
END;