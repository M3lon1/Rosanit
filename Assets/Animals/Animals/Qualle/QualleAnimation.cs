using UnityEngine;

public class QualleAnimation : MonoBehaviour
{
    [Header("Körper")]
    public Transform blume;
    public Transform radial1;

    [Header("Tentakel")]
    public Transform radial001;
    public Transform radial002;
    public Transform radial003;
    public Transform radial004;

    private Vector3 blumeStartScale;
    private Vector3 radial1StartScale;

    private Quaternion[] startRotations;
    private Transform[] tentacles;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;

        blumeStartScale = blume.localScale;
        radial1StartScale = radial1.localScale;

        tentacles = new Transform[]
        {
            radial001,
            radial002,
            radial003,
            radial004
        };

        startRotations = new Quaternion[tentacles.Length];

        for (int i = 0; i < tentacles.Length; i++)
        {
            startRotations[i] = tentacles[i].localRotation;
        }
    }

    void Update()
    {
        float t = Time.time;

        // ==========================
        // QUALLE SCHWEBT
        // ==========================

        transform.position = startPosition +
            new Vector3(
                Mathf.Sin(t * 0.3f) * 0.03f,
                Mathf.Sin(t * 0.7f) * 0.08f,
                0f
            );

        // ==========================
        // GLOCKE PUMPT
        // ==========================

        float pulse = (Mathf.Sin(t * 2.2f) + 1f) * 0.5f;

        float width = Mathf.Lerp(1f, 0.88f, pulse);
        float height = Mathf.Lerp(1f, 1.15f, pulse);

        Vector3 bellScale =
            new Vector3(width, height, width);

        blume.localScale =
            Vector3.Scale(blumeStartScale, bellScale);

        radial1.localScale =
            Vector3.Scale(radial1StartScale, bellScale);

        // ==========================
        // TENTAKEL
        // ==========================

        AnimateTentacle(0, 0f);
        AnimateTentacle(1, 0.7f);
        AnimateTentacle(2, 1.4f);
        AnimateTentacle(3, 2.1f);
    }

    void AnimateTentacle(int index, float offset)
    {
        Transform t = tentacles[index];

        float waveA =
            Mathf.Sin(Time.time * 1.3f + offset);

        float waveB =
            Mathf.Sin(Time.time * 2.0f + offset + 1f);

        float rotX = waveA * 20f;
        float rotZ = waveB * 15f;

        t.localRotation =
            startRotations[index] *
            Quaternion.Euler(rotX, 0f, rotZ);

        float stretch =
    1f + Mathf.Sin(Time.time * 2.4f + offset) * 0.12f;

        t.localScale =
            new Vector3(
                1f,
                stretch,
                1f
            );
    }
}