//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//[System.Serializable]
//public class HeightMap {

//	public Matrix4x4 Pivot = Matrix4x4.identity;

//	public Vector3[] Map = new Vector3[0];
//	public Vector3[] Points = new Vector3[0];

//	public float Size = 1f;
//	public int Resolution = 25;
//	public LayerMask Mask = -1;

//	public HeightMap(float size, int resolution, LayerMask mask) {
//		Size = size;
//		Resolution = resolution;
//		Mask = mask;
//		Generate();
//	}

//	private void Generate() {
//		Map = new Vector3[Resolution*Resolution];
//		Points = new Vector3[Resolution*Resolution];
//		for(int x=0; x<Resolution; x++) {
//			for(int y=0; y<Resolution; y++) {
//				Map[y*Resolution + x] = new Vector3(-Size/2f + (float)x/(float)(Resolution-1)*Size, 0f, -Size/2f + (float)y/(float)(Resolution-1)*Size);
//			}
//		}
//	}

//	public void SetSize(float value) {
//		if(Size != value) {
//			Size = value;
//			Generate();
//		}
//	}

//	public void SetResolution(int value) {
//		if(Resolution != value) {
//			Resolution = value;
//			Generate();
//		}
//	}

//	public void Sense(Matrix4x4 pivot, LayerMask mask) {
//		Pivot = pivot;
//		Vector3 position = Pivot.GetPosition();
//		Quaternion rotation = Quaternion.AngleAxis(Pivot.GetRotation().eulerAngles.y, Vector3.up);
//		for(int i=0; i<Map.Length; i++) {
//			Points[i] = Project(position + rotation * Map[i], mask);
//		}
//	}

//	public float[] GetHeights() {
//		float[] heights = new float[Points.Length];
//		for(int i=0; i<heights.Length; i++) {
//			heights[i] = Points[i].y;
//		}
//		return heights;
//	}

//	public float[] GetHeights(float maxHeight) {
//		float[] heights = new float[Points.Length];
//		for(int i=0; i<heights.Length; i++) {
//			heights[i] = Mathf.Clamp(Points[i].y, 0f, maxHeight);
//		}
//		return heights;
//	}

//	private Vector3 Project(Vector3 position, LayerMask mask) {
//		RaycastHit hit;
//		Physics.Raycast(new Vector3(position.x, 100f, position.z), Vector3.down, out hit, float.PositiveInfinity, mask);
//		position = hit.point;
//		return position;
//	}

//	public void Draw(float[] mean=null, float[] std=null) {
//		//return;
//		UltiDraw.Begin();

//		//Quaternion rotation = Pivot.GetRotation() * Quaternion.Euler(90f, 0f, 0f);
//		Color color = UltiDraw.IndianRed.Transparent(0.5f);
//		//float area = (float)Size/(float)(Resolution-1);
//		for(int i=0; i<Points.Length; i++) {
//			UltiDraw.DrawCircle(Points[i], 0.025f, color);
//			//UltiDraw.DrawQuad(Points[i], rotation, area, area, color);
//		}

//		UltiDraw.End();
//	}
	
//	public void Render(Vector2 center, Vector2 size, int width, int height, float maxHeight) {
//		UltiDraw.Begin();
//		UltiDraw.DrawGUIGreyscaleImage(center, size, width, height, GetHeights(maxHeight));
//		UltiDraw.End();
//	}

//}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
public class CircleMap
{

    public Matrix4x4 Pivot = Matrix4x4.identity;

    public Vector3[] Map = new Vector3[0];
	public Vector3[] SamplePoints = new Vector3[0];
	public string[] SamplePoints_Furniture = new string[0];
	public bool[] BaseInteraction = new bool[0];
	public bool[] SpineInteraction = new bool[0];
    public bool[] HandInteraction = new bool[0];
    public bool[] FootInteraction = new bool[0];
	public bool[] EnableInteraction = new bool[0];
    //public Vector3[] Points = new Vector3[0];
    public Vector3[] HorizontalMap = new Vector3[0];
	//public Vector3[] HorizontalPoints = new Vector3[0];

    public float Radius = 1f;
    public int Samples = 7;
    public int Rays = 20;
	public int HorizontalSamples = 20;
	public LayerMask Mask = -1;

	public float[] SampleWeights;
	public bool[] HitorNot;
    public CircleMap(float radius, int samples, int rays, LayerMask mask)
    {
        //Radius = radius;
        //Samples = samples;
        //Rays = rays;
        Mask = mask;
		UpdateSensors();
	}

	public void UpdateSensors()
    {
		Map = new Vector3[Rays * Samples];
		//Points = new Vector3[Rays * Samples];
		HorizontalMap = new Vector3[Rays];
		//HorizontalPoints = new Vector3[Rays * HorizontalSamples];

		SamplePoints = new Vector3[Rays * Samples + Rays * HorizontalSamples];
		SamplePoints_Furniture = new string[Rays * Samples + Rays * HorizontalSamples];
        SampleWeights = new float[Rays * Samples + Rays * HorizontalSamples];

		HitorNot = new bool[Rays * Samples + Rays * HorizontalSamples];
        BaseInteraction = new bool[Rays * Samples + Rays * HorizontalSamples];
        SpineInteraction = new bool[Rays * Samples + Rays * HorizontalSamples];
        HandInteraction = new bool[Rays * Samples + Rays * HorizontalSamples];
        FootInteraction = new bool[Rays * Samples + Rays * HorizontalSamples];
        EnableInteraction = new bool[Rays * Samples + Rays * HorizontalSamples];

        for (int i = 0; i < Rays; i++)
		{
			float angle = 360f * (float)i / (float)Rays;
			HorizontalMap[i] = Quaternion.AngleAxis(angle, Vector3.up) * new Vector3(0f, 0f, Radius);
			for (int j = 0; j < Samples; j++)
			{
				float distance = Radius * (float)(j + 1) / (float)Samples;
				Map[i * Samples + j] = Quaternion.AngleAxis(angle, Vector3.up) * new Vector3(0f, 0f, distance);
			}
		}
	}
    public void Sense(Matrix4x4 pivot)
    {
        Pivot = pivot;
        Vector3 position = Pivot.GetPosition();
        Quaternion rotation = Quaternion.AngleAxis(Pivot.GetRotation().eulerAngles.y, Vector3.up);

        for (int i = 0; i < Map.Length; i++)
        {
            SamplePoints[i] = Project(position + rotation * Map[i], out HitorNot[i], out SamplePoints_Furniture[i], 
				out BaseInteraction[i], out SpineInteraction[i], out HandInteraction[i], out FootInteraction[i], out EnableInteraction[i]);
        }

        for (int j = 0; j < HorizontalMap.Length; j++)
        {
            for (int k = 0; k < HorizontalSamples; k++)
            {
                float height = 2.5f * (k + 1) / HorizontalSamples;
                SamplePoints[Map.Length + j * HorizontalSamples + k] = HorizontalProject(
                    new Vector3(position.x, height, position.z),
                    rotation * HorizontalMap[j],
                    out HitorNot[Map.Length + j * HorizontalSamples + k],
                    out SamplePoints_Furniture[Map.Length + j * HorizontalSamples + k],
                    out BaseInteraction[Map.Length + j * HorizontalSamples + k], out SpineInteraction[Map.Length + j * HorizontalSamples + k], 
					out HandInteraction[Map.Length + j * HorizontalSamples + k], out FootInteraction[Map.Length + j * HorizontalSamples + k], out EnableInteraction[Map.Length + j * HorizontalSamples + k]);
            }
        }
    }


	private Vector3 Project(Vector3 position, out bool _hit, out string hitObjectName, 
		out bool BaseInteraction, out bool SpineInteraction, out bool HandInteraction, out bool FootInteraction, out bool EnableInteraction)
    {
        RaycastHit hit;
        _hit = Physics.Raycast(new Vector3(position.x, 100f, position.z), Vector3.down, out hit, float.PositiveInfinity, Mask);

        if (_hit)
        {
            position = hit.point;
            hitObjectName = hit.collider.gameObject.name;

            GameObject hit_collider = hit.collider.gameObject;
            Affordance_Label label = hit_collider.GetComponent<Affordance_Label>();
            if (label != null)
            {
                BaseInteraction = label.base_interactable;
                SpineInteraction = label.spine_interactable;
                HandInteraction = label.hand_interactable;
                FootInteraction = label.foot_interactable;
                EnableInteraction = label.Do_not_interact;
            }
            else
            {
                Debug.Log(hitObjectName + ": Affordance label is null");
                BaseInteraction = false;
                SpineInteraction = false;
                HandInteraction = false;
                FootInteraction = false;
                EnableInteraction = false;
            }
        }
        else
        {
            position = Vector3.one * 10.0f;
            hitObjectName = null;
            BaseInteraction = false;
            SpineInteraction = false;
            HandInteraction = false;
            FootInteraction = false;
            EnableInteraction = false;
        }

        return position;
    }

    private Vector3 HorizontalProject(Vector3 position, Vector3 direction, out bool _hit, out string hitObjectName, 
		out bool BaseInteraction, out bool SpineInteraction, out bool HandInteraction, out bool FootInteraction, out bool EnableInteraction)
    {
        RaycastHit hit;
        _hit = Physics.Raycast(position, direction, out hit, float.PositiveInfinity, Mask);

        if (_hit)
        {
            position = hit.point;
            hitObjectName = hit.collider.gameObject.name;

            GameObject hit_collider = hit.collider.gameObject;
            Affordance_Label label = hit_collider.GetComponent<Affordance_Label>();
			if (label != null) 
			{
                BaseInteraction = label.base_interactable;
                SpineInteraction = label.spine_interactable;
                HandInteraction = label.hand_interactable;
                FootInteraction = label.foot_interactable;
                EnableInteraction = label.Do_not_interact;
            }
			else 
			{
				Debug.Log(hitObjectName + ": Affordance label is null");
                BaseInteraction = false;
                SpineInteraction = false;
                HandInteraction = false;
                FootInteraction = false;
                EnableInteraction = false;
            }			
        }
        else
        {
            position = Vector3.one * 10.0f;
            hitObjectName = null;
			BaseInteraction = false;
            SpineInteraction = false;
            HandInteraction = false;
            FootInteraction = false;
            EnableInteraction = false;
        }

        return position;
    }
    public void Draw()
    {
        UltiDraw.Begin();
		//      for (int i = 0; i < Points.Length; i++)
		//      {
		//          //UltiDraw.DrawCircle(Points[i], 0.025f, UltiDraw.Mustard.Transparent(1.0f));
		//	UltiDraw.DrawSphere(Points[i], Quaternion.identity, 0.02f, UltiDraw.Mustard);
		//      }

		//for (int i = 0; i < HorizontalPoints.Length; i++)
		//{
		//	//UltiDraw.DrawCircle(Points[i], 0.025f, UltiDraw.Mustard.Transparent(1.0f));
		//	UltiDraw.DrawSphere(HorizontalPoints[i], Quaternion.identity, 0.02f, UltiDraw.Blue);
		//}

		for (int i = 0; i < SamplePoints.Length; i++)
		{
			//if (SampleWeights[i] > 0.01f)
			{
				//if (i < Map.Length)
				//	UltiDraw.DrawSphere(SamplePoints[i], Quaternion.identity, 0.1f, UltiDraw.Mustard.Transparent(1.0f));
				//else
				if(HitorNot[i])
					UltiDraw.DrawSphere(SamplePoints[i], Quaternion.identity, 0.1f, UltiDraw.Mustard.Transparent(1.0f));
			}
		}

		UltiDraw.End();
    }

	public static void CalcRelativeWeightsWithBounds(Vector3 targetJoint, Vector3[] Samples, float upperbound, float lowerbound,
		out Vector3[] direction, out float[] mag, out float[] weight)
	{
		//float min = 10000.0f, sum = 0.0f, max = 0.0f;
		// get min max distance with descriptors
		int src_sp_size = Samples.Length;

		mag = new float[src_sp_size];
		direction = new Vector3[src_sp_size];
		weight = new float[src_sp_size];

		for (int j = 0; j < src_sp_size; j++)
		{
			Vector3 dir = targetJoint - Samples[j];
			float mg = dir.magnitude; // dir

			direction[j] = dir.normalized;
			mag[j] = mg;

			//if (mg < min)
			//	min = mg;
			//if (mg > max)
			//	max = mg;
			//sum += mg;
		}

		//Debug.Log("max " + max + " min  " + min + " sum " + sum);

		//make fade out function 
		for (int k = 0; k < src_sp_size; k++)
		{

			if (mag[k] > upperbound)
				weight[k] = 0.0f;
			else if (mag[k] < lowerbound)
				weight[k] = 1.0f;
			else
            {
				weight[k] = (lowerbound + upperbound) / (mag[k] + upperbound);
            }

		}
		
	}

	public static void CalcRelativeWeights(Vector3 targetJoint, Vector3[] Samples, out Vector3[] direction, out float[] mag, out float[] weight)
    {
		float min = 10000.0f, sum = 0.0f, max = 0.0f;
		// get min max distance with descriptors
		int src_sp_size = Samples.Length;
		
		mag = new float[src_sp_size];
		direction = new Vector3[src_sp_size];
		weight = new float[src_sp_size];

		for (int j = 0; j < src_sp_size; j++)
		{
			Vector3 dir = targetJoint - Samples[j];
			float mg = dir.magnitude; // dir
			
			direction[j] = dir.normalized;
			mag[j] = mg;

			if (mg < min)
				min = mg;
			if (mg > max)
				max = mg;
			sum += mg;
		}

		//Debug.Log("max " + max + " min  " + min + " sum " + sum);

		//make fade out function
		float mid = sum / src_sp_size; float weightsum = 0.0f;
		for (int k = 0; k < src_sp_size; k++)
		{

			if (mag[k] >= mid)
			{
				weight[k] = 0.0f;
			}
			else if (min < mag[k] && mag[k] < mid)
			{
				float w = 1 - (mag[k] - min) / (mid - min);
				weight[k] = w;
			}
			weightsum += weight[k];
		}

		//make weight
		for (int j = 0; j < src_sp_size; j++)
		{
			if (weightsum > 1e-3)
			{
				weight[j] = (weight[j] / weightsum); // * (direction[j] * mag[j] + tarSamples[j])
			}
            else
            {
				weight[j] = 0.0f;
            }
		}
	}
	public static void CalcNewPos(Vector3 SrcPoint, Vector3[] SrcSamples, Vector3[] TarSamples, out Vector3 TarPoint)
    {
		Vector3[] direction;
		float[] mag;
		float[] weight;
		CalcRelativeWeights(SrcPoint, SrcSamples, out direction, out mag, out weight);

		Vector3 NewJntPos = Vector3.zero;
		for (int j = 0; j < SrcSamples.Length; j++)
		{
			Vector3 newPos = (weight[j]) * (direction[j] * mag[j] + TarSamples[j]);
			NewJntPos += newPos;
		}
		TarPoint = NewJntPos;
	}
	public void inspector()
    {
		UltiDraw.Begin();
		Utility.SetGUIColor(UltiDraw.Grey);

		EditorGUILayout.BeginHorizontal(); // 나중에 FBX, Motion TEXT File 도 쓸 수 있도록 inspector 를 만들던지 각 파일의 inspector 에 추가하도록 한다.
		EditorGUILayout.LabelField("Samples", GUILayout.Width(40f));
		Samples = EditorGUILayout.IntField("", Samples, GUILayout.Width(40f));

		EditorGUILayout.LabelField("Rays", GUILayout.Width(40f));
		Rays = EditorGUILayout.IntField("", Rays, GUILayout.Width(40f));

		EditorGUILayout.LabelField("Radius", GUILayout.Width(70f));
		Radius = EditorGUILayout.FloatField(Radius, GUILayout.Width(50f));

		EditorGUILayout.LabelField("HorizontalSamples", GUILayout.Width(70f));
		HorizontalSamples = EditorGUILayout.IntField("", HorizontalSamples, GUILayout.Width(40f));

		EditorGUILayout.LabelField("Mask", GUILayout.Width(50));
		Mask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(Mask), InternalEditorUtility.layers, GUILayout.Width(90f)));

		EditorGUILayout.EndHorizontal();

		if (Utility.GUIButton(" Setup Sensors ", Color.white, Color.green))
		{

			UpdateSensors();
		}



		UltiDraw.End();
	}

}


/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleMap {

	public Matrix4x4 Pivot = Matrix4x4.identity;

	public Vector3[] Map = new Vector3[0];
	public Vector3[] Points = new Vector3[0];

	public float Radius = 1f;
	public int Samples = 10;
	public int Rays = 10;
	public LayerMask Mask = -1;

	public CircleMap(float radius, int samples, int rays, LayerMask mask) {
		Radius = radius;
		Samples = samples;
		Rays = rays;
		Mask = mask;
		List<Vector3> map = new List<Vector3>();
		for(int i=0; i<Rays; i++) {
			float r = Radius*(float)(i+1)/(float)Rays;
			int count = Mathf.RoundToInt(Samples * r / Radius);
			float step = 360f / count;
			//float step = 360f / (r/Radius) / Samples;
			//int count = Mathf.RoundToInt(360f / step);
			for(int j=0; j<count; j++) {
				map.Add(Quaternion.AngleAxis(j*step, Vector3.up) * new Vector3(0f, 0f, r));
			}
		}
		Map = map.ToArray();
		Points = new Vector3[Map.Length];
		//Debug.Log(Map.Length);
	}

	private float GetArc(float r) {
		return 2f*Mathf.PI*r;
	}

	public float[] GetHeights() {
		float[] heights = new float[Points.Length];
		for(int i=0; i<heights.Length; i++) {
			heights[i] = Points[i].y;
		}
		return heights;
	}

	public void Sense(Matrix4x4 pivot) {
		Pivot = pivot;
		Vector3 position = Pivot.GetPosition();
		Quaternion rotation = Quaternion.AngleAxis(Pivot.GetRotation().eulerAngles.y, Vector3.up);
		for(int i=0; i<Map.Length; i++) {
			Points[i] = Project(position + rotation * Map[i]);
		}
	}

	private Vector3 Project(Vector3 position) {
		RaycastHit hit;
		Physics.Raycast(new Vector3(position.x, 100f, position.z), Vector3.down, out hit, float.PositiveInfinity, Mask);
		position = hit.point;
		return position;
	}

	public void Draw() {
		UltiDraw.Begin();
		for(int i=0; i<Points.Length; i++) {
			UltiDraw.DrawCircle(Points[i], 0.025f, UltiDraw.Mustard.Transparent(0.5f));
		}
		UltiDraw.End();
	}

}
*/