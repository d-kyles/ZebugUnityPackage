// -------------------------------------------------------------------------------------------------
// -------------------------------------------------------------------------------------------------

using System;
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
        
        private int offsetIdx;
        private bool _looping;
        
        public int startIdxOffset
        {
            get { return offsetIdx; }
        }
        
        public int endIdxOffset
        {
            get { return (offsetIdx + _maxPoints -1) % _maxPoints; }
        }

        private int _maxPoints = 400;
        public int maxPoints
        {
            get => _maxPoints;
            set {
                if (value is < 2 or > 10000)
                {
                    return;
                }
                
                //  --- For now, just start fresh with a change.
                points.Clear();
                points.Capacity = Math.Max(points.Capacity, value);
                _maxPoints = value;
                _looping = false;
            }
        }
        public float minValue = float.MaxValue;
        public float maxValue = float.MinValue;
        public float minTime = float.MaxValue;
        public float maxTime = float.MinValue;
        
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
            
            minValue = (value < minValue) ? value : minValue;
            maxValue = (value > maxValue) ? value : maxValue;
            
            float time = sample.time;
            minTime = (time < minTime) ? time : minTime;
            maxTime = (time > maxTime) ? time : maxTime;

            _looping = (points.Count >= _maxPoints);
            
            if (_looping)
            {
                points[offsetIdx] = sample;
                offsetIdx = (offsetIdx + 1) % _maxPoints;
            }
            else
            {
                points.Add(sample);
            }
        }

        public Sample First()
        {
            return (_looping)
                       ? points[offsetIdx] 
                       : points.Count > 0 ? points[0] : new Sample(0);
        }

        public Sample Last()
        {
            return (_looping) 
                       ? points[endIdxOffset]
                       : points.Count > 0 ? points[^1] : new Sample(0);
        }
        
        private BoundRect _cachedBounds;
        private float _lastBoundCalcTime;
        
        public struct BoundRect
        {
            public float xMin, yMin, xMax, yMax;
            private bool _empty;
            
            public static BoundRect empty => new () { _empty = true };


            public void Encapsulate(BoundRect other)
            {
                if (other._empty) { return; }
                
                if (_empty)
                {
                    xMin = other.xMin;
                    yMin = other.yMin;
                    xMax = other.xMax;
                    yMax = other.yMax;
                }
                else
                {
                    xMin = xMin < other.xMin ? xMin : other.xMin;
                    yMin = yMin < other.yMin ? yMin : other.yMin;
                    xMax = xMax > other.xMax ? xMax : other.xMax;
                    yMax = yMax > other.yMax ? yMax : other.yMax;
                }
                _empty = false;
            }

            public bool IsEmpty() => _empty;
        }
        
        private static BoundRect CalculateDataBounds(GraphData data)
        {
            if (data.points.Count == 0){ return BoundRect.empty; }
            
            List<Sample> points = data.points;
            int pointCount = points.Count;
            
            float freshMinValue = float.MaxValue;
            float freshMaxValue = float.MinValue;
            
            float freshMinTime = float.MaxValue;
            float freshMaxTime = float.MinValue;
            
            int startIdxOffset = data.startIdxOffset;
            
            for (int i = 0; i < pointCount; i++)
            {
                int idx = (startIdxOffset + i) % pointCount;
                Sample sample = points[idx];
                
                float value = sample.value;
                float time = sample.time;

                //  --- Math min/max don't get optimized to intrinsics when in-editor
                freshMinValue = (value < freshMinValue) ? value : freshMinValue;
                freshMaxValue = (value > freshMaxValue) ? value : freshMaxValue;
                
                freshMinTime = (time < freshMinTime) ? time : freshMinTime;
                freshMaxTime = (time > freshMaxTime) ? time : freshMaxTime;
            }
            
            SmoothUpdateValue(freshMinValue, ref data.minValue);
            SmoothUpdateValue(freshMaxValue, ref data.maxValue);
            SmoothUpdateValue(freshMinTime, ref data.minTime);
            SmoothUpdateValue(freshMaxTime, ref data.maxTime);

            return new BoundRect
            {
                xMin = data.minTime,
                xMax = data.maxTime,
                yMin = data.minValue,
                yMax = data.maxValue,
            };
        } 
        
        public BoundRect CalculateGraphBounds()
        {
            float dt = Time.deltaTime;
            dt = Mathf.Clamp(dt, 1/60f, Time.maximumDeltaTime);
            
            if (Time.realtimeSinceStartup < (_lastBoundCalcTime + dt)
                && Time.realtimeSinceStartup > _lastBoundCalcTime)
            {
                return _cachedBounds;
            }
            _lastBoundCalcTime = Time.realtimeSinceStartup;
            
            var bounds = BoundRect.empty; 
            
            bounds.Encapsulate(CalculateDataBounds(this));
            
            foreach ((string _, GraphData subData) in subGraphs)
            {
                bounds.Encapsulate(CalculateDataBounds(subData));
            }
            
            _cachedBounds = bounds;
            return bounds;
        }
        
        public static void SmoothUpdateValue(float target, ref float smoothValue)
        {
            float dt = Time.unscaledDeltaTime;
            if (dt == 0f) dt = 1/60f;
            
            smoothValue = Damp(smoothValue, target, 0.1f, dt);

            float delta = Mathf.Abs(smoothValue - target);
            if (Mathf.Abs(target) > 0.001f || delta < 0.001f)
            {
                var ratioLeft = delta / target;
                if (ratioLeft < 0.01f)
                {
                    //  --- Almost there. Snap to the value.
                    smoothValue = target;
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
        
        ///
        /// <p>
        /// SetGraphGridLine
        /// </p>
        /// 
        /// Call in Awake or Start, not in update.
        /// 
        public static void SetGraphGridLine(float value, Color color, bool dotted = false)
        {
            var lines = ChannelGraphData().gridLines;
            for (var i = lines.Count - 1; i >= 0; i--)
            {
                if (Mathf.Approximately(lines[i].Item1, value))
                {
                    lines[i] = (value, color, dotted);
                    return;
                }    
            }
            
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