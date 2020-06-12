using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfter : MonoBehaviour
{
	public float delay = 1;

	private void Start()
	{
		Invoke("Destroy", delay);
	}

	public void Destroy()
	{
		Destroy(gameObject);
	}
}
