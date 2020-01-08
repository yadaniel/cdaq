#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <stddef.h>

#define __linux__

#include "nisyscfg.h"
#include "nisyscfg_errors.h"
#include "nisyscfg_wide.h"
#include "NIDAQmx.h"

#define DAQmxErrChk(functionCall) if( DAQmxFailed(error=(functionCall)) ) goto Error; else

#define DAQ_CARD "cDAQ9188-187E8E4Mod3"

void test1() {
    int32       error=0;
    TaskHandle  taskHandle=0;
    char        errBuff[2048]= {'\0'};

    /*********************************************/
    // DAQmx Configure Code
    /*********************************************/
    DAQmxErrChk (DAQmxCreateTask("",&taskHandle));
    DAQmxErrChk (DAQmxCreateDOChan(taskHandle, DAQ_CARD "/port0/line0","",DAQmx_Val_ChanForAllLines));

    /*********************************************/
    // DAQmx Start Code
    /*********************************************/
    DAQmxErrChk (DAQmxStartTask(taskHandle));

    /*********************************************/
    // DAQmx Write Code
    /*********************************************/
    DAQmxErrChk (DAQmxWriteDigitalScalarU32(taskHandle,1,10.0,1,NULL));

Error:
    if( DAQmxFailed(error) ) {
        DAQmxGetExtendedErrorInfo(errBuff,2048);
    }
    if( taskHandle!=0 ) {
        /*********************************************/
        // DAQmx Stop Code
        /*********************************************/
        DAQmxStopTask(taskHandle);
        DAQmxClearTask(taskHandle);
    }
    if( DAQmxFailed(error) ) {
        printf("DAQmx Error: %s\n",errBuff);
    }

}


void test2() {
    int32       error=0;
    TaskHandle  taskHandle=0;
    uInt8       data[4]= {1,1,1,1};
    char        errBuff[2048]= {'\0'};

    /*********************************************/
    // DAQmx Configure Code
    /*********************************************/
    DAQmxErrChk (DAQmxCreateTask("",&taskHandle));
    DAQmxErrChk (DAQmxCreateDOChan(taskHandle, DAQ_CARD "/port0/line0:3","",DAQmx_Val_ChanForAllLines));

    /*********************************************/
    // DAQmx Start Code
    /*********************************************/
    DAQmxErrChk (DAQmxStartTask(taskHandle));

    /*********************************************/
    // DAQmx Write Code
    /*********************************************/
    DAQmxErrChk (DAQmxWriteDigitalLines(taskHandle,1,1,10.0,DAQmx_Val_GroupByChannel,data,NULL,NULL));

Error:
    if( DAQmxFailed(error) ) {
        DAQmxGetExtendedErrorInfo(errBuff,2048);
    }
    if( taskHandle!=0 ) {
        /*********************************************/
        // DAQmx Stop Code
        /*********************************************/
        DAQmxStopTask(taskHandle);
        DAQmxClearTask(taskHandle);
    }
    if( DAQmxFailed(error) ) {
        printf("DAQmx Error: %s\n",errBuff);
    }
}

int main() {
    /* test1(); */
    test2();

    printf("End of program, press Enter key to quit\n");
    getchar();
    return 0;
}

