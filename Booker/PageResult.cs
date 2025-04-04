﻿using System.Diagnostics;

namespace Booker;

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