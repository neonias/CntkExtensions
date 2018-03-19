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
        private readonly Dictionary<StreamInformation, Queue<float[]>> _loadedSamples;
        private int[] _chunksRandomOrder;
        private int _currentChunkIdx;
        private readonly int _numberOfChunks;
        private readonly bool _randomize;
        private readonly bool _repeatInfinitely;

        public ExtendedMinibatchSource(IDeserializer deserializer, bool randomize = true, bool repeatInfinitely = false)
        {
            _randomize = randomize;
            _repeatInfinitely = repeatInfinitely;
            _deserializer = deserializer;
            _numberOfChunks = _deserializer.NumChunks;
            _loadedSamples = new Dictionary<StreamInformation, Queue<float[]>>(StreamInfos.Count);

            InitializeNextEpoch();
        }

        public UnorderedMapStreamInformationMinibatchData GetNextMinibatch(uint minibatchSizeInSamples,
            DeviceDescriptor device)
        {
            if(!HasNextMinibatch())
                throw new NoMoreMinibatchesException();

            LoadNextSamplesIfNeeded(minibatchSizeInSamples);

            // UnorderedMap <- MinibatchData <- Value <- IEnumerable<float[]>
            var minibatch = new UnorderedMapStreamInformationMinibatchData();
            var realMinibatchSize = (uint) Math.Min(minibatchSizeInSamples, RemainingLoadedSamples);
 
            foreach (var streamInfo in StreamInfos.Values)
            {
                var streamVectorSize = streamInfo.m_sampleLayout.TotalSize;
                var minibatchArray = new float[realMinibatchSize * streamVectorSize];
                for (var i = 0; i < realMinibatchSize; ++i)
                {
                    var sample = _loadedSamples[streamInfo].Dequeue();
                    sample.CopyTo(minibatchArray, streamVectorSize * i);
                }

                var minibatchValue = Value.CreateBatch(streamInfo.m_sampleLayout, minibatchArray, device);
                var minibatchData = new MinibatchData(minibatchValue, realMinibatchSize);
                minibatch.Add(streamInfo, minibatchData);
            }

            return minibatch;
        }

        public bool HasNextMinibatch()
        {
            return HasNextChunk();
        }

        public Dictionary<string, StreamInformation> StreamInfos => _deserializer.StreamInfos;

        private int RemainingLoadedSamples => _loadedSamples.First().Value.Count;

        private bool HasNextChunk()
        {
            return _currentChunkIdx != -1;
        }

        private void InitializeNextEpoch(bool clearLoadedSamples = true)
        {
            _chunksRandomOrder = GenerateRandomPermutation(_numberOfChunks);
            _currentChunkIdx = 0;

            if (clearLoadedSamples)
            {
                _loadedSamples.Clear();
                foreach (var streamInfo in StreamInfos)
                    _loadedSamples.Add(streamInfo.Value, new Queue<float[]>());
            }
        }

        private void LoadNextSamplesIfNeeded(uint requiredNumberOfSamples)
        {
            while (RemainingLoadedSamples < requiredNumberOfSamples)
            {
                LoadNextChunk();
                if (_currentChunkIdx == -1)
                    return;
            }
        }

        private void LoadNextChunk()
        {
            var nextChunkId = RequestNextChunkId();
            var nextChunk = _deserializer.GetChunk(nextChunkId);
            foreach (var sampleStreamAndFeatureVector in nextChunk)
            {
                foreach (var sampleFeatureVector in sampleStreamAndFeatureVector.Value)
                {
                    _loadedSamples[sampleStreamAndFeatureVector.Key].Enqueue(sampleFeatureVector);
                }   
            }
        }

        private int RequestNextChunkId()
        {
            int nextChunkId;

            if (_currentChunkIdx < _chunksRandomOrder.Length - 1)
            {
                nextChunkId = _chunksRandomOrder[_currentChunkIdx++];
            }
            else if (_repeatInfinitely)
            {
                InitializeNextEpoch(false);
                nextChunkId = _chunksRandomOrder[_currentChunkIdx++];
            }
            else
            {
                nextChunkId = _chunksRandomOrder[_currentChunkIdx];
                _currentChunkIdx = -1;
            }

            return nextChunkId;
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
