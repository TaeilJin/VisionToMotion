using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using UnityEditor.Experimental.GraphView;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System.Linq;
using static UnityEngine.GraphicsBuffer;
using static UnityEditor.Progress;
using System;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine.XR;
using UnityEngine.TextCore.Text;

public class FileReader
{
    public string[] ReadFilesInDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path, "*.txt");
            return files;
        }
        else
        {
            Debug.LogError($"Directory not found: {path}");
            return null;
        }
    }

    public List<float[]> Get_Float_Data(string path)
    {
        if (File.Exists(path))
        {
            Debug.Log($"I found Directory: {path}");
            string fileContent = File.ReadAllText(path);
            List<float[]> allFloatValues = new List<float[]>();
            string[] rows = fileContent.Split('\n');
            foreach (string row in rows)
            {
                List<float> importancelist = new List<float>();
                string[] componants = row.Split(' ');

                // Debug.Log(row);
                foreach (string valueString in componants)
                {
                    if (valueString != "")
                    {
                        if (float.TryParse(valueString, out float floatValue))
                            importancelist.Add(floatValue);
                        else
                            Debug.LogError($"Failed to parse '{valueString}' as float in file: {path}");
                    }
                }
                if (importancelist.Count > 0)
                    allFloatValues.Add(importancelist.ToArray());
            }
            return allFloatValues;
        }
        else
        {
            Debug.LogError($"Directory not found: {path}");
            return null;
        }
    }
}
public class TrainResult : RealTimeAnimation
{
    public Actor actor_source;
    public Motion_Capsule capsule_actor;
    // Using Motion Dataset 
    public MotionDataFile _MotionData;
    public MotionData_Type selectedOption;
   
    // Data Extraction
    public string LeftShoulder, RightShoulder, LeftHip, RightHip, LeftFoot, RightFoot, LeftToe, RightToe;
    public string Spine, LeftElbow, LeftHand, RightElbow, RightHand;
    public float character_height;
    public float th_length;
   
    //Data Visualization
    public List<Vector3> BaseSamplePoints;
    public List<Vector3> SpineSamplePoints;
    public List<Vector3> ArmSamplePoints;
    public List<Vector3> FootSamplePoints; 
    private float draw_threshold = 0.7f;//Edit
    private float hand_draw_threshold = 0.3f;//Edit

    // Environmental Data
    public GameObject Chair_Matrix;
    public GameObject Desk_Matrix;
    public Matrix4x4 Chair_RootMat;
    public Matrix4x4 Desk_RootMat;
    public GameObject EnvInspector;
    public ImporterClass import_class;
    
    // Offset
    GameObject Key_offset;
    GameObject Full_offset;
    GameObject JointObj;
    GameObject JointObj_2;

    // Play Setting
    private int StartFrame = 1;
    public bool b_play = false;
    public bool b_vis = false;
    public bool is_random = false;
    private bool is_start = false;
    public bool key_joint_visualize = false;
    public bool full_joint_visualize = false;

    // Data Info Setting
    public int key_joint_length;
    public int key_joint_information_length;
    public int full_joint_length;
    public int full_joint_information_length;
    public int Env_length;
    public int bodypart_length;

    // Model Result
    public string Model_Result_Dir;
    public string[] Env_importance_dir;
    public string[] Env_pos_dir;
    public string[] Generated_motion_dir;
    public string[] Optimized_motion_dir;

    public List<float[]> Env_Importance_data;
    public List<float[]> Env_Pos_data;
    public List<float[]> Generated_motion_data;
    public List<float[]> Optimized_motion_data;

    public Vector3[] Env_Pos;
    public Vector3 R_pos;
    public Quaternion R_rot;

    public void Start()
    {
        capsule_actor.actor_script = actor_source;
        Key_offset = new GameObject("key_joint_offset");
        Full_offset = new GameObject("full_joint_offset");
        JointObj = new GameObject("key_joint_Root");
        JointObj_2 = new GameObject("full_joint_Root");
    }
    public void RandomSelection()
    {
        // Sampling Environment
        int randomInt = UnityEngine.Random.Range(1, 11);

        string lastFolderName = Path.GetFileName(_MotionData.DirectoryName);
        //Debug.Log("file in folder " + lastFolderName);
        Transform parentObject = EnvInspector.transform.Find(lastFolderName);


        if (parentObject.childCount == 2)
        {
            Transform h0 = parentObject.GetChild(0);
            Transform h1 = parentObject.GetChild(1);

            int index = (randomInt / 1) % h0.transform.childCount;
            for (int i = 0; i < h0.transform.childCount; i++)
            {
                h0.transform.GetChild(i).gameObject.SetActive(false);
                h0.transform.GetChild(i).gameObject.SetActive(i == index);
            }
            for (int i = 0; i < h1.transform.childCount; i++)
            {
                h1.transform.GetChild(i).gameObject.SetActive(false);
                h1.transform.GetChild(i).gameObject.SetActive(i == index);
            }
        }

        else
        {
            int index = (randomInt / 1) % parentObject.transform.childCount;
            for (int i = 0; i < parentObject.transform.childCount; i++)
            {
                parentObject.transform.GetChild(i).gameObject.SetActive(false);
                parentObject.transform.GetChild(i).gameObject.SetActive(i == index);
            }
        }

    }
    public Vector3[] Change_importance_to_pos(float[] Current_Env_Pos)
    {
        Vector3[] vector3List = new Vector3[Env_length];

        if (Current_Env_Pos.Length == Env_length*3)
        {
            for (int i = 0; i < Current_Env_Pos.Length; i += 3)
            {
                Vector3 Posvector3 = new Vector3(Current_Env_Pos[i], Current_Env_Pos[i + 1], Current_Env_Pos[i + 2]);

                int index = i / 3;
                vector3List[index] = Posvector3;
            }
        }
        return vector3List;
    }
   
    protected override void Feed()
    {
        if (b_play)
        {
            // 첫 프레임에 첫번째 데이터를 불러온다.
            if (Frame == StartFrame)
            {
                if (selectedOption == MotionData_Type.ALL || selectedOption == MotionData_Type.FBX)
                    _MotionData.ImportFBXData(_MotionData.selectedData, _MotionData.scale);
                if (selectedOption == MotionData_Type.ALL || selectedOption == MotionData_Type.BVH)
                    _MotionData.ImportBVHData(_MotionData.selectedData, _MotionData.scale);
                if (selectedOption == MotionData_Type.ALL || selectedOption == MotionData_Type.MOTIONTEXT)
                    _MotionData.ImportMotionTextData(_MotionData.selectedData, _MotionData.scale);

                _MotionData.GenerateRootTrOFFile(actor_source);

                FileReader fileReader = new FileReader();
                Debug.Log(Env_importance_dir[_MotionData.selectedData]);

                //// Get the data ////
                Env_Importance_data = fileReader.Get_Float_Data(Env_importance_dir[_MotionData.selectedData]);
                Env_Pos_data = fileReader.Get_Float_Data(Env_pos_dir[_MotionData.selectedData]);
                Generated_motion_data = fileReader.Get_Float_Data(Generated_motion_dir[_MotionData.selectedData]);
                Optimized_motion_data = fileReader.Get_Float_Data(Optimized_motion_dir[_MotionData.selectedData]);

                Debug.Log($"importance is {Env_Importance_data.Count} EnvPos is {Env_Pos_data.Count}, Generated Motion is {Generated_motion_data.Count}, Optimized Motion is {Optimized_motion_data.Count}");
                R_pos = _MotionData.RootTrajectory[Frame].GetPosition();
                R_rot = _MotionData.RootTrajectory[Frame].GetRotation();

            }

            // 프레임이 끝나면 데이터 생성을 마무리 짓는다.
            if (Frame >= _MotionData.Motion.Length)
            {
                // 프레임이 다되면, 다음 데이터로 넘어간다.
                _MotionData.selectedData++;

                // Frame 을 처음 프레임으로 넘긴다.
                Frame = StartFrame;

                // 모든 데이터를 다 만들면 빠져나간다.
                if (_MotionData.selectedData >= _MotionData.Total_FileNumber)
                {
                    _MotionData.selectedData = 0;
                    b_play = false;
                    Debug.Log("finished all data files");
                    return;
                }

            }
            // 프레임은 계속해서 올라간다.
            else
            {
                // Updating current frame
                update_pose(Frame, _MotionData.Motion, actor_source);

                
                // Updating randomly selected furniture
                if (b_data && is_random)
                    RandomSelection();

                HashSet<int> SampleBaseIndices = new HashSet<int>();
                HashSet<int> SampleArmIndices = new HashSet<int>();
                HashSet<int> SampleFootIndices = new HashSet<int>();
                HashSet<int> SampleSpineIndices = new HashSet<int>();

                R_pos = _MotionData.RootTrajectory[Frame].GetPosition();
                R_rot = _MotionData.RootTrajectory[Frame].GetRotation();
                if (Frame - 1 >= 10 && Generated_motion_data.Count >= Frame - 1)
                {
                    if(Frame>=10)
                    {
                        float[] G_joint_pos_dir = new float[key_joint_length * key_joint_information_length];
                        float[] O_joint_pos_dir = new float[full_joint_length * full_joint_information_length];
                        Vector3[] target_key_pos = new Vector3[key_joint_length];
                        Vector3[] target_full_pos = new Vector3[full_joint_length];

                        ///// Generated key joint Motion Visualization /////
                        if (key_joint_visualize)
                        {
                            G_joint_pos_dir = Generated_motion_data[Frame - 11];
                            if (G_joint_pos_dir.Length == key_joint_length * key_joint_information_length)
                            {
                                if (JointObj != null)
                                {
                                    Vector3 key_R_pos = R_pos;
                                    key_R_pos += Key_offset.transform.position;
                                    JointObj.transform.position = key_R_pos;
                                    JointObj.transform.rotation = R_rot;
                                }

                                for (int j = 0; j < key_joint_length; j++)
                                {
                                    Vector3 pos = new Vector3(G_joint_pos_dir[key_joint_information_length * j + 0], G_joint_pos_dir[key_joint_information_length * j + 1], G_joint_pos_dir[key_joint_information_length * j + 2]);
                                    pos = JointObj.transform.TransformPoint(pos);
                                    target_key_pos[j] = pos;
                                }
                            }
                            else { Debug.Log("You get strange Key joint Motion Data!!"); }
                        }

                        ///// Optimized full body Motion Visualization /////
                        if (full_joint_visualize)
                        {
                            O_joint_pos_dir = Optimized_motion_data[Frame - 11];
                            if (O_joint_pos_dir.Length == full_joint_length * full_joint_information_length)
                            {
                                if (JointObj_2 != null)
                                {
                                    Vector3 full_R_pos = R_pos;
                                    full_R_pos += Full_offset.transform.position;
                                    JointObj_2.transform.position = full_R_pos;
                                    JointObj_2.transform.rotation = R_rot;
                                }
                                
                                for (int j = 0; j < full_joint_length; j++)
                                {
                                    Vector3 pos = new Vector3(O_joint_pos_dir[full_joint_information_length * j + 0], O_joint_pos_dir[full_joint_information_length * j + 1], O_joint_pos_dir[full_joint_information_length * j + 2]);
                                    pos = JointObj_2.transform.TransformPoint(pos);
                                    target_full_pos[j] = pos;
                                }
                            }
                            else { Debug.Log("You get strange full joint Motion Data!!"); }
                        }

                        capsule_actor.Key_motion_vector = target_key_pos;
                        capsule_actor.Full_motion_vector = target_full_pos;
                        capsule_actor.Update_Visualization();

                        ///// Environment Visualization /////
                        float[] Current_Env_Importance = new float[Env_length * bodypart_length];
                        float[] Current_Env_Pos = new float[Env_length * 3];
                        Current_Env_Importance = Env_Importance_data[Frame - 11];
                        Current_Env_Pos = Env_Pos_data[Frame - 11];

                        Env_Pos = Change_importance_to_pos(Current_Env_Pos);

                        for (int i = 0; i < Env_Pos.Length; i++)
                            Env_Pos[i] = JointObj_2.GetComponent<Transform>().TransformPoint(Env_Pos[i]);

                        if (Env_Pos.Length == Env_length && Current_Env_Importance.Length == Env_length*bodypart_length)
                        {
                            for (int s = 0; s < Env_Pos.Length; s++)
                            {
                                if (Current_Env_Importance[s] >= draw_threshold)
                                    SampleBaseIndices.Add(s);
                                if (Current_Env_Importance[s + Env_length] >= draw_threshold)
                                    SampleSpineIndices.Add(s);
                                if (Current_Env_Importance[s + Env_length * 2] >= hand_draw_threshold || Current_Env_Importance[s + Env_length * 3] >= hand_draw_threshold)
                                    SampleArmIndices.Add(s);
                                if (Current_Env_Importance[s + Env_length * 4] >= draw_threshold || Current_Env_Importance[s + Env_length * 5] >= draw_threshold)
                                    SampleFootIndices.Add(s);
                            }
                        }
                        else
                        { Debug.Log("You get strange Envrionment Data!!!"); }

                        BaseSamplePoints = new List<Vector3>();
                        SpineSamplePoints = new List<Vector3>();
                        ArmSamplePoints = new List<Vector3>();
                        FootSamplePoints = new List<Vector3>();

                        foreach (int p in SampleBaseIndices)
                            BaseSamplePoints.Add(Env_Pos[p]);
                        foreach (int p in SampleSpineIndices)
                            SpineSamplePoints.Add(Env_Pos[p]);
                        foreach (int p in SampleArmIndices)
                            ArmSamplePoints.Add(Env_Pos[p]);
                        foreach (int p in SampleFootIndices)
                            FootSamplePoints.Add(Env_Pos[p]);
                    }
                   
                }
                //Update Frame
                Frame++;
            }
        }
    }

    protected override void Read()
    {
        //throw new System.NotImplementedException();
    }

    protected override void OnGUIDerived()
    {
        //throw new System.NotImplementedException();
    }

    protected override void OnRenderObjectDerived()
    {
        if (b_vis)
        {
            UltiDraw.Begin();

            if (_MotionData != null)
            {
                int framewidth = 30;
                for (int i = 0; i < Mathf.RoundToInt(_MotionData.RootTrajectory.Length / framewidth); i++)
                {
                    UltiDraw.DrawWiredSphere(_MotionData.RootTrajectory[framewidth * i].GetPosition(), _MotionData.RootTrajectory[framewidth * i].rotation, 0.1f, UltiDraw.Orange, UltiDraw.Black);
                    UltiDraw.DrawTranslateGizmo(_MotionData.RootTrajectory[framewidth * i].GetPosition(), _MotionData.RootTrajectory[framewidth * i].rotation, 0.1f);

                }
            }
            UltiDraw.End();

            UltiDraw.Begin();

            for (int p = 0; p < FootSamplePoints.Count; p++)
                UltiDraw.DrawSphere(FootSamplePoints[p], Quaternion.identity, 0.1f, Color.cyan.Transparent(0.5f));
            for (int p = 0; p < SpineSamplePoints.Count; p++)
                UltiDraw.DrawSphere(SpineSamplePoints[p], Quaternion.identity, 0.1f, Color.blue.Transparent(0.5f));
            for (int p = 0; p < ArmSamplePoints.Count; p++)
                UltiDraw.DrawSphere(ArmSamplePoints[p], Quaternion.identity, 0.1f, Color.green.Transparent(0.5f));

            for (int p = 0; p < BaseSamplePoints.Count; p++)
                UltiDraw.DrawSphere(BaseSamplePoints[p], Quaternion.identity, 0.1f, Color.red.Transparent(0.5f));
            UltiDraw.End();
        }
    }

    protected override void Postprocess()
    {
        //throw new System.NotImplementedException();
    }

    protected override void Setup()
    {
        _MotionData = ScriptableObject.CreateInstance<MotionDataFile>();
        b_data = false;
        b_play = false;
    }
    protected override void Close()
    {
        //throw new System.NotImplementedException();
    }
    public void event_LoadRootDeskData(GameObject desk, string Name, string DirectoryName)
    {
        string name = Name + "_desk";


        if (ImporterClass.ImportTextRootData(DirectoryName, name))
        {
            Vector3 pos = new Vector3(ImporterClass.RootMat[0][0], ImporterClass.RootMat[0][1], ImporterClass.RootMat[0][2]);
            Quaternion quat = new Quaternion(ImporterClass.RootMat[0][3], ImporterClass.RootMat[0][4], ImporterClass.RootMat[0][5], ImporterClass.RootMat[0][6]);
            Vector3 sca = new Vector3(ImporterClass.RootMat[0][7], ImporterClass.RootMat[0][8], ImporterClass.RootMat[0][9]);
            desk.transform.SetPositionAndRotation(pos, quat);
            desk.transform.localScale = sca;
        }
        else
        {
            Debug.Log("there is no file " + name);
        }
    }
    public void event_LoadRootChairData(GameObject chair, string Name, string DirectoryName)
    {
        string name = Name + "_chair";
        if (ImporterClass.ImportTextRootData(DirectoryName, name))
        {
            Vector3 pos = new Vector3(ImporterClass.RootMat[0][0], ImporterClass.RootMat[0][1], ImporterClass.RootMat[0][2]);
            Quaternion quat = new Quaternion(ImporterClass.RootMat[0][3], ImporterClass.RootMat[0][4], ImporterClass.RootMat[0][5], ImporterClass.RootMat[0][6]);
            Vector3 sca = new Vector3(ImporterClass.RootMat[0][7], ImporterClass.RootMat[0][8], ImporterClass.RootMat[0][9]);
            chair.transform.SetPositionAndRotation(pos, quat);
            chair.transform.localScale = sca;
        }
        else
        {
            Debug.Log("there is no file " + name);
        }
    }

    [CustomEditor(typeof(TrainResult), true)]
    public class TrainResult_Editor : Editor
    {
        public TrainResult Target;
        SerializedProperty selectedOption;
        //public bool is_start = false;
        public void SetGameObject(GameObject go, bool ischair)
        {
            if (go != null)
            {
                if (ischair)
                    Target.Chair_Matrix = go;
                else
                    Target.Desk_Matrix = go;
            }
            else
            {
                if (ischair)
                    Target.Chair_Matrix = null;
                else
                    Target.Desk_Matrix = null;
            }
        }
       
        public void Awake()
        {
            Target = (TrainResult)target;
            selectedOption = serializedObject.FindProperty("selectedOption");
        }
        public override void OnInspectorGUI()
        {
            ///Undo.RecordObject(Target, Target.name);
            Inspector();
        }
        private void Inspector()
        {
            Utility.ResetGUIColor();
            Utility.SetGUIColor(UltiDraw.LightGrey);

            // Assigning Source Avatar
            EditorGUILayout.BeginVertical();
            Target.actor_source = (Actor)EditorGUILayout.ObjectField("Source Actor", Target.actor_source, typeof(Actor), true);
            EditorGUILayout.EndVertical();

            // Assigning Target Avatar
            EditorGUILayout.BeginVertical();
            Target.capsule_actor = (Motion_Capsule)EditorGUILayout.ObjectField("Target Actor", Target.capsule_actor, typeof(Motion_Capsule), true);
            EditorGUILayout.EndVertical();

            Target.character_height = EditorGUILayout.FloatField("character_height", Target.character_height);

            // Assigning Environment Data
            EditorGUILayout.BeginVertical();
            Target.EnvInspector = (GameObject)EditorGUILayout.ObjectField("Source Env", Target.EnvInspector, typeof(GameObject), true);
            EditorGUILayout.EndVertical();

            Target.Spine = EditorGUILayout.TextField("Spine", Target.Spine);

            EditorGUILayout.BeginHorizontal();
            Target.LeftShoulder = EditorGUILayout.TextField("LeftShoulder", Target.LeftShoulder);
            Target.RightShoulder = EditorGUILayout.TextField("RightShoulder", Target.RightShoulder);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(); 
            Target.LeftElbow = EditorGUILayout.TextField("LeftElbow", Target.LeftElbow);
            Target.RightElbow = EditorGUILayout.TextField("RightElbow", Target.RightElbow);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(); 
            Target.LeftHand = EditorGUILayout.TextField("LeftHand", Target.LeftHand);
            Target.RightHand = EditorGUILayout.TextField("RightHand", Target.RightHand);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            Target.LeftHip = EditorGUILayout.TextField("LeftHip", Target.LeftHip);
            Target.RightHip = EditorGUILayout.TextField("RightHip", Target.RightHip);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            Target.LeftFoot = EditorGUILayout.TextField("LeftFoot", Target.LeftFoot);
            Target.RightFoot = EditorGUILayout.TextField("RightFoot", Target.RightFoot);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            Target.LeftToe = EditorGUILayout.TextField("LeftToe", Target.LeftToe);
            Target.RightToe = EditorGUILayout.TextField("RightToe", Target.RightToe);
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space(); // 행간 추가

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Write Your Own Setting!!", "BoldLabel");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(); 
            Target.key_joint_length = EditorGUILayout.IntField("Key Joint Count", Target.key_joint_length);
            Target.key_joint_information_length = EditorGUILayout.IntField("Key Joint Info Count", Target.key_joint_information_length);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            Target.full_joint_length = EditorGUILayout.IntField("Full Joint Count", Target.full_joint_length);
            Target.full_joint_information_length = EditorGUILayout.IntField("Full Joint Info Count", Target.full_joint_information_length);
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            Target.Env_length = EditorGUILayout.IntField("Env Surface Point Count", Target.Env_length);
            Target.bodypart_length = EditorGUILayout.IntField("bodypart Count", Target.bodypart_length);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(); 
            Target.draw_threshold = EditorGUILayout.FloatField("Draw Threshold", Target.draw_threshold);
            Target.hand_draw_threshold = EditorGUILayout.FloatField("Draw hand Threshold", Target.hand_draw_threshold);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            Target.Model_Result_Dir = EditorGUILayout.TextField("Data Directory", Target.Model_Result_Dir);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(); // 행간 추가

            // Motion Data Type Selection
            EditorGUILayout.PropertyField(selectedOption, true);
            serializedObject.ApplyModifiedProperties();

            Target.b_vis = EditorGUILayout.Toggle("Env Visualize", Target.b_vis);
            Target.key_joint_visualize = EditorGUILayout.Toggle("Generated Motion Visualize", Target.key_joint_visualize);
            Target.full_joint_visualize = EditorGUILayout.Toggle("Retargeted Motion Visualize", Target.full_joint_visualize);
            Target.is_random = EditorGUILayout.Toggle("Randomize Furniture", Target.is_random);

            EditorGUILayout.Space(); // 행간 추가

            if (Target._MotionData != null)
            {
                Target._MotionData.LeftShoulder = Target.LeftShoulder;
                Target._MotionData.RightShoulder = Target.RightShoulder;
                Target._MotionData.LeftHip = Target.LeftHip;
                Target._MotionData.RightHip = Target.RightHip;
                if (Target.selectedOption == MotionData_Type.ALL || Target.selectedOption == MotionData_Type.MOTIONTEXT)
                {
                    // Motion Data inspector
                    Target._MotionData.MotionTextFile_inspector(Target.actor_source);
                    if (Target._MotionData.MotionTextFiles != null && Target._MotionData.MotionTextFiles.Length > 0)
                    {
                        Target._MotionData.Total_FileNumber = Target._MotionData.MotionTextFiles.Length;
                        Target.b_data = true;
                    }
                }
                if (Target.b_data)
                {
                    string lastFolderName = Path.GetFileName(Target._MotionData.DirectoryName);
                    Transform parentObject = Target.EnvInspector.transform.Find(lastFolderName);
                    if (parentObject != null)
                    {

                        if (parentObject.name == "hc_hd" || parentObject.name == "hcw_hdw" || parentObject.name == "lc_ld"
                           || parentObject.name == "lc_ld_a" || parentObject.name == "lcw_ldw")
                        {
                            parentObject.gameObject.SetActive(true);

                            Transform hcChild = parentObject.GetChild(0).transform;
                            if (hcChild != null)
                            {
                                hcChild.gameObject.SetActive(true);
                                Target.Chair_Matrix = hcChild.gameObject;
                            }

                            Transform hdChild = parentObject.GetChild(1).transform;
                            if (hdChild != null)
                            {
                                hdChild.gameObject.SetActive(true);
                                Target.Desk_Matrix = hdChild.gameObject;
                            }
                        }

                        if (parentObject.name == "board" || parentObject.name == "bed"
                           || parentObject.name == "hc" || parentObject.name == "hcw" || parentObject.name == "lc" || parentObject.name == "lcw" || parentObject.name == "lc_a"
                           || parentObject.name == "hd" || parentObject.name == "hdw" || parentObject.name == "ld" || parentObject.name == "ldw")
                        {
                            parentObject.gameObject.SetActive(true);
                            Target.Chair_Matrix = parentObject.gameObject;
                            Target.Desk_Matrix = parentObject.gameObject;
                        }
                    }
                }
            }
            
            //////////// EDIT /////////////////
            if (Utility.GUIButton(" Load importance ", Color.white, Color.red))
            {
                Target.is_start = true;
                FileReader fileReader = new FileReader();
                
                //Get data list
                Target.Env_importance_dir = fileReader.ReadFilesInDirectory(Target.Model_Result_Dir + "/Importance");
                Target.Env_pos_dir = fileReader.ReadFilesInDirectory(Target.Model_Result_Dir + "/env_Pos");
                Target.Generated_motion_dir = fileReader.ReadFilesInDirectory(Target.Model_Result_Dir + "/Generated_Motion");
                Target.Optimized_motion_dir = fileReader.ReadFilesInDirectory(Target.Model_Result_Dir + "/Retargeted_Motion");

                //Get data to float type
                Target.Env_Importance_data = fileReader.Get_Float_Data(Target.Env_importance_dir[Target._MotionData.selectedData]);
                Target.Env_Pos_data = fileReader.Get_Float_Data(Target.Env_pos_dir[Target._MotionData.selectedData]);
                Target.Generated_motion_data = fileReader.Get_Float_Data(Target.Generated_motion_dir[Target._MotionData.selectedData]);
                Target.Optimized_motion_data = fileReader.Get_Float_Data(Target.Optimized_motion_dir[Target._MotionData.selectedData]);

            }

            if (Target.Env_importance_dir.Length > 0 && Target.is_start==true)
            {
                EditorGUILayout.BeginHorizontal();
                int A = EditorGUILayout.Popup("Importance", Target._MotionData.selectedData, Target.Env_importance_dir.ToArray());
                EditorGUILayout.EndHorizontal();
            }   //EDIT
            
            if (Target.Env_pos_dir.Length > 0 && Target.is_start == true)
            {
                EditorGUILayout.BeginHorizontal();
                int A = EditorGUILayout.Popup("EnvPos", Target._MotionData.selectedData, Target.Env_pos_dir.ToArray());
                EditorGUILayout.EndHorizontal();
            }   //EDIT
            
            if (Target.Generated_motion_dir.Length > 0 && Target.is_start == true)
            {
                EditorGUILayout.BeginHorizontal();
                int A = EditorGUILayout.Popup("Generated Motion", Target._MotionData.selectedData, Target.Generated_motion_dir.ToArray());
                EditorGUILayout.EndHorizontal();
            }   //EDIT

            if (Target.Optimized_motion_dir.Length > 0 && Target.is_start == true)
            {
                EditorGUILayout.BeginHorizontal();
                int A = EditorGUILayout.Popup("Optimized Motion", Target._MotionData.selectedData, Target.Optimized_motion_dir.ToArray());
                EditorGUILayout.EndHorizontal();
            }   //EDIT


            if (Utility.GUIButton("Capsule Set", Color.white, Color.red))
            {
                Target.capsule_actor.InitializeCapsules();
            }

            if (Utility.GUIButton("reset & play animation", Color.white, Color.red))
            {
                Target.Frame = Target.StartFrame;
                Target._MotionData.selectedData = 0;
                Target.b_play = true;
                //Target.SetupSensors();
            }
            if (Target.b_play == false)
            {
                if (Utility.GUIButton("re-play animation", Color.white, Color.red))
                    Target.b_play = true;
            }
            if (Target.b_play == true)
            {
                if (Utility.GUIButton("pause animation", Color.white, Color.red))
                    Target.b_play = false;
            }
            if (Target.b_play != true && Target.b_data && Target._MotionData.Motion != null)
                Target.Frame = EditorGUILayout.IntSlider(Target.Frame, 1, Target._MotionData.Motion.Length - 1);

            //Root Object
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.EndHorizontal();
            // chair object
            SetGameObject((GameObject)EditorGUILayout.ObjectField("Chair", Target.Chair_Matrix, typeof(GameObject), true), true);
            // desk object
            SetGameObject((GameObject)EditorGUILayout.ObjectField("Desk", Target.Desk_Matrix, typeof(GameObject), true), false);

            if (Target.Chair_Matrix != null && Target.Desk_Matrix != null)
            {
                //-- event function
                EditorGUILayout.BeginHorizontal();
                // chair rootmat write
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                // chair rootmat load
                if (Utility.GUIButton("Load Chair Root", Color.green, UltiDraw.White))
                {
                    Target.event_LoadRootChairData(Target.Chair_Matrix, "root_mat", Target._MotionData.DirectoryName);
                    Target.Chair_RootMat = Target.Chair_Matrix.transform.GetWorldMatrix();
                }
                // chair rootmat load
                if (Utility.GUIButton("Load Desk Root", Color.green, UltiDraw.White))
                {
                    Target.event_LoadRootDeskData(Target.Desk_Matrix, "root_mat", Target._MotionData.DirectoryName);
                    Target.Desk_RootMat = Target.Desk_Matrix.transform.GetWorldMatrix();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}

