using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootAt : MonoBehaviour
{
    public Transform target;
    public GameObject bullet;
    public float shootInterval = 1;
	public float innerRadius = 1;
	public float OuterRadius = 2;

	private void OnEnable()
	{
		StartCoroutine(Shoot_Routine());
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}

	private void OnValidate()
	{
		innerRadius = Mathf.Max(0, innerRadius);
		OuterRadius = Mathf.Max(0, OuterRadius);

		innerRadius = Mathf.Min(innerRadius, OuterRadius);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, innerRadius);
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, OuterRadius);
	}

	private IEnumerator Shoot_Routine()
	{
		Vector3 pos = new Vector3();

		while (true)
		{
			float distance = Random.Range(innerRadius, OuterRadius);
			pos = Random.insideUnitSphere * distance;

			Transform go = Instantiate(bullet).transform;
			go.position = transform.position + pos;
			go.rotation = Quaternion.LookRotation(target.position - go.position);

			yield return new WaitForSeconds(shootInterval);
		}
	}
}
