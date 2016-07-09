using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading;

namespace OpenVPNMonitor
{
    class Program
    {
        private static bool keepRunning = true;
        private static AutoResetEvent waitHandle = new AutoResetEvent(false);
        private static Properties.Settings settings = Properties.Settings.Default;
        private static string pushoverUrl = "https://api.pushover.net/1/messages.json";

        static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            Console.WriteLine("Using log file: {0}", settings.LogFile);

            LogScanner scanner = new LogScanner(settings.LogFile);
            scanner.NewConnectionDetected += Scanner_NewConnectionDetected;
            scanner.Scan();

            System.Timers.Timer timer = new System.Timers.Timer(settings.ScanFrequency);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            while (keepRunning)
            {
                scanner.Scan();
                waitHandle.WaitOne();
            }

            timer.Stop();
        }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            waitHandle.Set();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            keepRunning = false;
            waitHandle.Set();
        }

        private static void Scanner_NewConnectionDetected(string commonName, IPAddress realAddress)
        {
            WebClient client = new WebClient();
            NameValueCollection values = new NameValueCollection();
            string message = string.Format("Detected new connection by {0} from {1}", commonName, realAddress.ToString());

            values.Add("token", settings.PushoverAppToken);
            values.Add("user", settings.PushoverUserKey);
            values.Add("message", message);

            client.UploadValues(pushoverUrl, "POST", values);
            Console.WriteLine(message);
        }
    }
}
