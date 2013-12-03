using BigRedCloud.Api.Model;
using BigRedCloud.Api.Model.Batch;
using BigRedCloud.Api.Model.Querying;
using BigRedCloud.Api.Samples.Components;
using System.IO;
using BigRedCloud.Api.Samples.Parameters;

namespace BigRedCloud.Api.Samples.SalesInvoice
{
    internal class SalesInvoiceNegativeSample : SalesInvoiceBaseSample
    {
        public SalesInvoiceNegativeSample(StreamWriter tracer) : base(tracer) { }

        public void RunSample()
        {
            Tracer.WriteLine("***** Begin negative sample *****");
            Tracer.WriteLine();

            TryToCreateSalesInvoiceWithIncorrectTotal();
            TryToCreateSalesInvoiceWithIncorrectTotalInBatch();
            TryToGetNonexistentSalesInvoice();
            TryToUpdateSalesInvoiceWithInvalidTimestamp();

            Tracer.WriteLine("***** End negative sample *****");
            Tracer.WriteLine();
        }

        private void TryToCreateSalesInvoiceWithIncorrectTotal()
        {
            Tracer.WriteLine("Try to create Sales Invoice with incorrect total...");

            SalesInvoiceGenerationParameters parameters = GetParametersForSingleSalesInvoiceCreation();
            SalesInvoiceCreditNoteDto salesInvoice = SampleDtoGenerator.GenerateSalesInvoice(parameters);
            salesInvoice.total = 123;
            ExecuteInvalidApiCall(() => ApiClientProvider.Default.SalesInvoices.Create(salesInvoice));
        }

        private void TryToCreateSalesInvoiceWithIncorrectTotalInBatch()
        {
            Tracer.WriteLine("Try to create Sales Invoice with incorrect total in batch...");

            // Prepare batch items.
            SalesInvoiceGenerationParameters parameters = GetParametersForSingleSalesInvoiceCreation();
            BatchItem<SalesInvoiceCreditNoteDto>[] batchSalesInvoices = new BatchItem<SalesInvoiceCreditNoteDto>[2];
            for (int i = 0; i < batchSalesInvoices.Length; i++)
            {
                batchSalesInvoices[i] = new BatchItem<SalesInvoiceCreditNoteDto>
                {
                    item = SampleDtoGenerator.GenerateSalesInvoice(parameters),
                    opCode = BatchOperationCodes.Create
                };
            }
            batchSalesInvoices[batchSalesInvoices.Length - 1].item.total = 123;

            // Execute batch operation.
            BatchItemProcessResult[] batchResult = ApiClientProvider.Default.SalesInvoices.ProcessBatch(batchSalesInvoices);

            // Display batch results.
            PrintBatchResult(batchResult, "Sales Invoice");
        }

        private void TryToGetNonexistentSalesInvoice()
        {
            Tracer.WriteLine("Try to get nonexistent Sales Invoice...");

            ExecuteInvalidApiCall(() => ApiClientProvider.Default.SalesInvoices.Get(0));
        }

        private void TryToUpdateSalesInvoiceWithInvalidTimestamp()
        {
            // Retriewe some single Sales Invoice.
            ODataResult<SalesInvoiceCreditNoteDto> salesInvoicesOdataResult = ApiClientProvider.Default.SalesInvoices.GetPage("$top=1");
            SalesInvoiceCreditNoteDto salesInvoice = salesInvoicesOdataResult.Items[0];
            
            Tracer.WriteLine("Try to update Sales Invoice with correct timestamp...");
            ApiClientProvider.Default.SalesInvoices.Update(salesInvoice.id, salesInvoice);
            Tracer.WriteLine("Success.");
            Tracer.WriteLine();

            // After previous update, timestamp of Sales Invoice was changed.
            // If now we will use old timestamp to update this Sales Invoice, then conflict error will occured.
            Tracer.WriteLine("Try to update Sales Invoice with outdated timestamp...");
            ExecuteInvalidApiCall(() => ApiClientProvider.Default.SalesInvoices.Update(salesInvoice.id, salesInvoice));

            // To update this Sales Invoice we should retrieve an actual entity and then execute update operation for this entity.
            Tracer.WriteLine("Retrieve Sales Invoice with updated timestamp and try to update...");
            SalesInvoiceCreditNoteDto salesInvoiceWithUpdatedTimestamp = ApiClientProvider.Default.SalesInvoices.Get(salesInvoice.id);
            ApiClientProvider.Default.SalesInvoices.Update(salesInvoiceWithUpdatedTimestamp.id, salesInvoiceWithUpdatedTimestamp);
            Tracer.WriteLine("Success.");
            Tracer.WriteLine();
        }
    }
}
