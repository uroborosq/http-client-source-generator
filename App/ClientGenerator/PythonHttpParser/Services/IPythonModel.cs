namespace PythonHttpParser.Services;

public interface IPythonModel
{
    string Name { get; }
    Dictionary<string, string> Values { get; }
}