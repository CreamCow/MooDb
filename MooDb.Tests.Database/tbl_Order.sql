CREATE TABLE [dbo].[tbl_Order]
(
    [OrderId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] INT NOT NULL,
    [OrderNumber] NVARCHAR(50) NOT NULL,
    [Total] DECIMAL(18,2) NOT NULL,
    [CreatedUtc] DATETIME2(7) NOT NULL,

    CONSTRAINT [ak_Order_OrderNumber] UNIQUE ([OrderNumber]),
    CONSTRAINT [fk_Order_User_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[tbl_User]([UserId])
);
