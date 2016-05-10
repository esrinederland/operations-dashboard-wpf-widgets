using ESRI.ArcGIS.OperationsDashboard;
using ExportWidget.Enums;
using ExportWidget.Eventargs;
using ExportWidget.Managers;
using ExportWidget.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using client = ESRI.ArcGIS.Client;

namespace ExportWidget
{
    /// <summary>
    /// A Widget is a dockable add-in class for Operations Dashboard for ArcGIS that implements IWidget. By returning true from CanConfigure, 
    /// this widget provides the ability for the user to configure the widget properties showing a settings Window in the Configure method.
    /// By implementing IDataSourceConsumer, this Widget indicates it requires a DataSource to function and will be notified when the 
    /// data source is updated or removed.
    /// </summary>
    [Export("ESRI.ArcGIS.OperationsDashboard.Widget")]
    [ExportMetadata("DisplayName", "Extract Data")]
    [ExportMetadata("Description", "This widget enables end users to download the data in the Data Source to a CSV file, Geodatabase Attachments to files and map to image files")]
    [ExportMetadata("ImagePath", "/ExportWidget;component/Images/export.png")]
    [ExportMetadata("DataSourceRequired", true)]
    [KnownType(typeof(ConfigSettings))]
    [DataContract]
    public partial class Export : UserControl, IWidget, IDataSourceConsumer
    {
        private const string DELIMITER = ";";

        private string _caption = "Default Caption";

        #region Properties

        private LogManager LogManager { get; set; }

        public DataSource DataSource { get; set; }

        private string FolderName { get; set; }

        private string Dir { get; set; }

        private bool CancelPressed { get; set; }

        private int NumberOfTasksComplete
        {
            get
            {
                return this.TaskStatusses.Where(t => t.IsComplete).Count();
            }
        }

        private List<ExportWidget.Objects.TaskStatus> TaskStatusses { get; set; }

        private IList<ITask> _observers { get; set; }
        private IList<ITask> Observers
        {
            get
            {
                if (_observers == null)
                {
                    _observers = new List<ITask>();
                }
                return _observers;
            }
        }

        private IList<ExportTask> _tasks { get; set; }
        private IList<ExportTask> Tasks
        {
            get
            {
                if (_tasks == null)
                {
                    _tasks = new List<ExportTask>();

                    if (this.ConfigSettings.Export)
                    {
                        _tasks.Add(ExportTask.Export);
                    }

                    if (this.ConfigSettings.ExportAttachments)
                    {
                        _tasks.Add(ExportTask.Attachment);
                    }

                    if (this.ConfigSettings.ExportMapImage)
                    {
                        _tasks.Add(ExportTask.MapImage);
                    }
                }
                return _tasks;
            }
        }

        #endregion

        #region DataMembers
        /// <summary>
        /// A unique identifier of a data source in the configuration. This property is set during widget configuration.
        /// </summary>
        [DataMember(Name = "dataSourceId")]
        public string DataSourceId { get; set; }

        [DataMember(Name = "configSettings")]
        public ConfigSettings ConfigSettings { get; set; }

        /// <summary>
        /// The text that is displayed in the widget's containing window title bar. This property is set during widget configuration.
        /// </summary>
        [DataMember(Name = "caption")]
        public string Caption
        {
            get
            {
                return _caption;
            }

            set
            {
                if (value != _caption)
                {
                    _caption = value;
                }
            }
        }

        /// <summary>
        /// The unique identifier of the widget, set by the application when the widget is added to the configuration.
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        #endregion

        #region IWidget Members

        /// <summary>
        /// OnActivated is called when the widget is first added to the configuration, or when loading from a saved configuration, after all 
        /// widgets have been restored. Saved properties can be retrieved, including properties from other widgets.
        /// Note that some widgets may have properties which are set asynchronously and are not yet available.
        /// </summary>
        public void OnActivated()
        {
            UpdateControls();
        }

        /// <summary>
        ///  OnDeactivated is called before the widget is removed from the configuration.
        /// </summary>
        public void OnDeactivated()
        {
        }

        /// <summary>
        ///  Determines if the Configure method is called after the widget is created, before it is added to the configuration. Provides an opportunity to gather user-defined settings.
        /// </summary>
        /// <value>Return true if the Configure method should be called, otherwise return false.</value>
        public bool CanConfigure
        {
            get { return true; }
        }

        /// <summary>
        ///  Provides functionality for the widget to be configured by the end user through a dialog.
        /// </summary>
        /// <param name="owner">The application window which should be the owner of the dialog.</param>
        /// <param name="dataSources">The complete list of DataSources in the configuration.</param>
        /// <returns>True if the user clicks ok, otherwise false.</returns>
        public bool Configure(Window owner, IList<DataSource> dataSources)
        {
            ConfigSettings settings = null;
            if (this.ConfigSettings != null)
            {
                settings = new ConfigSettings()
                {
                    Title = this.Caption,
                    DataSources = dataSources,
                    DataSourceId = this.DataSourceId,
                    Export = ConfigSettings.Export,
                    SelectedFields = ConfigSettings.SelectedFields,
                    ExportAttachments = ConfigSettings.ExportAttachments,
                    ExportMapImage = ConfigSettings.ExportMapImage,
                    PrintTaskUrl = ConfigSettings.PrintTaskUrl,
                    PrintLayout = ConfigSettings.PrintLayout,
                    PrintFormat = ConfigSettings.PrintFormat,
                    Scale = ConfigSettings.Scale,
                    ExportFolderPath = ConfigSettings.ExportFolderPath,
                    ExportButtonText = ConfigSettings.ExportButtonText,
                    CancelButtonText = ConfigSettings.CancelButtonText
                };
            }

            // Show the configuration dialog.
            Config.ExportDialog dialog = new Config.ExportDialog(settings) { Owner = owner };
            if (dialog.ShowDialog() != true)
                return false;

            // Retrieve the selected values for the properties from the configuration dialog.
            this.Caption = dialog.ConfigSettings.Title;
            this.DataSourceId = dialog.ConfigSettings.DataSourceId;
            this.ConfigSettings = dialog.ConfigSettings;

            // The default UI simply shows the values of the configured properties.
            UpdateControls();

            return true;
        }

        #endregion

        #region IDataSourceConsumer Members

        /// <summary>
        /// Returns the ID(s) of the data source(s) consumed by the widget.
        /// </summary>
        public string[] DataSourceIds
        {
            get { return new string[] { this.ConfigSettings.DataSourceId }; }
        }

        /// <summary>
        /// Called when a DataSource is removed from the configuration. 
        /// </summary>
        /// <param name="dataSource">The DataSource being removed.</param>
        public void OnRemove(DataSource dataSource)
        {
            // Respond to data source being removed.
            this.ConfigSettings.DataSourceId = null;
        }

        /// <summary>
        /// Called when a DataSource found in the DataSourceIds property is updated.
        /// </summary>
        /// <param name="dataSource">The DataSource being updated.</param>
        public void OnRefresh(DataSource dataSource)
        {
            // If required, respond to the update from the selected data source.
            // Consider using an async method.
            if (dataSource != null)
            {
                if (!dataSource.IsBroken)
                {
                    this.DataSource = dataSource;
                }
            }
        }

        #endregion

        public Export()
        {
            InitializeComponent();
        }

        private void UpdateControls()
        {
            this._tasks = null;

            this.TxtConsole.Text = string.Empty;

            this.ExportProgress.Value = 0;

            BtnExport.IsEnabled = true;

            if (!string.IsNullOrEmpty(this.ConfigSettings.ExportButtonText))
            {
                BtnExport.Content = this.ConfigSettings.ExportButtonText;
            }

            if (!string.IsNullOrEmpty(this.ConfigSettings.CancelButtonText))
            {
                BtnCancel.Content = this.ConfigSettings.CancelButtonText;
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(this.ConfigSettings.ExportFolderPath))
                {
                    this.FolderName = string.Format("Export_{0}", this.GetFormattedTimestamp());

                    this.Dir = string.Format("{0}\\{1}", ConfigSettings.ExportFolderPath, this.FolderName);

                    if (!Directory.Exists(this.Dir))
                        Directory.CreateDirectory(this.Dir);
                }

                if (this.Dir != null && Directory.Exists(this.Dir))
                {
                    this.BtnExport.IsEnabled = false;

                    this.TxtConsole.Text = string.Empty;

                    this.TaskStatusses = new List<Objects.TaskStatus>();

                    this.ExportProgress.Value = 0;

                    this.LogManager = new LogManager(this.Dir);

                    this.LogManager.UpdateLog(string.Format("Started with exporting to {0}.", this.Dir));

                    this.TxtConsole.Text = "Started with exporting";

                    this.LogManager.UpdateLog("Checking portal availability.");

                    this.TxtConsole.Text = "Checking portal availability";

                    await this.CheckPortalAvailability();

                    this.Observers.Clear();

                    foreach (ExportTask task in this.Tasks)
                    {
                        this.ExecuteTask(task);
                    }

                    this.BtnCancel.IsEnabled = true;
                }
                else
                {
                    string msg = "Export could not be started. Directory to store files could not be found. Please re-configure the widget with an existing directory.";
                    this.LogManager.UpdateLog(msg);
                    this.TxtConsole.Text = msg;
                }
            }
            catch (ApplicationException ex)
            {
                HandleException(ex.Message, ex.Message);
            }
            catch (Exception ex)
            {
                string friendlyMessage = "An error occurred. The export has been canceled. See the log file for more information";
                HandleException(ex.Message, friendlyMessage);
            }
        }

        private void HandleException(string message, string friendlymessage)
        {
            this.LogManager.UpdateLog(string.Format("An error occurred. The export has been canceled. Exception: {0}", message));

            this.TxtConsole.Text = friendlymessage;

            if (TaskStatusses != null)
            {
                foreach (var taskStatus in this.TaskStatusses)
                {
                    taskStatus.IsComplete = true;
                }
            }

            bool hasErrors = true;
            this.ToggleButtonAvailability(hasErrors);
        }

        private async Task CheckPortalAvailability()
        {
            try
            {
                client.Portal.ArcGISPortal portal = new client.Portal.ArcGISPortal();

                portal = await portal.InitializeTaskAsync();
            }
            catch (Exception e)
            {
                this.HandleException(e.Message, "The portal is not accessible. Please check your portal availability or connection.");
            }
        }

        public void ExecuteTask(ExportTask taskName)
        {
            ITask task = null;

            ExportWidget.Objects.TaskStatus status = new Objects.TaskStatus()
            {
                ExportTask = taskName,
                IsComplete = false
            };
            this.TaskStatusses.Add(status);

            switch (taskName)
            {
                case ExportTask.Export:
                    task = new CsvManager(this.DataSource, this.ConfigSettings, this.Dir);
                    break;
                case ExportTask.Attachment:
                    task = new AttachmentManager(this.DataSource, this.ConfigSettings, this.Dir);
                    break;
                case ExportTask.MapImage:
                    task = new MapImageManager(this.DataSource, this.ConfigSettings, this.Dir);
                    break;
                default:
                    throw new ApplicationException("Unknown exporttask");
            }

            task.LogStatusChanged += new LogStatusChangedEventHandler(LogStatusChanged);
            task.ProcessStatusChanged += new ProcessStatusChangedEventHandler(ProcessStatusChanged);
            task.PartCompleted += new PartCompletedEventHandler(PartCompleted);

            this.Observers.Add(task);

            task.ExecuteTask();
        }

        private void ProcessStatusChanged(object sender, ProcessStatusEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                ExportWidget.Objects.TaskStatus taskStatus = this.TaskStatusses.Single(t => t.ExportTask == e.TaskStatus.ExportTask);

                taskStatus.IsComplete = e.TaskStatus.IsComplete;

                bool hasErrors = false;

                this.ToggleButtonAvailability(hasErrors);
            }));
        }

        private void LogStatusChanged(object sender, LogStatusEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.LogManager.UpdateLog(e.LogMessage);

                this.TxtConsole.Text = string.Format("{0} - {1}", DateTime.Now, e.FriendlyMessage);
            }));
        }

        private void PartCompleted(object sender, PartCompletedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                double shareInProgress = (100 / this.Tasks.Count());

                double value = shareInProgress / e.TotalNumberOfParts;

                ExportProgress.Value += value;
            }));
        }

        private void ToggleButtonAvailability(bool hasErrors)
        {
            if (this.NumberOfTasksComplete >= this.Tasks.Count())
            {
                this.BtnExport.IsEnabled = true;

                this.BtnCancel.IsEnabled = false;

                if (!hasErrors && !CancelPressed)
                {
                    this.LogManager.UpdateLog(string.Format("All processes completed. Available at {0}", this.Dir));

                    this.TxtConsole.Text = "All processes completed.";
                }
                else
                {
                    this.LogManager.UpdateLog(string.Format("Processes finished. Not all files were downloaded. Available at {0}", this.Dir));

                    this.TxtConsole.Text = "Processes finished. Not all files were downloaded.";
                }

                this.ExportProgress.Value = this.ExportProgress.Maximum;
            }
            else
            {
                if (!hasErrors & !CancelPressed)
                {
                    this.LogManager.UpdateLog("Continuing exporting data...");

                    this.TxtConsole.Text = "Continuing exporting data...";
                }

                this.BtnExport.IsEnabled = false;

                this.BtnCancel.IsEnabled = true;
            }
        }

        private string GetFormattedTimestamp()
        {
            DateTime time = DateTime.Now;

            return string.Format("{0}{1}{2}_{3}{4}{5}",
                time.Year,
                time.Month,
                time.Day,
                time.Hour,
                time.Minute,
                time.Second);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            BtnCancel.IsEnabled = false;

            foreach (ITask task in Observers)
            {
                task.Update();
            }

            this.CancelPressed = true;
        }
    }
}
