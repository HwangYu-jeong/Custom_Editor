using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace yj.lightingdata
{
    public class LightingDataSetting : MonoBehaviour
    {
#if UNITY_EDITOR
        public LightMapData originalLightData;
#endif
        public CloneSceneData cloneSceneData;

        public List<CloneLightMapInfo> dataList = new List<CloneLightMapInfo>();
        private List<CloneLightMapInfo> updateDataList = new List<CloneLightMapInfo>();

        //__________________________________________________________________________ Init
#if UNITY_EDITOR
        /// <summary>
        /// 빌드 전 Clone씬의 라이팅 데이터 저장
        /// </summary>
        public void SaveIndexAndMeshData()
        {
            if (originalLightData == null || cloneSceneData == null) return;

            CompareMeshAndSaveSiblingIndexs();
            cloneSceneData.clonelightdatas = dataList.ToArray();
        }
        private void CompareMeshAndSaveSiblingIndexs()
        {
            Scene scene = SceneManager.GetActiveScene();
            List<GameObject> rootObjects = new List<GameObject>();
            scene.GetRootGameObjects(rootObjects);

            Dictionary<Mesh, BakedLightMapInfo> dataDict = new Dictionary<Mesh, BakedLightMapInfo>();
            for (int i = 0, l1 = originalLightData.lightMapDataInfos.Length; i < l1; i++)
            {
                if (dataDict.ContainsKey(originalLightData.lightMapDataInfos[i].meshfilterLink)) continue;
                dataDict.Add(originalLightData.lightMapDataInfos[i].meshfilterLink, originalLightData.lightMapDataInfos[i]);
            }


            for (int i = 0, l1 = rootObjects.Count; i < l1; i++)
            {
                MeshRenderer[] clnObject = rootObjects[i].GetComponentsInChildren<MeshRenderer>();

                for (int j = 0, l2 = clnObject.Length; j < l2; j++)
                {
                    CloneLightMapInfo ci = new CloneLightMapInfo();
                    int[] index = GetHierarchyIndexes(clnObject[j].transform);
                    ci._silblingIndex = string.Join(",", index);
                    string s = ci._silblingIndex;

                    int idx = s.IndexOf(",");

                    if (idx > -1)
                    {
                        s = s.Replace(s.Substring(0, s.IndexOf(",")), "");
                    }
                    
                    ci._silblingIndex = rootObjects[i].name + s;

                    Mesh clnMesh = clnObject[j].GetComponent<MeshFilter>().sharedMesh;
                    if (clnMesh == null || !dataDict.ContainsKey(clnMesh)) continue;

                    var dataInfo = dataDict[clnMesh];
                    if (dataInfo == null) continue;

                    ci._redataInfo = dataInfo;
                    ci._meshrendererInfo = clnObject[j];
                    dataList.Add(ci);
                }
            }
        }
#endif

        /// <summary>
        /// 라이팅 데이터 적용 확인용 버튼
        /// </summary>
        [ContextMenu("Bake")]
        public void TestBake()
        {
            Dictionary<string, BakedLightMapInfo> mathList = new Dictionary<string, BakedLightMapInfo>();

            for (int i = 0, l1 = cloneSceneData.clonelightdatas.Length; i < l1; i++)
            {
                if (mathList.ContainsKey(cloneSceneData.clonelightdatas[i]._silblingIndex)) continue;

                mathList.Add(cloneSceneData.clonelightdatas[i]._silblingIndex, cloneSceneData.clonelightdatas[i]._redataInfo);
            }

            for (int i = 0, l1 = dataList.Count; i < l1; i++)
            {
                if (!mathList.ContainsKey(dataList[i]._silblingIndex)) continue;

                var dataInfo = mathList[dataList[i]._silblingIndex];
                PutOnLightMapData(dataList[i]._meshrendererInfo, dataInfo);
            }
        }

        /// <summary>
        /// 불필요한 데이터 비우기용
        /// </summary>
        [ContextMenu("Reset List")]
        public void ResetCopyDataList()
        {
            dataList.Clear();
        }
        //__________________________________________________________________________ Start
        private void Start()
        {
            ResetCopyDataList();
            InputOriginalLightData();
        }
        //__________________________________________________________________________ Util & Input
        private void InputOriginalLightData()
        {
            //Play 모드 일때만
            Scene scene = SceneManager.GetActiveScene();
            List<GameObject> rootObjects = new List<GameObject>();
            scene.GetRootGameObjects(rootObjects);

            Dictionary<string, BakedLightMapInfo> dataDict = new Dictionary<string, BakedLightMapInfo>();
            for (int i = 0, l1 = cloneSceneData.clonelightdatas.Length; i < l1; i++)
            {
                if (dataDict.ContainsKey(cloneSceneData.clonelightdatas[i]._silblingIndex)) continue;
                dataDict.Add(cloneSceneData.clonelightdatas[i]._silblingIndex, cloneSceneData.clonelightdatas[i]._redataInfo);
            }

            for (int i = 0, l1 = rootObjects.Count; i < l1; i++)
            {
                MeshRenderer[] clnObject = rootObjects[i].GetComponentsInChildren<MeshRenderer>();

                for (int j = 0, l2 = clnObject.Length; j < l2; j++)
                {
                    CloneLightMapInfo ci = new CloneLightMapInfo();
                    int[] index = GetHierarchyIndexes(clnObject[j].transform);
                    ci._silblingIndex = string.Join(",", index);
                    string s = ci._silblingIndex;
                    s = s.Replace(s.Substring(0, s.IndexOf(",")), "");
                    ci._silblingIndex = rootObjects[i].name + s;

                    //확인용
                    updateDataList.Add(ci);

                    if (ci._silblingIndex == null || !dataDict.ContainsKey(ci._silblingIndex)) continue;

                    var dataInfo = dataDict[ci._silblingIndex];
                    if (dataInfo == null) continue;

                    PutOnLightMapData(clnObject[j], dataInfo);
                }
            }
        }
        private int[] GetHierarchyIndexes(Transform tr)
        {
            List<int> indexes = new List<int>();
            Transform parent = tr;
            while (parent != null)
            {
                indexes.Insert(0, parent.GetSiblingIndex());
                parent = parent.parent;
            }
            return indexes.ToArray();
        }
        private void PutOnLightMapData(MeshRenderer mr, BakedLightMapInfo info)
        {
            mr.lightmapIndex = info.d_index;
            mr.lightmapScaleOffset = info.d_scaleOffset;
            mr.lightProbeProxyVolumeOverride = info.d_probeProxyVolumeOverride;
            mr.lightProbeUsage = info.d_probeUsage;
        }
    }

    [System.Serializable]
    public class CloneLightMapInfo
    {
        public string _silblingIndex;
        public BakedLightMapInfo _redataInfo;
        public MeshRenderer _meshrendererInfo;
    }
}

