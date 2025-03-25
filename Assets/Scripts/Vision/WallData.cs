using UnityEngine;

namespace Remalux.WallPainting
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
      }
}