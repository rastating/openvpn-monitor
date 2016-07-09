using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading;

namespace OpenVPNMonitor
{
    class Program
    {
        private static AutoResetEvent waitHandle = new AutoResetEvent(false);
        private static Properties.Settings settings = Properties.Settings.Default;
        private static string pushoverUrl = "https://api.pushover.net/1/messages.json";
        private static LogScanner scanner;

        static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            Console.WriteLine("Using log file: {0}", settings.LogFile);

            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName(settings.LogFile);
            watcher.Filter = Path.GetFileName(settings.LogFile);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += LogFile_Changed;
            watcher.EnableRaisingEvents = true;

            scanner = new LogScanner(settings.LogFile);
            scanner.NewConnectionDetected += Scanner_NewConnectionDetected;
            scanner.Scan();

            waitHandle.WaitOne();

            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        private static void LogFile_Changed(object sender, FileSystemEventArgs e)
        {
            scanner.Scan();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            waitHandle.Set();
        }

        private static void Scanner_NewConnectionDetected(string commonName, IPAddress realAddress)
        {
            string message = string.Format("Detected new connection by {0} from {1}", commonName, realAddress.ToString());
            using (WebClient client = new WebClient())
            {
                NameValueCollection values = new NameValueCollection();

                values.Add("token", settings.PushoverAppToken);
                values.Add("user", settings.PushoverUserKey);
                values.Add("message", message);

                client.UploadValues(pushoverUrl, "POST", values);
            }

            Console.WriteLine(message);
        }
    }
}
