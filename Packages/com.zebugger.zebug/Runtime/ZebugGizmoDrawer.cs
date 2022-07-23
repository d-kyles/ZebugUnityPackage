// -------------------------------------------------------------------------------------------------
// -------------------------------------------------------------------------------------------------

using System;
using UnityEditor;
using UnityEngine;

namespace ZebugProject
{

    public class ZebugGizmoDrawer : MonoBehaviour
    {

        [SerializeField] private DrawWhen _drawWhen = DrawWhen.Gizmo;
        [SerializeField] private Type _drawType = Type.Locator;
        [SerializeField] private Color _color = new Color(0.48f, 0.51f, 0.71f);
        [SerializeField] private float _duration = 0;

        public enum DrawWhen
        {
            None,
            Gizmo,
            GizmoSelected,
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

        protected void Awake()
        {
            _transform = transform;
        }

        protected void OnDrawGizmos()
        {
            if (_drawWhen == DrawWhen.Gizmo)
            {
                DrawGizmo();
            }
        }

        protected void OnDrawGizmosSelected()
        {
            if (_drawWhen == DrawWhen.GizmoSelected)
            {
                DrawGizmo();
            }
        }

        private void DrawGizmo()
        {
            switch (_drawType)
            {
                case Type.LineX:
                {
                    Vector3 vec = new Vector3(_transform.localScale.x, 0, 0);
                    Vector3 pos = _transform.position;
                    Zebug.DrawLine(pos, pos + vec, _color, _duration);
                    break;
                }
                case Type.LineY:
                {
                    Vector3 vec = new Vector3(0, _transform.localScale.y,0);
                    Vector3 pos = _transform.position;
                    Zebug.DrawLine(pos, pos + vec, _color, _duration);
                    break;
                }

                case Type.LineZ:
                {
                    Vector3 vec = new Vector3(0, 0, _transform.localScale.z);
                    Vector3 pos = _transform.position;
                    Zebug.DrawLine(pos, pos + vec, _color, _duration);
                    break;
                }
                case Type.Box:
                {
                    Zebug.DrawBox( _transform.position
                        , _transform.rotation
                        , _transform.lossyScale
                        , _color
                        , _duration);
                    break;
                }
                case Type.Burst:
                {
                    float size = _transform.localScale.magnitude;
                    Zebug.DrawBurst(_transform.position, size, _color, _duration);
                    break;
                }
                case Type.Locator:
                {
                    float size = _transform.localScale.magnitude;
                    Zebug.DrawLocator(_transform.position, size, _transform.rotation, _duration);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }

}