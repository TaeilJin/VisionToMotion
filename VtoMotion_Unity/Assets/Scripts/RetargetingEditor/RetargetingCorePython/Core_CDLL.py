import ctypes
import os

""" external dll """
os.environ['PATH'] = 'D:\\TJ_develop\\MW\\MW_HumanMotionRetargeting\\Libraries\\OpenSceneGraph_354\\bin_x64\\vs2015' + os.pathsep + os.environ['PATH']
os.environ['PATH'] = 'D:\\TJ_develop\\MW\\MW_HumanMotionRetargeting\\Libraries\\lapack\\bin' + os.pathsep + os.environ['PATH']

lib = ctypes.WinDLL('D:/TJ_develop/MW/MW_HumanMotionRetargeting/Examples/MBS_CDLL/x64/Release/MBS_CDLL')

# mbs 0 : mbs_src
# mbs 1 : mbs_tar
# mbs 2 : mbs
""" Function """

# init DATA
init_data = lib.INIT_DATA
""" 
    Initialize Motion Vector 

    Parameters : 
        [ mbs_id (int), Frames (int)] 
    
    Returns :
        Total Frames (int)
"""
init_data.argtypes = [ctypes.c_int, ctypes.c_int]
init_data.restype = ctypes.c_int

# save bvh
save_bvh = lib.SAVE_BVH 
""" 
    Saving BVH Files (! INIT DATA 가 먼저 정의되어야함 !)

    Parameters : 
        [ filename (char), mbs_id (int), framerate (float), scale (float)] 
    
    Returns :
        None
"""
save_bvh.argtypes = [ctypes.c_char_p, ctypes.c_int, ctypes.c_float, ctypes.c_float]
save_bvh.restype = None




lib.DO_RETARGET_OUTPUT.argtypes =[ctypes.c_float]
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