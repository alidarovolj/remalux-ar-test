using UnityEngine;
using UnityEngine.UI;

namespace Remalux.AR
{
    /// <summary>
    /// Компонент для кнопки выбора цвета
    /// </summary>
    public class SimpleColorButton : MonoBehaviour
    {
        [SerializeField] private Image buttonImage;
        [SerializeField] private Material paintMaterial;
        [SerializeField] private bool useMainTexturePreview = true;

        private void Awake()
        {
            if (buttonImage == null)
                buttonImage = GetComponent<Image>();
        }

        private void Start()
        {
            UpdateButtonAppearance();
        }

        /// <summary>
        /// Установить материал для кнопки
        /// </summary>
        public void SetMaterial(Material material)
        {
            paintMaterial = material;
            UpdateButtonAppearance();
        }

        /// <summary>
        /// Получить материал кнопки
        /// </summary>
        public Material GetMaterial()
        {
            return paintMaterial;
        }
        
        /// <summary>
        /// Установить изображение кнопки
        /// </summary>
        public void SetButtonImage(Image image)
        {
            buttonImage = image;
            UpdateButtonAppearance();
        }
        
        /// <summary>
        /// Получить изображение кнопки
        /// </summary>
        public Image GetButtonImage()
        {
            return buttonImage;
        }

        private void UpdateButtonAppearance()
        {
            if (buttonImage == null || paintMaterial == null)
                return;

            // Устанавливаем цвет кнопки в соответствии с материалом
            if (paintMaterial.HasProperty("_Color"))
            {
                buttonImage.color = paintMaterial.color;
            }

            // Если нужно, используем текстуру материала
            if (useMainTexturePreview && paintMaterial.HasProperty("_MainTex"))
            {
                Texture mainTexture = paintMaterial.mainTexture;
                if (mainTexture != null)
                {
                    // Создаем спрайт из текстуры
                    Sprite sprite = Sprite.Create(
                        mainTexture as Texture2D,
                        new Rect(0, 0, mainTexture.width, mainTexture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    
                    buttonImage.sprite = sprite;
                    
                    // Настраиваем режим отображения изображения
                    buttonImage.type = Image.Type.Sliced;
                    buttonImage.preserveAspect = true;
                }
            }
        }

        /// <summary>
        /// Выделить кнопку как выбранную
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (buttonImage != null)
            {
                // Изменяем внешний вид кнопки при выборе
                if (selected)
                {
                    // Добавляем обводку или другой эффект выделения
                    buttonImage.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                }
                else
                {
                    // Возвращаем обычный вид
                    buttonImage.transform.localScale = Vector3.one;
                }
            }
        }
    }
}