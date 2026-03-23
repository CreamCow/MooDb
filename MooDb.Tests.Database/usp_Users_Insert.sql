CREATE PROCEDURE [dbo].[usp_Users_Insert]
    @Email NVARCHAR(320),
    @DisplayName NVARCHAR(200),
    @Age INT = NULL,
    @IsActive BIT,
    @CreatedUtc DATETIME2(7),
    @UpdatedUtc DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[tbl_User]
    (
        [Email],
        [DisplayName],
        [Age],
        [IsActive],
        [CreatedUtc],
        [UpdatedUtc]
    )
    VALUES
    (
        @Email,
        @DisplayName,
        @Age,
        @IsActive,
        @CreatedUtc,
        @UpdatedUtc
    );
END;