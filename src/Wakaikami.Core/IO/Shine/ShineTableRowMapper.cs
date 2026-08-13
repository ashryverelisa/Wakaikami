using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Wakaikami.Core.IO.Shine.Extensions;

namespace Wakaikami.Core.IO.Shine;

public static class ShineTableRowMapper
{
    public static IEnumerable<T> MapTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this ShineTable table)
        where T : new() => table.EnumerateRows().Select(MapTo<T>);

    public static T MapTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this ShineTableRow row)
        where T : new()
    {
        var instance = new T();
        foreach (var (propertyType, column, setter) in MapperCache<T>.Mappings)
            setter(instance, row.Get(propertyType, column));
        return instance;
    }

    public static T MapVerticalTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
        this ShineTable table,
        string keyColumn = "key",
        string valueColumn = "argument"
    )
        where T : new()
    {
        var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in table.EnumerateRows())
            values[row.Get<string>(keyColumn)] = row.Get<object>(valueColumn);

        var instance = new T();
        foreach (var (propertyType, column, setter) in MapperCache<T>.Mappings)
        {
            if (values.TryGetValue(column, out var value))
                setter(instance, Convert.ChangeType(value, propertyType, System.Globalization.CultureInfo.InvariantCulture));
        }

        return instance;
    }

    private static class MapperCache<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
        where T : new()
    {
        public static readonly (Type PropertyType, string Column, Action<T, object?> Setter)[] Mappings = Build();

        private static (Type, string, Action<T, object?>)[] Build()
        {
            var mappings = new List<(Type, string, Action<T, object?>)>();

            foreach (var prop in typeof(T).GetProperties())
            {
                var attr = prop.GetCustomAttribute<ShineTableColumnAttribute>();
                if (attr is null || prop.SetMethod is null)
                    continue;

                mappings.Add((prop.PropertyType, attr.Name, BuildSetter(prop)));
            }

            return mappings.ToArray();
        }

        private static Action<T, object?> BuildSetter(PropertyInfo prop)
        {
            var instance = Expression.Parameter(typeof(T), "instance");
            var value = Expression.Parameter(typeof(object), "value");

            var assign = Expression.Assign(Expression.Property(instance, prop), Expression.Convert(value, prop.PropertyType));

            return Expression.Lambda<Action<T, object?>>(assign, instance, value).Compile();
        }
    }
}
