using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class OutlineV3RendererFeature : ScriptableRendererFeature
{
    private OutlineV3RenderPass _renderPass;
    
    public override void Create()
    {
        _renderPass = new OutlineV3RenderPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _renderPass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
    }
}
