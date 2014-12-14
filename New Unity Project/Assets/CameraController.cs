using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

	[SerializeField] private Transform Car;
	[SerializeField] private Transform CarPivot;
	private bool Parented;
	[SerializeField] private float Distance = 6.4f;
	[SerializeField] private float Height = 5.4f;
	[SerializeField] private float rDamp = 3.0f;
	[SerializeField] private float hDamp = 2.0f;
	[SerializeField] private float zoom = 0.5f;

	[SerializeField] private float xSpeed = 250.0f;
	[SerializeField] private float ySpeed = 120.0f;
	
	[SerializeField] private int yMinLimit = -20;
	[SerializeField] private int yMaxLimit = 80;
	
	private float x = 0.0f;
	private float y = 0.0f;
	private Vector3 angles;

	[SerializeField] private float DefaultFOV = 50;

	private Vector3 VectorOfRotation;

	// Use this for initialization
	void Start () {
		angles = transform.eulerAngles;
		x = angles.y;
		y = angles.x;
	}
	
	// Update is called once per frame
	void LateUpdate () {
		if(Input.GetButton("Fire2")){
			x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
			y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
			
			y = ClampAngle(y, yMinLimit, yMaxLimit);
			
			Quaternion rotation = Quaternion.Euler(y, x, 0);
			Vector3 position = rotation *new Vector3(0, 0, -Distance) + CarPivot.position;
			
			transform.rotation = rotation;
			transform.position = position;}
		else
		{
		float carAngle = VectorOfRotation.y;
		float carHeight = Car.position.y+Height;
		float cameraAngle = transform.eulerAngles.y;
		float cameraHeight = transform.position.y;

		cameraAngle = Mathf.LerpAngle(cameraAngle,carAngle,rDamp*Time.deltaTime);
		cameraHeight = Mathf.Lerp(cameraHeight,carHeight,hDamp*Time.deltaTime);

		Quaternion currentRotation = Quaternion.Euler(0,cameraAngle,0);
		
		transform.position=Car.position;
		transform.position-=currentRotation*Vector3.forward*Distance;
		transform.position=new Vector3(transform.position.x,carHeight,transform.position.z);
			transform.LookAt(CarPivot);}
	}

	void FixedUpdate(){

		if(Input.GetButton("Fire2")){
			angles = transform.eulerAngles;
			x = angles.y;
			y = angles.x;
		}
		else{

		Vector3 localVelocity = Car.InverseTransformDirection(Car.rigidbody.velocity);

		if(localVelocity.z<0.5f){
			VectorOfRotation = new Vector3(VectorOfRotation.x,Car.eulerAngles.y + 180,VectorOfRotation.z);
		}
		else VectorOfRotation = new Vector3(VectorOfRotation.x,Car.eulerAngles.y,VectorOfRotation.z);


		float Acceleration = Car.rigidbody.velocity.magnitude;
			camera.fieldOfView = DefaultFOV + Acceleration*zoom;}
	}

	static float ClampAngle (float angle,float min,float max) {
		if (angle < -360)
			angle += 360;
		if (angle > 360)
			angle -= 360;
		return Mathf.Clamp (angle, min, max);
	}
}
