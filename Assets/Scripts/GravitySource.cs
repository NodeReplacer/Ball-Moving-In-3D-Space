using UnityEngine;

public class GravitySource : MonoBehaviour {

    //virtual means that this method will be overwritten
    //if a lower level has its own.
    //Usually CustomGravity calls this
    //But if GravityPlane/GravitySphere exist then the virtual method
    //will JMP to their instructions instead.
	public virtual Vector3 GetGravity (Vector3 position) {
		return Physics.gravity;
	}
    void OnEnable () {
		CustomGravity.Register(this);
	}
	void OnDisable () {
		CustomGravity.Unregister(this);
	}
}
