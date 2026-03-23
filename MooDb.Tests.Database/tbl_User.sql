CREATE TABLE [dbo].[tbl_User]
(
    [UserId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Email] NVARCHAR(320) NOT NULL,
    [DisplayName] NVARCHAR(200) NOT NULL,
    [Age] INT NULL,
    [IsActive] BIT NOT NULL,
    [CreatedUtc] DATETIME2(7) NOT NULL,
    [UpdatedUtc] DATETIME2(7) NULL,

    CONSTRAINT [ak_User_Email] UNIQUE ([Email])
);