using PX.Data;
using PX.Objects.AR;

namespace PX.Objects.ACH
{
    public class CustomerPaymentMethodDetailExt: PXCacheExtension<CustomerPaymentMethodDetail>
    {
        #region Value  

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [DecryptACHValues(typeof(CustomerPaymentMethodDetail.paymentMethodID))]
        public string Value { get; set; }

        #endregion
    }
}
