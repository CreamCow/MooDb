CREATE PROCEDURE [Tests].[usp_User_InsertFromTable]
    @Users [Tests].[udt_UserSeed] READONLY
AS
BEGIN
    INSERT INTO [dbo].[tbl_User]
    (
        [Email],
        [DisplayName],
        [Age],
        [IsActive],
        [CreatedUtc],
        [UpdatedUtc]
    )
    SELECT
        [Email],
        [DisplayName],
        [Age],
        [IsActive],
        [CreatedUtc],
        [UpdatedUtc]
    FROM @Users;
END;
