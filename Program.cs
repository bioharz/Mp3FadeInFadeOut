using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Lame;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;

namespace Mp3FadeInFadeOut
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            const long fadeTimeInMs = 2000;
            const string infile = @"../../../input.mp3";
            const string outfileAll = @"../../../output.mp3";
            const LAMEPreset lamePreset = LAMEPreset.ABR_128;
            var stream = new MemoryStream();

            /*
             * TODO: Reuse the same Mp3Reader (var reader = GetMp3FileReaderFromFilePath(infile)) for all steps to increase performance?
             * TODO: Use Constant bitrate instead of Average bitrate. Turn off run-length encoding (RLE)??? Reason: To remove the crackling noise at the transition phases.
             * TODO: Clean Code!
             */
            
            /*
             * A very trivial benchmark not very accurate.
             * TODO: Use more files, warm up phase for benchmarking...
             */
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // Fade in and converting back and forward
            await using (var reader = GetMp3FileReaderFromFilePath(infile))
            {
                var sampleProviderBegin = reader.ToSampleProvider().Take(TimeSpan.FromMilliseconds(fadeTimeInMs));
                var fadeBegin = new FadeInOutSampleProvider(sampleProviderBegin);
                fadeBegin.BeginFadeIn(fadeTimeInMs);
                await ConvertWaveProviderToMp3Stream(fadeBegin.ToWaveProvider(), stream, lamePreset);
            }

            // Mid part, just coping the the raw mp3 stream
            await using (var reader = GetMp3FileReaderFromFilePath(infile))
            {
                var beginMs = fadeTimeInMs;
                var endMs = reader.TotalTime.TotalMilliseconds - fadeTimeInMs;

                ConcatFromReaderToStream(reader, stream, beginMs, endMs);
            }

            // Fade out and converting back and forward
            await using (var reader = GetMp3FileReaderFromFilePath(infile))
            {
                var sampleProviderEnd = reader.ToSampleProvider().Skip(reader.TotalTime - TimeSpan.FromMilliseconds(fadeTimeInMs));
                var fadeEnd = new FadeInOutSampleProvider(sampleProviderEnd);
                fadeEnd.BeginFadeOut(fadeTimeInMs);
                await ConvertWaveProviderToMp3Stream(fadeEnd.ToWaveProvider(), stream, lamePreset);
            }

            // Write mp3 stream to file
            await using (var fileStream = File.Create(outfileAll))
            {
                stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(fileStream);
            }
            
            Console.WriteLine($"Consumed time {stopWatch.Elapsed}");

        }


        private static Mp3FileReader GetMp3FileReaderFromFilePath(string filePath)
        {
            var builder = new Mp3FileReader.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf));
            return new Mp3FileReader(filePath, builder);
        }


        private static async Task<Stream> ConvertWaveProviderToMp3Stream(IWaveProvider waveProvider, Stream outStream, LAMEPreset lamePreset, CancellationToken ctx = default)
        {
            var memoryStream = new MemoryStream();
            using var streamReader = new StreamReader(memoryStream);
            WaveFileWriter.WriteWavFileToStream(memoryStream, waveProvider);

            await using var lameMp3StreamWriter = new LameMP3FileWriter(outStream, waveProvider.WaveFormat, lamePreset);

            await streamReader.BaseStream.CopyToAsync(lameMp3StreamWriter, ctx);

            return outStream;
        }

        private static async Task ConvertWaveProviderToMp3File(IWaveProvider waveProvider, string outPutFile, LAMEPreset lamePreset, CancellationToken ctx = default)
        {
            await using var memoryStream = new MemoryStream();
            using var reader = new StreamReader(memoryStream);
            WaveFileWriter.WriteWavFileToStream(memoryStream, waveProvider);

            await using var lameMp3FileWriter = new LameMP3FileWriter(outPutFile, waveProvider.WaveFormat, lamePreset);
            await reader.BaseStream.CopyToAsync(lameMp3FileWriter, ctx);
        }

        private static void ConcatFromReaderToStream(Mp3FileReader reader, Stream stream, double beginMs, double endMs)
        {
            Mp3Frame frame;
            while ((frame = reader.ReadNextFrame()) != null)
            {
                var currentMs = reader.CurrentTime.TotalMilliseconds;
                if (currentMs >= beginMs && currentMs <= endMs)
                {
                    stream.Write(frame.RawData, 0, frame.RawData.Length);
                }
                else
                {
                    if (currentMs > endMs) break;
                }
            }
        }
    }
}