using UnityEngine;

public class PlayerController : YSingleton<PlayerController>
{
    [SerializeField] private Transform playerTransform;

    public Vector3 GetPosition() => playerTransform.position;
}