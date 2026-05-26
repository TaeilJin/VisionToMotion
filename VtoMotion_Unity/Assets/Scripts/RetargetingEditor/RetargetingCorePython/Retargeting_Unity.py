import numpy as np
import os;
import ctypes
from numpy.ctypeslib import ndpointer
import Core_Draw as core_draw
import Core_CDLL as core_cdll

from array import array
import struct

""" external dll """
os.environ['PATH'] = 'vs2015' + os.pathsep + os.environ['PATH']
os.environ['PATH'] = 'lapack/bin' + os.pathsep + os.environ['PATH']

lib = ctypes.WinDLL('MBS_CDLL')
# D:\\TJ_develop\\MW\\MW_HumanMotionRetargeting\\Examples\\MBS_CDLL\\x64\\Release\\MBS_CDLL
""" define dll in/out """
lib.LOAD_SRC_TAR_MBS.argtypes = [ctypes.c_char_p, ctypes.c_char_p]
lib.INIT_MAPPING_fromTXT.argtypes = [ctypes.c_char_p]
lib.UPDATE_POSE_UnitytoMW.argtypes =[ctypes.c_int,ctypes.POINTER(ctypes.c_float)]
lib.OUTPUT_JOINT_POSE_UNITY.argtypes =[ctypes.c_int, ctypes.POINTER(ctypes.c_float)]
lib.DO_RETARGET_OUTPUT.argtypes =[ctypes.c_float,ctypes.c_float,ctypes.c_float]
lib.SAVE_BVH.argtypes = [ctypes.c_char_p, ctypes.c_int, ctypes.c_float, ctypes.c_float]
""" python connection """


import socket
import json

server_ip = '143.248.6.198'  # 모든 네트워크 인터페이스에서 연결 수락
server_port = 80

server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind((server_ip, server_port))
server_socket.listen(1)  # 최대 동시 연결 수

print(f"서버 대기 중... IP: {server_ip}, 포트: {server_port}")

client_socket, client_address = server_socket.accept()
print(f"연결 수락: {client_address[0]}:{client_address[1]}") 

while True:
    # Unity에서의 메시지 수신
    data = client_socket.recv(4096)
    if not data:
        break

    try:
        # JSON 데이터를 파싱하여 데이터 추출
        data_packet = json.loads(data.decode('utf-8'))
    except (json.JSONDecodeError) as e:
        print(f"예외 발생 : {e}")

    """ initialize MBS """
    if(data_packet['text_indicator'] == "initMBS"):
       
        numlinks = lib.LOAD_SRC_TAR_MBS(data_packet['text_mbs_src_txt'].encode('utf-8'),
                                        data_packet['text_mbs_tar_txt'].encode('utf-8'))

        numlinks =lib.INIT_RETARGET()
        print(f"TARGET numlink : {numlinks}")

        lib.INIT_MAPPING_fromTXT(data_packet['text_jointmapping_txt'].encode('utf-8'))
       
    """ retargeting """
    if(data_packet['text_indicator'] == "doRetarget"):
        # float 배열 생성
        float_array = (ctypes.c_float * len(data_packet['floatArray']))(*data_packet['floatArray'])
        # update mbs src
        lib.UPDATE_POSE_UnitytoMW(0,float_array)
        # do retarget
        base_offset = (ctypes.c_float * len(data_packet['base_offset']))(*data_packet['base_offset'])
        lib.DO_RETARGET_OUTPUT(-1*base_offset[0],base_offset[1],base_offset[2])
        # output mbs tar
        float_output_array = (ctypes.c_float * (numlinks*4+3))()
        lib.OUTPUT_JOINT_POSE_UNITY(1,float_output_array)
        
        # float 배열을 바이트로 변환
        data_to_send = bytearray(struct.pack(f'{len(float_output_array)}f', *float_output_array))
        client_socket.send(data_to_send)

client_socket.close()