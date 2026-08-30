using UnityEngine;

public class LevelEditorController : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject pinPrefab;
    [SerializeField] private GameObject x2Prefab;
    [SerializeField] private GameObject x3Prefab;
    [SerializeField] private GameObject x5Prefab;
    [SerializeField] private GameObject jumpPrefab;
    [SerializeField] private GameObject collectorPrefab;

    [Header("Spawn Point")]
    [SerializeField] private Transform editorSpawnPoint;

    private GameObject Spawn(GameObject prefab)
    {
        if (prefab == null)
            return null;

        Vector3 position = editorSpawnPoint != null ? editorSpawnPoint.position : Vector3.zero;
        return Instantiate(prefab, position, Quaternion.identity);
    }

    public void AddWall() => Spawn(wallPrefab);
    public void AddPin() => Spawn(pinPrefab);
    public void AddX2() => Spawn(x2Prefab);
    public void AddX3() => Spawn(x3Prefab);
    public void AddX5() => Spawn(x5Prefab);
    public void AddJump() => Spawn(jumpPrefab);
    public void AddCollector() => Spawn(collectorPrefab);
}
