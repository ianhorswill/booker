using System.Diagnostics;

namespace Booker;

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