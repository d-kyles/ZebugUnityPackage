// -------------------------------------------------------------------------------------------------
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace ZebugProject
{
    using static ZebugUtils;
    
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

        public readonly List<Sample> points = new();
        public readonly List<(float, Color, bool dotted)> gridLines = new();
        public readonly Dictionary<string, GraphData> subGraphs = new();
        
        public Color lineColor = new (0,0,0,0);
        
        public int startIdx;
        public int nextIdx;
        public int maxPoints = 200;
        public float minValue = float.MaxValue;
        public float maxValue = float.MinValue;
        private float _breakValue;
        public float breakValue => _breakValue;
        private bool _hasBreakValue;
        public bool hasBreakValue => _hasBreakValue;

        public void Add(float value)
        {
            if (_hasBreakValue) {
                if (value > _breakValue)
                {
                    #if DEBUG
                    if(System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Diagnostics.Debugger.Break();
                        _hasBreakValue = false;
                    }
                    #endif
                }
            }
            
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

        public void SmoothUpdateMinValue(float freshMinValue)
        {
            minValue = Damp(minValue, freshMinValue, 0.1f, Time.deltaTime);

            var delta = Mathf.Abs(minValue - freshMinValue);
            if (freshMinValue > 0.001f || delta < 0.001f)
            {
                var ratioLeft = delta / freshMinValue;
                if (ratioLeft < 0.01f)
                {
                    //  --- Almost there. Snap to the value.
                    minValue = freshMinValue;
                }
            }
        }
        
        public void SmoothUpdateMaxValue(float freshMaxValue)
        {
            maxValue = Damp(maxValue, freshMaxValue, 0.1f, Time.deltaTime);

            var delta = Mathf.Abs(maxValue - freshMaxValue);
            if (freshMaxValue > 0.001f || delta < 0.001f)
            {
                var ratioLeft = delta / freshMaxValue;
                if (ratioLeft < 0.01f)
                {
                    //  --- Almost there. Snap to the value.
                    maxValue = freshMaxValue;
                }
            }
        }
        
        public void AddBreakpoint(float atValue)
        {
            _breakValue = atValue;
            _hasBreakValue = true;
        }
    }
    
    
    public partial class Channel<T>
    {
        public static void SetSubgraphLine(string name, Color color)
        {
            SubGraphData(name).lineColor = color;
        }
        
        public static void GraphValue(string subGraphName, float value)
        {
            Channel<T> instance = Instance;
            if (!instance.GizmosEnabled())
            {
                return;
            }
            
            var data = SubGraphData(subGraphName);
            data.Add(value);
        }
        
        public static void GraphValue(float value)
        {
            Channel<T> instance = Instance;
            if (!instance.GizmosEnabled())
            {
                return;
            }
            
            var data = ChannelGraphData();
            data.Add(value);
        }
        
        public void SetGraphValueMinMax(float min, float max)
        {
            var data = ChannelGraphData();
            
            if (max < min)
            {
                (min, max) = (max, min);
            }

            data.minValue = min;
            data.maxValue = max;
        }
        
        public void SetGraphGridLine(float value, Color color, bool dotted = false)
        {
            ChannelGraphData().gridLines.Add((value, color, dotted));
        }
        
        private static GraphData SubGraphData(string subGraphName)
        {
            if (!Zebug.s_ChannelGraphData.TryGetValue(Instance, out GraphData data))
            {
                data = new GraphData();
                Zebug.s_ChannelGraphData.Add(Instance, data);
            }
            
            if (!data.subGraphs.TryGetValue(subGraphName, out GraphData subGraph))
            {
                subGraph = new GraphData();
                data.subGraphs.Add(subGraphName, subGraph);
            }
            
            return subGraph;
        }
        
        private static GraphData ChannelGraphData()
        {
            if (!Zebug.s_ChannelGraphData.TryGetValue(Instance, out GraphData data))
            {
                data = new GraphData();
                Zebug.s_ChannelGraphData.Add(Instance, data);
            }
            return data;
        }
    }

}