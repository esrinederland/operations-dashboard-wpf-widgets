using ESRI.ArcGIS.Client.Printing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportWidget.Helpers
{
    public class PrintValidator
    {
        public static PrintTask PrintTask { get; set; }

        public static bool Validate(string printUrl)
        {
            if (!string.IsNullOrEmpty(printUrl))
            {
                try
                {
                    PrintTask task = new PrintTask(printUrl);

                    PrintServiceInfo info = task.GetServiceInfo();

                    PrintTask = task;

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }
    }
}
