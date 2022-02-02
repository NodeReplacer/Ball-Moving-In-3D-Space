using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class DetectionZone : MonoBehaviour
{
    [SerializeField]
    UnityEvent onFirstEnter = default, onLastExit = default;
    
    List<Collider> colliders = new List<Collider>();
    
    void Awake () {
        //Start the object disabled because passively fixedUpdate will get heavy
        //on the CPU.
		enabled = false;
	}
    void OnTriggerEnter(Collider other) {
        if (colliders.Count == 0) {
            onFirstEnter.Invoke();
            enabled = true;
        }
        colliders.Add(other);
    }
    void OnTriggerExit(Collider other) {
        if (colliders.Remove(other) && colliders.Count==0) {
            onLastExit.Invoke();
            enabled = false;
        }
    }
    void FixedUpdate () {
		//We need to check if a collider is destroyed while inside.
        for (int i = 0; i < colliders.Count; i++) {
			Collider collider = colliders[i];
			if (!collider || !collider.gameObject.activeInHierarchy) {
				//If not found remove the object at i and then decrement.
                colliders.RemoveAt(i--);
				if (colliders.Count == 0) {
					onLastExit.Invoke();
                    enabled = false;
				}
			}
		}
	}
    //If someone destroys this object.
    void OnDisable () {
#if UNITY_EDITOR
		if (enabled && gameObject.activeInHierarchy) {
			return;
		}
#endif
		if (colliders.Count > 0) {
			colliders.Clear();
			onLastExit.Invoke();
		}
	}
}
