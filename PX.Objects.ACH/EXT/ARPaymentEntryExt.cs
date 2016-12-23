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
    public class ARPaymentEntryExt : ARPaymentEntry
    {
        public PXSetup<PaymentMethodAccount, Where<PaymentMethodAccount.cashAccountID, Equal<Optional<ARPayment.cashAccountID>>, And<PaymentMethodAccount.paymentMethodID, Equal<Current<ARPayment.paymentMethodID>>>>> cashaccountdetail;
        
        //we want to put this method public so we can access it during ARProcessPayment.AssignNumbers
        public new void ARAdjust_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
        {
            base.ARAdjust_RowPersisting(sender, e);
        }
    }
}
