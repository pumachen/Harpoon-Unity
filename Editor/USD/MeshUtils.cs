using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Harpoon.USD
{
    public static class MeshUtils
    {
        public static Mesh ExtractSubMesh(this Mesh mesh, int subMeshIndex)
        {
            Vector3[] srcVertices = mesh.vertices;
            Vector3[] srcNormals = mesh.normals;
            Vector4[] srcTangents = mesh.tangents;
            Color[] srcColors = mesh.colors;
            if (srcColors.Length == 0)
                srcColors = new Color[mesh.vertexCount];
            Vector2[] srcUV = mesh.uv;

            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector4> tangents = new();
            List<Color> colors = new();
            List<Vector2> uv = new();
            int[] triangles = mesh.GetTriangles(subMeshIndex);

            Dictionary<int, int> indexMapping = new();

            for (int i = 0; i < triangles.Length; ++i)
            {
                int srcVtx = triangles[i];
                if (!indexMapping.TryGetValue(srcVtx, out int dstVtx))
                {
                    dstVtx = vertices.Count;
                    vertices.Add(srcVertices[srcVtx]);
                    normals.Add(srcNormals[srcVtx]);
                    tangents.Add(srcTangents[srcVtx]);
                    colors.Add(srcColors[srcVtx]);
                    uv.Add(srcUV[srcVtx]);
                }

                triangles[i] = dstVtx;
            }

            Mesh o = new Mesh();
            o.vertices = vertices.ToArray();
            o.normals = normals.ToArray();
            o.tangents = tangents.ToArray();
            o.colors = colors.ToArray();
            o.uv = uv.ToArray();
            o.triangles = triangles;
            return o;
        }
    }
}