using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 게임 매니저 - 게임 오버 로직 관리
/// </summary>
public class GameManager : MonoBehaviour, IGameOverHandler {
    public static GameManager Instance { get; private set; }

    [Header("게임 오버 설정")]
    [Range(0.9f, 0.99f)]
    [Tooltip("게임 오버 경계선 위치 (대기권 반경의 %)")]
    public float gameOverThreshold = 0.95f;

    [Range(0.05f, 0.5f)]
    [Tooltip("물체가 경계선을 넘은 후 게임 오버까지의 지연 시간(초)")]
    public float gameOverDelay = 0.1f;

    [Tooltip("게임 오버 경계선 색상")]
    public Color gameOverLineColor = Color.red;

    [Header("게임 오버 효과")]
    public float shakeIntensity = 0.1f;    // 흔들림 강도
    public float shakeDuration = 3.0f;     // 흔들림 지속 시간
    public float shakeFrequency = 10.0f;   // 흔들림 빈도

    [Header("디버그")]
    [SerializeField] private bool debugMode = false;

    // 내부 변수
    private GravitationalForce gravitySource;
    private bool isGameOver = false;
    private Dictionary<int, float> objectsOverThreshold = new Dictionary<int, float>();
    private HashSet<int> initiallySpawnedObjects = new HashSet<int>();

    private void Awake() {
        // 싱글톤 패턴
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        // 행성(중력 소스) 찾기
        gravitySource = FindAnyObjectByType<GravitationalForce>();
        if (gravitySource == null) {
            Debug.LogError("GravitationalForce를 찾을 수 없습니다.");
        }
    }

    private void Update() {
        if (isGameOver || gravitySource == null) return;

        // F10 키로 게임 오버 
        if (Input.GetKeyDown(KeyCode.F10)) {
            Debug.Log("F10 키로 게임 오버 발생");
            TriggerGameOver();
            return;
        }

        // 게임 오버 라인 시각화
        DrawGameOverLine();

        // 게임 오버 판정
        CheckGameOverConditions();
    }

    // 게임 오버 라인 시각화 (런타임)
    private void DrawGameOverLine() {
        float gameOverRadius = gravitySource.gravityRadius * gameOverThreshold;
        Debug.DrawLine(
            gravitySource.transform.position + Vector3.left * gameOverRadius,
            gravitySource.transform.position + Vector3.right * gameOverRadius,
            gameOverLineColor);
        Debug.DrawLine(
            gravitySource.transform.position + Vector3.up * gameOverRadius,
            gravitySource.transform.position + Vector3.down * gameOverRadius,
            gameOverLineColor);
    }

    // 게임 오버 조건 검사
    private void CheckGameOverConditions() {
        // 모든 물리 객체 검사
        Rigidbody2D[] allRigidbodies = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
        float gameOverRadius = gravitySource.gravityRadius * gameOverThreshold;

        // 삭제된 객체 추적
        List<int> objectsToRemove = new List<int>();

        // 각 객체가 게임 오버 경계선을 넘었는지 확인
        foreach (var rb in allRigidbodies) {
            if (!rb.gameObject.activeInHierarchy || rb.bodyType == RigidbodyType2D.Static) continue;

            int objectId = rb.gameObject.GetInstanceID();
            Collider2D collider = rb.GetComponent<Collider2D>();
            if (collider == null) continue;

            // 물체의 일부라도 경계선을 넘었는지 확인
            bool isOverThreshold = IsObjectOverThreshold(collider, gameOverRadius);
            
            // 스폰 직후 떨어지는 중인지 확인
            bool isInitiallyFalling = IsInitiallyFallingObject(rb.gameObject);
            
            // 경계선을 넘지 않은 경우
            if (!isOverThreshold) {
                // 기존 추적에서 제거
                if (objectsOverThreshold.ContainsKey(objectId)) {
                    objectsOverThreshold.Remove(objectId);
                    if (debugMode) Debug.Log($"{rb.gameObject.name}이(가) 경계선 아래로 내려왔습니다.");
                }
                continue;
            }

            // 최고 레벨 물체인지 확인
            bool isMaxLevelObject = IsMaxLevelObject(rb.gameObject);
            
            // 경계선을 넘었지만 초기 스폰된 물체는 제외 (최고 레벨 물체는 예외)
            if (isInitiallyFalling && !isMaxLevelObject) {
                if (objectsOverThreshold.ContainsKey(objectId)) {
                    objectsOverThreshold.Remove(objectId);
                }
                continue;
            }

            // 경계선을 넘었고 초기 스폰된 물체가 아닌 경우 또는 최고 레벨 물체인 경우
            if (!objectsOverThreshold.ContainsKey(objectId)) {
                // 처음 경계선을 넘은 시간 기록
                objectsOverThreshold.Add(objectId, Time.time);
                string logMessage = isMaxLevelObject ? 
                    $"최고 레벨 물체 {rb.gameObject.name}이(가) 경계선을 넘었습니다. 시간 측정 시작." :
                    $"{rb.gameObject.name}이(가) 경계선을 넘었습니다. 시간 측정 시작.";
                if (debugMode) Debug.Log(logMessage);
            } else {
                // 경계선을 넘은 시간이 지정된 시간을 초과하면 게임 오버
                float timeOverThreshold = Time.time - objectsOverThreshold[objectId];
                if (timeOverThreshold >= gameOverDelay) {
                    string logMessage = isMaxLevelObject ?
                        $"최고 레벨 물체 {rb.gameObject.name}이(가) 경계선을 {timeOverThreshold:F2}초 동안 넘어 게임 오버!" :
                        $"{rb.gameObject.name}이(가) 경계선을 {timeOverThreshold:F2}초 동안 넘어 게임 오버!";
                    Debug.Log(logMessage);
                    TriggerGameOver();
                    return;
                } else if (debugMode && Time.frameCount % 30 == 0) {
                    // 30프레임마다 한 번씩 로그 출력 (디버그 모드)
                    Debug.Log($"{rb.gameObject.name} - 경계선 초과 시간: {timeOverThreshold:F2}/{gameOverDelay}초");
                }
            }
        }

        // 삭제된 객체 정리
        foreach (var objectId in objectsToRemove) {
            objectsOverThreshold.Remove(objectId);
        }
    }
    
    // 물체의 일부라도 경계선을 넘었는지 확인
    private bool IsObjectOverThreshold(Collider2D collider, float gameOverRadius) {
        Vector2 planetCenter = gravitySource.transform.position;
        
        // 충돝체 형태에 따른 특수 처리
        if (collider is CircleCollider2D circleCollider) {
            // 원형 충돝체는 중심과 반경을 사용하여 검사
            Vector2 colliderCenter = collider.bounds.center;
            float furthestDistance = Vector2.Distance(colliderCenter, planetCenter) + circleCollider.radius;
            if (furthestDistance > gameOverRadius) {
                return true;
            }
        } 
        else if (collider is BoxCollider2D) {
            // 사각형 충돝체는 회전을 고려해 모든 모서리 확인
            Vector2[] corners = GetBoxColliderCorners(collider);
            foreach (Vector2 corner in corners) {
                if (Vector2.Distance(corner, planetCenter) > gameOverRadius) {
                    return true;
                }
            }
        }
        else {
            // 다각형이나 기타 충돝체는 사각 범위 대각선을 확인
            Bounds bounds = collider.bounds;
            float diagonalRadius = bounds.extents.magnitude;
            float centerDistance = Vector2.Distance(bounds.center, planetCenter);
            
            // 중심과 범위의 가장 멀리 있는 점이 경계선을 넘었는지 확인
            if (centerDistance + diagonalRadius > gameOverRadius) {
                // 더 정확한 검사를 위해 여러 점을 샘플링
                return IsAnyPointOverThreshold(collider, planetCenter, gameOverRadius);
            }
        }
        
        return false;
    }
    
    // 박스 충돝체의 실제 모서리 위치 계산 (회전 고려)
    private Vector2[] GetBoxColliderCorners(Collider2D collider) {
        BoxCollider2D boxCollider = collider as BoxCollider2D;
        Vector2[] corners = new Vector2[4];
        
        Transform transform = collider.transform;
        Vector2 center = (Vector2)transform.TransformPoint(boxCollider.offset);
        Vector2 size = boxCollider.size;
        float angle = transform.eulerAngles.z * Mathf.Deg2Rad;
        
        float halfWidth = size.x * 0.5f * transform.localScale.x;
        float halfHeight = size.y * 0.5f * transform.localScale.y;
        
        // 원래 모서리 위치 (회전 전)
        corners[0] = new Vector2(-halfWidth, -halfHeight);
        corners[1] = new Vector2(halfWidth, -halfHeight);
        corners[2] = new Vector2(halfWidth, halfHeight);
        corners[3] = new Vector2(-halfWidth, halfHeight);
        
        // 회전 및 위치 적용
        for (int i = 0; i < 4; i++) {
            // 회전
            float x = corners[i].x * Mathf.Cos(angle) - corners[i].y * Mathf.Sin(angle);
            float y = corners[i].x * Mathf.Sin(angle) + corners[i].y * Mathf.Cos(angle);
            
            // 최종 전역 좌표에 적용
            corners[i] = new Vector2(x, y) + center;
        }
        
        return corners;
    }
    
    // 충돝체 내부의 여러 점을 샘플링해서 검사
    private bool IsAnyPointOverThreshold(Collider2D collider, Vector2 planetCenter, float gameOverRadius) {
        // 충돝체 경계를 따라 12개 점 샘플링
        const int sampleCount = 12;
        
        // 중심이 경계선을 넘었는지 먼저 확인
        Vector2 colliderCenter = collider.bounds.center;
        if (Vector2.Distance(colliderCenter, planetCenter) > gameOverRadius) {
            return true;
        }
        
        // 포인트인사이드 방식으로 확인하고 싶지만, 2D 경계선을 호출해야 해서 다른 방식 사용
        
        // 충돝체 범위의 가장자리점에서 시작하여 시계 방향으로 검사
        Bounds bounds = collider.bounds;
        Vector2 extents = bounds.extents;
        
        // 범위 무게중심에서 행성로의 방향 벡터 계산
        Vector2 directionToCenter = (planetCenter - (Vector2)bounds.center).normalized;
        
        // 행성 반대방향으로 가장 멀리 있는 점이 경계선을 넘는지 우선 확인
        Vector2 furthestPoint = (Vector2)bounds.center - directionToCenter * extents.magnitude;
        if (Vector2.Distance(furthestPoint, planetCenter) > gameOverRadius) {
            return true;
        }
        
        // 충돝체 메쉬가 경계를 넘었는지 범위를 확장해서 확인
        float angleStep = 360f / sampleCount;
        for (int i = 0; i < sampleCount; i++) {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            
            // 범위 내 메쉬에서 경계까지의 거리 추정
            RaycastHit2D hit = Physics2D.Raycast(
                bounds.center,
                direction, 
                extents.magnitude, 
                1 << collider.gameObject.layer
            );
            
            // 경계선을 넘는지 확인
            if (hit.collider == collider) {
                float distance = Vector2.Distance(hit.point, planetCenter);
                if (distance > gameOverRadius) {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    // 최고 레벨 물체인지 확인
    private bool IsMaxLevelObject(GameObject obj) {
        FallingObject fallingObj = obj.GetComponent<FallingObject>();
        if (fallingObj == null || ObjectFactory.Instance == null) return false;
        
        if (fallingObj.shape == FallingObject.ShapeType.Round) {
            return fallingObj.level >= ObjectFactory.Instance.maxRoundLevel;
        } else if (fallingObj.shape == FallingObject.ShapeType.Square) {
            return fallingObj.level >= ObjectFactory.Instance.maxSquareLevel;
        }
        
        return false;
    }

    // 초기에 스폰되어 떨어지는 중인 물체인지 확인
    private bool IsInitiallyFallingObject(GameObject obj) {
        // 방법 1: FallingObject의 isNewlyCreated 플래그 확인
        FallingObject fallingObj = obj.GetComponent<FallingObject>();
        if (fallingObj != null && fallingObj.isNewlyCreated) {
            return true;
        }

        // 방법 2: 스폰 위치 근처인지 확인
        if (ObjectFactory.Instance != null) {
            float distanceFromSpawn = Vector2.Distance(obj.transform.position, ObjectFactory.Instance.spawnPoint.position);
            if (distanceFromSpawn < 0.5f) {
                return true;
            }
        }

        // 방법 3: 오브젝트 ID 기반 판별 (2초 이내 생성된 객체는 초기 객체로 간주)
        int objectId = obj.GetInstanceID();
        if (!initiallySpawnedObjects.Contains(objectId)) {
            // 새로 등록된 객체는 초기 객체로 등록
            initiallySpawnedObjects.Add(objectId);
            // 2초 후에 리스트에서 제거하는 코루틴 시작
            StartCoroutine(RemoveFromInitialObjectsAfterDelay(objectId, 2.0f));
            return true;
        }

        return false;
    }

    // 초기 스폰 객체 목록에서 일정 시간 후 제거
    private IEnumerator RemoveFromInitialObjectsAfterDelay(int objectId, float delay) {
        yield return new WaitForSeconds(delay);
        initiallySpawnedObjects.Remove(objectId);
    }

    // 게임 오버 발생
    public void TriggerGameOver() {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("게임 오버!");

        // 모든 물체 멈추기
        Rigidbody2D[] allRigidbodies = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
        foreach (var rb in allRigidbodies) {
            if (rb != null) {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        // ObjectFactory 비활성화
        if (ObjectFactory.Instance != null) {
            ObjectFactory.Instance.enabled = false;
        }

        // 흔들림 효과 시작
        StartCoroutine(ShakeObjects());
    }

    // 흔들림 효과
    private IEnumerator ShakeObjects() {
        // 모든 FallingObject 찾기
        FallingObject[] allObjects = FindObjectsByType<FallingObject>(FindObjectsSortMode.None);
        Vector3[] originalPositions = new Vector3[allObjects.Length];

        // 원래 위치 저장
        for (int i = 0; i < allObjects.Length; i++) {
            if (allObjects[i] != null) {
                originalPositions[i] = allObjects[i].transform.position;
            }
        }

        // 흔들림 효과 지속
        float startTime = Time.time;
        while (Time.time - startTime < shakeDuration) {
            for (int i = 0; i < allObjects.Length; i++) {
                if (allObjects[i] != null) {
                    // 사인/코사인 함수로 물체마다 약간 다른 흔들림 효과
                    float xOffset = Mathf.Sin(Time.time * shakeFrequency + i) * shakeIntensity;
                    float yOffset = Mathf.Cos(Time.time * shakeFrequency * 1.3f + i) * shakeIntensity;

                    allObjects[i].transform.position = originalPositions[i] + new Vector3(xOffset, yOffset, 0);
                }
            }
            yield return null;
        }

        // 위치 복원
        for (int i = 0; i < allObjects.Length; i++) {
            if (allObjects[i] != null) {
                allObjects[i].transform.position = originalPositions[i];
            }
        }

        // 게임 오버 UI 표시 로직은 여기에 추가
    }

    // 씬 뷰에서 게임 오버 경계선 시각화
    private void OnDrawGizmos() {
        if (gravitySource != null) {
            float gameOverRadius = gravitySource.gravityRadius * gameOverThreshold;
            Gizmos.color = gameOverLineColor;

            // 두 개의 원을 그려서 더 두껍게 표시
            Gizmos.DrawWireSphere(gravitySource.transform.position, gameOverRadius);
            Gizmos.DrawWireSphere(gravitySource.transform.position, gameOverRadius - 0.02f);
        }
    }

    // 테스트용 메서드 (에디터 컨텍스트 메뉴에서 실행 가능)
    [ContextMenu("Trigger Game Over")]
    public void TestGameOver() {
        TriggerGameOver();
    }

    // 인터페이스 구현
    public bool IsGameOver() {
        return isGameOver;
    }
}