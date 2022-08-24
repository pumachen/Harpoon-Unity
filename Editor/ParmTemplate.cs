using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Harpoon
{
	[Serializable]
	public class ParmTemplate
	{
		public string name;
		public string label;
		[JsonConverter(typeof(StringEnumConverter))]
		public ParmTemplateType type;
		[JsonConverter(typeof(StringEnumConverter))]
		public ParmData dataType;
		public int numComponents;
		// public string namingScheme;
		[JsonConverter(typeof(StringEnumConverter))]
		public ParmLook look;
		public string help;
		public bool isHidden;
		public bool isLabelHidden;
		public bool joinsWithNext;
		// public string disableWhen;
		// public string conditionals;
		public Dictionary<string, string> tags;
		// public string scriptCallback;
		// public string scriptCallbackLanguage;
	}

	[Serializable]
	public class FloatParmTemplate : ParmTemplate
	{
		public float[] defaultValue;
		// public string[] defaultExpression;
		// public string[] defaultExpressionLanguage;
		public float minValue;
		public float maxValue;
		public bool minIsStrict;
		public bool maxIsStrict;
	}
	
	[Serializable]
	public class IntParmTemplate : ParmTemplate
	{
		public int[] defaultValue;
		// public string[] defaultExpression;
		// public string[] defaultExpressionLanguage;
		public int minValue;
		public int maxValue;
		public bool minIsStrict;
		public bool maxIsStrict;
		//public string itemGeneratorScript;
		//public string itemGeneratorScriptLanguage;
		[JsonConverter(typeof(StringEnumConverter))]
		public MenuType menuType;
		public bool menuUseToken;
	}
	
	[Serializable]
	public class StringParmTemplate : ParmTemplate
	{
		public string[] defaultValue;
		// public string[] defaultExpression;
		// public string[] defaultExpressionLanguage;
		[JsonConverter(typeof(StringEnumConverter))]
		public StringParmType stringType;
		[JsonConverter(typeof(StringEnumConverter))]
		public FileType fileType;
		public string[] menuItems;
		public string[] menuLabels;
		//public string[] iconNames;
		//public string itemGeneratorScript;
		//public string itemGeneratorScriptLanguage;
		[JsonConverter(typeof(StringEnumConverter))]
		public MenuType menuType;
		public bool menuUseToken;
	}
}