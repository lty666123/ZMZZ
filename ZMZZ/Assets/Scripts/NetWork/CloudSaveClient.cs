using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using LitJson;
using System.Text;
using System;

//该类 提供了 读档存档的方法
public class CloudSaveClient : MonoBehaviour
{
    //读取存档是否成功
    public bool DownLoadSucceed {  get; private set; }
    //收到的服务器回复 主要为 是否有存档 和 存档内容
    public string ResponseJson {  get; private set; }

    //存档时使用的登录令牌
    private string uploadToken;

    private bool cloudSaveEnabled;
    //是否需要存档
    private bool needUpload;
    //是否正在上传 防止同时启动多个请求
    private bool isUploading;
    //下一次允许上传的时间
    public float nextUploadTime;
    private GameDataMgr saveSource;

    //本次上传是否成功
    public bool UpLoadSucceed {  get; private set; }

    [Serializable]
    private class SaveGameRequest //上传给服务器的数据结构
    {
        public string saveJson;
    }



    private void Update()
    {
        if(!cloudSaveEnabled
            ||!needUpload
            ||isUploading
            ||Time.unscaledTime<nextUploadTime)
        {
            return;
        }
        StartCoroutine(UploadPendingSave());
    }

    //读取服务器存档 得到的ResponseJson是服务器存储的数据json 等待解析
    public IEnumerator DownLoadSave(string accessToken)
    {
        cloudSaveEnabled = false;
        needUpload = false;
        uploadToken = "";

        DownLoadSucceed = false;
        ResponseJson = "";

        if(string.IsNullOrWhiteSpace(accessToken))
        {
            Debug.LogError("读档失败 没有登录令牌");
            yield break;
        }
        using(UnityWebRequest request = UnityWebRequest.Get("http://127.0.0.1:5080/saves/me"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.timeout = 5;
            yield return request.SendWebRequest();
            if(request.result!=UnityWebRequest.Result.Success)
            {
                Debug.LogError("读档失败" + request.error + request.responseCode + request.downloadHandler.text);
                yield break;
            }
            string responseText = request.downloadHandler.text;
            if(string.IsNullOrWhiteSpace (responseText))
            {
                Debug.LogError("读档失败 服务器返回空内容");
                yield break;
            }
            ResponseJson = responseText;
            DownLoadSucceed=true;
            Debug.Log("已收到服务器回复");
        }
    }

    //对读取的服务器存档进行解析 并使用存档
    public bool ApplyDownLoadedave()
    {
        if(!DownLoadSucceed)
        {
            Debug.LogError("尚未成功取得存档回复");
            return false;
        }

        try
        {
            JsonData response = JsonMapper.ToObject(ResponseJson);
            //服务器回复内容不能丢失
            if(response == null
                ||!response.IsObject
                ||!response.ContainsKey("hasSave")
                ||!response.ContainsKey("saveJson"))
            {
                Debug.LogError("存档回复缺少必要字段");
                return false;
            }
            //服务器返回的内容不能不符合格式
            if (response["hasSave"] == null
                || !response["hasSave"].IsBoolean
                ||  response["saveJson"] == null
                || !response["saveJson"].IsString)
            {
                Debug.LogError("存档回复的字段类型不正确");
                return false;
            }
            //获取服务器返回的两个具体内容 并 存储
            bool hasSave = (bool)response["hasSave"];
            string saveJson = (string)response["saveJson"];

            GameSaveData save;

            if(hasSave)
            {
                if(string.IsNullOrWhiteSpace(saveJson))
                {
                    Debug.LogError("服务器表示有存档 但存档内容为空");
                    return false;
                }
                //服务器里有存档 进行解析
                save = JsonMapper.ToObject<GameSaveData>(saveJson);
            }
            else
            {
                //服务器里没有存档 进行新建
                save = new GameSaveData
                {
                    playerData = new PlayerData(),
                    inventoryData = new InventoryData(),
                    equipmentBagData = new EquipmentBagData(),
                    worldData = new WorldData()
                };
            }
            return GameDataMgr.Instance.ApplyCloudSave(save);//读档成功 返回true
        }
        catch(System.Exception ex)
        {
            Debug.LogError("读档处理失败" + ex.Message);
            return false;
        }
    }

    private IEnumerator UploadPendingSave()
    {
        isUploading = true;
        string token = uploadToken;
        bool succeeded = false;
        try
        {
            GameSaveData save = saveSource.GetAllNeedSaveData();
            string saveJson = JsonMapper.ToJson(save);

            needUpload = false;
            yield return UploadSave(token, saveJson);

            succeeded = UpLoadSucceed;
        }
        finally
        {
            if(!succeeded&&cloudSaveEnabled&&uploadToken == token)
            {
                needUpload = true;
            }
            isUploading = false;

            nextUploadTime = Time.unscaledTime + 3f;
        }
    }

    //存档协程
    private IEnumerator UploadSave(string accessToken,string saveJson)
    {
        UpLoadSucceed = false;
        if(string.IsNullOrWhiteSpace(accessToken)||string.IsNullOrWhiteSpace(saveJson))
        {
            Debug.LogError("上传失败 令牌或存档内容为空");
            yield break;
        }
        SaveGameRequest data  = new SaveGameRequest { saveJson = saveJson };
        
        string requestJson = JsonMapper.ToJson(data);
        using(UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:5080/saves/me","Post"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJson));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.timeout = 10;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("上传存档失败" + request.error + request.responseCode + request.downloadHandler.text);
                yield break;
            }

            UpLoadSucceed = true;
            Debug.Log("云存档保存成功");

        }
    }


    //存档相关
    public void EnableCloudSave(string accessToken)
    {
        if(string.IsNullOrWhiteSpace (accessToken))
        {
            Debug.LogError("无法启动云同步 登录令牌为空");
            return;
        }
        //避免重复订阅
        if(saveSource!=null)
        {
            saveSource.GameSaveChanged -= MarkNeedUpLoad;
        }

        uploadToken = accessToken;
        saveSource = GameDataMgr.Instance;

        saveSource.GameSaveChanged += MarkNeedUpLoad;

        //允许存档
        cloudSaveEnabled = true;

        needUpload = true;

    }


    private void OnDestroy()
    {
        cloudSaveEnabled = false;
        if(saveSource != null)
        {
            saveSource.GameSaveChanged -= MarkNeedUpLoad;
        }
    }
    //记录需要存档的
    private void MarkNeedUpLoad()
    {
        if(!cloudSaveEnabled)
        {
            return;
        }
        needUpload = true;
    }

}
