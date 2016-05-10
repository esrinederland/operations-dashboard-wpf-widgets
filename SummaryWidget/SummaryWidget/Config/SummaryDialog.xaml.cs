using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using SummaryWidget.Objects;

namespace SummaryWidget.Config
{
    /// <summary>
    /// Interaction logic for SummaryDialog.xaml
    /// </summary>
    public partial class SummaryDialog : Window
    {
        private const string DISPLAYTYPE_STATISTIC = "Statistics";
        private const string DISPLAYTYPE_COUNT = "Count";

        private bool SumFieldIsEnabled { get; set; }

        public SumData SumData { get; set; }

        public SummaryDialog(IList<DataSource> dataSources,
            string initialCaption,
            string initialDataSourceId,
            string initialGroupByFieldName,
            string initialGroupByFieldHeader,
            string initialSumFieldName,
            string initialSumFieldHeader)
        {
            InitializeComponent();
            this.Title = "Configuration Summary Group By";
            // When re-configuring, initialize the widget config dialog from the existing settings.
            CaptionTextBox.Text = initialCaption;
            GroupByFieldTextBox.Text = initialGroupByFieldHeader;
            StatisticsFieldTextBox.Text = initialSumFieldHeader;

            if (!string.IsNullOrEmpty(initialDataSourceId))
            {
                DataSource dataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == initialDataSourceId);
                if (dataSource != null)
                {
                    DataSourceSelector.SelectedDataSource = dataSource;

                    client.Field initialGroupByField = dataSource.Fields.FirstOrDefault(f => f.FieldName == initialGroupByFieldName);
                    if (initialGroupByField != null)
                    {
                        this.FillComboBox(dataSource.Fields, GroupbyFieldComboBox, initialGroupByField); 
                    }

                    client.Field initialSumField = dataSource.Fields.FirstOrDefault(f => f.FieldName == initialSumFieldName);
                    if (initialSumField != null)
                    {
                        this.FillComboBox(dataSource.Fields.Where(f => this.IsNumeric(f.Type)), SumComboBox, initialSumField);
                        this.SetComboBoxAvailability(DISPLAYTYPE_STATISTIC);
                        this.DisplayTypeComboBox.SelectedItem = DISPLAYTYPE_STATISTIC;
                        this.DisplayTypeComboBox.Text = DISPLAYTYPE_STATISTIC;
                    }
                    else
                    {
                        this.DisplayTypeComboBox.Text = DISPLAYTYPE_COUNT;
                        this.SetComboBoxAvailability(DISPLAYTYPE_COUNT);
                        this.SumComboBox.ItemsSource = null;
                    }
                }
            }
        }

        private void SetComboBoxAvailability(string selectedValue)
        {
            if (selectedValue == DISPLAYTYPE_STATISTIC)
            {
                this.SumFieldIsEnabled = true;
                this.StatisticsFieldLabel.Text = "Sum field header";
            }
            else
            {
                this.SumComboBox.ItemsSource = null;
                this.SumFieldIsEnabled = false;
                this.StatisticsFieldLabel.Text = "Count field header";
            }
            this.SumComboBox.IsEnabled = this.SumFieldIsEnabled;
        }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ValidateUserInput();

                SumData = new SumData();

                this.SumData.DataSource = DataSourceSelector.SelectedDataSource;
                this.SumData.GroupByField = (ESRI.ArcGIS.Client.Field)GroupbyFieldComboBox.SelectedItem;
                this.SumData.SumField = (this.SumFieldIsEnabled) ? (ESRI.ArcGIS.Client.Field)SumComboBox.SelectedItem : null;

                this.SumData.Caption = CaptionTextBox.Text;
                this.SumData.GroupByFieldHeader = GroupByFieldTextBox.Text;
                this.SumData.StatisticsFieldHeader = StatisticsFieldTextBox.Text;

                DialogResult = true;
            }
            catch (ApplicationException ex)
            {
                this.ErrorMessageTextBlock.Text = ex.Message.ToString();
            }
        }

        private void ValidateUserInput()
        {
            if (DataSourceSelector.SelectedDataSource == null)
            {
                throw new ApplicationException("No valid 'Data Source' is selected.");
            }

            if (GroupbyFieldComboBox.SelectedItem == null)
            {
                throw new ApplicationException("No valid 'Group by field' is selected.");
            }

            if (this.SumFieldIsEnabled && this.SumComboBox.SelectedItem == null)
            {
                throw new ApplicationException("No valid 'Sum field' is selected.");
            }

            if (string.IsNullOrEmpty(CaptionTextBox.Text))
            {
                throw new ApplicationException("'Title' is empty.");
            }

            if (string.IsNullOrEmpty(GroupByFieldTextBox.Text))
            {
                throw new ApplicationException("'Group by field header' is empty.");
            }

            if (string.IsNullOrEmpty(StatisticsFieldTextBox.Text))
            {
                throw new ApplicationException("'Statistics field header' is empty.");
            }
        }


        private void DataSourceSelector_SelectionChanged(object sender, EventArgs e)
        {
            DataSource dataSource = DataSourceSelector.SelectedDataSource;
            this.FillComboBox(dataSource.Fields, GroupbyFieldComboBox, dataSource.Fields[0]);
            this.FillComboBox(dataSource.Fields.Where(f => this.IsNumeric(f.Type)), SumComboBox, null);
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

        private void DisplayTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SumComboBox != null)
            {
                var combobox = (ComboBox)sender;
                ContentControl content = (ContentControl)combobox.SelectedItem;
                IEnumerable<client.Field> fields = DataSourceSelector.SelectedDataSource.Fields.Where(f => this.IsNumeric(f.Type));
                this.FillComboBox(fields, this.SumComboBox, DataSourceSelector.SelectedDataSource.Fields[0]); 
                this.SetComboBoxAvailability(content.Content.ToString());
            }
            ValidateInput(sender, e);
        }

        private void FillComboBox(IEnumerable<client.Field> fields, ComboBox comboBox, client.Field selectedItem) 
        {
            comboBox.ItemsSource = fields;
            if (selectedItem != null)
            {
                comboBox.SelectedItem = selectedItem;
            }
        }

        private void ValidateInput(object sender, EventArgs e)
        {
            if (OKButton == null)
                return;

            OKButton.IsEnabled = false;
            if (string.IsNullOrEmpty(CaptionTextBox.Text))
                return;

            if (DataSourceSelector.SelectedDataSource == null)
                return;

            if (GroupbyFieldComboBox.SelectedItem == null)
                return;

            if (this.SumFieldIsEnabled && this.SumComboBox.SelectedItem == null)
                return;

            if (string.IsNullOrEmpty(CaptionTextBox.Text))
                return;

            if (string.IsNullOrEmpty(GroupByFieldTextBox.Text))
                return;

            if (string.IsNullOrEmpty(StatisticsFieldTextBox.Text))
                return;
            OKButton.IsEnabled = true;
        }
    }
}
