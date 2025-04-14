using UnityEngine;

namespace Remalux.WallPainting.Vision
{
      public class WallIdentifier : MonoBehaviour
      {
            public string wallId;
            public Vector3 wallNormal;
            public float wallHeight;
            public float wallWidth;

            private void Start()
            {
                  // Generate a unique ID if not set
                  if (string.IsNullOrEmpty(wallId))
                  {
                        wallId = System.Guid.NewGuid().ToString();
                  }

                  // Calculate wall dimensions
                  var renderer = GetComponent<Renderer>();
                  if (renderer != null)
                  {
                        var bounds = renderer.bounds;
                        wallHeight = bounds.size.y;
                        wallWidth = bounds.size.x;
                  }

                  // Set wall normal based on transform
                  wallNormal = transform.forward;
            }
      }
}