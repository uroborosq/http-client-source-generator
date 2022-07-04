using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PythonHttpParser.Services;

namespace SourceGenerator;


public static class ModelCreator
{
    public static CompilationUnitSyntax CreateModelFromPythonModel(IPythonModel model, string @namespaceString)
    {
        var modelUnit = SyntaxFactory.CompilationUnit();
        var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceString));
        var classDeclaration = SyntaxFactory.ClassDeclaration(model.Name);
        classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        classDeclaration = model.Values.Select(value => SyntaxFactory
                .PropertyDeclaration(SyntaxFactory.ParseTypeName(value.Value), value.Key)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))))
            .Aggregate(classDeclaration, (current, propertyDeclaration) => current.AddMembers(propertyDeclaration));
        @namespace = @namespace.AddMembers(classDeclaration);
        modelUnit = modelUnit.AddMembers(@namespace);
        return modelUnit;
    }
}