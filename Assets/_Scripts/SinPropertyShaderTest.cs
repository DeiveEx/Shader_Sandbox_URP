using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinPropertyShaderTest : MonoBehaviour {
	public string propertyName;
	public float min, max, speed = 1;

	private Renderer rend;

	// Use this for initialization
	void Start () {
		rend = GetComponent<Renderer>();
	}
	
	// Update is called once per frame
	void Update () {
		float t = Mathf.Lerp(min, max, (Mathf.Sin(Time.time * speed) * .5f) + .5f);
		rend.material.SetFloat(propertyName, t);
	}
}
