namespace PythonHttpParser.Services;

public interface IModelCreator
{
    public IPythonModel Create(string name, Dictionary<string, string> values);
}