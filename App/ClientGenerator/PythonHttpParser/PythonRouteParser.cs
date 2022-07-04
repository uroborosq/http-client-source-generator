using System.Text.RegularExpressions;
using PythonHttpParser.Services;

namespace PythonHttpParser;

public class PythonModelParser
{
    private readonly string _path;
    private readonly TypeTable _typeTable;
    private readonly IModelCreator _modelCreator;

    public PythonModelParser(string path, TypeTable typeTable, IModelCreator modelCreator)
    {
        _path = path;
        _modelCreator = modelCreator;
        _typeTable = typeTable;
        Models = new List<IPythonModel>();
    }
        
    
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
    
    public List<IPythonModel> Models { get; }
}