using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExportWidget.Objects;

namespace ExportWidget.Eventargs
{
    public class ProcessStatusEventArgs : EventArgs
    {
        //public int NumberToIncrement { get; set; }

        public ExportWidget.Objects.TaskStatus TaskStatus { get; set; }
    }
}
