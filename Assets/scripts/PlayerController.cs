using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerController : NetworkBehaviour {

	const float SPEED = 10;
	const float MAP_BOUNDARY = 4;

	Transform childNone, childLerp, childExtrap;

	[SyncVar]  // vars that are synchronized from the server to clients
	float sync_pos_x;

	void Awake(){
		childNone = transform.GetChild (0);
		childLerp = transform.GetChild (1);
		childExtrap = transform.GetChild (2);
	}

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
		CmdSyncPos (childNone.position.x); // sending absulte position of any child node since they have the same x
	}

	void SyncWithServer(){
		SyncPlain ();
		SyncLerp ();
		SyncExtrap ();
	}

	// ================== Numerical Methods ==================
	void SyncPlain(){
		Vector3 currentPos = childNone.position;
		Vector3 newPos = currentPos;
		newPos.x = sync_pos_x;
		childNone.position = newPos;
	}

	void SyncLerp(){
		Vector3 currentPos = childLerp.position;
		Vector3 newPos = currentPos;
		newPos.x = sync_pos_x;
		Vector3 lerpPos = Vector3.Lerp (currentPos, newPos, Time.deltaTime * SPEED);
		childLerp.position = lerpPos;
	}

	void SyncExtrap(){
        Vector3 currentPos = childExtrap.position;
        Vector3 newPos = currentPos;
        newPos.x = sync_pos_x;
        Vector3 lerpPos = Lerp(currentPos, newPos+newPos-currentPos, Time.deltaTime * SPEED);
        childExtrap.position = lerpPos;
    }
	// ======================================================

}
