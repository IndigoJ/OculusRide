using UnityEngine;
using System.Collections;

public class RoadStiffness : MonoBehaviour {

	[SerializeField] private float StiffnessOfRoad;

	/*public float wheelFRstiffness = 1.0f;
	  public float wheelFLstiffness = 1.0f;
      public float wheelRLstiffness = 1.0f;
	  public float wheelRRstiffness = 1.0f;
	  NewControl.cs
	 */
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	}

	void OnTriggerEnter(Collider other) {
		//Debug.Log(other.gameObject.name);
		if(other.gameObject.tag=="Wheel") {
			//Debug.Log("I Wanna change, changechangechange");
			other.transform.parent.transform.parent.gameObject.GetComponent<NewControl> ().wheelsStiffness[other.gameObject.name] = StiffnessOfRoad;
		}

	}

}
