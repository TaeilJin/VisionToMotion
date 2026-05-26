using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class BonePairingManager : MonoBehaviour
{
    [Header("Drag and Drop Before Playing")]
    public GameObject Source;
    public GameObject Target;  

    // UI
    [Header("UI")]
    public Text Error_Txt;
    public Text S_Name_Txt;
    public GameObject S_Content;
    public Text T_Name_Txt;
    public GameObject T_Content;
    public GameObject P_Content;
    public GameObject NodeObj;
    public Button Build_Btn;
    public Button Pair_Btn;
    public Button Unpair_Btn;
    public Button Upload_Btn;
    public Button Save_Btn;

    private Actor S_Actor = null;
    private Actor T_Actor = null;

    public static BoneNodeController CurSCtrl = null;
    public static BoneNodeController CurTCtrl = null;
    public static HashSet<string> SelPName = new HashSet<string>();
    private Dictionary<string, BoneNodeController> PairedNodeList = new Dictionary<string, BoneNodeController>();
    //---------- Sorting Button and Controlling interactability ----------
    //public static int curSBtnIdx = 0;
    //private SortedList<int, Button> SrcButtonList = new SortedList<int, Button>();
    //---------- Sorting Button and Controlling interactability ----------

    private int columnNum = 21;

    // Start is called before the first frame update
    //void Start()
    //{

    //}

    // Update is called once per frame
    void Update()
    {
        if (NullErrorCheck()) return;
    }

    IEnumerator ErrorMsg(string msg, float sec)
    {
        Error_Txt.text = msg;
        yield return new WaitForSeconds(sec);
        Error_Txt.text = "";
    }

    // Button ON OFF & Error MSG
    private bool NullErrorCheck()
    {
        bool check = false;
        if (Source == null || Target == null)
        {
            string msg = "Reference the source and target correctly. Seems to be empty some part.";
            StartCoroutine(ErrorMsg(msg, 5f));
            Build_Btn.interactable = false;
            Pair_Btn.interactable = false;
            Unpair_Btn.interactable = false;
            Upload_Btn.interactable = false;
            Save_Btn.interactable = false;
            check = true;
        }
        else
        {
            S_Name_Txt.text = Source.name;
            T_Name_Txt.text = Target.name;
            
            Build_Btn.interactable = true;
            Upload_Btn.interactable = true;

            if (CurSCtrl != null && CurTCtrl != null) Pair_Btn.interactable = true;
            else Pair_Btn.interactable = false;

            if (SelPName.Count > 0) Unpair_Btn.interactable = true;
            else Unpair_Btn.interactable = false;

            if (PairedNodeList.Count > 0) Save_Btn.interactable = true;
            else Save_Btn.interactable = false;
        }
        return check;
    }

    public SortedList<int, BoneNodeController> SortButtons(GameObject parent)
    {
        SortedList<int, BoneNodeController> buttonCtrls = new SortedList<int, BoneNodeController>();
        BoneNodeController[] tempList = parent.GetComponentsInChildren<BoneNodeController>();
        foreach (BoneNodeController ctrl in tempList)
        {
            ctrl.transform.SetParent(null);
            buttonCtrls.Add(ctrl.Index, ctrl);
        }
        foreach (BoneNodeController ctrl in buttonCtrls.Values)
        {
            ctrl.transform.SetParent(parent.transform);
        }

        return buttonCtrls;
    }

    public void ControlButtonInteractablity(SortedList<int, BoneNodeController> buttonCtrls)
    {
        for (int i = 0; i < buttonCtrls.Count; i++)
        {
            if (buttonCtrls.Values[i].Button == null) buttonCtrls.Values[i].Button = buttonCtrls.Values[i].GetComponent<Button>();
            if (i == 0)
            {
                Debug.Log(buttonCtrls.Values[i].BoneName);
                buttonCtrls.Values[i].Button.interactable = true;
            }
            else buttonCtrls.Values[i].Button.interactable = false;
        }
    }

    // --------------------UI Trigger Function--------------------
    public void BuildBoneListOnClick()
    {
        if (S_Actor == null)
        {
            S_Actor = Source.GetComponent<Actor>();
        }

        if (S_Actor != null)
        {
            for (int i = 0; i < S_Actor.Bones.Length; i++)
            {
                GameObject tempObj = Instantiate(NodeObj);
                tempObj.transform.SetParent(S_Content.transform, false);
                BoneNodeController tempNodeCtrl = tempObj.GetComponent<BoneNodeController>();
                tempNodeCtrl.BoneType = BoneType.SOURCE;
                tempNodeCtrl.Index = S_Actor.Bones[i].Index;
                tempNodeCtrl.BoneName = S_Actor.Bones[i].GetName();
                tempNodeCtrl.GetComponentInChildren<Text>().text = tempNodeCtrl.BoneName;
            }
        }

        if (T_Actor == null)
        {
            T_Actor = Target.GetComponent<Actor>();
        }

        if (T_Actor != null)
        {
            for (int i = 0; i < T_Actor.Bones.Length; i++)
            {
                GameObject tempObj = Instantiate(NodeObj);
                tempObj.transform.SetParent(T_Content.transform, false);
                BoneNodeController tempNodeCtrl = tempObj.GetComponent<BoneNodeController>();
                tempNodeCtrl.BoneType = BoneType.TARGET;
                tempNodeCtrl.Index = T_Actor.Bones[i].Index;
                tempNodeCtrl.BoneName = T_Actor.Bones[i].GetName();
                tempNodeCtrl.GetComponentInChildren<Text>().text = tempNodeCtrl.BoneName;
            }
        }

        //if (S_Content != null) ControlButtonInteractablity(SortButtons(S_Content));
    }

    public void PairingOnClick()
    {
        // Pairing : 2������ BoneNodeController�� 1���� ��ħ
        GameObject tempObj = Instantiate(NodeObj);
        tempObj.transform.SetParent(P_Content.transform, false);
        BoneNodeController tempNodeCtrl = tempObj.GetComponent<BoneNodeController>();
        tempNodeCtrl.BoneType = BoneType.PAIRED;
        tempNodeCtrl.Index = CurSCtrl.Index;
        tempNodeCtrl.BoneName = CurSCtrl.BoneName;
        tempNodeCtrl.Index_T = CurTCtrl.Index;
        tempNodeCtrl.BoneName_T = CurTCtrl.BoneName;
        tempNodeCtrl.GetComponentInChildren<Text>().text = tempNodeCtrl.BoneName + " + " + tempNodeCtrl.BoneName_T;
        PairedNodeList.Add(CurSCtrl.BoneName, tempNodeCtrl);
        Destroy(CurSCtrl.gameObject);
        Destroy(CurTCtrl.gameObject);
        CurSCtrl = null;
        CurTCtrl = null;

        //if (P_Content != null) SortButtons(P_Content);
        //if (S_Content != null) ControlButtonInteractablity(SortButtons(S_Content));
        SortButtons(P_Content);
    }

    public void UnpairingOnClick()
    {
        // Unpairing: pairing�� �ݴ��, 1������ BoneNodeController�� 2���� �ɰ�
        foreach (string name in SelPName)
        {
            //Debug.Log(name);
            if (!PairedNodeList.ContainsKey(name)) continue;

            // Source
            GameObject tempObj = Instantiate(NodeObj);
            tempObj.transform.SetParent(S_Content.transform, false);
            BoneNodeController tempNodeCtrl = tempObj.GetComponent<BoneNodeController>();
            tempNodeCtrl.BoneType = BoneType.SOURCE;
            tempNodeCtrl.Index = PairedNodeList[name].Index;
            tempNodeCtrl.BoneName = PairedNodeList[name].BoneName;
            tempNodeCtrl.GetComponentInChildren<Text>().text = tempNodeCtrl.BoneName;

            // Target
            GameObject tempObj_T = Instantiate(NodeObj);
            tempObj_T.transform.SetParent(T_Content.transform, false);
            BoneNodeController tempNodeCtrl_T = tempObj_T.GetComponent<BoneNodeController>();
            tempNodeCtrl_T.BoneType = BoneType.TARGET;
            tempNodeCtrl_T.Index = PairedNodeList[name].Index_T;
            tempNodeCtrl_T.BoneName = PairedNodeList[name].BoneName_T;
            tempNodeCtrl_T.GetComponentInChildren<Text>().text = tempNodeCtrl_T.BoneName;

            Destroy(PairedNodeList[name].gameObject);
            PairedNodeList[name] = null;
            PairedNodeList.Remove(name);
        }

        //if (T_Content != null) SortButtons(T_Content);
        //if (S_Content != null) ControlButtonInteractablity(SortButtons(S_Content));
        SortButtons(S_Content);
        SortButtons(T_Content);
    }

    public void SaveFileOnClick()
    {
        string writeFilepath = EditorUtility.SaveFilePanel("Overwrite with txt", "", Source.name + "+" + Target.name + ".txt", "txt");
        Debug.Log("Filepath : " + writeFilepath);
        if (writeFilepath.Length != 0)
        {
            // Write
            BoneNodeController[] pairedCtrls = P_Content.GetComponentsInChildren<BoneNodeController>();
            string[,] output = new string[pairedCtrls.Length, columnNum];
            int i = 0;
            foreach (BoneNodeController ctrl in pairedCtrls)
            {
                int curIdx_S = ctrl.Index;
                int curIdx_T = ctrl.Index_T;

                output[i, 0] = GetIsEndEft(curIdx_S) ? "1" : "0";
                output[i, 1] = ctrl.BoneName;
                output[i, 2] = ctrl.BoneName_T;

                //output[i, 3] = S_Actor.FindBone(ctrl.BoneName).GetParent().GetName();
                //output[i, 4] = T_Actor.FindBone(ctrl.BoneName_T).GetParent().GetName();



                Vector3 srcAxisX = GetNormalizedDir(S_Actor.Bones[curIdx_S].Transform.position, 
                    S_Actor.Bones[curIdx_S].Transform.rotation.GetInverse(), Vector3.right);
                Vector3 srcAxisY = GetNormalizedDir(S_Actor.Bones[curIdx_S].Transform.position,
                    S_Actor.Bones[curIdx_S].Transform.rotation.GetInverse(), Vector3.up);
                Vector3 srcAxisZ = GetNormalizedDir(S_Actor.Bones[curIdx_S].Transform.position,
                    S_Actor.Bones[curIdx_S].Transform.rotation.GetInverse(), Vector3.forward);
                Vector3 tarAxisX = GetNormalizedDir(T_Actor.Bones[curIdx_T].Transform.position,
                    T_Actor.Bones[curIdx_T].Transform.rotation.GetInverse(), Vector3.right);
                Vector3 tarAxisY = GetNormalizedDir(T_Actor.Bones[curIdx_T].Transform.position,
                    T_Actor.Bones[curIdx_T].Transform.rotation.GetInverse(), Vector3.up);
                Vector3 tarAxisZ = GetNormalizedDir(T_Actor.Bones[curIdx_T].Transform.position,
                    T_Actor.Bones[curIdx_T].Transform.rotation.GetInverse(), Vector3.forward);

                Debug.Log(" src axis x " + srcAxisX + " tar axis x " + tarAxisX + " tar axis x2 " + GetNormalizedDir(T_Actor.Bones[curIdx_T].Transform.position, T_Actor.Bones[curIdx_T].Transform.rotation.GetInverse(), srcAxisX));
                Debug.Log(" src axis y " + srcAxisY + " tar axis y " + tarAxisY + " tar axis y2 " + GetNormalizedDir(T_Actor.Bones[curIdx_T].Transform.position, T_Actor.Bones[curIdx_T].Transform.rotation.GetInverse(), srcAxisY));
                Debug.Log(" src axis z " + srcAxisZ + " tar axis z " + tarAxisZ + " tar axis z2 " + GetNormalizedDir(T_Actor.Bones[curIdx_T].Transform.position, T_Actor.Bones[curIdx_T].Transform.rotation.GetInverse(), srcAxisZ));

                int start = 0;
                output[i, start+3] = srcAxisX.x.ToString();
                output[i, start+4] = srcAxisX.y.ToString();
                output[i, start+5] = srcAxisX.z.ToString();
                output[i, start+6] = tarAxisX.x.ToString();
                output[i, start+7] = tarAxisX.y.ToString();
                output[i, start+8] = tarAxisX.z.ToString();
                         
                output[i, start+9] = srcAxisY.x.ToString();
                output[i, start+10] = srcAxisY.y.ToString();
                output[i, start+11] = srcAxisY.z.ToString();
                output[i, start+12] = tarAxisY.x.ToString();
                output[i, start+13] = tarAxisY.y.ToString();
                output[i, start+14] = tarAxisY.z.ToString();
                          
                output[i, start+15] = srcAxisZ.x.ToString();
                output[i, start+16] = srcAxisZ.y.ToString();
                output[i, start+17] = srcAxisZ.z.ToString();
                output[i, start+18] = tarAxisZ.x.ToString();
                output[i, start+19] = tarAxisZ.y.ToString();
                output[i, start+20] = tarAxisZ.z.ToString();

                i++;
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
            StartCoroutine(ErrorMsg("Saving completed.", 3f));
        }
    }

    public void UploadFileOnClick()
    {
        string readFilepath = EditorUtility.OpenFilePanel("Overwrite with txt", "", "txt");
        if (!File.Exists(readFilepath))
        {
            string msg = "File Path(" + readFilepath + ") Not Exists.";
            StartCoroutine(ErrorMsg(msg, 5f));
            return;
        }

        string value = "";
        StreamReader reader = new StreamReader(readFilepath);
        value = reader.ReadToEnd();
        reader.Close();

        Debug.Log(value);
        StartCoroutine(ErrorMsg("Loading completed.", 3f));
        // Parsing how?
    }
    // --------------------UI Trigger Function--------------------

    public bool GetIsEndEft(int boneIdx)
    {
        bool isEndEft = false;
        int nextIdx = boneIdx + 1;
        if (S_Actor.Bones.Length > nextIdx)
        {
            if (S_Actor.Bones[boneIdx] != S_Actor.Bones[nextIdx].GetParent()) isEndEft = true;
        }
        else isEndEft = true;
        return isEndEft;
    }

    public Vector3 GetNormalizedDir(Vector3 pos, Quaternion rot, Vector3 basis)
    {
        Vector3 cacVec = rot * basis;
        Vector3 dirVec = cacVec.normalized;
        return dirVec;
    }

    public List<int> GetSelectedBoneList(Actor actor)
    {
        List<int> selBoneList = new List<int>();

        if (S_Actor != null && T_Actor != null)
        {
            if (actor == S_Actor)
            {
                if (CurSCtrl != null) selBoneList.Add(CurSCtrl.Index);
                if (SelPName.Count > 0)
                {
                    foreach (string name in SelPName)
                    {
                        if (!PairedNodeList.ContainsKey(name)) continue;
                        selBoneList.Add(PairedNodeList[name].Index);
                    }
                }
            }
            else if (actor == T_Actor)
            {
                if (CurTCtrl != null) selBoneList.Add(CurTCtrl.Index);
                if (SelPName.Count > 0)
                {
                    foreach (string name in SelPName)
                    {
                        if (!PairedNodeList.ContainsKey(name)) continue;
                        selBoneList.Add(PairedNodeList[name].Index_T);
                    }
                }
            }
        }

        return selBoneList;
    }

}
