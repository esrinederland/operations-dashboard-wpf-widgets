using ESRI.ArcGIS.Client.Printing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportWidget.Objects
{
    public class PrintContainer
    {
        public PrintTask PrintTask { get; set; }

        public PrintParameters PrintParameters { get; set; }
    }
}
