using PythonHttpParser.Services;

namespace PythonHttpParser;

public class PythonModelCreator : IModelCreator
{
    public IPythonModel Create(string name, Dictionary<string, string> values)
    {
        return new PythonModel(name, values);
    }
}