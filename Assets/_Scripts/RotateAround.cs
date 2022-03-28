using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAround : MonoBehaviour
{
	[SerializeField] private Vector3 axis;
	[SerializeField] private float degPerSec = 90;

	void Update()
	{
		transform.Rotate(axis, degPerSec * Time.deltaTime);
	}
}
