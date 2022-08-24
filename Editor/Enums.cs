using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Harpoon
{
	// https://www.sidefx.com/docs/houdini/hom/hou/parmData.html
	public enum ParmData
	{
		Int,
		Float,
		String,
		Ramp
	}

	// https://www.sidefx.com/docs/houdini/hom/hou/stringParmType.html
	public enum StringParmType
	{
		Regular,
		FileReference,
		NodeReference,
		NodeReferenceList
	}

	// https://www.sidefx.com/docs/houdini/hom/hou/parmLook.html
	public enum ParmLook
	{
		Regular,
		Logarithmic,
		Angle,
		Vector,
		ColorSquare,
		HueCircle,
		CRGBAPlaneChooser
	}

	// https://www.sidefx.com/docs/houdini/hom/hou/fileType.html
	public enum FileType
	{
		Any,
		Image,
		Geometry,
		Ramp,
		Capture,
		Clip,
		Lut,
		Cmd,
		Midi,
		I3d,
		Chan,
		Sim,
		SimData,
		Hip,
		Otl,
		Dae,
		Gallery,
		Directory,
		Icon,
		Ds,
		Alembic,
		Psd,
		LightRig,
		Gltf,
		Movie,
		Fbx,
		Usd,
		Sqlite,
	}

	// https://www.sidefx.com/docs/houdini/hom/hou/folderType.html
	public enum FolderType
	{
		Collapsible,
		Simple,
		Tabs,
		RadioButtons,
		MultiparmBlock,
		ScrollingMultiparmBlock,
		TabbedMultiparmBlock,
		ImportBlock
	}

	// https://www.sidefx.com/docs/houdini/hom/hou/parmTemplateType.html
	public enum ParmTemplateType
	{
		Int,
		Float,
		String,
		Toggle,
		Menu,
		Button,
		FolderSet,
		Separator,
		Label,
		Remap,
		Data
	}

	// https://www.sidefx.com/docs/houdini/hom/hou/dataParmType.html
	public enum DataParmType
	{
		Geometry,
		KeyValueDictionary
	}

	// https://www.sidefx.com/docs/houdini/hom/hou/menuType.html
	public enum MenuType
	{
		Normal,
		Mini,
		ControlNextParameter,
		StringReplace,
		StringToggle
	}
}