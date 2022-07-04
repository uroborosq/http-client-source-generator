using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGenerator;

public static class DeclarationCreator
{
    public static LocalDeclarationStatementSyntax Create(string identifier, ExpressionSyntax initialiazer)
    {
        var statement = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(
                        SyntaxTriviaList.Empty,
                        SyntaxKind.VarKeyword,
                        "var",
                        "var",
                        SyntaxTriviaList.Empty)))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier(identifier)
                        ).WithInitializer(
                            SyntaxFactory.EqualsValueClause(initialiazer)))
                ));
        return statement;
    }
}