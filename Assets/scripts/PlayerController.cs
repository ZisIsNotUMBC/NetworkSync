using UnityEngine;
using UnityEngine.Networking;

public class PlayerController : Photon.MonoBehaviour {

	const float SPEED = 10;
	const float MAP_BOUNDARY = 4;

	Transform childNone, childLerp, childExtrap;

	private float lastSentPosX;

	float sync_pos_x;
	bool firstSync = true;
	
	void Awake(){
		childNone = transform.GetChild (0);
		childLerp = transform.GetChild (1);
		childExtrap = transform.GetChild (2);
	}

	void Start(){
		lastSentPosX = transform.position.x;
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
			if(currentX != lastSentPosX || firstSync){
				stream.SendNext(childNone.position.x);
				lastSentPosX = currentX;

				if(firstSync)
					firstSync = false;
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

		if(Mathf.Abs(currentPos.x - newPos.x) > 0.1){
			Vector3 lerpPos = Lerp (currentPos, newPos, Time.deltaTime * SPEED);
			childLerp.position = lerpPos;
		}
	}


	Vector3 lastSyncPos;
	float extrapTimer = 0;
	float extrapTimeLimit = 0.25f; // 250 millisecond
	bool alreadyPredicted = false;
	int dir = 1;
	Vector3 predictedPos;
	void SyncExtrap(){

		Vector3 currentPos = childExtrap.position;
		
		Vector3 newPos = currentPos;
		newPos.x = sync_pos_x;

		// received a new packet
		if(lastSyncPos.x != sync_pos_x){

			// reset extrap stats
			alreadyPredicted = false;
			extrapTimer = 0;
			
			// which direction is it moving 
			float delta = newPos.x - currentPos.x;
			if(delta < 0) dir = -1;
			else if (delta > 0) dir = 1;
			else dir = 0;
			// dir = (int)Mathf.Sign(newPos.x - currentPos.x);  

			// smoothly transit to the new position received
			if(Mathf.Abs(delta) > 0.1f){
				Debug.Log("moving to the new packet postion");
				Vector3 lerpPos = Lerp (currentPos, newPos, Time.deltaTime * SPEED);
				childExtrap.position = lerpPos;
			}
			// reached new position
			else{
				// save synced position
				lastSyncPos = currentPos;
				lastSyncPos.x = sync_pos_x;

				// calcualte prediction
				// TODO: properly predict position
				predictedPos = lastSyncPos;
				predictedPos.x += dir;
				
				Debug.Log("reahed the new packet postion");	
				Debug.Log("moving to :" + dir);
				Debug.Log("currentPos :" + currentPos);
				Debug.Log("predictedPos :" + predictedPos);
			}
		}

		// no packets in coming
		// possible packet loss 
		else{
			Debug.Log("no new packet received");	
			
			// move further to prediction
			// with a time limit  
			if(!alreadyPredicted && extrapTimer < extrapTimeLimit){
				
				if(Mathf.Abs(currentPos.x - predictedPos.x) > 0.1f){
					Debug.Log("moving to the predicted position");	
					
					Vector3 extrapPos = Lerp(currentPos, predictedPos, Time.deltaTime * SPEED);
					childExtrap.position = extrapPos;
					
					extrapTimer += Time.deltaTime;
				}
				
				// TODO: shouldn't need to do this 
				// got to predicted position
				else{
					Debug.Log("reached the predicted position");

					alreadyPredicted = true;
					// temp
					extrapTimer = 0;
				}
			}

			// prediction finished 
			// smoothly move back to corrected postion 
			// else{
			// 	// reset extrap stats
			// 	alreadyPredicted = true;
			// 	extrapTimer = 0;

			// 	if(Mathf.Abs(currentPos.x - newPos.x) > 0.1){
			// 		Vector3 lerpPos = Lerp (currentPos, lastSyncPos, Time.deltaTime * SPEED);
			// 		childExtrap.position = lerpPos;
			// 	} 
			// }

		}

		// if(lastSyncX != sync_pos_x){
		// 	Vector3 extrapPos = Lerp(currentPos, newPos+newPos-currentPos, Time.deltaTime * SPEED);
        // 	childExtrap.position = extrapPos;
		// 	lastSyncX = sync_pos_x;
		// }
		// else{
		// 	if(Mathf.Abs(currentPos.x - newPos.x) > 0.1){
		// 		Vector3 lerpPos = Lerp (currentPos, newPos, Time.deltaTime * SPEED);
		// 		childExtrap.position = lerpPos;
		// 	}	
		// }
    }
	// ======================================================

}
