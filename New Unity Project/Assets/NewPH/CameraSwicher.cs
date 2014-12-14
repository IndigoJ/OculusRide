using UnityEngine;
using System.Collections;

public class CameraSwicher : MonoBehaviour {
	private bool driverCamera=false;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(KeyCode.C)){
			if(driverCamera){
				GetComponent<CameraFixedTo>().enabled = true;
				GetComponent<CameraController>().enabled = false;
				driverCamera=!driverCamera;
			}
			else{
				GetComponent<CameraFixedTo>().enabled = false;
				GetComponent<CameraController>().enabled = true;
				driverCamera=!driverCamera;}
		}
	}
}
