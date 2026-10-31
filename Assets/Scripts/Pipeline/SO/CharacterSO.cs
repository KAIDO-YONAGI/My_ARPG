using UnityEngine;

namespace Pipeline.SO
{
    [CreateAssetMenu(fileName = "CharacterSO", menuName = "Dialog/Character", order = 1)]
    public class CharacterSO : GuidSO
    {
        public string characterName;
        public Sprite characterPortrait;


    }
}
