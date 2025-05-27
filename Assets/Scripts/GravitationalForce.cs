using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

// Gravitational force 
public class GravitationalForce : MonoBehaviour {
    public LayerMask attractionLayer;
    public float gravity = 10f; // Gravitational constant
    [SerializeField] private float radius; // Atomshphere radius
    public float gravityRadius => radius;

    public List<Collider2D> attractedObjects = new List<Collider2D>();
    public Transform centerOfMass;

    private void Awake() {
        centerOfMass = GetComponent<Transform>();
    }


    private void Update() {
        SetAttractedObjects();

    }

    private void FixedUpdate() {
        ApplyGravitationalForce();
    }

    void SetAttractedObjects() {
        attractedObjects = Physics2D.OverlapCircleAll(centerOfMass.position, radius, attractionLayer).ToList();
    }

    void ApplyGravitationalForce() {
        foreach (var col in attractedObjects) {
            FallingObject fallingObject = col.GetComponent<FallingObject>();
            if (fallingObject != null) {
                fallingObject.Attract(this);
            }
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radius);
    }


}
