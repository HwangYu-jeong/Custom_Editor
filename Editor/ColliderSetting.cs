using UnityEngine;
using UnityEditor;

public class ColliderSetting : EditorWindow
{
    private static ColliderSetting _window;
    private static GameObject _copyTarget = null;
    private static string _cubeScale = "0.01";
    private static bool _collidersType = true;
    private static bool _cubeMesh = false;

    [UnityEditor.MenuItem("Window/Test/ColliderSetting")]
    static void Init()
    {
        _window = EditorWindow.GetWindow(typeof(ColliderSetting)) as ColliderSetting;
        if (_window)
        {
            _window.Show();
            _window.titleContent = new GUIContent("Create Object");
        }
    }

    void OnGUI()
    {
        Rect pos = new Rect();

        pos.width = 100; pos.height = 20;
        GUI.Label(pos, "* 콜라이더 생성");

        pos.y += 25f; pos.width = 100f;

        if (GUI.Button(pos, "Create Colliders"))
        {
            SetColliders(_collidersType);
        }

        pos.y += 40f; pos.width = 200f;
        GUI.Label(pos, "* 선택 오브젝트 붙여넣기");

        pos.y += 22f; pos.width = 100f;
        _copyTarget = (GameObject)EditorGUI.ObjectField(pos, _copyTarget, typeof(GameObject), true);
        pos.y += 25f; pos.width = 100f;
        if (GUI.Button(pos, "Paste Object"))
        {
            CopyObject();
        }

        pos.y += 40f; pos.width = 200f;
        GUI.Label(pos, "* Cube Scale");

        pos.y += 22f; 
        _cubeScale = EditorGUI.TextField(pos, "Scale", _cubeScale);

        pos.y += 22f; pos.width = 100f;
        if (GUI.Button(pos, "Set Scale"))
        {
            CubeScale();
        }

        pos.y += 40f; pos.width = 100f;
        GUI.Label(pos, "* Fit Collider");

        pos.y += 22f; pos.width = 100f;
        if (GUI.Button(pos, "Set Collider"))
        {
            SetColliders(!_collidersType);
        }

        pos.y += 40f; pos.width = 150f;
        GUI.Label(pos, "* Mesh Renderer");

        pos.y += 22f; pos.width = 80f;
        if (GUI.Button(pos, "Set False"))
        {
            SetCubeMesh(_cubeMesh);
        }

        pos.x += 80f; pos.width = 80f;
        if (GUI.Button(pos, "Set True"))
        {
            SetCubeMesh(!_cubeMesh);
        }

        pos.x -= 80f; pos.y += 80f; pos.width = 150f;
        GUI.Label(pos, "* Remove Colliders");

        pos.x += 180f; pos.width = 25f;
        if (GUI.Button(pos, "*"))
        {
            bool remove = EditorUtility.DisplayDialog("Remove Colliders", "Warring You Can't be back before station", "Yes, Remove", "No, Back");
            if (remove) RemoveColliders();
        }
    }

    //선택된 하위 자식의 모든 collider를 삭제함
    private void RemoveColliders()
    {
        GameObject[] gameObjects = Selection.gameObjects;

        if (gameObjects.Length == 0)
        {
            Debug.Log("Remove Colliders | 선택된 오브젝트가 없어요.");
        }

        for (int i = 0; i < gameObjects.Length; i++)
        {
            Transform[] allChild = gameObjects[i].GetComponentsInChildren<Transform>();
            
            for (int j = 0; j < allChild.Length; j++)
            {
                if (allChild[j].name == "colliders")
                {
                    if (allChild[j].transform != null)
                    {
                        Undo.DestroyObjectImmediate(allChild[j].gameObject);
                        DestroyImmediate(allChild[j].gameObject);
                    }
                }
            }
        }
    }

    //자식 오브젝트가 딸려있는 오브젝트 복사 해서 붙여넣기
    private void CopyObject()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.Log("Copy Object | 선택된 오브젝트가 없네요.");
            return;
        }

        if (_copyTarget == null)
        {
            Debug.Log("Copy Object | 복사할 오브젝트가 없네요.");
            return;
        }

        GameObject[] gameObjects = Selection.gameObjects;
        Transform childObjects = _copyTarget.GetComponentInChildren<Transform>();
        

        for (int i = 0; i < gameObjects.Length; i++)
        {
            GameObject tr = Instantiate(childObjects.gameObject,gameObjects[i].transform);
            Undo.RegisterCreatedObjectUndo(tr, childObjects.name);

            tr.name = childObjects.name;
            SetCube(tr);
        }
    }

    //큐브 스케일 변경하기
    private void CubeScale()
    {
        float _scale = float.Parse(_cubeScale);

        GameObject[] gameObjects = Selection.gameObjects;

        if (gameObjects.Length == 0) {
            Debug.Log("Cube Scale | 선택된 오브젝트가 없네요.");
        }

        for(int i=0; i<gameObjects.Length; i++)
        {
            Undo.RecordObject(gameObjects[i].transform, "reset scale");
            gameObjects[i].transform.localScale = new Vector3(_scale, _scale, _scale);
        }
    }

    private void SetColliders(bool type) 
    {
        GameObject[] gameObjects = Selection.gameObjects;
        GameObject[] tr = new GameObject[gameObjects.Length];
        GameObject[] trChild = new GameObject[gameObjects.Length];

        if (gameObjects.Length == 0)
        {
            Debug.Log("Create Colliders | 선택된 오브젝트가 없네요.");
            return;
        }

        for (int i = 0; i < gameObjects.Length; ++i)
        {
            tr[i] = new GameObject("colliders");
            Undo.RegisterCreatedObjectUndo(tr[i], "colliders");

            tr[i].transform.SetParent(gameObjects[i].transform);
            SetCube(tr[i]);


            trChild[i] = GameObject.CreatePrimitive(PrimitiveType.Cube); 
            Undo.RegisterCreatedObjectUndo(trChild[i], "Cube");

            Undo.RecordObject(gameObjects[i].transform, "reset transform"); 
            trChild[i].transform.SetParent(tr[i].transform);
            trChild[i].transform.localPosition = Vector3.zero;


            if (!type)
            {
                MeshRenderer renderer = gameObjects[i].GetComponent<MeshRenderer>();
                trChild[i].transform.localRotation = Quaternion.Inverse(gameObjects[i].transform.localRotation);
                trChild[i].transform.localScale = renderer.bounds.size / gameObjects[i].transform.localScale.x;
            }

            else
            {
                trChild[i].transform.localRotation = Quaternion.identity;
                trChild[i].transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            }
        }

    }

    //선택된 모델의 colliders를 찾아 모든 cube mesh renderer true
    private void SetCubeMesh(bool set)
    {
        Transform[] transforms = Selection.transforms;
        Transform[] allTransforms;
        MeshRenderer[] allMeshRenderer;

        if (Selection.gameObjects.Length == 0)
        {
            Debug.Log("Mesh Renderer | 선택된 오브젝트가 없네요.");
            return;
        }

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name.Contains("Cube"))
            {
                transforms[i].gameObject.GetComponent<MeshRenderer>().enabled = true;
            }

            else
            {
                allTransforms = transforms[i].GetComponentsInChildren<Transform>();

                if (!allTransforms[i].gameObject.activeSelf) continue;

                for (int j = 0; j < allTransforms.Length; j++)
                {
                    if (allTransforms[j].name != "colliders") continue;
                    allMeshRenderer = allTransforms[j].GetComponentsInChildren<MeshRenderer>();

                    for (int k = 0; k < allMeshRenderer.Length; k++)
                    {
                        allMeshRenderer[k].enabled = set;
                    }
                }
            }
        }
    }

    private void SetCube(GameObject cube)
    {
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = Vector3.one;
    }

}
