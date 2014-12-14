using UnityEngine;
using System.Collections;

public class CarSound : MonoBehaviour {

	private NewControl NC;

	// Use this for initialization
	void Start () {
		NC = gameObject.GetComponent<NewControl> ();
	}
	
	// Update is called once per frame
	void Update () {
		EngineSound();
	}

	private void EngineSound(){
		if (NC.currentGear > 0) {
			audio.pitch = ((NC.rpm - 1500) / (NC.changGears [NC.currentGear - 1] - 1500))+1;
		}
		else {
			audio.pitch = 0.1f;
		}
	}
}
