using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponScript : MonoBehaviour {

	public GameObject[] shotSpawns;

	public GameObject shot;

	private void OnFire()
	{
		foreach (GameObject ss in shotSpawns) {
			Instantiate (shot,ss.transform.position,ss.transform.rotation);
		}
	}
}
