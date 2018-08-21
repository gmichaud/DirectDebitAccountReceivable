using PX.Api;
using PX.Data;
using PX.Objects.CA;
using System;

namespace PX.Objects.ACH
{
    [PXTable(typeof(PaymentMethod.paymentMethodID), IsOptional = true)]
    public class PaymentMethodExt : PXCacheExtension<PaymentMethod>
    {
        #region ARCreateBatchPayment
        public abstract class aRCreateBatchPayment : PX.Data.IBqlField
        {
        }

        [PXDBBool()]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Create Batch Payment")]
        public virtual Boolean? ARCreateBatchPayment { get; set; }
        #endregion

        #region ARBatchExportSYMappingID
        public abstract class aRBatchExportSYMappingID : PX.Data.IBqlField
        {
        }

        [PXDBGuid]
        [PXUIField(DisplayName = "Export Scenario", Visibility = PXUIVisibility.Visible)]
        [PXSelector(typeof(Search<SYMapping.mappingID, Where<SYMapping.mappingType, Equal<SYMapping.mappingType.typeExport>>>), SubstituteKey = typeof(SYMapping.name))]
        public virtual Guid? ARBatchExportSYMappingID { get; set; }
        #endregion
    }
}
