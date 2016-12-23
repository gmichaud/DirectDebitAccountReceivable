using PX.Api;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.SM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Compilation;

namespace PX.Objects.ACH
{
    public class ARBatchEntry : PXGraph<ARBatchEntry, CABatch>
    {
        public PXSelect<CABatch, Where<CABatchExt.batchModule, Equal<GL.BatchModule.moduleAR>>> Document;
        public PXSelect<CABatchDetail, Where<CABatchDetail.batchNbr, Equal<Current<CABatch.batchNbr>>>> Details;

        public PXSelectJoin<CABatchDetail,
                            LeftJoin<ARPayment, On<CABatchDetail.origDocType, Equal<ARPayment.docType>,
                            And<CABatchDetail.origRefNbr, Equal<ARPayment.refNbr>>>>,
                            Where<CABatchDetail.batchNbr, Equal<Current<CABatch.batchNbr>>>> BatchPayments;

        public PXSelect<CashAccount, Where<CashAccount.cashAccountID, Equal<Current<CABatch.cashAccountID>>>> cashAccount;
        public PXSetup<CASetup> casetup;

        public PXSelectJoin<CABatchDetail,
                    InnerJoin<ARPayment, On<ARPayment.docType, Equal<CABatchDetail.origDocType>,
                    And<ARPayment.refNbr, Equal<CABatchDetail.origRefNbr>,
                    And<ARPayment.released, Equal<True>>>>>,
                    Where<CABatchDetail.batchNbr, Equal<Current<CABatch.batchNbr>>>> ReleasedPayments;

        #region AchFileCreation
        public PXSelectJoin<ARPayment,
                InnerJoin<CABatchDetail, On<CABatchDetail.origDocType, Equal<ARPayment.docType>,
                And<CABatchDetail.origRefNbr, Equal<ARPayment.refNbr>,
                And<CABatchDetail.origModule, Equal<GL.BatchModule.moduleAR>>>>>,
                Where<CABatchDetail.batchNbr, Equal<Current<CABatch.batchNbr>>>> ARPaymentList;

        public PXSelectReadonly<CashAccountPaymentMethodDetail,
                Where<CashAccountPaymentMethodDetail.paymentMethodID, Equal<Current<CABatch.paymentMethodID>>,
                And<Current<ARPayment.docType>, IsNotNull,
                And<Current<ARPayment.refNbr>, IsNotNull,
                And<CashAccountPaymentMethodDetail.accountID, Equal<Current<CABatch.cashAccountID>>,
                And<CashAccountPaymentMethodDetail.detailID, Equal<Required<CashAccountPaymentMethodDetail.detailID>>>>>>>> cashAccountSettings;

        public PXSelectReadonly2<CustomerPaymentMethodDetail,
                InnerJoin<CustomerPaymentMethod, 
                    On<CustomerPaymentMethod.pMInstanceID, 
                        Equal<CustomerPaymentMethodDetail.pMInstanceID>, 
                    And<CustomerPaymentMethod.paymentMethodID, 
                        Equal<CustomerPaymentMethodDetail.paymentMethodID>>>>,
                Where<CustomerPaymentMethodDetail.paymentMethodID, Equal<Current<CABatch.paymentMethodID>>,
                    And<Current<ARPayment.docType>, IsNotNull,
                    And<Current<ARPayment.refNbr>, IsNotNull,
                    And<CustomerPaymentMethod.pMInstanceID, Equal<Current<ARPayment.pMInstanceID>>,
                    And<CustomerPaymentMethod.bAccountID, Equal<Current<ARPayment.customerID>>,
                    And<CustomerPaymentMethodDetail.detailID, Equal<Required<CustomerPaymentMethodDetail.detailID>>>>>>>>> customerPaymentSettings;

        #endregion

        public ARBatchEntry()
        {
            CASetup setup = casetup.Current;
            RowUpdated.AddHandler<CABatch>(ParentFieldUpdated);
        }

        [PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
        [PXDefault()]
        [PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
        [CABatchType.Numbering()]
        [CABatchType.RefNbr(typeof(Search<CABatch.batchNbr, Where<CABatchExt.batchModule, Equal<GL.BatchModule.moduleAR>>>))]
        protected virtual void CABatch_BatchNbr_CacheAttached(PXCache sender) { }

        public PXAction<CABatch> viewARDocument;
        [PXUIField(DisplayName = Messages.ViewARDocument, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = true)]
        [PXLookupButton]
        public virtual IEnumerable ViewARDocument(PXAdapter adapter)
        {

            CABatchDetail doc = this.BatchPayments.Current;
            if (doc != null)
            {
                ARRegister arDoc = PXSelect<ARRegister,
                                    Where<ARRegister.docType, Equal<Required<ARRegister.docType>>,
                                And<ARRegister.refNbr, Equal<Required<ARRegister.refNbr>>>>>.Select(this, doc.OrigDocType, doc.OrigRefNbr);
                if (arDoc != null)
                {
                    ARPaymentEntry apGraph = PXGraph.CreateInstance<ARPaymentEntry>();
                    apGraph.Document.Current = apGraph.Document.Search<ARRegister.refNbr>(arDoc.RefNbr, arDoc.DocType);
                    if (apGraph.Document.Current != null)
                        throw new PXRedirectRequiredException(apGraph, true, "") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                }
            }
            return adapter.Get();
        }

        #region CABatch Events
        protected virtual void CABatch_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            CABatch row = e.Row as CABatch;
            if (row == null) return;
            CABatchExt rowExt = PXCache<CABatch>.GetExtension<CABatchExt>(row);
            bool isReleased = (row.Released == true);


            PXUIFieldAttribute.SetEnabled(sender, row, false);
            PXUIFieldAttribute.SetEnabled<CABatch.batchNbr>(sender, row, true);

            bool isProcessing = row.Processing ?? false;
            PXUIFieldAttribute.SetEnabled<CABatch.processing>(sender, row, true);

            bool allowDelete = !isReleased;
            if (allowDelete)
            {
                allowDelete = !(this.ReleasedPayments.Select(row.BatchNbr).Count > 0);
            }
            sender.AllowDelete = allowDelete;

            CashAccount cashaccount = (CashAccount)PXSelectorAttribute.Select<CABatch.cashAccountID>(sender, row);
            bool clearEnabled = (row.Released != true) && (cashaccount != null) && (cashaccount.Reconcile == true);

            if (!isReleased)
            {
                PXUIFieldAttribute.SetEnabled<CABatch.hold>(sender, row, !isReleased);
                PXUIFieldAttribute.SetEnabled<CABatch.tranDesc>(sender, row, !isReleased);
                PXUIFieldAttribute.SetEnabled<CABatch.tranDate>(sender, row, !isReleased);
                PXUIFieldAttribute.SetEnabled<CABatch.batchSeqNbr>(sender, row, !isReleased);
                PXUIFieldAttribute.SetEnabled<CABatch.extRefNbr>(sender, row, !isReleased);
                PXUIFieldAttribute.SetEnabled<CABatch.released>(sender, row, true);

                bool hasDetails = this.BatchPayments.Select().Count > 0;
                PXUIFieldAttribute.SetEnabled<CABatch.paymentMethodID>(sender, row, !hasDetails && !isReleased);
                PXUIFieldAttribute.SetEnabled<CABatch.cashAccountID>(sender, row, !hasDetails && !isReleased);
                if (hasDetails)
                {
                    decimal? curyTotal = Decimal.Zero, total = Decimal.Zero;
                    this.CalcDetailsTotal(ref curyTotal, ref total);
                    row.DetailTotal = total;
                    row.CuryTotal = curyTotal;
                }

            }
            PXUIFieldAttribute.SetVisible<CABatch.curyDetailTotal>(sender, row, isReleased);
            PXUIFieldAttribute.SetVisible<CABatch.curyTotal>(sender, row, !isReleased);
            PXUIFieldAttribute.SetEnabled<CABatch.exportFileName>(sender, row, isProcessing);
            PXUIFieldAttribute.SetEnabled<CABatch.exportTime>(sender, row, isProcessing);
            PXUIFieldAttribute.SetVisible<CABatch.dateSeqNbr>(sender, row, isReleased);

            this.Release.SetEnabled(!isReleased && (row.Hold == false));
            this.Export.SetEnabled(isReleased);
        }

        protected virtual void CABatch_CashAccountID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            CABatch row = (CABatch)e.Row;
            row.Cleared = false;
            row.ClearDate = null;
            if (cashAccount.Current == null || cashAccount.Current.CashAccountID != row.CashAccountID)
            {
                cashAccount.Current = (CashAccount)PXSelectorAttribute.Select<CABatch.cashAccountID>(sender, row);
            }
            if (cashAccount.Current.Reconcile != true)
            {
                row.Cleared = true;
                row.ClearDate = row.TranDate;
            }
            sender.SetDefaultExt<CABatch.referenceID>(e.Row);
            sender.SetDefaultExt<CABatch.paymentMethodID>(e.Row);
        }

        protected virtual void CABatch_PaymentMethodID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            CABatch row = (CABatch)e.Row;
            sender.SetDefaultExt<CABatch.batchSeqNbr>(e.Row);
        }

        protected virtual void CABatch_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
        {
            this._isMassDelete = false;
        }

        private bool _isMassDelete = false;
        protected virtual void CABatch_RowDeleting(PXCache sender, PXRowDeletingEventArgs e)
        {
            this._isMassDelete = true;
        }

        #endregion
        #region CABatch Detail events

        protected virtual void CABatchDetail_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
        {
            CABatchDetail row = (CABatchDetail)e.Row;
            bool isReleased = false;

            ARRegister doc = PXSelect<ARRegister, Where<ARRegister.docType, Equal<Required<ARRegister.docType>>,
                                        And<ARRegister.refNbr, Equal<Required<ARRegister.refNbr>>>>>.Select(this, row.OrigDocType, row.OrigRefNbr);
            isReleased = (bool)doc.Released;
            
            if (isReleased)
                throw new PXException(CA.Messages.ReleasedDocumentMayNotBeAddedToCABatch);
        }

        protected virtual void CABatchDetail_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
        {
            CABatchDetail row = (CABatchDetail)e.Row;
            UpdateDocAmount(row, false);
        }

        protected virtual void CABatchDetail_RowDeleting(PXCache sender, PXRowDeletingEventArgs e)
        {
            CABatchDetail row = (CABatchDetail)e.Row;
            bool isReleased = false;
            bool isVoided = false;

            ARRegister doc = PXSelect<ARRegister, Where<ARRegister.docType, Equal<Required<ARRegister.docType>>,
                                        And<ARRegister.refNbr, Equal<Required<ARRegister.refNbr>>>>>.Select(this, row.OrigDocType, row.OrigRefNbr);
            isReleased = (bool)doc.Released;
            isVoided = (bool)doc.Voided;
            
            if (isReleased && !isVoided)
                throw new PXException(CA.Messages.ReleasedDocumentMayNotBeDeletedFromCABatch);
        }

        protected virtual void CABatchDetail_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
        {
            CABatchDetail row = (CABatchDetail)e.Row;
            if (!this._isMassDelete)
            {
                UpdateDocAmount(row, true);
            }
        }

        private CABatch UpdateDocAmount(CABatchDetail row, bool negative)
        {
            CABatch doc = this.Document.Current;
            if (row.OrigDocType != null && row.OrigRefNbr != null)
            {
                decimal? curyAmount = null, amount = null;
                ARPayment pmt = PXSelect<ARPayment,
                        Where<ARPayment.docType, Equal<Required<ARPayment.docType>>,
                        And<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>>>>.Select(this, row.OrigDocType, row.OrigRefNbr);
                if (pmt != null)
                {
                    curyAmount = pmt.CuryOrigDocAmt;
                    amount = pmt.OrigDocAmt;
                }
                
                CABatch copy = (CABatch)this.Document.Cache.CreateCopy(doc);
                if (curyAmount.HasValue)
                    doc.CuryDetailTotal += negative ? -curyAmount : curyAmount;
                if (amount.HasValue)
                    doc.DetailTotal += negative ? -amount : amount;
                doc = this.Document.Update(doc);
            }
            return doc;
        }


        #endregion
        #region Buttons
        public PXAction<CABatch> Release;
        [PXUIField(DisplayName = CA.Messages.Release, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
        [PXProcessButton]
        public virtual IEnumerable release(PXAdapter adapter)
        {
            if (PXLongOperation.Exists(UID))
            {
                throw new ApplicationException(GL.Messages.PrevOperationNotCompleteYet);
            }
            Save.Press();
            CABatch doc = this.Document.Current;
            if (doc.Released == false && doc.Hold == false)
            {
                PXLongOperation.StartOperation(this, delegate () { ReleaseDoc(doc); });
            }

            return adapter.Get();
        }

        public PXAction<CABatch> Export;
        [PXUIField(DisplayName = CA.Messages.Export, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
        [PXProcessButton]
        public virtual IEnumerable export(PXAdapter adapter)
        {
            if (PXLongOperation.Exists(UID))
            {
                throw new ApplicationException(GL.Messages.PrevOperationNotCompleteYet);
            }
            CABatch doc = this.Document.Current;
            if (doc != null && doc.Released == true && doc.Hold == false)
            {
                PXResult<PaymentMethod, SYMapping> res = (PXResult<PaymentMethod, SYMapping>)PXSelectJoin<PaymentMethod,
                                    LeftJoin<SYMapping, On<SYMapping.mappingID, Equal<PaymentMethodExt.aRBatchExportSYMappingID>>>,
                                        Where<PaymentMethod.paymentMethodID, Equal<Optional<CABatch.paymentMethodID>>>>.Select(this, doc.PaymentMethodID);
                PaymentMethod pt = res;
                PaymentMethodExt ptx = PXCache<PaymentMethod>.GetExtension<PaymentMethodExt>(pt);
                SYMapping map = res;
                if (ptx != null && ptx.ARCreateBatchPayment == true && ptx.ARBatchExportSYMappingID != null && map != null)
                {
                    string defaultFileName = this.GenerateFileName(doc);
                    PXLongOperation.StartOperation(this, delegate ()
                    {

                        PX.Api.SYExportProcess.RunScenario(map.Name, SYMapping.RepeatingOption.All, true, true,
                            new PX.Api.PXSYParameter(CABatchEntry.ExportProviderParams.FileName, defaultFileName),
                            new PX.Api.PXSYParameter(CABatchEntry.ExportProviderParams.BatchNbr, doc.BatchNbr)

                            //,
                            //new PX.Api.PXSYParameter(ExportProviderParams.BatchSequenceStartingNbr, "0000")
                            );
                    });
                }
                else
                {
                    throw new PXException(CA.Messages.CABatchExportProviderIsNotConfigured);
                }
            }

            return adapter.Get();
        }

        #endregion
        #region Methods
        public virtual CABatchDetail AddPayment(ARPayment aPayment)
        {
            CABatchDetail detail = new CABatchDetail();
            detail.Copy(aPayment);
            detail = this.BatchPayments.Insert(detail);
            return detail;
        }

        protected virtual void ParentFieldUpdated(PXCache sender, PXRowUpdatedEventArgs e)
        {
            if (!sender.ObjectsEqual<CABatch.tranDate>(e.Row, e.OldRow))
            {
                foreach (CABatchDetail tran in this.Details.Select())
                {
                    if (this.Details.Cache.GetStatus(tran) == PXEntryStatus.Notchanged)
                    {
                        this.Details.Cache.SetStatus(tran, PXEntryStatus.Updated);
                    }
                }
            }
        }
        #endregion
        #region Internal Utilities
        public virtual string GenerateFileName(CABatch aBatch)
        {
            if (aBatch.CashAccountID != null && !string.IsNullOrEmpty(aBatch.PaymentMethodID))
            {
                CashAccount acct = PXSelect<CashAccount, Where<CashAccount.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>>.Select(this, aBatch.CashAccountID);
                if (acct != null)
                {
                    return String.Format(CA.Messages.CABatchDefaultExportFilenameTemplate, aBatch.PaymentMethodID, acct.CashAccountCD, aBatch.TranDate.Value, aBatch.DateSeqNbr);
                }
            }
            return string.Empty;
        }

        public virtual void CalcDetailsTotal(ref decimal? aCuryTotal, ref decimal? aTotal)
        {
            aCuryTotal = Decimal.Zero;
            aTotal = Decimal.Zero;
            foreach (PXResult<CABatchDetail, ARPayment> it in this.BatchPayments.Select())
            {
                ARPayment pmt = it;
                if (!String.IsNullOrEmpty(pmt.RefNbr))
                {
                    aCuryTotal += pmt.CuryOrigDocAmt;
                    aTotal += pmt.OrigDocAmt;
                }
            }
        }
        #endregion

        #region Static Methods
        public static void ReleaseDoc(CABatch aDoc)
        {
            if ((bool)aDoc.Released || (bool)aDoc.Hold)
                throw new PXException(CA.Messages.CABatchStatusIsNotValidForProcessing);
            ARBatchUpdate be = PXGraph.CreateInstance<ARBatchUpdate>();
            CABatch doc = be.Document.Select(aDoc.BatchNbr);
            be.Document.Current = doc;
            if ((bool)doc.Released || (bool)doc.Hold)
                throw new PXException(CA.Messages.CABatchStatusIsNotValidForProcessing);

            ARPayment voided = PXSelectReadonly2<ARPayment,
                            InnerJoin<CABatchDetail, On<CABatchDetail.origDocType, Equal<ARPayment.docType>,
                            And<CABatchDetail.origRefNbr, Equal<ARPayment.refNbr>,
                            And<CABatchDetail.origModule, Equal<GL.BatchModule.moduleAR>>>>>,
                            Where<CABatchDetail.batchNbr, Equal<Required<CABatch.batchNbr>>,
                                And<ARPayment.voided, Equal<True>>>>.Select(be, doc.BatchNbr);
            if (voided != null && String.IsNullOrEmpty(voided.RefNbr) == false)
            {
                throw new PXException(CA.Messages.CABatchContainsVoidedPaymentsAndConnotBeReleased);
            }

            List<ARRegister> unreleasedList = new List<ARRegister>();
            PXSelectBase<ARPayment> selectUnreleased = new PXSelectReadonly2<ARPayment,
                            InnerJoin<CABatchDetail, On<CABatchDetail.origDocType, Equal<ARPayment.docType>,
                            And<CABatchDetail.origRefNbr, Equal<ARPayment.refNbr>,
                            And<CABatchDetail.origModule, Equal<GL.BatchModule.moduleAR>>>>>,
                            Where<CABatchDetail.batchNbr, Equal<Optional<CABatch.batchNbr>>,
                                And<ARPayment.released, Equal<boolFalse>>>>(be);
            foreach (ARPayment iPmt in selectUnreleased.Select(doc.BatchNbr))
            {
                if (iPmt.Released != true)
                {
                    unreleasedList.Add(iPmt);
                }
            }
            if (unreleasedList.Count > 0)
            {
                ARDocumentRelease.ReleaseDoc(unreleasedList, true);
            }

            selectUnreleased.View.Clear();
            ARPayment pmt = selectUnreleased.Select(doc.BatchNbr);
            if (pmt != null)
            {
                throw new PXException(CA.Messages.CABatchContainsUnreleasedPaymentsAndCannotBeReleased);
            }
            doc.Released = true;
            doc.DateSeqNbr = CABatchEntry.GetNextDateSeqNbr(be, aDoc); //Nothing AP specific in this static function
            be.RecalcTotals();
            doc = be.Document.Update(doc);
            be.Actions.PressSave();
        }
        #endregion

        #region Processing Graph Definition
        [PXHidden]
        public class ARBatchUpdate : PXGraph<ARBatchUpdate>
        {
            public PXSelect<CABatch, Where<CABatch.batchNbr, Equal<Required<CABatch.batchNbr>>>> Document;
            public PXSelectJoin<ARPayment,
                            InnerJoin<CABatchDetail, On<CABatchDetail.origDocType, Equal<ARPayment.docType>,
                            And<CABatchDetail.origRefNbr, Equal<ARPayment.refNbr>,
                            And<CABatchDetail.origModule, Equal<GL.BatchModule.moduleAR>>>>>,
                            Where<CABatchDetail.batchNbr, Equal<Optional<CABatch.batchNbr>>>> ARPaymentList;
            public virtual void RecalcTotals()
            {
                CABatch row = this.Document.Current;
                if (row != null)
                {
                    row.DetailTotal = row.CuryDetailTotal = row.CuryTotal = row.Total = decimal.Zero;
                    foreach (PXResult<ARPayment, CABatchDetail> it in this.ARPaymentList.Select())
                    {
                        ARPayment pmt = it;
                        if (!String.IsNullOrEmpty(pmt.RefNbr))
                        {
                            row.CuryDetailTotal += pmt.CuryOrigDocAmt;
                            row.DetailTotal += pmt.OrigDocAmt;
                        }
                    }

                }
            }
        }

        #endregion
    }
}
