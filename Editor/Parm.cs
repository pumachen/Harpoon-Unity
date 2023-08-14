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
    public abstract class Parm
    {
        public abstract ParmTemplate parmTemplate { get; }
        public abstract void GUILayout();

        public abstract IMultipartFormSection formSection { get; }
        public string name => parmTemplate.name;
        
        public static IEnumerable<Parm> CreateParms(dynamic parmTemplateGroup)
        {
            IEnumerable<dynamic> parmTemplates = parmTemplateGroup.parmTemplates;
            foreach (var parmTemplate in parmTemplates)
            {
                ParmTemplate template = parmTemplate.ToObject<ParmTemplate>();
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
    public class FloatParm : Parm
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
            if(template.isHidden)
                return;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(template.label);
            if (template.numComponents == 1)
            {
                if (template.minIsStrict && template.maxIsStrict)
                {
                    value[0] = EditorGUILayout.Slider(value[0], template.minValue, template.maxValue);
                }
                else
                {
                    value[0] = EditorGUILayout.FloatField(value[0]);
                }
                if (template.minIsStrict)
                {
                    value[0] = Mathf.Max(value[0], template.minValue);
                }
                if (template.maxIsStrict)
                {
                    value[0] = Mathf.Min(value[0], template.maxValue);
                }
            }
            else
            {
                for (int i = 0; i < template.numComponents; ++i)
                {
                    value[i] = EditorGUILayout.FloatField(value[i]);
                    if (template.minIsStrict)
                    {
                        value[i] = Mathf.Max(value[i], template.minValue);
                    }
                    if (template.maxIsStrict)
                    {
                        value[i] = Mathf.Min(value[i], template.maxValue);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public override IMultipartFormSection formSection
        {
            get => new MultipartFormDataSection(template.name, JsonConvert.SerializeObject(value));
        }
    }
    
    [Serializable]
    public class IntParm : Parm
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
            if(template.isHidden)
                return;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(template.label);
            if (template.numComponents == 1)
            {
                if (template.menuItems != null && template.menuItems.Length > 0)
                {
                    value[0] = EditorGUILayout.Popup(value[0], template.menuLabels);
                }
                else if (template.minIsStrict && template.maxIsStrict)
                {
                    value[0] = EditorGUILayout.IntSlider(value[0], template.minValue, template.maxValue);
                }
                else
                {
                    value[0] = EditorGUILayout.IntField(value[0]);
                }
                if (template.minIsStrict)
                {
                    value[0] = Mathf.Max(value[0], template.minValue);
                }
                if (template.maxIsStrict)
                {
                    value[0] = Mathf.Min(value[0], template.maxValue);
                }
            }
            else
            {
                for (int i = 0; i < template.numComponents; ++i)
                {
                    value[i] = EditorGUILayout.IntField(value[i]);
                    if (template.minIsStrict)
                    {
                        value[i] = Mathf.Max(value[i], template.minValue);
                    }
                    if (template.maxIsStrict)
                    {
                        value[i] = Mathf.Min(value[i], template.maxValue);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
    
    [Serializable]
    public class StringParm : Parm
    {
        public override ParmTemplate parmTemplate => template;
        public StringParmTemplate template;

        [SerializeField]
        public Texture2D texture;
        [SerializeField]
        public bool embedTexture;
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
            if(template.isHidden)
                return;
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
                            embedTexture = EditorGUILayout.Toggle("Embed Texture", embedTexture);
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
                ExportFBX(path, model, embedTexture);
                var rawFBX = File.ReadAllBytes(path);
                File.WriteAllBytes(Path.Combine(Application.temporaryCachePath, "TMP.fbx"), rawFBX);
                return rawFBX;
            }
        }

        private static void ExportFBX(string path, GameObject model, bool embedTexture)
        {
            model = GameObject.Instantiate(model);
            ExportModelOptions exportSettings = new ExportModelOptions();
            exportSettings.ExportFormat = ExportFormat.Binary;
            exportSettings.EmbedTextures = embedTexture;
            ModelExporter.ExportObject(path, model, exportSettings);
            Object.DestroyImmediate(model);
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
                    return new MultipartFormFileSection(template.name, rawModel, $"{template.name}_{model.GetInstanceID()}.fbx", "multipart/form-data");
                }
                return new MultipartFormDataSection(template.name, JsonConvert.SerializeObject(value));   
            }
        }
    }
}