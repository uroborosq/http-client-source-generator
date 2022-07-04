namespace PythonHttpParser.Services;

public interface IPythonRoute
{
    string Route { get; }
    RequestType RequestType { get; }
    string ReturnValue { get; }
    Dictionary<string, string> BodyParameters { get; }
    Dictionary<string, string> QueryParameters { get; }
}