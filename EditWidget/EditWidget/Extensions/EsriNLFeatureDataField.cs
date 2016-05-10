﻿// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.ComponentModel;
using ESRI.ArcGIS.Client.FeatureService;
using ESRI.ArcGIS.Client;
using EditWidget.Extensions.Utilities;

namespace EditWidget.Extensions
{
	internal interface IKeyValue : INotifyPropertyChanged
	{
		string Key { get; }
		object Value { get; }
	}

    /// <summary>
    /// *FOR INTERNAL USE ONLY* The FeatureDataField class. Used by FeatureDataForm to create values corresponding to each graphic attribute. Beside 
    /// data validation since FeatureDataField implements INotifyPropertyChanged interface it will notify FeatureDataForm about 
    /// any change in an attribute.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <exclude/>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class EsriNLFeatureDataField<T> : IKeyValue
    {
        private EsriNLFeatureDataForm _featureDataForm;
        private Field _field;
        private Type _propertyType;
        private T _propertyValue;
        private CodedValueDomain _codedValueDomain;
        private RangeDomain<DateTime> _dateRangeDomain;
        private RangeDomain<double> _doubleRangeDomain;
        private RangeDomain<float> _floatRangeDomain;
        private RangeDomain<int> _intRangeDomain;
        private RangeDomain<short> _shortRangeDomain;
        private RangeDomain<long> _longRangeDomain;
        private RangeDomain<byte> _byteRangeDomain;

        /// <summary>
        /// Initializes a new instance of the <see cref="EsriNLFeatureDataField&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="featureDataForm">The feature data form.</param>
        /// <param name="field">The field.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyValue">The property value.</param>
        internal EsriNLFeatureDataField(EsriNLFeatureDataForm featureDataForm, Field field, Type propertyType, T propertyValue)
        {
            this._featureDataForm = featureDataForm;
            this._codedValueDomain = null;
            Domain domain = field.Domain;
            if (domain != null)
            {
                this._codedValueDomain = domain as CodedValueDomain;
                if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
                    this._dateRangeDomain = domain as RangeDomain<DateTime>;
                else if (propertyType == typeof(double) || propertyType == typeof(double?))
                    this._doubleRangeDomain = domain as RangeDomain<double>;
                else if (propertyType == typeof(float) || propertyType == typeof(float?))
                    this._floatRangeDomain = domain as RangeDomain<float>;
                else if (propertyType == typeof(int) || propertyType == typeof(int?))
                    this._intRangeDomain = domain as RangeDomain<int>;
                else if (propertyType == typeof(short) || propertyType == typeof(short?))
                    this._shortRangeDomain = domain as RangeDomain<short>;
                else if (propertyType == typeof(long) || propertyType == typeof(long?))
                    this._longRangeDomain = domain as RangeDomain<long>;
                else if (propertyType == typeof(byte) || propertyType == typeof(byte?))
                    this._byteRangeDomain = domain as RangeDomain<byte>;
            }

            this._field = field;
            this._propertyType = propertyType;
            this._propertyValue = propertyValue;
        }

        /// <summary>
        /// Gets the key (the attribute name).
        /// </summary>
        /// <value>The key.</value>
        public string Key
        {
            get { return this._field.Name; }
        }

        /// <summary>
        /// Gets or sets the attribute value.
        /// </summary>
        /// <value>The value.</value>
        public T Value
        {
            get { return this._propertyValue; }
            set
            {
                string propertyName = this._field.Name;
                string typeFriendlyName = "";
                if (EsriNLFeatureDataForm.Utilities.IsNotOfTypeSystemNullable(this._propertyType))
                    typeFriendlyName = this._propertyType.ToString();
                else
                    typeFriendlyName = System.Nullable.GetUnderlyingType(this._propertyType).ToString();
                
                if (this._codedValueDomain != null)
                {
                    try
                    {
                        this._propertyValue = (T)value;
                        NotifyPropertyChanged(propertyName);
                    }
                    catch
                    {
                        this._featureDataForm.IsValid = false;
                        if ((value as EsriNLCodedValueSource).Code == null)
                            throw new ArgumentException(string.Format(Properties.Resources.Validation_ValueCannotBeNull, propertyName));
                        else
							throw new ArgumentException(string.Format(Properties.Resources.Validation_InvalidValue, value, typeFriendlyName, propertyName));
                    }
                }
                else
                {
                    // Checking whether the attribute is allowed to be NULL:
                    if (value == null && !this._field.Nullable)
                    {
                        this._featureDataForm.IsValid = false;
						throw new ArgumentException(string.Format(Properties.Resources.Validation_ValueCannotBeNull, propertyName));
                    }

                    if (value == null || (value != null && string.IsNullOrEmpty(value.ToString().Trim())))
                    {
                        try
                        {
                            this._propertyValue = value;
                            NotifyPropertyChanged(propertyName);
                        }
                        catch
                        {
                            this._featureDataForm.IsValid = false;
                            throw new ArgumentException(string.Format(Properties.Resources.Validation_ValueCannotBeNull, propertyName));
                        }
                    }
                    else
                    {
                        // First verify type of the value:
                        try
                        {
                            if (value != null)
                            {
                                object verifyType = null;
                                if (EsriNLFeatureDataForm.Utilities.IsNotOfTypeSystemNullable(this._propertyType))
                                    verifyType = System.Convert.ChangeType(value, this._propertyType, null);
                                else
                                    verifyType = System.Convert.ChangeType(value, System.Nullable.GetUnderlyingType(this._propertyType), null);
                            }
                        }
                        catch
                        {
                            this._featureDataForm.IsValid = false;
							throw new ArgumentException(string.Format(Properties.Resources.Validation_InvalidType, propertyName, typeFriendlyName));
                        }

                        // Type of the value is valid; first check for a range domain and verify the value over its min and max:
                        try
                        {
                            if (this._dateRangeDomain != null)
                            {
                                DateTime parsedValue = DateTime.Parse(value.ToString());
                                EsriNLFeatureDataForm.Utilities.IsValidRange(this._dateRangeDomain, parsedValue);
                            }
                            else if (this._doubleRangeDomain != null)
                            {
                                double parsedValue = double.Parse(value.ToString());
                                EsriNLFeatureDataForm.Utilities.IsValidRange(this._doubleRangeDomain, parsedValue);
                            }
                            else if (this._floatRangeDomain != null)
                            {
                                float parsedValue = float.Parse(value.ToString());
                                EsriNLFeatureDataForm.Utilities.IsValidRange(this._floatRangeDomain, parsedValue);
                            }
                            else if (this._intRangeDomain != null)
                            {
                                int parsedValue = int.Parse(value.ToString());
                                EsriNLFeatureDataForm.Utilities.IsValidRange(this._intRangeDomain, parsedValue);
                            }
                            else if (this._shortRangeDomain != null)
                            {
                                short parsedValue = short.Parse(value.ToString());
                                EsriNLFeatureDataForm.Utilities.IsValidRange(this._shortRangeDomain, parsedValue);
                            }
                            else if (this._longRangeDomain != null)
                            {
                                long parsedValue = long.Parse(value.ToString());
                                EsriNLFeatureDataForm.Utilities.IsValidRange(this._longRangeDomain, parsedValue);
                            }
                            else if (this._byteRangeDomain != null)
                            {
                                byte parsedValue = byte.Parse(value.ToString());
                                EsriNLFeatureDataForm.Utilities.IsValidRange(this._byteRangeDomain, parsedValue);
                            }
                        }
                        catch (ArgumentException ex)
                        {
                            this._featureDataForm.IsValid = false;
							_featureDataForm.UpdateDictionary(ref _featureDataForm._attributeValidationStatus, propertyName, false);						
									throw new ArgumentException(ex.Message);
                        }

                        this._propertyValue = value;
                        NotifyPropertyChanged(propertyName);

                        this._featureDataForm.IsValid = true;
                    }
                }
            }
        }

        #region INotifyPropertyChanged Members
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

		object IKeyValue.Value
		{
			get { return this.Value; }
		}
	}
}