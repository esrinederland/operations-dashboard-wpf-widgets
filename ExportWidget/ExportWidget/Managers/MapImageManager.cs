using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Printing;
using ESRI.ArcGIS.Client.Tasks;
using ESRI.ArcGIS.OperationsDashboard;
using ExportWidget.Eventargs.Exceptions;
using ExportWidget.Objects;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using client = ESRI.ArcGIS.Client;
using ExportWidget.Enums;
using System.Collections.Generic;

namespace ExportWidget.Managers
{
    public class MapImageManager : ManagerBase, ITask
    {
        //The current number of the file that is being downloaded for example file 6 out of 20
        private int DownloadIndex = 1;

        #region Properties

        protected override Enums.ExportTask TaskType
        {
            get { return ExportTask.MapImage; }
        }

        private DataSource DataSource { get; set; }

        private ConfigSettings ConfigSettings { get; set; }

        private string Dir { get; set; }

        private int TotalNumberOfRecords { get; set; }

        private BackgroundWorker Worker { get; set; }

        private bool CancelPressed { get; set; }

        private List<WebClient> WebClients { get; set; }

        #endregion

        public MapImageManager(DataSource dataSource, ConfigSettings settings, string directory)
        {
            this.CancelPressed = false;

            this.DataSource = dataSource;

            this.ConfigSettings = settings;

            this.Dir = directory;
        }

        public void ExecuteTask()
        {
            try
            {
                this.DownloadMapImage();
            }
            catch (CancellationException)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "(Mapimages) Cancel pressed by user");
                base.OnProcessStatusChanged(this, this.TaskType, true);
            }
            catch (Exception ex)
            {
                base.OnLogStatusChanged(this, "An error occurred in creating the mapimages. See the logfile for more information,",
                    string.Format("An error occurred in creating the mapimages. Exception: {0}", ex.Message));
                base.OnProcessStatusChanged(this, this.TaskType, true);
            }
        }

        private async void DownloadMapImage()
        {
            try
            {
                if (CancelPressed)
                    throw new CancellationException("Downloading of mapimages cancelled.");

                this.WebClients = new List<WebClient>();

                this.CreateBackgroundWorker();

                MapWidget widget = MapWidget.FindMapWidget(this.DataSource);

                ESRI.ArcGIS.OperationsDashboard.Query query = new ESRI.ArcGIS.OperationsDashboard.Query();
                query.Fields = (this.ConfigSettings.SelectedFields.Select(s => s.FieldName)).ToArray();
                query.WhereClause = "1=1";
                query.ReturnGeometry = true;

                ESRI.ArcGIS.OperationsDashboard.QueryResult results = await this.DataSource.ExecuteQueryAsync(query);

                this.TotalNumberOfRecords = results.Features.Count();

                string updateMsg = string.Format("Started creating {0} mapimages. This may take a few minutes.", this.TotalNumberOfRecords);
                base.OnLogStatusChanged(this, updateMsg, updateMsg);

                MapGraphicsPrintTask printArgs = new MapGraphicsPrintTask();
                printArgs.Graphics = results.Features.ToList();
                printArgs.Map = widget.Map;

                if (results.Features.Count() > 0)
                {
                    this.Worker.RunWorkerAsync(printArgs);
                }
                else
                {
                    base.OnLogStatusChanged(this, "No mapimages to create", "No mapimages to create");
                    base.OnProcessStatusChanged(this, this.TaskType, true);
                }
            }
            catch (CancellationException)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "(Mapimages) Cancel pressed by user");
                this.ProcessCanceled();
            }
            catch (Exception ex)
            {
                base.OnLogStatusChanged(this, "An error occurred in creating the mapimages. See the logfile for more information,",
                    string.Format("An error occurred in creating the mapimages. Exception: {0}", ex.Message));
                this.ProcessCanceled();
            }
        }

        private void CreateBackgroundWorker()
        {
            this.Worker = new BackgroundWorker();

            this.Worker.DoWork += Bw_DoWork;

            this.Worker.WorkerReportsProgress = false;

            this.Worker.WorkerSupportsCancellation = true;
        }

        private async void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                BackgroundWorker worker = sender as BackgroundWorker;

                if (CancelPressed)
                {
                    worker.CancelAsync();
                    throw new CancellationException("Downloading of mapimages cancelled.");
                }

                MapGraphicsPrintTask printArgs = e.Argument as MapGraphicsPrintTask;

                ESRI.ArcGIS.Client.Printing.PrintTask print = new PrintTask(ConfigSettings.PrintTaskUrl);

                PrintServiceInfo info = print.GetServiceInfo();

                foreach (client.Graphic graph in printArgs.Graphics)
                {
                    if (info.IsServiceAsynchronous)
                    {
                        Application.Current.Dispatcher.Invoke(() => PrintMapAsync(graph, printArgs.Map));
                    }
                    else
                    {
                        await Application.Current.Dispatcher.Invoke(() => PrintMap(graph, printArgs.Map));
                    }


                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;

                        break;
                    }
                }
            }
            catch (CancellationException)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "(Mapimages) Cancel pressed by user");
                this.ProcessCanceled();
            }
            catch (Exception ex)
            {
                base.OnLogStatusChanged(this, "An error occurred in creating the mapimages. See the logfile for more information,",
                    string.Format("An error occurred in creating the mapimages. Exception: {0}", ex.Message));
                this.ProcessCanceled();
            }
        }

        private void PrintMapAsync(client.Graphic graph, client.Map map)
        {
            try
            {
                if (CancelPressed)
                    throw new CancellationException("Downloading of mapimages cancelled.");

                PrintContainer container = this.CreatePrintContainer(graph, map);

                int objectId = Convert.ToInt32(graph.Attributes["OBJECTID"]);

                container.PrintTask.StatusUpdated += print_StatusUpdated;

                container.PrintTask.JobCompleted += (sender, args) => Print_JobCompleted(sender, args, objectId);

                container.PrintTask.SubmitJobAsync(container.PrintParameters);
            }
            catch (CancellationException)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "(Mapimages) Cancel pressed by user");
                this.ProcessCanceled();
            }
            catch (Exception ex)
            {
                base.OnLogStatusChanged(this, "An error occurred in creating the mapimages. See the logfile for more information,",
                    string.Format("An error occurred in creating the mapimages. Exception: {0}", ex.Message));
                this.ProcessCanceled();
            }
        }

        private async Task PrintMap(client.Graphic graph, client.Map map)
        {
            try
            {
                if (CancelPressed)
                    throw new CancellationException("Downloading of mapimages cancelled.");

                PrintContainer container = this.CreatePrintContainer(graph, map);

                PrintResult result = await container.PrintTask.ExecuteTaskAsync(container.PrintParameters);

                if (!CancelPressed)
                {
                    int objectID = Convert.ToInt32(graph.Attributes["OBJECTID"]);

                    this.DownloadData(result, objectID);

                    string updateMsg = string.Format("Mapimage {0} of {1} downloaded.", DownloadIndex, this.TotalNumberOfRecords);

                    base.OnLogStatusChanged(this, updateMsg, updateMsg);
                }
            }
            catch (CancellationException)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "(Mapimages) Cancel pressed by user");
                this.ProcessCanceled();
            }
            catch (Exception ex)
            {
                base.OnLogStatusChanged(this, "An error occurred in creating the mapimages. See the logfile for more information,",
                    string.Format("An error occurred in creating the mapimages. Exception: {0}", ex.Message));
                this.ProcessCanceled();
            }
        }

        private PrintContainer CreatePrintContainer(client.Graphic graph, client.Map map)
        {
            ESRI.ArcGIS.Client.Printing.PrintTask print = new PrintTask(ConfigSettings.PrintTaskUrl);

            Envelope env = graph.Geometry.Extent;

            env = new Envelope(graph.Geometry.Extent.XMin - 150,
                    graph.Geometry.Extent.YMin - 150,
                    graph.Geometry.Extent.XMax + 150,
                    graph.Geometry.Extent.YMax + 150);

            MapOptions moptions = new MapOptions(env)
            {
                OutSpatialReference = graph.Geometry.SpatialReference,
                Scale = ConfigSettings.Scale
            };

            ESRI.ArcGIS.Client.Printing.ExportOptions exportOptions = new ESRI.ArcGIS.Client.Printing.ExportOptions()
            {
                Dpi = 96,
                OutputSize = new Size(env.Width, env.Height)
            };


            //To make sure the graphics on the mapimages don't have a 'selectionglow' around them a new layercollection is created.
            client.LayerCollection collection = CreateLayerCollection(map);

            //Pass the new collection to the printparameters constructor
            PrintParameters printparams = new PrintParameters(collection, map.Extent)
            {
                MapOptions = moptions,
                Format = this.ConfigSettings.PrintFormat,
                LayoutTemplate = this.ConfigSettings.PrintLayout,
                ExportOptions = exportOptions
            };

            print.DisableClientCaching = true;

            print.UpdateDelay = 10000;

            PrintContainer container = new PrintContainer()
            {
                PrintTask = print,
                PrintParameters = printparams
            };

            return container;

        }

        private static client.LayerCollection CreateLayerCollection(client.Map map)
        {
            client.LayerCollection collection = new client.LayerCollection();

            //loop through the existing layers
            foreach (var parentLayer in map.Layers)
            {
                if (parentLayer is client.AcceleratedDisplayLayers)
                {
                    //loop throug the child layers of the accelerateddisplaylayer
                    client.AcceleratedDisplayLayers innerLayers = parentLayer as client.AcceleratedDisplayLayers;
                    foreach (var innerLayer in innerLayers)
                    {
                        //if layer is of type FeatureLayer create a new FeatureLayer with the graphics unselected
                        //else just add the layer to the collection
                        if (innerLayer is client.FeatureLayer)
                        {
                            client.FeatureLayer layerWithSymbol = innerLayer as client.FeatureLayer;

                            client.FeatureLayer newLayer = new client.FeatureLayer();

                            foreach (var oldGraphic in layerWithSymbol.Graphics)
                            {
                                client.Graphic newGraphic = new client.Graphic()
                                {
                                    Geometry = Geometry.Clone(oldGraphic.Geometry),
                                    Symbol = oldGraphic.Symbol ?? layerWithSymbol.Renderer.GetSymbol(oldGraphic),
                                    Selected = false
                                };

                                foreach (var item in oldGraphic.Attributes)
                                {
                                    newGraphic.Attributes[item.Key] = item.Value;
                                }

                                newLayer.Graphics.Add(newGraphic);
                            }

                            collection.Add(newLayer);
                        }
                        else if (innerLayer is client.ArcGISTiledMapServiceLayer)
                        {
                            client.ArcGISTiledMapServiceLayer oldTl = innerLayer as client.ArcGISTiledMapServiceLayer;

                            client.ArcGISTiledMapServiceLayer tl = new client.ArcGISTiledMapServiceLayer()
                            {
                                ClientCertificate = oldTl.ClientCertificate,
                                Credentials = oldTl.Credentials,
                                DisplayName = oldTl.DisplayName,
                                ID = "basemapLayer",
                                ShowLegend = oldTl.ShowLegend,
                                Token = oldTl.Token,
                                Url = oldTl.Url
                            };

                            collection.Add(tl);
                        }
                    }
                }
            }
            return collection;
        }

        private void print_StatusUpdated(object sender, client.Tasks.JobInfoEventArgs e)
        {
            try
            {
                if (this.CancelPressed)
                {
                    PrintTask task = sender as PrintTask;
                    task.CancelJobStatusUpdates(e.JobInfo.JobId);
                    task.CancelJobTaskAsync(e.JobInfo.JobId);
                    throw new CancellationException("Downloading of mapimages cancelled.");
                }
                else
                {
                    string updateMsg;
                    switch (e.JobInfo.JobStatus)
                    {
                        case esriJobStatus.esriJobSubmitted:
                            // Disable automatic status checking.
                            updateMsg = string.Format("{0} - {1}", e.JobInfo.JobId, "Busy downloading mapimages");
                            base.OnLogStatusChanged(this, "Busy downloading mapimages...", updateMsg);
                            break;
                        case esriJobStatus.esriJobSucceeded:
                            // Get the results.
                            updateMsg = string.Format("{0} - {1}", e.JobInfo.JobId, "Mapimage downloaded");
                            string friendlyUpdateMsg = string.Format("Mapimage {0} of {1} downloaded.", DownloadIndex, this.TotalNumberOfRecords);
                            base.OnLogStatusChanged(this, friendlyUpdateMsg, updateMsg);
                            break;
                        case esriJobStatus.esriJobFailed:
                        case esriJobStatus.esriJobTimedOut:
                            string messages = string.Empty;

                            foreach (var message in e.JobInfo.Messages)
                            {
                                messages += string.Format("message: {0}. ", message.Description.ToString());
                            }

                            updateMsg = string.Format("{0} - {2} for job {1}. Messages: {3}", DateTime.Now, e.JobInfo.JobId, "Download of mapimage failed", messages);
                            base.OnLogStatusChanged(this, "Download of mapimage failed. See the log for more information", updateMsg);
                            base.OnPartCompleted(this, this.TotalNumberOfRecords);
                            if (DownloadIndex >= TotalNumberOfRecords)
                            {
                                base.OnProcessStatusChanged(this, this.TaskType, true);
                            }
                            DownloadIndex++;

                            break;
                    }
                }
            }
            catch (CancellationException)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "(Mapimages) Cancel pressed by user");
                this.ProcessCanceled();
            }
            catch (Exception ex)
            {
                base.OnLogStatusChanged(this, "An error occurred in creating the mapimages. See the logfile for more information,",
                    string.Format("An error occurred in creating the mapimages. Exception: {0}", ex.Message));
                this.ProcessCanceled();
            }
        }

        private void Print_JobCompleted(object sender, PrintJobEventArgs e, int objectId)
        {
            try
            {
                if (CancelPressed)
                    throw new CancellationException("Downloading of mapimages cancelled.");

                PrintResult result = e.PrintResult;

                this.DownloadData(result, objectId);
            }
            catch (CancellationException)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "(Mapimages) Cancel pressed by user");
                this.ProcessCanceled();
            }
            catch (Exception ex)
            {
                base.OnLogStatusChanged(this, "An error occurred in creating the mapimage, trying to create other images. See the logfile for more information.",
                    string.Format("An error occurred in creating the mapimages. Exception: {0}", ex.Message));
                this.ProcessCanceled();
            }
        }

        private void DownloadData(PrintResult result, int objectId)
        {
            if (result != null && result.Url != null)
            {
                string extension = base.GetExtension(result.Url);

                WebClient client = new WebClient();
                this.WebClients.Add(client);

                client.DownloadDataCompleted += (s, a) => Client_DownloadDataCompleted(s, a, extension, objectId);

                client.DownloadDataAsync(result.Url);
            }
            else
            {
                this.ProcessCanceled();
                throw new ApplicationException("No result returned from server.");
            }
        }

        private void Client_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs args, string extension, int objectId)
        {
            try
            {
                if (CancelPressed)
                    throw new CancellationException("Downloading of mapimages cancelled.");

                string filePath = string.Format("{0}\\MapImages", this.Dir);

                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                string fileName = string.Format("{0}\\mapimage_object{1}{2}", filePath, objectId, extension);

                File.WriteAllBytes(fileName, args.Result);

                this.UpdateStatus(filePath);

                base.OnPartCompleted(this, this.TotalNumberOfRecords);
            }
            catch (CancellationException)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "(Mapimages) Cancel pressed by user");
                ProcessCanceled();
            }
            catch (Exception ex)
            {
                base.OnLogStatusChanged(this, "An error occurred in creating the mapimages. See the logfile for more information,",
                    string.Format("An error occurred in creating the mapimages. Exception: {0}", ex.Message));
                ProcessCanceled();
            }
        }

        private void ProcessCanceled()
        {
            base.OnPartCompleted(this, this.TotalNumberOfRecords);

            if (DownloadIndex == TotalNumberOfRecords)
            {
                base.OnProcessStatusChanged(this, this.TaskType, true);
            }
            DownloadIndex++;
        }

        private void UpdateStatus(string path)
        {
            try
            {
                if (CancelPressed)
                    throw new CancellationException("Downloading of mapimages cancelled.");

                if (DownloadIndex >= TotalNumberOfRecords)
                {
                    string completeMsg = "Printtask completed.";
                    base.OnLogStatusChanged(this, completeMsg, completeMsg);

                    base.OnProcessStatusChanged(this, this.TaskType, true);
                }
                DownloadIndex++;
            }
            catch (CancellationException)
            {
                base.OnLogStatusChanged(this, "Cancel pressed, rounding up running processes.", "(Mapimages) Cancel pressed by user");
                this.ProcessCanceled();
            }
            catch (Exception ex)
            {
                base.OnLogStatusChanged(this, "An error occurred in creating the mapimages. See the logfile for more information,",
                    string.Format("An error occurred in creating the mapimages. Exception: {0}", ex.Message));
                this.ProcessCanceled();
            }
        }

        public void Update()
        {
            this.CancelPressed = true;
            this.Worker.CancelAsync();
            foreach (WebClient c in this.WebClients)
            {
                if (c.IsBusy)
                {
                    c.CancelAsync();
                }
            }

            base.OnProcessStatusChanged(this, this.TaskType, true);
            base.OnLogStatusChanged(this, "Downloading of mapimages cancelled.", "Downloading of mapimages cancelled.");
        }
    }
}
