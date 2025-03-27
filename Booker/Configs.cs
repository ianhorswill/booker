using System.Diagnostics;

namespace Booker
{
    /// <summary>
    /// App configuration parameters passed from command line
    /// </summary>
    public class MiesConfig
    {
        public FileInfo SiteConfig { get; set; } = null!;
        public DirectoryInfo SiteDirectory { get; set; } = null!;
    }

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

        public string GTag;

        public bool IsGAEnabled => !string.IsNullOrWhiteSpace(GTag);

        // filled in at runtime:
        public FileInfo ConfigFile = null!;
    }

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

    /// <summary>
    /// Per-page metadata. Contains a combination of per-page metadata, as well as
    /// references to broad generator state (e.g. site config, pages produced so far)
    /// so that page templates can make use of it if desired.
    /// </summary>
    [DebuggerDisplay("{"+nameof(PageTitle)+"}")]
    public class PageModel
    {
        // note: the following get read from a yaml header at the top of each markdown page
        public string PageTitle = null!;  // title of the page, shown in the browser title bar, metadata, and in inbound links
        public string PageDesc = null!;   // short one liner description, used in table of contents as well as metadata
        public DateTime? Date;    // timestamp for this page; may be null in which case it will not be displayed
        public bool IsDraft;      // if true, this page will be skipped during generation (useful for draft posts)
        public bool IsBlogPost;   // if true, this page will be added chronologically on the index page and in the table of contents
        public bool IsIndex;      // if true, this page will be generated last, so that it can index the other ones
        public string Template = null!;   // which .cshtml file to use for this page

        // note: the following get filled in at runtime from the markdown file.
        // they are generated after
        public string Markdown = null!;   // the raw markdown text loaded for this page
        public string Contents = null!;   // the results of converting markdown to HTML, but before pushing through the page template
        public string PageLink = null!;   // name of the target HTML file, e.g. "foo.html" if the source was "foo.md"

        public PageModel? Next;  // Next sibling or null
        public PageModel? Previous;  // Previous sibling or null
        public PageModel? Up;
        public PageModel[]? Children;

        public PageModel? EffectiveNext {
            get {
                if (Children != null && Children.Length > 0)
                    return Children[0];
                if (Next != null)
                    return Next;
                // We're the last leaf under somebody
                var n = Up;
                while (n is { Next: null })
                    n = n.Up;
                return n?.Next;
            }
        }

        public PageModel? EffectivePrevious {
            get {
                if (Previous != null)
                    return Previous;
                if (Up is { Previous.Children.Length: > 0 })
                    return Up.Previous.Children[^1];
                return Up;
            }
        }

        // note: the following gets filled in for the entire site
        public SiteConfig Site = null!;   // global site configuration
        public AllPages All = null!;      // all the pages produced so far
    }

    /// <summary>
    /// Wrapper around a single page, including its filesystem location, the page model,
    /// and the final HTML text of the page.
    /// </summary>
    [DebuggerDisplay("{"+nameof(Name)+"}")]
    public class PageResult
    {
        public FileInfo InPath = null!;   // source path of the markdown file
        public FileInfo OutPath = null!;  // target path of the HTML file in the output directory 
        public PageModel Model = null!;   // model generated for this page
        public string HtmlOutput = null!; // final text of the HTML page, after markdown conversion and templating
        public PageResult? Up;
        public PageResult? Next;
        public PageResult? Previous;
        public PageResult[]? Children;
        public string Name => OutPath.Name;
        public string LinkName => Name.Replace(' ', '_');
    }

    /// <summary>
    /// Generator state, which includes all pages processed by this generator.
    /// Depending on the stage of execution, this could contain just raw models,
    /// or models that have been converted from markdown to html, or full results
    /// including final HTML text for each page.
    /// </summary>
    public class AllPages
    {
        public List<PageResult> Pages = new List<PageResult>();
    }
}
