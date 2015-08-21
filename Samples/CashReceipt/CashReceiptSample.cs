using System;
using System.IO;

namespace BigRedCloud.Api.Samples.CashReceipt
{
    internal class CashReceiptSample : CashReceiptBaseSample
    {
        private readonly CashReceiptPositiveSample _positiveSample;
        private readonly CashReceiptNegativeSample _negativeSample;

        public CashReceiptSample(DateTime financialYearStart, StreamWriter tracer)
            : base(financialYearStart, tracer)
        {
            _positiveSample = new CashReceiptPositiveSample(financialYearStart, tracer);
            _negativeSample = new CashReceiptNegativeSample(financialYearStart, tracer);
        }

        public void RunSample()
        {
            Tracer.WriteLine("********************* Begin Cash Receipt Sample *********************");
            Tracer.WriteLine();

            PrepareEnvironment();
            _positiveSample.RunSample();
            _negativeSample.RunSample();

            Tracer.WriteLine("********************* End Cash Receipt Sample *********************");
            Tracer.WriteLine();
        }
    }
}
