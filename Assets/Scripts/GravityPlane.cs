using UnityEngine;

public class GravityPlane : GravitySource {

	[SerializeField]
	float gravity = 9.81f;
    [SerializeField, Min(0f)]
	float range = 1f; //Without this the effect of the plane will continue forever.
    //Range tells us how far above (relative to the plane) the gravity will work.
    
    //to override a method you must explicitly call
    //override.
	public override Vector3 GetGravity (Vector3 position) {
        Vector3 up =  transform.up; //Get the plane's upward orientation.
        //to get the distance
        float distance = Vector3.Dot(up, position - transform.position);
		if (distance > range) {
			return Vector3.zero;
		}
        float g = -gravity;
		if (distance > 0f) {
			g *= 1f - distance / range;
		}
        return -gravity * up;
	}
    //To make our planes easier to see, we'll draw gizmos.
    void OnDrawGizmos () {
		Vector3 scale = transform.localScale;
		scale.y = range;
        //If we used transform.localToWorldMatrix
        //We would take the scale as well, which we don't want because range
        //should be clear just given the number the maker input.
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
        Vector3 size = new Vector3(1f, 0f, 1f);
        Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(Vector3.zero, size);
        if (range > 0f) {
            Gizmos.color = Color.cyan;
		    Gizmos.DrawWireCube(Vector3.up, size);
        }
	}
}