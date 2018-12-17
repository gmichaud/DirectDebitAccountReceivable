using PX.Api;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.AR.MigrationMode;
using PX.Objects.CA;
using PX.Objects.CS;
using System;
using System.Collections;
using System.Collections.Generic;

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
        public PXSetup<CASetup> CASetup;
        public ARSetupNoMigrationMode ARSetup;

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
            CASetup caSetup = CASetup.Current;
            ARSetup arSetup = ARSetup.Current;

            RowUpdated.AddHandler<CABatch>(ParentFieldUpdated);
        }

        [PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
        [PXDefault()]
        [PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
        [CABatchType.Numbering()]
        [CABatchType.RefNbr(typeof(Search<CABatch.batchNbr, Where<CABatchExt.batchModule, Equal<GL.BatchModule.moduleAR>>>))]
        protected virtual void CABatch_BatchNbr_CacheAttached(PXCache sender) { }

        public PXAction<CABatch> ViewARDocument;
        //Upgrade to 2018R1 Note : This action was also made Visible = false in CABatchEntry
        [PXUIField(DisplayName = Messages.ViewARDocument, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
        [PXLookupButton]
        public virtual IEnumerable viewARDocument(PXAdapter adapter)
        {

            CABatchDetail detail = this.BatchPayments.Current;
            if (detail == null)
            {
                return adapter.Get();
            }

            ARRegister arDocument = PXSelect<ARRegister,
                                Where<ARRegister.docType, Equal<Required<ARRegister.docType>>,
                            And<ARRegister.refNbr, Equal<Required<ARRegister.refNbr>>>>>.Select(this, detail.OrigDocType, detail.OrigRefNbr);
            if (arDocument == null)
            {
                return adapter.Get();
            }

            ARPaymentEntry arGraph = PXGraph.CreateInstance<ARPaymentEntry>();
            arGraph.Document.Current = arGraph.Document.Search<ARRegister.refNbr>(arDocument.RefNbr, arDocument.DocType);
            if (arGraph.Document.Current != null)
            {
                throw new PXRedirectRequiredException(arGraph, true, "") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }

            return adapter.Get();
        }

        #region CABatch Events
        protected virtual void CABatch_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            CABatch row = e.Row as CABatch;
            if (row == null)
            {
                return;
            }

            CABatchExt rowExt = PXCache<CABatch>.GetExtension<CABatchExt>(row);
            bool isReleased = row.Released == true;

            PXUIFieldAttribute.SetEnabled(sender, row, false);
            PXUIFieldAttribute.SetEnabled<CABatch.batchNbr>(sender, row, true);
            PXUIFieldAttribute.SetEnabled<CABatch.exportFileName>(sender, row, IsExport);
            PXUIFieldAttribute.SetEnabled<CABatch.exportTime>(sender, row, IsExport);

            bool allowDelete = !isReleased;
            if (allowDelete)
            {
                allowDelete = !(this.ReleasedPayments.Select(row.BatchNbr).Count > 0);
            }
            sender.AllowDelete = allowDelete;

            CashAccount cashaccount = (CashAccount)PXSelectorAttribute.Select<CABatch.cashAccountID>(sender, row);
            bool clearEnabled = row.Released != true && cashaccount?.Reconcile == true;

            PXUIFieldAttribute.SetEnabled<CABatch.hold>(sender, row, !isReleased);
            PXUIFieldAttribute.SetEnabled<CABatch.tranDesc>(sender, row, !isReleased);
            PXUIFieldAttribute.SetEnabled<CABatch.tranDate>(sender, row, !isReleased);
            PXUIFieldAttribute.SetEnabled<CABatch.batchSeqNbr>(sender, row, !isReleased);
            PXUIFieldAttribute.SetEnabled<CABatch.extRefNbr>(sender, row, !isReleased);

            if (!isReleased)
            {
                bool hasDetails = BatchPayments.Select().Count > 0;
                PXUIFieldAttribute.SetEnabled<CABatch.paymentMethodID>(sender, row, !hasDetails);
                PXUIFieldAttribute.SetEnabled<CABatch.cashAccountID>(sender, row, !hasDetails);
                if (hasDetails)
                {
                    decimal? curyTotal = 0m, total = 0m;
                    CalcDetailsTotal(ref curyTotal, ref total);
                    row.DetailTotal = total;
                    row.CuryDetailTotal = curyTotal;
                }

            }
            PXUIFieldAttribute.SetVisible<CABatch.curyDetailTotal>(sender, row, isReleased);
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

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "Type")]
        [ARDocType.List()]
        public virtual void CABatchDetail_OrigDocType_CacheAttached(PXCache sender)
        {
        }

        protected virtual void CABatchDetail_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
        {
            CABatchDetail row = (CABatchDetail)e.Row;
            bool isReleased = false;

            ARRegister document = PXSelect<ARRegister, Where<ARRegister.docType, Equal<Required<ARRegister.docType>>,
                                        And<ARRegister.refNbr, Equal<Required<ARRegister.refNbr>>>>>.Select(this, row.OrigDocType, row.OrigRefNbr);
            isReleased = (bool)document.Released;

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

            ARRegister document = PXSelect<ARRegister, Where<ARRegister.docType, Equal<Required<ARRegister.docType>>,
                                        And<ARRegister.refNbr, Equal<Required<ARRegister.refNbr>>>>>.Select(this, row.OrigDocType, row.OrigRefNbr);
            isReleased = (bool)document.Released;
            isVoided = (bool)document.Voided;

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
            CABatch document = this.Document.Current;
            if (row.OrigDocType != null && row.OrigRefNbr != null)
            {
                decimal? curyAmount = null, amount = null;
                ARPayment payment = PXSelect<ARPayment,
                        Where<ARPayment.docType, Equal<Required<ARPayment.docType>>,
                        And<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>>>>.Select(this, row.OrigDocType, row.OrigRefNbr);
                if (payment != null)
                {
                    curyAmount = payment.CuryOrigDocAmt;
                    amount = payment.OrigDocAmt;
                }

                CABatch copy = (CABatch)this.Document.Cache.CreateCopy(document);
                if (curyAmount.HasValue)
                {
                    document.CuryDetailTotal += negative ? -curyAmount : curyAmount;
                }
                if (amount.HasValue)
                {
                    document.DetailTotal += negative ? -amount : amount;
                }

                document = this.Document.Update(document);
            }
            return document;
        }


        #endregion
        #region Buttons
        public PXAction<CABatch> Release;
        [PXUIField(DisplayName = CA.Messages.Release, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
        [PXProcessButton]
        public virtual IEnumerable release(PXAdapter adapter)
        {
            CheckPrevOperation();
            Save.Press();
            CABatch document = this.Document.Current;
            if (document.Released == false && document.Hold == false)
            {
                PXLongOperation.StartOperation(this, delegate () { ReleaseDoc(document); });
            }

            return adapter.Get();
        }

        private void CheckPrevOperation()
        {
            if (PXLongOperation.Exists(UID))
            {
                throw new ApplicationException(GL.Messages.PrevOperationNotCompleteYet);
            }
        }

        public PXAction<CABatch> Export;
        [PXUIField(DisplayName = CA.Messages.Export, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
        [PXProcessButton]
        public virtual IEnumerable export(PXAdapter adapter)
        {
            CheckPrevOperation();
            CABatch document = this.Document.Current;
            if (document != null && document.Released == true && document.Hold == false)
            {
                PXResult<PaymentMethod, SYMapping> res = (PXResult<PaymentMethod, SYMapping>)PXSelectJoin<PaymentMethod,
                                    LeftJoin<SYMapping, On<SYMapping.mappingID, Equal<PaymentMethodExt.aRBatchExportSYMappingID>>>,
                                        Where<PaymentMethod.paymentMethodID, Equal<Optional<CABatch.paymentMethodID>>>>.Select(this, document.PaymentMethodID);
                PaymentMethod pt = res;
                PaymentMethodExt ptx = PXCache<PaymentMethod>.GetExtension<PaymentMethodExt>(pt);
                SYMapping map = res;
                if (ptx != null && ptx.ARCreateBatchPayment == true && ptx.ARBatchExportSYMappingID != null && map != null)
                {
                    string defaultFileName = this.GenerateFileName(document);
                    PXLongOperation.StartOperation(this, delegate ()
                    {

                        PX.Api.SYExportProcess.RunScenario(map.Name, SYMapping.RepeatingOption.All,
                            true,
                            true,
                            new PX.Api.PXSYParameter(CABatchEntry.ExportProviderParams.FileName, defaultFileName),
                            new PX.Api.PXSYParameter(CABatchEntry.ExportProviderParams.BatchNbr, document.BatchNbr));
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
                    return string.Format(CA.Messages.CABatchDefaultExportFilenameTemplate, aBatch.PaymentMethodID, acct.CashAccountCD, aBatch.TranDate.Value, aBatch.DateSeqNbr);
                }
            }
            return string.Empty;
        }

        public virtual void CalcDetailsTotal(ref decimal? aCuryTotal, ref decimal? aTotal)
        {
            aCuryTotal = 0m;
            aTotal = 0m;
            foreach (PXResult<CABatchDetail, ARPayment> item in this.BatchPayments.Select())
            {
                ARPayment payment = item;
                if (!string.IsNullOrEmpty(payment.RefNbr))
                {
                    aCuryTotal += payment.CuryOrigDocAmt;
                    aTotal += payment.OrigDocAmt;
                }
            }
        }
        #endregion

        #region Static Methods
        public static void ReleaseDoc(CABatch aDocument)
        {
            if ((bool)aDocument.Released || (bool)aDocument.Hold)
            {
                throw new PXException(CA.Messages.CABatchStatusIsNotValidForProcessing);
            }

            ARBatchUpdate batchEntry = PXGraph.CreateInstance<ARBatchUpdate>();
            CABatch document = batchEntry.Document.Select(aDocument.BatchNbr);
            batchEntry.Document.Current = document;

            if ((bool)document.Released || (bool)document.Hold)
            {
                throw new PXException(CA.Messages.CABatchStatusIsNotValidForProcessing);
            }

            ARPayment voided = PXSelectReadonly2<ARPayment,
                            InnerJoin<CABatchDetail, On<CABatchDetail.origDocType, Equal<ARPayment.docType>,
                            And<CABatchDetail.origRefNbr, Equal<ARPayment.refNbr>,
                            And<CABatchDetail.origModule, Equal<GL.BatchModule.moduleAR>>>>>,
                            Where<CABatchDetail.batchNbr, Equal<Required<CABatch.batchNbr>>,
                                And<ARPayment.voided, Equal<True>>>>.Select(batchEntry, document.BatchNbr);
            if (voided != null && string.IsNullOrEmpty(voided.RefNbr) == false)
            {
                throw new PXException(CA.Messages.CABatchContainsVoidedPaymentsAndConnotBeReleased);
            }

            List<ARRegister> unreleasedList = new List<ARRegister>();
            PXSelectBase<ARPayment> selectUnreleased = new PXSelectReadonly2<ARPayment,
                            InnerJoin<CABatchDetail, On<CABatchDetail.origDocType, Equal<ARPayment.docType>,
                            And<CABatchDetail.origRefNbr, Equal<ARPayment.refNbr>,
                            And<CABatchDetail.origModule, Equal<GL.BatchModule.moduleAR>>>>>,
                            Where<CABatchDetail.batchNbr, Equal<Optional<CABatch.batchNbr>>,
                                And<ARPayment.released, Equal<boolFalse>>>>(batchEntry);

            foreach (ARPayment item in selectUnreleased.Select(document.BatchNbr))
            {
                if (item.Released != true)
                {
                    unreleasedList.Add(item);
                }
            }

            if (unreleasedList.Count > 0)
            {
                ARDocumentRelease.ReleaseDoc(unreleasedList, true);
            }

            selectUnreleased.View.Clear();
            ARPayment payment = selectUnreleased.Select(document.BatchNbr);
            if (payment != null)
            {
                throw new PXException(CA.Messages.CABatchContainsUnreleasedPaymentsAndCannotBeReleased);
            }

            document.Released = true;
            document.DateSeqNbr = CABatchEntry.GetNextDateSeqNbr(batchEntry, aDocument); //Nothing AP specific in this static function
            batchEntry.RecalcTotals();
            document = batchEntry.Document.Update(document);
            batchEntry.Actions.PressSave();
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
                    row.DetailTotal = row.CuryDetailTotal = row.Total = decimal.Zero;
                    foreach (PXResult<ARPayment, CABatchDetail> item in this.ARPaymentList.Select())
                    {
                        ARPayment payment = item;
                        if (!string.IsNullOrEmpty(payment.RefNbr))
                        {
                            row.CuryDetailTotal += payment.CuryOrigDocAmt;
                            row.DetailTotal += payment.OrigDocAmt;
                        }
                    }

                }
            }
        }

        #endregion
    }
}
