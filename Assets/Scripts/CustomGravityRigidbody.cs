//This is for non player controlled objects.
//Under normal circumstance it will be affected by Unity's normal gravity.
//But we can't have that because we are customizing our own gravity right now.

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravityRigidbody : MonoBehaviour {
    
    float submergence;

	Vector3 gravity;
    
	Rigidbody body;
    //Our idea is robust, but we assume gravity is constant.
    //A sleeping body will remain constant even if gravity were to suddenly flip.
    //Bodies might also move very slowly, the floor might disappear while
    //we are floating but not sleeping.
    //Collision is a real bastard.
    [SerializeField]
	bool floatToSleep = false;

    [SerializeField]
	float submergenceOffset = 0.5f;

	[SerializeField, Min(0.1f)]
	float submergenceRange = 1f;

	[SerializeField, Min(0f)]
	float buoyancy = 1f;

	[SerializeField, Range(0f, 10f)]
	float waterDrag = 1f;

	[SerializeField]
	LayerMask waterMask = 0;
    
    [SerializeField]
	Vector3 buoyancyOffset = Vector3.zero;
    
    float floatDelay;//A delay created for static bodies to check
    //if an object is hovering in place for a moment for various reasons
    //In floatDelay we assume the object is floating but might still fall.
    //Always resets to zero except when the velocity is below the threshold.
    
	void Awake () {
		body = GetComponent<Rigidbody>();
		body.useGravity = false;
	}
    
    void FixedUpdate () {
        if (floatToSleep) {
            //Applying gravity ourselves means the object will never sleep.
            //Sleep is a process that effectively puts an object in stasis,
            //reducing the amount of work it has to do.
            if (body.IsSleeping()) {
                floatDelay = 0f;
                return;
            }
            
            //we are constantly applying acceleration so it will never sleep
            //unless we force it to.
            if (body.velocity.sqrMagnitude < 0.0001f) {
                floatDelay += Time.deltaTime;
                if (floatDelay >= 1f) {
                    return;
                }
            }
            else {
                floatDelay = 0f;
            }
        }
        gravity = CustomGravity.GetGravity(body.position);
		if (submergence > 0f) {
			float drag = Mathf.Max(0f, 1f - waterDrag * submergence * Time.deltaTime);
			body.velocity *= drag;
			body.angularVelocity *= drag;
			body.AddForceAtPosition(gravity * - (buoyancy * submergence),transform.TransformPoint(buoyancyOffset),ForceMode.Acceleration);
            submergence = 0f;
		}
        body.AddForce(gravity, ForceMode.Acceleration);
	}
    //Standard suite of water based floating tests.
    void OnTriggerEnter (Collider other) {
		if ((waterMask & (1 << other.gameObject.layer)) != 0) {
			EvaluateSubmergence();
		}
	}

	void OnTriggerStay (Collider other) {
		if (!body.IsSleeping() && (waterMask & (1 << other.gameObject.layer)) != 0) {
			EvaluateSubmergence();
		}
	}
	//Same as in MovingSphere but we calculate the up axis only when needed 
    //and don't support connected bodies.
	void EvaluateSubmergence () {
		Vector3 upAxis = -gravity.normalized;
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
	}
}