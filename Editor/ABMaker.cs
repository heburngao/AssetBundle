
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class ABMaker : MonoBehaviour
{
  
    [MenuItem("Custom/makeAB")]
    public static void MakeAssetBundle()
    {
        string path = "./ABoutput";
        string bundSaveAt = Path.Combine(path, EditorUserBuildSettings.activeBuildTarget.ToString());
        Debug.Log("bundleSaveAt:<color=yellow>\r\n" + bundSaveAt + "</color>");
        if (!Directory.Exists(bundSaveAt))
        {
            Directory.CreateDirectory(bundSaveAt);
        }
        BuildPipeline.BuildAssetBundles(bundSaveAt, BuildAssetBundleOptions.DeterministicAssetBundle, EditorUserBuildSettings.activeBuildTarget);
        //DeterministicAssetBundle or ChunkBasedCompression
        //      var files = Directory.GetFiles(bundSaveAt);
        //      foreach(var file in files){
        //	var md5 = LoadFromAB.UserMd5(file);
        //          Debug.Log("file: " + file + " | " + md5);
        //}
    }



    //	// Use this for initialization
    //	void Start () {

    //	}

    //	// Update is called once per frame
    //	void Update () {

    //	}
}