using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wakaikami.Networking.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class PacketHandlerGenerator : IIncrementalGenerator
{
    // Fully-qualified metadata name for ForAttributeWithMetadataName
    private const string GameClassMarker = "Wakaikami.Networking.HandlerStores.Attributes.GameHandlerClassAttribute";
    // Simple name for attr.AttributeClass?.Name comparison in ExtractFromClass
    private const string GameMethodAttr = "GameHandlerAttribute";
    // Fully-qualified type references for generated code (moved from global namespace by linter)
    private const string FiestaHandlerTypeFqn = "global::Wakaikami.Networking.HandlerTypes.FiestaHandlerType";
    private const string FiestaClientPacketFqn = "global::Wakaikami.Networking.Protocol.Fiesta.FiestaClientPacket";
    private const string FiestaSessionFqn = "global::Wakaikami.Networking.Session.FiestaSession";
    private const string GamePacketHandlerFqn = "global::Wakaikami.Networking.HandlerStores.Interfaces.IGamePacketHandler";

    // FullyQualifiedFormat lässt den Nullability-Modifier (?) weg; ohne ihn verlieren die gespiegelten
    // ISh##-Interfaces die Nullable-Annotationen der Sh##-Implementierungen (CS8604 an den Aufrufstellen).
    private static readonly SymbolDisplayFormat FullyQualifiedNullableFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var gameBindings = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                GameClassMarker,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => ExtractFromClass(ctx)
            )
            .Where(static x => x is { Count: > 0 })
            .SelectMany(static (x, _) => x!);

        // Sh* handlers carry no attribute (they are not packet-bound via the marker attributes), so
        // they are discovered by naming convention at compile time and registered as concrete
        // singletons. This replaces the runtime Assembly.GetTypes() scan in the service hosts, which
        // is not trim/AOT-safe (IL2026/IL2072).
        //
        // In addition, an ISh##... interface is generated per handler (mirroring its public methods)
        // and the concrete class is made to implement it via a generated partial declaration. Inbound
        // (Ch) and game handlers can then depend on the interface instead of the concrete class, which
        // decouples the two directions and keeps callers unit-testable. The concrete registration is
        // kept too, so existing concrete injections keep working during incremental migration.
        var shHandlers = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) =>
                    node is ClassDeclarationSyntax cds
                    && cds.Identifier.ValueText.StartsWith("SH", System.StringComparison.OrdinalIgnoreCase)
                    && cds.Identifier.ValueText.EndsWith("Handler", System.StringComparison.Ordinal),
                transform: static (ctx, ct) => ExtractShHandler(ctx, ct)
            )
            .Where(static x => x is not null)
            .Select(static (x, _) => x!);

        var combined = gameBindings
            .Collect()
            .Combine(shHandlers.Collect())
            .Combine(context.CompilationProvider.Select(static (c, _) => c.AssemblyName ?? "Unknown"));

        context.RegisterSourceOutput(
            combined,
            static (spc, data) =>
            {
                var ((games, shHandlers), assemblyName) = data;

                if (games.IsDefaultOrEmpty && shHandlers.IsDefaultOrEmpty)
                    return;

                var src = Emit(games, shHandlers, assemblyName);
                spc.AddSource("GeneratedPacketHandlerRegistration.g.cs", src);

                if (!shHandlers.IsDefaultOrEmpty)
                {
                    var ifaceSrc = EmitShInterfaces(shHandlers);
                    spc.AddSource("GeneratedShHandlerInterfaces.g.cs", ifaceSrc);
                }
            }
        );
    }

    private static ShHandlerInfo? ExtractShHandler(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, cancellationToken) is not INamedTypeSymbol cls)
            return null;
        if (cls.TypeKind != TypeKind.Class || cls.IsAbstract || cls.IsStatic || cls.IsGenericType)
            return null;

        // Only the game-side Sh##GameHandler classes are mirrored into ISh## interfaces. The internal
        // (cluster) handlers are hand-written gRPC seams, so there are no Sh## classes under the cluster
        // handler tree to discover here.
        var ns = cls.ContainingNamespace?.ToDisplayString();
        if (ns is null || !ns.Contains(".GameNetwork.Handlers"))
            return null;

        // Mirror the public instance methods into an interface body. Render to a string here (in the
        // syntax transform) so the cached model stays value-equatable for the incremental pipeline.
        var members = new StringBuilder();
        foreach (var m in cls.GetMembers().OfType<IMethodSymbol>())
        {
            if (m.MethodKind != MethodKind.Ordinary || m.IsStatic || m.IsOverride)
                continue;
            if (m.DeclaredAccessibility != Accessibility.Public)
                continue;

            var returnType = m.ReturnType.ToDisplayString(FullyQualifiedNullableFormat);
            var parameters = string.Join(", ", m.Parameters.Select(RenderParameter));
            members.Append("        ").Append(returnType).Append(' ').Append(m.Name).Append('(').Append(parameters).AppendLine(");");
        }

        return new ShHandlerInfo(
            ClassFqn: cls.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Namespace: ns,
            ClassName: cls.Name,
            // Normalize the interface name to a stable "ISh##…" casing regardless of whether the
            // concrete class is named SH##… or Sh##… (an analyzer/linter may re-case the acronym).
            InterfaceName: "ISh" + cls.Name.Substring(2),
            MembersSource: members.ToString()
        );
    }

    private static string RenderParameter(IParameterSymbol p)
    {
        var sb = new StringBuilder();
        switch (p.RefKind)
        {
            case RefKind.Ref:
                sb.Append("ref ");
                break;
            case RefKind.Out:
                sb.Append("out ");
                break;
            case RefKind.In:
                sb.Append("in ");
                break;
        }
        if (p.IsParams)
            sb.Append("params ");

        sb.Append(p.Type.ToDisplayString(FullyQualifiedNullableFormat)).Append(' ').Append(p.Name);

        if (p.HasExplicitDefaultValue)
            sb.Append(" = ").Append(RenderConstant(p.ExplicitDefaultValue, p.Type));

        return sb.ToString();
    }

    private static string RenderConstant(object? value, ITypeSymbol type)
    {
        if (value is null)
            return "default";
        if (type is INamedTypeSymbol e && e.TypeKind == TypeKind.Enum)
            return $"({e.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({value})";

        return value switch
        {
            bool b => b ? "true" : "false",
            string s => SymbolDisplay.FormatLiteral(s, true),
            char c => SymbolDisplay.FormatLiteral(c, true),
            _ => System.Convert.ToString(value, CultureInfo.InvariantCulture) ?? "default",
        };
    }

    private static List<Binding>? ExtractFromClass(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol cls)
            return null;
        if (cls.IsStatic || cls.IsAbstract)
            return null;

        var marker = ctx.Attributes.FirstOrDefault();
        if (marker is null || marker.ConstructorArguments.Length < 1)
            return null;
        var handlerTypeExpr = RenderEnum(marker.ConstructorArguments[0], FiestaHandlerTypeFqn);

        var classFqn = cls.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var bindings = new List<Binding>();
        foreach (var member in cls.GetMembers().OfType<IMethodSymbol>())
        {
            if (member.MethodKind != MethodKind.Ordinary)
                continue;
            if (member.IsStatic)
                continue;

            foreach (var attr in member.GetAttributes())
            {
                var aname = attr.AttributeClass?.Name;
                if (attr.ConstructorArguments.Length < 2)
                    continue;

                if (!string.Equals(aname, GameMethodAttr, System.StringComparison.Ordinal))
                    continue;

                // Regular handler - needs at least 2 parameters
                if (member.Parameters.Length < 2)
                    continue;

                var sessionFqn = member.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var packetParamFqn = member.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                var opcodeArg = attr.ConstructorArguments[0];
                var packetTypeArg = attr.ConstructorArguments[1];
                if (packetTypeArg.Value is not INamedTypeSymbol packetClass)
                    continue;
                var packetClassFqn = packetClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                var opcodeExpr = RenderCastable(opcodeArg, "ushort");

                var returnKind = "sync";
                if (!member.ReturnsVoid)
                {
                    var rtName = member.ReturnType.Name;
                    if (string.Equals(rtName, "Task", System.StringComparison.Ordinal))
                        returnKind = "task";
                    else if (string.Equals(rtName, "ValueTask", System.StringComparison.Ordinal))
                        returnKind = "valuetask";
                }

                bindings.Add(
                    new Binding(
                        HandlerClassFqn: classFqn,
                        MethodName: member.Name,
                        SessionTypeFqn: sessionFqn,
                        PacketParamTypeFqn: packetParamFqn,
                        PacketClassTypeFqn: packetClassFqn,
                        HandlerTypeExpr: handlerTypeExpr,
                        OpcodeExpr: opcodeExpr,
                        ReturnKind: returnKind
                    )
                );
            }
        }

        return bindings;
    }

    private static string RenderEnum(TypedConstant tc, string castTo)
    {
        if (tc.Value is null)
            return $"default({castTo})";
        if (tc.Type is INamedTypeSymbol e && e.TypeKind == TypeKind.Enum)
        {
            var enumFqn = e.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            foreach (var f in e.GetMembers().OfType<IFieldSymbol>())
            {
                if (f.HasConstantValue && Equals(f.ConstantValue, tc.Value))
                    return $"{enumFqn}.{f.Name}";
            }
            return $"({castTo})({tc.Value})";
        }
        return tc.Value!.ToString()!;
    }

    private static string RenderCastable(TypedConstant tc, string castTo)
    {
        if (tc.Value is null)
            return $"default({castTo})";
        if (tc.Type is INamedTypeSymbol e && e.TypeKind == TypeKind.Enum)
        {
            var enumFqn = e.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            foreach (var f in e.GetMembers().OfType<IFieldSymbol>())
            {
                if (f.HasConstantValue && Equals(f.ConstantValue, tc.Value))
                    return $"({castTo}){enumFqn}.{f.Name}";
            }
        }
        return $"({castTo})({tc.Value})";
    }

    // Emits one file with, per Sh handler: an ISh##... interface mirroring its public methods plus a
    // partial declaration that makes the concrete class implement that interface. Grouped by namespace
    // so the generated types land next to their handler (existing 'using's resolve them).
    private static string EmitShInterfaces(ImmutableArray<ShHandlerInfo> shHandlers)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        foreach (var group in shHandlers.GroupBy(h => h.Namespace, System.StringComparer.Ordinal).OrderBy(g => g.Key, System.StringComparer.Ordinal))
        {
            sb.Append("namespace ").AppendLine(group.Key);
            sb.AppendLine("{");

            foreach (var h in group.GroupBy(x => x.ClassFqn, System.StringComparer.Ordinal).Select(x => x.First()).OrderBy(x => x.ClassName, System.StringComparer.Ordinal))
            {
                sb.Append("    public interface ").AppendLine(h.InterfaceName);
                sb.AppendLine("    {");
                sb.Append(h.MembersSource);
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.Append("    partial class ").Append(h.ClassName).Append(" : ").Append(h.InterfaceName).AppendLine(" { }");
                sb.AppendLine();
            }

            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string Emit(ImmutableArray<Binding> games, ImmutableArray<ShHandlerInfo> shHandlers, string assemblyName)
    {
        var distinctHandlerClasses = games.Select(m => m.HandlerClassFqn).Distinct(System.StringComparer.Ordinal).OrderBy(s => s, System.StringComparer.Ordinal).ToArray();

        var ns = assemblyName.Replace('-', '_') + ".Hosting";
        var simpleName = assemblyName.Split('.').Last();
        var methodName = "Add" + simpleName + "PacketHandlers";

        var sb = new StringBuilder();
        var index = 0;

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.Append("namespace ").Append(ns).AppendLine(";");
        sb.AppendLine();

        // Emit file-scoped wrapper classes at namespace level first.
        foreach (var g in games)
        {
            var className = $"Handler_{index++}";

            sb.Append("file sealed class ").Append(className).Append(" : ").AppendLine(GamePacketHandlerFqn);
            sb.AppendLine("{");
            sb.Append("    public ").Append(FiestaHandlerTypeFqn).Append(" HandlerType => ").Append(g.HandlerTypeExpr).AppendLine(";");
            sb.Append("    public ushort Opcode => ").Append(g.OpcodeExpr).AppendLine(";");
            sb.Append("    public ").Append(FiestaClientPacketFqn).Append(" CreatePacket(byte[] data) => new ").Append(g.PacketClassTypeFqn).AppendLine("(data);");
            sb.AppendLine();
            sb.Append("    public global::System.Threading.Tasks.Task Handle(global::System.IServiceProvider services, ")
                .Append(FiestaSessionFqn)
                .Append(" session, ")
                .Append(FiestaClientPacketFqn)
                .AppendLine(" packet)");
            sb.AppendLine("    {");
            EmitHandleBody(sb, g);
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        sb.AppendLine("public static class GeneratedPacketHandlerRegistration");
        sb.AppendLine("{");
        sb.Append("    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection ")
            .Append(methodName)
            .AppendLine("(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var t in distinctHandlerClasses)
        {
            sb.Append("        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<")
                .Append(t)
                .AppendLine(">(services);");
        }

        // Concrete self-registration for the convention-discovered Sh* handlers (resolved by
        // concrete type elsewhere). Skip any already registered above via a marker attribute.
        var shClassFqns = shHandlers.Select(h => h.ClassFqn).Distinct(System.StringComparer.Ordinal).Where(s => !distinctHandlerClasses.Contains(s, System.StringComparer.Ordinal)).OrderBy(s => s, System.StringComparer.Ordinal).ToArray();
        foreach (var t in shClassFqns)
        {
            sb.Append("        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<")
                .Append(t)
                .AppendLine(">(services);");
        }

        // Interface registration for Sh* handlers: ISh## -> concrete singleton. Lets inbound (Ch) and
        // game handlers depend on the abstraction instead of the concrete class.
        foreach (var h in shHandlers.GroupBy(x => x.ClassFqn, System.StringComparer.Ordinal).Select(x => x.First()).OrderBy(x => x.ClassFqn, System.StringComparer.Ordinal))
        {
            var interfaceFqn = "global::" + h.Namespace + "." + h.InterfaceName;
            sb.Append("        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<")
                .Append(interfaceFqn)
                .Append(">(services, static sp => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<")
                .Append(h.ClassFqn)
                .AppendLine(">(sp));");
        }

        // Reset index to re-emit registrations referencing the file classes.
        index = 0;
        foreach (var g in games)
        {
            var className = $"Handler_{index++}";
            sb.Append("        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<")
                .Append(GamePacketHandlerFqn)
                .Append(">(services, new ")
                .Append(className)
                .AppendLine("());");
        }

        sb.AppendLine();
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // Emits the body of a generated IGamePacketHandler.Handle method, which
    // returns Task. The dispatch shape depends on the user handler method's return type so that
    // void handlers stay synchronous (no state machine) and Task/ValueTask handlers are returned
    // directly without an extra await.
    private static void EmitHandleBody(StringBuilder sb, Binding b)
    {
        var call =
            $"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{b.HandlerClassFqn}>(services)"
            + $".{b.MethodName}(({b.SessionTypeFqn})session, ({b.PacketParamTypeFqn})packet)";

        switch (b.ReturnKind)
        {
            case "task":
                sb.Append("        return ").Append(call).AppendLine(";");
                break;
            case "valuetask":
                sb.Append("        return (").Append(call).AppendLine(").AsTask();");
                break;
            default: // "sync" - void (or non-awaitable) handler
                sb.Append("        ").Append(call).AppendLine(";");
                sb.AppendLine("        return global::System.Threading.Tasks.Task.CompletedTask;");
                break;
        }
    }

    private sealed record Binding(
        string HandlerClassFqn,
        string MethodName,
        string SessionTypeFqn,
        string PacketParamTypeFqn,
        string PacketClassTypeFqn,
        string HandlerTypeExpr,
        string OpcodeExpr,
        string ReturnKind
    );

    // Value-equatable (all string fields) so the incremental pipeline caches correctly.
    private sealed record ShHandlerInfo(string ClassFqn, string Namespace, string ClassName, string InterfaceName, string MembersSource);
}
