using UnityEngine;

namespace Assets
{
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    public class RaymarchCamera : SceneViewFilter
    {
        [SerializeField]
#pragma warning disable 649 // Set in Inspector
        private Shader _shader; 
#pragma warning restore 649
        private Material _raymarchMat;
        private Camera _camera;

        public Material RaymarchMaterial
        {
            get
            {
                return (_raymarchMat || !_shader) ? 
                    _raymarchMat : _raymarchMat = new Material(_shader) { hideFlags = HideFlags.HideAndDontSave };
            }
        }

        public Camera Camera
        {
            get
            {
                return _camera ? 
                    _camera : _camera = GetComponent<Camera>();
            }
        }
    
        [Header("Setup")]
        public float MaxDistance = 1000f;
        [Range(1, 300)]
        public int MaxIterations = 300;
        [Range(0.1f, 0.001f)]
        public float Accuracy = .05f;

        [Header("Directional Light")]
        public Transform DirectionalLight;
        public Color LightColor;
        public float LightIntensity = 0.75f;

        [Header("Shadow")]
        [Range(0, 4)]
        public float ShadowIntensity = 1.19f;
        public Vector2 ShadowDistance = new Vector2(0.1f, 100f);
        [Range(1, 128)]
        public float ShadowPenumbra = 5.9f;

        [Header("Ambient Occlusion")]
        [Range(0.01f, 10.0f)]
        public float AoStepsize = 2.43f;
        [Range(1, 5)]
        public int AoIterations = 2;
        [Range(0, 1)]
        public float AoIntensity = 0.275f;

        [Header("Signed Distance Field")]
        public Color MainColor;
        public Vector4 Sphere1 = new Vector4(0f, 0f, 0f, 4.15f);
        public Vector4 Box = new Vector4(0f, 0f, 0f, 3.1f);
        public float BoxRound = 0.19f;
        public float BoxSphereSmooth = 0.51f;
        public Vector4 Sphere2 = new Vector4(0f, 0f, 0f, 2.29f);
        public float SphereIntersectSmooth = -1f;

        private void OnRenderImage(Texture source, RenderTexture destination)
        {
            if (!RaymarchMaterial)
            {
                Graphics.Blit(source, destination);
                return;
            }

            SetMaterialVariables(source, destination);
            DrawFrustum();
        }

        private void SetMaterialVariables(Texture source, RenderTexture destination)
        {
            SetMaterialMatrices();
            SetMaterialIntegers();
            SetMaterialFloats();
            SetMaterialVectors();
            SetMaterialColors();
            RaymarchMaterial.SetTexture("_MainTex", source);
            RenderTexture.active = destination;
        }

        private void SetMaterialIntegers()
        {
            RaymarchMaterial.SetInt("_AoIterations", AoIterations);
            RaymarchMaterial.SetInt("_MaxIterations", MaxIterations);
        }

        private void SetMaterialColors()
        {
            RaymarchMaterial.SetColor("_LightCol", LightColor);
            RaymarchMaterial.SetColor("_mainColor", MainColor);
        }

        private void SetMaterialVectors()
        {
            RaymarchMaterial.SetVector("_LightDir", DirectionalLight ? DirectionalLight.forward : Vector3.down);
            RaymarchMaterial.SetVector("_sphere1", Sphere1);
            RaymarchMaterial.SetVector("_box1", Box);
            RaymarchMaterial.SetVector("_sphere2", Sphere2);
            RaymarchMaterial.SetVector("_ShadowDistance", ShadowDistance);
        }

        private void SetMaterialMatrices()
        {
            RaymarchMaterial.SetMatrix("_CamFrustum", CameraFrustum(Camera));
            RaymarchMaterial.SetMatrix("_CamToWorld", Camera.cameraToWorldMatrix);
        }

        private void SetMaterialFloats()
        {
            RaymarchMaterial.SetFloat("_LightIntensity", LightIntensity);
            RaymarchMaterial.SetFloat("_ShadowIntensity", ShadowIntensity);
            RaymarchMaterial.SetFloat("_ShadowPenumbra", ShadowPenumbra);
            RaymarchMaterial.SetFloat("_maxDistance", MaxDistance);
            RaymarchMaterial.SetFloat("_Accuracy", Accuracy);
            RaymarchMaterial.SetFloat("_boxround", BoxRound);
            RaymarchMaterial.SetFloat("_boxSphereSmooth", BoxSphereSmooth);
            RaymarchMaterial.SetFloat("_sphereIntersectSmooth", SphereIntersectSmooth);
            RaymarchMaterial.SetFloat("_AoStepsize", AoStepsize);
            RaymarchMaterial.SetFloat("_AoIntensity", AoIntensity);

        }

        private void DrawFrustum()
        {
            RaymarchMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(GL.QUADS);
            GL.MultiTexCoord2(0, 0.0f, 1.0f); // topLeft
            GL.Vertex3(0.0f, 1.0f, 0.0f);
            GL.MultiTexCoord2(0, 1.0f, 1.0f); // topRight
            GL.Vertex3(1.0f, 1.0f, 1.0f);
            GL.MultiTexCoord2(0, 1.0f, 0.0f); // bottomRight
            GL.Vertex3(1.0f, 0.0f, 2.0f);
            GL.MultiTexCoord2(0, 0.0f, 0.0f); // bottomLeft
            GL.Vertex3(0.0f, 0.0f, 3.0f);
            GL.End();
            GL.PopMatrix();
        }

        private static Matrix4x4 CameraFrustum(Camera cam)
        {
            var frustum = Matrix4x4.identity;
            var fov = Mathf.Tan((cam.fieldOfView * 0.5f) * Mathf.Deg2Rad);

            var up = Vector3.up * fov;
            var right = Vector3.right * fov * cam.aspect;

            var topLeft = (-Vector3.forward - right + up);
            var topRight = (-Vector3.forward + right + up);
            var bottomRight = (-Vector3.forward + right - up);
            var bottomLeft = (-Vector3.forward - right - up);

            frustum.SetRow(0, topLeft);
            frustum.SetRow(1, topRight);
            frustum.SetRow(2, bottomRight);
            frustum.SetRow(3, bottomLeft);

            return frustum;
        }
    }
}
