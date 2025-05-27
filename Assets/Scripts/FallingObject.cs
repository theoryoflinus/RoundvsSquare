using UnityEngine;

public class FallingObject : MonoBehaviour {

    [Header("병합 설정")]
    public int level = 0;
    public enum ShapeType { Round, Square }
    public ShapeType shape;

    [Header("중력 설정")]
    [SerializeField] private bool rotateToCenter = true;
    [SerializeField] private float gravityStrength = 100;

    private Transform m_transform;
    private Collider2D m_collider;
    private Rigidbody2D m_rigidbody;
    private GravitationalForce currentGravitySource;

    private void Start() {
        m_transform = GetComponent<Transform>();
        m_collider = GetComponent<Collider2D>();
        m_rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update() {
        if (currentGravitySource != null) {
            if (!currentGravitySource.attractedObjects.Contains(m_collider)) {
                currentGravitySource = null;
                return;
            }
            if (rotateToCenter) RotateToCenter();
            m_rigidbody.gravityScale = 0;
        } else {
            m_rigidbody.gravityScale = 1;
        }
    }

    public void Attract(GravitationalForce source) {
        Vector2 dir = ((Vector2)source.centerOfMass.position - m_rigidbody.position).normalized;
        m_rigidbody.AddForce(dir * source.gravity * gravityStrength * Time.fixedDeltaTime);

        if (currentGravitySource == null)
            currentGravitySource = source;
    }

    void RotateToCenter() {
        if (currentGravitySource != null) {
            Vector2 distance = (Vector2)currentGravitySource.centerOfMass.position - (Vector2)m_transform.position;
            float angle = Mathf.Atan2(distance.y, distance.x) * Mathf.Rad2Deg;
            m_transform.rotation = Quaternion.AngleAxis(angle + 90f, Vector3.forward);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        FallingObject other = collision.gameObject.GetComponent<FallingObject>();
        if (other == null || other.shape != this.shape || other.level != this.level)
            return;

        if (this.transform.position.x > other.transform.position.x) return; // 중복 병합 방지

        Vector3 newPos = (this.transform.position + other.transform.position) / 2f;
        GameObject next = ObjectFactory.Instance.SpawnNext(shape, level + 1, newPos);
        Destroy(other.gameObject);
        Destroy(this.gameObject);
    }

}