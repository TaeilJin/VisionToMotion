using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class TCPClient : ScriptableObject
{
    private TcpClient client;
    private NetworkStream stream;
    //public string serverAddress;// = "서버 IP 주소"; // Python 서버의 IP 주소
    //public int serverPort;// = 포트 번호; // Python 서버의 포트 번호
    [Serializable]
    public class DataPacket
    {
        public string text_indicator;
        public string text_mbs_src_txt;
        public string text_mbs_tar_txt;
        public string text_jointmapping_txt;
        public float[] floatArray;
    }

    public float[] receivedFloatArray;
    public void Setup(string serverAddress, int serverPort)
    {
        try
        {
            client = new TcpClient(serverAddress, serverPort);
            stream = client.GetStream();
            Debug.Log("연결 성공");

            receivedFloatArray = new float[22 * 4 + 3];
            // 클라이언트 초기화 후 서버로 메시지 전송
            //SendToServer("apple")

        }
        catch (Exception e)
        {
            Debug.LogError("연결 실패: " + e.Message);
        }
    }

    public void SendData(string data)
    {
        byte[] dataBytes = Encoding.ASCII.GetBytes(data);
        stream.Write(dataBytes, 0, dataBytes.Length);
    }

    public void ReceiveData(int dof)
    {
        // 데이터 수신 및 처리
        // 서버로부터 데이터 수신
        byte[] receivedData = new byte[(dof) * sizeof(float)];
        int bytesRead = stream.Read(receivedData, 0, receivedData.Length);
        if (bytesRead > 0)
        {
            //Debug.Log(bytesRead);
            receivedFloatArray = new float[bytesRead / sizeof(float)];
            Buffer.BlockCopy(receivedData, 0, receivedFloatArray, 0, bytesRead);
            //Debug.Log("Received float array from Python:" + receivedFloatArray.Length);
            //foreach (float value in receivedFloatArray)
            //{
            //    Debug.Log(value);
            //}
        }
    }

    private void SendToServer(string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        stream.Write(data, 0, data.Length);

        // 서버에서 처리된 데이터 수신
        byte[] responseBuffer = new byte[1024];
        int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
        string response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);
        Debug.Log("서버에서의 응답: " + response);
    }

    public void OnDestroy()
    {
        if (client != null)
        {
            stream.Close();
            client.Close();
        }
    }
}
