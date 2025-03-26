using System;
using UnityEngine;

public class JumpFlood : MonoBehaviour
{
    [SerializeField] private ComputeShader _generatorShader;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Texture2D _seedTexture;
    [SerializeField] private FilterMode _textureFilterMode;
    [SerializeField] private TextureWrapMode _textureWrapMode;
    [Range(0, 10)]
    [SerializeField] private int _steps = 4;
    [SerializeField] private bool _showOutline;
    [Range(0, 1)]
    [SerializeField] private float _outlineRadius = 1;
    [Header("Debug")]
    [SerializeField] private bool _showSteps;
    [Range(0, 10)]
    [SerializeField] private int _stopAfter = 1;
    
    private int _setupKernelIndex;
    private int _jfaKernelIndex;
    private int _outlineKernelIndex;
    private Vector3Int _threadGroupSizes;
    private RenderTexture _rtA;
    private RenderTexture _rtB;
    private ComputeBuffer _buffer;
    
    private void Start()
    {
        Setup();
        Generate();
    }

    private void OnValidate()
    {
        Generate();
    }

    private void Setup()
    {
        //get some info about the compute shader so we don't have to mirror it here (like the thread group size)
        _setupKernelIndex = _generatorShader.FindKernel("Setup");
        _jfaKernelIndex = _generatorShader.FindKernel("JFA");
        _outlineKernelIndex = _generatorShader.FindKernel("Outline");
        
        _generatorShader.GetKernelThreadGroupSizes(_jfaKernelIndex, out var sizeX, out var sizeY, out var sizeZ);
        _threadGroupSizes = new()
        {
            x = (int)sizeX,
            y = (int)sizeY,
            z = (int)sizeZ,
        };

        //Create a RenderTexture for our shader to write to. For writing into textures, it NEEDS to be a RenderTexture with "enableRandomWrite" enabled
        _rtA = new RenderTexture(_seedTexture.width, _seedTexture.height, 0, RenderTextureFormat.ARGB32)
        {
            name = "rtA",
            enableRandomWrite = true,
            filterMode = _textureFilterMode,
            wrapMode = _textureWrapMode,
        };
        _rtA.Create();
        
        _rtB = new RenderTexture(_seedTexture.width, _seedTexture.height, 0, RenderTextureFormat.ARGB32)
        {
            name = "rtB",
            enableRandomWrite = true,
            filterMode = _textureFilterMode,
            wrapMode = _textureWrapMode
        };
        _rtB.Create();
    }

    private void Generate()
    {
        if(!Application.isPlaying)
            return;
        
        if(_rtA == null || _seedTexture == null)
            return;
        
        //Non-texture properties we can set globally for all kernels
        _generatorShader.SetInt("_TextureSizeX", _seedTexture.width);
        _generatorShader.SetInt("_TextureSizeY", _seedTexture.height);
        _generatorShader.SetInt("_TextureSizeY", _seedTexture.height);
        
        //We need to set the textures for each kernel
        _generatorShader.SetTexture(_setupKernelIndex, "_SeedTexture", _seedTexture);
        _generatorShader.SetTexture(_setupKernelIndex, "_Source", _rtA);
        
        _generatorShader.SetTexture(_jfaKernelIndex, "_SeedTexture", _seedTexture);
        
        _generatorShader.SetTexture(_outlineKernelIndex, "_SeedTexture", _seedTexture);

        //First, lets setup the texture
        Dispatch(_setupKernelIndex);
        
        if (_steps == 0)
        {
            _renderer.material.mainTexture = _rtA;
            return;
        }

        //We need to do dispatch until we get to size 1, halving the size on each dispatch
        int currentStep = (int)Mathf.Pow(2, _steps); //For each step, we "jump" a distance which is a power of 2
        bool isSourceA = true;
        int stepIndex = 0;
        
        //We need to swap the source/result textures every step so the next step can use the result of the previous step as the source
        while (currentStep >= 1)
        {
            _generatorShader.SetTexture(_jfaKernelIndex, "_Source", isSourceA ? _rtA : _rtB);
            _generatorShader.SetTexture(_jfaKernelIndex, "_Result", isSourceA ? _rtB : _rtA);
            _generatorShader.SetInt("_Step", currentStep);
            
            Dispatch(_jfaKernelIndex);

            currentStep /= 2;
            isSourceA = !isSourceA;

            stepIndex++;
            
            if(_showSteps && stepIndex >= _stopAfter)
                break;
        }

        var jfaTexture = isSourceA ? _rtA : _rtB;
        var finalTexture = jfaTexture;

        if (_showOutline)
        {
            //Finally, we use the resulting JFA texture as the source for the outline
            finalTexture = isSourceA ? _rtB : _rtA;
            
            _generatorShader.SetTexture(_outlineKernelIndex, "_Source", jfaTexture);
            _generatorShader.SetTexture(_outlineKernelIndex, "_Result", finalTexture);
            _generatorShader.SetFloat("_Radius", _outlineRadius);
            
            Dispatch(_outlineKernelIndex);
        }

        _renderer.material.mainTexture = finalTexture;
    }

    private void Dispatch(int kernelIndex)
    {
        _generatorShader.Dispatch(kernelIndex, Mathf.CeilToInt(_seedTexture.width / _threadGroupSizes.x), Mathf.CeilToInt(_seedTexture.height / _threadGroupSizes.y), 1);
    }
}
