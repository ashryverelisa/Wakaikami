using Bogus;
using Wakaikami.Content.Account;
using Wakaikami.LoginService.Content.World;

namespace Wakaikami.LoginService.Tests.TestSupport;

internal static class Fakers
{
    static Fakers() => Randomizer.Seed = new Random(0xD2A6);

    private static readonly Faker<GameAccount> _gameAccount = new Faker<GameAccount>()
        .RuleFor(a => a.Id, f => f.Random.Int(1, 100_000))
        .RuleFor(a => a.Name, f => f.Internet.UserName())
        .RuleFor(a => a.SessionId, f => f.Random.UShort());

    private static readonly Faker _faker = new();

    public static GameAccount Account() => _gameAccount.Generate();

    public static GameAccount Account(int id)
    {
        var account = _gameAccount.Generate();
        account.Id = id;
        return account;
    }

    public static WorldInfo World(sbyte? id = null, bool isTestServer = false) =>
        new()
        {
            Id = id ?? _faker.Random.SByte(1, 100),
            WorldName = _faker.Random.String2(4, 16),
            Ip = _faker.Internet.Ip(),
            Port = _faker.Random.UShort(9000, 9999),
            IsTestServer = isTestServer,
        };
}
