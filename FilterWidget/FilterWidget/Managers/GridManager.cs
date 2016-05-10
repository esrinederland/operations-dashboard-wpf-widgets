using client = ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.FeatureService;
using ESRI.ArcGIS.OperationsDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using FilterWidget.Helpers;

namespace FilterWidget.Managers
{
    public class GridManager
    {
        #region properties
        private DataSource DataSource { get; set; }

        private FeatureLayerInfo FeatureLayerInfo { get; set; }

        private Grid Grid { get; set; }

        private string[] SelectedFields { get; set; }

        private Dictionary<string, string> SelectedFieldValues { get; set; }

        private string DataSourceID { get; set; }
        #endregion

        public GridManager(Grid grid, DataSource dataSource, string datasourceID, FeatureLayerInfo featureLayerInfo, string[] selectedFields, Dictionary<string, string> selectedFieldValues)
        {
            this.DataSource = dataSource;

            this.FeatureLayerInfo = featureLayerInfo;

            this.Grid = grid;

            this.SelectedFields = selectedFields;

            this.SelectedFieldValues = selectedFieldValues;

            this.DataSourceID = datasourceID;
        }

        public void FillGrid()
        {
            this.Grid.Children.Clear();

            if (this.SelectedFields != null
                && this.SelectedFields.Count() > 0)
            {
                for (int i = 0; i < this.SelectedFields.Count(); i++)
                {
                    this.AddGridRow();

                    client.Field field = this.DataSource.Fields.Single(f => f.FieldName == SelectedFields[i]);

                    string textName = string.Format("{0}TextBlock", field.FieldName);
                    TextBlock txtBlock = this.CreateTextBlock(textName, field.Alias, 5);

                    string comboName = string.Format("{0}ComboBox", field.FieldName);
                    ComboBox comboBox = this.CreateComboBox(comboName, field, 5);

                    string typeIDField = this.FeatureLayerInfo.TypeIdField;
                    this.SetComboBoxProperties(typeIDField, field, comboBox);

                    if (this.SelectedFieldValues != null &&
                        this.SelectedFieldValues.ContainsKey(field.FieldName))
                    {
                        comboBox.SelectedValue = this.SelectedFieldValues[field.FieldName];
                    }

                    this.Grid.Children.Add(txtBlock);
                    this.Grid.Children.Add(comboBox);

                    Grid.SetRow(txtBlock, i);
                    Grid.SetRow(comboBox, i);
                    Grid.SetColumn(txtBlock, 0);
                    Grid.SetColumn(comboBox, 1);

                    if (this.Grid.FindName(comboName) != null)
                    {
                        this.Grid.UnregisterName(comboName);
                    }

                    this.Grid.RegisterName(comboName, comboBox);
                }
            }
        }

        private void AddGridRow()
        {
            RowDefinition row = new RowDefinition();
            row.Height = GridLength.Auto;
            this.Grid.RowDefinitions.Add(row);
        }

        private ComboBox CreateComboBox(string name, client.Field field, double margin)
        {
            ComboBox comboBox = new ComboBox();
            comboBox.Margin = new Thickness(margin);
            comboBox.Name = name;
            comboBox.Style = Application.Current.Resources["ThemedComboBoxStyle"] as Style;
            comboBox.FontSize = (double)Application.Current.Resources["ThemedTextSize"];
            return comboBox;
        }

        private TextBlock CreateTextBlock(string name, string text, double margin)
        {
            TextBlock txtBlock = new TextBlock();
            txtBlock.Margin = new Thickness(margin);
            txtBlock.Text = text;
            txtBlock.Name = name;
            txtBlock.Style = Application.Current.Resources["DisplayNameStyle"] as Style;
            txtBlock.Foreground = Application.Current.Resources["ThemedSecondaryTextBrush"] as Brush;
            txtBlock.FontSize = (double)Application.Current.Resources["ThemedTextSize"];

            return txtBlock;
        }

        private void SetComboBoxProperties(string typeIDField, client.Field field, ComboBox comboBox)
        {
            comboBox.SelectedValuePath = "Key";

            if (field.Domain != null)
            {
                CodedValueDomain cvDomain = field.Domain as CodedValueDomain;
                comboBox.DisplayMemberPath = "Value";

                if (cvDomain != null)
                {
                    comboBox.ItemsSource = cvDomain.CodedValues.OrderBy(c => c.Value);
                }
                else
                {
                    this.SetComboBoxForRangeDomain(field, comboBox);
                }
            }
            else if (field.FieldName == typeIDField)
            {
                comboBox.ItemsSource = FeatureLayerInfo.FeatureTypes;
                comboBox.DisplayMemberPath = "Value.Name";
                comboBox.SelectionChanged += ComboBox_SelectionChanged;
            }
        }

        private void SetItemsSource<T>(Domain domain, ComboBox comboBox) where T : struct, IConvertible, IComparable
        {
            RangeDomain<T> rangeDomain = domain as RangeDomain<T>;
            Dictionary<T, T> dictionary = new Dictionary<T, T>();

            var minVal = Convert.ToDouble(rangeDomain.MinimumValue, CultureInfo.InvariantCulture.NumberFormat);
            var maxVal = Convert.ToDouble(rangeDomain.MaximumValue, CultureInfo.InvariantCulture.NumberFormat);

            for (var i = minVal; i <= maxVal; i++)
            {
                var val = Convert.ChangeType(i, typeof(T));
                dictionary.Add((T)val, (T)val);
            }
            comboBox.ItemsSource = dictionary;
        }

        private void SetComboBoxForRangeDomain(client.Field field, ComboBox comboBox)
        {
            switch (field.Type)
            {
                case client.Field.FieldType.SmallInteger:
                    this.SetItemsSource<short>(field.Domain, comboBox);
                    break;
                case client.Field.FieldType.Double:
                    this.SetItemsSource<double>(field.Domain, comboBox);
                    break;
                case client.Field.FieldType.Integer:
                    this.SetItemsSource<int>(field.Domain, comboBox);
                    break;
                case client.Field.FieldType.Single:
                    this.SetItemsSource<float>(field.Domain, comboBox);
                    break;
                default:
                    throw new ApplicationException("Unknown rangedomain");
            }
        }

        #region EventHandlers
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ComboBox comboBox = sender as ComboBox;

                if (comboBox.SelectedItem == null)
                    return;
                if (!(comboBox.SelectedItem is KeyValuePair<Object, FeatureType>))
                    return;

                DataSource dataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == this.DataSourceID);
                FeatureLayerInfo layerInfo = FeatureLayerInfoFinder.GetFeatureLayerInfo(dataSource);

                KeyValuePair<Object, FeatureType> comboitem = (KeyValuePair<Object, FeatureType>)comboBox.SelectedItem;
                FeatureType type = comboitem.Value;
                foreach (var item in type.Domains)
                {
                    string fieldName = item.Key.ToString();

                    if (fieldName == layerInfo.TypeIdField)
                        continue;

                    string comboName = string.Format("{0}ComboBox", fieldName);
                    ComboBox combox = this.Grid.FindName(comboName) as ComboBox;
                    if (combox == null)
                        continue;

                    CodedValueDomain cvDomain = item.Value as CodedValueDomain;
                    if (cvDomain != null)
                    {
                        combox.ItemsSource = null;
                        combox.ItemsSource = cvDomain.CodedValues.OrderBy(c => c.Value);
                        combox.DisplayMemberPath = "Value";
                        combox.SelectedValuePath = "Key";
                    }
                    else
                    {
                        client.Field field = dataSource.Fields.FirstOrDefault(f => f.FieldName == fieldName);
                        SetComboBoxForRangeDomain(field, combox);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion
    }
}
