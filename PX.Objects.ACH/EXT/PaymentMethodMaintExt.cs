using PX.Data;
using PX.Objects.CA;
using System;
using System.Collections;

namespace PX.Objects.ACH
{
    public class PaymentMethodMaintExt : PXGraphExtension<PaymentMethodMaint>
    {
        protected virtual void PaymentMethod_RowSelected(PXCache cache, PXRowSelectedEventArgs e, PXRowSelected del)
        {
            if (e.Row == null) return;

            var row = (PaymentMethod)e.Row;
            var rowExt = PXCache<PaymentMethod>.GetExtension<PaymentMethodExt>(row);

            bool isDirectDeposit = (row.PaymentType != PaymentMethodType.CreditCard);
            bool missingConfigCreateBatch = (rowExt.ARCreateBatchPayment == true && (row.UseForCA != true || row.IsAccountNumberRequired != true));
            bool createARBatch = (rowExt.ARCreateBatchPayment == true);


            PXUIFieldAttribute.SetVisible<PaymentMethodExt.aRCreateBatchPayment>(cache, row, isDirectDeposit);
            PXUIFieldAttribute.SetWarning<PaymentMethodExt.aRCreateBatchPayment>(cache, row, missingConfigCreateBatch ? Messages.WarningSettingACHPaymentMethod
                                                                                                                      : String.Empty);

            PXUIFieldAttribute.SetVisible<PaymentMethodExt.aRBatchExportSYMappingID>(cache, row, isDirectDeposit && createARBatch);
            PXUIFieldAttribute.SetEnabled<PaymentMethodExt.aRBatchExportSYMappingID>(cache, row, isDirectDeposit && createARBatch);
            PXUIFieldAttribute.SetRequired<PaymentMethodExt.aRBatchExportSYMappingID>(cache, isDirectDeposit && createARBatch);
            insertDefaultData.SetVisible(isDirectDeposit && createARBatch);

            if (del != null)
                del(cache, e);

            //Note they need to be call after the delegate because they are overriding an operation from it
            bool isCreditCard = (row.PaymentType == PaymentMethodType.CreditCard);
            PXUIFieldAttribute.SetVisible<PaymentMethodDetail.isIdentifier>(Base.Details.Cache, null, isCreditCard || (isDirectDeposit && createARBatch));
            PXUIFieldAttribute.SetEnabled<PaymentMethodDetail.isIdentifier>(Base.Details.Cache, null, isCreditCard || (isDirectDeposit && createARBatch));

            PXUIFieldAttribute.SetVisible<PaymentMethodDetail.displayMask>(Base.Details.Cache, null, isCreditCard || (isDirectDeposit && createARBatch));
            PXUIFieldAttribute.SetEnabled<PaymentMethodDetail.displayMask>(Base.Details.Cache, null, isCreditCard || (isDirectDeposit && createARBatch));

            PXUIFieldAttribute.SetVisible<PaymentMethodDetail.isEncrypted>(Base.Details.Cache, null, !(isDirectDeposit && createARBatch));
            PXUIFieldAttribute.SetEnabled<PaymentMethodDetail.isEncrypted>(Base.Details.Cache, null, !(isDirectDeposit && createARBatch));

            PXUIFieldAttribute.SetVisible<PaymentMethodDetail.orderIndex>(Base.Details.Cache, null, !(isDirectDeposit && createARBatch));
            PXUIFieldAttribute.SetEnabled<PaymentMethodDetail.orderIndex>(Base.Details.Cache, null, !(isDirectDeposit && createARBatch));

            //We use a common field for batch numbering
            PXUIFieldAttribute.SetVisible<PaymentMethodAccount.aPBatchLastRefNbr>(Base.CashAccounts.Cache, null, row.UseForAP == true || (isDirectDeposit && createARBatch));
        }

        public PXAction<PaymentMethod> insertDefaultData;
        [PXUIField(DisplayName = Messages.InsertDefaultData)]
        [PXButton()]
        public virtual IEnumerable InsertDefaultData(PXAdapter adapter)
        {
            foreach(PaymentMethod pt in adapter.Get())
            {
                pt.UseForCA = true;
                pt.IsAccountNumberRequired = true;
                foreach (PaymentMethodDetail iDet in Base.Details.Select())
                {
                    Base.Details.Cache.Delete(iDet);
                }               
                this.fillACHforARDefaults();
                this.fillACHforCADefaults();
                if(pt.UseForAP == true || Base.PaymentMethod.Ask(Messages.InsertDefaultInAP, MessageButtons.YesNo) == WebDialogResult.Yes)
                {
                    pt.UseForAP = true;
                    pt.APAdditionalProcessing = CA.PaymentMethod.aPAdditionalProcessing.CreateBatchPayment;

                    this.fillACHforAPDefaults();
                }

                yield return pt;
            }            
        }

        #region Internal Auxillary Functions
        protected virtual void fillACHforARDefaults()
        {
            PaymentMethodDetail det = this.addDefaultsToDetails(ACHAttributes.AttributeName.BeneficiaryAccoutNo, Messages.BeneficiaryAccoutNo, PaymentMethodDetailUsage.UseForARCards);
            det.DisplayMask = ACHAttributes.MaskDefaults.DefaultIdentifier;
            det.IsIdentifier = true;
            this.addDefaultsToDetails(ACHAttributes.AttributeName.BeneficiaryName, Messages.BeneficiaryName, PaymentMethodDetailUsage.UseForARCards);
            this.addDefaultsToDetails(ACHAttributes.AttributeName.BankRoutingNumber, Messages.BankRoutingNumber, PaymentMethodDetailUsage.UseForARCards);
            this.addDefaultsToDetails(ACHAttributes.AttributeName.BankName, Messages.BankName, PaymentMethodDetailUsage.UseForARCards);
        }

        protected virtual void fillACHforAPDefaults()
        {
            this.addDefaultsToDetails(ACHAttributes.AttributeName.BeneficiaryAccoutNo, Messages.BeneficiaryAccoutNo, PaymentMethodDetailUsage.UseForVendor);
            this.addDefaultsToDetails(ACHAttributes.AttributeName.BeneficiaryName, Messages.BeneficiaryName, PaymentMethodDetailUsage.UseForVendor);
            this.addDefaultsToDetails(ACHAttributes.AttributeName.BankRoutingNumber, Messages.BankRoutingNumber, PaymentMethodDetailUsage.UseForVendor);
            this.addDefaultsToDetails(ACHAttributes.AttributeName.BankName, Messages.BankName, PaymentMethodDetailUsage.UseForVendor);
        }

        protected virtual void fillACHforCADefaults()
        {
            this.addDefaultsToDetails(ACHAttributes.AttributeName.BeneficiaryAccoutNo, Messages.BeneficiaryAccoutNo, PaymentMethodDetailUsage.UseForCashAccount);
            this.addDefaultsToDetails(ACHAttributes.AttributeName.BeneficiaryName, Messages.BeneficiaryName, PaymentMethodDetailUsage.UseForCashAccount);
            this.addDefaultsToDetails(ACHAttributes.AttributeName.BankRoutingNumber, Messages.BankRoutingNumber, PaymentMethodDetailUsage.UseForCashAccount);
            this.addDefaultsToDetails(ACHAttributes.AttributeName.BankName, Messages.BankName, PaymentMethodDetailUsage.UseForCashAccount);
            this.addDefaultsToDetails(ACHAttributes.AttributeName.CompanyID, Messages.CompanyID, PaymentMethodDetailUsage.UseForCashAccount);
            this.addDefaultsToDetails(ACHAttributes.AttributeName.CompanyIDType, Messages.CompanyIDType, PaymentMethodDetailUsage.UseForCashAccount);
        }

        private PaymentMethodDetail addDefaultsToDetails(ACHAttributes.AttributeName aAttr, string aDescr, string useFor)
        {
            PaymentMethodDetail det = new PaymentMethodDetail();
            ImportDefaults(det, aAttr);
            det.Descr = aDescr;
            det.UseFor = useFor;
            det = (PaymentMethodDetail)Base.Details.Cache.Insert(det);
            return det;
        }

        private static void ImportDefaults(PaymentMethodDetail aData, ACHAttributes.AttributeName aAttr)
        {
            aData.DetailID = ACHAttributes.GetID(aAttr);
            aData.EntryMask = ACHAttributes.GetMask(aAttr);
            aData.ValidRegexp = ACHAttributes.GetValidationRegexp(aAttr);
            aData.IsRequired = true;
        }
        #endregion

        public static class ACHAttributes
        {
            public enum AttributeName
            {
                BeneficiaryAccoutNo,
                BeneficiaryName,
                BankRoutingNumber,
                BankName,
                CompanyID,
                CompanyIDType
            }

            public static string GetID(AttributeName aID)
            {
                return IDS[(int)aID];
            }

            public static string GetMask(AttributeName aID)
            {
                return EntryMasks[(int)aID];
            }

            public static string GetValidationRegexp(AttributeName aID)
            {
                return ValidationRegexps[(int)aID];
            }

            public const string BeneficiaryAccoutNo = "1";
            public const string BeneficiaryName = "2";
            public const string BankRoutingNumber = "3";
            public const string BankName = "4";
            public const string CompanyID = "5";
            public const string CompanyIDType = "6";

            public static class MaskDefaults
            {
                public const string DefaultIdentifier = "#################";

                public const string BeneficiaryAccoutNo = "";
                public const string BeneficiaryName = "";
                public const string BankRoutingNumber = "000000000";
                public const string BankName = "";
                public const string CompanyID = "";
                public const string CompanyIDType = "0";
            }

            public static class ValidationRegexp
            {
                public const string BeneficiaryAccoutNo = @"^\d{1,17}$";
                public const string BeneficiaryName = @"^([\w]|\s){0,22}$";
                public const string BankRoutingNumber = @"^\d{9,9}$";
                public const string BankName = @"^([\w]|\s){0,22}$";
                public const string CompanyID = @"^([\w]|\s){0,9}$";
                public const string CompanyIDType = @"^\d{1,1}$";
            }

            #region Private Members
            private static string[] IDS = { BeneficiaryAccoutNo, BeneficiaryName, BankRoutingNumber, BankName, CompanyID, CompanyIDType };
            private static string[] EntryMasks = { MaskDefaults.BeneficiaryAccoutNo, MaskDefaults.BeneficiaryName, MaskDefaults.BankRoutingNumber, MaskDefaults.BankName, MaskDefaults.CompanyID, MaskDefaults.CompanyIDType };
            private static string[] ValidationRegexps = { ValidationRegexp.BeneficiaryAccoutNo, ValidationRegexp.BeneficiaryName, ValidationRegexp.BankRoutingNumber, ValidationRegexp.BankName, ValidationRegexp.CompanyID, ValidationRegexp.CompanyIDType };

            #endregion
        }
    }
}
