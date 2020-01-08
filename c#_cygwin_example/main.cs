using System;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using NationalInstruments;
using NationalInstruments.DAQmx;
using System.Diagnostics;
using System.IO.Ports;
using CommandLine;
using CommandLine.Text;
using System.Reflection;

/**
 *
* ./main.exe --cDAQ9188 cDAQ9188-187E8E4
*
*/

public class App {

    public cDAQ9188 cDAQ;
    public cDAQ9263 ao;
    public cDAQ9221 ai;
    public cDAQ9482 ro;
    public string serialNumber = string.Empty;
    public string outputFile = string.Empty;
    public bool silent = false;

    static void Main(string[] args) {
        var app = new App();
        try {
            var commandLineOptions = new CommandLineOptions();
            if(CommandLine.Parser.Default.ParseArguments(args, commandLineOptions)) {
                if(commandLineOptions.Usage) {
                    app.usage();
                }
                if(commandLineOptions.SerialNumber == "") {
                    app.usage();
                } else {
                    app.serialNumber = commandLineOptions.SerialNumber;
                }
                if(commandLineOptions.OutputFile == "") {
                    app.outputFile = DateTime.Now.ToString("yyyy_MM_dd__HH_mm_ss_fff__") + "kv1kv2ma_dxm.log";
                }
                if(commandLineOptions.Silent) {
                    app.silent = true;
                }
            }
            app.cDAQ = new cDAQ9188(commandLineOptions.SerialNumber);
            app.ao = new cDAQ9263(commandLineOptions.SerialNumber + "Mod1");
            app.ai = new cDAQ9221(commandLineOptions.SerialNumber + "Mod2");
            app.ro = new cDAQ9482(commandLineOptions.SerialNumber + "Mod3");
            app.cDAQ[0] = app.ai;
            app.cDAQ.reset();
            app.run();
        } catch(DaqException ex) {
            Console.WriteLine("DAQ exception {0}", ex.Message);
        } catch(Exception ex) {
            Console.WriteLine("exception {0}", ex.Message);
        }
    }

    public void usage() {
        Console.WriteLine("usage: main --cDAQ9188 <serial number>");
        Console.WriteLine("e.g. main --cDAQ9188 cDAQ9188-187E8E4");
        Console.WriteLine("following NI racks are found on the network");
        DaqSys.showDevices();
        Environment.Exit(1);
    }

    public void run() {

        // out
        int FIL_PREHEAT_CH = 0;
        int FIL_LIMIT_CH = 0;
        int KV_SET_CH = 2;
        int MA_SET_CH = 3;

        // ramp up

        // disable DXM interlock
        ro.setPort(0x00);
        Thread.Sleep(1000);

        // set fil.preheat
        // ao.setChannel(0, 10);
        // Thread.Sleep(1000);

        // set fil.limit
        // ao.setChannel(1, 10);
        // Thread.Sleep(1000);

        // set fil.preheat and fil.limit
        //
        ao.setChannel(FIL_PREHEAT_CH, 10);
        Thread.Sleep(1000);

        // set kv
        ao.setChannel(KV_SET_CH, 10);
        Thread.Sleep(1000);

        // set ma
        ao.setChannel(MA_SET_CH, 0);
        Thread.Sleep(1000);

        // enable DXM interlock
        ro.setPort(0x01);
        Thread.Sleep(1000);

        // sample and show the voltages
        Task task = new Task();
        string devFullname = string.Format("{0}Mod2/ai0:2", serialNumber);
        Console.WriteLine(devFullname);
        AIChannel channel = task.AIChannels.CreateVoltageChannel(devFullname, "", AITerminalConfiguration.Rse, 0.0, 10.0, AIVoltageUnits.Volts);
        AnalogMultiChannelReader reader = new AnalogMultiChannelReader(task.Stream);
        task.Timing.ConfigureSampleClock("", rate:100, activeEdge:SampleClockActiveEdge.Rising, sampleMode:SampleQuantityMode.ContinuousSamples, samplesPerChannel:1000);
        task.Control(TaskAction.Verify);
        //var sw = new Stopwatch(); // debug
        for(uint i=0; i<1000; i++) {
            //sw.Reset();
            //sw.Start();
            double[] vals = reader.ReadSingleSample();
            //Console.WriteLine("data length {0}, stopwatch elapsed {1}", vals.GetLength(0), sw.Elapsed);
            //sw.Stop();
            string kVmon1 = (vals[0]/10.0*50).ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8);
            string kVmon2 = (vals[1]/10.0*50).ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8);
            string mAmon = (vals[2]/10.0*17).ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8);
            DateTime timestamp = DateTime.UtcNow;
            int unixTimestamp = (int)(timestamp.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string ms = timestamp.Millisecond.ToString("000");
            string line = string.Format("{0}_{1}: {2}, {3}, {4}", unixTimestamp, ms, kVmon1, kVmon2, mAmon);
            if(silent == false) {
                Console.WriteLine(line);
            }
            if(outputFile != string.Empty) {
                System.IO.File.AppendAllText(outputFile, line + Environment.NewLine);
            }
        }

        // ramp down
        //
        // set ma
        ao.setChannel(MA_SET_CH, 0);
        Thread.Sleep(1000);
        //
        // set kv
        ao.setChannel(KV_SET_CH, 0);
        Thread.Sleep(1000);

        for(uint i=0; i<500; i++) {
            //sw.Reset();
            //sw.Start();
            double[] vals = reader.ReadSingleSample();
            //Console.WriteLine("data length {0}, stopwatch elapsed {1}", vals.GetLength(0), sw.Elapsed);
            //sw.Stop();
            string kVmon1 = (vals[0]/10.0*50).ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8);
            string kVmon2 = (vals[1]/10.0*50).ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8);
            string mAmon = (vals[2]/10.0*17).ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8);
            DateTime timestamp = DateTime.UtcNow;
            int unixTimestamp = (int)(timestamp.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string ms = timestamp.Millisecond.ToString("000");
            string line = string.Format("{0}_{1}: {2}, {3}, {4}", unixTimestamp, ms, kVmon1, kVmon2, mAmon);
            if(silent == false) {
                Console.WriteLine(line);
            }
            if(outputFile != string.Empty) {
                System.IO.File.AppendAllText(outputFile, line + Environment.NewLine);
            }
        }

        //
        // disable DXM interlock
        ro.setPort(0x00);
        Thread.Sleep(1000);
    }

    // public void adcLoop() {
    //     var sw = new Stopwatch();
    //     while(true) {
    //         sw.Reset();
    //         sw.Start();
    //         double[,] vals = ai.getChannelsValues(0, 2, 1600);  // 800kS/s => record 2 seconds
    //         sw.Stop();
    //         Console.WriteLine("data {0}x{1}, stopwatch elapsed {2}", vals.GetLength(0), vals.GetLength(1), sw.Elapsed);
    //         for(int idx=0; idx<vals.GetLength(1); idx++) {
    //             string kVmon1 = (vals[0,idx]/10.0*50).ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8);
    //             string kVmon2 = (vals[1,idx]/10.0*50).ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8);
    //             string mAmon = (vals[2,idx]/10.0*17).ToString("0.000", CultureInfo.InvariantCulture).PadLeft(8);
    //             int unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
    //             string line = string.Format("{0}:{1:0000}: {2}, {3}, {4}", unixTimestamp, idx, kVmon1, kVmon2, mAmon);
    //             Console.WriteLine(line);
    //             if(outputFile != string.Empty) {
    //                 System.IO.File.AppendAllText(outputFile, line + Environment.NewLine);
    //             }
    //         }
    //     }
    // }

}

class CommandLineOptions {
    [Option('0', "usage", DefaultValue = false, HelpText = "show usage")]
    public bool Usage {
        get;
        set;
    }

    [Option('1', "o", DefaultValue = "", HelpText = "output file")]
    public string OutputFile {
        get;
        set;
    }

    [Option('2', "cDAQ9188", DefaultValue = "", HelpText = "serial number of cDAQ9188")]
    public string SerialNumber {
        get;
        set;
    }

    [Option('3', "s", DefaultValue = false, HelpText = "silent")]
    public bool Silent {
        get;
        set;
    }

    [ParserState]
    public IParserState LastParserState {
        get;
        set;
    }

    [HelpOption]
    public string GetUsage() {
        return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
    }
}
