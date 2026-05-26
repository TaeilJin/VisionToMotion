using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using UnityEditor;

public class MBS : ScriptableObject
{
	public WriterClass _writer_class = new WriterClass();
	public StringBuilder sb_record = new StringBuilder();
	public StreamWriter File_record;

	public Actor actor = null;
	public bool b_is_source = false;
	public Matrix4x4[] Default_local_mat;
	public Quaternion[] Default_world_mat;
	public Matrix4x4[] jointdelta;
	public string MBS_FullName;


	// set Actor Class
	public void SetCharacter(Actor _actor)
	{
		//actor = new Actor();

		actor = _actor;
	}
	public bool SetAsTarget()
	{
		b_is_source = false;
		return b_is_source;
	}
	public bool SetAsSource()
	{
		b_is_source = true;
		return b_is_source;
	}

	// calculate local rotation and position using world matrix
	public void CalcLocalFrames(out Matrix4x4[] localmat)
	{
		localmat = new Matrix4x4[actor.Bones.Length]; // local matrix  -> MBS 만들때랑, 조인트 값 업데이트 할때 사용한다.
													  //Default_world_mat = new Quaternion[actor.Bones.Length]; // world quaternion -> joint delta 구할 때 사용
													  //jointdelta = new Quaternion[actor.Bones.Length]; // joint delta

		for (int j = 0; j < actor.Bones.Length; j++)
		{
			//Default_world_mat[j] = actor.Bones[j].Transform.localToWorldMatrix.GetRotation();
			for (int i = 0; i < actor.Bones[j].Childs.Length; i++)
			{
				int child_index = actor.Bones[j].GetChild(i).Index;


				Matrix4x4 local = actor.Bones[j].Transform.localToWorldMatrix.inverse * actor.Bones[child_index].Transform.localToWorldMatrix;
				localmat[child_index].SetTRS(local.GetPosition(), local.GetRotation(), Vector3.one);


				//Debug.Log("Parent : " + actor.Bones[j].GetName() + " Child: " + actor.Bones[child_index].GetName() 
				//	+ " position " + localmat[child_index].GetPosition() + " rotation " + localmat[child_index].GetRotation());

			}
		}
	}
	// calculate local joint delta from default T-pose frame 
	public void CalcJoinDelta()
	{
		for (int j = 0; j < actor.Bones.Length; j++)
		{
			jointdelta[j] = Default_local_mat[j].inverse * jointdelta[j];
			//Debug.Log("Joint Delta : " + actor.Bones[j].GetName()
			//		+ " position " + jointdelta[j].GetPosition() + " rotation " + jointdelta[j].GetRotation());
		}
	}
	// calculate rotation from right-handed oriented to left-handed oriented rotation 
	public Quaternion SET_JOINT_QUATERNION_MW(Quaternion input_Quat)
	{
		Quaternion quat_rH = new Quaternion(input_Quat.x, input_Quat.y * -1.0f, input_Quat.z * -1.0f, input_Quat.w);

		return quat_rH;
	}
	// generate MBS text file using Actor
	public void genMBSTxtFile(string foldername, string i_fileName, Actor i_actor, float scale)
	{

		MBS_FullName = foldername + "/" + i_fileName + ".txt";
		File_record = _writer_class.CreateFile(foldername, i_fileName, false, ".txt");
		sb_record = new StringBuilder();

		File_record.WriteLine("HIERARCHY\n");

		//sb_record = _writer_class.WritePosition(sb_record, i_actor.Bones[0].Transform.position, true);
		Vector3 pos;
		Quaternion quat;

		for (int i = 0; i < i_actor.Bones.Length; i++)
		{
			if (!i_actor.Bones[i].GetName().Contains("Site"))
			{
				if (i != 0)
				{
					pos = Default_local_mat[i].GetPosition() * scale;
					pos.Set(-1 * pos.x, pos.y, pos.z); // to MotionWorks (righthand)

					quat = Default_local_mat[i].GetRotation();
					quat = SET_JOINT_QUATERNION_MW(quat);

				}
				else
				{
					pos = i_actor.Bones[i].Transform.position * scale;
					pos.Set(-1 * pos.x, pos.y, pos.z); // to MotionWorks (righthand)

					quat = i_actor.Bones[i].Transform.rotation;
					quat = SET_JOINT_QUATERNION_MW(quat);
				}


				File_record.WriteLine("LINK");
				File_record.WriteLine("NAME " + i_actor.Bones[i].GetName());

				// Warning: Assume that the root bone index is zero as like getRootPose
				string Joint_Type = "JOINT ACC BALL";
				if (i == 0)
				{
					File_record.WriteLine("REF WORLD");
					Joint_Type = "JOINT ACC FREE";
				}
				else
				{
					//Debug.Log(i + " " + i_actor.Bones[i].GetName());
					File_record.WriteLine("PARENT " + i_actor.Bones[i].GetParent().GetName());
					File_record.WriteLine("REF LOCAL");
				}

				sb_record = _writer_class.WritePosition(sb_record, pos, false);
				File_record.WriteLine("POS " + sb_record.ToString());
				sb_record.Clear();

				sb_record = _writer_class.WriteQuat(sb_record, quat, false);
				File_record.WriteLine("ROT QUAT " + sb_record.ToString());
				sb_record.Clear();

				File_record.WriteLine(Joint_Type);

				File_record.WriteLine("END_LINK\n");

			}
			else
				Debug.LogError("You should erase 'SITE' from the bones !! ");
		}
		File_record.WriteLine("END_HIERARCHY\n");

		File_record.Close();
		sb_record.Clear();


	}

	// do post-processing for base orientation
	public void DoPostProcessing_BaseModifying(Matrix4x4 control_mat)
	{
		Matrix4x4 bone_hip = actor.Bones[0].Transform.GetWorldMatrix();
		bone_hip = bone_hip.GetRelativeTransformationFrom(control_mat);
		actor.Bones[0].Transform.SetPositionAndRotation(bone_hip.GetPosition(), bone_hip.GetRotation());
	}

	public void UpdateFromJoinDelta(float[] array)
	{
		//Debug.Log(" joint " + RetargetingTarget.actor.Bones.Length + "dof " + array.Length);
		for (int j = 0; j < actor.Bones.Length; j++)
		{
			if (j == 0)
			{
				actor.Bones[0].Transform.position = new Vector3(array[0], array[1], array[2]);
				actor.Bones[0].Transform.rotation = new Quaternion(array[3], array[4], array[5], array[6]);
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

				actor.Bones[j].Transform.localRotation = Default_local_mat[j].GetRotation() * quat_lH;
			}
		}
	}

	public void inspector(Actor source)
    {
		SetCharacter(source);
		SetAsSource();
		
		//Setting source and retarget
		EditorGUILayout.BeginHorizontal();
		if (Utility.GUIButton("MBS: generate MBS txt", Color.white , new Color(255,235,0,1)))
		{

			CalcLocalFrames(out Default_local_mat);
			EditorApplication.delayCall += () =>
			{
				string dataPath = EditorUtility.OpenFolderPanel("Folder", "", "Assets");
				genMBSTxtFile(dataPath, actor.name, actor, 1.0f);
				Debug.Log("saved : " + MBS_FullName);

			};
		}
		EditorGUILayout.EndHorizontal();
	}

}
