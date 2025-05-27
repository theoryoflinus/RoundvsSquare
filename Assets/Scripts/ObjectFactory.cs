using UnityEngine;

/// <summary>
/// FallingObject(Round/Square) 생성 관리 및 병합 시 다음 레벨 생성
/// </summary>
public class ObjectFactory : MonoBehaviour {
    public static ObjectFactory Instance { get; private set; }

    [Header("프리팹 설정")]
    public GameObject[] roundPrefabs;     // Round_0 ~ Round_n
    public GameObject[] squarePrefabs;    // Square_0 ~ Square_n

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

    public void SpawnRandom() {
        bool spawnRound = Random.value < 0.5f;
        GameObject[] pool = spawnRound ? roundPrefabs : squarePrefabs;
        int index = Random.Range(0, pool.Length);

        if (pool[index] != null) {
            Instantiate(pool[index], spawnPoint.position, Quaternion.identity);
        }
    }

    /// <summary>
    /// 병합 시 다음 레벨의 오브젝트 생성
    /// </summary>
    public GameObject SpawnNext(FallingObject.ShapeType shape, int level, Vector3 position) {
        GameObject[] pool = shape == FallingObject.ShapeType.Round ? roundPrefabs : squarePrefabs;
        if (level >= pool.Length || pool[level] == null) {
            Debug.LogWarning($"[Factory] {shape} 레벨 {level} 프리팹이 없습니다.");
            return null;
        }

        return Instantiate(pool[level], position, Quaternion.identity);
    }
}
