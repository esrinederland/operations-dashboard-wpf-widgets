using ESRI.ArcGIS.OperationsDashboard;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ExportWidget.Objects
{
    [DataContract]
    public class ConfigSettings
    {
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public IList<DataSource> DataSources { get; set; }

        [DataMember]
        public string DataSourceId { get; set; }

        [DataMember]
        public bool Export { get; set; }

        [DataMember]
        public IList<FieldValue> SelectedFields { get; set; }

        [DataMember]
        public bool ExportAttachments { get; set; }

        [DataMember]
        public bool ExportMapImage { get; set; }

        [DataMember]
        public string PrintTaskUrl { get; set; }

        [DataMember]
        public string PrintLayout { get; set; }

        [DataMember]
        public string PrintFormat { get; set; }

        [DataMember]
        public double Scale { get; set; }

        [DataMember]
        public string ExportFolderPath { get; set; }

        [DataMember]
        public string ExportButtonText { get; set; }

        [DataMember]
        public string CancelButtonText { get; set; }
    }
}
