using UnityEngine;
using UnityEditor;
using System.IO;

using System.Collections.Generic;

[System.Serializable]
public class CameraImageMap
{
    public List<Camera> cameras = new List<Camera>();
    private List<Vector3> cameraPositions = new List<Vector3>();
    private List<Vector3> cameraDirections = new List<Vector3>();
    private int verticalCount;
    private int horizontalCount;
    private float radius;

    public string Image_data_path; //= "None";

    public CameraImageMap()
    {
        UpdateRadius(this.radius);
    }

    public void SetCamera(Vector3 center,
    int verticalCount, int horizontalCount, float radius,
    string image_data_path)
    {
        this.verticalCount = verticalCount;
        this.horizontalCount = horizontalCount;
        this.radius = radius;
        this.Image_data_path = image_data_path;
        cameras.Clear();
        cameraPositions.Clear();
        cameraDirections.Clear();

        for (int i = 0; i < verticalCount; i++)
        {
            float theta = Mathf.Lerp(5f, 85f, (float)i / (verticalCount - 1)) * Mathf.Deg2Rad;
            for (int j = 0; j < horizontalCount; j++)
            {
                float phi = 360f * j / horizontalCount * Mathf.Deg2Rad;

                float x = radius * Mathf.Sin(theta) * Mathf.Cos(phi);
                float y = radius * Mathf.Cos(theta);
                float z = radius * Mathf.Sin(theta) * Mathf.Sin(phi);

                Vector3 camPos = center + new Vector3(x, y, z);
                Vector3 camDir = (center - camPos).normalized;

                GameObject camObj = new GameObject($"Camera_{i}_{j}");
                camObj.transform.position = camPos;
                camObj.transform.LookAt(center);
                Camera cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.fieldOfView = 60f;

                if (!Application.isPlaying)
                    Undo.RegisterCreatedObjectUndo(camObj, "Create Dome Camera");

                cameras.Add(cam);
                cameraPositions.Add(camPos);
                cameraDirections.Add(camDir);
            }
        }

        Debug.Log("init " + cameraPositions.Count);
    }

    public void UpdateRadius(float newRadius)
    {
        // 새로운 radius 값을 설정
        radius = newRadius;

        // radius 값에 따라 카메라 위치와 방향을 업데이트
        int index = 0;
        for (int i = 0; i < verticalCount; i++)
        {
            float theta = Mathf.Lerp(5f, 85f, (float)i / (verticalCount - 1)) * Mathf.Deg2Rad;
            for (int j = 0; j < horizontalCount; j++)
            {
                float phi = 360f * j / horizontalCount * Mathf.Deg2Rad;

                float x = radius * Mathf.Sin(theta) * Mathf.Cos(phi);
                float y = radius * Mathf.Cos(theta);
                float z = radius * Mathf.Sin(theta) * Mathf.Sin(phi);

                Vector3 camPos = cameraPositions[index];
                Vector3 camDir = (cameraDirections[index]);

                // 새로운 위치 계산
                camPos = new Vector3(x, y, z) + cameraPositions[index];
                cameraPositions[index] = camPos;

                // 카메라 위치 갱신
                if (index < cameras.Count)
                {
                    cameras[index].transform.position = camPos;
                    cameras[index].transform.LookAt(cameraPositions[index]);
                }

                cameraDirections[index] = camDir;
                index++;
            }
        }
    }

    public void UpdateCamera(Matrix4x4 centerMatrix)
    {
        Vector3 center = centerMatrix.GetColumn(3);
        int index = 0;

        for (int i = 0; i < this.verticalCount; i++)
        {
            float theta = Mathf.Lerp(5f, 85f, (float)i / (this.verticalCount - 1)) * Mathf.Deg2Rad;
            for (int j = 0; j < horizontalCount; j++)
            {
                float phi = 360f * j / horizontalCount * Mathf.Deg2Rad;

                float x = radius * Mathf.Sin(theta) * Mathf.Cos(phi);
                float y = radius * Mathf.Cos(theta);
                float z = radius * Mathf.Sin(theta) * Mathf.Sin(phi);

                Vector3 offset = new Vector3(x, y, z);
                Vector3 camPos = center + offset;
                Vector3 camDir = (center - camPos).normalized;

                if (index < cameras.Count)
                {
                    cameras[index].transform.position = camPos;
                    cameras[index].transform.LookAt(center);
                }

                cameraPositions[index] = camPos;
                cameraDirections[index] = camDir;
                index++;
            }
        }
        //Debug.Log("update " + cameraPositions.Count);
    }

    public void SaveImage(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        int width = 512;
        int height = 512;

        for (int i = 0; i < cameras.Count; i++)
        {
            Camera cam = cameras[i];

            // RenderTexture 설정
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;

            // 렌더링 수행
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = rt;
            cam.Render();

            // Texture2D로 읽기
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();

            // 저장 경로 지정 및 저장
            byte[] bytes = image.EncodeToPNG();
            string filename = Path.Combine(directoryPath, $"camera_{i:D3}.png");
            File.WriteAllBytes(filename, bytes);

            // 정리
            cam.targetTexture = null;
            RenderTexture.active = currentRT;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(image);
        }

        Debug.Log($"Saved {cameras.Count} images to {directoryPath}");
    }


public void Draw()
{
    UltiDraw.Begin();
    Color[] colors = UltiDraw.GetRainbowColors(cameraPositions.Count);
    //Debug.Log(cameraPositions.Count);
    for (int i = 0; i < cameraPositions.Count; i++)
    {
        Vector3 pos = cameraPositions[i];
        Vector3 dir = cameraDirections[i];
        UltiDraw.DrawSphere(pos, Quaternion.LookRotation(dir), 0.5f, colors[i]);
    }
    UltiDraw.End();
}

    public int GetCameraCount() => cameras.Count;
    public Camera GetCamera(int index) => cameras[index];

    public void inspector()
    {
		UltiDraw.Begin();
		Utility.SetGUIColor(UltiDraw.Grey);

		EditorGUILayout.BeginHorizontal(); // 나중에 FBX, Motion TEXT File 도 쓸 수 있도록 inspector 를 만들던지 각 파일의 inspector 에 추가하도록 한다.
		
		EditorGUILayout.LabelField("Radius", GUILayout.Width(70f));
		radius = EditorGUILayout.FloatField(radius, GUILayout.Width(50f));

		EditorGUILayout.EndHorizontal();

        if (Utility.GUIButton(" Setup radius ", Color.white, Color.green))
        {

            UpdateRadius(radius);
            Debug.Log("Radius " + radius);
		}

		UltiDraw.End();
	}
}
