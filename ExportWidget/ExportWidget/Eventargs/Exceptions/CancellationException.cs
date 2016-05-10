using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportWidget.Eventargs.Exceptions
{
    public class CancellationException : Exception
    {
        public string FriendlyMessage { get; set; }

        public CancellationException(string message)
        {
            this.FriendlyMessage = message;
        }
        
    }
}
