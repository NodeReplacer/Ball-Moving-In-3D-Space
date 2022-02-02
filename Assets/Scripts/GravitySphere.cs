using UnityEngine;

public class GravitySphere : GravitySource {

	[SerializeField]
	float gravity = 9.81f;
    
	[SerializeField, Min(0f)]
	float outerRadius = 10f;//Inner sphere range. Like the plane itself.
    [SerializeField, Min(0f)]
    float outerFalloffRadius = 15f; //outer range, effected by falloff, gets weaker as you
    //reach its limit.
    
    //Same as above but for inside out gravity.
    [SerializeField, Min(0f)]
	float innerFalloffRadius = 1f, innerRadius = 5f;
    
    //Falloff factors make the gravity weaker depending on how close
    //FallofRadius is to outerRadius.
    //It's established in OnValidate
    
    float innerFalloffFactor, outerFalloffFactor;
    
    public override Vector3 GetGravity (Vector3 position) {
		//position has been passed down through a 3 link chain.
        //MovingSphere/OrbitCamera->CustomGravity.GetGravity->GravitySources.GetGravity->
        //GravitySphere.GetGravity
        Vector3 vector = transform.position - position;
		float distance = vector.magnitude;
        if (distance > outerFalloffRadius || distance < innerFalloffRadius) {
            //if our distance is outside of our FalloffRadius
			return Vector3.zero; //Then we abort gravity
		}
        //gravity gets weaker as you go futher away
        //We express how far away we are as a ratio/decimal.
		float g = gravity / distance;
        
        //If we are on the outside of the inner yellow sphere
        //Then our FalloffFactor should reduce gravity here.
        //If we are inside (distnace>outerRadius) then gravity is stronger.
        //Our result is then multiplied by our g
        if (distance > outerRadius) {
			g *= 1f - (distance - outerRadius) * outerFalloffFactor;
		}
        else if (distance < innerRadius) {
			g *= 1f - (innerRadius - distance) * innerFalloffFactor;
		}
		return g * vector;
	}
    
    void Awake () {
		OnValidate();
	}
    
    void OnValidate () {
		innerFalloffRadius = Mathf.Max(innerFalloffRadius, 0f);
		innerRadius = Mathf.Max(innerRadius, innerFalloffRadius);
		outerRadius = Mathf.Max(outerRadius, innerRadius);
		outerFalloffRadius = Mathf.Max(outerFalloffRadius, outerRadius);
		
		innerFalloffFactor = 1f / (innerRadius - innerFalloffRadius);
		outerFalloffFactor = 1f / (outerFalloffRadius - outerRadius);
	}
    
	void OnDrawGizmos () {
		Vector3 p = transform.position;
		if (innerFalloffRadius > 0f && innerFalloffRadius < innerRadius) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(p, innerFalloffRadius);
		}
        Gizmos.color = Color.yellow;
        if (innerRadius > 0f && innerRadius < outerRadius) {
			Gizmos.DrawWireSphere(p, innerRadius);
		}
		Gizmos.DrawWireSphere(p, outerRadius);
		if (outerFalloffRadius > outerRadius) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(p, outerFalloffRadius);
		}
	}
}