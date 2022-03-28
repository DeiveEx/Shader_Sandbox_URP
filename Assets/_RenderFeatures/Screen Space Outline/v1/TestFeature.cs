using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TestFeature : ScriptableRendererFeature
{
	[Serializable]
	private class TextureSettings
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

	class CustomRenderPass : ScriptableRenderPass
	{
		private string profilingName;
		private RenderTargetHandle targetTexture;
		private TextureSettings textureSettings;
		private List<ShaderTagId> shaderTagIDList;
		private Material customMaterial;
		private FilteringSettings filteringSettings;
		private RenderTargetIdentifier cameraColorTarget; //For debugging


		public CustomRenderPass(string profilingName, TextureSettings textureSettings, LayerMask outlineLayerMask, LayerMask occludersLayerMask, Material customMaterial)
		{
			this.profilingName = profilingName;
			this.textureSettings = textureSettings;
			targetTexture.Init("_TestTexture");

			shaderTagIDList = new List<ShaderTagId>() {
				new ShaderTagId("UniversalForward"),
				new ShaderTagId("UniversalForwardOnly"),
				new ShaderTagId("LightweightForward"),
				new ShaderTagId("SRPDefaultUnlit"),
			};

			this.customMaterial = customMaterial;

			filteringSettings = new FilteringSettings(RenderQueueRange.opaque, outlineLayerMask);
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			RenderTextureDescriptor textureDescriptor = cameraTextureDescriptor;
			textureDescriptor.colorFormat = textureSettings.colorFormat;
			textureDescriptor.depthBufferBits = (int)textureSettings.depthBufferBits;

			cmd.GetTemporaryRT(targetTexture.id, textureDescriptor, textureSettings.filterMode); //Get a temporary Render texture and sets it to the Handle we defined earlier so shaders and stuff can access it through a global variable
			ConfigureTarget(targetTexture.Identifier()); //Set the RT from our handle as the target for this pass
			ConfigureClear(ClearFlag.All, textureSettings.backgroundColor); //Clears the RT to guarantee that the temp RT we got doesn't have anything on it, since it can give us a cached RT
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			//Get the camera color texture to use as a source for the blit
			cameraColorTarget = renderingData.cameraData.renderer.cameraColorTarget;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			//If we don't have the material to render, we can't really do anything
			if (customMaterial == null)
				return;

			var cmd = CommandBufferPool.Get(profilingName);

			using (new ProfilingScope(cmd, new ProfilingSampler("SceneViewSpaceNormalsTextureCreation")))
			{
				//Clear the buffer
				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();

				//Create the draw Settings, passing the list of shaders tags to be rendered, the current rendering data and the sorting criteria to be used for rendering (in this case, we're using the same as the camera)
				var drawSettings = CreateDrawingSettings(shaderTagIDList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
				drawSettings.overrideMaterial = customMaterial; //Set the material to use to override the rendering of the objects

				//Draws the renderers that were not culled (cull results) and that pass the "filterSettings" using the defined "drawSettings"
				context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);

				Blit(cmd, targetTexture.Identifier(), cameraColorTarget); //For debbugging
			}

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		public override void FrameCleanup(CommandBuffer cmd)
		{
			cmd.ReleaseTemporaryRT(targetTexture.id);
		}
	}

	[SerializeField] private RenderPassEvent renderPassEvent;
	[SerializeField] private LayerMask drawLayerMask;
	[SerializeField] private LayerMask occludersLayerMask;
	[SerializeField] private TextureSettings textureSettings;
	[SerializeField] private Material customMaterial;

	private CustomRenderPass customPass;

	/// <inheritdoc/>
	public override void Create()
	{
		customPass = new CustomRenderPass(name, textureSettings, drawLayerMask, occludersLayerMask, customMaterial);

		// Configures where the render pass should be injected.
		customPass.renderPassEvent = renderPassEvent;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		renderer.EnqueuePass(customPass);
	}
}


