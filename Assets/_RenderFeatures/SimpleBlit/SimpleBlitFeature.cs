using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SimpleBlitFeature : ScriptableRendererFeature
{
	//These fields will appear in the inspector of the Render Feature
	[SerializeField] private RenderPassEvent renderEvent = RenderPassEvent.AfterRenderingPostProcessing;
	[SerializeField] private Material blitMaterial; //The material to use for the blit
	[SerializeField] private int passIndex; //The pass to be used by the shader (Shader Graphs should always use "0")

	SimpleBlitRenderPass m_ScriptablePass;

	public override void Create()
	{
		m_ScriptablePass = new SimpleBlitRenderPass(name, blitMaterial, passIndex);

		// Configures where the render pass should be injected.
		m_ScriptablePass.renderPassEvent = renderEvent;
	}

	public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
	{
		//Get the renderer color texture and pass it as the source of our pass
		m_ScriptablePass.SetSource(renderer.cameraColorTargetHandle);
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		//Enqueue the pass
		renderer.EnqueuePass(m_ScriptablePass);
	}
}


