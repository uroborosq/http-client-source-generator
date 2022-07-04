using System.Text.RegularExpressions;
using PythonHttpParser.Services;

namespace PythonHttpParser;

public class PythonRouteParser
{
    private readonly string _path;
    private readonly TypeTable _typeTable;

    public PythonRouteParser(string path, TypeTable typeTable)
    {
        _path = path;
        _typeTable = typeTable;
        Routes = new List<IPythonRoute>();
    }
    
    public List<IPythonRoute> Routes { get; }
    
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
                        returnValue,
                        listBodyParameters,
                        listQueryParameters
                        ));

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
                returnValue,
                listBodyParameters,
                listQueryParameters
                ));
        }
}