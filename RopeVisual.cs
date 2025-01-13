using System.Collections.Generic;
using UnityEngine;

public class RopeVisual : MonoBehaviour
{
    private LineRenderer line;
    private float timeSinceStart;

    void Start()
    {
        line = new GameObject("ROPEVISUAL").AddComponent<LineRenderer>();
        line.widthMultiplier = 0.06f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        line.material = new Material(Shader.Find("Standard"));
        line.material.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        line.generateLightingData = true;
        line.numCapVertices = 2;
    }

    public void RopeShoot()
    {
        line.enabled = true;
        timeSinceStart = 0f;
    }

    public void RopeUpdate(Vector3 a, Vector3 b)
    {
        timeSinceStart += Time.deltaTime;

        int numPoints = 16;
        float amp = Mathf.Lerp(0.66f, 0, timeSinceStart * 3f);
        float freq = Mathf.Lerp(8f, 2f, timeSinceStart * 5f);
        List<Vector3> points = GenerateSineWavePoints(a, b, numPoints, amp, freq);

        if (line.positionCount != numPoints)
            line.positionCount = numPoints;
        line.SetPositions(points.ToArray());
    }

    List<Vector3> GenerateSineWavePoints(Vector3 a, Vector3 b, int numPoints, float amp, float freq)
    {
        List<Vector3> points = new List<Vector3>();

        for (int i = 0; i <= numPoints; i++)
        {
            float t = (float)i / numPoints;

            Vector3 linearPoint = Vector3.Lerp(a, b, t);

            float sineValue = Mathf.Sin(t * freq * Mathf.PI * 2) * amp;

            // Calculate the direction orthogonal to the line for applying sine wave
            Vector3 direction = (b - a).normalized;
            Vector3 orthogonalDirection = Vector3.Cross(direction, Vector3.right).normalized;

            // Apply the sine wave along the orthogonal direction
            Vector3 sineWavePoint = linearPoint + orthogonalDirection * sineValue;

            points.Add(sineWavePoint);
        }

        return points;
    }

    public void RopeRelease()
    {
        line.enabled = false;
    }
}
