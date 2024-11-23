using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MapGenerator mapGenerator; // MapGenerator 참조
    [SerializeField] private MapDataManager mapDataManager; // MapDataManager 참조

    void Start()
    {
        Debug.Log("GameManager 시작");

        string path = Application.persistentDataPath + "/mapdata.json";

        if (System.IO.File.Exists(path))
        {
            Debug.Log("맵 데이터 로드 중...");
            mapDataManager.LoadMap(); // JSON 파일에서 기존 데이터를 불러옴
        }
        else
        {
            Debug.Log("새로운 맵 생성");
            mapGenerator.GenerateInitialChunks(); // 새로운 맵 생성
        }

        Debug.Log("게임 시작");
    }

    void OnApplicationQuit()
    {
        Debug.Log("맵 데이터 저장 중...");
        mapDataManager.SaveMap(); // 현재 맵 데이터를 저장
        Debug.Log("맵 데이터 저장 완료");
    }
}