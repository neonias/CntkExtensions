using System;

namespace CntkExtensions.IO
{
    public class NoMoreMinibatchesException : Exception
    {
        public NoMoreMinibatchesException()
            : base("There are no more minibatches (consider setting RepeatInfinitely to true)")
        {
        }
    }
}
