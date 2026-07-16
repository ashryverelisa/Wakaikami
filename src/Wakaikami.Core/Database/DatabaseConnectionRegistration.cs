using Wakaikami.Core.Database.Enums;

namespace Wakaikami.Core.Database;

public sealed record DatabaseConnectionRegistration(DatabaseType Type, string ConnectionString);
