using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportWidget.Eventargs
{
    public class LogStatusEventArgs : EventArgs
    {
        public string FriendlyMessage { get; set; }

        public string LogMessage { get; set; }
    }
}
