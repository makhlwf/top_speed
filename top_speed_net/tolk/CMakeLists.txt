cmake_minimum_required(VERSION 3.10)
project(Tolk)

option (TOLK_MULTITHREADED "Build Multi-Threaded (/MT) instead of Multi-Threaded DLL (/MD)" OFF)
option (BUILD_EXAMPLES "Build example applications" OFF)

if (TOLK_MULTITHREADED)
  foreach (flag_var
      CMAKE_C_FLAGS CMAKE_C_FLAGS_DEBUG CMAKE_C_FLAGS_RELEASE
      CMAKE_C_FLAGS_MINSIZEREL CMAKE_C_FLAGS_RELWITHDEBINFO
      CMAKE_CXX_FLAGS CMAKE_CXX_FLAGS_DEBUG CMAKE_CXX_FLAGS_RELEASE
      CMAKE_CXX_FLAGS_MINSIZEREL CMAKE_CXX_FLAGS_RELWITHDEBINFO)
    if (${flag_var} MATCHES "/MD")
      STRING(REGEX REPLACE "/MD" "/MT" ${flag_var} "${${flag_var}}")
    endif()
  endforeach(flag_var)
endif()

set (TOLK_COMPILE_FLAGS -D_EXPORTING -DUNICODE -D_UNICODE)
set (TOLK_LINK_FLAGS user32 ole32 oleaut32 sapi)

add_library(Tolk SHARED src/Tolk.cpp src/Tolk.h src/TolkVersion.h
  src/ScreenReaderDriver.h src/Tolk.rc
  src/ScreenReaderDriverBOY.cpp src/ScreenReaderDriverBOY.h
  src/ScreenReaderDriverJAWS.cpp src/ScreenReaderDriverJAWS.h
  src/ScreenReaderDriverNVDA.cpp src/ScreenReaderDriverNVDA.h
  src/ScreenReaderDriverSA.cpp src/ScreenReaderDriverSA.h
  src/ScreenReaderDriverSNova.cpp src/ScreenReaderDriverSNova.h
  src/ScreenReaderDriverWE.cpp src/ScreenReaderDriverWE.h
  src/ScreenReaderDriverZDSR.cpp src/ScreenReaderDriverZDSR.h
  src/ScreenReaderDriverZT.cpp src/ScreenReaderDriverZT.h
  src/ScreenReaderDriverSAPI.cpp src/ScreenReaderDriverSAPI.h
  src/fsapi.c src/fsapi.h src/wineyes.c src/wineyes.h src/zt.c src/zt.h)

target_include_directories (Tolk INTERFACE src)
target_link_libraries (Tolk PRIVATE ${TOLK_LINK_FLAGS})
target_compile_options (Tolk PRIVATE ${TOLK_COMPILE_FLAGS})

if(CMAKE_CXX_COMPILER_ID MATCHES "GNU")
  target_link_libraries(Tolk PUBLIC -static-libgcc;-static-libstdc++)
endif()

## ---- install
# add README
install (FILES docs/README.md DESTINATION docs)
# add LICENSE
install (FILES LICENSE.txt LICENSE-NVDA.txt DESTINATION license)
# add Binay DLL
install (TARGETS Tolk DESTINATION lib)
# Install PDB files for Debug builds
install (FILES Tolk.pdb DESTINATION lib OPTIONAL)
# add header file
install (FILES src/Tolk.h DESTINATION include)
# add Client libraries
if (${CMAKE_GENERATOR_PLATFORM} MATCHES "x64")
  install (FILES libs/x64/nvdaControllerClient64.dll libs/x64/SAAPI64.dll DESTINATION lib)
  install (FILES libs/x64/BoyCtrl-x64.dll DESTINATION lib)
  install (FILES libs/x64/ZDSRAPI_x64.dll libs/x64/ZDSRAPI.ini DESTINATION lib)
else()
  install (FILES libs/x86/dolapi32.dll libs/x86/nvdaControllerClient32.dll libs/x86/SAAPI32.dll DESTINATION lib)
  install (FILES libs/x86/BoyCtrl.dll DESTINATION lib)
  install (FILES libs/x86/ZDSRAPI.dll libs/x64/ZDSRAPI.ini DESTINATION lib)
endif()
