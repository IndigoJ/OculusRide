using UnityEngine;
using System.Collections;

public class TimerDestroyer : MonoBehaviour {

	public float LiveTime;
	private bool started=false;

	// Use this for initialization
	void Start () {
		LiveTime = 30;
	}

	public void SetTime(float LTime){
		LiveTime = LTime;
		started = true;
	}
	
	// Update is called once per frame
	void Update () {
		if(started)
		LiveTime-=Time.deltaTime;

		if (LiveTime <= 0) {
			Destroy(gameObject);		
		}
	}
}
