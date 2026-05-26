using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class PositionOffsetManager : MonoBehaviour
{
    public Transform offsetMatrix;
    public float offset = 0.1f;

    private float pressTime = 0f;
    private float intervalTime = 0.1f;

    private UnityEvent rightDirection = new UnityEvent();
    private UnityEvent leftDirection = new UnityEvent();
    private UnityEvent upDirection = new UnityEvent();
    private UnityEvent downDirection = new UnityEvent();

    // Start is called before the first frame update
    void Start()
    {
        rightDirection.AddListener(RightDirection);
        leftDirection.AddListener(LeftDirection);
        upDirection.AddListener(UpDirection);
        downDirection.AddListener(DownDirection);
    }

    // Update is called once per frame
    void Update()
    {
        if (offsetMatrix == null) return;

        if (Time.time - this.pressTime > intervalTime)
        {
            CheckKeyboardEvent();
        }
    }

    private void CheckKeyboardEvent()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.A))
        {
            this.rightDirection.Invoke();
            this.pressTime = Time.time;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.D))
        {
            this.leftDirection.Invoke();
            this.pressTime = Time.time;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            this.upDirection.Invoke();
            this.pressTime = Time.time;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            this.downDirection.Invoke();
            this.pressTime = Time.time;
        }
    }

    private void RightDirection()
    {      
        float newX = offsetMatrix.position.x - offset;
        offsetMatrix.position = new Vector3(newX, offsetMatrix.position.y, offsetMatrix.position.z);
    }

    private void LeftDirection()
    {
        float newX = offsetMatrix.position.x + offset;
        offsetMatrix.position = new Vector3(newX, offsetMatrix.position.y, offsetMatrix.position.z);
    }

    private void UpDirection()
    {
        float newY = offsetMatrix.position.y + offset;
        offsetMatrix.position = new Vector3(offsetMatrix.position.x, newY, offsetMatrix.position.z);
    }

    private void DownDirection()
    {
        float newY = offsetMatrix.position.y - offset;
        offsetMatrix.position = new Vector3(offsetMatrix.position.x, newY, offsetMatrix.position.z);
    }
}


[CustomEditor(typeof(PositionOffsetManager), true)]
public class PositionOffsetManager_Editor : Editor
{
    public PositionOffsetManager Target;


    public void Awake()
    {
        Target = (PositionOffsetManager)target;
    }

    public override void OnInspectorGUI()
    {
        inspector();
    }

    private void inspector()
    {
        Utility.ResetGUIColor();
        Utility.SetGUIColor(UltiDraw.LightGrey);

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Target actor");
        Target.offsetMatrix = (Transform)EditorGUILayout.ObjectField(Target.offsetMatrix, typeof(Transform), true);
        GUILayout.EndHorizontal();

        if (Target.offsetMatrix)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Actor pivot position");           
            EditorGUILayout.LabelField("X", GUILayout.MaxWidth(10));
            float newX = EditorGUILayout.FloatField(Target.offsetMatrix.position.x);
            EditorGUILayout.LabelField("Y", GUILayout.MaxWidth(10));
            float newY = EditorGUILayout.FloatField(Target.offsetMatrix.position.y);
            Target.offsetMatrix.position = new Vector3(newX, newY, Target.offsetMatrix.position.z);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Offset value");
            Target.offset = EditorGUILayout.FloatField(Target.offset);
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Set offset values and change target pivot position by keyboard(arrow keys, <W><A><S><D>) on play mode.", EditorStyles.helpBox);
        }
        
    }
}
