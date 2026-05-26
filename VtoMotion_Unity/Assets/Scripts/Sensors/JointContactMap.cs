
using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using UnityEditor;
using UnityEditorInternal;
[System.Serializable]
public class JointContactMap
{

    public class JointContactSensor
    {
		public Matrix4x4 Cur_Pose;
		public int nFrames;
        public int Bone;
		public string Name;
        public float RegularContacts;
        public Vector3 RegularContactPoints;
        public Vector3 RegularDistances;
		public LayerMask Mask = 1 << LayerMask.NameToLayer("Penetration");
		public ID CaptureID = ID.Closest;
		public float Threshold_collision;
		public float Threshold_velocity;
		public Vector3 Offset = Vector3.zero;
		public float FramePerSeconds;
		public string ContactFurniture;
		public bool Base_int;
		public bool Spine_int;
		public bool Hand_int;
		public bool Foot_int;
		public bool Enable_int;

		public enum ID
		{
			None,
			Closest,
			RayTopDown, RayCenterDown, RayBottomUp, RayCenterUp,
			SphereTopDown, SphereCenterDown, SphereBottomUp, SphereCenterUp,
			RayXPositive, RayXNegative, RayYPositive, RayYNegative, RayZPositive, RayZNegative,
			Identity
		};

		public JointContactSensor(int bone, string name, float threshold, float FPS, LayerMask mask)
        {
			Bone = bone;
			Name = name;
			Threshold_collision = threshold;
			Threshold_velocity = 0.03f;
			Mask = mask;
			FramePerSeconds = FPS;
        }

		public Vector3 DetectCollision(Matrix4x4 tr, JointContactSensor.ID mode, Vector3 pivot, float radius, out Collider collider)
		{
			if (mode == ID.Closest)
			{
				//Debug.Log("closest contact " + Mask);
				return Utility.GetClosestPointOverlapSphere(pivot, radius, Mask, out collider);
			}

			if (mode == ID.RayTopDown)
			{
				RaycastHit info;
				bool hit = Physics.Raycast(pivot + new Vector3(0f, radius, 0f), Vector3.down, out info, 2f * radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.RayCenterDown)
			{
				RaycastHit info;
				bool hit = Physics.Raycast(pivot, Vector3.down, out info, radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.RayBottomUp)
			{
				RaycastHit info;
				bool hit = Physics.Raycast(pivot - new Vector3(0f, radius, 0f), Vector3.up, out info, 2f * radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.RayCenterUp)
			{
				RaycastHit info;
				bool hit = Physics.Raycast(pivot, Vector3.up, out info, radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.SphereTopDown)
			{
				RaycastHit info;
				bool hit = Physics.SphereCast(pivot + new Vector3(0f, radius + Threshold_collision, 0f), Threshold_collision, Vector3.down, out info, 2f * radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.SphereCenterDown)
			{
				RaycastHit info;
				bool hit = Physics.SphereCast(pivot + new Vector3(0f, radius, 0f), Threshold_collision, Vector3.down, out info, radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.SphereBottomUp)
			{
				RaycastHit info;
				bool hit = Physics.SphereCast(pivot - new Vector3(0f, radius + Threshold_collision, 0f), Threshold_collision, Vector3.up, out info, 2f * radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.SphereCenterUp)
			{
				RaycastHit info;
				bool hit = Physics.SphereCast(pivot - new Vector3(0f, radius, 0f), Threshold_collision, Vector3.up, out info, radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.RayXPositive)
			{
				Vector3 dir = tr.GetRight();
				RaycastHit info;
				bool hit = Physics.Raycast(pivot - radius * dir, dir, out info, 2f * radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.RayXNegative)
			{
				Vector3 dir = -tr.GetRight();
				RaycastHit info;
				bool hit = Physics.Raycast(pivot - radius * dir, dir, out info, 2f * radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.RayYPositive)
			{
				Vector3 dir = tr.GetUp();
				RaycastHit info;
				bool hit = Physics.Raycast(pivot - radius * dir, dir, out info, 2f * radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.RayYNegative)
			{
				Vector3 dir = -tr.GetUp();
				RaycastHit info;
				bool hit = Physics.Raycast(pivot - radius * dir, dir, out info, 2f * radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.RayZPositive)
			{
				Vector3 dir = tr.GetForward();
				RaycastHit info;
				bool hit = Physics.Raycast(pivot - radius * dir, dir, out info, 2f * radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.RayZNegative)
			{
				Vector3 dir = -tr.GetForward();
				RaycastHit info;
				bool hit = Physics.Raycast(pivot - radius * dir, dir, out info, 2f * radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			collider = null;
			return pivot;
		}

		public Vector3 GetBoneVelocity(float delta, Matrix4x4 trs_previous, Matrix4x4 trs)
		{
			if (delta == 0f)
			{
				return Vector3.zero;
			}
			return (trs.GetPosition() - trs_previous.GetPosition()) / delta;
		}
		public void CaptureJointContact(Matrix4x4 tr_previous, Matrix4x4 tr)
        {
			Cur_Pose = tr;

			Vector3 vel = GetBoneVelocity(FramePerSeconds, tr_previous, tr);

			Collider collider;
			Vector3 collision = DetectCollision(tr, CaptureID, tr.GetPosition(), Threshold_collision, out collider);
			if (collider != null)
			{
				Vector3 distance = collision - tr.GetPosition();
				
				RegularContacts = 1f;
				RegularDistances = distance;
				RegularContactPoints = collision;
				ContactFurniture = collider.name;
				GameObject obj = collider.gameObject.gameObject;
				Affordance_Label aff_label = obj.GetComponent<Affordance_Label>();

				Base_int = aff_label.base_interactable;
				Spine_int = aff_label.spine_interactable;
				Hand_int = aff_label.hand_interactable;
				Foot_int = aff_label.foot_interactable;
				Enable_int = aff_label.Do_not_interact;
			}
            else
            {
				RegularContacts = 0f;
				RegularDistances = Vector3.zero;
				RegularContactPoints = Vector3.zero;
				ContactFurniture = null;
				Base_int = false;
				Spine_int = false;
				Hand_int = false;
				Foot_int = false;
				Enable_int = false;
			}
            //Debug.Log($"You contact with {ContactFurniture}");
        }

		public void Inspector()
		{
			UltiDraw.Begin();
			Utility.SetGUIColor(UltiDraw.Grey);

			EditorGUILayout.BeginHorizontal(); // 나중에 FBX, Motion TEXT File 도 쓸 수 있도록 inspector 를 만들던지 각 파일의 inspector 에 추가하도록 한다.
			EditorGUILayout.LabelField("Bone", GUILayout.Width(40f));
			Bone = EditorGUILayout.IntField("", Bone, GUILayout.Width(40f));
			
			EditorGUILayout.LabelField("Mask", GUILayout.Width(50));
			Mask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(Mask), InternalEditorUtility.layers, GUILayout.Width(90f)));
			EditorGUILayout.LabelField("Capture", GUILayout.Width(60f));
			CaptureID = (ID)EditorGUILayout.EnumPopup(CaptureID, GUILayout.Width(75f));
			
			EditorGUILayout.LabelField("Offset", GUILayout.Width(40f));
			Offset = EditorGUILayout.Vector3Field("", Offset, GUILayout.Width(180f));
			EditorGUILayout.EndHorizontal(); // 나중에 FBX, Motion TEXT File 도 쓸 수 있도록 inspector 를 만들던지 각 파일의 inspector 에 추가하도록 한다.


			EditorGUILayout.BeginHorizontal(); // 나중에 FBX, Motion TEXT File 도 쓸 수 있도록 inspector 를 만들던지 각 파일의 inspector 에 추가하도록 한다.

			EditorGUILayout.LabelField("Threshold", GUILayout.Width(70f));
			Threshold_collision = EditorGUILayout.FloatField(Threshold_collision, GUILayout.Width(50f));

			EditorGUILayout.LabelField("Threshold_Velocity", GUILayout.Width(70f));
			Threshold_velocity = EditorGUILayout.FloatField(Threshold_velocity, GUILayout.Width(50f));
			EditorGUILayout.EndHorizontal(); // 나중에 FBX, Motion TEXT File 도 쓸 수 있도록 inspector 를 만들던지 각 파일의 inspector 에 추가하도록 한다.

			UltiDraw.End();
		}
	}

	public JointContactSensor[] JointContactSensors = new JointContactSensor[0];
	public JointContactMap()
    {

    }
 //   {
	//	JointContactSensors = new JointContactSensor[nBones];
	//	for (int j=0; j < nBones; j++)
 //       {
	//		JointContactSensors[j] = new JointContactSensor(j, 0.05f, 30, LayerMask.NameToLayer("Penetration"));
 //       }
 //   }

	public void JointSense(Matrix4x4 pivot_pre, Matrix4x4 pivot, int joint_index)
	{
		// 
		JointContactSensors[joint_index].CaptureJointContact(pivot_pre, pivot);

	}
	public void Draw()
    {
        UltiDraw.Begin();
		Color[] colors = UltiDraw.GetRainbowColors(JointContactSensors.Length);
		for (int i = 0; i < JointContactSensors.Length; i++)
		{
			UltiDraw.DrawCube(JointContactSensors[i].Cur_Pose.GetPosition(), JointContactSensors[i].Cur_Pose.GetRotation(),
				0.025f, UltiDraw.Black);
			UltiDraw.DrawWireSphere(JointContactSensors[i].Cur_Pose.GetPosition(), JointContactSensors[i].Cur_Pose.GetRotation(),
				2f * JointContactSensors[i].Threshold_collision, colors[i].Transparent(0.25f));

			if (JointContactSensors[i].RegularContacts == 1f)
            {
				//Debug.Log("contact " + JointContactSensors[i].Bone);
				UltiDraw.DrawSphere(JointContactSensors[i].Cur_Pose.GetPosition(), JointContactSensors[i].Cur_Pose.GetRotation(),
					2f * JointContactSensors[i].Threshold_collision, colors[i]);

			}
			else
			{
				//Debug.Log("non contact " + JointContactSensors[i].Bone);
				UltiDraw.DrawSphere(JointContactSensors[i].Cur_Pose.GetPosition(), JointContactSensors[i].Cur_Pose.GetRotation(),
					2f * JointContactSensors[i].Threshold_collision, colors[i].Transparent(0.05f));
			}
		}
		UltiDraw.End();
    }

	
}
