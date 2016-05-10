using ExportWidget.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportWidget.Objects
{
    public class TaskStatus
    {
        public ExportTask ExportTask { get; set; }

        public bool IsComplete { get; set; }
    } 
}
