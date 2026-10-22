using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Wakaikami.Core.IO.Shn;

public static class ShnRowMapper
{
    public static IEnumerable<T> MapTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this ShnTable table)
        where T : new() => table.EnumerateRows().Select(MapTo<T>);

    public static T MapTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this ShnRow row)
        where T : new()
    {
        var instance = new T();
        foreach (var (propertyType, column, setter) in MapperCache<T>.Mappings)
            setter(instance, row.Get(propertyType, column));
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
                var attr = prop.GetCustomAttribute<ShnColumnAttribute>();
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
