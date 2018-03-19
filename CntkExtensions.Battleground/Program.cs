using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using CNTK;

namespace CntkExtensions.Battleground
{
    class Program
    {
        static void Main(string[] args)
        {
            MinibatchSource source;
            var map = new UnorderedMapStreamInformationMinibatchData();
            map.Add(new StreamInformation(), new MinibatchData());
            Trainer t = null;
            source.GetNextMinibatch()
            Value.CreateBatchOfSequences<float>(new NDShape(new []{5, 5}), new )
        }
    }
}
