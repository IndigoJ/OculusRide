using UnityEngine;
using System.Collections;

public class Tachometer : MonoBehaviour {

	public AnimationCurve RPMdCurve;
	public float RPM;
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
		
		if(RPM>100)
			transform.localEulerAngles = Vector3.Lerp (transform.localEulerAngles, new Vector3 (13.41965f, 0f, RPMdCurve.Evaluate(RPM/1000)), 0.5f);
	}
}
