// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Windows.Data;
using ESRI.ArcGIS.Client.FeatureService;
using System.Linq;
using System.Reflection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using ESRI.ArcGIS.Client;

namespace EditWidget.Extensions.Utilities
{
	#region Coded Value Domain
	/// <summary>
	/// *FOR INTERNAL USE ONLY* The CodedValueSource class.
	/// </summary>
	/// <remarks>Used to populate each entry in the coded value domain.</remarks>
	/// <exclude/>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class EsriNLCodedValueSource
    {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>The code.</value>
        public object Code { get; set; }
        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName { get; set; }
        /// <summary>
        /// Gets or sets a true or false value indicating if this item is a 
        /// temporary place holder item.
        /// </summary>		
        public bool Temp { get; set; }

    }
	/// <summary>
	/// *FOR INTERNAL USE ONLY* The CodedValueSources class.
	/// </summary>
	/// <remarks>Used to maintain collection of coded value domains.</remarks>	
	/// <exclude/>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public class EsriNLCodedValueSources : List<EsriNLCodedValueSource>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EsriNLCodedValueSources"/> class.
		/// </summary>
		public EsriNLCodedValueSources() : base() { }

		internal static string CodedValueNameLookup(string field, object generic, EsriNLCodedValueSources codedValueSources)
		{
#if SILVERLIGHT
			PropertyInfo fieldProperty = generic.GetType().GetProperty(field);
			if (fieldProperty == null)
				return "";

			var code = fieldProperty.GetValue(generic, null);
#else
			var code =  generic is Graphic ? (generic as Graphic).Attributes[field] : null;
#endif
			if (code == null)
				return "";

			foreach (EsriNLCodedValueSource codedValueSource in codedValueSources)
			{
				if (codedValueSource.Code != null && code != null)
				{
					if (codedValueSource.Code.ToString() == code.ToString())
						return codedValueSource.DisplayName;
				}
				else if (codedValueSource.Code == null && code == null)
					return codedValueSource.DisplayName;
			}
			return code.ToString();
		}

		internal static object CodedValueCodeLookup(string field, object generic, EsriNLCodedValueSources codedValueSources)
		{
#if SILVERLIGHT
			PropertyInfo fieldProperty = generic.GetType().GetProperty(field);
			if (fieldProperty == null)
				return null;

			var code = fieldProperty.GetValue(generic, null);
#else
			var code = generic is Graphic ? (generic as Graphic).Attributes[field] : null;
#endif
			if (code == null)
				return null;
			
			return code;
		}
	}

	/// <summary>
	/// *FOR INTERNAL USE ONLY* DynamicCodedValueSource is used to hold 
	/// CodedValueSources that change depending on the a value of another graphic attribute. 
	/// </summary>
	/// <exclude/>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public sealed class EsriNLDynamicCodedValueSource : Dictionary<object, EsriNLCodedValueSources>
	{
		internal static EsriNLCodedValueSources GetCodedValueSources(string lookupField,Field field, object generic, EsriNLDynamicCodedValueSource dynamicCodedValueSource, EsriNLCodedValueSources nullableSources)
		{
			if (dynamicCodedValueSource != null && generic != null && !string.IsNullOrEmpty(lookupField))
			{
#if SILVERLIGHT
				PropertyInfo lookupFieldProperty = generic.GetType().GetProperty(lookupField);
				if (lookupFieldProperty == null)
					return null;

				var key = lookupFieldProperty.GetValue(generic, null);
#else
				var key = (generic is Graphic) ? (generic as Graphic).Attributes[lookupField] : null;
#endif
				if (key == null)
				{
					object code = EsriNLCodedValueSources.CodedValueCodeLookup(field.Name, generic, new EsriNLCodedValueSources());
					nullableSources.Clear();				
					if (field.Nullable)
					{																		
						EsriNLCodedValueSource nullableSource = new EsriNLCodedValueSource() { Code = null, DisplayName = " " };						
						nullableSources.Add(nullableSource);						
					}
					if (code != null)
					{
						EsriNLCodedValueSource currentSource = new EsriNLCodedValueSource() { Code = code, DisplayName = code.ToString() };
						nullableSources.Add(currentSource);
					}
					return nullableSources;
				}

				EsriNLCodedValueSources codedValueSoruces = null;
				if (dynamicCodedValueSource.ContainsKey(key))
					codedValueSoruces = dynamicCodedValueSource[key];
				else if (dynamicCodedValueSource.ContainsKey(key.ToString()))
					codedValueSoruces = dynamicCodedValueSource[key.ToString()];

				EsriNLCodedValueSource tempSource = codedValueSoruces.FirstOrDefault(x => x.Temp == true);
				if (tempSource != null)
					codedValueSoruces.Remove(tempSource);
#if SILVERLIGHT
				object value = CodedValueSources.CodedValueNameLookup(field.Name, generic, new CodedValueSources());
				if (string.IsNullOrEmpty(value as string))
					value = null;
#else
				object value = (generic as Graphic).Attributes[field.Name];
#endif
				if (value != null)
				{
					EsriNLCodedValueSource source = codedValueSoruces.FirstOrDefault(x => x.Code != null && x.Code.ToString() == value.ToString());
					if (source == null)
					{
						EsriNLCodedValueSource currentSource = new EsriNLCodedValueSource() { Code = value, DisplayName = value.ToString(), Temp = true };
						if (field.Nullable)
							codedValueSoruces.Insert(1, currentSource);
						else
							codedValueSoruces.Insert(0, currentSource);
					}
				}
				return codedValueSoruces;
			}
			return null;
		}

		internal static string CodedValueNameLookup(string lookupField, string field, object generic, EsriNLDynamicCodedValueSource dynamicCodedValueSource)
		{
			if (dynamicCodedValueSource != null && generic != null && !string.IsNullOrEmpty(field))
			{
#if SILVERLIGHT
				PropertyInfo lookupFieldProperty = generic.GetType().GetProperty(lookupField);
				if (lookupFieldProperty == null)
					return null;

				var key = lookupFieldProperty.GetValue(generic, null);
#else				
				var key = generic is Graphic ? (generic as Graphic).Attributes[lookupField] : null;
#endif
				if (key == null)
					return EsriNLCodedValueSources.CodedValueNameLookup(field, generic, new EsriNLCodedValueSources());				

				if (dynamicCodedValueSource.ContainsKey(key))
				{
					EsriNLCodedValueSources codedValueSources = dynamicCodedValueSource[key];
					if (codedValueSources != null)
					{
						string name = EsriNLCodedValueSources.CodedValueNameLookup(field, generic, codedValueSources);
						if (!string.IsNullOrEmpty(name))
							return name;
					}
				}
				else if (dynamicCodedValueSource.ContainsKey(key.ToString()))
				{
					EsriNLCodedValueSources codedValueSources = dynamicCodedValueSource[key.ToString()];
					if (codedValueSources != null)
					{
						string name = EsriNLCodedValueSources.CodedValueNameLookup(field, generic, codedValueSources);
						if (!string.IsNullOrEmpty(name))
							return name;
					}
				}
			}
			return null;
		}

	}
	#endregion
}