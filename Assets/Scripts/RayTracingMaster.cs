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

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
    }

    private void SetShaderUniforms()
    {
        rayTracingShader.SetMatrix("uCameraToWorld", _camera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("uCameraInverseProjection", _camera.projectionMatrix.inverse);
        rayTracingShader.SetTexture(0, "uSkybox", skybox);
        rayTracingShader.SetVector("uPixelOffset", new Vector2(Random.value, Random.value));
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
