using UnityEngine;
using System.Collections;

public class CubeSpawn : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Debug.Log ("Hello, World!");
		Debug.Log ("I am dead inside");
	}
	
	// Update is called once per frame
	void Update () {
		Debug.Log ("times since last frame" + Time.deltaTime);
	}


}
