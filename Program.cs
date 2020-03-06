using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using NAudio.Lame;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;

namespace Mp3FadeInFadeOut
{
    class Program
    {
        private const long fadeTimeInMs = 2000;
        
        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            
            const string infile = @"../../../example.mp3";
            const string outfile = @"../../../converted.wav";
            const string outfile2 = @"../../../converted2.wav";


            
            sw.Start();

            Console.WriteLine($"builder initialising {sw.Elapsed}");
            var builder = new Mp3FileReader.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf));
            Console.WriteLine($"builder finished {sw.Elapsed}");

            Console.WriteLine($"reader initialising {sw.Elapsed}");
            using var reader = new Mp3FileReader(infile, builder);
            Console.WriteLine($"reader finished {sw.Elapsed}");

            Console.WriteLine($"sampleProvider initialising {sw.Elapsed}");
            var sampleProvider = reader.ToSampleProvider();
            Console.WriteLine($"sampleProvider finished {sw.Elapsed}");

            Console.WriteLine($"readerSkipped initialising {sw.Elapsed}");
            var readerSkipped = sampleProvider.Skip(reader.TotalTime - TimeSpan.FromMilliseconds(fadeTimeInMs));
            Console.WriteLine($"readerSkipped finished {sw.Elapsed}");
            
            Console.WriteLine($"FadeInOutSampleProvider initialising {sw.Elapsed}");
            var fade = new FadeInOutSampleProvider(readerSkipped, true);
            Console.WriteLine($"FadeInOutSampleProvider finished {sw.Elapsed}");

            Console.WriteLine($"BeginFadeOut initialising {sw.Elapsed}");
            fade.BeginFadeOut(fadeTimeInMs);
            Console.WriteLine($"BeginFadeOut finished {sw.Elapsed}");

            Console.WriteLine($"fadeWaveProvider initialising {sw.Elapsed}");
            var fadeWaveProvider = fade.ToWaveProvider();
            Console.WriteLine($"fadeWaveProvider finished {sw.Elapsed}");

            
            Console.WriteLine($"CreateWaveFile initialising {sw.Elapsed}");
            WaveFileWriter.CreateWaveFile(outfile, fadeWaveProvider);
            Console.WriteLine($"CreateWaveFile finished {sw.Elapsed}");

            
            var ms = new MemoryStream();
            
            WaveFileWriter.WriteWavFileToStream(ms, fade.ToWaveProvider());

            
            var rs = new RawSourceWaveStream(ms, fade.WaveFormat);
            
            
            using var lame = new LameMP3FileWriter(rs, fade.WaveFormat, LAMEPreset.V0);


            //using var lame = new LameMP3FileWriter(, fade.WaveFormat, LAMEPreset.V0);

            //WaveFileWriter.CreateWaveFile(outfile,reader);
        }
    }
}