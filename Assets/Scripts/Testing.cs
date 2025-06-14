using UnityEngine;

public class Testing : MonoBehaviour {
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        Debug.Log("World size: " + GetComponent<SpriteRenderer>().bounds.size);
    }

}
