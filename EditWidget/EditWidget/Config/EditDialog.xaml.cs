using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using System.Windows.Media;
using EditWidget.Objects;
using System.Threading.Tasks;

namespace EditWidget.Config
{
    /// <summary>
    /// Interaction logic for EditDialog.xaml
    /// </summary>
    public partial class EditDialog : Window
    {
        private static readonly string READONLY_CHECKBOX_POSTFIX = "IsReadOnly";
        private static readonly string VISIBILITY_CHECKBOX_POSTFIX = "IsVisible";

        public DataSource DataSource { get; private set; }

        public bool IsReadOnly { get; private set; }

        public bool AllIsVisible { get; private set; }

        public IList<FieldSetting> FieldSettings { get; set; }

        public string Caption { get; private set; }

        public string SaveButton { get; private set; }

        public string DeleteButton { get; private set; }

        private CheckBox ChkAllReadOnly { get; set; }

        private CheckBox ChkAllVisible { get; set; }

        public EditDialog(IList<DataSource> dataSources, string initialCaption, string initialDataSourceId, IList<FieldSetting> initialFieldSettings, bool initialIsReadonly, bool initialIsVisible, string initialSaveText, string initialDeleteText)
        {
            try
            {
                InitializeComponent();

                this.TxtErrorMsg.Visibility = System.Windows.Visibility.Collapsed;

                this.Title = "Configuration View/Modify Attributes";
                // When re-configuring, initialize the widget config dialog from the existing settings.
                CaptionTextBox.Text = initialCaption;

                TxtSaveTitle.Text = initialSaveText;

                TxtDeleteTitle.Text = initialDeleteText;

                if (!string.IsNullOrEmpty(initialDataSourceId))
                {
                    if (OperationsDashboard.Instance != null)
                    {
                        DataSource dataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == initialDataSourceId);
                        if (dataSource != null)
                        {
                            if (initialFieldSettings != null && initialFieldSettings.Count() > 0)
                            {
                                this.FieldSettings = initialFieldSettings;
                            }

                            DataSourceSelector.SelectedDataSource = dataSource;

                            this.TriggerReadOnlyEvent = false;

                            this.TriggerVisibilityEvent = false;

                            ChkAllReadOnly.IsChecked = initialIsReadonly;

                            ChkAllVisible.IsChecked = initialIsVisible;

                            this.TriggerReadOnlyEvent = true;

                            this.TriggerVisibilityEvent = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                this.TxtErrorMsg.Text = "An error occured while loading the saved settings. Default settings are loaded.";
                this.TxtErrorMsg.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.FieldSettings = this.GetFieldSettings();
            this.DataSource = DataSourceSelector.SelectedDataSource;
            this.Caption = CaptionTextBox.Text;
            this.DeleteButton = TxtDeleteTitle.Text;
            this.SaveButton = TxtSaveTitle.Text;
            this.IsReadOnly = ChkAllReadOnly.IsChecked.Value;
            this.AllIsVisible = ChkAllVisible.IsChecked.Value;
            this.DialogResult = true;
        }

        private IList<FieldSetting> GetFieldSettings()
        {
            IList<FieldSetting> list = new List<FieldSetting>();

            foreach (var field in this.DataSourceSelector.SelectedDataSource.Fields)
            {
                if (field.Type == client.Field.FieldType.OID)
                    continue;

                FieldSetting setting = new FieldSetting();
                setting.FieldName = field.FieldName;

                string readonlyName = string.Format("{0}{1}", field.FieldName, READONLY_CHECKBOX_POSTFIX);
                CheckBox roc = this.FieldAvailabilityGrid.FindName(readonlyName) as CheckBox;
                setting.IsReadOnly = roc.IsChecked.Value;

                string visibilityName = string.Format("{0}{1}", field.FieldName, VISIBILITY_CHECKBOX_POSTFIX);
                CheckBox vc = this.FieldAvailabilityGrid.FindName(visibilityName) as CheckBox;
                setting.IsVisible = vc.IsChecked.Value;

                list.Add(setting);
            }

            return list;
        }

        private void ValidateInput(object sender, TextChangedEventArgs e)
        {
            this.ValidateInput();
        }

        private void ValidateInput()
        {
            if (OKButton == null)
                return;

            OKButton.IsEnabled = false;
            if (string.IsNullOrEmpty(CaptionTextBox.Text))
                return;

            if (string.IsNullOrEmpty(TxtSaveTitle.Text))
                return;

            if (string.IsNullOrEmpty(TxtDeleteTitle.Text))
                return;

            IList<FieldSetting> fieldSettings = this.GetFieldSettings();

            if (fieldSettings.Where(c => c.IsVisible).Count() == 0)
                return;

            OKButton.IsEnabled = true;
        }

        private void DataSourceSelector_SelectionChanged(object sender, EventArgs e)
        {
            DataSource dataSource = DataSourceSelector.SelectedDataSource;

            this.FieldAvailabilityGrid.Children.Clear();
            this.FieldAvailabilityGrid.RowDefinitions.Clear();

            this.AddDefaultRows();

            for (int i = 0; i < dataSource.Fields.Count(); i++)
            {
                client.Field field = dataSource.Fields.Single(f => f == dataSource.Fields[i]);

                if (field.Type == client.Field.FieldType.OID)
                    continue;

                FieldSetting initialFieldSetting = null;
                if (this.FieldSettings != null)
                {
                    initialFieldSetting = this.FieldSettings.SingleOrDefault(f => f.FieldName == field.FieldName);
                }

                this.CreateRowDefinitions(1);

                //create textblock
                string textName = string.Format("Txt{0}", field.FieldName);
                this.CreateTextblock(textName, field.Alias, FontWeights.Normal, FontStyles.Normal, 0, i + 2);

                //create readonly checkbox
                string roChkName = string.Format("{0}{1}", field.FieldName, READONLY_CHECKBOX_POSTFIX);
                bool roIsChecked = (initialFieldSetting != null) ? initialFieldSetting.IsReadOnly : false;
                bool roIsEnabled = (initialFieldSetting != null) ? initialFieldSetting.IsVisible : true;
                this.CreateCheckBox(roChkName, roIsChecked, roIsEnabled, 1, i + 2, ChkBoxReadOnly_CheckChanged, ChkBoxReadOnly_CheckChanged);

                //create visible checkbox
                string visChkName = string.Format("{0}{1}", field.FieldName, VISIBILITY_CHECKBOX_POSTFIX);
                bool visIsChecked = (initialFieldSetting != null) ? initialFieldSetting.IsVisible : true;
                bool visIsEnabled = true;
                this.CreateCheckBox(visChkName, visIsChecked, visIsEnabled, 2, i + 2, ChkBoxIsVisible_Unchecked, ChkBoxIsVisible_Checked);
            }
        }

        private void AddDefaultRows()
        {
            this.CreateRowDefinitions(3);

            this.CreateTextblock("TxtFieldName", "Fieldname", FontWeights.Bold, FontStyles.Normal, 0, 0);
            this.CreateTextblock("TxtReadOnly", "Read-only", FontWeights.Bold, FontStyles.Normal, 1, 0);
            this.CreateTextblock("TxtVisible", "Visible", FontWeights.Bold, FontStyles.Normal, 2, 0);
            this.CreateTextblock("TxtFields" , "All fields", FontWeights.Normal, FontStyles.Italic, 0, 1);

            this.ChkAllReadOnly = this.CreateCheckBox("ChkAllReadOnly", false, true, 1, 1, ChkAllReadOnly_Unchecked, ChkAllReadOnly_Checked);

            this.ChkAllVisible = this.CreateCheckBox("ChkAllVisible", false, true, 2, 1, ChkAllVisible_Unchecked, ChkAllVisible_Checked);
        }

        private void CreateTextblock(string name, string text, FontWeight fontWeight, FontStyle fontStyle, int columnNumber, int rowNumber)
        {
            TextBlock txtblock = new TextBlock()
            {
                Text = text,
                FontWeight = fontWeight,
                FontStyle = fontStyle,
                Name = name,
                Margin = new Thickness(5),
                Style = Application.Current.Resources["DisplayNameStyle"] as Style,
                Foreground = Application.Current.Resources["ThemedSecondaryTextBrush"] as Brush,
                FontSize = (double)Application.Current.Resources["ThemedTextSize"],
                VerticalAlignment = System.Windows.VerticalAlignment.Bottom
            };

            this.FieldAvailabilityGrid.Children.Add(txtblock);

            Grid.SetRow(txtblock, rowNumber);
            Grid.SetColumn(txtblock, columnNumber);
        }

        private CheckBox CreateCheckBox(string name, bool isChecked, bool isEnabled, int columnNumber, int rowNumber, RoutedEventHandler uncheckedEvent = null, RoutedEventHandler checkedEvent = null)
        {
            CheckBox chkBox = new CheckBox();
            chkBox.IsChecked = isChecked;
            chkBox.Name = name;
            chkBox.IsEnabled = isEnabled;
            chkBox.Margin = new Thickness(5);
            if (uncheckedEvent != null)
            {
                chkBox.Unchecked += uncheckedEvent;
            }

            if (checkedEvent != null)
            {
                chkBox.Checked += checkedEvent;
            }

            chkBox.Margin = new Thickness(5);

            this.FieldAvailabilityGrid.Children.Add(chkBox);

            Grid.SetRow(chkBox, rowNumber);
            Grid.SetColumn(chkBox, columnNumber);

            if (this.FieldAvailabilityGrid.FindName(chkBox.Name) != null)
            {
                this.FieldAvailabilityGrid.UnregisterName(chkBox.Name);
            }
            this.FieldAvailabilityGrid.RegisterName(chkBox.Name, chkBox);

            return chkBox;
        }

        private void CreateRowDefinitions(int numberOfRowsToCreate)
        {
            for (int i = 0; i < numberOfRowsToCreate; i++)
            {
                RowDefinition row = new RowDefinition();
                row.Height = GridLength.Auto;
                this.FieldAvailabilityGrid.RowDefinitions.Add(row);
            }
        }

        private void ChkBoxReadOnly_CheckChanged(object sender, RoutedEventArgs e)
        {
            IList<FieldSetting> allItems = this.GetFieldSettings();

            IList<FieldSetting> selectedItems = allItems.Where(c => c.IsReadOnly).ToList();

            this.TriggerReadOnlyEvent = false;

            this.ChkAllReadOnly.IsChecked = (selectedItems.Count >= allItems.Count);

            this.TriggerReadOnlyEvent = true;
        }

        private void ChkBoxIsVisible_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox chkbx = sender as CheckBox;
            ToggleReadOnlyCheckboxUsability(chkbx, true);
        }

        private void ChkBoxIsVisible_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox chkbx = sender as CheckBox;
            ToggleReadOnlyCheckboxUsability(chkbx, false);
        }

        private void ToggleReadOnlyCheckboxUsability(CheckBox chkbx, bool isEnabled)
        {
            IList<FieldSetting> allItems = this.GetFieldSettings();

            IList<FieldSetting> selectedItems = allItems.Where(c => c.IsVisible).ToList();

            this.TriggerVisibilityEvent = false;

            this.ChkAllVisible.IsChecked = (selectedItems.Count >= allItems.Count);

            this.TriggerVisibilityEvent = true;

            string name = chkbx.Name.Substring(0, chkbx.Name.Length - VISIBILITY_CHECKBOX_POSTFIX.Length);
            string visibilityName = string.Format("{0}{1}", name, READONLY_CHECKBOX_POSTFIX);
            CheckBox roc = this.FieldAvailabilityGrid.FindName(visibilityName) as CheckBox;
            if (isEnabled)
            {
                roc.IsChecked = this.ChkAllReadOnly.IsChecked.Value;
            }
            else
            {
                roc.IsChecked = false;
            }
            roc.IsEnabled = isEnabled;

            this.ValidateInput();
        }

        private bool TriggerReadOnlyEvent = true;
        private bool TriggerVisibilityEvent = true;

        private void ChkAllReadOnly_Checked(object sender, RoutedEventArgs e)
        {
            bool isChecked = true;
            this.ToggleSelectAllReadonly(isChecked);
        }

        private void ChkAllReadOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            bool isChecked = false;
            this.ToggleSelectAllReadonly(isChecked);
        }

        private void ToggleSelectAllReadonly(bool isChecked)
        {
            if (this.TriggerReadOnlyEvent && this.ChkAllReadOnly.IsChecked.HasValue)
            {
                foreach (var field in this.DataSourceSelector.SelectedDataSource.Fields)
                {
                    if (field.Type == client.Field.FieldType.OID)
                        continue;

                    FieldSetting setting = new FieldSetting();
                    setting.FieldName = field.FieldName;

                    string readonlyName = string.Format("{0}{1}", field.FieldName, READONLY_CHECKBOX_POSTFIX);
                    CheckBox roc = this.FieldAvailabilityGrid.FindName(readonlyName) as CheckBox;
                    if (isChecked)
                    {
                        string visibleName = string.Format("{0}{1}", field.FieldName, VISIBILITY_CHECKBOX_POSTFIX);
                        CheckBox vc = this.FieldAvailabilityGrid.FindName(visibleName) as CheckBox;
                        if (vc.IsChecked.Value)
                        {
                            roc.IsChecked = isChecked;
                        }
                    }
                    else
                    {
                        roc.IsChecked = isChecked;
                    }
                }
            }

            this.ValidateInput();
        }

        private void ChkAllVisible_Checked(object sender, RoutedEventArgs e)
        {
            bool isChecked = true;
            this.ToggleSelectAllVisible(isChecked);
        }

        private void ChkAllVisible_Unchecked(object sender, RoutedEventArgs e)
        {
            bool isChecked = false;
            this.ToggleSelectAllVisible(isChecked);
        }

        private void ToggleSelectAllVisible(bool isChecked)
        {
            if (this.TriggerVisibilityEvent && this.ChkAllReadOnly.IsChecked.HasValue)
            {
                foreach (var field in this.DataSourceSelector.SelectedDataSource.Fields)
                {
                    if (field.Type == client.Field.FieldType.OID)
                        continue;

                    FieldSetting setting = new FieldSetting();
                    setting.FieldName = field.FieldName;

                    string readonlyName = string.Format("{0}{1}", field.FieldName, VISIBILITY_CHECKBOX_POSTFIX);
                    CheckBox vc = this.FieldAvailabilityGrid.FindName(readonlyName) as CheckBox;
                    vc.IsChecked = isChecked;
                }
            }
        }
    }
}
