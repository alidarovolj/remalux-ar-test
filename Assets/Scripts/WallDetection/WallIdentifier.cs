using UnityEngine;

/// <summary>
/// Компонент для идентификации объекта как стены
/// </summary>
public class WallIdentifier : MonoBehaviour
{
      private void Awake()
      {
            // Устанавливаем тег и слой для стены
            gameObject.tag = "Wall";
            gameObject.layer = LayerMask.NameToLayer("Wall");
      }
}