using UnityEngine;
using System.Collections;

public class Control : MonoBehaviour {

	// Контролируемые объекты
	
	public Wheel WheelFL;
	public Wheel WheelFR;
	public Wheel WheelRL;
	public Wheel WheelRR;
	public AntiRollBar AntiRollFront;
	public AntiRollBar AntiRollRear;

	public Transform CenterOfMass;

	// Кривые трения колес
	public Wheel.FrictionCurve ForwardWheelFriction = new Wheel.FrictionCurve(800f,5f,300f,0f);
	public Wheel.FrictionCurve SidewaysWheelFriction = new Wheel.FrictionCurve(1900f,2,600f,0f);
	public bool optimized = false;	// В режиме оптимизации параметров кривых трения не может быть изменен в режиме реального времени (не реагирует должным образом).
	
	// Входные параметры
	public bool readUserInput;			// Читает пользовательский ввод сам вместо внешне расположенный зависимости ввода. Этот флаг отключена автоматически, когда автомобиль управляется из скрипта Carmain. (для меня, потом убрать)
	public bool reverseRequiresStop = false;	// Функция доступна только при readUserInput верно. Если автомобиль управляется из скрипта Carmain то общая флаг CarMain.reverseRequiresStop применяется. (тоже для меня, тоже убрать)
	public float steerInput = 0.0f; //Ввод поворота
	public float motorInput = 0.0f; //Ввод "газа"
	public float brakeInput = 0.0f; //Ввод тормоза (стрелка назад)
	public float handbrakeInput = 0.0f; //Ввод ручника (пробел)
	public int gearInput = 1;

	// 	Рабочие параметры и поведение
	
	public float steerMax = 45.0f;
	public float motorMax = 1.0f;			// Максимальное плавное ускорение до TC
	public float brakeMax = 1.0f;			// Максимальное плавное торможении перед ABS  
	
	public float autoSteerLevel = 1.0f;	// ESP
	public bool autoMotorMax = true;	// TC
	public bool autoBrakeMax = true;	// ABS
	public float antiRollLevel = 1.0f;	// Стабилизаторы поперечной устойчивости

	public float motorPerformancePeak = 5.0f;		// м/с  максимального ускорения в режиме 4х4. В одном колесе необходимо поднять motorForceFactor
	public float motorForceFactor = 1.0f;
	public float motorBalance = 0.5f;				// 0.0 = передний привод, задний привод 1,0 = 0,5 = 4x4.
	public float brakeForceFactor = 1.5f;
	public float brakeBalance = 0.5f;				// 0.0 = все вместе, все позади 1.0 = 0.5 = на том же уровне.
	
	public float sidewaysDriftFriction = 0.35f;
	public float staticFrictionMax = 1500.0f;		// Управление трением (Wheel)
	public float frontSidewaysGrip = 1.0f;
	public float rearSidewaysGrip = 1.0f;

	// Параметры настройки
	
	public bool serviceMode = false;			// Если активировна параметры сцеплиния, будут такими как их назначили. Позволяет тестировать кривую трения
	
	public float airDrag = 3 * 2.2f;		// Коэффициент аэродинамического сопротивления (например, 0,30 для корвета) * Переднюю Площадь
	public float frictionDrag = 30.0f;	// 30 кратное сопротивление воздуха (это будет более весомым airDrag от 30 м/с = 100 км/ч)
	
	public float rollingFrictionSlip = 0.075f;
	
	// ЭКСПЕРИМЕНТАЛЬНАЯ V3 РЕЖИМ:
	// - Антипробуксовочная уменьшает по мере приближения к максимальной скорости. Достижение этой тяги является 0.
	
	public bool tractionV3 = false;
	public float maxSpeed = 40.0f;


	// private переменные + телеметрия
	
	private bool m_brakeRelease = false;
	
	private float m_motorSlip = 0.0f;
	private float m_brakeSlip = 0.0f;
	
	private float m_frontMotorSlip = 0.0f;
	private float m_frontBrakeSlip = 0.0f;
	private float m_rearMotorSlip = 0.0f;
	private float m_rearBrakeSlip = 0.0f;
	
	private float m_steerL = 0.0f;
	private float m_steerR = 0.0f;
	private float m_steerAngleMax = 0.0f;
	
	private float m_maxRollAngle = 45.0f;

	public string getGear(){ return gearInput>0? "D" : gearInput<0? "R" : "N"; }
	public float getMotor(){ return motorInput; }
	public float getBrake(){ return brakeInput; }
	public float getHandBrake(){ return handbrakeInput; }
	
	public float getSteerL(){ return m_steerL; }
	public float getSteerR(){ return m_steerR; }

	public float getMaxRollAngle(){ return m_maxRollAngle; }

	public void OnEnable ()
	{
		ApplyEnabled(WheelFL, true);
		ApplyEnabled(WheelFR, true);
		ApplyEnabled(WheelRL, true);
		ApplyEnabled(WheelRR, true);
	}
	
	public void OnDisable ()
	{
		ApplyEnabled(WheelFL, false);
		ApplyEnabled(WheelFR, false);
		ApplyEnabled(WheelRL, false);
		ApplyEnabled(WheelRR, false);
	}


	// Use this for initialization
	void Start () {
		// Центр Масс (CoM)
		// В LocalScale нужно применить любое масштабирование которое было применено к модели.
		
		if (CenterOfMass) rigidbody.centerOfMass =new Vector3(CenterOfMass.localPosition.x * transform.localScale.x, CenterOfMass.localPosition.y * transform.localScale.y, CenterOfMass.localPosition.z * transform.localScale.z);	
		
		// Боковая точка баланса (по оценке)
		
		WheelCollider WheelC = WheelFL.GetComponent<WheelCollider>();
		Vector3 V = rigidbody.centerOfMass - transform.InverseTransformPoint(WheelC.transform.position);	
		float h = Mathf.Abs((V.y + WheelC.radius + WheelC.suspensionDistance/2.0f) * transform.localScale.y);
		float l = Mathf.Abs(V.x * transform.localScale.x);
		m_maxRollAngle = Mathf.Atan2(l, h) * Mathf.Rad2Deg;
		
		// Другая информация
		
		rigidbody.maxAngularVelocity = 10;
		rigidbody.useConeFriction = false;
		
		// Оптимизация параметров, если это необхадимо
		
		if (optimized)
		{
			ApplyCommonParameters(WheelFL);
			ApplyCommonParameters(WheelFR);
			ApplyCommonParameters(WheelRL);
			ApplyCommonParameters(WheelRR);
			WheelFL.RecalculateStuff();
			WheelFR.RecalculateStuff();
			WheelRL.RecalculateStuff();
			WheelRR.RecalculateStuff();
		}
	}
	

	void FixedUpdate () {
		//!!!!!!!
		// 1. Применить параметры к колесам
		//!!!!!!!
		
		// Параметры колес устанавливаются каждый вызов FixedUpdate
		
		ApplyCommonParameters(WheelFL);
		ApplyCommonParameters(WheelFR);
		ApplyCommonParameters(WheelRL);
		ApplyCommonParameters(WheelRR);
		
		// Параметры для отдельных колес устанавливаются в соответствии с текущими настройками
		
		ApplyFrontParameters(WheelFL);
		ApplyFrontParameters(WheelFR);
		ApplyRearParameters(WheelRL);
		ApplyRearParameters(WheelRR);

		//!!!!!!!
		// 2. Применить привод / торможение
		//!!!!!!!
		
		m_motorSlip = motorInput * motorMax;
		m_brakeSlip = brakeInput * brakeMax;
		
		if (gearInput == 0) m_motorSlip = 0;
		else if (gearInput < 0) m_motorSlip = -m_motorSlip;
		
		// Баланс привода
		
		if (serviceMode)
		{
			m_frontMotorSlip = m_motorSlip;
			m_rearMotorSlip = m_motorSlip;
		}
		else
			if (motorBalance >= 0.5f)		// уменьшить передний привод
		{
			m_frontMotorSlip = m_motorSlip * (1.0f-motorBalance) * 2.0f;
			m_rearMotorSlip = m_motorSlip;
		}
		else	// Уменьшите задний привод
		{
			m_frontMotorSlip = m_motorSlip;
			m_rearMotorSlip = m_motorSlip * motorBalance * 2.0f;		
		}
		
		// Баланс тормозной
		
		if (serviceMode)
		{
			m_frontBrakeSlip = m_brakeSlip;
			m_rearBrakeSlip = m_brakeSlip;
		}
		else
			if (brakeBalance >= 0.5f)  // уменьшить передний тормоз
		{
			m_frontBrakeSlip = m_brakeSlip * (1.0f-brakeBalance) * 2.0f;
			m_rearBrakeSlip = m_brakeSlip;
		}
		else	// уменьшить задниq тормоз
		{
			m_frontBrakeSlip = m_brakeSlip;
			m_rearBrakeSlip = m_brakeSlip * brakeBalance * 2.0f;
		}
		
		ApplyTraction(WheelFL, m_frontMotorSlip, m_frontBrakeSlip, 0.0f);
		ApplyTraction(WheelFR, m_frontMotorSlip, m_frontBrakeSlip, 0.0f);
		ApplyTraction(WheelRL, m_rearMotorSlip, m_rearBrakeSlip, handbrakeInput);
		ApplyTraction(WheelRR, m_rearMotorSlip, m_rearBrakeSlip, handbrakeInput);

		//!!!!!!!
		// 3. Применить адрес
		//!!!!!!!
		
		// autoSteerLevel определяет угол, под которым колеса достигают максимальную силу поворота  (peakSlip).
		// Он получает любую ссылку на переднее колесо (например WheelFL)
		
		if (autoSteerLevel > 0.0f) 
		{
			var peakSlip = WheelFL.getSidewaysPeakSlip();
			var forwardSpeed = Mathf.Abs(transform.InverseTransformDirection(rigidbody.velocity).z * autoSteerLevel);
			
			if (forwardSpeed > peakSlip)
				m_steerAngleMax = 90.0f - Mathf.Acos(peakSlip / forwardSpeed) * Mathf.Rad2Deg;
			else
				m_steerAngleMax = steerMax;
		}
		else 
			m_steerAngleMax = steerMax;
		
		// Направление вращения каждого колеса рассчитывается для обновления дисплея осторожно
		// даже в замедленном темпе.
		
		WheelFL.getWheelCollider().steerAngle = m_steerL;
		WheelFR.getWheelCollider().steerAngle = m_steerR;

		//!!!!!!!
		// 4.Силы сопротивления
		//!!!!!!!
		
		//Аэродинамическое сопротивление (сопротивление) и сопротивление крену (rr)
		//
		// Fdrag = -Cdrag * V * |V|
		// Frr = -Crr * V
		
		// Cdrag =  0.5 * Cd * A * rho
		// 	Cd = коэффициент аэродинамичности (например на карвете 0.30)
		//	A = лобовая поверхности автомобиля
		//	rho = Плотность воздуха = 1.29 kg/m3
		//
		// Crr = 30 Иногда  Cdrag (так что это будет более важным Fdrag от 30 m/s = 100 Km/h)
		
		if (!serviceMode)
		{
			var Cdrag = 0.5f * airDrag * 1.29f ; // * (motorMax+1) / 2;
			var Crr = frictionDrag * Cdrag;
			
			var Fdrag = -Cdrag * rigidbody.velocity * rigidbody.velocity.magnitude;
			var Frr = -Crr * rigidbody.velocity;
			
			rigidbody.AddForce(Fdrag + Frr);
		}

		//!!!!!!!
		// 5. Дополнительные настройки техники
		//!!!!!!!
		
		// стабилизаторами поперечной устойчивости
		
		if (AntiRollFront) AntiRollFront.AntiRollFactor = antiRollLevel;
		if (AntiRollRear) AntiRollRear.AntiRollFactor = antiRollLevel;	

	}

	void Update()
	{
		// Считать пользовательский ввод, если readUserInput=true
		
		if (readUserInput)
			m_brakeRelease = SendInput(this, reverseRequiresStop, m_brakeRelease);
		
		// Рассчитать углы поворота колес для изменения направления видимых частей, в Update это будет мягче и без дерганий
		
		CalculateSteerAngles();
	}


	static bool SendInput(Control Car ,bool reverseRequiresStop,bool brakeRelease)
	{
		float GEARSPEEDMIN = 0.2f;
		// Obtener datos de la entrada
		
		float steerValue = Mathf.Clamp(Input.GetAxis("Horizontal"), -1, 1);
		float forwardValue = Mathf.Clamp(Input.GetAxis("Vertical"), 0, 1);
		float reverseValue = -1 * Mathf.Clamp(Input.GetAxis("Vertical"), -1, 0);
		float handbrakeValue = Mathf.Clamp(Input.GetAxis("Jump"), 0, 1);
		
		float speedForward = Vector3.Dot(Car.rigidbody.velocity, Car.transform.forward);
		float speedSideways = Vector3.Dot(Car.rigidbody.velocity, Car.transform.right);
		float speedRotation = Car.rigidbody.angularVelocity.magnitude;
		
		float speedAbs = speedForward * speedForward;
		speedSideways *= speedSideways;
		
		float motorInput = 0f;
		float brakeInput = 0f;
		
		if (reverseRequiresStop)
		{
			// Determinar la marcha a meter (adelante - detrбs)
			// Las marchas van en funciуn de las acciones sobre el eje vertical (forward - reverse)
			
			if (speedAbs < GEARSPEEDMIN && forwardValue == 0 && reverseValue == 0)
			{
				brakeRelease = true;
				Car.gearInput = 0;
			}
			
			if (brakeRelease)
				if (speedAbs < GEARSPEEDMIN)
			{
				// Cambio de marcha en parado
				
				if (reverseValue > 0) { Car.gearInput = -1; }
				if (forwardValue > 0) { Car.gearInput = 1; }
			}
			else
				if (speedSideways < GEARSPEEDMIN && speedRotation < GEARSPEEDMIN)
			{
				// Hacer que entre la marcha adecuada con el coche moviйndose por inercia longitudinalmente.
				// (no se cambia de marcha en desplazamientos laterales)
				
				if (speedForward > 0 && Car.gearInput <= 0 && (forwardValue > 0 || reverseValue > 0)) Car.gearInput = 1;
				if (speedForward < 0 && Car.gearInput >= 0 && (forwardValue > 0 || reverseValue > 0)) Car.gearInput = -1;
			}
			
			if (Car.gearInput < 0)
			{
				motorInput = reverseValue;
				brakeInput = forwardValue;
			}
			else
			{
				motorInput = forwardValue;
				brakeInput = reverseValue;
			}
			
			if (brakeInput > 0) brakeRelease = false;
		}
		else
		{
			// Modo adelante-atrбs sin detenerse (por Sagron)
			
			if (speedForward > GEARSPEEDMIN)
			{
				Car.gearInput = 1;
				motorInput = forwardValue;
				brakeInput = reverseValue;
			}
			else if (speedForward <= GEARSPEEDMIN && reverseValue > GEARSPEEDMIN)
			{
				Car.gearInput = -1;
				motorInput = reverseValue;
				brakeInput = 0;
			}
			else if (forwardValue > GEARSPEEDMIN && reverseValue <= 0)
			{
				Car.gearInput = 1;
				motorInput = forwardValue;
				brakeInput = 0;
			}
			else if (forwardValue > GEARSPEEDMIN)
				Car.gearInput = 1;
			else if (reverseValue > GEARSPEEDMIN)
				Car.gearInput = -1;
			else
				Car.gearInput = 0;	
			
			brakeRelease = false;
		}
		
		// Aplicar acciones sobre el coche actual
		
		Car.steerInput = steerValue;
		Car.motorInput = motorInput;
		Car.brakeInput = brakeInput;
		Car.handbrakeInput = handbrakeValue;	
		
		return brakeRelease;
	}




	public void ApplyEnabled(Wheel Wheel,bool enable)
	{
		// Необходимо сравнить с нулем в качестве OnDisable вызывается в конце приложения, а объект, возможно, больше не имеется в наличии, если ваша ссылка не является нулевым.
		
		if (Wheel != null) Wheel.enabled = enable;
	}

	public void  ApplyCommonParameters(Wheel Wheel)
	{
		Wheel.ForwardWheelFriction = ForwardWheelFriction;
		Wheel.SidewaysWheelFriction = SidewaysWheelFriction;
		Wheel.brakeForceFactor = brakeForceFactor;
		Wheel.motorForceFactor = motorForceFactor;
		Wheel.performancePeak = motorPerformancePeak;
		Wheel.sidewaysDriftFriction = sidewaysDriftFriction;
		Wheel.staticFrictionMax = staticFrictionMax;
		
		Wheel.serviceMode = serviceMode;
		Wheel.optimized = optimized;
	}

	public void ApplyFrontParameters(Wheel Wheel)
	{
		Wheel.sidewaysForceFactor = frontSidewaysGrip;
	}
	
	public void ApplyRearParameters(Wheel Wheel)
	{
		Wheel.sidewaysForceFactor = rearSidewaysGrip;
	}

	public void ApplyTraction(Wheel Wheel,float motorSlip,float brakeSlip,float handBrakeInput)
	{
		float slipPeak = Wheel.getForwardPeakSlip();
		float slipMax = Wheel.getForwardMaxSlip();
		
		WheelHit Hit;
		float slip;
		
		// Полный привод
		
		float motor = Mathf.Abs(motorSlip);	// motor = [0..motorMax]
		
		bool grounded = Wheel.getWheelCollider().GetGroundHit(out Hit);
		if (grounded)
		{
			Quaternion steerRot = Quaternion.AngleAxis(Wheel.getWheelCollider().steerAngle, Wheel.transform.up);
			Vector3 wheelDir = steerRot * Wheel.transform.forward;	
			
			Vector3 pointV = rigidbody.GetPointVelocity(Hit.point);
			if (Hit.collider.attachedRigidbody)
				pointV -= Hit.collider.attachedRigidbody.GetPointVelocity(Hit.point);
			
			float v = Mathf.Abs(Vector3.Dot(pointV, wheelDir));
			
			if (v + slipPeak <= motorMax)
			{
				slip = motor - v;
				
				if (slip < 0) 
					slip = 0;
				else
					if (autoMotorMax && slip > slipPeak)
						slip = slipPeak;
			}
			else
			{
				float maxSlip;
				
				if (tractionV3)			
					maxSlip = Mathf.Lerp(slipPeak, 0, Mathf.InverseLerp(motorMax-slipPeak, maxSpeed, v));
				else
					maxSlip = slipPeak;
				slip = maxSlip * motor / motorMax;
			}
			
			if (motorSlip < 0)
				slip = -slip;
		}
		else
			slip = motorSlip;		
		
		// Тормоза
		
		if (autoBrakeMax && brakeSlip > slipPeak)
			brakeSlip = slipPeak;		
		
		brakeSlip = Mathf.Max(brakeSlip, handBrakeInput * slipMax);
		
		if (motorInput == 0.0f) 
			brakeSlip += rollingFrictionSlip / brakeForceFactor;	
		
		if (!grounded)  // Колеса с тормозами в воздухе в зависимости от их скорости
		{
			float omega = Wheel.getWheelCollider().rpm * Mathf.Deg2Rad;
			brakeSlip += omega*omega * 0.0008f / brakeForceFactor;
		}
		
		Wheel.motorInput = slip;
		Wheel.brakeInput = brakeSlip;
	}

	public void CalculateSteerAngles()	{
		// -------- Реверс передних колес
		
		// Рассчитать угол обоих колес для идеального вращения. 
		// Предполагается, что расстояния между передним левым, правым и задним равны.
		// Максимальные значения вращения: -90..+90
		
		float B = (WheelFL.transform.position-WheelFR.transform.position).magnitude;
		float H = (WheelFR.transform.position-WheelRR.transform.position).magnitude;	
		
		if (steerInput > 0.0f)			// По часовой стрелке
		{
			m_steerR = steerMax * steerInput;
			if (m_steerR > m_steerAngleMax) m_steerR = m_steerAngleMax;
			m_steerL = Mathf.Rad2Deg * Mathf.Atan( 1.0f / (Mathf.Tan((90 - m_steerR) * Mathf.Deg2Rad) + B / H));
		}
		else if (steerInput < 0.0f)		// Против часовой стрелки
		{
			m_steerL = steerMax * steerInput;
			if (m_steerL < -m_steerAngleMax) m_steerL = -m_steerAngleMax;
			
			m_steerR = -Mathf.Rad2Deg * Mathf.Atan( 1.0f / (Mathf.Tan((90 + m_steerL) * Mathf.Deg2Rad) + B / H));
		}
		else
		{
			m_steerL = 0;
			m_steerR = 0;
		}
	}

}
