using System.Data;
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
    public Task<T> WithCharacterDataAsync<T>(int characterId, Func<IDataReader, T> handler, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<CharCreateResult> CreateCharacterAsync(
        int accountId,
        string name,
        byte slot,
        short level,
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
        throw new NotImplementedException();
    }

    public Task<int> DeleteCharacterAsync(int characterId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<int>> GetCharacterListAsync(int accountId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
