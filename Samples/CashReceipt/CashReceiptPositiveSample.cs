using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BigRedCloud.Api.Model;
using BigRedCloud.Api.Model.Batch;
using BigRedCloud.Api.Model.Querying;
using BigRedCloud.Api.Samples.Components;
using BigRedCloud.Api.Samples.Parameters;

namespace BigRedCloud.Api.Samples.CashReceipt
{
    internal class CashReceiptPositiveSample : CashReceiptBaseSample
    {
        public CashReceiptPositiveSample(DateTime financialYearStart, StreamWriter tracer) : base(financialYearStart, tracer) { }

        public void RunSample()
        {
            Tracer.WriteLine("***** Begin positive sample *****");
            Tracer.WriteLine();

            CreateSingleCashReceipt(true);
            CreateSingleCashReceipt(false);
            CreateCashReceiptsBatch();

            ODataResult<CashReceiptDto> cashReceiptsOdataResult = GetCashReceiptsPage();

            long singleCashReceiptId = cashReceiptsOdataResult.Items[0].id;
            CashReceiptDto singleCashReceipt = GetSingleCashReceipt(singleCashReceiptId);

            ModifyCashReceiptForUpdate(singleCashReceipt);
            UpdateSingleCashReceipt(singleCashReceipt);

            CashReceiptDto cashReceiptToDelete = cashReceiptsOdataResult.Items[1];
            DeleteSingleCashReceipt(cashReceiptToDelete.id, cashReceiptToDelete.timestamp);

            CashReceiptDto[] sourceForBatch = cashReceiptsOdataResult.Items.Skip(2).Take(4).ToArray();
            ProcessCashReceiptBatch(sourceForBatch);

            Tracer.WriteLine("***** End positive sample *****");
            Tracer.WriteLine();
        }

        private void CreateSingleCashReceipt(bool withCustomer)
        {
            CashReceiptGenerationParameters parameters = GetParametersForSingleCashReceiptCreation(withCustomer);
            CashReceiptDto cashReceipt = SampleDtoGenerator.GenerateCashReceipt(parameters);
            long createdId = ApiClientProvider.Default.CashReceipts.Create(cashReceipt);

            Tracer.WriteLine("Single Cash Receipt {0} Customer was created. Id of created item: {1}.", 
                withCustomer ? "with" : "without",
                createdId
            );
            Tracer.WriteLine();
        }

        private void CreateCashReceiptsBatch()
        {
            Func<int, CashReceiptDto> cashReceiptGenerator = GetCashReceiptGeneratorForBatchCreation();
            BatchItemProcessResult<CashReceiptDto>[] batchResult = CreateItems(CashReceiptsCountToCreate, cashReceiptGenerator, ApiClientProvider.Default.CashReceipts.ProcessBatch);

            Tracer.WriteLine("Batch of {0} Cash Receipts were created. Ids of created items: {1}.",
                CashReceiptsCountToCreate,
                String.Join(", ", batchResult.Select(itemResult => itemResult.id))
            );
            Tracer.WriteLine();
        }

        private ODataResult<CashReceiptDto> GetCashReceiptsPage()
        {
            // Retrieve page of Cash Receipts inside specific month.
            // "$inlinecount=allpages" includes in response total count of Cash Receipts satisfying specified filter.
            // "ge" means "greate or equal" (i.e. '>=').
            // "lt" means "less than" (i.e. '<').
            string odataParameters = String.Format(
                "$inlinecount=allpages&$filter=entryDate ge datetime'{0}' and entryDate lt datetime'{1}'",
                CashReceiptEntryDate.ToString(OdataDateFormat),
                CashReceiptEntryDate.AddMonths(1).ToString(OdataDateFormat)
            );
            ODataResult<CashReceiptDto> cashReceiptsOdataResult = ApiClientProvider.Default.CashReceipts.GetPage(odataParameters);

            Tracer.WriteLine(
                "Page of Cash Receipts was received. Total count: {0}. Ids of items on the received page: {1}.",
                cashReceiptsOdataResult.Count,
                String.Join(", ", cashReceiptsOdataResult.Items.Select(cashReceipt => cashReceipt.id))
            );
            Tracer.WriteLine();

            return cashReceiptsOdataResult;
        }

        private CashReceiptDto GetSingleCashReceipt(long id)
        {
            CashReceiptDto cashReceipt = ApiClientProvider.Default.CashReceipts.Get(id);

            Tracer.WriteLine("Single Cash Receipt with Id {0} was received.", cashReceipt.id);
            Tracer.WriteLine();

            return cashReceipt;
        }

        private void UpdateSingleCashReceipt(CashReceiptDto cashReceipt)
        {
            ApiClientProvider.Default.CashReceipts.Update(cashReceipt.id, cashReceipt);

            Tracer.WriteLine("Single Cash Receipt with Id {0} was updated.", cashReceipt.id);
            Tracer.WriteLine();
        }

        private void DeleteSingleCashReceipt(long id, byte[] timestamp)
        {
            ApiClientProvider.Default.CashReceipts.Delete(id, timestamp);

            Tracer.WriteLine("Single Cash Receipt with Id {0} was deleted.", id);
            Tracer.WriteLine();
        }

        private void ProcessCashReceiptBatch(CashReceiptDto[] source)
        {
            const int itemsToCreateCount = 2;
            const int itemsToUpdateCount = 2;
            const int itemsToDeleteCount = 2;

            List<BatchItem<CashReceiptDto>> batchCashReceipts = new List<BatchItem<CashReceiptDto>>();

            // Prepare batch items to create.
            CashReceiptGenerationParameters parametersWithCustomer = GetParametersForSingleCashReceiptCreation(true);
            CashReceiptGenerationParameters parametersWithoutCustomer = GetParametersForSingleCashReceiptCreation(false);
            for (int i = 0; i < itemsToCreateCount; i++)
            {
                bool withCustomer = (i % 2 == 0); // True if index is even.
                CashReceiptGenerationParameters parameters = withCustomer ? parametersWithCustomer : parametersWithoutCustomer;
                batchCashReceipts.Add(new BatchItem<CashReceiptDto>
                {
                    item = SampleDtoGenerator.GenerateCashReceipt(parameters), 
                    opCode = BatchOperationCodes.Create
                });
            }
            
            int sourceIndex = 0;

            // Prepare batch items to update.
            for (int i = 0; i < itemsToUpdateCount; i++)
            {
                CashReceiptDto cashReceiptToUpdate = source[sourceIndex];
                ModifyCashReceiptForUpdate(cashReceiptToUpdate);
                batchCashReceipts.Add(new BatchItem<CashReceiptDto>
                {
                    item = cashReceiptToUpdate, 
                    opCode = BatchOperationCodes.Update
                });
                sourceIndex++;
            }
            
            // Prepare batch items to delete.
            for (int i = 0; i < itemsToDeleteCount; i++)
            {
                batchCashReceipts.Add(new BatchItem<CashReceiptDto>
                {
                    item = new CashReceiptDto { id = source[sourceIndex].id, timestamp = source[sourceIndex].timestamp },
                    opCode = BatchOperationCodes.Delete
                });
                sourceIndex++;
            }

            // Execute batch operation.
            BatchItemProcessResult<CashReceiptDto>[] batchResult = ProcessBatch(batchCashReceipts.ToArray(), ApiClientProvider.Default.CashReceipts.ProcessBatch);

            // Display batch results.
            Tracer.WriteLine("Batch of Cash Receipts was processed:");

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
