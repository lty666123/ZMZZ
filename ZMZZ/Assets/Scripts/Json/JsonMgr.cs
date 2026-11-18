using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System.IO;

//确定使用的方案
public enum JsonType
{
    JsonUtlity,
    LitJson,
}

/// <summary>
/// 数据管理类 用于序列化与反序列化
/// </summary>
public class JsonMgr
{
    private static JsonMgr instance = new JsonMgr();
    public static JsonMgr Instance => instance;

    private JsonMgr() { }

    //序列化并储存
    public void SaveData(object data,string fileName,JsonType type = JsonType.LitJson)
    {
        //确定存储路径
        string path = Application.persistentDataPath + "/" + fileName + ".json";
        //进行序列化
        string jsonStr = "";
        switch (type)
        {
            case JsonType.JsonUtlity:
                jsonStr = JsonUtility.ToJson(data);
                break;
            case JsonType.LitJson:
                jsonStr = JsonMapper.ToJson(data);
                break;
        }
        //把序列化所得 存储起来
       File.WriteAllText(path, jsonStr);
    }

    //反序列化 读取数据
    public T LoadData<T>(string fileName, JsonType type = JsonType.LitJson) where T : new()
    {
        string path = Application.streamingAssetsPath + "/" + fileName + ".json";
        if (!File.Exists(path))
            path = Application.persistentDataPath + "/" + fileName + ".json";
        if (!File.Exists(path))
            return new T();
        //进行反序列化
        string jsonStr = File.ReadAllText(path);
        T data = default(T);
        switch (type)
        {
            case JsonType.JsonUtlity:
                data = JsonUtility.FromJson<T>(jsonStr);
                break;
            case JsonType.LitJson:
                data = JsonMapper.ToObject<T>(jsonStr);
                break;
        }
        return data;
    }
}
