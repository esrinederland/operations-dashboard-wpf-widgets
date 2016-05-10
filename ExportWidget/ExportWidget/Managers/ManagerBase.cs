using ExportWidget.Enums;
using ExportWidget.Eventargs;
using System;
using System.IO;

namespace ExportWidget.Managers
{
    public delegate void LogStatusChangedEventHandler(object sender, LogStatusEventArgs e);

    public delegate void ProcessStatusChangedEventHandler(object sender, ProcessStatusEventArgs e);

    public delegate void PartCompletedEventHandler(object sender, PartCompletedEventArgs e);

    public abstract class ManagerBase
    {
        public event LogStatusChangedEventHandler LogStatusChanged;

        public event ProcessStatusChangedEventHandler ProcessStatusChanged;

        public event PartCompletedEventHandler PartCompleted;

        protected abstract ExportTask TaskType { get; }

        protected virtual void OnLogStatusChanged(object sender, string friendlyMessage, string logMessage)
        {
            LogStatusEventArgs e = new LogStatusEventArgs()
            {
                FriendlyMessage = friendlyMessage,
                LogMessage = logMessage
            };

            if (LogStatusChanged != null)
            {
                LogStatusChanged(sender, e);
            }
        }

        protected virtual void OnProcessStatusChanged(object sender, ExportTask exportTask, bool isComplete)
        {
            ExportWidget.Objects.TaskStatus taskStatus = new ExportWidget.Objects.TaskStatus()
            {
                ExportTask = exportTask,
                IsComplete = isComplete
            };

            ProcessStatusEventArgs e = new ProcessStatusEventArgs()
            {
                TaskStatus = taskStatus
            };

            if (ProcessStatusChanged != null)
            {
                ProcessStatusChanged(sender, e);
            }
        }

        protected virtual void OnPartCompleted(object sender, int totalNumberOfParts)
        {
            PartCompletedEventArgs e = new PartCompletedEventArgs()
            {
                TotalNumberOfParts = totalNumberOfParts
            };

            if (PartCompleted != null)
            {
                PartCompleted(sender, e);
            }
        }

        protected string GetExtension(Uri url)
        {
            if(Path.HasExtension(url.AbsoluteUri))
            {
                return Path.GetExtension(url.AbsoluteUri);
            }
            return string.Empty;
        }
    }
}
