using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Merq;

class ExecuteReturnRewriter : CSharpSyntaxRewriter
{
    readonly SemanticModel semantic;
    readonly INamedTypeSymbol commandSymbol;
    readonly string returnType;

    public ExecuteReturnRewriter(SemanticModel semantic, INamedTypeSymbol commandSymbol, string returnType)
        => (this.semantic, this.commandSymbol, this.returnType)
        = (semantic, commandSymbol, returnType);

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (node.ParameterList.Parameters.Count < 1 ||
            node.ParameterList.Parameters.Count > 2)
            return node;

        if (node.ParameterList.Parameters[0].Type is not TypeSyntax parameterSyntax ||
            semantic.GetSymbolInfo(parameterSyntax).Symbol is not ITypeSymbol parameterType ||
            !parameterType.Equals(commandSymbol, SymbolEqualityComparer.Default))
            return node;

        if (node.Identifier.ToString() == "Execute")
        {
            return node.WithReturnType(ParseTypeName(returnType).WithTrailingTrivia(Space));
        }
        else if (node.Identifier.ToString() == "ExecuteAsync")
        {
            if (node.ReturnType is GenericNameSyntax generic)
                return node.WithReturnType(generic
                    .WithTypeArgumentList(
                        TypeArgumentList(
                            SeparatedList(new[] { ParseTypeName(returnType) }))));
            else
                return node.WithReturnType(
                    GenericName(Identifier("Task"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SeparatedList(new[] { ParseTypeName(returnType) }))));
        }

        return node;
    }
}
