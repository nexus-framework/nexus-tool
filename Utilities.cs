using CaseExtensions;

namespace Nexus;

public static class Utilities
{
    /// <summary>
    /// Replaces spaces with . and removes: ,/
    /// </summary>
    /// <param name="name"></param>
    /// <param name="replaceSpaceWith"></param>
    /// <returns></returns>
    public static string CleanNameAndReplaceSpaces(string name, char replaceSpaceWith)
    {
        return name.Trim().ToLower()
            .Replace(' ', replaceSpaceWith)
            .Replace(",", "")
            .Replace("/", "");
    }
    
    /// <summary>
    /// Returns service name like: project-api
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string GetKebabCasedNameAndApi(string name)
    {
        string cleanedName = CleanNameAndReplaceSpaces(name, '-');
        string kebabCased = cleanedName.ToKebabCase();

        return kebabCased.EndsWith("-api") ? kebabCased : $"{kebabCased}-api";
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