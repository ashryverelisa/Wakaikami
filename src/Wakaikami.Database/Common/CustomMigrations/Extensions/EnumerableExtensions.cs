namespace Wakaikami.Database.Common.CustomMigrations.Extensions;

public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T?> enumerable)
        where T : class
    {
        public IEnumerable<T> WhereNotNull() => enumerable.Where(x => x != null).Cast<T>();
    }
}
