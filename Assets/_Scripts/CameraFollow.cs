using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {
    public Transform target;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void LateUpdate () {
        transform.position = target.position;
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime);
	}
}
