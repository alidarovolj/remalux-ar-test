using UnityEngine;

namespace Remalux.AR
{
    public struct Line
    {
        public Vector2 start;
        public Vector2 end;
        public float confidence;

        public Vector2 center => (start + end) * 0.5f;
        public Vector2 direction => (end - start).normalized;
        public float length => Vector2.Distance(start, end);

        public Line(Vector2 start, Vector2 end, float confidence = 1.0f)
        {
            this.start = start;
            this.end = end;
            this.confidence = confidence;
        }
    }
} 