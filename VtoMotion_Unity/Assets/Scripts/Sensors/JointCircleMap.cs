
using System.Collections;
using System.Collections.Generic;
using UnityEngine; 

[System.Serializable]
public class JointCircleMap
{

    public Matrix4x4 Pivot = Matrix4x4.identity;

    public int numLongitudes = 0;
    public int numLatitudes = 0;
    public int numRays = 0;
    public float[] Grid_latitudes = new float[0];
    public float[] Grid_longithudes = new float[0];
    public LayerMask Mask = -1;

    public class CircleMapInfo
    {
        public Vector3[,] Positions;
        public Vector3[,] Directions;
        public float[,] HitDistances;
        public bool[,] Hit;
        public CircleMapInfo(int Bones, int Rays)
        {
            Positions = new Vector3[Bones, Rays];
            Directions = new Vector3[Bones, Rays];
            Hit = new bool[Bones, Rays];
            HitDistances = new float[Bones, Rays];


        }
    }
    CircleMapInfo CMapInfo;

    public JointCircleMap(int L, int Bones, LayerMask mask)
    {
        //int L = 10; // 최대 구면 조화함수 차수

        // 구면 조화함수 차수를 이용하여 grid sampling
        
        Grid_latitudes = SamplingDegrees(2*L);
        Grid_longithudes = SamplingDegrees(2 * L);

        numLatitudes = 2*L;
        numLongitudes = 2 * L;

        numRays = numLatitudes * numLongitudes; 
        CMapInfo = new CircleMapInfo(Bones, numRays);
        GenerateRays(Bones);
        Mask = mask;
        //// 결과 출력
        //Console.WriteLine("Grid Sampling Result:");
        //for (int i = 0; i < numLatitudes; i++)
        //{
        //    for (int j = 0; j < numLongitudes; j++)
        //    {
        //        double longitude = grid[i, j];
        //        double latitude = (i * Math.PI) / L;

        //        Console.WriteLine("Latitude: {0}, Longitude: {1}", latitude, longitude);
        //    }
        //}
    }
 

    // 0에서 360 사이의 경도를 2L 등분
    public float[] SamplingDegrees(int L)
    {
        float[] longitudes = new float[L];

        for (int i = 0; i < L; i++)
        {
            float longitude = i * (360.0f / (L));
            longitudes[i] = longitude;
            //xDebug.Log(longitude);
        }

        return longitudes;
    }

    public Vector3 RayFromLongLati(float latitudeDegrees, float longitudeDegrees)
    {
       
        // unit vector 계산
        float x = Mathf.Sin(latitudeDegrees * Mathf.Deg2Rad) * Mathf.Cos(longitudeDegrees * Mathf.Deg2Rad);
        float y = Mathf.Sin(latitudeDegrees * Mathf.Deg2Rad) * Mathf.Sin(longitudeDegrees * Mathf.Deg2Rad);
        float z = Mathf.Cos(latitudeDegrees * Mathf.Deg2Rad);

        Vector3 ray = new Vector3(x, y, z);
        ray = ray.normalized;
        return ray;
    }

    public void GenerateRays(int Bones)
    {
        for (int joint_index = 0; joint_index < Bones; joint_index++)
        {
            int cnt = 0;
            for (int i = 0; i < numLatitudes; i++)
            {
                float lati = Grid_longithudes[i];
                for (int j = 0; j < numLongitudes; j++)
                {
                    Vector3 dir = RayFromLongLati(lati, Grid_longithudes[j]);
                    CMapInfo.Directions[joint_index, cnt] = dir;
                    cnt++;
                }
            }
            Debug.Log(cnt);
        }
    }
    public void JointSense(Matrix4x4 pivot, int joint_index)
    {
        // 
        //Debug.Log("num Lati " + numLatitudes + " num Long " + numLongitudes + " nuRays " + numRays);
        for (int i = 0; i < numRays; i++)
        {
            Vector3 dir = CMapInfo.Directions[joint_index, i];
            
            Vector3 point = Project(pivot.GetPosition(), dir,
                    out bool b_hit,
                    out float hit_value);

            CMapInfo.Positions[joint_index, i] = point;
            CMapInfo.HitDistances[joint_index, i] = hit_value;
            CMapInfo.Hit[joint_index, i] = b_hit;
        }
        
    }
  
    private Vector3 Project(Vector3 position, Vector3 dir, 
        out bool b_hit,
        out float distance)
    {
        RaycastHit hit;
        b_hit = Physics.Raycast(position, dir, out hit, 100f, Mask);
        position = hit.point;
        distance = hit.distance;
        return position;
    }

    public void Draw(Matrix4x4 pivot, int joint_index)
    {
        UltiDraw.Begin();
        for (int j = 0; j < numRays; j++)
        {
            UltiDraw.DrawLine(pivot.GetPosition(), pivot.GetPosition() + (CMapInfo.Directions[joint_index, j]).GetRelativeDirectionFrom(pivot) * 0.7f, UltiDraw.Black);
            if (CMapInfo.Hit[joint_index, j])
            {
                //Debug.Log("hit " + CMapInfo.HitDistances[joint_index, j]);
                UltiDraw.DrawSphere(CMapInfo.Positions[joint_index, j], Quaternion.identity, 0.025f, UltiDraw.Red.Transparent(1.0f));
               
            }
        }
        UltiDraw.End();
    }

}
