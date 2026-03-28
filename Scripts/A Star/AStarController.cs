using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AStarController : MonoBehaviour
{
    Vector3 startPos = new Vector3(0, 0, 0); Vector3 endPos = new Vector3(0, -5f, 0);
    public Stack<PathFinderDetails> FindWay()
    {
        return AStarPathFinder.instance.FindPath(startPos, endPos);
    }
    private void Start()
    {
        Stack<PathFinderDetails> wayStack = FindWay();
        if (wayStack == null)
        {
            Debug.Log("没找到路径");
            return;
        }
        while (wayStack.Count > 0)
        {
            Debug.Log(wayStack.Peek().GetNodePos().ToString()
            // + wayStack.Peek().GetFatherNode().GetNodePos().ToString()
            + AStarPathFinder.instance.GetNodeMap()[wayStack.Peek().GetNodePos()].GetNodeType().ToString()
            );
            wayStack.Pop();
        }
    }
}
