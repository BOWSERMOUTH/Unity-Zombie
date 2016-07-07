using UnityEngine;
using System.Collections;

public class FinishScript : MonoBehaviour {
	AudioSource myAudio;

	// Use this for initialization
	void Start () {
		myAudio = GetComponent<AudioSource> ();
	}

	void OnCollisionEnter(Collision collision) {
		foreach (Collider collider in collision.collider) {
			Debug.Log ("COLISSION DETECTED!!!");
		}
	}

	// Update is called once per frame
	void Update () {
	
	}
}
