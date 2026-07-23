using Microsoft.EntityFrameworkCore;
using Wakaikami.Database.Common.CustomMigrations.Interfaces;

namespace Wakaikami.Database.Migrations.Login.CustomMigrations;

public class M20260723_AddGetLoginAndMarkLoggedIn : ICustomMigration<LoginDbContext>
{
    public string Name => nameof(M20260723_AddGetLoginAndMarkLoggedIn);

    public async Task MigrateAsync(LoginDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE PROCEDURE [dbo].[p_Account_GetLoginInfo]
                @pName NVARCHAR(50)
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT
                    A.ID          AS Id,
                    A.Password    AS PasswordHash,
                    A.IsActivated AS IsActivated,
                    CASE
                        WHEN (SELECT MAX(B.EndOfBan) FROM AccountBan B WHERE B.AccountID = A.ID) > GETDATE()
                        THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT)
                    END           AS IsBanned
                FROM Account A
                WHERE A.Username = @pName;
            END
            """
        );

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE PROCEDURE [dbo].[p_Account_MarkLoggedIn]
                @pID INT,
                @pLoginIP NVARCHAR(16)
            AS
            BEGIN
                SET NOCOUNT ON;

                DELETE FROM AccountBan WHERE AccountID = @pID AND EndOfBan <= GETDATE();

                UPDATE Account
                SET LastLoginIp = @pLoginIP
                WHERE ID = @pID;
            END
            """
        );
    }
}
