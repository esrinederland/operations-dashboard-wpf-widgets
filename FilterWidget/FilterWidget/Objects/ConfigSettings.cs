using ESRI.ArcGIS.OperationsDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterWidget.Objects
{
    public class ConfigSettings
    {
        public IList<DataSource> DataSources { get; set; }

        public string InitialCaption { get; set; }
        
        public string InitialClearButtonContent { get; set; }

        public string InitialOkButtonContent { get; set; }

        public string InitialDataSourceId { get; set; }

        public string[] InitialSelectedFields { get; set; }
    }
}
