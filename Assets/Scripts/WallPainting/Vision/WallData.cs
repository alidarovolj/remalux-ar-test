using UnityEngine;

namespace Remalux.WallPainting.Vision
{
      public class WallData
      {
            public Vector3 topLeft;
            public Vector3 topRight;
            public Vector3 bottomLeft;
            public Vector3 bottomRight;
            public float width;
            public float height;
            public string id;

            public WallData(Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight)
            {
                  this.topLeft = topLeft;
                  this.topRight = topRight;
                  this.bottomLeft = bottomLeft;
                  this.bottomRight = bottomRight;
                  this.width = Vector3.Distance(topLeft, topRight);
                  this.height = Vector3.Distance(topLeft, bottomLeft);
                  this.id = System.Guid.NewGuid().ToString();
            }

            public WallData(Vector3 position, Quaternion rotation, Vector3 scale, int wallId)
            {
                  Vector3 right = rotation * Vector3.right * (scale.x * 0.5f);
                  Vector3 up = rotation * Vector3.up * (scale.y * 0.5f);

                  this.topLeft = position - right + up;
                  this.topRight = position + right + up;
                  this.bottomLeft = position - right - up;
                  this.bottomRight = position + right - up;
                  this.width = scale.x;
                  this.height = scale.y;
                  this.id = wallId.ToString();
            }

            public Vector3 position => (topLeft + topRight + bottomLeft + bottomRight) * 0.25f;
            public Quaternion rotation => Quaternion.LookRotation(Vector3.Cross(topRight - topLeft, topLeft - bottomLeft).normalized);
            public Vector3 scale => new Vector3(width, height, 0.1f);
      }
}