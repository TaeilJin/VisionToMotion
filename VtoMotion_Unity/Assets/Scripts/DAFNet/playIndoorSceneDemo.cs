using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using System.Text;
//using Unity.Barracuda;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;

//using NetMQ;
//using NetMQ.Sockets;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

//[RequireComponent(typeof(NavMeshAgent))]

[ExecuteInEditMode]
public class playIndoorSceneDemo : RealTimeAnimation
{
    //-- BVH File or FBX File 
    public MotionDataFile _MotionData;
    //-- importer class : DAFNet 을 위한 데이터들을 import 하는 클래스
    public ImporterClass io_class = new ImporterClass();
    //-- Environment-Human Data 
    public EnvMotionData[] Files = new EnvMotionData[0];
    //-- Experiments Class 
    public ExperimentsUtils exp_utils;
    //-- DAFNet 
    public DAFNet_FeatureExtraction _DAFNet_utils;
    public TCPClient _tcpClient;

    public MotionData_Type selectedOption;

    //-- Global Variables
    // Calibration 을 이용해서 적용할 진짜 캐릭터
    public Actor _actor_target = new Actor();
    // File 불러오기
    public string directoryPath;
    public int FileOrder = 0; // EnvMotion 데이터의 order 에 해당하는 File 의 index 
    public int FileIndex = 0; // order 순서 ( 0 : First ) 
    int startFrame = 1; // 첫 포즈 데이터의 frame index
    
    // Feature Extraction :
    int b_upper_cond = 0;
    private StringBuilder sb_record;
    private StreamWriter File_record;
    private StreamWriter File_record_prob;

    // 실험할 데이터의 마지막 프레임
    public int EndFrame = 50;
    // End-Effector
    public GameObject Head;
    public GameObject LeftHand;
    public GameObject RightHand;
       
    // bool variables
    public bool b_play = false;
    public bool b_draw_total = false;
    public bool b_visualize = false;
    public bool b_penetration = false;
    public bool b_shows_gtEE = false;
    bool b_data_exist = false;
    bool b_goal_data_exist = false;
    bool b_output_data_exist = false;
    bool b_record = false;
    bool b_connect = false;

    //--------------------------------------------

    // Actor 
    [SerializeField] private Actor Actor = null;
    public void SetCharacter(Actor character)
    {
        if (_actor == null && character != null)
        {
            if (Actor != null)
            {
                Utility.Destroy(Actor.gameObject);
                Actor = null;
            }
            _actor = character;
        }
        else
        {
            _actor = character;
        }
    }
    public void SetTargetCharacter(Actor character)
    {
        if (_actor_target == null && character != null)
        {
            if (Actor != null)
            {
                Utility.Destroy(Actor.gameObject);
                Actor = null;
            }
            _actor_target = character;
        }
        else
        {
            _actor_target = character;
        }
    }

    // BVH Motion : BVH 파일들 불러오기
    /* Update Function */
    private void update_pose(int index)
    {
        //Debug.Log("Frame " + index + "/" + io_class.Motion.GetLength(0));
        for (int j = 0; j < _actor.Bones.Length; j++)
            _actor.Bones[j].Transform.SetPositionAndRotation(io_class.Motion[index][j].GetPosition(), io_class.Motion[index][j].GetRotation());

    }
    private void update_pose(int index, Matrix4x4[][] _motion, Matrix4x4[] _root)
    {
        //Debug.Log("Frame " + index + "/" + _motion.GetLength(0));
        for (int j = 0; j < _actor.Bones.Length; j++)
        {
            Matrix4x4 jointmat = _motion[index][j].GetRelativeTransformationFrom(_root[index]);
            _actor.Bones[j].Transform.SetPositionAndRotation(jointmat.GetPosition(), jointmat.GetRotation());
        }

    }
    protected void update_ee()
    {
        Head.transform.SetPositionAndRotation(io_class.GTMotion[Frame][5].GetPosition(), io_class.GTMotion[Frame][5].rotation);

        LeftHand.transform.SetPositionAndRotation(io_class.GTMotion[Frame][9].GetPosition(), io_class.GTMotion[Frame][9].rotation);

        RightHand.transform.SetPositionAndRotation(io_class.GTMotion[Frame][13].GetPosition(), io_class.GTMotion[Frame][13].rotation);
    }


    // ENVMotionData: 환경을 만들 BVH 파일 불러올 순서 정하기
    public int findOrder(EnvMotionData[] data, int order)
    {
        for (int e = 0; e < data.Length; e++)
        {
            if (order == data[e].Order)
                return e;
        }
        Debug.Log("EnvData has no Order");
        return 900;
    }
    //public Matrix4x4 start_root_frame;
    private Vector3 desk_root_org;
    private Vector3 chair_root_org;

    //--- Current
    public void event_WriteChairRoot(GameObject chair, string Name, string Directory)
    {
        string foldername = Directory;

        Matrix4x4 chair_root = chair.transform.GetWorldMatrix();

        Debug.Log("chair root of " + Name);
        if (File.Exists(foldername + "/" + Name))
        {

            string name = Name + "_chair";

            Debug.Log("wrtie chair root on " + name);
            File_record = CreateFile(foldername, name, false, ".txt");
            sb_record = new StringBuilder();

            sb_record = WritePosition(sb_record, chair_root.GetPosition(), true);
            sb_record = WriteQuat(sb_record, chair_root.GetRotation(), false);

            File_record.WriteLine(sb_record.ToString());

            File_record.Close();
            sb_record.Clear();
        }
    }
    public void event_WriteDeskRoot(GameObject go_desk, string Name, string Directory)
    {
        string foldername = Directory;

        Matrix4x4 desk_root = go_desk.transform.GetWorldMatrix();

        Debug.Log("desk root of " + Name);
        if (File.Exists(foldername + "/" + Name))
        {
            string name = Name + "_desk";

            Debug.Log("wrtie desk root on " + name);

            File_record = CreateFile(foldername, name, false, ".txt");
            sb_record = new StringBuilder();

            sb_record = WritePosition(sb_record, desk_root.GetPosition(), true);
            sb_record = WriteQuat(sb_record, desk_root.GetRotation(), false);

            File_record.WriteLine(sb_record.ToString());

            File_record.Close();
            sb_record.Clear();
        }
    }
    public void event_LoadRootDeskData(GameObject desk, string Name, string Directory)
    {
        string foldername = Directory;
        string name = Name + "_desk";

        if (ImporterClass.ImportTextRootData(foldername, name))
        {
            desk_root_org = new Vector3(ImporterClass.RootMat[0][0], ImporterClass.RootMat[0][1], ImporterClass.RootMat[0][2]);
            Quaternion quat = new Quaternion(ImporterClass.RootMat[0][3], ImporterClass.RootMat[0][4], ImporterClass.RootMat[0][5], ImporterClass.RootMat[0][6]);
            desk.transform.SetPositionAndRotation(desk_root_org, quat);

        }
        else
        {
            Debug.Log("there is no file " + name);
        }
    }
    public void event_LoadRootChairData(GameObject chair, string Name, string Directory )
    {
        string foldername = Directory;
        string name = Name + "_chair";

        if (ImporterClass.ImportTextRootData(foldername, name))
        {
            chair_root_org = new Vector3(ImporterClass.RootMat[0][0], ImporterClass.RootMat[0][1], ImporterClass.RootMat[0][2]);
            Quaternion quat = new Quaternion(ImporterClass.RootMat[0][3], ImporterClass.RootMat[0][4], ImporterClass.RootMat[0][5], ImporterClass.RootMat[0][6]);
            chair.transform.SetPositionAndRotation(chair_root_org, quat);
        }
        else
        {
            Debug.Log("there is no file " + name);
        }
    }


    public CylinderMap Environment;
    private float size = 2f;
    private float resolution = 8;
    private float layers = 15;

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
    private void extract_goal_feature(Matrix4x4 pre_root, Matrix4x4 current_root, out Vector3 root_vel, out float[,] joint_position)
    {

        // root velocity 구하기
        root_vel = GetRootVelocity(pre_root, current_root);

        // current root 로 environment sensing 하기
        //Debug.Log(current_root.GetPosition());
        // environment sensing
        Vector3 root_pos = current_root.GetPosition();
        current_root.GetPosition().Set(root_pos.x, 0.0f, root_pos.y);
        Environment.Sense(current_root, LayerMask.GetMask("Default", "Interaction"));

        // current root 에서 본 pose 얻기
        joint_position = new float[_actor.Bones.Length, 3];
        for (int j = 0; j < _actor.Bones.Length; j++)
        {
            Vector3 position_j = _actor.Bones[j].Transform.position.GetRelativePositionTo(current_root);
            joint_position[j, 0] = position_j.x;
            joint_position[j, 1] = position_j.y;
            joint_position[j, 2] = position_j.z;
        }


    }
    private void write_goal_feature(float[,] joint_position, Vector3 root_vel, int b_upper_cond)
    {
        sb_record = new StringBuilder();
        sb_record = WriteFloat(sb_record, b_upper_cond, true);
        // joint position
        for (int j = 0; j < _actor.Bones.Length; j++)
        {
            if (j == 0)
                sb_record = WritePosition(sb_record, new Vector3(joint_position[j, 0], joint_position[j, 1], joint_position[j, 2]), false);
            else
                sb_record = WritePosition(sb_record, new Vector3(joint_position[j, 0], joint_position[j, 1], joint_position[j, 2]), false);
        }
        // Environment Sensor
        for (int e = 0; e < Environment.Occupancies.Length; e++)
            sb_record = WriteFloat(sb_record, Environment.Occupancies[e], false);
        // root velocity
        sb_record = WritePosition(sb_record, root_vel, false);

    }




    // Calibration
    protected Matrix4x4[] joint_mat_offset = new Matrix4x4[0];
    protected void update_target()
    {
        for (int j = 0; j < 22; j++)
        {

            Matrix4x4 bone_mat = _actor.Bones[j].Transform.GetWorldMatrix() * joint_mat_offset[j];
            _actor_target.Bones[j].Transform.SetPositionAndRotation(bone_mat.GetPosition(), bone_mat.GetRotation());
        }

    }
    public void Calibrate()
    {
        joint_mat_offset = new Matrix4x4[_actor.Bones.Length];

        _actor.Bones[0].Transform.position = _actor_target.Bones[0].Transform.position;
        for (int j = 0; j < 22; j++)
        {
            _actor.Bones[j].Transform.localRotation = Quaternion.identity;
        }

        for (int j = 0; j < 22; j++)
        {
            joint_mat_offset[j] = _actor_target.Bones[j].Transform.GetWorldMatrix().GetRelativeTransformationTo(_actor.Bones[j].Transform.GetWorldMatrix());
        }


    }

    // Experiments : Probabilities 출력 하기
    public UltiDraw.GUIRect Rect;
    private float[] cur_prob_value;
    public Matrix4x4 start_root_frame;
    public bool b_probability =false;


    public bool event_DoContact()
    {
        b_penetration = true;
        int nlength = io_class.Motion.GetLength(0);

        Debug.Log("Threshold" + exp_utils.Threshold);
        for (int j = 0; j < _actor.Bones.Length; j++)
            exp_utils.ContactJoints[j] = new ExperimentsUtils.ContactJointSensor(nlength, j, Vector3.zero, 0, exp_utils.Threshold, 0, ExperimentsUtils.ContactJointSensor.ID.Closest, ExperimentsUtils.ContactJointSensor.ID.None);

        //exp_utils.ContactJoints[0] = new ExperimentsUtils.ContactJointSensor(nlength, _actor.FindBone("mixamorig:RightToeBase").Index, Vector3.zero, 0, exp_utils.Threshold, 0, ExperimentsUtils.ContactJointSensor.ID.Closest, ExperimentsUtils.ContactJointSensor.ID.None);
        //exp_utils.ContactJoints[1] = new ExperimentsUtils.ContactJointSensor(nlength, _actor.FindBone("mixamorig:LeftToeBase").Index, Vector3.zero, 0, exp_utils.Threshold, 0, ExperimentsUtils.ContactJointSensor.ID.Closest, ExperimentsUtils.ContactJointSensor.ID.None);

        //exp_utils.ContactJoints[2] = new ExperimentsUtils.ContactJointSensor(nlength, _actor.FindBone("mixamorig:Head").Index, Vector3.zero, 0, exp_utils.Threshold, 0, ExperimentsUtils.ContactJointSensor.ID.Closest, ExperimentsUtils.ContactJointSensor.ID.None);
        //exp_utils.ContactJoints[3] = new ExperimentsUtils.ContactJointSensor(nlength, _actor.FindBone("mixamorig:LeftHand").Index, Vector3.zero, 0, exp_utils.Threshold, 0, ExperimentsUtils.ContactJointSensor.ID.Closest, ExperimentsUtils.ContactJointSensor.ID.None);
        //exp_utils.ContactJoints[4] = new ExperimentsUtils.ContactJointSensor(nlength, _actor.FindBone("mixamorig:RightHand").Index, Vector3.zero, 0, exp_utils.Threshold, 0, ExperimentsUtils.ContactJointSensor.ID.Closest, ExperimentsUtils.ContactJointSensor.ID.None);

        return true;
    }
    public bool event_ExtractContact()
    {
        for (int k = 0; k < exp_utils.ContactJoints[0].nFrames; k++)
        {
            update_pose(k);
            for (int j = 0; j < exp_utils.ContactJoints.Length; j++)
                exp_utils.ContactJoints[j].bone_mat[k] = _actor.Bones[exp_utils.ContactJoints[j].Bone].Transform.GetWorldMatrix();
            //exp_utils.ContactJoints[1].bone_mat[k] = _actor.Bones[exp_utils.ContactJoints[1].Bone].Transform.GetWorldMatrix();
            //exp_utils.ContactJoints[2].bone_mat[k] = _actor.Bones[exp_utils.ContactJoints[2].Bone].Transform.GetWorldMatrix();
            //exp_utils.ContactJoints[3].bone_mat[k] = _actor.Bones[exp_utils.ContactJoints[3].Bone].Transform.GetWorldMatrix();
            //exp_utils.ContactJoints[4].bone_mat[k] = _actor.Bones[exp_utils.ContactJoints[4].Bone].Transform.GetWorldMatrix();

        }

        exp_utils.CaptureFootContacts();
        //exp_utils.ExtractFootSliding();

        return true;
    }







    /* Event Write Function */


    public void event_LoadGoalData()
    {
        b_data_exist = false;
        b_goal_data_exist = true;

        string foldername = directoryPath;
        string name = "SeqDemo";

        Debug.Log("loadGoal: " + io_class.ImportTextGoalData(foldername, name));

        Debug.Log("Goal Frames: " + io_class.Goals.Length);

        Debug.Log("Goal start Root: " + ImporterClass.ImportTextRootData(foldername, name + "_start_root"));

        Vector3 position = new Vector3(ImporterClass.RootMat[0][0], ImporterClass.RootMat[0][1], ImporterClass.RootMat[0][2]);
        Quaternion quat = new Quaternion(ImporterClass.RootMat[0][3], ImporterClass.RootMat[0][4], ImporterClass.RootMat[0][5], ImporterClass.RootMat[0][6]);
        start_root_frame.SetTRS(position, quat, Vector3.one);

        //write result
        File_record = CreateFile(foldername, name, true, "output.csv");
        //
        File_record_prob = CreateFile(foldername, name, true, "output_prob.csv");

        b_record = true;

        Frame = 0;
        EndFrame = io_class.Goals.Length - 11;
    }
    public void event_WriteGoalData()
    {
        string foldername = directoryPath;
        string name = "SeqDemo";

        File_record = CreateFile(foldername, name, true, ".txt");

        b_record = true;
        b_play = true;
        b_output_data_exist = false;

        FileIndex = 0;
        FileOrder = findOrder(Files, FileIndex);
        Frame = 1;

        b_upper_cond = Files[FileOrder].b_uppercond;
        start_root_frame = Files[FileOrder].RootTr[Frame];
        io_class.WriteMatData(foldername, name + "_start_root", start_root_frame);
        Debug.Log("let's b_upper_cond " + b_upper_cond);
    }

    
    public void event_LoadOutputMotionFile()
    {
        string name = "SeqDemo";

        Debug.Log("loadDirectory : " + io_class.LoadDirectory(directoryPath, "*output.csv"));

        Debug.Log("DirectoryPath : " + directoryPath + " files : " + io_class.Files.Length);

        //inputslider_num.maxValue = io_class.Files.Length - 1;

        //int AnimFile_Num = (int)inputslider_num.value;

        Debug.Log("Goal start Root: " + ImporterClass.ImportTextRootData(directoryPath, name + "_start_root"));

        Vector3 position = new Vector3(ImporterClass.RootMat[0][0], ImporterClass.RootMat[0][1], ImporterClass.RootMat[0][2]);
        Quaternion quat = new Quaternion(ImporterClass.RootMat[0][3], ImporterClass.RootMat[0][4], ImporterClass.RootMat[0][5], ImporterClass.RootMat[0][6]);
        start_root_frame.SetTRS(position, quat, Vector3.one);

        Debug.Log("load CSV File : " + io_class.ImportOutputMotionData(directoryPath, 0, _actor.Bones.Length, start_root_frame, out io_class.Motion));

        Debug.Log("Motion Frames: " + io_class.Motion.GetLength(0));
        Debug.Log("Motion Frames: " + io_class.Prob.GetLength(0));

        if (io_class.Motion.GetLength(0) == io_class.Prob.GetLength(0))
            b_probability = true;
        else
            Debug.Log("there is no matching prob data");
        //inputslider_frame.maxValue = io_class.Motion.GetLength(0) - 1;

        //inputText.text = io_class.Files[AnimFile_Num].Object.FullName;

        EndFrame = io_class.Motion.GetLength(0) - 11;
        b_data_exist = false;
        b_output_data_exist = true;
        b_goal_data_exist = false;
        b_penetration = false;

        Frame = startFrame;
    }
    public void event_LoadGTMotionFile()
    {

        Debug.Log("loadDirectory : " + io_class.LoadDirectory(directoryPath, "*output_GT.csv"));

        Debug.Log("DirectoryPath : " + directoryPath + " files : " + io_class.Files.Length);

        //inputslider_num.maxValue = io_class.Files.Length - 1;

        //int AnimFile_Num = (int)inputslider_num.value;
        Vector3 position = new Vector3(ImporterClass.RootMat[0][0], ImporterClass.RootMat[0][1], ImporterClass.RootMat[0][2]);
        Quaternion quat = new Quaternion(ImporterClass.RootMat[0][3], ImporterClass.RootMat[0][4], ImporterClass.RootMat[0][5], ImporterClass.RootMat[0][6]);
        start_root_frame.SetTRS(position, quat, Vector3.one);
        Debug.Log("load CSV File : " + io_class.ImportOutputMotionData(directoryPath, 0, _actor.Bones.Length, start_root_frame, out io_class.GTMotion));

        Debug.Log("Motion Frames: " + io_class.GTMotion.GetLength(0));
        Debug.Log("Motion Frames: " + io_class.Prob.GetLength(0));

        if (io_class.Motion.GetLength(0) == io_class.Prob.GetLength(0))
            b_probability = true;
        else
            Debug.Log("there is no matching prob data");
        //inputslider_frame.maxValue = io_class.Motion.GetLength(0) - 1;

        //inputText.text = io_class.Files[AnimFile_Num].Object.FullName;

        b_data_exist = false;
        b_output_data_exist = true;
        b_goal_data_exist = false;
        b_penetration = false;

        Frame = startFrame;
    }
    public void event_UpdateRootTR()
    {
        for (int order = 0; order < Files.Length - 1; order++)
        {
            int id_order = findOrder(Files, order);
            int id_order_next = findOrder(Files, order + 1);
            Files[id_order_next].GenerateRootTrajectory(Files[id_order].RootTr.Last<Matrix4x4>());
        }
    }
    public void event_PlayAnimation()
    {
        b_play = true;
        b_data_exist = true;
        b_output_data_exist = false;
        b_goal_data_exist = false;

        Frame = startFrame;// Files[FileOrder].Sequences[0].Start;
        //end_frame = Files[FileOrder].Sequences[0].End;

    }
    public void event_PauseAnimation()
    {
        b_play = false;
    }


    
    protected override void Setup()
    {
        // calibrate character
        Calibrate();

        _MotionData = (MotionDataFile)ScriptableObject.CreateInstance(typeof(MotionDataFile));
        // io class
        //io_class = new ImporterClass();
        // environment
        Environment = new CylinderMap(size, (int)resolution, (int)layers, false);

        // connection toggle
        //inputToggle_connection.onValueChanged.AddListener(event_ToggleConnect);
        cur_prob_value = new float[10];
        b_probability = false;
        // experiment
        exp_utils = new ExperimentsUtils();
        exp_utils.ContactJoints = new ExperimentsUtils.ContactJointSensor[22];
        event_DoContact();

        _DAFNet_utils = ScriptableObject.CreateInstance<DAFNet_FeatureExtraction>();
        _DAFNet_utils.Setup();
        _tcpClient = new TCPClient();
        _tcpClient.Setup("143.248.6.198", 80);
        b_play = false;
        Debug.Log("b_play " + b_play + " b_recrod " + b_record);
    }

    protected override void Feed()
    {
        
        // load bvh file play and write goal 
        if (b_data_exist || b_output_data_exist)
        {
            // update chair 
            if (Files[FileIndex].b_same_furniture)
            {
                Files[FileIndex].Desk_mat = Files[FileIndex - 1].Desk_mat;
                Files[FileIndex].Chair_mat = Files[FileIndex - 1].Chair_mat;
            }

            if (b_play == true)
            {
                if (b_output_data_exist == false)
                {
                    //Debug.Log(" see frame " + Frame + "/ " + Files[FileOrder].RootTr.Length);
                    if (Frame == EndFrame)
                    {

                        FileIndex += 1;

                        if (FileIndex == Files.Length)
                        {
                            // 모든 파일이 끝났으면 첫 번째 파일을 불러온다.
                            Debug.Log("write finish: ");
                            if (b_record)
                            {
                                // goal 만드는 중이었으면, goal 을 저장한다.
                                File_record.Close();
                                b_record = false;
                            }
                            FileIndex = 0;
                            Frame = 0;
                        }
                        else
                        {
                            // fileindex 가 멀었으면, 다음 file 을 불러온다.
                            FileOrder = findOrder(Files, FileIndex);
                            Debug.Log(" file order " + FileOrder);
                            b_upper_cond = Files[FileOrder].b_uppercond;
                            if (b_record)
                                Frame = 1;
                            else
                                Frame = 0;
                        }

                    }

                    //Debug.Log("play Frame: " + Frame + " / " + Files[FileOrder].Sequences[0].End + " length");
                    //Debug.Log(" File order " + FileOrder + " / " + Files.Length + " Files");
                    // update actor pose
                    update_pose(Frame, Files[FileOrder].MotionWR, Files[FileOrder].RootTr);

                   

                    if (Files[FileOrder].b_uppercond == 1)
                    {
                        Vector3 position = _actor.Bones[0].Transform.position;
                        position.y += Files[FileOrder].y_offset_h;
                        _actor.Bones[0].Transform.position = position;
                    }

                    if (b_record)
                    {
                        // extract goal 
                        Vector3 root_vel;
                        float[,] joint_position;
                        extract_goal_feature(Files[FileOrder].RootTr[Frame - 1], Files[FileOrder].RootTr[Frame], out root_vel, out joint_position);

                        //if(b_upper_cond == 1)
                        //{
                        //    //// end -effector
                        //    //joint_position[_actor.FindBone("LeftHand").Index,1] += y_offset;
                        //    //joint_position[_actor.FindBone("RightHand").Index, 1] += y_offset;
                        //    //joint_position[_actor.FindBone("Head").Index, 1] += y_offset;

                        //    // upper-body
                        //    for (int upper = 0; upper < 14; upper++)
                        //    {
                        //        joint_position[upper, 1] += Files[FileOrder].y_offset;
                        //    }

                        //}

                        // write goal 
                        write_goal_feature(joint_position, root_vel,b_upper_cond);
                        // write one line 
                        File_record.WriteLine(sb_record.ToString());
                        sb_record.Clear();
                    }

                    Frame++;

                }
                if (b_output_data_exist == true)
                {
                    if (Frame == (io_class.Motion.GetLength(0)))
                    {
                        Frame = startFrame;
                        if (b_record)
                        {
                            Debug.Log("write finish generate motion : ");
                            File_record.Close();
                            b_record = false;
                        }
                    }

                    if (b_play == true)
                    {
                        //Debug.Log("play Frame: " + Frame);
                        // update actor pose
                        update_pose(Frame);
                        //update_pose(Frame, io_class.Motion, Files[FileOrder].RootTr);
                        cur_prob_value = io_class.Prob[Frame];
                    }

                    if (b_shows_gtEE)
                        update_ee();

                    Frame++;
                }
            }
            else
            {
                if (Files[FileOrder].MotionWR.Length > 0 && b_output_data_exist == false)
                {
                    // update actor pose
                    update_pose(Frame, Files[FileOrder].MotionWR, Files[FileOrder].RootTr);
                }
                if (b_output_data_exist == true)
                {
                    // update actor pose
                    update_pose(Frame, io_class.Motion, Files[FileOrder].RootTr);
                    cur_prob_value = io_class.Prob[Frame];
                }
            }
        }
        // goal feature play
        if (b_goal_data_exist)
        {
            //Debug.Log("Frame " + Frame);
            if (Frame == EndFrame)
            {
                Frame = 0;
                if (b_record)
                {
                    Debug.Log("write finish: ");
                    File_record.Close();
                    File_record_prob.Close();
                    b_record = false;
                }
            }


            if (b_connect)
            {

                string jsonData = _DAFNet_utils.send_index_Play(Frame);
                _tcpClient.SendData(jsonData);
                _tcpClient.ReceiveData(_actor.Bones.Length * 4 + 3 + 10);

                //
                _DAFNet_utils.update_matrix(_tcpClient.receivedFloatArray, _actor, out cur_prob_value);
                //

                if (b_record)
                {
                    // write output pose 
                    sb_record = new StringBuilder();
                    sb_record = WritePosition(sb_record, _actor.Bones[0].Transform.position, true);
                    sb_record = WriteQuat(sb_record, _actor.Bones[0].Transform.rotation, false);

                    for (int j = 1; j < _actor.Bones.Length; j++)
                    {
                        sb_record = WritePosition(sb_record, _actor.Bones[j].Transform.position, false);
                        sb_record = WriteQuat(sb_record, _actor.Bones[j].Transform.rotation, false);
                    }
                    // write one line 
                    File_record.WriteLine(sb_record.ToString());
                    sb_record.Clear();

                    // write output probability
                    sb_record = new StringBuilder();
                    sb_record = WriteFloat(sb_record, cur_prob_value[0], true);
                    for (int k = 1; k < 10; k++)
                        sb_record = WriteFloat(sb_record, cur_prob_value[k], false);
                    // write one line 
                    File_record_prob.WriteLine(sb_record.ToString());
                    sb_record.Clear();
                }

                Frame += 1;
            }


        }
    }
    
    protected override void Read()
    {
        
        update_target();
        
        //visRoot.SetPositionAndRotation( GetRootTransformation_JointTransformation(_actor_target, 0.0f).GetPosition(), GetRootTransformation_JointTransformation(_actor_target, 0.0f).GetRotation());   
    }

    protected override void Postprocess()
    {
        
    }

    protected override void OnRenderObjectDerived()
    {
        if (b_visualize)
        {
            if (b_data_exist || b_output_data_exist)
            {
                UltiDraw.Begin();
                //UltiDraw.DrawWiredSphere(Files[FileOrder].StartRootMat.GetPosition(), Files[FileOrder].StartRootMat.rotation, 0.1f, UltiDraw.DarkRed, UltiDraw.Black);
                //UltiDraw.DrawTranslateGizmo(Files[FileOrder].StartRootMat.GetPosition(), Files[FileOrder].StartRootMat.rotation, 0.1f);

                //UltiDraw.DrawWiredSphere(Files[FileOrder].EndRootMat.GetPosition(), Files[FileOrder].EndRootMat.rotation, 0.1f, UltiDraw.DarkRed, UltiDraw.Black);
                //UltiDraw.DrawTranslateGizmo(Files[FileOrder].EndRootMat.GetPosition(), Files[FileOrder].EndRootMat.rotation, 0.1f);

                int framewidth = 30;
                for (int i = 0; i < Mathf.RoundToInt(Files[FileOrder].RootTr.Length / framewidth); i++)
                {
                    UltiDraw.DrawWiredSphere(Files[FileOrder].RootTr[framewidth * i].GetPosition(), Files[FileOrder].RootTr[framewidth * i].rotation, 0.1f, UltiDraw.Orange, UltiDraw.Black);
                    UltiDraw.DrawTranslateGizmo(Files[FileOrder].RootTr[framewidth * i].GetPosition(), Files[FileOrder].RootTr[framewidth * i].rotation, 0.1f);
                    //UltiDraw.DrawTranslateGizmo(Files[FileOrder].Motion[framewidth * i][5].GetPosition(), Files[FileOrder].Motion[framewidth * i][5].rotation, 0.1f);
                }

                if (b_shows_gtEE)
                {
                    //UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[0][5].GetPosition(), Files[FileOrder].Motion[0][5].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                    framewidth = 10;
                    for (int p = 0; p < Mathf.RoundToInt(io_class.GTMotion.Length / framewidth); p++)
                    {
                        UltiDraw.DrawWiredSphere(io_class.GTMotion[framewidth*p][9].GetPosition(), io_class.GTMotion[framewidth*p][9].rotation, 0.01f, UltiDraw.Red, UltiDraw.Black);
                        UltiDraw.DrawWiredSphere(io_class.GTMotion[framewidth*p][13].GetPosition(), io_class.GTMotion[framewidth*p][13].rotation, 0.01f, UltiDraw.Red, UltiDraw.Black);
                    }
                }
                //// start 
                //UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[0][5].GetPosition(), Files[FileOrder].Motion[0][5].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                //UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[0][9].GetPosition(), Files[FileOrder].Motion[0][9].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                //UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[0][13].GetPosition(), Files[FileOrder].Motion[0][13].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                ////
                ////
                //end_frame = Files[FileOrder].RootTr.Length - 1;
                //UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[end_frame][5].GetPosition(), Files[FileOrder].Motion[end_frame][5].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                //UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[end_frame][9].GetPosition(), Files[FileOrder].Motion[end_frame][9].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);
                //UltiDraw.DrawWiredSphere(Files[FileOrder].Motion[end_frame][13].GetPosition(), Files[FileOrder].Motion[end_frame][13].rotation, 0.1f, UltiDraw.Red, UltiDraw.Black);


                UltiDraw.DrawWiredSphere(Files[FileOrder].RootTr.First<Matrix4x4>().GetPosition(), Files[FileOrder].RootTr.First<Matrix4x4>().rotation, 0.1f, UltiDraw.Orange, UltiDraw.Black);
                UltiDraw.DrawTranslateGizmo(Files[FileOrder].RootTr.First<Matrix4x4>().GetPosition(), Files[FileOrder].RootTr.First<Matrix4x4>().rotation, 0.1f);

                UltiDraw.DrawWiredSphere(Files[FileOrder].RootTr.Last<Matrix4x4>().GetPosition(), Files[FileOrder].RootTr.Last<Matrix4x4>().rotation, 0.1f, UltiDraw.Green, UltiDraw.Black);
                UltiDraw.DrawTranslateGizmo(Files[FileOrder].RootTr.Last<Matrix4x4>().GetPosition(), Files[FileOrder].RootTr.Last<Matrix4x4>().rotation, 0.1f);

                UltiDraw.End();

                if (b_draw_total)
                {
                    if (Files.Length > 1)
                    {
                        UltiDraw.Begin();
                        for (int order = 0; order < Files.Length - 1; order++)
                        {
                            int id_order = findOrder(Files, order);

                            for (int i = 0; i < Mathf.RoundToInt(Files[id_order].RootTr.Length / framewidth); i++)
                            {
                                UltiDraw.DrawWiredSphere(Files[id_order].RootTr[framewidth * i].GetPosition(), Files[id_order].RootTr[framewidth * i].rotation, 0.1f, UltiDraw.Orange, UltiDraw.Black);
                                UltiDraw.DrawTranslateGizmo(Files[id_order].RootTr[framewidth * i].GetPosition(), Files[id_order].RootTr[framewidth * i].rotation, 0.1f);
                            }


                            int id_order_next = findOrder(Files, order + 1);
                            //Files[id_order_next].GenerateRootTrajectory(Files[id_order].RootTr.Last<Matrix4x4>());

                            for (int i = 0; i < Mathf.RoundToInt(Files[id_order_next].RootTr.Length / framewidth); i++)
                            {
                                UltiDraw.DrawWiredSphere(Files[id_order_next].RootTr[framewidth * i].GetPosition(), Files[id_order_next].RootTr[framewidth * i].rotation, 0.1f, UltiDraw.Orange, UltiDraw.Black);
                                UltiDraw.DrawTranslateGizmo(Files[id_order_next].RootTr[framewidth * i].GetPosition(), Files[id_order_next].RootTr[framewidth * i].rotation, 0.1f);
                            }
                        }
                        UltiDraw.End();
                    }
                }
            }

            if (b_goal_data_exist || b_record)
            {
                Environment.Draw(Color.green, true, false);//Draw_Dynamic_Env(Environment.Points, Environment.Occupancies)
            }

            if (b_penetration)
            {
                //exp_utils.DerivedDraw(Frame);

            }
            
            if (b_probability)
            {
                //Debug.Log("values" + cur_prob_value);
                DrawGraph(cur_prob_value);
            }
        }

    }

    protected override void Close()
    {
        
    }

    private void DrawGraph(float[] Values)
    {
        UltiDraw.Begin();
        Color[] colors = UltiDraw.GetRainbowColors(Values.Length);
        Vector2 pivot = Rect.GetPosition();
        float radius = 0.2f * Rect.W;
        UltiDraw.DrawGUICircle(pivot, Rect.W * 1.05f, UltiDraw.Gold);
        UltiDraw.DrawGUICircle(pivot, Rect.W, UltiDraw.White);
        Vector2[] anchors = new Vector2[Values.Length];
        for (int i = 0; i < Values.Length; i++)
        {
            float step = (float)i / (float)Values.Length;
            anchors[i] = new Vector2((Rect.W - radius / 2f) * Screen.height / Screen.width * Mathf.Cos(step * 2f * Mathf.PI), (Rect.W - radius / 2f) * Mathf.Sin(step * 2f * Mathf.PI));
        }
        Vector2[] positions = new Vector2[Values.Length];
        for (int i = 0; i < Values.Length; i++)
        {
            int _index = 0;
            positions[_index] += Values[i] * anchors[i];
            _index += 1;
        }
        for (int i = 1; i < positions.Length; i++)
        {
            UltiDraw.DrawGUILine(pivot + positions[i - 1], pivot + positions[i], 0.1f * radius, UltiDraw.Black.Transparent((float)(i + 1) / (float)positions.Length));
        }
        for (int i = 0; i < anchors.Length; i++)
        {
            UltiDraw.DrawGUILine(pivot + positions.Last(), pivot + anchors[i], 0.1f * radius, colors[i].Transparent(Values[i]));
            UltiDraw.DrawGUICircle(pivot + anchors[i], Mathf.Max(0.5f * radius, Utility.Normalise(Values[i], 0f, 1f, 0.5f, 1f) * radius), Color.Lerp(UltiDraw.Black, colors[i], Values[i]));
        }
        UltiDraw.DrawGUICircle(pivot + positions.Last(), 0.5f * radius, UltiDraw.Purple);
        UltiDraw.End();
    }


    protected override void OnGUIDerived()
    {
        
        //if(b_exp)
        //    if(exp_utils.ContactJoints.Length> 0)
        //    {
        //        exp_utils.ContactJoints[0].Inspector(Frame);
        //        exp_utils.ContactJoints[1].Inspector(Frame);
        //    }
    }

    public Matrix4x4[][] ArrayConcat(Matrix4x4[][] org, Matrix4x4[][] input, int start, int end)
    {
        Matrix4x4[][] result = new Matrix4x4[org.GetLength(0) + (end-start+1)][];

        for (int id_o = 0; id_o < org.GetLength(0); id_o++)
        {
            result[id_o] = new Matrix4x4[org.GetLength(1)];
            result[id_o] = org[id_o];
        }
        for (int id = start , id_total = 0; id < (end+1); id++, id_total++)
        {
            result[org.GetLength(0) + id_total] = new Matrix4x4[input.GetLength(1)];
            result[org.GetLength(0) + id_total] = input[id];
        }

        return result;
    }

    
    [CustomEditor(typeof(playIndoorSceneDemo),true)]
    public class playIndoorSceneDemo_Editor : Editor
    {
        public playIndoorSceneDemo Target;
        SerializedProperty selectedOption;
        void Awake()
        {
            Target = (playIndoorSceneDemo)target;
            selectedOption = serializedObject.FindProperty("selectedOption");
            ////connection initialization : 여기서 실행되면 시작하자마자 쓰레드가 실행된다
            //connectionSetup();
        }
        
        public override void OnInspectorGUI()
        {
            Undo.RecordObject(Target, Target.name);
            Inspector();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(Target);
            }
        }

        public void Inspector()
        {
            Utility.SetGUIColor(UltiDraw.DarkGrey);
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                Utility.ResetGUIColor();

                Utility.SetGUIColor(UltiDraw.LightGrey);
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    Utility.ResetGUIColor();

                    
                    Target.SetCharacter((Actor)EditorGUILayout.ObjectField("Character", Target._actor, typeof(Actor), true));
                    Target.SetTargetCharacter((Actor)EditorGUILayout.ObjectField("Target Character", Target._actor_target, typeof(Actor), true));
                    
                    Target.Head = (GameObject)EditorGUILayout.ObjectField("Head", Target.Head, typeof(GameObject), true);
                    Target.LeftHand = (GameObject)EditorGUILayout.ObjectField("LeftHand", Target.LeftHand, typeof(GameObject), true);
                    Target.RightHand = (GameObject)EditorGUILayout.ObjectField("RightHand", Target.RightHand, typeof(GameObject), true);

                    Target.b_visualize = EditorGUILayout.Toggle("Visualize", Target.b_visualize);

                    // Motion Data Type Selection
                    EditorGUILayout.PropertyField(selectedOption, true);
                    serializedObject.ApplyModifiedProperties();

                    EditorGUILayout.BeginHorizontal();
                    //Target.directoryPath = EditorGUILayout.TextField("Folder", Target.directoryPath);
                    //--- get All BVH Files
                    if (Utility.GUIButton("Import", UltiDraw.DarkGrey, UltiDraw.White))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            Target.directoryPath = EditorUtility.OpenFolderPanel("BVH Folder", "", "Assets");
                            
                            Debug.Log("push the import button " + Target.directoryPath);
                            if (Target.selectedOption == MotionData_Type.BVH || Target.selectedOption == MotionData_Type.ALL)
                            {
                                Debug.Log("loadDirectory : " + Target._MotionData.LoadBVHDirectory(Target.directoryPath, "*.bvh"));
                                if (Target._MotionData.BVHFiles != null)
                                    Debug.Log("DirectoryPath : " + Target.directoryPath + " files : " + Target._MotionData.BVHFiles.Length);
                            }
                            else if (Target.selectedOption == MotionData_Type.MOTIONTEXT || Target.selectedOption == MotionData_Type.ALL)
                            {
                                Target._MotionData.Character = Target._actor;
                                Debug.Log("loadDirectory : " + Target._MotionData.LoadMotionTextDirectory(Target.directoryPath, "*_motion.txt"));
                                if (Target._MotionData.MotionTextFiles != null)
                                    Debug.Log("DirectoryPath : " + Target.directoryPath + " files : " + Target._MotionData.MotionTextFiles.Length);
                            }


                        };
                        

                    }
                    EditorGUILayout.EndHorizontal();
                    // && Application.isPlaying
                    if (Target._MotionData.BVHFiles != null)
                    {

                        using (new EditorGUILayout.VerticalScope("Box"))
                        {

                            //--- import BVH Data (Btn event)
                            if (Utility.GUIButton("Import all BVH", UltiDraw.DarkGrey, UltiDraw.White))
                            {
                                // for all data, inspector give a motion data inspector
                                Target.Files = new EnvMotionData[Target._MotionData.BVHFiles.Length];
                                // import Files
                                for (int AnimFile_Num = 0; AnimFile_Num < Target.Files.Length; AnimFile_Num++)
                                {
                                    Debug.Log("load BVH File : " + Target._MotionData.ImportBVHData(AnimFile_Num, 0.01f));
                                    Debug.Log("Motion Frames: " + Target._MotionData.BVHFiles[AnimFile_Num].Motion.GetLength(0));

                                    // import Motion

                                    Target.Files[AnimFile_Num] = ScriptableObject.CreateInstance<EnvMotionData>();
                                    Target.Files[AnimFile_Num].Motion = (Matrix4x4[][])Target._MotionData.BVHFiles[AnimFile_Num].Motion.Clone(); // Motion
                                    Target.Files[AnimFile_Num].Index = AnimFile_Num;
                                    Target.Files[AnimFile_Num].Order = AnimFile_Num;
                                    Target.Files[AnimFile_Num].FileName = Target._MotionData.BVHFiles[AnimFile_Num].FILE_Info.Name;
                                    Target.Files[AnimFile_Num].DirectoryName = Target._MotionData.BVHFiles[AnimFile_Num].FILE_Info.DirectoryName;
                                    Target.Files[AnimFile_Num].AddSequence(); // Sequence
                                    Target.Files[AnimFile_Num].GenerateRootTrajectory(0, Target.Files[AnimFile_Num].Motion.GetLength(0) - 1); // RootTr , Motion Wr
                                    Target.Files[AnimFile_Num].StartRootMat = Target.Files[AnimFile_Num].RootTr.First<Matrix4x4>();
                                    Target.Files[AnimFile_Num].EndRootMat = Target.Files[AnimFile_Num].RootTr.Last<Matrix4x4>();
                                    Target.Files[AnimFile_Num].Chair_mat = Target.Files[AnimFile_Num].RootTr.First<Matrix4x4>();
                                    Target.Files[AnimFile_Num].Desk_mat = Target.Files[AnimFile_Num].RootTr.First<Matrix4x4>();
                                    Target.Files[AnimFile_Num].GenerateRootRelative(); // seenbyChild Root realtive
                                    Target.Files[AnimFile_Num].b_uppercond = 0;
                                    Target.Files[AnimFile_Num].gui_color = UltiDraw.GetRandomColor();
                                    Debug.Log("EnvMotion Frames: " + Target.Files[AnimFile_Num].Motion.GetLength(0));
                                    //Target.Files[AnimFile_Num].Environment = new CylinderMap(Target.size, (int)Target.resolution, (int)Target.layers, false); 


                                }
                                Target.b_data_exist = true;
                                Target.b_goal_data_exist = false;

                            }
                        }
                    }

                    if (Target._MotionData.MotionTextFiles != null)
                    {

                        using (new EditorGUILayout.VerticalScope("Box"))
                        {

                            //--- import BVH Data (Btn event)
                            if (Utility.GUIButton("Import all MotionTextFiles", UltiDraw.DarkGrey, UltiDraw.White))
                            {
                                // for all data, inspector give a motion data inspector
                                Target.Files = new EnvMotionData[Target._MotionData.MotionTextFiles.Length];
                                // import Files
                                for (int AnimFile_Num = 0; AnimFile_Num < Target.Files.Length; AnimFile_Num++)
                                {
                                    Debug.Log("load BVH File : " + Target._MotionData.ImportMotionTextData(AnimFile_Num, 1.0f));
                                    Debug.Log("Motion Frames: " + Target._MotionData.MotionTextFiles[AnimFile_Num].Motion.GetLength(0));

                                    // import Motion

                                    Target.Files[AnimFile_Num] = ScriptableObject.CreateInstance<EnvMotionData>();
                                    Target.Files[AnimFile_Num].Motion = (Matrix4x4[][])Target._MotionData.MotionTextFiles[AnimFile_Num].Motion.Clone(); // Motion
                                    Target.Files[AnimFile_Num].Index = AnimFile_Num;
                                    Target.Files[AnimFile_Num].Order = AnimFile_Num;
                                    Target.Files[AnimFile_Num].FileName = Target._MotionData.MotionTextFiles[AnimFile_Num].FILE_Info.Name;
                                    Target.Files[AnimFile_Num].DirectoryName = Target._MotionData.MotionTextFiles[AnimFile_Num].FILE_Info.DirectoryName;
                                    Target.Files[AnimFile_Num].AddSequence(); // Sequence
                                    Target.Files[AnimFile_Num].GenerateRootTrajectory(0, Target.Files[AnimFile_Num].Motion.GetLength(0) - 1); // RootTr , Motion Wr
                                    Target.Files[AnimFile_Num].StartRootMat = Target.Files[AnimFile_Num].RootTr.First<Matrix4x4>();
                                    Target.Files[AnimFile_Num].EndRootMat = Target.Files[AnimFile_Num].RootTr.Last<Matrix4x4>();
                                    Target.Files[AnimFile_Num].Chair_mat = Target.Files[AnimFile_Num].RootTr.First<Matrix4x4>();
                                    Target.Files[AnimFile_Num].Desk_mat = Target.Files[AnimFile_Num].RootTr.First<Matrix4x4>();
                                    Target.Files[AnimFile_Num].GenerateRootRelative(); // seenbyChild Root realtive
                                    Target.Files[AnimFile_Num].b_uppercond = 0;
                                    Target.Files[AnimFile_Num].gui_color = UltiDraw.GetRandomColor();
                                    Debug.Log("EnvMotion Frames: " + Target.Files[AnimFile_Num].Motion.GetLength(0));
                                    //Target.Files[AnimFile_Num].Environment = new CylinderMap(Target.size, (int)Target.resolution, (int)Target.layers, false); 


                                }
                                Target.b_data_exist = true;
                                Target.b_goal_data_exist = false;

                            }
                        }
                    }


                    if (Target.b_data_exist || Target.b_goal_data_exist || Target.b_output_data_exist)
                    {
                        //-- generate motion or generate new root trajectory ( in playmode )
                        // --- Save Chair, Desk root Mat 
                        for (int e = 0; e < Target.Files.Length; e++)
                            Target.Files[e].inspector(Target);

                        //--Each Motion Play
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
                        EditorGUILayout.LabelField("Each Clip Play", GUILayout.Width(100f));
                        EditorGUILayout.LabelField("Select Order", GUILayout.Width(100f));
                        Target.FileIndex = EditorGUILayout.IntField(Target.FileIndex, GUILayout.Width(40f));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        Target.FileOrder = Target.findOrder(Target.Files, Target.FileIndex);
                                
                        if (Utility.GUIButton("update total Root Trajectory", UltiDraw.DarkGrey, UltiDraw.White))
                        {
                            Target.event_UpdateRootTR();
                        }
                        if (Utility.GUIButton("play animation", UltiDraw.DarkGrey, UltiDraw.White))
                        {
                            Target.event_PlayAnimation();
                        }
                        if (Utility.GUIButton("pause animation", UltiDraw.DarkGrey, UltiDraw.White))
                        {
                            Target.event_PauseAnimation();
                        }
                        if (Utility.GUIButton("write goal data", UltiDraw.DarkGrey, UltiDraw.White))
                        {
                            Target.event_WriteGoalData();
                        }

                        using (new EditorGUILayout.VerticalScope("Box"))
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
                                                        //EditorGUILayout.LabelField("b_connect", GUILayout.Width(100f));
                            Target.b_connect = EditorGUILayout.Toggle("b_connect", Target.b_connect);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();

                            Target.EndFrame = EditorGUILayout.IntField("End Frame:", Target.EndFrame);

                            if (Utility.GUIButton("load goal data and generate outputs", UltiDraw.DarkGrey, UltiDraw.White))
                            {
                                Target.event_LoadGoalData();
                            }
                            if (Utility.GUIButton("load output data", UltiDraw.DarkGrey, UltiDraw.White))
                            {
                                Target.event_LoadOutputMotionFile();
                            }
                            if (Utility.GUIButton("load GT data", UltiDraw.DarkGrey, UltiDraw.White))
                            {
                                Target.event_LoadGTMotionFile();
                            }
                        }
                        //
                        Target.b_draw_total = EditorGUILayout.Toggle("Visualize Total Root Trajectory", Target.b_draw_total);
                        Target.b_shows_gtEE = EditorGUILayout.Toggle("b_shows_gtEE", Target.b_shows_gtEE);
                        if (Target.b_play != true && Target.b_data_exist == true)
                            Target.Frame = EditorGUILayout.IntSlider(Target.Frame, 1, Target.Files[Target.FileOrder].RootTr.Length - 1);
                        if (Target.b_play != true && Target.b_output_data_exist == true)
                            Target.Frame = EditorGUILayout.IntSlider(Target.Frame, 1, Target.Files[Target.FileOrder].Motion.Length - 1);

                        if (Target.b_output_data_exist == true)
                        {
                            if (Utility.GUIButton("init Contact Joints", UltiDraw.DarkGrey, UltiDraw.White))
                            {
                                Debug.Log("ddd?1");
                                Target.event_DoContact();
                            }
                            if (Utility.GUIButton("update Contacts", UltiDraw.DarkGrey, UltiDraw.White))
                            {
                                Debug.Log("ddd?3");
                                Target.event_ExtractContact();

                                int nCount = 0; int nCount_Good=0;
                                for (int index=0; index < Target.EndFrame; index++)
                                {
                                    for (int i = 0; i < Target.exp_utils.ContactJoints.Length; i++)
                                    {
                                            if (Target.exp_utils.ContactJoints[i].GetContact(index) == 1f)
                                        {
                                                   
                                            if (Target.exp_utils.ContactJoints[i].Bone != 0 && Target.exp_utils.ContactJoints[i].Bone != 14 && Target.exp_utils.ContactJoints[i].Bone != 18 
                                                && Target.exp_utils.ContactJoints[i].Bone != 9 && Target.exp_utils.ContactJoints[i].Bone != 13 &&
                                                Target.exp_utils.ContactJoints[i].Bone != 16 && Target.exp_utils.ContactJoints[i].Bone != 17 && Target.exp_utils.ContactJoints[i].Bone != 20 && Target.exp_utils.ContactJoints[i].Bone != 21)
                                                nCount++;
                                            else
                                            {
                                                nCount_Good++;
                                            }
                                        }
                                    }
                                    Debug.Log("Frame:" + index + "count: " + nCount + "count Good: " + nCount_Good);
                                }
                            }
                            //if (Target.exp_utils.ContactJoints.Length > 0 && Target.b_penetration == true)
                            //{
                            //    Debug.Log("ddd?2");
                            //    for (int j=0; j < Target.exp_utils.ContactJoints.Length; j++)
                            //        Target.exp_utils.ContactJoints[j].Inspector(Target.Frame);
                            //}
                        }
                        Target.Rect.Inspector();


                    }
                        

                    
                    

                    //Target.io_class.Refresh();
                }
            }
         }
    }

}
