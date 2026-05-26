using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Text;
using System.IO;

public class WriterClass
{
    private StreamWriter File_record;
    private StringBuilder sb_record; // string builder 
    public StreamWriter CreateFile(string foldername, string name, bool newfile, string root_extension)
    {
        string filename = string.Empty;
        string folder = foldername;
        if (!File.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            folder = folder + '/';
        }
        else
            folder = folder + "/";
        if (!File.Exists(folder + name + root_extension))
        {
            filename = folder + name + root_extension;
        }
        else
        {
            if (newfile)
            {
                int i = 1;
                while (File.Exists(folder + name + "_" + i + "_" + root_extension))
                {
                    i += 1;
                }
                filename = folder + name + "_" + i + "_" + root_extension;
            }
            else
                filename = folder + name + root_extension;
        }
        return File.CreateText(filename);
    }
    public StringBuilder WriteFloat(StringBuilder sb_, float x, bool first)
    {
        if (first)
        {
            sb_.Append(x);
        }
        else
        {
            sb_.Append(" ");
            sb_.Append(x);
        }
        return sb_;
    }
    public StringBuilder WriteString(StringBuilder sb_, string x, bool first)
    {
        if (first)
        {
            sb_.Append(x);
        }
        else
        {
            sb_.Append(" ");
            sb_.Append(x);
        }
        return sb_;
    }
    public StringBuilder WritePosition(StringBuilder sb_, Vector3 position, bool first)
    {
        sb_ = WriteFloat(sb_, position.x, first);
        sb_ = WriteFloat(sb_, position.y, false);
        sb_ = WriteFloat(sb_, position.z, false);

        return sb_;
    }
    public StringBuilder WriteQuat(StringBuilder sb_, Quaternion quat, bool first)
    {
        sb_ = WriteFloat(sb_, quat.x, first);
        sb_ = WriteFloat(sb_, quat.y, false);
        sb_ = WriteFloat(sb_, quat.z, false);
        sb_ = WriteFloat(sb_, quat.w, false);

        return sb_;
    }
    public bool WriteMatData(string DirectoryPath, string filename, Matrix4x4 root_mat)
    {
        
        if (Directory.Exists(DirectoryPath))
        {
            Debug.Log("wrtie matrix on " + filename);

            File_record = CreateFile(DirectoryPath, filename,true,".txt");
            sb_record = new StringBuilder();

            sb_record = WritePosition(sb_record, root_mat.GetPosition(), true);
            sb_record = WriteQuat(sb_record, root_mat.GetRotation(), false);

            File_record.WriteLine(sb_record.ToString());

            File_record.Close();
            sb_record.Clear();

            return true;
        }
        else
        {
            return false;
        }
    }
    

}

public class ImporterClass
{
    public BVHFile[] Files = null;// new BVHFile[0];
    public string Filter = string.Empty;
    public BVHFile[] Instances = new BVHFile[0];
    public Matrix4x4[][] Motion = new Matrix4x4[0][];
    public Matrix4x4[][] GTMotion = new Matrix4x4[0][];
    //public MotionData data;

    public float scale = 100.0f;
    public float[][] Prob = new float[0][];

    public float[][] Goals = new float[0][];
    public static float[][] RootMat = new float[0][];
    public float[][] Markers = new float[0][];

    public string Destination = "";
    public bool LoadDirectory(string Source, string type)
    {
        if (!string.IsNullOrEmpty(Source))
        {
            if (Directory.Exists(Source))
            {
                DirectoryInfo info = new DirectoryInfo(Source);
                FileInfo[] items = info.GetFiles(type);
                Files = new BVHFile[items.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    Files[i] = new BVHFile();
                    Files[i].Object = items[i];
                    Files[i].Import = true;
                }
            }
            else
            {
                //Files = new BVHFile[0];
                Files = null;
                return false;
            }
        }
        else
        {
            //Files = new BVHFile[0];
            Files = null;
            return false;
        }
        return ApplyFilter();
    }

    public bool ApplyFilter()
    {
        if (Filter == string.Empty)
        {
            Instances = Files;
        }
        else
        {
            List<BVHFile> instances = new List<BVHFile>();
            for (int i = 0; i < Files.Length; i++)
            {
                if (Files[i].Object.Name.ToLowerInvariant().Contains(Filter.ToLowerInvariant()))
                {
                    instances.Add(Files[i]);
                }
            }
            Instances = instances.ToArray();
        }

        return true;
    }

    public static bool ImportTextRootData(string DirectoryPath, string filename)
    {
        if (!Directory.Exists(DirectoryPath))
        {
            Debug.Log("Folder " + "'" + DirectoryPath + "'" + " is not valid.");
            return false;

        }
        else
        {
            string folder = DirectoryPath + "/" + filename + ".txt";
            if (File.Exists(folder))
            {
                string[] Output_pose; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
                Output_pose = FileUtility.ReadAllLines(folder);

                if (Output_pose.Length != 0)
                {

                    RootMat = new float[Output_pose.Length][];
                    for (int g = 0; g < Output_pose.Length; g++)
                    {
                        RootMat[g] = FileUtility.ReadArray(Output_pose[g]);
                    }

                    return true;
                }
                else
                {
                    Debug.Log("Error!: there is no Data in text file");
                    return false;
                }
            }
            else
            {
                Debug.Log("there is no file " + folder);
                return false;
            }

        }
    }
    public bool ImportTextGoalData(string DirectoryPath, string filename)
    {
        if (!Directory.Exists(DirectoryPath))
        {
            Debug.Log("Folder " + "'" + DirectoryPath + "'" + " is not valid.");
            return false;


        }
        else
        {
            string folder = DirectoryPath + "/" + filename + ".txt";
            if (File.Exists(folder))
            {
                string[] Output_pose; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
                Output_pose = FileUtility.ReadAllLines(folder);

                if (Output_pose.Length != 0)
                {

                    Goals = new float[Output_pose.Length][];
                    for (int g = 0; g < Output_pose.Length; g++)
                    {
                        Goals[g] = FileUtility.ReadArray(Output_pose[g]);
                    }

                    return true;
                }
                else
                {
                    Debug.Log("Error!: there is no Data in text file");
                    return false;
                }
            }
            else
            {
                Debug.Log("there is no file " + folder);
                return false;
            }

        }
    }

    public bool ImportTxTData(string DirectoryPath, string filename, out float[][] Data)
    {
        Data = new float[1][];
        if (!Directory.Exists(DirectoryPath))
        {
            Debug.Log("Folder " + "'" + DirectoryPath + "'" + " is not valid.");
            return false;


        }
        else
        {
            string folder = DirectoryPath + "/" + filename + ".txt";
            if (File.Exists(folder))
            {
                string[] Output_pose; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
                Output_pose = FileUtility.ReadAllLines(folder);

                if (Output_pose.Length != 0)
                {

                    Data = new float[Output_pose.Length][];
                    for (int g = 0; g < Output_pose.Length; g++)
                    {
                        Data[g] = FileUtility.ReadArray(Output_pose[g]);
                    }

                    return true;
                }
                else
                {
                    Debug.Log("Error!: there is no Data in text file");
                    return false;
                }
            }
            else
            {
                Debug.Log("there is no file " + folder);
                return false;
            }

        }
    }

    public bool ImportConnectionDataProb(string message, Actor _actor, Matrix4x4 root_mat, out float[] prob)
    {
        var splittedStrings = message.Split(' ');
        //Debug.Log(" get " + splittedStrings[0] + " " + splittedStrings.Length);
        int total_length = (22 * 4 + 3) + 1 + (10);
        prob = new float[10];
        if (splittedStrings[0] == "Hello" && splittedStrings.Length == total_length)
        {
            float x; float y; float z;
            float.TryParse(splittedStrings[1 + 0], out x);
            float.TryParse(splittedStrings[1 + 1], out y);
            float.TryParse(splittedStrings[1 + 2], out z);
            Vector3 pos = new Vector3(x, y, z);

            //_actor.Bones[0].Transform.position = pos / 100.0f;
            //pos = pos / 100.0f;

            _actor.Bones[0].Transform.position = pos;

            for (int j = 0; j < _actor.Bones.Length; j++)
            {
                float w;
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 0], out x);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 1], out y);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 2], out z);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 3], out w);

                Quaternion quat_lH = new Quaternion(x, y, z, w);

                _actor.Bones[j].Transform.localRotation = quat_lH;

            }

            Matrix4x4 bone_hip = _actor.Bones[0].Transform.GetWorldMatrix();
            bone_hip = bone_hip.GetRelativeTransformationFrom(root_mat);
            _actor.Bones[0].Transform.SetPositionAndRotation(bone_hip.GetPosition(), bone_hip.GetRotation());


            for (int k = 0; k < 10; k++)
            {
                float.TryParse(splittedStrings[(1 + 4 * 22 + 3) + k], out x);
                prob[k] = x;
            }


            return true;

        }
        else
        {
            Debug.Log("check pose data");
            return false;
        }


    }
    public bool ImportConnectionData(string message, Actor _actor, Quaternion[] Default_Mat, Matrix4x4 root_mat)
    {
        var splittedStrings = message.Split(' ');
        //Debug.Log(" get " + splittedStrings[0] + " " + splittedStrings.Length);
        int total_length = (_actor.Bones.Length * 4 + 3) + 1;
        if (splittedStrings[0] == "Hello" && splittedStrings.Length == total_length)
        {
            float x; float y; float z;
            float.TryParse(splittedStrings[1 + 0], out x);
            float.TryParse(splittedStrings[1 + 1], out y);
            float.TryParse(splittedStrings[1 + 2], out z);
            Vector3 pos = new Vector3(x, y, z);

            //_actor.Bones[0].Transform.position = pos / 100.0f;
            _actor.Bones[0].Transform.position = pos;

            for (int j = 0; j < _actor.Bones.Length; j++)
            {
                float w;
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 0], out x);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 1], out y);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 2], out z);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 3], out w);

                Quaternion quat_lH = new Quaternion(x, y, z, w);
                quat_lH = quat_lH.normalized;
                _actor.Bones[j].Transform.localRotation = Default_Mat[j] * quat_lH;

            }

            Matrix4x4 bone_hip = _actor.Bones[0].Transform.GetWorldMatrix();
            bone_hip = bone_hip.GetRelativeTransformationFrom(root_mat);
            _actor.Bones[0].Transform.SetPositionAndRotation(bone_hip.GetPosition(), bone_hip.GetRotation());


            return true;

        }
        else
        {
            Debug.Log("check pose data");
            return false;
        }


    }
    public bool ImportConnectionData(string message, Actor _actor, Matrix4x4 root_mat, float scale)
    {
        var splittedStrings = message.Split(' ');
        //Debug.Log(" get " + splittedStrings[0] + " " + splittedStrings.Length);
        //Debug.Log(" scale " + scale);
        int total_length = (_actor.Bones.Length * 4 + 3) + 1;
        if (splittedStrings[0] == "Hello" && splittedStrings.Length == total_length)
        {
            float x; float y; float z;
            float.TryParse(splittedStrings[1 + 0], out x);
            float.TryParse(splittedStrings[1 + 1], out y);
            float.TryParse(splittedStrings[1 + 2], out z);
            Vector3 pos = new Vector3(x, y, z);

            //_actor.Bones[0].Transform.position = pos / 100.0f;
            pos = pos / scale;
            _actor.Bones[0].Transform.position = pos;
            //Debug.Log(" pos " + pos + "actor" + _actor.Bones[0].Transform.position);

            for (int j = 0; j < _actor.Bones.Length; j++)
            {
                float w;
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 0], out x);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 1], out y);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 2], out z);
                float.TryParse(splittedStrings[(1 + 4 * j + 3) + 3], out w);

                Quaternion quat_lH = new Quaternion(x, y, z, w);

                _actor.Bones[j].Transform.localRotation = quat_lH;

            }

            return true;

        }
        else
        {
            Debug.Log("check pose data");
            return false;
        }


    }

    public bool ImportOutputMotionData(string DirectoryPath, int fileindex, int bones, Matrix4x4 root_mat, out Matrix4x4[][] Motion)
    {
        Motion = new Matrix4x4[1][];
        string destination = DirectoryPath;
        if (!Directory.Exists(destination))
        {
            Debug.Log("Folder " + "'" + destination + "'" + " is not valid.");
            return false;
        }
        else
        {
            bool b_true;
            Debug.Log("load File : " + Files[fileindex].Object.Name);
            if (Files[fileindex].Import)
            {
                string[] Output; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
                Output = FileUtility.ReadAllLines(Files[fileindex].Object.FullName);

                Motion = new Matrix4x4[Output.Length][];

                for (int k = 0; k < Output.Length; k++)
                {
                    float[] pose_data = FileUtility.ReadArray(Output[k]);
                    Motion[k] = new Matrix4x4[bones];
                    for (int i = 0; i < bones; i++)
                    {
                        //Debug.Log("see " + pose_data.Length);

                        Matrix4x4 world_mat = new Matrix4x4();

                        Vector3 position = new Vector3(pose_data[7 * i + 0], pose_data[7 * i + 1], pose_data[7 * i + 2]);
                        Quaternion quat = new Quaternion(pose_data[7 * i + 3], pose_data[7 * i + 4], pose_data[7 * i + 5], pose_data[7 * i + 6]);

                        world_mat.SetTRS(position, quat, Vector3.one);

                        Motion[k][i] = world_mat.GetRelativeTransformationFrom(root_mat);
                    }

                }
                

                // probabilitiy
                string fileName = Files[fileindex].Object.FullName.Replace(".csv", "_prob.csv");
                Debug.Log(fileName);
                ImportTextFloatArrayData(fileName, 10, out Prob);

                b_true = true;
            }
            else
            {
                b_true = false;
            }

            return b_true;
        }
    }

    public bool ImportTextMarkerData(string DirectoryPath, string filename)
    {
        if (!Directory.Exists(DirectoryPath))
        {
            Debug.Log("Folder " + "'" + DirectoryPath + "'" + " is not valid.");
            return false;


        }
        else
        {
            string folder = DirectoryPath + "/" + filename + ".txt";
            if (File.Exists(folder))
            {
                string[] Output_pose; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
                Output_pose = FileUtility.ReadAllLines(folder);

                if (Output_pose.Length != 0)
                {

                    Markers = new float[Output_pose.Length][];
                    for (int g = 0; g < Output_pose.Length; g++)
                    {
                        Markers[g] = FileUtility.ReadArray(Output_pose[g]);
                    }

                    return true;
                }
                else
                {
                    Debug.Log("Error!: there is no Data in text file");
                    return false;
                }
            }
            else
            {
                Debug.Log("there is no file " + folder);
                return false;
            }

        }
    }

    public bool ImportTextFloatArrayData(string FullName, int rows, out float[][] _outarray)
    {

        string[] Output; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
        Output = FileUtility.ReadAllLines(FullName);

        _outarray = new float[Output.Length][];

        for (int k = 0; k < Output.Length; k++)
        {
            float[] pose_data = FileUtility.ReadArray(Output[k]);
            _outarray[k] = new float[rows];
            for (int i = 0; i < rows; i++)
            {
                _outarray[k][i] = pose_data[i];
            }

        }

        return true;

    }
    public bool ImportStringArrayData(string FullName, int rows, out string[][] _outarray)
    {
        string[] Output; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
        Output = FileUtility.ReadAllLines(FullName);

        _outarray = new string[Output.Length][];

        for (int k = 0; k < Output.Length; k++)
        {
            string[] pose_data = FileUtility.ReadStringArray(Output[k]);
            _outarray[k] = new string[rows];
            for (int i = 0; i < rows; i++)
            {
                _outarray[k][i] = pose_data[i];
            }

        }

        return true;

    }

    public bool ImportTextData(string DirectoryPath, string filename)
    {
        if (!Directory.Exists(DirectoryPath))
        {
            Debug.Log("Folder " + "'" + DirectoryPath + "'" + " is not valid.");
            return false;


        }
        else
        {
            string folder = DirectoryPath + "/" + filename + ".txt";
            if (File.Exists(folder))
            {
                string[] Output_pose; // nFrames, String RawData ( 69 + 30 + 2640 + 3 + 1 )
                Output_pose = FileUtility.ReadAllLines(folder);

                if (Output_pose.Length != 0)
                {

                    Markers = new float[Output_pose.Length][];
                    for (int g = 0; g < Output_pose.Length; g++)
                    {
                        Markers[g] = FileUtility.ReadArray(Output_pose[g]);
                    }

                    return true;
                }
                else
                {
                    Debug.Log("Error!: there is no Data in text file");
                    return false;
                }
            }
            else
            {
                Debug.Log("there is no file " + folder);
                return false;
            }

        }
    }
    private StreamWriter File_record;
    private StringBuilder sb_record;
    private StreamWriter CreateFile(string foldername, string name)
    {
        string filename = string.Empty;
        string folder = foldername;
        if (!File.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            folder = folder + '/';
        }
        else
            folder = folder + "/";
        if (!File.Exists(folder + name + ".txt"))
        {
            filename = folder + name;
        }
        else
        {
            //int i = 1;
            //while (File.Exists(folder + name + " (" + i + ").txt"))
            //{
            //    i += 1;
            //}
            //filename = folder + name + " (" + i + ")";
            filename = folder + name;
        }
        return File.CreateText(filename + ".txt");
    }
    public StreamWriter CreateFile(string foldername, string name, bool newfile, string root_extension)
    {
        string filename = string.Empty;
        string folder = foldername;
        if (!File.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            folder = folder + '/';
        }
        else
            folder = folder + "/";
        if (!File.Exists(folder + name + root_extension))
        {
            filename = folder + name + root_extension;
        }
        else
        {
            if (newfile)
            {
                int i = 1;
                while (File.Exists(folder + name + "_" + i + "_" + root_extension))
                {
                    i += 1;
                }
                filename = folder + name + "_" + i + "_" + root_extension;
            }
            else
                filename = folder + name + root_extension;
        }
        return File.CreateText(filename);
    }
    private StringBuilder WriteFloat(StringBuilder sb_, float x, bool first)
    {
        if (first)
        {
            sb_.Append(x);
        }
        else
        {
            sb_.Append(" ");
            sb_.Append(x);
        }
        return sb_;
    }
    private StringBuilder WritePosition(StringBuilder sb_, Vector3 position, bool first)
    {
        sb_ = WriteFloat(sb_, position.x, first);
        sb_ = WriteFloat(sb_, position.y, false);
        sb_ = WriteFloat(sb_, position.z, false);

        return sb_;
    }
    private StringBuilder WriteQuat(StringBuilder sb_, Quaternion quat, bool first)
    {
        sb_ = WriteFloat(sb_, quat.x, first);
        sb_ = WriteFloat(sb_, quat.y, false);
        sb_ = WriteFloat(sb_, quat.z, false);
        sb_ = WriteFloat(sb_, quat.w, false);

        return sb_;
    }
    public bool WriteMatData(string DirectoryPath, string filename, Matrix4x4 root_mat)
    {

        if (Directory.Exists(DirectoryPath))
        {
            Debug.Log("wrtie matrix on " + filename);

            File_record = CreateFile(DirectoryPath, filename);
            sb_record = new StringBuilder();

            sb_record = WritePosition(sb_record, root_mat.GetPosition(), true);
            sb_record = WriteQuat(sb_record, root_mat.GetRotation(), false);

            File_record.WriteLine(sb_record.ToString());

            File_record.Close();
            sb_record.Clear();

            return true;
        }
        else
        {
            return false;
        }
    }
    public void Refresh()
    {
        Files = new BVHFile[0];
        Filter = string.Empty;
        Instances = new BVHFile[0];
        Motion = new Matrix4x4[0][];
        Goals = new float[0][];
        RootMat = new float[0][];
    }
  

    public class BVHFile
    {
        public FileInfo Object;
        public bool Import;
    }

}