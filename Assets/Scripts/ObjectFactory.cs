using UnityEngine;


/// FallingObject(Round/Square) 생성 관리 및 병합 시 다음 레벨 생성

public class ObjectFactory : MonoBehaviour {
    public static ObjectFactory Instance { get; private set; }

    [Header("프리팹 설정")]
    public GameObject[] roundPrefabs;     // Round_0 ~ Round_n
    public GameObject[] squarePrefabs;    // Square_0 ~ Square_n
    
    [Header("난이도 조절 설정")]
    public int difficultyIncreaseInterval = 10;  // 난이도가 증가하는 회수(10회마다 레벨업)
    public int maxInitialLevel = 2;            // 최대 스폰 레벨 (0, 1, 2)
    public float higherLevelChance = 0.3f;      // 더 높은 레벨이 나올 확률(30%)
    private int spawnCount = 0;                // 현재까지 스폰된 오브젝트 회수

    [Header("스폰 위치")]
    public Transform spawnPoint;

    private void Awake() {
        Instance = this;
    }

    void Update() {
        // 왼쪽 클릭 시 새로운 오브젝트 스폰
        if (Input.GetMouseButtonDown(0)) {
            SpawnRandom();
        }
    }

    // 다음에 생성될 오브젝트 정보
    private GameObject nextPrefab;
    private bool isNextRound;
    private int nextLevel;
    
    private void Start() {
        // 게임 시작 시 미리 다음 오브젝트 결정
        PrepareNextObject();
        
        // 스폰 커서 업데이트
        UpdateSpawnerCursor();
    }
    
    /// <summary>
    /// 다음에 생성될 오브젝트 결정
    /// </summary>
    private void PrepareNextObject() {
        // 랜덤하게 Round 또는 Square 타입 선택 (확률 50%)
        isNextRound = Random.value < 0.5f;
        GameObject[] pool = isNextRound ? roundPrefabs : squarePrefabs;
        
        // 현재 난이도 레벨 계산 (10회마다 더 높은 레벨 오브젝트 등장 확률 증가)
        int difficultyLevel = Mathf.Min(spawnCount / difficultyIncreaseInterval, maxInitialLevel);
        
        // 기본적으로 레벨 0 사용
        nextLevel = 0;
        
        // 난이도 레벨에 따라 더 높은 레벨 사용 확률 계산
        if (difficultyLevel > 0) {
            for (int i = 1; i <= difficultyLevel; i++) {
                // 각 레벨마다 higherLevelChance(30%) 확률로 레벨업
                if (Random.value < higherLevelChance) {
                    nextLevel = i;
                }
            }
        }
        
        // 프리팹 배열 범위 확인
        if (nextLevel < pool.Length && pool[nextLevel] != null) {
            nextPrefab = pool[nextLevel];
        } else {
            // 해당 레벨 프리팹이 없으면 레벨 0 사용
            nextLevel = 0;
            nextPrefab = pool[0];
            Debug.LogWarning($"[Factory] {(isNextRound ? "Round" : "Square")} 레벨 {nextLevel} 프리팹이 없습니다. 레벨 0 사용.");
        }
    }
    
    /// <summary>
    /// 스폰 커서에 다음 오브젝트 표시 업데이트
    /// </summary>
    private void UpdateSpawnerCursor() {
        // FindObjectOfType 대신 FindAnyObjectByType 사용 (성능 향상)
        SpawnerCursor cursor = FindAnyObjectByType<SpawnerCursor>();
        if (cursor != null) {
            cursor.ShowNextObject(nextPrefab, isNextRound, nextLevel);
        }
    }
    
    /// <summary>
    /// 실제 오브젝트 스폰 (마우스 클릭 시 호출)
    /// </summary>
    public void SpawnRandom() {
        // 스폰 횟수 증가
        spawnCount++;
        
        // 마지막으로 결정된 다음 오브젝트 생성
        if (nextPrefab != null) {
            Instantiate(nextPrefab, spawnPoint.position, Quaternion.identity);
            Debug.Log($"스폰: {(isNextRound ? "Round" : "Square")} 레벨 {nextLevel} (스폰 횟수: {spawnCount})");
        }
        
        // 다음 오브젝트 결정
        PrepareNextObject();
        
        // 스폰 커서 업데이트
        UpdateSpawnerCursor();
    }


    /// 병합 시 다음 레벨의 오브젝트 생성

    [Header("최고 레벨 설정")]
    public int maxRoundLevel = 10;  // 최고 Round 레벨
    public int maxSquareLevel = 10; // 최고 Square 레벨
    public bool showMaxLevelEffect = true; // 최고 레벨 효과 여부

    // 점수 관리를 위한 이벤트
    public delegate void MergeSuccessEvent(FallingObject.ShapeType shape, int level, bool isMaxLevel);
    public static event MergeSuccessEvent OnMergeSuccess;
    /// <summary>
    /// 병합 시 다음 레벨의 오브젝트 생성
    /// FallingObject에서 이미 다음 레벨을 계산하여 전달하민로 여기서는 그대로 사용
    /// </summary>
    public GameObject SpawnNext(FallingObject.ShapeType shape, int level, Vector3 position) {
        // 현재 병합된 레벨 표시 (이미 FallingObject에서 level + 1이 전달됨)
        Debug.Log($"{shape} 레벨 {level - 1} + {level - 1} 병합 성공 -> 레벨 {level}");

        // 해당 타입의 프리팹 배열 선택
        GameObject[] pool = shape == FallingObject.ShapeType.Round ? roundPrefabs : squarePrefabs;

        // 최고 레벨 확인
        int maxLevel = shape == FallingObject.ShapeType.Round ? maxRoundLevel : maxSquareLevel;
        bool isMaxLevel = level > maxLevel;

        // 병합 이벤트 발생
        OnMergeSuccess?.Invoke(shape, level, isMaxLevel);

        // 현재 레벨이 최고 레벨을 초과하거나 프리팹이 없을 경우
        if (level >= pool.Length || pool[level] == null || isMaxLevel) {
            // 최고 레벨 도달 시 특별한 효과 제공 가능
            if (isMaxLevel && showMaxLevelEffect) {
                Debug.Log($"합병 최고 레벨 {shape} 도달!");
                // 여기에 효과 추가 가능 (파티클, 사운드 등)
                // 예: PlayMaxLevelEffect(position);
            }

            Debug.LogWarning($"[Factory] {shape} 레벨 {level} 프리팹이 없거나 최고 레벨입니다.");
            return null;
        }

        // 레벨에 해당하는 오브젝트 생성 (이미 FallingObject에서 level+1이 전달됨)
        return Instantiate(pool[level], position, Quaternion.identity);
    }

    // 최고 레벨 도달 시 효과 (추후 구현)
    private void PlayMaxLevelEffect(Vector3 position) {
        // 파티클 시스템 생성 또는 사운드 플레이 등
    }
}
