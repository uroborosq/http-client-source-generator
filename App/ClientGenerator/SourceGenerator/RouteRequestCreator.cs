using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PythonHttpParser.Services;
using SourceGenerator.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SourceGenerator;

public static class RouteRequestCreator
{
    public static CompilationUnitSyntax CreateRouteClass(IPythonRoute route, string namespaceString, string rootUrl)
    {
        var routeUnit = CompilationUnit();
        routeUnit = routeUnit.AddUsings(UsingDirective(ParseName("System.Text")));
        routeUnit = routeUnit.AddUsings(UsingDirective(ParseName("System.Net")));
        routeUnit = routeUnit.AddUsings(UsingDirective(ParseName("Newtonsoft.Json")));

        var @namespace = NamespaceDeclaration(ParseName($"{namespaceString}"));
        var classDeclaration = ClassDeclaration($"Route{route.Route.ToClassName()}");
        classDeclaration = classDeclaration.AddModifiers(Token(SyntaxKind.PublicKeyword));

        var parameterList = route.QueryParameters.Select(pair => Parameter(Identifier(pair.Key))
            .WithType(ParseTypeName(pair.Value))).ToList();

        parameterList.AddRange(route.BodyParameters
            .Select(pair => Parameter(Identifier(pair.Key)).WithType(ParseTypeName(pair.Value))));

        var expressionsOfBody = new SyntaxList<StatementSyntax>();

        var interpolatedText = new List<InterpolatedStringContentSyntax>
        {
            InterpolatedStringText()
                .WithTextToken(Token(
                    TriviaList(),
                    SyntaxKind.InterpolatedStringTextToken,
                    rootUrl + route.Route + "?",
                    rootUrl + "?",
                    TriviaList()))
        };
        var counter = 0;
        foreach (var routeQueryParameter in route.QueryParameters)
        {
            interpolatedText.Add(InterpolatedStringText().WithTextToken(Token(
                TriviaList(),
                SyntaxKind.InterpolatedStringTextToken,
                $"{routeQueryParameter.Key}=",
                $"{routeQueryParameter.Key}=",
                TriviaList()
            )));
            interpolatedText.Add(Interpolation(
                IdentifierName(routeQueryParameter.Key)));
            counter++;
            if (counter < route.QueryParameters.Count)
            {
                interpolatedText.Add(InterpolatedStringText().WithTextToken(Token(
                    TriviaList(),
                    SyntaxKind.InterpolatedStringTextToken,
                    "&",
                    "&",
                    TriviaList()
                )));
            }
        }


        expressionsOfBody = expressionsOfBody.Add(DeclarationCreator.Create(
            "url",
            InterpolatedStringExpression(
                    Token(SyntaxKind.InterpolatedStringStartToken))
                .WithContents(
                    List(interpolatedText))));

        if (route.BodyParameters.Count > 1)
        {
            expressionsOfBody = expressionsOfBody.Add(DeclarationCreator.Create(
                "json",
                LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal("{"))));
        }
        else
        {
            expressionsOfBody = expressionsOfBody.Add(DeclarationCreator.Create("json",
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    PredefinedType(
                        Token(SyntaxKind.StringKeyword)),
                    IdentifierName("Empty"))));
        }


        expressionsOfBody = expressionsOfBody.Add(DeclarationCreator.Create(
            "httpClient",
            ObjectCreationExpression(
                    IdentifierName("HttpClient"))
                .WithArgumentList(
                    ArgumentList())));
        counter = 0;
        foreach (var routeBodyParameter in route.BodyParameters)
        {
            if (route.BodyParameters.Count > 1)
            {
                expressionsOfBody = expressionsOfBody.Add(ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        IdentifierName("json"),
                        InterpolatedStringExpression(
                                Token(SyntaxKind.InterpolatedVerbatimStringStartToken))
                            .WithContents(
                                SingletonList<InterpolatedStringContentSyntax>(
                                    InterpolatedStringText()
                                        .WithTextToken(
                                            Token(
                                                TriviaList(),
                                                SyntaxKind.InterpolatedStringTextToken,
                                            $"\"\"{routeBodyParameter.Key}\"\":",
                                            $"\"{routeBodyParameter.Key}\":", 
                                                TriviaList())))))));
            }


            expressionsOfBody = expressionsOfBody.Add(ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    IdentifierName("json"),
                    InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("JsonConvert"),
                                IdentifierName("SerializeObject")))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        IdentifierName(routeBodyParameter.Key))))))));
            counter++;
            if (counter < route.BodyParameters.Count)
            {
                expressionsOfBody = expressionsOfBody.Add(ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        IdentifierName("json"),
                        LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal(",")))));
            }
        }

        if (route.BodyParameters.Count > 1)
        {
            expressionsOfBody = expressionsOfBody.Add(ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    IdentifierName("json"),
                    LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        Literal("}")))));
        }


        expressionsOfBody = expressionsOfBody.Add(
            DeclarationCreator.Create(
                "data",
                ObjectCreationExpression(
                        IdentifierName("StringContent"))
                    .WithArgumentList(
                        ArgumentList(
                            SeparatedList<ArgumentSyntax>(
                                new SyntaxNodeOrToken[]
                                {
                                    Argument(
                                        IdentifierName("json")),
                                    Token(SyntaxKind.CommaToken),
                                    Argument(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("Encoding"),
                                            IdentifierName("UTF8"))),
                                    Token(SyntaxKind.CommaToken),
                                    Argument(
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal("application/json")))
                                })))));

        // var response = httpClient.{route.RequestType.ToString()}Async(url{(route.RequestType == RequestType.Post ? ", data": string.Empty)});

        var argumentList = SeparatedList<ArgumentSyntax>(
            new SyntaxNodeOrToken[]
            {
                Argument(IdentifierName("url")),
            });
        if (route.RequestType == RequestType.Post)
        {
            argumentList = argumentList.Add(Argument(IdentifierName("data")));
        }

        expressionsOfBody = expressionsOfBody.Add(
            DeclarationCreator.Create(
                "response",
                InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("httpClient"),
                            IdentifierName($"{route.RequestType.ToString()}Async")))
                    .WithArgumentList(ArgumentList(argumentList))));

        // var result = response.Result.Content.ReadAsStringAsync();
        expressionsOfBody = expressionsOfBody.Add(
            DeclarationCreator.Create(
                "result",
                InvocationExpression(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("response"),
                            IdentifierName("Result")),
                        IdentifierName("Content")),
                    IdentifierName("ReadAsStringAsync")))));

        if (route.ReturnValue != "void")
        {
            expressionsOfBody = expressionsOfBody.Add(ReturnStatement(
                InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("JsonConvert"),
                            GenericName(
                                    Identifier("DeserializeObject"))
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList<TypeSyntax>(
                                            IdentifierName(route.ReturnValue))))))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("result"),
                                        IdentifierName("Result"))))))));
        }


        var methodDeclaration = MethodDeclaration(ParseTypeName(route.ReturnValue), "Request")
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddModifiers(Token(SyntaxKind.StaticKeyword))
            .AddParameterListParameters(parameterList.ToArray())
            .WithBody(Block(expressionsOfBody));

        classDeclaration = classDeclaration.AddMembers(methodDeclaration);
        @namespace = @namespace.AddMembers(classDeclaration);
        routeUnit = routeUnit.AddMembers(@namespace);
        return routeUnit;
    }
}