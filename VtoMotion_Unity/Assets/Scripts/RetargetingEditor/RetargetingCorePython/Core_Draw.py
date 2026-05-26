from raylib import *
import pyray as pr

def np3_to_Vector3(np3):
    ry_vec = pr.Vector3(0,0,0)
    ry_vec.x = np3[0]
    ry_vec.y = np3[1]
    ry_vec.z = np3[2]
    return ry_vec

def update_drawJoints(parents, nGlobalCnt, input_joints, color):
    """
        Drawing Pose using Color Lines

        Parameters:

            parents: 모든 조인트의 parent 벡터

            nGlobalCnt (int)

            input_joints (TotalFrames, num_joints, 3)

            color (pyray.color)
        
    """
    joints = input_joints.copy()
    # draw joints
    for j in range(len(parents)):
        if parents[j] != -1:
            DrawLine3D(np3_to_Vector3(joints[nGlobalCnt,j,:]),np3_to_Vector3(joints[nGlobalCnt,parents[j],:]),color)