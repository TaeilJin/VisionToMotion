using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using UnityEditor;

public enum RETARGETING_MODE
{
	ALL,
	REALTIME_RETARGETING,
	DATASET_RETARGETING,
	TESTING
}

public class MW_RETARGET_Utils : ScriptableObject
{
	
	public RETARGETING_MODE selectedMode;

	public StringBuilder sb_record = new StringBuilder();
	public WriterClass wr_class = new WriterClass();
	public ImporterClass io_class = new ImporterClass();
	//public static string[][] Joint_Pair_Idx = new string[1][];
	public string jointPairPath;

	public bool b_connect = false;
	public bool b_connect_init_MBS = false;
	public bool b_connect_init_RETARGET = false;
	public bool b_connect_do_retargeting = false;
	public Transform base_offset;
	
	public MBS RetargetingSource;
	public MBS RetargetingTarget;


	//// 

	[Serializable]
	public class DataPacket
	{
		public string text_indicator;
		public string text_mbs_src_txt;
		public string text_mbs_tar_txt;
		public string text_jointmapping_txt;

		public float[] floatArray;
		public float[] base_offset;
	}

	public string InitMBS()
    {
		// 데이터 구성
		DataPacket dataToSend = new DataPacket
		{
			text_indicator = "initMBS",
			text_mbs_src_txt = RetargetingSource.MBS_FullName,
			text_mbs_tar_txt = RetargetingTarget.MBS_FullName,
			text_jointmapping_txt = jointPairPath,
			floatArray = null,
			base_offset = null
		};
		// 데이터를 JSON 문자열로 직렬화하여 전송
		string jsonData = JsonUtility.ToJson(dataToSend);

		return jsonData;
    }

    public string DoRetargeting(Transform base_offset)
    {

		float[] floatArray = new float[RetargetingSource.actor.Bones.Length * 4 + 3];
		for (int j = 0; j < RetargetingSource.actor.Bones.Length; j++)
		{

			if (j == 0)
			{
				Vector3 position = RetargetingSource.actor.Bones[j].Transform.position; // 0 번 joint 일 때는 world position 을 구한다.

				floatArray[0] = position.x;
				floatArray[1] = position.y;
				floatArray[2] = position.z;

				Quaternion rot = RetargetingSource.actor.Bones[j].Transform.rotation; // 0 번 joint 일 때는 world rotation 을 준다.
				floatArray[3 + 4 * j + 0] = rot.x;
				floatArray[3 + 4 * j + 1] = rot.y;
				floatArray[3 + 4 * j + 2] = rot.z;
				floatArray[3 + 4 * j + 3] = rot.w;

			}
			else
			{
				Quaternion rot = RetargetingSource.jointdelta[j].GetRotation();
				floatArray[3 + 4 * j + 0] = rot.x;
				floatArray[3 + 4 * j + 1] = rot.y;
				floatArray[3 + 4 * j + 2] = rot.z;
				floatArray[3 + 4 * j + 3] = rot.w;
			}
		}

		float[] offset_position = new float[3];
		offset_position[0] = base_offset.transform.position.x;
		offset_position[1] = base_offset.transform.position.y;
		offset_position[2] = base_offset.transform.position.z;
		// 데이터 구성
		DataPacket dataToSend = new DataPacket
		{
			text_indicator = "doRetarget",
			text_mbs_src_txt = null,
			text_mbs_tar_txt = null,
			text_jointmapping_txt = null,
			floatArray = floatArray,
			base_offset = offset_position
		};
		// 데이터를 JSON 문자열로 직렬화하여 전송
		string jsonData = JsonUtility.ToJson(dataToSend);

		return jsonData;
    }

    public void UpdateFromJoinDelta(float[] array)
    {
		//Debug.Log(" joint " + RetargetingTarget.actor.Bones.Length + "dof " + array.Length);
		for (int j = 0; j < RetargetingTarget.actor.Bones.Length; j++)
        {
			if (j == 0)
			{
				RetargetingTarget.actor.Bones[0].Transform.position = new Vector3(array[0], array[1], array[2]);
				RetargetingTarget.actor.Bones[0].Transform.rotation = new Quaternion(array[3], array[4], array[5],array[6]);
			}
			else
			{
				//Debug.Log(" joint name " + RetargetingTarget.actor.Bones[j].GetName());
				float x = array[3 + 4 * j + 0];
				float y = array[3 + 4 * j + 1];
				float z = array[3 + 4 * j + 2];
				float w = array[3 + 4 * j + 3];
				Quaternion quat_lH = new Quaternion(x, y, z, w); // delta
				quat_lH = quat_lH.normalized;

				RetargetingTarget.actor.Bones[j].Transform.localRotation = RetargetingTarget.Default_local_mat[j].GetRotation() * quat_lH; 
			}
        }
    }
	public void inspector(Actor source, Actor target, Transform offset)
    {
		base_offset = offset;

		//Setting source and retarget
		EditorGUILayout.BeginHorizontal();
		if (Utility.GUIButton("Retargeting: generate MBS txt & Joint Pair", Color.white, Color.yellow))
		{
			RetargetingSource = new MBS();
			RetargetingSource.SetCharacter(source);
			RetargetingSource.SetAsSource();

			//RetargetingSource.Default_local_mat = new Matrix4x4[RetargetingSource.actor.Bones.Length];
			RetargetingSource.CalcLocalFrames(out RetargetingSource.Default_local_mat);

			RetargetingTarget = new MBS();
			RetargetingTarget.SetCharacter(target);
			RetargetingTarget.SetAsTarget();

			//RetargetingTarget.Default_local_mat = new Matrix4x4[RetargetingTarget.actor.Bones.Length];
			RetargetingTarget.CalcLocalFrames(out RetargetingTarget.Default_local_mat);

			EditorApplication.delayCall += () =>
			{
				string dataPath = EditorUtility.OpenFolderPanel("BVH Folder", "", "Assets");
				RetargetingSource.genMBSTxtFile(dataPath, RetargetingSource.actor.name, RetargetingSource.actor, 1.0f);
				RetargetingTarget.genMBSTxtFile(dataPath, RetargetingTarget.actor.name, RetargetingTarget.actor, 1.0f);
				Debug.Log("saved : " + RetargetingSource.MBS_FullName);
				Debug.Log("saved : " + RetargetingTarget.MBS_FullName);

			};

			EditorApplication.delayCall += () =>
			{

				jointPairPath = EditorUtility.OpenFilePanel("Overwrite with txt", "", "txt");
				if (!File.Exists(jointPairPath))
				{
					UnityEngine.Debug.Log("File Path(" + jointPairPath + ") Not Exists.");
				}

				//load pairing data
				string[][] Joint_Pair_Idx = new string[1][];
				io_class.ImportStringArrayData(jointPairPath, 3, out Joint_Pair_Idx);
				for (int j = 0; j < Joint_Pair_Idx.Length; j++)
					UnityEngine.Debug.Log("|Pair| |SRC|: " + Joint_Pair_Idx[j][1] + " |TAR|: " + Joint_Pair_Idx[j][2]);
				int num_TargetJoints = Joint_Pair_Idx.Length;
			};

			

		}
		EditorGUILayout.EndHorizontal();
		
		// Play Motion Data
		if (!Application.isPlaying) return;


		// bool connection
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
		b_connect = EditorGUILayout.Toggle("b_connect", b_connect);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		if (Utility.GUIButton("Retargeting: Init MBS ", Color.white, Color.yellow))
		{
			b_connect_init_MBS = true;
		}

		if (selectedMode == RETARGETING_MODE.REALTIME_RETARGETING)
		{
			if (Utility.GUIButton("Retargeting: Do Retargeting ", Color.white, Color.yellow))
			{
				b_connect_do_retargeting = true;
			}

			// bool connection
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
			b_connect_do_retargeting = EditorGUILayout.Toggle("b_retargeting", b_connect_do_retargeting);
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

		}


	}	

}
