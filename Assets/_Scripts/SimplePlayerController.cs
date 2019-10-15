using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
	public float force;

	private Rigidbody rb;
	private Vector3 input;

	private void Awake() {
		rb = GetComponent<Rigidbody>();
	}

    // Update is called once per frame
    void FixedUpdate()
    {
		input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

		if(input.x != 0 || input.z != 0) {
			rb.AddForce(input * force);
		}
    }
}
