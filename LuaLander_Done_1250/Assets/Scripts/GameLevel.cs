using UnityEngine;

public class GameLevel : MonoBehaviour
{
    [SerializeField] private int level;
    [SerializeField] private Transform landerStartPositionTransform;
    [SerializeField] private Transform cameraStartTargetTransform;


    public int GetLevel()
    {
        return level;
    }

    public Vector3 GetLanderStartPosition()
    {
        return landerStartPositionTransform.position;
    }

    public Transform GetCameraStartTargetTransform()
    {
        return cameraStartTargetTransform;
    }
}
