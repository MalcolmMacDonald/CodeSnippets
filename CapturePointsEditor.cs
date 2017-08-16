using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class CapturePointsEditor : EditorWindow
{


    public static string PathToResources = "Assets/Resources/";
    public static string PathToScriptableObjectsFromResources = "SavedPositions";
    public static string PathToScriptableObjects = PathToResources + PathToScriptableObjectsFromResources;


    [MenuItem("Tools/Porcelain/Capture Point Editor")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(CapturePointsEditor));
    }

    Vector3[] capturePointPositions;

    [SerializeField]
    GameObject capturePointPrefab;

    CapturePointPositionsWrapper so;
    CapturePointPositionsWrapper savedPositions;

    GameObject parent;

    private void OnGUI()
    {
        CapturePoints[] pointsArray = FindObjectsOfType<CapturePoints>();
        if (capturePointPositions == null)
        {
            capturePointPositions = new Vector3[pointsArray.Length];
        }

        //  

        if (GUILayout.Button("Get Capture Points From Scene"))
        {

            GetCapturePoints();
        }
        if (GUILayout.Button("Save Capture Points To File"))
        {
            CreateSO();
        }
        if (GUILayout.Button("Remove Capture Points From Scene"))
        {

            RemoveCapturePoints();
        }
        capturePointPrefab = (GameObject)EditorGUILayout.ObjectField("Capture Point Prefab:", capturePointPrefab, typeof(GameObject), true);
        if (GUILayout.Button("Spawn Capture Points In Scene"))
        {

            SpawnCapturePoints();
        }

        if (GUILayout.Button("Retrieve Capture Points From File"))
        {
            RetrieveCapturePoints();
        }
        if (GUILayout.Button("Save Scene"))
        {
            SaveScene();
        }



        if (capturePointPositions.Length > 0)
        {
            foreach (Vector3 pos in capturePointPositions)
            {
                EditorGUILayout.LabelField("X:" + pos.x + " Y:" + pos.y + " Z:" + pos.z);
            }
        }
    }

    void GetCapturePoints()
    {
        if(parent == null)
        {
          parent =  GameObject.Find("Capture Points Parent");
        }
        if (parent != null)
        {

            CapturePoints[] pointsArray = new CapturePoints[parent.transform.childCount];

            capturePointPositions = new Vector3[pointsArray.Length];
            for (int i = 0; i < capturePointPositions.Length; i++)
            {
                capturePointPositions[i] = parent.transform.GetChild(i).position;
            }
        }
    }

    void RemoveCapturePoints()
    {
        if (parent != null && parent.transform.childCount >= 1)
        {
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                DestroyImmediate(parent.transform.GetChild(i));
            }

        }
        CapturePoints[] pointsArray = FindObjectsOfType<CapturePoints>();
        for (int i = 0; i < pointsArray.Length; i++)
        {
            DestroyImmediate(pointsArray[i].gameObject);
        }
    }

    void SpawnCapturePoints()
    {
        if (parent == null)
        {
            parent = new GameObject();
            parent.name = "Capture Points Parent";
        }
        Vector3[] points = new Vector3[0];
        if (so)
        {
            points = so.capturePointPositions;
        }
        for (int i = 0; i < points.Length; i++)
        {
            Instantiate(capturePointPrefab, points[i], Quaternion.identity, parent.transform);
        }

    }
    void RetrieveCapturePoints()
    {
        savedPositions = (CapturePointPositionsWrapper)(Resources.LoadAll("SavedPositions")[0]);
        if (savedPositions)
        {
            //     so = savedPositions;
            capturePointPositions = savedPositions.capturePointPositions;
        }
    }

    void CreateSO()
    {
        so = ScriptableObject.CreateInstance<CapturePointPositionsWrapper>();

        if (capturePointPositions.Length > 0)
        {
            so.capturePointPositions = capturePointPositions;
        }
        string filename = "/CapturePointsPositions.asset";
        //       string fullPath = PathToScriptableObjects + filename;

        if (!AssetDatabase.IsValidFolder(PathToScriptableObjects))
        {
            AssetDatabase.CreateFolder(PathToResources.TrimEnd('/'), PathToScriptableObjectsFromResources);
        }

        AssetDatabase.CreateAsset(so, PathToScriptableObjects + filename);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
    void SaveScene()
    {
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

    }

}
