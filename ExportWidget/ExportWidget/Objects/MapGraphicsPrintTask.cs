using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using client = ESRI.ArcGIS.Client;

namespace ExportWidget.Objects
{
    public class MapGraphicsPrintTask
    {
        public List<client.Graphic> Graphics { get; set; }

        public client.Map Map { get; set; }
    }
}
