using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour {
    [SerializeField]
	Transform focus = default;
	[SerializeField, Range(1f, 20f)]
	float distance = 5f;
    [SerializeField, Min(0f)]
	float focusRadius = 1f; //Keeping the focus point in the exact center might feel too rigid.
    //This gives it some drag.
    [SerializeField, Range(0f, 1f)]
	float focusCentering = 0.5f; //If only using focusRadius the camera won't drift towards
    //the center of the object. This number divides the distance again and again until
    //we can sneakily make focusPoint = targetPoint.
    [SerializeField, Range(-89f, 89f)]
	float minVerticalAngle = -30f, maxVerticalAngle = 60f; //Min and max camera angles.
    [SerializeField, Min(0f)]
	float alignDelay = 5f; //Automatic alignment. But it is important that the player can override
    //this at any time and the automatic rotation doesn't immediately kick back in.
    [SerializeField, Range(1f, 360f)]
	float rotationSpeed = 90f;
    //Automatic alignment speed now determined by the difference
    //between our curent angle and our desired angle.
    [SerializeField, Range(0f, 90f)]
	float alignSmoothRange = 45f;
    //Adjust the speed that the camera aligns its up vector
	[SerializeField, Min(0f)]
	float upAlignmentSpeed = 360f;//In degrees per second.
	
	//Yeah, next is layer masks. It lets us ignore some layered objects
    //but we have to include such objects in the mask.
    [SerializeField]
	LayerMask obstructionMask = -1;
    
    Vector3 focusPoint, previousFocusPoint;
    //We'll change this variable to determine which way the camera faces.
	//cameraFacingAngles is a bit too long of a variable name.
    Vector2 orbitAngles = new Vector2(45f, 0f);
    
	//Our orbit camera is still a little awkward on account of it still using
	//Vector3.up instead of taking into account complex gravity
	Quaternion gravityAlignment = Quaternion.identity;
	//The orbit rotation logic must not be aware of the gravity's alignment.
	//So we keep track of the orbit rotation separately.
	Quaternion orbitRotation;
	
    Camera regularCamera;
    float lastManualRotationTime;
    
    //A box cast requires 3d vectors that contains the half extends of a box.
    //Half its width, height, and depth.
    Vector3 CameraHalfExtends {
		get {
			Vector3 halfExtends;
			halfExtends.y =
				regularCamera.nearClipPlane *
				Mathf.Tan(0.5f * Mathf.Deg2Rad * regularCamera.fieldOfView);
			halfExtends.x = halfExtends.y * regularCamera.aspect;
			halfExtends.z = 0f;
			return halfExtends;
		}
	}
    void OnValidate () {
		if (maxVerticalAngle < minVerticalAngle) {
			maxVerticalAngle = minVerticalAngle;
		}
	}
    void Awake () {
        //With relaxed focus we need to know where we WANT to be.
		regularCamera = GetComponent<Camera>();
        focusPoint = focus.position;
        transform.localRotation = orbitRotation = Quaternion.Euler(orbitAngles);
	}
	
    //LateUpdate is called after all update functions are called.
    void LateUpdate () {
		//Adjust our camera alignment so it is the same as the new up direction of gravity
		//Minimal rotation can be found with FromToRotation which takes two directions
		//and creates a rotation that will take us from one direction to the other.
		//So in our case our directions are our current up direction (before the change in
		//gravity), and the new gravity we have just walked into (or changed ourselves).
		//We take our time to prevent a whiplash inducing camera change
		UpdateGravityAlignment();
		
		//Anyways, LateUpdate goes last, we basically take the "real" up direction
		//(Vector3.up) and our current "fake" up direction. Then
		//multiply that with the current alignment to end up with the new one.
        UpdateFocusPoint();
		
		//Quaternion lookRotation;
		if (ManualRotation() || AutomaticRotation()) {
			ConstrainAngles();
			//lookRotation = Quaternion.Euler(orbitAngles);
			orbitRotation = Quaternion.Euler(orbitAngles);
		}
		
		//In a normal place, gravity Alignment is just Vector3.up
		Quaternion lookRotation = gravityAlignment * orbitRotation;
		
		//Quaternion lookRotation = Quaternion.Euler(orbitAngles);
		Vector3 lookDirection = lookRotation * Vector3.forward;
		Vector3 lookPosition = focusPoint - lookDirection * distance;
        
        //The above two variables assumed that the focusPoint was where
        //we were always looking but we have implemented camera lag to 
        //catch up with our object, so that is not always true.
        //We must resolve that or we can clip into objects.
        Vector3 rectOffset = lookDirection * regularCamera.nearClipPlane;
		Vector3 rectPosition = lookPosition + rectOffset;
		Vector3 castFrom = focus.position;
		Vector3 castLine = rectPosition - castFrom;
		float castDistance = castLine.magnitude;
		Vector3 castDirection = castLine / castDistance;
        
        //Check if we are clipping through an object before we hit our focus sphere.
        //The HalfExtends arguments have to be added as a second argument, along with the box's 
        //rotation as a new fifth argument.
        if (Physics.BoxCast(
		castFrom, CameraHalfExtends, castDirection, out RaycastHit hit,
		lookRotation, castDistance, obstructionMask,  QueryTriggerInteraction.Ignore)) {
			rectPosition = castFrom + castDirection * hit.distance;
			lookPosition = rectPosition - rectOffset;
		}
		transform.SetPositionAndRotation(lookPosition, lookRotation);
	}
    
	//Tell our camera which direction or new gravity is aligned.
	void UpdateGravityAlignment () {
		Vector3 fromUp = gravityAlignment * Vector3.up;
		Vector3 toUp = CustomGravity.GetUpAxis(focusPoint);
		
		//take the dot product of fromUp and toUp
		float dot = Mathf.Clamp(Vector3.Dot(fromUp, toUp), -1f, 1f);
		//Convert the results into degrees (Acos) because right now it's
		//just a number between -1 to 1.
		//But Acos returns in radians so convert from radians to degrees with
		//Mathf.Rad2Deg
		float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
		//MaxAngle allowed is our AlignmentSpeed * time.
		float maxAngle = upAlignmentSpeed * Time.deltaTime;
		
		Quaternion newAlignment =
			Quaternion.FromToRotation(fromUp, toUp) * gravityAlignment;
		if (angle <= maxAngle) {
			gravityAlignment = newAlignment;
		}
		else {
			gravityAlignment = Quaternion.SlerpUnclamped(
				gravityAlignment, newAlignment, maxAngle / angle
			);
		}
	}
	
    void UpdateFocusPoint () {
		previousFocusPoint = focusPoint;
        Vector3 targetPoint = focus.position;
		//We want to compare where we WANT to be with how far we allow ourselves
        //to wander away from that point.
        if (focusRadius > 0f) {
			float distance = Vector3.Distance(targetPoint, focusPoint);
            float t = 1f;
            //This is the slow centering if statement.
			if (distance > 0.01f && focusCentering > 0f) {
				t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
			}
			if (distance > focusRadius) {
                t = Mathf.Min(t, focusRadius / distance);
			}
            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
		}
		else {
			focusPoint = targetPoint;
		}
	}
    
    bool ManualRotation () {
		Vector2 input = new Vector2(
			Input.GetAxis("Vertical Camera"),
			Input.GetAxis("Horizontal Camera")
		);
		const float e = 0.001f;
		if (input.x < -e || input.x > e || input.y < -e || input.y > e) {
			orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
            lastManualRotationTime = Time.unscaledTime;
            return true;
		}
        return false;
	}
    //Clamps vertical angle to the configured range.
    void ConstrainAngles () {
		orbitAngles.x =
			Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

		if (orbitAngles.y < 0f) {
			orbitAngles.y += 360f;
		}
		else if (orbitAngles.y >= 360f) {
			orbitAngles.y -= 360f;
		}
	}
    
    //Used to return to normal. It figures out what movement vector we need.
    bool AutomaticRotation () {
		if (Time.unscaledTime - lastManualRotationTime < alignDelay) {
			return false;
		}
		
		//We need to undo gravity alignment
		Vector3 alignedDelta =
			Quaternion.Inverse(gravityAlignment) *
			(focusPoint - previousFocusPoint);
		Vector2 movement = new Vector2(alignedDelta.x, alignedDelta.z);
		
		float movementDeltaSqr = movement.sqrMagnitude;
		if (movementDeltaSqr < 0.000001f) {
			return false;
		}
        
        float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));
        //Smooth out the speed of the rotation to prevent jarring snap camera movements.
        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
		if (deltaAbs < alignSmoothRange) {
			rotationChange *= deltaAbs / alignSmoothRange;
		}
        else if (180f - deltaAbs < alignSmoothRange) {
			rotationChange *= (180f - deltaAbs) / alignSmoothRange;
		}
        orbitAngles.y =
			Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);
		return true;
	}
    
    //Convert a 2D direction to an angle
    static float GetAngle (Vector2 direction) {
		float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
		return direction.x < 0f ? 360f - angle : angle;
	}
    
}