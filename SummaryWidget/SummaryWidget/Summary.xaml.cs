using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.FeatureService;

namespace SummaryWidget
{
    /// <summary>
    /// A Widget is a dockable add-in class for Operations Dashboard for ArcGIS that implements IWidget. By returning true from CanConfigure, 
    /// this widget provides the ability for the user to configure the widget properties showing a settings Window in the Configure method.
    /// By implementing IDataSourceConsumer, this Widget indicates it requires a DataSource to function and will be notified when the 
    /// data source is updated or removed.
    /// </summary>

    [Export("ESRI.ArcGIS.OperationsDashboard.Widget")]
    [ExportMetadata("DisplayName", "Summary Group By")]
    [ExportMetadata("Description", "A Summary Group By widget displays the features into a set of summary rows by the value of one column")]
    [ExportMetadata("ImagePath", "/SummaryWidget;component/Images/table.png")]
    [ExportMetadata("DataSourceRequired", true)]
    [DataContract]
    public partial class Summary : UserControl, IWidget, IDataSourceConsumer
    {
        /// <summary>
        /// A unique identifier of a data source in the configuration. This property is set during widget configuration.
        /// </summary>
        [DataMember(Name = "dataSourceId")]
        public string DataSourceId { get; set; }

        /// <summary>
        /// The name of a field within the selected data source. This property is set during widget configuration.
        /// </summary>
        [DataMember(Name = "groupByfieldName")]
        public string GroupByFieldName { get; set; }

        [DataMember(Name = "sumFieldName")]
        public string SumFieldName { get; set; }

        [DataMember(Name = "groupByFieldHeader")]
        public string GroupByFieldHeader { get; set; }

        [DataMember(Name = "sumFieldHeader")]
        public string SumFieldHeader { get; set; }

        public Summary()
        {
            InitializeComponent();
        }

        private void UpdateControls()
        {
            //DataSourceBox.Text = DataSourceId;
            //FieldBox.Text = Field;
        }

        #region IWidget Members

        private string _caption = "Default Caption";
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
            // Show the configuration dialog.
            Config.SummaryDialog dialog = new Config.SummaryDialog(dataSources, Caption, DataSourceId, GroupByFieldName, GroupByFieldHeader, SumFieldName, SumFieldHeader) { Owner = owner };
            if (dialog.ShowDialog() != true)
                return false;

            // Retrieve the selected values for the properties from the configuration dialog.
            this.Caption = dialog.SumData.Caption;
            this.DataSourceId = dialog.SumData.DataSource.Id;
            if (dialog.SumData.SumField != null)
            {
                this.SumFieldName = dialog.SumData.SumField.FieldName;
            }
            this.GroupByFieldName = dialog.SumData.GroupByField.FieldName;
            this.GroupByFieldHeader = dialog.SumData.GroupByFieldHeader;
            this.SumFieldHeader = dialog.SumData.StatisticsFieldHeader;

            // The default UI simply shows the values of the configured properties.
            return true;

        }

        private async void FillDataGrid(DataSource dataSource)
        {
            try
            {
                this.ErrorMessageTextBlock.Text = string.Empty;

                var fields = new List<string>();
                fields.Add(this.GroupByFieldName);
                if (this.SumFieldName != null)
                {
                    fields.Add(this.SumFieldName);
                }
                fields.Add(dataSource.ObjectIdFieldName);

                var query = new Query();
                query.Fields = fields.ToArray();
                query.ReturnGeometry = false;
                QueryResult results = await dataSource.ExecuteQueryAsync(query);

                if (results.Features.Count == 0)
                {
                    this.SumGrid.ItemsSource = null;
                    return;
                }

                List<SummaryObject> list = new List<SummaryObject>();
                client.FeatureService.CodedValueDomain codedValueDomain = getCodedValueDomainFromField(GroupByFieldName, dataSource);
                if (this.GroupByFieldHasSubTypes(GroupByFieldName, dataSource))
                {
                    list = this.GetDataBasedOnSubtypes(dataSource, results, list);
                }
                else if (codedValueDomain == null)
                {
                    list = this.GetData(results, list);
                }
                else
                {
                    list = this.GetDataByCodedValueDomain(results, list, codedValueDomain);
                }

                this.SetDataSource(list);

            }
            catch (Exception ex)
            {
                this.ErrorMessageTextBlock.Text = ex.Message.ToString();
            }
        }

        private void SetDataSource(List<SummaryObject> list)
        {
            var sumList = from feature in list
                          group feature by new { feature.GroupByFieldValue } into g
                          orderby g.Key.GroupByFieldValue
                          select new
                          {
                              Soort = g.Key.GroupByFieldValue,
                              Aantal = g.Sum(f => decimal.Parse(f.SumFieldValue))
                          };

            if (sumList.Count() > 0)
            {
                this.SumGrid.ItemsSource = sumList;
                this.SumGrid.Columns[0].Header = this.GroupByFieldHeader;
                this.SumGrid.Columns[0].Width = DataGridLength.Auto;
                this.SumGrid.Columns[1].Header = this.SumFieldHeader;
                this.SumGrid.Columns[0].Width = DataGridLength.Auto;
            }
            else
            {
                this.SumGrid.ItemsSource = null;
            }
        }

        private List<SummaryObject> GetDataByCodedValueDomain(QueryResult results, List<SummaryObject> list, client.FeatureService.CodedValueDomain codedValueDomain)
        {
            foreach (var f in results.Features)
            {
                try
                {
                    SummaryObject summaryObject = new SummaryObject();
                    if (f.Attributes[GroupByFieldName] == null)
                        continue;
                    var fieldvalue = f.Attributes[GroupByFieldName].ToString();
                    var domainvalue = codedValueDomain.CodedValues.FirstOrDefault(t => t.Key.ToString() == fieldvalue);
                    if (domainvalue.Equals(default(KeyValuePair<object, string>)))
                    {
                        throw new ApplicationException(string.Format("No domain value found for value '{0}' \n", fieldvalue));
                    }
                    summaryObject.GroupByFieldValue = domainvalue.Value.ToString();
                    //summaryObject.SumFieldValue = (this.SumFieldName != null) ? f.Attributes[SumFieldName].ToString() : "1";
                    if (this.SumFieldName != null)
                    {
                        if (f.Attributes[SumFieldName] == null)
                            continue;
                        summaryObject.SumFieldValue = f.Attributes[SumFieldName].ToString();
                    }
                    else
                    {
                        summaryObject.SumFieldValue = "1";
                    }
                    list.Add(summaryObject);
                }
                catch (ApplicationException ex)
                {
                    this.ErrorMessageTextBlock.Text = this.ErrorMessageTextBlock.Text + ex.Message.ToString();
                }
            }

            return list;
        }

        private List<SummaryObject> GetData(QueryResult results, List<SummaryObject> list)
        {
            foreach (var f in results.Features)
            {
                try
                {
                    SummaryObject summaryObject = new SummaryObject();
                    if (f.Attributes[GroupByFieldName] == null)
                        continue;
                    summaryObject.GroupByFieldValue = f.Attributes[GroupByFieldName].ToString();
                    summaryObject.SumFieldValue = (this.SumFieldName != null) ? f.Attributes[SumFieldName].ToString() : "1";
                    if (this.SumFieldName != null)
                    {
                        if (f.Attributes[SumFieldName] == null)
                            continue;
                        summaryObject.SumFieldValue = f.Attributes[SumFieldName].ToString();
                    }
                    else
                    {
                        summaryObject.SumFieldValue = "1";
                    }
                    list.Add(summaryObject);
                }
                catch (ApplicationException ex)
                {
                    this.ErrorMessageTextBlock.Text = this.ErrorMessageTextBlock.Text + ex.Message.ToString();
                }
            }

            return list;
        }

        private List<SummaryObject> GetDataBasedOnSubtypes(DataSource dataSource, QueryResult results, List<SummaryObject> list)
        {
            try
            {
                FeatureLayerInfo layerInfo = this.GetLayerInfo(dataSource);

                foreach (var f in results.Features)
                {
                    SummaryObject summaryObject = new SummaryObject();

                    if (f.Attributes[GroupByFieldName] == null)
                        continue;

                    summaryObject.GroupByFieldValue = f.Attributes[GroupByFieldName].ToString();

                    foreach (var item in layerInfo.FeatureTypes)
                    {
                        if (item.Value.Id.Equals(f.Attributes[GroupByFieldName]))
                        {
                            summaryObject.GroupByFieldValue = item.Value.Name;
                            break;
                        }
                    }

                    if (this.SumFieldName != null)
                    {
                        if (f.Attributes[SumFieldName] != null)
                        {
                            summaryObject.SumFieldValue = f.Attributes[SumFieldName].ToString();
                        }
                        else
                        {
                            summaryObject.SumFieldValue = "0";
                        }
                    }
                    else
                    {
                        //if object is null default value is 1
                        summaryObject.SumFieldValue = "1";
                    }
                    list.Add(summaryObject);
                }
            }
            catch (Exception ex)
            {
                this.ErrorMessageTextBlock.Text = this.ErrorMessageTextBlock.Text + ex.Message.ToString();
            }

            return list;
        }

        private bool GroupByFieldHasSubTypes(string groupByFieldName, DataSource dataSource)
        {
            FeatureLayerInfo layerInfo = GetLayerInfo(dataSource);

            return (layerInfo.TypeIdField == groupByFieldName);
        }

        private FeatureLayerInfo GetLayerInfo(DataSource dataSource)
        {
            MapWidget widget = MapWidget.FindMapWidget(dataSource);

            client.FeatureLayer fLayer = widget.FindFeatureLayer(dataSource);

            FeatureLayerInfo layerInfo = fLayer.LayerInfo;

            return layerInfo;
        }

        private client.FeatureService.CodedValueDomain getCodedValueDomainFromField(string fieldName, DataSource datasource)
        {
            client.Field field = datasource.Fields.FirstOrDefault(f => f.FieldName == fieldName);
            if (field != null)
            {
                if (field.Domain != null)
                {
                    if (field.Domain is client.FeatureService.CodedValueDomain)
                    {
                        return field.Domain as client.FeatureService.CodedValueDomain;
                    }
                }
            }
            return null;
        }
        #endregion

        #region IDataSourceConsumer Members

        /// <summary>
        /// Returns the ID(s) of the data source(s) consumed by the widget.
        /// </summary>
        public string[] DataSourceIds
        {
            get { return new string[] { DataSourceId }; }
        }

        /// <summary>
        /// Called when a DataSource is removed from the configuration. 
        /// </summary>
        /// <param name="dataSource">The DataSource being removed.</param>
        public void OnRemove(DataSource dataSource)
        {
            // Respond to data source being removed.
            DataSourceId = null;
        }

        /// <summary>
        /// Called when a DataSource found in the DataSourceIds property is updated.
        /// </summary>
        /// <param name="dataSource">The DataSource being updated.</param>
        public void OnRefresh(DataSource dataSource)
        {
            // If required, respond to the update from the selected data source.
            // Consider using an async method.
            if (dataSource.IsBroken)
                return;
            this.FillDataGrid(dataSource);
        }

        #endregion
    }
}

public class SummaryObject
{
    public string GroupByFieldValue { get; set; }
    public string SumFieldValue { get; set; }
}
