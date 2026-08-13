namespace Wakaikami.WorldService.Content.Entity.Character.Interfaces;

public interface IDetachedCharacterLoader
{
    public Task<(WorldCharacter? character, CharacterErrors error)> TryLoadAsync(int characterId, CancellationToken cancellationToken = default);
}
