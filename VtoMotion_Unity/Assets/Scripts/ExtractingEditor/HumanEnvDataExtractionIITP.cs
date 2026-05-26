using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

public class HumanEnvDataExtractionIITP : RealTimeAnimation
{
    public Actor actor_source;
    // Using Motion Dataset 
    public MotionDataFile _MotionData;
    public MotionData_Type selectedOption;
    // Data Extraction
    public string LeftShoulder, RightShoulder, LeftHip, RightHip, LeftFoot, RightFoot, LeftToe, RightToe;
    public string Spine, LeftElbow, LeftHand, RightElbow, RightHand;
    public float character_height;
    public float samplepoints_len =0.9f;
    public float th_length = 0.2f;
    public List<Vector3> BaseSamplePoints;
    public List<Vector3> ArmSamplePoints;
    public List<Vector3> FootSamplePoints;
    // Environmental Data
    public GameObject Chair_Matrix;
    public GameObject Desk_Matrix;
    public Matrix4x4 Chair_RootMat;
    public Matrix4x4 Desk_RootMat;
    public GameObject EnvInspector;
    public ImporterClass import_class;

    private int StartFrame = 1;

    public bool b_play = false;
    public bool b_write = false;
    public bool b_vis = false;
    // public bool b_data = false;
    
    // Feature sensors
    public CylinderMap Environment;
    public CircleMap CircleMap;
    public JointCircleMap JointCircleMap;
    public JointContactMap JointContactMap;
    
    private float size = 2f;
    private float resolution = 5;
    private float layers = 10;
    public float[] JointLength;

    // Feature Saving
    private StringBuilder sb_record;
    private StreamWriter File_record;
    private string output_folder;
    public void SetupSensors()
    {
        Environment = new CylinderMap(size, (int)resolution, (int)layers, false);
        Debug.Log("Space " + Environment.Points.Length);
        CircleMap = new CircleMap(1f, (int)10,10, LayerMask.GetMask("Default", "Interaction"));
        //JointCircleMap = new JointCircleMap(5, actor_source.Bones.Length, LayerMask.GetMask("Default", "Interaction"));
        Debug.Log("Sample Points " + CircleMap.SamplePoints.Length);

        // sustain contact
        Actor.Bone rh = actor_source.FindBoneContains(RightHip);
        Actor.Bone lh = actor_source.FindBoneContains(LeftHip);
        int righthip = rh == null ? 0 : rh.Index;
        int lefthip = lh == null ? 0 : lh.Index;
        int pelvis = 0;
        Actor.Bone spine = actor_source.FindBoneContains(Spine);
        int spine_idx = spine == null ? 0 : spine.Index;

        // foot contact 
        Actor.Bone rs = actor_source.FindBoneContains(RightFoot);
        Actor.Bone ls = actor_source.FindBoneContains(LeftFoot);
        int rightfoot = rs == null ? 0 : rs.Index;
        int leftfoot = ls == null ? 0 : ls.Index;

        Actor.Bone rss = actor_source.FindBoneContains(RightToe);
        Actor.Bone lss = actor_source.FindBoneContains(LeftToe);
        int righttoe = rss == null ? 0 : rss.Index;
        int lefttoe = lss == null ? 0 : lss.Index;

        // sustain contact
        Actor.Bone rhand = actor_source.FindBoneContains(RightHand);
        Actor.Bone lhand = actor_source.FindBoneContains(LeftHand);
        Actor.Bone relbow = actor_source.FindBoneContains(RightElbow);
        Actor.Bone lelbow = actor_source.FindBoneContains(LeftElbow);

        int righthand = rhand == null ? 0 : rhand.Index;
        int lefthand = lhand == null ? 0 : lhand.Index;
        int rightelbow = relbow == null ? 0 : relbow.Index;
        int leftelbow = lelbow == null ? 0 : lelbow.Index;

        JointContactMap = new JointContactMap();
        JointContactMap.JointContactSensors = new JointContactMap.JointContactSensor[12];

        JointContactMap.JointContactSensors[0] = new JointContactMap.JointContactSensor(pelvis, actor_source.Bones[0].GetName(), 0.15f * character_height/1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[1] = new JointContactMap.JointContactSensor(spine_idx, spine.GetName(), 0.15f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));

        JointContactMap.JointContactSensors[2] = new JointContactMap.JointContactSensor(righthip,rh.GetName(), 0.1f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[3] = new JointContactMap.JointContactSensor(lefthip,lh.GetName(), 0.1f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        
        JointContactMap.JointContactSensors[4] = new JointContactMap.JointContactSensor(rightelbow, relbow.GetName(), 0.07f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[5] = new JointContactMap.JointContactSensor(righthand, rhand.GetName(), 0.07f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[6] = new JointContactMap.JointContactSensor(leftelbow, lelbow.GetName(), 0.07f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[7] = new JointContactMap.JointContactSensor(lefthand, lhand.GetName(), 0.07f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));

        JointContactMap.JointContactSensors[8] = new JointContactMap.JointContactSensor(rightfoot, rs.GetName(), 0.1f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[9] = new JointContactMap.JointContactSensor(leftfoot, ls.GetName(), 0.1f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[10] = new JointContactMap.JointContactSensor(righttoe, rss.GetName(), 0.02f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[11] = new JointContactMap.JointContactSensor(lefttoe, lss.GetName(), 0.02f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));


        JointLength = new float[actor_source.Bones.Length - 1];
    }
    public void CharacterLength(int index)
    {
        for (int c=0;  c< actor_source.Bones[index].Childs.Length; c++)
        {
            int child = actor_source.Bones[index].Childs[c];

            JointLength[index] = (actor_source.Bones[child].Transform.position - actor_source.Bones[index].Transform.position).magnitude;

            CharacterLength(child);
        }
    }
    public void DrawCharacterLength(int index)
    {
       
        for (int c = 0; c < actor_source.Bones[index].Childs.Length; c++)
        {
            int child = actor_source.Bones[index].Childs[c];

            UltiDraw.Begin();
            UltiDraw.DrawLine(actor_source.Bones[index].Transform.position, actor_source.Bones[child].Transform.position, Color.cyan);
            UltiDraw.End();
            DrawCharacterLength(child);
        }
       
    }
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
    private void ExtractFeatures(Actor _actor, Matrix4x4 pre_root, Matrix4x4 current_root, 
        out Vector3 root_vel, out float[] joint_position, out float[] occupancies, out float[] jointlength,
        out float f_contact_R, out float f_contact_L , out float[] joint_direction)
    {
        // parent ���� �� child 
        CharacterLength(0);
        jointlength = JointLength;

        // root velocity ���ϱ�
        root_vel = GetRootVelocity(pre_root, current_root);

        // environment sensing
        Vector3 root_pos = current_root.GetPosition();
        current_root.GetPosition().Set(root_pos.x, 0.0f, root_pos.y);
        Environment.Sense(current_root, LayerMask.GetMask("Default", "Interaction"));
        occupancies = Environment.Occupancies;

        joint_direction = new float[_actor.Bones.Length * 3];
        for (int j = 0; j < _actor.Bones.Length; j++)
        {
            Vector3 dir;
            if (j == 0)
                dir = _actor.Bones[j].Transform.position;
            else
                dir = _actor.Bones[j].Transform.position - _actor.Bones[j].GetParent().Transform.position;
            
            dir.Normalize();
            
            joint_direction[3 * j + 0] = dir.x;
            joint_direction[3 * j + 1] = dir.y;
            joint_direction[3 * j + 2] = dir.z;
            //dir.GetRelativeDirectionTo(_actor.Bones[j].GetParent().Transform.GetWorldMatrix());
        }
        
        // current root ���� �� pose ���
        joint_position = new float[_actor.Bones.Length * 3];
        for (int j = 0; j < _actor.Bones.Length; j++)
        {
            Vector3 position_j = _actor.Bones[j].Transform.position.GetRelativePositionTo(current_root);
            joint_position[3 * j + 0] = position_j.x;
            joint_position[3 * j + 1] = position_j.y;
            joint_position[3 * j + 2] = position_j.z;
        }

        // current root �� foot contact ���� ���
        Debug.Log("f contact of " + actor_source.Bones[JointContactMap.JointContactSensors[0].Bone].GetName());
        f_contact_R = JointContactMap.JointContactSensors[0].RegularContacts;
        Debug.Log("f contact of " + actor_source.Bones[JointContactMap.JointContactSensors[1].Bone].GetName());
        f_contact_L = JointContactMap.JointContactSensors[1].RegularContacts;
    }
    private void WriteFeatures(Actor _actor,float[] joint_position, Vector3 root_vel, float[] jointlength,
        float f_contact_R, float f_contact_L)
    {
        sb_record = new StringBuilder();
        // joint position
        for (int j = 0; j < _actor.Bones.Length; j++)
        {
            if(j == 0)
                sb_record = WritePosition(sb_record, new Vector3(joint_position[3 * j + 0], joint_position[3 * j + 1], joint_position[3 * j + 2]), true);
            else
                sb_record = WritePosition(sb_record, new Vector3(joint_position[3*j +  0], joint_position[3 * j + 1], joint_position[3 * j + 2]), false);
        }
        //// Environment Sensor
        //for (int e = 0; e < Environment.Occupancies.Length; e++)
        //    sb_record = WriteFloat(sb_record, Environment.Occupancies[e], false);
        // joint length
        for (int e = 0; e < jointlength.Length; e++)
            sb_record = WriteFloat(sb_record, jointlength[e], false);
        
        // foot contact
        sb_record = WriteFloat(sb_record, f_contact_R,false);
        sb_record = WriteFloat(sb_record, f_contact_L,false);
        // root velocity
        sb_record = WritePosition(sb_record, root_vel, false);
        
        Debug.Log(" total len" + " joint: " + joint_position.Length + " Env: " + Environment.Occupancies.Length + " joint length " + jointlength.Length + " f contact " + 2 + " vel " + 3 );

        File_record.WriteLine(sb_record.ToString());
        sb_record.Clear();
    }


    private void ExtractSampleFeatures(Actor _actor, Matrix4x4 pre_root, Matrix4x4 current_root,
        out Vector3 root_vel, out float[] joint_position, out float[] jointlength)
    {
        // parent ���� �� child 
        CharacterLength(0);
        jointlength = JointLength;

        // root velocity ���ϱ�
        root_vel = GetRootVelocity(pre_root, current_root);

        // current root ���� �� pose ���
        joint_position = new float[_actor.Bones.Length * 3];
        for (int j = 0; j < _actor.Bones.Length; j++)
        {
            Vector3 position_j = _actor.Bones[j].Transform.position.GetRelativePositionTo(current_root);
            joint_position[3 * j + 0] = position_j.x;
            joint_position[3 * j + 1] = position_j.y;
            joint_position[3 * j + 2] = position_j.z;
        }

        // interaction-based environments
        Debug.Log(" total len" + " joint: " + joint_position.Length +
            " Env Base: " + Environment.SpaceWeights_Base.Length +
            " Env Arm: " + Environment.SpaceWeights_Arm.Length +
            " Env Foot: " + Environment.SpaceWeights_Foot.Length +
            " Sample Points: " + CircleMap.SamplePoints.Length * 3 +
            " joint length " + jointlength.Length + " vel " + 3);


        //// 
        ////Debug.Log("Circle Height Maps " + CircleMap.Map.Length);
        ////Debug.Log("Circle Horizontal Maps " + CircleMap.HorizontalMap.Length);
        //joint_direction = new float[_actor.Bones.Length * CircleMap.SamplePoints.Length * 3];// * CircleMap.HorizontalPoints.Length * 3];

        //Vector3[] direction; float[] mag; float[] weight;
        //int id = _actor.FindBone(JointContactMap.JointContactSensors[4].Name).Index;

        //CircleMap.CalcRelativeWeights(_actor.Bones[id].Transform.position, CircleMap.SamplePoints, out direction, out mag, out CircleMap.SampleWeights);

        //CircleMap.SampleWeights = weight;

    }

    private void WriteSampleFeatures(Actor _actor, float[] joint_position, Vector3 root_vel, float[] jointlength,
       float f_contact_R, float f_contact_L)
    {
        sb_record = new StringBuilder();
        // joint position
        for (int j = 0; j < _actor.Bones.Length; j++)
        {
            if (j == 0)
                sb_record = WritePosition(sb_record, new Vector3(joint_position[3 * j + 0], joint_position[3 * j + 1], joint_position[3 * j + 2]), true);
            else
                sb_record = WritePosition(sb_record, new Vector3(joint_position[3 * j + 0], joint_position[3 * j + 1], joint_position[3 * j + 2]), false);
        }
        // Interaction-based Environment Sensor
        for (int e = 0; e < Environment.SpaceWeights_Base.Length; e++)
            sb_record = WriteFloat(sb_record, Environment.SpaceWeights_Base[e], false);

        for (int e = 0; e < Environment.SpaceWeights_Arm.Length; e++)
            sb_record = WriteFloat(sb_record, Environment.SpaceWeights_Arm[e], false);

        for (int e = 0; e < Environment.SpaceWeights_Foot.Length; e++)
            sb_record = WriteFloat(sb_record, Environment.SpaceWeights_Foot[e], false);

        // Sample Points
        for (int e = 0; e < CircleMap.SamplePoints.Length; e++)
            sb_record = WritePosition(sb_record, CircleMap.SamplePoints[e], false);

        // joint length
        for (int e = 0; e < jointlength.Length; e++)
            sb_record = WriteFloat(sb_record, jointlength[e], false);

        // foot contact
        //sb_record = WriteFloat(sb_record, f_contact_R, false);
        //sb_record = WriteFloat(sb_record, f_contact_L, false);
        // root velocity
        sb_record = WritePosition(sb_record, root_vel, false);

        //Debug.Log(" total len" + " joint: " + joint_position.Length + 
        //    " Env Base: " + Environment.SpaceWeights_Base.Length + 
        //    " Env Arm: " + Environment.SpaceWeights_Arm.Length +
        //    " Env Foot: " + Environment.SpaceWeights_Foot.Length +
        //    " Sample Points: " + CircleMap.SamplePoints.Length * 3 +
        //    " joint length " + jointlength.Length + " vel " + 3);


        File_record.WriteLine(sb_record.ToString());
        sb_record.Clear();
    }


    private void WriteDirection(Actor _actor, float[] jointdirection)
    {
        sb_record = new StringBuilder();
        // joint direction
        for (int j = 0; j < _actor.Bones.Length; j++)
        {
            if (j == 0)
                sb_record = WritePosition(sb_record, new Vector3(jointdirection[3 * j + 0], jointdirection[3 * j + 1], jointdirection[3 * j + 2]), true);
            else
                sb_record = WritePosition(sb_record, new Vector3(jointdirection[3 * j + 0], jointdirection[3 * j + 1], jointdirection[3 * j + 2]), false);
        }
        File_record.WriteLine(sb_record.ToString());
        sb_record.Clear();
    }
    
    public void RandomSelection()
    {
        // Sampling Environment
        int randomInt = Random.Range(1, 11);

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
    protected override void Feed()
    {
        if (b_play)
        {
            // ù �����ӿ� ù��° �����͸� �ҷ��´�.
            if(Frame == StartFrame)
            {
                if (selectedOption == MotionData_Type.ALL || selectedOption == MotionData_Type.FBX)
                    _MotionData.ImportFBXData(_MotionData.selectedData, _MotionData.scale);
                if (selectedOption == MotionData_Type.ALL || selectedOption == MotionData_Type.BVH)
                    _MotionData.ImportBVHData(_MotionData.selectedData, _MotionData.scale);
                if (selectedOption == MotionData_Type.ALL || selectedOption == MotionData_Type.MOTIONTEXT)
                    _MotionData.ImportMotionTextData(_MotionData.selectedData, _MotionData.scale);

                _MotionData.GenerateRootTrOFFile(actor_source);

                if (b_write)
                {
                    //string directoryPath = EditorUtility.OpenFolderPanel("BVH Folder", "", "Assets");
                    output_folder = _MotionData.DirectoryName + "\\Extraction";
                    if (!Directory.Exists(output_folder))
                    {
                        Debug.Log("Create Dataset Directory : " + output_folder);
                        Directory.CreateDirectory(output_folder);
                    }
                    Debug.Log("hmm " + _MotionData.FileName);
                    File_record = CreateFile(output_folder, _MotionData.FileName, false, ".txt");
                }
            }
            // �������� ������ ������ ������ ������ ���´�.
            if(Frame >= _MotionData.Motion.Length)
            {
                // �������� �ٵǸ�, ���� �����ͷ� �Ѿ��.
                _MotionData.selectedData++;

                if (b_write)
                {
                    // Saving Training Features
                    File_record.Close();
                }

                // Frame �� ó�� ���������� �ѱ��.
                Frame = StartFrame;

                // ��� �����͸� �� ����� ����������.
                if (_MotionData.selectedData >= _MotionData.Total_FileNumber)
                {
                    _MotionData.selectedData = 0;
                    b_play = false;
                    Debug.Log("finished all data files");
                    b_write = false;
                    return;

                }

            }
            // �������� ����ؼ� �ö󰣴�.
            else
            {
                // Updating current frame
                update_pose(Frame, _MotionData.Motion, actor_source);

                // Updating randomly selected furniture
                //if (b_data)
                //    RandomSelection();
                
                //Environment.Sense(_MotionData.RootTrajectory[Frame], LayerMask.GetMask("Default", "Interaction"));
                // Taeil modified : update space spheres
                Environment.Update(_MotionData.RootTrajectory[Frame]);

                // Spread sample points
                CircleMap.Sense(_MotionData.RootTrajectory[Frame]);

                // Update Interesting Joints
                HashSet<int> SampleBaseIndices = new HashSet<int>();
                HashSet<int> SampleArmIndices = new HashSet<int>();
                HashSet<int> SampleFootIndices = new HashSet<int>();
                
                for (int j = 0; j < JointContactMap.JointContactSensors.Length; j++)
                {
                    int target_joint_index = JointContactMap.JointContactSensors[j].Bone;
                    JointContactMap.JointSense(_MotionData.Motion[Frame - 1][target_joint_index], _MotionData.Motion[Frame][target_joint_index], j);

                    // Interesting Joint �� 

                    // Finding Sample points from Interacting Joint
                    // 0,1,2,3 : base 
                    Vector3[] direction; float[] Mag;
                    if (j == 0 || j == 1 || j ==2 || j == 3)
                    {
                        CircleMap.CalcRelativeWeightsWithBounds(JointContactMap.JointContactSensors[j].Cur_Pose.GetPosition(), CircleMap.SamplePoints,
                            (0.1f+character_height*0.4f), 0.1f,
                            out direction, out Mag, out CircleMap.SampleWeights);

                        // Finding Closer Sample Points
                        for(int p=0; p< CircleMap.SampleWeights.Length; p++)
                        {
                            if (CircleMap.SampleWeights[p] > samplepoints_len)
                            {
                                SampleBaseIndices.Add(p);
                            }
                        }

                    }
                    // 4,5,6,7 : hand 
                    if (j == 4 || j == 5 || j == 6 || j == 7)
                    {
                        if (JointContactMap.JointContactSensors[j].RegularContacts == 1f)
                        {
                            CircleMap.CalcRelativeWeightsWithBounds(JointContactMap.JointContactSensors[j].Cur_Pose.GetPosition(), CircleMap.SamplePoints,
                            (0.1f + character_height * 0.4f), 0.1f,
                            out direction, out Mag, out CircleMap.SampleWeights);

                            // Finding Closer Sample Points
                            for (int p = 0; p < CircleMap.SampleWeights.Length; p++)
                            {
                                if (CircleMap.SampleWeights[p] > samplepoints_len)
                                {
                                    SampleArmIndices.Add(p);
                                }
                            }
                        }
                    }
                    // 8,9,10,11 : foot
                    if (j == 8 || j == 9 || j == 10 || j == 11)
                    {
                        if(JointContactMap.JointContactSensors[j].RegularContacts == 1f) { 
                            CircleMap.CalcRelativeWeightsWithBounds(JointContactMap.JointContactSensors[j].Cur_Pose.GetPosition(), CircleMap.SamplePoints,
                                (0.1f + character_height * 0.4f), 0.1f,
                                out direction, out Mag, out CircleMap.SampleWeights);

                            // Finding Closer Sample Points
                            for (int p = 0; p < CircleMap.SampleWeights.Length; p++)
                            {
                                if (CircleMap.SampleWeights[p] > samplepoints_len)
                                {
                                    SampleFootIndices.Add(p);
                                }
                            }
                        }
                    }
                }
                //Debug.Log(" Base Samples " + SampleBaseIndices.Count);
                BaseSamplePoints = CylinderMap.GetSpaceWeight_fromInteraction(CircleMap.SamplePoints, Environment.References, new List<int>(SampleBaseIndices),
                    th_length,
                    out Environment.SpaceWeights_Base);
                //Debug.Log(" Arm Samples " + SampleArmIndices.Count);
                ArmSamplePoints = CylinderMap.GetSpaceWeight_fromInteraction(CircleMap.SamplePoints, Environment.References, new List<int>(SampleArmIndices),
                    th_length,
                    out Environment.SpaceWeights_Arm);
                //Debug.Log(" Foot Samples " + SampleFootIndices.Count);
                FootSamplePoints = CylinderMap.GetSpaceWeight_fromInteraction(CircleMap.SamplePoints, Environment.References, new List<int>(SampleFootIndices),
                    th_length,
                    out Environment.SpaceWeights_Foot);

                // Directional Weights of Sample points and Space Spheres
                Debug.Log(" total len" + " joint: " + actor_source.Bones.Length +
                 " Env Base: " + Environment.SpaceWeights_Base.Length +
                 " Env Arm: " + Environment.SpaceWeights_Arm.Length +
                 " Env Foot: " + Environment.SpaceWeights_Foot.Length +
                 " Sample Points: " + CircleMap.SamplePoints.Length * 3 +
                 " joint length " + (actor_source.Bones.Length - 1) + " vel " + 3);



                //for (int j = 0; j < actor_source.Bones.Length; j++)
                //JointCircleMap.JointSense(actor_source.Bones[15].Transform.localToWorldMatrix, 15);

                if (b_write)
                {
                    // Extracting Features
                    //Vector3 root_vel;
                    //float[] joint_position;
                    ////float[] occupancies;
                    //float[] joint_length;
                    ////float f_contact_R, f_contact_L;
                    //float[] joint_direction;
                    //ExtractSampleFeatures(actor_source, _MotionData.RootTrajectory[Frame - 1], _MotionData.RootTrajectory[Frame],
                    //    out root_vel, out joint_position, out joint_length, out joint_direction);

                    //WriteFeatures(actor_source, joint_position, root_vel, joint_length,
                    //    f_contact_R,f_contact_L);

                    //WriteDirection(actor_source, joint_direction);


                    // Extracting Features
                    Vector3 root_vel;
                    float[] joint_position;
                    //float[] occupancies;
                    float[] joint_length;
                    //float f_contact_R, f_contact_L;
                    ExtractSampleFeatures(actor_source, _MotionData.RootTrajectory[Frame - 1], _MotionData.RootTrajectory[Frame],
                        out root_vel, out joint_position, out joint_length);

                    WriteSampleFeatures(actor_source, joint_position, root_vel, joint_length, 1.0f, 1.0f);

                }
                // Updating Frame
                Frame++;
            }
            
        }
        else if(b_data && _MotionData.Motion != null){
            // Updating current frame
            update_pose(Frame, _MotionData.Motion, actor_source);
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

            Environment.Draw(Color.green, true, false);

            if (CircleMap != null && CircleMap.SampleWeights != null)
            {
                CircleMap.Draw();

                //UltiDraw.Begin();
                //for (int p = 0; p < BaseSamplePoints.Count; p++)
                //    UltiDraw.DrawSphere(BaseSamplePoints[p], Quaternion.identity, 0.15f, Color.red.Transparent(0.5f));
                //for (int p = 0; p < ArmSamplePoints.Count; p++)
                //    UltiDraw.DrawSphere(ArmSamplePoints[p], Quaternion.identity, 0.15f, Color.magenta.Transparent(0.5f));
                //for (int p = 0; p < FootSamplePoints.Count; p++)
                //    UltiDraw.DrawSphere(FootSamplePoints[p], Quaternion.identity, 0.15f, Color.cyan.Transparent(0.5f));
                //UltiDraw.End();
            }
            //for (int j = 0; j < actor_source.Bones.Length; j++)
            //JointCircleMap.Draw(actor_source.Bones[15].Transform.localToWorldMatrix, 15);

            JointContactMap.Draw();
            //for (int j = 0; j < JointContactMap.JointContactSensors.Length; j++)

            DrawCharacterLength(0);

            
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


    //--- Current
    public void event_WriteChairRoot(GameObject chair, string Name, string DirectoryName)
    {
        Matrix4x4 chair_root = chair.transform.GetWorldMatrix();

        Debug.Log("chair root of " + Name);
        if (Directory.Exists(DirectoryName))
        {

            string name = Name + "_chair";

            Debug.Log("wrtie chair root on " + name);
            File_record = CreateFile(DirectoryName, name, false, ".txt");
            sb_record = new StringBuilder();

            sb_record = WritePosition(sb_record, chair_root.GetPosition(), true);
            sb_record = WriteQuat(sb_record, chair_root.GetRotation(), false);

            File_record.WriteLine(sb_record.ToString());

            File_record.Close();
            sb_record.Clear();
        }
    }
    public void event_WriteDeskRoot(GameObject go_desk, string Name, string DirectoryName)
    {
        Matrix4x4 desk_root = go_desk.transform.GetWorldMatrix();

        Debug.Log("desk root of " + Name);
        if (Directory.Exists(DirectoryName))
        {
            string name = Name + "_desk";

            Debug.Log("wrtie desk root on " + name);

            File_record = CreateFile(DirectoryName, name, false, ".txt");
            sb_record = new StringBuilder();

            sb_record = WritePosition(sb_record, desk_root.GetPosition(), true);
            sb_record = WriteQuat(sb_record, desk_root.GetRotation(), false);

            File_record.WriteLine(sb_record.ToString());

            File_record.Close();
            sb_record.Clear();
        }
    }
    public void event_LoadRootDeskData(GameObject desk, string Name, string DirectoryName)
    {
        string name = Name + "_desk";

       
        if (ImporterClass.ImportTextRootData(DirectoryName, name))
        {
            Vector3 pos = new Vector3(ImporterClass.RootMat[0][0], ImporterClass.RootMat[0][1], ImporterClass.RootMat[0][2]);
            Quaternion quat = new Quaternion(ImporterClass.RootMat[0][3], ImporterClass.RootMat[0][4], ImporterClass.RootMat[0][5], ImporterClass.RootMat[0][6]);
            desk.transform.SetPositionAndRotation(pos, quat);

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
            chair.transform.SetPositionAndRotation(pos, quat);
        }
        else
        {
            Debug.Log("there is no file " + name);
        }
    }

    [CustomEditor(typeof(HumanEnvDataExtractionIITP), true)]
    public class HumanEnvDataExtractionIITP_Editor : Editor
    {
        public HumanEnvDataExtractionIITP Target;
        SerializedProperty selectedOption;

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
            Target = (HumanEnvDataExtractionIITP)target;
            selectedOption = serializedObject.FindProperty("selectedOption");
        }
        public override void OnInspectorGUI()
        {
            ///Undo.RecordObject(Target, Target.name);
            Inspector();
            //if (GUI.changed)
            //{
            //    EditorUtility.SetDirty(Target);
            //}
        }
        private void Inspector()
        {
            Utility.ResetGUIColor();
            Utility.SetGUIColor(UltiDraw.LightGrey);

            // Assigning Target Avatar
            EditorGUILayout.BeginVertical();
            Target.actor_source = (Actor)EditorGUILayout.ObjectField("Source Actor", Target.actor_source, typeof(Actor), true);
            EditorGUILayout.EndVertical();

            Target.character_height = EditorGUILayout.FloatField("character_height", Target.character_height);
            
            // Assigning Environment Data
            EditorGUILayout.BeginVertical();
            Target.EnvInspector = (GameObject)EditorGUILayout.ObjectField("Source Env", Target.EnvInspector, typeof(GameObject), true);
            EditorGUILayout.EndVertical();

            Target.Spine = EditorGUILayout.TextField("Spine", Target.Spine);

            EditorGUILayout.BeginHorizontal(); // ���߿� FBX, Motion TEXT File �� �� �� �ֵ��� inspector �� ������� �� ������ inspector �� �߰��ϵ��� �Ѵ�.
            Target.LeftShoulder = EditorGUILayout.TextField("LeftShoulder", Target.LeftShoulder);
            Target.RightShoulder = EditorGUILayout.TextField("RightShoulder", Target.RightShoulder);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(); // ���߿� FBX, Motion TEXT File �� �� �� �ֵ��� inspector �� ������� �� ������ inspector �� �߰��ϵ��� �Ѵ�.
            Target.LeftElbow = EditorGUILayout.TextField("LeftElbow", Target.LeftElbow);
            Target.RightElbow = EditorGUILayout.TextField("RightElbow", Target.RightElbow);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(); // ���߿� FBX, Motion TEXT File �� �� �� �ֵ��� inspector �� ������� �� ������ inspector �� �߰��ϵ��� �Ѵ�.
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


            // Motion Data Type Selection
            EditorGUILayout.PropertyField(selectedOption, true);
            serializedObject.ApplyModifiedProperties();

            Target.b_vis = EditorGUILayout.Toggle("Visualize", Target.b_vis);

            if (Target._MotionData != null)
            {

                Target._MotionData.LeftShoulder = Target.LeftShoulder;
                Target._MotionData.RightShoulder = Target.RightShoulder;
                Target._MotionData.LeftHip = Target.LeftHip;
                Target._MotionData.RightHip = Target.RightHip;

                if (Target.selectedOption == MotionData_Type.ALL || Target.selectedOption == MotionData_Type.FBX)
                {
                    // Motion Data inspector
                    Target._MotionData.FBX_inspector(Target.actor_source);
                    if (Target._MotionData.FBXFiles != null && Target._MotionData.FBXFiles.Length > 0)
                    {
                        Debug.Log("FBX" + Target._MotionData.FBXFiles.Length);
                        Target._MotionData.Total_FileNumber = Target._MotionData.FBXFiles.Length;
                        Target.b_data = true;
                    }
                }
                if (Target.selectedOption == MotionData_Type.ALL || Target.selectedOption == MotionData_Type.BVH)
                {
                    Target._MotionData.BVH_inspector();
                    if (Target._MotionData.BVHFiles != null && Target._MotionData.BVHFiles.Length > 0)
                    {
                        //Debug.Log("BVH" + Target._MotionData.BVHFiles.Length);
                        Target._MotionData.Total_FileNumber = Target._MotionData.BVHFiles.Length;
                        Target.b_data = true;
                    }
                }
                if (Target.selectedOption == MotionData_Type.ALL || Target.selectedOption == MotionData_Type.MOTIONTEXT)
                {
                    // Motion Data inspector
                    Target._MotionData.MotionTextFile_inspector(Target.actor_source);
                    if (Target._MotionData.MotionTextFiles != null && Target._MotionData.MotionTextFiles.Length > 0)
                    {
                        //Debug.Log("MotionTextFiles " + Target._MotionData.MotionTextFiles.Length);
                        Target._MotionData.Total_FileNumber = Target._MotionData.MotionTextFiles.Length;
                        Target.b_data = true;
                    }
                }

                //if (Target.b_data)
                //{
                //    string lastFolderName = Path.GetFileName(Target._MotionData.DirectoryName);
                //    //Debug.Log("file in folder " + lastFolderName);
                //    Transform parentObject = Target.EnvInspector.transform.Find(lastFolderName);
                //    if (parentObject != null)
                //    {

                //        if (parentObject.name == "hc_hd" || parentObject.name == "hcw_hdw" || parentObject.name == "lc_ld"
                //           || parentObject.name == "lc_ld_a" || parentObject.name == "lcw_ldw")
                //        {
                //            parentObject.gameObject.SetActive(true);
                            
                //            //Transform hcChild = parentObject.transform.Find("hc");

                //            Transform hcChild  = parentObject.GetChild(0).transform;
                //            if (hcChild != null)
                //            {
                //                hcChild.gameObject.SetActive(true);
                //                Target.Chair_Matrix = hcChild.gameObject;
                //            }

                //            Transform hdChild = parentObject.GetChild(1).transform;
                //            if (hdChild != null)
                //            {
                //                hdChild.gameObject.SetActive(true);
                //                Target.Desk_Matrix = hdChild.gameObject;
                //            }
                //        }

                //        if(parentObject.name == "board" || parentObject.name == "hc" || parentObject.name == "bed"
                //           || parentObject.name == "hcw" || parentObject.name == "hd" || parentObject.name == "hdw"
                //           || parentObject.name == "lc" || parentObject.name == "lcw" || parentObject.name == "lc_a")
                //        {
                //            parentObject.gameObject.SetActive(true);
                //            Target.Chair_Matrix = parentObject.gameObject;
                //            Target.Desk_Matrix = parentObject.gameObject;
                //        }
                        
                //    }
                //}
            }

            
            if (Utility.GUIButton("reset & play animation", Color.white, Color.red))
            {
                Target.Frame = Target.StartFrame;
                Target._MotionData.selectedData = 0;
                Target.b_play = true;
                Target.SetupSensors();
            }
            if (Target.b_play == false)
            {
                if (Utility.GUIButton("re-play animation", Color.white, Color.red))
                {
                    Target.b_play = true;
                }
            }
            if (Target.b_play == true)
            {
                if (Utility.GUIButton("pause animation", Color.white, Color.red))
                {
                    Target.b_play = false;
                }
            }
            if (Target.b_play != true && Target.b_data && Target._MotionData.Motion !=null)
                Target.Frame = EditorGUILayout.IntSlider(Target.Frame, 1, Target._MotionData.Motion.Length - 1);


            // chair object
            SetGameObject((GameObject)EditorGUILayout.ObjectField("Chair", Target.Chair_Matrix, typeof(GameObject), true), true);
            // desk object
            SetGameObject((GameObject)EditorGUILayout.ObjectField("Desk", Target.Desk_Matrix, typeof(GameObject), true), false);

            if (Target.Chair_Matrix != null && Target.Desk_Matrix != null)
            {
                
                
                //-- event function
                EditorGUILayout.BeginHorizontal();
                // chair rootmat write
                if (Utility.GUIButton("Write Chair Root", Color.green, UltiDraw.White))
                {
                    Target.event_WriteChairRoot(Target.Chair_Matrix, "root_mat", Target._MotionData.DirectoryName);
                    Target.Chair_RootMat = Target.Chair_Matrix.transform.GetWorldMatrix();
                }
                // desk rootmat write
                if (Utility.GUIButton("Write Desk Root", Color.green, UltiDraw.White))
                {
                    Target.event_WriteDeskRoot(Target.Desk_Matrix, "root_mat", Target._MotionData.DirectoryName);
                    Target.Desk_RootMat = Target.Desk_Matrix.transform.GetWorldMatrix();
                }
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


            Target.samplepoints_len = EditorGUILayout.FloatField("samplepoints_th", Target.samplepoints_len);
            Target.th_length = EditorGUILayout.FloatField("th_space_length", Target.th_length);

            // Contact Sensors
            if (Target.JointContactMap != null)
            {
                for (int j = 0; j < Target.JointContactMap.JointContactSensors.Length; j++)
                    Target.JointContactMap.JointContactSensors[j].Inspector();
            }
            if( Target.CircleMap != null)
            {
                Target.CircleMap.inspector();
            }

            // Target oriented Functions
            if (Utility.GUIButton(" Do Extracting ", Color.white, Color.red))
            {
                Target.Frame = Target.StartFrame;
                Target._MotionData.selectedData = 0;
                Target.b_play = true;
                Target.b_write = true;
                Target.SetupSensors();

                ////string directoryPath = EditorUtility.OpenFolderPanel("BVH Folder", "", "Assets");
                //Target.output_folder = Target._MotionData.DirectoryName + "\\Extraction";
                //if (!Directory.Exists(Target.output_folder))
                //{
                //    Debug.Log("Create Dataset Directory : " + Target.output_folder);
                //    Directory.CreateDirectory(Target.output_folder);
                //    Debug.Log("hmm " + Target._MotionData.FileName);
                //    Target.File_record = Target.CreateFile(Target.output_folder, Target._MotionData.FileName, false, ".txt");
                //}
            }
        }
        
    }
 }
