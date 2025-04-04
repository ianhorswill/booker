namespace Booker;

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