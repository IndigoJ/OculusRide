using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NewControl : MonoBehaviour {


	//Special class
	//which incapsulates
	//wheel collider
	//and some functionality 
	//specific for wheels
	[System.Serializable]
	private class Wheel {

		[SerializeField]
		public WheelCollider collider = null;
		[SerializeField]
		string objectName = "";
		bool isRear = false;
		bool isFront = false;
		Vector3 velocity = Vector3.zero;

		public float getSuspensionDistance() {
			return collider.suspensionDistance;
		}

		public Transform getTransform() {
			return collider.transform;
		}

		public bool getGrounded() {
			return collider.isGrounded;
		}

		public bool getGroundHit(out WheelHit hit) {
			return collider.GetGroundHit(out hit);
		}

		//Setting objectName variable
		//to the name of this wheels gameobject
		public void UpdateObjectName() {
			objectName = collider.gameObject.name;
		}

		public string getObjectName() {
			return objectName;
		}

		public void ApplyMotorTorque(float torque) {
			collider.motorTorque = torque;
		}

		public void ApplyBrakeTorque(float torque) {
			collider.brakeTorque = torque;
		}

		public void ChangeSidewaysFriction(WheelFrictionCurve curve) {
			collider.sidewaysFriction = curve;
		}

		public void ChangeForwardFriction(WheelFrictionCurve curve) {
			collider.forwardFriction = curve;
		}

		public void setIsRear(bool rear) {
			isRear = rear;
		}

		public void setIsFront(bool front) {
			isFront = front;
		}

		public void setSteer(float steer) {
			collider.steerAngle = steer;
		}

		public float getWheelRpm() {
			return collider.rpm;
		}

		public float getWheelRadius() {
			return collider.radius;
		}
		//Detecting if wheel is skidding
		public bool GetSkidding(float skidRate) {
			WheelHit hit = new WheelHit();
			collider.GetGroundHit(out hit);
			return Mathf.Abs(hit.sidewaysSlip) > skidRate;
		}
		//Test function, gets amount of sideways slip
		public float getSidewaysSlip() {
			WheelHit hit = new WheelHit();
			collider.GetGroundHit(out hit);
			return hit.sidewaysSlip;
		}
		//Updating friction curve
		//When driving on the specific surface
		//with specific stiffness
		public void UpdateStiffness(float stiffness) {

			WheelFrictionCurve wfc = new WheelFrictionCurve();
			wfc.asymptoteSlip = collider.forwardFriction.asymptoteSlip;
			wfc.extremumSlip = collider.forwardFriction.extremumSlip;
			wfc.extremumValue = collider.forwardFriction.extremumValue;
			wfc.asymptoteValue = collider.forwardFriction.asymptoteValue;
			wfc.stiffness = stiffness;

			WheelFrictionCurve swfc = new WheelFrictionCurve();
			swfc.asymptoteSlip = collider.sidewaysFriction.asymptoteSlip;
			swfc.extremumSlip = collider.sidewaysFriction.extremumSlip;
			swfc.extremumValue = collider.sidewaysFriction.extremumValue;
			swfc.asymptoteValue = collider.sidewaysFriction.asymptoteValue;
			swfc.stiffness = stiffness;

			collider.forwardFriction = wfc;
			collider.sidewaysFriction = swfc;
		}

	}
	//Test gui, showing current parameters
	void OnGUI() {
		GUI.Box(new Rect(10,10,250,200), "Car values");
		GUI.Label(new Rect(20,30,250,100),"rpm " + rpm.ToString());
		GUI.Label(new Rect(20,50,250,100),"gear " + currentGear);
		GUI.Label(new Rect(20,70,250,100),"vlocity z " + transform.InverseTransformDirection(rigidbody.velocity).z);
		GUI.Label(new Rect(20,90,250,100),"Sideways slip wheel fl " + WheelFL.getSidewaysSlip());
		GUI.Label(new Rect(20,110,250,100),"All wheels grounded " + AllWheelsGrounded());
		GUI.Label(new Rect(20,130,250,100),"Wheel fl rpm " + (WheelFL.collider.rpm));
		GUI.Label(new Rect(20,150,250,100),"Motor torque " + motorTorque);
		GUI.Label(new Rect(20,170,250,100),"Engaged delay " + engagedDelay);

	}
	//Car wheels
	[SerializeField]
	private Wheel WheelFL;
	[SerializeField]
	private Wheel WheelFR;
	[SerializeField]
	private Wheel WheelRL;
	[SerializeField]
	private Wheel WheelRR;
	public steeringWheelAnim aim;
	//Physical parameters of the car
	public float wheelRadius = 0.4f;
	public float suspDistance = 0.3f;
	public float suspDamper = 200.0f;
	public float suspSpringFront = 9000.0f;
	public float suspSpringRear = 9000.0f;
	public float slipValue = 0.8f;
	public float maxTurn = 37;
	public float minTurn = 17;
	//Car gears and other
	//gear-related ratios
	public float[] gearRatios = {3.2f, 2.1f, 1.4f, 1.0f, 0.77f};
	public float[] changGears = {2500.0f, 2500.0f, 2500.0f, 3000.0f};
	public float[] throttleLimit = {};
	public float reverseRatio = 1.4f;
	public float diffRatio = 2.42f;
	public float transmissionEff = 0.7f;
	public float frontalArea = 2.5f;
	public float sideArea = 3.5f;
	public float dragCoef = 1.62f;
	public float rollingResist = 0.01f;
	public float engBrake = 0.54f;
	public float brakeTorue = 1000.0f;
	public float motorTorque = 0.0f;
	//Engine torque curve, torque
	//based on engine rpm
	public AnimationCurve EngineTorqueCurve;
	//Friction curves
	private WheelFrictionCurve frontFriction;
	private WheelFrictionCurve sidewaysFriction;
	private WheelFrictionCurve brakeFriction;
	private WheelFrictionCurve ebrakeFriction;
	private WheelFrictionCurve ebrakeFwdFriction;
	//Center of mass
	public Transform centerOfMass;
	//Car inputs
	private float steerValue = 0.0f; 
	private float forwardValue = 0.0f;
	private float reverseValue = 0.0f;
	private float handbrakeValue = 0.0f;
	private float overallValue = 0.0f;
	private bool engaged = false;
	private float engagedDelay = 0.0f;
	//Gear change timer
	private float gearChange = 0.0f;
	//Current car parameters
	public float maxSpeed = 150.0f;
	public float rpm = 1000.0f;
	public int currentGear = 0;
	private bool eBraking = false;
	//Skid parameters
	public bool isSkidding = false;
	public float skidRate = 10.0f;
	//Driving limitations
	private bool canSteer = true;
	private bool canDrive = true;
	private bool isStarted = false;
	//Wheels stiffness override
	//dictionary in form of
	//objectname-stiffness
	//where objectname is name of
	//wheel object
	//and stiffness is overriden
	//stiffness of that wheel
	public Dictionary<string,float> wheelsStiffness = new Dictionary<string, float>();
	// Use this for initialization
	private float afterAir = 0.0f;
	private float afterAirThrottle = 1.0f;

	void Start () {
		maxSpeed = KmsIntoMs(maxSpeed);
		//Setting up wheels
		//and friction curves
		SetupWheels();
		//Setting up center of mass
		//Edy approves
		rigidbody.centerOfMass = new Vector3(0.0f,0.0f,-1.3f);
	}
	// Update is called once per frame
	void Update () {
		//Ввод даных руками
		//Manual input
		ManualInput();
		//Test code
		//Debug.Log("Right " + wheelsStiffness[WheelFL.getObjectName()]);
		//Debug.Log("Left " + wheelsStiffness[WheelFR.getObjectName()]);
		//Obsolete code
		//SendInput(this);
	}
	void ManualInput() {
		//Taking inputs from keyboard
		steerValue = Mathf.Clamp(Input.GetAxis("Horizontal"), -1, 1);
		forwardValue = Mathf.Clamp(Input.GetAxis("Vertical"), 0, 1);
		reverseValue = Mathf.Clamp(Input.GetAxis("Vertical"), -1, 0);
		overallValue = forwardValue + reverseValue;
		handbrakeValue = Mathf.Clamp(Input.GetAxis("Jump"), 0, 1);

		float relSteer = CalculateSteer();

		WheelFL.setSteer(steerValue * relSteer);
		WheelFR.setSteer(steerValue * relSteer);
		aim.NeedAngle = -1.0f * steerValue * relSteer * 5;
		//Test code
		//WheelFL.collider.steerAngle = steerValue * 5.0f;
		//WheelFR.collider.steerAngle = steerValue * 5.0f;
		//Debug.Log(steerValue);
		//if(overallValue > 0 && currentGear == 0) currentGear = 1;
	}

	float CalculateSteer() {
		float velocity = transform.InverseTransformDirection(rigidbody.velocity).z;
		float relSteer = 0;
		
		if(velocity < 10.0f) relSteer = maxTurn;
		else {
			float toLerp = (velocity - 10.0f)/ 40.0f;
			relSteer = minTurn / toLerp;
		}
		relSteer = Mathf.Clamp(relSteer,minTurn,maxTurn);
		return relSteer;
	}

	private float flWheelDealy = 0.0f;
	private float frWheelDealy = 0.0f;
	private float rlWheelDealy = 0.0f;
	private float rrWheelDealy = 0.0f;

	void SetupWheelsTorqueDelay() {
		if(!WheelFL.getGrounded()) flWheelDealy = 0.02f;
		if(!WheelFR.getGrounded()) frWheelDealy = 0.02f;
		if(!WheelRL.getGrounded()) rlWheelDealy = 0.02f;
		if(!WheelRR.getGrounded()) rrWheelDealy = 0.02f;
	}

	void FixedUpdate() {
		rigidbody.AddForce(new Vector3(0f,0f,2000.0f));
		AntiRoll();
		//Debug.Log(overallValue);
		//Test code
		//WheelFL.setSteer(0f);
		//WheelFR.setSteer(0f);
		//WheelRL.setSteer(0f);
		//WheelRR.setSteer(0f);
		//Debug.Log(WheelFL.collider.motorTorque);
		//Vector3 relVelocity = new Vector3(0,0,0);
		//if (AllWheelsGrounded())
		SetupWheelsTorqueDelay();
		Vector3	relVelocity = transform.InverseTransformDirection(rigidbody.velocity);
		//else {
			/*gearChange = 0.60f;
			if(!WheelFL.getGrounded())
				WheelFL.ApplyMotorTorque(0);
			if(!WheelFR.getGrounded())
				WheelFR.ApplyMotorTorque(0);
			if(!WheelRL.getGrounded())
				WheelRL.ApplyMotorTorque(0);
			if(!WheelRR.getGrounded())
				WheelRR.ApplyMotorTorque(0);
			rpm = 1000;
			afterAirThrottle = 0.5f;
			afterAir = 2.0f;
			ChangeGear(relVelocity);
			ChangeGear(relVelocity);
			return;*/
		//}
		//Test code
		//Debug.Log(rigidbody.velocity);
		//Decreasing timer of
		//gear change
		//this timer models
		//time, when motor torque
		//is not applying
		//due to pressed
		//clutch pedal
		if(gearChange > 0) {
			gearChange -= Time.deltaTime;
		}
		if(engagedDelay > 0) {
			engagedDelay -= Time.deltaTime;
		}
		//In case of neutral
		//starting the movement
		if(currentGear == 0) {
			ApplyBrakeTorque(relVelocity);
			if (overallValue == 0){
				WheelFL.ApplyMotorTorque(0);
				WheelFR.ApplyMotorTorque(0);
				WheelRL.ApplyMotorTorque(0);
				WheelRR.ApplyMotorTorque(0);
			}

			else {
				ChangeGear(relVelocity);
			}
			//Needs further investigation
			//rigidbody.velocity = new Vector3(0, 0, 0);
		}
		else {
			//Updating friction in case
			//of hitting specific surface
			//or moving backwards
			//or something else
			UpdateFriction(relVelocity);
			//Applying resistence forces
			//such as rolling resistance
			//and air drag
			ApplyResistance(relVelocity);
			//Driving if ther is need to drive
			if(overallValue != 0) {
				//The grand culmination
				if(gearChange <= 0 ) ApplyMotorTorque();
			}
			//Braking if there is need to brake
			ApplyBrakeTorque(relVelocity);
			//Calculating engine rpm
			//(revolutions per minute)
			//if(AllWheelsGrounded())
			CalculateRPM(relVelocity);
			//else 
			//rpm = 1000;
			//Changing gear in case there are
			//too much revolutions per minute
			ChangeGear(relVelocity);
			//Test code
			//Debug.Log(currentGear + " " + Time.time);
		}
	}
	//Prototype for really far future
	private void ArtInput() {
	}
	//Updating friction
	private void UpdateFriction(Vector3 relVelocity) {
		//Test code
		//Debug.Log(WheelFL.collider.forwardFriction.stiffness);
		//Debug.Log(wheelsStiffness[WheelFL.getObjectName()]);
		if(eBraking) {
			//Setting rear wheels to deadlock
			//in case of e-braking
			WheelRL.ApplyBrakeTorque(10000);
			WheelRR.ApplyBrakeTorque(10000);
			WheelRL.ChangeSidewaysFriction(ebrakeFriction);
			WheelRR.ChangeSidewaysFriction(ebrakeFriction);
			//Setting front wheels friction
			//to the e-braking friction
			WheelFL.ChangeSidewaysFriction(ebrakeFwdFriction);
			WheelFR.ChangeSidewaysFriction(ebrakeFwdFriction);

		}
		else {
			//If not e-braking
			//setting default sideways friction
			WheelFL.ChangeSidewaysFriction(sidewaysFriction);
			WheelFR.ChangeSidewaysFriction(sidewaysFriction);
			WheelRL.ChangeSidewaysFriction(sidewaysFriction);
			WheelRR.ChangeSidewaysFriction(sidewaysFriction);
			//If braking, friction changes
			//torque lessens, and friction curve
			//should become brakeFriction
			//instead of frontFriction
			if (Mathf.Sign(overallValue) != Mathf.Sign(relVelocity.z)) {
				WheelFL.ChangeForwardFriction(brakeFriction);
				WheelFR.ChangeForwardFriction(brakeFriction);
				WheelRL.ChangeForwardFriction(brakeFriction);
				WheelRR.ChangeForwardFriction(brakeFriction);

			}
			//If not braking, friction curve remains
			//frontFriction with a lot of torque
			else {
				WheelFL.ChangeForwardFriction(frontFriction);
				WheelFR.ChangeForwardFriction(frontFriction);
				WheelRL.ChangeForwardFriction(frontFriction);
				WheelRR.ChangeForwardFriction(frontFriction);

			}
			//Updating stiffness in case of 
			//specific surafce
			WheelFL.UpdateStiffness(wheelsStiffness[WheelFL.getObjectName()]);
			WheelFR.UpdateStiffness(wheelsStiffness[WheelFR.getObjectName()]);
			WheelRL.UpdateStiffness(wheelsStiffness[WheelRL.getObjectName()]);
			WheelRR.UpdateStiffness(wheelsStiffness[WheelRR.getObjectName()]);

		}

	}

	private void ApplyResistance(Vector3 relVelocity) {
		//Drag vectors
		//squared speed and
		//relative drag
		//calculate by the formula 
		//of the relative drag
		Vector3 relDrag, sqrDrag;
		sqrDrag = new Vector3( -relVelocity.x * Mathf.Abs(relVelocity.x), 
		                      -relVelocity.y * Mathf.Abs(relVelocity.y), 
		                      -relVelocity.z * Mathf.Abs(relVelocity.z));
		//Lerp lerp lerp lerp lerp lerp
		//The higher speed - higer air drag
		//at the point of 80 km/h it reaches
		//maximum value of the squared velocity
		relDrag = Vector3.Lerp(-relVelocity, sqrDrag, Mathf.Clamp01(GetCurrentSpeed() / 80.0f));
		//Test code
		//relDrag = sqrDrag;
		float airDensity = 1.2041f;
		float CDrag = 0.5f * frontalArea * airDensity * dragCoef;
		//Applying drag force to the car
		//z - front of the car
		//x and y - sides, which
		//must be multyplied by 
		//side area multiplier
		//(float sideArea = 3.5f)
		relDrag.x *= CDrag * sideArea;
		relDrag.y *= CDrag * sideArea;
		relDrag.z *= CDrag;
		//Test code
		//Debug.Log("reldrag = " + transform.TransformDirection(relDrag) + " relvel = " + relVelocity);
		//Debug.Log(relVelocity);

		//Rolling resistance coeficient x GRAVITY
		float Crr = rollingResist * 9.81f;
		//Test code
		//Debug.Log(Crr);
		Vector3 RollingResistanceForce = -Mathf.Sign(relVelocity.z) * transform.forward * (Crr * rigidbody.mass);
		//Test code
		//Debug.Log("rellres = " + RollingResistanceForce);
		if(Mathf.Abs(relVelocity.z) < Crr) RollingResistanceForce *= 0.0f;
		//Applying resistance forces
		//when car moves
		//in oreder to prevent backwards
		//movement when car is standing still
		if(Mathf.Abs(relVelocity.z) > 0.01f) {
			//Debug.Log(transform.TransformDirection(relDrag));
			Vector3 inversed = transform.TransformDirection(relDrag);
			inversed.x *= 1.0f;
			inversed.y *= 1.0f;
			inversed.z *= 1.0f;
			//Debug.Log("reldrag = " + inversed + " relvel = " + relVelocity + " " + Time.time);
			rigidbody.AddForce(inversed, ForceMode.Impulse);
			//Debug.Log(relVelocity.z);
			//Debug.Log("resist = " + RollingResistanceForce);
			//Debug.Log("resist = " + RollingResistanceForce + " forward =" + transform.forward);
			rigidbody.AddForce(RollingResistanceForce, ForceMode.Impulse);
		}
	}
	//Method for applying motor torque
	//based on engine rpm and gears system
	private void ApplyMotorTorque() {
		float motorTorque = 0.0f;
		//Clamping rpm
		//to rmp > 1000
		float nRpm = Mathf.Abs(rpm);
		if(rpm < 1000 || gearChange > 0) nRpm = 1000;
		//Evaluating engine torque
		//based on given engine rpm
		//using the magic
		//Evaluate method from
		//Animation curve
		float maxEngineTorque = EngineTorqueCurve.Evaluate(nRpm);
		//Moving forward
		if(overallValue > 0 && currentGear >= 0) {
			float engineTorque = maxEngineTorque * overallValue;
			//Calculating final motor torque
			if (engagedDelay <= 0)
				motorTorque = engineTorque * gearRatios[currentGear - 1] * diffRatio * transmissionEff * throttleLimit[currentGear - 1];
			else
				motorTorque = engineTorque * gearRatios[currentGear - 1] * diffRatio * transmissionEff * 0.4f;
			this.motorTorque = motorTorque;
			//Froward right wheel
			if(AllWheelsGrounded())
				WheelFR.ApplyMotorTorque(motorTorque);
			else {
				//WheelFR.ApplyMotorTorque(0);
				//WheelFR.ApplyBrakeTorque(motorTorque);
				//gearChange = 0.60f;
			}
				//WheelFR.ApplyBrakeTorque(0);
			//Froward left wheel
			if(AllWheelsGrounded())
				WheelFL.ApplyMotorTorque(motorTorque); 
			else {
				//WheelFL.ApplyMotorTorque(0);
				//WheelFL.ApplyBrakeTorque(motorTorque);
				//gearChange = 0.60f;
			} 
				//WheelFL.ApplyMotorTorque(0);
			//Rear left wheel
			if(AllWheelsGrounded())
				WheelRL.ApplyMotorTorque(motorTorque);
			else {
				//WheelRL.ApplyMotorTorque(0);
				//WheelRL.ApplyBrakeTorque(motorTorque);
				//gearChange = 0.60f;
			} 
				//WheelRL.ApplyMotorTorque(0);
			//Rear right wheel
			if(AllWheelsGrounded())
				WheelRR.ApplyMotorTorque(motorTorque);
			else {
				//WheelRR.ApplyMotorTorque(0);
				//WheelRR.ApplyBrakeTorque(motorTorque);
				//gearChange = 0.60f;
			}  
				//WheelRR.ApplyMotorTorque(0);
			Debug.Log("Torque applied " + motorTorque + " " + Time.time);
		}
		//Moving backwards
		else {
			if (currentGear < 0) {
				float engineTorque = maxEngineTorque * overallValue;
				motorTorque = engineTorque * reverseRatio * diffRatio * transmissionEff;
				//Debug.Log(motorTorque);
				WheelFR.ApplyMotorTorque(motorTorque);
				WheelFL.ApplyMotorTorque(motorTorque);
				WheelRL.ApplyMotorTorque(motorTorque);
				WheelRR.ApplyMotorTorque(motorTorque);
			}
		}
	}

	private void ApplyBrakeTorque(Vector3 relVeloity) {
		//If car moves, detecting,  if car also
		//is braking
		if(Mathf.Abs(relVeloity.z) > 0.01) {
			//Applying brake torque if braking
			if(Mathf.Sign(overallValue) != Mathf.Sign(relVeloity.z)) {
				//Debug.Log(brakeTorue + (engBrake * (rpm/60)));
				WheelFL.ApplyBrakeTorque(brakeTorue + (engBrake * (rpm/60)));
				WheelFR.ApplyBrakeTorque(brakeTorue + (engBrake * (rpm/60)));
				WheelRL.ApplyBrakeTorque(brakeTorue + (engBrake * (rpm/60)));
				WheelRR.ApplyBrakeTorque(brakeTorue + (engBrake * (rpm/60)));
			}
			//Not applying brake torque
			//if not braking
			else {
				WheelFL.ApplyBrakeTorque(0.0f);
				WheelFR.ApplyBrakeTorque(0.0f);
				WheelRL.ApplyBrakeTorque(0.0f);
				WheelRR.ApplyBrakeTorque(0.0f);
			}
		}
	}

	private float CalculateGroundedRPM() {
		if(WheelFL.getGrounded()) {
			return WheelFL.getWheelRpm();
		}
		if(WheelFR.getGrounded()) {
			return WheelFR.getWheelRpm();
		}
		if (WheelRL.getGrounded()) {
			return WheelRL.getWheelRpm();
		}
		if (WheelRR.getGrounded()) {
			return WheelRR.getWheelRpm();
		}
		return 0.0f;
	}

	//Method for rpm calculation
	private void CalculateRPM(Vector3 relVelocity) {
		float wheelRpm = 0.0f;
		//Obsolete code
		//float wheelRadius = 0.0f;
		//Calculating wheel rpm
		wheelRpm = CalculateGroundedRPM();
		//Obsolete code
		//wheelRadius = WheelFL.getWheelRadius();

		//Calculating rpm by formula
		//which was derived trustworthy physicist
		if (currentGear >= 0)
			rpm = wheelRpm * gearRatios[currentGear - 1] * diffRatio;
		else rpm = wheelRpm * reverseRatio * diffRatio;
		//Clamping rpm to rpm > 1000
		if(rpm < 1000) rpm = 1000;
		//Test code
		//Debug.Log(rpm + " " + Time.time);

		//Obsolete code
		//float wheelSpeed = (wheelRpm * 2.0f * Mathf.PI * wheelRadius) / 60.0f;

		//Detecting if any of the wheels
		//is skidding
		if(WheelFL.GetSkidding(skidRate) || 
		   WheelFR.GetSkidding(skidRate) || 
		   WheelRL.GetSkidding(skidRate) || 
		   WheelRR.GetSkidding(skidRate)) {
			//Set skidding boolean equal true if sideways slip
			//is greater than some value
			//that represents alot of slipping
			isSkidding = true;
		}
		else {
			//In any other case car isn't skidding
			isSkidding = false;
		}
	    /*Oblolete code
	    if(Mathf.Abs(relVelocity.z - wheelSpeed) > 1.5f)
			isSlipping = true;
		else 
			isSlipping = false;
			*/
	}

	private void ChangeGear(Vector3 relVelocity) {
		//Test code
		//Debug.Log("Here " + (gearChange));

		//If gear is not changing right now
		//detecting if it needs to be
		//changed
		if(gearChange <= 0) {
			//Starting movement
			//if not moving
			if(overallValue < 0 && currentGear >= 0) {
				//if(Mathf.Abs(relVelocity.z) < 0.01f)
				currentGear--;
				if(currentGear == 0) engaged = false;
				gearChange = 0.12f;
				return;

			}
			if(overallValue > 0 && currentGear <= 0) {
				//Test code
				//Debug.Log("First");
				Debug.Log("first " + Time.time);
				currentGear++;
				if (currentGear == 1 && engaged == false) {
					engaged = true;
					engagedDelay = 2.24f;
				}
				if(currentGear > 1 && engagedDelay > 0.0f) engagedDelay = 0.0f;
				gearChange = 0.12f;
				return;
			}
			//If moving detecting
			//if gear needs to be
			//increased or dercreased
			if(currentGear >= 1){
				//If there are too much rpm
				//(amount of needed rpm is 
				// given in changeGear array
				//n-1 element is rpm for n gear
				//to be changed
				if(currentGear < changGears.Length && rpm > changGears[currentGear - 1] && overallValue > 0) {
					currentGear++;
					if (currentGear == 1 && engaged == false) {
						engaged = true;
						engagedDelay = 2.24f;
					}
					if(currentGear > 1 && engagedDelay > 0.0f) engagedDelay = 0.0f;
					gearChange = 0.12f;
				}
				//If there is less than 1500 rpm
				//gear need to be changed
				else if(currentGear > 0 && ((rpm < 2000 && overallValue <= 0) || rpm < 1000)) {
					Debug.Log("decreased " + Time.time);
					currentGear--;
					if(currentGear == 0) engaged = false;
					gearChange = 0.12f;
				}
				return;
			}
		}

	}
	//Setting up wheels
	private void SetupWheels() {
		//Setting up friction curves
		SetupFrictionCurves();
		//Indicating that front wheels
		//are front
		WheelFL.setIsFront(true);
		WheelFR.setIsFront(true);
		//Indicating that rer wheels 
		//are rear
		WheelRL.setIsRear(true);
		WheelRR.setIsRear(true);
		//Setting front frictin curves
		//for all wheels
		WheelFL.ChangeForwardFriction(frontFriction);
		WheelFR.ChangeForwardFriction(frontFriction);
		WheelRL.ChangeForwardFriction(frontFriction);
		WheelRR.ChangeForwardFriction(frontFriction);
		//Setting sideways friction
		//for all wheels
		WheelFL.ChangeSidewaysFriction(sidewaysFriction);
		WheelFR.ChangeSidewaysFriction(sidewaysFriction);
		WheelRL.ChangeSidewaysFriction(sidewaysFriction);
		WheelRR.ChangeSidewaysFriction(sidewaysFriction);
		//Setting name of the object for all wheels
		WheelFL.UpdateObjectName();
		WheelFR.UpdateObjectName();
		WheelRL.UpdateObjectName();
		WheelRR.UpdateObjectName();
		//Assigning specific friction for all wheels
		wheelsStiffness.Add(WheelFL.getObjectName(),1.0f);
		wheelsStiffness.Add(WheelFR.getObjectName(),1.0f);
		wheelsStiffness.Add(WheelRL.getObjectName(),1.0f);
		wheelsStiffness.Add(WheelRR.getObjectName(),1.0f);
	
	}
	//Setting up friction curves
	private void SetupFrictionCurves() {
		//Friction curve of
		//forward movement
		frontFriction = new WheelFrictionCurve();
		frontFriction.extremumValue = 5000.0f;
		frontFriction.asymptoteValue = 500.0f;
		frontFriction.extremumSlip = 0.5f;
		frontFriction.asymptoteSlip = 2.0f;
		frontFriction.stiffness = 1.0f;
		//Friction curve of
		//sideways turns
		sidewaysFriction = new WheelFrictionCurve();
		sidewaysFriction.extremumValue = 400.0f;
		sidewaysFriction.asymptoteValue = 200.0f;
		sidewaysFriction.extremumSlip = 0.8f;
		sidewaysFriction.asymptoteSlip = 1.5f;
		sidewaysFriction.stiffness = 1.0f;
		//Friction curve of
		//braking
		brakeFriction = new WheelFrictionCurve();
		brakeFriction.extremumValue = 500.0f;
		brakeFriction.asymptoteValue = 250.0f;
		brakeFriction.extremumSlip = 1.0f;
		brakeFriction.asymptoteSlip = 2.0f;
		brakeFriction.stiffness = 1.0f;
		//Frictioon curve of 
		//blocked rear wheels
		//while handbraking
		ebrakeFriction = new WheelFrictionCurve();
		ebrakeFriction.extremumValue = 300.0f;
		ebrakeFriction.asymptoteValue = 150.0f;
		ebrakeFriction.extremumSlip = 1.0f;
		ebrakeFriction.asymptoteSlip = 2.0f;
		ebrakeFriction.stiffness = 0.1f;
		//Friction curve of
		//non-blocked front wheels
		//while handbraking
		ebrakeFwdFriction = new WheelFrictionCurve();
		ebrakeFwdFriction.extremumValue = 300.0f;
		ebrakeFwdFriction.asymptoteValue = 150.0f;
		ebrakeFwdFriction.extremumSlip = 1.0f;
		ebrakeFwdFriction.asymptoteSlip = 2.0f;
		ebrakeFwdFriction.stiffness = 2.0f;
	}
	//Converting kilometers per second
	//into meters per second
	private float KmsIntoMs(float kilo) {
		return kilo / 3.6f;
	}
	//Getting current car speed
	//needs further investigation
	private float GetCurrentSpeed() {
		return rigidbody.velocity.magnitude * 3.6f;
	}

	private bool AllWheelsGrounded() {
		return WheelFL.getGrounded() && WheelFR.getGrounded() && WheelRL.getGrounded() && WheelRR.getGrounded();
	}

	//In case of emergency use this
	//Edy approves
	void AntiRoll() {
		WheelHit hit = new WheelHit();
		float left = 1.0f;
		float right = 1.0f;
		
		bool lGrnd = WheelFL.getGroundHit(out hit);
		bool rGrnd = WheelFR.getGroundHit(out hit);
		
		if (lGrnd) {
			left = (-WheelFL.getTransform().InverseTransformPoint(hit.point).y - 0.4f ) / WheelFL.getSuspensionDistance();
		}
		if (rGrnd) {
			right = (-WheelFR.getTransform().InverseTransformPoint(hit.point).y - 0.4f ) / WheelFR.getSuspensionDistance();
		}
		float antiForce = (left - right) * 8000.0f;
		if (lGrnd)
			rigidbody.AddForceAtPosition(WheelFL.getTransform().right * -antiForce,
			                             WheelFL.getTransform().position); 
		if (rGrnd)
			rigidbody.AddForceAtPosition(WheelFR.getTransform().right * antiForce,
			                             WheelFR.getTransform().position); 
	}
}
