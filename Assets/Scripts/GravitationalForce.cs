using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 행성 중력과 대기권 관리 시스템
/// </summary>
public class GravitationalForce : MonoBehaviour {
    [Header("중력 설정")]
    public LayerMask attractionLayer;         // 중력 영향을 받는 레이어
    public float gravity = 10f;               // 중력 상수
    [SerializeField] private float radius;    // 대기권 반경
    public float gravityRadius => radius;      // 외부에서 반경 읽기용 프로퍼티

    [Header("시각화 설정")]
    public Color atmosphereColor = new Color(0.5f, 0.5f, 1f, 0.3f);  // 대기권 색상
    public Color atmosphereBorderColor = Color.magenta;             // 대기권 경계선 색상

    // 중력 영향을 받는 오브젝트 목록
    public List<Collider2D> attractedObjects = new List<Collider2D>();
    public Transform centerOfMass;  // 중력 중심점

    private IGameOverHandler gameOverHandler;  // 게임 오버 상태 검사용

    private void Awake() {
        centerOfMass = GetComponent<Transform>();
    }

    private void Start() {
        // 게임 오버 핸들러 가져오기
        gameOverHandler = FindAnyObjectByType<GameManager>();
        if (gameOverHandler == null) {
            Debug.LogWarning("GameManager(IGameOverHandler)를 찾을 수 없습니다. 게임 오버 기능이 동작하지 않을 수 있습니다.");
        }
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

    private void OnDrawGizmos() {
        // 대기권 영역 시각화
        Gizmos.color = atmosphereColor;
        Gizmos.DrawWireSphere(transform.position, radius);

        // 대기권 경계선 시각화
        Gizmos.color = atmosphereBorderColor;
        Gizmos.DrawWireSphere(transform.position, radius);

        // 게임 오버 경계선은 GameManager에서 그림
    }
}
