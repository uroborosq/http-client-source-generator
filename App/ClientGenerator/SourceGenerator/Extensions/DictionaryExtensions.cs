namespace SourceGenerator;

public static class DictionaryExtensions
{
    public static string ToInputExpressions(this Dictionary<string, string> dictionary)
    {
        var res = string.Empty;
        foreach (var pair in dictionary)
        {
            res += @$"Console.WriteLine(@""Input value """"{pair.Key}"""" of type """"{pair.Value}"""""");";
            if (pair.Value == "string")
            {
                res += $"var {pair.Key} = Console.ReadLine();";
            }
            else
            {
                res += $"var {pair.Key} =  JsonConvert.DeserializeObject<{pair.Value}>(Console.ReadLine());";
            }
        }

        return res;
    }
}