using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MapGenerator mapGenerator; // MapGenerator ����
    [SerializeField] private MapDataManager mapDataManager; // MapDataManager ����

    void Start()
    {
        Debug.Log("GameManager ����");

        string path = Application.persistentDataPath + "/mapdata.json";

        if (System.IO.File.Exists(path))
        {
            Debug.Log("�� ������ �ε� ��...");
            mapDataManager.LoadMap(); // JSON ���Ͽ��� ���� �����͸� �ҷ���
            mapGenerator.ValidateIntegrity();
        }
        else
        {
            Debug.Log("���ο� �� ����");
            mapGenerator.GenerateInitialChunks(); // ���ο� �� ����
            mapDataManager.SaveMap(); // ������ ���� �ٷ� ����
        }

        Debug.Log("���� ����");
    }

    void OnApplicationQuit()
    {
        Debug.Log("�� ������ ���� ��...");
        mapDataManager.SaveMap(); // ���� �� �����͸� ����
        Debug.Log("�� ������ ���� �Ϸ�");
    }
}