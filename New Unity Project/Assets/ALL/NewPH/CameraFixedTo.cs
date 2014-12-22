using UnityEngine;
using System.Collections;

public class CameraFixedTo : MonoBehaviour {

	public Transform Pos;

	// Use this for initialization
	void Start () {
	
	}
	
	void LateUpdate () 
	{
		transform.position = Pos.transform.position;
		transform.rotation = Pos.transform.rotation;
	}
}
