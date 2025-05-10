using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using RazorLight;
using Serilog;

namespace Booker
{
    public class SiteGenerator
    {
        public const string SourceExtension = ".md";
        public readonly MiesConfig MiesConfig;
        public readonly SiteConfig SiteConfig;
        public readonly ThemeConfig ThemeConfig;
        private string? sourceDirectoryPath;

        public readonly MarkdownPipeline Pipeline;
        public readonly RazorLightEngine Engine;

        private Dictionary<string, string> pageNames = new();

        public int MaxPages = 100;

        public SiteGenerator (MiesConfig config) {
            Log.Information("Initializing site: " + config.SiteDirectory);
            Log.Information("Loading site file: " + config.SiteConfig);

            MiesConfig = config;
            SiteConfig = LoadSiteConfig(config);
            ThemeConfig = LoadThemeConfig(FindThemeConfig(SiteConfig));

            var templatesDir = GetThemeDirectory(ThemeConfig.TemplatesDir);
            Pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseSmartyPants()
                .UseEmojiAndSmiley()
                .UseYamlFrontMatter().Build();

            Engine = new RazorLightEngineBuilder()
                .UseFileSystemProject(templatesDir.FullName)
                .UseMemoryCachingProvider().Build();
        }

        private FileInfo FindThemeConfig (SiteConfig siteConfig) {
            var theme = siteConfig.ThemeFile;
            var path = Path.Combine(MiesConfig.SiteDirectory.FullName, theme);
            return new FileInfo(path);
        }

        /// <summary>
        /// Entry point for the CLI
        /// </summary>
        public async Task<int> Execute (int max) {
            await ProcessPages(max);
            await MonitorChanges();
            return 0;
        }

        private bool rebuilding;
        private async Task MonitorChanges () {

            void MaybeRebuild (object sender, FileSystemEventArgs e) {
                lock (this) {
                    if (rebuilding)
                        return;
                    rebuilding = true;
                }
                Task.Run(Rebuild);
            }

            using var watcher = new FileSystemWatcher(MiesConfig.SiteDirectory.FullName);

            watcher.NotifyFilter = NotifyFilters.Attributes
                                   | NotifyFilters.CreationTime
                                   | NotifyFilters.DirectoryName
                                   | NotifyFilters.FileName
                                   | NotifyFilters.LastAccess
                                   | NotifyFilters.LastWrite
                                   | NotifyFilters.Security
                                   | NotifyFilters.Size;

            watcher.Changed += MaybeRebuild;
            watcher.Created += MaybeRebuild;
            watcher.Deleted += MaybeRebuild;
            watcher.Renamed += MaybeRebuild;

            watcher.Filter = "*"+SourceExtension;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            await Task.Delay(int.MaxValue);
        }

        public static event Action? OnRebuild;

        private async Task Rebuild () {
            OnRebuild?.Invoke();

            Console.WriteLine();
            Log.Information("Rebuilding");
            try {
                await ProcessPages(MaxPages);
                Log.Information("Rebuilding complete");
            } catch (Exception e) {
                Log.Error(e.Message);
                Log.Information("Rebuild aborted");
            }
            rebuilding = false;
        }

        private DirectoryInfo GetSiteDirectory (string name) =>
            new DirectoryInfo(Path.Combine(SiteConfig.ConfigFile.DirectoryName!, name));

        private DirectoryInfo GetThemeDirectory (string name) =>
            new DirectoryInfo(Path.Combine(ThemeConfig.ConfigFile.DirectoryName!, name));


        private void CheckExistence (DirectoryInfo info, string debugName) {
            if (info.Exists) return;
            throw new DirectoryNotFoundException($"{debugName} not found: {info.FullName}");
        }

        private void CheckExistence (FileInfo info, string debugName) {
            if (info.Exists) return;
            throw new FileNotFoundException($"{debugName} not found: {info.FullName}");
        }



        private SiteConfig LoadSiteConfig (MiesConfig config) {
            CheckExistence(config.SiteDirectory, "Site directory");
            CheckExistence(config.SiteConfig, "Site config file");

            var contents = File.ReadAllText(config.SiteConfig.FullName);
            var result = YamlUtils.ParseYaml<SiteConfig>(config.SiteConfig, contents);
            result.ConfigFile = config.SiteConfig;
            return result;
        }

        private ThemeConfig LoadThemeConfig (FileInfo themeYaml) {
            CheckExistence(themeYaml, "Theme config file");

            var contents = File.ReadAllText(themeYaml.FullName);
            var result = YamlUtils.ParseYaml<ThemeConfig>(themeYaml, contents);
            result.ConfigFile = themeYaml;
            return result;
        }

        //
        // page loading and generation
        //
        public async Task<AllPages> ProcessPages (int max) {
            var rawfiles = GetThemeDirectory(ThemeConfig.RawFilesDir);
            var inputs = GetSiteDirectory(SiteConfig.PagesDir);
            sourceDirectoryPath = inputs.FullName;
            var outputs = GetSiteDirectory(SiteConfig.OutputsDir);

            Log.Debug($"  Theme: {ThemeConfig.ConfigFile.FullName}");
            Log.Debug($"  Input directory: {inputs.FullName}");
            Log.Debug($"  Output directory: {outputs.FullName}");

            (PageResult root, IEnumerable<PageResult> subTree) LoadDirectory (string path, string sequenceNumber) {
                var entries = Directory.GetFiles(path).Where(p => Equals(Path.GetExtension(p), SourceExtension))
                    .Concat(Directory.GetDirectories(path)).ToArray();
                Array.Sort(entries);
                if (entries.Length == 0 || !Equals(Path.GetFileName(entries[0]), "0.md"))
                    throw new FileNotFoundException($"Directory {path} contains no 0.md file");
                var root = PrepareEmptyPageResult(new FileInfo(entries[0]), outputs);
#if SEQUENCE_NUMBERS
                root.SequenceNumber = sequenceNumber;
#endif
                IEnumerable<PageResult> subtree = new[] { root };

                // Read children
                var children = new PageResult[entries.Length - 1];
                for (var i = 1; i < entries.Length; i++) {
                    var p = entries[i];
                    PageResult page;
                    var mySequenceNumber = $"{sequenceNumber}.{i}";
                    if (Equals(Path.GetExtension(p), ".md")) {
                        page = PrepareEmptyPageResult(new FileInfo(p), outputs);
#if SEQUENCE_NUMBERS
                        page.SequenceNumber = $"{sequenceNumber}.{i}";
#endif
                    }
                    else {
                        var result = LoadDirectory(p, mySequenceNumber);
                        page = result.root;
#if SEQUENCE_NUMBERS
                        page.SequenceNumber = sequenceNumber;
#endif
                        subtree = subtree.Concat(result.subTree);
                    }
                    subtree = subtree.Append(page);
                    children[i - 1] = page;
                    page.Up = root;
                }

                for (var i = 0; i < children.Length; i++) {
                    var child = children[i];
                    if (i > 0)
                        child.Previous = children[i - 1];
                    if (i < children.Length - 1)
                        child.Next = children[i + 1];
                }

                root.Children = children;

                return (root, subtree);
            }

            var everything = LoadDirectory(inputs.FullName, "");

            var pages = everything.subTree.Distinct().ToList();

            var all = new AllPages() { Pages = pages };

            pageNames = new(pages.Select(p => new KeyValuePair<string, string>(
                Path.GetFileNameWithoutExtension(p.OutPath.Name.ToLower().Replace(' ','_')),
                p.LinkName)));

            // Load the pages
            Log.Information($"Loading {all.Pages.Count} markdown pages...");
            foreach (var page in all.Pages) LoadPage(page, all);

            // Link the pages together
            foreach (var page in all.Pages) {
                var model = page.Model;
                model.Parent = page.Up?.Model;
                model.NextSibling = page.Next?.Model;
                model.PreviousSibling = page.Previous?.Model;
#if SEQUENCE_NUMBERS
                model.SequenceNumber = page.SequenceNumber;
#endif
                if (page.Children != null)
                    model.Children = page.Children.Select(p => p.Model).ToArray();
            }

            RemoveDrafts(all.Pages);
            MoveIndexPagesToEnd(all.Pages);

            Log.Information("Converting pages to HTML...");
            foreach (var page in all.Pages) await RenderPage(page);

            Log.Information("Preparing the output directory...");
            ClearOutOutputDirectory(outputs);
            CopyRawFiles(rawfiles, outputs);
            CopyMediaFiles(inputs.FullName, outputs.FullName);

            Log.Information("Writing HTML pages to disk...");
            foreach (var page in all.Pages) WritePage(page);

            Log.Information($"Done. Processed {all.Pages.Count} pages.");
            return all;
        }

        public static readonly string[] MediaExtensions = { ".jpg", ".jpeg", ".png" };

        private void CopyMediaFiles (string inputs, string outputs) {
            foreach (var file in Directory.GetFiles(inputs))
                if (HasMediaFileExtension(file))
                    File.Copy(file, Path.Combine(outputs, Path.GetFileName(file)));

            foreach (var sub in Directory.GetDirectories(inputs))
                CopyMediaFiles(sub, outputs);
        }

        private static bool HasMediaFileExtension(string file) => MediaExtensions.Contains(Path.GetExtension(file));


        /// <summary>
        /// Each empty result contains info about the page's filesystem paths, and will be filled in later
        /// </summary>
        private PageResult PrepareEmptyPageResult (FileInfo inpath, DirectoryInfo outputs) {
            var dir = inpath.DirectoryName;
            var name = inpath.Name;
            if (name == "0.md") {
                
                if (dir == sourceDirectoryPath)
                    name = "index.md";
                else
                    name = Path.GetFileName(dir)!;
            }
            if (char.IsDigit(name[0]))
                name = name.Substring(name.IndexOf(' ') + 1);
            var outfile = Path.ChangeExtension(name, ".html");
            var outpath = new FileInfo(Path.Combine(outputs.FullName, outfile));
            return new PageResult { InPath = inpath, OutPath = outpath };
        }

        /// <summary>
        /// Remove any draft pages from the list, so we don't render them at all.
        /// </summary>
        private void RemoveDrafts (List<PageResult> results) {
            for (int i = results.Count - 1; i >= 0; i--) {
                var page = results[i];
                if (page.Model.IsDraft) {
                    Log.Debug($"  Skipping draft page {page.InPath.Name}");
                    results.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// After reading all pages, but before processing them, let's find all index pages and move them
        /// to the end, so that they will be processed last (so they can access the entire list of
        /// pages processed before them, and do with them whatever they want).
        /// </summary>
        private void MoveIndexPagesToEnd (List<PageResult> results) {
            var indices = results.Where(p => p.Model.IsIndex).ToList();
            var others = results.Where(p => !p.Model.IsIndex).ToList();

            results.Clear();
            results.AddRange(others);
            results.AddRange(indices);
        }

        /// <summary>
        /// Destroy and recreate the output directory. 
        /// </summary>
        private void ClearOutOutputDirectory (DirectoryInfo outdir) {
            // just use the string path, we don't want to hold on to dirinfo that's being deleted
            string path = outdir.FullName;
            
            if (Directory.Exists(path)) {
                Log.Information($"Deleting output directory {path}");
                string temp = $"{path}_temp_{DateTime.Now.Ticks.ToString()}";
                Log.Debug($"Renaming {path} => {temp}");
                Directory.Move(path, temp);
                Log.Debug($"Deleting {temp}");
                Directory.Delete(temp, true);
            }

            Log.Information($"Creating output directory {path}");
            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Copy all the raw web files from the source directory into output
        /// </summary>
        private void CopyRawFiles (DirectoryInfo raw, DirectoryInfo output) {
            var count = CopyRawFilesHelper(raw, output);
            Log.Debug($"  Copied {count} files from raw files directory {raw.FullName}");
        }

        private int CopyRawFilesHelper (DirectoryInfo sourceDir, DirectoryInfo targetDir) {
            int count = 0;

            foreach (FileInfo sourceFile in sourceDir.GetFiles()) {
                var targetFileName = Path.Combine(targetDir.FullName, sourceFile.Name);
                File.Copy(sourceFile.FullName, targetFileName);
                count++;
            }

            foreach (DirectoryInfo sourceSubDir in sourceDir.GetDirectories()) {
                var targetSubDir = targetDir.CreateSubdirectory(sourceSubDir.Name);
                count += CopyRawFilesHelper(sourceSubDir, targetSubDir);
            }

            return count;
        }

        /// <summary>
        /// Blocking load from the input directory, followed by conversion of page contents
        /// from markdown to HTML. 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="stats"></param>
        private void LoadPage (PageResult page, AllPages stats) {
            FileInfo inpath = page.InPath;
            Log.Debug($"  Loading page {inpath.Name}");

            var markdown = File.ReadAllText(inpath.FullName);

            var cut = markdown.IndexOf("#NoPublish", StringComparison.InvariantCultureIgnoreCase);
            if (cut >= 0)
                markdown = markdown.Substring(0, cut);
            cut = markdown.IndexOf("# NoPublish", StringComparison.InvariantCultureIgnoreCase);
            if (cut >= 0)
                markdown = markdown.Substring(0, cut);

            markdown = Regex.Replace(markdown, @"\.(""*\[\^.+\]""*|""*)  ", @".$1&ensp; ");

            var document = Markdown.Parse(markdown, Pipeline);

            // Find all the links whose URLs are names of pages and replace them with the URL for the page.
            foreach (var link in document.Descendants<LinkInline>()) {
                var target = link.Url;
                if (target!= null && !target.StartsWith("http") && !HasMediaFileExtension(target)) {
                    var hash = target.IndexOf('#');
                    var url = hash > 0 ? target.Substring(0, hash) : target;
                    var anchor = hash > 0 ? target.Substring(hash) : "";
                    if (pageNames.TryGetValue(url.ToLower(), out var p))
                        link.Url = p+anchor;
                    else {
                        var path = page.InPath.FullName.Substring(sourceDirectoryPath!.Length+1);
                        Log.Warning($"{path}: Unknown link url '{url}'");
                    }
                }
            }

            var contents = document.ToHtml(Pipeline);

            var model = page.Model = YamlUtils.ExtractYamlHeader<PageModel>(inpath, markdown);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (model.Template == null)
                model.Template = "page.cshtml";
            model.Markdown = markdown;
            model.Contents = contents;
            model.PageLink = page.LinkName;
            model.Site = SiteConfig;
            model.All = stats;
        }

        /// <summary>
        /// Renders the HTML page using a .cshtml template file and the contents from
        /// the already-transformed markdown file
        /// </summary>
        private async Task RenderPage (PageResult page) {
            try {
                Log.Debug($"  Rendering page {page.InPath.Name} => {page.OutPath.Name}");
                page.HtmlOutput = await Engine.CompileRenderAsync(page.Model.Template, page.Model);
            } catch (Exception e) {
                throw new InvalidOperationException($"Error while rendering HTML for page {page.InPath.Name}", e);
            }
        }

        /// <summary>
        /// Blocking write to the output directory
        /// </summary>
        private void WritePage (PageResult page) {
            Log.Debug($"  Writing page {page.OutPath.Name}");
            var path = Path.Combine(page.OutPath.DirectoryName!, page.LinkName);
            File.WriteAllText(path, page.HtmlOutput);
        }

    }
}
