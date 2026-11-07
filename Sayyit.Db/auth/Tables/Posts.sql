CREATE TABLE [content].[Posts]
(
    [PostId] BIGINT IDENTITY(1, 1) NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [Body] NVARCHAR(MAX) NOT NULL,

    [CreatedUtc] DATETIME2(7) NOT NULL
        CONSTRAINT [DF_Posts_CreatedUtc]
        DEFAULT (SYSUTCDATETIME()),

    [UpdatedUtc] DATETIME2(7) NULL,

    [IsDeleted] BIT NOT NULL
        CONSTRAINT [DF_Posts_IsDeleted]
        DEFAULT (0),

    CONSTRAINT [PK_Posts]
        PRIMARY KEY CLUSTERED ([PostId]),

    CONSTRAINT [FK_Posts_Users]
        FOREIGN KEY ([UserId])
        REFERENCES [auth].[Users] ([UserId]),

    CONSTRAINT [CK_Posts_Body_Length]
        CHECK (DATALENGTH([Body]) BETWEEN 2 AND 10000)
);