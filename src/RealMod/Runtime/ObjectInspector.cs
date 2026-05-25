using System.Collections.Generic;
using System.Linq;
using Mafi;
using Mafi.Collections;

namespace CoiTelemetry.RealMod.Runtime;
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

public static class ObjectInspector
{
    public static string Inspect(object? obj, int maxDepth = 3)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer<object>.Instance);
        var lines = InspectValue(obj, obj?.GetType() ?? typeof(object), "", 0, maxDepth, visited);
        return string.Join("\n", lines);
    }

    private static string[] InspectValue(
        object? obj,
        Type declaredType,
        string indent,
        int depth,
        int maxDepth,
        HashSet<object> visited)
    {
        if (obj == null)
        {
            return new[] { $"{indent}<null> : {FormatType(declaredType)}" };
        }
        List<string> lines = new();
        
        var type = obj.GetType();

        lines.Add($"{indent}{FormatType(type)}");

        if (IsSimple(type))
        {
            lines.Add($"{indent}  Value = {obj}");
            return lines.ToArray();
        }

        if (!type.IsValueType)
        {
            if (!visited.Add(obj))
            {
                lines.Add($"{indent}  <already visited>");
                return lines.ToArray();
            }
        }

        if (depth >= maxDepth)
        {
            lines.Add($"{indent}  <max depth reached>");
            return lines.ToArray();
        }

        var flags =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.FlattenHierarchy;

        lines.Add($"{indent}  Fields:");
        foreach (var field in type.GetFields(flags))
        {
            object? value = SafeGet(() => field.GetValue(field.IsStatic ? null : obj));

            lines.Add(
                $"{indent}    {Visibility(field)} {(field.IsStatic ? "static " : "")}{FormatType(field.FieldType)} {field.Name} = {FormatValue(value)}");

            if (value != null && !IsSimple(value.GetType()))
            {
                lines.AddRange(InspectValue(value, field.FieldType, indent + "      ", depth + 1, maxDepth, visited));
            }
        }

        lines.Add($"{indent}  Properties:");
        foreach (var prop in type.GetProperties(flags))
        {
            var getter = prop.GetGetMethod(nonPublic: true);
            var setter = prop.GetSetMethod(nonPublic: true);

            object? value = "<not readable>";

            if (getter != null && prop.GetIndexParameters().Length == 0)
            {
                value = SafeGet(() => prop.GetValue(getter.IsStatic ? null : obj));
            }
            else if (prop.GetIndexParameters().Length > 0)
            {
                value = "<indexer>";
            }

            lines.Add(
                $"{indent}    {Visibility(prop)} {(getter?.IsStatic == true ? "static " : "")}{FormatType(prop.PropertyType)} {prop.Name} {{ {(getter != null ? "get; " : "")}{(setter != null ? "set; " : "")}}} = {FormatValue(value)}");

            if (value != null && value is not string && !IsSimple(value.GetType()))
            {
                lines.AddRange(InspectValue(value, prop.PropertyType, indent + "      ", depth + 1, maxDepth, visited));
            }
        }

        lines.Add($"{indent}  Events:");
        foreach (var ev in type.GetEvents(flags))
        {
            lines.Add($"{indent}    {FormatType(ev.EventHandlerType!)} {ev.Name}");
        }

        lines.Add($"{indent}  Constructors:");
        foreach (var ctor in type.GetConstructors(flags))
        {
            lines.Add($"{indent}    {Visibility(ctor)} {type.Name}({FormatParams(ctor.GetParameters())})");
        }

        lines.Add($"{indent}  Methods:");
        foreach (var method in type.GetMethods(flags))
        {
            if (method.IsSpecialName)
                continue;

            lines.Add(
                $"{indent}    {Visibility(method)} {(method.IsStatic ? "static " : "")}{FormatType(method.ReturnType)} {method.Name}({FormatParams(method.GetParameters())})");
        }

        lines.Add($"{indent}  Nested Types:");
        foreach (var nested in type.GetNestedTypes(flags))
        {
            lines.Add($"{indent}    {FormatType(nested)}");
        }
        return lines.ToArray();
    }

    private static object? SafeGet(Func<object?> getter)
    {
        try
        {
            return getter();
        }
        catch (Exception ex)
        {
            return $"<threw {ex.GetType().Name}: {ex.Message}>";
        }
    }

    private static bool IsSimple(Type type)
    {
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid);
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
            return "null";

        if (value is string s)
            return $"\"{s}\"";

        if (value is IEnumerable enumerable && value is not string)
        {
            var type = value.GetType();
            return $"<{FormatType(type)}>";
        }

        return value.ToStringSafe() ?? "<no ToString()>";
    }

    private static string FormatType(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        var name = type.Name;
        var tickIndex = name.IndexOf('`');

        if (tickIndex >= 0)
            name = name.Substring(0, tickIndex);

        var args = type.GetGenericArguments()
            .Select(FormatType);

        return $"{name}<{string.Join(", ", args)}>";
    }

    private static string FormatParams(ParameterInfo[] parameters)
    {
        return string.Join(", ", parameters.Select(p => $"{FormatType(p.ParameterType)} {p.Name}"));
    }

    private static string Visibility(FieldInfo field)
    {
        if (field.IsPublic) return "public";
        if (field.IsPrivate) return "private";
        if (field.IsFamily) return "protected";
        if (field.IsAssembly) return "internal";
        if (field.IsFamilyOrAssembly) return "protected internal";
        return "private protected";
    }

    private static string Visibility(MethodBase method)
    {
        if (method.IsPublic) return "public";
        if (method.IsPrivate) return "private";
        if (method.IsFamily) return "protected";
        if (method.IsAssembly) return "internal";
        if (method.IsFamilyOrAssembly) return "protected internal";
        return "private protected";
    }

    private static string Visibility(PropertyInfo prop)
    {
        var accessor =
            prop.GetGetMethod(nonPublic: true)
            ?? prop.GetSetMethod(nonPublic: true);

        return accessor == null ? "unknown" : Visibility(accessor);
    }
}

