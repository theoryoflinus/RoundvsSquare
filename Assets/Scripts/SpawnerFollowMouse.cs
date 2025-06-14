using UnityEngine;

/// <summary>
/// 마우스를 따라 중심 구체의 바깥 원형 궤도에서 회전 위치를 따라가는 스폰 위치 제어
/// </summary>
public class SpawnerFollowMouse : MonoBehaviour {
    //public float radius = 7f;                     // 대기권 밖 거리
    //public Transform planetCenter;                // 중심 구체
    public GravitationalForce gravitationalForce;
    public Transform planetCenter;
    public SpawnerCursor spawnerCursor;

    void Update() {


        // 마우스 월드 좌표
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 offset = mousePos - planetCenter.position;

        // 거리 체크: 너무 가까우면 회전 방향 판단 불가 → 무시
        if (offset.magnitude < 0.01f) {
            spawnerCursor.Hide();
            return;
        }

        Vector2 dir = offset.normalized;
        float radius = gravitationalForce.gravityRadius;

        Vector2 spawnPos = (Vector2)planetCenter.position + dir * radius;
        transform.position = new Vector3(spawnPos.x, spawnPos.y, 0);


        spawnerCursor.UpdatePosition(spawnPos);
    }

}
