using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pxr;
using USD.NET;
using USD.NET.Unity;
using Unity.Formats.USD;
using UnityEngine;
using UnityEngine.Rendering;

namespace Harpoon.USD
{
	public static class USDMesh
	{
		public static Mesh BuildMesh(MeshSample usdMesh, MeshImporter.GeometrySubsets geomSubsets)
		{
			Mesh mesh = new Mesh();
			Vector3[] points = usdMesh.points;
			Vector3[] normals = usdMesh.normals;
			Vector2[] uvs = usdMesh.ArbitraryPrimvars["st"]?.GetValue() as Vector2[];
			int[] faceVertexIndices = usdMesh.faceVertexIndices;

			// Change Basis
			for (int i = 0; i < points.Length; ++i)
			{
				points[i] = UnityTypeConverter.ChangeBasis(points[i]);
				normals[i] = UnityTypeConverter.ChangeBasis(normals[i]);
			}

			if (uvs.Length == points.Length && uvs.Length != faceVertexIndices.Length)
			{
				mesh.vertices = points;
				mesh.normals = normals;
				mesh.uv = uvs;
				mesh.triangles = faceVertexIndices;
			}
			else
			{
				Dictionary<(Vector2, int), int> uvVertices = new Dictionary<(Vector2, int), int>();
				int[] indices = new int[faceVertexIndices.Length];
				for (int i = 0; i < faceVertexIndices.Length; ++i)
				{
					int pointID = faceVertexIndices[i];
					Vector2 uv = uvs[i];
					var uvVertex = (uv, pointID);
					uvVertices.TryAdd(uvVertex, uvVertices.Count);
					indices[i] = uvVertices[uvVertex];
				}

				Vector3[] meshVertices = new Vector3[uvVertices.Count];
				Vector3[] meshNormals = new Vector3[uvVertices.Count];
				Vector2[] meshUVs = new Vector2[uvVertices.Count];
				foreach (var ((uv, pointID), vid) in uvVertices)
				{
					meshVertices[vid] = points[pointID];
					meshNormals[vid] = normals[pointID];
					meshUVs[vid] = uv;
				}

				mesh.vertices = meshVertices;
				mesh.normals = meshNormals;
				mesh.uv = meshUVs;
				mesh.triangles = indices;
			}

			if (geomSubsets.Subsets.Count > 0)
			{
				int indexOffset = 0;
				List<SubMeshDescriptor> subMeshes = new();
				foreach (var subset in geomSubsets.Subsets)
				{
					subMeshes.Add(new SubMeshDescriptor(indexOffset, subset.Value.Length * 3));
					indexOffset += subset.Value.Length * 3;
				}

				mesh.SetSubMeshes(subMeshes.ToArray());
			}
			mesh.RecalculateNormals();
			mesh.RecalculateTangents();
			mesh.Optimize();
			return mesh;
		}
	}
}