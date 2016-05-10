using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using FilterWidget.Objects;
using ESRI.ArcGIS.Client.FeatureService;

namespace FilterWidget.Config
{
    /// <summary>
    /// Interaction logic for FilterDialog.xaml
    /// </summary>
    public partial class FilterDialog : Window
    {
        #region Properties
        public DataSource DataSource { get; private set; }

        public ESRI.ArcGIS.Client.Field Field { get; private set; }

        public string Caption { get; private set; }

        public string OkButtonContent { get; private set; }

        public string ClearButtonContent { get; private set; }

        public string[] SelectedFields { get; set; }

        private ObservableCollection<FieldValue> FieldsListInternal { get; set; }
        private ObservableCollection<FieldValue> FieldsList
        {
            get
            {
                if (this.FieldsListInternal == null)
                {
                    this.FieldsListInternal = new ObservableCollection<FieldValue>();
                }
                return this.FieldsListInternal;
            }
        }
        #endregion

        public FilterDialog(ConfigSettings settings)
        {
            InitializeComponent();

            this.InitializeConfigDialog(settings);

            this.SetDataSource(settings);
        }

        #region EventHandlers

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            IList<FieldValue> list = this.LstBxFields.Items.Cast<FieldValue>().ToList();

            this.DataSource = DataSourceSelector.SelectedDataSource;
            this.Caption = CaptionTextBox.Text;
            this.OkButtonContent = OkButtonTextBox.Text;
            this.ClearButtonContent = ClearButtonTextBox.Text;
            this.SelectedFields = list.Where(v => v.IsSelected).Select(v => v.FieldName).ToArray();
            this.DialogResult = true;
        }

        private void DataSourceSelector_SelectionChanged(object sender, EventArgs e)
        {
            DataSource dataSource = this.DataSourceSelector.SelectedDataSource;

            if (dataSource != null && dataSource.IsSelectable)
            {
                this.LstBxFields.ItemsSource = null;
                this.FieldsList.Clear();

                this.ErrorMessageTextBlock.Text = "Selectable datasources are not supported.";
                this.ErrorMessageTextBlock.Visibility = System.Windows.Visibility.Visible;

                this.ValidateInput();
            }
            else if (dataSource != null)
            {
                this.UpdateCheckboxListDatasource(dataSource);
            }

            this.ValidateInput();
        }

        private void Input_Changed(object sender, EventArgs e)
        {
            this.ValidateInput();
        }

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = this.LstBxFields.SelectedIndex;

            if (selectedIndex > 0)
            {
                int insertAt = selectedIndex - 1;
                this.MoveItemInList(selectedIndex, insertAt);
                this.LstBxFields.SelectedIndex = insertAt;
            }
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = this.LstBxFields.SelectedIndex;

            if (selectedIndex < 0)
                return;
            if (selectedIndex + 1 < this.LstBxFields.Items.Count)
            {
                int insertAt = selectedIndex + 1;
                this.MoveItemInList(selectedIndex, insertAt);
                this.LstBxFields.SelectedIndex = insertAt;
            }
        }

        private void Check_Changed(object sender, RoutedEventArgs e)
        {
            this.ValidateInput();
        }

        #endregion

        private void SetDataSource(ConfigSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.InitialDataSourceId))
            {
                try
                {
                    DataSource dataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == settings.InitialDataSourceId);

                    if (dataSource != null && dataSource.IsSelectable)
                    {
                        this.LstBxFields.ItemsSource = null;
                        this.FieldsList.Clear();

                        this.ErrorMessageTextBlock.Text = "Selectable datasources are not supported.";
                        this.ErrorMessageTextBlock.Visibility = System.Windows.Visibility.Visible;

                        this.ValidateInput();
                    }
                    else if (dataSource != null)
                    {
                        DataSourceSelector.SelectedDataSource = dataSource;
                        if (settings.InitialSelectedFields != null)
                        {
                            if (settings.InitialSelectedFields.Length != 0)
                            {
                                this.UpdateCheckboxListDatasource(dataSource, settings.InitialSelectedFields);
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        private void InitializeConfigDialog(ConfigSettings settings)
        {
            this.DataContext = this;
            this.Title = "Configuration Filter";
            // When re-configuring, initialize the widget config dialog from the existing settings.
            CaptionTextBox.Text = settings.InitialCaption;
            OkButtonTextBox.Text = settings.InitialOkButtonContent;
            ClearButtonTextBox.Text = settings.InitialClearButtonContent;
        }

        private void UpdateCheckboxListDatasource(DataSource dataSource, string[] initialFields = null)
        {
            if (dataSource != null)
            {
                this.LstBxFields.ItemsSource = null;
                this.FieldsList.Clear();
                //SubTypes
                FeatureLayerInfo layerInfo = this.GetLayerInfo(dataSource);
                string typeIDField = layerInfo.TypeIdField;
                ICollection<FeatureType> collectionFeatureType = layerInfo.FeatureTypes.Values;
                foreach (var field in dataSource.Fields)
                {
                    if (field.Domain != null)
                    {
                        this.GenerateFieldList(initialFields, field, false);
                    }
                    else if (typeIDField != null
                     && field.FieldName == typeIDField)
                    {
                        this.GenerateFieldList(initialFields, field, true);
                    }
                    else
                    {
                        FieldValue fieldValue = this.FieldsList.FirstOrDefault(t => t.FieldName == field.FieldName);
                        if (fieldValue == null)
                        {
                            foreach (var item in collectionFeatureType)
                            {
                                var fieldDomain = item.Domains.FirstOrDefault(t => t.Key == field.FieldName);
                                if (!fieldDomain.Equals(default(KeyValuePair<String, client.FeatureService.Domain>)))
                                {
                                    GenerateFieldList(initialFields, field, true);
                                    break;
                                }
                            }
                        }
                    }

                }
                if (FieldsList.Count == 0)
                {
                    this.ErrorMessageTextBlock.Text = "Cannot find fields with a domain in DataSource";
                    ValidateInput();
                }
                else
                {
                    this.LstBxFields.ItemsSource = this.FieldsList;
                    this.ErrorMessageTextBlock.Text = "";
                    if (initialFields != null)
                    {
                        for (int i = 0; i < initialFields.Length; i++)
                        {
                            string fieldName = initialFields[i];
                            int index = FieldsList.IndexOf(FieldsList.Single(f => f.FieldName == fieldName));
                            this.MoveItemInList(index, i);
                        }
                    }
                    ValidateInput();
                }
            }
        }

        private FeatureLayerInfo GetLayerInfo(DataSource dataSource)
        {
            MapWidget mw = MapWidget.FindMapWidget(dataSource);
            client.FeatureLayer featurelayer = mw.FindFeatureLayer(dataSource);
            FeatureLayerInfo LayerInfo = featurelayer.LayerInfo;
            return LayerInfo;
        }

        private void GenerateFieldList(string[] initialFields, client.Field field, bool dependsOnSubType)
        {
            FieldValue value = new FieldValue();
            string initialValue = (initialFields != null) ? initialFields.FirstOrDefault(f => f == field.FieldName) : null;

            value.IsSelected = initialValue != null; //false
            value.FieldName = field.Name;
            value.FieldAlias = field.Alias;
            value.DependsOnSubType = dependsOnSubType;

            this.FieldsList.Add(value);
        }

        private void ValidateInput()
        {
            if (OKButton == null)
                return;

            OKButton.IsEnabled = false;

            if (string.IsNullOrEmpty(CaptionTextBox.Text))
                return;

            if (string.IsNullOrEmpty(OkButtonTextBox.Text))
                return;

            if (string.IsNullOrEmpty(ClearButtonTextBox.Text))
                return;

            if (this.DataSourceSelector.SelectedDataSource == null)
                return;

            if (FieldsList.Count == 0)
                return;

            if (this.FieldHasNoSelection())
                return;

            OKButton.IsEnabled = true;
        }

        private bool FieldHasNoSelection()
        {
            IList<FieldValue> list = this.LstBxFields.Items.Cast<FieldValue>().ToList();
            return (list.Where(v => v.IsSelected).Count() == 0);
        }

        private void MoveItemInList(int selectedIndex, int insertAt)
        {
            var itemToMove = FieldsList[selectedIndex];
            this.FieldsList.RemoveAt(selectedIndex);
            this.FieldsList.Insert(insertAt, itemToMove);
        }
    }
}
