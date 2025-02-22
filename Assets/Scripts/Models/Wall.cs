using UnityEngine;

public class Wall : MonoBehaviour
{
    public Vector3 start;
    public Vector3 end;
    public float height;
    public float confidence;

    public void Initialize(Vector3 startPoint, Vector3 endPoint, float wallHeight)
    {
        start = startPoint;
        end = endPoint;
        height = wallHeight;
    }
} 