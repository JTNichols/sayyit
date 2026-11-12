/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM [auth].[Users])
BEGIN
    THROW 50001, 'Create at least one user in auth.Users before generating test posts.', 1;
END;

DECLARE @Alphabet NVARCHAR(100) =
    N'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?';

DECLARE @AlphabetLength INT = LEN(@Alphabet);
DECLARE @PostNumber INT = 0;
DECLARE @BodyLength INT;
DECLARE @CharacterIndex INT;
DECLARE @Body NVARCHAR(1000);
DECLARE @UserId UNIQUEIDENTIFIER;

BEGIN TRY
    BEGIN TRANSACTION;

    WHILE @PostNumber < 100
    BEGIN
        -- Select an existing user to satisfy FK_Posts_Users.
        SELECT TOP (1)
            @UserId = [UserId]
        FROM [auth].[Users]
        ORDER BY NEWID();

        -- Convert to BIGINT before ABS to avoid INT overflow.
        SET @BodyLength =
            1 + CONVERT(INT, ABS(CONVERT(BIGINT, CHECKSUM(NEWID()))) % 1000);

        SET @Body = N'';

        WHILE DATALENGTH(@Body) / 2 < @BodyLength
        BEGIN
            SET @CharacterIndex =
                1 + CONVERT(
                    INT,
                    ABS(CONVERT(BIGINT, CHECKSUM(NEWID()))) % @AlphabetLength
                );

            SET @Body = @Body + SUBSTRING(@Alphabet, @CharacterIndex, 1);
        END;

        INSERT INTO [content].[Posts] ([UserId], [Body])
        VALUES (@UserId, @Body);

        SET @PostNumber += 1;
    END;

    COMMIT TRANSACTION;

    SELECT @PostNumber AS [PostsCreated];
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;