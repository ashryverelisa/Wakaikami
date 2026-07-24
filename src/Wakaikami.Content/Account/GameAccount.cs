namespace Wakaikami.Content.Account;

public class GameAccount
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ushort SessionId { get; set; }
}
