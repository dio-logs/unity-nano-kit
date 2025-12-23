using UnityEngine;

public class LineThicknessCalculator : MonoBehaviour
{
    // 선분의 시작과 끝, 그리고 두께
    public Vector3 fromPoint;
    public Vector3 toPoint;
    public float thickness = 1.0f;

    // 특정 포인트 (선분 위의 임의의 점이라고 가정)
    public Vector3 targetPointOnLine;

    void OnDrawGizmos()
    {
        // 1. 선분의 방향 벡터 (단위 벡터)
        Vector3 lineDir = (toPoint - fromPoint).normalized;

        // 2. 선분에 직교하는 벡터 구하기 (외적 사용)
        // 만약 선분이 누워있다면 Vector3.up을 기준으로 하여 옆 방향을 구함
        // 주의: 선분이 수직(Vertical)이라면 Vector3.up과 평행해서 외적 결과가 0이 되므로 예외 처리 필요
        Vector3 normal = Vector3.up; 
        
        // 선분이 거의 수직인 경우, 기준축을 앞뒤(Forward)로 변경
        if (Mathf.Abs(Vector3.Dot(lineDir, Vector3.up)) > 0.99f)
        {
            normal = Vector3.forward;
        }

        Vector3 sideDir = Vector3.Cross(lineDir, normal).normalized;

        // 3. 두께의 절반만큼 이동할 오프셋 벡터
        Vector3 offset = sideDir * (thickness * 0.5f);

        // 4. 결과 좌표 구하기 (왼쪽, 오른쪽)
        Vector3 pointLeft = targetPointOnLine - offset;
        Vector3 pointRight = targetPointOnLine + offset;

        // --- 시각화 (Gizmos) ---
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(fromPoint, toPoint); // 원래 선분

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetPointOnLine, 0.1f); // 기준점

        Gizmos.color = Color.green;
        Gizmos.DrawLine(pointLeft, pointRight); // 두께를 나타내는 선
        Gizmos.DrawSphere(pointLeft, 0.05f);    // 왼쪽 끝
        Gizmos.DrawSphere(pointRight, 0.05f);   // 오른쪽 끝
    }
}