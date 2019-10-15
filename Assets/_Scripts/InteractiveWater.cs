using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveWater : MonoBehaviour
{
	[SerializeField]
	RenderTexture renderTexture;
	[SerializeField]
	Transform target;

	private Transform myTransform;
	private float posY;

	private void Awake() {
		myTransform = transform;
		posY = myTransform.position.y;

		Shader.SetGlobalTexture("_WaterRippleRT", renderTexture);
		Shader.SetGlobalFloat("_OrtographicCameraSize", GetComponent<Camera>().orthographicSize);
	}

	// Update is called once per frame
	void Update() {
		myTransform.position = target.position + (Vector3.up * posY);
		Shader.SetGlobalVector("_Position", myTransform.position);
    }
}
