using CaseExtensions;

namespace Nexus.Extensions;

public static class NameExtensions
{
    public static string CleanNameAndReplaceSpaces(string name, char replaceSpaceWith)
    {
        return name.Trim().ToLower()
            .Replace(' ', replaceSpaceWith)
            .Replace('-', replaceSpaceWith)
            .Replace(",", "")
            .Replace("/", "");
    }
    
    public static string GetKebabCasedNameAndApi(string name)
    {
        string cleanedName = CleanNameAndReplaceSpaces(name, '-');
        string kebabCased = cleanedName.ToKebabCase();

        return kebabCased.EndsWith("-api") ? kebabCased : $"{kebabCased}-api";
    }
    
    public static string GetSnakeCasedNameAndApi(string name)
    {
        string cleanedName = CleanNameAndReplaceSpaces(name, '-');
        string snakeCased = cleanedName.ToSnakeCase();

        return snakeCased.EndsWith("_api") ? snakeCased : $"{snakeCased}_api";
    }
    
    public static string GetKebabCasedNameWithoutApi(string name)
    {
        string cleanedName = CleanNameAndReplaceSpaces(name, '-');
        string kebabCased = cleanedName.ToKebabCase();

        return kebabCased.EndsWith("-api") ? kebabCased.Replace("-api", "") : kebabCased;
    }

    public static string GetPascalCasedNameAndDotApi(string name)
    {
        string cleanedName = CleanNameAndReplaceSpaces(name, '.');
        string nameWithoutApi = cleanedName.EndsWith("api") ? cleanedName[..^3].TrimEnd('-').Trim('.') : cleanedName;
        return $"{nameWithoutApi.ToPascalCase()}.Api";
    }
}