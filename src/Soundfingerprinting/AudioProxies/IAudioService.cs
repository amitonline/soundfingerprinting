﻿namespace Soundfingerprinting.AudioProxies
{
    using System;

    public interface IAudioService : IDisposable
    {
        /// <summary>
        ///   Read audio from file at a specific frequency rate
        /// </summary>
        /// <param name = "fileName">Filename to read from</param>
        /// <param name = "sampleRate">Sample rate</param>
        /// <param name = "milliSeconds">Milliseconds to read</param>
        /// <param name = "startMilliSeconds">Start at a specific millisecond</param>
        /// <returns>Array with data samples</returns>
        float[] ReadMonoFromFile(string fileName, int sampleRate, int milliSeconds, int startMilliSeconds);
    }
}