// AimLine.cs
// - 플레이어 위치에서 마우스 월드 좌표까지 선을 그립니다.
// - 장애물 레이어를 지정하면 Raycast로 선 길이를 그 지점까지 "클램프"할 수 있습니다.
// - URP/2D 프로젝트에서도 바로 사용 가능.

using System;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class Background : MonoBehaviour
{
    [Header("Background Settings")]
    public Color color = new Color(0.2f, 0.2f, 0.2f, 1.0f); // 선 색 (불투명 흰색)

    [Header("Sorting (2D draw order)")]
    public string sortingLayerName = "Default";
    public int sortingOrder = -1;        // 낮아야 함

    private Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        if (_cam == null) return;
    }
}