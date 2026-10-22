using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wakaikami.Database.World.Repositories.Interfaces;
using Wakaikami.Database.World.Repositories.Models;
using Wakaikami.WorldService.Content.Entity.Character;
using Wakaikami.WorldService.Content.Entity.Character.Enums;
using Wakaikami.WorldService.Content.Entity.Character.Interfaces;

namespace Wakaikami.WorldService.Tests.Content.Entity.Character;

public sealed class CharacterRosterServiceTests
{
    private readonly ICharacterRepository _repository = Substitute.For<ICharacterRepository>();
    private readonly IDetachedCharacterLoader _loader = Substitute.For<IDetachedCharacterLoader>();

    private CharacterRosterService CreateService() => new(_repository, _loader, NullLogger<CharacterRosterService>.Instance);

    private static CreateCharacterRequest Request(IReadOnlySet<ushort>? skills = null) =>
        new()
        {
            Slot = 0,
            Name = "Hero",
            ClassId = 1,
            IsMale = true,
            Hair = 0,
            HairColor = 0,
            Face = 0,
            MapIndex = "RouN",
            StartPosition = new Vector2(6888, 7045),
            Level = 1,
            Exp = 0,
            Money = 0,
            Hp = 52,
            Sp = 10,
            Lp = 0,
            HpStones = 15,
            SpStones = 7,
            Skills = skills,
        };

    private Task<CharCreateResult> CreateCall() =>
        _repository.CreateCharacterAsync(
            default,
            default!,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default!,
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            default
        );

    private void CreateReturns(CharCreateResult result) => CreateCall().ReturnsForAnyArgs(result);

    [Theory]
    [InlineData(-2, CharacterCreateOutcome.NameInUse)]
    [InlineData(-3, CharacterCreateOutcome.MaxSlotReached)]
    [InlineData(-99, CharacterCreateOutcome.Failed)]
    public async Task CreateMapsProcedureCodeToOutcome(int code, CharacterCreateOutcome expected)
    {
        CreateReturns(new CharCreateResult(code, -1));

        var result = await CreateService().CreateCharacterAsync(1, Request(), TestContext.Current.CancellationToken);

        Assert.False(result.Ok);
        Assert.Equal(expected, result.Outcome);
    }

    [Fact]
    public async Task CreateWithoutResultRowFails()
    {
        CreateReturns(new CharCreateResult(null, -1));

        var result = await CreateService().CreateCharacterAsync(1, Request(), TestContext.Current.CancellationToken);

        Assert.Equal(CharacterCreateOutcome.Failed, result.Outcome);
    }

    [Fact]
    public async Task CreateSuccessReturnsIdAndGrantsDefaultSkills()
    {
        CreateReturns(new CharCreateResult(-1, 42));
        IReadOnlySet<ushort> skills = new HashSet<ushort> { 29200, 29201 };

        var result = await CreateService().CreateCharacterAsync(1, Request(skills), TestContext.Current.CancellationToken);

        Assert.True(result.Ok);
        Assert.Equal(42, result.CharId);
        await _repository.Received(1).AddSkillsAsync(42, skills, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateStillSucceedsWhenSkillGrantFails()
    {
        CreateReturns(new CharCreateResult(-1, 42));
        _repository.AddSkillsAsync(default, default!, TestContext.Current.CancellationToken).ThrowsAsyncForAnyArgs(new InvalidOperationException("db down"));

        var result = await CreateService().CreateCharacterAsync(1, Request(new HashSet<ushort> { 29200 }), TestContext.Current.CancellationToken);

        Assert.True(result.Ok);
    }

    [Fact]
    public async Task CreateWithoutSkillsDoesNotTouchSkillTable()
    {
        CreateReturns(new CharCreateResult(-1, 42));

        await CreateService().CreateCharacterAsync(1, Request(), TestContext.Current.CancellationToken);

        await _repository.DidNotReceiveWithAnyArgs().AddSkillsAsync(default, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateRepositoryErrorFails()
    {
        CreateCall().ThrowsAsyncForAnyArgs(new InvalidOperationException("db down"));

        var result = await CreateService().CreateCharacterAsync(1, Request(), TestContext.Current.CancellationToken);

        Assert.Equal(CharacterCreateOutcome.Failed, result.Outcome);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    public async Task DeleteMapsProcedureCode(int code, bool expected)
    {
        _repository.DeleteCharacterAsync(7, Arg.Any<CancellationToken>()).Returns(code);

        Assert.Equal(expected, await CreateService().DeleteCharacterAsync(7, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoadCharacterListSkipsCharactersThatFailToLoad()
    {
        _repository.GetCharacterListAsync(1, Arg.Any<CancellationToken>()).Returns([10, 11, 12]);
        _loader
            .TryLoadAsync(10, Arg.Any<CancellationToken>())
            .Returns(
                (
                    new WorldCharacter
                    {
                        Id = 10,
                        Slot = 0,
                        Name = "A",
                    },
                    CharacterErrors.None
                )
            );
        _loader.TryLoadAsync(11, Arg.Any<CancellationToken>()).Returns(((WorldCharacter?)null, CharacterErrors.Info));
        _loader
            .TryLoadAsync(12, Arg.Any<CancellationToken>())
            .Returns(
                (
                    new WorldCharacter
                    {
                        Id = 12,
                        Slot = 2,
                        Name = "C",
                    },
                    CharacterErrors.None
                )
            );
        var list = new WorldCharacterList();

        Assert.True(await CreateService().LoadCharacterListAsync(1, list, TestContext.Current.CancellationToken));

        Assert.Equal(2, list.Count);
        Assert.Equal(10, list[0]!.Id);
        Assert.Equal(12, list[2]!.Id);
    }

    [Fact]
    public async Task LoadCharacterListReplacesPreviousEntries()
    {
        _repository.GetCharacterListAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<int>());
        var list = new WorldCharacterList();
        list.Add(new CharacterListEntry(new WorldCharacter { Id = 99, Slot = 4 }));

        Assert.True(await CreateService().LoadCharacterListAsync(1, list, TestContext.Current.CancellationToken));

        Assert.Equal(0, list.Count);
    }

    [Fact]
    public async Task LoadCharacterListFailsWhenIdsCannotBeRead()
    {
        _repository.GetCharacterListAsync(1, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("db down"));
        var list = new WorldCharacterList();
        list.Add(new CharacterListEntry(new WorldCharacter { Id = 99, Slot = 4 }));

        Assert.False(await CreateService().LoadCharacterListAsync(1, list, TestContext.Current.CancellationToken));

        Assert.Equal(1, list.Count);
    }
}
