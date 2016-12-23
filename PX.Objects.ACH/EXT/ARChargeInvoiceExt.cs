using PX.Common;
using PX.Data;

using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.CR;
using PX.Objects.CS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayBillsFilter = PX.Objects.AR.ARChargeInvoices.PayBillsFilter;

namespace PX.Objects.ACH
{
    public class ARChargeInvoiceExt : PXGraphExtension<ARChargeInvoices>
    {
        #region "Entry parameters"
        public static class PaymentChargeType
        {
            //NOTE: The reason why we don't use CA.PaymentMethodType.CreditCard is that the entry param in url doesn't play well with capital letters.
            public const string CreditCard = "ccd";
            public const string DirectDeposit = "ddt";

            [PXLocalizable]
            public static class UI
            {
                public const string CreditCard = "Credit Card";
                public const string DirectDeposit = "Direct Deposit";
            }
        }
        
        public class PayBillsFilterExt : PXCacheExtension<PayBillsFilter>
        {
            public abstract class paymentType : PX.Data.IBqlField
            {
            }

            [PXString(3, IsFixed = true)]
            [PXDefault(PaymentChargeType.CreditCard)]
            [PXUIField(DisplayName = Messages.MeansOfPayment, Visibility = PXUIVisibility.SelectorVisible)]
            [PXStringList(new string[] { PaymentChargeType.CreditCard, PaymentChargeType.DirectDeposit }, new string[] { PaymentChargeType.UI.CreditCard, PaymentChargeType.UI.DirectDeposit })]
            public virtual String PaymentType { get; set; }
        }
        #endregion

        #region select
        [PXFilterable]
        public PXFilteredProcessingJoin<ARInvoice, PayBillsFilter,
            InnerJoin<Customer, On<Customer.bAccountID, Equal<ARInvoice.customerID>>,
            InnerJoin<CustomerPaymentMethod, On<CustomerPaymentMethod.pMInstanceID, Equal<ARInvoice.pMInstanceID>>,
            LeftJoin<CashAccount, On<CashAccount.cashAccountID, Equal<CustomerPaymentMethod.cashAccountID>>>>>> ARDocumentList;
        protected virtual IEnumerable ardocumentlist()
        {
            PayBillsFilter filter = Base.Filter.Current;
            var filterExt = PXCache<PayBillsFilter>.GetExtension<PayBillsFilterExt>(filter);

            //It would also be possible to change the BQL query in the loop but to avoid breaking system pages, we will call the base class.
            if (filterExt.PaymentType == PaymentChargeType.CreditCard)
            {
                foreach(var arDocument in Base.ARDocumentList.Select())
                    yield return arDocument;
            }
            else
            {
                if (filter == null || filter.PayDate == null) yield break;

                DateTime OverDueForDate = ((DateTime)filter.PayDate).AddDays((short)-1 * (short)filter.OverDueFor);
                DateTime DueInLessThan = ((DateTime)filter.PayDate).AddDays((short)+1 * (short)filter.DueInLessThan);

                DateTime DiscountExparedWithinLast = ((DateTime)filter.PayDate).AddDays((short)-1 * (short)filter.DiscountExparedWithinLast);
                DateTime DiscountExpiresInLessThan = ((DateTime)filter.PayDate).AddDays((short)+1 * (short)filter.DiscountExpiresInLessThan);

                foreach (PXResult<ARInvoice, Customer, CustomerPaymentMethod, PaymentMethod, CashAccount, ARAdjust, ARPayment> it
                        in PXSelectJoin<ARInvoice,
                                    InnerJoin<Customer, On<Customer.bAccountID, Equal<ARInvoice.customerID>>,
                                    InnerJoin<CustomerPaymentMethod, On<CustomerPaymentMethod.bAccountID, Equal<ARInvoice.customerID>,
                                        And<CustomerPaymentMethod.pMInstanceID, Equal<ARInvoice.pMInstanceID>,
                                        And<CustomerPaymentMethod.isActive, Equal<boolTrue>>>>,
                                    InnerJoin<PaymentMethod, On<PaymentMethod.paymentMethodID, Equal<CustomerPaymentMethod.paymentMethodID>,
                                        //And<PaymentMethod.paymentType, Equal<PaymentMethodType.creditCard>, -- This line was replace by the one below
                                        And<PaymentMethod.paymentType, NotEqual<PaymentMethodType.creditCard>, //we want to supporte cash / check because Fedwire does...
                                        //And<PaymentMethod.aRIsProcessingRequired, Equal<boolTrue>>>>, -- This line was replace by the one below
                                        And<PaymentMethodExt.aRCreateBatchPayment, Equal<boolTrue>>>>,
                                    LeftJoin<CashAccount, On<CashAccount.cashAccountID, Equal<ARInvoice.cashAccountID>>,
                                    LeftJoin<ARAdjust, On<ARAdjust.adjdDocType, Equal<ARInvoice.docType>,
                                        And<ARAdjust.adjdRefNbr, Equal<ARInvoice.refNbr>,
                                        And<ARAdjust.released, Equal<boolFalse>,
                                        And<ARAdjust.voided, Equal<boolFalse>>>>>,
                                    LeftJoin<ARPayment, On<ARPayment.docType, Equal<ARAdjust.adjgDocType>,
                                        And<ARPayment.refNbr, Equal<ARAdjust.adjgRefNbr>>>>>>>>>,
                                    Where<ARInvoice.released, Equal<boolTrue>,
                                        And<ARInvoice.openDoc, Equal<boolTrue>,
                                        And<ARInvoice.pendingPPD, NotEqual<True>,
                                        And2<Where2<Where2<Where<Current<PayBillsFilter.showOverDueFor>, Equal<boolTrue>,
                                                    And<ARInvoice.dueDate, LessEqual<Required<ARInvoice.dueDate>>
                                                    >>,
                                            Or2<Where<Current<PayBillsFilter.showDueInLessThan>, Equal<boolTrue>,
                                                    And<ARInvoice.dueDate, GreaterEqual<Current<PayBillsFilter.payDate>>,
                                                    And<ARInvoice.dueDate, LessEqual<Required<ARInvoice.dueDate>>
                                                    >>>,
                                            Or2<Where<Current<PayBillsFilter.showDiscountExparedWithinLast>, Equal<boolTrue>,
                                                    And<ARInvoice.discDate, GreaterEqual<Required<ARInvoice.discDate>>,
                                                    And<ARInvoice.discDate, LessEqual<Current<PayBillsFilter.payDate>>
                                                    >>>,
                                            Or<Where<Current<PayBillsFilter.showDiscountExpiresInLessThan>, Equal<boolTrue>,
                                                    And<ARInvoice.discDate, GreaterEqual<Current<PayBillsFilter.payDate>>,
                                                    And<ARInvoice.discDate, LessEqual<Required<ARInvoice.discDate>>
                                                    >>>>>>>,
                                            Or<Where<Current<PayBillsFilter.showOverDueFor>, Equal<boolFalse>,
                                           And<Current<PayBillsFilter.showDueInLessThan>, Equal<boolFalse>,
                                           And<Current<PayBillsFilter.showDiscountExparedWithinLast>, Equal<boolFalse>,
                                           And<Current<PayBillsFilter.showDiscountExpiresInLessThan>, Equal<boolFalse>>>>>>>,

                                        And<Where2<Where<ARAdjust.adjgRefNbr, IsNull, Or<ARPayment.voided, Equal<boolTrue>>>,
                                        And<Match<Customer, Current<AccessInfo.userName>>>>>>>>>,
                                        OrderBy<Asc<ARInvoice.customerID>>>
                                .Select(Base, OverDueForDate,
                                              DueInLessThan,
                                              DiscountExparedWithinLast,
                                              DiscountExpiresInLessThan))
                {
                    ARInvoice doc = it;
                    CashAccount acct = it;
                    if (acct == null || acct.AccountID == null)
                    {
                        acct = findDefaultCashAccount(doc);
                    }
                    if (acct == null) continue;
                    if (String.IsNullOrEmpty(filter.CuryID) == false && (filter.CuryID != acct.CuryID)) continue;
                    yield return new PXResult<ARInvoice, Customer, CustomerPaymentMethod, CashAccount>(it, it, it, acct);
                }

                Base.ARDocumentList.Cache.IsDirty = false;
            }
        }

        #endregion

        #region Events
        protected virtual void PayBillsFilter_RowSelected(PXCache cache, PXRowSelectedEventArgs e, PXRowSelected del)
        {
            if (e.Row == null) return;
            var filter = (PayBillsFilter)e.Row;
            var filterExt = PXCache<PayBillsFilter>.GetExtension<PayBillsFilterExt>(filter);
            //Direct Deposit only works with USD
            if(filterExt.PaymentType == PaymentChargeType.DirectDeposit && filter.CuryID != "USD")
            {
                cache.SetValueExt<PayBillsFilter.curyID>(filter, "USD");
            }
            PXUIFieldAttribute.SetEnabled<PayBillsFilter.curyID>(cache, filter, filterExt.PaymentType != PaymentChargeType.DirectDeposit);

            if (filterExt.PaymentType == PaymentChargeType.DirectDeposit)
                PXGraph.InstanceCreated.AddHandler<ARPaymentEntry>(HandleSetDescriptionRefName);
            else
                PXGraph.InstanceCreated.RemoveHandler<ARPaymentEntry>(HandleSetDescriptionRefName);
            if (del != null)
                del(cache, e);
        }

        private void HandleSetDescriptionRefName(ARPaymentEntry graph)
        {
            graph.FieldUpdated.AddHandler<ARPayment.customerID>((cache, e) =>
            {
                var pmt = (ARPayment)e.Row;
                var cust = (BAccountR)PXSelectorAttribute.Select<ARPayment.customerID>(cache, pmt);
                pmt.DocDesc = String.Format(Messages.PaymentFrom, cust.AcctName);
            });

            //PaymentRefNumberAttribute behave so nicely that we only get it at the time of persisting
            graph.RowPersisting.AddHandler<ARPayment>((cache, e) =>
            {
                ((ARPayment)e.Row).ExtRefNbr = String.Empty; //Don't even think putting null here
            });
        }
        #endregion
        #region helpers
        protected CashAccount findDefaultCashAccount(ARInvoice aDoc)
        {
            CashAccount acct = null;
            PXCache cache = Base.arPayment.Cache;
            ARPayment payment = new ARPayment();
            payment.DocType = ARDocType.Payment;
            payment.CustomerID = aDoc.CustomerID;
            payment.CustomerLocationID = aDoc.CustomerLocationID;
            payment.BranchID = aDoc.BranchID;
            payment.PaymentMethodID = aDoc.PaymentMethodID;
            payment.PMInstanceID = aDoc.PMInstanceID;
            {
                object newValue;
                cache.RaiseFieldDefaulting<ARPayment.cashAccountID>(payment, out newValue);
                Int32? acctID = newValue as Int32?;
                if (acctID.HasValue)
                {
                    acct = PXSelect<CashAccount, Where<CashAccount.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>>.Select(Base, acctID);
                }
            }
            return acct;
        }
        #endregion
    }
}
