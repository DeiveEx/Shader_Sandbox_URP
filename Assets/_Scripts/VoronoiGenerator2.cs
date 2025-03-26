using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = System.Numerics.Vector2;

public class VoronoiGenerator2 : MonoBehaviour
{
    private struct PointBufferEntry
    {
        public int X;
        public int Y;
    }
    
    [SerializeField] private ComputeShader _generatorShader;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Vector2Int _textureSize;
    [SerializeField] private int _cellAmount;
    [SerializeField] private float _offset;
    [SerializeField] private bool _randomSeed;
    [SerializeField] private int _seed;
    [SerializeField] private FilterMode _textureFilterMode;
    [SerializeField] private TextureWrapMode _textureWrapMode;

    private int _kernelIndex;
    private Vector3Int _threadGroupSizes;
    private RenderTexture _rt;
    private ComputeBuffer _buffer;
    
    private readonly int _cellAmountID = Shader.PropertyToID("_CellAmount");
    private readonly int _bufferID = Shader.PropertyToID("_PointsBuffer");
    private readonly int _OffsetID = Shader.PropertyToID("_Offset");

    private void Start()
    {
        Setup();
        Generate();
    }

    private void OnDestroy()
    {
        if(_rt != null)
            _rt.Release();
        
        if(_buffer != null)
            _buffer.Release();
    }
    
    private void OnGUI()
    {
        if (GUILayout.Button("Generate"))
        {
            Generate();
        }
    }

    private void Setup()
    {
        //get some info about the compute shader
        _kernelIndex = _generatorShader.FindKernel("ComputeVoronoiTexture");
        _generatorShader.GetKernelThreadGroupSizes(_kernelIndex, out var sizeX, out var sizeY, out var sizeZ);
        _threadGroupSizes = new()
        {
            x = (int)sizeX,
            y = (int)sizeY,
            z = (int)sizeZ,
        };

        //Create and set a RenderTexture for our shader to write to. For writing into textures, it NEEDS to be a RenderTexture with "enableRandomWrite" enabled
        _rt = new RenderTexture(_textureSize.x, _textureSize.y, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true,
            filterMode = _textureFilterMode,
            wrapMode = _textureWrapMode
        };
        _rt.Create();
        
        _generatorShader.SetTexture(_kernelIndex, "_VoronoiTexture", _rt);
        _generatorShader.SetInt("_TextureSizeX", _textureSize.x);
        _generatorShader.SetInt("_TextureSizeY", _textureSize.y);
        
        _renderer.material.mainTexture = _rt;

        Debug.Log($"Size of {typeof(int)}: {sizeof(int)}");
        Debug.Log($"Size of {nameof(PointBufferEntry)}: {Marshal.SizeOf(typeof(PointBufferEntry))}");

        Vector2 r = new Vector2(-1.2f, -0.8f);
        Debug.Log(Vector2.Dot(r, r));
    }

    private void Generate()
    {
        Debug.Log("Generating... ");
        
        if(!_randomSeed)
            Random.InitState(_seed);
        
        //Set the voronoi points
        _generatorShader.SetInt(_cellAmountID, _cellAmount);
        _generatorShader.SetFloat(_OffsetID, _offset);
        
        if(_buffer != null)
            _buffer.Release();

        _buffer = new ComputeBuffer(_cellAmount * 2, Marshal.SizeOf(typeof(PointBufferEntry)));
        var entries = new PointBufferEntry[_cellAmount];

        for (int i = 0; i < _cellAmount; i++)
        {
            entries[i] = new PointBufferEntry()
            {
                X = Random.Range(0, _textureSize.x),
                Y = Random.Range(0, _textureSize.y),
            };
        }
        
        _buffer.SetData(entries);
        _generatorShader.SetBuffer(_kernelIndex, _bufferID, _buffer);
        
        //Generate the texture
        _generatorShader.Dispatch(_kernelIndex, Mathf.CeilToInt(_textureSize.x / _threadGroupSizes.x), Mathf.CeilToInt(_textureSize.y / _threadGroupSizes.y), 1);
    }
}
