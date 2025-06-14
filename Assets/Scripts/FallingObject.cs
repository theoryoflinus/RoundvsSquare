using UnityEngine;
using System.Collections;

// 게임 오버 상태를 검사하기 위한 인터페이스
public interface IGameOverHandler {
    bool IsGameOver();
}

public class FallingObject : MonoBehaviour {

    [Header("병합 설정")]
    public int level = 0;
    public enum ShapeType { Round, Square }
    public ShapeType shape;

    // 새로 생성된 오브젝트인지 표시
    [HideInInspector] public bool isNewlyCreated = true;
    // 병합 중인지 표시 (중복 병합 방지)
    [HideInInspector] public bool isMerging = false;

    [Header("중력 설정")]
    [SerializeField] private bool rotateToCenter = true;
    [SerializeField] private float gravityStrength = 100;

    private Transform m_transform;
    private Collider2D m_collider;
    private Rigidbody2D m_rigidbody;
    private GravitationalForce currentGravitySource;
    private IGameOverHandler gameOverHandler; // 게임 오버 상태 검사용

    private void Start() {
        m_transform = GetComponent<Transform>();
        m_collider = GetComponent<Collider2D>();
        m_rigidbody = GetComponent<Rigidbody2D>();
        
        // 게임 오버 상태 검사용 참조 가져오기
        gameOverHandler = FindAnyObjectByType<GameManager>();

        // 새로 생성된 오브젝트는 잠시 동안 병합을 방지
        if (isNewlyCreated) {
            StartCoroutine(ResetNewlyCreatedFlag());
        }
    }

    // 새로 생성된 플래그를 잠시 후 초기화
    private System.Collections.IEnumerator ResetNewlyCreatedFlag() {
        // 물리 업데이트 주기에 맞추기 위해 더 짧은 시간 사용
        yield return new WaitForFixedUpdate(); // 물리 업데이트 1회 동안만 대기
        isNewlyCreated = false;
    }

    private void Update() {
        // 게임 오버 상태 확인
        if (gameOverHandler != null && gameOverHandler.IsGameOver()) {
            // 게임 오버 상태에서는 물체 움직임 처리 건너뛰기
            return;
        }

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

    // 최고 레벨 체크를 위한 반환값을 가진 병합 시도 함수
    private bool TryMerge(FallingObject other, out Vector3 mergePosition) {
        mergePosition = Vector3.zero;

        // 다른 타입이거나 다른 레벨이면 병합 불가
        if (other == null || other.shape != this.shape || other.level != this.level)
            return false;

        // 중복 병합 방지
        if (this.transform.position.x > other.transform.position.x)
            return false;

        // 이미 병합 중인 오브젝트는 병합 불가 (중복 병합 방지에 중점)
        if (this.isMerging || other.isMerging) {
            return false;
        }

        // 새로 생성된 오브젝트 처리 - 한쪽만 새로 생성된 경우도 상대적으로 오래된 오브젝트라면 병합 허용
        if (this.isNewlyCreated && other.isNewlyCreated) {
            // 두 오브젝트 모두 새로 생성된 경우만 병합 방지
            //Debug.Log("두 오브젝트 모두 새로 생성되어 병합이 방지되었습니다.");
            return false;
        }

        // 최고 레벨인지 확인 (스탠딩 소스를 사용하지 않고 싱글톤 사용)
        int maxLevel = (shape == ShapeType.Round) ?
            ObjectFactory.Instance.maxRoundLevel :
            ObjectFactory.Instance.maxSquareLevel;

        // 현재 레벨이 최고 레벨이면 병합 불가
        if (level >= maxLevel) {
            Debug.Log($"최고 레벨 {shape} 오브젝트는 더 이상 병합되지 않습니다.");
            return false;
        }

        // 병합 위치 계산
        mergePosition = (this.transform.position + other.transform.position) / 2f;
        return true;
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        FallingObject other = collision.gameObject.GetComponent<FallingObject>();
        if (other == null) return;

        // 상대적으로 중요한 충돌인지 확인 - 너무 약한 충돌은 무시
        ContactPoint2D[] contacts = new ContactPoint2D[1];
        int contactCount = collision.GetContacts(contacts);
        if (contactCount <= 0) return;

        // 충돌 반응이 있는지 확인
        if (collision.relativeVelocity.magnitude < 0.1f) {
            // 너무 느린 충돌은 무시와 사용하는 것이 유용할 수 있습니다.
            // return; // 생략 가능
        }

        // 병합 시도
        Vector3 mergePosition;
        if (TryMerge(other, out mergePosition)) {
            // 디버그: 병합 성공 로그
            Debug.Log($"{shape} 레벨 {level} + {other.shape} 레벨 {other.level} 병합 시도 성공");

            // 병합 중 플래그 설정 (중복 병합 방지)
            this.isMerging = true;
            other.isMerging = true;

            // 병합 성공 시 다음 레벨 오브젝트 생성
            GameObject nextObj = ObjectFactory.Instance.SpawnNext(shape, level + 1, mergePosition);

            // 새로 생성된 오브젝트의 isNewlyCreated 플래그 설정
            if (nextObj != null) {
                FallingObject nextFallingObj = nextObj.GetComponent<FallingObject>();
                if (nextFallingObj != null) {
                    nextFallingObj.isNewlyCreated = true;
                }
            }

            // 기존 오브젝트 제거
            Destroy(other.gameObject);
            Destroy(this.gameObject);
        }
    }

}