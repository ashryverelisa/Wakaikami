using System.Data;
using Wakaikami.Database.World.Repositories.Models;

namespace Wakaikami.Database.World.Repositories.Interfaces;

public interface ICharacterRepository
{
    public Task<T> WithCharacterDataAsync<T>(int characterId, Func<IDataReader, T> handler, CancellationToken cancellationToken = default);

    public Task<CharCreateResult> CreateCharacterAsync(
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
    );

    public Task<int> DeleteCharacterAsync(int characterId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<int>> GetCharacterListAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>Grants <paramref name="skillIds"/> to the character, skipping ones it already knows.</summary>
    /// <returns>The number of skills actually added.</returns>
    public Task<int> AddSkillsAsync(int characterId, IReadOnlyCollection<ushort> skillIds, CancellationToken cancellationToken = default);
}
