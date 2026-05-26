using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;



public enum MotionData_Type
{
    ALL,
    BVH,
    FBX,
    MOTIONTEXT,
    FBX_MOTIONTEXT,
    BVH_MOTIONTEXT
}

public enum RecordingState //! ZedBodyRecordingManager 에 있는 enum 정의를 수정해야한다.
{
    NONE,
    RECORDING,
    TESTPLAYING,
    PLAYING,
}
public class MotionDataFile : ScriptableObject
{
    public class BVHFile
    {
        public FileInfo FILE_Info;
        public Matrix4x4[][] Motion, MotionWR;
        public Matrix4x4[] RootTrajectory = new Matrix4x4[0];
        public Matrix4x4[] seenByChild = new Matrix4x4[0];
        public int RightShoulder, LeftShoulder, RightUpLeg, LeftUpLeg;
        public int nFrames;
        public float Framerate;
        public bool Import;
        public Skeleton.Hierarchy skel_hierarchy;

        public int GetTotalFrames()
        {
            return nFrames;
        }
        public Matrix4x4[][] MotionByForcedFPS(int FPS)
        {
            int width = Mathf.RoundToInt(Framerate / FPS);
            int totalframes = Mathf.RoundToInt(GetTotalFrames() / width);
            Matrix4x4[][] newMotion = new Matrix4x4[totalframes][];
            for (int k = 0; k < totalframes; k++)
            {
                newMotion[k] = new Matrix4x4[Motion[width * k].Length];
                newMotion[k] = Motion[width * k];

                //for (int i = 0; i < data.Source.Bones.Length; i++)
                //    Motion[k,i] = data.Frames[k].World[i];
            }

            return newMotion;
        }
        public void InitializeMatrix(int numRows, int numCols)
        {
            Motion = new Matrix4x4[numRows][];

            for (int i = 0; i < numRows; i++)
            {
                Motion[i] = new Matrix4x4[numCols];
            }
        }


        // find right,left shoulder & upleg
        public void FindUpperBody(string rightshoulder, string leftshoulder, string righthip, string lefthip)
        {
            Skeleton.Hierarchy.Bone rs = skel_hierarchy.FindBoneContains(rightshoulder);
            RightShoulder = rs == null ? 0 : rs.Index;
            Skeleton.Hierarchy.Bone ls = skel_hierarchy.FindBoneContains(leftshoulder);
            LeftShoulder = ls == null ? 0 : ls.Index;
            Skeleton.Hierarchy.Bone rh = skel_hierarchy.FindBoneContains(righthip);
            RightUpLeg = rh == null ? 0 : rh.Index;
            Skeleton.Hierarchy.Bone lh = skel_hierarchy.FindBoneContains(lefthip);
            LeftUpLeg = lh == null ? 0 : lh.Index;

        }
        // genreate root trajectory of motion
        public void GenerateRootTrajectory(int start, int end)
        {
            RootTrajectory = new Matrix4x4[end - start + 1];
            MotionWR = new Matrix4x4[end - start + 1][];
            for (int n = start, k = 0; n <= end; n++, k++)
            {
                RootTrajectory[k] = GetRoot(n, 0.0f);

                MotionWR[k] = new Matrix4x4[skel_hierarchy.Bones.Length];
                for (int j = 0; j < skel_hierarchy.Bones.Length; j++)
                    MotionWR[k][j] = Motion[n][j].GetRelativeTransformationTo(RootTrajectory[k]);
            }
          
            GenerateRootRelative();
        }
        public Matrix4x4 GetRoot(int index, float y_offset)
        {
            //vector_x
            Vector3 vec_shoulder = Motion[index][LeftShoulder].GetPosition() - Motion[index][RightShoulder].GetPosition();
            vec_shoulder = vec_shoulder.normalized;
            Vector3 vec_upleg = Motion[index][LeftUpLeg].GetPosition() - Motion[index][RightUpLeg].GetPosition();
            vec_upleg = vec_upleg.normalized;
            Vector3 vec_across = vec_shoulder + vec_upleg;
            vec_across = vec_across.normalized;
            //vector_forward
            Vector3 vec_forward = Vector3.Cross(-1.0f * vec_across, Vector3.up);
            //vector_x_new
            Vector3 vec_right = Vector3.Cross(-1.0f * vec_forward, Vector3.up);
            //root matrix 
            Matrix4x4 root_interaction = Matrix4x4.identity;
            Vector4 vec_x = new Vector4(vec_right.x, vec_right.y, vec_right.z, 0.0f);
            Vector4 vec_z = new Vector4(vec_forward.x, vec_forward.y, vec_forward.z, 0.0f);
            Vector4 vec_y = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
            Vector3 pos__ = Motion[index][0].GetPosition();
            Vector4 pos_h = new Vector4(pos__.x, y_offset, pos__.z, 1.0f);
            root_interaction.SetColumn(0, vec_x); root_interaction.SetColumn(1, vec_y); root_interaction.SetColumn(2, vec_z);
            root_interaction.SetColumn(3, pos_h);
            //
            return root_interaction;
        }
        public void GenerateRootRelative()
        {
            seenByChild = new Matrix4x4[RootTrajectory.Length - 1];
            for (int n = 0; n < seenByChild.Length; n++)
            {
                seenByChild[n] = RootTrajectory[n + 1].GetRelativeTransformationTo(RootTrajectory[n]);
            }
        }
    }

    public class FBXFile
    {
        public FileInfo FILE_Info;
        public GameObject Object;
        public Actor Character;

        public Matrix4x4[][] Motion;
        public int nFrames;
        public float Framerate = 30;
        public bool Import;
        public Skeleton.Hierarchy skel_hierarchy;
        
        public int GetTotalFrames()
        {
            return nFrames;
        }
        public Matrix4x4[][] MotionByForcedFPS(int FPS)
        {
            int width = Mathf.RoundToInt(Framerate / FPS);
            int totalframes = Mathf.RoundToInt(GetTotalFrames() / width);
            Matrix4x4[][] newMotion = new Matrix4x4[totalframes][];
            for (int k = 0; k < totalframes; k++)
            {
                newMotion[k] = new Matrix4x4[Motion[width * k].Length];
                newMotion[k] = Motion[width * k];

                //for (int i = 0; i < data.Source.Bones.Length; i++)
                //    Motion[k,i] = data.Frames[k].World[i];
            }

            return newMotion;
        }
        public void InitializeMatrix(int numRows, int numCols)
        {
            Motion = new Matrix4x4[numRows][];

            for (int i = 0; i < numRows; i++)
            {
                Motion[i] = new Matrix4x4[numCols];
            }
        }

        public void CreateScene(string path)
        {
            UnityEngine.SceneManagement.Scene active = EditorSceneManager.GetActiveScene();
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(scene);
            Lightmapping.bakedGI = false;
            Lightmapping.realtimeGI = false;
            EditorSceneManager.SetActiveScene(active);
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    public class MotionTextFile
    {
        public FileInfo FILE_Info;
        public Matrix4x4[][] Motion;
        public int nFrames;
        public float Framerate;
        public bool Import;
        public Skeleton.Hierarchy skel_hierarchy;
        public Actor Character;

        public int GetTotalFrames()
        {
            return nFrames;
        }
        public Matrix4x4[][] MotionByForcedFPS(int FPS)
        {
            int width = Mathf.RoundToInt(Framerate / FPS);
            int totalframes = Mathf.RoundToInt(GetTotalFrames() / width);
            Matrix4x4[][] newMotion = new Matrix4x4[totalframes][];
            for (int k = 0; k < totalframes; k++)
            {
                newMotion[k] = new Matrix4x4[Motion[width * k].Length];
                newMotion[k] = Motion[width * k];

                //for (int i = 0; i < data.Source.Bones.Length; i++)
                //    Motion[k,i] = data.Frames[k].World[i];
            }

            return newMotion;
        }
        public void InitializeMatrix(int numRows, int numCols)
        {
            Motion = new Matrix4x4[numRows][];

            for (int i = 0; i < numRows; i++)
            {
                Motion[i] = new Matrix4x4[numCols];
            }
        }
    }



    public BVHFile[] BVHFiles = null;// new BVHFile[0];
    public FBXFile[] FBXFiles = null;
    public MotionTextFile[] MotionTextFiles = null;
    public List<string> BVHFileNameList = new List<string>();
    public List<string> FBXFileNameList = new List<string>();
    public List<string> MotionTextFileNameList = new List<string>();
    public Matrix4x4[][] Motion = null;
    public Matrix4x4[][] MotionWR = null;
    public Matrix4x4[] RootTrajectory = new Matrix4x4[0];
    public Matrix4x4[] seenByChild = new Matrix4x4[0];
    public string DirectoryName;
    public string FileName;
    public int Total_FileNumber = 0;
    public bool b_play = false;
    public bool b_data = false;
    public int CurFrame = 0;

    public Actor Character;

    public int selectedData;
    public bool file_exist;
    public bool b_draw_root_trajectory;
    [SerializeField]
    public string LeftShoulder, RightShoulder, LeftHip, RightHip;
    public int int_LS, int_RS, int_LH, int_RH;
    public float scale = 1.0f;
    public float Framerate = 30; // target framerate
    public bool LoadBVHDirectory(string Source, string type)
    {
        bool b_success;
        if (BVHFileNameList.Count > 0)
            BVHFileNameList.Clear();
        if (!string.IsNullOrEmpty(Source))
        {
            if (Directory.Exists(Source))
            {
                DirectoryInfo info = new DirectoryInfo(Source);
                FileInfo[] items = info.GetFiles(type);

                Debug.Log("type " + type + " are " + items.Length);
                if (type == "*.bvh" && items.Length > 0)
                {
                    BVHFiles = new BVHFile[items.Length];
                    for (int i = 0; i < items.Length; i++)
                    {
                        BVHFiles[i] = new BVHFile();
                        BVHFiles[i].FILE_Info = items[i];
                        BVHFiles[i].Import = true;
                        BVHFileNameList.Add(items[i].FullName);
                    }

                    b_success = true;
                }
                else
                    b_success = false;
            }
            else
            {
                //Files = new BVHFile[0];
                BVHFiles = null;
                b_success =  false;
            }
        }
        else
        {
            //Files = new BVHFile[0];
            BVHFiles = null;
            b_success =  false;
        }
        return b_success;
    }

    public bool ImportBVHData(int fileindex, float scale)
    {
        bool b_true = false;
        string fileName = BVHFiles[fileindex].FILE_Info.Name.Replace(".bvh", "");
        if (BVHFiles[fileindex].Import)
        {
            //data = ScriptableObject.CreateInstance<MotionData>();
            string[] lines = System.IO.File.ReadAllLines(BVHFiles[fileindex].FILE_Info.FullName);
            char[] whitespace = new char[] { ' ' };
            int index = 0;
            //Create Source Data
            List<Vector3> offsets = new List<Vector3>();
            List<int[]> channels = new List<int[]>();
            List<float[]> motions = new List<float[]>();

            // new skeleton hierarchy 
            //data.Source = new MotionData.Hierarchy();
            BVHFiles[fileindex].skel_hierarchy = new Skeleton.Hierarchy();

            string name = string.Empty;
            string parent = string.Empty;
            Vector3 offset = Vector3.zero;
            int[] channel = null;

            for (index = 0; index < lines.Length; index++)
            {
                if (lines[index] == "MOTION")
                {
                    break;
                }
                string[] entries = lines[index].Split(whitespace);
                for (int entry = 0; entry < entries.Length; entry++)
                {
                    if (entries[entry].Contains("ROOT"))
                    {
                        parent = "None";
                        name = entries[entry + 1];
                        break;
                    }
                    else if (entries[entry].Contains("JOINT"))
                    {
                        parent = name;
                        name = entries[entry + 1];
                        break;
                    }
                    else if (entries[entry].Contains("End"))
                    {
                        parent = name;
                        name = name + entries[entry + 1];
                        string[] subEntries = lines[index + 2].Split(whitespace);
                        for (int subEntry = 0; subEntry < subEntries.Length; subEntry++)
                        {
                            if (subEntries[subEntry].Contains("OFFSET"))
                            {
                                offset.x = FileUtility.ReadFloat(subEntries[subEntry + 1]);
                                offset.y = FileUtility.ReadFloat(subEntries[subEntry + 2]);
                                offset.z = FileUtility.ReadFloat(subEntries[subEntry + 3]);
                                break;
                            }
                        }
                        //data.Source.AddBone(name, parent);
                        // Debug.Log("Bone " + name);
                        //BVHFiles[fileindex].skel_hierarchy.AddBone(name, parent);
                        //offsets.Add(offset);
                        //channels.Add(new int[0]);
                        index += 2;
                        break;
                    }
                    else if (entries[entry].Contains("OFFSET"))
                    {
                        offset.x = FileUtility.ReadFloat(entries[entry + 1]);
                        offset.y = FileUtility.ReadFloat(entries[entry + 2]);
                        offset.z = FileUtility.ReadFloat(entries[entry + 3]);
                        break;
                    }
                    else if (entries[entry].Contains("CHANNELS"))
                    {
                        channel = new int[FileUtility.ReadInt(entries[entry + 1])];
                        for (int i = 0; i < channel.Length; i++)
                        {
                            if (entries[entry + 2 + i] == "Xposition")
                            {
                                channel[i] = 1;
                            }
                            else if (entries[entry + 2 + i] == "Yposition")
                            {
                                channel[i] = 2;
                            }
                            else if (entries[entry + 2 + i] == "Zposition")
                            {
                                channel[i] = 3;
                            }
                            else if (entries[entry + 2 + i] == "Xrotation")
                            {
                                channel[i] = 4;
                            }
                            else if (entries[entry + 2 + i] == "Yrotation")
                            {
                                channel[i] = 5;
                            }
                            else if (entries[entry + 2 + i] == "Zrotation")
                            {
                                channel[i] = 6;
                            }
                        }
                        //Debug.Log("Bone " + name);
                        BVHFiles[fileindex].skel_hierarchy.AddBone(name, parent);
                        offsets.Add(offset);
                        channels.Add(channel);
                        break;
                    }
                    else if (entries[entry].Contains("}"))
                    {
                        name = parent;
                        parent = name == "None" ? "None" : BVHFiles[fileindex].skel_hierarchy.FindBone(name).Parent;
                        break;
                    }
                }
            }

            //Set Frames
            index += 1;
            while (lines[index].Length == 0)
            {
                index += 1;
            }
            //ArrayExtensions.Resize(ref BVHFiles[fileindex].Motion, FileUtility.ReadInt(lines[index].Substring(8)));

            BVHFiles[fileindex].InitializeMatrix(FileUtility.ReadInt(lines[index].Substring(8)), BVHFiles[fileindex].skel_hierarchy.Bones.Length);
            Debug.Log(" nFrames " + BVHFiles[fileindex].Motion.Length);
            BVHFiles[fileindex].nFrames = BVHFiles[fileindex].Motion.Length;
            //Set Framerate
            index += 1;
            BVHFiles[fileindex].Framerate = Mathf.RoundToInt(1f / FileUtility.ReadFloat(lines[index].Substring(12)));

            //Compute Frames
            index += 1;
            for (int i = index; i < lines.Length; i++)
            {
                motions.Add(FileUtility.ReadArray(lines[i]));
            }
            for (int k = 0; k < BVHFiles[fileindex].GetTotalFrames(); k++)
            {
                //data.Frames[k] = new Frame(data, k + 1, (float)k / data.Framerate);
                int idx = 0;
                for (int i = 0; i < BVHFiles[fileindex].skel_hierarchy.Bones.Length; i++)
                {
                    //MotionData.Hierarchy.Bone info = data.Source.Bones[i];
                    Skeleton.Hierarchy.Bone info = BVHFiles[fileindex].skel_hierarchy.Bones[i];
                    Vector3 position = Vector3.zero;
                    Quaternion rotation = Quaternion.identity;

                    for (int j = 0; j < channels[i].Length; j++)
                    {
                        if (channels[i][j] == 1)
                        {
                            position.x = motions[k][idx]; idx += 1;
                        }
                        if (channels[i][j] == 2)
                        {
                            position.y = motions[k][idx]; idx += 1;
                        }
                        if (channels[i][j] == 3)
                        {
                            position.z = motions[k][idx]; idx += 1;
                        }
                        if (channels[i][j] == 4)
                        {
                            rotation *= Quaternion.AngleAxis(motions[k][idx], Vector3.right); idx += 1;
                        }
                        if (channels[i][j] == 5)
                        {
                            rotation *= Quaternion.AngleAxis(motions[k][idx], Vector3.up); idx += 1;
                        }
                        if (channels[i][j] == 6)
                        {
                            rotation *= Quaternion.AngleAxis(motions[k][idx], Vector3.forward); idx += 1;
                        }
                    }

                    position = (position == Vector3.zero ? offsets[i] : position) * scale; // to meter 
                    Matrix4x4 local = Matrix4x4.TRS(position, rotation, Vector3.one);
                    local = local.GetMirror(Axis.XPositive);

                    //storing motion data
                    //data.Frames[k].World[i] = info.Parent == "None" ? local : data.Frames[k].World[data.Source.FindBone(info.Parent).Index] * local;
                    //Debug.Log("hi frame " + k +" bone " + i);
                    BVHFiles[fileindex].Motion[k][i] = info.Parent == "None" ? local : BVHFiles[fileindex].Motion[k][BVHFiles[fileindex].skel_hierarchy.FindBone(info.Parent).Index] * local;
                }


            }

            if (BVHFiles[fileindex].GetTotalFrames() == 1)
            {
                Matrix4x4[] reference = BVHFiles[fileindex].Motion.First();
                ArrayExtensions.Resize(ref BVHFiles[fileindex].Motion, Mathf.RoundToInt(BVHFiles[fileindex].Framerate));
                for (int k = 0; k < BVHFiles[fileindex].Motion.Length; k++)
                {
                    Debug.Log("index " + k);
                    //data.Frames[k] = new Frame(data, k + 1, (float)k / data.Framerate);
                    //data.Frames[k].World = (Matrix4x4[])reference.World.Clone();
                    BVHFiles[fileindex].Motion[k] = (Matrix4x4[])reference.Clone();
                }
            }

            Matrix4x4[][] newMotion = BVHFiles[fileindex].MotionByForcedFPS(30);
            BVHFiles[fileindex].Motion = newMotion;
            BVHFiles[fileindex].nFrames = newMotion.Length;

            Motion = BVHFiles[fileindex].Motion;
            
            FileName = BVHFiles[fileindex].FILE_Info.Name;
            CurFrame = 0;
            b_true = true;
            b_data = true;
        }
        else
        {
            Debug.Log("File with name " + fileName + " already exists.");
            b_true = false;
        }

        return b_true;
    }

    public bool GenerateRootTrOFFile(Actor _actor)
    {
        FindUpperBody(_actor,RightShoulder, LeftShoulder,RightHip,LeftHip);
        GenerateRootTrajectory(_actor,0, Motion.GetLength(0) - 1);
        return true;
    }

    // genreate root trajectory of motion
    public void FindUpperBody(Actor _actor, string rightshoulder, string leftshoulder, string righthip, string lefthip)
    {
        Actor.Bone rs = _actor.FindBoneContains(rightshoulder);
        int_RS = rs == null ? 0 : rs.Index;
        Actor.Bone ls = _actor.FindBoneContains(leftshoulder);
        int_LS = ls == null ? 0 : ls.Index;
        Actor.Bone rh = _actor.FindBoneContains(righthip);
        int_RH = rh == null ? 0 : rh.Index;
        Actor.Bone lh = _actor.FindBoneContains(lefthip);
        int_LH = lh == null ? 0 : lh.Index;

    }

    public void GenerateRootTrajectory(Actor _actor, int start, int end)
    {
        RootTrajectory = new Matrix4x4[end - start + 1];
        MotionWR = new Matrix4x4[end - start + 1][];
        for (int n = start, k = 0; n <= end; n++, k++)
        {
            RootTrajectory[k] = GetRoot(n, 0.0f);

            MotionWR[k] = new Matrix4x4[_actor.Bones.Length];
            for (int j = 0; j < _actor.Bones.Length; j++)
                MotionWR[k][j] = Motion[n][j].GetRelativeTransformationTo(RootTrajectory[k]);
        }

        GenerateRootRelative();
    }
    public Matrix4x4 GetRoot(int index, float y_offset)
    {
        //vector_x
        Vector3 vec_shoulder = Motion[index][int_LS].GetPosition() - Motion[index][int_RS].GetPosition();
        vec_shoulder = vec_shoulder.normalized;
        Vector3 vec_upleg = Motion[index][int_LH].GetPosition() - Motion[index][int_RH].GetPosition();
        vec_upleg = vec_upleg.normalized;
        Vector3 vec_across = vec_shoulder + vec_upleg;
        vec_across = vec_across.normalized;
        //vector_forward
        Vector3 vec_forward = Vector3.Cross(-1.0f * vec_across, Vector3.up);
        //vector_x_new
        Vector3 vec_right = Vector3.Cross(-1.0f * vec_forward, Vector3.up);
        //root matrix 
        Matrix4x4 root_interaction = Matrix4x4.identity;
        Vector4 vec_x = new Vector4(vec_right.x, vec_right.y, vec_right.z, 0.0f);
        Vector4 vec_z = new Vector4(vec_forward.x, vec_forward.y, vec_forward.z, 0.0f);
        Vector4 vec_y = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
        Vector3 pos__ = Motion[index][0].GetPosition();
        Vector4 pos_h = new Vector4(pos__.x, y_offset, pos__.z, 1.0f);
        root_interaction.SetColumn(0, vec_x); root_interaction.SetColumn(1, vec_y); root_interaction.SetColumn(2, vec_z);
        root_interaction.SetColumn(3, pos_h);
        //
        return root_interaction;
    }
    public void GenerateRootRelative()
    {
        seenByChild = new Matrix4x4[RootTrajectory.Length - 1];
        for (int n = 0; n < seenByChild.Length; n++)
        {
            seenByChild[n] = RootTrajectory[n + 1].GetRelativeTransformationTo(RootTrajectory[n]);
        }
    }
    public void RenderRootTrajectory(int fileindex)
    {
        if (b_draw_root_trajectory && BVHFiles[fileindex].RootTrajectory.Length>0)
        {
            UltiDraw.Begin();
            int framewidth = 30;
            for (int i = 0; i < Mathf.RoundToInt(BVHFiles[fileindex].RootTrajectory.Length / framewidth); i++)
            {
                UltiDraw.DrawWiredSphere(BVHFiles[fileindex].RootTrajectory[framewidth * i].GetPosition(), BVHFiles[fileindex].RootTrajectory[framewidth * i].rotation, 0.1f, UltiDraw.Orange, UltiDraw.Black);
                UltiDraw.DrawTranslateGizmo(BVHFiles[fileindex].RootTrajectory[framewidth * i].GetPosition(), BVHFiles[fileindex].RootTrajectory[framewidth * i].rotation, 0.1f);
                //UltiDraw.DrawTranslateGizmo(Files[fileindex].Motion[framewidth * i][5].GetPosition(), Files[fileindex].Motion[framewidth * i][5].rotation, 0.1f);
            }
            UltiDraw.End();
        }
    }
    public BVHFile GetBVHData(int fileindex)
    {
        return BVHFiles[fileindex];
    }

    public bool LoadFBXDirectory(string Source, string type)
    {
        bool b_success;
        if (FBXFileNameList.Count > 0)
            FBXFileNameList.Clear();
        if (!string.IsNullOrEmpty(Source))
        {
            if (Directory.Exists(Source))
            {
                DirectoryInfo info = new DirectoryInfo(Source);
                FileInfo[] items = info.GetFiles(type);

                Debug.Log("type " + type + " are " + items.Length);
                
                if (type == "*.fbx" && items.Length > 0)
                {
                    FBXFiles = new FBXFile[items.Length];
                    for (int i = 0; i < items.Length; i++)
                    {
                        //string path = items[i].FullName;//.Substring(items[i].FullName.IndexOf("Assets/"));
                        string path = items[i].FullName.Substring(items[i].FullName.IndexOf("Assets\\"));
                        if ((AnimationClip)AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)))
                        {
                            Debug.Log(items[i].FullName);
                            FBXFiles[i] = new FBXFile();
                            FBXFiles[i].FILE_Info = items[i];
                            FBXFiles[i].Object = (GameObject)AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                            FBXFiles[i].Import = true;
                            FBXFileNameList.Add(items[i].FullName);
                        }
                    }

                    b_success = true;
                }
                else
                    b_success = false;
            }
            else
            {
                //Files = new BVHFile[0];
                FBXFiles = null;
                b_success = false;
            }
        }
        else
        {
            //Files = new BVHFile[0];
            FBXFiles = null;
            b_success = false;
        }
        return b_success;
    }

    public bool ImportFBXData(int fileindex, float scale)
    {
        bool b_true = false;
        //Debug.Log(FBXFiles[fileindex].FILE_Info.Name);
        string fileName = FBXFiles[fileindex].FILE_Info.Name.Replace(".fbx", "");
        if (FBXFiles[fileindex].Import)
        {

            //AssetDatabase.CreateFolder(destination, Files[f].Object.name);
            //MotionData data = ScriptableObject.CreateInstance<MotionData>();
            //data.name = "Data";
            //AssetDatabase.CreateAsset(data, destination + "/" + Files[f].Object.name + "/" + data.name + ".asset");


            AnimationClip clip = (AnimationClip)AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(FBXFiles[fileindex].Object), typeof(AnimationClip));

            //Create Source Data
            //data.Source = new MotionData.Hierarchy();
            FBXFiles[fileindex].skel_hierarchy = new Skeleton.Hierarchy();
            FBXFiles[fileindex].Character = Character;
            Debug.Log("Character length " + Character.Bones.Length);
            for (int i = 0; i < Character.Bones.Length; i++)
            {
                FBXFiles[fileindex].skel_hierarchy.AddBone(Character.Bones[i].GetName(), Character.Bones[i].GetParent() == null ? "None" : Character.Bones[i].GetParent().GetName());
            }

            //Set Frames
            //ArrayExtensions.Resize(ref data.Frames, Mathf.RoundToInt((float)Framerate * clip.length));
            FBXFiles[fileindex].nFrames = Mathf.RoundToInt((float)Framerate * clip.length);
            FBXFiles[fileindex].InitializeMatrix(FBXFiles[fileindex].GetTotalFrames(), FBXFiles[fileindex].skel_hierarchy.Bones.Length);
            
            //Set Framerate
            FBXFiles[fileindex].Framerate = (float)Framerate;

            //Compute Frames
            Debug.Log("select " + fileindex + " nFrames " + FBXFiles[fileindex].Motion.Length);

            for (int i = 0; i < FBXFiles[fileindex].GetTotalFrames(); i++)
            {
                clip.SampleAnimation(Character.gameObject, (float)i / FBXFiles[fileindex].Framerate);
                for (int j = 0; j < Character.Bones.Length; j++)
                {
                    //data.Frames[i].World[j] = Character.Bones[j].Transform.GetWorldMatrix();
                    FBXFiles[fileindex].Motion[i][j] = Character.Bones[j].Transform.GetWorldMatrix();
                    
                }
            }

            
            //Add Scene
            //CreateScene("Assets");

            //EditorUtility.SetDirty(Motion);


            if (FBXFiles[fileindex].GetTotalFrames() == 1)
            {
                Matrix4x4[] reference = FBXFiles[fileindex].Motion.First();
                ArrayExtensions.Resize(ref FBXFiles[fileindex].Motion, Mathf.RoundToInt(FBXFiles[fileindex].Framerate));
                for (int k = 0; k < FBXFiles[fileindex].Motion.Length; k++)
                {
                    //Debug.Log("index " + k);
                    //data.Frames[k] = new Frame(data, k + 1, (float)k / data.Framerate);
                    //data.Frames[k].World = (Matrix4x4[])reference.World.Clone();
                    FBXFiles[fileindex].Motion[k] = (Matrix4x4[])reference.Clone();
                }
            }
            
            Motion = FBXFiles[fileindex].Motion;
            DirectoryName = FBXFiles[fileindex].FILE_Info.DirectoryName;
            FileName = FBXFiles[fileindex].FILE_Info.Name;

            CurFrame = 0;
            b_true = true;
            b_data = true;
        }
        else
        {
            Debug.Log("File with name " + fileName + " already exists.");
            b_true = false;
        }

        return b_true;
    }

    /* -- Motion Text File -- */

    public List<List<float>> recordedList = new List<List<float>>();
   

    private RecordingState recordingState = RecordingState.NONE;
    public RecordingState RecordingState
    {
        get => recordingState;
        set
        {
            // if state changed
            if (recordingState != value)
            {
                // previouse state
                switch (recordingState)
                {
                    case RecordingState.NONE:
                        break;
                    case RecordingState.RECORDING:
                        Debug.Log("End of recording.");
                        break;
                    case RecordingState.TESTPLAYING:
                        Debug.Log("Test-playing recorded animation data is finished.");
                        break;
                    case RecordingState.PLAYING:
                        Debug.Log("Playing animation data is finished.");
                        break;
                }

                recordingState = value;

                // current changed state
                switch (recordingState)
                {
                    case RecordingState.NONE:
                        break;
                    case RecordingState.RECORDING:
                        Debug.Log("Start recording!");
                        break;
                    case RecordingState.TESTPLAYING:
                        Debug.Log("Start test-playing recorded animation data");
                        break;
                    case RecordingState.PLAYING:
                        Debug.Log("Start playing loaded animation data");
                        break;
                }
            }
        }
    }
    public bool LoadMotionTextDirectory(string Source, string type)
    {
        bool b_success;
        if (MotionTextFileNameList.Count > 0)
            MotionTextFileNameList.Clear();
        if (!string.IsNullOrEmpty(Source))
        {
            if (Directory.Exists(Source))
            {
                DirectoryInfo info = new DirectoryInfo(Source);
                Debug.Log("info " + info.FullName + type);
                FileInfo[] items = info.GetFiles(type);

                Debug.Log("type " + type + " are " + items.Length);
                if (type == "*_motion.txt" && items.Length > 0)
                {
                    MotionTextFiles = new MotionTextFile[items.Length];
                    for (int i = 0; i < items.Length; i++)
                    {
                        MotionTextFiles[i] = new MotionTextFile();
                        MotionTextFiles[i].FILE_Info = items[i];
                        MotionTextFiles[i].Import = true;
                        MotionTextFiles[i].Character = Character;
                        MotionTextFileNameList.Add(items[i].FullName);
                    }

                    b_success = true;
                }
                else
                    b_success = false;
            }
            else
            {
                //Files = new BVHFile[0];
                MotionTextFiles = null;
                b_success = false;
            }
        }
        else
        {
            //Files = new BVHFile[0];
            MotionTextFiles = null;
            b_success = false;
        }
        return b_success;
    }

    public void RecordingPose(Actor actor)
    {
        List<float> jointlist = new List<float>();

        // Add global position of pelvis
        jointlist.Add(actor.Bones[0].Transform.position.x);
        jointlist.Add(actor.Bones[0].Transform.position.y);
        jointlist.Add(actor.Bones[0].Transform.position.z);

        // Add local quarternion rotation of all joints
        for (int i = 0; i < actor.Bones.Length; i++)
        {
            jointlist.Add(actor.Bones[i].Transform.rotation.x);
            jointlist.Add(actor.Bones[i].Transform.rotation.y);
            jointlist.Add(actor.Bones[i].Transform.rotation.z);
            jointlist.Add(actor.Bones[i].Transform.rotation.w);
        }

        //Debug.Log("jointlist : " + jointlist.Count);

        if (jointlist.Count == 3 + (int)actor.Bones.Length * 4)
        {
            // Add to framelist
            recordedList.Add(jointlist);
        }
        else
        {
            Debug.LogError("The number of bones is not correct. it should be " + (int)actor.Bones.Length);
            RecordingState = RecordingState.NONE;
        }
    }

    public void SavingRecordedData()
    {
        string writeFilepath = EditorUtility.SaveFilePanel("Overwrite with txt", "", "Test" + "_motion.txt", "txt");
        Debug.Log("Filepath : " + writeFilepath);
        if (writeFilepath.Length != 0)
        {
            // Write
            string[,] output = new string[recordedList.Count, recordedList[0].Count];
            
            for (int i = 0; i < recordedList.Count; i++)
            {
                for (int j = 0; j < recordedList[0].Count; j++)
                {
                    output[i, j] = recordedList[i][j].ToString();
                }
            }
            int length = output.GetLength(0);
            int clength = output.GetLength(1);
            string delimiter = " ";
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index < length; index++)
            {
                string line = output[index, 0];
                for (int cindex = 1; cindex < clength; cindex++)
                {
                    line = line + delimiter + output[index, cindex];
                }
                stringBuilder.AppendLine(line);
            }
            StreamWriter outStream = File.CreateText(writeFilepath);
            outStream.Write(stringBuilder);
            outStream.Close();

            recordedList.Clear();
        }
    }
    public void SavingRecordedData(string FileName, string CharacterName)
    {
        string writeFilepath = FileName +"_"+ CharacterName + "_motion.txt";//EditorUtility.SaveFilePanel("Overwrite with txt", "", , "txt");

        Debug.Log("Filepath : " + writeFilepath);
        if (writeFilepath.Length != 0)
        {
            // Write
            string[,] output = new string[recordedList.Count, recordedList[0].Count];

            for (int i = 0; i < recordedList.Count; i++)
            {
                for (int j = 0; j < recordedList[0].Count; j++)
                {
                    output[i, j] = recordedList[i][j].ToString();
                }
            }
            int length = output.GetLength(0);
            int clength = output.GetLength(1);
            string delimiter = " ";
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index < length; index++)
            {
                string line = output[index, 0];
                for (int cindex = 1; cindex < clength; cindex++)
                {
                    line = line + delimiter + output[index, cindex];
                }
                stringBuilder.AppendLine(line);
            }
            StreamWriter outStream = File.CreateText(writeFilepath);
            outStream.Write(stringBuilder);
            outStream.Close();

            recordedList.Clear();
        }
    }

    private void MotionUpdate(List<List<float>> frameList, int fileindex, int frameIdx)
    {

        for (int j = 0; j < MotionTextFiles[fileindex].Character.Bones.Length; j++)
        {
            if (j == 0)
            {
                Vector3 base_position = new Vector3(frameList[frameIdx][0], frameList[frameIdx][1], frameList[frameIdx][2]);
                Quaternion rotation = new Quaternion(frameList[frameIdx][3], frameList[frameIdx][4], frameList[frameIdx][5], frameList[frameIdx][6]);
                MotionTextFiles[fileindex].Character.Bones[j].Transform.SetPositionAndRotation(base_position, rotation);
                MotionTextFiles[fileindex].Motion[frameIdx][j].SetTRS(MotionTextFiles[fileindex].Character.Bones[j].Transform.position,
                    MotionTextFiles[fileindex].Character.Bones[j].Transform.rotation, Vector3.one);
            }
            else
            {
                //Debug.Log(" joint name " + RetargetingTarget.actor.Bones[j].GetName());
                Quaternion rotation = new Quaternion(
                    frameList[frameIdx][3 + 4 * j + 0],
                    frameList[frameIdx][3 + 4 * j + 1],
                    frameList[frameIdx][3 + 4 * j + 2],
                    frameList[frameIdx][3 + 4 * j + 3]);
                rotation = rotation.normalized;
                MotionTextFiles[fileindex].Character.Bones[j].Transform.rotation = rotation;
                MotionTextFiles[fileindex].Motion[frameIdx][j].SetTRS(MotionTextFiles[fileindex].Character.Bones[j].Transform.position,
                    MotionTextFiles[fileindex].Character.Bones[j].Transform.rotation, Vector3.one);
            }

        }
    }

    public bool ImportMotionTextData(int fileindex, float scale)
    {
        bool b_true = false;
        string fileName = MotionTextFiles[fileindex].FILE_Info.Name.Replace("_motion.txt", "");
        if (MotionTextFiles[fileindex].Import)
        {
            List<List<float>> loadedList = new List<List<float>>();
            
            string fileContnet = File.ReadAllText(MotionTextFiles[fileindex].FILE_Info.FullName);
            //Debug.Log(fileContnet);
            string[] rows = fileContnet.Split('\n');
            rows = new List<string>(rows).GetRange(0, rows.Length - 1).ToArray();

            foreach (string row in rows)
            {
                List<float> jointlist = new List<float>();
                string[] componants = row.Split(' ');
                //Debug.Log(componants[0]);
                foreach (string componant in componants)
                {
                    float parse = 0f;
                    if (float.TryParse(componant, out parse))
                    {
                        jointlist.Add(parse);
                    }
                }
                //Debug.Log(" DOF " + jointlist.Count);
                if (jointlist.Count == 3 + (int)MotionTextFiles[fileindex].Character.Bones.Length * 4)
                {
                    
                    loadedList.Add(jointlist);
                }
            }
            int totalFrames = loadedList.Count;
            
            Debug.Log("Total Frames " + totalFrames);
            MotionTextFiles[fileindex].nFrames = totalFrames;
            // update Motion data
            MotionTextFiles[fileindex].InitializeMatrix(MotionTextFiles[fileindex].nFrames, MotionTextFiles[fileindex].Character.Bones.Length);
            for (int frameidx=0; frameidx < MotionTextFiles[fileindex].nFrames; frameidx++)
            {
                //Debug.Log(frameidx);
                MotionUpdate(loadedList, fileindex, frameidx);
            }
            

            if (MotionTextFiles[fileindex].GetTotalFrames() == 1)
            {
                Matrix4x4[] reference = MotionTextFiles[fileindex].Motion.First();
                ArrayExtensions.Resize(ref MotionTextFiles[fileindex].Motion, Mathf.RoundToInt(MotionTextFiles[fileindex].Framerate));
                for (int k = 0; k < MotionTextFiles[fileindex].Motion.Length; k++)
                {
                    Debug.Log("index " + k);
                    MotionTextFiles[fileindex].Motion[k] = (Matrix4x4[])reference.Clone();
                }
            }

            Motion = MotionTextFiles[fileindex].Motion;
            DirectoryName = MotionTextFiles[fileindex].FILE_Info.DirectoryName;
            FileName = MotionTextFiles[fileindex].FILE_Info.Name;
            CurFrame = 0;
            b_true = true;
            b_data = true;
        }
        else
        {
            Debug.Log("File with name " + fileName + " already exists.");
            b_true = false;
        }

        return b_true;

    }


    public Actor CreateActor(int fileindex)
    {
        Actor actor = new GameObject("Skeleton").AddComponent<Actor>();
        List<Transform> instances = new List<Transform>();
        for (int i = 0; i < BVHFiles[fileindex].skel_hierarchy.Bones.Length; i++)
        {
            Transform instance = new GameObject(BVHFiles[fileindex].skel_hierarchy.Bones[i].Name).transform;
            instance.SetParent(BVHFiles[fileindex].skel_hierarchy.Bones[i].Parent == "None" ? actor.GetRoot() : actor.FindTransform(BVHFiles[fileindex].skel_hierarchy.Bones[i].Parent));
            Matrix4x4 matrix = BVHFiles[fileindex].Motion.First()[i];//.GetBoneTransformation(i, false);
            instance.position = matrix.GetPosition();
            instance.rotation = matrix.GetRotation();
            instance.localScale = Vector3.one;
            instances.Add(instance);
        }
        Transform root = actor.FindTransform(BVHFiles[fileindex].skel_hierarchy.Bones[0].Name);
        root.position = new Vector3(0f, root.position.y, 0f);
        root.rotation = Quaternion.Euler(root.eulerAngles.x, 0f, root.eulerAngles.z);
        actor.ExtractSkeleton(instances.ToArray());
        return actor;
    }

    public void inspector()
    {

    }
    public void Inspector_PlayMode(out int Frame)
    {
        //// bool connection
        //EditorGUILayout.BeginHorizontal();
        //GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
        //b_play = EditorGUILayout.Toggle("b_play", b_play);
        //GUILayout.FlexibleSpace();
        //EditorGUILayout.EndHorizontal();

        if (Utility.GUIButton("reset & play animation", Color.white, Color.red))
        {
            CurFrame = 0;
            b_play = true;

        }
        if (b_play == false)
        {
            if (Utility.GUIButton("re-play animation", Color.white, Color.red))
            {
                b_play = true;
            }
        }
        if (b_play == true)
        {
            if (Utility.GUIButton("pause animation", Color.white, Color.red))
            {
                b_data = true;
                b_play = false;
            }
        }
        if (b_play != true && Motion != null)
            CurFrame = EditorGUILayout.IntSlider(CurFrame, 1, Motion.Length - 1);

        Frame = CurFrame;
    }
    public void FBX_inspector(Actor _actor)
    {
        EditorGUILayout.BeginHorizontal();

        if (Utility.GUIButton("Motion Loader: Load FBX Directory", Color.white, Color.blue))
        {
            EditorApplication.delayCall += () =>
            {
                string dataPath = EditorUtility.OpenFolderPanel("FBX Folder", "", "Assets");
                file_exist = LoadFBXDirectory(dataPath, "*.fbx");
            };
        }
        EditorGUILayout.EndHorizontal();

        if (FBXFileNameList.Count > 0)
        {
            Character = _actor;

            //Debug.Log("Let's see " + Character.Bones.Length);
            // Target Framerate
            EditorGUILayout.BeginHorizontal();
            Framerate = EditorGUILayout.FloatField("FBX Loader : Target FPS", Framerate);
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            selectedData = EditorGUILayout.Popup("FBX Loader : Select FBX Motion Data", selectedData, FBXFileNameList.ToArray());
            if (Utility.GUIButton("FBX Loader : Import FBX Data", Color.white, Color.blue))
            {
                ImportFBXData(selectedData, scale);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            scale = EditorGUILayout.FloatField("Scale", scale);
            EditorGUILayout.EndHorizontal();


        }


    }
    public void BVH_inspector()
    {
        //EditorGUILayout.BeginHorizontal(); // 나중에 FBX, Motion TEXT File 도 쓸 수 있도록 inspector 를 만들던지 각 파일의 inspector 에 추가하도록 한다.
        //LeftShoulder = EditorGUILayout.TextField("LeftShoulder", LeftShoulder);
        //RightShoulder = EditorGUILayout.TextField("RightShoulder", RightShoulder);
        //EditorGUILayout.EndHorizontal();

        //EditorGUILayout.BeginHorizontal();
        //LeftHip = EditorGUILayout.TextField("LeftHip", LeftHip);
        //RightHip = EditorGUILayout.TextField("RightHip", RightHip);
        //EditorGUILayout.EndHorizontal();


        EditorGUILayout.BeginHorizontal();

        if (Utility.GUIButton("Motion Loader: Load BVH Directory", Color.white, Color.red))
        {
            EditorApplication.delayCall += () =>
            {
                string dataPath = EditorUtility.OpenFolderPanel("BVH Folder", "", "Assets");
                DirectoryName = dataPath;
                file_exist = LoadBVHDirectory(dataPath, "*.bvh");
            };
        }
        EditorGUILayout.EndHorizontal();

       
        if (BVHFileNameList.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();

            selectedData = EditorGUILayout.Popup("Select Motion Data", selectedData, BVHFileNameList.ToArray());

            if (Utility.GUIButton("Motion Loader : Import BVH Data", Color.white, Color.red))
            {
                ImportBVHData(selectedData, scale);
                //GenerateRootTrajectory(selectedData);
            }
         
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            scale = EditorGUILayout.FloatField("Scale", scale);
            EditorGUILayout.EndHorizontal();

            if (Utility.GUIButton("Motion Loader : Create Actor", Color.white, Color.red))
            {
                CreateActor(selectedData);
            }

            

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();  // 고정된 여백을 넣습니다.
                                        //EditorGUILayout.LabelField("b_connect", GUILayout.Width(100f));
            b_draw_root_trajectory = EditorGUILayout.Toggle("b_draw_root_trajectory", b_draw_root_trajectory);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        

    }
    public void MotionTextFile_inspector(Actor _actor)
    {
        Utility.ResetGUIColor();
        Utility.SetGUIColor(UltiDraw.LightGrey);

        Character = _actor;


        EditorGUILayout.BeginHorizontal();
        if (Utility.GUIButton("Motion Loader: Load Motion Text File Directory", Color.white, Color.red))
        {
            EditorApplication.delayCall += () =>
            {
                string dataPath = EditorUtility.OpenFolderPanel("Text File Folder", "", "Assets");
                file_exist = LoadMotionTextDirectory(dataPath, "*_motion.txt");
            };
        }
        EditorGUILayout.EndHorizontal();


        if (MotionTextFileNameList.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();

            selectedData = EditorGUILayout.Popup("Select Motion Data", selectedData, MotionTextFileNameList.ToArray());

            if (Utility.GUIButton("Motion Loader : Import Motion Text Data", Color.white, Color.red))
            {
                ImportMotionTextData(selectedData, scale);
                
            }

            EditorGUILayout.EndHorizontal();
        }

        if (!Application.isPlaying) return;

    
        
    }
   
    public void clearFile()
    {
        BVHFiles.Initialize();
        FBXFiles.Initialize();
        MotionTextFiles.Initialize();
    }
}
