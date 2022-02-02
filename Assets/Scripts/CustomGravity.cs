using UnityEngine;
using System.Collections.Generic;

public static class CustomGravity {
	
	//Each of the methods used to just operate with one gravity.
	static List<GravitySource> sources = new List<GravitySource>();
	//Now that there is a list, each method will now accumulate the gravity
	//from all sources.
	//But we do take the upAxis which needs to be the total g but normalized.
	
	//ABOUT LISTS:
	//This is not the best approach for many gravity sources but good for a few.
	//Also, spaces with many gravity sources are not fun or easy to navigate.
	
    //Two different GetGravity functions. One for normal gravity
    //the other for nonstandard gravity.
    public static Vector3 GetGravity (Vector3 position) {
		Vector3 g = Vector3.zero;
		for (int i = 0; i < sources.Count; i++) {
			g += sources[i].GetGravity(position);
		}
		return g;
	}
	//out is almost like having another return value but not really.
    //It tells us that the method is responsible for 
    //correctly setting the parameter, replacing its previous value. 
    //Not assigning a value to it will produce a compiler error.
    public static Vector3 GetGravity (Vector3 position, out Vector3 upAxis) {
		Vector3 g = Vector3.zero;
		for (int i = 0; i < sources.Count; i++) {
			g += sources[i].GetGravity(position);
		}
		upAxis = -g.normalized;
		return g;
	}
    public static Vector3 GetUpAxis (Vector3 position) {
		Vector3 g = Vector3.zero;
		for (int i = 0; i < sources.Count; i++) {
			g += sources[i].GetGravity(position);
		}
		return -g.normalized;
	}
	
	//List management functions
	//The idea is that a single source is only registered once
	//or else it will be multiplied.
	//likewise, only unregister a source that's already been registered.
	public static void Register (GravitySource source) {
		//Debug.Assert means if the first argument is false then 
		//it logs an assertion error.
		Debug.Assert(
			!sources.Contains(source),
			"Duplicate registration of gravity source!", source
		);
		sources.Add(source);
	}
	public static void Unregister (GravitySource source) {
		Debug.Assert(
			sources.Contains(source),
			"Unregistration of unknown gravity source!", source
		);
		sources.Remove(source);
	}
	
}