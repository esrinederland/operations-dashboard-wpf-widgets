using ESRI.ArcGIS.OperationsDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummaryWidget.Objects
{
    public class SumData
    {
        public DataSource DataSource { get; set; }
        public ESRI.ArcGIS.Client.Field GroupByField { get; set; }
        public ESRI.ArcGIS.Client.Field SumField { get; set; }
        public string Caption { get; set; }
        public string GroupByFieldHeader { get; set; }
        public string StatisticsFieldHeader { get; set; }
    }
}
