//No matter what gets interpolated it can be interpreted
//as a slider running from 0 to 1. Which is a process that
//can be replicated here. It only goes from 0 to 1
//so all time scales must be divided to fit into that.
using UnityEngine;
using UnityEngine.Events;

public class AutomaticSlider : MonoBehaviour {
    
	[SerializeField, Min(0.01f)]
	float duration = 1f;
    //We need to make three lefts here to make a right.
    //We tried to do: UnityEvent<float> onValueChanged for a UnityEvent
    //that needs a float parameter.
    //But it won't show up. It seems generic UnityEvent types can't be serialized.
    //Fine. Well it turns out we can create our own concrete serializable event.
    //Simply extend UnityEvent and it'd be the same but unique for us.
    [System.Serializable]
	public class OnValueChangedEvent : UnityEvent<float> { }
    [SerializeField]
	OnValueChangedEvent onValueChanged = default;
    [SerializeField]
	bool autoReverse = false, smoothstep = false;
	float value;
	//
	public bool Reversed { get; set; }
	public bool AutoReverse {
		get => autoReverse;
		set => autoReverse = value;
	}
	float SmoothedValue => 3f * value * value - 2f * value * value * value;
	
    void Awake() {
        enabled = false;
    }
    void FixedUpdate () {
		float delta = Time.deltaTime / duration;
		if (Reversed) {
			value -= delta;
			if (value <= 0f) {
				if (AutoReverse) {
					value = Mathf.Min(1f, -value);
					Reversed = false;
				}
				else {
					value = 0f;
					enabled = false;
				}
			}
		}
		else {
			value += delta;
			if (value >= 1f) {
				if (AutoReverse) {
					value = Mathf.Max(0f, 2f - value);
					Reversed = true;
				}
				else {
					value = 1f;
					enabled = false;
				}
			}
		}
		onValueChanged.Invoke(smoothstep ? SmoothedValue : value);
	}
}