using System.Reflection;
using System.Runtime.CompilerServices;

namespace Wakaikami.LoginService.Tests.TestSupport;

internal static class Reflection
{
    public static T Uninitialized<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    public static T SetBackingField<T>(this T target, string propertyName, object? value)
        where T : notnull
    {
        var fieldName = $"<{propertyName}>k__BackingField";
        for (var type = target.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field is not null)
            {
                field.SetValue(target, value);
                return target;
            }
        }

        throw new MissingFieldException(target.GetType().FullName, fieldName);
    }
}
