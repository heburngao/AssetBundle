//using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
//using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
// 远程加载assetbundle, 并保存在本地目录，
//从本地目录assetbundle中加载对象，显示，消毁内存中的assetbundle对象
public class LoadFromAB : MonoBehaviour
{

	string path;
    // Use this for initialization
    void Start()
    {
        
        Caching.compressionEnabled = true;
        var clist = new List<string>();
        Caching.GetAllCachePaths(clist);

        for (int i = 0; i < clist.Count; i ++ ){
            print(Caching.cacheCount +"<color=red>占用缓存路径:"+ clist[i]+ "</color>");
            path = clist[i];
            print(Application.persistentDataPath);
            print(Application.dataPath);
            print(Application.temporaryCachePath);
            print(Application.streamingAssetsPath);
		}
        //Caching.ClearCache();
        //return;

        //string pathSaveAt = "file:///"+Application.persistentDataPath+ "/iOS/";
        //string pathSaveAt = "file://" + Path.Combine(Application.dataPath, "iOS");

        //string pathSaveAt = GetPath() + "/iOS/";
        //if (!Directory.Exists(pathSaveAt))
        //{
        //    Directory.CreateDirectory(pathSaveAt);
        //}

        StartCoroutine(DownLoadAssetBundle(PLATFORM, true));

        StartCoroutine(LoadFromFile("fbx2", "sword_1"));
        StartCoroutine(LoadFromFile("fbx2", "axe_1"));
    }
    //先回笼出第一次加载保存的依赖关系，再加载一次相关被依赖bundle,最终加载并实例化目标bundle的对象
    IEnumerator LoadFromFile(string bundleName, string goName)
    {
        while (!Caching.ready)
        {
            yield return null;
        }
        while (allbundles == null || fileCount < allbundles.Length)
        {
            Debug.LogError("watting...");
            yield return null;
        }

        string pathSaveAt = GetPath() + "/" + PLATFORM+"/";


        BundlesInfo info = null;
        bool hasManifest = remoteDependce.TryGetValue(bundleName, out info);
        if (hasManifest)
        {

            string[] depc = info.bundlesNames.ToArray();
            if (depc.Length > 0)
            {
                foreach (var dependbundle in depc)
                {
                    Debug.Log(bundleName + "<color=yellow>->依赖: </color>" + dependbundle);
                    AssetBundle dependAB = AssetBundle.LoadFromFile(pathSaveAt + dependbundle);
                }
            }
        }
        AssetBundle ab = AssetBundle.LoadFromFile(pathSaveAt + bundleName);
        GameObject go = ab.LoadAsset<GameObject>(goName);
        Instantiate(go);
        AssetBundle.UnloadAllAssetBundles(false);
    }
    int fileCount = 0;
    string[] allbundles;
    AssetBundleManifest manifest;
    Dictionary<string, BundlesInfo> remoteDependce = new Dictionary<string, BundlesInfo>();
    const string PLATFORM = "iOS";
    IEnumerator DownLoadAssetBundle(string assetbundleName, bool isFirst)
    {
        while (!Caching.ready)
        {
            yield return null;
        }
        //string uri = "http://localhost/ABoutput/"+PLATFORM;
        string uri = "http://192.168.1.106/ABoutput/" + PLATFORM;

        if (isFirst)
        {
            Debug.Log("isFirst:  " + isFirst + "  uri: <color=yellow>" + uri + "/" + assetbundleName + "</color>");

             //方法1:
            //UnityWebRequest request = UnityWebRequest.GetAssetBundle(uri + "/" + assetbundleName);
            //yield return request.SendWebRequest();
            //while (!request.isDone || !string.IsNullOrEmpty(request.error))
            //{
            //    yield return null;
            //}
            //// assetbundle加载 实现::
            ////AssetBundle bundle = (request.downloadHandler as DownloadHandlerAssetBundle).assetBundle;
            //AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);

            //方法2:
            
            WWW www = new WWW(uri + "/" + assetbundleName);
            yield return www;

            if (!string.IsNullOrEmpty(www.error))
            {
                yield break;
            }

            while (!www.isDone)
            {
                yield return null;
            }
            AssetBundle bundle = www.assetBundle;
             
             
            manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            allbundles = manifest.GetAllAssetBundles();
            var bundlsList = new List<string>(allbundles);
            Debug.Log("<color=orange>总bundle 数</color> :count of bundlsList::: " + bundlsList.Count);

            //Debug.Log(assetbundleName +" || "+ manifest.GetAssetBundleHash(assetbundleName));
            //bool iscached = Caching.IsVersionCached(GetPath()+"/iOS/"+assetbundleName, manifest.GetAssetBundleHash(assetbundleName));
            //Debug.Log(GetPath() + "/iOS/" + assetbundleName + "<color=red>是否缓存过:</color>" + iscached);
            
                 
            foreach (var remoteBundle in bundlsList)
            {

                //bundlsList.Remove(bundleinfo);
                // 加载bundle 
                //StartCoroutine(DownLoadAssetBundle(eachBundle, false)); 移到下面
                //Debug.Log("count of bundlsList: " + bundlsList.Count);

                // 加载依赖
                Debug.Log("<color=white>bundle: " + remoteBundle + "</color>");
                var dependenceBundles = manifest.GetAllDependencies(remoteBundle);
                var subDependce = new List<string>();//每个bundle的子级依赖
                                                     //var objs = bundle.LoadAllAssets();
                                                     //处理依赖关系
                foreach (string dependbundle in dependenceBundles)
                {
                    if (!bundlsList.Contains(dependbundle))
                    {
                        // 加载bundle  ,其实并没有使用到 
                        //Debug.Log("非重复,加载依赖 <color=yellow>dependce bundle: " + dependbundle + "</color>");
                        //StartCoroutine(DownLoadAssetBundle(dependbundle, false));

                    }
                    else
                    {
                        Debug.Log("<color=yellow>加载子级依赖</color> <color=red> dependce bundle: " + dependbundle + "</color>");
                        subDependce.Add(dependbundle);
                    }

                }
                Hash128 remoteHashcode = manifest.GetAssetBundleHash(remoteBundle);
                BundlesInfo remoteBundleInfo = new BundlesInfo
                {
                    bundlesNames = subDependce,
                    hashCode = remoteHashcode,
                };
                remoteDependce.Add(remoteBundle, remoteBundleInfo);
                // 加载bundle 
                //StartCoroutine(DownLoadAssetBundle(remoteBundle, false));
            }
            SaveTheDownLoad(www, assetbundleName);//只适用于方法二

            foreach (var remoteBundle in bundlsList){
                // 加载bundle 
                StartCoroutine(DownLoadAssetBundle(remoteBundle, false));
            }
        }
        else
        {
            BundlesInfo remoteBundlesInfo = null; 
            if (remoteDependce.TryGetValue(assetbundleName, out remoteBundlesInfo))
            {
                //检测是否被缓存过，如果没有则下载
                //if (!Caching.IsVersionCached(uri + "/" + assetbundleName, bundlesInfo.hashCode))
                var localHashCode = getLocalHashCode(assetbundleName);
                var remoteHashCode = remoteBundlesInfo.hashCode.ToString();

                Debug.Log("<color=purple>"+localHashCode + " / " + remoteHashCode + "</color>");
                if (localHashCode != remoteHashCode)//如果本地记录的hashcode 与服务端不同，则要更新下载
                {

                    Debug.Log("<color=cyan> 更新,下载 bundle 就绪: " +path+"/"+ assetbundleName + "</color>");

#region

                    //UnityWebRequest request = UnityWebRequest.GetAssetBundle(uri + "/" + assetbundleName);
                    //yield return request.SendWebRequest();
                    //if(!string.IsNullOrEmpty(request.error))
                    //{
                    //    yield break;
                    //}
                    
                    //SaveTheDownLoad(request, assetbundleName);
                     
#endregion
                    #region
                    WWW www = new WWW(uri + "/" + assetbundleName);

                    if (!string.IsNullOrEmpty(www.error))
                    {
                        yield break;
                    }

                    yield return www;
                    while (!www.isDone)
                    {
                        yield return null;
                    }
                    fileCount++;
                    print("加载进度::::: " + www.progress * 100 + " | " + fileCount);

                    SaveTheDownLoad(www, assetbundleName);

#endregion
                    //}
                    //catch (Exception ex)
                    //{
                    //    Debug.LogError("错:" + ex.Message);
                    //}
                }else{
                    print("无需更新: " + assetbundleName);
                    fileCount++;
                }
            }
        }


    }
    AssetBundle localABRoot;
    string getLocalHashCode(string bundleName){
        string pathSaveAt = GetPath() + "/"+PLATFORM+"/";
        if(!Directory.Exists(pathSaveAt)){
            Debug.LogError("A");
            return string.Empty;
        }
        if(localABRoot== null)  localABRoot = AssetBundle.LoadFromFile(pathSaveAt + PLATFORM);
        if(!File.Exists(pathSaveAt+bundleName)){
            Debug.LogError("B:" + pathSaveAt + bundleName);
            return string.Empty;
        }
        var localABmanifest = localABRoot.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        var localHashcode = localABmanifest.GetAssetBundleHash(bundleName);
        //var mf = localABmanifest.GetAllDependencies(bundleName);
        //var mf2 = localABmanifest.GetDirectDependencies(bundleName);
        localABRoot.Unload(true);
        return localHashcode.ToString();
         
    }
    void SaveTheDownLoad(WWW www, string assetbundleName)
    {
        string pathSaveAt = GetPath() + "/"+PLATFORM+"/";
        Debug.Log("AT: " + pathSaveAt);
        if (!Directory.Exists(pathSaveAt))
        {
            Directory.CreateDirectory(pathSaveAt);
        }


        //if (request.error != null)
        //{
        //    Debug.LogError("request error");
        //    yield break;
        //}
        //while (!request.downloadHandler.isDone)
        //{
        //    yield return null;
        //}


        //try
        //{
        if (www.isDone)
        {
            Debug.Log("save AB: <color=cyan>[" + assetbundleName + "]</color> is done : <color=green>" + www.isDone + "</color>");
        }
        else
        {
            Debug.Log("save AB: <color=cyan>[" + assetbundleName + "]</color> is done : <color=red>" + www.isDone + "</color>");
        }
        byte[] bytes = www.bytes;//request.downloadHandler.data;


        //var localmanifest = www.assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
         
        //将assetbundle存到本地，方法一,效率最高:
        //FileInfo info = new FileInfo(pathSaveAt + assetbundleName);
        //FileStream fs = info.Create();
        //fs.Write(bytes, 0, bytes.Length);
        //fs.Flush();
        //fs.Close();
        //fs.Dispose();
        saveBytes(pathSaveAt,assetbundleName,www.bytes);

        www.assetBundle.Unload(true);
        www.Dispose();

        //将assetbundle存到本地，方法二:
        //File.WriteAllBytes(pathSaveAt+assetbundleName,bytes);//不可用

        //将assetbundle存到本地，方法三:
        //var stream = File.Open(pathSaveAt+assetbundleName, FileMode.Create);
        //    stream.Write(bytes, 0, bytes.Length);
        //stream.Flush();
        //    stream.Close();
        //stream.Dispose();

        //将assetbundle存到本地，方法四:
        //FileStream fs = new FileStream(pathSaveAt+assetbundleName, FileMode.CreateNew);
        //BinaryWriter bw = new BinaryWriter(fs);
        //bw.Write(bytes);
        //bw.Close();
        //fs.Close();
    }
    void saveBytes(string path,string filename,byte[] bytes){
        FileInfo info = new FileInfo(path + filename);
        FileStream fs = info.Create();
        fs.Write(bytes, 0, bytes.Length);
        fs.Flush();
        fs.Close();
        fs.Dispose();

        //www.assetBundle.Unload(true);
        //www.Dispose();
    }

    void SaveTheDownLoad(UnityWebRequest request, string assetbundleName)
    {
        string pathSaveAt = GetPath() + "/"+PLATFORM+"/";
        Debug.Log("AT: " + pathSaveAt);
        if (!Directory.Exists(pathSaveAt))
        {
            Directory.CreateDirectory(pathSaveAt);
        }


        //if (request.error != null)
        //{
        //    Debug.LogError("request error");
        //    yield break;
        //}
        //while (!request.downloadHandler.isDone)
        //{
        //    yield return null;
        //}


        //try
        //{
        if (request.isDone)
        {
            Debug.Log("save AB: <color=cyan>[" + assetbundleName + "]</color> is done : <color=green>" + request.isDone + "</color>");
        }
        else
        {
            Debug.Log("save AB: <color=cyan>[" + assetbundleName + "]</color> is done : <color=red>" + request.isDone + "</color>");
        }
        byte[] bytes = request.downloadHandler.data;

        //将assetbundle存到本地，方法一,效率最高:
        FileInfo info = new FileInfo(pathSaveAt + assetbundleName);
        FileStream fs = info.Create();
        fs.Write(bytes, 0, bytes.Length);
        fs.Flush();
        fs.Close();
        fs.Dispose();

        AssetBundle bundle = (request.downloadHandler as DownloadHandlerAssetBundle).assetBundle;
        bundle.Unload(true);
        request.Dispose();

        //将assetbundle存到本地，方法二:
        //File.WriteAllBytes(pathSaveAt+assetbundleName,bytes);//不可用

        //将assetbundle存到本地，方法三:
        //var stream = File.Open(pathSaveAt+assetbundleName, FileMode.Create);
        //    stream.Write(bytes, 0, bytes.Length);
        //stream.Flush();
        //    stream.Close();
        //stream.Dispose();

        //将assetbundle存到本地，方法四:
        //FileStream fs = new FileStream(pathSaveAt+assetbundleName, FileMode.CreateNew);
        //BinaryWriter bw = new BinaryWriter(fs);
        //bw.Write(bytes);
        //bw.Close();
        //fs.Close();

    }

    string GetPath()
    {
#if UNITY_EDITOR
        //return @"file://"+Application.persistentDataPath;//+ "/StreamingAssets";
        //return Application.persistentDataPath;
        return path;
#elif UNITY_IPHONE
        return Application.dataPath +"/Raw";
#elif UNITY_ANDROID
        return "jar:file://"+Application.dataPath+"!/assets//";
#else
		return "";
#endif
    }
    // Update is called once per frame
    void Update()
    {

    }



    public static string UserMd5(string str)
    {
        string cl = str;
        StringBuilder pwd = new StringBuilder();
        MD5 md5 = MD5.Create();//实例化一个md5对像
                               // 加密后是一个字节类型的数组，这里要注意编码UTF8/Unicode等的选择　
        byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(cl));
        // 通过使用循环，将字节类型的数组转换为字符串，此字符串是常规字符格式化所得
        for (int i = 0; i < s.Length; i++)
        {
            // 将得到的字符串使用十六进制类型格式。格式后的字符是小写的字母，如果使用大写（X）则格式后的字符是大写字符
            pwd.Append(s[i].ToString("X2"));
            //pwd = pwd + s[i].ToString("X");

        }
        return pwd.ToString();
    }
}


public class BundlesInfo {
    public List<string> bundlesNames;
    public Hash128 hashCode;
        }