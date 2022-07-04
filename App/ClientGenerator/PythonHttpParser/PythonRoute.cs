using PythonHttpParser.Services;

namespace PythonHttpParser;

public class PythonRoute : IPythonRoute
{
    public PythonRoute(string route, RequestType requestType, string returnValue, Dictionary<string, string> bodyParameters, Dictionary<string, string> queryParameters)
    {
        Route = route;
        RequestType = requestType;
        ReturnValue = returnValue;
        BodyParameters = bodyParameters;
        QueryParameters = queryParameters;
    }

    public string Route { get; }
    public RequestType RequestType { get; }
    public string ReturnValue { get; }
    public Dictionary<string, string> BodyParameters { get; }
    public Dictionary<string, string> QueryParameters { get; }
}