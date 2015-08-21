using BigRedCloud.Api.Samples.CashReceipt;
using BigRedCloud.Api.Samples.SalesInvoice;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;

namespace BigRedCloud.Api.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var financialYearStartStr = ConfigurationManager.AppSettings["FinancialYearStart"];
            var financialYearStart = DateTime.ParseExact(financialYearStartStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            using (StreamWriter tracer = new StreamWriter(Console.OpenStandardOutput()))
            {
                tracer.AutoFlush = true;

                SalesInvoiceSample salesInvoiceSample = new SalesInvoiceSample(financialYearStart, tracer);
                salesInvoiceSample.RunSample();

                CashReceiptSample cashReceiptSample = new CashReceiptSample(financialYearStart, tracer);
                cashReceiptSample.RunSample();
            }
        }
    }
}
