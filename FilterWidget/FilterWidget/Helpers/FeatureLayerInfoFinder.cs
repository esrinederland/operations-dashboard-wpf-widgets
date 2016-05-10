using ESRI.ArcGIS.Client.FeatureService;
using ESRI.ArcGIS.OperationsDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using client = ESRI.ArcGIS.Client;

namespace FilterWidget.Helpers
{
    public static class FeatureLayerInfoFinder
    {
        public static FeatureLayerInfo GetFeatureLayerInfo(DataSource dataSource)
        {
            MapWidget mw = MapWidget.FindMapWidget(dataSource);
            client.FeatureLayer featurelayer = mw.FindFeatureLayer(dataSource);
            FeatureLayerInfo LayerInfo = featurelayer.LayerInfo;
            return LayerInfo;
        }
    }
}
