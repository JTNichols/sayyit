CREATE TABLE auth.UserStatusHistory
(
    UserStatusHistoryId BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_UserStatusHistory PRIMARY KEY,

    UserId UNIQUEIDENTIFIER NOT NULL,
    StatusType NVARCHAR(30) NOT NULL,
    Reason NVARCHAR(500) NULL,
    ChangedByUserId UNIQUEIDENTIFIER NULL,

    ChangedUtc DATETIME2(3) NOT NULL
        CONSTRAINT DF_UserStatusHistory_ChangedUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_UserStatusHistory_Users
        FOREIGN KEY (UserId) REFERENCES auth.Users(UserId),

    CONSTRAINT FK_UserStatusHistory_ChangedByUsers
        FOREIGN KEY (ChangedByUserId) REFERENCES auth.Users(UserId),

    CONSTRAINT CK_UserStatusHistory_StatusType
        CHECK (StatusType IN ('Created','Activated','Suspended','Unsuspended','Deleted','Restored'))
);
GO
CREATE INDEX IX_UserStatusHistory_UserId_ChangedUtc
    ON auth.UserStatusHistory(UserId, ChangedUtc DESC);