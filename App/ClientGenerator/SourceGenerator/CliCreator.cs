using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PythonHttpParser.Services;
using SourceGenerator.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace SourceGenerator;

public static class CliCreator
{
    public static CompilationUnitSyntax Create(List<IPythonRoute> routesList,string namespacesString)
    {
        var syntaxFactory = CompilationUnit();
            syntaxFactory = syntaxFactory.AddUsings(UsingDirective(ParseName("System")));
            syntaxFactory = syntaxFactory.AddUsings(UsingDirective(ParseName("System.Collections.Generic")));
            syntaxFactory = syntaxFactory.AddUsings(UsingDirective(ParseName("Newtonsoft.Json")));
            syntaxFactory = syntaxFactory.AddUsings(UsingDirective(ParseName(namespacesString)));


            var @namespace =
                NamespaceDeclaration(
                    ParseName(namespacesString));
            var classDeclaration = ClassDeclaration("ClientGen");
            classDeclaration = classDeclaration
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddModifiers(Token(SyntaxKind.StaticKeyword))
                .AddModifiers(Token(SyntaxKind.PartialKeyword));

            var routes = routesList
                .Aggregate("{", (current, serverParserRoute) => current + @$"""{serverParserRoute.Route}"",") + "}";

            var choice = string.Empty;
            var counter = 1;
            foreach (var serverParserRoute in routesList)
            {
                var parameters = serverParserRoute.QueryParameters
                    .Aggregate(string.Empty, (current, bodyParameter)
                        => current + $"{bodyParameter.Key}, ");
                parameters = serverParserRoute.BodyParameters
                    .Aggregate(parameters, (current, bodyParameter)
                        => current + $"{bodyParameter.Key}, ");
                if (parameters.Length > 1)
                    parameters = parameters.Substring(0, parameters.Length - 2);

                
                choice += @$"else if (reply == ""{counter}"")
                              {{
                                {serverParserRoute.BodyParameters.ToInputExpressions()}
                                {serverParserRoute.QueryParameters.ToInputExpressions()} 
                                var res = Route{routesList[counter - 1].Route.ToClassName()}.Request({parameters});
                                Console.WriteLine(JsonConvert.SerializeObject(res));
                              }}";
                counter += 1;
            }

        
            var syntax = ParseStatement(@$"while (true)
        {{
            var routes = new List<string>(){routes};
            
            Console.WriteLine(""Choose route or 0 for exit:"");
            var counter = 1;
            foreach (var pythonRoute in routes)
            {{
                Console.WriteLine($""{{counter}}: {{pythonRoute}}"");
                counter++;
            }}

            var reply = Console.ReadLine();
            if (reply == ""0"")
                {{
                    return;
                }}
           {choice}
            else
            {{
                Console.WriteLine(""Try one more time"");
            }}
        }}");
            var parameterList = new List<ParameterSyntax>
            {
                Parameter(Identifier("modelsPath")).WithType(ParseTypeName("string")),
                Parameter(Identifier("routesPath")).WithType(ParseTypeName("string")),
                Parameter(Identifier("rootUrl")).WithType(ParseTypeName("string"))
            };
            
            
            var generateClientMethodDeclaration = MethodDeclaration(ParseTypeName("void"), "GenerateClient")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddModifiers(Token(SyntaxKind.StaticKeyword))
                .AddModifiers(Token(SyntaxKind.PartialKeyword))
                .AddParameterListParameters(parameterList.ToArray())
                .WithBody(Block(syntax));

           
            classDeclaration = classDeclaration.AddMembers(generateClientMethodDeclaration);
            @namespace = @namespace.AddMembers(classDeclaration);
            syntaxFactory = syntaxFactory.AddMembers(@namespace);
            return syntaxFactory;
    }
}