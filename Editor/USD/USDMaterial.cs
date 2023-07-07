using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using pxr;
using Unity.Formats.USD;
using UnityEditor;
using UnityEngine;
using USD.NET;
using USD.NET.Unity;
using ShaderPropertyType = UnityEngine.Rendering.ShaderPropertyType;

namespace Harpoon.USD
{
    public partial class USDImportContext
    {
        public Material ImportMaterial(SdfPath usdMaterialPath)
        {
            if (materials.TryGetValue(usdMaterialPath, out Material material))
            {
                return material;
            }

            MaterialSample usdMaterial = new MaterialSample();
            usdScene.Read(usdMaterialPath, usdMaterial);
            /*if (string.IsNullOrEmpty(usdMaterial.surface.connectedPath))
            {
                return null;
            }*/

            var matPrim = usdScene.GetPrimAtPath(usdMaterialPath);
            if (matPrim.HasCustomDataKey(new TfToken("materialPath")))
            {
                string matAssetPath = matPrim.GetCustomDataByKey(new TfToken("materialPath"));
                if (!string.IsNullOrEmpty(matAssetPath))
                {
                    material = AssetDatabase.LoadAssetAtPath<Material>(matAssetPath);
                    if (material != null)
                    {
                        materials.Add(usdMaterialPath, material);
                        return material;
                    }
                }
            }

            Shader shader = defaultShader;
            if (shader == null)
            {
                shader = Shader.Find("HDRP/Lit");
                //shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            material = new Material(shader);
            material.name = usdMaterialPath.GetName();
            materials.Add(usdMaterialPath, material);

            PreviewSurfaceSample previewSurf = new PreviewSurfaceSample();
            usdScene.Read(new pxr.SdfPath(usdMaterial.surface.connectedPath).GetPrimPath(), previewSurf);
            string texturePath = "";
            string textureReaderPath = previewSurf.diffuseColor.GetConnectedPath();
            if (!string.IsNullOrEmpty(textureReaderPath))
            {
                textureReaderPath = textureReaderPath.Split('.')[0];
                TextureReaderSample texSample = new TextureReaderSample();
                usdScene.Read(new pxr.SdfPath(textureReaderPath).GetPrimPath(), texSample);
                texturePath = texSample.file.defaultValue.GetAssetPath();
                texturePath = Path.Combine(usdFileDir, texturePath);
            }

            if (!File.Exists(texturePath))
            {
                return material;
            }

            Texture2D texture;

            if (!string.IsNullOrEmpty(textureImportDir))
            {
                string textureName = Path.GetFileName(texturePath);
                string textureDirFS = Path.Combine(Path.GetDirectoryName(Application.dataPath), textureImportDir);
                if (!Directory.Exists(textureDirFS))
                {
                    Directory.CreateDirectory(textureDirFS);
                }

                File.Copy(texturePath, Path.Combine(textureDirFS, textureName), true);
                AssetDatabase.Refresh();
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(textureImportDir, textureName));
            }
            else
            {
                byte[] rawTexture = File.ReadAllBytes(texturePath);
                texture = new Texture2D(1, 1);
                texture.LoadImage(rawTexture);
            }

            material.mainTexture = texture;

            if (!AssetDatabase.Contains(material) && !string.IsNullOrEmpty(materialImportDir))
            {
                string materialDirFS = Path.Combine(Path.GetDirectoryName(Application.dataPath), materialImportDir);
                string materialPath = Path.Combine(materialImportDir, $"{material.name}.mat");
                if (!Directory.Exists(materialDirFS))
                {
                    Directory.CreateDirectory(materialDirFS);
                }

                AssetDatabase.Refresh();
                AssetDatabase.CreateAsset(material, materialPath);
                material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            }

            return material;
        }
    }
}