using ESRI.ArcGIS.Client.FeatureService;
using ESRI.ArcGIS.OperationsDashboard;
using ExportWidget.Enums;
using ExportWidget.Eventargs.Exceptions;
using ExportWidget.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using client = ESRI.ArcGIS.Client;

namespace ExportWidget.Managers
{
    public class CsvManager : ManagerBase, ITask
    {
        protected override ExportTask TaskType
        {
            get { return ExportTask.Export; }
        }

        private const string DELIMITER = ";";

        #region Properties

        private DataSource DataSource { get; set; }

        private ConfigSettings ConfigSettings { get; set; }

        private string Dir { get; set; }

        private bool CancelPressed { get; set; }

        //Only 1 CSV is created
        private int TotalNumberOfParts { get { return 1; } }

        #endregion

        public CsvManager(DataSource dataSource, ConfigSettings settings, string directory)
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
                this.CreateCSV();
            }
            catch (CancellationException)
            {
                base.OnPartCompleted(this, this.TotalNumberOfParts);
                base.OnLogStatusChanged(this, "Cancel pressed", "(CSV) Cancel pressed by user");
                base.OnProcessStatusChanged(this, this.TaskType, true);
            }
            catch (Exception ex)
            {
                base.OnLogStatusChanged(this,
                    "An error occurred in creating the CSV. See the log file for more information.",
                    string.Format("An error occurred in creating the CSV. Exception: {0}", ex.Message));
                base.OnProcessStatusChanged(this, this.TaskType, true);
            }
        }

        private async void CreateCSV()
        {
            if (CancelPressed)
                throw new CancellationException("Downloading of csv cancelled.");

            string startMsg = "Creating a CSV...";
            base.OnLogStatusChanged(this, startMsg, startMsg);

            MapWidget widget = MapWidget.FindMapWidget(this.DataSource);

            client.FeatureLayer fl = widget.FindFeatureLayer(this.DataSource);

            List<client.Field> fields = fl.LayerInfo.Fields;
            string typeIdField = fl.LayerInfo.TypeIdField;

            foreach (var field in fields)
            {
                if (ConfigSettings.SelectedFields.Select(f => f.FieldName).Contains(field.FieldName))
                {
                    ConfigSettings.SelectedFields.Single(s => s.FieldName == field.FieldName).Field = field;
                }
            }

            Query query = new Query();
            query.Fields = (this.ConfigSettings.SelectedFields.Select(s => s.FieldName)).ToArray();
            query.WhereClause = "1=1";
            query.ReturnGeometry = false;

            QueryResult results = await this.DataSource.ExecuteQueryAsync(query);

            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();

            if (results.Features.Count() > 0)
            {
                foreach (var result in results.Features)
                {
                    Dictionary<string, object> value = new Dictionary<string, object>();
                    object subtypeID = result.Attributes[typeIdField];
                    foreach (var at in result.Attributes)
                    {
                        FieldValue fieldValue = ConfigSettings.SelectedFields.SingleOrDefault(s => s.FieldName == at.Key);
                        if (fieldValue == null)
                            continue;

                        client.Field field = fieldValue.Field;

                        object v = at.Value;
                        if (v != null)
                        {
                            if (!string.IsNullOrEmpty(typeIdField) && subtypeID != null)
                            {
                                if (field.FieldName == typeIdField)
                                {
                                    v = fl.LayerInfo.FeatureTypes[v].Name;
                                }
                                else if (fl.LayerInfo.FeatureTypes.ContainsKey(subtypeID)
                                    && fl.LayerInfo.FeatureTypes[subtypeID].Domains.ContainsKey(at.Key))
                                {
                                    CodedValueDomain cv = fl.LayerInfo.FeatureTypes[subtypeID].Domains[at.Key] as CodedValueDomain;
                                    if (cv != null && cv.CodedValues.ContainsKey(v))
                                    {
                                        v = cv.CodedValues[v];
                                    }
                                    else
                                    {
                                        v = string.Empty;
                                    }
                                }
                            }
                            else if (field.Domain != null)
                            {
                                CodedValueDomain cvDomain = field.Domain as CodedValueDomain;

                                if (cvDomain != null &&
                                    cvDomain.CodedValues.ContainsKey(v))
                                {
                                    v = cvDomain.CodedValues[v];
                                }
                            }
                        }

                        value.Add(at.Key, v);
                    }

                    list.Add(value);
                }

                string filePath = string.Format("{0}\\Export.csv", Dir);
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Close();
                }

                StringBuilder csvBuilder = new StringBuilder();

                csvBuilder.AppendLine(this.CreateHeader());

                csvBuilder = this.CreateContent(list, csvBuilder);

                File.AppendAllText(filePath, csvBuilder.ToString());

                string completeMsg = "CSV created.";
                base.OnLogStatusChanged(this, completeMsg, completeMsg);

                base.OnPartCompleted(this, this.TotalNumberOfParts);

                base.OnProcessStatusChanged(this, this.TaskType, true);
            }
            else
            {
                base.OnPartCompleted(this, this.TotalNumberOfParts);
                base.OnLogStatusChanged(this, "No records available for creating CSV", "No records available for creating CSV");
                base.OnProcessStatusChanged(this, this.TaskType, true);
            }
        }

        private StringBuilder CreateContent(List<Dictionary<string, object>> list, StringBuilder csvBuilder)
        {
            foreach (Dictionary<string, object> item in list)
            {
                int i = 0;
                StringBuilder lineBuilder = new StringBuilder();
                foreach (FieldValue fieldName in this.ConfigSettings.SelectedFields)
                {
                    lineBuilder.Append(item[fieldName.FieldName]);

                    if (i < ConfigSettings.SelectedFields.Count() - 1)
                    {
                        lineBuilder.Append(DELIMITER);
                    }

                    i++;
                }

                csvBuilder.AppendLine(lineBuilder.ToString());
            }
            return csvBuilder;
        }

        private string CreateHeader()
        {
            string header = string.Empty;

            for (int i = 0; i < ConfigSettings.SelectedFields.Count(); i++)
            {
                header += ConfigSettings.SelectedFields[i].FieldName;

                if (i < ConfigSettings.SelectedFields.Count() - 1)
                {
                    header += DELIMITER;
                }
            }
            return header;
        }


        public void Update()
        {
            this.CancelPressed = true;

            base.OnLogStatusChanged(this, "Downloading of csv cancelled.", "Downloading of csv cancelled.");
        }
    }
}
