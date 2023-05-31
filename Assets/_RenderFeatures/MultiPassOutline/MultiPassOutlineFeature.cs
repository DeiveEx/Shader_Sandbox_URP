using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MultiPassOutlineFeature : ScriptableRendererFeature
{
    [SerializeField] private Material _outlineMaterial;

    private OutlineIDsPass _iDsPass;
    private OutlineColors _colorsPass;
    private FinalOutlinePass _finalOutlinesPass;

    public override void Create()
    {
        //This pass renders the object IDs into a render texture
        _iDsPass = new OutlineIDsPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses
        };

        //This pass renders the colors of the outline
        _colorsPass = new OutlineColors()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses
        };
        
        //This pass creates a Render Texture with the outlines
        _finalOutlinesPass = new FinalOutlinePass(_outlineMaterial)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_iDsPass);
        renderer.EnqueuePass(_colorsPass);
        renderer.EnqueuePass(_finalOutlinesPass);
    }
}

public class OutlineIDsPass : ScriptableRenderPass
{
    private RenderTargetHandle _renderTextureHandle;
    private FilteringSettings _filter;
    private List<ShaderTagId> _shaderTagIDList;

    public OutlineIDsPass()
    {
        _renderTextureHandle.Init("_OutlineId"); //Initializes our RT Handle. Note that a handle just represents a RT, but it's not an actual RT
        _filter = new FilteringSettings(RenderQueueRange.opaque);

        //The ShaderTag list is a way of filtering shaders passes that has the "LightMode" tag defined. Unity has some
        //built-in values for this tag, but you can also set the LightMode tag to a custom value for your own use. I have no idea why it's
        //called "LightMode" when it's not strictly related to lighting, though. They probably didn't change it in order
        //to not break old projects (although they had teh chance to change it when creating the SRPs...)
        _shaderTagIDList = new List<ShaderTagId>()
        {
            new ShaderTagId("CustomOutlineID")
        };
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        RenderTextureDescriptor tempRTDescriptor = cameraTextureDescriptor;
        tempRTDescriptor.colorFormat = RenderTextureFormat.R8; //For this pass we want a simple, single channel 8 bit texture

        //This tells Unity that we want a texture with this description and filterMode and then sets the target RT from
        //our handle as a global shader property that any shader can use using the defined ID
        cmd.GetTemporaryRT(_renderTextureHandle.id, tempRTDescriptor, FilterMode.Point);
        
        //Set the target texture of this pass to be the RT instead of the camera
        ConfigureTarget(_renderTextureHandle.Identifier());
        
        //Tells how we want to clear the RT
        ConfigureClear(ClearFlag.All, Color.black);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if(_shaderTagIDList == null || _shaderTagIDList.Count == 0)
            return;
        
        var commandBuffer = CommandBufferPool.Get($"{nameof(OutlineIDsPass)}");

        using (new ProfilingScope(commandBuffer, new ProfilingSampler($"{nameof(OutlineIDsPass)}")))
        {
            //Clears the buffer. I'm not sure why we need to execute it before clearing, but it doesn't work if we don't do that
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
            
            //We need to create some settings to draw objects. I don't know why we have so many different types of "filters"
            //all over the place, but for some reason that's how Unity did things. In here we are saying that we only want
            //to draw objects with materials which shader includes any "LightMode" tag value from the tag list, which data
            //are we rendering and what's the sorting criteria (here we're using teh camera's default)
            var drawingSettings = CreateDrawingSettings(_shaderTagIDList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);

            //Finally, we draw our objects. The only objects that are gonna be draw are the ones that were not culled,
            //that match the drawing settings AND that match the filter
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filter);
        }
        
        //Finally, execute our own commands that were inserted into the buffer
        context.ExecuteCommandBuffer(commandBuffer);
        CommandBufferPool.Release(commandBuffer);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(_renderTextureHandle.id);
    }
}

public class OutlineColors : ScriptableRenderPass
{
    private RenderTargetHandle _renderTextureHandle;
    private FilteringSettings _filter;
    private List<ShaderTagId> _shaderTagIDList;

    public OutlineColors()
    {
        _renderTextureHandle.Init("_OutlineColors");
        _filter = new FilteringSettings(RenderQueueRange.opaque);

        _shaderTagIDList = new List<ShaderTagId>()
        {
            new ShaderTagId("CustomOutlineColor")
        };
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        RenderTextureDescriptor tempRTDescriptor = cameraTextureDescriptor;
        tempRTDescriptor.colorFormat = RenderTextureFormat.ARGBFloat;
        
        cmd.GetTemporaryRT(_renderTextureHandle.id, tempRTDescriptor, FilterMode.Point);
        
        ConfigureTarget(_renderTextureHandle.Identifier());
        
        ConfigureClear(ClearFlag.All, Color.black);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if(_shaderTagIDList == null || _shaderTagIDList.Count == 0)
            return;
        
        var commandBuffer = CommandBufferPool.Get($"{nameof(OutlineColors)}");

        using (new ProfilingScope(commandBuffer, new ProfilingSampler($"{nameof(OutlineColors)}")))
        {
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
            
            var drawingSettings = CreateDrawingSettings(_shaderTagIDList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filter);
        }
        
        context.ExecuteCommandBuffer(commandBuffer);
        CommandBufferPool.Release(commandBuffer);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(_renderTextureHandle.id);
    }
}


public class FinalOutlinePass : ScriptableRenderPass
{
    private Material _outlineMaterial;
    private RenderTargetIdentifier _source;
    private RenderTargetHandle _target;

    public FinalOutlinePass(Material outlineMaterial)
    {
        _outlineMaterial = outlineMaterial;
        _target.Init("_OutlineTexture");
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        _source = "_OutlineId";
        
        RenderTextureDescriptor tempRTDescriptor = cameraTextureDescriptor;
        tempRTDescriptor.colorFormat = RenderTextureFormat.ARGB32;

        cmd.GetTemporaryRT(_target.id, tempRTDescriptor, FilterMode.Point); //Set this texture as a global property
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if(_outlineMaterial == null)
            return;
        
        var cmdBuffer = CommandBufferPool.Get($"{nameof(FinalOutlinePass)}");

        //We can't read and write to the same texture, so we need to use a temporary texture
        Blit(cmdBuffer, _source, _target.Identifier(), _outlineMaterial, 0);

        //Execute and release the buffer
        context.ExecuteCommandBuffer(cmdBuffer);
        CommandBufferPool.Release(cmdBuffer);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(_target.id);
    }
}
