using UnityEngine;
using System.Collections;

public class AntiRollBar : MonoBehaviour {
	//Параметры
	public WheelCollider WheelL; 
	public WheelCollider WheelR; 
	public float AntiRoll = 5000.0f; 
	public float AntiRollFactor = 1.0f;
	public float AntiRollBias = 0.5f;
	public int StrictMode = 0;

	// Телеметрические данные
	
	private float m_extensionL = 0.0f;
	private float m_extensionR = 0.0f;
	private float m_antiRollForce = 0.0f;
	private float m_antiRollRatio = 0.0f;
	
	public float getExtensionL(){ return m_extensionL; }
	public float getExtensionR(){ return m_extensionR; }
	public float getAntiRollForce(){ return m_antiRollForce; }
	public float getAntiRollRatio(){ return m_antiRollRatio; }
	public float getAntiRollTravel(){ return m_extensionL - m_extensionR; }



	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		WheelHit hitL; 
		WheelHit hitR;

		bool groundedL = WheelL.GetGroundHit(out hitL); 
		if (groundedL) 
			m_extensionL = (-WheelL.transform.InverseTransformPoint(hitL.point).y - WheelL.radius) / WheelL.suspensionDistance;
		else
			m_extensionL = 1.0f;

		bool groundedR = WheelR.GetGroundHit(out hitR); 
		if (groundedR)
			m_extensionR = (-WheelR.transform.InverseTransformPoint(hitR.point).y - WheelR.radius) / WheelR.suspensionDistance;
		else
			m_extensionR = 1.0f;

		m_antiRollRatio = Bias(m_extensionL - m_extensionR, AntiRollBias);
		m_antiRollForce = m_antiRollRatio * AntiRoll * AntiRollFactor; 

		// Strict режим влияет на случай, когда одно колесо поднято, а другой нет.
		// Если сила на одной стороне удаляется, нужно заменить его на другой для поддержания постоянного общего веса.
		// 
		// - Strict 0: поддерживается только усилие на колеса.
		// - Strict 1: пополнить прочность колеса, построенного в центре масс.
		// - Strict 2: применять силы на колеса независимо от того на земле они или нет
		
		if (groundedL || StrictMode == 2)
			rigidbody.AddForceAtPosition(WheelL.transform.up * -m_antiRollForce, WheelL.transform.position);
		else if (StrictMode == 1)
			rigidbody.AddForce(WheelL.transform.up * -m_antiRollForce);
		
		if (groundedR || StrictMode == 2) 
			rigidbody.AddForceAtPosition(WheelR.transform.up * m_antiRollForce, WheelR.transform.position); 
		else if (StrictMode == 1)
			rigidbody.AddForce(WheelL.transform.up * m_antiRollForce);
	}

	private float m_lastExponent = 0.0f;
	private float m_lastBias = -1.0f;

	private float BiasRaw(float x,float fBias){
		if (x <= 0.0f) return 0.0f;
		if (x >= 1.0f) return 1.0f;
		
		if (fBias != m_lastBias)
		{
			if (fBias <= 0.0f) return x >= 1.0f? 1.0f : 0.0f;
			else if (fBias >= 1.0f) return x > 0.0f? 1.0f : 0.0f;
			else if (fBias == 0.5f) return x;
			
			m_lastExponent = Mathf.Log(fBias) * -1.4427f;
			m_lastBias = fBias;
		}
		
		return Mathf.Pow(x, m_lastExponent);
	}

	// Симметричное смещение, использует только нижнюю кривую(fBias < 0.5)
	// Поддерживает диапазон -1, 1, применяя симметричный эффект от 0 до +1 и -1.
	
	private float Bias(float x,float fBias)	{
		float fResult;
		
		fResult = fBias <= 0.5f? BiasRaw(Mathf.Abs(x), fBias) : 1.0f - BiasRaw(1.0f - Mathf.Abs(x), 1.0f - fBias);
		
		return x<0.0f? -fResult : fResult;
	}
}
