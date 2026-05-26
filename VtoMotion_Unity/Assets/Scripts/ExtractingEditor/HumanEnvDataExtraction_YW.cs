using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using UnityEditor.Experimental.GraphView;
using Unity.VisualScripting;
using UnityEditorInternal;
using System;
//using System.Diagnostics;

public class HumanEnvDataExtraction_YW : RealTimeAnimation
{
    public Actor actor_source;
    // Using Motion Dataset 
    public MotionDataFile _MotionData;
    public MotionData_Type selectedOption;
    // Data Extraction
    public string LeftShoulder, RightShoulder, LeftHip, RightHip, LeftFoot, RightFoot, LeftToe, RightToe;
    public string Spine, LeftElbow, LeftHand, RightElbow, RightHand;
    public float character_height;
    public float th_length;

    public List<Vector3> BaseSamplePoints;
    public List<Vector3> SpineSamplePoints;
    public List<Vector3> ArmSamplePoints;
    public List<Vector3> FootSamplePoints; //Edit
    private float draw_threshold = 0.7f;//Edit

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
    public bool is_random = true;

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
    private int Env_length = 540;

    public void SetupSensors()
    {
        Environment = new CylinderMap(size, (int)resolution, (int)layers, false);
        Debug.Log("Space " + Environment.Points.Length);
        CircleMap = new CircleMap(1f, (int)10, 10, LayerMask.GetMask("Default", "Interaction"));
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

        JointContactMap.JointContactSensors[0] = new JointContactMap.JointContactSensor(pelvis, actor_source.Bones[0].GetName(), 0.15f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[1] = new JointContactMap.JointContactSensor(spine_idx, spine.GetName(), 0.1f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));

        JointContactMap.JointContactSensors[2] = new JointContactMap.JointContactSensor(righthip, rh.GetName(), 0.1f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[3] = new JointContactMap.JointContactSensor(lefthip, lh.GetName(), 0.1f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));

        JointContactMap.JointContactSensors[4] = new JointContactMap.JointContactSensor(rightelbow, relbow.GetName(), 0.07f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[5] = new JointContactMap.JointContactSensor(righthand, rhand.GetName(), 0.07f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[6] = new JointContactMap.JointContactSensor(leftelbow, lelbow.GetName(), 0.07f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[7] = new JointContactMap.JointContactSensor(lefthand, lhand.GetName(), 0.07f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));

        JointContactMap.JointContactSensors[8] = new JointContactMap.JointContactSensor(rightfoot, rs.GetName(), 0.1f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[9] = new JointContactMap.JointContactSensor(leftfoot, ls.GetName(), 0.1f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[10] = new JointContactMap.JointContactSensor(righttoe, rss.GetName(), 0.07f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));
        JointContactMap.JointContactSensors[11] = new JointContactMap.JointContactSensor(lefttoe, lss.GetName(), 0.07f * character_height / 1.83f, 30, LayerMask.NameToLayer("Penetration"));

        JointLength = new float[actor_source.Bones.Length - 1];
    }
    public void CharacterLength(int index)
    {
        for (int c = 0; c < actor_source.Bones[index].Childs.Length; c++)
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
        out float f_contact_R, out float f_contact_L, out float[] joint_direction)
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
    private void WriteFeatures(Actor _actor, float[] joint_position, Vector3 root_vel, float[] jointlength,
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

        // joint length
        for (int e = 0; e < jointlength.Length; e++)
            sb_record = WriteFloat(sb_record, jointlength[e], false);

        // foot contact
        sb_record = WriteFloat(sb_record, f_contact_R, false);
        sb_record = WriteFloat(sb_record, f_contact_L, false);
        // root velocity
        sb_record = WritePosition(sb_record, root_vel, false);

        File_record.WriteLine(sb_record.ToString());
        sb_record.Clear();
    }

    public Vector3 GetForward(Matrix4x4 matrix)
    {
        return new Vector3(matrix[0, 2], matrix[1, 2], matrix[2, 2]).normalized;
    }
    public Vector3 GetUp(Matrix4x4 matrix)
    {
        return new Vector3(matrix[0, 1], matrix[1, 1], matrix[2, 1]).normalized;
    }

    private void ExtractSampleFeatures(Actor _actor, Matrix4x4 pre_root, Matrix4x4 current_root,
        out Vector3 root_vel, out Vector3[] joint_position, out float[] jointlength, out float[] Bodypart_Contact_Label,
        out Vector3[] EnvPos_R, out Vector3[] Velocity_Forward, out Vector3[] Velocity_Up, out int[] Env_Affordnace_Label, out float[] Contact_Furniture_List,
        out string[] Contact_Furniture_str)
    {

        // parent ���� �� child 
        CharacterLength(0);
        jointlength = JointLength;

        // root velocity ���ϱ�
        root_vel = GetRootVelocity(pre_root, current_root);

        // current root ���� �� pose ���
        joint_position = new Vector3[_actor.Bones.Length];
        Velocity_Forward = new Vector3[_actor.Bones.Length];
        Velocity_Up = new Vector3[_actor.Bones.Length];
        for (int j = 0; j < _actor.Bones.Length; j++)
        {
            Matrix4x4 position = _actor.Bones[j].Transform.GetWorldMatrix();

            Vector3 position_j = position.GetColumn(3);
            position_j = position_j.GetRelativePositionTo(current_root);
            joint_position[j] = position_j;
            Vector3 foward = GetForward(position);
            foward = foward.GetRelativeDirectionTo(current_root);
            Vector3 up = GetUp(position);
            up = up.GetRelativeDirectionTo(current_root);
            Velocity_Forward[j] = foward;
            Velocity_Up[j] = up;
        }

        EnvPos_R = new Vector3[CircleMap.SamplePoints.Length];
        for (int i = 0; i < CircleMap.SamplePoints.Length; i++)
        {
            Vector3 current_vec = CircleMap.SamplePoints[i];
            EnvPos_R[i] = CircleMap.SamplePoints[i].GetRelativePositionTo(current_root);

            for (int j = 0; j < 3; j++)
            {
                if (current_vec[j] == 10) EnvPos_R[i][j] = 10;
            }

        }

        //Edit
        Bodypart_Contact_Label = new float[6];
        for (int j = 0; j < JointContactMap.JointContactSensors.Length; j++)
        {
            float contat_or_not = JointContactMap.JointContactSensors[j].RegularContacts;
            if (contat_or_not == 1f)
            {
                if (j == 0 || j == 2 || j == 3) Bodypart_Contact_Label[0] = 1f;  //base
                else if (j == 1) Bodypart_Contact_Label[1] = 1f;                 //spine
                else if (j == 4 || j == 5) Bodypart_Contact_Label[2] = 1f;         //right hand
                else if (j == 6 || j == 7) Bodypart_Contact_Label[3] = 1f;      //left hand
                else if (j == 8 || j == 10) Bodypart_Contact_Label[4] = 1f;      //right foot
                else if (j == 9 || j == 11) Bodypart_Contact_Label[5] = 1f;      //left foot
            }
        }

        Contact_Furniture_str = new string[6];
        for (int j = 0; j < JointContactMap.JointContactSensors.Length; j++)
        {
            string str = JointContactMap.JointContactSensors[j].ContactFurniture;
            if (str != null)
            {
                if (j == 0 || j == 2 || j == 3) Contact_Furniture_str[0] = str;  //base
                else if (j == 1) Contact_Furniture_str[1] = str;                 //spine
                else if (j == 4 || j == 5) Contact_Furniture_str[2] = str;         //right hand
                else if (j == 6 || j == 7) Contact_Furniture_str[3] = str;      //left hand
                else if (j == 8 || j == 10) Contact_Furniture_str[4] = str;      //right foot
                else if (j == 9 || j == 11) Contact_Furniture_str[5] = str;
            }
        }

        int HMF = 6;
        Contact_Furniture_List = new float[6 * (HMF + 4)];

        for (int i = 0; i < 6; i++)
        {
            string current_F = Contact_Furniture_str[i];

            //// GROUND ////
            if (current_F == "Ground")
                Contact_Furniture_List[i * 10 + 0] = 1.0f;

            //// CHAIR ////
            else if (current_F == "lc" || current_F == "lcw" || current_F == "hc" || current_F == "hcw")
                Contact_Furniture_List[i * 10 + 1] = 1.0f;

            //// DESK ////
            else if (current_F == "ld" || current_F == "ldw" || current_F == "hd" || current_F == "hdw")
                Contact_Furniture_List[i * 10 + 2] = 1.0f;

            //// BOARD ////    
            else if (current_F == "board")
                Contact_Furniture_List[i * 10 + 3] = 1;

            //// BED ////    
            else if (current_F == "bed")
                Contact_Furniture_List[i * 10 + 4] = 1;

            //// NULL ////
            else
                Contact_Furniture_List[i * 10 + 5] = 1.0f;
        }

        for (int j = 0; j < JointContactMap.JointContactSensors.Length; j++)
        {
            bool base_int;
            bool spine_int;
            bool hand_int;
            bool foot_int;
            bool enable_int;
            int body_part_idx = -1;
            if (JointContactMap.JointContactSensors[j].RegularContacts == 1.0f)
            {
                base_int = JointContactMap.JointContactSensors[j].Base_int;
                spine_int = JointContactMap.JointContactSensors[j].Spine_int;
                hand_int = JointContactMap.JointContactSensors[j].Hand_int;
                foot_int = JointContactMap.JointContactSensors[j].Foot_int;
                enable_int = JointContactMap.JointContactSensors[j].Enable_int;

                if (j == 0 || j == 2 || j == 3) body_part_idx = 0; //base
                else if (j == 1) body_part_idx = 1;                //spine
                else if (j == 4 || j == 5) body_part_idx = 2;     //right hand
                else if (j == 6 || j == 7) body_part_idx = 3;     //left hand
                else if (j == 8 || j == 10) body_part_idx = 4;    //right foot
                else if (j == 9 || j == 11) body_part_idx = 5;     //left foot

                if (base_int) Contact_Furniture_List[10 * body_part_idx + 6] = 1;
                if (spine_int) Contact_Furniture_List[10 * body_part_idx + 7] = 1;
                if (hand_int) Contact_Furniture_List[10 * body_part_idx + 8] = 1;
                if (foot_int) Contact_Furniture_List[10 * body_part_idx + 9] = 1;
            }
        }

        Env_Affordnace_Label = new int[CircleMap.SamplePoints.Length * 10];
        for (int i = 0; i < JointContactMap.JointContactSensors.Length; i++)
        {
            string current_F = CircleMap.SamplePoints_Furniture[i];

            //// GROUND ////
            if (current_F == "Ground")
                Env_Affordnace_Label[i * 10 + 0] = 1;

            //// CHAIR ////
            else if (current_F == "lc" || current_F == "lcw" || current_F == "hc" || current_F == "hcw")
                Env_Affordnace_Label[i * 10 + 1] = 1;

            //// DESK ////
            else if (current_F == "ld" || current_F == "ldw" || current_F == "hd" || current_F == "hdw")
                Env_Affordnace_Label[i * 10 + 2] = 1;

            //// BOARD ////    
            else if (current_F == "board")
                Env_Affordnace_Label[i * 10 + 3] = 1;

            //// BED ////    
            else if (current_F == "bed")
                Env_Affordnace_Label[i * 10 + 4] = 1;

            //// NULL ////
            else
                Env_Affordnace_Label[i * 10 + 5] = 1;
        }


        for (int e = 0; e < CircleMap.SamplePoints.Length; e++)
        {
            bool base_aff = CircleMap.BaseInteraction[e];
            bool spine_aff = CircleMap.SpineInteraction[e];
            bool hand_aff = CircleMap.HandInteraction[e];
            bool foot_aff = CircleMap.FootInteraction[e];
            bool Enable_aff = CircleMap.EnableInteraction[e];

            if (base_aff)
                Env_Affordnace_Label[10 * e + 6] = 1;
            if (spine_aff)
                Env_Affordnace_Label[10 * e + 7] = 1;
            if (hand_aff)
                Env_Affordnace_Label[10 * e + 8] = 1;

            if (foot_aff)
                Env_Affordnace_Label[10 * e + 9] = 1;
        }


    }
    private void WriteSampleFeatures(Actor _actor, Vector3[] joint_position, Vector3 root_vel, float[] jointlength,
     float[][] Full_Weight, float[] Bodypart_Contact_Label, int[] Env_Affordance, Vector3[] EnvPos_R,
     Vector3[] Velocity_Forward, Vector3[] Velocity_Up, float[] Contact_Furniture_List, string[] Contact_Furniture_str)
    {
        sb_record = new StringBuilder();

        int[] KeyBone15 = { 0, 1, 3, 4, 6, 8, 9, 13, 15, 16, 17, 20, 22, 23, 24 };
        foreach (int j in KeyBone15)
        {
            if (j == 0)
            {
                sb_record = WritePosition(sb_record, joint_position[j], true);
                sb_record = WritePosition(sb_record, Velocity_Forward[j], false);
                sb_record = WritePosition(sb_record, Velocity_Up[j], false);
            }

            else
            {
                sb_record = WritePosition(sb_record, joint_position[j], false);
                sb_record = WritePosition(sb_record, Velocity_Forward[j], false);
                sb_record = WritePosition(sb_record, Velocity_Up[j], false);
            }

        }

        //root velocity
        sb_record = WritePosition(sb_record, root_vel, false);

        //Body Contact Label
        for (int c = 0; c < Bodypart_Contact_Label.Length; c++)
            sb_record = WriteFloat(sb_record, Bodypart_Contact_Label[c], false);

        for (int e = 0; e < Contact_Furniture_List.Length; e++)
            sb_record = WriteFloat(sb_record, Contact_Furniture_List[e], false);

        // Sample Points relative to root
        for (int e = 0; e < CircleMap.SamplePoints.Length; e++)
            sb_record = WritePosition(sb_record, EnvPos_R[e], false);

        //// Funiture Info Label
        for (int e = 0; e < CircleMap.SamplePoints.Length * 10; e++)
            sb_record = WriteFloat(sb_record, Env_Affordance[e], false);

        // Only Export when interaction label is 1
        for (int i = 2; i < Full_Weight.Length; i++)
        {
            if (Bodypart_Contact_Label[i] == 0f)
                Full_Weight[i] = new float[Env_length];
            else
            {
                for (int j = 0; j < Full_Weight[i].Length; j++)
                {
                    if (Contact_Furniture_str[i] != CircleMap.SamplePoints_Furniture[j])
                        Full_Weight[i][j] = 0f;
                    //else
                    //    count[i] += 1;
                }
            }
        }

        //Importance weight
        for (int i = 0; i < Full_Weight.Length; i++)
        {
            for (int e = 0; e < CircleMap.SamplePoints.Length; e++)
                sb_record = WriteFloat(sb_record, Full_Weight[i][e], false);
        }

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
        int randomInt = UnityEngine.Random.Range(1, 11);

        string lastFolderName = Path.GetFileName(_MotionData.DirectoryName);
        Transform parentObject;
        //Debug.Log("file in folder " + lastFolderName);
        parentObject = EnvInspector.transform.Find(lastFolderName);

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
            if (Frame == StartFrame)
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
            if (Frame >= _MotionData.Motion.Length)
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
                if (b_data && is_random)
                    RandomSelection();

                //Environment.Sense(_MotionData.RootTrajectory[Frame], LayerMask.GetMask("Default", "Interaction"));
                // Taeil modified : update space spheres
                Environment.Update(_MotionData.RootTrajectory[Frame]);

                // Spread sample points
                CircleMap.Sense(_MotionData.RootTrajectory[Frame]);

                // Update Interesting Joints
                HashSet<int> SampleBaseIndices = new HashSet<int>();
                HashSet<int> SampleArmIndices = new HashSet<int>();
                HashSet<int> SampleFootIndices = new HashSet<int>();
                HashSet<int> SampleSpineIndices = new HashSet<int>();//Edit

                //Edit
                float[] G_base = new float[Env_length];
                float[] G_spine = new float[Env_length];
                float[] G_left_h = new float[Env_length];
                float[] G_right_h = new float[Env_length];
                float[] G_left_f = new float[Env_length];
                float[] G_right_f = new float[Env_length];
                for (int j = 0; j < JointContactMap.JointContactSensors.Length; j++)
                {
                    int target_joint_index = JointContactMap.JointContactSensors[j].Bone;
                    // Debug.Log("J is" + j.ToString() +", Target_joint_index is " + target_joint_index.ToString());
                    JointContactMap.JointSense(_MotionData.Motion[Frame - 1][target_joint_index], _MotionData.Motion[Frame][target_joint_index], j);
                    //  Debug.Log("J is" + j.ToString() + _MotionData.Motion[Frame - 1][target_joint_index]);
                    // Interesting Joint �� 

                    // Finding Sample points from Interacting Joint

                    // 0,1,2,3 : base + spine
                    Vector3[] direction; float[] Mag;
                    if (j == 0 || j == 1 || j == 2 || j == 3) //����� �� if ���ص�?
                    {
                        CircleMap.CalcRelativeWeightsWithBounds(JointContactMap.JointContactSensors[j].Cur_Pose.GetPosition(), CircleMap.SamplePoints,
                            (0.1f + character_height * 0.25f), 0.1f,
                            out direction, out Mag, out CircleMap.SampleWeights);

                        float[] weight = CircleMap.SampleWeights;//Edit
                        if (j == 1)
                        {
                            for (int i = 0; i < weight.Length; i++)
                                G_spine[i] += weight[i];
                        }
                        else
                        {
                            for (int i = 0; i < weight.Length; i++)
                                G_base[i] += weight[i];
                        }

                    }

                    // 4,5,6,7 : hand 
                    if (j == 4 || j == 5 || j == 6 || j == 7)
                    {
                        CircleMap.CalcRelativeWeightsWithBounds(JointContactMap.JointContactSensors[j].Cur_Pose.GetPosition(), CircleMap.SamplePoints,
                        (0.1f + character_height * 0.25f), 0.1f,
                        out direction, out Mag, out CircleMap.SampleWeights);

                        float[] weight = CircleMap.SampleWeights;//Edit
                        if (j == 4 || j == 5)
                        {
                            for (int i = 0; i < weight.Length; i++)
                                G_right_h[i] += weight[i];
                        }
                        else
                        {
                            for (int i = 0; i < weight.Length; i++)
                                G_left_h[i] += weight[i];
                        }

                    }
                    // 8,9,10,11 : foot
                    if (j == 8 || j == 9 || j == 10 || j == 11)
                    {
                        CircleMap.CalcRelativeWeightsWithBounds(JointContactMap.JointContactSensors[j].Cur_Pose.GetPosition(), CircleMap.SamplePoints,
                            (0.1f + character_height * 0.25f), 0.1f,
                            out direction, out Mag, out CircleMap.SampleWeights);

                        float[] weight = CircleMap.SampleWeights;//Edit
                        if (j == 8 || j == 10)
                        {
                            for (int i = 0; i < weight.Length; i++)
                                G_right_f[i] += weight[i];
                        }
                        else
                        {
                            for (int i = 0; i < weight.Length; i++)
                                G_left_f[i] += weight[i];
                        }

                    }
                }

                for (int i = 0; i < G_base.Length; i++)
                {
                    G_base[i] /= 3.0f;
                    G_left_h[i] /= 2.0f;
                    G_right_h[i] /= 2.0f;
                    G_left_f[i] /= 2.0f;
                    G_right_f[i] /= 2.0f;
                }

                float[][] Full_Weight = new float[6][];
                //Full_Weight = new float[6][];
                Full_Weight[0] = G_base;
                Full_Weight[1] = G_spine;
                Full_Weight[2] = G_right_h;
                Full_Weight[3] = G_left_h;
                Full_Weight[4] = G_right_f;
                Full_Weight[5] = G_left_f;

                for (int s = 0; s < CircleMap.SamplePoints.Length; s++)
                {
                    if (G_base[s] >= draw_threshold)
                        SampleBaseIndices.Add(s);
                    if (G_spine[s] >= draw_threshold)
                        SampleSpineIndices.Add(s);
                    if (G_right_h[s] >= 0.4f || G_left_h[s] >= 0.4f)
                        SampleArmIndices.Add(s);
                    if (G_right_f[s] >= draw_threshold || G_left_f[s] >= draw_threshold)
                        SampleFootIndices.Add(s);

                }

                BaseSamplePoints = new List<Vector3>();
                SpineSamplePoints = new List<Vector3>();
                ArmSamplePoints = new List<Vector3>();
                FootSamplePoints = new List<Vector3>();


                foreach (int p in SampleBaseIndices)
                    BaseSamplePoints.Add(CircleMap.SamplePoints[p]);
                foreach (int p in SampleSpineIndices)
                    SpineSamplePoints.Add(CircleMap.SamplePoints[p]);
                foreach (int p in SampleArmIndices)
                    ArmSamplePoints.Add(CircleMap.SamplePoints[p]);
                foreach (int p in SampleFootIndices)
                    FootSamplePoints.Add(CircleMap.SamplePoints[p]);

                //////////////////////////////////////////////////////////////////////////////////////////////////////
                //for (int j = 0; j < actor_source.Bones.Length; j++)
                //    JointCircleMap.JointSense(actor_source.Bones[15].Transform.localToWorldMatrix, 15);


                if (b_write)
                {
                    Vector3 root_vel;
                    Vector3[] Velocity_Forward;
                    Vector3[] Velocity_Up;
                    Vector3[] joint_position;
                    float[] joint_length;
                    float[] Bodypart_Contact_Label;
                    float[] Contact_Furniture_List;
                    Vector3[] EnvPos_R;
                    int[] Env_Affordance;
                    string[] Contact_Furniture_str;
                    ExtractSampleFeatures(actor_source, _MotionData.RootTrajectory[Frame - 1], _MotionData.RootTrajectory[Frame],
                        out root_vel, out joint_position, out joint_length, out Bodypart_Contact_Label, out EnvPos_R,
                        out Velocity_Forward, out Velocity_Up, out Env_Affordance, out Contact_Furniture_List, out Contact_Furniture_str);
                    WriteSampleFeatures(actor_source, joint_position, root_vel, joint_length, Full_Weight, Bodypart_Contact_Label, Env_Affordance, EnvPos_R,
                        Velocity_Forward, Velocity_Up, Contact_Furniture_List, Contact_Furniture_str);
                }
                // Updating Frame
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

            //Environment.Draw(Color.green, true, false);

            if (CircleMap != null && CircleMap.SampleWeights != null)
            {
                //CircleMap.Draw(); //////////////////ORANGE CIRCLE////////////////

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


                JointContactMap.Draw();
                for (int j = 0; j < JointContactMap.JointContactSensors.Length; j++)
                    DrawCharacterLength(0);
            }

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
            sb_record = WriteScale(sb_record, chair_root.GetScale(), false);

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
            sb_record = WriteScale(sb_record, desk_root.GetScale(), false);

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

    [CustomEditor(typeof(HumanEnvDataExtraction_YW), true)]
    public class HumanEnvDataExtraction_YW_Editor : Editor
    {
        public HumanEnvDataExtraction_YW Target;
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
            Target = (HumanEnvDataExtraction_YW)target;
            selectedOption = serializedObject.FindProperty("selectedOption");
            //Target.is_random = true;

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

            EditorGUILayout.BeginVertical();
            Target.draw_threshold = EditorGUILayout.FloatField("Draw Threshold", Target.draw_threshold);
            EditorGUILayout.EndVertical();

            // Motion Data Type Selection
            EditorGUILayout.PropertyField(selectedOption, true);
            serializedObject.ApplyModifiedProperties();

            Target.b_vis = EditorGUILayout.Toggle("Visualize", Target.b_vis);
            Target.is_random = EditorGUILayout.Toggle("Randomize Furniture", Target.is_random);

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

                if (Target.b_data)
                {
                    string lastFolderName = Path.GetFileName(Target._MotionData.DirectoryName);
                    //  Debug.Log("file in folder " + lastFolderName);
                    Transform parentObject = Target.EnvInspector.transform.Find(lastFolderName);
                    if (parentObject != null)
                    {

                        if (parentObject.name == "hc_hd" || parentObject.name == "hcw_hdw" || parentObject.name == "lc_ld"
                           || parentObject.name == "lc_ld_a" || parentObject.name == "lcw_ldw")
                        {
                            parentObject.gameObject.SetActive(true);

                            //Transform hcChild = parentObject.transform.Find("hc");

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
            if (Target.b_play != true && Target.b_data && Target._MotionData.Motion != null)
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


            // Contact Sensors
            if (Target.JointContactMap != null)
            {
                for (int j = 0; j < Target.JointContactMap.JointContactSensors.Length; j++)
                    Target.JointContactMap.JointContactSensors[j].Inspector();
            }
            if (Target.CircleMap != null)
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
