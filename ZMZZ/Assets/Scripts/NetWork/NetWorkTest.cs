using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;

public class NetWorkTest : MonoBehaviour
{

    private IEnumerator Start()
    {
        using (UnityWebRequest request = UnityWebRequest.Get("http://127.0.0.1:5080/health"))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();
            if(request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("收到服务器回复" + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("请求失败" + request.error + request.downloadHandler.text);
            }
        }


        
    }


   
}
