using UnityEngine;

public class FishWave : MonoBehaviour
{
    public float amplitude = 0.05f;
    public float frequency = 3f;
    public float speed = 2f;

    [Header("Head Control")]
    [Range(0f, 1f)]
    public float headStability = 0.6f;   // ab wann der Kopf ruhig wird

    [Range(0f, 1f)]
    public float headMovement = 0.2f;     // wie viel der Kopf sich trotzdem bewegt

    public enum Axis { X, Y, Z }
    public Axis bodyAxis = Axis.Z;        // welche Achse Kopf -> Schwanz ist

    Mesh mesh;
    Vector3[] originalVertices;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
    }

    void Update()
    {
        Vector3[] vertices = new Vector3[originalVertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = originalVertices[i];

            // Achse wählen
            float pos = GetAxis(v);

            // Sinus Welle
            float wave = Mathf.Sin(Time.time * speed + pos * frequency);

            // Maske: Kopf -> Schwanz
            float mask = Mathf.InverseLerp(headStability, 1f, pos);
            mask = Mathf.SmoothStep(0f, 1f, mask);

            // Kopf soll nicht komplett still sein
            float finalMask = Mathf.Lerp(headMovement, 1f, mask);

            // Bewegung
            v.y += wave * amplitude * finalMask;
            v.x += wave * amplitude * 0.5f * finalMask;

            vertices[i] = v;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }

    float GetAxis(Vector3 v)
    {
        switch (bodyAxis)
        {
            case Axis.X: return v.x;
            case Axis.Y: return v.y;
            case Axis.Z: return v.z;
        }
        return v.z;
    }
}