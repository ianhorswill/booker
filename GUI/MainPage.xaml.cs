﻿using System.Collections.ObjectModel;
using Booker;
using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.Logging.EventSource;
using Serilog.Events;

namespace GUI
{
    public partial class MainPage
    {
        public static MainPage Singleton = null!;

        public MainPage () {
            //LogEvents.Add(new LogEventWrapper(null));
            InitializeComponent();
            EventLog.ItemsSource = LogEvents;
            var path = Preferences.Get(SitePathKey, "");
            SitePath.Text = path;
            //if (Directory.Exists(path))
            //    StartSite(path);
            Singleton = this;
            SiteGenerator.OnRebuild += () => Application.Current!.Dispatcher.Dispatch(ClearLog);
        }

        private const string SitePathKey = "SitePath";

        private void StartButton_OnClicked (object? sender, EventArgs e) {
            StartButton.IsEnabled = false;
            StartSite(SitePath.Text);
        }

        public async void StartSite (string site) {
            if (Preferences.Get(SitePathKey, "") != site)
                Preferences.Set(SitePathKey, site);

            var dir = new DirectoryInfo(site);

            var config = new MiesConfig {
                SiteDirectory = dir,
                SiteConfig = new FileInfo(Path.Combine(dir.FullName, "site.yaml")),
            };

            var gen = new SiteGenerator(config);

            await gen.Execute(100);
        }

        private void ClearLog() => LogEvents.Clear();

        public ObservableCollection<LogEventWrapper> LogEvents { get; } = new();

        public static readonly BindableProperty LogEventsProperty = BindableProperty.Create(
            nameof(LogEvents),
            typeof(List<LogEventWrapper>),
            typeof(MainPage));

        public static void LogMessage (LogEvent e) =>
            Application.Current!.Dispatcher.Dispatch(() => { Singleton.LogMessageInternal(e); });

        private void LogMessageInternal(LogEvent e)
        {
            LogEvents.Add(new LogEventWrapper(e));
            if (e.Level >= LogEventLevel.Warning) {
#if WINDOWS
// Bring window to front.  There has to be a better way than this
Microsoft.UI.Xaml.Window window = (Microsoft.UI.Xaml.Window)Application.Current!.Windows.First().Handler!.PlatformView!;
IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
((appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter)!).IsAlwaysOnTop = true;
((appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter)!).IsAlwaysOnTop = false;
#endif
            }
        }

        /// <summary>
        /// Renumber children of a folder in increments of 10
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RenumberButton_OnClicked (object? sender, EventArgs e) {
            var result = await FolderPicker.Default.PickAsync(SitePath.Text);
            if (result.Folder == null)
                return;
            var path = result.Folder.Path;
            var children = Directory.GetFileSystemEntries(path);
            Array.Sort(children);
            if (Path.GetFileName(children[0]) != "0.md") {
                // Something wrong...
                return;
            }

            var big = children.Length >= 10;
            for (var i = 1; i < children.Length; i++) {
                string childPath = children[i];
                var name = Path.GetFileName(childPath);
                var realName = name.Substring(name.IndexOf(' ')+1);
                var sequenceNumber = big ? $"{i * 10:D3}" : $"{i * 10:D2}";
                var newPath = Path.Combine(path,$"{sequenceNumber} {realName}");
                if (Directory.Exists(childPath))
                    Directory.Move(childPath, newPath);
                else
                    File.Move(childPath,newPath);
            }
        }
    }

}
