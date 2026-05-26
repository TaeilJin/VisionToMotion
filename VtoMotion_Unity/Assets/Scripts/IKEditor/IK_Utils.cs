using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using UnityEditor;

public class IK_Utils : ScriptableObject
{
    public MBS IK_Target;

	public bool b_connect_init_MBS;
	public bool b_connect_set_Points;
	public bool b_connect_set_Dirs;
	public bool b_connect_set_Pose;
	public bool b_connect_IK;
	public bool b_connect;

	public Vector3[] DesPoints;
	public Vector3[] DesDirs;

	public float[] PointWeights;
	public float[] DirWeights;
	
	public float[] PointDes;
	public float[] DirDes;

   [Serializable]
	public class DataPacket
	{
		public string text_indicator;
		public string text_mbs_txt;
		public string jointname;
		public float[] floatArr_WeightsP;
		public float[] floatArr_WeightsD;
		public float[] floatArr_Position;
		public float[] floatArr_Direction;
		public float[] floatArr_DesP;
		public float[] floatArr_DesD;
	}

	public string InitMBS()
	{
		// 데이터 구성
		DataPacket dataToSend = new DataPacket
		{
			text_indicator = "initMBS",
			text_mbs_txt = IK_Target.MBS_FullName,
			jointname = "jointname",
			floatArr_Position = null,
			floatArr_Direction = null,
			floatArr_DesP = null,
			floatArr_DesD = null,
			floatArr_WeightsP = null,
			floatArr_WeightsD = null
		};
		// 데이터를 JSON 문자열로 직렬화하여 전송
		string jsonData = JsonUtility.ToJson(dataToSend);

		return jsonData;
	}
	public string SetDesiredPositionArr()
	{
		float[] floatArray = new float[DesPoints.Length*3];
		
		// Desired Array 
		for (int i = 0; i < DesPoints.Length; i++)
		{
			floatArray[3*i + 0] = DesPoints[i].x; floatArray[3 * i + 1] = DesPoints[i].y; floatArray[3 * i + 2] = DesPoints[i].z;
		}
		
		// 데이터 구성
		DataPacket dataToSend = new DataPacket
		{
			text_indicator = "SetDesPositons",
			text_mbs_txt = null,
			jointname = null,
			floatArr_Position = floatArray,
			floatArr_Direction = null,
			floatArr_DesP = PointDes,
			floatArr_DesD = null,
			floatArr_WeightsP = PointWeights,
			floatArr_WeightsD = null
		};
		// 데이터를 JSON 문자열로 직렬화하여 전송
		string jsonData = JsonUtility.ToJson(dataToSend);

		return jsonData;
	}

	public string SetDesiredDirectionArr()
	{
		float[] floatArray = new float[DesDirs.Length * 3];

		// Desired Array 
		for (int i = 0; i < DesDirs.Length; i++)
		{
			floatArray[3 * i + 0] = DesDirs[i].x; floatArray[3 * i + 1] = DesDirs[i].y; floatArray[3 * i + 2] = DesDirs[i].z;
		}

		// 데이터 구성
		DataPacket dataToSend = new DataPacket
		{
			text_indicator = "SetDesDirections",
			text_mbs_txt = null,
			jointname = null,
			floatArr_Position = null,
			floatArr_Direction = floatArray,
			floatArr_DesP = null,
			floatArr_DesD = DirDes,
			floatArr_WeightsP = null,
			floatArr_WeightsD = DirWeights
		};
		// 데이터를 JSON 문자열로 직렬화하여 전송
		string jsonData = JsonUtility.ToJson(dataToSend);

		return jsonData;
	}

	public string SetDesiredPositionDirectionArr()
	{
		float[] floatArrayDir = new float[DesDirs.Length * 3];
		float[] floatArrayPoint = new float[DesPoints.Length * 3];

		//Debug.Log(" DesPoints " + DesPoints.Length + " DesDirs " + DesDirs.Length);
		// Desired Array 
		for (int i = 0; i < DesDirs.Length; i++)
		{
			floatArrayDir[3 * i + 0] = DesDirs[i].x; floatArrayDir[3 * i + 1] = DesDirs[i].y; floatArrayDir[3 * i + 2] = DesDirs[i].z;
		}
		for (int i = 0; i < DesPoints.Length; i++)
		{
			floatArrayPoint[3 * i + 0] = DesPoints[i].x; floatArrayPoint[3 * i + 1] = DesPoints[i].y; floatArrayPoint[3 * i + 2] = DesPoints[i].z;
		}

		// 데이터 구성
		DataPacket dataToSend = new DataPacket
		{
			text_indicator = "SetDesiredPose",
			text_mbs_txt = null,
			jointname = null,
			floatArr_Position = floatArrayPoint,
			floatArr_Direction = floatArrayDir,
			floatArr_DesP = PointDes,
			floatArr_DesD = DirDes,
			floatArr_WeightsP = PointWeights,
			floatArr_WeightsD = DirWeights
		};
		// 데이터를 JSON 문자열로 직렬화하여 전송
		string jsonData = JsonUtility.ToJson(dataToSend);

		return jsonData;
	}

	public string SetPoints(string jointname, Vector3 Points)
	{
		float[] floatArray = new float[3];
		floatArray[0] = Points.x; floatArray[1] = Points.y; floatArray[2] = Points.z;

		// 데이터 구성
		DataPacket dataToSend = new DataPacket
		{
			text_indicator = "SetPoints",
			text_mbs_txt = null,
			jointname = jointname,
			floatArr_Position = floatArray
		};
		// 데이터를 JSON 문자열로 직렬화하여 전송
		string jsonData = JsonUtility.ToJson(dataToSend);

		return jsonData;
	}
	
	public string doIK()
    {
		// 데이터 구성
		DataPacket dataToSend = new DataPacket
		{
			text_indicator = "doIK"			
		};
		// 데이터를 JSON 문자열로 직렬화하여 전송
		string jsonData = JsonUtility.ToJson(dataToSend);

		return jsonData;
	}

	
	public void inspector(Actor source)
    {
		// MBS Inspector
		if(source != null)
        {
			IK_Target.inspector(source);
			DesPoints = new Vector3[IK_Target.actor.Bones.Length];
			DesDirs = new Vector3[IK_Target.actor.Bones.Length - 1];
			
			PointWeights = new float[IK_Target.actor.Bones.Length];
			DirWeights = new float[IK_Target.actor.Bones.Length - 1];
			
			PointDes = new float[IK_Target.actor.Bones.Length];
			DirDes = new float[IK_Target.actor.Bones.Length - 1];
		}

		// bool connection
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
		b_connect = EditorGUILayout.Toggle("b_connect", b_connect);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		if (b_connect)
		{
			if (Utility.GUIButton("IK: Init IK ", Color.white, Color.green))
			{
				b_connect_init_MBS = true;
			}

			if (Utility.GUIButton("IK: Set Positions ", Color.white, Color.green))
			{
				b_connect_set_Points = true;
			}
			if (Utility.GUIButton("IK: Set Pose ", Color.white, Color.green))
			{
				b_connect_set_Pose = true;
			}
			if (Utility.GUIButton("IK: Do IK ", Color.white, Color.green))
			{
				b_connect_IK = true;
			}
		}
	}
}
