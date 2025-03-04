using UnityEngine;
using System.Collections.Generic;

namespace Remalux.AR.Utilities
{
    /// <summary>
    /// Утилитарный класс для поиска объектов в сцене, заменяющий устаревшие методы FindObjectOfType и FindObjectsOfType
    /// </summary>
    public static class ObjectFinder
    {
        /// <summary>
        /// Находит первый активный объект указанного типа в сцене
        /// Заменяет устаревший метод FindObjectOfType
        /// </summary>
        public static T FindFirstObject<T>() where T : Object
        {
            return Object.FindFirstObjectByType<T>();
        }

        /// <summary>
        /// Находит любой объект указанного типа в сцене (может быть быстрее, чем FindFirstObject)
        /// Заменяет устаревший метод FindObjectOfType
        /// </summary>
        public static T FindAnyObject<T>() where T : Object
        {
            return Object.FindAnyObjectByType<T>();
        }

        /// <summary>
        /// Находит все объекты указанного типа в сцене, отсортированные по InstanceID
        /// Заменяет устаревший метод FindObjectsOfType
        /// </summary>
        public static T[] FindAllObjectsSorted<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.InstanceID);
        }

        /// <summary>
        /// Находит все объекты указанного типа в сцене без сортировки (быстрее)
        /// Заменяет устаревший метод FindObjectsOfType
        /// </summary>
        public static T[] FindAllObjects<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        }

        /// <summary>
        /// Находит все объекты указанного типа в сцене, включая неактивные, отсортированные по InstanceID
        /// Заменяет устаревший метод FindObjectsOfType(true)
        /// </summary>
        public static T[] FindAllObjectsIncludingInactiveSorted<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        }

        /// <summary>
        /// Находит все объекты указанного типа в сцене, включая неактивные, без сортировки (быстрее)
        /// Заменяет устаревший метод FindObjectsOfType(true)
        /// </summary>
        public static T[] FindAllObjectsIncludingInactive<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }
    }
} 