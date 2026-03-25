using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeMapManager : MonoBehaviour
{
    public static NodeMapManager instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

    }

    private AStarNode[,] nodeMap;
    private List<AStarNode> openList;
    private List<AStarNode> closeList;

    public void InitMapInfo(int w,int h)
    {
        
    }
    public List<AStarNode> FindPath(Vector3 startPos,Vector3 endPos)
    {

        
        return new List<AStarNode>();
    }
}
