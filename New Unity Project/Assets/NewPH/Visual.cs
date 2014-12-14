using UnityEngine;
using System.Collections;

public class Visual : MonoBehaviour {

	public Transform PivotFL;
	public Transform PivotFR;
	public Transform PivotRL;
	public Transform PivotRR;
	public Transform MeshFL;
	public Transform MeshFR;
	public Transform MeshRL;
	public Transform MeshRR;
	public Transform SteeringWheel;
	public Collider[] ignoredColliders;	//Коллайдерыкоторые следует игнорировать при расчете положение колес. Коллайдерыкоторые касаются WheelColliders. Вы должны держать колеса "в прыжке", если используется интерполяция в Rigidbody.

	public float forwardSkidmarksBegin = 1.5f;	 
	public float forwardSkidmarksRange = 1.0f;
	public float sidewaysSkidmarksBegin = 1.5f;
	public float sidewaysSkidmarksRange = 1.0f;
	public float skidmarksWidth = 0.275f;			// Ширина колесных следов
	public float skidmarksOffset = 0.0f;			//Смещение следов 
	public bool alwaysDrawSkidmarks = false;	

	public float forwardSmokeBegin = 5.0f;	
	public float forwardSmokeRange = 3.0f;
	public float sidewaysSmokeBegin = 4.0f;
	public float sidewaysSmokeRange = 3.0f;
	public float smokeStartTime = 2.0f;			// Секунды, необходимые для передачи горения колеса перед дым начнет выходить постепенно.
	public float smokePeakTime = 8.0f;			// Время, когда дым идет c полной интенсивностью.
	public float smokeMaxTime = 10.0f;			// Максимальное время учитывается, прежде чем начать уменьшить время.

	public float wheelGroundedBias = 0.02f;		
	public float steeringWheelMax = 520f;			
	
	public float impactThreeshold = 0.6f;			
	public float impactInterval = 0.2f;			
	public float impactIntervalRandom = 0.4f;		
	public float impactMinSpeed = 2.0f;			
	
	public bool disableRaycast = false;			
	public bool disableWheelVisuals = false;	

	[HideInInspector] float spinRateFL = 0.0f;
	[HideInInspector] float spinRateFR = 0.0f;
	[HideInInspector] float spinRateRL = 0.0f;
	[HideInInspector] float spinRateRR = 0.0f;
	
	[HideInInspector] float skidValueFL = 0.0f;
	[HideInInspector] float skidValueFR = 0.0f;
	[HideInInspector] float skidValueRL = 0.0f;
	[HideInInspector] float skidValueRR = 0.0f;
	
	[HideInInspector] float suspensionStressFL = 0.0f;
	[HideInInspector] float suspensionStressFR = 0.0f;
	[HideInInspector] float suspensionStressRL = 0.0f;
	[HideInInspector] float suspensionStressRR = 0.0f;
	
	[HideInInspector] Vector3 localImpactPosition = Vector3.zero;			
	[HideInInspector] Vector3 localImpactVelocity = Vector3.zero;
	[HideInInspector] bool localImpactSoftSurface = false;
	
	[HideInInspector] Vector3 localDragPosition = Vector3.zero;			
	[HideInInspector] Vector3 localDragVelocity = Vector3.zero;
	[HideInInspector] bool localDragSoftSurface = false;
	
	[HideInInspector] Vector3 localDragPositionDiscrete = Vector3.zero;	
	[HideInInspector] Vector3 localDragVelocityDiscrete = Vector3.zero;

	public class WheelVisualData
	{
		public float colliderOffset = 0.0f;
		public float skidmarkOffset = 0.0f;
		
		public Vector3 wheelVelocity = Vector3.zero;
		public Vector3 groundSpeed = Vector3.zero;
		public float angularVelocity = 0.0f;
		
		public float lastSuspensionForce = 0.0f;
		public float suspensionStress = 0.0f;
		
		public int lastSkidmark = -1;
		public float skidmarkTime = 0.0f;	
		
		public float skidSmokeTime = Time.time;
		public Vector3 skidSmokePos = Vector3.zero;
		public float skidSmokeIntensity = 0.0f;
		
		public float skidValue = 0.0f;
	}

	private Control m_Car;

	private WheelVisualData[] m_wheelData;

	// Use this for initialization
	void Start () {
		m_Car = GetComponent<Control>();
		


		
		m_wheelData = new WheelVisualData[4];
		for (var i=0; i<4; i++)
			m_wheelData[i] = new WheelVisualData();
		
		m_wheelData[0].colliderOffset = transform.InverseTransformDirection(PivotFL.position - m_Car.WheelFL.transform.position).x;
		m_wheelData[1].colliderOffset = transform.InverseTransformDirection(PivotFR.position - m_Car.WheelFR.transform.position).x;
		m_wheelData[2].colliderOffset = transform.InverseTransformDirection(PivotRL.position - m_Car.WheelRL.transform.position).x;
		m_wheelData[3].colliderOffset = transform.InverseTransformDirection(PivotRR.position - m_Car.WheelRR.transform.position).x;

	}
	
	// Update is called once per frame
	void Update () {		
		DoWheelVisuals(m_Car.WheelFL, MeshFL, PivotFL, m_wheelData[0]);
		DoWheelVisuals(m_Car.WheelFR, MeshFR, PivotFR, m_wheelData[1]);
		DoWheelVisuals(m_Car.WheelRL, MeshRL, PivotRL, m_wheelData[2]);
		DoWheelVisuals(m_Car.WheelRR, MeshRR, PivotRR, m_wheelData[3]);
		
		spinRateFL = m_wheelData[0].angularVelocity;
		spinRateFR = m_wheelData[1].angularVelocity;
		spinRateRL = m_wheelData[2].angularVelocity;
		spinRateRR = m_wheelData[3].angularVelocity;	
		
		skidValueFL = m_wheelData[0].skidValue;
		skidValueFR = m_wheelData[1].skidValue;
		skidValueRL = m_wheelData[2].skidValue;
		skidValueRR = m_wheelData[3].skidValue;
		
		suspensionStressFL = m_wheelData[0].suspensionStress;
		suspensionStressFR = m_wheelData[1].suspensionStress;
		suspensionStressRL = m_wheelData[2].suspensionStress;
		suspensionStressRR = m_wheelData[3].suspensionStress;
		

		ProcessImpacts();
		ProcessDrags(Vector3.zero, Vector3.zero, false);

		float steerL = m_Car.getSteerL(); 
		float steerR = m_Car.getSteerR();
		
		foreach (Collider coll in ignoredColliders)
			coll.gameObject.layer = 2;
		
		DoWheelPosition(m_Car.WheelFL, PivotFL, steerL, m_wheelData[0]);
		DoWheelPosition(m_Car.WheelFR, PivotFR, steerR, m_wheelData[1]);
		DoWheelPosition(m_Car.WheelRL, PivotRL, 0, m_wheelData[2]);
		DoWheelPosition(m_Car.WheelRR, PivotRR, 0, m_wheelData[3]);
		
		foreach (Collider coll in ignoredColliders)
			coll.gameObject.layer = 0;
		

		
		if (SteeringWheel)
		{
			float currentAngle = m_Car.steerInput >= 0.0f? steerR : steerL;
			SteeringWheel.localEulerAngles=new Vector3(SteeringWheel.localEulerAngles.x,SteeringWheel.localEulerAngles.y, -steeringWheelMax * currentAngle/m_Car.steerMax);
		}
	}

	private static bool IsHardSurface(Collider col)	{
		return !col.sharedMaterial || col.attachedRigidbody != null;
	}
	
	private static bool IsStaticSurface(Collider col)
	{
		return !col.attachedRigidbody;
	}
	
	
	public void DoWheelVisuals(Wheel Wheel,Transform Graphic,Transform Pivot,WheelVisualData wheelData)
	{
		WheelCollider WheelCol;
		WheelHit Hit;
		float deltaT;
		float Skid;
		float wheelSpeed;
		
		float forwardSkidValue;
		float sidewaysSkidValue;
		
		WheelCol = Wheel.getWheelCollider();
		
		if (!disableWheelVisuals && WheelCol.GetGroundHit(out Hit))
		{
			
			wheelData.suspensionStress = Hit.force - wheelData.lastSuspensionForce;
			wheelData.lastSuspensionForce = Hit.force;

			
			wheelData.wheelVelocity = rigidbody.GetPointVelocity(Hit.point);
			if (Hit.collider.attachedRigidbody)
				wheelData.wheelVelocity -= Hit.collider.attachedRigidbody.GetPointVelocity(Hit.point);

			wheelData.groundSpeed = Pivot.transform.InverseTransformDirection(wheelData.wheelVelocity);
			wheelData.groundSpeed.y = 0.0f;
			

			float frictionPeak = Wheel.getForwardPeakSlip();
			float frictionMax = Wheel.getForwardMaxSlip();
			
			float MotorSlip = Wheel.motorInput;
			float BrakeSlip = Wheel.brakeInput;

			
			float TorqueSlip = Mathf.Abs(MotorSlip) - Mathf.Max(BrakeSlip);
			
			if (TorqueSlip >= 0)	
			{
				Skid = TorqueSlip -	frictionPeak;
				if (Skid > 0)
				{
					wheelSpeed = Mathf.Abs(wheelData.groundSpeed.z) + Skid;
					
					if (MotorSlip < 0)	
						wheelSpeed = -wheelSpeed;
				}
				else
					wheelSpeed = wheelData.groundSpeed.z;
			}
			else	
			{
				Skid = Mathf.InverseLerp(frictionMax, frictionPeak, -TorqueSlip);			
				wheelSpeed = wheelData.groundSpeed.z * Skid;
			}
			
			if (m_Car.serviceMode)
				wheelSpeed = RpmToMs(WheelCol.rpm, WheelCol.radius * Wheel.transform.lossyScale.y);
			
			wheelData.angularVelocity = wheelSpeed / (WheelCol.radius * Wheel.transform.lossyScale.y);		

	
		}
		else
		{
			wheelData.angularVelocity = WheelCol.rpm * 6 * Mathf.Deg2Rad;  
			
			wheelData.suspensionStress = 0.0f - wheelData.lastSuspensionForce;
			wheelData.lastSuspensionForce = 0.0f;
			
			wheelData.skidValue = 0.0f;
			wheelData.lastSkidmark = -1;
			wheelData.skidSmokeTime = Time.time;
			wheelData.skidSmokePos = Wheel.transform.position - Wheel.transform.up * ((WheelCol.suspensionDistance + WheelCol.radius * 0.5f) * Wheel.transform.lossyScale.y) + transform.right * wheelData.skidmarkOffset;
			wheelData.skidSmokeIntensity -= Time.deltaTime;
		}
		
		Graphic.Rotate(wheelData.angularVelocity * Mathf.Rad2Deg * Time.deltaTime, 0.0f, 0.0f);
	}
	

	
	public void DoWheelPosition(Wheel Wheel,Transform WheelMesh,float steerAngle,WheelVisualData wheelData)
	{
		Vector3 hitPoint = new Vector3();
		bool grounded = false;
		
		WheelCollider WheelCol = Wheel.getWheelCollider();
		
		if (!disableRaycast)
		{
			RaycastHit HitR = new RaycastHit();	
			
			if (Physics.Raycast(Wheel.transform.position, -Wheel.transform.up,out HitR, (WheelCol.suspensionDistance + WheelCol.radius) * Wheel.transform.lossyScale.y))
			{
				hitPoint = HitR.point + Wheel.transform.up * (WheelCol.radius * Wheel.transform.lossyScale.y - wheelGroundedBias) + transform.right * wheelData.colliderOffset;
				grounded = true;
			}
		}
		else
		{
			WheelHit HitW = new WheelHit();
			
			if (WheelCol.GetGroundHit(out HitW))
			{
				hitPoint = HitW.point + Wheel.transform.up * (WheelCol.radius * Wheel.transform.lossyScale.y - wheelGroundedBias) + transform.right * wheelData.colliderOffset;
				grounded = true;
			}
		}
		
		if (grounded)
			WheelMesh.position = hitPoint;
		else
			WheelMesh.position = Wheel.transform.position - Wheel.transform.up * (WheelCol.suspensionDistance * Wheel.transform.lossyScale.y + wheelGroundedBias) + transform.right * wheelData.colliderOffset;

		WheelMesh.localEulerAngles = new Vector3(WheelMesh.localEulerAngles.x,Wheel.transform.localEulerAngles.y + steerAngle,Wheel.transform.localEulerAngles.z);
	}
	

	
	float RpmToMs(float Rpm,float Radius)	{
		return Mathf.PI * Radius * Rpm / 30.0f;
	}
	
	float MsToRpm(float Ms,float Radius)
	{
		return 30.0f * Ms / (Mathf.PI * Radius);
	}	
	
	

	
	private int m_sumImpactCount = 0;
	private int m_sumImpactCountSoft = 0;
	private Vector3 m_sumImpactPosition = Vector3.zero;
	private Vector3 m_sumImpactVelocity = Vector3.zero;
	
	private float m_lastImpactTime = 0.0f;
	
	

	
	private void ProcessImpacts()
	{
		bool bCanProcessCollisions = Time.time-m_lastImpactTime >= impactInterval;
			
		if (bCanProcessCollisions && m_sumImpactCount > 0)
		{
			localImpactPosition = m_sumImpactPosition / m_sumImpactCount;
			localImpactVelocity = m_sumImpactVelocity;
			localImpactSoftSurface = m_sumImpactCountSoft > m_sumImpactCount/2;
			
			localDragPositionDiscrete = localDragPosition;
			localDragVelocityDiscrete = localDragVelocity;
			
			m_sumImpactCount = 0;
			m_sumImpactCountSoft = 0;
			m_sumImpactPosition = Vector3.zero;
			m_sumImpactVelocity = Vector3.zero;
			
			m_lastImpactTime = Time.time + impactInterval * Random.Range(-impactIntervalRandom, impactIntervalRandom);	// Add a random variation for avoiding regularities
		}
		else
		{
			localImpactPosition = Vector3.zero;
			localImpactVelocity = Vector3.zero;
			
			localDragVelocityDiscrete = Vector3.zero;
		}
		}

	private void ProcessDrags(Vector3 dragPosition,Vector3 dragVelocity,bool dragSoftSurface)
	{
		if (dragVelocity.sqrMagnitude > 0.001f)
		{	
			localDragPosition = Vector3.Lerp(localDragPosition, dragPosition, 10.0f * Time.deltaTime);
			localDragVelocity = Vector3.Lerp(localDragVelocity, dragVelocity, 20.0f * Time.deltaTime);
			localDragSoftSurface = dragSoftSurface;
		}
		else
		{
			localDragVelocity = Vector3.Lerp(localDragVelocity, Vector3.zero, 10.0f * Time.deltaTime);
		}		
		}

	private void ProcessContacts (Collision col,bool forceImpact)
	{
		int colImpactCount = 0;
		int colImpactCountSoft = 0;
		Vector3 colImpactPosition = Vector3.zero;
		Vector3 colImpactVelocity = Vector3.zero;
		
		int colDragCount = 0;
		int colDragCountSoft = 0;
		Vector3 colDragPosition = Vector3.zero;
		Vector3 colDragVelocity = Vector3.zero;
		
		var sqrImpactSpeed = impactMinSpeed*impactMinSpeed;

		foreach(ContactPoint contact in col.contacts){

			var thisCol = contact.thisCollider;
			var otherCol = contact.otherCollider;
			
			if (thisCol == null || thisCol.attachedRigidbody != rigidbody)
			{
				thisCol = contact.otherCollider;
				otherCol = contact.thisCollider;
			}
			 
			if (thisCol is WheelCollider && otherCol is WheelCollider)
			{

				Vector3 V = rigidbody.GetPointVelocity(contact.point);
				if (otherCol && otherCol.attachedRigidbody)
					V -= otherCol.attachedRigidbody.GetPointVelocity(contact.point);
				
				var dragRatio = Vector3.Dot(V, contact.normal);
				
				// Determine whether this contact is an impact or a drag
				
				if (dragRatio < -impactThreeshold || forceImpact && col.relativeVelocity.sqrMagnitude > sqrImpactSpeed)
				{
					
					colImpactCount++;
					colImpactPosition += contact.point;
					colImpactVelocity += col.relativeVelocity;				
					if (otherCol && !IsHardSurface(otherCol)) colImpactCountSoft++;
					
				
				}
				else if (dragRatio < impactThreeshold)
				{
					
					colDragCount++;
					colDragPosition += contact.point;
					colDragVelocity += V;
					if (otherCol && !IsHardSurface(otherCol)) colDragCountSoft++;

				}
				}
		}

		
		if (colImpactCount > 0)
		{
			colImpactPosition /= colImpactCount;
			colImpactVelocity /= colImpactCount;
			
			m_sumImpactCount++;
			m_sumImpactPosition += transform.InverseTransformPoint(colImpactPosition);
			m_sumImpactVelocity += transform.InverseTransformDirection(colImpactVelocity);
			if (colImpactCountSoft > colImpactCount/2) m_sumImpactCountSoft++;
		}

		
		if (colDragCount > 0)
		{
			colDragPosition /= colDragCount;
			colDragVelocity /= colDragCount;
			
			ProcessDrags(transform.InverseTransformPoint(colDragPosition), transform.InverseTransformDirection(colDragVelocity), colDragCountSoft > colDragCount/2);
			}
	}
	
	
	public void OnCollisionEnter(Collision collision)
	{
		ProcessContacts(collision, true);
	}
	
	
	public void OnCollisionStay(Collision collision)
	{
		ProcessContacts(collision, false);
	}

	static float Lin2Log(float value)
	{
		return Mathf.Log(Mathf.Abs(value)+1) * Mathf.Sign(value);	
	}
	
	static Vector3 Lin2Log(Vector3 value)
	{
		return Vector3.ClampMagnitude(value, Lin2Log(value.magnitude));
	}
}
