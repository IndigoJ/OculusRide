using UnityEngine;
using System.Collections;

public class steeringWheelAnim : MonoBehaviour {

	public float NeedAngle;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		transform.localEulerAngles = Vector3.Lerp (transform.localEulerAngles, new Vector3 (19.52147f, 0, NeedAngle), 0.5f);
	}
}
