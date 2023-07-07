using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Harpoon.USD
{
    public static class MaterialUtils
    {
        public static void GetProperties(this Material material,
            out Dictionary<string, float> floats,
            out Dictionary<string, int> integers,
            out Dictionary<string, Color> colors,
            out Dictionary<string, Vector4> vectors,
            out Dictionary<string, Texture> textures)
        {
            Shader shader = material.shader;
            floats = new Dictionary<string, float>();
            integers = new Dictionary<string, int>();
            colors = new Dictionary<string, Color>();
            vectors = new Dictionary<string, Vector4>();
            textures = new Dictionary<string, Texture>();

            int propertyCount = shader.GetPropertyCount();
            for (int i = 0; i < propertyCount; ++i)
            {
                string name = shader.GetPropertyName(i);
                if (!material.HasProperty(name))
                    continue;
                switch (shader.GetPropertyType(i))
                {
                    case ShaderPropertyType.Color:
                    {
                        colors[name] = material.GetColor(name).linear;
                        break;
                    }
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                    {
                        floats[name] = material.GetFloat(name);
                        break;
                    }
                    case ShaderPropertyType.Int:
                    {
                        integers[name] = material.GetInt(name);
                        break;
                    }
                    case ShaderPropertyType.Vector:
                    {
                        vectors[name] = material.GetVector(name);
                        break;
                    }
                    case ShaderPropertyType.Texture:
                    {
                        textures[name] = material.GetTexture(name);
                        break;
                    }
                }
            }
        }
    }
}
