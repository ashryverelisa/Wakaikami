namespace Wakaikami.Database.Login.Entities;

public class Account
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public DateTime CreateAt { get; set; }
    public required string CreationIp { get; set; }
    public bool IsActivated { get; set; }
    public string? LastLoginIp { get; set; }
    public AccountBan? AccountBan { get; set; }
}
