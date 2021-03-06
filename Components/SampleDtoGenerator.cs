﻿using BigRedCloud.Api.Model;
using BigRedCloud.Api.Samples.Parameters;

namespace BigRedCloud.Api.Samples.Components
{
    internal static class SampleDtoGenerator
    {
        public static SalesInvoiceCreditNoteDto GenerateSalesInvoice(SalesInvoiceGenerationParameters parameters)
        {
            return new SalesInvoiceCreditNoteDto
            {
                customerId = parameters.CustomerId,
                procDate = parameters.EntryDate.AddDays(5),
                entryDate = parameters.EntryDate,
                note = "note",
                details = "details",
                ourReference = "or",
                yourReference = "yr",
                total = 82.5m,
                totalVAT = 12.5m,
                loType = "1",
                vatTypeId = parameters.VatTypeId,
                deliveryTo = new[] { "dt_1", "dt_2" },
                productTrans = new ProductTranDto[]
                {
                    new ProductTranDto
                    {
                        unitPrice = 10,
                        quantity = 3,
                        vatRateId = parameters.VatRates[15].id,
                        productId = parameters.ProductId,
                        tranNotes = new[] { "tn_1_1", "tn_1_2" },
                        amountNet = 30m,
                        acEntries = new AcEntryDto[] 
                        {
                            new AcEntryDto
                            {
                                accountCode = parameters.AnalysisCategories[0].accountCode,
                                analysisCategoryId = parameters.AnalysisCategories[0].id,
                                value = 20m
                            },
                            new AcEntryDto
                            {
                                accountCode = parameters.AnalysisCategories[1].accountCode,
                                analysisCategoryId = parameters.AnalysisCategories[1].id,
                                value = 10m
                            }
                        },
                    },
                    new ProductTranDto
                    {
                        unitPrice = 20,
                        quantity = 2,
                        vatRateId = parameters.VatRates[20].id,
                        tranNotes = new[] { "tn_2_1", "tn_2_2" },
                        amountNet = 40m,
                        acEntries = new AcEntryDto[]
                        {
                            new AcEntryDto
                            {
                                accountCode = parameters.AnalysisCategories[0].accountCode,
                                analysisCategoryId = parameters.AnalysisCategories[0].id,
                                value = 40m
                            }
                        }
                    }
                },
                customFields = new UserDefinedFieldValueDto[]
                {
                    new UserDefinedFieldValueDto
                    {
                        userDefinedFieldId = parameters.UserDefinedFields[0].id,
                        value = "field_1"
                    },
                    new UserDefinedFieldValueDto
                    {
                        userDefinedFieldId = parameters.UserDefinedFields[1].id,
                        value = "field_2"
                    }
                }
            };
        }

        public static CashReceiptDto GenerateCashReceipt(CashReceiptGenerationParameters parameters)
        {
            CashReceiptDto cashReceipt = new CashReceiptDto
            {
                procDate = parameters.EntryDate.AddDays(5),
                entryDate = parameters.EntryDate,
                note = "note_1",
                total = 50,
                detailCollection = new[] { "det_1", "det_2" },
                customFields = new UserDefinedFieldValueDto[]
                {
                    new UserDefinedFieldValueDto
                    {
                        userDefinedFieldId = parameters.UserDefinedFields[0].id,
                        value = "field_1"
                    },
                    new UserDefinedFieldValueDto
                    {
                        userDefinedFieldId = parameters.UserDefinedFields[1].id,
                        value = "field_2"
                    }
                }
            };

            if (parameters.CustomerId.HasValue)
            {
                cashReceipt.customerId = parameters.CustomerId.Value;
                cashReceipt.discount = 10;
            }
            else
            {
                cashReceipt.acEntries = new AcEntryDto[] 
                {
                    new AcEntryDto
                    {
                        accountCode = parameters.AnalysisCategories[0].accountCode, 
                        analysisCategoryId = parameters.AnalysisCategories[0].id, 
                        value = 30m
                    },
                    new AcEntryDto
                    {
                        accountCode = parameters.AnalysisCategories[1].accountCode, 
                        analysisCategoryId = parameters.AnalysisCategories[1].id, 
                        value = 20m
                    }
                };
            }

            return cashReceipt;
        }

        public static CustomerDto GenerateCustomer()
        {
            return new CustomerDto
            {
                accountName = "accountName",
                accountNumber = "accountNumber",
                authCode = "authCode",
                code = Utils.GenerateRandomString(8),
                contact = "contact",
                eFTReference = "eFTReference",
                email = "email@email.com",
                fax = "1234567",
                mobile = "2345678",
                name = "name",
                ourCode = "ourCode",
                phone = "3456789",
                vatReg = "vatReg",
                bank = new EFTBankDto
                {
                    name = Utils.GenerateRandomString(8),
                    branch = "branch",
                    sortCode = "sc"
                },
                address = new[] { "a_1", "a_2", "a_3" },
                delivery = new[] { "del_1", "del_2", "del_3" }
            };
        }

        public static ProductDto GenerateProduct()
        {
            return new ProductDto
            {
                stockCode = Utils.GenerateRandomString(8),
                unitPrice = 10,
                details = new[] { "det_1", "det_2", "det_3" }
            };
        }
    }
}
