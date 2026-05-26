using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skeleton : ScriptableObject
{
	public class Hierarchy
	{
		public Bone[] Bones;

		private string[] Names = null;

		public Hierarchy()
		{
			Bones = new Bone[0];
		}

		public void AddBone(string name, string parent)
		{
			ArrayExtensions.Add(ref Bones, new Bone(Bones.Length, name, parent));
		}

		public Bone FindBone(string name)
		{
			return System.Array.Find(Bones, x => x.Name == name);
		}

		public Bone FindBoneContains(string name)
		{
			return System.Array.Find(Bones, x => x.Name.Contains(name));
		}

		public string[] GetBoneNames()
		{
			if (Names == null || Names.Length != Bones.Length)
			{
				Names = new string[Bones.Length];
				for (int i = 0; i < Bones.Length; i++)
				{
					Names[i] = Bones[i].Name;
				}
			}
			return Names;
		}

		public bool[] GetBoneFlags(params string[] bones)
		{
			bool[] flags = new bool[Bones.Length];
			for (int i = 0; i < bones.Length; i++)
			{
				Bone bone = FindBone(bones[i]);
				if (bone != null)
				{
					flags[bone.Index] = true;
				}
			}
			return flags;
		}

		public int[] GetBoneIndices(params string[] bones)
		{
			int[] indices = new int[bones.Length];
			for (int i = 0; i < bones.Length; i++)
			{
				Bone bone = FindBone(bones[i]);
				indices[i] = bone == null ? -1 : bone.Index;
			}
			return indices;
		}

		[System.Serializable]
		public class Bone
		{
			public int Index = -1;
			public string Name = "";
			public string Parent = "";
			public float Mass = 1f;
			public Vector3 Alignment = Vector3.zero;
			public Bone(int index, string name, string parent)
			{
				Index = index;
				Name = name;
				Parent = parent;
				Mass = 1f;
				Alignment = Vector3.zero;
			}
		}
	}
}
