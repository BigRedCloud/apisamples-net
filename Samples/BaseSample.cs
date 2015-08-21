using BigRedCloud.Api.Exceptions;
using BigRedCloud.Api.Model;
using BigRedCloud.Api.Model.Batch;
using BigRedCloud.Api.Model.Querying;
using BigRedCloud.Api.Samples.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BigRedCloud.Api.Samples
{
    internal abstract class BaseSample
    {
        protected StreamWriter Tracer { get; private set; }

        protected BaseSample(StreamWriter tracer)
        {
            Tracer = tracer;
        }

        /// <summary>
        /// Checks if specified count of Analysis Categories with specific Category Type exist.
        /// </summary>
        protected void CheckAnalysisCategories(int analysisCategoriesMinCount, CategoryTypeDto categoryType)
        {
            ODataResult<AnalysisCategoryDto> analysisCategories = ApiClientProvider.Default.AnalysisCategories.GetPageByCategoryType(categoryType.id);

            if (analysisCategories.Count < analysisCategoriesMinCount)
            {
                string message = String.Format(
                    "The minimal count of {0} Analysis Categories is {1}. Actual count is {2}. Please create missed Analysis Categories.",
                    categoryType.description,
                    analysisCategoriesMinCount,
                    analysisCategories.Count
                );
                throw new InvalidOperationException(message);
            }
        }

        protected void CheckUserDefinedFields(int userDefinedFieldsMinCount, CategoryTypeDto categoryType)
        {
            ODataResult<UserDefinedFieldDto> userDefinedFields = ApiClientProvider.Default.UserDefinedFields.GetPageByCategoryType(categoryType.id);

            if (userDefinedFields.Count < userDefinedFieldsMinCount)
            {
                string message = String.Format(
                    "The minimal count of {0} User Defined Fields is {1}. Actual count is {2}. Please create missed User Defined Fields.",
                    categoryType.description,
                    userDefinedFieldsMinCount,
                    userDefinedFields.Count
                );
                throw new InvalidOperationException(message);
            }
        }

        protected void CheckVatRates(int[] necessaryVatRatesPercentages, VatCategoryDto vatCategory)
        {
            ODataResult<VatRateDto> vatRates = ApiClientProvider.Default.VatRates.GetPageByVatCategory(vatCategory.id);

            bool areNecessaryVatRatesExist = necessaryVatRatesPercentages.All(percentage =>
                vatRates.Items.Any(vatRate => vatRate.percentage == percentage)
            );
            if (!areNecessaryVatRatesExist)
            {
                string message = String.Format(
                    "{0} Vat Rates with necessary percentages are missed. Necessary percentages: {1}. Please create missed Vat Rates.",
                    vatCategory.description,
                    String.Join(", ", necessaryVatRatesPercentages.Select(p => p + "%"))
                );
                throw new InvalidOperationException(message);
            }
        }

        protected void PrepareCustomers(int customersMinCount)
        {
            // "$inlinecount=allpages" includes in response total count of Customers.
            ODataResult<CustomerDto> customers = ApiClientProvider.Default.Customers.GetPage("$inlinecount=allpages");
            int missedCustomersCount = customersMinCount - customers.Count.Value;
            if (missedCustomersCount > 0)
            {
                CreateCustomers(missedCustomersCount);
            }
        }

        protected void PrepareProducts(int productsMinCount)
        {
            // "$inlinecount=allpages" includes in response total count of Products.
            ODataResult<ProductDto> products = ApiClientProvider.Default.Products.GetPage("$inlinecount=allpages");
            int missedProductsCount = productsMinCount - products.Count.Value;
            if (missedProductsCount > 0)
            {
                CreateProducts(missedProductsCount);
            }
        }

        protected void CreateCustomers(int count)
        {
            BatchItemProcessResult<CustomerDto>[] batchResult = CreateItems(count, i => SampleDtoGenerator.GenerateCustomer(), ApiClientProvider.Default.Customers.ProcessBatch);

            Tracer.WriteLine("Batch of {0} Customers was created. Ids of created items: {1}.",
                count,
                String.Join(", ", batchResult.Select(itemResult => itemResult.id))
            );
            Tracer.WriteLine();
        }

        protected void CreateProducts(int count)
        {
            BatchItemProcessResult<ProductDto>[] batchResult = CreateItems(count, i => SampleDtoGenerator.GenerateProduct(), ApiClientProvider.Default.Products.ProcessBatch);

            Tracer.WriteLine("Batch of {0} Products was created. Ids of created items: {1}.",
                count,
                String.Join(", ", batchResult.Select(itemResult => itemResult.id))
            );
            Tracer.WriteLine();
        }

        protected BatchItemProcessResult<TApiDto>[] CreateItems<TApiDto>(int count, Func<int, TApiDto> itemGenerator, Func<BatchItem<TApiDto>[], BatchItemProcessResult<TApiDto>[]> batchProcessor)
        {
            List<BatchItem<TApiDto>> itemsBatch = new List<BatchItem<TApiDto>>(count);
            for (int i = 0; i < count; i++)
            {
                BatchItem<TApiDto> batchItem = new BatchItem<TApiDto>
                {
                    opCode = BatchOperationCodes.Create,
                    item = itemGenerator.Invoke(i)
                };
                itemsBatch.Add(batchItem);
            }

            return ProcessBatch(itemsBatch.ToArray(), batchProcessor);
        }

        protected BatchItemProcessResult<TApiDto>[] ProcessBatch<TApiDto>(BatchItem<TApiDto>[] itemsBatch, Func<BatchItem<TApiDto>[], BatchItemProcessResult<TApiDto>[]> batchProcessor)
        {
            BatchItemProcessResult<TApiDto>[] batchResult = batchProcessor.Invoke(itemsBatch);
            ValidateBatchResult(batchResult);

            return batchResult;
        }

        protected void ValidateBatchResult<TApiDto>(BatchItemProcessResult<TApiDto>[] batchResult)
        {
            StringBuilder resultMessage = new StringBuilder();
            for (int i = 0; i < batchResult.Length; i++)
            {
                BatchItemProcessResult<TApiDto> itemResult = batchResult[i];
                if (!IsSuccessStatusCode(itemResult.code))
                {
                    string itemMessage = String.Format(
                        "There was an error during processing item number {0}. Error code: {1}. Error message: {2}",
                        i, itemResult.code, itemResult.message
                    );
                    resultMessage.AppendLine(itemMessage);
                }
            }
            if (resultMessage.Length > 0)
            {
                throw new InvalidOperationException(resultMessage.ToString());
            }
        }

        protected void PrintBatchResult<TApiDto>(BatchItemProcessResult<TApiDto>[] batchResult, string entityName)
        {
            for (var i = 0; i < batchResult.Length; i++)
            {
                BatchItemProcessResult<TApiDto> itemResult = batchResult[i];
                string messageTemplate = "{0} #{1}: Status code = {2}.";
                if (!IsSuccessStatusCode(itemResult.code))
                {
                    messageTemplate += " Message = \"{3}\"";
                }
                Tracer.WriteLine(messageTemplate, entityName, i + 1, itemResult.code, itemResult.message);
            }
            Tracer.WriteLine();
        }

        protected void ExecuteInvalidApiCall(Action operation)
        {
            try
            {
                operation.Invoke();
            }
            catch (ApiRequestException ex)
            {
                Tracer.WriteLine(ex.Message);
                Tracer.WriteLine();
            }
        }

        protected bool IsSuccessStatusCode(int statusCode)
        {
            return (200 <= statusCode && statusCode <= 299);
        }
    }
}
