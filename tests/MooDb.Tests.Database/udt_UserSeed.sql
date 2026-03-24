CREATE TYPE [Tests].[udt_UserSeed] AS TABLE
(
    [Email] NVARCHAR(320) NOT NULL,
    [DisplayName] NVARCHAR(200) NOT NULL,
    [Age] INT NULL,
    [IsActive] BIT NOT NULL,
    [CreatedUtc] DATETIME2(7) NOT NULL,
    [UpdatedUtc] DATETIME2(7) NULL
);
