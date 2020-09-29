using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Onion.Core
{
    public class SyncEventArgs
    {
        public int PeersCount { get; set; }
        public int LinksCount { get; set; }
        public int UsersCount { get; set; }
    }
    public class RemovedUserEventArgs
    {
        public string[] Users { get; set; }
    }
    public class ReceivedEventArgs
    {
        public object Data { get; set; }
        public string From { get; set; }
    }
}
