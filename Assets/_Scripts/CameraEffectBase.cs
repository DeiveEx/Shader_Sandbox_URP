using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class CameraEffectBase : MonoBehaviour
{
    public Material effectMaterial;

    protected Camera cam;
    protected RawImage target;
    protected CustomRenderTexture rt;

    protected virtual void Awake()
	{
        //Get the basic components
        cam = GetComponent<Camera>();
        target = FindObjectOfType<RawImage>();
        rt = new CustomRenderTexture(Screen.width, Screen.height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

        //Set the camera to render to our RenderTexture and then set the render texture to the Raw Image
        //      cam.targetTexture = rt;
        target.material = effectMaterial;    

        //effectMaterial.SetTexture("_MainTex", rt); 

        rt.updateMode = CustomRenderTextureUpdateMode.Realtime;
        rt.material = effectMaterial;
        rt.initializationSource = CustomRenderTextureInitializationSource.Material;
        rt.initializationMaterial = effectMaterial;
        rt.doubleBuffered = true;

	}
}
