CREATE TABLE auth.Users
(
    UserId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_Users PRIMARY KEY
        CONSTRAINT DF_Users_UserId DEFAULT NEWSEQUENTIALID(),

    EntraObjectId UNIQUEIDENTIFIER NOT NULL,
    TenantId UNIQUEIDENTIFIER NOT NULL,

    UserName NVARCHAR(50) NULL,
    DisplayName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(320) NULL,

    LeftRightScore DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_Users_LeftRightScore DEFAULT (0),

    IsActive BIT NOT NULL
        CONSTRAINT DF_Users_IsActive DEFAULT (1),

    IsSuspended BIT NOT NULL
        CONSTRAINT DF_Users_IsSuspended DEFAULT (0),

    IsDeleted BIT NOT NULL
        CONSTRAINT DF_Users_IsDeleted DEFAULT (0),

    CreatedUtc DATETIME2(3) NOT NULL
        CONSTRAINT DF_Users_CreatedUtc DEFAULT SYSUTCDATETIME(),

    UpdatedUtc DATETIME2(3) NOT NULL
        CONSTRAINT DF_Users_UpdatedUtc DEFAULT SYSUTCDATETIME(),

    LastSeenUtc DATETIME2(3) NULL,

    CONSTRAINT UQ_Users_TenantId_EntraObjectId UNIQUE (TenantId, EntraObjectId),
    CONSTRAINT UQ_Users_UserName UNIQUE (UserName),

    CONSTRAINT CK_Users_UserName_NotBlank
        CHECK (UserName IS NULL OR LTRIM(RTRIM(UserName)) <> ''),

    CONSTRAINT CK_Users_DisplayName_NotBlank
        CHECK (LTRIM(RTRIM(DisplayName)) <> ''),

    CONSTRAINT CK_Users_LeftRightScore
        CHECK (LeftRightScore >= -100.00 AND LeftRightScore <= 100.00)
);
GO
CREATE INDEX IX_Users_DisplayName
    ON auth.Users(DisplayName);
GO
CREATE INDEX IX_Users_LeftRightScore
    ON auth.Users(LeftRightScore);
GO
CREATE INDEX IX_Users_TenantId_EntraObjectId
    ON auth.Users(TenantId, EntraObjectId);