// WallPainter.cs
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class WallPainter : MonoBehaviour
{
    [SerializeField] private Material paintMaterial;
    [SerializeField] private float brushSize = 0.1f;
    private Camera arCamera;
    private bool isPainting;

    private void Start()
    {
        arCamera = Camera.main;
    }

    public void SetPaintColor(Color color)
    {
        paintMaterial.color = color;
    }

    public void StartPainting()
    {
        isPainting = true;
    }

    public void StopPainting()
    {
        isPainting = false;
    }

    private void Update()
    {
        if (!isPainting) return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Ray ray = arCamera.ScreenPointToRay(touch.position);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("Wall"))
                {
                    PaintWall(hit.point, hit.collider);
                }
            }
        }
    }

    private void PaintWall(Vector3 position, Collider wall)
    {
        // Создаем отметку краски
        GameObject paintMark = GameObject.CreatePrimitive(PrimitiveType.Quad);
        paintMark.transform.position = position;
        paintMark.transform.rotation = Quaternion.LookRotation(-wall.transform.forward);
        paintMark.transform.localScale = Vector3.one * brushSize;
        
        // Применяем материал краски
        MeshRenderer renderer = paintMark.GetComponent<MeshRenderer>();
        renderer.material = paintMaterial;
        
        // Делаем отметку краски дочерним объектом стены
        paintMark.transform.SetParent(wall.transform);
    }
}