using UnityEngine;
using System.Collections;

public class SkidMarks : MonoBehaviour {

	private float markWidth = 0.2f;
	private int skidding;
	private Vector3[] lastPosition = new Vector3[2];
	[SerializeField] private Material SkidMaterial;

	[SerializeField] private float LiveTimeOfSkidMarks = 25;
	[SerializeField] private float LiveTimeOfSkidSound = 1;

	[SerializeField] private GameObject SkidSmoke;
	[SerializeField] private GameObject SkidSound;
	[SerializeField] private float soundEmission = 15f;
	private float SoundWait;

	// Use this for initialization
	void Start () {
		SkidSmoke.transform.position = new Vector3 (transform.position.x, transform.position.y - 0.4f, transform.position.z);
	
	}
	
	// Update is called once per frame
	void Update () {
		bool isSkidding = gameObject.transform.parent.transform.parent.gameObject.GetComponent<NewControl>().isSkidding;
		if (isSkidding)/*СЮДА БЫЛА ВПИСАНА БУЛЯ ЗАНОСА*/  {
			SkidSmoke.particleSystem.enableEmission=true;

			WheelHit hit;
			transform.GetComponent<WheelCollider> ().GetGroundHit (out hit);
			if(SoundWait<=0){
				GameObject SS = (GameObject)Instantiate(SkidSound,hit.point,Quaternion.identity);
				SoundWait=1;
				TimerDestroyer TD = SS.AddComponent<TimerDestroyer>();
				TD.SetTime (LiveTimeOfSkidSound);
			}
			SoundWait-=Time.deltaTime*soundEmission;

			SkidMesh();		
		}
		else{
			SkidSmoke.particleSystem.enableEmission=false;
			skidding=0;
		}
	}

	private void SkidMesh(){
		WheelHit hit;
		transform.GetComponent<WheelCollider> ().GetGroundHit (out hit);
		GameObject mark = new GameObject("Mark");
		MeshFilter filter = mark.AddComponent<MeshFilter> ();
		mark.AddComponent<MeshRenderer> ();
		Mesh markMesh = new Mesh ();
		Vector3[] vertices = new Vector3[4];
		int[] triangles={0,1,2,2,3,0};
		if (skidding == 0) {
			vertices[0] = hit.point + Quaternion.Euler(transform.eulerAngles.x,transform.eulerAngles.y,transform.eulerAngles.z)*new Vector3(markWidth,0.01f,0);
			vertices[1] = hit.point + Quaternion.Euler(transform.eulerAngles.x,transform.eulerAngles.y,transform.eulerAngles.z)*new Vector3(-markWidth,0.01f,0);
			vertices[2] = hit.point + Quaternion.Euler(transform.eulerAngles.x,transform.eulerAngles.y,transform.eulerAngles.z)*new Vector3(-markWidth,0.01f,0);
			vertices[3] = hit.point + Quaternion.Euler(transform.eulerAngles.x,transform.eulerAngles.y,transform.eulerAngles.z)*new Vector3(markWidth,0.01f,0);
			lastPosition[0] = vertices[2];
			lastPosition[1] = vertices[3];

			skidding=1;		
		}
		else{
			vertices[1] = lastPosition[0];
			vertices[0] = lastPosition[1];
			vertices[2] = hit.point + Quaternion.Euler(transform.eulerAngles.x,transform.eulerAngles.y,transform.eulerAngles.z)*new Vector3(-markWidth,0.01f,0);
			vertices[3] = hit.point + Quaternion.Euler(transform.eulerAngles.x,transform.eulerAngles.y,transform.eulerAngles.z)*new Vector3(markWidth,0.01f,0);
			lastPosition[0] = vertices[2];
			lastPosition[1] = vertices[3];
		}

		markMesh.vertices = vertices;
		markMesh.triangles = triangles;
		markMesh.RecalculateNormals ();
		Vector2[] uvm = new Vector2[4];
		uvm[0] = new Vector2(1,0);
		uvm[1] = new Vector2(0,0);
		uvm[2] = new Vector2(0,1);
		uvm[3] = new Vector2(1,1);

		markMesh.uv = uvm;
		filter.mesh = markMesh;
		mark.renderer.material = SkidMaterial;
		TimerDestroyer TD = mark.AddComponent<TimerDestroyer>();
		TD.SetTime (LiveTimeOfSkidMarks);

	}

}
