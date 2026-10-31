using UnityEngine;

namespace Pipeline.SO
{
    [CreateAssetMenu(fileName = "Location", menuName = "LocationSO")]
    public class LocationSO : ScriptableObject
    {
        public string locationID;
        public string locationName;
    }
}
