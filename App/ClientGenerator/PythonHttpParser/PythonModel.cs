using PythonHttpParser.Services;

namespace PythonHttpParser;

public class PythonModel : IPythonModel
{
    public PythonModel(string name, Dictionary<string, string> values)
    {
        Name = name;
        Values = values;
    }
    public string Name { get; }
    public Dictionary<string, string> Values { get; }
}