namespace SourceGenerator.Extensions;

public static class StringExtensions
{
    public static string ToClassName(this string str)
    {
        var res = string.Empty;
        for (var i = 0; i < str.Length - 1; i++)
        {
            if (str[i] == '/')
            {
                res += char.ToUpper(str[i + 1]);
                i += 1;
            }
            else
            {
                res += str[i];
            }
        }
        res += str[str.Length - 1];
        return res;
    }
}