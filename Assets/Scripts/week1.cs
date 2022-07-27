using System.Security.AccessControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Text;
using System.Web;
using System.IO;

public class week1 : MonoBehaviour
{
    TMP_InputField outputArea;
    string endpoint="https://centralindia.api.cognitive.microsoft.com/";
    string apim_key="4bcb8912069b477d9ae36d4ed756662f";
    void Start()
    {
        outputArea = GameObject.Find("OutputArea").GetComponent<TMP_InputField>();
        GameObject.Find("HardcodedImageButton").GetComponent<Button>().onClick.AddListener(FindData);
    }
    void FindData()
    {
        outputArea.text = "Loading...";
        StartCoroutine(PostRead());
    }
    IEnumerator PostRead()
    {
        //request url with parameters
        var uri = endpoint+"vision/v3.2/read/analyze?readingOrder=natural";
        string rawData="{\"url\": \"https://www.pngitem.com/pimgs/m/214-2142655_transparent-inspirational-quotes-png-calligraphy-png-download.png\"}";
        using UnityWebRequest request = new UnityWebRequest(uri,"POST");
        //request header
        request.SetRequestHeader("Content-Type","application/json");
        request.SetRequestHeader("Ocp-Apim-Subscription-Key",apim_key); 
        //request body
        byte[] bytearray = Encoding.UTF8.GetBytes(rawData);
        request.uploadHandler=new UploadHandlerRaw(bytearray);
        request.downloadHandler=new DownloadHandlerBuffer();
        yield return request.SendWebRequest();
        string post_response=request.GetResponseHeader("Operation-Location");
        StartCoroutine(GetRead(post_response));
    }
    IEnumerator GetRead(string post_response)
    {
        int status=0;
        //request url 
        var uri=post_response;
        string get_response="";
         do
         {
            using UnityWebRequest request = new UnityWebRequest(uri,"GET");
            //request header
            request.SetRequestHeader("Ocp-Apim-Subscription-Key",apim_key);
            request.downloadHandler=new DownloadHandlerBuffer(); 
            yield return request.SendWebRequest();
            if(request.result==UnityWebRequest.Result.Success)
            {
               get_response=request.downloadHandler.text;
               status=getStatus(get_response);
            }
         } 
          while (status!=1);
         outputArea.text=get_response;
    }
    int getStatus(string res)
    {
       string[] x = res.Split(",");
       string[] y=x[0].Split("\"");
       int status=setStatus(y[3]);
       return status;
    }
    int setStatus(string str)
    {
       if(str=="succeeded")
          return 1;
       else if (str=="running")
          return -1;
       else return 0;
    }
}