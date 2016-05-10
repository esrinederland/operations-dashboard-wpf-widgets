using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterWidget.Objects
{
    public class SelectedField
    {
        public string FieldName { get; set; }

        public string FieldAlias { get; set; }

        public bool DependsOnSubType { get; set; }
    }
}
