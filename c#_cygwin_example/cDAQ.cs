using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using NationalInstruments;
using NationalInstruments.DAQmx;
using CommandLine;
using CommandLine.Text;
using System.Reflection;

[assembly:AssemblyVersionAttribute("1.0.0.0")]
[assembly:AssemblyInformationalVersion("cDAQ_framework")]
[assembly:AssemblyCopyrightAttribute("MIT")]

public static class DaqSys {
    public static void showDevices() {
        DaqSystem daqsys = NationalInstruments.DAQmx.DaqSystem.Local;
        foreach(var device in daqsys.Devices) {
            Console.WriteLine("{0}", device.ToString());
        }
    }
}

public class DaqChassis {
    public string name {
        get;
        set;
    }
    public uint numSlots {
        get ;
        private set;
    }
    public DaqChassis(uint numSlots, string name) {
        this.name = name;
        this.numSlots = numSlots;
        this.cards = new DaqCard[8];
    }
    private DaqCard[] cards;
    public DaqCard this[uint slot] {
        get {
            if(slot < numSlots)
                return cards[slot];
            return null;
        } set {
            if(slot < numSlots)
                cards[slot] = value;
        }
    }
    public void reset() {
        for(int slot = 0; slot < numSlots; slot++) {
            if(cards[slot] != null)
                cards[slot].reset();
        }
    }
}

public class cDAQ9184 : DaqChassis {
    // Ethernet chassis
    public cDAQ9184(string name) : base(4, name) {}
}

public class cDAQ9178 : DaqChassis {
    // USB chassis
    public cDAQ9178(string name) : base(8, name) {}
}

public class cDAQ9188 : DaqChassis {
    // Ethernet chassis
    public cDAQ9188(string name) : base(8, name) {
    }
    public void AddReserveDevice(string ip, string name, uint timeout) {
        DaqSystem daqsys = NationalInstruments.DAQmx.DaqSystem.Local;
        Device device = daqsys.AddNetworkDevice(ip, name, timeout);
        device.ReserveNetworkDevice();
    }
}

public class DaqCard {
    private DaqSystem cdaq = NationalInstruments.DAQmx.DaqSystem.Local;
    public string name {
        get;
        set;
    }
    public DaqCard(string name) {
        this.name = name;
    }
    public void reset() {
        Device device = cdaq.LoadDevice(name);
        device.Reset();
    }
}

public class AICard : DaqCard {
    public AICard(string name) : base(name) {}
    public double getChannel(int line) {
        Task task = new Task();
        string devFullname = string.Format("{0}/ai{1}", name, line);
        AIChannel channel = task.AIChannels.CreateVoltageChannel(devFullname, "", AITerminalConfiguration.Rse, 0.0, 10.0, AIVoltageUnits.Volts);
        task.Start();
        AnalogMultiChannelReader reader = new AnalogMultiChannelReader(task.Stream);
        IAsyncResult result = reader.BeginReadSingleSample(null, null);
        double value = reader.EndReadSingleSample(result).ElementAt(0);
        task.Stop();
        return value;
    }
    public double[,] getChannelValues(int line) {
        Task task = new Task();
        string devFullname = string.Format("{0}/ai{1}", name, line);
        AIChannel channel = task.AIChannels.CreateVoltageChannel(devFullname, "", AITerminalConfiguration.Rse, 0.0, 10.0, AIVoltageUnits.Volts);
        task.Start();
        AnalogMultiChannelReader reader = new AnalogMultiChannelReader(task.Stream);
        IAsyncResult result = reader.BeginReadMultiSample(1000, null, null);
        double[,] values = reader.EndReadMultiSample(result);
        task.Stop();
        return values;
    }
    public double[,] getChannelsValues(int lineStart, int lineStop, int numOfSamples) {
        Task task = new Task();
        string devFullname = string.Format("{0}/ai{1}:{2}", name, lineStart, lineStop);
        AIChannel channel = task.AIChannels.CreateVoltageChannel(devFullname, "", AITerminalConfiguration.Rse, 0.0, 10.0, AIVoltageUnits.Volts);
        task.Start();
        AnalogMultiChannelReader reader = new AnalogMultiChannelReader(task.Stream);
        IAsyncResult result = reader.BeginReadMultiSample(numOfSamples, null, null);    // 2d array 2xN when lineStart=0, lineStop=1
        double[,] values = reader.EndReadMultiSample(result);   // 2d array 3xN when lineStart=0, lineStop=2
        task.Stop();
        return values;
    }
    public double[,] getChannelsValuesContinuous(int lineStart, int lineStop, int numOfSamples) {
        Task task = new Task();
        string devFullname = string.Format("{0}/ai{1}:{2}", name, lineStart, lineStop);
        AIChannel channel = task.AIChannels.CreateVoltageChannel(devFullname, "", AITerminalConfiguration.Rse, 0.0, 10.0, AIVoltageUnits.Volts);
        task.Start();
        AnalogMultiChannelReader reader = new AnalogMultiChannelReader(task.Stream);
        IAsyncResult result = reader.BeginReadMultiSample(numOfSamples, null, null);    // 2d array 2xN when lineStart=0, lineStop=1
        double[,] values = reader.EndReadMultiSample(result);   // 2d array 3xN when lineStart=0, lineStop=2
        task.Stop();
        return values;
    }
}

public class AOCard : DaqCard {
    public AOCard(string name) : base(name) {}
    public void setChannel(int line, double value) {
        Task task = new Task();
        string devFullname = string.Format("{0}/ao{1}", name, line);
        AOChannel channel = task.AOChannels.CreateVoltageChannel(devFullname, "", 0.0, 10.0, AOVoltageUnits.Volts);
        task.Start();
        AnalogMultiChannelWriter writer = new AnalogMultiChannelWriter(task.Stream);
        IAsyncResult result = writer.BeginWriteSingleSample(true, new double[] { value }, null, null);
        task.Stop();
    }
    public void setChannelValues(int line, double[] values) {
        Task task = new Task();
        string devFullname = string.Format("{0}/ao{1}", name, line);
        AOChannel channel = task.AOChannels.CreateVoltageChannel(devFullname, "", 0.0, 10.0, AOVoltageUnits.Volts);
        task.Timing.ConfigureSampleClock("", 1000, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, values.Length);
        task.Stream.WriteRegenerationMode = WriteRegenerationMode.AllowRegeneration;
        AnalogSingleChannelWriter writer = new AnalogSingleChannelWriter(task.Stream);
        writer.WriteMultiSample(false, values);
        task.Start();
        while(task.IsDone == false);
        task.Dispose();
    }
}

public class DICard : DaqCard {
    public DICard(string name) : base(name) {}
    public bool getLine(uint line) {
        Task task = new Task();
        string devFullname = string.Format("{0}/port0/line{1}", name, line);
        DIChannel channel = task.DIChannels.CreateChannel(devFullname, "", ChannelLineGrouping.OneChannelForEachLine);
        task.Start();
        DigitalMultiChannelReader reader = new DigitalMultiChannelReader(task.Stream);
        IAsyncResult result = reader.BeginReadSingleSamplePortUInt32(null, null);
        uint value = reader.EndReadSingleSamplePortUInt32(result).ElementAt(0);
        task.Stop();
        int shft = (int)line;
        return (value & (1u<<shft)) == (1u<<shft);
    }
    public uint getPort() {
        Task task = new Task();
        string devFullname = string.Format("{0}/port0/line0:31", name);
        DIChannel channel = task.DIChannels.CreateChannel(devFullname, "", ChannelLineGrouping.OneChannelForEachLine);
        task.Start();
        DigitalMultiChannelReader reader = new DigitalMultiChannelReader(task.Stream);
        IAsyncResult result = reader.BeginReadSingleSamplePortUInt32(null, null);
        uint value = 0;
        var bits = reader.EndReadSingleSamplePortUInt32(result);
        foreach(uint bit in bits) {
            value += bit;
        }
        task.Stop();
        return value;
    }
}

public class DOCard : DaqCard {
    public DOCard(string name) : base(name) {}
    public void setLine(uint line, uint value) {
        setLine(line, (value == 1u ? true : false));
    }
    public void setLine(uint line, bool value) {
        Task task = new Task();
        string devFullname = string.Format("{0}/port0/line{1}", name, line);
        DOChannel dochannel = task.DOChannels.CreateChannel(devFullname, "", ChannelLineGrouping.OneChannelForEachLine);
        task.Start();
        //DigitalMultiChannelWriter writer = new DigitalMultiChannelWriter(task.Stream);
        //writer.BeginWriteSingleSampleSingleLine(true, new bool[] {value}, null, null);
        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(task.Stream);
        writer.BeginWriteSingleSampleSingleLine(true, value, null, null);
        task.Stop();
    }
    public void setPort(uint value) {
        Task task = new Task();
        string devFullname = string.Format("{0}/port0/line0:31", name);
        //DOChannel dochannel = task.DOChannels.CreateChannel(devFullname, "", ChannelLineGrouping.OneChannelForEachLine);
        DOChannel dochannel = task.DOChannels.CreateChannel(devFullname, "", ChannelLineGrouping.OneChannelForAllLines);
        task.Start();
        //DigitalMultiChannelWriter writer = new DigitalMultiChannelWriter(task.Stream);
        //writer.BeginWriteSingleSamplePort(true, new uint[] {value&0x01, value&0x02, value&0x04, value&0x08}, null, null);
        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(task.Stream);
        writer.BeginWriteSingleSamplePort(true, value, null, null);
        task.Stop();
    }
}

public class RelCard : DaqCard {
    public RelCard(string name) : base(name) {}
    public void setLine(uint line, uint value) {
        setLine(line, (value == 1u ? true : false));
    }
    public void setLine(uint line, bool value) {
        Task task = new Task();
        string devFullname = string.Format("{0}/port0/line{1}", name, line);
        DOChannel dochannel = task.DOChannels.CreateChannel(devFullname, "", ChannelLineGrouping.OneChannelForEachLine);
        task.Start();
        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(task.Stream);
        writer.BeginWriteSingleSampleSingleLine(true, value, null, null);
        task.Stop();
    }
    public void setPort(uint value) {
        Task task = new Task();
        string devFullname = string.Format("{0}/port0/line0:3", name);
        DOChannel dochannel = task.DOChannels.CreateChannel(devFullname, "", ChannelLineGrouping.OneChannelForEachLine);
        task.Start();
        DigitalMultiChannelWriter writer = new DigitalMultiChannelWriter(task.Stream);
        writer.BeginWriteSingleSamplePort(true, new uint[] {value&0x01, value&0x02, value&0x04, value&0x08}, null, null);
        task.Stop();
    }
}

public class cDAQ9215 : AICard {
    public cDAQ9215(string name) : base(name) {}
}
public class cDAQ9221 : AICard {
    public cDAQ9221(string name) : base(name) {}
}
public class cDAQ9263 : AOCard {
    public cDAQ9263(string name) : base(name) {}
}
public class cDAQ9425 : DICard {
    public cDAQ9425(string name) : base(name) {}
}
public class cDAQ9421 : DICard {
    public cDAQ9421(string name) : base(name) {}
}
public class cDAQ9476 : DOCard {
    public cDAQ9476(string name) : base(name) {}
}
public class cDAQ9481 : RelCard {
    public cDAQ9481(string name) : base(name) {}
}
public class cDAQ9482 : RelCard {
    public cDAQ9482(string name) : base(name) {}
}

class cDAQ_UnitTests {

    static void Main(string[] args) {

        cDAQ_UnitTests test = new cDAQ_UnitTests();

        var commandLineOptions = new cDAQUnitTests_CommandLineOptions();
        if(CommandLine.Parser.Default.ParseArguments(args, commandLineOptions)) {
            if(commandLineOptions.RunTest1)
                test.test_cDAQ9178();
            if(commandLineOptions.RunTest2)
                test.test_cDAQ9188();
            if(commandLineOptions.RunTest3)
                test.test_cDAQ9184();
        }

    }

    public void test_cDAQ9184() {
        try {
            DaqSys.showDevices();
            cDAQ9184 cDAQ = new cDAQ9184("cDAQ9184-1BDE983");
            cDAQ9482 ro4 = new cDAQ9482("cDAQ9184-1BDE983Mod1");
            cDAQ9476 do32 = new cDAQ9476("cDAQ9184-1BDE983Mod2");
            cDAQ9425 di32 = new cDAQ9425("cDAQ9184-1BDE983Mod3");
            cDAQ[0] = ro4;
            cDAQ[1] = do32;
            cDAQ[2] = di32;
            cDAQ.reset();

            // test ro4
            for(uint i=0u; i<4u; i++) {
                //ro4.setLine(i, true);
                ro4.setLine(i, 1);
                Thread.Sleep(1000);
                //ro4.setLine(i, false);
                ro4.setLine(i, 0);
                Thread.Sleep(1000);
            }
            for(uint i=0; i<16; i++) {
                ro4.setPort(i);
                Thread.Sleep(1000);
            }

            // test do32/di32
            for(uint i=0; i<32; i++) {
                do32.setLine(i, 1);
                Thread.Sleep(10);
                bool r = di32.getLine(i);
                Console.WriteLine("written 1, read {0}, {1}", r?1:0, r?"passed":"failed");

                do32.setLine(i, 0);
                Thread.Sleep(10);
                r = di32.getLine(i);
                Console.WriteLine("written 0, read {0}, {1}", r?1:0, r?"failed":"passed");
            }

            for(uint i=0; i<32; i++) {
                do32.setLine(i, 1);
                Thread.Sleep(10);
                uint value = di32.getPort();
                Console.WriteLine("written {0:X8}, read {1:X8}, {2}", 1u<<((int)i), value, 1u<<((int)i) == value ? "passed" : "failed");

                do32.setLine(i, 0);
                Thread.Sleep(10);
                value = di32.getPort();
                Console.WriteLine("written 00000000, read {0:X8}, {1}", value, value == 0u ? "passed" : "failed");
            }

            // random pattern
            Random rand = new Random();
            for(int i=0; i<32; i++) {
                uint w = (uint)rand.Next();
                do32.setPort(w);
                Thread.Sleep(10);
                uint r = di32.getPort();
                Console.WriteLine("w = {0:X8}, r = {1:X8}, {2}", w, r, r==w?"passed":"failed");
            }
        } catch(DaqException ex) {
            Console.WriteLine("DAQ exception {0}", ex.Message);
        } catch(Exception ex) {
            Console.WriteLine("exception {0}", ex.Message);
        }
    }

    public void test_cDAQ9188() {
        try {
            DaqSys.showDevices();
            // 1AFEC54
            cDAQ9188 cDAQ = new cDAQ9188("cDAQ9188-1AFEC54");
            cDAQ9482 ro4 = new cDAQ9482("cDAQ9188-1AFEC54Mod1");
            cDAQ9476 do32 = new cDAQ9476("cDAQ9188-1AFEC54Mod2");
            cDAQ9425 di32 = new cDAQ9425("cDAQ9188-1AFEC54Mod3");
            cDAQ[0] = ro4;
            cDAQ[1] = do32;
            cDAQ[2] = di32;
            cDAQ.reset();
            // test do32/di32

            // bitwise
            for(uint i=0; i<32; i++) {
                do32.setLine(i, 1);
                Thread.Sleep(10);
                bool r = di32.getLine(i);
                Console.WriteLine("written 1, read {0}, {1}", r?1:0, r?"passed":"failed");

                do32.setLine(i, 0);
                Thread.Sleep(10);
                r = di32.getLine(i);
                Console.WriteLine("written 0, read {0}, {1}", r?1:0, r?"failed":"passed");
            }

            for(uint i=0; i<32; i++) {
                do32.setLine(i, 1);
                Thread.Sleep(10);
                uint value = di32.getPort();
                Console.WriteLine("written {0:X8}, read {1:X8}, {2}", 1u<<((int)i), value, 1u<<((int)i) == value ? "passed" : "failed");

                do32.setLine(i, 0);
                Thread.Sleep(10);
                value = di32.getPort();
                Console.WriteLine("written 00000000, read {0:X8}, {1}", value, value == 0u ? "passed" : "failed");
            }

            // random pattern
            Random rand = new Random();
            for(int i=0; i<32; i++) {
                uint w = (uint)rand.Next();
                do32.setPort(w);
                Thread.Sleep(10);
                uint r = di32.getPort();
                Console.WriteLine("w = {0:X8}, r = {1:X8}, {2}", w, r, r==w?"passed":"failed");
            }
        } catch(DaqException ex) {
            Console.WriteLine("DAQ exception {0}", ex.Message);
        } catch(Exception ex) {
            Console.WriteLine("exception {0}", ex.Message);
        }
    }

    public void _test_cDAQ9188() {
        try {
            DaqSys.showDevices();
            // 1AFEC54
            cDAQ9188 cDAQ = new cDAQ9188("cDAQ9188-1AFEC54");
            cDAQ9482 ro4 = new cDAQ9482("cDAQ9188-1AFEC54Mod1");
            cDAQ9476 do32 = new cDAQ9476("cDAQ9188-1AFEC54Mod2");
            cDAQ9425 di32 = new cDAQ9425("cDAQ9188-1AFEC54Mod3");
            cDAQ[0] = ro4;
            cDAQ[1] = do32;
            cDAQ[2] = di32;
            cDAQ.reset();
            // test ro4
            for(uint i=0u; i<4u; i++) {
                //ro4.setLine(i, true);
                ro4.setLine(i, 1);
                Thread.Sleep(1000);
                //ro4.setLine(i, false);
                ro4.setLine(i, 0);
                Thread.Sleep(1000);
            }
            for(uint i=0; i<16; i++) {
                ro4.setPort(i);
                Thread.Sleep(1000);
            }
            // test di32
            Console.WriteLine("di32.port = {0}", di32.getPort());
            for(uint i=0; i<32; i++) {
                Console.WriteLine("di32.line[{0}] = {1}", i, di32.getLine(i));
            }
            // test do32
            do32.setPort(0xFF);
            for(uint i=0; i<32; i++) {
                //do32.setLine(i, true);
                do32.setLine(i, 1);
            }
        } catch(DaqException ex) {
            Console.WriteLine("DAQ exception {0}", ex.Message);
        } catch(Exception ex) {
            Console.WriteLine("exception {0}", ex.Message);
        }
    }

    public void test_cDAQ9178() {
        try {
            DaqSys.showDevices();
            cDAQ9178 cDAQ = new cDAQ9178("cDAQ1");
            cDAQ9421 di8 = new cDAQ9421("cDAQ1Mod1");
            cDAQ9482 ro4_1 = new cDAQ9482("cDAQ1Mod2");
            cDAQ9482 ro4_2 = new cDAQ9482("cDAQ1Mod3");
            cDAQ9215 ai4 = new cDAQ9215("cDAQ1Mod4");
            cDAQ[0] = di8;
            cDAQ[1] = ro4_1;
            cDAQ[2] = ro4_2;
            cDAQ[3] = ai4;
            cDAQ.reset();
            for(int i=0; i<10; i++) {
                ro4_1.setLine(0, i%2 == 0);
                Thread.Sleep(1000);
            }
            for(uint i=0; i<16; i++) {
                ro4_1.setPort(i);
                Thread.Sleep(1000);
            }
        } catch(DaqException ex) {
            Console.WriteLine("DAQ exception {0}", ex.Message);
        } catch(Exception ex) {
            Console.WriteLine("exception {0}", ex.Message);
        }
    }
}

class cDAQUnitTests_CommandLineOptions {
    [Option('a', "cDAQ9178", DefaultValue = false, HelpText = "run test_cDAQ9178")]
    public bool RunTest1 {
        get;
        set;
    }

    [Option('b', "cDAQ9188", DefaultValue = false, HelpText = "run test_cDAQ9188")]
    public bool RunTest2 {
        get;
        set;
    }

    [Option('c', "cDAQ9184", DefaultValue = false, HelpText = "run test_cDAQ9184")]
    public bool RunTest3 {
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
