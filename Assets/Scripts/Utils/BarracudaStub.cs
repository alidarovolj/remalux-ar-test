using UnityEngine;

// Эта заглушка используется только если включена опция USE_BARRACUDA_INDIRECTLY 
// и недоступен BarracudaWrapper. В нормальных обстоятельствах должен
// использоваться BarracudaWrapper, который работает через рефлексию.
#if USE_BARRACUDA_INDIRECTLY

// ВАЖНО: Не добавляйте прямые using на Unity.Barracuda в другие файлы проекта.
// Всегда используйте BarracudaWrapper вместо прямых вызовов API.
namespace Unity.Barracuda
{
    // Минимальная заглушка основных классов Barracuda, чтобы проект мог компилироваться
    // даже если оригинальный Barracuda недоступен
    public class Model
    {
        public string name = "StubModel";
        
        public Model(string modelName = "StubModel")
        {
            name = modelName;
        }
    }
    
    public interface IWorker : System.IDisposable
    {
        string summary { get; }
        void Execute(Tensor input);
        Tensor PeekOutput();
        void Dispose();
    }
    
    public class Tensor : System.IDisposable
    {
        public int[] shape;
        public float[] data;
        
        public Tensor(int batch, int height, int width, int channels)
        {
            shape = new int[] { batch, height, width, channels };
            data = new float[batch * height * width * channels];
        }
        
        public Tensor(Texture texture)
        {
            shape = new int[] { 1, texture.height, texture.width, 4 };
            data = new float[texture.width * texture.height * 4];
        }
        
        public void Dispose()
        {
            // Заглушка для метода Dispose
        }
    }
    
    public class ModelLoader
    {
        public static Model Load(string path, bool verbose = false)
        {
            Debug.LogWarning("Using Barracuda stub - models cannot be loaded. Consider using BarracudaWrapper instead.");
            return new Model("StubLoadedModel");
        }
        
        public static Model Load(byte[] bytes, bool verbose = false)
        {
            Debug.LogWarning("Using Barracuda stub - models cannot be loaded. Consider using BarracudaWrapper instead.");
            return new Model("StubLoadedFromBytesModel");
        }
    }
    
    public class WorkerFactory
    {
        public enum Type
        {
            CSharp,
            ComputePrecompiled
        }
        
        public static IWorker CreateWorker(Type type, Model model)
        {
            Debug.LogWarning("Using Barracuda stub - workers cannot be created. Consider using BarracudaWrapper instead.");
            return new StubWorker();
        }
        
        private class StubWorker : IWorker
        {
            public string summary => "Stub Worker";
            
            public void Execute(Tensor input) {}
            
            public Tensor PeekOutput()
            {
                return new Tensor(1, 1, 1, 1);
            }
            
            public void Dispose() {}
        }
    }
}
#endif 