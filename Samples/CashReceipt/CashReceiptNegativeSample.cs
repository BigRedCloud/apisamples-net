using System.IO;
using BigRedCloud.Api.Model;
using BigRedCloud.Api.Model.Batch;
using BigRedCloud.Api.Model.Querying;
using BigRedCloud.Api.Samples.Components;
using BigRedCloud.Api.Samples.Parameters;

namespace BigRedCloud.Api.Samples.CashReceipt
{
    internal class CashReceiptNegativeSample : CashReceiptBaseSample
    {
        public CashReceiptNegativeSample(StreamWriter tracer) : base(tracer) { }

        public void RunSample()
        {
            Tracer.WriteLine("***** Begin negative sample *****");
            Tracer.WriteLine();

            TryToCreateCashReceiptWithIncorrectTotal();
            TryToCreateCashReceiptWithIncorrectTotalInBatch();
            TryToGetNonexistentCashReceipt();
            TryToUpdateCashReceiptWithInvalidTimestamp();

            Tracer.WriteLine("***** End negative sample *****");
            Tracer.WriteLine();
        }

        private void TryToCreateCashReceiptWithIncorrectTotal()
        {
            Tracer.WriteLine("Try to create Cash Receipt with incorrect total...");

            CashReceiptGenerationParameters parameters = GetParametersForSingleCashReceiptCreation(false);
            CashReceiptDto cashReceipt = SampleDtoGenerator.GenerateCashReceipt(parameters);
            cashReceipt.total = 123;
            ExecuteInvalidApiCall(() => ApiClientProvider.Default.CashReceipts.Create(cashReceipt));
        }

        private void TryToCreateCashReceiptWithIncorrectTotalInBatch()
        {
            Tracer.WriteLine("Try to create Cash Receipt with incorrect total in batch...");

            // Prepare batch items.
            CashReceiptGenerationParameters parameters = GetParametersForSingleCashReceiptCreation(false);
            BatchItem<CashReceiptDto>[] batchCashReceipts = new BatchItem<CashReceiptDto>[2];
            for (int i = 0; i < batchCashReceipts.Length; i++)
            {
                batchCashReceipts[i] = new BatchItem<CashReceiptDto>
                {
                    item = SampleDtoGenerator.GenerateCashReceipt(parameters),
                    opCode = BatchOperationCodes.Create
                };
            }
            batchCashReceipts[batchCashReceipts.Length - 1].item.total = 123;

            // Execute batch operation.
            BatchItemProcessResult[] batchResult = ApiClientProvider.Default.CashReceipts.ProcessBatch(batchCashReceipts);

            // Display batch results.
            PrintBatchResult(batchResult, "Cash Receipt");
        }

        private void TryToGetNonexistentCashReceipt()
        {
            Tracer.WriteLine("Try to get nonexistent Cash Receipt...");

            ExecuteInvalidApiCall(() => ApiClientProvider.Default.CashReceipts.Get(0));
        }

        private void TryToUpdateCashReceiptWithInvalidTimestamp()
        {
            // Retriewe some single Cash Receipt.
            ODataResult<CashReceiptDto> cashReceiptsOdataResult = ApiClientProvider.Default.CashReceipts.GetPage("$top=1");
            CashReceiptDto cashReceipt = cashReceiptsOdataResult.Items[0];
            
            Tracer.WriteLine("Try to update Cash Receipt with correct timestamp...");
            ApiClientProvider.Default.CashReceipts.Update(cashReceipt.id, cashReceipt);
            Tracer.WriteLine("Success.");
            Tracer.WriteLine();

            // After previous update, timestamp of Cash Receipt was changed.
            // If now we will use old timestamp to update this Cash Receipt, then conflict error will occured.
            Tracer.WriteLine("Try to update Cash Receipt with outdated timestamp...");
            ExecuteInvalidApiCall(() => ApiClientProvider.Default.CashReceipts.Update(cashReceipt.id, cashReceipt));

            // To update this Cash Receipt we should retrieve an actual entity and then execute update operation for this entity.
            Tracer.WriteLine("Retrieve Cash Receipt with updated timestamp and try to update...");
            CashReceiptDto cashReceiptWithUpdatedTimestamp = ApiClientProvider.Default.CashReceipts.Get(cashReceipt.id);
            ApiClientProvider.Default.CashReceipts.Update(cashReceiptWithUpdatedTimestamp.id, cashReceiptWithUpdatedTimestamp);
            Tracer.WriteLine("Success.");
            Tracer.WriteLine();
        }
    }
}
