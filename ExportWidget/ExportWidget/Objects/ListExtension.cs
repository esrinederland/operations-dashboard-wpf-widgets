using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportWidget.Objects
{
    public static class ListExtension
    {
        public static IList<FieldValue> IncludeObjectID(this IList<FieldValue> list)
        {
            if (!list.Select(c => c.FieldName).Contains("OBJECTID"))
            {
                FieldValue v = new FieldValue();
                v.FieldName = "OBJECTID";
                v.FieldAlias = "OBJECTID ";
                v.IsSelected = true;

                list.Insert(0, v);
            }
            return list;
        }
    }
}
