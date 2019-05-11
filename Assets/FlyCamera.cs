using UnityEngine;

namespace Assets
{
    public class FlyCamera : MonoBehaviour
    {
        public float MouseSensitivity = 2.0f;
        public float Speed = 10.0f;
        public float ShiftMultiplier = 25.0f;
        public float MaxShift = 100.0f;

        private float _totalRun = 1.0f;
        private float _rotationY;

        private const float MaximumY = 90.0f;    // Not recommended to change
        private const float MinimumY = -90.0f; // these parameters.

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            var rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * MouseSensitivity;
            _rotationY += Input.GetAxis("Mouse Y") * MouseSensitivity;
            _rotationY = Mathf.Clamp(_rotationY, MinimumY, MaximumY);
            transform.localEulerAngles = new Vector3(-_rotationY, rotationX, 0.0f);
            var translation = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
            translation = Input.GetKey(KeyCode.LeftShift) ? Sprint(translation) : Walk(translation);
            translation = translation * Time.deltaTime;
            transform.Translate(translation);
        }

        private Vector3 Sprint(Vector3 translation)
        {
            _totalRun += Time.deltaTime;
            translation = translation * _totalRun * ShiftMultiplier;
            translation.x = Mathf.Clamp(translation.x, -MaxShift, MaxShift);
            translation.y = Mathf.Clamp(translation.y, -MaxShift, MaxShift);
            translation.z = Mathf.Clamp(translation.z, -MaxShift, MaxShift);
            return translation;
        }

        private Vector3 Walk(Vector3 translation)
        {
            _totalRun = Mathf.Clamp(_totalRun * 0.5f, 1.0f, 1000.0f);
            translation = translation * Speed;
            return translation;
        }
    }
}
