using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SimpleBlitRenderPass_V2 : ScriptableRenderPass
{
    private Material _material; //The material to use for the blit
    private int _passIndex; //Which pass of the Shader to use for the blit
    private RTHandle _colorTarget;
    
    public SimpleBlitRenderPass_V2(Material material, int passIndex)
    {
        _material = material;
        _passIndex = passIndex;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        ConfigureTarget(_colorTarget);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if(_material == null)
            return;
        
        //Get a CommandBuffer from the pool
        CommandBuffer cmd = CommandBufferPool.Get("SimpleBlit_V2");

        Blit(cmd, _colorTarget, _colorTarget, _material, _passIndex);
        
        //Execute the CommandBuffer and release it
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void SetTarget(RTHandle colorTarget)
    {
        _colorTarget = colorTarget;
    }
}
