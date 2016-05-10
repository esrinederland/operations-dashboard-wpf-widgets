using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using ExportWidget.Objects;
using System.Windows.Forms;
using System.IO;
using ESRI.ArcGIS.Client.Portal;
using ESRI.ArcGIS.Client.Printing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ExportWidget.Helpers;

namespace ExportWidget.Config
{
    /// <summary>
    /// Interaction logic for ExportDialog.xaml
    /// </summary>
    public partial class ExportDialog : Window
    {
        private const string VALIDIMGSOURCE = @"pack://application:,,,/ExportWidget;component/Images/check.png";
        private const string ERRORIMGSOURCE = @"pack://application:,,,/ExportWidget;component/Images/error.png";

        #region Properties
        private bool CanExecute { get; set; }

        private bool IsValidPrintUrl { get; set; }

        private bool PrintUrlSet { get; set; }

        //private PrintTask PrintTask { get; set; }

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

        IList<FieldValue> SelectedFields { get; set; }

        public ConfigSettings ConfigSettings { get; set; }

        public DataSource DataSource { get; private set; }

        public ESRI.ArcGIS.Client.Field Field { get; private set; }

        public string Caption { get; private set; }

        #endregion

        public ExportDialog(ConfigSettings settings)
        {
            InitializeComponent();

            this.PrintUrlSet = false;

            if (settings != null)
            {
                if (!string.IsNullOrEmpty(settings.PrintTaskUrl))
                    this.PrintUrlSet = true;

                this.ChkExportAttachments.IsChecked = settings.ExportAttachments;

                this.ChkExportMapImage.IsChecked = settings.ExportMapImage;

                if (!string.IsNullOrEmpty(settings.DataSourceId))
                {
                    OperationsDashboard instance = OperationsDashboard.Instance;
                    if (instance != null)
                    {
                        DataSource dataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == settings.DataSourceId);

                        if (dataSource != null)
                        {
                            DataSourceSelector.SelectedDataSource = dataSource;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(settings.Title))
                    this.CaptionTextBox.Text = settings.Title;

                if (!string.IsNullOrEmpty(settings.ExportButtonText))
                    this.TxtExportTitle.Text = settings.ExportButtonText;

                if (!string.IsNullOrEmpty(settings.CancelButtonText))
                    this.TxtCancelTitle.Text = settings.CancelButtonText;

                if (!string.IsNullOrEmpty(settings.ExportFolderPath))
                    this.TxtFilePath.Text = settings.ExportFolderPath;

                if (settings.ExportMapImage && !string.IsNullOrEmpty(settings.PrintTaskUrl))
                {
                    this.TxtPrintUrl.Text = settings.PrintTaskUrl;

                    this.TxtPrintUrl.IsEnabled = true;

                    this.BtnValidate.IsEnabled = true;

                    this.ValidatePrintTaskUrlAndUpdateComboBoxes();

                    if (this.IsValidPrintUrl)
                    {
                        if (!string.IsNullOrEmpty(settings.PrintFormat))
                        {
                            ComboFormat.SelectedValue = settings.PrintFormat;
                        }

                        if (!string.IsNullOrEmpty(settings.PrintLayout))
                        {
                            ComboLayout.SelectedValue = settings.PrintLayout;
                        }

                        TxtScale.Text = settings.Scale.ToString();
                    }
                }

                if (settings.Export)
                {
                    this.SelectedFields = settings.SelectedFields;
                    this.ChkExportCSV.IsChecked = settings.Export;
                }
            }
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (this.DataSourceSelector.SelectedDataSource != null)
            {
                DataSource ds = this.DataSourceSelector.SelectedDataSource;

                this.ToggleAttachmentCheckBox(ds);
            }
        }

        //Check if layer has attachments
        private void ToggleAttachmentCheckBox(DataSource ds)
        {
            MapWidget widget = MapWidget.FindMapWidget(ds);

            client.FeatureLayer fl = widget.FindFeatureLayer(ds);

            if (!fl.LayerInfo.HasAttachments)
            {
                this.ChkExportAttachments.IsEnabled = false;
            }
            else
            {
                this.ChkExportAttachments.IsEnabled = true;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigSettings settings = new ConfigSettings()
            {
                Title = this.CaptionTextBox.Text,
                Export = ChkExportCSV.IsChecked.Value,
                DataSourceId = DataSourceSelector.SelectedDataSource.Id,
                SelectedFields = this.GetSelectedExportFields(),
                ExportAttachments = ChkExportAttachments.IsChecked.Value,
                ExportMapImage = ChkExportMapImage.IsChecked.Value,
                PrintTaskUrl = (ChkExportMapImage.IsChecked.Value) ? TxtPrintUrl.Text : string.Empty,
                PrintFormat = (ComboFormat.SelectedValue != null) ? ComboFormat.SelectedValue.ToString() : string.Empty,
                PrintLayout = (ComboLayout.SelectedValue != null) ? ComboLayout.SelectedValue.ToString() : string.Empty,
                Scale = (ChkExportMapImage.IsChecked.Value) ? Convert.ToDouble(TxtScale.Text) : 0,
                ExportFolderPath = TxtFilePath.Text,
                ExportButtonText = TxtExportTitle.Text,
                CancelButtonText = TxtCancelTitle.Text
            };

            this.ConfigSettings = settings;

            DialogResult = true;
        }

        private IList<FieldValue> GetSelectedExportFields()
        {
            IList<FieldValue> list = this.LstBxFields.Items
                .Cast<FieldValue>()
                .Where(f => f.IsSelected)
                .ToList();

            list.IncludeObjectID();

            return list;
        }

        //private bool ValidatePrintTaskUrl()
        //{
        //    if (ChkExportMapImage.IsChecked.Value
        //        && !string.IsNullOrEmpty(TxtPrintUrl.Text))
        //    {
        //        try
        //        {
        //            PrintTask task = new PrintTask(TxtPrintUrl.Text);

        //            PrintServiceInfo info = task.GetServiceInfo();

        //            this.PrintTask = task;

        //            return true;
        //        }
        //        catch (Exception)
        //        {
        //            return false;
        //        }
        //    }
        //    return false;
        //}

        private void DataSourceSelector_SelectionChanged(object sender, EventArgs e)
        {
            DataSource dataSource = DataSourceSelector.SelectedDataSource;

            if (this.ChkExportCSV.IsChecked.Value)
            {
                this.BindFieldsList();
            }

            this.ToggleAttachmentCheckBox(dataSource);

            ValidateInput();

        }

        private void ChkExportCSV_Checked(object sender, RoutedEventArgs e)
        {
            this.BindFieldsList();
        }

        private void BindFieldsList()
        {
            this.LstBxFields.Visibility = System.Windows.Visibility.Visible;
            this.LblFields.Visibility = System.Windows.Visibility.Visible;
            this.ChkSelectAll.Visibility = System.Windows.Visibility.Visible;

            this.LstBxFields.ItemsSource = this.GetItemsSource();

            IList<FieldValue> selectedItems = this.LstBxFields.Items
                .Cast<FieldValue>()
                .Where(f => f.IsSelected)
                .ToList();

            this.ChkSelectAll.IsChecked = (selectedItems.Count() >= this.LstBxFields.Items.Count);

            ValidateInput();
        }

        private void ChkExportCSV_Unchecked(object sender, RoutedEventArgs e)
        {
            ValidateInput();

            this.LstBxFields.Visibility = System.Windows.Visibility.Collapsed;
            this.LblFields.Visibility = System.Windows.Visibility.Collapsed;
            this.ChkSelectAll.Visibility = System.Windows.Visibility.Collapsed;

            this.LstBxFields.ItemsSource = null;
        }

        private void ChkExportMapImage_Checked(object sender, RoutedEventArgs e)
        {
            ValidateInput();

            this.GrdMapImage.Visibility = System.Windows.Visibility.Visible;

            if (!this.PrintUrlSet)
                this.GetPrintUrl();
        }

        private void GetPrintUrl()
        {
            ArcGISPortal portal = new ArcGISPortal();
            portal.InitializeAsync("http://www.arcgis.com/sharing", PortalInitialized);
        }

        private void PortalInitialized(ArcGISPortal portal, Exception e)
        {
            if (e == null
                && portal.ArcGISPortalInfo.HelperServices.PrintTaskService != null)
            {
                this.TxtPrintUrl.Text = portal.ArcGISPortalInfo.HelperServices.PrintTaskService.Url;
            }
            else
            {
                this.TxtPrintUrl.Text = string.Empty;
            }

            this.TxtPrintUrl.IsEnabled = true;
            this.BtnValidate.IsEnabled = true;
        }

        private void ChkExportMapImage_Unchecked(object sender, RoutedEventArgs e)
        {
            ValidateInput();

            this.GrdMapImage.Visibility = System.Windows.Visibility.Collapsed;
        }

        private ObservableCollection<FieldValue> GetItemsSource()
        {
            if (DataSourceSelector.SelectedDataSource != null)
            {
                this.FieldsList.Clear();

                foreach (var field in this.DataSourceSelector.SelectedDataSource.Fields)
                {
                    if (field.Type == client.Field.FieldType.OID)
                        continue;

                    FieldValue value = new FieldValue();

                    FieldValue selectedField = null;
                    if (this.SelectedFields != null)
                    {
                        selectedField = this.SelectedFields.SingleOrDefault(s => s.FieldName == field.Name);
                        value.IsSelected = (selectedField != null) ? selectedField.IsSelected : false;
                    }
                    else
                    {
                        value.IsSelected = true;
                    }

                    value.FieldName = field.Name;
                    value.FieldAlias = field.Alias;

                    this.FieldsList.Add(value);
                }
            }

            return this.FieldsList;
        }

        private void OpenFileDialog_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fb = new FolderBrowserDialog();

            fb.Description = "Select the directory that you want to use to store your files.";
            fb.ShowNewFolderButton = true;
            fb.RootFolder = Environment.SpecialFolder.MyComputer;

            DialogResult userSelectedFolder = fb.ShowDialog();

            if (userSelectedFolder == System.Windows.Forms.DialogResult.OK)
            {
                TxtFilePath.Text = fb.SelectedPath;
            }
        }

        private void ValidateInput(object sender, EventArgs e)
        {
            this.ValidateInput();
        }

        private void ValidateInput()
        {
            if (OKButton == null)
                return;

            this.OKButton.IsEnabled = false;

            if (string.IsNullOrEmpty(CaptionTextBox.Text))
                return;

            if (DataSourceSelector.SelectedDataSource == null)
                return;

            IList<FieldValue> list = this.LstBxFields.Items
                .Cast<FieldValue>()
                .Where(f => f.IsSelected)
                .ToList();

            if (!ChkExportCSV.IsChecked.Value
                && !ChkExportAttachments.IsChecked.Value
                && !ChkExportMapImage.IsChecked.Value)
                return;

            if (ChkExportCSV.IsChecked.Value
                && list.Count() == 0)
                return;

            if (ChkExportMapImage.IsChecked.Value &&
                !this.IsValidPrintUrl)
                return;

            if (ChkExportMapImage.IsChecked.Value
                && string.IsNullOrEmpty(TxtPrintUrl.Text)
                && ComboFormat.SelectedValue == null
                && ComboLayout.SelectedValue == null)
                return;


            if (ChkExportMapImage.IsChecked.Value && string.IsNullOrEmpty(TxtScale.Text))
                return;

            double scale;
            bool isDouble = Double.TryParse(TxtScale.Text, out scale);
            if (ChkExportMapImage.IsChecked.Value && !isDouble)
                return;


            if (string.IsNullOrEmpty(TxtFilePath.Text))
                return;

            if (string.IsNullOrEmpty(TxtFilePath.Text)
                || !Directory.Exists(TxtFilePath.Text))
                return;

            if (string.IsNullOrEmpty(TxtExportTitle.Text))
                return;

            if (string.IsNullOrEmpty(TxtCancelTitle.Text))
                return;

            this.OKButton.IsEnabled = true;
        }

        private void ValidateNumericInput(object sender, RoutedEventArgs e)
        {
            double scale;
            bool isDouble = Double.TryParse(TxtScale.Text, out scale);
            if (!isDouble)
                this.LblInvalidInputMsg.Text = "Only numbers are allowed!";
            else
                this.LblInvalidInputMsg.Text = string.Empty;

            this.ValidateInput();
        }

        private void BtnValidate_Click(object sender, RoutedEventArgs e)
        {
            ValidatePrintTaskUrlAndUpdateComboBoxes();
        }

        private void ValidatePrintTaskUrlAndUpdateComboBoxes()
        {
            this.IsValidPrintUrl = PrintValidator.Validate(TxtPrintUrl.Text);

            if (this.IsValidPrintUrl)
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();

                img.UriSource = new Uri(VALIDIMGSOURCE);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                this.ImgValid.Source = img;
                this.SetComboBoxDataSources();
            }
            else
            {
                System.Windows.MessageBox.Show("Invalid PrintTask url or the server does not respond.", "Error in the PrintTask url", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetComboBoxDataSources()
        {
            PrintServiceInfo info = PrintValidator.PrintTask.GetServiceInfo();

            this.ComboLayout.ItemsSource = null;
            this.ComboLayout.ItemsSource = info.LayoutTemplates;
            this.ComboLayout.IsEnabled = true;

            this.ComboFormat.ItemsSource = null;
            this.ComboFormat.ItemsSource = info.Formats;
            this.ComboFormat.IsEnabled = true;
        }

        private void ComboFormat_SelectionChanged(object sender, EventArgs e)
        {
            ValidateInput();
        }

        private void ComboLayout_SelectionChanged(object sender, EventArgs e)
        {
            ValidateInput();
        }

        private void TxtPrintUrl_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (this.ImgValid != null
                && this.ComboFormat != null
                && this.ComboLayout != null)
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();

                img.UriSource = new Uri(ERRORIMGSOURCE);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                this.ImgValid.Source = img;

                ComboFormat.ItemsSource = null;
                ComboFormat.IsEnabled = false;

                ComboLayout.ItemsSource = null;
                ComboLayout.IsEnabled = false;

                ValidateInput();
            }
        }

        private bool TriggerEvent = true;

        private void ToggleSelectAll(object sender, RoutedEventArgs e)
        {
            if (TriggerEvent && this.ChkSelectAll.IsChecked.HasValue)
            {
                IList<FieldValue> items = this.LstBxFields.Items.Cast<FieldValue>().ToList();

                IList<FieldValue> updatedItems = new List<FieldValue>();
                for (int i = 0; i < items.Count(); i++)
                {
                    FieldValue value = items[i];
                    value.IsSelected = this.ChkSelectAll.IsChecked.Value;
                    updatedItems.Add(value);
                }

                this.LstBxFields.ItemsSource = updatedItems;
            }

            ValidateInput();
        }

        private void FieldValueCheckChanged()
        {
            IList<FieldValue> selectedItems = this.LstBxFields.Items
                .Cast<FieldValue>()
                .Where(f => f.IsSelected)
                .ToList();

            this.TriggerEvent = false;

            this.ChkSelectAll.IsChecked = (selectedItems.Count >= this.LstBxFields.Items.Count);

            this.TriggerEvent = true;

            ValidateInput();
        }

        private void FieldValue_Unchecked(object sender, RoutedEventArgs e)
        {
            this.FieldValueCheckChanged();
        }

        private void FieldValue_Checked(object sender, RoutedEventArgs e)
        {
            this.FieldValueCheckChanged();
        }
    }
}
