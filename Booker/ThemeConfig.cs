namespace Booker;

/// <summary>
/// Theme configuration data read from theme.yaml.
/// All directories are assumed to be relative to the location of theme.yaml unless otherwise specified.
/// </summary>
public class ThemeConfig
{
    public string ThemeName = null!;    // user-friendly theme name
    public string TemplatesDir = null!; // subdirectory containing .cshtml template files
    public string RawFilesDir = null!;  // contents of this subdirectory will be copied verbatim (e.g. for images, .css files)

    // filled in at runtime:
    public FileInfo ConfigFile = null!;
}