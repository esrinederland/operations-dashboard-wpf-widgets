using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ExportWidget.Objects
{
    [DataContract]
    public class FieldValue : SelectedField
    {
        [DataMember]
        public bool IsSelected { get; set; }
    }
}
