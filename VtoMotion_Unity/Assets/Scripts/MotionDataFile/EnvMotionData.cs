#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class EnvMotionData : ScriptableObject
{

	public int Index; // file load order
	public int Order; // file specified order
	public string FileName;
	public string DirectoryName;

	public Sequence[] Sequences = new Sequence[0];
	public Matrix4x4[][] Motion = new Matrix4x4[0][];
	public Matrix4x4[][] MotionWR = new Matrix4x4[0][];

	public Matrix4x4[] RootTr = new Matrix4x4[0];
	public Matrix4x4 StartRootMat = new Matrix4x4();
	public Matrix4x4 EndRootMat = new Matrix4x4();
	public Matrix4x4[] seenByChild;

	public Matrix4x4 Desk_mat;
	public Matrix4x4 Chair_mat;
	public GameObject Desk;
	public GameObject Chair;
	public int b_uppercond;
	//public CylinderMap Environment;
	//public float[][] Occupancies;
	public float y_offset_h = 0;
	public float y_offset_f = 0;
	public bool b_same_furniture = false;

	public float Framerate = 30;
	public float Scale = 1f;

	public bool Export = true;

	private int RightShoulder = 10;
	private int LeftShoulder = 6;
	private int RightHip = 14;
	private int LeftHip = 18;
	public Color gui_color;
	public void InspectAll(bool value)
	{

	}

	public void VisualiseAll(bool value)
	{

	}

	public string GetName()
	{
		return name;
	}

	public float GetTotalTime()
	{
		return Motion.GetLength(0) / Framerate;
	}

	public int GetTotalFrames()
	{
		return Motion.GetLength(0);
	}

	//-- update joint pos
	public Matrix4x4[] GetFramePose(int index)
	{
		//Matrix4x4[] joints = new Matrix4x4[Motion.GetLength(1)];
		//for (int j = 0; j < Motion.GetLength(1); j++)
		//	joints[j] = Motion[index,j];
		return Motion[index];
	}
	public Matrix4x4[] GetFramePose(float time)
	{
		int index = Mathf.Clamp(Mathf.RoundToInt(time * Framerate), 0, Motion.GetLength(0) - 1);
		Matrix4x4[] joints = GetFramePose(index);
		return joints;
	}



	//-- sequence
	public void AddSequence()
	{
		ArrayExtensions.Add(ref Sequences, new Sequence(0, GetTotalFrames() - 1));
	}

	public void RemoveSequence(Sequence sequence)
	{
		ArrayExtensions.Remove(ref Sequences, sequence);
	}


	public Sequence GetUnrolledSequence()
	{
		if (Sequences.Length == 0)
		{
			return new Sequence(1, Motion.GetLength(0));
		}
		if (Sequences.Length == 1)
		{
			return Sequences[0];
		}
		int start = Motion.GetLength(0);
		int end = 1;
		foreach (Sequence seq in Sequences)
		{
			start = Mathf.Min(seq.Start, start);
			end = Mathf.Max(seq.End, end);
		}
		return new Sequence(start, end);
	}

	//-- Root Trajectory
	public Matrix4x4 GetRootTransformation_JointTransformation(int index, float y_offset)
	{
		//vector_x
		Vector3 vec_shoulder = Motion[index][LeftShoulder].GetPosition() - Motion[index][RightShoulder].GetPosition();
		vec_shoulder = vec_shoulder.normalized;
		Vector3 vec_upleg = Motion[index][LeftHip].GetPosition() - Motion[index][RightHip].GetPosition();
		vec_upleg = vec_upleg.normalized;
		Vector3 vec_across = vec_shoulder + vec_upleg;
		vec_across = vec_across.normalized;
		//vector_forward
		Vector3 vec_forward = Vector3.Cross(-1.0f * vec_across, Vector3.up);
		//vector_x_new
		Vector3 vec_right = Vector3.Cross(-1.0f * vec_forward, Vector3.up);
		//root matrix 
		Matrix4x4 root_interaction = Matrix4x4.identity;
		Vector4 vec_x = new Vector4(vec_right.x, vec_right.y, vec_right.z, 0.0f);
		Vector4 vec_z = new Vector4(vec_forward.x, vec_forward.y, vec_forward.z, 0.0f);
		Vector4 vec_y = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
		Vector3 pos__ = Motion[index][0].GetPosition();
		Vector4 pos_h = new Vector4(pos__.x, y_offset, pos__.z, 1.0f);
		root_interaction.SetColumn(0, vec_x); root_interaction.SetColumn(1, vec_y); root_interaction.SetColumn(2, vec_z);
		root_interaction.SetColumn(3, pos_h);
		//
		return root_interaction;
	}

	public void GenerateRootTrajectory(int start, int end)
	{
		RootTr = new Matrix4x4[end - start + 1];
		MotionWR = new Matrix4x4[end - start + 1][];
		for (int n = start, k = 0; n <= end; n++, k++)
		{
			RootTr[k] = GetRootTransformation_JointTransformation(n, 0.0f);
			MotionWR[k] = new Matrix4x4[22];
			for (int j = 0; j < 22; j++)
				MotionWR[k][j] = Motion[n][j].GetRelativeTransformationTo(RootTr[k]);
		}
		GenerateRootRelative();

		//RootVelTr = new Vector3[end - start];
		//for (int t = 0; t < RootVelTr.Length; t++)
		//	RootVelTr[t] = GetRootVelocity(RootTr[t], RootTr[t + 1]);
	}
	public void GenerateRootRelative()
	{
		seenByChild = new Matrix4x4[RootTr.Length - 1];
		for (int n = 0; n < seenByChild.Length; n++)
		{
			seenByChild[n] = RootTr[n + 1].GetRelativeTransformationTo(RootTr[n]);

			//Vector3 vel = RootTr[n + 1].GetPosition() - RootTr[n].GetPosition();
			//RootVelTr[k] = vel.GetRelativeDirectionTo(RootTr[n]);

			//Vector3 forward = RootTr[n + 1].GetForward();
			//RootVelRot[k] = forward.GetRelativeDirectionTo(RootTr[n]);
		}
	}
	public void GenerateRootTrajectory(Matrix4x4 startMat)
	{
		RootTr[0] = startMat;
		//Debug.Log("RootTR " + RootTr.Length + " Root Vel " + RootVelTr.Length);
		for (int n = 0; n < seenByChild.Length; n++)
		{
			RootTr[n + 1] = seenByChild[n].GetRelativeTransformationFrom(RootTr[n]);

		}

	}

	//-- Environment Object
	public void SetGameObject(GameObject go, bool ischair)
	{
		if (go != null)
		{
			if (ischair)
				Chair = go;
			else
				Desk = go;
		}
		else
		{
			if (ischair)
				Chair = null;
			else
				Desk = null;
		}
	}

	//-- Extract Goal
	public Vector3 GetRootVelocity(Matrix4x4 pre_root, Matrix4x4 current_root)
	{

		Vector3 root_vel = new Vector3();

		// rotation
		Vector3 cur_forward = pre_root.GetForward().normalized;
		Vector3 next_forward = current_root.GetForward().normalized;

		float sin = cur_forward.x * next_forward.z - next_forward.x * cur_forward.z;
		float rad_1 = Mathf.Asin(sin);

		// linear
		root_vel.z = rad_1;
		Vector3 linear_root_vel = current_root.GetPosition() - pre_root.GetPosition();
		linear_root_vel = linear_root_vel.GetRelativeDirectionTo(pre_root);
		root_vel.x = linear_root_vel.x;
		root_vel.y = linear_root_vel.z;

		return root_vel;
	}

	//-- get EnvMotionData
	public void GetFrame(float time, out Matrix4x4[] joints, out Matrix4x4 root)
	{
		int index = Mathf.Clamp(Mathf.RoundToInt(time * Framerate), 0, Motion.GetLength(0) - 1);
		GetFrame(index, out joints, out root);
		//joints = Motion[index];
		//root = Goals[index];
	}
	public void GetFrame(int index, out Matrix4x4[] joints, out Matrix4x4 root)
	{

		joints = GetFramePose(index);
		root = RootTr[index];
	}
	public bool GetFrames(int start, int end, out Matrix4x4[][] joints_clip, out Matrix4x4[] root_clip)
	{
		if (start < 1 || end > GetTotalFrames())
		{
			Debug.Log("Please specify indices between 1 and " + GetTotalFrames() + ". Given " + start + " and " + end + ".");
			joints_clip = new Matrix4x4[0][];
			root_clip = new Matrix4x4[0];
			return false;
		}
		int count = end - start + 1;
		joints_clip = new Matrix4x4[count][];
		root_clip = new Matrix4x4[count];
		for (int i = start; i <= end; i++)
		{
			joints_clip[i - start] = GetFramePose(i - 1);
			root_clip[i - start] = RootTr[i - 1];
		}
		return true;
	}

	public bool GetFrames(float start, float end, out Matrix4x4[][] joints_clip, out Matrix4x4[] root_clip)
	{

		int i_start = Mathf.Clamp(Mathf.RoundToInt(start * Framerate), 0, Motion.GetLength(0) - 1);
		int i_end = Mathf.Clamp(Mathf.RoundToInt(end * Framerate), 0, Motion.GetLength(0) - 1);
		if (start < 0f || end > GetTotalTime())
		{
			Debug.Log("Please specify times between 0 and " + GetTotalTime() + ". Given " + start + " and " + end + ".");
			joints_clip = new Matrix4x4[0][];
			root_clip = new Matrix4x4[0];
			return false;
		}

		return GetFrames(i_start, i_end, out joints_clip, out root_clip);
	}

	public void Load()
	{

		//Check Naming
		if (name == "Data")
		{
			Debug.Log("Updating name of asset at " + AssetDatabase.GetAssetPath(this) + ".");
			string dataName = Directory.GetParent(AssetDatabase.GetAssetPath(this)).Name;
			int dataDot = dataName.LastIndexOf(".");
			if (dataDot != -1)
			{
				dataName = dataName.Substring(0, dataDot);
			}
			AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(this), dataName);
			string parentDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
			int parentDot = parentDirectory.LastIndexOf(".");
			if (parentDot != -1)
			{
				AssetDatabase.MoveAsset(parentDirectory, parentDirectory.Substring(0, parentDot));
			}
		}

		//Open Scene
		GetScene();
	}

	public void Unload()
	{
		Scene scene = EditorSceneManager.GetSceneByName(name);
		if (Application.isPlaying)
		{
			SceneManager.UnloadSceneAsync(scene);
		}
		else
		{
			EditorSceneManager.CloseScene(scene, false);
			EditorCoroutines.StartCoroutine(RemoveScene(scene), this);
		}
	}

	private IEnumerator RemoveScene(Scene scene)
	{
		yield return new WaitForSeconds(1f);
		EditorSceneManager.CloseScene(scene, true);
		yield return new WaitForSeconds(0f);
		EditorApplication.RepaintHierarchyWindow();
	}

	public void Save()
	{
		if (!Application.isPlaying)
		{
			EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetSceneByName(name));
			EditorSceneManager.SaveScene(EditorSceneManager.GetSceneByName(name));
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}

	public Scene GetScene()
	{
		for (int i = 0; i < SceneManager.sceneCount; i++)
		{
			if (SceneManager.GetSceneAt(i).name == name)
			{
				return SceneManager.GetSceneAt(i);
			}
		}
		if (Application.isPlaying)
		{
			if (File.Exists(GetScenePath()))
			{
				EditorSceneManager.LoadSceneInPlayMode(GetScenePath(), new LoadSceneParameters(LoadSceneMode.Additive));
			}
			else
			{
				Debug.Log("Creating temporary scene for data " + name + ".");
				SceneManager.CreateScene(name);
			}
		}
		else
		{
			Scene active = EditorSceneManager.GetActiveScene();
			if (File.Exists(GetScenePath()))
			{
				EditorSceneManager.OpenScene(GetScenePath(), OpenSceneMode.Additive);
			}
			else
			{
				Debug.Log("Recreating scene for data " + name + ".");
				EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive), GetScenePath());
			}
			EditorSceneManager.SetActiveScene(SceneManager.GetSceneByName(name));
			Lightmapping.bakedGI = false;
			Lightmapping.realtimeGI = false;
			EditorSceneManager.SetActiveScene(active);
		}
		return SceneManager.GetSceneByName(name);
	}

	private string GetScenePath()
	{
		return Path.GetDirectoryName(AssetDatabase.GetAssetPath(this)) + "/" + name + ".unity";
	}

	public void CreateScene()
	{
		UnityEngine.SceneManagement.Scene active = EditorSceneManager.GetActiveScene();
		UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
		EditorSceneManager.SetActiveScene(scene);
		Lightmapping.bakedGI = false;
		Lightmapping.realtimeGI = false;
		EditorSceneManager.SetActiveScene(active);
		EditorSceneManager.SaveScene(scene, GetScenePath());
		EditorSceneManager.CloseScene(scene, true);
	}



	public void inspector(playIndoorSceneDemo pisd)
	{
		using (new EditorGUILayout.VerticalScope("Box"))
		{

			Utility.SetGUIColor(gui_color);
			using (new EditorGUILayout.VerticalScope("Box"))
			{
				Utility.ResetGUIColor();

				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
				Utility.GUIButton("MOTION", gui_color, UltiDraw.White);
				GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				//-- file name
				EditorGUILayout.LabelField("Frames: " + FileName, GUILayout.Width(500f));
				EditorGUILayout.LabelField("Order: ", GUILayout.Width(80f));
				Order = EditorGUILayout.IntField(Order, GUILayout.Width(40f));
				EditorGUILayout.EndHorizontal();

				//-------- motion
				if (Sequences.Length > 0)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Start", GUILayout.Width(50f));
					Sequences[0].Start = EditorGUILayout.IntField(Sequences[0].Start);

					EditorGUILayout.LabelField("End", GUILayout.Width(50f));
					Sequences[0].End = EditorGUILayout.IntField(Sequences[0].End);
					Sequences[0].End = Mathf.Clamp(Sequences[0].End, Sequences[0].Start, Motion.GetLength(0) - 1);
					EditorGUILayout.EndHorizontal();
				}

				// generate Root Trajectory
				if (Utility.GUIButton("Generate Root Trajectory", gui_color, UltiDraw.White))
				{
					GenerateRootTrajectory(Sequences[0].Start, Sequences[0].End);

					StartRootMat = RootTr.First<Matrix4x4>();
					EndRootMat = RootTr.Last<Matrix4x4>();

				}

				EditorGUILayout.BeginHorizontal();
				//-- file name
				GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
				Utility.GUIButton("ENVIRONMENT", gui_color, UltiDraw.White);
				GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
				EditorGUILayout.EndHorizontal();

				//-- scene object
				// chair object
				SetGameObject((GameObject)EditorGUILayout.ObjectField("Chair", Chair, typeof(GameObject), true), true);
				// desk object
				SetGameObject((GameObject)EditorGUILayout.ObjectField("Desk", Desk, typeof(GameObject), true), false);

				if (Chair != null && Desk != null)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("y_offset_f", GUILayout.Width(100f));
					y_offset_f = EditorGUILayout.FloatField(y_offset_f, GUILayout.Width(50f));
					EditorGUILayout.EndHorizontal();

					//Vector3 position = Chair_mat.GetPosition();
					//position.y = position.y + y_offset_f;
					//Chair.transform.position = position;

					//position = Desk_mat.GetPosition();
					//position.y = position.y + y_offset_f;
					//Desk.transform.position = position;

					//// make chair, desk matrix
					//b_same_furniture = EditorGUILayout.Toggle(" same with parent furniture ", b_same_furniture, GUILayout.Width(70f));
					
					//-- event function
					EditorGUILayout.BeginHorizontal();
					// chair rootmat write
					if (Utility.GUIButton("Write Chair Root", gui_color, UltiDraw.White))
					{
						pisd.event_WriteChairRoot(Chair, FileName, DirectoryName);
						Chair_mat = Chair.transform.GetWorldMatrix();
					}
					// desk rootmat write
					if (Utility.GUIButton("Write Desk Root", gui_color, UltiDraw.White))
					{
						pisd.event_WriteDeskRoot(Desk, FileName, DirectoryName);
						Desk_mat = Desk.transform.GetWorldMatrix();
					}
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
					// chair rootmat load
					if (Utility.GUIButton("Load Chair Root", gui_color, UltiDraw.White))
					{
						pisd.event_LoadRootChairData(Chair,FileName, DirectoryName);
						Chair_mat = Chair.transform.GetWorldMatrix();
					}
					// chair rootmat load
					if (Utility.GUIButton("Load Desk Root", gui_color, UltiDraw.White))
					{
						pisd.event_LoadRootDeskData(Desk, FileName, DirectoryName);
						Desk_mat = Desk.transform.GetWorldMatrix();
					}
					EditorGUILayout.EndHorizontal();


					// 
					EditorGUILayout.BeginHorizontal();
					Utility.SetGUIColor(UltiDraw.White);
					EditorGUILayout.LabelField("bool upper cond", GUILayout.Width(100f));
					b_uppercond = EditorGUILayout.IntField(b_uppercond, GUILayout.Width(100f));
					EditorGUILayout.LabelField("y_offset_h", GUILayout.Width(100f));
					y_offset_h = EditorGUILayout.FloatField(y_offset_h, GUILayout.Width(50f));
					EditorGUILayout.EndHorizontal();

				}

			}

		}
	}

}
#endif