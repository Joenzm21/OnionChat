using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Onion.Core
{
    public enum Flags
    {
        None = 0,
        Register = 1,
        Join = 2,
        Response = 3,
        AutoSync = 4,
        Routing = 5,
        EndPoint = 6,
        ManualSync = 7,
        Link = 8,
    }
    public enum StatusCode
    {
        None = 0,
        Ok = 1,
        Already = 2,
        Error = 3,
        Loop = 4,
        Closed = 5,
        Empty = 6,
        Invaild = 7,
        Timeout = 8,
        BeDamaged = 9,
    }
}
