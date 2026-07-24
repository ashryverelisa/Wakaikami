namespace Wakaikami.Database.Login.Repositories.Models;

public sealed record AccountLoginRow(int Id, string PasswordHash, bool IsActivated, bool IsBanned);
