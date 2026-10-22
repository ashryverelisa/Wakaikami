using Microsoft.EntityFrameworkCore;
using Wakaikami.Database.Common.CustomMigrations.Interfaces;
using Wakaikami.Database.World;

namespace Wakaikami.Database.Migrations.World.CustomMigrations;

public class M20261022_AddCharacterProcedures : ICustomMigration<WorldDbContext>
{
    public string Name => nameof(M20261022_AddCharacterProcedures);

    public async Task MigrateAsync(WorldDbContext db)
    {
        // Result codes are mirrored by CharacterRosterService.CreateCode: -1 created, -2 name in use, -3 slot taken.
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE PROCEDURE [dbo].[p_Character_Create]
                @pAccountID INT,
                @pName NVARCHAR(20),
                @pSlot TINYINT,
                @pLevel TINYINT,
                @pExp BIGINT,
                @pMoney BIGINT,
                @pHP INT,
                @pSP INT,
                @pLP INT,
                @pHPStone INT,
                @pSPStone INT,
                @pMapIndex NVARCHAR(64),
                @pPosX INT,
                @pPosY INT,
                @pDirection TINYINT,
                @pClass TINYINT,
                @pIsMale BIT,
                @pHair TINYINT,
                @pHairColor TINYINT,
                @pFace TINYINT
            AS
            BEGIN
                SET NOCOUNT ON;
                SET XACT_ABORT ON;

                DECLARE @CharID INT;

                BEGIN TRANSACTION;

                -- UPDLOCK + HOLDLOCK keep the checked name / slot locked until commit, so a concurrent
                -- create cannot slip in between the check and the insert.
                IF EXISTS (SELECT 1 FROM [character] WITH (UPDLOCK, HOLDLOCK) WHERE [Name] = @pName)
                BEGIN
                    ROLLBACK TRANSACTION;
                    SELECT -2 AS Code, -1 AS CharId;
                    RETURN;
                END

                IF EXISTS (
                    SELECT 1 FROM [character] WITH (UPDLOCK, HOLDLOCK)
                    WHERE [AccountId] = @pAccountID AND [Slot] = @pSlot AND [IsDeleted] = 0
                )
                BEGIN
                    ROLLBACK TRANSACTION;
                    SELECT -3 AS Code, -1 AS CharId;
                    RETURN;
                END

                INSERT INTO [character] (
                    [AccountId], [Name], [Slot], [Level], [Exp], [Money], [Hp], [Sp], [Lp], [HpStones], [SpStones],
                    [Fame], [PosX], [PosY], [Direction], [MapIndex], [LoginCount], [IsRename], [CreatedAt],
                    [FriendPoints], [LastFriendPoint], [KillCount], [IsDeleted]
                )
                VALUES (
                    @pAccountID, @pName, @pSlot, @pLevel, @pExp, @pMoney, @pHP, @pSP, @pLP, @pHPStone, @pSPStone,
                    0, @pPosX, @pPosY, @pDirection, @pMapIndex, 0, 0, GETDATE(),
                    0, 0, 0, 0
                );

                SET @CharID = CAST(SCOPE_IDENTITY() AS INT);

                INSERT INTO [characterShape] ([CharacterId], [Class], [Race], [IsMale], [Hair], [HairColor], [Face])
                VALUES (@CharID, @pClass, 0, @pIsMale, @pHair, @pHairColor, @pFace);

                INSERT INTO [characterStatPoints] ([CharacterId], [Points], [Spr], [Int], [Dex], [Str], [End], [RedistributePoint])
                VALUES (@CharID, 0, 0, 0, 0, 0, 0, 0);

                -- TS_PROGRESS / step 0: a fresh character starts the tutorial.
                INSERT INTO [characterTutorial] ([CharacterId], [State], [Step], [Date])
                VALUES (@CharID, 0, 0, GETDATE());

                INSERT INTO [characterOptions] ([CharacterId])
                VALUES (@CharID);

                COMMIT TRANSACTION;

                SELECT -1 AS Code, @CharID AS CharId;
            END
            """
        );

        // Result codes are mirrored by CharacterRosterService.DeleteCode: 0 deleted, 1 already deleted,
        // 2 not found, 3 rename failed. The row is kept (soft delete) but gives up its name and slot.
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE PROCEDURE [dbo].[p_Character_Delete]
                @pCharID INT
            AS
            BEGIN
                SET NOCOUNT ON;
                SET XACT_ABORT ON;

                DECLARE @IsDeleted BIT;

                BEGIN TRANSACTION;

                SELECT @IsDeleted = [IsDeleted] FROM [character] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = @pCharID;

                IF @IsDeleted IS NULL
                BEGIN
                    ROLLBACK TRANSACTION;
                    SELECT 2;
                    RETURN;
                END

                IF @IsDeleted = 1
                BEGIN
                    ROLLBACK TRANSACTION;
                    SELECT 1;
                    RETURN;
                END

                BEGIN TRY
                    -- '@<id>' frees the name for reuse and is unique per row.
                    UPDATE [character]
                    SET [IsDeleted] = 1,
                        [DeletedAt] = GETDATE(),
                        [Name] = CONCAT(N'@', [Id])
                    WHERE [Id] = @pCharID;
                END TRY
                BEGIN CATCH
                    IF XACT_STATE() <> 0
                        ROLLBACK TRANSACTION;
                    SELECT 3;
                    RETURN;
                END CATCH

                COMMIT TRANSACTION;

                SELECT 0;
            END
            """
        );

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE PROCEDURE [dbo].[p_Character_GetList]
                @pAccountID INT
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT [Id]
                FROM [character]
                WHERE [AccountId] = @pAccountID AND [IsDeleted] = 0
                ORDER BY [Slot];
            END
            """
        );

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE PROCEDURE [dbo].[p_Character_GetData]
                @pCharID INT
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT
                    C.[Id],
                    C.[AccountId],
                    C.[Name],
                    C.[Slot],
                    C.[Level],
                    C.[Exp],
                    C.[Money],
                    C.[Hp],
                    C.[Sp],
                    C.[Lp],
                    C.[HpStones],
                    C.[SpStones],
                    C.[Fame],
                    C.[PosX],
                    C.[PosY],
                    C.[Direction],
                    C.[MapIndex],
                    C.[KqHandle],
                    C.[KqDate],
                    C.[IsRename],
                    C.[KillCount],
                    S.[Class],
                    S.[Race],
                    S.[IsMale],
                    S.[Hair],
                    S.[HairColor],
                    S.[Face],
                    T.[State] AS TutorialState,
                    T.[Step]  AS TutorialStep
                FROM [character] C
                INNER JOIN [characterShape] S ON S.[CharacterId] = C.[Id]
                LEFT JOIN [characterTutorial] T ON T.[CharacterId] = C.[Id]
                WHERE C.[Id] = @pCharID AND C.[IsDeleted] = 0;
            END
            """
        );

        // @pSkillIDs is a comma separated id list; ids the character already knows are skipped.
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE PROCEDURE [dbo].[p_Character_AddSkills]
                @pCharID INT,
                @pSkillIDs NVARCHAR(MAX)
            AS
            BEGIN
                SET NOCOUNT ON;

                INSERT INTO [characterSkill] (
                    [CharacterId], [SkillId], [CoolTime],
                    [EmpowerDamage], [EmpowerSp], [EmpowerKeepTime], [EmpowerCoolTime], [Mastery]
                )
                SELECT DISTINCT @pCharID, N.SkillId, 0, 0, 0, 0, 0, 0
                FROM (
                    SELECT TRY_CAST([value] AS INT) AS SkillId
                    FROM STRING_SPLIT(@pSkillIDs, N',')
                ) N
                WHERE N.SkillId IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM [characterSkill] K
                      WHERE K.[CharacterId] = @pCharID AND K.[SkillId] = N.SkillId
                  );

                SELECT @@ROWCOUNT;
            END
            """
        );
    }
}
