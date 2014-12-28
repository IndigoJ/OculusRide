using UnityEngine;
using System.Collections;

public class Speedometer : MonoBehaviour {

	public AnimationCurve SpeedCurve;
	public float Speed;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

		if(Speed>1)
		transform.localEulerAngles = Vector3.Lerp (transform.localEulerAngles, new Vector3 (13.41965f, 0f, SpeedCurve.Evaluate(Speed)), 0.5f);
	}


}
