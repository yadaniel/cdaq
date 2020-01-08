#!/usr/bin/env make

#
# cygwin
#
# CC = i686-w64-mingw32-gcc
# i686-w64-mingw32-gcc -m64 -I include main.c NIDAQmx.lib
# cc1: nicht implementiert: 64-Bit-Modus nicht einkompiliert

# CC = "i686-pc-cygwin-gcc.exe"
# $ i686-pc-cygwin-gcc -m64 -I ./include main.c -o main
# cc1: nicht implementiert: 64-Bit-Modus nicht einkompiliert

#
# msys2
CC = "/usr/bin/gcc"
# COLLECT_LTO_WRAPPER=/usr/lib/gcc/x86_64-pc-msys/7.4.0/lto-wrapper.exe
# COLLECT_GCC_OPTIONS='--version' '-v' '-mtune=generic' '-march=x86-64'

# 1) install daqmx drivers from national instruments 
# 2) copy nicaiu.dll from C:/windows/system32 to the local directory
# 3) chmod 644 nicaiu.dll
# gcc -m64 -I include main.c NIDAQmx.lib -Wl,nicaiu.dll -o main
# also working
# gcc -m64 -I include main.c -Wl,nicaiu.dll -o main

CFLAGS = -I ./include
LDFLAGS = -Wl,nicaiu.dll

.PHONY: prog clean

prog: main.c
	$(CC) $(CFLAGS) $(LDFLAGS) main.c -o main.exe

clean:
	@echo "cleaning up ..."
	rm -rf main.exe

