using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;

public class BodyRetargetingManager : RealTimeAnimation
{
  
    //Using Motion Data Input
    public MotionDataFile _MotionData;
    //Using Human-Object Motion Input
    public EnvMotionData[] _envMotionData;
    public bool existing_envMotionData;
    //Using Python Connection
    public TCPClient _tcpClient;
    public MotionData_Type selectedOption;
    public RETARGETING_MODE selectedMode;
  
    //Using Retargeting Module
    public MW_RETARGET_Utils _retargetingUtil;

    //Global Variables
    public Actor actor_source = null;
    public Actor actor_target = null;
    public Transform base_offset = null;

    
    public int frameIdx = 0;
    public bool save_data = false;
    protected override void Setup()
    {
        _tcpClient = new TCPClient();
        _tcpClient.Setup("143.248.6.198",80);
        _MotionData = ScriptableObject.CreateInstance<MotionDataFile>();
        _retargetingUtil = ScriptableObject.CreateInstance<MW_RETARGET_Utils>();
        existing_envMotionData = false;

    }
    protected override void Feed()
    {
        if (play_data || b_data)
        {
            if (frameIdx > TotalFrames)
            {
                frameIdx = 0;
                play_data = false;
                save_data = true;
            }
            if (selectedOption == MotionData_Type.ALL || selectedOption == MotionData_Type.FBX_MOTIONTEXT || selectedOption == MotionData_Type.FBX)
            {
               //Debug.Log("frame " + frameIdx + " / " + _MotionData.FBXFiles[_MotionData.selectedData].Motion.Length + " select " + _MotionData.FBXFiles[_MotionData.selectedData].Character.Bones.Length);
                base.update_pose(frameIdx, _MotionData.FBXFiles[_MotionData.selectedData].Motion, _MotionData.FBXFiles[_MotionData.selectedData].Character);

            }
            if (selectedOption == MotionData_Type.ALL || selectedOption == MotionData_Type.BVH_MOTIONTEXT || selectedOption == MotionData_Type.BVH)
            {
                //Debug.Log("frame " + frameIdx + " / " + _MotionData.BVHFiles[_MotionData.selectedData].Motion.Length + " select " + _MotionData.BVHfiles[_MotionData.selectedData].Character.Bones.Length);
                //Debug.Log(" joint " + RetargetingTarget.actor.Bones.Length + "dof " + array.Length);

                _retargetingUtil.RetargetingSource.CalcLocalFrames(out _retargetingUtil.RetargetingSource.jointdelta);
                
                for (int j = 0; j < actor_source.Bones.Length; j++)
                {
                    if (j == 0)
                    {
                        actor_source.Bones[0].Transform.position = _MotionData.BVHFiles[_MotionData.selectedData].Motion[frameIdx][j].GetPosition();// new Vector3(array[0], array[1], array[2]);
                        actor_source.Bones[0].Transform.rotation = _MotionData.BVHFiles[_MotionData.selectedData].Motion[frameIdx][j].GetRotation();
                    }
                    else
                    {
                        //Debug.Log(" joint name " + RetargetingTarget.actor.Bones[j].GetName());
                        Quaternion quat_lH = _MotionData.BVHFiles[_MotionData.selectedData].Motion[frameIdx][actor_source.Bones[j].Parent].GetRotation().GetInverse() * 
                            _MotionData.BVHFiles[_MotionData.selectedData].Motion[frameIdx][j].GetRotation();

                        _retargetingUtil.RetargetingSource.actor.Bones[j].Transform.localRotation = _retargetingUtil.RetargetingSource.Default_local_mat[j].GetRotation() * quat_lH;
                    }
                }

                //base.update_pose(frameIdx, _MotionData.BVHFiles[_MotionData.selectedData].Motion, actor_source);
            }
            if (selectedOption == MotionData_Type.ALL || selectedOption == MotionData_Type.MOTIONTEXT)
            {
                //Debug.Log("frame " + frameIdx + " / " + _MotionData.MotionTextFiles[_MotionData.selectedData].Motion.Length + " select " + _MotionData.MotionTextFiles[_MotionData.selectedData].Character.Bones.Length);
                base.update_pose(frameIdx, _MotionData.MotionTextFiles[_MotionData.selectedData].Motion, _MotionData.MotionTextFiles[_MotionData.selectedData].Character);
            }

        }

        if (_retargetingUtil.b_connect == true)
        {
           
            // init MBS txt files & set joint mapping
            if (_retargetingUtil.b_connect_init_MBS)
            {
                
                string jsonData = _retargetingUtil.InitMBS();

                
                _tcpClient.SendData(jsonData);


                _retargetingUtil.b_connect_init_MBS = false;
            }
            // do Retargeting
            if (_retargetingUtil.b_connect_do_retargeting)
            {
                //if (play_data && selectedOption == MotionData_Type.FBX_MOTIONTEXT)
                //   _retargetingUtil.RetargetingSource.actor = _MotionData.FBXFiles[_MotionData.selectedData].Character;

                _retargetingUtil.RetargetingSource.CalcLocalFrames(out _retargetingUtil.RetargetingSource.jointdelta);
                _retargetingUtil.RetargetingSource.CalcJoinDelta();

                string jsonData = _retargetingUtil.DoRetargeting(base_offset);

                _tcpClient.SendData(jsonData);

            }
        }
    }
    protected override void Read()
    {
        if (_retargetingUtil.b_connect == true && _retargetingUtil.b_connect_do_retargeting == true)
        {
            _tcpClient.ReceiveData(_retargetingUtil.RetargetingTarget.actor.Bones.Length * 4 + 3);

            _retargetingUtil.UpdateFromJoinDelta(_tcpClient.receivedFloatArray);

            if (_MotionData.RecordingState == RecordingState.RECORDING)
            {
                // record output pose 
                _MotionData.RecordingPose(actor_target);
                // save output poses (= motion ) in text file
                if (save_data)
                {
                    _MotionData.SavingRecordedData();
                    save_data = false;
                    _MotionData.RecordingState = RecordingState.NONE;
                }
            }
            
        }
        if(play_data)
            frameIdx++;
    }
    protected override void Postprocess()
    {
        
    }
    protected override void OnGUIDerived()
    {

    }
    protected override void OnRenderObjectDerived()
    {
       
    }

    protected override void Close()
    {
        //_helloRequester.Stop();
        _tcpClient.OnDestroy();
    }

    
    [CustomEditor(typeof(BodyRetargetingManager), true)]
    public class BodyRetargetingManager_Editor : Editor
    {
        public BodyRetargetingManager Target;
        SerializedProperty selectedOption;
        SerializedProperty selectedMode;
        public void Awake()
        {
            Target = (BodyRetargetingManager)target;
            selectedOption = serializedObject.FindProperty("selectedOption");
            selectedMode = serializedObject.FindProperty("selectedMode");
        }

        public override void OnInspectorGUI()
        {
            inspector();
        }

        void inspector()
        {
            Utility.ResetGUIColor();
            Utility.SetGUIColor(UltiDraw.LightGrey);

            // Assigning Target Avatar
            EditorGUILayout.BeginVertical();
            Target.actor_source = (Actor)EditorGUILayout.ObjectField("Source Actor", Target.actor_source, typeof(Actor), true);
            Target.actor_target = (Actor)EditorGUILayout.ObjectField("Target Actor", Target.actor_target, typeof(Actor), true);
            Target.base_offset = (Transform)EditorGUILayout.ObjectField("Base Offset ", Target.base_offset, typeof(Transform), true);
            EditorGUILayout.EndVertical();

            // Motion Data Type Selection
            EditorGUILayout.PropertyField(selectedOption, true);
            serializedObject.ApplyModifiedProperties();

            // Retargeting Mode Selection
            EditorGUILayout.PropertyField(selectedMode, true);
            serializedObject.ApplyModifiedProperties();

            Target._retargetingUtil.selectedMode = Target.selectedMode;
            if(Target.selectedMode != RETARGETING_MODE.TESTING)
                // Retargeting Inspector
                Target._retargetingUtil.inspector(Target.actor_source, Target.actor_target, Target.base_offset);


            if (Target.selectedOption == MotionData_Type.ALL ||
                Target.selectedOption == MotionData_Type.FBX_MOTIONTEXT
                || Target.selectedOption == MotionData_Type.FBX)
            {
                // Motion Data inspector
                Target._MotionData.FBX_inspector(Target.actor_source);
                if(Target._MotionData.FBXFiles != null && Target._MotionData.FBXFiles.Length > 0)
                    Target.TotalFrames = Target._MotionData.FBXFiles[Target._MotionData.selectedData].GetTotalFrames() - 1;
            }
            
            if (Target.selectedOption == MotionData_Type.ALL || 
                Target.selectedOption == MotionData_Type.BVH_MOTIONTEXT || 
                Target.selectedOption == MotionData_Type.BVH)
            {
                // BVH Data inspector
                Target._MotionData.BVH_inspector();
                if (Target._MotionData.BVHFiles != null && Target._MotionData.BVHFiles.Length > 0)
                    Target.TotalFrames = Target._MotionData.BVHFiles[Target._MotionData.selectedData].GetTotalFrames() - 1;
            }


            if (Target.selectedOption == MotionData_Type.ALL ||
                Target.selectedOption == MotionData_Type.BVH_MOTIONTEXT ||
                Target.selectedOption == MotionData_Type.FBX_MOTIONTEXT ||
                Target.selectedOption == MotionData_Type.MOTIONTEXT)
            {
                // Motion Text Data inspector
                Target._MotionData.MotionTextFile_inspector(Target.actor_target);
                if (Target._MotionData.MotionTextFiles != null && Target._MotionData.MotionTextFiles.Length > 0)
                    Target.TotalFrames = Target._MotionData.MotionTextFiles[Target._MotionData.selectedData].GetTotalFrames() - 1;
            }
            
           
            // Play Motion Data
            if (!Application.isPlaying) return;

            if (Utility.GUIButton("reset & play animation", Color.white, Color.red))
            {
                Target.frameIdx = 0;
                Target.play_data = true;
            }
            if (Target.play_data == false)
            {
                if (Utility.GUIButton("re-play animation", Color.white, Color.red))
                {
                    Target.play_data = true;
                }
            }
            if (Target.play_data == true)
            {
                if (Utility.GUIButton("pause animation", Color.white, Color.red))
                {
                    Target.b_data = true;
                    Target.play_data = false;
                }
            }
            if (Target.play_data != true && Target._MotionData.BVHFiles != null)
                Target.frameIdx = EditorGUILayout.IntSlider(Target.frameIdx, 1, Target._MotionData.Motion.Length - 1);

            // Recording Function
            EditorGUILayout.BeginHorizontal();
            // if recording is finished and recorded data exists
            if (Target._MotionData.RecordingState == RecordingState.NONE && Target._MotionData.recordedList.Count > 0)
            {
                // Save data
                if (Utility.GUIButton("Save Recorded Data", UltiDraw.DarkBlue, UltiDraw.Blue))
                {
                    Target._MotionData.SavingRecordedData();
                }
            }
            else
            {
                if (Target.selectedMode == RETARGETING_MODE.REALTIME_RETARGETING) {
                    if (Target._MotionData.RecordingState == RecordingState.NONE)
                    {
                        if (Utility.GUIButton("Start Recording!", UltiDraw.DarkGrey, UltiDraw.White))
                        {
                            Target._MotionData.recordedList.Clear();
                            Target._MotionData.RecordingState = RecordingState.RECORDING;

                            Target.frameIdx = 0;
                            Target.play_data = true;
                            Target.save_data = false;
                        }
                    }
                    if (Target._MotionData.RecordingState == RecordingState.RECORDING)
                    {
                        if (Utility.GUIButton("Stop Recording!", UltiDraw.DarkRed, UltiDraw.Red))
                        {
                            Target._MotionData.RecordingState = RecordingState.NONE;
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (Target._retargetingUtil.b_connect)
            {
                if (Utility.GUIButton("Retargeting Without Visualization", UltiDraw.DarkRed, UltiDraw.Red))
                {
                    Target.play_data = false;
                    
                    /* retarget motion & save motion file text */
                    if (Target.selectedOption == MotionData_Type.ALL || Target.selectedOption == MotionData_Type.FBX_MOTIONTEXT)
                    {
                        for (int selectedData = 0; selectedData < Target._MotionData.FBXFiles.Length; selectedData++)
                        {
                            Target._MotionData.recordedList = new List<List<float>>();
                            Target._MotionData.ImportFBXData(selectedData, 1.0f);
                            for (int frameidx = 0; frameidx < Target._MotionData.FBXFiles[selectedData].GetTotalFrames(); frameidx++)
                            {
                                Debug.Log("frame " + frameidx + " / " + Target._MotionData.FBXFiles[selectedData].GetTotalFrames());
                                Target.update_pose(frameidx, Target._MotionData.FBXFiles[selectedData].Motion,
                                Target._retargetingUtil.RetargetingSource.actor);
                                Target._retargetingUtil.RetargetingSource.CalcLocalFrames(out Target._retargetingUtil.RetargetingSource.jointdelta);
                                Target._retargetingUtil.RetargetingSource.CalcJoinDelta();

                                // Send retargeting message
                                string jsonData = Target._retargetingUtil.DoRetargeting(Target.base_offset);
                                Target._tcpClient.SendData(jsonData);

                                // Received retargetted pose
                                Target._tcpClient.ReceiveData(Target._retargetingUtil.RetargetingTarget.actor.Bones.Length * 4 + 3);

                                Target._retargetingUtil.UpdateFromJoinDelta(Target._tcpClient.receivedFloatArray);


                                // record output pose 
                                Target._MotionData.RecordingPose(Target.actor_target);

                                
                            }
                            if (Target._MotionData.recordedList.Count > 0)
                            {
                                // save output poses (= motion ) in text file
                                Target._MotionData.SavingRecordedData(
                                    Target._MotionData.FBXFiles[selectedData].FILE_Info.FullName.Replace(".fbx",""),
                                    Target._retargetingUtil.RetargetingTarget.actor.name);
                                Target._MotionData.RecordingState = RecordingState.NONE;
                            }
                            else
                            {
                                Debug.Log(" FBX files are " + Target._MotionData.FBXFiles.Length);
                            }
                        }

                    }
                    if (Target.selectedOption == MotionData_Type.ALL || Target.selectedOption == MotionData_Type.BVH_MOTIONTEXT)
                    {
                        for (int selectedData = 0; selectedData < Target._MotionData.BVHFiles.Length; selectedData++)
                        {
                            Target._MotionData.recordedList = new List<List<float>>();
                            Target._MotionData.ImportBVHData(selectedData, Target._MotionData.scale);

                            Matrix4x4[][] Tpose = (Matrix4x4[][])Target._MotionData.BVHFiles[selectedData].Motion.Clone();

                            for (int j=0; j < Target.actor_source.Bones.Length; j++)
                                Tpose[0][j] = Matrix4x4.identity;

                            
                            for (int frameidx = 0; frameidx < Target._MotionData.BVHFiles[selectedData].GetTotalFrames(); frameidx++)
                            {
                                if(frameidx == 0)
                                    Target.update_pose(0, Tpose, Target.actor_source);
                                else
                                    Target.update_pose(frameidx, Target._MotionData.BVHFiles[selectedData].Motion, Target.actor_source);

                                Debug.Log("frame " + frameidx + " / " + Target._MotionData.BVHFiles[selectedData].Motion.Length);

                                Target._retargetingUtil.RetargetingSource.CalcLocalFrames(out Target._retargetingUtil.RetargetingSource.jointdelta);
                                Target._retargetingUtil.RetargetingSource.CalcJoinDelta();

                                // Send retargeting message
                                string jsonData = Target._retargetingUtil.DoRetargeting(Target.base_offset);
                                Target._tcpClient.SendData(jsonData);

                                // Received retargetted pose
                                Target._tcpClient.ReceiveData(Target._retargetingUtil.RetargetingTarget.actor.Bones.Length * 4 + 3);

                                // Update target charcter
                                Target._retargetingUtil.UpdateFromJoinDelta(Target._tcpClient.receivedFloatArray);


                                // record output pose 
                                Target._MotionData.RecordingPose(Target.actor_target);

                                

                            }
                            // save output poses (= motion ) in text file
                            Target._MotionData.SavingRecordedData(Target._MotionData.BVHFiles[selectedData].FILE_Info.FullName.Replace(".fbx", ""),
                                    Target._retargetingUtil.RetargetingTarget.actor.name);
                            Target._MotionData.RecordingState = RecordingState.NONE;
                        }
                    }
                }
            }
        }
    }

}
