using System.Collections.Generic;
using CNTK;

namespace CntkExtensions.IO.Deserialization
{
    public interface IDeserializer
    {
        int NumChunks { get; }

        /// <summary>
        /// <para>string: stream name</para>
        /// <para>StreamInformation: stream itself</para>
        /// </summary>
        Dictionary<string, StreamInformation> StreamInfos { get; }

        /// <summary>
        /// <para>string: stream name</para>
        /// <para>StreamInformation: stream itself</para>
        /// {'stream_name': sequence_of_samples, 'stream_name_2": sequence_of_samples}
        /// </summary>
        Dictionary<StreamInformation, IEnumerable<float[]>> GetChunk(int chunkId);
    }
}
