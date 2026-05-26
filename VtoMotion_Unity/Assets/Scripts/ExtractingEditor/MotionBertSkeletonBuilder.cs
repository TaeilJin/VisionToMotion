using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.VFX;

public class MotionBertSkeletonBuilder : ScriptableObject
{
    public Actor actor;
    public Dictionary<int, int> boneMap = new Dictionary<int, int>();

    public int[,] motionBertBones = new int[,] {
        {0, 7},   // Hip -> Spine
        {7, 8},   // Spine -> Neck
        {8, 9},   // Neck -> Nose
        {8, 10},  // Neck -> Head
        {9, 10},  // Nose -> Head

        {0, 1},   // Hip -> RHip
        {1, 2},   // RHip -> RKnee
        {2, 3},   // RKnee -> RAnkle

        {0, 4},   // Hip -> LHip
        {4, 5},   // LHip -> LKnee
        {5, 6},   // LKnee -> LAnkle

        {8, 11},  // Neck -> LShoulder
        {11, 12}, // LShoulder -> LElbow
        {12, 13}, // LElbow -> LWrist

        {8, 14},  // Neck -> RShoulder
        {14, 15}, // RShoulder -> RElbow
        {15, 16}  // RElbow -> RWrist
    };

    public void BuildActorSkeleton()
    {
        GameObject root = new GameObject("motion_bert_actor");
        Transform[] joints = new Transform[17];
        for (int i = 0; i < 17; i++)
        {
            GameObject joint = new GameObject($"joint_{i}");
            joint.transform.parent = root.transform;
            joint.transform.localPosition = Vector3.zero;
            joint.transform.localRotation = Quaternion.identity;
            joints[i] = joint.transform;
        }

        // parent-child ����
        for (int i = 0; i < motionBertBones.GetLength(0); i++)
        {
            int parent = motionBertBones[i, 0];
            int child = motionBertBones[i, 1];
            joints[child].parent = joints[parent];
            joints[child].localPosition = Vector3.up * 0.05f; // ������ ����
            joints[child].localRotation = Quaternion.identity;
        }

        // Actor�� ����
        actor = root.AddComponent<Actor>();
        actor.ExtractSkeleton(joints); // Transform ������ ������� Bone[] ����
        BuildMapping(actor);
        Debug.Log($"Created procedural Actor skeleton with {actor.Bones.Length} bones");
    }

    // �ʱ� 1ȸ ���� ����
    public void BuildMapping(Actor actor)
    {
        boneMap.Clear();
        for (int i = 0; i < 17; i++)
        {
            Actor.Bone b = Array.Find(actor.Bones, x => x.GetName() == $"joint_{i}");
            if (b != null)
            {
                boneMap[i] = b.Index;
            }
            else
            {
                Debug.LogWarning($"joint_{i} not found in actor.");
            }
        }
        Debug.Log($"Bone mapping table built: {boneMap.Count} entries");
    }

    public void BuildHumanoidMapping(Actor actor, Animator animator)
    {
        boneMap.Clear();

        if (actor == null)
        {
            Debug.LogError("[BlazePoseSkeletonBuilder] Actor is null. Cannot build humanoid mapping.");
            return;
        }

        if (animator == null)
        {
            Debug.LogError("[BlazePoseSkeletonBuilder] Animator is null. Please assign a Humanoid character with Animator.");
            return;
        }

        // Actor의 Bone 배열에서 Transform -> Bone.Index 빠른 lookup 테이블 만들기
        Dictionary<Transform, int> transformToBoneIndex = new Dictionary<Transform, int>();
        foreach (var bone in actor.Bones)
        {
            if (bone.Transform != null && !transformToBoneIndex.ContainsKey(bone.Transform))
            {
                transformToBoneIndex[bone.Transform] = bone.Index;
            }
        }

        // 내부에서 쓸 헬퍼 함수
        void MapBlazeToHumanoid(int blazeIndex, HumanBodyBones unityBone)
        {
            Transform t = animator.GetBoneTransform(unityBone);
            if (t == null)
            {
                Debug.LogWarning($"[BlazePoseSkeletonBuilder] Humanoid bone {unityBone} not found on {animator.name}");
                return;
            }

            int boneIndex;
            if (!transformToBoneIndex.TryGetValue(t, out boneIndex))
            {
                // Transform으로 못 찾았으면 이름으로 한 번 더 시도
                Actor.Bone found = Array.Find(actor.Bones, x => x.Transform == t || x.GetName() == t.name);
                if (found == null)
                {
                    Debug.LogWarning($"[BlazePoseSkeletonBuilder] Actor bone for Transform {t.name} not found.");
                    return;
                }

                boneIndex = found.Index;
            }

            boneMap[blazeIndex] = boneIndex;
        }

        // 실제 매핑 규칙: BlazePose index → Humanoid 본 
        // 머리
        MapBlazeToHumanoid(10, HumanBodyBones.Head);

        // 상체 / 팔
        MapBlazeToHumanoid(11, HumanBodyBones.LeftUpperArm);
        MapBlazeToHumanoid(12, HumanBodyBones.LeftLowerArm);
        MapBlazeToHumanoid(13, HumanBodyBones.LeftHand);
        MapBlazeToHumanoid(14, HumanBodyBones.RightUpperArm);
        MapBlazeToHumanoid(15, HumanBodyBones.RightLowerArm);
        MapBlazeToHumanoid(16, HumanBodyBones.RightHand);

        // 하체
        MapBlazeToHumanoid(4, HumanBodyBones.LeftUpperLeg);
        MapBlazeToHumanoid(1, HumanBodyBones.RightUpperLeg);
        MapBlazeToHumanoid(5, HumanBodyBones.LeftLowerLeg);
        MapBlazeToHumanoid(2, HumanBodyBones.RightLowerLeg);
        MapBlazeToHumanoid(6, HumanBodyBones.LeftFoot);
        MapBlazeToHumanoid(3, HumanBodyBones.RightFoot);
        // MapBlazeToHumanoid(31, HumanBodyBones.LeftToes);
        // MapBlazeToHumanoid(32, HumanBodyBones.RightToes);

        // 가슴/목/척추 쪽도 머리 기준으로 더 매핑 가능 (예: nose → Spine, Chest 등)
        // -> 보정으로 처리
        // MapBlazeToHumanoid(7, HumanBodyBones.Spine);
        // MapBlazeToHumanoid(0, HumanBodyBones.Hips);
        // MapBlazeToHumanoid(8, HumanBodyBones.Neck);

        Debug.Log($"Humanoid bone mapping table built: {boneMap.Count} entries");
    }
}
