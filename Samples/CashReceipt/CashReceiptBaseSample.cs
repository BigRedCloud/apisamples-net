using BigRedCloud.Api.Model;
using BigRedCloud.Api.Model.Querying;
using BigRedCloud.Api.Samples.Components;
using BigRedCloud.Api.Samples.Parameters;
using System;
using System.IO;
using System.Linq;

namespace BigRedCloud.Api.Samples.CashReceipt
{
    internal abstract class CashReceiptBaseSample : BaseSample
    {
        protected const int AnalysisCategoriesMinCount = 2;
        protected const int UserDefinedFieldsMinCount = 2;
        protected const int CustomersMinCount = 5;
        protected const int CashReceiptsCountToCreate = 7;
        protected readonly DateTime CashReceiptEntryDate = new DateTime(2014, 8, 1);
        protected const string OdataDateFormat = "yyyy-MM-dd";

        protected CashReceiptBaseSample(StreamWriter tracer) : base(tracer) { }

        /// <summary>
        /// Prepares environment to run sample: checks existing of necessary entities. If possible, missed entities will be created. Otherwise, error will be raised.
        /// At least 2 Cash Receipts Analysis Categories and User Defined Fields should exist. Otherwise, error will be raised.
        /// At least 5 Customers should exist. Otherwise, missed Customers will be created.
        /// </summary>
        protected void PrepareEnvironment()
        {
            Tracer.WriteLine("***** Begin environment preparation *****");
            Tracer.WriteLine();

            CategoryTypeDto cashReceiptsCategoryType = ApiClientProvider.Default.CategoryTypes.CashReceipts;
            CheckAnalysisCategories(AnalysisCategoriesMinCount, cashReceiptsCategoryType);
            CheckUserDefinedFields(UserDefinedFieldsMinCount, cashReceiptsCategoryType);

            PrepareCustomers(CustomersMinCount);
            
            Tracer.WriteLine("***** End environment preparation *****");
            Tracer.WriteLine();
        }

        /// <summary>
        /// Retrieves and combines parameters which necessary for creating Cash Receipt entity.
        /// </summary>
        protected CashReceiptGenerationParameters GetParametersForSingleCashReceiptCreation(bool withCustomer)
        {
            CategoryTypeDto cashReceiptsCategoryType = ApiClientProvider.Default.CategoryTypes.CashReceipts;

            // Retrieve two Analysis Categories with minimal order index.
            string odataParameters = String.Format("$filter=categoryTypeId eq {0}&$orderby=orderIndex&$top=2", cashReceiptsCategoryType.id);
            ODataResult<AnalysisCategoryDto> analysisCategoriesOdataResult = ApiClientProvider.Default.AnalysisCategories.GetPage(odataParameters);
            AnalysisCategoryDto[] analysisCategories = analysisCategoriesOdataResult.Items;

            // Retrieve two User Defined Fields with minimal order index.
            ODataResult<UserDefinedFieldDto> userDefinedFieldsOdataResult = ApiClientProvider.Default.UserDefinedFields.GetPage(odataParameters);
            UserDefinedFieldDto[] userDefinedFields = userDefinedFieldsOdataResult.Items;

            CustomerDto customer = null;
            if (withCustomer)
            {
                // Retrieve a customer with minimal Id.
                ODataResult<CustomerDto> customersOdataResult = ApiClientProvider.Default.Customers.GetPage("$orderby=id&$top=1");
                customer = customersOdataResult.Items.Single();
            }

            return new CashReceiptGenerationParameters
            {
                EntryDate = CashReceiptEntryDate,
                CustomerId = withCustomer ? customer.id : (long?)null,
                AnalysisCategories = withCustomer ? null : analysisCategories,
                UserDefinedFields = userDefinedFields
            };
        }

        /// <summary>
        /// Initializes delegate which used for multiple Sales Invoices creation in batch sample.
        /// </summary>
        protected Func<int, CashReceiptDto> GetCashReceiptGeneratorForBatchCreation()
        {
            CustomerDto[] customers = ApiClientProvider.Default.Customers.GetPage().Items;

            CategoryTypeDto cashReceiptsCategoryType = ApiClientProvider.Default.CategoryTypes.CashReceipts;
            AnalysisCategoryDto[] analysisCategories = ApiClientProvider.Default.AnalysisCategories.GetPageByCategoryType(cashReceiptsCategoryType.id).Items;
            UserDefinedFieldDto[] userDefinedFields = ApiClientProvider.Default.UserDefinedFields.GetPageByCategoryType(cashReceiptsCategoryType.id).Items;

            return index =>
            {
                bool withCustomer = (index % 2 == 0); // True if index is even.
                CashReceiptGenerationParameters parameters = new CashReceiptGenerationParameters
                {
                    EntryDate = CashReceiptEntryDate,
                    CustomerId = withCustomer ? customers[index % customers.Length].id : (long?)null,
                    AnalysisCategories = withCustomer ? null : analysisCategories,
                    UserDefinedFields = userDefinedFields
                };
                return SampleDtoGenerator.GenerateCashReceipt(parameters);
            };
        }

        protected void ModifyCashReceiptForUpdate(CashReceiptDto cashReceipt)
        {
            cashReceipt.total = 70;

            if (cashReceipt.customerId.HasValue)
            {
                cashReceipt.discount = 20;
            }
            else
            {
                cashReceipt.acEntries.First().value = 50m;
            }
        }
    }
}
