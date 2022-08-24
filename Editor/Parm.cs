using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Harpoon
{
    [Serializable]
    public abstract class HouParm
    {
        public abstract ParmTemplate parmTemplate { get; }
        public abstract void GUILayout();

        public abstract IMultipartFormSection formSection { get; }
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
        private Texture2D texture;

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
                            EditorGUILayout.LabelField("Geometry parameters are not supported");
                            break;
                        }
                        case FileType.Image:
                        {
                            texture = EditorGUILayout.ObjectField(template.label, texture, typeof(Texture2D), true) as Texture2D;
                            value = texture == null ? "" : Path.GetFileName(AssetDatabase.GetAssetPath(texture));
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
                    //return texture.EncodeToEXR();
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

        public override IMultipartFormSection formSection
        {
            get
            {
                if (template.stringType == StringParmType.FileReference
                    && template.fileType == FileType.Image
                    && texture != null)
                {
                    return new MultipartFormFileSection(template.name, rawTexture, "Heightmap.exr", "image/aces");
                }
                else
                {
                    return new MultipartFormDataSection(template.name, JsonConvert.SerializeObject(value));   
                }
            }
        }
    }
}