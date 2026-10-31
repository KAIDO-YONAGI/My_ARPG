using UnityEngine;

namespace Pipeline.SO
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "SkillTree/Skill")]
    public class SkillSO : ScriptableObject 
    {
        public string skillName;
        public int maxLevel=5;
        public Sprite skillIcon;

    }
}
