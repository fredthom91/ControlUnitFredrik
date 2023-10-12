namespace ControlUnitFredrik.Utilities;

public static class StringUtilities
{
    public static string CleanPropertyName(string propertyName)
    {
        return propertyName.Replace(" ", "_");
    }
}