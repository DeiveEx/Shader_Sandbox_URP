using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SimpleBlitFeature_V2 : ScriptableRendererFeature
{
    [SerializeField] private Material _blitMaterial;
    [SerializeField] private int _passIndex;
    [SerializeField] private RenderPassEvent _injectPoint;
    
    private SimpleBlitRenderPass_V2 _renderPass;
    
    public override void Create()
    {
        _renderPass = new SimpleBlitRenderPass_V2(_blitMaterial, _passIndex);
        _renderPass.renderPassEvent = _injectPoint;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(renderingData.cameraData.cameraType != CameraType.Game)
            return;
        
        _renderPass.ConfigureInput(ScriptableRenderPassInput.Color);
        renderer.EnqueuePass(_renderPass);
    }
    
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if(renderingData.cameraData.cameraType != CameraType.Game)
            return;
        
        _renderPass.SetTarget(renderer.cameraColorTargetHandle);
    }
}
