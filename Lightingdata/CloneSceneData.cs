using UnityEngine;

namespace yj.lightingdata
{
    /// <summary>
    /// Play전 하이어라키 Silbling Index 저장 및 빌드 후 비교용 데이터
    /// </summary>
    //[CreateAssetMenu(fileName = "ReadSceneData", menuName = "ScriptableObjects/SaveSceneData", order = 1)]
    public class CloneSceneData : ScriptableObject
    {
        public static string readOnly = "복제 Scene Sibling Index 데이터";
        public CloneLightMapInfo[] clonelightdatas;
    }
}
