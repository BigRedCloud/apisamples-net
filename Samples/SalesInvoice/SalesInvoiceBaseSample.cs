using BigRedCloud.Api.Model;
using BigRedCloud.Api.Model.Querying;
using BigRedCloud.Api.Samples.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BigRedCloud.Api.Samples.Parameters;

namespace BigRedCloud.Api.Samples.SalesInvoice
{
    internal abstract class SalesInvoiceBaseSample : BaseSample
    {
        protected const int AnalysisCategoriesMinCount = 2;
        protected const int UserDefinedFieldsMinCount = 2;
        protected readonly int[] VatRatesNecessaryPercentages = new int[] { 15, 20 };
        protected const int CustomersMinCount = 5;
        protected const int ProductsMinCount = 5;
        protected const int SalesInvoicesCountToCreate = 7;
        protected readonly DateTime SalesInvoiceEntryDate;
        protected const string OdataDateFormat = "yyyy-MM-dd";

        protected SalesInvoiceBaseSample(DateTime financialYearStart, StreamWriter tracer) : base(tracer)
        {
            SalesInvoiceEntryDate = financialYearStart.AddMonths(3);
        }

        /// <summary>
        /// Prepares environment to run sample: checks existing of necessary entities. If possible, missed entities will be created. Otherwise, error will be raised.
        /// At least 2 Sales Analysis Categories and User Defined Fields should exist. Otherwise, error will be raised.
        /// Sales Vat Rates with 15% and 20% should exist. Otherwise, error will be raised.
        /// At least 5 Customers should exist. Otherwise, missed Customers will be created.
        /// At least 5 Products should exist. Otherwise, missed Products will be created.
        /// </summary>
        protected void PrepareEnvironment()
        {
            Tracer.WriteLine("***** Begin environment preparation *****");
            Tracer.WriteLine();

            CategoryTypeDto salesCategoryType = ApiClientProvider.Default.CategoryTypes.Sales;
            CheckAnalysisCategories(AnalysisCategoriesMinCount, salesCategoryType);
            CheckUserDefinedFields(UserDefinedFieldsMinCount, salesCategoryType);

            VatCategoryDto salesVatCategory = ApiClientProvider.Default.VatCategories.Sales;
            CheckVatRates(VatRatesNecessaryPercentages, salesVatCategory);

            PrepareCustomers(CustomersMinCount);
            PrepareProducts(ProductsMinCount);

            Tracer.WriteLine("***** End environment preparation *****");
            Tracer.WriteLine();
        }

        /// <summary>
        /// Retrieves and combines parameters which necessary for creating Sales Invoice entity.
        /// </summary>
        protected SalesInvoiceGenerationParameters GetParametersForSingleSalesInvoiceCreation()
        {
            VatTypeDto vatType = ApiClientProvider.Default.VatTypes.OtherEU;

            // To create a Sales Invoice we need some customer. Let's retrieve a customer with minimal Id.
            ODataResult<CustomerDto> customersOdataResult = ApiClientProvider.Default.Customers.GetPage("$orderby=id&$top=1");
            CustomerDto customer = customersOdataResult.Items.Single();

            // Retrieve a product with minimal Id.
            ODataResult<ProductDto> productsOdataResult = ApiClientProvider.Default.Products.GetPage("$orderby=id&$top=1");
            ProductDto product = productsOdataResult.Items.Single();

            // Retrieve Vat Rates with 15% and 20%.
            VatCategoryDto salesVatCategory = ApiClientProvider.Default.VatCategories.Sales;
            ODataResult<VatRateDto> vatRatesOdataResult = ApiClientProvider.Default.VatRates.GetPageByVatCategory(salesVatCategory.id);
            Dictionary<decimal, VatRateDto> vatRates = vatRatesOdataResult.Items
                .Where(vatRate => vatRate.percentage == 15 || vatRate.percentage == 20)
                .ToDictionary(vatRate => vatRate.percentage);

            CategoryTypeDto salesCategoryType = ApiClientProvider.Default.CategoryTypes.Sales;

            // Retrieve two Analysis Categories with minimal order index.
            string odataParameters = String.Format("$filter=categoryTypeId eq {0}&$orderby=orderIndex&$top=2", salesCategoryType.id);
            ODataResult<AnalysisCategoryDto> analysisCategoriesOdataResult = ApiClientProvider.Default.AnalysisCategories.GetPage(odataParameters);
            AnalysisCategoryDto[] analysisCategories = analysisCategoriesOdataResult.Items;

            // Retrieve two User Defined Fields with minimal order index.
            ODataResult<UserDefinedFieldDto> userDefinedFieldsOdataResult = ApiClientProvider.Default.UserDefinedFields.GetPage(odataParameters);
            UserDefinedFieldDto[] userDefinedFields = userDefinedFieldsOdataResult.Items;

            return new SalesInvoiceGenerationParameters
            {
                EntryDate = SalesInvoiceEntryDate,
                VatTypeId = vatType.id,
                CustomerId = customer.id,
                ProductId = product.id,
                VatRates = vatRates,
                AnalysisCategories = analysisCategories,
                UserDefinedFields = userDefinedFields
            };
        }

        /// <summary>
        /// Initializes delegate which used for multiple Sales Invoices creation in batch sample.
        /// </summary>
        protected Func<int, SalesInvoiceCreditNoteDto> GetSalesInvoiceGeneratorForBatchCreation()
        {
            VatTypeDto vatType = ApiClientProvider.Default.VatTypes.OtherEU;
            CustomerDto[] customers = ApiClientProvider.Default.Customers.GetPage().Items;
            ProductDto[] products = ApiClientProvider.Default.Products.GetPage().Items;

            VatCategoryDto salesVatCategory = ApiClientProvider.Default.VatCategories.Sales;
            Dictionary<decimal, VatRateDto> vatRates = ApiClientProvider.Default.VatRates.GetPageByVatCategory(salesVatCategory.id).Items.ToDictionary(vatRate => vatRate.percentage);

            CategoryTypeDto salesCategoryType = ApiClientProvider.Default.CategoryTypes.Sales;
            AnalysisCategoryDto[] analysisCategories = ApiClientProvider.Default.AnalysisCategories.GetPageByCategoryType(salesCategoryType.id).Items;
            UserDefinedFieldDto[] userDefinedFields = ApiClientProvider.Default.UserDefinedFields.GetPageByCategoryType(salesCategoryType.id).Items;

            return index =>
            {
                SalesInvoiceGenerationParameters parameters = new SalesInvoiceGenerationParameters
                {
                    EntryDate = SalesInvoiceEntryDate,
                    VatTypeId = vatType.id,
                    CustomerId = customers[index % customers.Length].id,
                    ProductId = products[index % products.Length].id,
                    VatRates = vatRates,
                    AnalysisCategories = analysisCategories,
                    UserDefinedFields = userDefinedFields
                };
                return SampleDtoGenerator.GenerateSalesInvoice(parameters);
            };
        }

        protected void ModifySalesInvoiceForUpdate(SalesInvoiceCreditNoteDto salesInvoice)
        {
            salesInvoice.total = 75.6m;
            salesInvoice.totalVAT = 11.6m;

            var productTran = salesInvoice.productTrans.First();
            productTran.unitPrice = 8m;
            productTran.acEntries.First().value = 14m;
        }
    }
}
