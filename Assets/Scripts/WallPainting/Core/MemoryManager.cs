using UnityEngine;
using System.Collections.Generic;

namespace Remalux.AR
{
      public class MemoryManager : MonoBehaviour
      {
            private static MemoryManager instance;
            public static MemoryManager Instance
            {
                  get
                  {
                        if (instance == null)
                        {
                              GameObject go = new GameObject("MemoryManager");
                              instance = go.AddComponent<MemoryManager>();
                              DontDestroyOnLoad(go);
                        }
                        return instance;
                  }
            }

            private List<Material> materialInstances = new List<Material>();
            private List<Texture2D> textureInstances = new List<Texture2D>();
            private List<Mesh> meshInstances = new List<Mesh>();

            private void Awake()
            {
                  if (instance != null && instance != this)
                  {
                        Destroy(gameObject);
                        return;
                  }
                  instance = this;
                  DontDestroyOnLoad(gameObject);
            }

            public Material CreateMaterialInstance(Material sourceMaterial, string name = null)
            {
                  if (sourceMaterial == null) return null;

                  Material instance = new Material(sourceMaterial);
                  if (!string.IsNullOrEmpty(name))
                        instance.name = name;
                  materialInstances.Add(instance);
                  return instance;
            }

            public Texture2D CreateTextureInstance(int width, int height, TextureFormat format, bool mipChain)
            {
                  Texture2D instance = new Texture2D(width, height, format, mipChain);
                  textureInstances.Add(instance);
                  return instance;
            }

            public Mesh CreateMeshInstance()
            {
                  Mesh instance = new Mesh();
                  instance.MarkDynamic(); // Оптимизация для часто изменяемых мешей
                  meshInstances.Add(instance);
                  return instance;
            }

            public void ReleaseMaterialInstance(Material material)
            {
                  if (material != null && materialInstances.Contains(material))
                  {
                        materialInstances.Remove(material);
                        Destroy(material);
                  }
            }

            public void ReleaseTextureInstance(Texture2D texture)
            {
                  if (texture != null && textureInstances.Contains(texture))
                  {
                        textureInstances.Remove(texture);
                        Destroy(texture);
                  }
            }

            public void ReleaseMeshInstance(Mesh mesh)
            {
                  if (mesh != null && meshInstances.Contains(mesh))
                  {
                        meshInstances.Remove(mesh);
                        Destroy(mesh);
                  }
            }

            public void ReleaseAllInstances()
            {
                  foreach (var material in materialInstances)
                  {
                        if (material != null)
                              Destroy(material);
                  }
                  materialInstances.Clear();

                  foreach (var texture in textureInstances)
                  {
                        if (texture != null)
                              Destroy(texture);
                  }
                  textureInstances.Clear();

                  foreach (var mesh in meshInstances)
                  {
                        if (mesh != null)
                              Destroy(mesh);
                  }
                  meshInstances.Clear();
            }

            private void OnDestroy()
            {
                  ReleaseAllInstances();
            }

            private void OnApplicationPause(bool pause)
            {
                  if (pause)
                  {
                        // Освобождаем ресурсы при переходе в фоновый режим
                        ReleaseAllInstances();
                  }
            }

            private void OnApplicationQuit()
            {
                  ReleaseAllInstances();
            }
      }
}