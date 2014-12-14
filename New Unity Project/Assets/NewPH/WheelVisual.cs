using UnityEngine;
using System.Collections;

public class WheelVisual : MonoBehaviour {

	[SerializeField] private Transform FLPivot;
	[SerializeField] private Transform FRPivot;

	[SerializeField] private Transform FL;
	[SerializeField] private Transform FR;
	[SerializeField] private Transform RL;
	[SerializeField] private Transform RR;

	 private Vector3 FLStart;
	 private Vector3 FRStart;
	 private Vector3 RLStart;
	 private Vector3 RRStart;
	 private float FLRotation;
	 private float FRRotation;
	 private float RLRotation;
	 private float RRRotation;

	float currentSpeed;

	[SerializeField] private WheelCollider WheelFR;
	[SerializeField] private WheelCollider WheelFL;
	[SerializeField] private WheelCollider WheelRL;
	[SerializeField] private WheelCollider WheelRR;

	// Use this for initialization
	void Start () {
		FLStart=FL.localPosition;
		FRStart=FR.localPosition;
		RLStart=FL.localPosition;
		RRStart=FR.localPosition;
		FLRotation=0;
		FRRotation=0;
		RLRotation=0;
		RRRotation=0;
	}
	
	// Update is called once per frame
	void Update () {
		FLRotation=UpdateWheels(WheelFL,FL,FLStart,FLRotation);
		FRRotation=UpdateWheels(WheelFR,FR,FRStart,FRRotation);
		RLRotation=UpdateWheels(WheelRL,RL,RLStart,RLRotation);
		RRRotation=UpdateWheels(WheelRR,RR,RRStart,RRRotation);
		currentSpeed=Mathf.Round(2*Mathf.PI*WheelFL.radius*WheelFL.rpm*60/1000);
	}



	private float UpdateWheels(WheelCollider col,Transform wheelTransform,Vector3 wheelStartPos,float rotation){


			/////////////ПОДВЕСКА
			WheelHit hit; 
			Vector3 lp = wheelTransform.localPosition; 
			if(col.GetGroundHit(out hit)){
				lp.y -= Vector3.Dot(wheelTransform.position - hit.point, transform.up) - col.radius; 
			}else{
			lp.y = wheelStartPos.y - (col.suspensionDistance-col.radius);
			}
			wheelTransform.localPosition = lp; 
			//////////////
			
			/////////////КРУЧЕНИЕ + ПОВОРОТ КОЛЕС
			rotation = Mathf.Repeat(rotation + Time.deltaTime * col.rpm * 360.0f / 60.0f, 360.0f); 
			wheelTransform.localRotation = Quaternion.Euler(rotation, col.steerAngle, 0); 
			return rotation;
			currentSpeed=Mathf.Round(2*Mathf.PI*col.radius*col.rpm*60/1000);
			/////////////

	}

	void OnGUI(){
		GUI.Box(new Rect(10,10,150,30),"Speed - "+currentSpeed);
	}
}
