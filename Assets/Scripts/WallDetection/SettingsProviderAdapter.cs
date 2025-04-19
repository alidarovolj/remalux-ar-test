using System;
using UnityEngine;
using System.Reflection;

namespace Remalux.WallDetection
{
    /// <summary>
    /// Adapter that converts SettingsManager to ISettingsProvider through reflection
    /// to avoid direct reference dependency
    /// </summary>
    public class SettingsProviderAdapter : MonoBehaviour, ISettingsProvider
    {
        private object _settingsManager;
        private Type _settingsManagerType;
        private PropertyInfo _customResolutionProperty;
        private PropertyInfo _processingIntervalProperty;
        private PropertyInfo _showPerformanceMetricsProperty;
        private PropertyInfo _autoAdjustLightingProperty;
        private PropertyInfo _lowLightBoostProperty;
        private PropertyInfo _lowLightThresholdProperty;
        private PropertyInfo _contrastEnhancementProperty;
        private MethodInfo _getCurrentResolutionMethod;
        private EventInfo _onSettingsChangedEvent;
        private Delegate _originalHandler;
        
        public void SetSettingsManager(object settingsManager)
        {
            _settingsManager = settingsManager;
            _settingsManagerType = settingsManager.GetType();
            
            // Get properties and methods through reflection
            _customResolutionProperty = _settingsManagerType.GetProperty("CustomResolution");
            _processingIntervalProperty = _settingsManagerType.GetProperty("ProcessingInterval");
            _showPerformanceMetricsProperty = _settingsManagerType.GetProperty("ShowPerformanceMetrics");
            _autoAdjustLightingProperty = _settingsManagerType.GetProperty("AutoAdjustLighting");
            _lowLightBoostProperty = _settingsManagerType.GetProperty("LowLightBoost");
            _lowLightThresholdProperty = _settingsManagerType.GetProperty("LowLightThreshold");
            _contrastEnhancementProperty = _settingsManagerType.GetProperty("ContrastEnhancement");
            
            _getCurrentResolutionMethod = _settingsManagerType.GetMethod("GetCurrentResolution");
            _onSettingsChangedEvent = _settingsManagerType.GetEvent("OnSettingsChanged");
            
            // Subscribe to settings change event
            if (_onSettingsChangedEvent != null)
            {
                Type delegateType = _onSettingsChangedEvent.EventHandlerType;
                _originalHandler = Delegate.CreateDelegate(delegateType, this, GetType().GetMethod("OnSettingsManagerChanged", BindingFlags.NonPublic | BindingFlags.Instance));
                _onSettingsChangedEvent.AddEventHandler(_settingsManager, _originalHandler);
            }
        }
        
        private void OnSettingsManagerChanged()
        {
            // Relay the event
            OnSettingsChanged?.Invoke();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from the event
            if (_settingsManager != null && _onSettingsChangedEvent != null && _originalHandler != null)
            {
                _onSettingsChangedEvent.RemoveEventHandler(_settingsManager, _originalHandler);
            }
        }

        public Vector2Int GetCurrentResolution()
        {
            if (_settingsManager != null && _getCurrentResolutionMethod != null)
            {
                return (Vector2Int)_getCurrentResolutionMethod.Invoke(_settingsManager, null);
            }
            return new Vector2Int(320, 320); // Default value
        }

        public Vector2Int CustomResolution
        {
            get
            {
                if (_settingsManager != null && _customResolutionProperty != null)
                {
                    return (Vector2Int)_customResolutionProperty.GetValue(_settingsManager);
                }
                return new Vector2Int(320, 320); // Default value
            }
        }

        public float ProcessingInterval
        {
            get
            {
                if (_settingsManager != null && _processingIntervalProperty != null)
                {
                    return (float)_processingIntervalProperty.GetValue(_settingsManager);
                }
                return 0.1f; // Default value
            }
        }

        public bool ShowPerformanceMetrics
        {
            get
            {
                if (_settingsManager != null && _showPerformanceMetricsProperty != null)
                {
                    return (bool)_showPerformanceMetricsProperty.GetValue(_settingsManager);
                }
                return false; // Default value
            }
        }

        public bool AutoAdjustLighting
        {
            get
            {
                if (_settingsManager != null && _autoAdjustLightingProperty != null)
                {
                    return (bool)_autoAdjustLightingProperty.GetValue(_settingsManager);
                }
                return true; // Default value
            }
        }

        public float LowLightBoost
        {
            get
            {
                if (_settingsManager != null && _lowLightBoostProperty != null)
                {
                    return (float)_lowLightBoostProperty.GetValue(_settingsManager);
                }
                return 1.5f; // Default value
            }
        }

        public float LowLightThreshold
        {
            get
            {
                if (_settingsManager != null && _lowLightThresholdProperty != null)
                {
                    return (float)_lowLightThresholdProperty.GetValue(_settingsManager);
                }
                return 0.3f; // Default value
            }
        }

        public float ContrastEnhancement
        {
            get
            {
                if (_settingsManager != null && _contrastEnhancementProperty != null)
                {
                    return (float)_contrastEnhancementProperty.GetValue(_settingsManager);
                }
                return 1.2f; // Default value
            }
        }

        public event Action OnSettingsChanged;
    }
} 