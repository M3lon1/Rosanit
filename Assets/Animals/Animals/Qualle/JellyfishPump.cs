using UnityEngine;

public class JellyfishPump : MonoBehaviour
{
    [Header("Pump-Einstellungen")]
    public float pumpSpeed = 2f;       // Wie schnell pumpt die Qualle
    public float pumpAmount = 0.2f;     // Wie stark zieht sie sich zusammen / dehnt sich aus

    private Vector3 originalScale;

    void Start()
    {
        // Speichert die Ausgangsgröße
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Berechnet den pulsierenden Wert mithilfe einer Sinuskurve
        // + 0.5f sorgt dafür, dass die Qualle eher stoßweise pumpt (wie eine echte Qualle)
        float wave = Mathf.Sin(Time.time * pumpSpeed);
        float pulse = Mathf.Max(0, wave);

        // Skalierung anpassen (zieht sich in der Breite zusammen, dehnt sich in der Länge aus)
        float scaleXz = originalScale.x - (pulse * pumpAmount * 0.5f);
        float scaleY = originalScale.y + (pulse * pumpAmount);

        transform.localScale = new Vector3(scaleXz, scaleY, scaleXz);
    }
}