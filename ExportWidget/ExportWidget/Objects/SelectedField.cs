using ESRI.ArcGIS.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ExportWidget.Objects
{
    [DataContract]
    public class SelectedField
    {
        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public string FieldAlias { get; set; }

        [DataMember]
        public bool DependsOnSubType { get; set; }

        [DataMember]
        public Field Field { get; set; }
    }
}
