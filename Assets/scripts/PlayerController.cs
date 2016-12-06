using UnityEngine;
using UnityEngine.Networking;

public class PlayerController : Photon.MonoBehaviour {

	const float SPEED = 10;
	const float MAP_BOUNDARY = 4;

	Transform childNone, childLerp, childExtrap;

	private float lastPosX;

	float sync_pos_x;
	
	void Awake(){
		childNone = transform.GetChild (0);
		childLerp = transform.GetChild (1);
		childExtrap = transform.GetChild (2);
	}

	void Start(){
		lastPosX = transform.position.x;
	}


	void Update () {
		if (photonView.isMine) {
			UpdateLocalControl ();
		}
		else{
			Sync();
		}
	}

	void UpdateLocalControl(){
		if (Input.GetKey (KeyCode.LeftArrow) && transform.position.x > -MAP_BOUNDARY) {
			transform.position += Vector3.left * SPEED * Time.deltaTime;
		}
		else if (Input.GetKey(KeyCode.RightArrow) && transform.position.x < MAP_BOUNDARY) {
			transform.position += Vector3.right * SPEED * Time.deltaTime;
		}
	}
	// ================================================


	void Sync(){
		SyncPlain ();
		SyncLerp ();
		SyncExtrap ();
	}

	// ================== Networking  ================== 
	void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{	
	
		// send packet
		if (stream.isWriting){
			float currentX = childNone.position.x; 
			if(currentX != lastPosX){
				stream.SendNext(childNone.position.x);
				lastPosX = currentX;
			}
		}

		// received packet
		else if (stream.isReading){
			// exmaple
			float packetSentTime = (float)info.timestamp; 
			float currentTime = (float)PhotonNetwork.time;

			sync_pos_x = (float)stream.ReceiveNext();
		}
	}
   // ======================================================

	// ================== Numerical Methods ==================
	Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {	
        t = Mathf.Clamp01(t);
        return a + (Vector3.Normalize(b - a) * t);
    }

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

		if(Vector3.Distance(currentPos,newPos) > 0.1){
			Vector3 lerpPos = Lerp (currentPos, newPos, Time.deltaTime * SPEED);
			childLerp.position = lerpPos;
		}
	}


	float lastSyncX;
	void SyncExtrap(){
		Vector3 currentPos = childExtrap.position;
		Vector3 newPos = currentPos;
		newPos.x = sync_pos_x;	
       
		if(lastSyncX == sync_pos_x){
			if(Vector3.Distance(currentPos,newPos) > 0.1){
				Vector3 lerpPos = Lerp (currentPos, newPos, Time.deltaTime * SPEED);
				childLerp.position = lerpPos;
			}
		}
		else{
			Vector3 extrapPos = Lerp(currentPos, newPos+newPos-currentPos, Time.deltaTime * SPEED);
        	childExtrap.position = extrapPos;
			lastSyncX = sync_pos_x;
		}
		
    }
	// ======================================================

}
