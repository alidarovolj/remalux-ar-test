using UnityEngine;

namespace Remalux.WallPainting
{
      public interface IWallTextureApplier
      {
            void ApplyTextureToAllWalls(Material material);
            void UpdateWallColors(Color color);
      }
}