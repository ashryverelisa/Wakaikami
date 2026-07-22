namespace Wakaikami.Database.Login.Entities;

public class AccountBan
{
    public int Id { get; set; }
    public int? AccountId { get; set; }
    public string Ip { get; set; } = null!;
    public DateTime EndOfBan { get; set; }
    public DateTime CreateDate { get; set; }
    public string Reason { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
