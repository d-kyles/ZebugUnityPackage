// -------------------------------------------------------------------------------------------------
// -------------------------------------------------------------------------------------------------

using System;

using UnityEngine;

namespace ZebugProject
{
    public class ZebugGizmoDrawerChannel : Channel<ZebugGizmoDrawerChannel>
    {
        public ZebugGizmoDrawerChannel() : base("ZebugGizmoDrawer", new Color(1f, 0.13f, 0.38f, 0.81f), Zebug.Instance)
        {
            m_LineDrawingType = ChannelLineData.Type.Runtime;
            m_LineWidthType = WidthType.Adaptive;
            m_LineDrawingWidth = 1.125f;
        }

        public void OverrideLineSettings(ChannelLineData.Type lineDrawingType, WidthType lineWidthType, float lineDrawingWidth)
        {
            m_LineDrawingType = lineDrawingType;
            m_LineWidthType = lineWidthType;
            m_LineDrawingWidth = lineDrawingWidth;

            if (Zebug.s_ChannelLines.TryGetValue(Instance, out ChannelLineData data))
            {
                data.type = m_LineDrawingType;
                data.widthType = m_LineWidthType;
            }
        }
    }

    public class ZebugGizmoDrawer : MonoBehaviour
    {
        [SerializeField] private DrawMethodCallWhen _drawMethodCallWhen = DrawMethodCallWhen.Gizmo;
        [SerializeField] private Type _drawType = Type.Locator;
        [SerializeField] private Color _color = new Color(0.48f, 0.51f, 0.71f);
        [SerializeField] private float _duration = 0;

        [Header("Line Drawing Settings - [STATIC!]")]
        [SerializeField] private ChannelLineData.Type m_LineDrawingType = ChannelLineData.Type.Runtime;
        [SerializeField] private WidthType m_LineWidthType = WidthType.Adaptive;
        [SerializeField] private float m_LineDrawingWidth = 1.125f;

        public enum DrawMethodCallWhen
        {
            None,
            Gizmo,
            GizmoSelected,
            Runtime,
        }

        public enum Type
        {
            LineX,
            LineY,
            LineZ,
            Box,
            Burst,
            Locator,
        }

        private Transform _transform;
        [NonSerialized] private bool _hasTransform = false;

        protected void Awake()
        {
            _transform = transform;
            _hasTransform = true;
        }

        protected void Update()
        {
            if (_drawMethodCallWhen == DrawMethodCallWhen.Runtime)
            {
                DrawGizmo();
            }
        }
        
        protected void OnDrawGizmos()
        {
            if (_drawMethodCallWhen == DrawMethodCallWhen.Gizmo)
            {
                DrawGizmo();
            }
        }

        protected void OnDrawGizmosSelected()
        {
            if (_drawMethodCallWhen == DrawMethodCallWhen.GizmoSelected)
            {
                DrawGizmo();
            }
        }

        private void DrawGizmo()
        {
            if (!_hasTransform)
            {
                _transform = transform;
                _hasTransform = true;
            }

            //  --- Unfortunately this is static right now, so the last in order of update wins
            (ZebugGizmoDrawerChannel.Instance as ZebugGizmoDrawerChannel).OverrideLineSettings(m_LineDrawingType, m_LineWidthType, m_LineDrawingWidth);

            switch (_drawType)
            {
                case Type.LineX:
                {
                    Vector3 vec = new Vector3(_transform.localScale.x, 0, 0);
                    Vector3 pos = _transform.position;
                    ZebugGizmoDrawerChannel.DrawLine(pos, pos + vec, _color, _duration);
                    break;
                }
                case Type.LineY:
                {
                    Vector3 vec = new Vector3(0, _transform.localScale.y,0);
                    Vector3 pos = _transform.position;
                    ZebugGizmoDrawerChannel.DrawLine(pos, pos + vec, _color, _duration);
                    break;
                }

                case Type.LineZ:
                {
                    Vector3 vec = new Vector3(0, 0, _transform.localScale.z);
                    Vector3 pos = _transform.position;
                    ZebugGizmoDrawerChannel.DrawLine(pos, pos + vec, _color, _duration);
                    break;
                }
                case Type.Box:
                {
                    ZebugGizmoDrawerChannel.DrawBox( _transform.position
                        , _transform.rotation
                        , _transform.lossyScale
                        , _color
                        , _duration);
                    break;
                }
                case Type.Burst:
                {
                    float size = _transform.localScale.magnitude;
                    ZebugGizmoDrawerChannel.DrawBurst(_transform.position, size, _color, _duration);
                    break;
                }
                case Type.Locator:
                {
                    float size = _transform.localScale.magnitude;
                    ZebugGizmoDrawerChannel.DrawLocator(_transform.position, size, _transform.rotation, _duration);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }

}