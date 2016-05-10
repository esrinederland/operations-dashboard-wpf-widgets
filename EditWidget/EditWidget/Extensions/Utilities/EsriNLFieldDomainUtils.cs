using System.Collections.Generic;
using ESRI.ArcGIS.Client.FeatureService;
using ESRI.ArcGIS.Client;

namespace EditWidget.Extensions.Utilities
{
	internal static partial class EsriNLFieldDomainUtils
	{
		/// <summary>
		/// Checks Fields and determines if Field is Dynamic Coded Value Domain (aka Sub Domain)
		/// </summary>
		/// <param name="field">The field that needs to be checked.</param>
		/// <param name="layerInfo">
		/// The FeatureLayerInfo that has the information to determine if the field
		/// is a dynamic coded value domain (aka Sub Domain).
		/// </param>
		/// <returns>Boolean value indicating if the field is a DynamicCodedValueDomain (aka Sub Domain)</returns>
		internal static bool IsDynamicDomain(Field field, FeatureLayerInfo layerInfo)
		{
			bool result = false;
			if (field.Domain == null && layerInfo != null
				&& layerInfo.FeatureTypes != null
				&& layerInfo.FeatureTypes.Count > 0)
			{
				foreach (object key in layerInfo.FeatureTypes.Keys)
				{
					FeatureType featureType = layerInfo.FeatureTypes[key];
					if (featureType.Domains.ContainsKey(field.Name))
					{
						result = true;
						break;
					}
				}
			}
			return result;
		}
		/// <summary>
		/// Builds a DynamicCodedValueSource object used to lookup display value.
		/// </summary>
		/// <param name="field">The field to build DynamicCodedValueSource for</param>
		/// <param name="layerInfo">The FeatureLayerInfo used to lookup all the coded value domains </param>
		/// <returns>DynamicCodedValueSource which is a collection of coded value domain values for a field.</returns>
		internal static EsriNLDynamicCodedValueSource BuildDynamicCodedValueSource(Field field, FeatureLayerInfo layerInfo)
		{
			EsriNLDynamicCodedValueSource dynamicCodedValueSource = null;
			foreach (object key in layerInfo.FeatureTypes.Keys)
			{
				FeatureType featureType = layerInfo.FeatureTypes[key];
				if (featureType.Domains.ContainsKey(field.Name))
				{
					if (dynamicCodedValueSource == null)
						dynamicCodedValueSource = new EsriNLDynamicCodedValueSource();

					CodedValueDomain codedValueDomain = featureType.Domains[field.Name] as CodedValueDomain;
					if (codedValueDomain != null)
					{
						EsriNLCodedValueSources codedValueSources = null;
						foreach (KeyValuePair<object, string> kvp in codedValueDomain.CodedValues)
						{
							if (codedValueSources == null)
							{
								codedValueSources = new EsriNLCodedValueSources();
								if (field.Nullable)
								{
									EsriNLCodedValueSource nullableSource = new EsriNLCodedValueSource() { Code = null, DisplayName = " " };
									codedValueSources.Add(nullableSource);
								}
							}

							codedValueSources.Add(new EsriNLCodedValueSource() { Code = kvp.Key, DisplayName = kvp.Value });
						}
						if (codedValueSources != null)
						{
							if (dynamicCodedValueSource == null)
								dynamicCodedValueSource = new EsriNLDynamicCodedValueSource();

							dynamicCodedValueSource.Add(featureType.Id, codedValueSources);
						}
					}
				}
			}
			return dynamicCodedValueSource;
		}
		/// <summary>
		/// Returns CodedValueSources that make up the display text of each value of the TypeIDField.
		/// </summary>
		/// <param name="field">TypeIDField</param>
		/// <param name="layerInfo">FeatureLayerInof used to construct the CodedValueSources from the FeatureTypes.</param>
		/// <returns>CodedValueSoruces that contain code an value matches to all possible TypeIDField values.</returns>
		internal static EsriNLCodedValueSources BuildTypeIDCodedValueSource(Field field, FeatureLayerInfo layerInfo)
		{
			EsriNLCodedValueSources codedValueSources = null;
			foreach (KeyValuePair<object, FeatureType> kvp in layerInfo.FeatureTypes)
			{
				if (kvp.Key == null) continue;
				string name = (kvp.Value != null && kvp.Value.Name != null) ? kvp.Value.Name : "";
				EsriNLCodedValueSource codedValueSource = new EsriNLCodedValueSource() { Code = kvp.Key, DisplayName = name };
				if (codedValueSources == null)
				{
					codedValueSources = new EsriNLCodedValueSources();
					if (field.Nullable)
					{
						EsriNLCodedValueSource nullableSource = new EsriNLCodedValueSource() { Code = null, DisplayName = " " };
						codedValueSources.Add(nullableSource);
					}
				}
				codedValueSources.Add(codedValueSource);
			}
			return codedValueSources;
		}
		/// <summary>
		/// Returns a CodedValueSources object constructed form the CodedValueDomain value.
		/// </summary>
		/// <param name="field">The field to make a CodedValueSource from.</param>
		/// <returns>The CodedValueSources object used for a code to value lookup.</returns>
		internal static EsriNLCodedValueSources BuildCodedValueSource(Field field)
		{
			CodedValueDomain codedValueDomain = field.Domain as CodedValueDomain;
			EsriNLCodedValueSources codedValueSources = null;
			if (codedValueDomain != null)
			{
				foreach (KeyValuePair<object, string> codedValue in codedValueDomain.CodedValues)
				{
					if (codedValueSources == null)
					{
						codedValueSources = new EsriNLCodedValueSources();
						if (field.Nullable)
						{
							EsriNLCodedValueSource nullableSource = new EsriNLCodedValueSource() { Code = null, DisplayName = " " };
							codedValueSources.Add(nullableSource);
						}
					}
					codedValueSources.Add(new EsriNLCodedValueSource() { Code = codedValue.Key, DisplayName = codedValue.Value });
				}
			}
			return codedValueSources;
		}
	}
}
