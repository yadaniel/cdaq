#!/usr/bin/env make

CS = "/cygdrive/c/Program Files (x86)/Mono/bin/csc"
#CS = /cygdrive/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe
#CS = /cygdrive/c/Windows/Microsoft.NET/Framework/v4.0.30319/csc.exe
#CS = /cygdrive/c/Windows/Microsoft.NET/Framework64/v3.5/csc.exe
#CS = /cygdrive/c/Windows/Microsoft.NET/Framework/v3.5/csc.exe

DAQMX = ./NationalInstruments.DAQmx.dll
COMMON = ./NationalInstruments.Common.dll
LIBS = ./CommandLine.dll

EXE: main.cs $(DAQMX) $(COMMON)
	@echo "compiling EXE ..."
	$(CS) /target:exe /main:App /platform:x86 /reference:$(LIBS) /reference:$(DAQMX) /linkresource:$(DAQMX) /reference:$(COMMON) /linkresource:$(COMMON) main.cs cDAQ.cs

DLL: main.cs $(DAQMX) $(COMMON)
	@echo "compiling DLL ..."
	$(CS) /target:library /reference:$(LIBS) /linkresource:$(DAQMX) /linkresource:$(COMMON) main.cs cDAQ.cs

clean:
	@echo "cleaning up ..."
	rm -rf main.exe main.dll
