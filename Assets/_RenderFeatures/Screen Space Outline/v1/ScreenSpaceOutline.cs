using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceOutline : ScriptableRendererFeature
{
	//Settings for the Scene Normal texture we'll be creating
	[Serializable]
	private class ViewSpaceNormalsTextureSettings
	{
		public enum DepthBufferValues
		{
			Depth_0 = 0,
			Depth_16 = 16,
			Depth_24 = 24,
			Depth_32 = 32
		}

		public RenderTextureFormat colorFormat;
		public DepthBufferValues depthBufferBits = DepthBufferValues.Depth_16;
		public FilterMode filterMode = FilterMode.Point;
		public Color backgroundColor;
	}

	//This pass will create a Scene Normal texture to be used for the Outline
	class ViewSpaceNormalsTexturePass : ScriptableRenderPass
	{
		private string profilingName; //The pass name that will appear in the Frame Debugger
		private RenderTargetHandle normalsTexture; //Our target texture
		private ViewSpaceNormalsTextureSettings normalsTextureSettings; //Some custom settings for the target texture
		private List<ShaderTagId> shaderTagIDList; //Which shaderss will be included when rendering?
		private Material normalsMaterial; //The material used to actually render the normals
		private Material occluderMaterial; //The material used to render objects that should occlude the normals
		private FilteringSettings filteringSettings; //Additional filtering settings to decide what to render (things like "which layer mask should the renderer be?")
		private FilteringSettings occluderFilteringSettings; //Filtering settings for objects that should occlude our outlines

		public ViewSpaceNormalsTexturePass(string profilingName, ViewSpaceNormalsTextureSettings normalsTextureSettings, LayerMask outlineLayerMask, LayerMask occludersLayerMask, Material normalsMaterial, Material occluderMaterial)
		{
			this.profilingName = profilingName;
			this.normalsTextureSettings = normalsTextureSettings;
			normalsTexture.Init("_ScreenSpaceNormals"); //Create a Texture handle for the texture we'll be writing to

			shaderTagIDList = new List<ShaderTagId>() {
				new ShaderTagId("UniversalForward"),
				new ShaderTagId("UniversalForwardOnly"),
				new ShaderTagId("LightweightForward"),
				new ShaderTagId("SRPDefaultUnlit"),
			};

			this.normalsMaterial = normalsMaterial;
			this.occluderMaterial = occluderMaterial;

			filteringSettings = new FilteringSettings(RenderQueueRange.opaque, outlineLayerMask);
			occluderFilteringSettings = new FilteringSettings(RenderQueueRange.opaque, occludersLayerMask);
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			RenderTextureDescriptor normalsTextureDescriptor = cameraTextureDescriptor;
			normalsTextureDescriptor.colorFormat = normalsTextureSettings.colorFormat;
			normalsTextureDescriptor.depthBufferBits = (int)normalsTextureSettings.depthBufferBits;

			cmd.GetTemporaryRT(normalsTexture.id, normalsTextureDescriptor, normalsTextureSettings.filterMode); //Get a temporary Render texture and sets it to the Handle we defined earlier so shaders and stuff can access it through a global variable
			ConfigureTarget(normalsTexture.Identifier()); //Set the RT from our handle as the target for this pass
			ConfigureClear(ClearFlag.All, normalsTextureSettings.backgroundColor); //Clears the RT to guarantee that the temp RT we got doesn't have anything on it, since it can give us a cached RT
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			//If we don't have the material to render, we can't really do anything
			if (normalsMaterial == null)
				return;

			var cmd = CommandBufferPool.Get(profilingName);

			using (new ProfilingScope(cmd, new ProfilingSampler("SceneViewSpaceNormalsTextureCreation")))
			{
				//Clear the buffer
				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();

				//Create the draw Settings, passing the list of shaders tags to be rendered, the current rendering data and the sorting criteria to be used for rendering (in this case, we're using the same as the camera)
				var drawSettings = CreateDrawingSettings(shaderTagIDList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
				drawSettings.overrideMaterial = normalsMaterial; //Set the material to use to override the rendering of the objects

				//Draws the renderers that were not culled (cull results) and that pass the "filterSettings" using the defined "drawSettings"
				context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);

				//Draws any other objects that should occlude our normals texture
				var occluderDrawSettings = CreateDrawingSettings(shaderTagIDList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
				occluderDrawSettings.overrideMaterial = occluderMaterial;
				context.DrawRenderers(renderingData.cullResults, ref occluderDrawSettings, ref occluderFilteringSettings);
			}

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		public override void FrameCleanup(CommandBuffer cmd)
		{
			cmd.ReleaseTemporaryRT(normalsTexture.id);
		}
	}

	//This pass will actually draw the outlines
	class ScreenSpaceOutlinePass : ScriptableRenderPass
	{
		private string profilingName;
		private readonly Material screenSpaceOutlineMaterial;
		private RenderTargetIdentifier cameraColorTarget;
		private RenderTargetHandle tempTexture;

		public ScreenSpaceOutlinePass(string profilingName, Material outlinematerial)
		{
			this.profilingName = profilingName;
			screenSpaceOutlineMaterial = outlinematerial;
			tempTexture.Init("_TempTexture");
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor); //Get a temporary Render Texture. We use the camera Texture descriptor to tell that the texture should have the same format of the camera texture we're using
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			//Get the camera color texture to use as a source for the blit
			cameraColorTarget = renderingData.cameraData.renderer.cameraColorTarget;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			//If we don't have a material for the blir, we can't do much
			if (screenSpaceOutlineMaterial == null)
				return;

			var cmd = CommandBufferPool.Get(profilingName);

			using (new ProfilingScope(cmd, new ProfilingSampler("ScreenSpaceOutlines")))
			{
				Blit(cmd, cameraColorTarget, tempTexture.Identifier());
				Blit(cmd, tempTexture.Identifier(), cameraColorTarget, screenSpaceOutlineMaterial);
			}

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		public override void FrameCleanup(CommandBuffer cmd)
		{
			cmd.ReleaseTemporaryRT(tempTexture.id); //Releases the tempTexture to free memory
		}
	}

	[SerializeField] private RenderPassEvent renderPassEvent;
	[SerializeField] private LayerMask outlinesLayerMask;
	[SerializeField] private LayerMask occludersLayerMask;
	[SerializeField] private ViewSpaceNormalsTextureSettings normalsTextureSettings;
	[SerializeField] private Material normalsMaterial;
	[SerializeField] private Material outlineMaterial;
	[SerializeField] private Material occluderMaterial;

	private ViewSpaceNormalsTexturePass screenSpaceNormalTexturePass;
	private ScreenSpaceOutlinePass screenSpaceOutlinePass;

	/// <inheritdoc/>
	public override void Create()
	{
		screenSpaceNormalTexturePass = new ViewSpaceNormalsTexturePass(name + "_ScreenSpaceNormalsPass", normalsTextureSettings, outlinesLayerMask, occludersLayerMask, normalsMaterial, occluderMaterial);
		screenSpaceOutlinePass = new ScreenSpaceOutlinePass(name + "_OutlinePass", outlineMaterial);

		// Configures where the render pass should be injected.
		screenSpaceNormalTexturePass.renderPassEvent = renderPassEvent;
		screenSpaceOutlinePass.renderPassEvent = renderPassEvent;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		renderer.EnqueuePass(screenSpaceNormalTexturePass);
		renderer.EnqueuePass(screenSpaceOutlinePass);
	}
}


