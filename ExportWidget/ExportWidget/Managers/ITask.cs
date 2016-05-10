using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportWidget.Managers
{
    public interface ITask
    {
        void ExecuteTask();

        void Update();

        event LogStatusChangedEventHandler LogStatusChanged;

        event ProcessStatusChangedEventHandler ProcessStatusChanged;

        event PartCompletedEventHandler PartCompleted;
    }
}
