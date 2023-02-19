using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace yj.lightingdata
{
    /// <summary>
    /// 씬에서 원본 라이트 맵 정보 추출 및 저장을 위한 Class
    /// </summary>
    public class LightMapData : ScriptableObject
    {
        public static string readOnly = "원본 Scene BakedLightMap 데이터";
        public bool enable;

#if UNITY_EDITOR
        [Header("Origin Data")]
        public SceneAsset originScene;
        public BakedLightMapInfo[] lightMapDataInfos;

        /// <summary>
        /// 씬에서 Mesh를 가지고 있는 오브젝트들의 원본 라이트 맵 정보 추출
        /// </summary>
        [ContextMenu("Save")]
        public void SaveOriginLightMapDatas()
        {
            if (!enable) return;
            if (originScene == null)
            {
                Debug.Log("복사 할 씬이 비워져 있습니다.");
                return;
            }

            List<GameObject> rootObjects = new List<GameObject>();
            Scene scene = SceneManager.GetSceneByName(originScene.name);
            scene.GetRootGameObjects(rootObjects);

            List<BakedLightMapInfo> dataInfos = new List<BakedLightMapInfo>();

            for (int i = 0, l1 = rootObjects.Count; i < l1; i++)
            {
                MeshRenderer[] orgMeshRenderer = rootObjects[i]
                    .GetComponentsInChildren<MeshRenderer>();

                for (int j = 0, l2 = orgMeshRenderer.Length; j < l2; j++)
                {
                    if (orgMeshRenderer[j].lightmapIndex == -1) continue;
                    dataInfos.Add(GetLightMapInfo(orgMeshRenderer[j]));
                }
            }
            lightMapDataInfos = dataInfos.ToArray();

            Debug.Log(string.Format("{0} 씬 라이팅데이터 저장 완료 .", scene.name));
        }
        private BakedLightMapInfo GetLightMapInfo(MeshRenderer m)
        {
            BakedLightMapInfo orgdata = new BakedLightMapInfo();

            orgdata.meshfilterLink = m.GetComponent<MeshFilter>().sharedMesh;
            orgdata.d_index = m.lightmapIndex;
            orgdata.d_scaleOffset = m.lightmapScaleOffset;
            orgdata.d_probeProxyVolumeOverride = m.lightProbeProxyVolumeOverride;
            orgdata.d_probeUsage = m.lightProbeUsage;

            return orgdata;
        }
#endif
    }

    [System.Serializable]
    public class BakedLightMapInfo
    {
        public Mesh meshfilterLink;
        public int d_index;
        public Vector4 d_scaleOffset;
        public GameObject d_probeProxyVolumeOverride;
        public UnityEngine.Rendering.LightProbeUsage d_probeUsage;
    }
}
