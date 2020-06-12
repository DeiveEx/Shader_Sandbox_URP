using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexaShieldDamageEffect : MonoBehaviour
{
    public string bulletTag;
	public GameObject damageEffect;

	private Material mat;

	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == bulletTag)
		{
			GameObject go = Instantiate(damageEffect, transform);
			go.SetActive(true);
			go.transform.localPosition = Vector3.zero;

			mat = go.GetComponent<ParticleSystemRenderer>().material;
			mat.SetVector("_Point", other.transform.position);

			Destroy(other.gameObject);
		}
	}
}
