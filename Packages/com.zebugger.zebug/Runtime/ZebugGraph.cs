// -------------------------------------------------------------------------------------------------
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace ZebugProject
{
    public class GraphData
    {
        public struct Sample
        {
            public float time;
            public int frame;
            public float value;

            public Sample(float value, float time, int frame)
            {
                this.time = time;
                this.value = value;
                this.frame = frame;
            }

            public Sample(float value)
            {
                this.time = Time.time;
                this.value = value;
                this.frame = Time.frameCount;
            }
        }

        public List<Sample> points = new();

        public int startIdx;
        public int nextIdx;
        public int maxPoints = 200;
        public float minValue = float.MaxValue;
        public float maxValue = float.MinValue;

        public void Add(float value)
        {
            var sample = new Sample(value);
            if (value < minValue)
            {
                minValue = value;
            }
            if (value > maxValue)
            {
                maxValue = value;
            }

            if (points.Count >= maxPoints)
            {
                points[nextIdx++] = sample;
                if (nextIdx >= points.Count)
                {
                    nextIdx = 0;
                }
            }
            else
            {
                points.Add(sample);
            }
        }

        public Sample First()
        {
            int pointCount = points.Count;
            if (pointCount > 0)
            {
                if (nextIdx > 0)
                {
                    return points[nextIdx];
                }
                else
                {
                    return points[0];
                }
            }
            else return new Sample(0);
        }

        public Sample Last()
        {
            int pointCount = points.Count;
            if (pointCount > 0)
            {
                if (nextIdx > 0)
                {
                    int prevIdx = (nextIdx + maxPoints - 1) % maxPoints;
                    return points[prevIdx];
                }
                else
                {
                    return points[pointCount -1];
                }
            }
            else return new Sample(0);
        }
    }

    public partial class Channel<T>
    {
        public static void GraphValue(float value)
        {
            Channel<T> instance = Instance;
            if (!instance.GizmosEnabled())
            {
                return;
            }

            if (!Zebug.s_ChannelGraphData.TryGetValue(instance, out GraphData data))
            {
                data = new GraphData();
                Zebug.s_ChannelGraphData.Add(instance, data);
            }

            // hack
            data.Add(value);
        }

        public void SetGraphValueMinMax(float min, float max)
        {
            if (max < min)
            {
                (min, max) = (max, min);
            }

            if (!Zebug.s_ChannelGraphData.TryGetValue(Instance, out GraphData data))
            {
                data = new GraphData();
                Zebug.s_ChannelGraphData.Add(Instance, data);
            }
            data.minValue = min;
            data.maxValue = max;
        }

    }

}