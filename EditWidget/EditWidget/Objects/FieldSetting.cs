using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace EditWidget.Objects
{
    [DataContract]
    public class FieldSetting
    {
        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public bool IsReadOnly { get; set; }

        [DataMember]
        public bool IsVisible { get; set; }
    }
}
