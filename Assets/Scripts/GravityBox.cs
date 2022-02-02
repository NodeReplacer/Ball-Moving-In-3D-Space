//The idea of a gravity box is the same as the inside of the sphere but the difference
//is that instead of the gravity smoothly transitioning
//it hard changes betwee each face.

using UnityEngine;

public class GravityBox : GravitySource {
    
	[SerializeField]
	float gravity = 9.81f;
	[SerializeField]
	Vector3 boundaryDistance = Vector3.one;
    //The inner distance is a lot like the sphere
    [SerializeField, Min(0f)]
	float innerDistance = 0f, innerFalloffDistance = 0f;
    //And an outer distance. Same as the sphere, so we can crawl along the back
    //of some arbitrary cube.
    [SerializeField, Min(0f)]
	float outerDistance = 0f, outerFalloffDistance = 0f;
    
    float innerFalloffFactor, outerFalloffFactor;
    
    //Similar to the sphere.
    //We need our position, and use it to find distances.
    //
    public override Vector3 GetGravity (Vector3 position) {
        //Inverse transform direction is given the positions of
        //the cube itself and itself to match any arbitrary rotation
        //of that cube.
		position =
			transform.InverseTransformDirection(position - transform.position);
		Vector3 vector = Vector3.zero;
        
        //int outside is the marker. This train of if statements
        //is the process that we use to check if we are inside or outside the box.
        int outside = 0;
        if (position.x > boundaryDistance.x) {
			//Check if we are beyond the right face
            vector.x = boundaryDistance.x - position.x;
			outside = 1;
		}
        else if (position.x < -boundaryDistance.x) {
            //if not then check if we are beyond the left face.
            vector.x = -boundaryDistance.x - position.x;
			outside = 1;
		}
        
        //Do the same as above but checking the y and z faces independently.
        if (position.y > boundaryDistance.y) {
            vector.y = boundaryDistance.y - position.y;
			outside += 1;
		}
		else if (position.y < -boundaryDistance.y) {
			vector.y = -boundaryDistance.y - position.y;
			outside += 1;
		}
        //check the z face here
		if (position.z > boundaryDistance.z) {
			vector.z = boundaryDistance.z - position.z;
			outside += 1;
		}
		else if (position.z < -boundaryDistance.z) {
			vector.z = -boundaryDistance.z - position.z;
			outside += 1;
		}
        //Now we use the variable "outside" here.
        
        if (outside > 0) {
            //This sentence below says: "If we are only outside one
            //face then I assume we are directly above it and no other.
            //therefore we take the absolute sum of vector components
            //which is quicker than calculating the length of an
            //arbitrary vector"
			float distance = outside == 1 ?
				Mathf.Abs(vector.x + vector.y + vector.z) : vector.magnitude;
			//standard if we are farther out than the fallOff check.
            if (distance > outerFalloffDistance) {
				return Vector3.zero;
			}
			float g = gravity / distance;
			if (distance > outerDistance) {
				g *= 1f - (distance - outerDistance) * outerFalloffFactor;
			}
			return transform.TransformDirection(g * vector);
		}
        
		Vector3 distances;
		distances.x = boundaryDistance.x - Mathf.Abs(position.x);
		distances.y = boundaryDistance.y - Mathf.Abs(position.y);
		distances.z = boundaryDistance.z - Mathf.Abs(position.z);
		//distance has no direction in it, so we can compare which
        //face we are closest to (x, y, or z) directly.
        //Once we know which face we are closest to we GetGravityComponent
        //for that direction. GetGravityComponent already checks to know
        //which of the two x faces we are closer to (the coordinate variable 
        //comparison at the end)
        
        if (distances.x < distances.y) {
			if (distances.x < distances.z) {
				vector.x = GetGravityComponent(position.x, distances.x);
			}
			else {
				vector.z = GetGravityComponent(position.z, distances.z);
			}
		}
		else if (distances.y < distances.z) {
			vector.y = GetGravityComponent(position.y, distances.y);
		}
		else {
			vector.z = GetGravityComponent(position.z, distances.z);
		}
		return transform.TransformDirection(vector);
	}
    
	void Awake () {
		OnValidate();
	}
	void OnValidate () {
		//boundaryDistance works like the radius but for a rectangle.
        boundaryDistance = Vector3.Max(boundaryDistance, Vector3.zero);
        
        //We can't use radius we have to find an inner distance (relative to the boundaries)
        //and an innerFallOffDistance relative to the inner distance
        float maxInner = Mathf.Min(
			Mathf.Min(boundaryDistance.x, boundaryDistance.y), boundaryDistance.z
		);
		innerDistance = Mathf.Min(innerDistance, maxInner);
		innerFalloffDistance = Mathf.Max(Mathf.Min(innerFalloffDistance, maxInner), innerDistance);
        
        outerFalloffDistance = Mathf.Max(outerFalloffDistance, outerDistance);
        
        innerFalloffFactor = 1f / (innerFalloffDistance - innerDistance);
        outerFalloffFactor = 1f / (outerFalloffDistance - outerDistance);
	}
	void OnDrawGizmos () {
		Gizmos.matrix =
			Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Vector3 size;
		if (innerFalloffDistance > innerDistance) {
			Gizmos.color = Color.cyan;
			size.x = 2f * (boundaryDistance.x - innerFalloffDistance);
			size.y = 2f * (boundaryDistance.y - innerFalloffDistance);
			size.z = 2f * (boundaryDistance.z - innerFalloffDistance);
			Gizmos.DrawWireCube(Vector3.zero, size);
		}
		if (innerDistance > 0f) {
			Gizmos.color = Color.yellow;
			size.x = 2f * (boundaryDistance.x - innerDistance);
			size.y = 2f * (boundaryDistance.y - innerDistance);
			size.z = 2f * (boundaryDistance.z - innerDistance);
			Gizmos.DrawWireCube(Vector3.zero, size);
		}
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(Vector3.zero, 2f * boundaryDistance);
        
        //Standard lock and load. If outerDistance exists then we draw it if it doesn't
        //then it's not gonna make a difference.
        if (outerDistance > 0f) {
			Gizmos.color = Color.yellow;
			DrawGizmosOuterCube(outerDistance);
		}
		if (outerFalloffDistance > outerDistance) {
			Gizmos.color = Color.cyan;
			DrawGizmosOuterCube(outerFalloffDistance);
		}
	}
    //Bears great similarity to the process in GravitySphere
    float GetGravityComponent (float coordinate, float distance) {
        //If distance is greater than innerFallofDistance then
        //We are in the null gravity zone.
        if (distance > innerFalloffDistance) {
			return 0f;
		}
        float g = gravity;
        
        if (distance > innerDistance) {
			g *= 1f - (distance - innerDistance) * innerFalloffFactor;
		}
        //coordinate tells us which side of the center we are on.
        //if coordinate is less than 0 then we are on the opposite side
        //and need to flip gravity to fall to the correct side
		return coordinate > 0f ? -g : g;
	}
    
    //Drawing the gizmo for this is a lot tougher than it seems.
    //To put it this way, what happens if we walk off the edge?
    //The gravity would pull us off the edge but there is also a gravity
    //that is trying to pull us onto other face because all edges have
    //at least two faces attached to them. So we'll get stuck on an edge
    //Therefore gravity can't change suddenly or harshly on the outside
    //Though it can on the inside.
    //
    //But there's no way to visualize a rounded cube easily to represent
    //that gravity is weaker on the edges, so we need to carve on ourselves
    void DrawGizmosRect (Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
		Gizmos.DrawLine(a, b);
		Gizmos.DrawLine(b, c);
		Gizmos.DrawLine(c, d);
		Gizmos.DrawLine(d, a);
	}
    void DrawGizmosOuterCube (float distance) {
		//It looks kinda intimidating but its not.
        //We take the boundary distance for the outer face
        //load them into points and then draw the rectangle
        Vector3 a, b, c, d;
		//This block draws one face only
        a.y = b.y = boundaryDistance.y;
		d.y = c.y = -boundaryDistance.y;
		b.z = c.z = boundaryDistance.z;
		d.z = a.z = -boundaryDistance.z;
		a.x = b.x = c.x = d.x = boundaryDistance.x + distance;
		DrawGizmosRect(a, b, c, d);
        
        //To draw the opposite face from the one we just drew we can mirror
        a.x = b.x = c.x = d.x = -a.x;
		DrawGizmosRect(a, b, c, d);
        
        a.x = d.x = boundaryDistance.x;
		b.x = c.x = -boundaryDistance.x;
		a.z = b.z = boundaryDistance.z;
		c.z = d.z = -boundaryDistance.z;
		a.y = b.y = c.y = d.y = boundaryDistance.y + distance;
		DrawGizmosRect(a, b, c, d);
		a.y = b.y = c.y = d.y = -a.y;
		DrawGizmosRect(a, b, c, d);

		a.x = d.x = boundaryDistance.x;
		b.x = c.x = -boundaryDistance.x;
		a.y = b.y = boundaryDistance.y;
		c.y = d.y = -boundaryDistance.y;
		a.z = b.z = c.z = d.z = boundaryDistance.z + distance;
		DrawGizmosRect(a, b, c, d);
		a.z = b.z = c.z = d.z = -a.z;
		DrawGizmosRect(a, b, c, d);
        
        //So while we COULD make a rounded cube and die underneath the programming
        //effort that would take, how about we just make a single wireframe cube to represent
        //the reduced boundaries instead.
        distance *= 0.5773502692f;
		Vector3 size = boundaryDistance;
		size.x = 2f * (size.x + distance);
		size.y = 2f * (size.y + distance);
		size.z = 2f * (size.z + distance);
		Gizmos.DrawWireCube(Vector3.zero, size);
	}
}