using UnityEngine;
using System.Collections.Generic;

public static class MeshGenerator {
    public static void GenerateTube(Mesh mesh, List<Vector3> points, float radius1, float radius2, int sides) {
        // For simplicity, just generate a cylinder-like mesh between two points (approx)
        if (points.Count < 2) return;

        Vector3 start = points[0];
        Vector3 end = points[points.Count - 1];
        Vector3 direction = (end - start).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < sides; i++) {
            float angle = (float)i / sides * Mathf.PI * 2;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);

            vertices.Add(start + rotation * offset * radius1);
            vertices.Add(end + rotation * offset * radius2);
        }

        for (int i = 0; i < sides; i++) {
            int i0 = i * 2;
            int i1 = (i * 2 + 2) % (sides * 2);
            int i2 = i0 + 1;
            int i3 = i1 + 1;

            triangles.Add(i0); triangles.Add(i2); triangles.Add(i1);
            triangles.Add(i1); triangles.Add(i2); triangles.Add(i3);
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }
}
