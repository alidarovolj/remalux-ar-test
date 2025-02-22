using UnityEngine;

public class Room : MonoBehaviour
{
    public Vector3[] corners;
    public Wall[] walls;
    
    public void Initialize(Vector3[] roomCorners)
    {
        corners = roomCorners;
    }
} 