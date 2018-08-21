using PX.Data;
using PX.Objects.AR;
using PX.Objects.AR.MigrationMode;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PX.Objects.ACH
{
    //NOTE: Based on APPrintChecks
    [TableAndChartDashboardType]
    public class ARProcessPayment : PXGraph<ARProcessPayment>
    {
        public PXFilter<ProcessPaymentFilter> Filter;
        public PXCancel<ProcessPaymentFilter> Cancel;
        public PXAction<ProcessPaymentFilter> ViewDocument;
        [PXFilterable]
        public PXFilteredProcessingJoin<ARPayment, ProcessPaymentFilter, InnerJoin<Customer, On<Customer.bAccountID, Equal<ARPayment.customerID>>>, Where<boolTrue, Equal<boolTrue>>, OrderBy<Asc<Customer.acctName, Asc<ARPayment.refNbr>>>> ARPaymentList;

        public PXSelect<CurrencyInfo> currencyinfo;
        public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Required<CurrencyInfo.curyInfoID>>>> CurrencyInfo_CuryInfoID;


        [PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
        [PXEditDetailButton]
        public virtual IEnumerable viewDocument(PXAdapter adapter)
        {
            if (ARPaymentList.Current != null)
            {
                PXRedirectHelper.TryRedirect(ARPaymentList.Cache, ARPaymentList.Current, "Document", PXRedirectHelper.WindowMode.NewWindow);
            }
            return adapter.Get();
        }

        #region Setups
        public ARSetupNoMigrationMode ARSetup;
        public CMSetupSelect CMSetup;
        public PXSetup<GL.Company> Company;
        public PXSetup<PaymentMethodAccount, Where<PaymentMethodAccount.cashAccountID, Equal<Current<ProcessPaymentFilter.payAccountID>>, And<PaymentMethodAccount.paymentMethodID, Equal<Current<ProcessPaymentFilter.payTypeID>>>>> cashaccountdetail;
        public PXSetup<PaymentMethod, Where<PaymentMethod.paymentMethodID, Equal<Current<ProcessPaymentFilter.payTypeID>>>> paymenttype;
        #endregion

        public ARProcessPayment()
        {
            ARSetup setup = ARSetup.Current;
            PXUIFieldAttribute.SetEnabled(ARPaymentList.Cache, null, false);
            PXUIFieldAttribute.SetEnabled<ARPayment.selected>(ARPaymentList.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<ARPayment.extRefNbr>(ARPaymentList.Cache, null, true);

            ARPaymentList.SetSelected<ARPayment.selected>();
            PXUIFieldAttribute.SetDisplayName<ARPayment.customerID>(ARPaymentList.Cache, "Customer ID");
        }

        bool cleared;
        public override void Clear()
        {
            Filter.Current.CurySelTotal = 0m;
            Filter.Current.SelTotal = 0m;
            Filter.Current.SelCount = 0;
            cleared = true;
            base.Clear();
        }

        Dictionary<object, object> _copies = new Dictionary<object, object>();

        protected virtual IEnumerable ARPaymentlist()
        {
            if (cleared)
            {
                foreach (ARPayment doc in ARPaymentList.Cache.Updated)
                {
                    var docExt = PXCache<ARPayment>.GetExtension<ARPaymentExt>(doc);
                    docExt.Passed = false;
                }
            }

            foreach (PXResult<ARPayment, Customer, PaymentMethod, CABatchDetail> doc in PXSelectJoin<ARPayment,
                InnerJoinSingleTable<Customer, On<Customer.bAccountID, Equal<ARPayment.customerID>>,
                InnerJoin<PaymentMethod, On<PaymentMethod.paymentMethodID, Equal<ARPayment.paymentMethodID>>,
                LeftJoin<CABatchDetail, On<CABatchDetail.origModule, Equal<BatchModule.moduleAR>,
                        And<CABatchDetail.origDocType, Equal<ARPayment.docType>,
                        And<CABatchDetail.origRefNbr, Equal<ARPayment.refNbr>>>>>>>,
                Where2<Where<ARPayment.status, Equal<ARDocStatus.balanced>,
                    And<CABatchDetail.batchNbr, IsNull,
                    And<ARPayment.cashAccountID, Equal<Current<ProcessPaymentFilter.payAccountID>>,
                    And<ARPayment.paymentMethodID, Equal<Current<ProcessPaymentFilter.payTypeID>>,
                    And<Match<Customer, Current<AccessInfo.userName>>>>>>>,
                    And<Where<ARPayment.docType, Equal<ARDocType.payment>,
                        Or<ARPayment.docType, Equal<ARDocType.prepayment>>>>>>
                            .Select(this))
            {
                yield return new PXResult<ARPayment, Customer>(doc, doc);
                if (_copies.ContainsKey((ARPayment)doc))
                {
                    _copies.Remove((ARPayment)doc);
                }
                _copies.Add((ARPayment)doc, PXCache<ARPayment>.CreateCopy(doc));
            }
        }

        public virtual void AssignNumbers(ARPaymentEntryExt pe, ARPayment doc, ref string NextCheckNbr)
        {
            pe.RowPersisting.RemoveHandler<ARAdjust>(pe.ARAdjust_RowPersisting);
            pe.Clear(PXClearOption.PreserveTimeStamp);
            doc = pe.Document.Current = pe.Document.Search<ARPayment.refNbr>(doc.RefNbr, doc.DocType);
            PaymentMethodAccount det = pe.cashaccountdetail.Select();

            var payExt = PXCache<ARPayment>.GetExtension<ARPaymentExt>(pe.Document.Current);

            if (!string.IsNullOrEmpty(NextCheckNbr))
            {
                if (String.IsNullOrEmpty(pe.Document.Current.ExtRefNbr))
                {
                    payExt.StubCntr = 1;
                    payExt.BillCntr = 0;
                    pe.Document.Current.ExtRefNbr = NextCheckNbr;
                    if (String.IsNullOrEmpty(NextCheckNbr)) throw new PXException(AP.Messages.NextCheckNumberIsRequiredForProcessing);

                    pe.cashaccountdetail.Update(det); // det.APLastRefNumber was modified in StoreStubNumber method

                    NextCheckNbr = AutoNumberAttribute.NextNumber(NextCheckNbr);
                    payExt.Printed = true;
                    pe.Document.Current.Hold = false;
                    pe.Document.Current.UpdateNextNumber = true;
                    pe.Document.Update(pe.Document.Current);
                }
                else
                {
                    if (payExt.Printed != true || pe.Document.Current.Hold != false)
                    {
                        payExt.Printed = true;
                        pe.Document.Current.Hold = false;
                        pe.Document.Update(pe.Document.Current);
                    }
                }
            }
        }

        public static CABatch CreateBatchPayment(List<ARPayment> list, ProcessPaymentFilter filter)
        {
            ARBatchEntry be = PXGraph.CreateInstance<ARBatchEntry>();
            var result = be.Document.Insert(new CABatch());
            var resultExt = PXCache<CABatch>.GetExtension<CABatchExt>(result);
            resultExt.BatchModule = GL.BatchModule.AR;
            be.Document.Current = result;
            CABatch copy = (CABatch)be.Document.Cache.CreateCopy(result);

            copy.CashAccountID = filter.PayAccountID;
            copy.PaymentMethodID = filter.PayTypeID;
            result = be.Document.Update(copy);
            foreach (ARPayment iPmt in list)
            {
                if (iPmt.CashAccountID != result.CashAccountID || iPmt.PaymentMethodID != iPmt.PaymentMethodID)
                {
                    throw new PXException(AP.Messages.APPaymentDoesNotMatchCABatchByAccountOrPaymentType);
                }
                if (String.IsNullOrEmpty(iPmt.ExtRefNbr) && string.IsNullOrEmpty(filter.NextCheckNbr))
                {
                    throw new PXException(Messages.NextCheckNumberIsRequiredForProcessing);
                }
                CABatchDetail detail = be.AddPayment(iPmt);
            }
            be.Save.Press();
            result = be.Document.Current;
            return result;
        }

        protected virtual void ProcessPayments(List<ARPayment> list, ProcessPaymentFilter filter, PaymentMethod paymenttype)
        {
            if (list.Count == 0)
            {
                return;
            }

            CABatch batch = CreateBatchPayment(list, filter);
            if (batch != null)
            {
                bool failed = false;
                ARPaymentEntryExt pe = PXGraph.CreateInstance<ARPaymentEntryExt>();

                string NextCheckNbr = filter.NextCheckNbr;
                for (int i = 0; i < list.Count; i++)
                {
                    try
                    {
                        AssignNumbers(pe, list[i], ref NextCheckNbr);
                        var docExt = PXCache<ARPayment>.GetExtension<ARPaymentExt>(list[i]);
                        if (docExt.Passed == true)
                            pe.TimeStamp = list[i].tstamp;
                        pe.Save.Press();
                        list[i].tstamp = pe.TimeStamp;
                        pe.Clear();
                    }
                    catch (Exception e)
                    {
                        PXProcessing<ARPayment>.SetError(i, e);
                        failed = true;
                    }
                }
                if (failed)
                {
                    throw new PXOperationCompletedWithErrorException(Messages.ARPaymentsAreAddedToTheBatchButWasNotUpdatedCorrectly, batch.BatchNbr);
                }
                RedirectToResultWithCreateBatch(batch);

            }
        }

        protected virtual void RedirectToResultWithCreateBatch(CABatch batch)
        {
            ARBatchEntry be = PXGraph.CreateInstance<ARBatchEntry>();
            be.Document.Current = be.Document.Search<CABatch.batchNbr>(batch.BatchNbr);
            be.TimeStamp = be.Document.Current.tstamp;
            throw new PXRedirectRequiredException(be, "Redirect");
        }

        protected virtual void ProcessPaymentFilter_PayTypeID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            Filter.Cache.SetDefaultExt<ProcessPaymentFilter.payAccountID>(e.Row);
        }

        protected virtual void ProcessPaymentFilter_PayAccountID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            Filter.Cache.SetDefaultExt<ProcessPaymentFilter.curyID>(e.Row);
        }

        protected virtual void ProcessPaymentFilter_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
        {
            //refresh last number when saved values are populated in filter
            ProcessPaymentFilter oldRow = (ProcessPaymentFilter)e.OldRow;
            ProcessPaymentFilter row = (ProcessPaymentFilter)e.Row;

            if ((oldRow.PayAccountID == null && oldRow.PayTypeID == null)
                || (oldRow.PayAccountID != row.PayAccountID || oldRow.PayTypeID != row.PayTypeID))
            {
                ((ProcessPaymentFilter)e.Row).CurySelTotal = 0m;
                ((ProcessPaymentFilter)e.Row).SelTotal = 0m;
                ((ProcessPaymentFilter)e.Row).SelCount = 0;
                ((ProcessPaymentFilter)e.Row).NextCheckNbr = null;
                ARPaymentList.Cache.Clear();
            }
        }

        protected virtual void ProcessPaymentFilter_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            bool SuggestNextNumber = false;
            ProcessPaymentFilter row = (ProcessPaymentFilter)e.Row;
            PXUIFieldAttribute.SetVisible<ProcessPaymentFilter.curyID>(sender, null, PXAccess.FeatureInstalled<FeaturesSet.multicurrency>());

            if (e.Row != null && cashaccountdetail.Current != null && (!Equals(cashaccountdetail.Current.CashAccountID, row.PayAccountID) || !Equals(cashaccountdetail.Current.PaymentMethodID, row.PayTypeID)))
            {
                cashaccountdetail.Current = null;
                SuggestNextNumber = true;
            }

            if (e.Row != null && paymenttype.Current != null && (!Equals(paymenttype.Current.PaymentMethodID, row.PayTypeID)))
            {
                paymenttype.Current = null;
            }

            if (e.Row != null && string.IsNullOrEmpty(row.NextCheckNbr))
            {
                SuggestNextNumber = true;
            }

            PXUIFieldAttribute.SetVisible<ProcessPaymentFilter.nextCheckNbr>(sender, null, true);

            if (e.Row == null) return;

            if (cashaccountdetail.Current != null && true == cashaccountdetail.Current.ARAutoNextNbr && SuggestNextNumber)
            {
                row.NextCheckNbr = string.IsNullOrEmpty(cashaccountdetail.Current.ARLastRefNbr) == false ? AutoNumberAttribute.NextNumber(cashaccountdetail.Current.ARLastRefNbr) : string.Empty;
            }

            sender.RaiseExceptionHandling<ProcessPaymentFilter.payTypeID>(e.Row, row.PayTypeID,
                paymenttype.Current != null && true != PXCache<PaymentMethod>.GetExtension<PaymentMethodExt>(paymenttype.Current).ARCreateBatchPayment
                ? new PXSetPropertyException(AP.Messages.PaymentTypeNoPrintCheck, PXErrorLevel.Warning)
                : null);

            sender.RaiseExceptionHandling<ProcessPaymentFilter.nextCheckNbr>(e.Row, row.NextCheckNbr,
                paymenttype.Current != null && paymenttype.Current.PrintOrExport == true && String.IsNullOrEmpty(row.NextCheckNbr)
                ? new PXSetPropertyException(AP.Messages.NextCheckNumberIsRequiredForProcessing, PXErrorLevel.Warning)
                : !string.IsNullOrEmpty(row.NextCheckNbr) && !AutoNumberAttribute.CanNextNumber(row.NextCheckNbr)
                    ? new PXSetPropertyException(AP.Messages.NextCheckNumberCanNotBeInc, PXErrorLevel.Warning)
                    : null);

            if (/*HttpContext.Current != null && */Filter.Current.BranchID != PXAccess.GetBranchID())
            {
                Filter.Current.BranchID = PXAccess.GetBranchID();
            }

            ProcessPaymentFilter filter = Filter.Current;
            PaymentMethod pt = paymenttype.Current;
            ARPaymentList.SetProcessTooltip(AR.Messages.Process);
            ARPaymentList.SetProcessAllTooltip(AR.Messages.ProcessAll);
            ARPaymentList.SetProcessDelegate(
                delegate (List<ARPayment> list)
                {
                    var graph = CreateInstance<ARProcessPayment>();
                    graph.ProcessPayments(list, filter, pt);
                }
            );
        }

        protected virtual void ARPayment_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
        {
            ProcessPaymentFilter filter = Filter.Current;
            if (filter != null)
            {
                object OldRow = e.OldRow;
                if (object.ReferenceEquals(e.Row, e.OldRow) && !_copies.TryGetValue(e.Row, out OldRow))
                {
                    decimal? curyval = 0m;
                    decimal? val = 0m;
                    int? count = 0;
                    foreach (ARPayment res in ARPaymentList.Select())
                    {
                        if (res.Selected == true)
                        {
                            curyval += res.CuryOrigDocAmt ?? 0m;
                            val += res.OrigDocAmt ?? 0m;
                            count++;
                        }
                    }

                    filter.CurySelTotal = curyval;
                    filter.SelTotal = val;
                    filter.SelCount = count;
                }
                else
                {
                    ARPayment old_row = OldRow as ARPayment;
                    ARPayment new_row = e.Row as ARPayment;

                    filter.CurySelTotal -= old_row.Selected == true ? old_row.CuryOrigDocAmt : 0m;
                    filter.CurySelTotal += new_row.Selected == true ? new_row.CuryOrigDocAmt : 0m;

                    filter.SelTotal -= old_row.Selected == true ? old_row.OrigDocAmt : 0m;
                    filter.SelTotal += new_row.Selected == true ? new_row.OrigDocAmt : 0m;

                    filter.SelCount -= old_row.Selected == true ? 1 : 0;
                    filter.SelCount += new_row.Selected == true ? 1 : 0;
                }
            }
        }
    }
    [Serializable]
    public partial class ProcessPaymentFilter : PX.Data.IBqlTable
    {
        #region BranchID
        public abstract class branchID : PX.Data.IBqlField
        {
        }
        protected Int32? _BranchID;
        [PXDefault(typeof(AccessInfo.branchID))]
        [Branch(Visible = true, Enabled = true)]
        public virtual Int32? BranchID
        {
            get
            {
                return this._BranchID;
            }
            set
            {
                this._BranchID = value;
            }
        }
        #endregion
        #region PayTypeID
        public abstract class payTypeID : PX.Data.IBqlField
        {
        }
        protected String _PayTypeID;
        [PXDefault()]
        [PXDBString(10, IsUnicode = true)]
        [PXUIField(DisplayName = "Payment Method", Visibility = PXUIVisibility.SelectorVisible)]
        [PXSelector(typeof(Search<PaymentMethod.paymentMethodID,
                          Where<PaymentMethod.useForAR, Equal<True>,
                            And<PaymentMethod.paymentType, NotEqual<PaymentMethodType.creditCard>,
                            And<PaymentMethodExt.aRCreateBatchPayment, Equal<boolTrue>,
                            And<PaymentMethod.isActive, Equal<True>>>>>>))]
        public virtual String PayTypeID
        {
            get
            {
                return this._PayTypeID;
            }
            set
            {
                this._PayTypeID = value;
            }
        }
        #endregion
        #region PayAccountID
        public abstract class payAccountID : PX.Data.IBqlField
        {
        }
        protected Int32? _PayAccountID;
        [CashAccount(typeof(ProcessPaymentFilter.branchID), typeof(Search2<CashAccount.cashAccountID,
                            InnerJoin<PaymentMethodAccount,
                                On<PaymentMethodAccount.cashAccountID, Equal<CashAccount.cashAccountID>>>,
                            Where2<Match<Current<AccessInfo.userName>>,
                            And<CashAccount.clearingAccount, Equal<False>,
                            And<PaymentMethodAccount.paymentMethodID, Equal<Current<ProcessPaymentFilter.payTypeID>>,
                            And<PaymentMethodAccount.useForAR, Equal<True>>>>>>), Visibility = PXUIVisibility.Visible)]
        [PXDefault(typeof(Search2<PaymentMethodAccount.cashAccountID,
                            InnerJoin<CashAccount, On<CashAccount.cashAccountID, Equal<PaymentMethodAccount.cashAccountID>>>,
                                        Where<PaymentMethodAccount.paymentMethodID, Equal<Current<ProcessPaymentFilter.payTypeID>>,
                                            And<PaymentMethodAccount.useForAR, Equal<True>,
                                            And<PaymentMethodAccount.aRIsDefault, Equal<True>,
                                            And<CashAccount.branchID, Equal<Current<AccessInfo.branchID>>>>>>>))]
        public virtual Int32? PayAccountID
        {
            get
            {
                return this._PayAccountID;
            }
            set
            {
                this._PayAccountID = value;
            }
        }
        #endregion

        #region NextCheckNbr
        public abstract class nextCheckNbr : PX.Data.IBqlField
        {
        }
        protected String _NextCheckNbr;
        [PXDBString(15, IsUnicode = true)]
        [PXUIField(DisplayName = "Next Check Number", Visible = false)]
        public virtual String NextCheckNbr
        {
            get
            {
                return this._NextCheckNbr;
            }
            set
            {
                this._NextCheckNbr = value;
            }
        }
        #endregion
        #region Balance
        public abstract class balance : PX.Data.IBqlField
        {
        }
        protected Decimal? _Balance;
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXDBDecimal(4)]
        [PXUIField(DisplayName = "Balance", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual Decimal? Balance
        {
            get
            {
                return this._Balance;
            }
            set
            {
                this._Balance = value;
            }
        }
        #endregion
        #region CurySelTotal
        public abstract class curySelTotal : PX.Data.IBqlField
        {
        }
        protected Decimal? _CurySelTotal;
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXDBCurrency(typeof(ProcessPaymentFilter.curyInfoID), typeof(ProcessPaymentFilter.selTotal), BaseCalc = false)]
        [PXUIField(DisplayName = "Selection Total", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        public virtual Decimal? CurySelTotal
        {
            get
            {
                return this._CurySelTotal;
            }
            set
            {
                this._CurySelTotal = value;
            }
        }
        #endregion
        #region SelTotal
        public abstract class selTotal : PX.Data.IBqlField
        {
        }
        protected Decimal? _SelTotal;
        [PXDBDecimal(4)]
        public virtual Decimal? SelTotal
        {
            get
            {
                return this._SelTotal;
            }
            set
            {
                this._SelTotal = value;
            }
        }
        #endregion
        #region SelCount
        public abstract class selCount : IBqlField { }
        [PXDBInt]
        [PXDefault(0)]
        [PXUIField(DisplayName = "Number of Payments", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        public virtual int? SelCount { get; set; }
        #endregion
        #region CuryID
        public abstract class curyID : PX.Data.IBqlField
        {
        }
        protected String _CuryID;
        [PXDBString(5, IsUnicode = true, InputMask = ">LLLLL")]
        [PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        [PXDefault(typeof(Search<CashAccount.curyID, Where<CashAccount.cashAccountID, Equal<Current<ProcessPaymentFilter.payAccountID>>>>))]
        [PXSelector(typeof(Currency.curyID))]
        public virtual String CuryID
        {
            get
            {
                return this._CuryID;
            }
            set
            {
                this._CuryID = value;
            }
        }
        #endregion
        #region CuryInfoID
        public abstract class curyInfoID : PX.Data.IBqlField
        {
        }
        protected Int64? _CuryInfoID;
        [PXDBLong()]
        [CurrencyInfo(ModuleCode = BatchModule.AR)]
        public virtual Int64? CuryInfoID
        {
            get
            {
                return this._CuryInfoID;
            }
            set
            {
                this._CuryInfoID = value;
            }
        }
        #endregion
        #region CashBalance
        public abstract class cashBalance : PX.Data.IBqlField
        {
        }
        protected Decimal? _CashBalance;
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXDBCury(typeof(ProcessPaymentFilter.curyID))]
        [PXUIField(DisplayName = "Available Balance", Enabled = false)]
        [CashBalance(typeof(ProcessPaymentFilter.payAccountID))]
        public virtual Decimal? CashBalance
        {
            get
            {
                return this._CashBalance;
            }
            set
            {
                this._CashBalance = value;
            }
        }
        #endregion
        #region PayFinPeriodID
        public abstract class payFinPeriodID : PX.Data.IBqlField
        {
        }
        protected string _PayFinPeriodID;
        [FinPeriodID(typeof(AccessInfo.businessDate))]
        [PXUIField(DisplayName = "Post Period", Visibility = PXUIVisibility.Visible)]
        public virtual String PayFinPeriodID
        {
            get
            {
                return this._PayFinPeriodID;
            }
            set
            {
                this._PayFinPeriodID = value;
            }
        }
        #endregion
        #region GLBalance
        public abstract class gLBalance : PX.Data.IBqlField
        {
        }
        protected Decimal? _GLBalance;

        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXDBCury(typeof(ProcessPaymentFilter.curyID))]
        [PXUIField(DisplayName = "GL Balance", Enabled = false)]
        [GLBalance(typeof(ProcessPaymentFilter.payAccountID), typeof(ProcessPaymentFilter.payFinPeriodID))]
        public virtual Decimal? GLBalance
        {
            get
            {
                return this._GLBalance;
            }
            set
            {
                this._GLBalance = value;
            }
        }
        #endregion
    }
}
