using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

namespace Remalux
{
    /// <summary>
    /// Обертка для Unity.Barracuda API, которая использует рефлексию для вызова методов.
    /// Это позволяет избежать прямых зависимостей от Barracuda в основных классах.
    /// </summary>
    public static class BarracudaWrapper
    {
        // Объекты для поиска типов через рефлексию
        private static Assembly barracudaAssembly = null;
        private static Type modelType = null;
        private static Type tensorType = null;
        private static Type workerFactoryType = null;
        private static Type workerType = null;
        
        // Инициализируем типы при первом использовании
        static BarracudaWrapper()
        {
            InitializeBarracudaTypes();
        }
        
        /// <summary>
        /// Загружает модель из файла
        /// </summary>
        public static object LoadModelFromFile(string path)
        {
            if (!IsBarracudaAvailable())
            {
                Debug.LogError("Barracuda not available. Cannot load model.");
                return null;
            }
            
            try
            {
                // Получаем метод ModelLoader.Load
                Type modelLoaderType = barracudaAssembly.GetType("Unity.Barracuda.ModelLoader");
                MethodInfo loadMethod = modelLoaderType.GetMethod("Load", new Type[] { typeof(string), typeof(bool) });
                
                // Загружаем модель
                return loadMethod.Invoke(null, new object[] { path, false });
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading model: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Создает тензор из текстуры
        /// </summary>
        public static object CreateTensorFromTexture(Texture texture)
        {
            if (!IsBarracudaAvailable() || texture == null)
            {
                Debug.LogError("Barracuda not available or texture is null. Cannot create tensor.");
                return null;
            }
            
            try
            {
                // Создаем тензор из текстуры
                ConstructorInfo tensorConstructor = tensorType.GetConstructor(new Type[] { typeof(Texture) });
                return tensorConstructor.Invoke(new object[] { texture });
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating tensor: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Создает воркер для выполнения инференса
        /// </summary>
        public static object CreateWorker(object model)
        {
            if (!IsBarracudaAvailable() || model == null)
            {
                Debug.LogError("Barracuda not available or model is null. Cannot create worker.");
                return null;
            }
            
            try
            {
                // Получаем тип Worker.Type
                Type workerTypeEnum = barracudaAssembly.GetType("Unity.Barracuda.WorkerFactory+Type");
                object workerTypeValue = Enum.Parse(workerTypeEnum, "CSharp");
                
                // Получаем метод CreateWorker
                MethodInfo createWorkerMethod = workerFactoryType.GetMethod("CreateWorker", 
                    new Type[] { workerTypeEnum, modelType });
                
                // Создаем воркер
                return createWorkerMethod.Invoke(null, new object[] { workerTypeValue, model });
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating worker: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Выполняет инференс на модели
        /// </summary>
        public static void ExecuteModel(object worker, object inputTensor)
        {
            if (!IsBarracudaAvailable() || worker == null || inputTensor == null)
            {
                Debug.LogError("Barracuda not available or parameters are null. Cannot execute model.");
                return;
            }
            
            try
            {
                // Получаем метод Execute
                MethodInfo executeMethod = workerType.GetMethod("Execute", new Type[] { tensorType });
                
                // Выполняем инференс
                executeMethod.Invoke(worker, new object[] { inputTensor });
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing model: {e.Message}");
            }
        }
        
        /// <summary>
        /// Получает результат инференса
        /// </summary>
        public static object PeekOutput(object worker)
        {
            if (!IsBarracudaAvailable() || worker == null)
            {
                Debug.LogError("Barracuda not available or worker is null. Cannot peek output.");
                return null;
            }
            
            try
            {
                // Получаем метод PeekOutput
                MethodInfo peekOutputMethod = workerType.GetMethod("PeekOutput", Type.EmptyTypes);
                
                // Получаем результат
                return peekOutputMethod.Invoke(worker, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error peeking output: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Освобождает ресурсы тензора
        /// </summary>
        public static void DisposeTensor(object tensor)
        {
            if (!IsBarracudaAvailable() || tensor == null)
                return;
            
            try
            {
                // Получаем метод Dispose
                MethodInfo disposeMethod = tensorType.GetMethod("Dispose");
                
                // Освобождаем ресурсы
                disposeMethod.Invoke(tensor, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error disposing tensor: {e.Message}");
            }
        }
        
        /// <summary>
        /// Освобождает ресурсы воркера
        /// </summary>
        public static void DisposeWorker(object worker)
        {
            if (!IsBarracudaAvailable() || worker == null)
                return;
            
            try
            {
                // Получаем метод Dispose
                MethodInfo disposeMethod = workerType.GetMethod("Dispose");
                
                // Освобождаем ресурсы
                disposeMethod.Invoke(worker, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error disposing worker: {e.Message}");
            }
        }
        
        /// <summary>
        /// Получает данные из тензора в виде массива float[]
        /// </summary>
        public static float[] GetTensorData(object tensor)
        {
            if (!IsBarracudaAvailable() || tensor == null)
            {
                Debug.LogError("Barracuda not available or tensor is null. Cannot get tensor data.");
                return new float[0];
            }
            
            try
            {
                // Получаем свойство data
                PropertyInfo dataProperty = tensorType.GetProperty("data");
                
                // Получаем данные
                return (float[])dataProperty.GetValue(tensor);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting tensor data: {e.Message}");
                return new float[0];
            }
        }
        
        /// <summary>
        /// Получает размерность тензора
        /// </summary>
        public static int[] GetTensorShape(object tensor)
        {
            if (!IsBarracudaAvailable() || tensor == null)
            {
                Debug.LogError("Barracuda not available or tensor is null. Cannot get tensor shape.");
                return new int[0];
            }
            
            try
            {
                // Получаем свойство shape
                PropertyInfo shapeProperty = tensorType.GetProperty("shape");
                
                // Получаем размерность
                return (int[])shapeProperty.GetValue(tensor);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting tensor shape: {e.Message}");
                return new int[0];
            }
        }
        
        /// <summary>
        /// Проверяет доступность Barracuda API
        /// </summary>
        public static bool IsBarracudaAvailable()
        {
            if (barracudaAssembly == null)
            {
                InitializeBarracudaTypes();
            }
            
            return barracudaAssembly != null && modelType != null && 
                   tensorType != null && workerFactoryType != null && workerType != null;
        }
        
        /// <summary>
        /// Инициализирует типы Barracuda через рефлексию
        /// </summary>
        private static void InitializeBarracudaTypes()
        {
            try
            {
                // Ищем Barracuda среди загруженных сборок
                barracudaAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Unity.Barracuda");
                
                if (barracudaAssembly != null)
                {
                    // Инициализируем типы
                    modelType = barracudaAssembly.GetType("Unity.Barracuda.Model");
                    tensorType = barracudaAssembly.GetType("Unity.Barracuda.Tensor");
                    workerFactoryType = barracudaAssembly.GetType("Unity.Barracuda.WorkerFactory");
                    workerType = barracudaAssembly.GetType("Unity.Barracuda.IWorker");
                    
                    Debug.Log("Barracuda API initialized successfully via reflection");
                }
                else
                {
                    Debug.LogWarning("Barracuda assembly not found");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error initializing Barracuda types: {e.Message}");
            }
        }
    }
} 