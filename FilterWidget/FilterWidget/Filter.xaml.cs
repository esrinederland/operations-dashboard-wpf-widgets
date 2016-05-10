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
using System.Collections.ObjectModel;
using FilterWidget.Objects;
using ESRI.ArcGIS.Client.FeatureService;
using System.Windows.Media;
using System.Globalization;
using FilterWidget.Managers;
using FilterWidget.Helpers;

namespace FilterWidget
{
    /// <summary>
    /// A Widget is a dockable add-in class for Operations Dashboard for ArcGIS that implements IWidget. By returning true from CanConfigure, 
    /// this widget provides the ability for the user to configure the widget properties showing a settings Window in the Configure method.
    /// By implementing IDataSourceConsumer, this Widget indicates it requires a DataSource to function and will be notified when the 
    /// data source is updated or removed.
    /// </summary>
    [Export("ESRI.ArcGIS.OperationsDashboard.Widget")]
    [ExportMetadata("DisplayName", "Filter")]
    [ExportMetadata("Description", "The Filter widget provides a means of filtering the Data Source based on different attribute field values")]
    [ExportMetadata("ImagePath", "/FilterWidget;component/Images/search.png")]
    [ExportMetadata("DataSourceRequired", true)]
    [DataContract]
    public partial class Filter : UserControl, IWidget, IDataSourceConsumer
    {
        /// <summary>
        /// A unique identifier of a data source in the configuration. This property is set during widget configuration.
        /// </summary>
        [DataMember(Name = "dataSourceId")]
        public string DataSourceId { get; set; }

        /// <summary>
        /// The name of a field within the selected data source. This property is set during widget configuration.
        /// </summary>
        [DataMember(Name = "SelectedFields")]
        public string[] SelectedFields { get; set; }

        public Dictionary<string, string> SelectedFieldValues { get; set; }

        private string _ClearButtonContent = "Clear";
        [DataMember(Name = "ClearButtonContent")]
        public string ClearButtonContent
        {
            get
            {
                return _ClearButtonContent;
            }

            set
            {
                if (value != _ClearButtonContent)
                {
                    _ClearButtonContent = value;
                }
            }
        }

        private string _OkButtonContent = "Ok";
        [DataMember(Name = "OkButtonContent")]
        public string OkButtonContent
        {
            get
            {
                return _OkButtonContent;
            }

            set
            {
                if (value != _OkButtonContent)
                {
                    _OkButtonContent = value;
                }
            }
        }

        public Filter()
        {
            InitializeComponent();
        }

        private void UpdateControls()
        {
            OKButton.Content = OkButtonContent;

            ClearButton.Content = ClearButtonContent;

            if (this.DataSource == null
                && !string.IsNullOrEmpty(this.DataSourceId))
            {
                try
                {
                    this.DataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == DataSourceId);
                }
                catch (Exception)
                {
                    this.ErrorMessageTextBlock.Text = "Unable to load instance";
                }
            }
            
            if (this.DataSource != null)
            {
                FeatureLayerInfo layerInfo = FeatureLayerInfoFinder.GetFeatureLayerInfo(this.DataSource);

                GridManager gm = new GridManager(this.ComboBoxesGrd, this.DataSource, this.DataSourceId, layerInfo, this.SelectedFields, this.SelectedFieldValues);

                gm.FillGrid();
            }
        }

        #region IWidget Members
        private bool _isLoaded = false;
        private DataSource DataSource { get; set; }
        /// <summary>
        /// The text that is displayed in the widget's containing window title bar. This property is set during widget configuration.
        /// </summary>
        private string _caption = "Default Caption";
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

            try
            {
                if (this.DataSource != null)
                {
                    this.ApplyFilter("1=1");
                    this.DataSource = null;
                }
            }
            catch (Exception)
            {
                //this exception will be thrown when the widget is deleted. No way to display the error message in a userfriendly way.
            }
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
            ConfigSettings settings = new ConfigSettings()
            {
                DataSources = dataSources,
                InitialCaption = this.Caption,
                InitialClearButtonContent = this.ClearButtonContent,
                InitialOkButtonContent = this.OkButtonContent,
                InitialDataSourceId = this.DataSourceId,
                InitialSelectedFields = this.SelectedFields
            };

            Config.FilterDialog dialog = new Config.FilterDialog(settings) { Owner = owner };
            if (dialog.ShowDialog() != true)
                return false;

            // Retrieve the selected values for the properties from the configuration dialog.
            Caption = dialog.Caption;
            OkButtonContent = dialog.OkButtonContent;
            ClearButtonContent = dialog.ClearButtonContent;
            DataSourceId = dialog.DataSource.Id;
            SelectedFields = dialog.SelectedFields;
            this.DataSource = dialog.DataSource;
            this.SelectedFieldValues = null;
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
            if (dataSource != null)
            {
                if (!dataSource.IsBroken)
                {
                    this.DataSource = dataSource;

                    this.UpdateControls();
                }
            }
        }

        #endregion

        protected void OKButton_Click(object sender, EventArgs e)
        {
            this.SelectedFieldValues = new Dictionary<string, string>();

            if (DataSource != null)
            {
                List<string> WhereCauseList = new List<string>();
                for (int i = 0; i < this.SelectedFields.Count(); i++)
                {
                    client.Field field = DataSource.Fields.Single(f => f.FieldName == this.SelectedFields[i]);
                    var name = string.Format("{0}ComboBox", field.FieldName);
                    ComboBox comboBox = this.ComboBoxesGrd.FindName(name) as ComboBox;
                    if (comboBox != null && comboBox.SelectedIndex > -1)
                    {
                        this.SelectedFieldValues.Add(field.FieldName, comboBox.SelectedValue.ToString());

                        string whereClause;
                        if (IsNumeric(field.Type))
                        {
                            whereClause = string.Format("{0} = {1}", field.FieldName, comboBox.SelectedValue.ToString());
                        }
                        else
                        {
                            whereClause = string.Format("{0} = '{1}'", field.FieldName, comboBox.SelectedValue.ToString());
                        }

                        WhereCauseList.Add(whereClause);
                    }
                }
                string queryString = "";
                if (WhereCauseList.Count == 0)
                {
                    queryString = "1=1";
                }
                else
                {
                    for (int i = 0; i < WhereCauseList.Count; i++)
                    {
                        if (i == 0)
                        {
                            queryString = WhereCauseList[i];
                        }
                        else
                        {
                            queryString = queryString + " and " + WhereCauseList[i].ToString();
                        }
                    }
                }
                this.ApplyFilter(queryString);
            }
        }

        private void ApplyFilter(string queryString)
        {
            MapWidget mapW = MapWidget.FindMapWidget(DataSource);
            client.FeatureLayer fl = mapW.FindFeatureLayer(DataSource);
            fl.ClearSelection();
            fl.Where = queryString;
            fl.Update();
            fl.Refresh();
            OperationsDashboard.Instance.RefreshDataSource(DataSource);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFilter();
        }

        private void ClearFilter()
        {
            for (int i = 0; i < this.SelectedFields.Count(); i++)
            {
                client.Field field = DataSource.Fields.Single(f => f.FieldName == SelectedFields[i]);
                var name = string.Format("{0}ComboBox", field.FieldName);
                ComboBox comboBox = this.ComboBoxesGrd.FindName(name) as ComboBox;
                if (comboBox != null && comboBox.SelectedIndex > -1)
                {
                    comboBox.SelectedIndex = -1;
                }
            }

            this.SelectedFieldValues = null;

            this.ApplyFilter("1=1");
        }

        private bool IsNumeric(client.Field.FieldType value)
        {
            if (value == client.Field.FieldType.Double ||
                value == client.Field.FieldType.Integer ||
                value == client.Field.FieldType.SmallInteger ||
                value == client.Field.FieldType.Single)
            {
                return true;
            }
            return false;
        }
    }
}


