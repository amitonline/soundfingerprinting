﻿namespace SoundFingerprinting.Audio.NAudio
{
    using System;
    using System.Collections.Generic;

    using global::NAudio.Wave;
    using global::NAudio.Wave.SampleProviders;

    public class NAudioService : AudioService, IExtendedAudioService
    {
        private static readonly IReadOnlyCollection<string> NAudioSupportedFormats = new[] { ".mp3" };

        private IWavePlayer waveOutDevice;
        private WaveStream mainOutputStream;

        public bool IsRecordingSupported
        {
            get
            {
                return false;
            }
        }

        public override IReadOnlyCollection<string> SupportedFormats
        {
            get
            {
                return NAudioSupportedFormats;
            }
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
        
        public override float[] ReadMonoFromFile(string pathToFile, int sampleRate, int secondsToRead, int startAtSecond)
        {
            using (var reader = new MediaFoundationReader(pathToFile))
            {
                int actualSampleRate = reader.WaveFormat.SampleRate;
                int bitsPerSample = reader.WaveFormat.BitsPerSample;
                reader.Seek(actualSampleRate * bitsPerSample / 8 * startAtSecond, System.IO.SeekOrigin.Begin);
                using (var resampler = new MediaFoundationResampler(reader, WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1)))
                {
                    float[] buffer = new float[sampleRate * 20]; // 20 seconds buffer
                    List<float[]> chunks = new List<float[]>();
                    int totalFloatsToRead = secondsToRead == 0 ? int.MaxValue : secondsToRead * sampleRate;
                    int totalFloatsRead = 0;
                    Pcm32BitToSampleProvider pcmReader = new Pcm32BitToSampleProvider(resampler);
                    while (totalFloatsRead < totalFloatsToRead)
                    {
                        // get re-sampled/mono data
                        int floatsRead = pcmReader.Read(buffer, 0, buffer.Length);
                        if (floatsRead == 0)
                        {
                            break;
                        }

                        totalFloatsRead += floatsRead;

                        float[] chunk;

                        if (totalFloatsRead > totalFloatsToRead)
                        {
                            chunk = new float[(totalFloatsToRead - (totalFloatsRead - floatsRead))];
                            Array.Copy(buffer, chunk, totalFloatsToRead - (totalFloatsRead - floatsRead));
                        }
                        else
                        {
                            chunk = new float[floatsRead]; // each float contains 4 bytes
                            Array.Copy(buffer, chunk, floatsRead);
                        }

                        chunks.Add(chunk);
                    }

                    if (totalFloatsRead < (secondsToRead * sampleRate))
                    {
                        return null; /*not enough samples to return the requested data*/
                    }

                    float[] data = ConcatenateChunksOfSamples(chunks);

                    return data;
                }
            }
        }

        public int PlayFile(string filename)
        {
            waveOutDevice = new WaveOut();
            mainOutputStream = CreateInputStream(filename);
            waveOutDevice.Init(mainOutputStream);
            waveOutDevice.Play();
            return 0;
        }

        public void StopPlayingFile(int stream)
        {
            if (waveOutDevice != null)
            {
                waveOutDevice.Stop();
            }

            if (mainOutputStream != null)
            {
                mainOutputStream.Close();
                mainOutputStream = null;
            }

            if (waveOutDevice != null)
            {
                waveOutDevice.Dispose();
                waveOutDevice = null;
            }
        }

        public void RecodeFileToMonoWave(string pathToFile, string outputPathToFile, int targetSampleRate)
        {
            using (Mp3FileReader reader = new Mp3FileReader(pathToFile))
            {
                using (var resampler = new MediaFoundationResampler(reader, WaveFormat.CreateIeeeFloatWaveFormat(targetSampleRate, 1)))
                {
                    WaveFileWriter.CreateWaveFile(outputPathToFile, resampler);
                }
            }
        }

        public float[] ReadMonoFromURL(string urlToResource, int sampleRate, int secondsToDownload)
        {
            throw new NotImplementedException();
        }

        public float[] RecordFromMicrophoneToFile(string pathToFile, int sampleRate, int secondsToRecord)
        {
            throw new NotImplementedException();
        }

        private WaveStream CreateInputStream(string fileName)
        {
            WaveChannel32 inputStream;
            if (fileName.EndsWith(".mp3"))
            {
                WaveStream mp3Reader = new Mp3FileReader(fileName);
                inputStream = new WaveChannel32(mp3Reader);
            }
            else
            {
                throw new InvalidOperationException("Unsupported extension");
            }

            return inputStream;
        }
    }
}
