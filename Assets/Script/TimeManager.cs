using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private Tilemap tileMap;

    [Header("Time Settings")]
    [SerializeField] private float dayDuration = 600f; // ��/�� �ֱ� ����
    [SerializeField] private Color dayColor = Color.white; // �� ����
    [SerializeField] private Color nightColor = new Color(0.1f, 0.1f, 0.3f); // �� ����
    [SerializeField] private float transitionSpeed = 0.5f;

    private float currentTime = 0f; // ���� �ð�
    private bool isDayTime = true;

    void Start()
    {
        StartCoroutine(TimeCycle());
    }

    IEnumerator TimeCycle()
    {
        while (true)
        {
            currentTime += Time.deltaTime;

            if (currentTime >= dayDuration)
            {
                isDayTime = !isDayTime;
                currentTime -= dayDuration;
            }

            yield return null;
        }
    }

    void Update()
    {
        UpdateLighting();
    }

    private void UpdateLighting()
    {
        Color targetColor = isDayTime ? dayColor : nightColor;
        tileMap.color = Color.Lerp(tileMap.color, targetColor, transitionSpeed * Time.deltaTime);
    }
}