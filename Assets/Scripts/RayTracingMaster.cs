using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader rayTracingShader;
    public Texture skybox;

    private RenderTexture _targetTexture;
    private Camera _camera;
    private uint _currentSample = 0;
    private Material _addMaterial;

    private List<Sphere> _spheres;
    private ComputeBuffer _sphereBuffer;

    struct Sphere // 40
    {
        public Vector3 center; // 12
        public float radius; // 4
        public Vector3 diffuse; // 12
        public Vector3 specular; // 12
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _spheres = new List<Sphere>();
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
    
        SetupScene();
    }

    private void OnDisable() 
    {
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
    }

    private void SetupScene()
    {
        _spheres.Clear();

        for (int i = 0; i < 30; i++)
        {
            var sphere = new Sphere();
            sphere.radius = Random.Range(0.3f, 1.5f);
            sphere.center = new Vector3(Random.Range(-20, 20), 3.0f, Random.Range(-20, 20));

            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            sphere.diffuse = metal ? Vector4.zero : new Vector4(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector4(color.r, color.g, color.b) : new Vector4(0.04f, 0.04f, 0.04f);
            _spheres.Add(sphere);

            // TODO : check for collision

        }

        if (_sphereBuffer != null)
            _sphereBuffer.Release();

        if (_spheres.Count > 0)
        {
            _sphereBuffer = new ComputeBuffer(_spheres.Count, 40);
            _sphereBuffer.SetData(_spheres);
        }
        else
        {
            Debug.LogWarning("No Spheres");
        }
    }

    private void SetShaderUniforms()
    {
        rayTracingShader.SetMatrix("uCameraToWorld", _camera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("uCameraInverseProjection", _camera.projectionMatrix.inverse);
        rayTracingShader.SetTexture(0, "uSkybox", skybox);
        rayTracingShader.SetVector("uPixelOffset", new Vector2(Random.value, Random.value));
        rayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderUniforms();
        Render(destination);
    }

    private void Render(RenderTexture destination)
    {
        InitializeRenderTexture();

        rayTracingShader.SetTexture(0, "Result", _targetTexture);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        // Execute the compute shader
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Display the _targetTexture to the screen
        _addMaterial.SetFloat("uSample", _currentSample);
        Graphics.Blit(_targetTexture, destination, _addMaterial);
        _currentSample++;
    }

    private void InitializeRenderTexture()
    {
        if (_targetTexture == null || _targetTexture.width != Screen.width || _targetTexture.height != Screen.height)
        {
            _currentSample = 0;
            
            if (_targetTexture != null)
                _targetTexture.Release(); // Release render texture if we already have one

            _targetTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            _targetTexture.enableRandomWrite = true;
            _targetTexture.Create();
        }
    }


}
