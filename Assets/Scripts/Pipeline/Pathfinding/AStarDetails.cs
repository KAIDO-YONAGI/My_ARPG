public class AStarDetails
{
    private int x;
    private int y;
    private float cost;
    private float disToBeg;
    private float disToEnd;
    private AStarDetails fatherNode;

    public float GetCost() => cost;
    public float GetDisToBeg() => disToBeg;
    public int GetX() => x;
    public int GetY() => y;

    public AStarDetails GetFatherNode() => fatherNode;

    public AStarDetails(int x, int y, int endX, int endY, AStarDetails fatherNode)
    {
        this.x = x;
        this.y = y;
        this.fatherNode = fatherNode;

        if (fatherNode == null)
        {
            disToBeg = 0;
        }
        else
        {
            disToBeg = fatherNode.disToBeg +
                CalDistance(x, y, fatherNode.x, fatherNode.y);
        }

        disToEnd = Heuristic(x, y, endX, endY);
        cost = disToBeg + disToEnd;
    }

    //octile 距离，仅作启发值（h），随实际距离增长以引导搜索方向
    public static float Heuristic(int ax, int ay, int bx, int by)
    {
        int dx = Abs(bx - ax);
        int dy = Abs(by - ay);
        return dx > dy ? dy * 1.414f + (dx - dy) : dx * 1.414f + (dy - dx);
    }

    //仅用于相邻格间的步长代价：直行 1，斜行 1.414
    private float CalDistance(int ax, int ay, int bx, int by)
    {
        int distance = Abs(bx - ax) + Abs(by - ay);
        return distance >= 2 ? 1.414f : 1;
    }

    private static int Abs(int v) => v < 0 ? -v : v;
}
