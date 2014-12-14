using UnityEngine;
using System.Collections;

public class ForcerConrol : MonoBehaviour {

	private float Speed=1000;
	private float CurentSpeed=0;


	float Damping=5f;
	public GameObject Target;
	public Transform pivot;

	public Controller C;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {



		if(Input.GetAxis("Vertical")>0 && CurentSpeed<=Speed)
			CurentSpeed+=10;
		if(Input.GetAxis("Vertical")<0 && CurentSpeed>=0)
			CurentSpeed-=40;

		if(Input.GetAxis("Vertical")==0 && CurentSpeed>=0)
			CurentSpeed-=20;

		if(Input.GetAxis("Vertical")==0 && CurentSpeed<=0)
			CurentSpeed=0;

		transform.position=pivot.position;
		Target.rigidbody.velocity=transform.forward*CurentSpeed*Time.deltaTime;
		//Target.rigidbody.AddForce(transform.forward*CurentSpeed*Time.deltaTime);

		Quaternion rotation = Quaternion.LookRotation(Target.transform.position - transform.position);
		transform.rotation = Quaternion.Slerp(transform.rotation,rotation,Time.deltaTime*Damping);
	}
}
