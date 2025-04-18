using System;
using UnityEngine;

namespace Remalux.Settings
{
    /// <summary>
    /// Base class for color pickers in the application
    /// </summary>
    public abstract class SimpleColorPicker : MonoBehaviour
    {
        /// <summary>
        /// Event triggered when a color is selected
        /// </summary>
        public event Action<Color> OnColorSelected;
        
        /// <summary>
        /// Set the initial color for the picker
        /// </summary>
        /// <param name="color">Initial color</param>
        public abstract void SetInitialColor(Color color);
        
        /// <summary>
        /// Called when a color is selected by the user
        /// </summary>
        /// <param name="color">The selected color</param>
        protected virtual void SelectColor(Color color)
        {
            OnColorSelected?.Invoke(color);
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Open the color picker with an initial color
        /// </summary>
        /// <param name="initialColor">Initial color to show</param>
        public virtual void Open(Color initialColor)
        {
            gameObject.SetActive(true);
            SetInitialColor(initialColor);
        }
    }
} 