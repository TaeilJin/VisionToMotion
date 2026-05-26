using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BoneType
{
    SOURCE,
    TARGET,
    PAIRED,
}

public class BoneNodeController : MonoBehaviour
{
    public BoneType BoneType = BoneType.SOURCE;
    public int Index = -1;
    public string BoneName ="";

    // Only Used BoneType PAIRED 
    public int Index_T = -1;
    public string BoneName_T = "";

    private Image Image;
    public Button Button;
    private bool IsPressed = false;
    private Color NormalColor= new Color(1f, 1f, 1f);
    private Color PressedColor = new Color(1f, 0.65f, 0.78f);

    // Start is called before the first frame update
    void Start()
    {
        if (Image == null) Image = this.GetComponent<Image>();
        if (Button == null) Button = this.GetComponent<Button>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsPressed) return;

        if ((BoneType == BoneType.SOURCE && BonePairingManager.CurSCtrl != this) ||
            (BoneType == BoneType.TARGET && BonePairingManager.CurTCtrl != this))
        {
            IsPressed = false;
            Image.color = NormalColor;
        }
    }

    // --------------------UI Trigger Function--------------------

    public void BoneNodeOnClick()
    {
        //Debug.Log(BoneType + " : " + Index + " : " + BoneName);

        if (IsPressed)
        {
            IsPressed = false;
            Image.color = NormalColor;

            switch (BoneType)
            {
                case BoneType.SOURCE:
                    BonePairingManager.CurSCtrl = null;
                    break;
                case BoneType.TARGET:
                    BonePairingManager.CurTCtrl = null;
                    break;
                case BoneType.PAIRED:
                    BonePairingManager.SelPName.Remove(BoneName);
                    break;
            }
        }
        else
        {
            IsPressed = true;
            Image.color = PressedColor;

            switch (BoneType)
            {
                case BoneType.SOURCE:
                    BonePairingManager.CurSCtrl = this;
                    break;
                case BoneType.TARGET:
                    BonePairingManager.CurTCtrl = this;
                    break;
                case BoneType.PAIRED:
                    BonePairingManager.SelPName.Add(BoneName);
                    break;
            }
        }
    }
}
