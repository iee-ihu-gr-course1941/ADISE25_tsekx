using UnityEngine;
using System.Collections.Generic;

public class BackgammonTriangles : MonoBehaviour
{
    public float boardWidth = 200f;
    public float boardHeight = 250f;

    public float triangleWidth = 200f / 12f; 
    public float triangleHeight = 110f;      

    public Color colorA = new Color(0.9f, 0.8f, 0.6f);
    public Color colorB = new Color(0.6f, 0.3f, 0.2f);

    void Start()
    {
        GenerateTriangles();
    }

    void GenerateTriangles()
    {
        float halfW = boardWidth / 2f;
        float halfH = boardHeight / 2f;

        int triIndex = 0;

        for (int i = 0; i < 12; i++)
        {
            float x1 = -halfW + triangleWidth * i;
            float x2 = x1 + triangleWidth;

            Vector3 p1 = new Vector3(x1, 0, halfH);
            Vector3 p2 = new Vector3(x2, 0, halfH);
            Vector3 p3 = new Vector3(x1 + triangleWidth / 2f, 0, halfH - triangleHeight);

            CreateTriangleMesh($"TriangleTop_{i+1}", p1, p2, p3, triIndex % 2 == 0 ? colorA : colorB);
            triIndex++;
        }

       
        for (int i = 0; i < 12; i++)
        {
            float x1 = -halfW + triangleWidth * i;
            float x2 = x1 + triangleWidth;

            Vector3 p1 = new Vector3(x1, 0, -halfH);
            Vector3 p2 = new Vector3(x2, 0, -halfH);
            Vector3 p3 = new Vector3(x1 + triangleWidth / 2f, 0, -halfH + triangleHeight);

            CreateTriangleMesh($"TriangleBottom_{i+1}", p1, p2, p3, triIndex % 2 == 0 ? colorA : colorB);
            triIndex++;
        }
    }

    void CreateTriangleMesh(string name, Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        GameObject tri = new GameObject(name);
        tri.transform.parent = transform;

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { a, b, c };
        mesh.triangles = new int[] { 0, 1, 2 };

        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshRenderer renderer = tri.AddComponent<MeshRenderer>();
        MeshFilter filter = tri.AddComponent<MeshFilter>();

        filter.mesh = mesh;

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        renderer.material = mat;
    }
}
