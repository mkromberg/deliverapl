#!/usr/bin/env python
# -*- coding: utf-8 -*-

import json
import os
from ctypes import cdll, c_wchar_p, create_unicode_buffer

path = (os.path.dirname(os.path.abspath(__file__))).replace('\\', '/')+'/' # Folder we were loaded from
APL = cdll.LoadLibrary(path + "libJSON_APL.dll") # Load JSON_APL library

def main():
  InitAPL(1,"")
  CallJSON("Load", path + "involute.dyalog")  
  spiral = CallJSON("Involute", 7)[0]
  for i in range (0,len(spiral)):
    print(spiral[i])

def InitAPL(runtime, WSargs): 
  __C_APL_WSargs_Binding_Params__ = cUnicodeList(WSargs) 
  APL.Initialise(runtime,len(WSargs),__C_APL_WSargs_Binding_Params__)

def cUnicodeList(pylist):
  cUL = (c_wchar_p * len(pylist))()
  cUL[:] = pylist
  return cUL

def CallJSON(function, parms):
  result = create_unicode_buffer('', 256)
  err = APL.CallJSON(function, json.dumps(parms), result)
  return (json.loads(result.value), err)

main()