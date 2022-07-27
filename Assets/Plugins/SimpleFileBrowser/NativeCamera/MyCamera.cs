using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Net.Mime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Text;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Linq;
using Object = UnityEngine.Object;
#if UNITY_ANDROID || UNITY_IOS
using NativeCameraNamespace;
#endif
public class MyCamera : MonoBehaviour
{ 
    TMP_InputField outputArea;
      string endpoint = "https://centralindia.api.cognitive.microsoft.com/";
    string apim_key = "4bcb8912069b477d9ae36d4ed756662f";
    string filepath = "";
    string contentType = "";
    string filename = "";
    void Start()
    {
        outputArea = GameObject.Find("OutputArea").GetComponent<TMP_InputField>();
        GameObject.Find("CameraButton").GetComponent<Button>().onClick.AddListener(CaptureData);
    }
  
    void CaptureData()
    {
        outputArea.text = "Loading..";
        if( NativeCamera.IsCameraBusy() )
			return;
        else
          TakePicture( 512 );
    }
     void TakePicture( int maxSize )
     {
	     NativeCamera.Permission permission = NativeCamera.TakePicture( ( path ) =>
	     {
	     	 if( path != null )
             {
                filename = string.Format(@"CapturedImage{0}_n.jpg", Time.time);
		        filepath = System.IO.Path.Combine(Application.persistentDataPath, filename);
                File.Move(path, filepath);
             }
         }, maxSize );
         setParameters();
     }
     void setParameters()
     {
        byte[] bytearray = File.ReadAllBytes(filepath);
        contentType = "application/octet-stream";
        StartCoroutine(PostRead(bytearray, contentType));
     }
     IEnumerator PostRead(byte[] bytearray, string contentType)
    {
        var uri = endpoint + "vision/v3.2/read/analyze?readingOrder=natural";
        using UnityWebRequest request = new UnityWebRequest(uri, "POST");
        //request header
        request.SetRequestHeader("Content-Type", contentType);
        request.SetRequestHeader("Ocp-Apim-Subscription-Key", apim_key);
        request.uploadHandler = new UploadHandlerRaw(bytearray);
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();
        string post_response = request.GetResponseHeader("Operation-Location");
        StartCoroutine(GetRead(post_response));
    }
    IEnumerator GetRead(string post_response)
    {
        int status = 0;
        //outputArea.text=post_response;
        var uri = post_response;
        string get_response = "";
        do
        {
            using UnityWebRequest request = new UnityWebRequest(uri, "GET");
            //request header
            request.SetRequestHeader("Ocp-Apim-Subscription-Key", apim_key);
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                get_response = request.downloadHandler.text;
                status = getStatus(get_response);
            }
            else
            {
                outputArea.text=request.downloadHandler.text;
                status=1;
            }
        }
        while (status != 1);
        //outputArea.text = get_response;
        parseResponse(get_response);
    }
    int getStatus(string res)
    {
        string[] x = res.Split(",");
        string[] y = x[0].Split("\"");
        int status = setStatus(y[3]);
        return status;
    }
    int setStatus(string str)
    {
        if (str == "succeeded")
            return 1;
        else if (str == "running")
            return -1;
        else return 0;
    }
    void parseResponse(string str)
    {
        JObject jsonstr = JObject.Parse(str);
        JToken json_lines = jsonstr["analyzeResult"]["readResults"][0]["lines"];
        int noOfLines = FindLines(json_lines);
        int[] noOfWords = FindWords(json_lines, noOfLines);
        int totalWords;
        int count = 0;
        foreach (int i in noOfWords)
        {
            count = count + i;
        }
        totalWords = count;
        double[,] bb = Findbb(json_lines, totalWords);
        string[] text = FindText(json_lines, totalWords);
        double[] confidence = FindConfidence(json_lines, totalWords);
        double[] height = FindHeight(bb, totalWords);
        int max = MaxHeight(bb, totalWords);
        //double[] bb_max=bb[max];
        string text_max = text[max];
        double confidence_max = confidence[max];
        string str1 = "";
        int k = 0;
        foreach (string i in text)
        {
            str1 = str1 + (k + 1) + ". " + text[k] + "\tconfidence=" + confidence[k] + "\tHeight=" + height[k] + "\n";
            k++;
        }
        str1 = str1 + "\nMax Height text = " + text_max + "\nMax Height confidence = " + confidence_max + "\n";
        outputArea.text = str1;
    }
    int FindLines(JToken str)
    {
        int i = 0;
        foreach (var line in str)
        {
            i++;
        }
        return i;
    }
    int[] FindWords(JToken str, int noOfLines)
    {
        int[] noOfWords = new int[noOfLines];
        int i = 0;
        foreach (var line in str)
        {
            int j = 0;
            JToken words = line["words"];
            foreach (var word in words)
            {
                j++;
            }
            noOfWords[i] = j;
            i++;
        }
        return noOfWords;
    }
    string[] FindText(JToken str, int totalWords)
    {
        string[] text = new string[totalWords];
        int i = 0;
        foreach (var line in str)
        {
            JToken words = line["words"];
            foreach (var word in words)
            {
                text[i] = word["text"] + "";
                i++;
            }
        }
        return text;
    }
    double[] FindConfidence(JToken str, int totalWords)
    {
        double[] confidence = new double[totalWords];
        int i = 0;
        foreach (var line in str)
        {
            JToken words = line["words"];
            foreach (var word in words)
            {
                confidence[i] = ConvertToDouble(word["confidence"] + "");
                i++;
            }
        }
        return confidence;
    }
    double[,] Findbb(JToken str, int totalWords)
    {
        double[,] bb = new double[totalWords, 8];
        JToken[] bbstr = new JToken[totalWords];
        int i = 0, j = 0;
        foreach (var line in str)
        {
            JToken words = line["words"];
            foreach (var word in words)
            {
                bbstr[i] = word["boundingBox"];
                i++;
            }
        }
        i = 0;
        foreach (JToken item in bbstr)
        {
            j = 0;
            while (j < 8)
            {
                bb[i, j] = ConvertToDouble(bbstr[i][j] + "");
                j++;
            }
            i++;
        }
        return bb;
    }
    int MaxHeight(double[,] bb, int totalWords)
    {
        int max = 0, count = 0;
        double max_height = 0;
        while (count < totalWords)
        {
            double left_height = bb[count, 7] - bb[count, 1];
            double right_height = bb[count, 5] - bb[count, 3];
            double height = (left_height + right_height) / 2;
            max_height = Math.Max(max_height, height);
            if (max_height == height)
                max = count;
            count++;
        }
        return max;
    }
    double[] FindHeight(double[,] bb, int totalWords)
    {
        double[] height = new double[totalWords];
        int count = 0;
        while (count < totalWords)
        {
            double left_height = bb[count, 7] - bb[count, 1];
            double right_height = bb[count, 5] - bb[count, 3];
            height[count] = (left_height + right_height) / 2;
            count++;
        }
        return height;
    }
    double ConvertToDouble(string s)
    {
        double doubleVal;
        if (Double.TryParse(s, out doubleVal))
            return doubleVal;
        else return 0;
    }

}