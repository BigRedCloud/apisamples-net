using BigRedCloud.Api.Model;
using BigRedCloud.Api.Model.Batch;
using BigRedCloud.Api.Model.Querying;
using BigRedCloud.Api.Samples.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BigRedCloud.Api.Samples.Parameters;

namespace BigRedCloud.Api.Samples.SalesInvoice
{
    internal class SalesInvoicePositiveSample : SalesInvoiceBaseSample
    {
        public SalesInvoicePositiveSample(StreamWriter tracer) : base(tracer) { }

        public void RunSample()
        {
            Tracer.WriteLine("***** Begin positive sample *****");
            Tracer.WriteLine();

            CreateSingleSalesInvoice();
            CreateSalesInvoicesBatch();

            ODataResult<SalesInvoiceCreditNoteDto> salesInvoicesOdataResult = GetSalesInvoicesPage();

            long singleSalesInvoiceId = salesInvoicesOdataResult.Items[0].id;
            SalesInvoiceCreditNoteDto singleSalesInvoice = GetSingleSalesInvoice(singleSalesInvoiceId);

            ModifySalesInvoiceForUpdate(singleSalesInvoice);
            UpdateSingleSalesInvoice(singleSalesInvoice);

            SalesInvoiceCreditNoteDto salesInvoiceToDelete = salesInvoicesOdataResult.Items[1];
            DeleteSingleSalesInvoice(salesInvoiceToDelete.id, salesInvoiceToDelete.timestamp);

            SalesInvoiceCreditNoteDto[] sourceForBatch = salesInvoicesOdataResult.Items.Skip(2).Take(4).ToArray();
            ProcessSalesInvoiceBatch(sourceForBatch);

            Tracer.WriteLine("***** End positive sample *****");
            Tracer.WriteLine();
        }

        private void CreateSingleSalesInvoice()
        {
            SalesInvoiceGenerationParameters parameters = GetParametersForSingleSalesInvoiceCreation();
            SalesInvoiceCreditNoteDto salesInvoice = SampleDtoGenerator.GenerateSalesInvoice(parameters);
            long createdId = ApiClientProvider.Default.SalesInvoices.Create(salesInvoice);

            Tracer.WriteLine("Single Sales Invoice was created. Id of created item: {0}.", createdId);
            Tracer.WriteLine();
        }

        private void CreateSalesInvoicesBatch()
        {
            Func<int, SalesInvoiceCreditNoteDto> salesInvoiceGenerator = GetSalesInvoiceGeneratorForBatchCreation();
            BatchItemProcessResult[] batchResult = CreateItems(SalesInvoicesCountToCreate, salesInvoiceGenerator, ApiClientProvider.Default.SalesInvoices.ProcessBatch);

            Tracer.WriteLine("Batch of {0} Sales Invoices were created. Ids of created items: {1}.",
                SalesInvoicesCountToCreate,
                String.Join(", ", batchResult.Select(itemResult => itemResult.id))
            );
            Tracer.WriteLine();
        }

        private ODataResult<SalesInvoiceCreditNoteDto> GetSalesInvoicesPage()
        {
            // Retrieve page of Sales Invoices inside specific month.
            // "$inlinecount=allpages" includes in response total count of Sales Invoices satisfying specified filter.
            // "ge" means "greate or equal" (i.e. '>=').
            // "lt" means "less than" (i.e. '<').
            string odataParameters = String.Format(
                "$inlinecount=allpages&$filter=entryDate ge datetime'{0}' and entryDate lt datetime'{1}'",
                SalesInvoiceEntryDate.ToString(OdataDateFormat),
                SalesInvoiceEntryDate.AddMonths(1).ToString(OdataDateFormat)
            );
            ODataResult<SalesInvoiceCreditNoteDto> salesInvoicesOdataResult = ApiClientProvider.Default.SalesInvoices.GetPage(odataParameters);

            Tracer.WriteLine(
                "Page of Sales Invoices was received. Total count: {0}. Ids of items on the received page: {1}.",
                salesInvoicesOdataResult.Count,
                String.Join(", ", salesInvoicesOdataResult.Items.Select(salesInvoice => salesInvoice.id))
            );
            Tracer.WriteLine();

            return salesInvoicesOdataResult;
        }

        private SalesInvoiceCreditNoteDto GetSingleSalesInvoice(long id)
        {
            SalesInvoiceCreditNoteDto salesInvoice = ApiClientProvider.Default.SalesInvoices.Get(id);

            Tracer.WriteLine("Single Sales Invoice with Id {0} was received.", salesInvoice.id);
            Tracer.WriteLine();

            return salesInvoice;
        }

        private void UpdateSingleSalesInvoice(SalesInvoiceCreditNoteDto salesInvoice)
        {
            ApiClientProvider.Default.SalesInvoices.Update(salesInvoice.id, salesInvoice);

            Tracer.WriteLine("Single Sales Invoice with Id {0} was updated.", salesInvoice.id);
            Tracer.WriteLine();
        }

        private void DeleteSingleSalesInvoice(long id, byte[] timestamp)
        {
            ApiClientProvider.Default.SalesInvoices.Delete(id, timestamp);

            Tracer.WriteLine("Single Sales Invoice with Id {0} was deleted.", id);
            Tracer.WriteLine();
        }

        private void ProcessSalesInvoiceBatch(SalesInvoiceCreditNoteDto[] source)
        {
            const int itemsToCreateCount = 2;
            const int itemsToUpdateCount = 2;
            const int itemsToDeleteCount = 2;

            List<BatchItem<SalesInvoiceCreditNoteDto>> batchSalesInvoices = new List<BatchItem<SalesInvoiceCreditNoteDto>>();

            // Prepare batch items to create.
            SalesInvoiceGenerationParameters parameters = GetParametersForSingleSalesInvoiceCreation();
            for (int i = 0; i < itemsToCreateCount; i++)
            {
                batchSalesInvoices.Add(new BatchItem<SalesInvoiceCreditNoteDto>
                {
                    item = SampleDtoGenerator.GenerateSalesInvoice(parameters), 
                    opCode = BatchOperationCodes.Create
                });
            }
            
            int sourceIndex = 0;

            // Prepare batch items to update.
            for (int i = 0; i < itemsToUpdateCount; i++)
            {
                SalesInvoiceCreditNoteDto salesInvoiceToUpdate = source[sourceIndex];
                ModifySalesInvoiceForUpdate(salesInvoiceToUpdate);
                batchSalesInvoices.Add(new BatchItem<SalesInvoiceCreditNoteDto>
                {
                    item = salesInvoiceToUpdate, 
                    opCode = BatchOperationCodes.Update
                });
                sourceIndex++;
            }
            
            // Prepare batch items to delete.
            for (int i = 0; i < itemsToDeleteCount; i++)
            {
                batchSalesInvoices.Add(new BatchItem<SalesInvoiceCreditNoteDto>
                {
                    item = new SalesInvoiceCreditNoteDto { id = source[sourceIndex].id, timestamp = source[sourceIndex].timestamp },
                    opCode = BatchOperationCodes.Delete
                });
                sourceIndex++;
            }

            // Execute batch operation.
            BatchItemProcessResult[] batchResult = ProcessBatch(batchSalesInvoices.ToArray(), ApiClientProvider.Default.SalesInvoices.ProcessBatch);

            // Display batch results.
            Tracer.WriteLine("Batch of Sales Invoices was processed:");

            IEnumerable<long> createdItemsIds = batchResult.Take(itemsToCreateCount).Select(itemResult => itemResult.id);
            Tracer.WriteLine("{0} items were created. Ids: {1}.", itemsToCreateCount, String.Join(", ", createdItemsIds));

            IEnumerable<long> updatedItemsIds = batchResult.Skip(itemsToCreateCount).Take(itemsToUpdateCount).Select(itemResult => itemResult.id);
            Tracer.WriteLine("{0} items were updated. Ids: {1}.", itemsToUpdateCount, String.Join(", ", updatedItemsIds));

            IEnumerable<long> deletedItemsIds = batchResult.Skip(itemsToCreateCount + itemsToUpdateCount).Take(itemsToDeleteCount).Select(itemResult => itemResult.id);
            Tracer.WriteLine("{0} items were deleted. Ids: {1}.", itemsToDeleteCount, String.Join(", ", deletedItemsIds));
            Tracer.WriteLine();
        }
    }
}
