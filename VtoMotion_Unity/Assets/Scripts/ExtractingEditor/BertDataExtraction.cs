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
using UnityEngine.TextCore.Text;

// using System.Diagnostics;


public class BertDataExtraction : RealTimeAnimation
{
    public bool b_play = false;
    public bool b_save_data = false;

    public int StartFrame = 1;
    public BlazePoseDataFile _BlazeMotionData;
    public MotionDataFile _MotionData;
    public Actor Character;

    // joint root cubes
    public Transform jointRoot;
    public float cube_scale = 0.02f;
    public float human_size_scale = 5f;
    public float static_yoffset = -1.2f;
    public float bone_size = 0.12f;

    public GameObject capsulePrefab;     // 기본 캡슐 프리팹
    [SerializeField] 
    public List<GameObject> capsules = new List<GameObject>();
    public float capsuleRadius = 0.15f;


    private List<GameObject> jointCubes = new List<GameObject>();
    public MotionBertSkeletonBuilder skel_build_bp;
     // 회전 리타게팅용 캐시
    private Dictionary<int, Quaternion> boneRestRotations = new Dictionary<int, Quaternion>();
    private Dictionary<int, Vector3> boneRestDir = new Dictionary<int, Vector3>(); // 본이 바라보는 기본 방향
    private bool restPoseCaptured = false;
    private static readonly Dictionary<int, int> directionChildMap = new Dictionary<int, int>
    {
        {11, 12}, // LeftUpperArm  -> LeftElbow
        {12, 13}, // LeftLowerArm  -> LeftWrist
        {14, 15}, // RightUpperArm -> RightElbow
        {15, 16}, // RightLowerArm -> RightWrist
        {4, 5}, // LeftUpperLeg  -> LeftKnee
        {5, 6}, // LeftLowerLeg  -> LeftAnkle
        {1, 2}, // RightUpperLeg -> RightKnee
        {2, 3}, // RightLowerLeg -> RightAnkle
        // {27, 31}, // LeftFoot -> LeftToes
        // {28, 32}, // RightFoot -> RightToes
    };
    protected override void Setup()
    {
        _BlazeMotionData = ScriptableObject.CreateInstance<BlazePoseDataFile>();
        _MotionData = ScriptableObject.CreateInstance<MotionDataFile>();
        b_play = false;
        cube_scale = 0.02f;
    }
    protected override void Close()
    {

    }
    
    //feed functions
    Vector3 ConvertBlazeToUnity(Vector3 bp)
    {
        // x축, z축 방향 전환 (필요 시)
        return new Vector3(-bp.x, bp.y, -bp.z);
    }
    public void UpdateCubes(Transform jointRoot, Vector3[] currentPose)
    {
        if (jointRoot == null)
        {
            Debug.LogWarning(" joint_root is null. Please assign it first.");
            return;
        }

        if (currentPose == null || currentPose.Length == 0)
        {
            Debug.LogWarning("currentPose is empty.");
            return;
        }

        // joint_root 아래의 자식들 중 joint_i 이름을 가진 오브젝트를 찾아 업데이트
        for (int i = 0; i < currentPose.Length; i++)
        {
            Transform joint = jointRoot.Find($"joint_{i}");
            if (joint != null)
            {
                Vector3 position_blaze_to_unity = ConvertBlazeToUnity(currentPose[i])*human_size_scale;
                joint.localPosition = position_blaze_to_unity;
                joint.transform.localScale = new Vector3(capsuleRadius, capsuleRadius, capsuleRadius);
            
            }
            else
            {
                Debug.LogWarning($"⚠️ joint_{i} not found under {jointRoot.name}");
            }
        }
    }

    // public void UpdateBlazePose(MotionBertSkeletonBuilder skel_bp, Vector3[] currentPose)
    // {
    //     for (int j = 0; j < Character.Bones.Length; j++)
    //     {
    //         int map_i = skel_bp.boneMap[j];
    //         //Debug.Log($" map_i : {map_i} bone : {j}");
    //         Vector3 position_blaze_to_unity = ConvertBlazeToUnity(currentPose[j])*human_size_scale;
    //         Character.Bones[map_i].Transform.SetPositionAndRotation(position_blaze_to_unity, Quaternion.identity);
    //     }
    // }
    public void UpdateBlazePose(MotionBertSkeletonBuilder skel_bp, Vector3[] currentPose)
    {
        if (currentPose == null || currentPose.Length == 0) return;
        if (skel_bp == null || skel_bp.boneMap == null || skel_bp.boneMap.Count == 0) return;
        if (Character == null || Character.Bones == null) return;

        // 첫 프레임에 아직 캡처 안 했으면 T-pose 기준 정보 저장
        if (!restPoseCaptured)
        {
            CaptureRestPose(skel_bp);
        }

        // 1) BlazePose 좌표를 Unity 좌표로 모두 변환
        Vector3[] worldJointPos = new Vector3[currentPose.Length];
        for (int i = 0; i < currentPose.Length; i++)
        {
            worldJointPos[i] = ConvertBlazeToUnity(currentPose[i]) * human_size_scale;
        }
        // Pelvis yoffset
        worldJointPos[0].y = worldJointPos[0].y + static_yoffset;

        Animator anim = Character.GetComponentInChildren<Animator>();

        Transform hipsT = anim.GetBoneTransform(HumanBodyBones.Hips);
        Character.Bones[0].Transform.position = worldJointPos[0];
        //Character.Bones[0].Transform.rotation = hipsT.rotation;
        // 2) 루트(골반) 위치를 BlazePose에 맞게 이동
        Animator animTorso = Character.GetComponentInChildren<Animator>();
        if (animTorso != null)
        {
            Transform hips = animTorso.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null)
            {
                Vector3 leftShoulder = ConvertBlazeToUnity(currentPose[11]) * human_size_scale;
                Vector3 rightShoulder = ConvertBlazeToUnity(currentPose[14]) * human_size_scale;
                Vector3 leftHip = ConvertBlazeToUnity(currentPose[4]) * human_size_scale;
                Vector3 rightHip = ConvertBlazeToUnity(currentPose[1]) * human_size_scale;

                Vector3 shoulderCenter = (leftShoulder + rightShoulder) * 0.5f;
                Vector3 hipCenter = (leftHip + rightHip) * 0.5f;

                Vector3 torsoDir = (shoulderCenter - hipCenter).normalized;     // 몸통의 위쪽 방향
                Vector3 rightDir = (rightShoulder - leftShoulder).normalized;   // 몸통의 오른쪽 방향
                Vector3 forwardDir = Vector3.Cross(rightDir, torsoDir).normalized;  // 정면 방향

                if (forwardDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(forwardDir, torsoDir);
                    Debug.Log("f" + forwardDir);
                    // 골반도 같은 방향으로 회전
                    hips.rotation = targetRot;
                    Character.Bones[0].Transform.localRotation = targetRot;
                }
            }
        }

        // int hipL = 23; // left hip landmark
        // int hipR = 24; // right hip landmark
        // Animator anim = Character.GetComponentInChildren<Animator>();
        // if (anim != null && hipL < worldJointPos.Length && hipR < worldJointPos.Length)
        // {
        //     // BlazePose 기준 양쪽 엉덩이의 중간 지점
        //     Vector3 hipPos = (worldJointPos[hipL] + worldJointPos[hipR]) * 0.5f;

        //     // Humanoid Hips Transform 얻기
        //     Transform hipsT = anim.GetBoneTransform(HumanBodyBones.Hips);
        //     if (hipsT != null)
        //     {
        //         // Actor.Bones 안에서 같은 Transform을 가진 Bone 찾기
        //         int hipsBoneIndex = -1;
        //         for (int i = 0; i < Character.Bones.Length; i++)
        //         {
        //             if (Character.Bones[i].Transform == hipsT)
        //             {
        //                 hipsBoneIndex = i;
        //                 break;
        //             }
        //         }

        //         if (hipsBoneIndex >= 0)
        //         {
        //             Character.Bones[hipsBoneIndex].Transform.position = hipPos;
        //             Character.Bones[hipsBoneIndex].Transform.rotation = hipsT.rotation;
        //         }
        //     }
        // }
        // //

        // 3) 각 본의 회전을 방향 벡터 기반으로 갱신
        foreach (var kv in skel_bp.boneMap)
        {
            int parentBlaze = kv.Key;
            int parentBone = kv.Value;
            //Debug.Log()
            int childBlaze;
            if (!directionChildMap.TryGetValue(parentBlaze, out childBlaze))
                continue;

            if (parentBlaze < 0 || parentBlaze >= worldJointPos.Length) continue;
            if (childBlaze < 0 || childBlaze >= worldJointPos.Length) continue;

            if (!boneRestRotations.ContainsKey(parentBone) ||
                !boneRestDir.ContainsKey(parentBone))
                continue;

            // BlazePose 기준 현재 방향 (부모 -> 자식)
            Vector3 targetDir = worldJointPos[childBlaze] - worldJointPos[parentBlaze];
            if (targetDir.sqrMagnitude < 1e-6f) continue;
            targetDir.Normalize();

            // 캐릭터 기본 포즈에서의 방향 / 회전
            Vector3 restDir = boneRestDir[parentBone];
            Quaternion restRot = boneRestRotations[parentBone];

            // 기본 방향 -> 현재 방향으로 회전 델타 계산
            Quaternion delta = Quaternion.FromToRotation(restDir, targetDir);

            // 최종 회전 = 델타 * 기본 회전
            Character.Bones[parentBone].Transform.rotation = delta * restRot;
        }
        
        // // 머리(Head / Neck) 회전 보정
        // Transform head = animTorso.GetBoneTransform(HumanBodyBones.Head);
        // Transform neck = animTorso.GetBoneTransform(HumanBodyBones.Neck);

        // if (head != null)
        // {
        //     Vector3 leftShoulder = ConvertBlazeToUnity(currentPose[11]) * human_size_scale;
        //     Vector3 rightShoulder = ConvertBlazeToUnity(currentPose[12]) * human_size_scale;
        //     Vector3 leftHip = ConvertBlazeToUnity(currentPose[23]) * human_size_scale;
        //     Vector3 rightHip = ConvertBlazeToUnity(currentPose[24]) * human_size_scale;

        //     Vector3 nose = ConvertBlazeToUnity(currentPose[0]) * human_size_scale;
        //     Vector3 shoulderCenter = (leftShoulder + rightShoulder) * 0.5f;
        //     Vector3 hipCenter = (leftHip + rightHip) * 0.5f;
        //     // Vector3 headDir = (nose - shoulderCenter).normalized;       // 얼굴이 바라보는 방향
        //     Vector3 torsoUp = Vector3.up;                           // 위쪽은 월드 기준으로 고정
        //     Vector3 headDir = (shoulderCenter - hipCenter).normalized;

        //     if (headDir.sqrMagnitude > 0.001f)
        //     {
        //         Quaternion headRot = Quaternion.FromToRotation(torsoUp);
        //         head.rotation = headRot;

        //         // if (neck != null)
        //         //     neck.rotation = Quaternion.Slerp(neck.rotation, headRot, 0.5f);
        //     }
        // }

        // // 몸통(Spine/Chest) 회전 보정
        // Animator animTorso = Character.GetComponentInChildren<Animator>();
        // if (animTorso != null)
        // {
        //     Transform hips = animTorso.GetBoneTransform(HumanBodyBones.Hips);
        //     Transform chest = animTorso.GetBoneTransform(HumanBodyBones.Chest);
        //     if (hips != null && chest != null)
        //     {
        //         Vector3 leftShoulder = ConvertBlazeToUnity(currentPose[11]) * human_size_scale;
        //         Vector3 rightShoulder = ConvertBlazeToUnity(currentPose[12]) * human_size_scale;
        //         Vector3 leftHip = ConvertBlazeToUnity(currentPose[23]) * human_size_scale;
        //         Vector3 rightHip = ConvertBlazeToUnity(currentPose[24]) * human_size_scale;

        //         Vector3 shoulderCenter = (leftShoulder + rightShoulder) * 0.5f;
        //         Vector3 hipCenter = (leftHip + rightHip) * 0.5f;

        //         Vector3 torsoDir = (shoulderCenter - hipCenter).normalized;     // 몸통의 위쪽 방향
        //         Vector3 rightDir = (rightShoulder - leftShoulder).normalized;   // 몸통의 오른쪽 방향
        //         Vector3 forwardDir = Vector3.Cross(rightDir, torsoDir).normalized;  // 정면 방향

        //         if (forwardDir.sqrMagnitude > 0.001f)
        //         {
        //             Quaternion targetRot = Quaternion.LookRotation(forwardDir, torsoDir);

        //             // 골반도 같은 방향으로 회전
        //             //hips.rotation = targetRot;

        //             chest.rotation = Quaternion.Slerp(chest.rotation, targetRot, 0.7f);
        //             // chest.rotation = targetRot;
        //         }

        //     }

        
        // }


    }


    public void DrawBlazeSkel(MotionBertSkeletonBuilder skel_bp, float boneSize, Color boneColor)
    {
        // 1. boneMap이 없거나 비어있으면 바로 리턴
        if (skel_bp == null || skel_bp.boneMap == null || skel_bp.boneMap.Count == 0)
        {
            Debug.LogWarning("[DrawBlazeSkel] boneMap is not initialized — skipping draw.");
            return;
        }

        // 2. blazePoseBones도 체크
        if (skel_bp.motionBertBones == null || skel_bp.motionBertBones.GetLength(0) == 0)
        {
            Debug.LogWarning("[DrawBlazeSkel] blazePoseBones is not defined — skipping draw.");
            return;
        }

        UltiDraw.Begin();

        for (int i = 0; i < skel_bp.motionBertBones.GetLength(0); i++)
        {
            int parent = skel_bp.motionBertBones[i, 0];
            int child = skel_bp.motionBertBones[i, 1];

            int map_parent = skel_bp.boneMap[parent];
            int map_child = skel_bp.boneMap[child];

            if (parent < 0 || parent >= Character.Bones.Length) continue;
            if (child < 0 || child >= Character.Bones.Length) continue;
            if (Character.Bones[map_parent] == null || Character.Bones[map_child] == null) continue;

            Vector3 parentPos = Character.Bones[map_parent].Transform.position;
            Vector3 childPos = Character.Bones[map_child].Transform.position;

            float length = Vector3.Distance(parentPos, childPos);

            //UltiDraw.DrawLine(parentPos, childPos,boneSize, boneColor);

            UltiDraw.DrawBone(
                parentPos,
                Quaternion.FromToRotation(Vector3.forward, (childPos - parentPos).normalized),
                12.5f * boneSize * length,
                length,
                Color.grey
            ); // boneColor.Transparent(1f)
        }

        UltiDraw.End();
    }
    
    // Capsule visualization
    public void BuildCapsules(MotionBertSkeletonBuilder skel_bp)
    {
        // 이전 캡슐 제거
        foreach (var cap in capsules)
            DestroyImmediate(cap);
        capsules.Clear();

        if (capsulePrefab == null)
        {
            capsulePrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsulePrefab.name = "CapsulePrefab";
            capsulePrefab.SetActive(true); // 템플릿용
        }

        GameObject capsuleRoot = new GameObject("Capsules");
        // 캡슐 생성
        for (int i = 0; i < skel_bp.motionBertBones.GetLength(0); i++)
        {
            GameObject capsule = Instantiate(capsulePrefab, capsuleRoot.transform);
            capsule.name = $"Capsule_{i}";
            capsule.transform.localScale = new Vector3(capsuleRadius, 0.05f, capsuleRadius);
            capsules.Add(capsule);
        }
    }

    public void EnsureCapsulesExist(MotionBertSkeletonBuilder skel_bp)
    {
        if (capsules == null || capsules.Count == 0)
        {
            Debug.Log("Capsules not assigned — building automatically.");
            BuildCapsules(skel_bp);
        }
        else
        {
            Debug.Log("Using existing capsules from Inspector.");
        }
    }
    public void UpdateCapsules(MotionBertSkeletonBuilder skel_bp, Actor actor)
    {
        // 본 시각화
        for (int i = 0; i < skel_bp.motionBertBones.GetLength(0); i++)
        {
            int parent = skel_bp.motionBertBones[i, 0];
            int child = skel_bp.motionBertBones[i, 1];

            int map_parent = skel_bp.boneMap[parent];
            int map_child = skel_bp.boneMap[child];

            if (parent < 0 || parent >= actor.Bones.Length) continue;
            if (child < 0 || child >= actor.Bones.Length) continue;
            if (actor.Bones[map_parent] == null || actor.Bones[map_child] == null) continue;

            Vector3 parentPos = actor.Bones[map_parent].Transform.position;
            Vector3 childPos = actor.Bones[map_child].Transform.position;

            float length = Vector3.Distance(parentPos, childPos);

            bool isHand = false;
            UpdateCapsuleTransform(capsules[i], parentPos, childPos, isHand);
        }
    }

    // ============================
    // ③ 캡슐 위치/회전 업데이트
    // ============================
    private void UpdateCapsuleTransform(GameObject capsule, Vector3 start, Vector3 end, bool isHand)
    {
        Vector3 offset = end - start;
        Vector3 position = start + offset / 2.0f;
        capsule.transform.position = position;
        capsule.transform.rotation = Quaternion.FromToRotation(Vector3.up, offset);

        float length = offset.magnitude;
        capsule.transform.localScale = new Vector3(capsuleRadius, length / 2.0f + 0.03f, capsuleRadius);

        if (isHand && capsule.transform.childCount > 0)
        {
            Transform hand = capsule.transform.GetChild(0);
            if (hand != null)
            {
                hand.localScale = new Vector3(capsuleRadius, 1.0f, capsuleRadius);
                hand.localRotation = Quaternion.FromToRotation(Vector3.up, offset);
                hand.position = end;
            }
        }
    }

    public void CaptureRestPose(MotionBertSkeletonBuilder skel_bp)
    {
        boneRestRotations.Clear();
        boneRestDir.Clear();
        restPoseCaptured = false;

        if (Character == null || Character.Bones == null)
        {
            Debug.LogWarning("[BlazeDataExtraction] Character is null, cannot capture rest pose.");
            return;
        }

        if (skel_bp == null || skel_bp.boneMap == null || skel_bp.boneMap.Count == 0)
        {
            Debug.LogWarning("[BlazeDataExtraction] skel_bp / boneMap not ready.");
            return;
        }

        // 1) 각 본의 월드 기준 기본 회전 저장
        foreach (var kv in skel_bp.boneMap)
        {
            int boneIndex = kv.Value;
            if (boneIndex < 0 || boneIndex >= Character.Bones.Length) continue;

            Transform t = Character.Bones[boneIndex].Transform;
            boneRestRotations[boneIndex] = t.rotation;
        }

        // 2) 각 본이 바라보는 방향 저장
        foreach (var kv in skel_bp.boneMap)
        {
            int parentBlaze = kv.Key;
            int parentBone = kv.Value;

            int childBlaze;
            if (!directionChildMap.TryGetValue(parentBlaze, out childBlaze))
                continue; // Blaze joint는 방향을 정의X

            if (!skel_bp.boneMap.ContainsKey(childBlaze))
                continue; // 자식 Blaze가 매핑돼 있지 않으면 스킵

            int childBone = skel_bp.boneMap[childBlaze];

            Transform tParent = Character.Bones[parentBone].Transform;
            Transform tChild = Character.Bones[childBone].Transform;

            Vector3 dir = tChild.position - tParent.position;
            if (dir.sqrMagnitude < 1e-6f) continue;
            dir.Normalize();

            boneRestDir[parentBone] = dir;
        }


        restPoseCaptured = true;
        Debug.Log("[BlazeDataExtraction] Rest pose captured.");
    }

    protected override void Feed()
    {
        // Vector3 vector = _BlazeMotionData.frameDict[0][0];
        // Debug.Log("see " + vector);
        if (b_play)
        {

            // import single data from csvList;
            if (Frame == StartFrame)
            {
                _BlazeMotionData.ImportCSVData(_BlazeMotionData.selectedData, 1.0f);
                EnsureCapsulesExist(skel_build_bp);
                
            }

            // initialize the data
            if (Frame >= _BlazeMotionData.frameDict.Count)
            {
                b_play = false;
                b_save_data = true;
                return;
            }
            else
            {
                Vector3[] currentPose = _BlazeMotionData.frameDict[Frame].ToArray();

                //Debug.Log($" Frame : {Frame} , currentPose {currentPose[0]} / {currentPose.Length}");
                UpdateBlazePose(skel_build_bp, currentPose);

                _MotionData.RecordingPose(Character);
                
                Frame++;
            }
        }
        if(b_save_data)
        {
            Debug.Log("save : " + Frame );
            _MotionData.SavingRecordedData();
            b_save_data = false;
            _MotionData.RecordingState=RecordingState.NONE;
        }

    }
    protected override void Read()
    {
    }
    protected override void Postprocess()
    { }
    protected override void OnGUIDerived()
    { }
    protected override void OnRenderObjectDerived()
    {
        //DrawBlazeSkel(skel_build_bp, bone_size, Color.cyan);
    }

    public void CreateJointCubes(float scale)
    {
        // 기존 joint_root 제거
        if (jointRoot != null)
        {
            GameObject.DestroyImmediate(jointRoot.gameObject);
        }

        // 새 joint_root 생성
        GameObject root = new GameObject("joint_root");
        jointRoot = root.transform;
        jointCubes.Clear();

        int nJoint = 33;
        for (int i = 0; i < nJoint; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"joint_{i}";
            cube.transform.parent = jointRoot;
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = Vector3.one * scale;
            // 주황색 material 생성
            Material orangeMaterial = new Material(Shader.Find("Standard"));
            orangeMaterial.color = new Color(1f, 0.647f, 0f); // RGB 값으로 주황색

            // cube에 material 적용
            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                cubeRenderer.material = orangeMaterial;
            }

            jointCubes.Add(cube);
        }

        //Debug.Log($"✅ Created {nJoint} joint cubes under 'joint_root'");
    }

    [CustomEditor(typeof(BertDataExtraction), true)]
    public class BertDataExtraction_Editor : Editor
    {
        public BertDataExtraction Target;
        public void Awake()
        {
            Target = (BertDataExtraction)target;
            //Target.is_random = true;
            Target.skel_build_bp = ScriptableObject.CreateInstance<MotionBertSkeletonBuilder>();
        }
        public override void OnInspectorGUI()
        {
            Inspector();
        }
        private void Inspector()
        {
            Utility.ResetGUIColor();
            Utility.SetGUIColor(UltiDraw.LightGrey);

            // Assigning Target Avatar
            EditorGUILayout.BeginVertical();
            Target.Character = (Actor)EditorGUILayout.ObjectField("Source Actor", Target.Character, typeof(Actor), true);
            EditorGUILayout.EndVertical();

            if (Target._BlazeMotionData != null)
            {
                //BlazeData
                Target._BlazeMotionData.MotionCSVFile_Inspector(Target.Character);
            }

            // // create_cubes
            // Target.cube_scale = EditorGUILayout.FloatField("cube_scale", Target.cube_scale);
            // if (Utility.GUIButton("Create Joint Cubes (33)", Color.white, Color.cyan))
            // {
            //    Target.CreateJointCubes(Target.cube_scale);
            // }
            
            
            Target.capsuleRadius = EditorGUILayout.FloatField("bone_size", Target.capsuleRadius);
            Target.human_size_scale = EditorGUILayout.FloatField("human_scale", Target.human_size_scale);
            Target.static_yoffset = EditorGUILayout.FloatField("static_yoffset", Target.static_yoffset);
            
            // capsule prefab field
            Target.capsulePrefab = (GameObject)EditorGUILayout.ObjectField("Capsule Prefab", Target.capsulePrefab, typeof(GameObject), true);

            // capsules list
            SerializedProperty capsulesProp = serializedObject.FindProperty("capsules");
            EditorGUILayout.PropertyField(capsulesProp, new GUIContent("Capsules List"), true);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);
        
            // if (Utility.GUIButton("Create BlazePose Actor (33)", Color.white, Color.cyan))
            // {
            //     Target.skel_build_bp.BuildActorSkeleton();
            //     Target.Character = Target.skel_build_bp.actor;
            //     Target.Character.DrawSkeleton = false;
            //     Debug.Log($"Target Character : {Target.Character.Bones.Length}");
            // }
            
            // if (Utility.GUIButton("build capsules", Color.white, Color.red))
            // {
            //     Target.BuildCapsules(Target.skel_build_bp);
            // }

            // play button
            if (Utility.GUIButton("reset & play animation", Color.white, Color.red))
            {
                //Target.skel_build_bp.BuildMapping(Target.Character);
                if (Target.Character != null)
                {
                    Animator anim = Target.Character.GetComponentInChildren<Animator>();

                    if (anim != null && anim.isHuman)
                    {
                        // 휴머노이드 캐릭터라면 자동 Humanoid 매핑 사용
                        //Debug.Log("hello");
                        Target.skel_build_bp.BuildHumanoidMapping(Target.Character, anim);
                    }
                    else
                    {
                        // 아니면 기존 매핑 사용
                        Target.skel_build_bp.BuildMapping(Target.Character);
                    }
                }
                Target.Frame = Target.StartFrame;
                Target.b_play = true;
            }
            if (Utility.GUIButton(" Do Extracting ", Color.white, Color.red))
            {
                Target.Frame = Target.StartFrame;
                Target.b_play = true;
                Target.b_save_data = false;
            }
        }
        
    }
}
