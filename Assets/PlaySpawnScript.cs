using UnityEngine;
using System.Collections;

public class PlaySpawnScript : MonoBehaviour {

	public Transform spawnLocation;

	// Use this for initialization
	void Start () {
		if (spawnLocation != null) {
			transform.position = spawnLocation.position;
		} else {
			Debug.LogError ("No Spawn location");
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
