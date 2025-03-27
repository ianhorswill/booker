using Booker;
using Serilog;

var Verbose = false;
var lconfig = new LoggerConfiguration();
lconfig = Verbose ? lconfig.MinimumLevel.Debug() : lconfig.MinimumLevel.Information();
lconfig = lconfig.WriteTo.Console();
Log.Logger = lconfig.CreateLogger();

var site = @"c:\users\ianho\Documents\GitHub\DPFG";
var dir = new DirectoryInfo(site);

var config = new MiesConfig {
    SiteDirectory = dir,
    SiteConfig = new FileInfo(Path.Combine(dir.FullName, "site.yaml")),
};

var gen = new SiteGenerator(config);

var status = await gen.Execute(100);