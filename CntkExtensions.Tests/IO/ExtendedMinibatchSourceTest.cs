using System.Collections.Generic;
using System.Linq;
using CntkExtensions.IO;
using CntkExtensions.IO.Deserialization;
using CNTK;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CntkExtensions.Tests.IO
{
    [TestClass]
    public class ExtendedMinibatchSourceTest
    {
        private Mock<IDeserializer> _simpleDeserializer;
        private Dictionary<StreamInformation, IEnumerable<float[]>> _simpleDeserializerChunk;

        [TestInitialize]
        public void Initialize()
        {
            CreateSimpleDeserializer();
        }

        [TestMethod]
        public void ItShouldReturnSamplesInTheSameOrderFromDeserializer()
        {
            // given
            var minibatchSource = new ExtendedMinibatchSource(_simpleDeserializer.Object, false, true);

            // when
            var trueNumberOfSamples = 2;
            var nextMinibatch = minibatchSource.GetNextMinibatch((uint) trueNumberOfSamples, DeviceDescriptor.CPUDevice);
            var labelsVal = CNTKLib.InputVariable(minibatchSource.StreamInfos["labels"].m_sampleLayout, DataType.Float);
            var featuresVal = CNTKLib.InputVariable(minibatchSource.StreamInfos["features"].m_sampleLayout, DataType.Float);
            var labelsMinibatchAsDenseArray = nextMinibatch[minibatchSource.StreamInfos["labels"]].data.GetDenseData<float>(labelsVal);
            var featuresMinibatchAsDenseArray = nextMinibatch[minibatchSource.StreamInfos["features"]].data.GetDenseData<float>(featuresVal);
            var numSamples = nextMinibatch.Values.First().numberOfSamples;
            var trueLabels = new[] {new[] {1f, 0f}, new[] {0f, 1f}};
            var trueFeatures = new[] {new[] {0f, 0f}, new[] {1f, 1f}};

            // then
            // 
            Assert.AreEqual(trueNumberOfSamples, (int) numSamples);
            Assert.AreEqual(trueNumberOfSamples, labelsMinibatchAsDenseArray.Count);
            Assert.AreEqual(trueNumberOfSamples, featuresMinibatchAsDenseArray.Count);
            for (var i = 0; i < trueNumberOfSamples; ++i)
            {
                CollectionAssert.AreEqual(trueLabels[i].ToArray(), labelsMinibatchAsDenseArray[i].ToArray());
                CollectionAssert.AreEqual(trueFeatures[i].ToArray(), featuresMinibatchAsDenseArray[i].ToArray());
            }
        }

        [TestMethod]
        public void ItShouldReturnSamplesInTheSameOrderTwiceFromDeserializer()
        {
            // given
            var minibatchSource = new ExtendedMinibatchSource(_simpleDeserializer.Object, false, true);

            // when
            var trueNumberOfSamples = 4;
            var nextMinibatch = minibatchSource.GetNextMinibatch((uint)trueNumberOfSamples, DeviceDescriptor.CPUDevice);
            var labelsVal = CNTKLib.InputVariable(minibatchSource.StreamInfos["labels"].m_sampleLayout, DataType.Float);
            var featuresVal = CNTKLib.InputVariable(minibatchSource.StreamInfos["features"].m_sampleLayout, DataType.Float);
            var labelsMinibatchAsDenseArray = nextMinibatch[minibatchSource.StreamInfos["labels"]].data.GetDenseData<float>(labelsVal);
            var featuresMinibatchAsDenseArray = nextMinibatch[minibatchSource.StreamInfos["features"]].data.GetDenseData<float>(featuresVal);
            var numSamples = nextMinibatch.Values.First().numberOfSamples;
            var trueLabels = new[] { new[] { 1f, 0f }, new[] { 0f, 1f }, new[] { 1f, 0f }, new[] { 0f, 1f } };
            var trueFeatures = new[] { new[] { 0f, 0f }, new[] { 1f, 1f }, new[] { 0f, 0f }, new[] { 1f, 1f } };

            // then
            // 
            Assert.AreEqual(trueNumberOfSamples, (int)numSamples);
            Assert.AreEqual(trueNumberOfSamples, labelsMinibatchAsDenseArray.Count);
            Assert.AreEqual(trueNumberOfSamples, featuresMinibatchAsDenseArray.Count);
            for (var i = 0; i < trueNumberOfSamples; ++i)
            {
                CollectionAssert.AreEqual(trueLabels[i].ToArray(), labelsMinibatchAsDenseArray[i].ToArray());
                CollectionAssert.AreEqual(trueFeatures[i].ToArray(), featuresMinibatchAsDenseArray[i].ToArray());
            }
        }

        [TestMethod]
        public void ShouldThrowExceptionIfRequestingMoreSamplesWithoutRepetition()
        {
            // given
            var minibatchSource = new ExtendedMinibatchSource(_simpleDeserializer.Object, false, false);
            var anotherMinibatchSource = new ExtendedMinibatchSource(_simpleDeserializer.Object, false, false);

            // when
            var tooBigMinibatchSize = 4;
            var okayMinibatchSize = 2;
            // should not throw exception, but return only a sufficiently small chunk == length 2
            var nextMinibatch = minibatchSource.GetNextMinibatch((uint)tooBigMinibatchSize, DeviceDescriptor.CPUDevice);
            var numSamples = nextMinibatch.Values.First().numberOfSamples;

            // then
            Assert.AreEqual(2, (int) numSamples);
            anotherMinibatchSource.GetNextMinibatch((uint) okayMinibatchSize, DeviceDescriptor.CPUDevice);
            Assert.ThrowsException<NoMoreMinibatchesException>(() =>
            {
                anotherMinibatchSource.GetNextMinibatch((uint)okayMinibatchSize, DeviceDescriptor.CPUDevice);
            });
        }

        private void CreateSimpleDeserializer()
        {
            var streamInfos = new Dictionary<string, StreamInformation>
            {
                {"labels", new ExtendedStreamInformation(
                    "labels", 
                    0, 
                    StorageFormat.Dense, 
                    DataType.Float, 
                    NDShape.CreateNDShape(new []{2}), 
                    true)},
                {"features", new ExtendedStreamInformation(
                    "features",
                    1,
                    StorageFormat.Dense,
                    DataType.Float,
                    NDShape.CreateNDShape(new []{2, 1}), 
                    false)}
            };
            _simpleDeserializerChunk = new Dictionary<StreamInformation, IEnumerable<float[]>>
            {
                {streamInfos["labels"], new[] {new float[] {1, 0}, new float[] {0, 1}}},
                {streamInfos["features"], new[] {new float[] {0, 0}, new float[] {1, 1}}}
            };
            var deserializer = new Mock<IDeserializer>();
            deserializer.Setup(_ => _.StreamInfos).Returns(streamInfos);
            deserializer.Setup(_ => _.NumChunks).Returns(1);
            deserializer.Setup(_ => _.GetChunk(0)).Returns(_simpleDeserializerChunk);

            _simpleDeserializer = deserializer;
        }
    }
}
