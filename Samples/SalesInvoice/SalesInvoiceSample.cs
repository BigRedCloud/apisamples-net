using System.IO;

namespace BigRedCloud.Api.Samples.SalesInvoice
{
    internal class SalesInvoiceSample : SalesInvoiceBaseSample
    {
        private readonly SalesInvoicePositiveSample _positiveSample;
        private readonly SalesInvoiceNegativeSample _negativeSample;

        public SalesInvoiceSample(StreamWriter tracer) : base(tracer)
        {
            _positiveSample = new SalesInvoicePositiveSample(tracer);
            _negativeSample = new SalesInvoiceNegativeSample(tracer);
        }

        public void RunSample()
        {
            Tracer.WriteLine("********************* Begin Sales Invoice Sample *********************");
            Tracer.WriteLine();

            PrepareEnvironment();
            _positiveSample.RunSample();
            _negativeSample.RunSample();

            Tracer.WriteLine("********************* End Sales Invoice Sample *********************");
            Tracer.WriteLine();
        }
    }
}
