using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarControllerNEW : MonoBehaviour {

	[SerializeField] private WheelCollider WheelFR;
	[SerializeField] private WheelCollider WheelFL;
	[SerializeField] private WheelCollider WheelBL;
	[SerializeField] private WheelCollider WheelBR;

	[SerializeField] private Transform TWheelFR;
	[SerializeField] private Transform TWheelFL;
	[SerializeField] private Transform TWheelBL;
	[SerializeField] private Transform TWheelBR;

	[SerializeField] private Transform CenterOfMass;

	[SerializeField] float maxTorque;
	private bool breaked = false; //Акитвность ручника
	[SerializeField] float maxBreakedTorque = 200; //Максимальная сила ручника
	//Стартовые фрикшены
	private float SidewayFrictionOfCar; 
	private float ForwardFrictionOfCar; 	
	//Тормозные фрикшены
	private float maxSlipSidewayFrictionOfCar; 
	private float maxSlipForwardFrictionOfCar; 
	private float slipSidewayFrictionOfCar; 
	private float slipForwardFrictionOfCar; 

	private int currentTransmission = 1;

	private float wheelOffset = 0.3f; //Высота колеса при падении на спину и тд

//	[SerializeField] private float minSteer = 1; //Минимальная скорость поворота (на большой скорости
//	[SerializeField] private float maxSteer = 30; //Максимальный поворот колеса (на низкой скорости)
	[SerializeField] private float maxAcceleration = 25; //Максимальный крутящийся момент (ускорение)
	[SerializeField] private float maxBrake = 50; //Максимальные тормоза

	[SerializeField] float minSpeed = 50; //Минимальная скорость
	[SerializeField] float currentSpeed; //Текущая скорость
	[SerializeField] float maxSpeed = 150; //Максимальная скорость
	[SerializeField] float maxReverseSpeed = 50; //Максимальная скорость назад

	public class Wheel{

		public Wheel(Transform wheelTransform,WheelCollider col,Vector3 wheelStartPos){
			this.wheelTransform=wheelTransform;
			this.col=col;
			this.wheelStartPos=wheelStartPos;
		}

		public Transform wheelTransform;
		public WheelCollider col;
		public Vector3 wheelStartPos;
		public float rotation = 0.0f;
	}

	private Wheel[] wheels;
	


	// Use this for initialization
	void Start () {
		rigidbody.centerOfMass=CenterOfMass.localPosition;

		ForwardFrictionOfCar = WheelBR.forwardFriction.stiffness;
		SidewayFrictionOfCar = WheelBR.sidewaysFriction.stiffness;
		
		maxSlipForwardFrictionOfCar = 0.04f;
		maxSlipSidewayFrictionOfCar = 0.01f;


		wheels = new Wheel[4];
		wheels[0] = new Wheel(TWheelBL,WheelBL,TWheelBL.localPosition);
		wheels[1] = new Wheel(TWheelBR,WheelBR,TWheelBR.localPosition);
		wheels[2] = new Wheel(TWheelFL,WheelFL,TWheelFL.localPosition);
		wheels[3] = new Wheel(TWheelFR,WheelFR,TWheelFR.localPosition);
	}
	float curentSteer;
	void FixedUpdate () {
	//	rigidbody.centerOfMass=new Vector3(Input.GetAxis("Horizontal")*0.4f,CenterOfMass.localPosition.y,CenterOfMass.localPosition.z);

		float acceleration = 0;
		float steer = 0;
		currentSpeed = Mathf.Round(2*Mathf.PI*WheelBL.radius*WheelBL.rpm*60/1000);// Считаем текущую скорость



		/////Считаем текущий уровень максимального поворота колеса в зависимости от скорости
//		float speedFactor = rigidbody.velocity.magnitude/45;
		//curentSteer = Mathf.Lerp(maxSteer,minSteer,speedFactor);
		curentSteer=InterpolateAngle(currentSpeed);
		/////

		////Считаем текущий уровень заноса при торможении
		if(!breaked){
			if(currentSpeed>0){
				slipForwardFrictionOfCar=(ForwardFrictionOfCar+maxSlipForwardFrictionOfCar)-(ForwardFrictionOfCar*(currentSpeed/maxSpeed));
				slipSidewayFrictionOfCar=(SidewayFrictionOfCar+maxSlipSidewayFrictionOfCar)-(SidewayFrictionOfCar*(currentSpeed/maxSpeed));
			}}
		////

		////Считаем текущий уровень заноса без тормозов
		if(!breaked){
			if(currentSpeed!=0){
				InterpolateFrictionOnMove(currentSpeed,out ForwardFrictionOfCar,out SidewayFrictionOfCar);
			}}
		////


		acceleration = Input.GetAxis("Vertical"); 
		steer =curentSteer*Input.GetAxis("Horizontal");

		Handbrakes();
		Move(acceleration,steer);
		transmission();

		if(currentSpeed>=maxSpeed && currentTransmission<3){
			currentTransmission++;
		}
		else{if(currentSpeed<minSpeed && currentTransmission>1){
				currentTransmission--;
			}
		}






	}


	// Update is called once per frame
	void Update () {
		UpdateWheels();
	}

	private void UpdateWheels(){
		foreach (Wheel w in wheels){
			/////////////ПОДВЕСКА
			WheelHit hit; 
			Vector3 lp = w.wheelTransform.localPosition; 
			if(w.col.GetGroundHit(out hit)){
				lp.y -= Vector3.Dot(w.wheelTransform.position - hit.point, transform.up) - w.col.radius; 
			}else{
				lp.y = w.wheelStartPos.y - wheelOffset;
			}
			w.wheelTransform.localPosition = lp; 
			//////////////
		
			/////////////КРУЧЕНИЕ + ПОВОРОТ КОЛЕС
			w.rotation = Mathf.Repeat(w.rotation + Time.deltaTime * w.col.rpm * 360.0f / 60.0f, 360.0f); 
			w.wheelTransform.localRotation = Quaternion.Euler(w.rotation, w.col.steerAngle, 0); 
			/////////////
		}
	}

	private void Move(float acceleration, float steer){ // ДВИЖКНИЕ МАШИНЫ


			WheelFL.steerAngle =steer; //Поворот передних колес
			WheelFR.steerAngle =steer; //Поворот передних колес


		if(acceleration !=0 && (currentSpeed<maxSpeed && currentSpeed>-maxReverseSpeed)){
			WheelBL.brakeTorque = 0;
			WheelBR.brakeTorque = 0;
			WheelBL.motorTorque = acceleration*maxAcceleration; //Ускорение на задние колеса
			WheelBR.motorTorque = acceleration*maxAcceleration; //Ускорение на задние колеса
		}
		else{
			WheelBL.brakeTorque = maxBrake; //Естественное торможение
			WheelBR.brakeTorque = maxBrake; //Естественное торможение 
		}


		}

	private void Handbrakes(){//РУЧНИК


		if((Input.GetButton("Jump"))||((Input.GetAxis("Vertical")<0 && currentSpeed>0)||(Input.GetAxis("Vertical")>0 && currentSpeed<0))){
			breaked=true;
		}
		else{
			breaked=false;
		}





		if(breaked){
			WheelFR.brakeTorque=maxBreakedTorque;
			WheelFL.brakeTorque=maxBreakedTorque;
			//WheelBR.motorTorque=0;
			//WheelBL.motorTorque=0;
			SetSlip(WheelBL,slipForwardFrictionOfCar,slipSidewayFrictionOfCar/3);
			SetSlip(WheelBR,slipForwardFrictionOfCar,slipSidewayFrictionOfCar/3);
			SetSlip(WheelFL,slipForwardFrictionOfCar,slipSidewayFrictionOfCar/1.5f);
			SetSlip(WheelFR,slipForwardFrictionOfCar,slipSidewayFrictionOfCar/1.5f);
		}
		else{
			WheelFR.brakeTorque=0;
			WheelFL.brakeTorque=0;
			SetSlip(WheelBL,ForwardFrictionOfCar,SidewayFrictionOfCar);
			SetSlip(WheelBR,ForwardFrictionOfCar,SidewayFrictionOfCar);
			SetSlip(WheelFL,ForwardFrictionOfCar,SidewayFrictionOfCar);
			SetSlip(WheelFR,ForwardFrictionOfCar,SidewayFrictionOfCar);
		}
		
	}



	private void SetSlip(WheelCollider wheel, float CurrentForwardFriction,float CurrentSidewayFriction){//Замена Фрикшена
		WheelFrictionCurve sf = wheel.sidewaysFriction;
		WheelFrictionCurve ff = wheel.forwardFriction;
		ff.stiffness = CurrentForwardFriction;
		sf.stiffness = CurrentSidewayFriction;
		wheel.forwardFriction = ff;
		wheel.sidewaysFriction = sf;
	}

	private float InterpolateAngle(float Speed){
		if(Speed==0)return 0;

		List<Vector2> Points =new List<Vector2>();

		//ТОЧКИ СООТНОШЕНИЯ СКОРОСТЬ/УГОЛ ПОВОРОТА
		Points.Add(new Vector2(-100,10));
		Points.Add(new Vector2(-60,20));
		Points.Add(new Vector2(-20,30));
		Points.Add(new Vector2(0,30));
		Points.Add(new Vector2(70,20));
		Points.Add(new Vector2(150,10));
		Points.Add(new Vector2(160,5));

		Vector2 Xi= new Vector2();
		Vector2 Xj= new Vector2();

		for(int i=0;i<=Points.Count-2;i++){
			if(Points[i].x<=Speed && Points[i+1].x>=Speed){
				Xi=Points[i];
				Xj=Points[i+1];
				break;
			}
		}
		float Angle = ((Xi.y*(Xj.x-Speed))+(Xj.y*(Speed-Xi.x)))/(Xj.x-Xi.x);
		return Angle;
	}

	private void InterpolateFrictionOnMove(float Speed,out float ForwardFrict,out float SidewaysFrict){
		List<Vector2> Forward =new List<Vector2>();
		List<Vector2> Sideways =new List<Vector2>();



		//ТОЧКИ СООТНОШЕНИЯ СКОРОСТЬ/ЗАНОС
		Forward.Add(new Vector2(-60,0.5f));
		Forward.Add(new Vector2(0,0.5f));
		Forward.Add(new Vector2(160,0.001f));

		Sideways.Add(new Vector2(-60,0.1f));
		Sideways.Add(new Vector2(0,0.5f));
		Sideways.Add(new Vector2(160,0.002f));


		Vector2 Xi= new Vector2();
		Vector2 Xj= new Vector2();
			
		for(int i=0;i<=Forward.Count-2;i++){
			if(Forward[i].x<=Speed && Forward[i+1].x>=Speed){
				Xi=Forward[i];
				Xj=Forward[i+1];
				break;
			}
		}
		ForwardFrict = ((Xi.y*(Xj.x-Speed))+(Xj.y*(Speed-Xi.x)))/(Xj.x-Xi.x);

		for(int i=0;i<=Sideways.Count-2;i++){
			if(Sideways[i].x<=Speed && Sideways[i+1].x>=Speed){
				Xi=Sideways[i];
				Xj=Sideways[i+1];
				break;
			}
		}
		SidewaysFrict = ((Xi.y*(Xj.x-Speed))+(Xj.y*(Speed-Xi.x)))/(Xj.x-Xi.x);

		}

	private void transmission(){
		switch(currentTransmission){
		case(1): maxAcceleration=30; maxSpeed=50; minSpeed=0;  break;
		case(2): maxAcceleration=25; maxSpeed=100; minSpeed=40; break;
		case(3): maxAcceleration=20; maxSpeed=150; minSpeed=90; break;
		}
	}


	/*void OnGUI(){
		GUI.Box(new Rect(10,10,150,30),"Speed - "+currentSpeed);
		GUI.Box(new Rect(10,40,150,30),"BF "+WheelBL.forwardFriction.stiffness);
		GUI.Box(new Rect(10,80,150,30),"BS "+WheelBL.sidewaysFriction.stiffness);
		GUI.Box(new Rect(10,120,150,30),"FF "+WheelFL.forwardFriction.stiffness);
		GUI.Box(new Rect(10,160,150,30),"FS "+WheelFL.sidewaysFriction.stiffness);
		GUI.Box(new Rect(10,200,150,30),"Transmisson "+currentTransmission);
		GUI.Box(new Rect(10,230,150,30),"Curent Steer "+curentSteer);
	}*/





}