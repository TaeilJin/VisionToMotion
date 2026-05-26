using UnityEngine;
using System.Text;
using System.IO;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class RealTimeAnimation : MonoBehaviour {

	public enum FPS {Thirty, Sixty}

	public Actor _actor;
    public int Frame;
	public int TotalFrames;
	public bool play_data;
	public bool b_data;

	public float AnimationTime {get; private set;}
	public float PostprocessingTime {get; private set;}
	public FPS Framerate = FPS.Thirty;

	protected abstract void Setup();
	protected abstract void Feed();
	protected abstract void Read();
	protected abstract void Postprocess();
    protected abstract void OnGUIDerived();
    protected abstract void OnRenderObjectDerived();

	protected abstract void Close();

    private void Awake()
    {
		int seed = 42; // 원하는 어떤 정수값이든 사용 가능
		Random.InitState(seed);
		Setup();
	}
    void Start() {
		
	}

	private void update_pose(Matrix4x4[] _pose, Actor _actor)
	{
		// update current pose data into actor class
		for (int j = 0; j < _actor.Bones.Length; j++)
			_actor.Bones[j].Transform.SetPositionAndRotation(_pose[j].GetPosition(), _pose[j].GetRotation());
	}

	public void update_pose(int index, Matrix4x4[][] _motion, Actor _actor)
	{
		//Debug.Log(_actor.name + " " + _actor.Bones[0].GetName());
		for (int j = 0; j < _actor.Bones.Length; j++){
			_actor.Bones[j].Transform.SetPositionAndRotation(_motion[index][j].GetPosition(), _motion[index][j].GetRotation());
		}
	}
	public void update_pose(int index, Matrix4x4[][] _motion, Matrix4x4[] _root, Actor _actor)
	{
		//Debug.Log("Frame " + index + "/" + _motion.GetLength(0));
		for (int j = 0; j < _actor.Bones.Length; j++)
		{
			Matrix4x4 jointmat = _motion[index][j].GetRelativeTransformationFrom(_root[index]);
			_actor.Bones[j].Transform.SetPositionAndRotation(jointmat.GetPosition(), jointmat.GetRotation());
		}

	}

    void FixedUpdate() {
		Utility.SetFPS(Mathf.RoundToInt(GetFramerate()));
		Time.fixedDeltaTime = (float)1 / 30f;
		
		System.DateTime t1 = Utility.GetTimestamp();

        // send target information 
        Feed();
		// get modified pose
		Read();
		// post processing
		AnimationTime = (float)Utility.GetElapsedTime(t1);
		System.DateTime t2 = Utility.GetTimestamp();
		Postprocess();
		PostprocessingTime = (float)Utility.GetElapsedTime(t2);

    }

    void OnGUI()
    {
		OnGUIDerived();
		//if (_kinectManager != null && _kinectManager.IsInitialized())
		//{
		//    _kinectManager.onGUIKinectSetting();

		//}
	}
	
    void OnRenderObject()
    {

        if (Application.isPlaying)
        {
            OnRenderObjectDerived();
        }
    }

	private void OnApplicationQuit()
    {
		OnDestroy();
		
    }
    private void OnDestroy()
    {
		Close();
		//_helloRequester.Stop();
		//_kinectManager.destoryKinectSetting();
	}

    public float GetFramerate() {
		switch(Framerate) {
			case FPS.Thirty:
			return 30f;
			case FPS.Sixty:
			return 60f;
			
		}
		return 1f;
	}
	public StreamWriter CreateFile(string foldername, string name, bool newfile, string root_extension)
	{
		string filename = string.Empty;
		string folder = foldername;
		if (!File.Exists(folder))
		{
			Directory.CreateDirectory(folder);
			folder = folder + '/';
		}
		else
			folder = folder + "/";
		if (!File.Exists(folder + name + root_extension))
		{
			filename = folder + name + root_extension;
		}
		else
		{
			if (newfile)
			{
				int i = 1;
				while (File.Exists(folder + name + "_" + i + "_" + root_extension))
				{
					i += 1;
				}
				filename = folder + name + "_" + i + "_" + root_extension;
			}
			else
				filename = folder + name + root_extension;
		}
		return File.CreateText(filename);
	}


	public StringBuilder WriteFloat(StringBuilder sb_, float x, bool first)
	{
		if (first)
		{
			sb_.Append(x);
		}
		else
		{
			sb_.Append(" ");
			sb_.Append(x);
		}
		return sb_;
	}
	public StringBuilder WriteString(StringBuilder sb_, string x, bool first)
	{
		if (first)
		{
			sb_.Append(x);
		}
		else
		{
			sb_.Append(" ");
			sb_.Append(x);
		}
		return sb_;
	}
	public StringBuilder WritePosition(StringBuilder sb_, Vector3 position, bool first)
	{
		sb_ = WriteFloat(sb_, position.x, first);
		sb_ = WriteFloat(sb_, position.y, false);
		sb_ = WriteFloat(sb_, position.z, false);

		return sb_;
	}
	public StringBuilder WriteQuat(StringBuilder sb_, Quaternion quat, bool first)
	{
		sb_ = WriteFloat(sb_, quat.x, first);
		sb_ = WriteFloat(sb_, quat.y, false);
		sb_ = WriteFloat(sb_, quat.z, false);
		sb_ = WriteFloat(sb_, quat.w, false);

		return sb_;
	}
    public StringBuilder WriteScale(StringBuilder sb_, Vector3 scale, bool first)
    {
        sb_ = WriteFloat(sb_, scale.x, first);
        sb_ = WriteFloat(sb_, scale.y, false);
        sb_ = WriteFloat(sb_, scale.z, false);

        return sb_;
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(RealTimeAnimation), true)]
	public class RealTimeAnimation_Editor : Editor {

		public RealTimeAnimation Target;

		void Awake() {
			Target = (RealTimeAnimation)target;
		}

		public override void OnInspectorGUI() {
			Undo.RecordObject(Target, Target.name);

			DrawDefaultInspector();

			EditorGUILayout.HelpBox("Animation: " + 1000f*Target.AnimationTime + "ms", MessageType.None);
			EditorGUILayout.HelpBox("Postprocessing: " + 1000f*Target.PostprocessingTime + "ms", MessageType.None);

			if(GUI.changed) {
				EditorUtility.SetDirty(Target);
			}
		}

	}
	#endif


}
