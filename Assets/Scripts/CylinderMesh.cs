using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CylinderMesh
{
    public Mesh mesh;
    int CircPoints;

    public CylinderMesh(float d0, float d1, float z0, float z1, int circPoints)
    {
        CircPoints = circPoints;

        Vector3[] vertices = new Vector3[CircPoints * 2];
        int[] triangles = new int[vertices.Length * 3];
        Vector3[] normals = new Vector3[CircPoints * 2];

        for (int i = 0; i < CircPoints; i++)
        {
            float angle = i * 2f * Mathf.PI / CircPoints;

            // Front circle
            vertices[i] = new Vector3(d0 / 2f * Mathf.Cos(angle), d0 / 2f * Mathf.Sin(angle), z0); // vertex
            normals[i] = vertices[i].normalized;
            triangles[3 * i] = i;
            triangles[3 * i + 1] = i + CircPoints;
            triangles[3 * i + 2] = i < CircPoints - 1 ? i + 1 : 0; // Return to 0th front circle vertex

            // Rear Circle
            vertices[i + CircPoints] = new Vector3(d1 / 2f * Mathf.Cos(angle), d1 / 2f * Mathf.Sin(angle), z1); // vertex
            normals[i + CircPoints] = vertices[i].normalized;
            triangles[3 * (i + CircPoints)] = i + CircPoints;
            triangles[3 * (i + CircPoints) + 1] = i < CircPoints - 1 ? i + CircPoints + 1 : CircPoints; // Return to 0th rear circle vertex
            triangles[3 * (i + CircPoints) + 2] = i < CircPoints - 1 ? i + 1 : 0; // Return to 0th front circle vertex
        }

        mesh = new();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
    }

    public void Resize(float d0, float dz, float z0, float z1)
    {
        Vector3[] vertices = new Vector3[CircPoints * 2];

        for (int i = 0; i < CircPoints; i++)
        {
            float angle = i * 2f * Mathf.PI / CircPoints;

            // Front circle
            vertices[i] = new Vector3(d0 / 2f * Mathf.Cos(angle), d0 / 2f * Mathf.Sin(angle), z0); // vertex

            // Rear Circle
            vertices[i + CircPoints] = new Vector3(dz / 2f * Mathf.Cos(angle), dz / 2f * Mathf.Sin(angle), z1); // vertex
        }

        mesh.vertices = vertices;
    }
}
