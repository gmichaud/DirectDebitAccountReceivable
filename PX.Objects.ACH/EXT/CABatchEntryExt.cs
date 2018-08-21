using PX.Data;
using PX.Objects.CA;

namespace PX.Objects.ACH
{
    public class CABatchEntryExt : PXGraphExtension<CABatchEntry>
    {
        //We only want to keep listing AP Batch in the original page

        [PXOverride]
        public PXSelect<CABatch, Where<CABatchExt.batchModule, IsNull, Or<CABatchExt.batchModule, Equal<GL.BatchModule.moduleAP>>>> Document;

        [PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
        [PXDefault()]
        [PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
        [CABatchType.Numbering()]
        [CABatchType.RefNbr(typeof(Search<CABatch.batchNbr, Where<CABatchExt.batchModule, IsNull, Or<CABatchExt.batchModule, Equal<GL.BatchModule.moduleAP>>>>))]
        protected virtual void CABatch_BatchNbr_CacheAttached(PXCache sender) { }
    }
}
