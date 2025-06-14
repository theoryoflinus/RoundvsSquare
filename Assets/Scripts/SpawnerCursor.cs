using UnityEngine;

public class SpawnerCursor : MonoBehaviour {

    [Header("커서 설정")]
    public float previewScale = 0.7f;      // 미리보기 오브젝트 크기 스케일
    public Color previewTint = new Color(1, 1, 1, 0.7f);  // 미리보기 오브젝트 투명도
    
    private SpriteRenderer cursorRenderer;    // 커서의 스프라이트 렌더러
    private GameObject previewObject;        // 미리보기 오브젝트
    
    private void Awake() {
        cursorRenderer = GetComponent<SpriteRenderer>();
    }
    
    /// <summary>
    /// 커서 위치 업데이트
    /// </summary>
    public void UpdatePosition(Vector2 position) {
        transform.position = new Vector3(position.x, position.y, 0);
        cursorRenderer.enabled = true;
        
        // 미리보기 오브젝트가 있다면 그 위치도 업데이트
        if (previewObject != null) {
            previewObject.transform.position = transform.position;
        }
    }
    
    /// <summary>
    /// 커서 숨기기
    /// </summary>
    public void Hide() {
        cursorRenderer.enabled = false;
        
        // 미리보기 오브젝트도 숨김
        if (previewObject != null) {
            previewObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 다음에 스폰될 오브젝트 표시
    /// </summary>
    public void ShowNextObject(GameObject prefab, bool isRound, int level) {
        // 기존 미리보기 오브젝트 제거
        if (previewObject != null) {
            Destroy(previewObject);
        }
        
        if (prefab == null) return;
        
        // 미리보기 오브젝트 생성
        previewObject = Instantiate(prefab, transform.position, Quaternion.identity);
        previewObject.name = $"Preview_{(isRound ? "Round" : "Square")}_{level}";
        
        // 미리보기 오브젝트 설정
        SetupPreviewObject(previewObject);
    }
    
    /// <summary>
    /// 미리보기 오브젝트 설정
    /// </summary>
    private void SetupPreviewObject(GameObject obj) {
        // 스케일 설정
        obj.transform.localScale *= previewScale;
        
        // 하위 오브젝트의 모든 스프라이트 렌더러 조정
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers) {
            renderer.color = previewTint;  // 투명도 조정
        }
        
        // 충돌 요소 비활성화
        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders) {
            collider.enabled = false;
        }
        
        // Rigidbody 비활성화
        Rigidbody2D[] rigidbodies = obj.GetComponentsInChildren<Rigidbody2D>();
        foreach (Rigidbody2D rb in rigidbodies) {
            // isKinematic 대신 bodyType 사용 (Unity 권장사항)
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }
        
        // 기타 스크립트 비활성화
        FallingObject fallingObject = obj.GetComponent<FallingObject>();
        if (fallingObject != null) {
            fallingObject.enabled = false;
        }
    }
}
