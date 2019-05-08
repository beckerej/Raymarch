using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            if (_raymarchMat || !_shader)
                return _raymarchMat;
            _raymarchMat = new Material(_shader) { hideFlags = HideFlags.HideAndDontSave };
            return _raymarchMat;
        }
    }

    public Camera Camera
    {
        get
        {
            return _camera ? _camera : _camera = GetComponent<Camera>();
        }
    }
    
    [Header("Setup")]
    public float MaxDistance;
    [Range(1, 300)]
    public int MaxIterations;
    [Range(0.1f, 0.001f)]
    public float Accuracy;

    [Header("Directional Light")]
    public Transform DirectionalLight;
    public Color LightCol;
    public float LightIntensity;

    [Header("Shadow")]
    [Range(0, 4)]
    public float ShadowIntensity;
    public Vector2 ShadowDistance;
    [Range(1, 128)]
    public float ShadowPenumbra;

    [Header("Ambient Occlusion")]
    [Range(0.01f, 10.0f)]
    public float AoStepsize;
    [Range(1, 5)]
    public int AoIterations;
    [Range(0, 1)]
    public float AoIntensity;

    [Header("Signed Distance Field")]
    public Color MainColor;
    public Vector4 Sphere1;
    public Vector4 Box;
    public float BoxRound;
    public float BoxSphereSmooth;
    public Vector4 Sphere2;
    public float SphereIntersectSmooth;
    public Vector3 ModInterval;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!RaymarchMaterial)
        {
            Graphics.Blit(source, destination);
            return;
        }

        RaymarchMaterial.SetVector("_LightDir", DirectionalLight ? DirectionalLight.forward : Vector3.down);
        RaymarchMaterial.SetColor("_LightCol", LightCol);
        RaymarchMaterial.SetFloat("_LightIntensity", LightIntensity);
        RaymarchMaterial.SetFloat("_ShadowIntensity", ShadowIntensity);
        RaymarchMaterial.SetFloat("_ShadowPenumbra", ShadowPenumbra);
        RaymarchMaterial.SetVector("_ShadowDistance", ShadowDistance);
        RaymarchMaterial.SetMatrix("_CamFrustum", CamFrustum(Camera));
        RaymarchMaterial.SetMatrix("_CamToWorld", Camera.cameraToWorldMatrix);
        RaymarchMaterial.SetFloat("_maxDistance", MaxDistance);
        RaymarchMaterial.SetFloat("_Accuracy", Accuracy);
        RaymarchMaterial.SetInt("_MaxIterations", MaxIterations);
        RaymarchMaterial.SetFloat("_boxround", BoxRound);
        RaymarchMaterial.SetFloat("_boxSphereSmooth", BoxSphereSmooth);
        RaymarchMaterial.SetFloat("_sphereIntersectSmooth", SphereIntersectSmooth);
        RaymarchMaterial.SetVector("_sphere1", Sphere1);
        RaymarchMaterial.SetVector("_sphere2", Sphere2);
        RaymarchMaterial.SetVector("_box1", Box);
        RaymarchMaterial.SetColor("_mainColor", MainColor);
        RaymarchMaterial.SetVector("_modInterval", ModInterval);

        RaymarchMaterial.SetFloat("_AoStepsize", AoStepsize);
        RaymarchMaterial.SetFloat("_AoIntensity", AoIntensity);
        RaymarchMaterial.SetInt("_AoIterations", AoIterations);

        RenderTexture.active = destination;
        RaymarchMaterial.SetTexture("_MainTex", source);

        GL.PushMatrix();
        GL.LoadOrtho();
        RaymarchMaterial.SetPass(0);
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

    private Matrix4x4 CamFrustum(Camera cam)
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
