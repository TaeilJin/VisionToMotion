using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Text;

[System.Serializable]
public class ExperimentsUtils
{
	
	public ContactJointSensor[] ContactJoints = new ContactJointSensor[0];

	public void CaptureFootContacts()
    {
		for (int j = 0; j < ContactJoints.Length; j++)
		{
			// Capture All Contacts of Sensor in total Frame
			for (int k = 0; k < ContactJoints[j].bone_mat.Length - 1; k++)
			{

				ContactJoints[j].CaptureContact(k);
				//ContactJoints[1].CaptureContact(k);
			}
		}
	}
	public void ExtractFootSliding()
    {
		ContactJoints[0].FootSlidingNum = 0;
		ContactJoints[1].FootSlidingNum = 0;

		// Extract Foot Sliding using All Contacts
		bool first = true;
		int start = 0;
		int end = 0;
		int engage_id = 10;
		int support_id = 10;
		for (int k = 11; k < ContactJoints[0].bone_mat.Length - 1; k++)
		{
			// move frames 을 찾는다.
			if (ContactJoints[0].RegularContacts[k] != ContactJoints[1].RegularContacts[k] && first == true)
			{
				first = false;
				start = k;
				if (ContactJoints[0].RegularContacts[k] == 0f)
				{
					engage_id = 0; support_id = 1;
				}
				else {
					engage_id = 1; support_id = 0;
				}
			}

			if(first == false)
            {
				if (ContactJoints[engage_id].RegularContacts[k] == 1f)
				{
					first = true;
					end = k;

					// extract sliding of support foot
					Debug.Log(" start: " + start + " end " + end + " sp " + (support_id));
					ContactJoints[support_id].ExtractFootSliding(start, end);
				}
            }
		}

		// Extract total number of foot sliding
		for(int p=11; p< ContactJoints[0].bone_mat.Length -1; p++)
        {
			if (ContactJoints[0].RegularFootSliding[p] == 1f)
				ContactJoints[0].FootSlidingNum += 1;
			if (ContactJoints[1].RegularFootSliding[p] == 1f)
				ContactJoints[1].FootSlidingNum += 1;
		}

	}
	
	public void ExtractEEdistance()
    {
		// Capture All Contacts of Sensor in total Frame
		for (int k = 0; k < ContactJoints[0].bone_mat.Length - 1; k++)
		{

			ContactJoints[2].ExtractEEdistance(k);
			ContactJoints[3].ExtractEEdistance(k);
			ContactJoints[4].ExtractEEdistance(k);
		}
	}
	public void DerivedDraw(int index)
	{
		UltiDraw.Begin();

		Color[] colors = UltiDraw.GetRainbowColors(ContactJoints.Length);

		//if (true)
		//{
		//	for (int i = 0; i < exp_util.ContactJoints.Length; i++)
		//	{
		//		if (exp_util.ContactJoints[i].GetContact(index) == 1f)
		//		{
		//			Vector3 contact = exp_util.ContactJoints[i].GetContactPoint(index);
		//			UltiDraw.DrawSphere(contact, Quaternion.identity, DrawScale * 0.025f, UltiDraw.Yellow);
		//		}
		//	}
		//	//for (int i = 0; i < Sensors.Length; i++)
		//	//{
		//	//	Matrix4x4 bone = frame.GetBoneTransformation(Sensors[i].Bone, editor.Mirror);
		//	//	Matrix4x4 corrected = Sensors[i].GetCorrectedTransformation(frame, editor.Mirror);
		//	//	UltiDraw.DrawCube(bone, DrawScale * 0.025f, UltiDraw.DarkRed.Transparent(0.5f));
		//	//	UltiDraw.DrawLine(bone.GetPosition(), corrected.GetPosition(), colors[i].Transparent(0.5f));
		//	//	UltiDraw.DrawCube(corrected, DrawScale * 0.025f, UltiDraw.DarkGreen.Transparent(0.5f));
		//	//}
		//}

		if (true)
		{
			//Debug.Log("see " + ContactJoints.Length);
			//int nCount = 0;
			for (int i = 0; i < ContactJoints.Length; i++)
			{
				Quaternion rot = ContactJoints[i].bone_mat[index].GetRotation();
				Vector3 pos = ContactJoints[i].bone_mat[index].GetPosition() + rot * ContactJoints[i].Offset;
				UltiDraw.DrawCube(pos, rot, ContactJoints[i].DrawScale * 0.025f, UltiDraw.Black);
				UltiDraw.DrawWireSphere(pos, rot, 2f * ContactJoints[i].Threshold, colors[i].Transparent(0.25f));
				if (ContactJoints[i].GetContact(index) == 1f)
				{
					UltiDraw.DrawSphere(pos, rot, 2f * ContactJoints[i].Threshold, colors[i]);

					//if (ContactJoints[i].Bone != 0 && ContactJoints[i].Bone != 14 && ContactJoints[i].Bone != 18 && ContactJoints[i].Bone != 9 && ContactJoints[i].Bone != 13 &&
					//	ContactJoints[i].Bone != 16 && ContactJoints[i].Bone != 17 && ContactJoints[i].Bone != 20 && ContactJoints[i].Bone != 21)
					//	nCount++;

					if (ContactJoints[i].RegularFootSliding[index] > 0f)
					{
						Debug.Log("foot sliding " + ContactJoints[i].RegularFootVectors[index].magnitude);
						UltiDraw.DrawArrow(pos, pos + ContactJoints[i].RegularFootVectors[index] * 10.0f, 0.8f, ContactJoints[i].DrawScale * 0.005f, ContactJoints[i].DrawScale * 0.025f, UltiDraw.Black.Lighten(0.0f).Transparent(1.0f));
					}
				}
				else
				{
					UltiDraw.DrawSphere(pos, rot, 2f * ContactJoints[i].Threshold, colors[i].Transparent(0.125f));
				}
			}
			//Debug.Log("Frame:"+ index + "count: " + nCount);
		}


		//if (true)
		//{
		//	for (int i = 2; i < ContactJoints.Length; i++)
		//	{
		//		Quaternion rot = ContactJoints[i].bone_mat[index].GetRotation();
		//		Vector3 pos = ContactJoints[i].bone_mat[index].GetPosition() + rot * ContactJoints[i].Offset;
		//		UltiDraw.DrawCube(pos, rot, ContactJoints[i].DrawScale * 0.025f, UltiDraw.Black);
		//		UltiDraw.DrawWireSphere(pos, rot, 2f * ContactJoints[i].Threshold, colors[i].Transparent(0.25f));

		//		pos = ContactJoints[i].gt_position[index];
		//		UltiDraw.DrawCube(pos, rot, ContactJoints[i].DrawScale * 0.025f, UltiDraw.Black);
		//		UltiDraw.DrawWireSphere(pos, rot, 2f * ContactJoints[i].Threshold, UltiDraw.Yellow);
		//	}
		//}

		//if (ShowTolerances)
		//{
		//	for (int i = 0; i < Sensors.Length; i++)
		//	{
		//		Quaternion rot = editor.GetActor().GetBoneTransformation(Sensors[i].GetName()).GetRotation();
		//		Vector3 pos = editor.GetActor().GetBoneTransformation(Sensors[i].GetName()).GetPosition() + rot * Sensors[i].Offset;
		//		UltiDraw.DrawWireSphere(pos, rot, 2f * (Sensors[i].Tolerance + Sensors[i].Threshold), UltiDraw.DarkGrey.Transparent(0.25f));
		//	}
		//}

		//if (ShowContacts)
		//{
		//	for (int i = 0; i < Sensors.Length; i++)
		//	{
		//		if (Sensors[i].Edit != Sensor.ID.None)
		//		{
		//			for (float j = 0f; j <= Data.GetTotalTime(); j += Mathf.Max(Step, 1) / Data.Framerate)
		//			{
		//				Frame reference = Data.GetFrame(j);
		//				if (Sensors[i].GetContact(reference, editor.Mirror) == 1f)
		//				{
		//					UltiDraw.DrawSphere(Sensors[i].GetContactPoint(reference, editor.Mirror), Quaternion.identity, DrawScale * 0.025f, colors[i]);
		//				}
		//			}
		//		}
		//	}
		//}

		///*
		//if(ShowSkeletons) {
		//	UltiDraw.End();
		//	float start = Mathf.Clamp(frame.Timestamp-Window, 0f, Data.GetTotalTime());
		//	float end = Mathf.Clamp(frame.Timestamp+Window, 0f, Data.GetTotalTime());
		//	float inc = Mathf.Max(SkeletonStep, 1)/Data.Framerate;
		//	for(float j=start; j<=end; j+=inc) {
		//		Frame reference = Data.GetFrame(j);
		//		float weight = (j-start+inc) / (end-start+inc);
		//		editor.GetActor().Sketch(reference.GetBoneTransformations(editor.GetActor().GetBoneNames(), editor.Mirror), Color.Lerp(UltiDraw.Cyan, UltiDraw.Magenta, weight).Transparent(weight));
		//	}
		//	UltiDraw.Begin();
		//}
		//*/

		//if (TrueMotionTrajectory || CorrectedMotionTrajectory)
		//{
		//	for (int i = 0; i < Sensors.Length; i++)
		//	{
		//		if (Sensors[i].Edit != Sensor.ID.None)
		//		{
		//			Vector3 previousPos = Vector3.zero;
		//			Vector3 previousCorrected = Vector3.zero;
		//			float start = Mathf.Clamp(frame.Timestamp - PastTrajectoryWindow, 0f, Data.GetTotalTime());
		//			float end = Mathf.Clamp(frame.Timestamp + FutureTrajectoryWindow, 0f, Data.GetTotalTime());
		//			for (float j = start; j <= end; j += Mathf.Max(Step, 1) / Data.Framerate)
		//			{
		//				Frame reference = Data.GetFrame(j);
		//				Matrix4x4 bone = reference.GetBoneTransformation(Sensors[i].Bone, editor.Mirror);
		//				Matrix4x4 corrected = Sensors[i].GetCorrectedTransformation(reference, editor.Mirror);
		//				if (j > start)
		//				{
		//					if (TrueMotionTrajectory)
		//					{
		//						UltiDraw.DrawArrow(previousPos, bone.GetPosition(), 0.8f, DrawScale * 0.005f, DrawScale * 0.025f, UltiDraw.DarkRed.Lighten(0.5f).Transparent(0.5f));
		//					}
		//					if (CorrectedMotionTrajectory)
		//					{
		//						UltiDraw.DrawArrow(previousCorrected, corrected.GetPosition(), 0.8f, DrawScale * 0.005f, DrawScale * 0.025f, UltiDraw.DarkGreen.Lighten(0.5f).Transparent(0.5f));
		//					}
		//					//UltiDraw.DrawLine(previousPos, bone.GetPosition(), UltiDraw.DarkRed.Transparent(0.5f));
		//					//UltiDraw.DrawLine(previousCorrected, corrected.GetPosition(), UltiDraw.DarkGreen.Transparent(0.5f));
		//				}
		//				previousPos = bone.GetPosition();
		//				previousCorrected = corrected.GetPosition();
		//				if (TrueMotionTrajectory)
		//				{
		//					UltiDraw.DrawCube(bone, DrawScale * 0.025f, UltiDraw.DarkRed.Transparent(0.5f));
		//				}
		//				//UltiDraw.DrawLine(bone.GetPosition(), corrected.GetPosition(), colors[i].Transparent(0.5f));
		//				if (CorrectedMotionTrajectory)
		//				{
		//					UltiDraw.DrawCube(corrected, DrawScale * 0.025f, UltiDraw.DarkGreen.Transparent(0.5f));
		//				}
		//			}
		//		}
		//	}
		//}

		//if (ContactTrajectories)
		//{
		//	for (int i = 0; i < Sensors.Length; i++)
		//	{
		//		if (Sensors[i].Edit != Sensor.ID.None)
		//		{
		//			float start = Mathf.Clamp(frame.Timestamp - Window, 0f, Data.GetTotalTime());
		//			float end = Mathf.Clamp(frame.Timestamp + Window, 0f, Data.GetTotalTime());
		//			for (float j = 0f; j <= Data.GetTotalTime(); j += Mathf.Max(Step, 1) / Data.Framerate)
		//			{
		//				Frame reference = Data.GetFrame(j);
		//				if (Sensors[i].GetContact(reference, editor.Mirror) == 1f)
		//				{
		//					Vector3 contact = Sensors[i].GetContactPoint(reference, editor.Mirror);
		//					Vector3 corrected = Sensors[i].GetCorrectedContactPoint(reference, editor.Mirror);
		//					UltiDraw.DrawArrow(contact, corrected, 0.8f, Vector3.Distance(contact, corrected) * DrawScale * 0.025f, Vector3.Distance(contact, corrected) * DrawScale * 0.1f, colors[i].Lighten(0.5f).Transparent(0.5f));
		//					UltiDraw.DrawSphere(contact, Quaternion.identity, DrawScale * 0.0125f, colors[i].Transparent(0.5f));
		//					UltiDraw.DrawSphere(corrected, Quaternion.identity, DrawScale * 0.05f, colors[i]);
		//				}
		//			}
		//		}
		//	}
		//}

		//if (ShowDistances)
		//{
		//	for (int i = 0; i < Sensors.Length; i++)
		//	{
		//		if (Sensors[i].Edit != Sensor.ID.None)
		//		{
		//			for (float j = frame.Timestamp - PastTrajectoryWindow; j <= frame.Timestamp + FutureTrajectoryWindow; j += Mathf.Max(Step, 1) / Data.Framerate)
		//			{
		//				Frame reference = Data.GetFrame(j);
		//				if (Sensors[i].GetContact(reference, editor.Mirror) == 1f)
		//				{
		//					UltiDraw.DrawArrow(Sensors[i].GetContactPoint(reference, editor.Mirror), Sensors[i].GetContactPoint(reference, editor.Mirror) - Sensors[i].GetContactDistance(reference, editor.Mirror), 0.8f, DrawScale * 0.0025f, DrawScale * 0.01f, colors[i].Transparent(0.5f));
		//				}
		//			}
		//		}
		//	}
		//}

		UltiDraw.End();
	}

	public float Threshold = 0.1f;

    [System.Serializable]
	public class ContactJointSensor
	{
		public enum ID
		{
			None,
			Closest,
			RayTopDown, RayCenterDown, RayBottomUp, RayCenterUp,
			SphereTopDown, SphereCenterDown, SphereBottomUp, SphereCenterUp,
			RayXPositive, RayXNegative, RayYPositive, RayYNegative, RayZPositive, RayZNegative,
			Identity
		};
		
		public float CaptureFilter = 0.1f;
		public float Framerate;
		public int Bone = 0;
		public int nFrames = 0;
		public Vector3 Offset = Vector3.zero;
		public float Threshold = 0.14f;
		public float Tolerance = 0f;
		public float Velocity = 0f;
		public bool SolvePosition = true;
		public bool SolveRotation = true;
		public bool SolveDistance = true;
		public LayerMask Mask = 1 << LayerMask.NameToLayer("Penetration");
		public ID Capture = ID.Closest;
		public ID Edit = ID.None;
		public float Weight = 1f;
		public float DrawScale = 1.0f;

		public float[] RegularContacts = new float[0];
		public Vector3[] RegularContactPoints = new Vector3[0];
		public Vector3[] RegularDistances = new Vector3[0];
		public float[] RegularFootSliding = new float[0];
		public Vector3[] RegularFootVectors = new Vector3[0];
		public int FootSlidingNum = 0;
		public float[] RegularEEdistances = new float[0];

		public int index;

		public Matrix4x4[] bone_mat = new Matrix4x4[0];
		public Vector3[] gt_position = new Vector3[0];
		public ContactJointSensor(int nframes, int bone, Vector3 offset, float tolerance, float threshold, float velocity, ID capture, ID edit)
		{
			nFrames = nframes; // 마지막 프레임은 velocity 를 못만든다
			Bone = bone;
			//Offset = offset;
			Threshold =  threshold;
			//Tolerance = tolerance;
			//Velocity = velocity;
			//Capture = capture;
			//Edit = edit;
			RegularContacts = new float[nFrames];
			RegularContactPoints = new Vector3[nFrames];
			RegularDistances = new Vector3[nFrames];
			RegularFootSliding = new float[nFrames];
			RegularFootVectors = new Vector3[nFrames];
			RegularEEdistances = new float[nFrames];

			//input 
			bone_mat = new Matrix4x4[nFrames];
			gt_position = new Vector3[nFrames];
		}

		public Vector3 GetPivot(Matrix4x4 f)
		{
			return Offset.GetRelativePositionFrom(f);
		}

		public float GetContact(int index)
		{
			return RegularContacts[index];
		}

		public Vector3 GetContactDistance(int index)
		{
			return RegularDistances[index];
		}

		public Vector3 GetContactPoint(int index)
		{
			return RegularContactPoints[index];
		}

		public Matrix4x4[] GetFrames(int start, int end, Matrix4x4[] trs)
		{
			if (start < 1 || end > nFrames)
			{
				Debug.Log("Please specify indices between 1 and " + nFrames + ". Given " + start + " and " + end + ".");
				return null;
			}
			int count = end - start + 1;
			Matrix4x4[] frames = new Matrix4x4[count];
			for (int i = start; i <= end; i++)
			{
				frames[i - start] = trs[i - 1];
			}
			return frames;
		}

		public Vector3 GetBoneVelocity(int index, float delta, Matrix4x4[] trs)
		{
			if (delta == 0f)
			{
				return Vector3.zero;
			}
			return (trs[index + 1].GetPosition() - trs[index].GetPosition()) / delta;
		}

		public Vector3 DetectCollision(Matrix4x4 tr, ContactJointSensor.ID mode, Vector3 pivot, float radius, out Collider collider)
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
				bool hit = Physics.SphereCast(pivot + new Vector3(0f, radius + Threshold, 0f), Threshold, Vector3.down, out info, 2f * radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.SphereCenterDown)
			{
				RaycastHit info;
				bool hit = Physics.SphereCast(pivot + new Vector3(0f, radius, 0f), Threshold, Vector3.down, out info, radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.SphereBottomUp)
			{
				RaycastHit info;
				bool hit = Physics.SphereCast(pivot - new Vector3(0f, radius + Threshold, 0f), Threshold, Vector3.up, out info, 2f * radius, Mask);
				if (hit)
				{
					collider = info.collider;
					return info.point;
				}
			}

			if (mode == ID.SphereCenterUp)
			{
				RaycastHit info;
				bool hit = Physics.SphereCast(pivot - new Vector3(0f, radius, 0f), Threshold, Vector3.up, out info, radius, Mask);
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

		//TODO: FilterGaussian here has problems at the boundary of the file since the pivot point is not centered.
		public void CaptureContact(int index)
		{
			//Debug.Log("threshold " + Threshold);
			int width = 3;// Mathf.RoundToInt(CaptureFilter * Framerate);
			Matrix4x4[] frames = GetFrames(Mathf.Clamp(index - width, 1, nFrames), Mathf.Clamp(index + width, 1, nFrames),bone_mat);
			//Debug.Log("Capture " + Capture + "mask " + Mask + "frames " +frames.Length);
			{
				bool[] contacts = new bool[frames.Length];
				Vector3[] contactPoints = new Vector3[frames.Length];
				Vector3[] distances = new Vector3[frames.Length];
				for (int i = 0; i < frames.Length; i++)
				{
					Matrix4x4 tr = frames[i];
					Vector3 bone = tr.GetPosition();
					Vector3 pivot = GetPivot(tr);
					Collider collider;
					Vector3 collision = DetectCollision(tr, Capture, pivot, Threshold, out collider);
					contacts[i] = collider != null;
					if (collider != null)
					{
						Vector3 distance = collision - bone;
						contactPoints[i] =  collision;
						distances[i] = distance;
					}
				}
				bool hit = Utility.GetMostCommonItem(contacts);
				if (hit)
				{
					RegularContacts[index] = 1f;
					RegularDistances[index] = Utility.GetMostCenteredVector(distances, contacts);
					RegularContactPoints[index] = Utility.GetMostCenteredVector(contactPoints, contacts);

				}
				else
				{
					RegularContacts[index] = 0f;
					RegularDistances[index] = Vector3.zero;
					RegularContactPoints[index] = Vector3.zero;
				}
			}
			
			if (Velocity > 0f)
			{
				if (GetContact(index) == 1f)
				{
					if (GetBoneVelocity(index, 1f / Framerate,bone_mat).magnitude > Velocity)
					{
						RegularContacts[index] = 0f;
						RegularContactPoints[index] = GetPivot(bone_mat[index]);
						RegularDistances[index] = Vector3.zero;
					}
				}
			}
		}
		public void ExtractFootSliding(int start, int end)
        {
			if(bone_mat.Length > 1)
            {

				// int width = 3;// Mathf.RoundToInt(CaptureFilter * Framerate);
				Matrix4x4[] frames = GetFrames(Mathf.Clamp(start, 1, nFrames), Mathf.Clamp(end, 1, nFrames), bone_mat);

				Vector3[] velocity = new Vector3[frames.Length - 1];
				for (int i = 0; i < frames.Length - 1; i++)
				{
					velocity[i] = frames[i + 1].GetPosition() - frames[0].GetPosition();
				}
				Vector3 mean_vel = velocity.Last(); 
				Debug.Log("velocity mag " + mean_vel.magnitude + " of " + (frames.Length - 1));
				if (mean_vel.magnitude > 0.01f)
				{
					RegularFootSliding[start] = 1f;
					RegularFootVectors[start] = mean_vel;
				}
				else
				{
					RegularFootSliding[start] = 0f;
					RegularFootVectors[start] = Vector3.zero;
				}

			}
            else
            {
				Debug.Log("There is no bones data ");
            }
        }

		public void ExtractEEdistance(int index)
        {
			RegularEEdistances[index] = (gt_position[index] - bone_mat[index].GetPosition()).magnitude;

		}
		public void Inspector(int index)
		{
			UltiDraw.Begin();
			Utility.SetGUIColor(UltiDraw.Grey);
			using (new EditorGUILayout.VerticalScope("Box"))
			{
				Utility.ResetGUIColor();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Bone" + Bone.ToString(), GUILayout.Width(40f));
				EditorGUILayout.LabelField("Mask", GUILayout.Width(30));
				Mask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(Mask), InternalEditorUtility.layers, GUILayout.Width(75f)));
				EditorGUILayout.LabelField("Capture", GUILayout.Width(50));
				Capture = (ID)EditorGUILayout.EnumPopup(Capture, GUILayout.Width(75f));
				EditorGUILayout.LabelField("Edit", GUILayout.Width(30));
				Edit = (ID)EditorGUILayout.EnumPopup(Edit, GUILayout.Width(75f));
				EditorGUILayout.LabelField("Solve Position", GUILayout.Width(80f));
				SolvePosition = EditorGUILayout.Toggle(SolvePosition, GUILayout.Width(20f));
				EditorGUILayout.LabelField("Solve Rotation", GUILayout.Width(80f));
				SolveRotation = EditorGUILayout.Toggle(SolveRotation, GUILayout.Width(20f));
				EditorGUILayout.LabelField("Solve Distance", GUILayout.Width(80f));
				SolveDistance = EditorGUILayout.Toggle(SolveDistance, GUILayout.Width(20f));
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Offset", GUILayout.Width(40f));
				Offset = EditorGUILayout.Vector3Field("", Offset, GUILayout.Width(180f));
				EditorGUILayout.LabelField("Threshold", GUILayout.Width(70f));
				Threshold = EditorGUILayout.FloatField(Threshold, GUILayout.Width(50f));
				EditorGUILayout.LabelField("Tolerance", GUILayout.Width(70f));
				Tolerance = EditorGUILayout.FloatField(Tolerance, GUILayout.Width(50f));
				EditorGUILayout.LabelField("Velocity", GUILayout.Width(70f));
				Velocity = EditorGUILayout.FloatField(Velocity, GUILayout.Width(50f));
				EditorGUILayout.LabelField("Weight", GUILayout.Width(60f));
				Weight = EditorGUILayout.FloatField(Weight, GUILayout.Width(50f));
				EditorGUILayout.EndHorizontal();

				//Frame frame = editor.GetCurrentFrame();
				//MotionData data = editor.GetData();

				EditorGUILayout.BeginVertical(GUILayout.Height(10f));
				Rect ctrl = EditorGUILayout.GetControlRect();
				Rect rect = new Rect(ctrl.x, ctrl.y, ctrl.width, 10f);
				EditorGUI.DrawRect(rect, UltiDraw.Black);

				//float startTime = frame.Timestamp - editor.GetWindow() / 2f;
				//float endTime = frame.Timestamp + editor.GetWindow() / 2f;
				//if (startTime < 0f)
				//{
				//	endTime -= startTime;
				//	startTime = 0f;
				//}
				//if (endTime > data.GetTotalTime())
				//{
				//	startTime -= endTime - data.GetTotalTime();
				//	endTime = data.GetTotalTime();
				//}
				//startTime = Mathf.Max(0f, startTime);
				//endTime = Mathf.Min(data.GetTotalTime(), endTime);
				int start = 0;
				int end = nFrames;
				int elements = end - start;

				Vector3 bottom = new Vector3(0f, rect.yMax, 0f);
				Vector3 top = new Vector3(0f, rect.yMax - rect.height, 0f);

				start = Mathf.Clamp(start, 1, nFrames);
				end = Mathf.Clamp(end, 1, nFrames);

				//Contacts
				for (int i = start; i <= end; i++)
				{
					if ((RegularContacts[i - 1]) == 1f)
					{
						float left = rect.xMin + (float)(i - start) / (float)elements * rect.width;
						float right = left;
						while (i < end && (RegularContacts[i - 1]) != 0f)
						{
							right = rect.xMin + (float)(i - start) / (float)elements * rect.width;
							i++;
						}
						if (left != right)
						{
							Vector3 a = new Vector3(left, rect.y, 0f);
							Vector3 b = new Vector3(right, rect.y, 0f);
							Vector3 c = new Vector3(left, rect.y + rect.height, 0f);
							Vector3 d = new Vector3(right, rect.y + rect.height, 0f);
							UltiDraw.DrawTriangle(a, c, b, UltiDraw.Green);
							UltiDraw.DrawTriangle(b, c, d, UltiDraw.Green);
						}
					}

					

				}

				//Current Pivot
				top.x = rect.xMin + (float)(index - start) / elements * rect.width;
				bottom.x = rect.xMin + (float)(index - start) / elements * rect.width;
				top.y = rect.yMax - rect.height;
				bottom.y = rect.yMax;
				UltiDraw.DrawLine(top, bottom, UltiDraw.Yellow);

				if ((RegularFootSliding[index]) == 1f)
				{
					//Current Pivot
					top.x = rect.xMin + (float)(index - start) / elements * rect.width;
					bottom.x = rect.xMin + (float)(index - start) / elements * rect.width;
					top.y = rect.yMax - rect.height;
					bottom.y = rect.yMax;
					UltiDraw.DrawLine(top, bottom, UltiDraw.Red);
				}




				Handles.DrawLine(Vector3.zero, Vector3.zero); //Somehow needed to get it working...

				EditorGUILayout.EndVertical();
			}

			UltiDraw.End();
		}
	}

}