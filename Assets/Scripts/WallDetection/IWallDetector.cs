using UnityEngine;

namespace Remalux.AR.WallDetection
{
      public interface IWallDetector
      {
            bool IsWall(Transform surface);
            Bounds GetWallBounds(Transform wall);
            bool IsPointOnWall(Vector3 worldPoint, Transform wall);
      }
}