using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Harpoon
{
    [Serializable]
    public abstract class HouParm
    {
        public abstract ParmTemplate parmTemplate { get; }
        public abstract void GUILayout();

        public abstract IMultipartFormSection formSection { get; }
        public string name => parmTemplate.name;
        
        public static IEnumerable<HouParm> CreateParms(dynamic hdaHeader)
        {
            IEnumerable<dynamic> parmTemplates = hdaHeader.parmTemplateGroup.parmTemplates;
            foreach (var parmTemplate in parmTemplates)
            {
                ParmTemplate template = parmTemplate.ToObject<ParmTemplate>();
                if (template.isHidden)
                    continue;
                switch (template.dataType)
                {
                    case (ParmData.Int):
                    {
                        yield return new IntParm(parmTemplate.ToObject<IntParmTemplate>());
                        break;
                    }
                    case (ParmData.Float):
                    {
                        yield return new FloatParm(parmTemplate.ToObject<FloatParmTemplate>());
                        break;
                    }
                    case (ParmData.String):
                    {
                        yield return new StringParm(parmTemplate.ToObject<StringParmTemplate>());
                        break;
                    }
                }
            }
        }
    }

    [Serializable]
    public class FloatParm : HouParm
    {
        public override ParmTemplate parmTemplate => template;
        public FloatParmTemplate template;
        public float[] value;

        public FloatParm(FloatParmTemplate template)
        {
            this.template = template;
            value = new float[template.numComponents];
            Array.Copy(template.defaultValue, value, template.numComponents);
        }

        public override void GUILayout()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(template.label);
            for (int i = 0; i < template.numComponents; ++i)
            {
                value[i] = EditorGUILayout.FloatField(value[i]);
            }
            EditorGUILayout.EndHorizontal();
        }

        public override IMultipartFormSection formSection
        {
            get => new MultipartFormDataSection(template.name, JsonConvert.SerializeObject(value));
        }
    }
    
    [Serializable]
    public class IntParm : HouParm
    {
        public override ParmTemplate parmTemplate => template;
        public IntParmTemplate template;
        public int[] value;
        public IntParm(IntParmTemplate template)
        {
            this.template = template;
            value = new int[template.numComponents];
            Array.Copy(template.defaultValue, value, template.numComponents);
        }
        
        public override IMultipartFormSection formSection
        {
            get => new MultipartFormDataSection(template.name, JsonConvert.SerializeObject(value));
        }

        public override void GUILayout()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(template.label);
            for (int i = 0; i < template.numComponents; ++i)
            {
                value[i] = EditorGUILayout.IntField(value[i]);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
    
    [Serializable]
    public class StringParm : HouParm
    {
        public override ParmTemplate parmTemplate => template;
        public StringParmTemplate template;

        [SerializeField]
        public Texture2D texture;
        [SerializeField]
        public GameObject model;

        public string value;

        public StringParm(StringParmTemplate template)
        {
            this.template = template;
            value = template.defaultValue[0];
        }

        public override void GUILayout()
        {
            switch (template.stringType)
            {
                case StringParmType.FileReference:
                {
                    switch (template.fileType)
                    {
                        case FileType.Geometry:
                        case FileType.Fbx:
                        case FileType.Usd:
                        {
                            model = EditorGUILayout.ObjectField(template.label, model, typeof(GameObject), true) as GameObject;
                            value = "";
                            break;
                        }
                        case FileType.Image:
                        {
                            texture = EditorGUILayout.ObjectField(template.label, texture, typeof(Texture2D), true) as Texture2D;
                            value = "";
                            break;
                        }
                    }
                    break;
                }
                default:
                {
                    value = EditorGUILayout.TextField(template.label, value);
                    break;
                }
            }
        }

        byte[] rawTexture
        {
            get
            {
                if (texture == null)
                {
                    return new byte[0];
                }
                if (AssetDatabase.Contains(texture))
                {
                    string path = Path.GetFullPath(
                        AssetDatabase.GetAssetPath(texture), 
                        Path.GetDirectoryName(Application.dataPath));
                    return File.ReadAllBytes(path);
                }
                else
                {
                    return texture.EncodeToEXR();
                }
            }
        }

        byte[] rawModel
        {
            get
            {
                if (model == null)
                {
                    return new byte[0];
                }
                string path = Path.Combine(Application.temporaryCachePath,
                    $"{template.name}_{model.GetInstanceID()}.fbx");
                ExportFBX(path, model);
                var rawFBX = File.ReadAllBytes(path);
                File.WriteAllBytes(Path.Combine(Application.temporaryCachePath, "TMP.fbx"), rawFBX);
                return rawFBX;
            }
        }

        private static void ExportFBX(string path, GameObject model)
        {
            model = GameObject.Instantiate(model);
            ExportModelOptions exportSettings = new ExportModelOptions();
            exportSettings.ExportFormat = ExportFormat.Binary;
            exportSettings.EmbedTextures = true;
            ModelExporter.ExportObject(path, model, exportSettings);
            Object.DestroyImmediate(model);
        }
        
        private static void ExportBinaryFBX (string filePath, UnityEngine.Object singleObject)
        {
            // Find relevant internal types in Unity.Formats.Fbx.Editor assembly
            Type[] types = AppDomain.CurrentDomain.GetAssemblies().First(x => x.FullName == "Unity.Formats.Fbx.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").GetTypes();
            Type optionsInterfaceType = types.First(x => x.Name == "IExportOptions");
            Type optionsType = types.First(x => x.Name == "ExportOptionsSettingsSerializeBase");
 
            // Instantiate a settings object instance
            MethodInfo optionsProperty = typeof(ModelExporter).GetProperty("DefaultOptions", BindingFlags.Static | BindingFlags.NonPublic).GetGetMethod(true);
            object optionsInstance = optionsProperty.Invoke(null, null);
 
            // Change the export setting from ASCII to binary
            FieldInfo exportFormatField = optionsType.GetField("exportFormat", BindingFlags.Instance | BindingFlags.NonPublic);
            exportFormatField.SetValue(optionsInstance, 1);
 
            // Invoke the ExportObject method with the settings param
            MethodInfo exportObjectMethod = typeof(ModelExporter).GetMethod("ExportObject", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new Type[] { typeof(string), typeof(UnityEngine.Object), optionsInterfaceType }, null);
            exportObjectMethod.Invoke(null, new object[] { filePath, singleObject, optionsInstance });
        }

        public override IMultipartFormSection formSection
        {
            get
            {
                if (template.stringType == StringParmType.FileReference
                    && template.fileType == FileType.Image
                    && texture != null)
                {
                    return new MultipartFormFileSection(template.name, rawTexture, $"{template.name}_{texture.GetInstanceID()}.exr", "image/aces");
                }
                if (template.stringType == StringParmType.FileReference
                    && template.fileType == FileType.Geometry
                    && model != null)
                {
                    return new MultipartFormFileSection(template.name, rawModel, $"{template.name}_{model.GetInstanceID()}.fbx", "image/aces");
                }
                return new MultipartFormDataSection(template.name, JsonConvert.SerializeObject(value));   
            }
        }
    }
}