using BigRedCloud.Api.Samples.CashReceipt;
using BigRedCloud.Api.Samples.SalesInvoice;
using System;
using System.IO;

namespace BigRedCloud.Api.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            using (StreamWriter tracer = new StreamWriter(Console.OpenStandardOutput()))
            {
                tracer.AutoFlush = true;
                
                SalesInvoiceSample salesInvoiceSample = new SalesInvoiceSample(tracer);
                salesInvoiceSample.RunSample();

                CashReceiptSample cashReceiptSample = new CashReceiptSample(tracer);
                cashReceiptSample.RunSample();
            }
        }
    }
}
