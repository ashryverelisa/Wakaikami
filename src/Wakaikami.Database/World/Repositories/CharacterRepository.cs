using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Wakaikami.Core.Database;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Database.Interfaces;
using Wakaikami.Database.World.Repositories.Interfaces;
using Wakaikami.Database.World.Repositories.Models;

namespace Wakaikami.Database.World.Repositories;

public sealed class CharacterRepository(IDbConnectionFactory connections, ILogger<CharacterRepository> logger)
    : RepositoryBase<CharacterRepository>(connections, DatabaseType.World, logger),
        ICharacterRepository
{
    private const string CharacterCreate = "p_Character_Create";
    private const string CharacterDelete = "p_Character_Delete";
    private const string CharacterGetList = "p_Character_GetList";
    private const string CharacterGetData = "p_Character_GetData";
    private const string CharacterAddSkills = "p_Character_AddSkills";

    public Task<T> WithCharacterDataAsync<T>(int characterId, Func<IDataReader, T> handler, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("pCharID", characterId);

        return WithReaderAsync(CharacterGetData, parameters, (reader, _) => Task.FromResult(handler(reader)), cancellationToken);
    }

    public async Task<CharCreateResult> CreateCharacterAsync(
        int accountId,
        string name,
        byte slot,
        short level,
        ulong exp,
        long money,
        int hp,
        int sp,
        int lp,
        short hpStone,
        short spStone,
        string mapIndex,
        int posX,
        int posY,
        byte posZ,
        byte classId,
        bool isMale,
        byte hair,
        byte hairColor,
        byte face,
        CancellationToken cancellationToken = default
    )
    {
        var parameters = new DynamicParameters();
        parameters.Add("pAccountID", accountId);
        parameters.Add("pName", name);
        parameters.Add("pSlot", slot);
        parameters.Add("pLevel", level);
        parameters.Add("pExp", checked((long)exp));
        parameters.Add("pMoney", money);
        parameters.Add("pHP", hp);
        parameters.Add("pSP", sp);
        parameters.Add("pLP", lp);
        parameters.Add("pHPStone", hpStone);
        parameters.Add("pSPStone", spStone);
        parameters.Add("pMapIndex", mapIndex);
        parameters.Add("pPosX", posX);
        parameters.Add("pPosY", posY);
        parameters.Add("pDirection", posZ);
        parameters.Add("pClass", classId);
        parameters.Add("pIsMale", isMale);
        parameters.Add("pHair", hair);
        parameters.Add("pHairColor", hairColor);
        parameters.Add("pFace", face);

        return await QuerySingleAsync<CharCreateResult>(CharacterCreate, parameters, cancellationToken) ?? new CharCreateResult(null, -1);
    }

    public Task<int> DeleteCharacterAsync(int characterId, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("pCharID", characterId);

        return ExecScalarAsync<int>(CharacterDelete, parameters, cancellationToken);
    }

    public Task<IReadOnlyList<int>> GetCharacterListAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("pAccountID", accountId);

        return QueryListAsync<int>(CharacterGetList, parameters, cancellationToken);
    }

    public async Task<int> AddSkillsAsync(int characterId, IReadOnlyCollection<ushort> skillIds, CancellationToken cancellationToken = default)
    {
        if (skillIds.Count == 0)
            return 0;

        var parameters = new DynamicParameters();
        parameters.Add("pCharID", characterId);
        parameters.Add("pSkillIDs", string.Join(',', skillIds));

        return await ExecScalarAsync<int>(CharacterAddSkills, parameters, cancellationToken);
    }
}
