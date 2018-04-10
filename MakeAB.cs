using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class MakeAB  {
[MenuItem("自定义/makeAB")]
public static void MakeAssetBundle (){
        string path = "ABoutput";
        string bundSaveAt = Path.Combine(path,EditorUserBuildSettings.activeBuildTarget.ToString());
        Debug.Log("bundSaveAt:"+bundSaveAt);
        if(!Directory.Exists(bundSaveAt)){
            Directory.CreateDirectory(bundSaveAt);
        }
        BuildPipeline.BuildAssetBundles(bundSaveAt,BuildAssetBundleOptions.ChunkBasedCompression/DeterministicAssetBundle,EditorUserBuildSettings.activeBuildTarget);

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
