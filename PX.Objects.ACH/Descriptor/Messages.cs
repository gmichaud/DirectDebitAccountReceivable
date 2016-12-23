using PX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.ACH
{
    [PXLocalizable("ACH/AR Error")]
    public static class Messages
    {
        public const string BeneficiaryAccoutNo = "Beneficiary Account No:";
        public const string BeneficiaryName = "Beneficiary Name:";
        public const string BankRoutingNumber = "Bank Routing Number (ABA):";
        public const string BankName = "Bank Name:";
        public const string CompanyID = "Company ID";
        public const string CompanyIDType = "Company ID Type";
        public const string InsertDefaultData = "Auto-Configure";
        public const string InsertDefaultInAP = "Do you want to use and insert ACH default data for AP ?";
        public const string MeansOfPayment = "Means of Payment";
        public const string PaymentFrom = "Payment from {0}";
        public const string ViewARDocument = "View AR Document";
        public const string NextCheckNumberIsRequiredForProcessing = "Next number is required if to generate ACH file. You can also check AR - Suggest Next Number in Payment Method Settings.";
        public const string ARPaymentsAreAddedToTheBatchButWasNotUpdatedCorrectly = "AR Payments have been successfully added to the Batch Payment {0}, but update of their statuses have failed.";
        public const string WarningSettingACHPaymentMethod = "You need to check 'Require Card/Account Number' and 'Require Remittance Information for Cash Account' to to use batch processing.";

    }
}
