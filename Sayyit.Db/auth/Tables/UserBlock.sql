CREATE TABLE auth.UserBlock
(
    UserBlockId BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_UserBlock PRIMARY KEY,

    UserId UNIQUEIDENTIFIER NOT NULL,
    BlockedUserId UNIQUEIDENTIFIER NOT NULL,

    CreatedUtc DATETIME2(3) NOT NULL
        CONSTRAINT DF_UserBlock_CreatedUtc DEFAULT SYSUTCDATETIME(),

    RemovedUtc DATETIME2(3) NULL,

    CONSTRAINT FK_UserBlock_User
        FOREIGN KEY (UserId) REFERENCES auth.Users(UserId),

    CONSTRAINT FK_UserBlock_BlockedUser
        FOREIGN KEY (BlockedUserId) REFERENCES auth.Users(UserId),

    CONSTRAINT CK_UserBlock_NoSelfBlock
        CHECK (UserId <> BlockedUserId)
);
GO
CREATE INDEX IX_UserBlock_UserId_RemovedUtc
    ON auth.UserBlock(UserId, RemovedUtc);
GO
CREATE INDEX IX_UserBlock_BlockedUserId_RemovedUtc
    ON auth.UserBlock(BlockedUserId, RemovedUtc);
GO
CREATE UNIQUE INDEX UX_UserBlock_Active
    ON auth.UserBlock(UserId, BlockedUserId)
    WHERE RemovedUtc IS NULL;