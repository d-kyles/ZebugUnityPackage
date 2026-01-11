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

using Unity.Mathematics;
using UnityEngine.InputSystem;

using UnityEngine;
using UnityEngine.Serialization;

namespace UnityTemplateProjects {
    public class SimpleCameraController : MonoBehaviour {
        private class CameraState {
            private float _yaw;
            private float _pitch;
            private float _roll;
            private float3 _pos;

            public void SetFromTransform(Transform t) {
                _pitch = t.eulerAngles.x;
                _yaw = t.eulerAngles.y;
                _roll = t.eulerAngles.z;
                _pos = t.position;
            }

            public void Translate(Vector3 translation) {
                Vector3 rotatedTranslation = Quaternion.Euler(_pitch, _yaw, _roll)*translation;
                _pos += (float3)rotatedTranslation;
            }
            
            public void Rotate(float yawDelta, float pitchDelta)
            {
                _yaw += yawDelta;
                _pitch += pitchDelta;
            }

            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct) {
                _yaw = Mathf.Lerp(_yaw, target._yaw, rotationLerpPct);
                _pitch = Mathf.Lerp(_pitch, target._pitch, rotationLerpPct);
                _roll = Mathf.Lerp(_roll, target._roll, rotationLerpPct);

                _pos = math.lerp(_pos, target._pos, positionLerpPct);
            }

            public void UpdateTransform(Transform t) {
                t.eulerAngles = new Vector3(_pitch, _yaw, _roll);
                t.position = _pos;
            }
        }

        private CameraState m_TargetCameraState = new();
        private CameraState m_InterpolatingCameraState = new();

        [Header("Movement Settings"), Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
        [SerializeField] private float _baseSpeed = 2f;

        [Tooltip("How much faster the camera moves during boostAction")]
        [SerializeField] private float _shiftBoostAmount = 5f;
        
        [Tooltip("Move vertically at a different rate than horizontally")]
        [SerializeField] private float _verticalSpeedRatio = 0.5f;

        [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
        [SerializeField] 
        private float _positionLerpTime = 0.2f;

        [Header("Rotation Settings"), Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
        [SerializeField] private AnimationCurve _mouseSensitivityCurve = new(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

        [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
        [SerializeField] private float _rotationLerpTime = 0.01f;

        [Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
        [SerializeField] private bool _invertY;
        
        [SerializeField] private InputActionProperty _moveXzAxesProp;
        [SerializeField] private InputActionProperty _moveYAxisProp;
        [SerializeField] private InputActionProperty _boostProp;
        [SerializeField] private InputActionProperty _rotateWhilePressedProp;
        [SerializeField] private InputActionProperty _rotateAxisProp;
        [SerializeField] private InputActionProperty _quitProp;
        [SerializeField] private InputActionProperty _boostModifierAxisProp;
        
        
        private InputAction _moveXzAxes;
        private InputAction _moveYAxis;
        private InputAction _boostAction;
        private InputAction _rotateWhilePressed;
        private InputAction _rotateAxis;
        private InputAction _quit;
        private InputAction _boostModifierAxis;
        
        private static void EnableAndGetAction(InputActionProperty prop, out InputAction action)
        {
            action = null;
        
            if (prop.action != null)
            {
                prop.action.Enable();
                action = prop.action;
            }
            else if (prop.reference != null)
            {
                prop.reference.asset.Enable();
                prop.reference.action.Enable();
                action = prop.reference.action;
            }
        }
        
        private void OnEnable() {
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);
            
            EnableAndGetAction(_moveXzAxesProp,         out _moveXzAxes);
            EnableAndGetAction(_moveYAxisProp,          out _moveYAxis);
            EnableAndGetAction(_boostProp,              out _boostAction);
            EnableAndGetAction(_rotateWhilePressedProp, out _rotateWhilePressed);
            EnableAndGetAction(_rotateAxisProp,         out _rotateAxis);
            EnableAndGetAction(_quitProp,               out _quit);
            EnableAndGetAction(_boostModifierAxisProp,   out _boostModifierAxis);
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
            
            float dt = Time.deltaTime;
            
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
                
                if (!_invertY)
                {
                    mouseMovement.y = -mouseMovement.y;
                }
                
                float mouseSensitivityFactor = _mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

                m_TargetCameraState.Rotate(yawDelta: mouseMovement.x*mouseSensitivityFactor,
                                           pitchDelta: mouseMovement.y*mouseSensitivityFactor); 
            }

            // Translation
            Vector3 translation = GetInputTranslationDirection()*dt;

            // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
            float mouseBoostModifer = _boostModifierAxis.ReadValue<Vector2>().y*0.025f;
            if (mouseBoostModifer != 0)
            {
                _baseSpeed += mouseBoostModifer;
            }
            _baseSpeed = Mathf.Clamp(_baseSpeed, _baseSpeed*0.25f, _baseSpeed*4.0f);
            
            translation *= Mathf.Pow(2.0f, _baseSpeed);

            // Speed-up movement when shift key is held
            if (_boostAction.IsPressed()) {
                translation *= _shiftBoostAmount;
            }
            
            //  --- Generally don't want to move up and down at the same speed as horizontal movement
            translation.y *= _verticalSpeedRatio;
            
            m_TargetCameraState.Translate(translation);

            // Framerate-independent interpolation
            // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
            float positionLerpPct = 1f - Mathf.Exp(Mathf.Log(1f - 0.99f)/_positionLerpTime*dt);
            float rotationLerpPct = 1f - Mathf.Exp(Mathf.Log(1f - 0.99f)/_rotationLerpTime*dt);
            m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct, rotationLerpPct);

            m_InterpolatingCameraState.UpdateTransform(transform);
        }
    }
}