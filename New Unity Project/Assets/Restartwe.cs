using UnityEngine;
using System.Collections;

public class Restartwe : MonoBehaviour {

	[SerializeField] private Vector3 pos;
	[SerializeField] private Quaternion rot;
	
		
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {


		if(Input.GetKeyDown(KeyCode.R)){
			gameObject.transform.position = pos;
			gameObject.transform.rotation = rot;
		}
	}
}
