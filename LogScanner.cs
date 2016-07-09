using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace OpenVPNMonitor
{
    class LogScanner
    {
        private const string CONNECTION_PATTERN = @"(?<CommonName>.+?),(?<RealAddress>(?:[0-9]{1,3}\.){3}[0-9]{1,3}):(?<Port>[0-9]+),(?<BytesReceived>.+?),(?<BytesSent>.+?),(?<ConnectedSince>.+)";

        private Dictionary<string, List<IPAddress>> connections;
        private string logFileName;

        public delegate void NewConnectionHandler(string commonName, IPAddress realAddress);
        public event NewConnectionHandler NewConnectionDetected;

        public LogScanner(string logFileName)
        {
            this.logFileName = logFileName;
            connections = new Dictionary<string, List<IPAddress>>();
        }

        public void Scan()
        {
            string data = File.ReadAllText(logFileName);
            Regex regex = new Regex(CONNECTION_PATTERN);
            MatchCollection matches = regex.Matches(data);

            for (int i = 0; i < matches.Count; i++)
            {
                string commonName = matches[i].Groups["CommonName"].Value;
                IPAddress realAddress = IPAddress.Parse(matches[i].Groups["RealAddress"].Value);

                if (connections.Keys.Contains(commonName))
                {
                    List<IPAddress> addresses = connections[commonName];
                    if (addresses.Count(ip => ip.Equals(realAddress)) == 0)
                    {
                        addresses.Add(realAddress);
                        NewConnectionDetected?.Invoke(commonName, realAddress);
                    }
                }
                else
                {
                    List<IPAddress> addresses = new List<IPAddress>();
                    addresses.Add(realAddress);
                    connections.Add(commonName, addresses);
                    NewConnectionDetected?.Invoke(commonName, realAddress);
                }

            }
        }
    }
}
