//  --- Zebug --------------------------------------------------------------------------------------
//  Copyright (c) 2020 Dan Kyles
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy of this software
//  and associated documentation files (the "Software"), to deal in the Software without
//  restriction, including without limitation the rights to use, copy, modify, merge, publish,
//  distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
//  Software is furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in all copies or
//  substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
//  BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
//  DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  ------------------------------------------------------------------------------------------------

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

using UnityEngine;

namespace UnityTemplateProjects {
    public class SimpleCameraController : MonoBehaviour {
        private class CameraState {
            public float yaw;
            public float pitch;
            public float roll;
            public float x;
            public float y;
            public float z;

            public void SetFromTransform(Transform t) {
                pitch = t.eulerAngles.x;
                yaw = t.eulerAngles.y;
                roll = t.eulerAngles.z;
                x = t.position.x;
                y = t.position.y;
                z = t.position.z;
            }

            public void Translate(Vector3 translation) {
                Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll)*translation;

                x += rotatedTranslation.x;
                y += rotatedTranslation.y;
                z += rotatedTranslation.z;
            }

            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct) {
                yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
                pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
                roll = Mathf.Lerp(roll, target.roll, rotationLerpPct);

                x = Mathf.Lerp(x, target.x, positionLerpPct);
                y = Mathf.Lerp(y, target.y, positionLerpPct);
                z = Mathf.Lerp(z, target.z, positionLerpPct);
            }

            public void UpdateTransform(Transform t) {
                t.eulerAngles = new Vector3(pitch, yaw, roll);
                t.position = new Vector3(x, y, z);
            }
        }

        private CameraState m_TargetCameraState = new CameraState();
        private CameraState m_InterpolatingCameraState = new CameraState();

        [Header("Movement Settings"), Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
        public float boost = 3.5f;

        [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
        public float positionLerpTime = 0.2f;

        [Header("Rotation Settings"), Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
        public AnimationCurve mouseSensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

        [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
        public float rotationLerpTime = 0.01f;

        [Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
        public bool invertY;

        [SerializeField] private InputActionProperty _moveXzAxesProp;
        [SerializeField] private InputActionProperty _moveYAxisProp;
        [SerializeField] private InputActionProperty _boostProp;
        [SerializeField] private InputActionProperty _rotateWhilePressedProp;
        [SerializeField] private InputActionProperty _rotateAxisProp;
        [SerializeField] private InputActionProperty _quitProp;
        [SerializeField] private InputActionProperty _boostMoifierAxisProp;
        
        
        
        private InputAction _moveXzAxes;
        private InputAction _moveYAxis;
        private InputAction _boost;
        private InputAction _rotateWhilePressed;
        private InputAction _rotateAxis;
        private InputAction _quit;
        private InputAction _boostModifierAxis;
        
        
        public static bool EnableAndGetAction(InputActionProperty prop, out InputAction action)
        {
            action = null;
        
            if (prop.action != null)
            {
                prop.action.Enable();
                action = prop.action;
                return true;
            }
            else if (prop.reference != null)
            {
                prop.reference.asset.Enable();
                prop.reference.action.Enable();
                action = prop.reference.action;
                return true;
            }
            return false;
        }
        
        private void OnEnable() {
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);
            
            EnableAndGetAction(_moveXzAxesProp,         out _moveXzAxes);
            EnableAndGetAction(_moveYAxisProp,          out _moveYAxis);
            EnableAndGetAction(_boostProp,              out _boost);
            EnableAndGetAction(_rotateWhilePressedProp, out _rotateWhilePressed);
            EnableAndGetAction(_rotateAxisProp,         out _rotateAxis);
            EnableAndGetAction(_quitProp,               out _quit);
            EnableAndGetAction(_boostMoifierAxisProp,   out _boostModifierAxis);
        }

        private Vector3 GetInputTranslationDirection() {
            Vector3 direction = new Vector3();

            if (_moveXzAxes.IsPressed()){
                var delta = _moveXzAxes.ReadValue<Vector2>();
                direction += new Vector3(delta.x, 0, delta.y);
            }

            if (_moveYAxis.IsPressed())
            {
                direction += Vector3.up * _moveYAxis.ReadValue<float>();
            }

            return direction;
        }

        private void Update() {
            Vector3 translation = Vector3.zero;

            // Exit Sample
            if (_quit.WasPerformedThisFrame()) {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }

            // Hide and lock cursor when right mouse button pressed
            if (_rotateWhilePressed.WasPressedThisFrame()) {
                Cursor.lockState = CursorLockMode.Locked;
            }

            // Unlock and show cursor when right mouse button released
            if (_rotateWhilePressed.WasPressedThisFrame()) {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // Rotation
            if (_rotateWhilePressed.IsPressed()) {
                Vector2 mouseMovement = _rotateAxis.ReadValue<Vector2>();
                
                if (!invertY)
                {
                    mouseMovement.y = -mouseMovement.y;
                }
                
                float mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

                m_TargetCameraState.yaw += mouseMovement.x*mouseSensitivityFactor;
                m_TargetCameraState.pitch += mouseMovement.y*mouseSensitivityFactor;
            }

            // Translation
            translation = GetInputTranslationDirection()*Time.deltaTime;

            // Speed up movement when shift key held
            if (_boost.IsPressed()) {
                translation *= 10.0f;
            }

            // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
            float mouseBoostModifer = _boostModifierAxis.ReadValue<Vector2>().y*0.025f;
            
            if (mouseBoostModifer != 0)
            {
                Debug.Log("Mouse boost modifier: " + mouseBoostModifer);
                boost += mouseBoostModifer;
            }
            
            boost = Mathf.Clamp(boost, 0.25f, 4.0f);
            
            translation *= Mathf.Pow(2.0f, boost);

            m_TargetCameraState.Translate(translation);

            // Framerate-independent interpolation
            // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
            float positionLerpPct = 1f - Mathf.Exp(Mathf.Log(1f - 0.99f)/positionLerpTime*Time.deltaTime);
            float rotationLerpPct = 1f - Mathf.Exp(Mathf.Log(1f - 0.99f)/rotationLerpTime*Time.deltaTime);
            m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct, rotationLerpPct);

            m_InterpolatingCameraState.UpdateTransform(transform);
        }
    }
}