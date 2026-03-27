using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(fileName = "PathFinderRequestSO", menuName = "PathFinderRequestSO", order = 0)]

public class PathFinderRequestSO : ScriptableObject
{
    public UnityAction<Vector3, Vector3> PathFinderRequestEvent;
    public UnityAction<List<Vector3>> PathFinderReceiveEvent;

    public void RaisePathFinderRequest(Vector3 startPos, Vector3 endPos)
    {
        PathFinderRequestEvent?.Invoke(startPos, endPos);
    }
    public void RaisePathFinderReceive(List<Vector3> wayList)
    {
        PathFinderReceiveEvent?.Invoke(wayList);
    }
}
