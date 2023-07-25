using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using pxr;
using USD.NET;
using USD.NET.Unity;
using Unity.Formats.USD;
using Object = UnityEngine.Object;

namespace Harpoon.USD
{
    public partial class USDExportContext
    {
        public enum USDTexOutput
        {
            R,
            G,
            B,
            A,
            RGB,
            RGBA
        }


        public string WriteTexture(
            Texture texture,
            string propertyName,
            string usdShaderPath,
            string dstTexturePath,
            USDTexOutput channel)
        {
            return WriteTexture(texture, propertyName, usdShaderPath, dstTexturePath, channel, out Hash128 hash);
        }
        
        public string WriteTexture(
            Texture texture, 
            string propertyName,
            string usdShaderPath,
            string dstTexturePath,
            USDTexOutput channel, 
            out Hash128 imageContentHash)
        {
            // TODO
            // We have to handle multiple cases here:
            // - file is only in memory
            //   - a Texture2D
            //   - a Texture
            //   - a RenderTexture
            //   - needs special care if marked as Normal Map
            //     (can probably only be detected in an Editor context, and heuristically at runtime)
            //   => need to blit / export
            // - file is not supported at all (or not yet)
            //   - a 3D texture
            //   => needs to be ignored, log Warning

            RenderTexture bufferRT = RenderTexture.GetTemporary(texture.width, texture.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(texture, bufferRT);
            RenderTexture.active = bufferRT;
            Texture2D texBuffer = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false, false);
            texBuffer.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            texBuffer.Apply();
            imageContentHash = texBuffer.imageContentsHash;
            RenderTexture.ReleaseTemporary(bufferRT);
            byte[] rawPNG = texBuffer.EncodeToPNG();
            Object.DestroyImmediate(texBuffer);
            
            string dir = Path.GetDirectoryName(dstTexturePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllBytes(dstTexturePath, rawPNG);
            
            var uvReader = new PrimvarReaderExportSample<Vector2>();
            uvReader.varname.defaultValue = new TfToken("st");

            try
            {
                usdScene.Write(usdShaderPath + "/uvReader", uvReader);
            }
            catch (Exception _)
            {
                Debug.Log(usdShaderPath);

            }

            string relTexturePath = Path.GetRelativePath(usdFileDir, dstTexturePath);
            var usdTexReader = new TextureReaderSample(relTexturePath, usdShaderPath + "/uvReader.outputs:result");
            usdTexReader.wrapS = new Connectable<TextureReaderSample.WrapMode>(
                TextureReaderSample.GetWrapMode(texture.wrapModeU));
            usdTexReader.wrapT = new Connectable<TextureReaderSample.WrapMode>(
                TextureReaderSample.GetWrapMode(texture.wrapModeV));

            string texPath = $"{usdShaderPath}/{propertyName}";
            try
            {
                usdScene.Write(texPath, usdTexReader);
            }
            catch (Exception e)
            {
                Debug.LogWarning(texPath);
            }
            
            return $"{usdShaderPath}/{propertyName}.outputs:{channel.ToString().ToLower()}";
        }
    }
}