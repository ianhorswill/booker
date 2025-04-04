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
}
