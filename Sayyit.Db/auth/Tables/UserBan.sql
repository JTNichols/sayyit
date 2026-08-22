CREATE TABLE auth.UserBan
(
    UserBanId BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_UserBan PRIMARY KEY,

    UserId UNIQUEIDENTIFIER NOT NULL,
    BanScope NVARCHAR(30) NOT NULL,
    Reason NVARCHAR(500) NULL,

    StartsUtc DATETIME2(3) NOT NULL
        CONSTRAINT DF_UserBan_StartsUtc DEFAULT SYSUTCDATETIME(),

    EndsUtc DATETIME2(3) NULL,
    LiftedUtc DATETIME2(3) NULL,
    IssuedByUserId UNIQUEIDENTIFIER NULL,

    CONSTRAINT FK_UserBan_User
        FOREIGN KEY (UserId) REFERENCES auth.Users(UserId),

    CONSTRAINT FK_UserBan_IssuedByUser
        FOREIGN KEY (IssuedByUserId) REFERENCES auth.Users(UserId),

    CONSTRAINT CK_UserBan_BanScope
        CHECK (BanScope IN ('Site','Posting','Commenting','Voting','Messaging')),

    CONSTRAINT CK_UserBan_DateRange
        CHECK (EndsUtc IS NULL OR EndsUtc > StartsUtc),

    CONSTRAINT CK_UserBan_LiftedUtc
        CHECK (LiftedUtc IS NULL OR LiftedUtc >= StartsUtc)
);
GO
CREATE INDEX IX_UserBan_UserId_LiftedUtc_EndsUtc
    ON auth.UserBan(UserId, LiftedUtc, EndsUtc);