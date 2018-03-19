using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CntkExtensions.IO;
using CNTK;

namespace CntkExtensions.Battleground.Examples
{
    class RetrieveDataFromMinibatch
    {
        public void RetrieveDataFromMb()
        {
            var minibatchSource = new ExtendedMinibatchSource(null, true, true);

            // this is wha minibatch source returns
            var nextMinibatch = new UnorderedMapStreamInformationMinibatchData();
            var labelSamplesShape = nextMinibatch[minibatchSource.StreamInfos["labels"]].data.Shape;
            var val = CNTKLib.InputVariable(labelSamplesShape, DataType.Float);

            // This is how you get data from minibatch stream as a C# array
            var arr = nextMinibatch[minibatchSource.StreamInfos["labels"]].data.GetDenseData<float>(val);
            var numSamples = nextMinibatch.Values.First().numberOfSamples;
        }
    }
}
