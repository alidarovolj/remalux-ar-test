using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Remalux.WallPainting.Vision
{
      public static class UnityMainThread
      {
            private static readonly Queue<Action> _executionQueue = new Queue<Action>();
            private static readonly object _lock = new object();

            public static void Execute(Action action)
            {
                  if (action == null)
                        return;

                  lock (_lock)
                  {
                        _executionQueue.Enqueue(action);
                  }
            }

            public static void Update()
            {
                  lock (_lock)
                  {
                        while (_executionQueue.Count > 0)
                        {
                              _executionQueue.Dequeue().Invoke();
                        }
                  }
            }
      }
}