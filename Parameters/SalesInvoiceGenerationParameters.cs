using BigRedCloud.Api.Model;
using System;
using System.Collections.Generic;

namespace BigRedCloud.Api.Samples.Parameters
{
    internal class SalesInvoiceGenerationParameters
    {
        public DateTime EntryDate { get; set; }
        public long VatTypeId { get; set; }
        public long CustomerId { get; set; }
        public long ProductId { get; set; }
        public Dictionary<decimal, VatRateDto> VatRates { get; set; }
        public AnalysisCategoryDto[] AnalysisCategories { get; set; }
        public UserDefinedFieldDto[] UserDefinedFields { get; set; }
    }
}
