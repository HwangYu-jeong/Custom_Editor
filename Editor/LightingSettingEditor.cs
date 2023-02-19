using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;

namespace yj.lightingdata
{
    public class LightingSettingEditor : EditorWindow
    {
        [Header("*상위 폴더")]
        private static UnityEngine.Object parentFolder = null;
        [Header("*파일 이름")]
        private static string folderName = "LightingDatas";

        [SerializeField]
        private SceneAsset originalScene = null;
        [SerializeField]
        public List<SceneLinkedInfo> linkList;

        [Header("variable")]
        private int orgindex = 0;
        private static string lightfolderpath = null;
        private string newPath;
        private float space = 10f;


        [Header("GUI")]
        private static LightingSettingEditor _window;
        private SerializedObject serializedobject;
        private static int listsize;
        private static Rect scrollviewRect = new Rect();

        //__________________________________________________________________________ Init
        [UnityEditor.MenuItem("Window/Test/LigtingData")]
        private static void Init()
        {
            _window = EditorWindow.GetWindow(typeof(LightingSettingEditor)) as LightingSettingEditor;

            if (_window)
            {
                _window.Show();
                _window.titleContent = new GUIContent("LightingData Setting");
            }
            scrollviewRect.width = 353f; scrollviewRect.height = 209f;
        }
        private void OnEnable()
        {
            serializedobject = new SerializedObject(this);
            serializedobject.FindProperty("SceneLinkedInfo");
            loadSettingConfigs();
        }

        //__________________________________________________________________________ GUI
        Vector2 scrollPos = Vector2.zero;
        private void OnGUI()
        {
            Rect pos = new Rect(); pos.width = 250f; pos.height = 20f;

            scrollPos = GUI.BeginScrollView(
                    new Rect(0, 0, position.width, position.height), scrollPos,
                    scrollviewRect);

            EditorGUILayout.LabelField("* 상위 폴더 지정");
            parentFolder = EditorGUILayout.ObjectField("Folder", parentFolder, typeof(UnityEngine.Object), true, GUILayout.Width(350), GUILayout.Height(20));
            EditorGUILayout.LabelField("* 데이터 저장 폴더 이름");
            folderName = EditorGUILayout.TextField("Folder Name", folderName, GUILayout.Width(350), GUILayout.Height(20));

            EditorGUILayout.Space(space);
            EditorGUILayout.LabelField("* 씬 링크");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("size", GUILayout.MaxWidth(50f));
            listsize = EditorGUILayout.DelayedIntField(listsize, GUILayout.MaxWidth(300f));
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel++;

            serializedobject.Update();

            // 초기화
            if (linkList == null) linkList = new List<SceneLinkedInfo>();
            if (linkList.Count != listsize)
            {
                if (linkList.Count < listsize)
                {
                    for (int i = linkList.Count, l1 = listsize; i < l1; i++)
                        linkList.Add(new SceneLinkedInfo());
                }
                else
                {
                    linkList.RemoveRange(listsize, linkList.Count - listsize);
                }
                serializedobject.Update();
            }

            for (int i = 0, l1 = listsize; i < l1; i++)
            {
                SerializedProperty prop = serializedobject.FindProperty("linkList");
                SerializedProperty propItem = prop.GetArrayElementAtIndex(i);
                SerializedProperty propTitle = propItem.FindPropertyRelative("title");

                EditorGUILayout.PropertyField(propItem, new GUIContent(propTitle.stringValue)
                    , GUILayout.Width(350));
                DrawUILine();
            }

            serializedobject.ApplyModifiedProperties();
            GUI.EndScrollView();

            EditorGUILayout.LabelField("* 씬 링크 저장");
            if (GUILayout.Button("Save", GUILayout.MaxWidth(350f)))
            {
                SubmitForm();
            }

            EditorGUILayout.LabelField("* 라이팅데이터 적용");
            if (GUILayout.Button("Input", GUILayout.MaxWidth(350f)))
            {
                InputLightingDataToScenes();
            }
        }
        //__________________________________________________________________________ ConvertData
        private void SubmitForm()
        {
            saveSettingConfigs();
        }
        private void InputLightingDataToScenes()
        {
            for (int i = 0, l1 = linkList.Count; i < l1; i++)
            {
                if (linkList[i].originalScene == null)
                {
                    Debug.Log(string.Format($"{i}번째 Original 씬을 등록해 주세요. "));
                    return;
                }

                lightfolderpath = AssetDatabase.GetAssetPath(parentFolder);
                newPath = string.Format("{0}/{1}", lightfolderpath, folderName);

                if (!Directory.Exists(newPath))
                {
                    AssetDatabase.CreateFolder(lightfolderpath, folderName);
                    lightfolderpath = newPath;
                }
                orgindex = i;

                ConvertScenesToData<LightMapData>(linkList[i].originalScene);

                for (int j = 0, l2 = linkList[i].copyScenes.Length; j < l2; j++)
                {
                    ConvertScenesToData<CloneSceneData>(linkList[i].copyScenes[j]);
                }
            }
        }

        /// <summary>
        /// SceneAsset 파일에서 원본 라이트맵 데이터 및 클론 하이어라키 인덱스 정보 추출
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sceneasset"></param>
        private void ConvertScenesToData<T>(SceneAsset sceneasset) where T : ScriptableObject
        {
            T list = ScriptableObject.CreateInstance<T>();

            string type = list.GetType().ToString();
            type = type.Substring(type.IndexOf('.', 3) + 1);

            string filePath = null;

            string scenePath = AssetDatabase.GetAssetPath(sceneasset);
            EditorSceneManager.OpenScene(scenePath);

            if (!Directory.Exists($"{newPath}/{linkList[orgindex].title}"))
            {
                AssetDatabase.CreateFolder(newPath, linkList[orgindex].title);
            }

            filePath = $"{newPath}/{linkList[orgindex].title}";

            if (list is LightMapData)
            {
                filePath = string.Format("{0}/{1}_{2}.asset", filePath, linkList[orgindex].title, type);

                LightMapData info = new LightMapData();
                info.originScene = sceneasset;
                info.enable = true;
                info.SaveOriginLightMapDatas();
                info.enable = false;

                list = info as T;

                CreateAssetAndSave(list, filePath);
            }

            if (list is CloneSceneData)
            {
                filePath = string.Format("{0}/{1}_{2}.asset", filePath, sceneasset.name, type);

                Scene scene = SceneManager.GetActiveScene();
                GameObject[] rootObjects = scene.GetRootGameObjects();
                //GameObject root;

                CloneSceneData info = new CloneSceneData();
                list = info as T;

                LightingDataSetting set = new LightingDataSetting();

                //LightingDataSetting 컴포넌트 찾기
                for (int j = 0, l2 = rootObjects.Length; j < l2; j++)
                {
                    set = rootObjects[j].GetComponentInChildren<LightingDataSetting>();
                    if (set != null) break;
                }

                if (set == null)
                {
                    Debug.Log(string.Format("{0} 씬에 LightingDataSetting이 없습니다 .", sceneasset.name));
                    return;
                }

                else
                {
                    //원본 라이팅 데이터 링크 및 CopyData 생성
                    //string n = linkList[orgindex].originalScene.name + $"_LightMapData.asset";
                    string n = linkList[orgindex].title + $"_LightMapData.asset";
                    string parentPath = filePath.Substring(0, filePath.LastIndexOf("/"));
                    n = string.Format("{0}/{1}", parentPath, n);
                    LightMapData orgdata = AssetDatabase.LoadAssetAtPath<LightMapData>(n);
                    set.originalLightData = orgdata;
                    set.cloneSceneData = list as CloneSceneData;
                    set.ResetCopyDataList();
                    set.SaveIndexAndMeshData();

                    CreateAssetAndSave(list, filePath);
                    EditorSceneManager.SaveScene(scene);

                    Debug.Log(string.Format("{0} 씬 라이팅 데이터 적용 완료 . LightingDataSetting의 Bake를 통해 확인하세요 .", scene.name));
                }
            }
        }
        private void CreateAssetAndSave(UnityEngine.Object list, string pathName)
        {
            AssetDatabase.CreateAsset(list, pathName);
            Selection.activeObject = list;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
        }
        //__________________________________________________________________________ Info
        private void saveSettingConfigs()
        {
            string rootPath = Application.dataPath;
            rootPath = rootPath.Substring(0, rootPath.LastIndexOf('/') + 1);
            if (rootPath.IndexOf("://") == -1) rootPath = rootPath.Replace(":/", "://");
            string txtDir = rootPath + "Library/lightingdatasetting/";
            string txtPath = txtDir + "settingConfigs.txt";

            StringBuilder sb = new StringBuilder();

            sb.Append(GetSceneGuid(parentFolder));
            sb.Append("//");

            sb.Append(listsize);
            sb.Append("//");

            for (int i = 0, l1 = linkList.Count; i < l1; i++)
            {
                SceneLinkedInfo info = linkList[i];
                if (info.copyScenes.Length == 0)
                {
                    Debug.Log("Empty CopyScenes | 씬을 등록해 주세요 .");
                    return;
                }
                if (info.originalScene == null)
                {
                    Debug.Log("Empty OriginalScene | 씬을 등록해 주세요 .");
                    return;
                }

                sb.Append("{{{");
                sb.Append("title:");
                sb.Append(info.title);
                sb.Append(",");
                sb.Append("guid:");
                sb.Append(GetSceneGuid(info.originalScene));
                sb.Append("[[");

                for (int j = 0, l2 = info.copyScenes.Length; j < l2; j++)
                {
                    sb.Append($"Copy{j + 1}:");
                    sb.Append(GetSceneGuid(info.copyScenes[j]));
                    sb.Append(",");
                }
                sb.Append("]]");
                sb.Append("}}}");
                if (i < l1 - 1) sb.Append("\n********************************\n");
            }

            if (!Directory.Exists(txtDir)) Directory.CreateDirectory(txtDir);

            FileStream fs = File.Create(txtPath);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(sb.ToString());
            sw.Close();
            fs.Close();

            Debug.Log("저장되었습니다. ");
        }
        private void loadSettingConfigs()
        {
            string rootPath = Application.dataPath;
            rootPath = rootPath.Substring(0, rootPath.LastIndexOf('/') + 1);
            if (rootPath.IndexOf("://") == -1) rootPath = rootPath.Replace(":/", "://");
            string txtDir = rootPath + "Library/lightingdatasetting/";
            string txtPath = txtDir + "settingConfigs.txt";

            if (!Directory.Exists(txtDir)) return;
            if (!File.Exists(txtPath)) return;

            FileStream fs = File.OpenRead(txtPath);
            StreamReader sw = new StreamReader(fs);

            string configs = sw.ReadToEnd();
            sw.Close();
            fs.Close();

            if (configs.Length == 0) return;

            int sIdx = 0;
            int eIdx = configs.IndexOf("//");

            string parentP = configs.Substring(sIdx, eIdx - sIdx);
            parentFolder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GetAssetPathFormGuid(parentP));

            int sEIdx = configs.LastIndexOf("//");
            listsize = int.Parse(configs.Substring(eIdx + 2, sEIdx - (eIdx + 2)));
            configs = configs.Remove(sIdx, sEIdx + 2);

            List <SceneLinkedInfo> list = new List<SceneLinkedInfo>();
            string[] strings = configs.Split(new string[] { "\n********************************\n" }, StringSplitOptions.None);
            for (int i = 0, l1 = strings.Length; i < l1; i++)
            {
                string[] values = strings[i].Split(new string[] { "\n" }, StringSplitOptions.None);
                SceneLinkedInfo info = new SceneLinkedInfo();

                //title, original scene 찾기
                string title = values[0].Replace("{{{title:", "");
                int startIdx = title.IndexOf(",");
                int oEndIdx = title.IndexOf("[[", startIdx + 1);
                int endIdx = title.IndexOf("]]");
                
                //copy scene size 구하기 위한 용도
                int cIdx = title.LastIndexOf("Copy") + 4;
                int clast = title.LastIndexOf(":") - 1;
                int cSize = 0;
                
                string line = title;
                string guid = null;

                if (startIdx > -1 && oEndIdx > -1)
                {
                    guid = title.Substring(startIdx + 1, (oEndIdx - startIdx) - 1);
                    guid = guid.Replace("guid:", "");
                    title = title.Substring(0, startIdx);
                }

                if (cIdx > -1 && clast > -1)
                {
                    if (cIdx < clast)
                    {
                        string s2 = line.Substring(cIdx, clast);
                        cSize = int.Parse(s2);
                    }

                    else
                    {
                        char s = line[cIdx];
                        cSize = int.Parse(s.ToString());
                    }
                }

                string cguid = null;
                info.copyScenes = new SceneAsset[cSize];

                if(endIdx > -1)
                {
                    cguid = line.Substring(oEndIdx + 2, (endIdx - oEndIdx) - 3);
                    string[] sguids = cguid.Split(',');

                    //if (sguids[0].Contains("null")) continue;

                    for (int j = 0, l2 = sguids.Length; j < l2; j++)
                    {
                        sguids[j] = sguids[j].Replace($"Copy{j + 1}:", "");
                        info.copyScenes[j] = AssetDatabase.LoadAssetAtPath<SceneAsset>(GetAssetPathFormGuid(sguids[j]));
                    }
                }

                info.title = title;
                info.originalScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(GetAssetPathFormGuid(guid));
                
                list.Add(info);
            }
            linkList = list;
        }
        private string GetAssetPathFormGuid(string guid)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            //Debug.Log(string.Format("p : {0}", p));
            if (p.Length == 0) p = "not found";
            return p;
        }
        private string GetSceneGuid<T>(T sceneasset)
        {
            string path = AssetDatabase.GetAssetPath(sceneasset as UnityEngine.Object);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (guid.Length == 0) Debug.Log("Scene의 GUID를 찾지 못했습니다. ");
            return guid;
        }
        //__________________________________________________________________________ DrawGUI
        private static void DrawUILine(Color color = default, int thickness = 1, int padding = 10)
        {
            color = color != default ? color : Color.gray;
            Rect r = EditorGUILayout.GetControlRect(false, GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding * 0.5f;
            EditorGUI.DrawRect(r, color);
        }
        //__________________________________________________________________________ List
        [System.Serializable]
        public class SceneLinkedInfo
        {
            public string title = "";
            public SceneAsset originalScene = null;
            public SceneAsset[] copyScenes = null;
        }
    }
}
#endif