using UnityEngine;
using System.Collections;

public class CarWheel : MonoBehaviour {

	[SerializeField] WheelCollider WheelFR;
	[SerializeField] WheelCollider WheelFL;
	[SerializeField] WheelCollider WheelBL;
	[SerializeField] WheelCollider WheelBR;

	[SerializeField] Transform WheelFRTr;
	[SerializeField] Transform WheelFLTr;
	[SerializeField] Transform WheelBLTr;
	[SerializeField] Transform WheelBRTr;

	[SerializeField] float minSpeed = 50;
	[SerializeField] float currentSpeed;
	[SerializeField] float maxSpeed = 150;
	[SerializeField] float maxReverseSpeed = 50;


	[SerializeField] float lowSpeedSteer = 10;
	[SerializeField] float highSpeedSteer = 1;

	[SerializeField] float decelerationSpeed = 30;

	[SerializeField] float maxTorque;
	private bool breaked = false;
	[SerializeField] float maxBreakedTorque = 100;

	private float SidewayFrictionOfCar; 
	private float ForwardFrictionOfCar; 

	private float slipSidewayFrictionOfCar; 
	private float slipForwardFrictionOfCar; 

	[SerializeField] GameObject CenterTest;

	// Use this for initialization
	void Start () {
		rigidbody.centerOfMass=CenterTest.transform.localPosition;

		ForwardFrictionOfCar = WheelBR.forwardFriction.stiffness;
		SidewayFrictionOfCar = WheelBR.sidewaysFriction.stiffness;

		slipForwardFrictionOfCar = 0.04f;
		slipSidewayFrictionOfCar = 0.08f;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		CenterTest.transform.localPosition=rigidbody.centerOfMass;

		currentSpeed = Mathf.Round(2*Mathf.PI*WheelBL.radius*WheelBL.rpm*60/1000);

		float speedFactor = rigidbody.velocity.magnitude/minSpeed;
		float curentSteer = Mathf.Lerp(lowSpeedSteer,highSpeedSteer,speedFactor);
		curentSteer*=Input.GetAxis("Horizontal");


		if(currentSpeed<maxSpeed && currentSpeed > -maxReverseSpeed && !breaked){
			WheelBR.motorTorque=maxTorque*Input.GetAxis("Vertical");
			WheelBL.motorTorque=maxTorque*Input.GetAxis("Vertical");
		}
		else{
			WheelBR.motorTorque=0;
			WheelBL.motorTorque=0;
		}

		WheelFL.steerAngle = curentSteer;
		WheelFR.steerAngle = curentSteer;

		if(!Input.GetButton("Vertical")){
			WheelBR.brakeTorque = decelerationSpeed;
			WheelBL.brakeTorque = decelerationSpeed;
		}
		else{
			WheelBR.brakeTorque=0;
			WheelBL.brakeTorque=0;
		}

		Handbrakes();
	}

	void Update(){
		WheelBLTr.Rotate(-WheelBL.rpm/60*360*Time.deltaTime,0,0);
		WheelBRTr.Rotate(-WheelBL.rpm/60*360*Time.deltaTime,0,0);
		WheelFLTr.Rotate(-WheelBL.rpm/60*360*Time.deltaTime,0,0);
		WheelFRTr.Rotate(-WheelBL.rpm/60*360*Time.deltaTime,0,0);
		WheelPosition(WheelFL,WheelFLTr);
		WheelPosition(WheelFR,WheelFRTr);
		WheelPosition(WheelBL,WheelBLTr);
		WheelPosition(WheelBR,WheelBRTr);
		WheelFRTr.localEulerAngles=new Vector3(WheelFRTr.localEulerAngles.x,WheelFL.steerAngle-WheelFRTr.localEulerAngles.z,WheelFRTr.localEulerAngles.z);
		WheelFLTr.localEulerAngles=new Vector3(WheelFRTr.localEulerAngles.x,WheelFL.steerAngle-WheelFRTr.localEulerAngles.z,WheelFRTr.localEulerAngles.z);
	}

	private void WheelPosition(WheelCollider WC, Transform WT){
		RaycastHit hit = new RaycastHit();
		Vector3 WheelPos = new Vector3();

		if(Physics.Raycast(WC.transform.position,-WC.transform.up,out hit,WC.radius+WC.suspensionDistance)){
			WheelPos=hit.point+WC.transform.up*WC.radius;
		}
		else{
			WheelPos=WC.transform.position -WC.transform.up*WC.radius;
		}
		WT.position=Vector3.Lerp(WT.position,WheelPos,1f);

	}

	private void Handbrakes(){
		if(Input.GetButton("Jump")){
			breaked=true;
		}
		else{
			breaked=false;
		}
		if(breaked){
			WheelFR.brakeTorque=maxBreakedTorque;
			WheelFL.brakeTorque=maxBreakedTorque;
			WheelBR.motorTorque=0;
			WheelBL.motorTorque=0;
			SetSlip(WheelBL,slipForwardFrictionOfCar,slipSidewayFrictionOfCar);
			SetSlip(WheelBR,slipForwardFrictionOfCar,slipSidewayFrictionOfCar);
			//SetSlip(WheelFL,slipForwardFrictionOfCar,slipSidewayFrictionOfCar);
			//SetSlip(WheelFR,slipForwardFrictionOfCar,slipSidewayFrictionOfCar);
		}
		else{
			WheelFR.brakeTorque=0;
			WheelFL.brakeTorque=0;
			SetSlip(WheelBL,ForwardFrictionOfCar,SidewayFrictionOfCar);
			SetSlip(WheelBR,ForwardFrictionOfCar,SidewayFrictionOfCar);
			//SetSlip(WheelFL,ForwardFrictionOfCar,SidewayFrictionOfCar);
			//SetSlip(WheelFR,ForwardFrictionOfCar,SidewayFrictionOfCar);
		}

	}

	private void SetSlip(WheelCollider wheel, float CurrentForwardFriction,float CurrentSidewayFriction){
		WheelFrictionCurve sf = wheel.sidewaysFriction;
		WheelFrictionCurve ff = wheel.forwardFriction;
		ff.stiffness = CurrentForwardFriction;
		sf.stiffness = CurrentSidewayFriction;
		wheel.forwardFriction = ff;
		wheel.sidewaysFriction = sf;
	}

}