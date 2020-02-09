using System;
using System.IO;
using System.Security.Permissions;
using System.Linq;
using log4net;
using System.Reflection;
using log4net.Config;
using System.Reactive.Linq;
using System.Text;

public class Watcher
{
    private static log4net.ILog log;

    public static void Main()
    {
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        Run();
    }

    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    private static void Run()
    {
        string[] args = Environment.GetCommandLineArgs();

        // If a directory is not specified, exit program.
        if (args.Length != 2)
        {
            // Display the proper way to call the program.
            Console.WriteLine("Usage: Watcher.exe (directory)");
            return;
        }

        // Create a new FileSystemWatcher and set its properties.
        using (FileSystemWatcher watcher = new FileSystemWatcher())
        {
            watcher.Path = args[1];

            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            watcher.NotifyFilter = NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName;

            watcher.Filter = "*.*";

            watcher.IncludeSubdirectories = true;

            // Add event handlers.
            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;

            // Begin watching.
            watcher.EnableRaisingEvents = true;

            // Wait for the user to quit the program.
            log.Info($"Watching {args[1]}...");
            log.Info("Press 'q' to quit the sample.");
            var actionStr = "";
            while (true)
            {
                //Console.Write("> ");
                actionStr = Console.ReadLine();
                if (actionStr == "" || actionStr == "q")
                {
                    log.Info("Exiting...");
                    break;
                }
                log.Info("===============================================================");
            }

        }
    }

    private static string getRelPath(object fswObj, string path) => Path.GetRelativePath((fswObj as FileSystemWatcher).Path, path);
    private static void dumpFile(string path)
    {
        using (var f = File.Open(path, FileMode.Open, FileAccess.Read))
        {
            using (var r = new BinaryReader(f))
            {
                var bytes = r.ReadBytes((int)f.Length);
                var base64data = System.Convert.ToBase64String(bytes);
                log.Debug($"File {path} contents:\n---\n{base64data}\n\n");
            }
        }
    }

    // Define the event handlers.
    private static void OnChanged(object source, FileSystemEventArgs e)
    {
        var msg = $"{getRelPath(source, e.FullPath)} {e.ChangeType}";
        log.Info(msg);
        try
        {
            if (e.ChangeType != WatcherChangeTypes.Deleted && !File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Directory))
            {
                dumpFile(e.FullPath);
            }
        }
        catch (Exception ex)
        {
            log.Error("(err) " + ex.Message);
        }
    }

    private static void OnRenamed(object source, RenamedEventArgs e)
    {
        var msg = $"File: {getRelPath(source, e.OldFullPath)} renamed to {getRelPath(source, e.FullPath)}";
        log.Info(msg);
    }
}