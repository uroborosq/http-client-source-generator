namespace PythonHttpParser;

public class TypeTable
{
    private readonly Dictionary<string, string> _types;
    public TypeTable(Dictionary<string, string> type)
    {
        _types = type;
    }

    public string GetType(string pythonType)
    {
        return _types[pythonType];
    }
}