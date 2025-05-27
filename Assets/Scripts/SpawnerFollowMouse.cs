using UnityEngine;

/// <summary>
/// 마우스를 따라 중심 구체의 바깥 원형 궤도에서 회전 위치를 따라가는 스폰 위치 제어
/// </summary>
public class SpawnerFollowMouse : MonoBehaviour {
    //public float radius = 7f;                     // 대기권 밖 거리
    //public Transform planetCenter;                // 중심 구체
    public GravitationalForce gravitationalForce;
    public Transform planetCenter;

    void Update() {
        // 마우스 월드 좌표
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseDir = (mousePos - planetCenter.position).normalized;

        // 대기권 경계 반지름 (PlanetVisual 스케일 2 적용)
        float radius = gravitationalForce.gravityRadius * 2;

        // 마우스가 대기권 내부에 있든 외부에 있든 상관없이
        // 항상 행성 중심에서 대기권 경계 거리만큼 떨어진 위치에 스폰
        Vector2 spawnPos = (Vector2)planetCenter.position + mouseDir * radius;
        transform.position = new Vector3(spawnPos.x, spawnPos.y, 0); // z는 고정 
    }

}
