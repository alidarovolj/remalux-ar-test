// ARManager.cs - главный менеджер приложения
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARManager : MonoBehaviour
{
    public static ARManager Instance { get; private set; }
    
    [SerializeField] private ARSession arSession;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Camera arCamera;
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StartAR()
    {
        arSession.enabled = true;
        planeManager.enabled = true;
    }

    public void StopAR()
    {
        arSession.enabled = false;
        planeManager.enabled = false;
    }
}