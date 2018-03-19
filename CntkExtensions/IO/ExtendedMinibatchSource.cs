using System;
using System.Collections.Generic;
using System.Linq;
using CntkExtensions.IO.Deserialization;
using CNTK;

namespace CntkExtensions.IO
{
    public class ExtendedMinibatchSource
    {
        private readonly IDeserializer _deserializer;
        private readonly Dictionary<string, StreamInformation> _streamInfos;
        private int _remainingLoadedSamples;
        private Dictionary<StreamInformation, Queue<float[]>> _loadedSamples;
        private int _numberOfChunks;
        private int[] _chunksRandomOrder;
        private int _currentChunkId;
        private bool _randomize;
        private bool _repeatInfinitely;

        public ExtendedMinibatchSource(IDeserializer deserializer, bool randomize = true, bool repeatInfinitely = false)
        {
            _deserializer = deserializer;
            _streamInfos = _deserializer.StreamInfos;
            _numberOfChunks = _deserializer.NumChunks;
            _chunksRandomOrder = GenerateRandomPermutation(_numberOfChunks);
            _currentChunkId = 0;

            _remainingLoadedSamples = 0;
            _loadedSamples = new Dictionary<StreamInformation, Queue<float[]>>(_streamInfos.Count);

            _randomize = randomize;
            _repeatInfinitely = repeatInfinitely;
        }

        public UnorderedMapStreamInformationMinibatchData GetNextMinibatch(uint minibatchSizeInSamples,
            DeviceDescriptor device)
        {
            LoadNextSamplesIfNeeded(minibatchSizeInSamples);

            // UnorderedMap <- MinibatchData <- Value <- IEnumerable<float[]>
            var minibatch = new UnorderedMapStreamInformationMinibatchData();

            foreach (var streamInfo in _streamInfos.Values)
            {
                var samples = new List<float[]>((int)minibatchSizeInSamples);
                for (var i = 0; i < minibatchSizeInSamples; ++i)
                {
                    var sample = _loadedSamples[streamInfo].Dequeue();
                    samples.Add(sample);
                }

                var minibatchValue = Value.CreateBatchOfSequences<float>(streamInfo.m_sampleLayout, samples, device);
                var minibatchData = new MinibatchData(minibatchValue, (uint) samples.Count);
                minibatch.Add(streamInfo, minibatchData);
            }

            return minibatch;
        }

        public bool HasNextMinibatch()
        {
            return _currentChunkId != -1;
        }

        private void LoadNextSamplesIfNeeded(uint requiredNumberOfSamples)
        {
            while (_remainingLoadedSamples < requiredNumberOfSamples)
                LoadNextChunk();
        }

        private void LoadNextChunk()
        {
            var nextChunkId = NextChunkId();
            var nextChunk = _deserializer.GetChunk(nextChunkId);
            var numSamplesInChunk = nextChunk.Values.First().Count();
            foreach (var sample in nextChunk)
            {
                foreach (var singleSample in sample.Value)
                {
                    _loadedSamples[sample.Key].Enqueue(singleSample);
                    _remainingLoadedSamples++;
                }   
            }
        }

        private int NextChunkId()
        {
            if (_currentChunkId < _chunksRandomOrder.Length)
                return _chunksRandomOrder[_currentChunkId++];

            if (_repeatInfinitely)
            {
                // Restart
                _currentChunkId = 0;
                _chunksRandomOrder = GenerateRandomPermutation(_numberOfChunks);
            }
            else
            {
                _currentChunkId = -1;
            }

            return _currentChunkId;
        }

        private int[] GenerateRandomPermutation(int length)
        {
            var random = new Random(DateTime.Now.Millisecond);
            var range = Enumerable.Range(0, length);

            return _randomize
                ? range
                    .OrderBy(_ => random.Next())
                    .ToArray()
                : range
                    .ToArray();
        }
    }
}
