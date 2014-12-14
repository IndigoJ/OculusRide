using UnityEngine;
using System.Collections;

public class Wheel : MonoBehaviour {

	//Класс для хранения фрикшенов.
	public class FrictionCurve : System.Object
	{
		public FrictionCurve(float grip,float gripRange,float drift,float driftSlope){
			this.grip=grip;
			this.gripRange=gripRange;
			this.drift=drift;
			this.driftSlope=driftSlope;
		}
		public FrictionCurve(){
			this.grip=800.0f;
			this.gripRange=5.0f;
			this.drift=150.0f;
			this.driftSlope=0.0f;
		}

		public float grip = 800.0f; //Сцепление с дорогой
		public float gripRange = 5.0f;
		public float drift = 150.0f; //Сила заноса 
		public float driftSlope = 0.0f; //Наклон заноса
	}

	public float motorInput = 0.0f; //Нажатие на педаль газа от 0 до 1
	public float brakeInput = 0.0f; //Нажатие на педаль тормоза от 0 до 1
	public float motorForceFactor = 1.0f; //Мощность двигателя
	public float brakeForceFactor = 1.0f; //Мощность тормозов
	public float sidewaysForceFactor = 1.0f; //Боковое сцепление
	public float sidewaysDriftFriction = 0.35f; //Боковое сцепление на ручнике

	public float performancePeak = 2.8f; // Максимальное ускорение
	public float staticFrictionMax = 1500.0f;	// Максимальное скольжение

	public FrictionCurve ForwardWheelFriction = new FrictionCurve(); //Коэфициенты скложения/сцепления вперед
	public FrictionCurve SidewaysWheelFriction = new FrictionCurve(); //и в сторону

	public bool optimized = false;	// Если TRUE, вызываем метод RecalculateStuff при изменении "кривой трения"
	public bool serviceMode = false;

	private WheelCollider m_wheel; //WheelCollider колеса
	private Rigidbody m_rigidbody; //Rigidbody колеса

	private Vector2 m_forwardFrictionPeak =new Vector2(1.0f, 1.0f); // Точка масимального значение трений вперед
	private Vector2 m_sidewaysFrictionPeak =new Vector2(1.0f, 1.0f); // Точка масимального значение трений в сторону

	private float m_forwardSlipRatio = 0.0f; //Передний коэфициент скольжения
	private float m_sidewaysSlipRatio = 0.0f; //Боковой коэфициент скольжения

	private float m_driftFactor = 1.0f; //Коэфициент заноса

	private WheelHit m_wheelHit; //Точка столкновения колеса с землей
	private bool m_grounded = false; //Находится ли колеса на земле (или в воздухе)

	private float m_DeltaTimeFactor = 1.0f; //Time.deltaTime

	public float getForwardPeakSlip () { return m_wheel.forwardFriction.extremumSlip * m_forwardFrictionPeak.x; }
	public float  getForwardMaxSlip () { return m_wheel.forwardFriction.asymptoteSlip; }
	public float  getSidewaysPeakSlip () { return m_wheel.sidewaysFriction.extremumSlip * m_sidewaysFrictionPeak.x; }  
	public float  getSidewaysMaxSlip () { return m_wheel.sidewaysFriction.asymptoteSlip; }
	
	public Vector2  getSidewaysPeak () { return m_sidewaysFrictionPeak; }
	public Vector2  getSidewaysMax () { return new Vector2(m_wheel.sidewaysFriction.asymptoteSlip, WheelFriction.GetValue(WheelFriction.MCsideways, m_wheel.sidewaysFriction.asymptoteValue, m_wheel.sidewaysFriction.asymptoteSlip)); }
	
	public float getForwardSlipRatio () { return m_forwardSlipRatio; }
	public float getSidewaysSlipRatio () { return m_sidewaysSlipRatio; }
	
	public float getDriftFactor () { return m_driftFactor; }
	
	public WheelCollider getWheelCollider () { return m_wheel; } 


	// Use this for initialization
	void Start () {
		// Доступ WheelCollider'ам
		m_wheel = GetComponent<WheelCollider>();
		m_rigidbody = m_wheel.attachedRigidbody;

		// Если включен режим "оптимизации" то будут пересчитаны данные 
		// Если параметры кривых трения не оптимизированы необходимо вызвать изменения через RecalculateStuff вручную.
		
		if (optimized)
			RecalculateStuff();	

	}

	public void RecalculateStuff()	{
		// Точка максимальной мощности оригинального продольной кривой
		
		m_forwardFrictionPeak = WheelFriction.GetPeakValue(WheelFriction.MCforward, ForwardWheelFriction.grip, ForwardWheelFriction.gripRange, ForwardWheelFriction.drift);
		
		// Punto de mбximo rendimiento de la curva lateral
		
		m_sidewaysFrictionPeak = WheelFriction.GetPeakValue(WheelFriction.MCsideways, SidewaysWheelFriction.grip, SidewaysWheelFriction.gripRange, SidewaysWheelFriction.drift);
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		// Рассчитайте корректировку
		
		m_DeltaTimeFactor = Time.fixedDeltaTime * Time.fixedDeltaTime * 2500.0f;   // эквивалентно (fixedDeltaTime/0.02)^2
		
		// Определить состояние колеса
		
		m_grounded = m_wheel.GetGroundHit(out m_wheelHit);	
		
		// Расчет кривых, пиков трения)
		
		if (!optimized)
			RecalculateStuff();

		//!!!!!!
		// 1. Продольное трения
		//!!!!!!
		// Вычислить чистое ускорение производительности-включенного тормоз на колесе. Для определения углового коэффициента (slopeFactor).
		
		float resultInput = Mathf.Abs(motorInput) - brakeInput;
		
		// Рассчитать параметры кривой регулировки:
		// - fslipFactor масштабируем кривую так, чтобы максимальная мощность точки совпадали (более или менее) с заданными значениеми.
		// - fSlopeFactor жесткость умноженая на сцепление и занос (grip и drift).
		//   Если не равно 1.0 слегка сдвигает точку максимальной производительности, но не очень заметно.
		
		float fSlipFactor = serviceMode? 1.0f : performancePeak / m_forwardFrictionPeak.y;		
		float fSlopeFactor = resultInput >= 0.0f? motorForceFactor : brakeForceFactor;
		
		// Рассчитать правильную кривую для WheelCollider
		
		m_wheel.forwardFriction = GetWheelFrictionCurve(ForwardWheelFriction, fSlopeFactor, fSlipFactor);
		m_wheel.motorTorque = SlipToTorque(motorInput);
		m_wheel.brakeTorque = SlipToTorque(brakeInput);

		//!!!!!!
		// 2. Боковое трение
		//!!!!!!
		
		// Рассчитать потерю производительности бокового сцепление в зависимости от продольной колеса
		// Если колесо находится в фактическом продольном скольжения с почвой используется, если не используется в записи.
		
		m_driftFactor = Mathf.Lerp(1.0f, sidewaysDriftFriction, Mathf.InverseLerp(m_wheel.forwardFriction.extremumSlip*m_forwardFrictionPeak.x, m_wheel.forwardFriction.asymptoteSlip, Mathf.Abs(m_grounded? m_wheelHit.forwardSlip : resultInput)));
		
		// Рассчитать и применять кривую бокового трения в WheelCollider
		
		fSlopeFactor = serviceMode? 1.0f : m_driftFactor*sidewaysForceFactor;
		m_wheel.sidewaysFriction = GetWheelFrictionCurve(SidewaysWheelFriction, fSlopeFactor, 1.0f); 


		//!!!!!!
		// 3. Данные корректировки и исправления
		//!!!!!!

		if (m_grounded)
		{
			// Телеметрические данные
			
			m_forwardSlipRatio = GetWheelSlipRatio(m_wheelHit.forwardSlip, m_wheel.forwardFriction.extremumSlip*m_forwardFrictionPeak.x, m_wheel.forwardFriction.asymptoteSlip);	
			m_sidewaysSlipRatio = GetWheelSlipRatio(m_wheelHit.sidewaysSlip, m_wheel.sidewaysFriction.extremumSlip*m_sidewaysFrictionPeak.x, m_wheel.sidewaysFriction.asymptoteSlip);
			
			// Закрепить кривые трения WheelFrictionCurve
			// - Бокове трение
			
			float absSlip = Mathf.Abs(m_wheelHit.sidewaysSlip);		
			if (staticFrictionMax > m_wheel.sidewaysFriction.extremumValue && absSlip < m_wheel.sidewaysFriction.extremumSlip)   // Baja velocidad - reforzar ligeramente el control estбtico del WheelCollider
			{
				WheelFrictionCurve sf =m_wheel.sidewaysFriction;
				sf.extremumValue = GetFixedSlope(WheelFriction.MCsideways, absSlip, m_wheel.sidewaysFriction.extremumSlip, m_wheel.sidewaysFriction.extremumValue, 0.0f); 
				m_wheel.sidewaysFriction=sf;
				if (m_wheel.sidewaysFriction.extremumValue > staticFrictionMax){ sf.extremumValue = staticFrictionMax; m_wheel.sidewaysFriction=sf;}
			}
			
			if (absSlip > m_wheel.sidewaysFriction.asymptoteSlip){
				WheelFrictionCurve sf =m_wheel.sidewaysFriction;
				sf.asymptoteValue = GetFixedSlope(WheelFriction.MCsideways, absSlip, m_wheel.sidewaysFriction.asymptoteSlip, m_wheel.sidewaysFriction.asymptoteValue, SidewaysWheelFriction.driftSlope);		
				m_wheel.sidewaysFriction=sf;
			}
			// - Продольное трение
			
			absSlip = Mathf.Abs(m_wheelHit.forwardSlip);
			if (absSlip > m_wheel.forwardFriction.asymptoteSlip){
				WheelFrictionCurve ff =m_wheel.forwardFriction;
				ff.asymptoteValue = GetFixedSlope(WheelFriction.MCforward, absSlip, m_wheel.forwardFriction.asymptoteSlip, m_wheel.forwardFriction.asymptoteValue, ForwardWheelFriction.driftSlope);
				m_wheel.forwardFriction=ff;
			}
			// Отрегулировать кривую как поле функции
			
			if (m_wheelHit.collider.sharedMaterial)
			{
				WheelFrictionCurve ff =m_wheel.forwardFriction;
				WheelFrictionCurve sf =m_wheel.sidewaysFriction;
				ff.stiffness *= (m_wheelHit.collider.sharedMaterial.dynamicFriction + 1.0f) * 0.5f;
				sf.stiffness *= (m_wheelHit.collider.sharedMaterial.dynamicFriction + 1.0f) * 0.5f;
				m_wheel.forwardFriction=ff;
				m_wheel.sidewaysFriction=sf;
				// Применить силу сопротивления по бездорожью
				
				Vector3 wheelV = m_rigidbody.GetPointVelocity(m_wheelHit.point);
				m_rigidbody.AddForceAtPosition(wheelV * wheelV.magnitude * -m_wheelHit.force * m_wheelHit.collider.sharedMaterial.dynamicFriction * 0.001f, m_wheelHit.point);
			}		
		}
		else
		{
			m_forwardSlipRatio = 0.0f;
			m_sidewaysSlipRatio = 0.0f;
		}
	}

	// Преобразование параметров трения кривой для WheelCollider`а
	
	public WheelFrictionCurve GetWheelFrictionCurve (FrictionCurve Friction,float Stiffness,float SlipFactor){
		WheelFrictionCurve Curve=new WheelFrictionCurve();
		
		Curve.extremumSlip = 1.0f * SlipFactor;
		Curve.extremumValue = Friction.grip * m_DeltaTimeFactor;
		Curve.asymptoteSlip = (1.0f + Friction.gripRange) * SlipFactor;
		Curve.asymptoteValue = Friction.drift * m_DeltaTimeFactor;
		
		Curve.stiffness = Stiffness;
		return Curve;
	}

	// Преобразование значения крутящего момента cкольжение для WheelCollider`а	
	public float SlipToTorque (float Slip){
		return (Slip * m_wheel.mass) / (m_wheel.radius * m_wheel.transform.lossyScale.y * Time.deltaTime);
	}

	    // Рассчитать относительный статус скольжения колеса
		//
	    // 0..1 -> полный захват до Peak
		// 1..2 -> начинает скользить между Peak и Max
		// 2... -> полное скольжение выше Max
		
	public float GetWheelSlipRatio(float Slip,float PeakSlip,float MaxSlip){
		float slipAbs = Slip >= 0.0f? Slip : -Slip;
		float result;
		
		if (slipAbs < PeakSlip)
			result = slipAbs / PeakSlip;
		else 
			if (slipAbs < MaxSlip)
				result = 1.0f + Mathf.InverseLerp(PeakSlip, MaxSlip, slipAbs);
		else
			result = 2.0f + slipAbs-MaxSlip;
		
		return Slip >= 0? result : -result;
	}

	// Получить исправленный наклон кривой в асимптоте что бы колеса правильно себя вели
	// Требования:
	//  - Катиться по полу
	//	- absSlip является абсолютной величиной текущего скольжения

	
	public float GetFixedSlope(float[] Coefs,float absSlip,float asymSlip,float asymValue,float valueFactor){	
		float Slope;
		
		// Значение в точке асимптоты. Это то, что будет поддерживать величину смещения.
		
		float Value = WheelFriction.GetValue(WheelFriction.MCsideways, asymValue/m_DeltaTimeFactor, asymSlip);
		
		// Впервые на значение, которое поддерживает текущее смещение колеса с помощью контролируемого склона (valueVactor)
		
		Slope = WheelFriction.GetSlope(WheelFriction.MCsideways, absSlip, Value + (absSlip-asymSlip)*valueFactor) * m_DeltaTimeFactor;
		
		return Slope;
	}

}
