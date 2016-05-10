using ESRI.ArcGIS.Client.FeatureService;
using ESRI.ArcGIS.OperationsDashboard;
using ExportWidget.Enums;
using ExportWidget.Eventargs.Exceptions;
using ExportWidget.Objects;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using client = ESRI.ArcGIS.Client;

namespace ExportWidget.Managers
{
    public class AttachmentManager : ManagerBase, ITask
    {
        #region Properties

        protected override ExportTask TaskType
        {
            get { return ExportTask.Attachment; }
        }

        private bool HasAttachments { get; set; }

        private DataSource DataSource { get; set; }

        private string Dir { get; set; }

        private ConfigSettings ConfigSettings { get; set; }

        private int DownloadsComplete { get; set; }

        private int NumberOfAttachments { get; set; }

        private int NumberOfResults { get; set; }

        private int NumberOfQueriesComplete { get; set; }

        private bool CancelPressed { get; set; }

        private List<WebClient> WebClients { get; set; }

        #endregion

        public AttachmentManager(DataSource dataSource, ConfigSettings settings, string directory)
        {
            this.DataSource = dataSource;

            this.ConfigSettings = settings;

            this.Dir = directory;

            this.DownloadsComplete = 0;

            this.NumberOfAttachments = 0;

            this.NumberOfResults = 0;

            this.NumberOfQueriesComplete = 0;

            this.CancelPressed = false;

            this.WebClients = new List<WebClient>();
        }

        public void ExecuteTask()
        {
            try
            {
                this.DownloadAttachments();
            }
            catch (CancellationException)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "(Attachments) Cancel pressed by user");
                base.OnProcessStatusChanged(this, this.TaskType, true);
            }
            catch (Exception ex)
            {
                base.OnLogStatusChanged(this,
                    "An error occurred in downloading the attachments. See the log file for more information.",
                    string.Format("An error occurred in downloading the attachments. Exception: {0}", ex.Message));
                base.OnProcessStatusChanged(this, this.TaskType, true);
            }
        }

        private async void DownloadAttachments()
        {
            try
            {
                if (CancelPressed)
                    throw new CancellationException("Downloading of attachments cancelled.");

                this.WebClients.Clear();

                MapWidget widget = MapWidget.FindMapWidget(this.DataSource);

                client.FeatureLayer fl = widget.FindFeatureLayer(this.DataSource);

                Query query = new Query();
                query.Fields = (this.ConfigSettings.SelectedFields.Select(s => s.FieldName)).ToArray();
                query.WhereClause = "1=1";
                query.ReturnGeometry = true;

                QueryResult results = await this.DataSource.ExecuteQueryAsync(query);

                string updateMsg = string.Format("{0} - Analyzing and downloading attachments", DateTime.Now);
                base.OnLogStatusChanged(this, updateMsg, updateMsg);

                this.NumberOfResults = results.Features.Count();

                if (results.Features.Count > 0)
                {

                    //this first attachmentquery is for the progressmonitoring
                    foreach (client.Graphic graphic in results.Features)
                    {
                        try
                        {
                            if (CancelPressed)
                                throw new CancellationException("Downloading of attachments cancelled.");

                            string objID = graphic.Attributes.Single(a => a.Key.ToLower() == "objectid").Value.ToString();

                            fl.QueryAttachmentInfos(objID, AttachmentInfoReady, QueryFailed);
                        }
                        catch (Exception e)
                        {
                            base.OnLogStatusChanged(this, "Error in downloading the attachment", string.Format("Error in downloading the attachment: {0}", e.Message));
                        }
                    }

                    //this second attachmentquery downloads the actual data
                    foreach (client.Graphic graphic in results.Features)
                    {
                        try
                        {
                            if (CancelPressed)
                                throw new CancellationException("Downloading of attachments cancelled.");

                            string objID = graphic.Attributes.Single(a => a.Key.ToLower() == "objectid").Value.ToString();

                            fl.QueryAttachmentInfos(objID, (attachments) => QueryComplete(attachments, graphic.Attributes.First().Value.ToString()), QueryFailed);
                        }
                        catch (Exception e)
                        {
                            base.OnLogStatusChanged(this, "Error in downloading the attachment", string.Format("Error in downloading the attachment: {0}", e.Message));
                        }
                    }

                }
                else
                {
                    base.OnPartCompleted(this, 1);
                    base.OnLogStatusChanged(this, "No attachments found", "No attachments found");
                    base.OnProcessStatusChanged(this, this.TaskType, true);
                }
            }
            catch (CancellationException e)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "Cancel pressed by user");
                this.QueryFailed(e);
            }
            catch (Exception e)
            {
                base.OnLogStatusChanged(this,
                    "An error occurred in downloading the attachments. See the log file for more information.",
                    string.Format("An error occurred in downloading the attachments. Exception: {0}", e.Message));
                this.QueryFailed(e);
            }
        }

        private void AttachmentInfoReady(IEnumerable<AttachmentInfo> attInfo)
        {
            try
            {
                if (CancelPressed)
                    throw new CancellationException("Downloading of attachments cancelled.");

                NumberOfAttachments = NumberOfAttachments + attInfo.Count();
            }
            catch (CancellationException e)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "Cancel pressed by user");
                this.QueryFailed(e);
            }
        }

        private void QueryComplete(IEnumerable<AttachmentInfo> attachments, string objectID)
        {
            try
            {
                this.NumberOfQueriesComplete++;

                if (CancelPressed)
                    throw new CancellationException("Downloading of attachments cancelled.");

                int totalNumber = attachments.Count();

                //this is the number of attachments that belong to a specific object. If one object has multiple attachments this number will be incremented
                int attachmentNumberByObject = 1;

                foreach (AttachmentInfo attachment in attachments)
                {
                    var tmpAttachmentNumber = attachmentNumberByObject;
                    this.HasAttachments = true;

                    string extension = this.GetExtension(attachment.ContentType);

                    WebClient client = new WebClient();
                    WebClients.Add(client);
                    
                    client.DownloadDataCompleted += (sender, args) => DownloadComplete(args, extension, tmpAttachmentNumber, totalNumber, objectID);

                    client.DownloadDataAsync(attachment.Uri);
                    attachmentNumberByObject++;
                }

                if (this.NumberOfQueriesComplete >= this.NumberOfResults
                    && !this.HasAttachments)
                {
                    base.OnPartCompleted(this, 1);
                    base.OnLogStatusChanged(this, "No attachments found", "No attachments found");
                    base.OnProcessStatusChanged(this, this.TaskType, true);
                }
            }
            catch (CancellationException e)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "Cancel pressed by user");
                this.QueryFailed(e);
            }
            catch (Exception e)
            {
                base.OnLogStatusChanged(this,
                    "An error occurred in downloading the attachments. See the log file for more information.",
                    string.Format("An error occurred in downloading the attachments. Exception: {0}", e.Message));
                this.QueryFailed(e);
            }
        }

        private void DownloadComplete(DownloadDataCompletedEventArgs args, string extension, int attachmentNumber, int totalNumber, string objectID)
        {
            try
            {
                if (CancelPressed)
                    throw new CancellationException("Downloading of attachments cancelled.");

                DownloadsComplete++;
                string filePath = string.Format("{0}\\Attachments", Dir);

                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                string fileName = string.Format("{0}\\attachment_object{1}_{2}{3}", filePath, objectID, attachmentNumber, extension);

                File.WriteAllBytes(fileName, args.Result);

                string updateMsg = string.Format("Attachment download {0}/{1} complete from object {2}.", attachmentNumber, totalNumber, objectID);
                base.OnLogStatusChanged(this, updateMsg, updateMsg);

                base.OnPartCompleted(this, this.NumberOfAttachments);

                if ((this.DownloadsComplete == this.NumberOfAttachments))
                {
                    base.OnProcessStatusChanged(this, this.TaskType, true);
                }
            }
            catch (CancellationException e)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "Cancel pressed by user");
                this.QueryFailed(e);
            }
            catch (Exception e)
            {
                base.OnLogStatusChanged(this,
                    "An error occurred in downloading the attachments. See the log file for more information.",
                    string.Format("An error occurred in downloading the attachments. Exception: {0}", e.Message));
                this.QueryFailed(e);
            }
        }

        public string GetExtension(string mimeType)
        {
            string result;
            RegistryKey key;
            object value;

            key = Registry.ClassesRoot.OpenSubKey(@"MIME\Database\Content Type\" + mimeType, false);
            value = key != null ? key.GetValue("Extension", null) : null;
            result = value != null ? value.ToString() : string.Empty;

            return result;
        }

        private void QueryFailed(Exception e)
        {
            DownloadsComplete++;

            base.OnPartCompleted(this, this.NumberOfAttachments);

            if ((this.DownloadsComplete == this.NumberOfAttachments))
            {
                base.OnProcessStatusChanged(this, this.TaskType, true);
            }
        }


        public void Update()
        {
            this.CancelPressed = true;

            foreach (WebClient c in this.WebClients)
            {
                if (c.IsBusy)
                {
                    c.CancelAsync();
                }
            }

            base.OnLogStatusChanged(this, "Downloading of attachments cancelled.", "Downloading of attachments cancelled.");
        }
    }
}
