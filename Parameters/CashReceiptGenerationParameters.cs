using BigRedCloud.Api.Model;
using System;

namespace BigRedCloud.Api.Samples.Parameters
{
    public class CashReceiptGenerationParameters
    {
        public DateTime EntryDate { get; set; }
        public long? CustomerId { get; set; }
        public AnalysisCategoryDto[] AnalysisCategories { get; set; }
        public UserDefinedFieldDto[] UserDefinedFields { get; set; }
    }
}
