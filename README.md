### Представление информации о http сервере

В ходе разработки проекта был реализован http сервер на фреймворке FastApi на базе Python, содержащий следующие эндпоинты:
```python

GET /requests/get/
Возращаемое значение - коллекция сущностей Request
Возращает все сущности типа Requests

POST /requests/create
Возращаемое значение - Request
Создание сущности Request
Query-параметры: client: str, date_begin: str
Body-параметры: manager:Manager

POST /managers/create
Возращамое значение - Manager
Создание сущности Manager
Query-параметры: client: str

GET /managers/get
Возращаемое значение - коллекция сущностей Manager
Возращает все хранимые сущности типа Manager

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

Непосредственно генерация происходит в методе Execute. Из объекта типа GeneratorExecutionContext можно получить информацию о текущей сборке, найти SyntaxTree, точку входа приложения.

В лабораторной работе в методе Execute вызывается парсер, далее по полученной метаинформации генерируется классы моделей и методов. Определения методов, классов, необходимые атрибуты генерируются с помощью SyntaxFactory, для создания тел методов используются парсинг выражений из той же SyntaxFactory.

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