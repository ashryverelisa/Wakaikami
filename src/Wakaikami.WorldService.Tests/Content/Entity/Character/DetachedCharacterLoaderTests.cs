using System.Data;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wakaikami.Database.World.Repositories.Interfaces;
using Wakaikami.WorldService.Content.Entity.Character;

namespace Wakaikami.WorldService.Tests.Content.Entity.Character;

public sealed class DetachedCharacterLoaderTests
{
    private readonly ICharacterRepository _repository = Substitute.For<ICharacterRepository>();

    private DetachedCharacterLoader CreateLoader() => new(_repository, NullLogger<DetachedCharacterLoader>.Instance);

    private static DataTable CharacterData()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Slot", typeof(byte));
        table.Columns.Add("Level", typeof(byte));
        table.Columns.Add("PosX", typeof(int));
        table.Columns.Add("PosY", typeof(int));
        table.Columns.Add("MapIndex", typeof(string));
        table.Columns.Add("Class", typeof(byte));
        table.Columns.Add("Race", typeof(byte));
        table.Columns.Add("IsMale", typeof(byte));
        table.Columns.Add("Hair", typeof(byte));
        table.Columns.Add("HairColor", typeof(byte));
        table.Columns.Add("Face", typeof(byte));
        return table;
    }

    private void DataReturns(int characterId, DataTable table) =>
        _repository
            .WithCharacterDataAsync(characterId, Arg.Any<Func<IDataReader, WorldCharacter?>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<IDataReader, WorldCharacter?>>()(table.CreateDataReader()));

    [Fact]
    public async Task LoadMapsRowOntoCharacter()
    {
        var table = CharacterData();
        table.Rows.Add(7, "Hero", (byte)3, (byte)12, 6888, 7045, "RouN", (byte)16, (byte)0, (byte)1, (byte)2, (byte)4, (byte)5);
        DataReturns(7, table);

        var (character, error) = await CreateLoader().TryLoadAsync(7, TestContext.Current.CancellationToken);

        Assert.Equal(CharacterErrors.None, error);
        Assert.NotNull(character);
        Assert.Equal(7, character.Id);
        Assert.Equal("Hero", character.Name);
        Assert.Equal(3, character.Slot);
        Assert.Equal(12, character.Level);
        Assert.Equal("RouN", character.MapIndex);
        Assert.Equal(6888, character.Position.X);
        Assert.Equal(7045, character.Position.Y);
        Assert.Equal(16, character.Shape.ClassId);
        Assert.True(character.Shape.IsMale);
        Assert.Equal(2, character.Shape.HairTypeId);
        Assert.Equal(4, character.Shape.HairColorId);
        Assert.Equal(5, character.Shape.FaceId);
    }

    [Fact]
    public async Task LoadMissingCharacterReturnsInfoError()
    {
        DataReturns(7, CharacterData());

        var (character, error) = await CreateLoader().TryLoadAsync(7, TestContext.Current.CancellationToken);

        Assert.Null(character);
        Assert.Equal(CharacterErrors.Info, error);
    }

    [Fact]
    public async Task LoadDatabaseErrorReturnsDataError()
    {
        _repository
            .WithCharacterDataAsync(7, Arg.Any<Func<IDataReader, WorldCharacter?>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));

        var (character, error) = await CreateLoader().TryLoadAsync(7, TestContext.Current.CancellationToken);

        Assert.Null(character);
        Assert.Equal(CharacterErrors.DataErr, error);
    }
}
