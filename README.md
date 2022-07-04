### Представление информации о http сервере

В ходе разработки проекта был реализован http сервер на фреймворке FastApi на базе Python, содержащий следующие методы:
```python
@app.get("/requests/get")
async def read_root() -> list[Request]:
    return requestsService.get_requests()


@app.post("/requests/create")
async def add_request(client: str = Query(''), manager: Manager = Body(None), date_begin: datetime = Query(datetime.today())) -> Request:
    return requestsService.add_request(client, manager, date_begin)


@app.post("/managers/create")
async def add_manager(name: str = Query('')) -> Manager:
    return managerService.add(name)


@app.get("/managers/get")
async def get_managers() -> list[Manager]:
    return managerService.get()
```
и модели данных:
```python
class Manager(BaseModel):
    full_name: str


class Request(BaseModel):
    client: str
    manager: Manager
    date_begin: datetime
```

Был написан парсер на С#, представляющий информацию о моделях в следующем виде:
```c#
public interface IPythonModel
{
    string Name { get; }
    Dictionary<string, string> Values { get; }
}
```
В качестве ключа в словаре используюется название поля в исходной модели, а в качестве значения - тип в С#

Информация о методах представлена в следующем виде:
```c#
public interface IPythonRoute
{
    string Route { get; }
    RequestType RequestType { get; }
    string ReturnValue { get; }
    Dictionary<string, string> BodyParameters { get; }
    Dictionary<string, string> QueryParameters { get; }
}
```
Названия и тип параметров храниться аналогичным моделям образом.

---

### Алгоритм получения модели сервера

Парсер получает на вход путь к .py файлу с моделями и к файлу с методами, после чего построчно считывает информацию, находя нужные строчки при помощи регулярных выражений. Затем создает объекты с информацией о сервере.

Метод, генерирующий информацию о моделях:

```c#
public void Parse()
    {
        if (!File.Exists(_path)) return;

        var strings = File.ReadAllLines(_path);
        var isModel = new Regex(@"class .+\(BaseModel\):");
        var isField = new Regex(@"\t*: [a-zA-Z]+");
        var modelName = string.Empty;
        var modelValues = new Dictionary<string, string>();
        foreach (var s in strings)
        {
            if (isModel.IsMatch(s) && modelName == string.Empty)
            {
                modelName = s.Substring(6, s.Length - 18);
            }
            else if (isModel.IsMatch(s))
            {
                Models.Add(_modelCreator.Create(modelName, modelValues));
                modelName = s.Substring(6, s.Length - 18);
                modelValues = new Dictionary<string, string>();
            }
            else if (isField.IsMatch(s))
            {
                var valueName = Regex.Replace(s, @"\s+", "");
                var valueType = valueName.Split(':')[1];

                if (valueType.Contains('='))
                {
                    valueType = valueType.Split('=')[0];
                }

                valueName = valueName.Split(':')[0];
                modelValues.Add(valueName, _typeTable.GetType(valueType));
            }
        }

        Models.Add(_modelCreator.Create(modelName, modelValues));
    }
```
Метод, генерирующий информацию о методах:
```c#
public void Parse()
        {
            if (!File.Exists(_path)) return;

            var isRoute = new Regex(@"@.+\..+");
            var isParameters = new Regex(@"def .+\(.*\).+:");

            var routeName = string.Empty;
            var listBodyParameters = new Dictionary<string, string>();
            var listQueryParameters = new Dictionary<string, string>();
            var returnValue = string.Empty;
            var requestType = RequestType.Get;


            foreach (var str in File.ReadAllLines(_path))
            {
                if (isRoute.IsMatch(str) && routeName == string.Empty)
                {
                    routeName = str.Substring(1, str.Length - 1).Split('"')[1];
                    requestType = new RequestType().FromString(str.Substring(str.IndexOf('.') + 1,
                        str.IndexOf('(') - str.IndexOf('.') - 1));
                }
                else if (isRoute.IsMatch(str))
                {
                    Routes.Add(new PythonRoute(
                        routeName,
                        requestType,
                        listBodyParameters,
                        listQueryParameters,
                        returnValue));

                    routeName = str.Substring(1, str.Length - 1).Split('"')[1];
                    requestType = new RequestType().FromString(str.Substring(str.IndexOf('.') + 1,
                        str.IndexOf('(') - str.IndexOf('.') - 1));
                    listBodyParameters = new Dictionary<string, string>();
                    listQueryParameters = new Dictionary<string, string>();
                    returnValue = string.Empty;
                }
                else if (isParameters.IsMatch(str))
                {
                    var parameters = str.Substring(str.IndexOf('(') + 1, str.LastIndexOf(')') - str.IndexOf('('))
                        .Split(',');
                    foreach (var item in parameters)
                    {
                        if (!item.Contains('=')) continue;
                        if (item.Split('=')[1].Contains("Query"))
                        {
                            var name = Regex.Replace(item.Split('=')[0].Split(':')[0], @"\s+", "");
                            var type = Regex.Replace(item.Split('=')[0].Split(':')[1], @"\s+", "");
                            listQueryParameters.Add(name, _typeTable.GetType(type));
                        }
                        else if (item.Split('=')[1].Contains("Body"))
                        {
                            var name = Regex.Replace(item.Split('=')[0].Split(':')[0], @"\s+", "");
                            var type = Regex.Replace(item.Split('=')[0].Split(':')[1], @"\s+", "");
                            listBodyParameters.Add(name, _typeTable.GetType(type));
                        }
                    }

                    var returnValueStartIndex = str.IndexOf('>');
                    returnValue =
                        Regex.Replace(str.Substring(returnValueStartIndex + 1, str.Length - returnValueStartIndex - 2),
                            @"\s+", "");
                    returnValue = _typeTable.GetType(returnValue);
                }
            }

            Routes.Add(new PythonRoute(
                routeName,
                requestType,
                listBodyParameters,
                listQueryParameters,
                returnValue));
        }
```

---

### Генерация исходного кода для http клиента

Для создания http клиента используется Source Generator. Для создания своего генератора требуется реализовать интерфейс ISourceGenerator.

```c#
[Generator]
public class HelloSourceGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
```

Непосредственно генерация происходит в методе Execute. Из объекта типа GeneratorExecutionContext можно получить информацию о текущей сборке, найти SyntaxTree, главный метод сборки.

В лабораторной работе в методе Execute вызывается парсер, далее по полученной метаинформации генерируется классы моделей и методов. Определения методов, классов, необходимые атрибуты генерируются с помощью SyntaxFactory, для создания тел методов используются парсинг выражений из той же SyntaxFactory.

```c#
[Generator]
public class HelloSourceGenerator : ISourceGenerator
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
        var modelParser = new PythonModelParser(modelPath, new PythonModelCreator(), typeTable);

        serverParser.Parse();
        modelParser.Parse();

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
```

Для генерации кода моделей, методов и консольного интерфейса используются три класса

```c#
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
```

```csharp

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PythonHttpParser.Services;
using SourceGene
rator.Extensions;
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
```
Пример сгенерированного кода:

```csharp
namespace App
{
    public class Request
    {
        public string client { get; init; }

        public Manager manager { get; init; }

        public DateTime date_begin { get; init; }
    }
}
```

```csharp
using System.Text;
using System.Net;
using Newtonsoft.Json;

namespace App
{
    public class RouteRequestsCreate
    {
        public static Request Request(string client, Manager manager, DateTime date_begin)
        {
            var url = $"http://localhost:8000/requests/create?client={client}";
            var json = "{";
            var httpClient = new HttpClient();
            json += $@" ""manager"":";
            json += JsonConvert.SerializeObject(manager);
            json += ",";
            json += $@" ""date_begin"":";
            json += JsonConvert.SerializeObject(date_begin);
            json += "}";
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            var response = httpClient.PostAsync(url, data);
            var result = response.Result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Request>(result.Result);
        }
    }
}
```