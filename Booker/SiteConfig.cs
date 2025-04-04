namespace Booker;

/// <summary>
/// Site configuration data read from site.yaml.
/// All directories are assumed to be relative to the location of site.yaml unless otherwise specified.
/// </summary>
public class SiteConfig
{
    // directories
    public string PagesDir = null!;     // subdirectory containing .md markdown files with page content
    public string OutputsDir = null!;   // working directory where the output will be placed
    public string ThemeFile = null!;    // file name of the theme.yaml file to apply

    // settings
    public string Generator = "Mies";
    public string Title = null!, Author = null!, Description = null!;   // filled in by the user, and used to fill in website metadata
    public int RecentPosts;     // controls how many recent posts show up on the main page

    public string? GTag;

    public bool IsGAEnabled => !string.IsNullOrWhiteSpace(GTag);

    // filled in at runtime:
    public FileInfo ConfigFile = null!;
}