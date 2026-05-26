import numpy as np
from scipy.optimize import least_squares, minimize, leastsq
import os;
import ctypes
from numpy.ctypeslib import ndpointer
from raylib import *
import pyray as pr
import Core_Draw as core_draw
import Core_CDLL as core_cdll

""" external dll """
os.environ['PATH'] = 'D:\\TJ_develop\\MW\\MW_HumanMotionRetargeting\\Libraries\\OpenSceneGraph_354\\bin_x64\\vs2015' + os.pathsep + os.environ['PATH']
os.environ['PATH'] = 'D:\\TJ_develop\\MW\\MW_HumanMotionRetargeting\\Libraries\\lapack\\bin' + os.pathsep + os.environ['PATH']

lib = ctypes.WinDLL('D:/TJ_develop/MW/MW_HumanMotionRetargeting/Examples/MBS_CDLL/x64/Release/MBS_CDLL')
""" Function """
lib.SAVE_BVH.argtypes = [ctypes.c_char_p, ctypes.c_int, ctypes.c_float, ctypes.c_float]
lib.DO_RETARGET_OUTPUT.argtypes =[ctypes.c_float,ctypes.c_float,ctypes.c_float]
lib.READ_FrameTime.restype = ctypes.c_float
lib.SET_DESIRED_BASE.argtypes = [ctypes.POINTER(ctypes.c_float)]
lib.SET_MBS_FromEXP.argtypes = [ctypes.c_int , ctypes.POINTER(ctypes.c_float)]
lib.MEASURE_DIRECTION.argtypes = [ctypes.c_int, ctypes.c_int]
lib.MEASURE_DIRECTION.restype = ctypes.POINTER(ctypes.c_float)
lib.MEASURE_POSITION.argtypes = [ctypes.c_int, ctypes.POINTER(ctypes.c_float)]
lib.MEASURE_POSITION.restype = ctypes.POINTER(ctypes.c_float)
lib.GET_DIRECTIONS.argtypes = [ctypes.c_int, ctypes.POINTER(ctypes.c_int),ctypes.POINTER(ctypes.c_int)]
lib.READ_DIRECTION_SIZE.restype = ctypes.c_int
lib.READ_MBS_JOINT_POSITION.argtypes = [ctypes.c_int,ctypes.c_int]
lib.READ_MBS_JOINT_POSITION.restype = ctypes.POINTER(ctypes.c_float)
lib.READ_MBS_POSE.argtypes = [ctypes.c_int]
lib.READ_MBS_POSE.restype = ctypes.POINTER(ctypes.c_float)

""" initialize SRC/TAR MBS """
#lib.GEN_MBS_TXTFILE(f'Fanie_Hiphop-01.bvh'.encode('utf-8'),f'MoCap.txt'.encode('utf-8'))
numlinks = lib.LOAD_SRC_TAR_MBS(f'Source_Fanie.txt'.encode('utf-8'),f'Target_amass.txt'.encode('utf-8'))
print(numlinks)
print (lib.READ_FrameTime())

""" initialize Retargeting Solver """
lib.LOAD_SRC_MOTION(f'Fanie_Hiphop-01.bvh'.encode('utf-8'), ctypes.c_float(0.0254))
total_frames = lib.READ_TOTALFRAMES()
lib.INIT_RETARGET()

""" initialize Directional Retargeting """
lib.INIT_MAPPING_fromTXT(f'Source_Fanie+Target_amass.txt'.encode('utf-8'))
#print(lib.INIT_DATA(0,ctypes.c_int(total_frames)))
print(lib.INIT_DATA(1,ctypes.c_int(total_frames)))


""" pose to pose retargeting """
for i in range(0,total_frames):
    lib.UPDATE_POSE_fromData(0,i)
    lib.DO_RETARGET_OUTPUT(0.0,-0.17,0.0)
    lib.UPDATE_JOINTVEC(1,i)

""" save BVH """
lib.SAVE_BVH(f"Amass_Fanie_Hiphop-01.bvh".encode('utf-8'),1,lib.READ_FrameTime(),1/0.0254)