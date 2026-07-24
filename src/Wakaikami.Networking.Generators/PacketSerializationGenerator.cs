using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wakaikami.Networking.Generators;

/// <summary>
/// Emits the <c>Write()</c> / <c>Read()</c> body for game packets marked with
/// <c>[GenerateSerialization]</c>, deriving the byte layout from <c>[Field]</c>-annotated members.
/// Direction is inferred from the base type: <c>FiestaServerPacket</c> gets <c>Write()</c> (from
/// primary-constructor parameters), <c>FiestaClientPacket</c> gets <c>Read()</c> (into settable properties).
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class PacketSerializationGenerator : IIncrementalGenerator
{
    private const string MarkerFqn = "Wakaikami.Networking.Protocol.Fiesta.GenerateSerializationAttribute";
    private const string FieldAttrName = "FieldAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Simple pipeline without any caching tricks - just like the original.
        var messages = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                MarkerFqn,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => Extract(ctx)
            )
            .Where(static m => m is not null);

        context.RegisterSourceOutput(messages, static (spc, message) => spc.AddSource(message!.HintName, Emit(message)));
    }

    private static MessageInfo? Extract(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol cls || cls.IsStatic || cls.IsAbstract)
            return null;

        // Direction from the base-type chain.
        bool isServer = false,
            isClient = false;
        const bool isRequest = false; // game packets carry no request-id prefix
        for (var b = cls.BaseType; b is not null; b = b.BaseType)
        {
            switch (b.Name)
            {
                case "FiestaServerPacket":
                    isServer = true;
                    break;
                case "FiestaClientPacket":
                    isClient = true;
                    break;
            }
        }

        if (isServer == isClient) // neither base recognised (or both, impossible) -> skip
            return null;

        var fields = new List<FieldInfo>();
        if (isServer)
        {
            // Server: [Field] on primary-constructor parameters (the values Write() serialises).
            foreach (var ctor in cls.Constructors)
            {
                foreach (var p in ctor.Parameters)
                {
                    if (TryReadField(p.GetAttributes(), out var order, out var length, out var wire))
                        fields.Add(Classify(p.Name, p.Type, order, length, wire));
                }
            }
        }
        else
        {
            // Client: [Field] on settable properties (the values Read() populates).
            foreach (var member in cls.GetMembers().OfType<IPropertySymbol>())
            {
                if (TryReadField(member.GetAttributes(), out var order, out var length, out var wire))
                    fields.Add(Classify(member.Name, member.Type, order, length, wire));
            }
        }

        // Server packets may also declare [Const(order, value)] magic/unknown bytes at class level.
        var consts = new List<ConstInfo>();
        if (isServer)
        {
            foreach (var a in cls.GetAttributes())
            {
                if (!string.Equals(a.AttributeClass?.Name, "ConstAttribute", System.StringComparison.Ordinal) || a.ConstructorArguments.Length < 2)
                    continue;
                if (a.ConstructorArguments[0].Value is not int order)
                    continue;
                var literal = RenderConstLiteral(a.ConstructorArguments[1]);
                if (literal is not null)
                    consts.Add(new ConstInfo(order, literal));
            }
        }

        if (fields.Count == 0 && consts.Count == 0 && !isRequest)
            return null;

        fields.Sort(static (a, b) => a.Order.CompareTo(b.Order));
        consts.Sort(static (a, b) => a.Order.CompareTo(b.Order));

        var ns = cls.ContainingNamespace.IsGlobalNamespace ? null : cls.ContainingNamespace.ToDisplayString();
        var hint = (ns is null ? "" : ns + ".") + cls.Name + ".Serialization.g.cs";

        return new MessageInfo(ns, cls.Name, hint, isServer, isRequest, fields.ToImmutableArray(), consts.ToImmutableArray());
    }

    private static bool TryReadField(ImmutableArray<AttributeData> attributes, out int order, out int length, out string? wireTypeFqn)
    {
        order = 0;
        length = 0;
        wireTypeFqn = null;
        foreach (var a in attributes)
        {
            if (!string.Equals(a.AttributeClass?.Name, FieldAttrName, System.StringComparison.Ordinal))
                continue;

            if (a.ConstructorArguments.Length >= 1 && a.ConstructorArguments[0].Value is int o)
                order = o;
            foreach (var named in a.NamedArguments)
            {
                if (string.Equals(named.Key, "Length", System.StringComparison.Ordinal) && named.Value.Value is int len)
                    length = len;
                else if (string.Equals(named.Key, "As", System.StringComparison.Ordinal) && named.Value.Value is INamedTypeSymbol wire)
                    wireTypeFqn = wire.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
            return true;
        }
        return false;
    }

    // Renders a [Const] value as a C# literal with an explicit wire-width cast so Write<T> picks the
    // right size. Returns null for non-primitive constants (e.g. an un-cast enum) which are skipped.
    private static string? RenderConstLiteral(TypedConstant tc)
    {
        if (tc.Value is null)
            return null;
        return tc.Type?.SpecialType switch
        {
            SpecialType.System_Boolean => (bool)tc.Value ? "true" : "false",
            SpecialType.System_Byte => "(byte)" + tc.Value,
            SpecialType.System_SByte => "(sbyte)" + tc.Value,
            SpecialType.System_Int16 => "(short)" + tc.Value,
            SpecialType.System_UInt16 => "(ushort)" + tc.Value,
            SpecialType.System_Int32 => "(int)" + tc.Value,
            SpecialType.System_UInt32 => "(uint)" + tc.Value,
            SpecialType.System_Int64 => "(long)" + tc.Value,
            SpecialType.System_UInt64 => "(ulong)" + tc.Value,
            _ => null,
        };
    }

    private static FieldInfo Classify(string name, ITypeSymbol type, int order, int length, string? wireTypeFqn)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            // Length > 0 -> fixed-width, NUL-padded; Length unset (0) -> byte-length-prefixed (variable).
            var kind = length > 0 ? FieldKind.String : FieldKind.VariableString;
            return new FieldInfo(order, name, "string", kind, length, null);
        }
        if (string.Equals(type.Name, "Guid", System.StringComparison.Ordinal) && string.Equals(type.ContainingNamespace?.ToDisplayString(), "System", System.StringComparison.Ordinal))
            return new FieldInfo(order, name, "global::System.Guid", FieldKind.Guid, 0, null);
        if (type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
            return new FieldInfo(order, name, "byte[]", FieldKind.Bytes, length, null);

        var typeFqn = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Enums and explicitly-narrowed integers travel as a smaller wire type than their C# type. Most
        // protocol enums default to int but are sent as ushort/byte, so cast through the wire type given
        // by [Field(As = typeof(...))]; fall back to an enum's declared underlying type when As is unset.
        var wire = wireTypeFqn;
        if (wire is null && type is INamedTypeSymbol { EnumUnderlyingType: { } underlying })
            wire = underlying.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new FieldInfo(order, name, typeFqn, FieldKind.Scalar, 0, string.Equals(wire, typeFqn, System.StringComparison.Ordinal) ? null : wire);
    }

    private static string Emit(MessageInfo m)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        if (m.Namespace is not null)
        {
            sb.Append("namespace ").Append(m.Namespace).AppendLine(";");
            sb.AppendLine();
        }

        sb.Append("partial class ").AppendLine(m.ClassName);
        sb.AppendLine("{");

        if (m.IsServer)
            EmitWrite(sb, m);
        else
            EmitRead(sb, m);

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void EmitWrite(StringBuilder sb, MessageInfo m)
    {
        sb.AppendLine("    public override void Write()");
        sb.AppendLine("    {");
        if (m.IsRequest)
            sb.AppendLine("        base.Write();");

        // Fields and [Const] writes share one Order space; emit them interleaved by ascending order.
        var fi = 0;
        var ci = 0;
        while (fi < m.Fields.Length || ci < m.Consts.Length)
        {
            var takeConst = fi >= m.Fields.Length || (ci < m.Consts.Length && m.Consts[ci].Order < m.Fields[fi].Order);
            if (takeConst)
            {
                sb.Append("        Writer.Write(").Append(m.Consts[ci].Literal).AppendLine(");");
                ci++;
                continue;
            }
            EmitWriteField(sb, m.Fields[fi]);
            fi++;
        }
        sb.AppendLine("    }");
    }

    private static void EmitWriteField(StringBuilder sb, FieldInfo f)
    {
        switch (f.Kind)
        {
            case FieldKind.String:
                sb.Append("        Writer.WriteString(").Append(f.Name).Append(", ").Append(f.Length).AppendLine(");");
                break;
            case FieldKind.VariableString:
                sb.Append("        Writer.Write((byte)").Append(f.Name).AppendLine(".Length);");
                sb.Append("        Writer.WriteString(").Append(f.Name).Append(", ").Append(f.Name).AppendLine(".Length);");
                break;
            case FieldKind.Guid:
                sb.Append("        Writer.WriteGuid(").Append(f.Name).AppendLine(");");
                break;
            case FieldKind.Bytes:
                sb.Append("        Writer.Write(").Append(f.Name).AppendLine(");");
                break;
            default:
                if (f.WireTypeFqn is not null)
                    sb.Append("        Writer.Write((").Append(f.WireTypeFqn).Append(")(").Append(f.Name).AppendLine("));");
                else
                    sb.Append("        Writer.Write(").Append(f.Name).AppendLine(");");
                break;
        }
    }

    private static void EmitRead(StringBuilder sb, MessageInfo m)
    {
        sb.AppendLine("    public override bool Read()");
        sb.AppendLine("    {");

        // Per-field checks: each Read*() call gets its own guard so the parser
        // can log exactly which field failed and at what byte offset.

        if (m.IsRequest)
        {
            sb.AppendLine("        if (!base.Read())");
            sb.AppendLine("        {");
            sb.AppendLine("            Reader.LastFailedField = \"<request-id>\";");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
            if (m.Fields.Length > 0)
                sb.AppendLine();
        }

        var temps = new List<(string Field, string ReadExpr, string AssignValue)>();
        var index = 0;
        foreach (var f in m.Fields)
        {
            var temp = "__f" + index++;
            string readExpr;
            string assign;
            switch (f.Kind)
            {
                case FieldKind.String:
                    readExpr = $"Reader.ReadString(out string {temp}, {f.Length})";
                    assign = temp;
                    break;
                case FieldKind.VariableString:
                    readExpr = $"Reader.ReadString(out string {temp})";
                    assign = temp;
                    break;
                case FieldKind.Guid:
                    readExpr = $"Reader.ReadGuid(out global::System.Guid {temp})";
                    assign = temp;
                    break;
                case FieldKind.Bytes:
                    readExpr = $"Reader.ReadBytes({f.Length}, out byte[]? {temp})";
                    assign = $"{temp}!";
                    break;
                default: // Scalar
                    if (f.WireTypeFqn is not null)
                    {
                        readExpr = $"Reader.Read(out {f.WireTypeFqn} {temp})";
                        assign = $"({f.TypeFqn}){temp}";
                    }
                    else
                    {
                        readExpr = $"Reader.Read(out {f.TypeFqn} {temp})";
                        assign = temp;
                    }
                    break;
            }
            temps.Add((f.Name, readExpr, assign));
        }

        foreach (var (field, readExpr, _) in temps)
        {
            sb.AppendLine($"        if (!{readExpr})");
            sb.AppendLine("        {");
            sb.AppendLine($"            Reader.LastFailedField = \"{field}\";");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
        }

        if (temps.Count > 0)
        {
            sb.AppendLine();
            foreach (var (field, _, assignValue) in temps)
                sb.AppendLine($"        {field} = {assignValue};");
        }

        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
    }

    private enum FieldKind
    {
        Scalar,
        String,
        VariableString,
        Guid,
        Bytes,
    }

    // WireTypeFqn: the unmanaged type actually written/read when it differs from the member type (enum
    // narrowing or explicit [Field(As=...)]); null means write/read the member type directly.
    private sealed record FieldInfo(int Order, string Name, string TypeFqn, FieldKind Kind, int Length, string? WireTypeFqn);

    private sealed record ConstInfo(int Order, string Literal);

    private sealed record MessageInfo(
        string? Namespace,
        string ClassName,
        string HintName,
        bool IsServer,
        bool IsRequest,
        ImmutableArray<FieldInfo> Fields,
        ImmutableArray<ConstInfo> Consts
    );
}
