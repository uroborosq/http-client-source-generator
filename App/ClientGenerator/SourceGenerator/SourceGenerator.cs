using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PythonHttpParser;
using SourceGenerator.Extensions;

namespace SourceGenerator;

[Generator]
public class SourceGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var dictionary = new Dictionary<string, string>
            {
                {"str", "string"},
                {"int", "int"},
                {"Manager", "Manager"},
                {"datetime", "DateTime"},
                {"list[Manager]", "List<Manager>"},
                {"list[Request]", "List<Request>"},
                {"Request", "Request"}
            };
        var typeTable = new TypeTable(dictionary);
        
        var mainMethod = context.Compilation.GetEntryPoint(context.CancellationToken);
        if (mainMethod is null)
            throw new NullReferenceException();

        var methodArgs = new List<string>();
        foreach (var compilationSyntaxTree in context.Compilation.SyntaxTrees)
        {
            var root = compilationSyntaxTree.GetRoot();
            var firstParameters = from methodDeclaration in root.DescendantNodes()
                    .OfType<ExpressionStatementSyntax>()
                                  select methodDeclaration.Expression;
            var start = false;


            foreach (var expressionSyntax in firstParameters)
            {
                foreach (var descendantNode in expressionSyntax.DescendantNodes())
                {
                    if (descendantNode.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        start = descendantNode.ToString() == "ClientGen.GenerateClient";
                    }

                    if (descendantNode.IsKind(SyntaxKind.Argument) && start)
                    {
                        methodArgs.Add(Regex.Replace(descendantNode.ToString(), @"""", ""));
                    }
                }
            }
        }

        var rootUrl = methodArgs[2];
        var serverPath = methodArgs[1];
        var modelPath = methodArgs[0];

        var serverParser = new PythonRouteParser(serverPath, typeTable);
        var modelParser = new PythonModelParser(modelPath,  typeTable, new PythonModelCreator());

        serverParser.Parse();
        modelParser.Parse();

        throw new Exception();
        
        foreach (var modelParserModel in modelParser.Models)
        {
            var modelUnit = ModelCreator.CreateModelFromPythonModel(modelParserModel, mainMethod.ContainingNamespace.ToDisplayString());
            context.AddSource($"{modelParserModel.Name}.g.cs", modelUnit.NormalizeWhitespace().ToFullString());
        }
        foreach (var route in serverParser.Routes)
        {
            var modelUnit = RouteRequestCreator.CreateRouteClass(route, mainMethod.ContainingNamespace.ToDisplayString(), rootUrl);
            context.AddSource($"{route.Route.ToClassName()}.g.cs", modelUnit.NormalizeWhitespace().ToFullString());
        }
        var syntaxFactory = CliCreator.Create(serverParser.Routes, mainMethod.ContainingNamespace.ToDisplayString());

        var code = syntaxFactory
            .NormalizeWhitespace()
            .ToFullString();
        context.AddSource($"ClientGen.g.cs", code);

    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}