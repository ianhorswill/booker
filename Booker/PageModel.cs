﻿using System.Diagnostics;

namespace Booker;

/// <summary>
/// Per-page metadata. Contains a combination of per-page metadata, as well as
/// references to broad generator state (e.g. site config, pages produced so far)
/// so that page templates can make use of it if desired.
/// </summary>
[DebuggerDisplay("{"+nameof(PageTitle)+"}")]
public class PageModel
{
    /// <summary>
    /// Sequence number string for page: chapter.section.subsection.etc., e.g. "2.1.7"
    /// Generated automatically.  Is empty string for top-level table.
    /// </summary>
    public string SequenceNumber = "";

    /// <summary>
    /// Title of the page, shown in the browser title bar, metadata, and in inbound links
    /// Read from yaml header
    /// </summary>
    public string PageTitle = null!;

    public string? ShortTitle = null;

    public string EffectiveShortTitle => ShortTitle ?? PageTitle;

    /// <summary>
    /// One-line description, used in ToC as well as metadata
    /// Read from Yaml header
    /// </summary>
    public string PageDesc = null!;

    /// <summary>
    /// Timestamp for page, if specified in yaml header
    /// </summary>
    public DateTime? Date;

    /// <summary>
    /// If set to true in yaml header, ignore this page when publishing
    /// </summary>
    public bool IsDraft;

    /// <summary>
    /// If set to true in yaml header, this page will be generated last, so it can be used to make the index.
    /// </summary>
    public bool IsIndex;

    /// <summary>
    /// .cshtml file to use for generating html for this page
    /// Set in yaml header or defaulted.
    /// </summary>
    public string Template = null!;   // which .cshtml file to use for this page

    /// <summary>
    /// Markdown code read from .md file
    /// </summary>
    public string Markdown = null!;

    /// <summary>
    /// HTML code generated from Markdown, but before running through Template.
    /// </summary>
    public string Contents = null!;

    /// <summary>
    /// Name of the target HTML file, so for foo.md this would be foo.html.
    /// </summary>
    public string PageLink = null!;

    public PageModel? NextSibling;
    public PageModel? PreviousSibling;
    public PageModel? Parent;
    public PageModel[]? Children;

    public PageModel? Up => Parent;

    public PageModel? EffectiveNext {
        get {
            if (Children != null && Children.Length > 0)
                return Children[0];
            if (NextSibling != null)
                return NextSibling;
            // We're the last leaf under somebody
            var n = Parent;
            while (n is { NextSibling: null })
                n = n.Parent;
            return n?.NextSibling;
        }
    }

    public PageModel? EffectivePrevious {
        get {
            if (PreviousSibling != null)
                return PreviousSibling;
            if (Parent is { PreviousSibling.Children.Length: > 0 })
                return Parent.PreviousSibling.Children[^1];
            return Parent;
        }
    }

    /// <summary>
    /// Generates ancestor chain of this page in reverse order (i.e. top-level first)
    /// </summary>
    public IEnumerable<PageModel> Ancestors {
        get {
            var stack = new Stack<PageModel>();
            for (var p = Up; p != null; p = p.Parent)
                stack.Push(p);
            while (stack.Count > 0)
                yield return stack.Pop();
        }
    }

    /// <summary>
    /// Generates ancestor chain of this page in reverse order, omitting the root
    /// </summary>
    public IEnumerable<PageModel> AncestorsNoTop {
        get {
            var stack = new Stack<PageModel>();
            for (var p = Up; p is { Up: not null }; p = p.Parent)
                stack.Push(p);
            while (stack.Count > 0)
                yield return stack.Pop();
        }
    }

    // note: the following gets filled in for the entire site
    public SiteConfig Site = null!;   // global site configuration
    public AllPages All = null!;      // all the pages produced so far
}