// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Windows.Data;
using ESRI.ArcGIS.Client.FeatureService;
using EditWidget.Extensions.Utilities;

namespace EditWidget.Extensions
{
    /// <summary>
    /// Value converter for each field created by the FeatureDataForm that corresponds to a graphic attribute.
    /// </summary>
    internal sealed class EsriNLFeatureDataFieldValueConverter : IValueConverter
    {
        private CodedValueDomain _codedValueDomain = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="EsriNLFeatureDataFieldValueConverter"/> class.
        /// </summary>
        /// <param name="codedValueDomain">The coded value domain.</param>
        public EsriNLFeatureDataFieldValueConverter(CodedValueDomain codedValueDomain)
        {
            this._codedValueDomain = codedValueDomain;
        }

        /// <summary>
        /// Modifies the source data before passing it to the target for display in the UI.
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the target dependency property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the target dependency property.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;

            if (this._codedValueDomain == null)
                return value;
            else
            {
                foreach (KeyValuePair<object, string> codeVal in this._codedValueDomain.CodedValues)
                {                    
                    if (codeVal.Key != null && codeVal.Key.Equals(value))
                    {
                        EsriNLCodedValueSource codedValueSource = new EsriNLCodedValueSource() { Code = codeVal.Key, DisplayName = codeVal.Value == null ? "" : codeVal.Value };
                        return codedValueSource;                     
                    }                    
                }
            }
			EsriNLCodedValueSource currentSource = new EsriNLCodedValueSource() { Code = value, DisplayName = (value == null) ? " " : value.ToString(), Temp = true };
			return currentSource;				
        }

        /// <summary>
        /// Modifies the target data before passing it to the source object.  This method is called only in <see cref="F:System.Windows.Data.BindingMode.TwoWay"/> bindings.
        /// </summary>
        /// <param name="value">The target data being passed to the source.</param>
        /// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the source object.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the source object.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is EsriNLCodedValueSource)
                return (value as EsriNLCodedValueSource).Code;

            if (value != null && string.IsNullOrEmpty(value.ToString().Trim()))
            {
                if (targetType == typeof(int?) || targetType == typeof(short?) ||
                    targetType == typeof(double?) || targetType == typeof(float?) ||
                    targetType == typeof(long?) || targetType == typeof(byte?) ||
                    targetType == typeof(bool?) || targetType == typeof(DateTime?))
                    return null;
            }

			if (value != null && targetType == typeof(DateTime?))
			{
				DateTime? dateTimeValue = value as DateTime?;
				if (dateTimeValue != null)
					return new DateTime(dateTimeValue.Value.Ticks, dateTimeValue.Value.Kind);
				return null;
			}

            return value;
        }
    }
}