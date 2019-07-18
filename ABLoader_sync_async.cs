using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
//using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
public enum PlatForm
{
    StandaloneWindows64,
    iOS,
    Android,
}
// 远程加载assetbundle, 并保存在本地目录，
//从本地目录assetbundle中加载对象，显示，消毁内存中的assetbundle对象
public class ABLoader : MonoBehaviour
{

    string local_cacheBundle_path;
    // Use this for initialization
    void Start()
    {
        //Caching.ClearCache();
        Caching.compressionEnabled = true;
        
        //setLocalCache();// no use

        //Caching.ClearCache();
        //return;

        //string pathSaveAt = "file:///"+Application.persistentDataPath+ "/iOS/";
        //string pathSaveAt = "file://" + Path.Combine(Application.dataPath, "iOS");

        //string pathSaveAt = GetPath() + "/iOS/";
        //if (!Directory.Exists(pathSaveAt))
        //{
        //    Directory.CreateDirectory(pathSaveAt);
        //}
       
    }

    private void setLocalCache()// no useful in dll lib, only useful in UnityEngine Project
    {
        var clist = new List<string>();
        Caching.GetAllCachePaths(clist);

        for (int i = 0; i < clist.Count; i++)
        {
            print("cacheCount: " + Caching.cacheCount + " \n\r <color=red>占用缓存路径:" + clist[i] + "</color>");
            local_cacheBundle_path = clist[i];
            print("<color=red>persistentDataPath:</color> " + Application.persistentDataPath);
            print("<color=red>dataPath:</color> " + Application.dataPath);
            print("<color=red>temporaryCachePath:</color> " + Application.temporaryCachePath);
            print("<color=red>streamingAssetsPath:</color> " + Application.streamingAssetsPath);
        }
    }

    public void RunABLoader()
    {
        PLATFORM = PlatForm.StandaloneWindows64;
        StartCoroutine(Initial_DownLoadAssetBundle(PLATFORM.ToString()));

        #region test code
        // StartCoroutine(LoadFromFile("a","Workshop Set"));
        //StartCoroutine(LoadFromFile("b", "Cube"));
        #endregion
    }
    //先回笼出第一次加载保存的依赖关系，再加载一次相关被依赖bundle,最终加载并实例化目标bundle的对象


    Dictionary<string, BundlesInfo> remoteDependce = new Dictionary<string, BundlesInfo>();
    Dictionary<string, BundlesInfo> localDependce = new Dictionary<string, BundlesInfo>();

    public PlatForm PLATFORM;//"StandaloneWindows64";//"iOS";
                             //string uri = "http://localhost/ABoutput/"+PLATFORM;
    public string URI_CDN;//= "http://192.168.11.51:7080/ABoutput/" + PLATFORM;
    string rootRemote_BundlePath;
    List<string> localBundleNameList = null;
    string pathSaveAt;// = GetPath() + "/" + PLATFORM + "/";
    IEnumerator Initial_DownLoadAssetBundle(string assetbundleName)
    {
        while (!Caching.ready)
        {
            yield return null;
        }

        if (string.IsNullOrEmpty(URI_CDN))
        {
            URI_CDN = "http://192.168.11.51:7080/ABoutput/" + PLATFORM;
        }
        else
        {
            URI_CDN += PLATFORM;
        }
        var rootAssetBundleName = PLATFORM.ToString();
        yield return GetLocalBundleList(rootAssetBundleName);


        rootRemote_BundlePath = URI_CDN + "/" + assetbundleName;
        Debug.Log("  uri: <color=yellow>" + rootRemote_BundlePath + "</color>");
        UnityWebRequest request = UnityWebRequest.Get(rootRemote_BundlePath);
        //方法1:
        yield return RequestDownload(request, assetbundleName);

        var rootBundleBytes = request.downloadHandler.data;

        AssetBundle rootRemoteBundle = AssetBundle.LoadFromMemory(rootBundleBytes);
        List<string> bundlsList = this.CollectionRemoteDependence(rootRemoteBundle, remoteDependce);
        rootRemoteBundle.Unload(true);

        //AssetBundle bundle = (request.downloadHandler as DownloadHandlerAssetBundle).assetBundle;
        //AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
        //##########################################################################################################################
        //方法2:

        //WWW www = new WWW(uri + "/" + assetbundleName);
        //yield return www;

        //if (!string.IsNullOrEmpty(www.error))
        //{
        //    yield break;
        //}

        //while (!www.isDone)
        //{
        //    yield return null;
        //}
        //AssetBundle bundle = www.assetBundle;


        //##########################################################################################################################

        //manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");



        //allbundles = manifest.GetAllAssetBundles();



        //List<string> bundlsList = new List<string>(allbundles);
        ////Debug.Log("<color=orange>总bundle 数</color> :count of bundlsList::: " + bundlsList.Count);



        ////Debug.Log(assetbundleName +" || "+ manifest.GetAssetBundleHash(assetbundleName));
        ////bool iscached = Caching.IsVersionCached(GetPath()+"/iOS/"+assetbundleName, manifest.GetAssetBundleHash(assetbundleName));
        ////Debug.Log(GetPath() + "/iOS/" + assetbundleName + "<color=red>是否缓存过:</color>" + iscached);


        //foreach (string remoteBundleName in bundlsList)
        //{
        //    // 加载依赖
        //    Debug.Log("<color=white>bundle: " + remoteBundleName + "</color>");
        //    var dependenceBundles = manifest.GetAllDependencies(remoteBundleName);
        //    var subDependce = new List<string>();//每个bundle的子级依赖
        //                                         //var objs = bundle.LoadAllAssets();
        //                                         //处理依赖关系
        //    foreach (string dependbundle in dependenceBundles)
        //    {
        //        if (!bundlsList.Contains(dependbundle))
        //        {
        //            // 加载bundle  ,其实并没有使用到 
        //            Debug.LogError("加载非依赖 <color=green>dependce bundle: " + dependbundle + "</color>");

        //        }
        //        else
        //        {
        //            Debug.Log("<color=yellow>加载子级依赖</color> <color=red> dependce bundle: " + dependbundle + "</color>");
        //            subDependce.Add(dependbundle);
        //        }

        //    }
        //    Hash128 remoteHashcode = manifest.GetAssetBundleHash(remoteBundleName);
        //    BundlesInfo remoteBundleInfo = new BundlesInfo
        //    {
        //        bundlesNames = subDependce,
        //        hashCode = remoteHashcode,
        //    };
        //    remoteDependce.Add(remoteBundleName, remoteBundleInfo);
        //    // 加载bundle 
        //    //StartCoroutine(DownLoadAssetBundle(remoteBundle, false));
        //}

        //SaveTheDownLoad(www, assetbundleName);//只适用于方法二
        //bundle.Unload(true);



        //############################################################################################################################################
        foreach (var remoteBundle in bundlsList)
        {
            // 加载bundle 
            StartCoroutine(Initial_DownLoad_DependenceAssetBundle(remoteBundle));
        }
    }
    private IEnumerator Initial_DownLoad_DependenceAssetBundle(string assetbundleName)
    {
        BundlesInfo remoteBundlesInfo;
        if (remoteDependce.TryGetValue(assetbundleName, out remoteBundlesInfo))
        {
            //检测是否被缓存过，如果没有则下载
            //if (!Caching.IsVersionCached(uri + "/" + assetbundleName, bundlesInfo.hashCode))
            print("<color=white>local bundle path:</color>" + pathSaveAt + assetbundleName);
            print("<color=cyan>remote bundle path:</color>" + rootRemote_BundlePath + "/" + assetbundleName);

            string localHashCode = "";//Check(pathSaveAt + assetbundleName);//getLocalHashCode(assetbundleName);

            if (localBundleNameList != null)
            {
                BundlesInfo localBundlesInfo;
                if (localDependce.TryGetValue(assetbundleName, out localBundlesInfo))
                {
                    localHashCode = localBundlesInfo.hashCode.ToString();

                }
            }
            //var remoteHashCode = Check_Stream(rootRemote_BundlePath + "/" + assetbundleName);//
            string remoteHashCode = remoteBundlesInfo.hashCode.ToString();

            Debug.Log("compaire hash code : <color=purple>" + localHashCode + " / " + remoteHashCode + "</color>");
            if (localHashCode != remoteHashCode)//如果本地记录的hashcode 与服务端不同，则要更新下载
            {

                Debug.Log("<color=orange> 需要更新,[ Update ] dependence bundle @ </color>: " + local_cacheBundle_path + "/" + assetbundleName);

                #region

                UnityWebRequest request = UnityWebRequest.Get(URI_CDN + "/" + assetbundleName);

                yield return RequestDownload(request, assetbundleName);

                #endregion
                #region
                //WWW www = new WWW(uri + "/" + assetbundleName);

                //if (!string.IsNullOrEmpty(www.error))
                //{
                //    yield break;
                //}

                //yield return www;
                //while (!www.isDone)
                //{
                //    yield return null;
                //}
                //fileCount++;
                //print("加载进度::::: " + www.progress * 100 + " | " + fileCount);

                //SaveTheDownLoad(www, assetbundleName);

                #endregion

            }
            else
            {
                print("<color=green>无需更新</color> assetbundleName :<color=white> " + assetbundleName + "</color> fileCount: " + count_downloaded);
                count_downloaded++;
            }
        }
    }
    private IEnumerator GetLocalBundleList(string rootAssetBundleName)
    {
        #region local assetbundle 
        pathSaveAt = GetLocalCachePath() + "/" + PLATFORM + "/";
        print("pathSaveAt :: " + pathSaveAt);
        if (Directory.Exists(pathSaveAt))
        {
            AssetBundle localRootAB_loaded = AssetBundle.LoadFromFile(pathSaveAt + rootAssetBundleName);
            localBundleNameList = CollectionLocalDependence(localRootAB_loaded, localDependce);
            localRootAB_loaded.Unload(true);
        }
        yield return new WaitForSeconds(1f);

        #endregion
    }
    private int count_downloaded = 0;
    private float currentDownloadProgress = 0f;
    private IEnumerator RequestDownload(UnityWebRequest request, string assetbundleName)
    {
        // UnityWebRequest request = UnityWebRequest.Get(rootRemoteBundlePath);
        request.timeout = 30;
        yield return request.SendWebRequest();
        while (!request.isDone || !string.IsNullOrEmpty(request.error))
        {
            Debug.LogError("load assets bundle Error");
            yield return null;
        }

        if (request.isHttpError || request.isNetworkError)
        {
            Debug.LogError("load assets bundle Error , break");
            yield break;
        }
        currentDownloadProgress = request.downloadProgress;
        while (currentDownloadProgress < .9f)
        {
            print(" assetbundleName: <color=cyan>" + assetbundleName + "</color> downloadProgress : " + currentDownloadProgress);
            yield return null;
        }

        print("done !! assetbundleName: <color=cyan>" + assetbundleName + "</color> downloadProgress : " + currentDownloadProgress + " ::: " + count_downloaded + "/" + remoteDependce.Count);
        count_downloaded++;
        //byte[] rootBundleBytes = request.downloadHandler.data;
        //print(" loaded root bundle byte n: " + rootBundleBytes.Length);
        // assetbundle加载 实现::
        //AssetBundle bundle = AssetBundle.LoadFromMemory(bytte);
        SaveTheDownLoad(request, assetbundleName);//只适用于方法一


    }
    public int GetDownLoadedCount()
    {
        return this.count_downloaded;
    }
    public int GetTotalDownLoadedCount()
    {
        return this.remoteDependce.Count;
    }
    public float GetCurrentBundleLoadProgress()
    {
        return currentDownloadProgress;
    }
    private List<string> CollectionLocalDependence(AssetBundle rootLocalBundle, Dictionary<string, BundlesInfo> _localDependce)
    {
        AssetBundleManifest rootLocalManifest = rootLocalBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        string[] allbundles = rootLocalManifest.GetAllAssetBundles();
        List<string> bundlsList = new List<string>(allbundles);
        //Debug.Log("<color=orange>总bundle 数</color> :count of bundlsList::: " + bundlsList.Count);
        //Debug.Log(assetbundleName +" || "+ manifest.GetAssetBundleHash(assetbundleName));
        //bool iscached = Caching.IsVersionCached(GetPath()+"/iOS/"+assetbundleName, manifest.GetAssetBundleHash(assetbundleName));
        //Debug.Log(GetPath() + "/iOS/" + assetbundleName + "<color=red>是否缓存过:</color>" + iscached);
        foreach (string localBundleName in bundlsList)
        {
            // 加载依赖
            Debug.Log("<color=white>bundle: " + localBundleName + "</color>");
            var dependenceBundles = rootLocalManifest.GetAllDependencies(localBundleName);
            var subDependce = new List<string>();//每个bundle的子级依赖
                                                 //var objs = bundle.LoadAllAssets();
                                                 //处理依赖关系
            foreach (string dependbundle in dependenceBundles)
            {
                if (!bundlsList.Contains(dependbundle))
                {
                    // 加载bundle  ,其实并没有使用到 
                    Debug.LogError("加载非依赖 <color=green>dependce bundle: " + dependbundle + "</color>");

                }
                else
                {
                    Debug.Log("<color=yellow>加载子级依赖</color> <color=red> dependce bundle: " + dependbundle + "</color>");
                    subDependce.Add(dependbundle);
                }
            }
            Hash128 remoteHashcode = rootLocalManifest.GetAssetBundleHash(localBundleName);
            BundlesInfo remoteBundleInfo = new BundlesInfo
            {
                bundlesNames = subDependce,
                hashCode = remoteHashcode,
            };
            _localDependce.Add(localBundleName, remoteBundleInfo);
        }
        return bundlsList;
    }
    private List<string> CollectionRemoteDependence(AssetBundle rootRemoteBundle, Dictionary<string, BundlesInfo> _remoteDependce)
    {
        AssetBundleManifest rootRemoteManifest = rootRemoteBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        string[] allbundles = rootRemoteManifest.GetAllAssetBundles();
        List<string> bundlsList = new List<string>(allbundles);
        //Debug.Log("<color=orange>总bundle 数</color> :count of bundlsList::: " + bundlsList.Count);
        //Debug.Log(assetbundleName +" || "+ manifest.GetAssetBundleHash(assetbundleName));
        //bool iscached = Caching.IsVersionCached(GetPath()+"/iOS/"+assetbundleName, manifest.GetAssetBundleHash(assetbundleName));
        //Debug.Log(GetPath() + "/iOS/" + assetbundleName + "<color=red>是否缓存过:</color>" + iscached);
        foreach (string remoteBundleName in bundlsList)
        {
            // 加载依赖
            Debug.Log("<color=white>bundle: " + remoteBundleName + "</color>");
            var dependenceBundles = rootRemoteManifest.GetAllDependencies(remoteBundleName);
            var subDependce = new List<string>();//每个bundle的子级依赖
                                                 //var objs = bundle.LoadAllAssets();
                                                 //处理依赖关系
            foreach (string dependbundle in dependenceBundles)
            {
                if (!bundlsList.Contains(dependbundle))
                {
                    // 加载bundle  ,其实并没有使用到 
                    Debug.LogError("加载非依赖 <color=green>dependce bundle: " + dependbundle + "</color>");

                }
                else
                {
                    Debug.Log("<color=yellow>加载子级依赖</color> <color=red> dependce bundle: " + dependbundle + "</color>");
                    subDependce.Add(dependbundle);
                }
            }
            Hash128 remoteHashcode = rootRemoteManifest.GetAssetBundleHash(remoteBundleName);
            BundlesInfo remoteBundleInfo = new BundlesInfo
            {
                bundlesNames = subDependce,
                hashCode = remoteHashcode,
            };
            _remoteDependce.Add(remoteBundleName, remoteBundleInfo);
        }
        return bundlsList;
    }



    //private string GetBundleHash(AssetBundle bundle ,string bundleName)
    //{
    //    var manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
    //    var hashCode = manifest.GetAssetBundleHash(bundleName);
    //    return hashCode.ToString();
    //}
    void SaveTheDownLoad(WWW www, string assetbundleName)
    {
        string pathSaveAt = GetLocalCachePath() + "/" + PLATFORM + "/";
        Debug.Log("AT: " + pathSaveAt);
        if (!Directory.Exists(pathSaveAt))
        {
            Directory.CreateDirectory(pathSaveAt);
        }



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
        saveBytes(pathSaveAt, assetbundleName, www.bytes);

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
    void saveBytes(string path, string filename, byte[] bytes)
    {
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
        string pathSaveAt = GetLocalCachePath() + "/" + PLATFORM + "/";

        if (!Directory.Exists(pathSaveAt))
        {
            Directory.CreateDirectory(pathSaveAt);
        }


        if (request.isDone)
        {
            Debug.Log("<color=cyan>[save AB]:</color> <color=cyan>[" + assetbundleName + "]</color> is done : <color=green>" + request.isDone + "</color>");
        }
        else
        {
            Debug.LogError("<color=cyan>[[save AB]]:</color> <color=cyan>[" + assetbundleName + "]</color> is done : <color=red>" + request.isDone + "</color>");
            return;
        }
        byte[] bytes = request.downloadHandler.data;

        //将assetbundle存到本地，方法一,效率最高:
        Debug.Log("SaveTheDownLoad bundle @ : " + pathSaveAt + assetbundleName);
        FileInfo info = new FileInfo(pathSaveAt + assetbundleName);
        FileStream fs = info.Create();
        fs.Write(bytes, 0, bytes.Length);
        fs.Flush();
        fs.Close();
        fs.Dispose();

        //AssetBundle bundle = (request.downloadHandler as DownloadHandlerAssetBundle).assetBundle;
        //bundle.Unload(true);
        //request.Dispose();

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
    private string _path;
    public void SetLocalCachePath(string path)
    {
        _path = path;
    }
    string GetLocalCachePath()
    {
        if (string.IsNullOrEmpty(_path))
        {
            Debug.LogError(" Error, You need to set the LocalAssetBundle Cache Path !");
        }
        return _path;
//#if UNITY_EDITOR
//        //return @"file://"+Application.persistentDataPath;//+ "/StreamingAssets";
//        //return Application.persistentDataPath;
//        return local_cacheBundle_path;
//#elif UNITY_IPHONE
//        return Application.dataPath +"/Raw";
//#elif UNITY_ANDROID
//        return "jar:file://"+Application.dataPath+"!/assets//";
//#else
//        return "";
//#endif
    }
    // // Update is called once per frame
    //void Update()
    //{

    //}
    //#########################################################################################
    //public void LoadAssetFromBunleAsync(string bundleName, string assetName, Action<GameObject> act)
    //{
    //    //StartCoroutine(LoadFromBunle(bundleName, assetName, act));
    //    LoadFromBunleAsync(bundleName, assetName, act);
    //}
    Dictionary<string,AssetBundle> tempBundles = new Dictionary<string,AssetBundle>();
    private IEnumerator LoadFromBunleAsync(string bundleName, string assetName, Action<UnityEngine.Object> act)
    {
        //print("<color=cyan>will load assets from bundle, 10 seconds later</color>");
        //  yield return new WaitForSeconds(10f);
        print("<color=cyan>will load assets from bundle </color>");
        while (!Caching.ready)
        {
            yield return null;
            //return;
        }


        string pathSaveAt = GetLocalCachePath() + "/" + PLATFORM + "/";


        
        BundlesInfo info;
        bool hasManifest = remoteDependce.TryGetValue(bundleName, out info);
        if (hasManifest)
        {
            string[] depc = info.bundlesNames.ToArray();
            if (depc.Length > 0)
            {
                foreach (string dependbundle in depc)
                {
                    Debug.Log(bundleName + "<color=yellow>-> loadAsset from dependence bundle : </color>" + pathSaveAt + dependbundle);
                    if (!tempBundles.ContainsKey(dependbundle))
                    {
                        AssetBundleCreateRequest dependAB = AssetBundle.LoadFromFileAsync(pathSaveAt + dependbundle);
                        yield return dependAB;
                        yield return new WaitUntil(() => (dependAB.assetBundle != null));
                        tempBundles.Add(dependAB.assetBundle.name, dependAB.assetBundle);
                    }
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                }
            }
        }
        print("<color=cyan>loading assets : " + assetName + " from bundle : " + pathSaveAt + bundleName + "</color>");
        AssetBundleRequest asset = null;
        if (!tempBundles.ContainsKey(bundleName))
        {
            AssetBundleCreateRequest ab = AssetBundle.LoadFromFileAsync(pathSaveAt + bundleName);
            yield return ab;
            yield return new WaitUntil(() => (ab != null));
            asset = ab.assetBundle.LoadAssetAsync<GameObject>(assetName);
            tempBundles.Add(ab.assetBundle.name, ab.assetBundle);
        }
        else
        {
            asset = tempBundles[bundleName].LoadAssetAsync<GameObject>(assetName);
        }
         
        yield return new WaitUntil(() => (asset != null));
        yield return asset;
        if (null != act)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            act(asset.asset);

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return asset;
            //print("bundle : " + ab.assetBundle.name + " Unload()");
            //ab.assetBundle.Unload(false);//AAA
            //foreach (AssetBundle item in tempBundles)
            //{
            //    print("sub bundle : " + item.name + " Unload()");
            //    item.Unload(false);
            //}
            //AssetBundle.UnloadAllAssetBundles(false);
        }
        //Instantiate(go);


    }
    public void UnloadAssetBundleAsyncLoaded()
    {
        foreach (AssetBundle item in tempBundles.Values)
        {
            print("bundle : " + item.name + " Unload()");
            item.Unload(false);
        }
        AssetBundle.UnloadAllAssetBundles(false);
        tempBundles.Clear();
    }
    //#########################################################################################
    public void LoadAssetFromBunle(string bundleName, string assetName, Action<UnityEngine.Object> act)
    {

        //LoadFromBunle(bundleName, assetName , act);
        //LoadFromBunleAsync(bundleName, assetName, act);
        StartCoroutine(LoadFromBunleAsync(bundleName, assetName, act));
    }
    private void LoadFromBunle(string bundleName, string assetName , Action<UnityEngine.Object> act)
    {
        //print("<color=cyan>will load assets from bundle, 10 seconds later</color>");
        //  yield return new WaitForSeconds(10f);
        print("<color=cyan>will load assets from bundle </color>");
        while (!Caching.ready)
        {
            //yield return null;
            return;
        }
 

        string pathSaveAt = GetLocalCachePath() + "/" + PLATFORM + "/";


        List<AssetBundle> bundles = new List<AssetBundle>();
        BundlesInfo info;
        bool hasManifest = remoteDependce.TryGetValue(bundleName, out info);
        if (hasManifest)
        {
            string[] depc = info.bundlesNames.ToArray();
            if (depc.Length > 0)
            {
                foreach (var dependbundle in depc)
                {
                    Debug.Log(bundleName + "<color=yellow>-> loadAsset from dependence bundle : </color>" + pathSaveAt + dependbundle);
                    AssetBundle dependAB = AssetBundle.LoadFromFile(pathSaveAt + dependbundle);
                     
                    bundles.Add(dependAB);
                    //yield return new WaitForEndOfFrame();
                }
            }
        }
        print("<color=cyan>loading assets : " + assetName + " from bundle : " + pathSaveAt + bundleName + "</color>");
        AssetBundle ab = AssetBundle.LoadFromFile(pathSaveAt + bundleName);
        GameObject asset = ab.LoadAsset<GameObject>(assetName);
        if (null != act)
        {
            act(asset);
            print("bundle : " + ab.name + " Unload()");
            ab.Unload(false);//AAA
            foreach (var item in bundles)
            {
                print("sub bundle : " + item.name + " Unload()");
                item.Unload(false);
            }
            AssetBundle.UnloadAllAssetBundles(false);
        }
        //Instantiate(go);
       
        
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


public class BundlesInfo
{
    public List<string> bundlesNames;
    public Hash128 hashCode;
}