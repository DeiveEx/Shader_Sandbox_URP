using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SimpleBlitFeature : ScriptableRendererFeature
{
	//This is the class that represents the actual pass to be added. You can have multiple passes in a single feature.
	class CustomRenderPass : ScriptableRenderPass
	{
		private string profileName;
		private Material material;
		private int passIndex;
		private RenderTargetIdentifier source; //This is basically the texture itself
		private RenderTargetHandle tempTexture; //This is basically a reference to a shader variable of a texture

		//Custom constructor
		public CustomRenderPass(string profileName, Material material, int passIndex)
		{
			this.profileName = profileName;
			this.material = material;
			this.passIndex = passIndex;
			tempTexture.Init("_TempTex"); //Here we're tying this handle to a global shader variable called "_TempTex". Note that this is the HANDLE, not the texture! When we use "GetTemporaryRT" below, THEN we'll have a texture tied to this variable!
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			ConfigureInput(ScriptableRenderPassInput.Normal);
			cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor); //Get a temporary Render Texture. We use the camera Texture descriptor to tell that the texture should have the same format of the camera texture we're using
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			//To execute commands, we need to enqueue them into a command buffer, so we get a command buffer from the
			//pool that unity provides and give it a custom name so we can identify it while debugging
			var cmdBuffer = CommandBufferPool.Get(profileName);

			//Here we write the commands that will be inserted into the buffer
			
			//We can't read and write to the same texture, so we need to use a temporary texture
			Blit(cmdBuffer, source, tempTexture.Identifier()); //Copy from source to the temp texture
			Blit(cmdBuffer, tempTexture.Identifier(), source, material, passIndex); //Copy from the temp texture to source again, now applying the shader

			//Execute and release the buffer
			context.ExecuteCommandBuffer(cmdBuffer);
			CommandBufferPool.Release(cmdBuffer);
		}

		public override void FrameCleanup(CommandBuffer cmd)
		{
			cmd.ReleaseTemporaryRT(tempTexture.id); //Releases the tempTexture to free memory
		}

		public void SetSource(RenderTargetIdentifier source)
		{
			this.source = source;
		}
	}


	//These fields will appear in the inspector of the Render Feature
	[SerializeField] private RenderPassEvent renderEvent = RenderPassEvent.AfterRenderingPostProcessing;
	[SerializeField] private Material blitMaterial; //The material to use for the blit
	[SerializeField] private int passIndex; //The pass to be used by the shader (Shader Graphs shoul always use "0")

	CustomRenderPass m_ScriptablePass;

	public override void Create()
	{
		m_ScriptablePass = new CustomRenderPass(name, blitMaterial, passIndex);

		// Configures where the render pass should be injected.
		m_ScriptablePass.renderPassEvent = renderEvent;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		//Get the renderer color texture and pass it as the source of our pass
		m_ScriptablePass.SetSource(renderer.cameraColorTarget);

		//Enqueue the pass
		renderer.EnqueuePass(m_ScriptablePass);
	}
}


