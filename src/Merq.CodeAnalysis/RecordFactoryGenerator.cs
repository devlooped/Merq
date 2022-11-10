using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace Merq;

[Generator(LanguageNames.CSharp)]
public class RecordFactoryGenerator : IIncrementalGenerator
{
    static readonly SymbolDisplayFormat fullNameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var types = context.CompilationProvider.SelectMany((x, c) =>
            TypesVisitor.Visit(x.GlobalNamespace, symbol =>
                x.IsSymbolAccessibleWithin(symbol, x.Assembly) &&
                symbol.IsRecord &&
                symbol.ContainingNamespace != null, c));

        context.RegisterSourceOutput(
            types.Combine(context.CompilationProvider),
            (ctx, data) =>
        {
            var ctor = data.Left.InstanceConstructors
                .Where(x => x.DeclaredAccessibility == Accessibility.Public || x.DeclaredAccessibility == Accessibility.Internal)
                .OrderByDescending(x => x.Parameters.Length).FirstOrDefault();
            if (ctor == null)
                return;

            using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Merq.RecordFactory.sbntxt");
            using var reader = new StreamReader(resource!);
            var template = Template.Parse(reader.ReadToEnd());
            var compilation = data.Right;
            var listType = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")!;

            string? GetConvert(ITypeSymbol type)
            {
                if (type.SpecialType != SpecialType.None ||
                    type is not IArrayTypeSymbol arrayType ||
                    arrayType.Rank != 1)
                    return null;

                return ".ToArray()";
            }

            string? GetFactory(ITypeSymbol type)
            {
                if (type.SpecialType != SpecialType.None)
                    return null;

                ITypeSymbol? elementType = default;
                if (type is IArrayTypeSymbol arrayType)
                    elementType = arrayType.ElementType;
                else if (listType.AllInterfaces.Any(iface => type.Is(iface)) &&
                    type is INamedTypeSymbol named &&
                    named.IsGenericType && named.TypeParameters.Length == 1)
                {
                    elementType = named.TypeArguments[0];
                }

                var factoryName = elementType != null ? "CreateMany" : "Create";
                if (elementType != null)
                    type = elementType;

                var prefix = type.ContainingNamespace.Equals(data.Left.ContainingNamespace, SymbolEqualityComparer.Default) ?
                    "" : $"{type.ContainingNamespace.ToDisplayString(fullNameFormat)}.";

                if (FindCreate(type, factoryName) is IMethodSymbol create &&
                    compilation.IsSymbolAccessibleWithin(create, compilation.Assembly))
                {
                    // We either had a custom Create factory method, or the type is partial, 
                    // and we'll generate it ourselves.
                    return prefix + type.Name + "." + factoryName;
                }
                else if (!HasCreate(type, factoryName) && IsPartial(type) && type.IsRecord)
                {
                    // We'll generate a Create factory method.
                    return prefix + type.Name + "." + factoryName;
                }
                else if (type.IsRecord)
                {
                    // If the type isn't partial or has a Create method, we will 
                    // generate a factory class for it.
                    return prefix + $"__{type.Name}Factory.{factoryName}";
                }

                return null;
            };

            // Get properties that can be set and are not named (case insensitive) as ctor parameters
            var properties = data.Left.GetMembers().OfType<IPropertySymbol>()
                .Where(x => x.SetMethod != null && !ctor.Parameters.Any(y => string.Equals(y.Name, x.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(x => new
                {
                    x.Name,
                    Convert = GetConvert(x.Type),
                    Factory = GetFactory(x.Type),
                })
                .OrderBy(x => x.Name)
                .ToImmutableArray();

            var output = template.Render(new
            {
                Namespace = data.Left.ContainingNamespace.ToDisplayString(fullNameFormat),
                Name = data.Left.Name,
                Factory = FindCreate(data.Left) is IMethodSymbol create &&
                    compilation.IsSymbolAccessibleWithin(create, compilation.Assembly) ? data.Left.Name + ".Create" : null,
                Parameters = ctor.Parameters.Select(x => new
                {
                    x.Name,
                    Convert = GetConvert(x.Type),
                    Factory = GetFactory(x.Type),
                }).ToArray(),
                HasProperties = !properties.IsDefaultOrEmpty,
                Properties = properties,

            }, member => member.Name);

            ctx.AddSource(data.Left.Name + ".Factory.g", output.Replace("\r\n", "\n").Replace("\n", Environment.NewLine));

            if (FindCreate(data.Left) is IMethodSymbol factory &&
                !compilation.IsSymbolAccessibleWithin(factory, compilation.Assembly))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CreateMethodNotAccessible,
                    factory.Locations.FirstOrDefault(),
                    data.Left.Name, "Create"));
            }

            if (FindCreate(data.Left, "CreateMany") is IMethodSymbol factoryMany &&
                !compilation.IsSymbolAccessibleWithin(factoryMany, compilation.Assembly))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CreateMethodNotAccessible,
                    factoryMany.Locations.FirstOrDefault(),
                    data.Left.Name, "CreateMany"));
            }
        });

        context.RegisterSourceOutput(
            // Only generate a partial factory method for partial records
            // Don't generate duplicate method names. We also don't generate if there's already a Create with 
            // a single parameter.
            types.Where(x => IsPartial(x) && !HasCreate(x)),
            (ctx, data) =>
            {
                ctx.AddSource(data.Name + ".Create.g",
                    $$"""
                    // <auto-generated />
                    namespace {{data.ContainingNamespace.ToDisplayString(fullNameFormat)}}
                    {
                        partial record {{data.Name}}
                        {
                            public static {{data.Name}} Create(dynamic value)
                                => __{{data.Name}}Factory.Create(value);
                        }
                    }
                    """.Replace("\r\n", "\n").Replace("\n", Environment.NewLine));
            });

        context.RegisterSourceOutput(
            // Only generate a partial factory method for partial records
            // Don't generate duplicate method names. We also don't generate if there's already a CreateMany with 
            // a single parameter.
            types.Where(x => IsPartial(x) && !HasCreate(x, "CreateMany")),
            (ctx, data) =>
            {
                ctx.AddSource(data.Name + ".CreateMany.g",
                    $$"""
                    // <auto-generated />
                    using System.Collections.Generic;

                    namespace {{data.ContainingNamespace.ToDisplayString(fullNameFormat)}}
                    {
                        partial record {{data.Name}}
                        {
                            public static List<{{data.Name}}> CreateMany(dynamic value)
                                => __{{data.Name}}Factory.CreateMany(value);
                        }
                    }
                    """.Replace("\r\n", "\n").Replace("\n", Environment.NewLine));
            });
    }

    /// <summary>
    /// Checks if there are declaring syntax references with the 'partial' keyword.
    /// Types declared in another project will not have them.
    /// </summary>
    static bool IsPartial(ITypeSymbol type)
        => type.DeclaringSyntaxReferences.Any() && type.DeclaringSyntaxReferences.All(
            r => r.GetSyntax() is RecordDeclarationSyntax c && c.Modifiers.Any(
                m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)));

    static bool HasCreate(ITypeSymbol type, string name = "Create") => FindCreate(type, name) is IMethodSymbol;

    static IMethodSymbol? FindCreate(ITypeSymbol type, string name = "Create")
        => type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(x => x.Name == name && x.IsStatic && x.Parameters.Length == 1 &&
                (x.Parameters[0].Type.SpecialType == SpecialType.System_Object || x.Parameters[0].Type.TypeKind == TypeKind.Dynamic))
            .FirstOrDefault();

    class TypesVisitor : SymbolVisitor
    {
        readonly Func<INamedTypeSymbol, bool> shouldInclude;
        readonly CancellationToken cancellation;
        readonly HashSet<INamedTypeSymbol> types = new(SymbolEqualityComparer.Default);

        public TypesVisitor(Func<INamedTypeSymbol, bool> shouldInclude, CancellationToken cancellation)
        {
            this.shouldInclude = shouldInclude;
            this.cancellation = cancellation;
        }

        public HashSet<INamedTypeSymbol> TypeSymbols => types;

        public static IEnumerable<INamedTypeSymbol> Visit(INamespaceSymbol symbol, Func<INamedTypeSymbol, bool> shouldInclude, CancellationToken cancellation)
        {
            var visitor = new TypesVisitor(shouldInclude, cancellation);
            symbol.Accept(visitor);
            return visitor.TypeSymbols;
        }

        public override void VisitAssembly(IAssemblySymbol symbol)
        {
            cancellation.ThrowIfCancellationRequested();
            symbol.GlobalNamespace.Accept(this);
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (var namespaceOrType in symbol.GetMembers())
            {
                cancellation.ThrowIfCancellationRequested();
                namespaceOrType.Accept(this);
            }
        }

        public override void VisitNamedType(INamedTypeSymbol type)
        {
            cancellation.ThrowIfCancellationRequested();

            if (!shouldInclude(type) || !types.Add(type))
                return;

            var nestedTypes = type.GetTypeMembers();
            if (nestedTypes.IsDefaultOrEmpty)
                return;

            foreach (var nestedType in nestedTypes)
            {
                cancellation.ThrowIfCancellationRequested();
                nestedType.Accept(this);
            }
        }
    }

}
