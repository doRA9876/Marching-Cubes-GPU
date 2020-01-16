using UnityEngine;
using System.Collections.Generic;

namespace MarchingCubesProject
{
  struct Polygon
  {
    public Vector3 t0;
    public Vector3 t1;
    public Vector3 t2;
  }
  public class Example : MonoBehaviour
  {
    public enum MARCHING_MODE {  CUBES, TETRAHEDRON };
    public Material m_material;

    public MARCHING_MODE mode = MARCHING_MODE.CUBES;

    public int seed = 0;

    List<GameObject> meshes = new List<GameObject>();

    void Start()
    {
      //Set the mode used to create the mesh.
      //Cubes is faster and creates less verts, tetrahedrons is slower and creates more verts but better represents the mesh surface.
      Marching marching = null;
      if (mode == MARCHING_MODE.TETRAHEDRON)
        marching = new MarchingTertrahedron();
      else
        marching = new MarchingCubes();

      //Surface is the value that represents the surface of mesh
      //For example the perlin noise has a range of -1 to 1 so the mid point is where we want the surface to cut through.
      //The target value does not have to be the mid point it can be any value with in the range.
      marching.Surface = 0.0f;

      //The size of voxel array.
      int width = 8;
      int height = 8;
      int length = 8;

      Vector3 center = new Vector3(3.5f, 3.5f, 3.5f);

      float[] voxels = new float[width * height * length];

      //Fill voxels with values. Im using perlin noise but any method to create voxels will work.
      for (int x = 0; x < width; x++)
      {
        for (int y = 0; y < height; y++)
        {
          for (int z = 0; z < length; z++)
          {
            int idx = x + y * width + z * width * height;
            float distance = Vector3.Distance(center, new Vector3(x, y, z));
            float r = 3f;

            if (distance < r)
            {
              float offset = r - distance;
              if (offset >= 1f)
                voxels[idx] = 1f;
              else
                voxels[idx] = offset;
            }
            else
              voxels[idx] = 0f;
          }
        }
      }

      List<Vector3> verts = new List<Vector3>();
      List<int> indices = new List<int>();

      //The mesh produced is not optimal. There is one vert for each index.
      //Would need to weld vertices for better quality mesh.
      marching.Generate(voxels, width, height, length, verts, indices);
      //RenderOnGPU(verts, indices);

      //A mesh in unity can only be made up of 65000 verts.
      //Need to split the verts between multiple meshes.

      int maxVertsPerMesh = 30000; //must be divisible by 3, ie 3 verts == 1 triangle
      int numMeshes = verts.Count / maxVertsPerMesh + 1;

      for (int i = 0; i < numMeshes; i++)
      {

        List<Vector3> splitVerts = new List<Vector3>();
        List<int> splitIndices = new List<int>();

        for (int j = 0; j < maxVertsPerMesh; j++)
        {
          int idx = i * maxVertsPerMesh + j;

          if (idx < verts.Count)
          {
            splitVerts.Add(verts[idx]);
            splitIndices.Add(j);
          }
        }

        if (splitVerts.Count == 0) continue;

        Mesh mesh = new Mesh();
        mesh.SetVertices(splitVerts);
        mesh.SetTriangles(splitIndices, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        GameObject go = new GameObject("Mesh");
        go.transform.parent = transform;
        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        go.GetComponent<Renderer>().material = m_material;
        go.GetComponent<MeshFilter>().mesh = mesh;
        go.transform.localPosition = new Vector3(-width / 2, -height / 2, -length / 2);

        meshes.Add(go);
      }

    }

    void Update()
    {
      transform.Rotate(Vector3.up, 10.0f * Time.deltaTime);
    }

    void RenderOnGPU( List<Vector3> verts, List<int> indices)
    {
      int polyNum = indices.Count / 3;
      Polygon[] polygon = new Polygon[polyNum];
      for (int i = 0; i < polyNum; i++)
      {
        polygon[i].t0 = verts[indices[i * 3]];
        polygon[i].t1 = verts[indices[i * 3 + 1]];
        polygon[i].t2 = verts[indices[i * 3 + 2]];
      }
    }
  }
}

