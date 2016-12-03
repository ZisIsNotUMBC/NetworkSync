using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerController : NetworkBehaviour {

	const float SPEED = 10;
	const float MAP_BOUNDARY = 4;

	[SyncVar]  // vars that are synchronized from the server to clients
	float sync_pos_x;  

	void Start(){
		// initial sync
		// position is set by network start position
		SendPosToServer();
	}

		
	void Update () {
		if (isLocalPlayer) {
			UpdateLocalControl ();
		} else {
			SyncWithServer ();
		}
	}

	void UpdateLocalControl(){
		if (Input.GetKey (KeyCode.LeftArrow) && transform.position.x > -MAP_BOUNDARY) {
			transform.position += Vector3.left * SPEED * Time.deltaTime;
			SendPosToServer ();
		}
		else if (Input.GetKey(KeyCode.RightArrow) && transform.position.x < MAP_BOUNDARY) {
			transform.position += Vector3.right * SPEED * Time.deltaTime;
			SendPosToServer ();
		}
	}

	[Command] // runs on the server but can be triggered by clients
	void CmdSyncPos(float x){
		sync_pos_x = x;
	}

	[ClientCallback] //  run on clients, but not generate warnings if called on server
	void SendPosToServer(){
		CmdSyncPos (transform.position.x);
	}

	void SyncWithServer(){
		Vector3 currentPos = transform.position;
		Vector3 newPos = currentPos;
		newPos.x = sync_pos_x;
		transform.position = newPos;
	}

}
