
namespace MyEnums
{
    public enum PlayerState
    {

        Idle,
        Running,
        Attacking,
        Shooting,
        KnockBack

    }
    public enum EnemyState
    {
        Idle,
        Chasing,
        Attacking,
        KnockBack
    }
    public enum SceneType
    {
        Location,
        Menu
    }
    public enum PersistentType
    {
        ReadWrite,
        DontPersist
    }
    public enum AStarNodeType
    {
        Obstacle,
        Walkable
    }
    public enum NPCState
    {
        Idle,
        Wander,
        Patrol,
        Chat
    }
    public enum ChatType
    {
        NormalChat,
        Option,
        RefuseChatByCharacter,
        RefuseChatByItem,
        RefuseChatByVisited

    }
}
