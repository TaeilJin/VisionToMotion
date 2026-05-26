using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class DAFNet_FeatureExtraction : ScriptableObject
{
    public CylinderMap Environment;
    private float size = 2f;
    private float resolution = 8;
    private float layers = 15;
    public bool b_connect;
    public bool b_extract;
  
    [Serializable]
    public class DAFNet_DataPacket
    {
        public string text_indicator;
        public int index;
        public float[] joint_position_GT;
        public float[] root_velocity;
        public float[] environment;
        public string ee_indicator;
    }

    public void Setup()
    {
        // environment
        Environment = new CylinderMap(size, (int)resolution, (int)layers, false);
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
    public void extract_goal_feature(Actor _actor, Matrix4x4 pre_root, Matrix4x4 current_root, out Vector3 root_vel, out float[] joint_position)
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
        joint_position = new float[_actor.Bones.Length * 3];
        for (int j = 0; j < _actor.Bones.Length; j++)
        {
            Vector3 position_j = _actor.Bones[j].Transform.position.GetRelativePositionTo(current_root);
            joint_position[3 * j + 0] = position_j.x;
            joint_position[3 * j + 1] = position_j.y;
            joint_position[3 * j + 2] = position_j.z;
        }


    }
    public string send_goal_feature(float[] joint_position, Vector3 root_velocity)
    {
        float[] root_vel = new float[3];
        // Environment Sensor
        root_vel[0] = root_velocity.x; root_vel[1] = root_velocity.y; root_vel[2] = root_velocity.z;
        // 데이터 구성
        DAFNet_DataPacket dataToSend = new DAFNet_DataPacket
        {
            text_indicator = "DAFNet_Input",
            ee_indicator = "None",
            joint_position_GT = joint_position,
            root_velocity = root_vel,
            environment = Environment.Occupancies
        };
        // 데이터를 JSON 문자열로 직렬화하여 전송
        string jsonData = JsonUtility.ToJson(dataToSend);

        return jsonData;
    }
    public string send_index_Play(int index)
    {
        // 데이터 구성
        DAFNet_DataPacket dataToSend = new DAFNet_DataPacket
        {
            text_indicator = "DAFNet",
            ee_indicator = "None",
            index = index,
            joint_position_GT = null,
            root_velocity = null,
            environment = null
        };
        // 데이터를 JSON 문자열로 직렬화하여 전송
        string jsonData = JsonUtility.ToJson(dataToSend);

        return jsonData;
    }

    public void update_matrix(float[] array, Actor _actor ,out float[] prob)
    {
        prob = new float[10];
        for(int j=0; j < _actor.Bones.Length; j++)
        {
            if(j == 0)
            {
                Vector3 pos = new Vector3(array[0], array[1], array[2]);
                _actor.Bones[0].Transform.position = pos;
            }
            Quaternion quat = new Quaternion(array[3 + 4 * j + 0], array[3 + 4 * j + 1], array[3 + 4 * j + 2], array[3 + 4 * j + 3]);
            _actor.Bones[j].Transform.localRotation = quat;

        }

        for (int p = 0; p < 10; p++)
            prob[p] = array[3 + 22 * 4 + p];
    }
    public void Inspector()
    {
        if (Utility.GUIButton("DAFNet: extract Features ", Color.white, Color.yellow))
        {
            b_extract = true;
        }
    }
}
