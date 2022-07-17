using UnityEngine;

public class MovingSphere : MonoBehaviour {
    //THIS IS INCREDIBLY IMPORTANT IT IS WHAT DETERMINES RELATIVE CAMERA MOVEMENT.
    [SerializeField]
	Transform playerInputSpace = default, ball = default;
    
    [SerializeField, Range (0f,100f)]
    float maxSpeed = 10f, //Usually our max speed is 1. Which is LOW. 1 meter a second.
    //Multiplying it by a maxSpeed value that the user can decide will change that.
    maxClimbSpeed = 2f, //Change the climbing speed to help the player keep up with their moving.
    maxSwimSpeed = 5f;
    
    [SerializeField, Range(0f, 100f)] //Accelerations are used in the AdjustVelocity method.
	float 
        maxAcceleration = 10f,
        maxAirAcceleration = 1f,
        maxClimbAcceleration = 20f,
        maxSwimAcceleration = 5f;
    //It might make sense to make it harder to control in the air.
    //though it is less responsive.
    /*
    [SerializeField]
	Rect allowedArea = new Rect(-5f, -5f, 10f, 10f);
    [SerializeField, Range(0f, 1f)]
	float bounciness = 0.5f;
    */
    [SerializeField, Range(0f, 10f)]
	float jumpHeight = 2f;
    [SerializeField, Range(0, 5)]
	int maxAirJumps = 0;
    [SerializeField, Range(0f, 90f)]
	float maxGroundAngle = 25f, maxStairsAngle = 50f; //Instead of using the slope's normal 
    //vector's y component, which goes from 0 - 1
    //We use the ground's angle instead because that would make it more intuitive.
    
    [SerializeField, Range(90, 180)]
	float maxClimbAngle = 140f; //when climbing, orientation is important.
    //We can't just climb the ceilings.
    
    //Our sphere gets launched away from us at high speeds.
    //Of course that's how it should be.
    //The maxSnapSpeed checks to overcome the normal movement speed.
    [SerializeField, Range(0f, 100f)]
	float maxSnapSpeed = 100f;
    //We always snap to ground beneath us no matter how far.
    //We shouldn't. We now limit the distance our probe goes.
    [SerializeField, Min(0f)]
	float probeDistance = 1f;
    [SerializeField]
	LayerMask probeMask = -1, stairsMask = -1, climbMask = -1,
    waterMask = 0; //Detect if we are touching water.
    
    [SerializeField] //This change in colour(material) will be used as a substitute for
    //a running animation vs climbing animation vs etc. for the sake of player clarity.
	Material 
        normalMaterial = default, 
        climbingMaterial = default,
		swimmingMaterial = default;
    MeshRenderer meshRenderer;
    
    Vector3 upAxis; //We relied on the Y axis being up, but this is not the case anymore, we need to establish
    //a new "up" relative to whatever we're standing on.
    
    //With relative up directions come relative left and right directions. I.E. on the ceiling.
    //otherwise we'll have reversed controls.
    //Like Vector.up, we replace Vector3.right
    Vector3 rightAxis, forwardAxis;
    
    bool InWater => submergence > 0f; //We need to know how far in we are to know when we start swimming
	float submergence;
    [SerializeField]
	float submergenceOffset = 0.5f;
	[SerializeField, Min(0.1f)]
	float submergenceRange = 1f;
    [SerializeField, Min(0f)]
	float buoyancy = 1f; //Another water based property
    [SerializeField, Range(0f, 10f)]
	float waterDrag = 1f; //Change our movement speed in water.
    [SerializeField, Range(0.01f, 1f)]
	float swimThreshold = 0.5f; //When I can start swimming
    [SerializeField, Min(0.1f)]
	float ballRadius = 0.5f;
    [SerializeField, Min(0f)]
	float ballAlignSpeed = 180f; //Make the ball rotation align with its forward motion.
    [SerializeField, Min(0f)]
	float ballAirRotation = 0.5f, ballSwimRotation = 2f;
    
    bool desiredJump, desiresClimbing;
    //bool onGround;
    int groundContactCount, steepContactCount, climbContactCount;
	bool OnGround => groundContactCount > 0;
    bool OnSteep => steepContactCount > 0;
    bool Climbing => climbContactCount > 0 && stepsSinceLastJump > 2; //For now we assume 
    //that we're automatically climbable if able.
    bool Swimming => submergence >= swimThreshold;
    
    Vector3 playerInput; //For now, player perspective is 2D. So our control is a 2d vector.
    Vector3 velocity, connectionVelocity;
    //We now need to keep track of velocity because we are using 
    //acceleration to manipulate and measure or movement
    //Which will be compared vs velocity for a variety of methods involving movement.
    //connectonVelocity is a new actor on our velocity, it figures out what speed
    //the connected rigidbody (and consequently the rest of the object) is moving at because the speed
    //of a touching surface should act upon the MovingSphere as well.
    
    Vector3 contactNormal, steepNormal, climbNormal, lastClimbNormal; //Gets the normal of all surfaces 
    //we are in contact with.
    //A steep contact is not a ceiling but is still too steep to count as the ground.
    //A climb normal checks the angle/facing of the wall we might try to climb.
    //A lastClimbNormal is for when we are in a crevasse. Usually we interpret that
    //as flat ground but we actually want to climb out if the two planes are steep
    //enough to allow it.
    
    //What if we touch an animated kinematic body? You can animate them after all,
    //but they don't have a velocity, due to being kinematic bodies.
    Vector3 connectionWorldPosition,
    connectionLocalPosition; //This is for rotations, because the position doesn't change
    //in a case like that so the sphere will think we are not moving.
    //ConnectionlocalPosition is like world position but it is in the connection body's
    //local space. We find connectionLocalPosition by invoking InverseTransformPoint on 
    //the connection body's Transform component
    
    Rigidbody body,
    //We need to know two things, what we are touching now and what
    //we just touched. This can be the same thing. If it IS
    //the same rigidbody then that means we have remained in contact
    //with something which indicates we shoul have moved along with it.
    connectedBody, previousConnectedBody; 
    
    int jumpPhase;
    
    //Let's say you had an angle. The dot product projects line A down onto line B
    //like it cast a straight shadow onto the below angle. But it doesn't always go down.
    //Basically it always makes a right angle.
    float minGroundDotProduct, minStairsDotProduct, minClimbDotProduct;
    
    int stepsSinceLastGrounded, stepsSinceLastJump;
    
    //To rotate in any direction we need our movement direction and the contact normal
    //Each physics step resets contactNormal to 0 so we must save it.
    Vector3 lastContactNormal; //Think of it this way: lastContactNormal is for cosmetic purposes.
    Vector3 lastSteepNormal, //Align to walls
    lastConnectionVelocity; //Ignore rotation when being moved by a platform.
    
    void OnValidate () {
		minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad); //Mathf.Cos still wants radians.
        minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
        minClimbDotProduct = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
	}
	void Awake () {
		body = GetComponent<Rigidbody>();
        body.useGravity = false;
        meshRenderer = ball.GetComponent<MeshRenderer>();
        OnValidate();
	}
    void Update() {
        //Vector2 playerInput; used to be here but has been promoted.
        /* keeping this in case everything goes wrong.
        playerInput.x = Input.GetAxis("Horizontal");
		playerInput.y = Input.GetAxis("Vertical");
        */
        
        playerInput.x = Input.GetAxis("Horizontal");
		playerInput.z = Input.GetAxis("Vertical");
		playerInput.y = Swimming ? Input.GetAxis("UpDown") : 0f;
        //The way it works is that playerInput.x and playerInput.y have maximums of 1 when the key is 
        //pressed.
        //But what if you pressed both wouldn't that Pythagorean theorem and be a bigger number but 
        //only in diagonal.
        //Yes. So we clamp the vectors.
        //playerInput.Normalize(); //We aren't using normalize because it's either 0 or 1 which is 
        //too much all or nothing.
        playerInput = Vector3.ClampMagnitude(playerInput, 1f);
        
        //So far we are immediately making position = input. That's not motion - that's teleportation.
        //We need an additional variable: "displacement" to make the movement look natural.
        
        //Just controlling acceleration (while it is how reality works) is actually very difficult to 
        //control.
        //And is honestly really messy. When you get velocity up to a certain point fighting back is 
        //difficult.
        //With velocity control moving that baby was easy as heck. It stopped and turned on a dime, 
        //but it was unnatural.
        
        //So we're going to mix the two for a happy balance.
        
        //Now we need to move in relation to the playerInputSpace, which is just
        //a more correct way of talking about "relative to the camera" in my case
        //but it doesn't NEED to be the camera.
        if (playerInputSpace) {
            //upAxis is the normal of whatever plane we are on.
            //We accept that, but use the normal to find the plane itself.
            //WE ARE NOT PROJECTING ANYTHING ONTO THE UPAXIS PLANE BECAUSE IT DOESN'T HAVE
            //A PLANE ASSOCIATED WITH IT.
            
            rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
			forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);
		}
		else {
            //Now if we aren't in a nonstandard gravity we can just use the
            //normal Vector3.right that's been built in.
            rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
			forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);
        }
        if (Swimming) {
			desiresClimbing = false;
		}
		else {
            //We establish we want to jump here but handle the jump logic in FixedUpdate.
            //But if Update hits its next frame before FixedUpdate fires desiredJump will be flipped 
            //back to zero on account of us not holding the button down at the time.
            //The symbol |= is an or value, it compares if true with itself. Meaning it 
            //will always be true until we explicitly turn it off.
            desiredJump |= Input.GetButtonDown("Jump");
            //Nearly the same as above but for climbing.
            desiresClimbing = Input.GetButton("Climb"); 
            //NOTE: desiresClimbing does not |= because we want the user to hold that
            //button down. We could configure it as a state.
        }
        UpdateBall(); //UpdateBall determines how our ball looks. That includes colour changes
        //and how it rotates.
    }
    void UpdateBall() {
        Material ballMaterial = normalMaterial;
        Vector3 rotationPlaneNormal = lastContactNormal;
        float rotationFactor = 1f;
		if (Climbing) {
			ballMaterial = climbingMaterial;
		}
		else if (Swimming) {
			ballMaterial = swimmingMaterial;
            rotationFactor = ballSwimRotation;
		}
        else if (!OnGround) {
			if (OnSteep) {
				lastContactNormal = lastSteepNormal;
			}
            else {
				rotationFactor = ballAirRotation;
			}
		}
		meshRenderer.material = ballMaterial;
        
        Vector3 movement = (body.velocity - lastConnectionVelocity) * Time.deltaTime;
        //We remove the upward direction from movement here so jumping
        //doesn't turn the ball.
        movement -= rotationPlaneNormal * Vector3.Dot(movement, rotationPlaneNormal);
		float distance = movement.magnitude;
        
        //Final piece of the puzzle. Rotate as the platform you are standing on rotates.
        Quaternion rotation = ball.localRotation;
		if (connectedBody && connectedBody == previousConnectedBody) {
			rotation = Quaternion.Euler(connectedBody.angularVelocity * (Mathf.Rad2Deg * Time.deltaTime)) * rotation;
			if (distance < 0.001f) {
				ball.localRotation = rotation;
				return;
			}
		}
		else if (distance < 0.001f) {
			return;
		}
        float angle = distance * rotationFactor * (180f / Mathf.PI) / ballRadius;
        //Remember how the cross product finds (in 3D space) a new vector that is
        //90 degerees to the two crossed vectors? Well what a coincidence that
        //we're rotating around that very axis right? (Like a spoke in a wheel)
        Vector3 rotationAxis = Vector3.Cross(rotationPlaneNormal, movement / distance);
		rotation = Quaternion.Euler(rotationAxis * angle) * rotation;
		if (ballAlignSpeed > 0f) {
			rotation = AlignBallRotation(rotationAxis, rotation, distance); //re-align the ball
            //to face the rolling direction based on distance travelled.
		}
		ball.localRotation = rotation;
    }
    
    //FixedUpdate is used with the PhysX which updates out of step with the framerate.
    //We used to update velocity in update which was out of sync with PhysX
    //This could cause problems updating speed a random number of times before the physx
    //process caught it. Making all forces stronger than usual.
    void FixedUpdate () { //Gets invoked at the start of each physics sim step.
        //velocity = body.velocity;
        //upAxis = -Physics.gravity.normalized;
        Vector3 gravity = CustomGravity.GetGravity(body.position, out upAxis);
        UpdateState();
        
        if (InWater) {
			velocity *= 1f - waterDrag * submergence * Time.deltaTime;
		}
        
        AdjustVelocity();
        
        if (desiredJump) {
			desiredJump = false;
            Jump(gravity);
		}
        //We accelerate towards the surface we are climbing
        //So we can climb around outer walls.
        if (Climbing) {
			velocity -= contactNormal * (maxClimbAcceleration * 0.9f * Time.deltaTime);
		}
        else if (InWater) {
			velocity += gravity * ((1f - buoyancy * submergence) * Time.deltaTime);
		}
        else if (OnGround && velocity.sqrMagnitude < 0.01f) {
			velocity += contactNormal * (Vector3.Dot(gravity, contactNormal) * Time.deltaTime);
		}
        else if (desiresClimbing && OnGround) {
			//if we want to climb but we're still on the ground we move slower.
            velocity += (gravity - contactNormal * (maxClimbAcceleration * 0.9f)) * Time.deltaTime;
		}
		else {
			velocity += gravity * Time.deltaTime;
		}
        
        body.velocity = velocity;
        //onGround = false;
        ClearState();
    }
    
    //Because we add normals together (+= instead of =) in case there are
    //multiple normals we need to clean out our contactNormal variable.
    //In fact we need to clear a bunch of variables.
    void ClearState() {
		//onGround = false;
        lastContactNormal = contactNormal;
        lastSteepNormal = steepNormal;
        lastConnectionVelocity = connectionVelocity;
		groundContactCount = steepContactCount = climbContactCount = 0;
        contactNormal = steepNormal = climbNormal = connectionVelocity = Vector3.zero;
        previousConnectedBody = connectedBody;
        connectedBody = null;
        submergence = 0f;
	}
    //This here to keep FixedUpdate short.
    void UpdateState () {
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;
		velocity = body.velocity;
        //Below's if statement is a list of checks to discover if
        //we are climbing,
        //we are swimming,
        //we are on the ground,
        //we have snapped to the ground,
        //we are stuck in a crevasse.
		if (CheckClimbing() || CheckSwimming() || OnGround || SnapToGround() || CheckSteepContacts()) {
            stepsSinceLastGrounded = 0;
			//We need to revisit air jumping. It used to always be assumed
            //that we hit the ground after the jump so we need to block fake landings.
            if (stepsSinceLastJump > 1) {
				jumpPhase = 0;
			}
            if (groundContactCount > 1) {
				contactNormal.Normalize();
			}
		}
        else { //If we air jump we are not touching the ground and not receiving any contact normal.
			contactNormal = upAxis;
		}
        
        if (connectedBody) {
            //There is a problem with pushing smaller objects out of the way.
            //Without checking mass in the below if statement a pushed block will
            //change our own velocity as if we were riding it.
            if (connectedBody.isKinematic || connectedBody.mass >= body.mass) {
				UpdateConnectionState();
			}
		}
	}
    //void Jump() {
    void Jump(Vector3 gravity) {
        Vector3 jumpDirection; //Now we're gonna implement wall jumping.
        //The if check below is now put here the direction of the jump we
        //are seeking is stored in jumpDirection and is determined by the
        //if statements.
        if (OnGround) {
			jumpDirection = contactNormal;
		}
		else if (OnSteep) {
			jumpDirection = steepNormal;
            jumpPhase = 0;
		}
        //The if list below checks we are actually have airjumps available.
		else if (maxAirJumps > 0 && jumpPhase <= maxAirJumps) {
			if (jumpPhase == 0) {
				jumpPhase = 1;
			}
            jumpDirection = contactNormal;
		}
		else {
			return;
		}
        //So now we are allowed to jump if we are on the ground or if we haven't
        //hit our max number of jumps.
		//if (OnGround || jumpPhase < maxAirJumps) {
		stepsSinceLastJump = 0;
        jumpPhase += 1;
        //Limit jumps speed so multiple jumps in quick succession don't break our max jump speed.
        float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * jumpHeight);
        if (InWater) {
			jumpSpeed *= Mathf.Max(0f, 1f - submergence / swimThreshold);
		}
        
        //give jumpDirection an upwards direction so you can ascend through wall jumping.
        //Unfortunately affects ALL jumps.
        jumpDirection = (jumpDirection + upAxis).normalized;
        
        float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
        //If we already have an updward speed then subtract it from the jump speed to limit it.
        if (alignedSpeed > 0f) {
			jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
		}
		//velocity.y += jumpSpeed;
		velocity += jumpDirection * jumpSpeed;
		//}
	}
    void OnCollisionEnter (Collision collision) {
		//onGround = true;
        EvaluateCollision(collision);
	}
    /*
	void OnCollisionExit () {
		onGround = false;
	}
    */
    
    //OnCollisionExit is not the best way to track if we're on the ground because
    //If we touch a wall and stop touching it we will have exited a collision right there.
    //We're still touching the ground but OnCollisionEnter doesn't check for active collisions
    //only if a collision has been ACTIVATED.
    void OnCollisionStay (Collision collision) {
		//onGround = true;
        EvaluateCollision(collision);
	}
    //We check if we are touching the ground by getting the normal of the thing we have
    //collided with.
    void EvaluateCollision(Collision collision) {
        if (Swimming) {
			return;
		}
        int layer = collision.gameObject.layer;
        float minDot = GetMinDot(layer);
        for (int i = 0; i < collision.contactCount; i++) {
			Vector3 normal = collision.GetContact(i).normal;
            //Our plane should have a normal of Y = 1
            //onGround |= normal.y >= minGroundDotProduct;
            float upDot = Vector3.Dot(upAxis, normal);
            if (upDot >= minDot) {
				//onGround = true;
				groundContactCount += 1;
				contactNormal += normal; //Combine normals in case we have multiple at once.
                connectedBody = collision.rigidbody;//Get a reference to a body that we are touching.
			}
            //So the above groundContactCheck did not return true, now we check 
            //if it is a slope/steep contact.
            //else if (upDot > -0.01f) {
            //We now need to check for both a steep and climb contact separately.
            else {
				if (upDot > -0.01f) {
                    steepContactCount += 1;
                    steepNormal += normal;
                    //If it is a slope contact we still need to keep a record of what we are touching.
                    //But we prefer the ground contact over the slope contact so we put it in this if statement.
                    if (groundContactCount == 0) {
                        connectedBody = collision.rigidbody;
                    }
                }
                if (desiresClimbing && upDot >= minClimbDotProduct && (climbMask & (1 << layer)) != 0) {
					climbContactCount += 1;
					climbNormal += normal;
					lastClimbNormal = normal;
					connectedBody = collision.rigidbody;
				}
			}
		}
    }
    
    //So it goes up the slope nicely but if we tried to go down the slope
    //we'd move directly right a bit then fall a bit to meet the slope below us
    //and then bounce down the slope instead of smoothly roll down.
    //So if we align our desired velocity with the ground beneath us we'll be able
    //to keep up with this.
    
    //We have now changed to accomodate arbitrary directions instead of assuming y is up.
    //Depending on the plane it could be that y is left.
    //Vector3 ProjectOnContactPlane (Vector3 vector) {
	//	return vector - contactNormal * Vector3.Dot(vector, contactNormal);
	//}
    
    //Same as above, but this time for arbitrary directions.
    Vector3 ProjectDirectionOnPlane (Vector3 direction, Vector3 normal) {
		return (direction - normal * Vector3.Dot(direction, normal)).normalized;
	}
    
    void AdjustVelocity () {
        float acceleration, speed;
		//Vector3 xAxis = ProjectDirectionOnPlane(rightAxis, contactNormal);
		//Vector3 zAxis = ProjectDirectionOnPlane(forwardAxis, contactNormal);
        
        //This section is the climbing logic
        Vector3 xAxis, zAxis;
		//Check if climbing
        if (Climbing) {
            acceleration = maxClimbAcceleration;
			speed = maxClimbSpeed;
            //if climbing then don't use our default right and forward 
            //input axes for X and Z.
            
            //Use he upAxis for Z and cross product of the contact normal
            //and upAxis for X.
            xAxis = Vector3.Cross(contactNormal, upAxis);
			zAxis = upAxis;
		}
        else if (InWater) {
			float swimFactor = Mathf.Min(1f, submergence / swimThreshold);
			acceleration = Mathf.LerpUnclamped(
                OnGround ? maxAcceleration : maxAirAcceleration, maxSwimAcceleration,
                swimFactor);
			speed = Mathf.LerpUnclamped(maxSpeed, maxSwimSpeed, swimFactor);
			xAxis = rightAxis;
			zAxis = forwardAxis;
		}
		else {
            acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
			speed = OnGround && desiresClimbing ? maxClimbSpeed : maxSpeed;
            //otherwise proceed as normal.
			xAxis = rightAxis;
			zAxis = forwardAxis;
		}
        //Below is the same as the ProjectDirectionOnPlane at the beginning.
        //the difference is which direction we are feeding in.
		xAxis = ProjectDirectionOnPlane(xAxis, contactNormal);
		zAxis = ProjectDirectionOnPlane(zAxis, contactNormal);
        //End of climbing logic. The xAxis and zAxis have been reoriented
        //to accomodate that we are on a wall but gravity is still pointing
        //in a different direction. (Our camera interprets "down" as gravity's
        //direction)
        
        Vector3 relativeVelocity = velocity - connectionVelocity;
        
        //Create an adjustment velocity.
        Vector3 adjustment;
		adjustment.x = playerInput.x * speed - Vector3.Dot(relativeVelocity, xAxis);
		adjustment.z = playerInput.z * speed - Vector3.Dot(relativeVelocity, zAxis);
        adjustment.y = Swimming ? playerInput.y * speed - Vector3.Dot(relativeVelocity, upAxis) : 0f;
        
        //Apply acceleration once to remove the bias the old system had.
        adjustment = Vector3.ClampMagnitude(adjustment, acceleration * Time.deltaTime);
        //Change in velocity is now the xAxis and zAxis scaled by their adjustments.
        velocity += xAxis * adjustment.x + zAxis * adjustment.z;
        //Do the same for swimming.
        if (Swimming) {
			velocity += upAxis * adjustment.y;
		}
        //NOTE: Fixing the bias makes the ball less responsive to user input as it
        //picks up acceleration. Turn the acceleration up to raise responsiveness.
	}
    
    bool SnapToGround() {
        //Snapping works and is configureable, but it acts even when we jump,
        //dragging us back to the cold hard earth.
        //We need to track the number of physics frame steps since our last jump.
        //Because of the collision data delay we're still considered grounded the 
        //step after the jump was initiated. So we must abort if we're two or fewer 
        //steps after a jump.
        //More accurately, after a jump command was issued.
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2) {
			return false;
		}
        //If we go fast enough off a ramp, we SHOULD be launched as opposed to the
        //unnatural sticking we have in front of us right now.
        float speed = velocity.magnitude;
		if (speed > maxSnapSpeed) {
			return false;
		}
        //We can make a raycast without sticking one on permanently.
        if (!Physics.Raycast(body.position, -upAxis, out RaycastHit hit, probeDistance, probeMask, QueryTriggerInteraction.Ignore)) {
			return false;
		}
        //hit vector includes the normal of the surface hit.
        //remember we can use the normal to determine whether it is the ground
        //because it will be facing "up"
        float upDot = Vector3.Dot(upAxis, hit.normal);
        if (upDot < GetMinDot(hit.collider.gameObject.layer)) {
			return false;
		}
        
        groundContactCount = 1;
		contactNormal = hit.normal;
		//float speed = velocity.magnitude;
		float dot = Vector3.Dot(velocity, hit.normal);
		if (dot > 0f) {
			velocity = (velocity - hit.normal * dot).normalized * speed;
		}
        //SnapToGround detects ground so we'll store the ground we're snapping to
        //while we have it because we need it around.
		connectedBody = hit.rigidbody;
        return true;
    }
    
    //Returns an appropriate minimum for a given layer.
    float GetMinDot (int layer) {
        //However, the mask is a bit mask, with one bit per layer. Specifically, 
        //if the stairs is the eleventh layer then it matches the eleventh bit. 
        //We can create a value with that single bit set by using 1 << layer, 
        //which applies the left-shift operator to the number 1 an amount of times 
        //equal to the layer index, which is ten. The result would be the binary 
        //number 10000000000.
		return (stairsMask & (1 << layer)) == 0 ?
			minGroundDotProduct : minStairsDotProduct;
	}
    
    //This checks if we are wedged into a narrow space.
    //CheckSteepContacts returns whether it succeeded in 
    //converting the steep contacts into virtual "ground". If there are 
    //multiple steep contacts then normalize them and check whether the 
    //result counts as ground. If so, return success, otherwise failure. 
    //In this case we don't have to check for stairs.
    bool CheckSteepContacts () {
		if (steepContactCount > 1) {
			steepNormal.Normalize();
			//minGroundDotProduct. It uses our MaxGroundAngle
            //so of course, if we're too steep for it then we can't go through.
            //upDot used to be steepNormal.y, the idea is the same, our relative
            //up direction.
            float upDot = Vector3.Dot(upAxis, steepNormal);
            if (upDot >= minGroundDotProduct) {
				groundContactCount = 1;
				contactNormal = steepNormal;
				return true;
			}
		}
		return false;
	}
    
    //I'm worried because the position of something isn't necessarily where I think it is.
    //like how probuilder cubes are always positive in relation to their position on all
    //axes. (the position would be "0,0,0" while the other corners would be like 1,1,1 etc.)
    void UpdateConnectionState () {
        //We find velocity clasically, by taking two positions, finding the difference
        //and then dividing the difference by the time elapsed.
        
        //The if statement here just makes sure that the thing we are in contact with is the 
        //same object, otherwise if two things hit us at once we'll take off like a jet
        //engine.
        if (connectedBody == previousConnectedBody) {
            Vector3 connectionMovement =
                connectedBody.transform.TransformPoint(connectionLocalPosition) -
                connectionWorldPosition;
            connectionVelocity = connectionMovement / Time.deltaTime;
        }
		connectionWorldPosition = body.position;
		connectionLocalPosition = connectedBody.transform.InverseTransformPoint(connectionWorldPosition);
	}
    bool CheckClimbing () {
		if (Climbing) {
            //Our new crevasse check means we need to determine
            //if we have multiple climbContactNormals
			if (climbContactCount > 1) {
                //Normalize the climbNormal to check if it counts as ground
                //as determined by our minGroundDotProduct
				climbNormal.Normalize();
				float upDot = Vector3.Dot(upAxis, climbNormal);
				if (upDot >= minGroundDotProduct) {
					climbNormal = lastClimbNormal;
				}
			}
            groundContactCount = 1;
			contactNormal = climbNormal;
			return true;
		}
		return false;
	}
    
    void OnTriggerEnter (Collider other) {
		if ((waterMask & (1 << other.gameObject.layer)) != 0) {
			EvaluateSubmergence(other);
		}
	}
	void OnTriggerStay (Collider other) {
		if ((waterMask & (1 << other.gameObject.layer)) != 0) {
			EvaluateSubmergence(other);
		}
	}
    
    //submergenceOffset is the offset from the sphere's center.
    //subermergenceRange goes down from the point that offset finds straight down.
    //So offset goes up by 0.5 and range goes down by 1 by default.
    
    //EXPLANATION OF RAYCAST. Our starting point is from the submergenceOffset.
    //because offset is just a float right now we multiply the upAxis into it
    //to give it an actual direction instead of being a meaningless magnitude.
    //The second argument is the direction we point the raycast. Downwards, so
    //opposing the upAxis.
    //The fourth argument "subemergenceRange + 1f" accomodates other code. Usually
    //when we are completely submerged the ray hits nothing and the sphere 
    //says we are not submerged anymore. That's false. But EvaluateSubmergence
    //is only called when a waterMask contact is made so if the contact is made,
    //but the raycast hits nothing we can say we are underwater.
    //But that may cause an invalid submergence. Our collision and triggers are
    //invoked at a delay so just as we leave, our body.position may be out of
    //the water but OnTriggerStay might have just fire checking for submergence.
    void EvaluateSubmergence (Collider collider) {
        if (Physics.Raycast(
			body.position + upAxis * submergenceOffset,
			-upAxis, out RaycastHit hit, submergenceRange + 1f,
			waterMask, QueryTriggerInteraction.Collide
		)) {
            submergence = 1f - hit.distance / submergenceRange;
		}
        else {
			submergence = 1f;
		}
        
        if (Swimming) {
			connectedBody = collider.attachedRigidbody;
		}
	}
    //This is one of the checkState methods. It disables snapping when we're swimming but not always 
    //when we're in water.
    bool CheckSwimming () {
		if (Swimming) {
			groundContactCount = 0;
			contactNormal = upAxis;
			return true;
		}
		return false;
	}
    
    public void PreventSnapToGround() {
        stepsSinceLastJump = -1;
    }
    
    Quaternion AlignBallRotation (Vector3 rotationAxis, Quaternion rotation, float traveledDistance) {
		Vector3 ballAxis = ball.up;
		float dot = Mathf.Clamp(Vector3.Dot(ballAxis, rotationAxis), -1f, 1f);
		float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
		float maxAngle = ballAlignSpeed * traveledDistance;

		Quaternion newAlignment =
			Quaternion.FromToRotation(ballAxis, rotationAxis) * rotation;
		if (angle <= maxAngle) {
			return newAlignment;
		}
		else {
			return Quaternion.SlerpUnclamped(
				rotation, newAlignment, maxAngle / angle
			);
		}
	}
    
}