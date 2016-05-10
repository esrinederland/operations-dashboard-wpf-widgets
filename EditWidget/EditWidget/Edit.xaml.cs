using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using System.Windows.Input;
using EditWidget.Objects;
using EditWidget.Extensions;
using System.Threading.Tasks;

namespace EditWidget
{
    /// <summary>
    /// A Widget is a dockable add-in class for Operations Dashboard for ArcGIS that implements IWidget. By returning true from CanConfigure, 
    /// this widget provides the ability for the user to configure the widget properties showing a settings Window in the Configure method.
    /// By implementing IDataSourceConsumer, this Widget indicates it requires a DataSource to function and will be notified when the 
    /// data source is updated or removed.
    /// </summary>
    [Export("ESRI.ArcGIS.OperationsDashboard.Widget")]
    [ExportMetadata("DisplayName", "View/Modify Attributes")]
    [ExportMetadata("Description", "The View/Modify Attributes widget provides the ability to view/modify feature attributes")]
    [ExportMetadata("ImagePath", "/EditWidget;component/Images/edit.png")]
    [ExportMetadata("DataSourceRequired", true)]
    [DataContract]
    [KnownType(typeof(FieldSetting))]
    [KnownType(typeof(IList<FieldSetting>))]
    public partial class Edit : UserControl, IWidget, IDataSourceConsumer
    {
        #region DefaultValues

        private string _caption = "Default Caption";
        private string _save = "Save";
        private string _delete = "Delete";

        #endregion

        #region DataMembers
        /// <summary>
        /// A unique identifier of a data source in the configuration. This property is set during widget configuration.
        /// </summary>
        [DataMember(Name = "dataSourceId")]
        public string DataSourceId { get; set; }

        [DataMember(Name = "readonly")]
        public bool IsAllReadOnly { get; set; }

        [DataMember(Name = "visible")]
        public bool IsAllVisible { get; set; }

        [DataMember(Name = "fieldsettings")]
        public IList<FieldSetting> FieldSettings { get; set; }

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

        [DataMember(Name = "savetext")]
        public string SaveText
        {
            get
            {
                return _save;
            }

            set
            {
                if (value != _save)
                {
                    _save = value;
                }
            }
        }

        [DataMember(Name = "deletetext")]
        public string DeleteText
        {
            get
            {
                return _delete;
            }

            set
            {
                if (value != _delete)
                {
                    _delete = value;
                }
            }
        }

        [DataMember(Name = "id")]
        public string Id { get; set; }
        #endregion

        #region properties
        /// <summary>
        ///  Determines if the Configure method is called after the widget is created, before it is added to the configuration. Provides an opportunity to gather user-defined settings.
        /// </summary>
        /// <value>Return true if the Configure method should be called, otherwise return false.</value>
        public bool CanConfigure { get { return true; } }

        /// <summary>
        /// Returns the ID(s) of the data source(s) consumed by the widget.
        /// </summary>
        public string[] DataSourceIds { get { return new string[] { DataSourceId }; } }

        private int CurrentObjectID { get; set; }

        List<int> EditObjectIDs { get; set; }

        private client.FeatureLayer FeatureLayer { get; set; }

        private client.FeatureLayer MapFeatureLayer { get; set; }

        private DataSource DataSource { get; set; }

        private MapWidget MapWidget { get; set; }

        private client.Graphic FormGraphic;

        private bool HasResults { get; set; }

        #endregion

        public Edit()
        {
            InitializeComponent();

            //enable buttons for first time loading
            this.ToggleButtonAvailability(true);
        }

        private void UpdateControls()
        {
            this.ResetProperties();

            this.FeatureDataForm.DeleteButtonContent = this.DeleteText;

            this.FeatureDataForm.CommitButtonContent = this.SaveText;

            this.FeatureDataForm.IsReadOnly = this.IsAllReadOnly;

            if (this.FieldSettings == null && this.DataSource != null)
            {
                this.FieldSettings = new List<FieldSetting>();
                foreach (var field in DataSource.Fields)
                {
                    FieldSetting setting = new FieldSetting()
                    {
                        FieldName = field.Name,
                        IsReadOnly = this.IsAllReadOnly,
                        IsVisible = true
                    };

                    this.FieldSettings.Add(setting);
                }
            }

            this.FeatureDataForm.FieldSettings = this.FieldSettings;
            this.FeatureDataForm.ConfirmBeforeDelete = true;
        }

        private void ResetProperties()
        {
            this.CurrentObjectID = -1;
            this.EditObjectIDs = null;
            this.FeatureLayer = null;
            this.MapFeatureLayer = null;
            this.DataSource = null;
            this.MapWidget = null;
            this.FormGraphic = null;
            this.HasResults = false;
            this.ToggleButtonAvailability(true);
        }

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
        ///  Provides functionality for the widget to be configured by the end user through a dialog.
        /// </summary>
        /// <param name="owner">The application window which should be the owner of the dialog.</param>
        /// <param name="dataSources">The complete list of DataSources in the configuration.</param>
        /// <returns>True if the user clicks ok, otherwise false.</returns>
        public bool Configure(Window owner, IList<DataSource> dataSources)
        {
            // Show the configuration dialog.
            Config.EditDialog dialog = new Config.EditDialog(dataSources, Caption, DataSourceId, FieldSettings, IsAllReadOnly, IsAllVisible, SaveText, DeleteText) { Owner = owner };
            if (dialog.ShowDialog() != true)
                return false;

            // Retrieve the selected values for the properties from the configuration dialog.
            this.Caption = dialog.Caption;
            this.DeleteText = dialog.DeleteButton;
            this.SaveText = dialog.SaveButton;
            this.DataSourceId = dialog.DataSource.Id;
            this.IsAllReadOnly = dialog.IsReadOnly;
            this.IsAllVisible = dialog.AllIsVisible;
            this.FieldSettings = dialog.FieldSettings;

            // The default UI simply shows the values of the configured properties.
            UpdateControls();

            return true;
        }

        #endregion

        #region IDataSourceConsumer Members
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
            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                {
                    if (dataSource != null)
                    {
                        if (!dataSource.IsBroken)
                        {
                            this.QueryDataSource(dataSource);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageTextBlock.Text = ex.Message;
                ErrorMessageTextBlock.Visibility = System.Windows.Visibility.Visible;
            }
            finally
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private async void QueryDataSource(DataSource dataSource)
        {
            try
            {
                if (dataSource != null)
                {
                    this.SetDataSource(dataSource);

                    this.SetMapWidget(dataSource);

                    List<int> listObjectIDs = new List<int>();

                    Query query = new Query()
                    {
                        Fields = new string[] { DataSource.ObjectIdFieldName }
                    };

                    QueryResult result = await dataSource.ExecuteQueryAsync(query);
                    if (result.Features.Count > 0)
                    {
                        this.HasResults = true;

                        this.ToggleButtonAvailability(true);

                        this.ErrorMessageTextBlock.Text = string.Empty;
                        this.ErrorMessageTextBlock.Visibility = System.Windows.Visibility.Hidden;

                        foreach (var item in result.Features)
                        {
                            int Objectid = Convert.ToInt32(item.Attributes[DataSource.ObjectIdFieldName]);
                            listObjectIDs.Add(Objectid);
                        }
                        EditObjectIDs = listObjectIDs;
                        CountLabel.Text = String.Format("1/{0}", EditObjectIDs.Count.ToString());

                        bool objectIdExists = EditObjectIDs.Exists(c => c == CurrentObjectID);
                        if (objectIdExists)
                        {
                            CountLabel.Text = String.Format("{0}/{1}", EditObjectIDs.IndexOf(CurrentObjectID) + 1, EditObjectIDs.Count.ToString());
                            await QueryById(CurrentObjectID);
                        }
                        else
                        {
                            await QueryById(EditObjectIDs[0]);
                            CurrentObjectID = EditObjectIDs[0];
                        }
                        return;
                    }
                }

                this.HideFeatureDataForm();
            }
            catch (Exception)
            {
                ErrorMessageTextBlock.Text = "An error occurred while fetching data. Please try again later."; 
                ErrorMessageTextBlock.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void HideFeatureDataForm()
        {
            if (this.FeatureDataForm.Visibility != System.Windows.Visibility.Hidden)
            {
                this.FeatureDataForm.Visibility = System.Windows.Visibility.Hidden;
                this.ToggleButtonAvailability(false);
                this.HasResults = false;
                this.EditObjectIDs = null;
                this.CurrentObjectID = 0;
                this.CountLabel.Text = "";

                this.ErrorMessageTextBlock.Text = "No records available.";
                this.ErrorMessageTextBlock.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void ToggleButtonAvailability(bool isEnabled)
        {
            ButtonLeft.IsEnabled = isEnabled;
            ButtonRight.IsEnabled = isEnabled;
            ButtonFlash.IsEnabled = isEnabled;
        }

        private void SetMapWidget(DataSource dataSource)
        {
            if (MapWidget == null)
            {
                this.MapWidget = MapWidget.FindMapWidget(dataSource);

                this.MapWidget.Map.Progress += Map_Progress;
            }
        }

        private void Map_Progress(object sender, client.ProgressEventArgs e)
        {
            if (e.Progress == 100)
            {
                this.ToggleButtonAvailability(this.HasResults);
            }
            else
            {
                this.ToggleButtonAvailability(false);
            }
        }

        private void SetDataSource(DataSource dataSource)
        {
            this.DataSource = dataSource;
        }

        private async Task QueryById(int objectId)
        {
            try
            {
                if (FeatureLayer == null)
                {
                    this.MapFeatureLayer = MapWidget.FindFeatureLayer(DataSource);

                    string Url = MapFeatureLayer.Url;

                    this.FeatureLayer = this.CreateFeatureLayer(objectId, Url, MapFeatureLayer.Token);

                    this.FeatureDataForm.FeatureLayer = FeatureLayer;

                    this.FeatureLayer.Initialize();
                }
                else
                {
                    await this.SelectGraphicForm(objectId);
                }
            }
            catch (Exception)
            {
                ErrorMessageTextBlock.Text = "An error occurred while fetching a feature. Please try again later.";
                ErrorMessageTextBlock.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private ESRI.ArcGIS.Client.FeatureLayer CreateFeatureLayer(int objectId, string url, string token)
        {
            client.FeatureLayer fl = new client.FeatureLayer()
            {
                Url = url,
                Token = token,
                AutoSave = false,
                Mode = client.FeatureLayer.QueryMode.OnDemand,
                OutFields = new client.Tasks.OutFields() { "*" },
                DisableClientCaching = true
            };

            fl.Initialized += async (s, e) =>
            {
                fl.Graphics.CollectionChanged += Graphics_CollectionChanged;
                await SelectGraphicForm(objectId);
                return;
            };

            return fl;
        }


        #endregion

        #region editForm
        private async Task SelectGraphicForm(int objectId)
        {
            this.FeatureLayer.Where = string.Format("{0} ={1}", this.DataSource.ObjectIdFieldName, objectId.ToString());

            this.FeatureLayer.Update();

            Query query = new Query()
                    {
                        WhereClause = "OBJECTID = " + objectId.ToString(),
                        ReturnGeometry = true
                    };

            QueryResult result = await DataSource.ExecuteQueryAsync(query);

            if (result.Features.Count > 0)
            {
                this.FormGraphic = result.Features.First();

                if (this.FormGraphic != null)
                {
                    HighlightFeatureAction highlightAction = new HighlightFeatureAction();
                    highlightAction.UpdateExtent = UpdateExtentType.None;
                    if (highlightAction.CanExecute(DataSource, FormGraphic))
                        highlightAction.Execute(DataSource, FormGraphic);
                }
            }
        }

        private void Graphics_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            client.GraphicCollection collection = sender as client.GraphicCollection;
            client.Graphic graphic = collection.FirstOrDefault();
            if (graphic != null)
            {
                this.FeatureDataForm.GraphicSource = graphic;
                this.FeatureDataForm.Visibility = Visibility.Visible;
            }
        }

        private void FeatureDataForm_EditEnded(object sender, EventArgs e)
        {
            try
            {
                EsriNLFeatureDataForm featureDataForm = sender as EsriNLFeatureDataForm;
                if (featureDataForm.FeatureLayer.HasEdits)
                {
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    featureDataForm.FeatureLayer.EndSaveEdits += FeatureLayer_EndSaveEdits;
                    featureDataForm.FeatureLayer.SaveEdits();
                }
            }
            catch (Exception)
            {
                ErrorMessageTextBlock.Text = "An error occurred while saving edits. Please try again later.";//string.Format("Error in EditEnded: {0}", ex.Message);
                ErrorMessageTextBlock.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void FeatureDataForm_BeforeDelete(object sender, EventArgs e)
        {
            if (this.EditObjectIDs != null)
            {
                this.CurrentObjectID = GetNext(this.EditObjectIDs, this.CurrentObjectID);
            }
        }

        private void FeatureLayer_EndSaveEdits(object sender, client.Tasks.EndEditEventArgs e)
        {
            foreach (var item in OperationsDashboard.Instance.DataSources)
            {
                OperationsDashboard.Instance.RefreshDataSource(item);
            }
        }
        #endregion

        #region Button Navigation

        private async void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            await this.GoToRecord(GetNext);
        }

        private async void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            await this.GoToRecord(GetPrevious);
        }

        private async Task GoToRecord(Func<IList<int>, int, int> GetID)
        {
            try
            {
                if (EditObjectIDs != null || CurrentObjectID != 0)
                {
                    int PrevObjectID = GetID(EditObjectIDs, CurrentObjectID);
                    CurrentObjectID = PrevObjectID;
                    await QueryById(PrevObjectID);
                    CountLabel.Text = String.Format("{0}/{1}", EditObjectIDs.IndexOf(CurrentObjectID) + 1, EditObjectIDs.Count.ToString());

                    if (FormGraphic != null)
                    {
                        PanToFeatureAction fAction = new PanToFeatureAction();
                        if (fAction.CanExecute(DataSource, FormGraphic))
                        {
                            fAction.Execute(DataSource, FormGraphic);
                        }
                    }
                }
            }
            catch (Exception)
            {
                ErrorMessageTextBlock.Text = "An error occurred while fetching a feature. Please try again later.";//string.Format("Error in ButtonLeft_Click: {0}", ex.Message);
                ErrorMessageTextBlock.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void ButtonFlash_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FormGraphic != null)
                {
                    HighlightFeatureAction highlightAction = new HighlightFeatureAction();
                    highlightAction.UpdateExtent = UpdateExtentType.None;
                    if (highlightAction.CanExecute(DataSource, FormGraphic))
                        highlightAction.Execute(DataSource, FormGraphic);
                }
            }
            catch (Exception)
            {
                ErrorMessageTextBlock.Text = "An error occurred while fetching a feature. Please try again later.";//string.Format("ButtonFlash_Click: {0}", ex.Message);
                ErrorMessageTextBlock.Visibility = System.Windows.Visibility.Visible;
            }
        }

        public static T GetNext<T>(IList<T> collection, T value)
        {
            int nextIndex = collection.IndexOf(value) + 1;
            if (nextIndex < collection.Count)
            {
                return collection[nextIndex];
            }
            else
            {
                return value;
            }
        }

        public static T GetPrevious<T>(IList<T> collection, T value)
        {
            int previousIndex = collection.IndexOf(value) - 1;
            if (previousIndex >= 0)
            {
                return collection[previousIndex];
            }
            else
            {
                return value;
            }
        }

        private void ButtonZoom_Click(object sender, RoutedEventArgs e)
        {
            ZoomToFeatureAction zoom = new ZoomToFeatureAction();
            if (zoom.CanExecute(this.DataSource, this.FormGraphic))
            {
                this.MapWidget.Map.PanTo(this.FormGraphic.Geometry);

                if (zoom.CanExecute(this.DataSource, this.FormGraphic))
                {
                    zoom.Execute(this.DataSource, this.FormGraphic);
                }

                this.ToggleButtonAvailability(false);
            }
        }
        #endregion
    }
}
