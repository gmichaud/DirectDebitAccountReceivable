using PX.Data;
using PX.Objects.AR;
using PX.Objects.CA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
