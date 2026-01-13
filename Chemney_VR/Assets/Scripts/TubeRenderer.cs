using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TubeRenderer : MonoBehaviour {
    private List<Vector3> points = new List<Vector3>();
    private Mesh mesh;

    public float _radiusOne = 0.01f;
    public float _radiusTwo = 0.01f;
    public int _sides = 8;
    public bool ColliderTrigger = false;

    private void Awake() {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    public void SetPositions(Vector3[] newPoints) {
        points.Clear();
        points.AddRange(newPoints);
    }

    public void GenerateMesh(bool finalMesh) {
        if (points.Count < 2) return;

        MeshGenerator.GenerateTube(mesh, points, _radiusOne, _radiusTwo, _sides);
        if (finalMesh) {
            AddMeshCollider();
        }
    }

    public void EnableGravity() {
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = true;
    }

    private void AddMeshCollider() {
        MeshCollider mc = gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = true;
        mc.isTrigger = ColliderTrigger;
    }
}
