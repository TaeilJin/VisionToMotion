using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Motion_Capsule : MonoBehaviour
{
    public Actor actor_script;
    public float capsuleRadius = 0.07f;
    public float handScale = 0.05f;
   // public bool Target_Full, Target_Key;

    public List<GameObject> S_capsuleObjects = new List<GameObject>();
    public List<GameObject> T_Full_capsuleObjects = new List<GameObject>();
    public List<GameObject> T_Key_capsuleObjects = new List<GameObject>();

    public GameObject Source_Root, Target_Full_Root, Target_Key_Root;

    public Vector3[] Key_motion_vector = new Vector3[15];
    public Vector3[] Full_motion_vector = new Vector3[25];

    private readonly int[][] Full_limbs = new int[][]
    {
        new int[] { 0, 1, 2, 3, 4, 5 }, // LeftFoot
        new int[] { 0, 6, 7, 8, 9, 10 }, // RightFoot
        new int[] { 0, 11, 12, 13 }, // Spine
        new int[] { 13, 14, 15, 16, 17 }, // LeftHand
        new int[] { 13, 18, 19, 20 }, // Head
        new int[] { 13, 21, 22, 23, 24 } // RightHand
    };

    private readonly int[][] Key_limbs = new int[][]
    {
        new int[] { 0, 1, 2, 3 }, // T_LeftFoot
        new int[] { 0, 4, 5, 6 }, // T_RightFoot
        new int[] { 0, 7 }, // T_Spine
        new int[] { 7, 8, 9, 10 }, // T_LeftHand
        new int[] { 7, 11 }, // T_Head
        new int[] { 7, 12, 13, 14 } // T_RightHand
    };
    void Start()
    {
        if (actor_script == null)
        {
            actor_script = GetComponent<Actor>();
        }
        Source_Root = new GameObject("Capsule_Source");
        Target_Full_Root = new GameObject("Capsule_Full_Target");
        Target_Key_Root = new GameObject("Capsule_Key_Target");

        InitializeCapsules();
        //handScale = capsuleRadius * 0.7f;
    }
    public void InitializeCapsules()
    {
        ClearCapsules(S_capsuleObjects);
        GameObject source_pelvis = new GameObject("Bone0");
        source_pelvis.transform.SetParent(Source_Root.transform);
        S_capsuleObjects.Add(source_pelvis);
        foreach (var limb in Full_limbs)
            CreateCapsulesForLimb(limb, S_capsuleObjects, false, false); // bool is_target, bool is_key

        ClearCapsules(T_Full_capsuleObjects);
        GameObject F_target_pelvis = new GameObject("Bone0");
        F_target_pelvis.transform.SetParent(Target_Full_Root.transform);
        T_Full_capsuleObjects.Add(F_target_pelvis);
        foreach (var T_limb in Full_limbs)
            CreateCapsulesForLimb(T_limb, T_Full_capsuleObjects, true, false); // bool is_target, bool is_key

        ClearCapsules(T_Key_capsuleObjects);
        GameObject K_target_pelvis = new GameObject("Bone0");
        K_target_pelvis.transform.SetParent(Target_Key_Root.transform);
        T_Key_capsuleObjects.Add(K_target_pelvis);
        foreach (var T_limb in Key_limbs)
            CreateCapsulesForLimb(T_limb, T_Key_capsuleObjects, true, true); // bool is_target, bool is_key
    }

    private void ClearCapsules(List<GameObject> capsuleList)
    {
        foreach (var capsule in capsuleList)
        {
            if (capsule != null) Destroy(capsule);
        }
        capsuleList.Clear();
    }

    private void CreateCapsulesForLimb(int[] indices, List<GameObject> capsuleList, bool is_target, bool is_key)
    {
        if (is_target)
        {
            for (int idx = 1; idx < indices.Length; idx++)
            {
                int parent_idx = indices[idx - 1];
                int my_idx = indices[idx];
                Vector3 parent_position = is_key ? Key_motion_vector[parent_idx] : Full_motion_vector[parent_idx];
                Vector3 my_position = is_key ? Key_motion_vector[my_idx] : Full_motion_vector[my_idx];
                CreateCapsuleBetweenPoints(parent_position, my_position, my_idx, capsuleList, is_target, is_key);
            }
        }

        else //Source
        {
            for (int idx = 1; idx < indices.Length; idx++)
            {
                int parent_idx = indices[idx - 1];
                int my_idx = indices[idx];
                Vector3 parent_position = actor_script.Bones[parent_idx].Transform.position;
                Vector3 my_position = actor_script.Bones[my_idx].Transform.position;
                CreateCapsuleBetweenPoints(parent_position, my_position, my_idx, capsuleList, is_target, is_key);
            }
        }
    }
    private void CreateCapsuleBetweenPoints(Vector3 start, Vector3 end, int idx, List<GameObject> capsuleList, bool is_target, bool is_key)
    {
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        UpdateCapsuleTransform(capsule, start, end, false);
        capsule.name = "Bone" + idx;
        Vector3 offset = end - start;


        if (!is_target) // Source
        {
            capsule.transform.SetParent(Source_Root.transform);
            capsuleList.Add(capsule);
            if (idx == 17 || idx == 24)
                AddHandCapsuleIfNeeded(idx, end, handScale, capsule, capsuleList, offset, false);
        }
        else
        {
            Color T_color = Color.blue;
            Material capsuleMaterial = new Material(Shader.Find("Standard"));
            capsuleMaterial.color = T_color;
            capsule.GetComponent<Renderer>().material = capsuleMaterial;

            if (is_key) //Key joint
            {
                capsule.transform.SetParent(Target_Key_Root.transform);
                capsuleList.Add(capsule);
                if (idx == 10 || idx == 14)
                    AddHandCapsuleIfNeeded(idx, end, handScale, capsule, capsuleList, offset, true);
            }

            else //Full joint
            {
                capsule.transform.SetParent(Target_Full_Root.transform);
                capsuleList.Add(capsule);
                if (idx == 17 || idx == 24)
                    AddHandCapsuleIfNeeded(idx, end, handScale, capsule, capsuleList, offset, true);
            }
        }
    }

    private void AddHandCapsuleIfNeeded(int idx, Vector3 end, float handScale, GameObject capsule, List<GameObject> capsuleList, Vector3 offset, bool is_target)
    {
        GameObject Hcapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Hcapsule.name = "Hand";
        Hcapsule.transform.localScale = new Vector3(capsuleRadius, handScale, capsuleRadius);
        Hcapsule.transform.localRotation = Quaternion.FromToRotation(Vector3.up, offset);
        Hcapsule.transform.position = end;
        Hcapsule.transform.SetParent(capsule.transform);
        // capsuleList.Add(Hcapsule);
        if (is_target)
        {
            Color T_color = Color.blue;
            Material capsuleMaterial = new Material(Shader.Find("Standard"));
            capsuleMaterial.color = T_color;
            Hcapsule.GetComponent<Renderer>().material = capsuleMaterial;
        }
    }
    public void Update_Visualization()
    {
        foreach (var limb in Full_limbs)
        {
            UpdateCapsulesForLimb(limb, S_capsuleObjects, false, false);  // bool is_target, bool is_key
        }

        foreach (var T_limb in Full_limbs)
        {
            UpdateCapsulesForLimb(T_limb, T_Full_capsuleObjects, true, false);  // bool is_target, bool is_key
        }

        foreach (var T_limb in Key_limbs)
        {
            UpdateCapsulesForLimb(T_limb, T_Key_capsuleObjects, true, true);  // bool is_target, bool is_key
        }
    }

    private void UpdateCapsuleTransform(GameObject capsule, Vector3 start, Vector3 end, bool is_hand)
    {
        Vector3 offset = end - start;
        Vector3 position = start + offset / 2.0f;
        capsule.transform.position = position;
        capsule.transform.rotation = Quaternion.FromToRotation(Vector3.up, offset);
        float length = offset.magnitude;
        capsule.transform.localScale = new Vector3(capsuleRadius, length / 2.0f + 0.03f, capsuleRadius);
        if (is_hand)
        {
            GameObject Hcapsule = capsule.transform.GetChild(0).gameObject;
            Debug.Log("Hand!");
            Hcapsule.transform.localScale = new Vector3(capsuleRadius, handScale, capsuleRadius);
            Hcapsule.transform.localRotation = Quaternion.FromToRotation(Vector3.up, offset);
            Hcapsule.transform.position = end;
        }
    }
    private void UpdateCapsulesForLimb(int[] indices, List<GameObject> capsuleList, bool is_target, bool is_key)
    {
        for (int idx = 1; idx < indices.Length; idx++)
        {
            int parent_idx = indices[idx - 1];
            int my_idx = indices[idx];
            bool is_hand = false;
            Vector3 parent_position;
            Vector3 my_position;

            if (is_target)
            {
                parent_position = is_key ? Key_motion_vector[parent_idx] : Full_motion_vector[parent_idx];
                my_position = is_key ? Key_motion_vector[my_idx] : Full_motion_vector[my_idx];
            }
            else
            {
                parent_position = actor_script.Bones[parent_idx].Transform.position;
                my_position = actor_script.Bones[my_idx].Transform.position;
            }


            if (is_key)
            {
                if (idx == 17 || idx == 24)
                    is_hand = true;
            }
            else
            {
                if (idx == 10 || idx == 14)
                    is_hand = true;
            }
            UpdateCapsuleTransform(capsuleList[my_idx], parent_position, my_position, is_hand);

        }

    }
}
