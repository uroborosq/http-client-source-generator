using PythonHttpParser.Services;

namespace PythonHttpParser;

public static class StringExtension
{
    public static RequestType FromString(this RequestType type, string str)
    {
        switch (str)
        {
            case "get":
                return RequestType.Get;
            case "post":
                return RequestType.Post;
            case "patch":
                return RequestType.Patch;
            case "put":
                return RequestType.Put;
            case "delete":
                return RequestType.Delete;
            default:
                throw new ArgumentException($"This string {str} can not be converted to RequestType");
        }
    }
}