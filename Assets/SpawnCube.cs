using UnityEngine;
using System.Collections;

public class SpawnCube : MonoBehaviour {

	public GameObject myCube;
	public GameObject myZombiePrefab;
	public GameObject targetPlayer;

	// Use this for initialization
	void Start() {

	}
	
	// Update is called once per frame
	void Update() {
//		Instantiate (myCube, transform.position, transform.rotation);

		if (Input.GetKeyDown (KeyCode.M)) {
			Instantiate (myZombiePrefab, targetPlayer.transform.position, targetPlayer.transform.rotation);
		}

	}
}
